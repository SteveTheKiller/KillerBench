using System.Text.Json;

namespace KillerBench.Engine.Network;

public sealed record WinRmListener(string Identity, string Address, string Transport, int? Port, bool Enabled,
    string UrlPrefix, bool HasCertificate);

public sealed record WinRmListenerInventory(IReadOnlyList<WinRmListener> Listeners, bool Complete, ToolResult Evidence);

public static class WinRmListeners
{
    private const int MaximumListeners = 32;
    private const string Scope = " Registry configuration only. Listener binding, firewall access, certificate validity, authentication, permissions and network reachability are not tested. No settings changed.";

    public static async Task<WinRmListenerInventory> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WinRM listener collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$root='Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WSMAN\\Listener';" +
            "$keys=@(Get-ChildItem -LiteralPath $root -ErrorAction Stop | Select-Object -First 33);" +
            "$rows=@($keys | Select-Object -First 32 | ForEach-Object {$p=Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction Stop;" +
            "[pscustomobject]@{identity=[string]$_.PSChildName;address=[string]$p.Address;transport=[string]$p.Transport;" +
            "port=if($null -eq $p.Port){$null}else{[int]$p.Port};enabled=if($null -eq $p.Enabled){$true}else{[bool]$p.Enabled};" +
            "urlPrefix=if($null -eq $p.URLPrefix){'wsman'}else{[string]$p.URLPrefix};hasCertificate=![string]::IsNullOrWhiteSpace([string]$p.CertificateThumbprint)}});" +
            "[pscustomobject]@{complete=($keys.Count -le 32);listeners=$rows}|ConvertTo-Json -Depth 4 -Compress";
        try
        {
            var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Parse(evidence, token);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return new([], false, new(-1, "", error.Message, false)); }
    }

    public static WinRmListenerInventory Parse(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new([], false, evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("listeners", out var listenersValue) || listenersValue.ValueKind != JsonValueKind.Array ||
                listenersValue.GetArrayLength() > MaximumListeners)
                return new([], false, evidence);
            var listeners = new List<WinRmListener>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in listenersValue.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 7 ||
                    !TryText(item, "identity", 256, out string identity) || !identities.Add(identity) ||
                    !TryText(item, "address", 256, out string address) ||
                    !TryText(item, "transport", 8, out string transport) || transport is not ("HTTP" or "HTTPS") ||
                    !item.TryGetProperty("port", out var portValue) || portValue.ValueKind is not (JsonValueKind.Number or JsonValueKind.Null) ||
                    (portValue.ValueKind == JsonValueKind.Number && (!portValue.TryGetInt32(out int parsedPort) || parsedPort is < 1 or > 65535)) ||
                    !item.TryGetProperty("enabled", out var enabledValue) || enabledValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !TryText(item, "urlPrefix", 128, out string urlPrefix) ||
                    !item.TryGetProperty("hasCertificate", out var certificateValue) || certificateValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return new([], false, evidence);
                listeners.Add(new(identity, address, transport,
                    portValue.ValueKind == JsonValueKind.Null ? null : portValue.GetInt32(), enabledValue.GetBoolean(), urlPrefix,
                    certificateValue.GetBoolean()));
            }
            return new(listeners.AsReadOnly(), completeValue.GetBoolean(), evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        { return new([], false, evidence); }
    }

    public static IReadOnlyList<WorkflowCheck> Describe(WinRmListenerInventory inventory)
    {
        if (!inventory.Complete)
            return [new("WinRM listeners", CheckState.Unknown, "Listener inventory was unavailable, invalid or exceeded the 32-listener evidence bound. No absence or current availability was inferred." + Scope, Evidence: inventory.Evidence)];
        if (inventory.Listeners.Count == 0)
            return [new("WinRM listeners", CheckState.Information, "No listener registrations were found in the inspected registry location." + Scope, Evidence: inventory.Evidence)];
        var checks = new List<WorkflowCheck>
        {
            new("WinRM listeners", CheckState.Information, $"{inventory.Listeners.Count} listener registration{(inventory.Listeners.Count == 1 ? "" : "s")} retained. Exact identities and certificate thumbprints remain in raw evidence." + Scope, Evidence: inventory.Evidence)
        };
        foreach (var listener in inventory.Listeners)
        {
            int port = listener.Port ?? (listener.Transport == "HTTPS" ? 5986 : 5985);
            string certificate = listener.Transport == "HTTPS" ? (listener.HasCertificate ? "certificate configured" : "certificate not reported") : "no HTTPS certificate expected";
            checks.Add(new($"WinRM {listener.Transport} listener", listener.Enabled ? CheckState.Information : CheckState.Attention,
                $"Address: {listener.Address}; port: {port}{(listener.Port is null ? " (transport default)" : "")}; URL prefix: {listener.UrlPrefix}; {certificate}; configured {(listener.Enabled ? "enabled" : "disabled")}." + Scope,
                Evidence: inventory.Evidence));
        }
        return checks.AsReadOnly();
    }

    private static bool TryText(JsonElement item, string name, int maximum, out string value)
    {
        value = "";
        if (!item.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String) return false;
        value = property.GetString() ?? "";
        return value.Length is > 0 && value.Length <= maximum && !value.Any(char.IsControl);
    }
}
