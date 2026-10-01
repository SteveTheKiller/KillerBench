using KillerBench.Engine;

internal static class ApplicationStabilityCorrelationChecks
{
    public static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("1 Reliability Monitor result") &&
            clear.Detail.Contains("not establish current application health"),
            "Complete application history produces a bounded informational conclusion");

        var history = Check(reliabilityState: CheckState.Attention);
        Require(history is { State: CheckState.Information, Repair: null } && history.Detail.Contains("Historical observations needing review: 1") &&
            history.Detail.Contains("Match the application"), "Historical application failures remain informational");

        var incomplete = Check(applicationState: CheckState.Unknown, reliabilityState: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("incomplete sources: 1"),
            "Incomplete application history takes precedence");

        Require(ApplicationStabilityCorrelation.Check(Info("Wrong name"), [Info("Reliability history")]).State == CheckState.Unknown &&
            ApplicationStabilityCorrelation.Check(Info("Recent application errors"), []).State == CheckState.Unknown &&
            ApplicationStabilityCorrelation.Check(Info("Recent application errors"),
                [.. Enumerable.Range(0, 66).Select(index => Info("Reliability " + index))]).State == CheckState.Unknown,
            "Unexpected, empty and oversized application history inputs fail closed");

        try { ApplicationStabilityCorrelation.Check(Info("Recent application errors"), [Info("Reliability history")], new CancellationToken(true)); throw new Exception("Application stability cancellation ignored"); }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: application stability correlation summarizes bounded history without claiming current health or selecting repair.");
    }

    private static WorkflowCheck Check(CheckState applicationState = CheckState.Information,
        CheckState reliabilityState = CheckState.Information) => ApplicationStabilityCorrelation.Check(
            new("Recent application errors", applicationState, "fixture"),
            [new("Reliability history", reliabilityState, "fixture")]);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
