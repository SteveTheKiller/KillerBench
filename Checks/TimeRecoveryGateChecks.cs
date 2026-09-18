using KillerBench.Engine;

internal static class TimeRecoveryGateChecks
{
    private const string Synchronized = "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0";
    private const string Unsynchronized = "Leap Indicator: 3(not synchronized)\nStratum: 0\nSource: Local CMOS Clock\nLast Sync Error: 5";

    internal static async Task RunAsync()
    {
        var eligible = TimeRecovery.Interpret(new ToolResult(0, Unsynchronized, "", false));
        var clear = TimeRecovery.Interpret(new ToolResult(0, Synchronized, "", false));
        Require(TimeRecovery.IsResyncEligible(eligible) && !TimeRecovery.IsResyncEligible(clear),
            "Only exact freshly interpreted unsynchronized evidence authorizes resync");
        Require(!TimeRecovery.IsResyncEligible(eligible with
        {
            Evidence = new ToolResult(0, Synchronized, "", false)
        }), "Caller labels cannot override contradictory Windows Time evidence");

        var eligibleHost = new Host(eligible);
        var eligibleResult = await TimeRecovery.ExecuteAsync(TimeRecovery.Step(false), eligibleHost);
        Require(eligibleResult.ExitCode == 0 && eligibleHost.Inspections == 1 && eligibleHost.Resyncs == 1,
            "Fresh exact unsynchronized evidence dispatches one bounded resync");

        var changedHost = new Host(clear);
        try
        {
            await TimeRecovery.ExecuteAsync(TimeRecovery.Step(false), changedHost);
            throw new Exception("A synchronized state returned fabricated resync evidence");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.ExitCode == 0 && changedHost.Inspections == 1 && changedHost.Resyncs == 0,
                "A state change to synchronized skips resync with retained fresh evidence");
        }

        var unavailable = TimeRecovery.Interpret(new ToolResult(5, "", "provider failed", false));
        var unavailableHost = new Host(unavailable);
        try
        {
            await TimeRecovery.ExecuteAsync(TimeRecovery.Step(false), unavailableHost);
            throw new Exception("Unavailable Windows Time evidence returned command evidence");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(unavailableHost.Inspections == 1 && unavailableHost.Resyncs == 0 &&
                error.Message.Contains("deferred", StringComparison.OrdinalIgnoreCase),
                "Unavailable fresh Windows Time evidence defers resync without dispatch");
        }

        var verifyHost = new Host(eligible, new ToolResult(0, Synchronized, "", false));
        await TimeRecovery.ExecuteAsync(TimeRecovery.Step(true), verifyHost);
        Require(verifyHost.Inspections == 0 && verifyHost.Queries == 1 && verifyHost.Resyncs == 0,
            "Windows Time verification dispatches only the exact read-only status command");
        try
        {
            await TimeRecovery.ExecuteAsync(new RepairStep(TimeRecovery.Tool, ["/config"], false), eligibleHost);
            throw new Exception("Unsupported Windows Time command was accepted");
        }
        catch (ArgumentException)
        {
            Require(eligibleHost.Resyncs == 1, "Unsupported Windows Time command is rejected before provider dispatch");
        }

        Console.WriteLine("PASS: Windows Time resync requires a fresh exact unsynchronized recheck immediately before simulated dispatch.");
    }

    private sealed class Host(WorkflowCheck inspection, ToolResult? commandResult = null) : ITimeRecoveryHost
    {
        public int Inspections { get; private set; }
        public int Queries { get; private set; }
        public int Resyncs { get; private set; }

        public Task<WorkflowCheck> InspectAsync(IProgress<string>? progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Inspections++;
            return Task.FromResult(inspection);
        }

        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (step.Verification) Queries++;
            else if (step.Tool == TimeRecovery.Tool) Resyncs++;
            return Task.FromResult(commandResult ?? new ToolResult(0, "The command completed successfully.", "", false));
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
