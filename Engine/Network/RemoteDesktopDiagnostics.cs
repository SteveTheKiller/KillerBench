using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class RemoteDesktopDiagnostics
{
    private const string Scope = " Settings are read only. Effective policy, listener binding, firewall access, certificate identity, permissions, authentication and remote reachability remain unverified.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Remote Desktop configuration collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$ts=Get-ItemProperty -LiteralPath 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server' -ErrorAction Stop;" +
            "$tcp=Get-ItemProperty -LiteralPath 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp' -ErrorAction Stop;" +
            "$pol=Get-ItemProperty -LiteralPath 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services' -ErrorAction SilentlyContinue;" +
            "[pscustomobject]@{deny=$ts.fDenyTSConnections;nla=$tcp.UserAuthentication;port=$tcp.PortNumber;policyDeny=if($null-eq $pol){$null}else{$pol.fDenyTSConnections};policyNla=if($null-eq $pol){$null}else{$pol.UserAuthentication}}|ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(result);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        WorkflowCheck Unknown(string detail) => new("Remote Desktop configuration", CheckState.Unknown, detail + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Configuration collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 5 ||
                !ReadRequired(root, "deny", out int deny) || deny is not (0 or 1) ||
                !ReadRequired(root, "nla", out int nla) || nla is not (0 or 1) ||
                !ReadRequired(root, "port", out int port) || port is < 1 or > 65535 ||
                !ReadOptional(root, "policyDeny", out int? policyDeny) || policyDeny is not (null or 0 or 1) ||
                !ReadOptional(root, "policyNla", out int? policyNla) || policyNla is not (null or 0 or 1))
                return [Unknown("Configuration values were missing, duplicated or invalid.")];
            string policy = policyDeny is null && policyNla is null ? "No matching machine policy values were observed." :
                $"Machine policy values observed: enablement={(policyDeny is null ? "not configured" : policyDeny == 0 ? "enabled" : "disabled")}; NLA={(policyNla is null ? "not configured" : policyNla == 1 ? "required" : "not required")}.";
            return
            [
                new WorkflowCheck("Remote Desktop enablement", CheckState.Information,
                    $"Local configuration reports Remote Desktop {(deny == 0 ? "enabled" : "disabled")}. {policy}" + Scope, Evidence: result),
                new WorkflowCheck("Remote Desktop Network Level Authentication", nla == 1 ? CheckState.Information : CheckState.Attention,
                    $"Local listener configuration reports NLA {(nla == 1 ? "required" : "not required")}. {policy}" + Scope, Evidence: result),
                new WorkflowCheck("Remote Desktop listener port", CheckState.Information,
                    $"Configured RDP-Tcp port: {port}. This does not prove a listener is bound or reachable." + Scope, Evidence: result)
            ];
        }
        catch (JsonException) { return [Unknown("Configuration data could not be interpreted.")]; }
    }

    private static bool ReadRequired(JsonElement root, string name, out int value)
    {
        value = 0;
        return root.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value);
    }

    private static bool ReadOptional(JsonElement root, string name, out int? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out int number)) return false;
        value = number;
        return true;
    }
}
