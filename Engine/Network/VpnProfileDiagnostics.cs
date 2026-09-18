using System.Text.Json;

namespace KillerBench.Engine;

public sealed record VpnProfile(string Name, string ServerAddress, string TunnelType,
    IReadOnlyList<string> AuthenticationMethods, bool SplitTunneling, bool RememberCredential, string ConnectionStatus);
public sealed record VpnProfileInventory(IReadOnlyList<VpnProfile> Profiles, bool Complete, ToolResult Evidence);

public static class VpnProfileDiagnostics
{
    public static async Task<VpnProfileInventory> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("VPN profile inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $items=@(Get-VpnConnection -ErrorAction Stop | Select-Object -First 129); " +
            "$rows=@($items | Select-Object -First 128 | ForEach-Object {[pscustomobject]@{name=$_.Name;server=$_.ServerAddress;tunnel=[string]$_.TunnelType;" +
            "authentication=@($_.AuthenticationMethod | ForEach-Object {[string]$_});split=[bool]$_.SplitTunneling;remember=[bool]$_.RememberCredential;status=[string]$_.ConnectionStatus}}); " +
            "[pscustomobject]@{complete=($items.Count -le 128);profiles=$rows}|ConvertTo-Json -Depth 4 -Compress";
        ToolResult evidence;
        try
        {
            evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            evidence = new(-1, "", error.Message, false);
        }
        return Interpret(evidence, token);
    }

    public static VpnProfileInventory Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new([], false, evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!Unique(root) || root.GetProperty("complete").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return new([], false, evidence);
            bool complete = root.GetProperty("complete").GetBoolean();
            var rows = root.GetProperty("profiles");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 128) return new([], false, evidence);
            var profiles = new List<VpnProfile>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(row)) { complete = false; continue; }
                try
                {
                    string? name = row.GetProperty("name").GetString();
                    string? server = row.GetProperty("server").GetString();
                    string? tunnel = row.GetProperty("tunnel").GetString();
                    string? status = row.GetProperty("status").GetString();
                    var authentication = row.GetProperty("authentication");
                    if (!Valid(name) || !Valid(server) || !Valid(tunnel) || !Valid(status) || !seen.Add(name!) ||
                        authentication.ValueKind != JsonValueKind.Array || authentication.GetArrayLength() > 16 ||
                        row.GetProperty("split").ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                        row.GetProperty("remember").ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    { complete = false; continue; }
                    var methods = authentication.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null).ToArray();
                    if (methods.Any(value => !Valid(value)) || methods.Distinct(StringComparer.OrdinalIgnoreCase).Count() != methods.Length)
                    { complete = false; continue; }
                    profiles.Add(new(name!, server!, tunnel!, Array.AsReadOnly(methods.Select(value => value!).ToArray()),
                        row.GetProperty("split").GetBoolean(), row.GetProperty("remember").GetBoolean(), status!));
                }
                catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(Array.AsReadOnly(profiles.OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase).ToArray()), complete, evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return new([], false, evidence);
        }
    }

    public static IReadOnlyList<WorkflowCheck> Describe(VpnProfileInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observed = Interpret(inventory.Evidence);
        var checks = new List<WorkflowCheck>
        {
            new("Current-account VPN profiles", observed.Complete ? CheckState.Information : CheckState.Unknown,
                observed.Complete
                    ? observed.Profiles.Count + " VPN profiles reported for the account running KillerBench. Passwords, certificates and keys are not collected. Profile presence does not verify authentication, reachability or connectivity. No connection or setting changed."
                    : "VPN profile inventory is unavailable or incomplete. Absence cannot be inferred. No connection or setting changed.", Evidence: observed.Evidence)
        };
        checks.AddRange(observed.Profiles.Select(profile => new WorkflowCheck("VPN profile: " + profile.Name, CheckState.Information,
            $"Server: {profile.ServerAddress}. Tunnel: {profile.TunnelType}. Authentication: {(profile.AuthenticationMethods.Count == 0 ? "not reported" : string.Join(", ", profile.AuthenticationMethods))}. Split tunneling: {profile.SplitTunneling}. Remember credential: {profile.RememberCredential}. Status: {profile.ConnectionStatus}. Configuration snapshot only; no connection attempted.")));
        return checks.AsReadOnly();
    }

    private static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 && value == value.Trim() && !value.Any(char.IsControl);
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
