using KillerBench.Engine;

internal static class RemoteAccessOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear.State == CheckState.Information && clear.Detail.Contains("Reported profiles: 2", StringComparison.Ordinal),
            "Complete Remote Access evidence remains informational and retains the bounded profile count");
        Require(Check(profiles: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete VPN profile evidence takes precedence over current service attention");
        Require(Check(history: CheckState.Attention).State == CheckState.Information,
            "Historical Remote Access service failures alone remain informational");
        var attention = Check(service: CheckState.Attention);
        Require(attention.State == CheckState.Attention && attention.Repair is null &&
            attention.Detail.Contains("cannot authorize recovery or a connection attempt", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish profile applicability", StringComparison.Ordinal),
            "Current Remote Access service concern requests review without widening repair authority");
        Require(RemoteAccessOperationalCorrelation.Check([.. Inputs(), Info("Current-account VPN profiles")]).State == CheckState.Unknown,
            "Duplicate Remote Access evidence identity fails closed");
        Require(RemoteAccessOperationalCorrelation.Check([.. Inputs().Where(check => check.Name != "Focused service readiness")]).State == CheckState.Unknown,
            "Missing Remote Access evidence identity fails closed");
        try
        {
            RemoteAccessOperationalCorrelation.Check(Inputs(), new CancellationToken(true));
            throw new Exception("Canceled Remote Access correlation completed");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: VPN profiles, exact history and Remote Access service readiness correlate without connection or repair selection.");
    }

    private static WorkflowCheck Check(CheckState profiles = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
        RemoteAccessOperationalCorrelation.Check(Inputs(profiles, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState profiles = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Current-account VPN profiles", profiles, "2 VPN profiles reported for the account running KillerBench."),
        new("Remote Access Connection Manager failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
