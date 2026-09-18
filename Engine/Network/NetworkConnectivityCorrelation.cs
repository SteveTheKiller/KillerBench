using System.Net;
using System.Text.Json;

namespace KillerBench.Engine;

public static class NetworkConnectivityCorrelation
{
    private sealed record Profile(int InterfaceIndex, string Alias, string IPv4, string IPv6);
    private sealed record Path(int InterfaceIndex, int Family, string NextHop);
    private sealed record Probe(int InterfaceIndex, int Family, string NextHop, GatewayProbeStatus Status);
    private sealed record Observation(int InterfaceIndex, string Interface, int AddressFamily, string WindowsConnectivity,
        bool HasDefaultPath, GatewayProbeStatus? GatewayStatus, string Finding);

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> profiles, WorkflowCheck routes, WorkflowCheck gateways,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(gateways);
        token.ThrowIfCancellationRequested();
        const string name = "Network connectivity correlation";
        const string boundary = " Windows connectivity labels and ICMP responses are point-in-time evidence. They do not prove DNS, application, VPN or Internet availability. No network setting changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary,
            Evidence: routes.Evidence ?? gateways.Evidence ?? profiles.FirstOrDefault()?.Evidence);
        try
        {
            var profileRows = ReadProfiles(profiles);
            var paths = ReadPaths(routes);
            var probes = ReadProbes(gateways);
            var observations = new List<Observation>();
            foreach (var profile in profileRows)
            {
                token.ThrowIfCancellationRequested();
                Add(profile, 2, profile.IPv4);
                Add(profile, 23, profile.IPv6);
            }

            int contradictions = observations.Count(row => row.Finding == "Contradictory");
            int unavailable = observations.Count(row => row.Finding == "Incomplete");
            int unprofiled = paths.Count(path => profileRows.All(profile => profile.InterfaceIndex != path.InterfaceIndex));
            CheckState state = unavailable > 0 ? CheckState.Unknown : contradictions > 0 ? CheckState.Attention : CheckState.Information;
            string detail = $"Correlated {profileRows.Count} active profile{(profileRows.Count == 1 ? "" : "s")}, {paths.Count} default path{(paths.Count == 1 ? "" : "s")} and {probes.Count} gateway result{(probes.Count == 1 ? "" : "s")}.";
            if (contradictions > 0) detail += $" {contradictions} interface-family state{(contradictions == 1 ? "" : "s")} need review because Windows connectivity and the current routing table disagree.";
            if (unavailable > 0) detail += $" {unavailable} interface-family state{(unavailable == 1 ? "" : "s")} could not be completed.";
            if (unprofiled > 0) detail += $" {unprofiled} default path{(unprofiled == 1 ? " has" : "s have")} no matching connection profile; VPN and virtual interfaces can produce this condition.";
            if (profileRows.Count == 0 && paths.Count == 0) detail += " No active connection profile or default path was reported.";
            if (state == CheckState.Information && profileRows.Count + paths.Count > 0) detail += " No contradiction was found in the collected scope.";
            var evidence = new ToolResult(0, JsonSerializer.Serialize(new { complete = state != CheckState.Unknown, observations, unprofiledDefaultPaths = unprofiled }, Capture.JsonOptions), "", false);
            return new(name, state, detail + boundary, Evidence: evidence);

            void Add(Profile profile, int family, string connectivity)
            {
                var path = paths.FirstOrDefault(item => item.InterfaceIndex == profile.InterfaceIndex && item.Family == family);
                var probe = path is null ? null : probes.FirstOrDefault(item => item.InterfaceIndex == path.InterfaceIndex &&
                    item.Family == path.Family && string.Equals(item.NextHop, path.NextHop, StringComparison.OrdinalIgnoreCase));
                bool hasPath = path is not null;
                string finding;
                if (hasPath && probe is null) finding = "Incomplete";
                else if (connectivity == "Internet" && !hasPath || connectivity is "Disconnected" or "NoTraffic" && hasPath)
                    finding = "Contradictory";
                else finding = "Consistent";
                observations.Add(new(profile.InterfaceIndex, profile.Alias, family, connectivity, hasPath, probe?.Status, finding));
            }
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Complete profile, route and gateway evidence is required before connectivity states can be correlated safely.");
        }
    }

    private static IReadOnlyList<Profile> ReadProfiles(IReadOnlyList<WorkflowCheck> checks)
    {
        if (checks.Count == 0 || checks.Any(check => check is null || check.EffectiveState == CheckState.Unknown || check.Evidence is null))
            throw new InvalidDataException("Profile evidence is incomplete.");
        string json = checks[0].Evidence!.Output;
        if (checks.Any(check => !string.Equals(check.Evidence!.Output, json, StringComparison.Ordinal)))
            throw new InvalidDataException("Profile evidence is inconsistent.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 6 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("profiles", out var rows) ||
            rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 32) throw new InvalidDataException("Profile evidence is invalid.");
        var result = new List<Profile>();
        var seen = new HashSet<int>();
        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("InterfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 1 || !seen.Add(index) ||
                !row.TryGetProperty("InterfaceAlias", out var aliasValue) || aliasValue.ValueKind != JsonValueKind.String || !DriverReference.ValidText(aliasValue.GetString(), 256) ||
                !row.TryGetProperty("IPv4Connectivity", out var ipv4Value) || ipv4Value.ValueKind != JsonValueKind.String || !ValidConnectivity(ipv4Value.GetString()) ||
                !row.TryGetProperty("IPv6Connectivity", out var ipv6Value) || ipv6Value.ValueKind != JsonValueKind.String || !ValidConnectivity(ipv6Value.GetString()))
                throw new InvalidDataException("Profile row is invalid.");
            result.Add(new(index, aliasValue.GetString()!, ipv4Value.GetString()!, ipv6Value.GetString()!));
        }
        return result;
    }

    private static IReadOnlyList<Path> ReadPaths(WorkflowCheck routes)
    {
        if (routes.EffectiveState == CheckState.Unknown || routes.Evidence is null) throw new InvalidDataException("Route evidence is incomplete.");
        using var document = JsonDocument.Parse(routes.Evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("entries", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 1024)
            throw new InvalidDataException("Route evidence is invalid.");
        var result = new List<Path>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("destination", out var destinationValue) || destinationValue.ValueKind != JsonValueKind.String ||
                !row.TryGetProperty("nextHop", out var nextHopValue) || nextHopValue.ValueKind != JsonValueKind.String ||
                !row.TryGetProperty("interfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 1)
                throw new InvalidDataException("Route row is invalid.");
            string destination = destinationValue.GetString()!;
            if (destination is not ("0.0.0.0/0" or "::/0")) continue;
            string nextHop = nextHopValue.GetString()!;
            int family = destination == "0.0.0.0/0" ? 2 : 23;
            if (!IPAddress.TryParse(nextHop, out var address) || family == 2 && address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                family == 23 && address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 || !seen.Add(index + "\0" + family + "\0" + nextHop))
                throw new InvalidDataException("Default path identity is invalid.");
            result.Add(new(index, family, nextHop));
        }
        return result;
    }

    private static IReadOnlyList<Probe> ReadProbes(WorkflowCheck gateways)
    {
        if (gateways.EffectiveState == CheckState.Unknown || gateways.Evidence is null) throw new InvalidDataException("Gateway evidence is incomplete.");
        using var document = JsonDocument.Parse(gateways.Evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("observations", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 16)
            throw new InvalidDataException("Gateway evidence is invalid.");
        var result = new List<Probe>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("interfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 1 ||
                !row.TryGetProperty("addressFamily", out var familyValue) || !familyValue.TryGetInt32(out int family) || family is not (2 or 23) ||
                !row.TryGetProperty("nextHop", out var nextHopValue) || nextHopValue.ValueKind != JsonValueKind.String || !IPAddress.TryParse(nextHopValue.GetString(), out _) ||
                !row.TryGetProperty("status", out var statusValue) || statusValue.ValueKind != JsonValueKind.String ||
                !Enum.TryParse<GatewayProbeStatus>(statusValue.GetString(), out var status) || !Enum.IsDefined(status) ||
                !seen.Add(index + "\0" + family + "\0" + nextHopValue.GetString())) throw new InvalidDataException("Gateway result is invalid.");
            result.Add(new(index, family, nextHopValue.GetString()!, status));
        }
        return result;
    }

    private static bool ValidConnectivity(string? value) => value is "Disconnected" or "NoTraffic" or "Subnet" or "LocalNetwork" or "Internet";
}
