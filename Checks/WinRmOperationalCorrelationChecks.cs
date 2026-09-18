using KillerBench.Engine;

internal static class WinRmOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear WinRM evidence remains informational");
        Require(Check(listener: CheckState.Attention).State == CheckState.Attention, "Disabled WinRM listener requests review");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current WinRM service concern requests review");
        Require(Check(events: CheckState.Attention, history: CheckState.Attention).State == CheckState.Information,
            "Historical WinRM records alone remain informational");
        Require(Check(events: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete WinRM history takes precedence over current attention");
        Require(WinRmOperationalCorrelation.Check(Inputs().Append(Info("WinRM listeners")).ToArray()).State == CheckState.Unknown,
            "Duplicate WinRM evidence identity fails closed");
        Require(WinRmOperationalCorrelation.Check(Inputs().Where(check => check.Name != "Focused service readiness").ToArray()).State == CheckState.Unknown,
            "Missing WinRM evidence identity fails closed");
        try
        {
            WinRmOperationalCorrelation.Check(Inputs(), new CancellationToken(true));
            throw new Exception("Canceled WinRM correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(listener: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery or configuration changes", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish effective policy", StringComparison.Ordinal),
            "WinRM conclusion preserves configuration and repair boundaries");
        Console.WriteLine("PASS: WinRM listeners, operational history and current service readiness correlate without selecting changes.");
    }

    private static WorkflowCheck Check(CheckState listener = CheckState.Information,
        CheckState events = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => WinRmOperationalCorrelation.Check(Inputs(listener, events, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState listener = CheckState.Information,
        CheckState events = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) =>
    [
        Info("WinRM listeners"),
        new("WinRM HTTP listener", listener, "fixture"),
        new("Recent Windows Remote Management events", events, "fixture"),
        new("Windows Remote Management service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
