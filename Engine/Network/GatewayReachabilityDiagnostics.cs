using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public enum GatewayProbeStatus { Reachable, NoReply, Unavailable, OnLink }

public sealed record GatewayProbeObservation(int InterfaceIndex, int AddressFamily, string NextHop,
    GatewayProbeStatus Status, long? RoundtripMilliseconds, string Detail);

public static class GatewayReachabilityDiagnostics
{
    private sealed record GatewayTarget(int InterfaceIndex, int AddressFamily, IPAddress Address, long Metric);

    public static async Task<WorkflowCheck> CheckAsync(WorkflowCheck routes,
        Func<IPAddress, TimeSpan, CancellationToken, Task<GatewayProbeObservation>>? probe = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(routes);
        token.ThrowIfCancellationRequested();
        const string name = "Default gateway reachability";
        if (routes.EffectiveState == CheckState.Unknown || routes.Evidence is null)
            return new(name, CheckState.Unknown,
                "Complete current route evidence is required before a gateway can be tested. No address was contacted and no network setting changed.",
                Evidence: routes.Evidence);
        IReadOnlyList<GatewayTarget> targets;
        try { targets = ReadTargets(routes.Evidence.Output); }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return new(name, CheckState.Unknown,
                "Default gateway identities could not be established from the current route evidence. No address was contacted and no network setting changed.",
                Evidence: routes.Evidence);
        }
        if (targets.Count == 0)
            return Result(CheckState.Attention, [], "No IPv4 or IPv6 default gateway was reported. Off-subnet reachability is not established.");

