using KillerBench.Engine;

internal static class FocusedStorageServiceOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear focused Storage Service evidence remains informational");
        Require(Check(jobs: CheckState.Attention).State == CheckState.Attention, "Current adverse storage job requests review");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current Storage Service concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Information, "Historical Storage Service failures remain informational");
        Require(Check(jobs: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown, "Incomplete storage jobs take precedence");
        Require(FocusedStorageServiceOperationalCorrelation.Check([.. Inputs().Append(new("Storage jobs", CheckState.Information, "duplicate"))]).State == CheckState.Unknown,
            "Duplicate focused Storage Service evidence fails closed");
        Require(FocusedStorageServiceOperationalCorrelation.Check([.. Inputs().Where(check => check.Name != "Focused service readiness")]).State == CheckState.Unknown,
            "Missing focused Storage Service evidence fails closed");
        try { FocusedStorageServiceOperationalCorrelation.Check(Inputs(), new CancellationToken(true)); throw new Exception("Canceled Storage Service correlation completed"); }
        catch (OperationCanceledException) { }
        var attention = Check(jobs: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery or storage changes", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish device", StringComparison.Ordinal),
            "Focused Storage Service conclusion preserves job, service and storage boundaries");
        Console.WriteLine("PASS: current Storage module jobs, exact history and focused service readiness correlate without selecting changes.");
    }

    private static WorkflowCheck Check(CheckState jobs = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => FocusedStorageServiceOperationalCorrelation.Check(Inputs(jobs, history, service));
    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState jobs = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Storage jobs", jobs, "fixture"),
        new("Storage Service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
