using KillerBench.Engine;

internal static class WindowsActivationOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Clear, Repair: null }, "Complete clear activation evidence produces a bounded clear conclusion");
        Require(Invoke(licensing: Check("Windows activation", CheckState.Attention)).State == CheckState.Attention,
            "Current licensing concerns take precedence over history");
        var historical = Invoke(history: Check("Recent Software Protection events", CheckState.Attention));
        Require(historical.State == CheckState.Information && historical.Detail.Contains("narrower historical review", StringComparison.Ordinal),
            "Historical activation warnings remain informational when current licensing is clear");
        Require(Invoke(licensing: Check("Windows activation", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete current licensing evidence fails closed");
        Require(Invoke(history: Check("Recent Software Protection events", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete history evidence fails closed");
        Require(Invoke(licensing: Check("Unexpected", CheckState.Clear)).State == CheckState.Unknown,
            "Unexpected source identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Activation correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows activation correlation separates current license state from sanitized history without selecting activation or license changes.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? licensing = null, WorkflowCheck? history = null,
        CancellationToken token = default) => WindowsActivationOperationalCorrelation.Check(
            licensing ?? Check("Windows activation", CheckState.Clear),
            history ?? Check("Recent Software Protection events", CheckState.Information), token);

    private static WorkflowCheck Check(string name, CheckState state) => new(name, state, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
