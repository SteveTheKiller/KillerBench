using KillerBench.Engine;

internal static class StartupOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = StartupOperationalCorrelation.Check(Evidence());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("inventory completed") &&
            clear.Detail.Contains("does not collect commands"), "Complete startup evidence produces a bounded informational conclusion");

        var attention = StartupOperationalCorrelation.Check(Evidence("Startup scheduled tasks", CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("Evidence needing review: 1"),
            "Startup attention remains review-only");

        var incomplete = StartupOperationalCorrelation.Check(Evidence("Startup approval state", CheckState.Unknown));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("incomplete sources: 1"),
            "Incomplete startup evidence takes precedence");

        Require(StartupOperationalCorrelation.Check([.. Evidence().Skip(1)]).State == CheckState.Unknown &&
            StartupOperationalCorrelation.Check([.. Evidence(), Evidence()[0]]).State == CheckState.Unknown,
            "Missing and duplicate startup identities fail closed");

        try { StartupOperationalCorrelation.Check(Evidence(), new CancellationToken(true)); throw new Exception("Startup correlation cancellation ignored"); }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: startup environment correlation combines registrations, approval state and task triggers without selecting changes.");
    }

    private static WorkflowCheck[] Evidence(string? changed = null, CheckState state = CheckState.Information) =>
    [
        new("Startup applications", changed == "Startup applications" ? state : CheckState.Information, "fixture"),
        new("Startup approval state", changed == "Startup approval state" ? state : CheckState.Information, "fixture"),
        new("Startup scheduled tasks", changed == "Startup scheduled tasks" ? state : CheckState.Information, "fixture")
    ];

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
