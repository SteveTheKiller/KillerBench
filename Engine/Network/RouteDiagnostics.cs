using System.Net;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class RouteDiagnostics
{
    private const int MaximumEntries = 1024;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($route in @(Get-NetRoute -ErrorAction Stop|Sort-Object AddressFamily,DestinationPrefix,InterfaceIndex,RouteMetric)){if($rows.Count-ge 1025){break};" +
            "$rows.Add([pscustomobject]@{destination=[string]$route.DestinationPrefix;nextHop=[string]$route.NextHop;interfaceIndex=[int64]$route.InterfaceIndex;metric=[int64]$route.RouteMetric;protocol=[string]$route.Protocol})};" +
            "[pscustomobject]@{complete=($rows.Count-le 1024);entries=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "IP routing table";
        WorkflowCheck Unknown(string detail = "The IP routing table is unavailable or incomplete. No route or adapter setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var completeValue) ||
                completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("entries", out var entriesValue) || entriesValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (!completeValue.GetBoolean() || entriesValue.GetArrayLength() > MaximumEntries)
                return Unknown("The IP routing table exceeded its evidence bound. Coverage is incomplete and no change was selected.");
            var rows = new List<(string Destination, string NextHop, long InterfaceIndex, long Metric, string Protocol)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entriesValue.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object ||
                    !entry.TryGetProperty("destination", out var destinationValue) || destinationValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("nextHop", out var nextHopValue) || nextHopValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("interfaceIndex", out var interfaceValue) || !interfaceValue.TryGetInt64(out long interfaceIndex) ||
                    !entry.TryGetProperty("metric", out var metricValue) || !metricValue.TryGetInt64(out long metric) ||
                    !entry.TryGetProperty("protocol", out var protocolValue) || protocolValue.ValueKind != JsonValueKind.String) return Unknown();
                string destination = destinationValue.GetString()!, nextHop = nextHopValue.GetString()!, protocol = protocolValue.GetString()!;
                if (!ValidPrefix(destination) || !IPAddress.TryParse(nextHop, out _) || interfaceIndex is < 1 or > int.MaxValue ||
                    metric is < 0 or > uint.MaxValue || !DriverReference.ValidText(protocol, 64) || protocol != protocol.Trim() ||
                    !seen.Add(destination + "\0" + nextHop + "\0" + interfaceIndex + "\0" + metric + "\0" + protocol)) return Unknown();
                rows.Add((destination, nextHop, interfaceIndex, metric, protocol));
            }
            var defaults = rows.Where(row => row.Destination is "0.0.0.0/0" or "::/0").ToArray();
            string detail = $"Reviewed {rows.Count} IP route{(rows.Count == 1 ? "" : "s")}; {defaults.Length} default route{(defaults.Length == 1 ? "" : "s")}.";
            if (defaults.Length > 0) detail += "\nDefault routes: " + string.Join("; ", defaults.Take(16).Select(row =>
                row.Destination + " via " + row.NextHop + " on interface " + row.InterfaceIndex + " metric " + row.Metric + " (" + row.Protocol + ")")) +
                (defaults.Length > 16 ? "; additional defaults remain in evidence." : ".");
            detail += "\nRoutes are configuration evidence, not proof of gateway reachability, path selection or Internet access. No route, interface, address, gateway, metric, VPN or policy setting changed.";
            return new(title, CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }

    private static bool ValidPrefix(string value)
    {
        if (!DriverReference.ValidText(value, 128) || value != value.Trim()) return false;
        int slash = value.LastIndexOf('/');
        if (slash < 1 || !IPAddress.TryParse(value[..slash], out var address) ||
            !int.TryParse(value[(slash + 1)..], out int bits)) return false;
        return bits >= 0 && bits <= (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128);
    }
}
