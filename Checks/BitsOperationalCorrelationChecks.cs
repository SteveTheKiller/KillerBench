using KillerBench.Engine;

internal static class BitsOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("owning-application completion remain unverified", StringComparison.Ordinal),
            "Complete BITS evidence remains informational without claiming transfer completion");

        var attention = Check(jobs: CheckState.Attention, history: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a focused retry or service action", StringComparison.Ordinal),
            "BITS job and history attention requests exact review without selecting repair");

        var incomplete = Check(service: CheckState.Unknown, history: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete BITS service evidence takes precedence over historical attention");

        var repairableInput = Check(service: CheckState.Repairable);
        Require(repairableInput is { State: CheckState.Attention, Repair: null },
            "Repairable input remains review-only in the aggregate");

        Require(BitsOperationalCorrelation.Check(Info("Wrong jobs"), Service(), History()).State == CheckState.Unknown,
            "Unexpected BITS job identity fails closed");
        Require(BitsOperationalCorrelation.Check(Jobs(), Info("Wrong service"), History()).State == CheckState.Unknown,
            "Unexpected BITS service identity fails closed");
        Require(BitsOperationalCorrelation.Check(Jobs(), Service(), Info("Wrong history")).State == CheckState.Unknown,
            "Unexpected BITS history identity fails closed");

        try
        {
            BitsOperationalCorrelation.Check(Jobs(), Service(), History(), new CancellationToken(true));
            throw new Exception("Background transfer operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: BITS jobs, current service state and exact failure history correlate deterministically without selecting repair.");
    }

    private static WorkflowCheck Check(CheckState jobs = CheckState.Information,
        CheckState service = CheckState.Information, CheckState history = CheckState.Information) =>
        BitsOperationalCorrelation.Check(Jobs(jobs), Service(service), History(history));

    private static WorkflowCheck Jobs(CheckState state = CheckState.Information) =>
        new("BITS transfer jobs", state, "fixture");
    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new("Background Intelligent Transfer Service state", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("BITS service failure history", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
