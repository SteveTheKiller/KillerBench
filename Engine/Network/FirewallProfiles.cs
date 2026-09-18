using System.Text.Json;

namespace KillerBench.Engine;

public static class FirewallProfiles
{
    private const string Scope = " ActiveStore profile configuration only; active network assignment, rules, third-party protection and traffic enforcement are not tested. Settings are preserved.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Firewall profile collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$profiles=@(Get-NetFirewallProfile -PolicyStore ActiveStore -ErrorAction Stop | Select-Object -First 4 " +
            "Name,@{n='Enabled';e={if($null -eq $_.Enabled){$null}else{$_.Enabled.ToString()}}}); " +
            "[pscustomobject]@{profiles=$profiles} | ConvertTo-Json -Depth 4 -Compress";
        try
        {
            var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Interpret(result);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return [new("Firewall profiles", CheckState.Unknown, "Profile collection was unavailable." + Scope, Evidence: new(-1, "", error.Message, false))]; }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        WorkflowCheck Unknown(string detail) => new("Firewall profiles", CheckState.Unknown, detail + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Profile collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("profiles", out var profiles) || profiles.ValueKind != JsonValueKind.Array || profiles.GetArrayLength() != 3)
                return [Unknown("The three profile definitions were not collected completely.")];
            var checks = new List<WorkflowCheck>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in profiles.EnumerateArray())
            {
                if (profile.ValueKind != JsonValueKind.Object || profile.EnumerateObject().Count() != 2 ||
                    !profile.TryGetProperty("Name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    nameValue.GetString() is not ("Domain" or "Private" or "Public") || !seen.Add(nameValue.GetString()!) ||
                    !profile.TryGetProperty("Enabled", out var enabledValue) || enabledValue.ValueKind != JsonValueKind.String)
                    return [Unknown("Profile identities or enabled settings were invalid or duplicated.")];
                string? enabled = enabledValue.GetString();
                var state = enabled == "False" ? CheckState.Attention : enabled == "True" ? CheckState.Information : CheckState.Unknown;
                string detail = enabled == "False" ? "This profile is configured disabled. Review intended policy and other security products with the policy owner before changing it." :
                    enabled == "True" ? "This profile is configured enabled. This does not establish effective protection." :
                    "The enabled setting is unresolved or unrecognized.";
                checks.Add(new("Firewall profile: " + nameValue.GetString(), state, detail + Scope, Evidence: result));
            }
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        { return [Unknown("Profile data could not be interpreted.")]; }
    }
}
