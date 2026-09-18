using System.Text.Json;

namespace KillerBench.Engine;

public static class SmbShareDiagnostics
{
    private const string Scope = " Share paths, permissions, sessions, open files and account identities are excluded. No share or SMB setting is changed.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("SMB share collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$shares=@(Get-SmbShare -ErrorAction Stop | Sort-Object ScopeName,Name | Select-Object -First 129 " +
            "@{n='Name';e={[string]$_.Name}},@{n='ScopeName';e={[string]$_.ScopeName}}," +
            "@{n='ShareState';e={[string]$_.ShareState}},@{n='AvailabilityType';e={[string]$_.AvailabilityType}}," +
            "@{n='CachingMode';e={[string]$_.CachingMode}},@{n='FolderEnumerationMode';e={[string]$_.FolderEnumerationMode}}," +
            "@{n='EncryptData';e={[bool]$_.EncryptData}},@{n='Special';e={[bool]$_.Special}}); " +
            "[pscustomobject]@{shares=$shares} | ConvertTo-Json -Depth 4 -Compress";
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
        {
            return [new("SMB shares", CheckState.Unknown, "Current SMB share collection was unavailable." + Scope,
                Evidence: new(-1, "", error.Message, false))];
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        WorkflowCheck Unknown(string detail) => new("SMB shares", CheckState.Unknown, detail + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Current SMB share collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("shares", out var shares) || shares.ValueKind != JsonValueKind.Array)
                return [Unknown("Current SMB share data was malformed.")];
            if (shares.GetArrayLength() > 128)
                return [Unknown("More than 128 current SMB shares were returned, so coverage was capped.")];
            if (shares.GetArrayLength() == 0)
                return [new("SMB shares", CheckState.Information,
                    "No current SMB shares were reported. This does not establish whether sharing is intended." + Scope, Evidence: result)];

            var checks = new List<WorkflowCheck>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var share in shares.EnumerateArray())
            {
                if (share.ValueKind != JsonValueKind.Object || share.EnumerateObject().Count() != 8 ||
                    !Text(share, "Name", 80, out string name) || !Text(share, "ScopeName", 256, out string scope, allowEmpty: true) ||
                    !Text(share, "ShareState", 64, out string state) || !Text(share, "AvailabilityType", 64, out string availability) ||
                    !Text(share, "CachingMode", 64, out string caching) || !Text(share, "FolderEnumerationMode", 64, out string enumeration) ||
                    !share.TryGetProperty("EncryptData", out var encryptValue) || encryptValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !share.TryGetProperty("Special", out var specialValue) || specialValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !identities.Add(scope + "\0" + name))
                    return [Unknown("Current SMB share identities or values were invalid or duplicated.")];
                bool encrypted = encryptValue.GetBoolean();
                bool special = specialValue.GetBoolean();
                CheckState checkState = state.Equals("Online", StringComparison.OrdinalIgnoreCase) ? CheckState.Information : CheckState.Attention;
                string kind = special ? "special or administrative" : "standard";
                checks.Add(new("SMB share: " + name, checkState,
                    $"Scope {Display(scope)}; state {state}; availability {availability}; {kind}; encryption {(encrypted ? "required" : "not required")}; caching {caching}; folder enumeration {enumeration}." + Scope,
                    Evidence: result));
            }
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return [Unknown("Current SMB share data could not be interpreted.")];
        }
    }

    private static bool Text(JsonElement item, string property, int maximum, out string value, bool allowEmpty = false)
    {
        value = "";
        if (!item.TryGetProperty(property, out var field) || field.ValueKind != JsonValueKind.String) return false;
        value = field.GetString()!;
        return (allowEmpty || value.Length > 0) && value.Length <= maximum && value == value.Trim() && !value.Any(char.IsControl);
    }

    private static string Display(string scope) => scope.Length == 0 ? "local server" : scope;
}
