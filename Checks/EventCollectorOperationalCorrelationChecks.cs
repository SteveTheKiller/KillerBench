using KillerBench.Engine;

internal static class EventCollectorOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear.State == CheckState.Information && clear.Detail.Contains("Reported subscriptions: 2", StringComparison.Ordinal),
            "Complete Windows Event Collector evidence remains informational and retains the bounded count");
        Require(Check(subscriptions: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete subscription evidence takes precedence over current service attention");
        Require(Check(history: CheckState.Attention).State == CheckState.Information,
            "Historical Windows Event Collector failures alone remain informational");
        var attention = Check(service: CheckState.Attention);
        Require(attention.State == CheckState.Attention && attention.Repair is null &&
            attention.Detail.Contains("cannot authorize recovery or subscription changes", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish source reachability", StringComparison.Ordinal),
            "Current service concern requests review without widening repair authority");
        Require(EventCollectorOperationalCorrelation.Check(Inputs().Append(Info("Event Collector subscriptions")).ToArray()).State == CheckState.Unknown,
            "Duplicate Event Collector evidence identity fails closed");
        Require(EventCollectorOperationalCorrelation.Check(Inputs().Where(check => check.Name != "Focused service readiness").ToArray()).State == CheckState.Unknown,
            "Missing Event Collector evidence identity fails closed");
        try
        {
            EventCollectorOperationalCorrelation.Check(Inputs(), new CancellationToken(true));
            throw new Exception("Canceled Event Collector correlation completed");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows Event Collector subscriptions, exact history and current service readiness correlate without selecting repair.");
    }

    private static WorkflowCheck Check(CheckState subscriptions = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
        EventCollectorOperationalCorrelation.Check(Inputs(subscriptions, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState subscriptions = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Event Collector subscriptions", subscriptions, "2 Windows Event Collector subscriptions reported: Operations, Security"),
        new("Windows Event Collector service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
