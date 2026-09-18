using System.Text.Json;
using KillerBench.Engine;

internal static class WinsockReadinessCorrelationChecks
{
    internal static void Run()
    {
        object Protocol(uint id, int family, int socket, int protocol, int chain = 1) => new
        {
            catalogId = id,
            provider = GuidFrom((int)id),
            chainLength = chain,
            addressFamily = family,
            socketType = socket,
            protocol
        };
        object Namespace(int id, uint type, bool active = true) => new
        {
            provider = GuidFrom(id),
            nameSpace = type,
            active,
            version = 0u
        };
        WorkflowCheck Protocols(object items, CheckState state = CheckState.Information, bool complete = true) =>
            new("Winsock protocol catalog", state, "fixture", Evidence: new(0, JsonSerializer.Serialize(new { complete, error = 0, items }), "", false));
        WorkflowCheck Namespaces(object items, CheckState state = CheckState.Information, bool complete = true) =>
            new("Winsock namespace providers", state, "fixture", Evidence: new(0, JsonSerializer.Serialize(new { complete, error = 0, items }), "", false));
        WorkflowCheck Files(CheckState state = CheckState.Information, string name = "Winsock protocol provider files") =>
            new(name, state, "fixture", Evidence: new(0, "{}", "", false));
        var ipv4 = new[] { Protocol(1, 2, 1, 6), Protocol(2, 2, 2, 17) };
        var dual = new[] { Protocol(1, 2, 1, 6), Protocol(2, 2, 2, 17), Protocol(3, 23, 1, 6), Protocol(4, 23, 2, 17) };
        var dns = new[] { Namespace(20, 12) };

        var ready = WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns), Files());
        Require(ready is { State: CheckState.Clear, Repair: null } && ready.Detail.Contains("IPv4 and IPv6", StringComparison.Ordinal) &&
            ready.Detail.Contains("not a resolution or connectivity test", StringComparison.Ordinal),
            "Complete dual-stack base transports and active DNS provider produce a scoped clear verdict");
        var ipv4Only = WinsockReadinessCorrelation.Check(Protocols(ipv4), Namespaces(dns), Files());
        Require(ipv4Only.State == CheckState.Clear && ipv4Only.Detail.Contains("for IPv4", StringComparison.Ordinal),
            "One complete transport family remains structurally ready without inventing dual-stack support");
        var noDns = WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(new[] { Namespace(20, 12, false) }), Files());
        Require(noDns.State == CheckState.Attention && noDns.Detail.Contains("active DNS namespace provider", StringComparison.Ordinal) && noDns.Repair is null,
            "Missing active DNS namespace evidence requests review without selecting reset");
        var incompleteTransport = WinsockReadinessCorrelation.Check(Protocols(new[] { Protocol(1, 2, 1, 6) }), Namespaces(dns), Files());
        Require(incompleteTransport.State == CheckState.Attention && incompleteTransport.Detail.Contains("TCP and UDP base-provider pair", StringComparison.Ordinal),
            "Missing paired base transport requests review without selecting repair");
        var providerReview = WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns), Files(CheckState.Attention));
        Require(providerReview.State == CheckState.Attention && providerReview.Repair is null &&
            providerReview.Detail.Contains("provider files", StringComparison.Ordinal),
            "Missing or non-Valid registered provider files affect readiness without selecting reset");

        foreach (var unknown in new[]
        {
            WinsockReadinessCorrelation.Check(Protocols(dual, CheckState.Unknown), Namespaces(dns), Files()),
            WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns, CheckState.Unknown), Files()),
            WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns), Files(CheckState.Unknown)),
            WinsockReadinessCorrelation.Check(Protocols(dual, complete: false), Namespaces(dns), Files()),
            WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns, complete: false), Files()),
            WinsockReadinessCorrelation.Check(Protocols(new[] { Protocol(1, 2, 1, 6), Protocol(1, 2, 2, 17) }), Namespaces(dns), Files()),
            WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(new[] { Namespace(20, 12), Namespace(20, 15) }), Files()),
            WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns), Files(name: "Wrong files"))
        }) Require(unknown.State == CheckState.Unknown && unknown.Repair is null,
            "Incomplete, unknown, duplicate and malformed catalog evidence cannot produce readiness");
        try
        {
            WinsockReadinessCorrelation.Check(Protocols(dual), Namespaces(dns), Files(), new CancellationToken(true));
            throw new Exception("Winsock readiness cancellation ignored");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Winsock protocol, namespace and provider-file correlation produces scoped readiness without reset or connectivity claims.");
    }

    private static string GuidFrom(int value) => new Guid(value, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString("D");
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
