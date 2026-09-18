using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record FirewallLoggingProfile(string Name, string Allowed, string Blocked, string Ignored,
    int? MaximumKilobytes, bool HasLogFilePath);

public static class FirewallLoggingDiagnostics
{
    private const string Scope = " Effective rules, packet capture, third-party filtering and log contents are not inspected. No firewall or logging setting was changed.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Firewall logging collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$profiles=@(Get-NetFirewallProfile -PolicyStore ActiveStore -ErrorAction Stop|Where-Object Name -in @('Domain','Private','Public')|Select-Object -First 4|ForEach-Object{" +
            "[pscustomobject]@{Name=$_.Name;Allowed=if($null -eq $_.LogAllowed){$null}else{$_.LogAllowed.ToString()};" +
            "Blocked=if($null -eq $_.LogBlocked){$null}else{$_.LogBlocked.ToString()};" +
            "Ignored=if($null -eq $_.LogIgnored){$null}else{$_.LogIgnored.ToString()};" +
            "MaximumKilobytes=if($null -eq $_.LogMaxSizeKilobytes){$null}else{$_.LogMaxSizeKilobytes.ToString([Globalization.CultureInfo]::InvariantCulture)};" +
            "HasLogFilePath=-not [string]::IsNullOrWhiteSpace([string]$_.LogFileName)}});" +
            "[pscustomobject]@{profiles=$profiles}|ConvertTo-Json -Depth 4 -Compress";
        ToolResult result;
        try
        {
            result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            result = new(-1, "", error.Message, false);
        }
        token.ThrowIfCancellationRequested();
        return Interpret(result);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        WorkflowCheck Unknown(string detail) => new("Windows Firewall logging", CheckState.Unknown, detail + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Firewall logging collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("profiles", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 3)
                return [Unknown("The three firewall logging profiles were not collected completely.")];
            var profiles = new List<FirewallLoggingProfile>(3);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows.EnumerateArray())
            {
                if (!TryRead(row, out var profile) || !seen.Add(profile.Name))
                    return [Unknown("Firewall logging identities or settings were invalid or duplicated.")];
                profiles.Add(profile);
            }
            return profiles.OrderBy(profile => profile.Name, StringComparer.Ordinal).Select(profile =>
            {
                bool writes = profile.Allowed == "True" || profile.Blocked == "True" || profile.Ignored == "True";
                bool coherent = !writes || profile.HasLogFilePath && profile.MaximumKilobytes is > 0;
                string detail = "Allowed connections: " + Label(profile.Allowed) + ". Blocked connections: " + Label(profile.Blocked) +
                    ". Ignored traffic: " + Label(profile.Ignored) + ". Log destination: " + (profile.HasLogFilePath ? "configured" : "not configured") +
                    ". Maximum size: " + (profile.MaximumKilobytes is int size ? size.ToString(CultureInfo.InvariantCulture) + " KiB" : "not configured") + ". " +
                    (writes ? "At least one traffic category is configured for logging. " : "No traffic category is explicitly configured for logging in the active profile. ") +
                    (coherent ? "This is configuration evidence, not proof that records are being written or retained." : "Logging is enabled without a complete destination and positive size; review the effective policy before relying on the log.") + Scope;
                return new WorkflowCheck("Firewall logging: " + profile.Name,
                    coherent ? CheckState.Information : CheckState.Attention, detail, Evidence: result);
            }).ToArray();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            return [Unknown("Firewall logging data could not be interpreted.")];
        }
    }

    private static bool TryRead(JsonElement row, out FirewallLoggingProfile profile)
    {
        profile = null!;
        if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 6 ||
            !row.TryGetProperty("Name", out var name) || name.ValueKind != JsonValueKind.String ||
            name.GetString() is not ("Domain" or "Private" or "Public") ||
            !TryMode(row, "Allowed", out string allowed) || !TryMode(row, "Blocked", out string blocked) ||
            !TryMode(row, "Ignored", out string ignored) ||
            !row.TryGetProperty("HasLogFilePath", out var hasPath) || hasPath.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
            !row.TryGetProperty("MaximumKilobytes", out var maximum) ||
            maximum.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) return false;
        int? size = null;
        if (maximum.ValueKind == JsonValueKind.String &&
            (!int.TryParse(maximum.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed <= 0)) return false;
        else if (maximum.ValueKind == JsonValueKind.String) size = int.Parse(maximum.GetString()!, CultureInfo.InvariantCulture);
        profile = new(name.GetString()!, allowed, blocked, ignored, size, hasPath.GetBoolean());
        return true;
    }

    private static bool TryMode(JsonElement row, string name, out string value)
    {
        value = "";
        if (!row.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String ||
            element.GetString() is not ("True" or "False" or "NotConfigured")) return false;
        value = element.GetString()!;
        return true;
    }

    private static string Label(string value) => value switch
    {
        "True" => "enabled",
        "False" => "disabled",
        _ => "not configured"
    };
}
