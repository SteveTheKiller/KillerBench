using KillerBench.Engine;

internal static class ComponentCleanupChecks
{
    private const string Yes = "Component Store Cleanup Recommended : Yes\r\nThe operation completed successfully.";
    private const string No = "Component Store Cleanup Recommended : No\r\nThe operation completed successfully.";
    internal static async Task RunAsync(string folder)
    {
        if (ComponentCleanup.Interpret(new(0, Yes, "", false)).Repair != RepairKind.ComponentCleanup ||
            ComponentCleanup.Interpret(new(0, No, "", false)).State != CheckState.Clear) throw new Exception("Cleanup recommendation classification failed.");
        foreach (var invalid in new[] { new ToolResult(5, Yes, "", false), new ToolResult(0, Yes, "", true),
            new ToolResult(0, Yes, "", false) { TimedOut = true }, new ToolResult(0, Yes + "\n" + No, "", false),
            new ToolResult(0, "Component Store Cleanup Recommended : Yes", "", false) })
            if (ComponentCleanup.Interpret(invalid).State != CheckState.Unknown) throw new Exception("Incomplete cleanup evidence authorized maintenance.");
        var eligible = ComponentCleanup.Interpret(new ToolResult(0, Yes, "", false));
        var clear = ComponentCleanup.Interpret(new ToolResult(0, No, "", false));
        if (!ComponentCleanup.IsEligible(eligible) || ComponentCleanup.IsEligible(clear) || ComponentCleanup.IsEligible(eligible with
            { Evidence = new ToolResult(0, No, "", false) }))
            throw new Exception("Cleanup eligibility did not independently validate exact recommendation evidence.");

        var eligibleGate = new GateFixture(eligible);
        var eligibleResult = await ComponentCleanup.ExecuteAsync(ComponentCleanup.Step(false), eligibleGate);
        if (eligibleResult.ExitCode != 0 || eligibleGate.Analyses != 1 || eligibleGate.Cleanups != 1)
            throw new Exception("Fresh exact cleanup recommendation did not dispatch one bounded cleanup.");
        var changedGate = new GateFixture(clear);
        try
        {
            await ComponentCleanup.ExecuteAsync(ComponentCleanup.Step(false), changedGate);
            throw new Exception("Changed cleanup recommendation reached maintenance.");
        }
        catch (RepairStepSkippedException skipped)
        {
            if (!skipped.Evidence.Output.Contains("Recommended : No") || changedGate.Analyses != 1 || changedGate.Cleanups != 0)
                throw new Exception("Changed cleanup recommendation did not skip maintenance with retained evidence.");
        }
        var unknownGate = new GateFixture(ComponentCleanup.Interpret(new ToolResult(5, "", "provider failed", false)));
        try
        {
            await ComponentCleanup.ExecuteAsync(ComponentCleanup.Step(false), unknownGate);
            throw new Exception("Unavailable cleanup evidence reached maintenance.");
        }
        catch (RepairDispatchDeferredException error)
        {
            if (unknownGate.Analyses != 1 || unknownGate.Cleanups != 0 ||
                !error.Message.Contains("deferred", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unavailable cleanup evidence did not defer maintenance.");
        }
        var verifyGate = new GateFixture(eligible, new ToolResult(0, No, "", false));
        await ComponentCleanup.ExecuteAsync(ComponentCleanup.Step(true), verifyGate);
        if (verifyGate.Analyses != 1 || verifyGate.Cleanups != 0)
            throw new Exception("Cleanup verification dispatched a mutation.");
        try
        {
            await ComponentCleanup.ExecuteAsync(new RepairStep("dism.exe", ["/Online", "/Cleanup-Image", "/ResetBase"], false), eligibleGate);
            throw new Exception("Unsupported cleanup command was accepted.");
        }
        catch (ArgumentException) { }
        foreach (string initial in new[] { Yes, No, "unrecognized" })
        {
            var host = new Fixture(initial);
            var result = await RecipeEngine.RunAsync(RecipeCatalog.ForWorkflow(WorkflowKind.ComponentCleanup), true, folder, host);
            if (host.Calls != (initial == Yes ? 1 : 0) || (initial == Yes && result[0].Outcome != RecipeOutcome.Repaired))
                throw new Exception("Cleanup recipe ignored the current recommendation.");
        }
        if (Repairs.Plan(RepairKind.ComponentCleanup).Steps.Any(s => s.Arguments.Contains("/ResetBase"))) throw new Exception("Cleanup must not reset the servicing base.");
        Console.WriteLine("PASS: component cleanup recommendation, rejected evidence and recipe verification (substituted provider only).");
    }
    private sealed class Fixture(string initial) : IWorkflowHost
    {
        internal int Calls;
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) => Task.FromResult<IReadOnlyList<WorkflowCheck>>([]);
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) => Task.FromResult(ComponentCleanup.Interpret(new(0, initial, "", false)));
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(new RepairResult("fixture", kind, DateTimeOffset.UtcNow, "Verification reported", "fixture",
                [.. Repairs.Plan(kind).Steps.Select(s => new RepairStepResult(s, new(0, s.Verification ? No : "The operation completed successfully.", "", false)))]));
        }
    }

    private sealed class GateFixture(WorkflowCheck inspection, ToolResult? commandResult = null) : IComponentCleanupHost
    {
        internal int Analyses;
        internal int Cleanups;

        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (step.Verification)
            {
                Analyses++;
                return Task.FromResult(commandResult ?? inspection.Evidence!);
            }
            Cleanups++;
            return Task.FromResult(commandResult ?? new ToolResult(0, "The operation completed successfully.", "", false));
        }
    }
}
