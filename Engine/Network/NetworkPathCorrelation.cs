using System.Net;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class NetworkPathCorrelation
{
    private sealed record DefaultPath(int InterfaceIndex, int Family, string NextHop, long Metric);

    public static WorkflowCheck Check(WorkflowCheck routes, WorkflowCheck dns, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Network path readiness";
        const string scope = " Interface indexes are point-in-time identities. Configuration does not prove gateway reachability, DNS response, path selection or Internet access. No route, DNS server, adapter, cache, proxy, firewall or policy setting changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + scope, Evidence: routes.Evidence ?? dns.Evidence);
        if (routes.EffectiveState == CheckState.Unknown || routes.Evidence is null)
            return Unknown("Complete route evidence is required before network paths can be correlated.");
        if (dns.EffectiveState == CheckState.Unknown || dns.Evidence is null)
            return Unknown("Complete DNS server evidence is required before network paths can be correlated.");
        try
        {
            var defaults = ReadDefaults(routes.Evidence.Output);
            var dnsRows = ReadDns(dns.Evidence.Output);
            var missing = defaults.Where(path => !dnsRows.Contains((path.InterfaceIndex, path.Family))).ToArray();
            string detail;
            CheckState state;
            if (defaults.Count == 0)
            {
                state = CheckState.Attention;
                detail = "No IPv4 or IPv6 default route was reported. Local and explicitly routed destinations may still work; off-subnet reachability needs separate testing.";
            }
            else
            {
                string paths = string.Join("; ", defaults.Take(16).Select(path =>
                    $"interface {path.InterfaceIndex} IPv{(path.Family == 2 ? 4 : 6)} via {path.NextHop}, metric {path.Metric}")) +
                    (defaults.Count > 16 ? "; additional default paths remain in route evidence" : "");
                detail = $"Correlated {defaults.Count} default path{(defaults.Count == 1 ? "" : "s")}: {paths}.";
                if (missing.Length == 0)
                {
                    state = CheckState.Information;
                    detail += " Every default-path interface and address family has at least one reported DNS server.";
                }
                else
                {
                    state = CheckState.Attention;
                    detail += " Default paths without a same-family DNS server row: " + string.Join(", ", missing.Select(path =>
                        $"interface {path.InterfaceIndex} IPv{(path.Family == 2 ? 4 : 6)}")) + ". Windows may resolve through another interface or local resolver; run a bounded hostname lookup before selecting recovery.";
                }
            }
            return new(name, state, detail + scope, Evidence: routes.Evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Route and DNS evidence could not be correlated safely.");
        }
    }

    private static List<DefaultPath> ReadDefaults(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 1024)
            throw new InvalidDataException("Route evidence is incomplete.");
        var result = new List<DefaultPath>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("destination", out var destinationValue) || destinationValue.ValueKind != JsonValueKind.String ||
                !entry.TryGetProperty("nextHop", out var nextHopValue) || nextHopValue.ValueKind != JsonValueKind.String ||
                !entry.TryGetProperty("interfaceIndex", out var interfaceValue) || !interfaceValue.TryGetInt32(out int index) || index < 1 ||
                !entry.TryGetProperty("metric", out var metricValue) || !metricValue.TryGetInt64(out long metric) || metric < 0)
                throw new InvalidDataException("Route identity is invalid.");
            string destination = destinationValue.GetString()!, nextHop = nextHopValue.GetString()!;
            if (destination is not ("0.0.0.0/0" or "::/0")) continue;
            int family = destination == "0.0.0.0/0" ? 2 : 23;
            if (!IPAddress.TryParse(nextHop, out var address) ||
                (family == 2 && address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) ||
                (family == 23 && address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) ||
                !seen.Add(index + ":" + family + ":" + nextHop + ":" + metric))
                throw new InvalidDataException("Default route identity is invalid or duplicated.");
            result.Add(new(index, family, nextHop, metric));
        }
        return [.. result.OrderBy(path => path.Metric).ThenBy(path => path.InterfaceIndex).ThenBy(path => path.Family)];
    }

    private static HashSet<(int InterfaceIndex, int Family)> ReadDns(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 512)
            throw new InvalidDataException("DNS evidence is incomplete.");
        var result = new HashSet<(int, int)>();
        var identities = new HashSet<(int, int)>();
        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("interfaceIndex", out var interfaceValue) || !interfaceValue.TryGetInt32(out int index) || index < 1 ||
                !row.TryGetProperty("addressFamily", out var familyValue) || !familyValue.TryGetInt32(out int family) || family is not (2 or 23) ||
                !row.TryGetProperty("serverAddresses", out var servers) || servers.ValueKind != JsonValueKind.Array || servers.GetArrayLength() > 16 ||
                !identities.Add((index, family))) throw new InvalidDataException("DNS row identity is invalid or duplicated.");
            bool configured = false;
            foreach (var serverValue in servers.EnumerateArray())
            {
                if (serverValue.ValueKind != JsonValueKind.String || !IPAddress.TryParse(serverValue.GetString(), out var server) ||
                    (family == 2 && server.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) ||
                    (family == 23 && server.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6))
                    throw new InvalidDataException("DNS server identity is invalid.");
                configured = true;
            }
            if (configured) result.Add((index, family));
        }
        return result;
    }
}
