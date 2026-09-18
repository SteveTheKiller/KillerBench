using KillerBench.Engine;

internal static class DnsOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = DnsOperationalCorrelation.Check(Service(), Servers(), Policy(), Events(), Path());
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("Current evidence needing attention: 0") && clear.Detail.Contains("no repair is selected"),
            "Complete DNS evidence produces a bounded informational result");

        var history = DnsOperationalCorrelation.Check(Service(), Servers(), Policy(), Events(CheckState.Attention), Path());
        Require(history is { State: CheckState.Information, Repair: null } &&
            history.Detail.Contains("historical sources with retained warnings or failures: 2") &&
            history.Detail.Contains("did not establish a DNS readiness problem"),
            "Historical DNS events do not become a current outage verdict");

        var attention = DnsOperationalCorrelation.Check(Service(CheckState.Attention), Servers(), Policy(), Events(), Path(CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("Current evidence needing attention: 2") && attention.Detail.Contains("bounded lookup"),
            "Current DNS readiness problems remain review-only and recommend exact validation");

        var incomplete = DnsOperationalCorrelation.Check(Service(CheckState.Attention), Servers(CheckState.Unknown), Policy(), Events(CheckState.Attention), Path());
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("incomplete sources: 1"),
            "Incomplete DNS evidence takes precedence over current and historical attention");

        var wrongIdentity = DnsOperationalCorrelation.Check(Service() with { Name = "Other service" }, Servers(), Policy(), Events(), Path());
        Require(wrongIdentity is { State: CheckState.Unknown, Repair: null } && wrongIdentity.Detail.Contains("unexpected identity"),
            "Unexpected DNS correlation identities fail closed");

        var duplicateEvents = new[] { Events()[0], Events()[0] };
        Require(DnsOperationalCorrelation.Check(Service(), Servers(), Policy(), duplicateEvents, Path()).State == CheckState.Unknown,
            "Duplicate DNS event identities fail closed");

        try
        {
            DnsOperationalCorrelation.Check(Service(), Servers(), Policy(), Events(), Path(), new CancellationToken(true));
            throw new Exception("DNS operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: DNS operational correlation separates current readiness from historical warnings without selecting cache, service or configuration changes.");
    }

    private static WorkflowCheck Service(CheckState state = CheckState.Clear) => new("DnsClient", state, "fixture");
    private static WorkflowCheck Servers(CheckState state = CheckState.Information) => new("DNS server configuration", state, "fixture");
    private static WorkflowCheck Policy(CheckState state = CheckState.Information) => new("Effective DNS policy", state, "fixture");
    private static WorkflowCheck Path(CheckState state = CheckState.Information) => new("Network path readiness", state, "fixture");
    private static WorkflowCheck[] Events(CheckState state = CheckState.Information) =>
    [
        new("Recent DNS Client system events", state, "fixture"),
        new("Recent DNS Client operational events", state, "fixture")
    ];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
