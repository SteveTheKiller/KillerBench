using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class SmbConnectionDiagnostics
{
    private const string Scope = " Server, share, account, credential and path identities are excluded. This snapshot does not establish policy ownership, future negotiation, authentication health, firewall reachability or file-transfer success. No connection or SMB setting is changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("SMB connection collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$c=@(Get-SmbConnection -ErrorAction Stop); if($c.Count -gt 256){throw 'SMB connection inventory exceeded 256 entries.'}; " +
            "[pscustomobject]@{Connections=@($c | ForEach-Object {[pscustomobject]@{" +
            "Dialect=[string]$_.Dialect;Signed=[bool]$_.Signed;Encrypted=[bool]$_.Encrypted;ContinuouslyAvailable=[bool]$_.ContinuouslyAvailable}})} | ConvertTo-Json -Compress -Depth 3";
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
            return Unknown(new(-1, "", error.Message, false), "Current SMB connection collection was unavailable.");
        }
    }

    public static WorkflowCheck Interpret(ToolResult? result)
    {
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return Unknown(result, "Current SMB connection collection failed or was incomplete.");
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("Connections", out var connections) || connections.ValueKind != JsonValueKind.Array ||
                connections.GetArrayLength() > 256)
                return Unknown(result, "Current SMB connection data was malformed or exceeded its bound.");

            var groups = new Dictionary<(string Dialect, bool Signed, bool Encrypted, bool Continuous), int>();
            foreach (var connection in connections.EnumerateArray())
            {
                if (connection.ValueKind != JsonValueKind.Object || connection.EnumerateObject().Count() != 4 ||
                    !Text(connection, "Dialect", out string dialect) || !Boolean(connection, "Signed", out bool signed) ||
                    !Boolean(connection, "Encrypted", out bool encrypted) || !Boolean(connection, "ContinuouslyAvailable", out bool continuous))
                    return Unknown(result, "Current SMB connection data was malformed.");
                var key = (dialect, signed, encrypted, continuous);
                groups[key] = groups.TryGetValue(key, out int count) ? count + 1 : 1;
            }

            if (connections.GetArrayLength() == 0)
                return new("Active SMB connections", CheckState.Information,
                    "No active SMB client connections were reported. This does not establish SMB availability or configuration." + Scope, Evidence: result);

            bool legacy = groups.Keys.Any(item => item.Dialect.StartsWith("1.", StringComparison.Ordinal));
            bool unsigned = groups.Keys.Any(item => !item.Signed);
            string summary = string.Join("; ", groups.OrderBy(item => item.Key.Dialect, StringComparer.Ordinal)
                .ThenBy(item => item.Key.Signed).ThenBy(item => item.Key.Encrypted).ThenBy(item => item.Key.Continuous)
                .Select(item => $"{item.Value} using dialect {item.Key.Dialect}, signed {Yes(item.Key.Signed)}, encrypted {Yes(item.Key.Encrypted)}, continuously available {Yes(item.Key.Continuous)}"));
            string review = legacy || unsigned
                ? "Review active SMB connections using SMB 1 or without signing against the required server and managed policy. "
                : "Active SMB connections use SMB 2 or newer and report signing enabled. ";
            return new("Active SMB connections", legacy || unsigned ? CheckState.Attention : CheckState.Information,
                review + summary + "." + Scope, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown(result, "Current SMB connection data could not be interpreted.");
        }
    }

    private static bool Text(JsonElement root, string name, out string value)
    {
        value = "";
        if (!root.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String) return false;
        value = field.GetString()?.Trim() ?? "";
        return value.Length is > 0 and <= 32 && value.All(character => char.IsLetterOrDigit(character) || character is '.' or '-');
    }

    private static bool Boolean(JsonElement root, string name, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(name, out var field) || field.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = field.GetBoolean();
        return true;
    }

    private static string Yes(bool value) => value ? "yes" : "no";
    private static WorkflowCheck Unknown(ToolResult? evidence, string detail) =>
        new("Active SMB connections", CheckState.Unknown, detail + Scope, Evidence: evidence);
}
