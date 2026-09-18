using KillerBench.Engine;

internal static class TimeOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("clock accuracy remain independently unverified", StringComparison.Ordinal),
            "Complete Windows Time evidence remains informational without claiming current accuracy");

        var attention = Check(configuration: CheckState.Attention, history: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("fresh synchronization-status check", StringComparison.Ordinal),
            "Configuration and service-history attention requests fresh status without selecting repair");

        var incomplete = Check(events: CheckState.Unknown, history: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete time event evidence takes precedence over service-history attention");

        Require(TimeOperationalCorrelation.Check(Info("Wrong configuration"), Info("Recent time-service events"),
            Info("Windows Time service failure history")).State == CheckState.Unknown,
            "Unexpected Windows Time evidence identities fail closed");

        try
        {
            TimeOperationalCorrelation.Check(Info("Time source configuration"), Info("Recent time-service events"),
                Info("Windows Time service failure history"), new CancellationToken(true));
            throw new Exception("Windows Time operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: Windows Time configuration and exact operational histories correlate deterministically without selecting repair.");
    }

    private static WorkflowCheck Check(CheckState configuration = CheckState.Information,
        CheckState events = CheckState.Information, CheckState history = CheckState.Information) =>
        TimeOperationalCorrelation.Check(new("Time source configuration", configuration, "fixture"),
            new("Recent time-service events", events, "fixture"),
            new("Windows Time service failure history", history, "fixture"));

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
