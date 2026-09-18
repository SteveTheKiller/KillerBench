using KillerBench.Engine;

internal static class NetworkOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = NetworkOperationalCorrelation.Check(Firewall(), Path(), Connectivity(), Winsock(CheckState.Clear), Dns(), Dhcp());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("no repair is selected"), "Complete network correlations produce a bounded informational result");

        var attention = NetworkOperationalCorrelation.Check(Firewall(CheckState.Attention), Path(), Connectivity(CheckState.Attention), Winsock(CheckState.Clear), Dns(), Dhcp());
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("2 need attention") &&
            attention.Detail.Contains("bounded target lookup or connection test"), "Network attention remains review-only and recommends targeted validation");

        var incomplete = NetworkOperationalCorrelation.Check(Firewall(CheckState.Attention), Path(CheckState.Unknown), Connectivity(), Winsock(CheckState.Clear), Dns(), Dhcp());
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete network evidence takes precedence over attention");

        var subsystemAttention = NetworkOperationalCorrelation.Check(Firewall(), Path(), Connectivity(), Winsock(), Dns(CheckState.Attention), Dhcp(CheckState.Attention));
        Require(subsystemAttention is { State: CheckState.Attention, Repair: null } && subsystemAttention.Detail.Contains("2 need attention"),
            "DNS and DHCP operational findings contribute to the top-level network outcome");

        var wrongIdentity = NetworkOperationalCorrelation.Check(Firewall() with { Name = "Other firewall" }, Path(), Connectivity(), Winsock(), Dns(), Dhcp());
        Require(wrongIdentity is { State: CheckState.Unknown, Repair: null } && wrongIdentity.Detail.Contains("unexpected identity"),
            "Unexpected network correlation identities fail closed");

        try
        {
            NetworkOperationalCorrelation.Check(Firewall(), Path(), Connectivity(), Winsock(), Dns(), Dhcp(), new CancellationToken(true));
            throw new Exception("Network operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: network operational correlation combines firewall, path, gateway, Winsock, DNS and DHCP evidence without selecting reset or repair.");
    }

    private static WorkflowCheck Firewall(CheckState state = CheckState.Information) => new("Active network firewall alignment", state, "fixture");
    private static WorkflowCheck Path(CheckState state = CheckState.Information) => new("Network path readiness", state, "fixture");
    private static WorkflowCheck Connectivity(CheckState state = CheckState.Information) => new("Network connectivity correlation", state, "fixture");
    private static WorkflowCheck Winsock(CheckState state = CheckState.Information) => new("Winsock name-resolution readiness", state, "fixture");
    private static WorkflowCheck Dns(CheckState state = CheckState.Information) => new("DNS operational correlation", state, "fixture");
    private static WorkflowCheck Dhcp(CheckState state = CheckState.Information) => new("DHCP operational correlation", state, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