        probe ??= ProbeAsync;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));
        var tasks = targets.Select(target => ObserveAsync(target, probe, deadline.Token)).ToArray();
        GatewayProbeObservation[] observations;
        try { observations = await Task.WhenAll(tasks).WaitAsync(deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Result(CheckState.Unknown, [],
                "The bounded gateway checks did not all finish within 12 seconds. Reachability coverage is incomplete.");
        }
        token.ThrowIfCancellationRequested();
        int reached = observations.Count(item => item.Status == GatewayProbeStatus.Reachable);
        int noReply = observations.Count(item => item.Status == GatewayProbeStatus.NoReply);
        int unavailable = observations.Count(item => item.Status == GatewayProbeStatus.Unavailable);
        int onLink = observations.Count(item => item.Status == GatewayProbeStatus.OnLink);
        CheckState state = unavailable > 0 ? CheckState.Unknown : noReply > 0 ? CheckState.Attention : CheckState.Information;
        string detail = $"Tested {observations.Length - onLink} of {observations.Length} configured default gateway paths: {reached} replied, {noReply} did not reply, {unavailable} could not be tested and {onLink} were on-link routes without a gateway address.";
        if (noReply > 0) detail += " A gateway can block ICMP while forwarding other traffic, so no reply is not proof that the gateway or Internet connection is down.";
        if (unavailable > 0) detail += " One or more probe results were incomplete; do not infer overall reachability.";
        return Result(state, observations, detail);

        static WorkflowCheck Result(CheckState state, IReadOnlyList<GatewayProbeObservation> rows, string detail)
        {
            var evidence = new ToolResult(0, JsonSerializer.Serialize(new { complete = state != CheckState.Unknown, observations = rows }, Capture.JsonOptions), "", false);
            string paths = rows.Count == 0 ? "" : " Results: " + string.Join("; ", rows.Select(row =>
                $"interface {row.InterfaceIndex} IPv{(row.AddressFamily == 2 ? 4 : 6)} {row.NextHop}: {row.Status}")) + ".";
            return new(name, state, detail + paths +
                " Only exact next-hop addresses from the current routing table were contacted. No neighboring address was scanned and no route, adapter, address, DNS, proxy, firewall or policy setting changed.", Evidence: evidence);
        }
    }

    private static async Task<GatewayProbeObservation> ObserveAsync(GatewayTarget target,
        Func<IPAddress, TimeSpan, CancellationToken, Task<GatewayProbeObservation>> probe, CancellationToken token)
    {
        if (IPAddress.IsLoopback(target.Address) || target.Address.Equals(IPAddress.Any) || target.Address.Equals(IPAddress.IPv6Any))
            return new(target.InterfaceIndex, target.AddressFamily, target.Address.ToString(), GatewayProbeStatus.OnLink, null,
                "The default route has no distinct gateway address to probe.");
        try
        {
            var result = await (probe(target.Address, TimeSpan.FromSeconds(3), token)
                ?? throw new InvalidOperationException("Gateway probe provider returned no task.")).WaitAsync(token).ConfigureAwait(false);
            if (result is null || result.InterfaceIndex is not 0 && result.InterfaceIndex != target.InterfaceIndex || result.AddressFamily != target.AddressFamily ||
                !IPAddress.TryParse(result.NextHop, out var returned) || !returned.Equals(target.Address) ||
                !Enum.IsDefined(result.Status) || result.RoundtripMilliseconds is < 0 or > 60_000 ||
                result.Status == GatewayProbeStatus.Reachable && result.RoundtripMilliseconds is null ||
                result.Status != GatewayProbeStatus.Reachable && result.RoundtripMilliseconds is not null ||
                !DriverReference.ValidText(result.Detail, 512))
                return new(target.InterfaceIndex, target.AddressFamily, target.Address.ToString(), GatewayProbeStatus.Unavailable, null,
                    "The gateway probe returned incomplete or mismatched evidence.");
            return result with { InterfaceIndex = target.InterfaceIndex, AddressFamily = target.AddressFamily, NextHop = target.Address.ToString() };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is PingException or SocketException or InvalidOperationException)
        {
            return new(target.InterfaceIndex, target.AddressFamily, target.Address.ToString(), GatewayProbeStatus.Unavailable, null,
                "The gateway probe was unavailable: " + error.GetType().Name + ".");
        }
    }

    public static async Task<GatewayProbeObservation> ProbeAsync(IPAddress address, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(3)) throw new ArgumentOutOfRangeException(nameof(timeout));
        token.ThrowIfCancellationRequested();
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(address, timeout, [], new PingOptions(64, true), token).ConfigureAwait(false);
        int family = address.AddressFamily == AddressFamily.InterNetwork ? 2 : 23;
        return reply.Status == IPStatus.Success
            ? new(0, family, address.ToString(), GatewayProbeStatus.Reachable, reply.RoundtripTime, "The exact gateway address replied to ICMP.")
            : new(0, family, address.ToString(), GatewayProbeStatus.NoReply, null, "The exact gateway address returned " + reply.Status + ".");
    }

    private static GatewayTarget[] ReadTargets(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var complete) ||
            complete.ValueKind != JsonValueKind.True || !root.TryGetProperty("entries", out var entries) ||
            entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 1024)
            throw new InvalidDataException("Route evidence is incomplete.");
        var targets = new List<GatewayTarget>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("destination", out var destinationValue) || destinationValue.ValueKind != JsonValueKind.String ||
                !entry.TryGetProperty("nextHop", out var nextHopValue) || nextHopValue.ValueKind != JsonValueKind.String ||
                !entry.TryGetProperty("interfaceIndex", out var interfaceValue) || !interfaceValue.TryGetInt32(out int index) || index < 1 ||
                !entry.TryGetProperty("metric", out var metricValue) || !metricValue.TryGetInt64(out long metric) || metric < 0)
                throw new InvalidDataException("Route identity is invalid.");
            string destination = destinationValue.GetString()!;
            if (destination is not ("0.0.0.0/0" or "::/0")) continue;
            string nextHop = nextHopValue.GetString()!;
            if (!IPAddress.TryParse(nextHop, out var address)) throw new InvalidDataException("Gateway address is invalid.");
            int family = destination == "0.0.0.0/0" ? 2 : 23;
            if (family == 2 && address.AddressFamily != AddressFamily.InterNetwork ||
                family == 23 && address.AddressFamily != AddressFamily.InterNetworkV6 ||
                !seen.Add(index + "\0" + family + "\0" + address)) throw new InvalidDataException("Gateway identity is invalid or duplicated.");
            targets.Add(new(index, family, address, metric));
            if (targets.Count > 16) throw new InvalidDataException("Default gateway coverage exceeded its bound.");
        }
        return [.. targets.OrderBy(target => target.Metric).ThenBy(target => target.InterfaceIndex).ThenBy(target => target.AddressFamily)];
    }
}
