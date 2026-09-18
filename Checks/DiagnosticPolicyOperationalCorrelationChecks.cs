using KillerBench.Engine;

internal static class DiagnosticPolicyOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Diagnostic Policy evidence remains informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current Diagnostic Policy service concern requests review");
        Require(Check(events: CheckState.Attention, history: CheckState.Attention).State == CheckState.Information,
            "Historical Diagnostic Policy records alone remain informational");
        Require(Check(events: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete Diagnostic Policy history takes precedence over current attention");
        Require(DiagnosticPolicyOperationalCorrelation.Check(Inputs().Append(new("Recent Diagnostic Policy events", CheckState.Information, "duplicate")).ToArray()).State == CheckState.Unknown,
            "Duplicate Diagnostic Policy evidence identity fails closed");
        Require(DiagnosticPolicyOperationalCorrelation.Check(Inputs().Where(check => check.Name != "Focused service readiness").ToArray()).State == CheckState.Unknown,
            "Missing Diagnostic Policy evidence identity fails closed");
        try { DiagnosticPolicyOperationalCorrelation.Check(Inputs(), new CancellationToken(true)); throw new Exception("Canceled Diagnostic Policy correlation completed"); }
        catch (OperationCanceledException) { }
        var attention = Check(service: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish a current fault", StringComparison.Ordinal),
            "Diagnostic Policy conclusion preserves troubleshooting and repair boundaries");
        Console.WriteLine("PASS: Diagnostic Policy history and current service readiness correlate without selecting troubleshooting or repair.");
    }

    private static WorkflowCheck Check(CheckState events = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => DiagnosticPolicyOperationalCorrelation.Check(Inputs(events, history, service));

    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState events = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Recent Diagnostic Policy events", events, "fixture"),
        new("Diagnostic Policy service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
