using KillerBench.Engine;

internal static class ReliabilityOperationalCorrelationChecks
{
    internal static void Run()
    {
        var quiet = Invoke();
        Require(quiet is { State: CheckState.Information, Repair: null } &&
            quiet.Detail.Contains("1 Reliability Monitor result") &&
            quiet.Detail.Contains("does not establish current reliability"),
            "Complete quiet reliability evidence remains bounded and informational");

        var historical = Invoke(reliability: [Check("Application terminated unexpectedly: fixture", CheckState.Attention)]);
        Require(historical is { State: CheckState.Information, Repair: null } &&
            historical.Detail.Contains("Historical observations needing review: 1") &&
            historical.Detail.Contains("Match exact application"),
            "Historical reliability concerns remain informational");

        Require(Invoke(application: Check("Recent application errors", CheckState.Unknown)).State == CheckState.Unknown &&
            Invoke(shutdown: Check("Recent unexpected shutdowns", CheckState.Unknown),
                reliability: [Check("Reliability record", CheckState.Attention)]).State == CheckState.Unknown,
            "Incomplete reliability evidence takes precedence");

        Require(Invoke(application: Check("Wrong application source", CheckState.Information)).State == CheckState.Unknown &&
            Invoke(shutdown: Check("Wrong shutdown source", CheckState.Information)).State == CheckState.Unknown &&
            Invoke(reliability: []).State == CheckState.Unknown &&
            Invoke(reliability: [.. Enumerable.Range(0, 66).Select(index => Check("Reliability " + index, CheckState.Information))]).State == CheckState.Unknown,
            "Unexpected, empty and oversized reliability inputs fail closed");

        try { Invoke(token: new CancellationToken(true)); throw new Exception("Reliability correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: reliability correlation combines bounded application, shutdown and Reliability Monitor history without selecting repairs.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? application = null, WorkflowCheck? shutdown = null,
        IReadOnlyList<WorkflowCheck>? reliability = null, CancellationToken token = default) =>
        ReliabilityOperationalCorrelation.Check(
            application ?? Check("Recent application errors", CheckState.Information),
            shutdown ?? Check("Recent unexpected shutdowns", CheckState.Information),
            reliability ?? [Check("Reliability history", CheckState.Information)], token);

    private static WorkflowCheck Check(string name, CheckState state) => new(name, state, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
