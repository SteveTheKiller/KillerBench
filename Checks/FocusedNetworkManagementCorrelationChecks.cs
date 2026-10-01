using KillerBench.Engine;

internal static class FocusedNetworkManagementCorrelationChecks
{
    internal static void Run()
    {
        Require(WinHttp().State == CheckState.Information, "Complete clear WinHTTP Auto-Proxy evidence remains informational");
        Require(WinHttp(history: CheckState.Attention).State == CheckState.Information, "Historical WinHTTP failures remain informational");
        Require(WinHttp(service: CheckState.Attention).State == CheckState.Attention, "Current WinHTTP service concern requests review");
        Require(WinHttp(proxy: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown, "Incomplete proxy evidence takes precedence");
        Require(WinHttpAutoProxyOperationalCorrelation.Check([.. WinHttpInputs().Append(new("WinHTTP default proxy", CheckState.Information, "duplicate"))]).State == CheckState.Unknown,
            "Duplicate WinHTTP evidence fails closed");

        Require(Connection().State == CheckState.Information, "Complete clear Connection Manager evidence remains informational");
        Require(Connection(profile: CheckState.Attention, history: CheckState.Attention).State == CheckState.Information,
            "Profile and historical Connection Manager context remain informational");
        Require(Connection(service: CheckState.Attention).State == CheckState.Attention, "Current Connection Manager service concern requests review");
        Require(Connection(profile: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown, "Incomplete profiles take precedence");
        Require(ConnectionManagerOperationalCorrelation.Check([.. ConnectionInputs().Where(check => check.Name != "Windows Connection Manager failure history")]).State == CheckState.Unknown,
            "Missing Connection Manager evidence fails closed");

        try { WinHttpAutoProxyOperationalCorrelation.Check(WinHttpInputs(), new CancellationToken(true)); throw new Exception("Canceled WinHTTP correlation completed"); }
        catch (OperationCanceledException) { }
        try { ConnectionManagerOperationalCorrelation.Check(ConnectionInputs(), new CancellationToken(true)); throw new Exception("Canceled Connection Manager correlation completed"); }
        catch (OperationCanceledException) { }
        Require(WinHttp(service: CheckState.Attention).Repair is null && Connection(service: CheckState.Attention).Repair is null,
            "Focused network-management conclusions never select repairs");
        Console.WriteLine("PASS: proxy and network-profile evidence correlate with focused network-management services without selecting changes.");
    }

    private static WorkflowCheck WinHttp(CheckState proxy = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => WinHttpAutoProxyOperationalCorrelation.Check(WinHttpInputs(proxy, history, service));
    private static IReadOnlyList<WorkflowCheck> WinHttpInputs(CheckState proxy = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("WinHTTP default proxy", proxy, "fixture"),
        new("Current-account Internet proxy", proxy, "fixture"),
        new("WinHTTP Auto-Proxy service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];
    private static WorkflowCheck Connection(CheckState profile = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => ConnectionManagerOperationalCorrelation.Check(ConnectionInputs(profile, history, service));
    private static IReadOnlyList<WorkflowCheck> ConnectionInputs(CheckState profile = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Network profile: Ethernet", profile, "fixture"),
        new("Windows Connection Manager failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
