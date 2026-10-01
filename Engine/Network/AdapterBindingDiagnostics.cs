using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class AdapterBindingDiagnostics
{
    private const int MaximumAdapters = 256;
    private const int MaximumBindings = 1024;
    private static readonly HashSet<string> Components = new(["ms_tcpip", "ms_tcpip6", "ms_msclient", "ms_server"], StringComparer.OrdinalIgnoreCase);
    private const string Scope = " Binding state is point-in-time configuration evidence. IPv4, IPv6, Microsoft network client and file-sharing bindings can be intentionally disabled. This check does not establish address assignment, route selection, name resolution, file sharing or Internet access. No adapter, binding, protocol, address, route, service, firewall or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$a=@(Get-NetAdapter -Name '*' -ErrorAction Stop|Sort-Object ifIndex|Select-Object -First 257);" +
        "$b=@(Get-NetAdapterBinding -Name '*' -ComponentID @('ms_tcpip','ms_tcpip6','ms_msclient','ms_server') -ErrorAction Stop|Select-Object -First 1025);" +
        "$complete=($a.Count-le 256-and $b.Count-le 1024);$names=@{};$rows=@();" +
        "foreach($x in @($a|Select-Object -First 256)){$name=[string]$x.Name;if($names.ContainsKey($name)){$complete=$false}else{$names[$name]=$true};" +
        "$items=@($b|Where-Object{[string]$_.Name-ceq $name}|ForEach-Object{[pscustomobject]@{component=[string]$_.ComponentID;enabled=[bool]$_.Enabled}});" +
        "$rows+=[pscustomobject]@{name=$name;interfaceIndex=[int64]$x.ifIndex;status=[string]$x.Status;hardware=[bool]$x.HardwareInterface;bindings=$items}};" +
        "foreach($x in @($b|Select-Object -First 1024)){if(-not $names.ContainsKey([string]$x.Name)){$complete=$false}};" +
        "[pscustomobject]@{complete=$complete;adapters=$rows}|ConvertTo-Json -Depth 5 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Network adapter binding inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Adapter-binding provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Adapter-binding provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Adapter-binding inspection exceeded its overall deadline.",
                new(-1, "", "Adapter-binding inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Adapter-binding inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Adapter-binding collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("adapters", out var adapters) || adapters.ValueKind != JsonValueKind.Array ||
                adapters.GetArrayLength() > MaximumAdapters)
                return Unknown("Adapter-binding evidence was malformed, incomplete or exceeded its adapter bound.", evidence);

            var rows = new List<AdapterRow>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var indexes = new HashSet<int>();
            int bindingCount = 0;
            foreach (var adapter in adapters.EnumerateArray())
            {
                if (!Unique(adapter, 5) || !Text(adapter, "name", 256, out string name) ||
                    !Integer(adapter, "interfaceIndex", 1, int.MaxValue, out int index) ||
                    !Text(adapter, "status", 64, out string status) || !Boolean(adapter, "hardware", out bool hardware) ||
                    !adapter.TryGetProperty("bindings", out var bindings) || bindings.ValueKind != JsonValueKind.Array ||
                    bindings.GetArrayLength() > Components.Count || !names.Add(name) || !indexes.Add(index))
                    return Unknown("Adapter-binding evidence contained an invalid or duplicate adapter.", evidence);
                var values = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var binding in bindings.EnumerateArray())
                {
                    if (!Unique(binding, 2) || !Text(binding, "component", 64, out string component) ||
                        !Components.Contains(component) || !Boolean(binding, "enabled", out bool enabled) ||
                        !values.TryAdd(component, enabled) || ++bindingCount > MaximumBindings)
                        return Unknown("Adapter-binding evidence contained an invalid, duplicate or excessive binding.", evidence);
                }
                rows.Add(new(name, index, status, hardware, values));
            }
            if (rows.Count == 0)
                return new("Network adapter bindings", CheckState.Information,
                    "No visible network adapters were reported, so no protocol bindings could be compared." + Scope, Evidence: evidence);

            var activeHardware = rows.Where(row => row.Hardware && row.Status.Equals("Up", StringComparison.OrdinalIgnoreCase)).ToArray();
            var incomplete = activeHardware.Where(row => !row.Bindings.ContainsKey("ms_tcpip") || !row.Bindings.ContainsKey("ms_tcpip6")).ToArray();
            if (incomplete.Length > 0)
                return Unknown("One or more active hardware adapters did not return both IPv4 and IPv6 binding evidence: " +
                    string.Join(", ", incomplete.Take(16).Select(row => row.Name)) + ".", evidence);
            var noIp = activeHardware.Where(row => !row.Bindings["ms_tcpip"] && !row.Bindings["ms_tcpip6"]).ToArray();
            int ipv4Disabled = activeHardware.Count(row => !row.Bindings["ms_tcpip"]);
            int ipv6Disabled = activeHardware.Count(row => !row.Bindings["ms_tcpip6"]);
            int clientDisabled = activeHardware.Count(row => row.Bindings.TryGetValue("ms_msclient", out bool enabled) && !enabled);
            int serverDisabled = activeHardware.Count(row => row.Bindings.TryGetValue("ms_server", out bool enabled) && !enabled);
            string detail = $"Reviewed {rows.Count} visible adapter{(rows.Count == 1 ? "" : "s")}, including {activeHardware.Length} active hardware adapter{(activeHardware.Length == 1 ? "" : "s")}. " +
                $"Among active hardware adapters, IPv4 is disabled on {ipv4Disabled}, IPv6 is disabled on {ipv6Disabled}, Microsoft network client is explicitly disabled on {clientDisabled}, and file sharing is explicitly disabled on {serverDisabled}.";
            if (noIp.Length > 0)
                detail += " Both IPv4 and IPv6 are disabled on: " + string.Join(", ", noIp.Take(16).Select(row => row.Name)) +
                    ". Review whether these active hardware adapters are intended to carry IP traffic before changing bindings.";
            else
                detail += " Every active hardware adapter with complete evidence retains at least one IP protocol binding.";
            return new("Network adapter bindings", noIp.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Adapter-binding evidence could not be interpreted.", evidence);
        }
    }

    private sealed record AdapterRow(string Name, int InterfaceIndex, string Status, bool Hardware, Dictionary<string, bool> Bindings);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Network adapter bindings", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt32(out value) && value >= minimum && value <= maximum;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String ||
            item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum ||
            text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
