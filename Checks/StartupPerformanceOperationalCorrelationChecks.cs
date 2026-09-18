using KillerBench.Engine;

internal static class StartupPerformanceOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Information, Repair: null },
            "Complete quiet startup evidence remains informational");
        var history = Invoke(performance: Check("Startup and shutdown performance", CheckState.Attention));
        Require(history.State == CheckState.Information && history.Detail.Contains("narrower correlation", StringComparison.Ordinal),
            "Historical degradation remains informational in the combined conclusion");
        Require(Invoke(restart: Check("Restart", CheckState.Attention)).State == CheckState.Attention,
            "A current pending restart requests attention");
        Require(Invoke(performance: Check("Startup and shutdown performance", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete history takes precedence");
        Require(Invoke(restart: Check("Wrong", CheckState.Clear)).State == CheckState.Unknown,
            "Unexpected source identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Startup performance correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: startup performance correlation combines sanitized history and restart evidence without selecting changes.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? performance = null, WorkflowCheck? restart = null,
        CancellationToken token = default) => StartupPerformanceOperationalCorrelation.Check(
            performance ?? Check("Startup and shutdown performance", CheckState.Information),
            restart ?? Check("Restart", CheckState.Clear), token);

    private static WorkflowCheck Check(string name, CheckState state) => new(name, state, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
