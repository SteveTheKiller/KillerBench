using KillerBench.Engine;

internal static class DhcpOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = DhcpOperationalCorrelation.Check(Service(), Lease(), Addresses(), Events());
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("Current evidence needing attention: 0") && clear.Detail.Contains("no repair is selected"),
            "Complete DHCP evidence produces a bounded informational result");

        var history = DhcpOperationalCorrelation.Check(Service(), Lease(), Addresses(), Events(CheckState.Attention));
        Require(history is { State: CheckState.Information, Repair: null } &&
            history.Detail.Contains("historical sources with retained warnings or failures: 4") &&
            history.Detail.Contains("did not establish a DHCP readiness problem"),
            "Historical DHCP events do not become a current failure verdict");

        var attention = DhcpOperationalCorrelation.Check(Service(CheckState.Attention), Lease(CheckState.Attention), Addresses(), Events());
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("Current evidence needing attention: 2") && attention.Detail.Contains("bounded renewal"),
            "Current DHCP readiness problems remain review-only");

        var addressAttention = DhcpOperationalCorrelation.Check(Service(), Lease(), Addresses(CheckState.Attention), Events());
        Require(addressAttention.State == CheckState.Attention && addressAttention.Repair is null,
            "Current address problems contribute to DHCP review without selecting renewal");

        var incomplete = DhcpOperationalCorrelation.Check(Service(CheckState.Attention), Lease(CheckState.Unknown), Addresses(), Events(CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("incomplete sources: 1"),
            "Incomplete DHCP evidence takes precedence over current and historical attention");

        Require(DhcpOperationalCorrelation.Check(Service(), Lease(), Array.Empty<WorkflowCheck>(), Events()).State == CheckState.Unknown,
            "Missing IP address evidence fails closed");
        Require(DhcpOperationalCorrelation.Check(Service(), Lease(), Addresses(), [Events()[0], Events()[0], Events()[2], Events()[3]]).State == CheckState.Unknown,
            "Duplicate DHCP event identities fail closed");

        try
        {
            DhcpOperationalCorrelation.Check(Service(), Lease(), Addresses(), Events(), new CancellationToken(true));
            throw new Exception("DHCP operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: DHCP operational correlation separates current lease and address readiness from historical warnings without selecting renewal or configuration changes.");
    }

    private static WorkflowCheck Service(CheckState state = CheckState.Clear) => new("DhcpClient", state, "fixture");
    private static WorkflowCheck Lease(CheckState state = CheckState.Information) => new("DHCP lease state", state, "fixture");
    private static WorkflowCheck[] Addresses(CheckState state = CheckState.Information) =>
    [
        new("IP address: Ethernet / 192.0.2.10", state, "fixture")
    ];
    private static WorkflowCheck[] Events(CheckState state = CheckState.Information) =>
    [
        new("Recent DHCP Client IPv4 administrative events", state, "fixture"),
        new("Recent DHCP Client IPv4 operational events", state, "fixture"),
        new("Recent DHCP Client IPv6 administrative events", state, "fixture"),
        new("Recent DHCP Client IPv6 operational events", state, "fixture")
    ];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
