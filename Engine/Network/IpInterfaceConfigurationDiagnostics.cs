using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class IpInterfaceConfigurationDiagnostics
{
    private const int MaximumInterfaces = 512;
    private const string Scope = " Interface settings are point-in-time configuration evidence. DHCP, forwarding, advertising, router discovery and metric choices can be intentional. No interface, address, MTU, metric, route, DHCP, forwarding, router-discovery, advertisement or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-NetIPInterface -ErrorAction Stop|Where-Object{$_.AddressFamily-in @('IPv4','IPv6')}|Sort-Object InterfaceIndex,AddressFamily|Select-Object -First 513);" +
        "$rows=@($all|Select-Object -First 512|ForEach-Object{[pscustomobject]@{interfaceIndex=[uint64]$_.InterfaceIndex;interfaceAlias=[string]$_.InterfaceAlias;addressFamily=[string]$_.AddressFamily;connectionState=[string]$_.ConnectionState;dhcp=[string]$_.Dhcp;nlMtu=[uint64]$_.NlMtu;forwarding=[string]$_.Forwarding;advertising=[string]$_.Advertising;routerDiscovery=[string]$_.RouterDiscovery;automaticMetric=[string]$_.AutomaticMetric;interfaceMetric=[uint64]$_.InterfaceMetric}});" +
        "[pscustomobject]@{complete=($all.Count-le 512);interfaces=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("IP interface configuration inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("IP interface configuration provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("IP interface configuration provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("IP interface configuration inspection exceeded its overall deadline.",
                new(-1, "", "IP interface configuration deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("IP interface configuration inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("IP interface configuration collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("interfaces", out var interfaces) || interfaces.ValueKind != JsonValueKind.Array ||
                interfaces.GetArrayLength() > MaximumInterfaces)
                return Unknown("IP interface configuration evidence was malformed, incomplete or exceeded its bound.", evidence);

            var rows = new List<InterfaceRow>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in interfaces.EnumerateArray())
            {
                if (!Unique(item, 11) || !Integer(item, "interfaceIndex", 1, uint.MaxValue, out long index) ||
                    !Text(item, "interfaceAlias", 256, out string alias) ||
                    !Text(item, "addressFamily", 4, out string family) || family is not ("IPv4" or "IPv6") ||
                    !Text(item, "connectionState", 16, out string connection) || connection is not ("Connected" or "Disconnected") ||
                    !Toggle(item, "dhcp", out bool dhcp) || !Integer(item, "nlMtu", 1, uint.MaxValue, out long mtu) ||
                    !Toggle(item, "forwarding", out bool forwarding) || !Toggle(item, "advertising", out bool advertising) ||
                    !Text(item, "routerDiscovery", 32, out string discovery) ||
                    discovery is not ("Enabled" or "Disabled" or "ControlledByDHCP") ||
                    !Toggle(item, "automaticMetric", out bool automaticMetric) ||
                    !Integer(item, "interfaceMetric", 0, uint.MaxValue, out long metric) ||
                    !identities.Add(index + "\0" + family))
                    return Unknown("IP interface configuration evidence contained an invalid or duplicate interface.", evidence);
                rows.Add(new((int)index, alias, family, connection, dhcp, mtu, forwarding, advertising, discovery,
                    automaticMetric, metric));
            }

            if (rows.Count == 0)
                return new("IP interface configuration", CheckState.Information,
                    "No IPv4 or IPv6 interfaces were reported." + Scope, Evidence: evidence);

            var lowMtu = rows.Where(row => row.Connection == "Connected" && row.Family == "IPv6" && row.Mtu < 1280).ToArray();
            int connected = rows.Count(row => row.Connection == "Connected");
            int dhcpCount = rows.Count(row => row.Dhcp);
            int forwardingCount = rows.Count(row => row.Forwarding);
            int advertisingCount = rows.Count(row => row.Advertising);
            int manualMetrics = rows.Count(row => !row.AutomaticMetric);
            string detail = $"Reviewed {rows.Count} IP interface configurations: {connected} connected, {dhcpCount} using DHCP, {forwardingCount} forwarding packets, {advertisingCount} advertising routes and {manualMetrics} using manual metrics.";
            if (lowMtu.Length > 0)
                detail += " Connected interfaces below the protocol minimum MTU: " + string.Join("; ", lowMtu.Select(row =>
                    $"{row.Alias} ({row.Family}, interface {row.Index}) reports {row.Mtu} bytes")) + ".";
            return new("IP interface configuration", lowMtu.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("IP interface configuration evidence could not be interpreted.", evidence);
        }
    }

    private sealed record InterfaceRow(int Index, string Alias, string Family, string Connection, bool Dhcp,
        long Mtu, bool Forwarding, bool Advertising, string RouterDiscovery, bool AutomaticMetric, long Metric);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("IP interface configuration", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, long minimum, long maximum, out long value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt64(out value) && value >= minimum && value <= maximum;
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
    private static bool Toggle(JsonElement owner, string name, out bool enabled)
    {
        enabled = false;
        if (!Text(owner, name, 8, out string value) || value is not ("Enabled" or "Disabled")) return false;
        enabled = value == "Enabled";
        return true;
    }
}
