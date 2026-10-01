using KillerBench.Engine;

internal static class NetworkConnectionsOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Network Connections evidence remains informational");
        Require(Check(profile: CheckState.Attention).State == CheckState.Information, "Public profile context does not become a service fault");
        Require(Check(history: CheckState.Attention).State == CheckState.Information, "Historical Network Connections failures remain informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current Network Connections service concern requests review");
        Require(Check(profile: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown, "Incomplete profiles take precedence over service attention");
        Require(NetworkConnectionsOperationalCorrelation.Check([.. Inputs().Append(new("Focused service readiness", CheckState.Information, "duplicate"))]).State == CheckState.Unknown,
            "Duplicate Network Connections identity fails closed");
        Require(NetworkConnectionsOperationalCorrelation.Check([.. Inputs().Where(check => check.Name != "Network Connections service failure history")]).State == CheckState.Unknown,
            "Missing Network Connections identity fails closed");
        try { NetworkConnectionsOperationalCorrelation.Check(Inputs(), new CancellationToken(true)); throw new Exception("Canceled Network Connections correlation completed"); }
        catch (OperationCanceledException) { }
        var attention = Check(service: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish adapter availability", StringComparison.Ordinal),
            "Network Connections conclusion preserves service and network boundaries");
        Console.WriteLine("PASS: network profiles, exact history and Network Connections service readiness correlate without selecting changes.");
    }

    private static WorkflowCheck Check(CheckState profile = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => NetworkConnectionsOperationalCorrelation.Check(Inputs(profile, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState profile = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Network profile: Ethernet", profile, "fixture"),
        new("Network Connections service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
