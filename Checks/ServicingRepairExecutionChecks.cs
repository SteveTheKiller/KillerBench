using System.IO;
using KillerBench.Engine;

internal static class ServicingRepairExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string folder = Path.Combine(scratch, "servicing-gate");
        var eligible = new Host(new("DISM component store", CheckState.Repairable, "Confirmed repairable damage.",
            RepairKind.ComponentStore, new ToolResult(0, "The component store is repairable", "", false)));
        Require(ServicingRepairExecution.IsEligible(RepairKind.ComponentStore, eligible.Eligibility) &&
            !ServicingRepairExecution.IsEligible(RepairKind.SystemFiles, eligible.Eligibility) &&
            !ServicingRepairExecution.IsEligible(RepairKind.ComponentStore, null),
            "The public eligibility check requires exact complete evidence for the selected servicing action");
        var executed = await ServicingRepairExecution.RunAsync(RepairKind.ComponentStore, folder, eligible);
        Require(executed.Executed && executed.Eligibility.Repair == RepairKind.ComponentStore &&
            executed.Repair?.Kind == RepairKind.ComponentStore && eligible.Checks == 1 && eligible.Repairs == 1,
            "Exact fresh servicing eligibility dispatches the selected repair once");

        var blocked = new Host(new("DISM component store", CheckState.Attention,
            "Windows Update is blocked and no alternate source is configured.", RepairKind.ComponentStore));
        var rejected = await ServicingRepairExecution.RunAsync(RepairKind.ComponentStore, folder, blocked);
        Require(!rejected.Executed && rejected.Eligibility.Repair is null && rejected.GetExitCode() == 3 &&
            blocked.Checks == 1 && blocked.Repairs == 0,
            "Blocked source evidence cannot dispatch DISM even when a provider proposed a repair");

        var unrecoverable = new Host(Workflows.Interpret(RepairKind.ComponentStore,
            new ToolResult(0, "The component store cannot be repaired.", "", false)));
        var unrecoverableResult = await ServicingRepairExecution.RunAsync(RepairKind.ComponentStore, folder, unrecoverable);
        Require(!unrecoverableResult.Executed && unrecoverableResult.Eligibility.State == CheckState.Attention &&
            unrecoverableResult.Eligibility.Repair is null && unrecoverable.Checks == 1 && unrecoverable.Repairs == 0,
            "An unrecoverable component store cannot dispatch RestoreHealth");

        var incomplete = new Host(new("SFC system files", CheckState.Repairable, "Fixture", RepairKind.SystemFiles,
            new ToolResult(0, "partial", "", false) { Truncated = true }));
        var incompleteResult = await ServicingRepairExecution.RunAsync(RepairKind.SystemFiles, folder, incomplete);
        Require(!incompleteResult.Executed && incomplete.Repairs == 0,
            "Incomplete effective evidence cannot dispatch SFC");

        foreach (var inconsistent in new[]
        {
            new Host(new("DISM component store", CheckState.Repairable, "Unrecognized output.", RepairKind.ComponentStore,
                new ToolResult(0, "The operation completed successfully.", "", false))),
            new Host(new("SFC system files", CheckState.Repairable, "Contradictory output.", RepairKind.SystemFiles,
                new ToolResult(0, "Windows Resource Protection found integrity violations. Windows Resource Protection did not find any integrity violations.", "", false)))
        })
        {
            var inconsistentResult = await ServicingRepairExecution.RunAsync(inconsistent.Eligibility.Repair!.Value, folder, inconsistent);
            Require(!inconsistentResult.Executed && inconsistentResult.Eligibility.State == CheckState.Unknown &&
                inconsistentResult.Eligibility.Repair is null && inconsistent.Repairs == 0,
                "A repairable label cannot override unrecognized or contradictory raw servicing evidence");
        }

        var missingEvidence = new Host(new("SFC system files", CheckState.Repairable, "Fixture", RepairKind.SystemFiles));
        var missingEvidenceResult = await ServicingRepairExecution.RunAsync(RepairKind.SystemFiles, folder, missingEvidence);
        Require(!missingEvidenceResult.Executed && missingEvidenceResult.Eligibility.Repair is null && missingEvidence.Repairs == 0,
            "A repairable claim without raw evidence cannot dispatch SFC");

        foreach (var evidence in new[]
        {
            new ToolResult(5, "", "", false),
            new ToolResult(0, "", "provider error", false),
            new ToolResult(0, "", "", true)
        })
        {
            var unavailable = new Host(new("DISM component store", CheckState.Repairable, "Fixture",
                RepairKind.ComponentStore, evidence));
            var unavailableResult = await ServicingRepairExecution.RunAsync(RepairKind.ComponentStore, folder, unavailable);
            Require(!unavailableResult.Executed && unavailableResult.Eligibility.Repair is null && unavailable.Repairs == 0,
                "Failed, error-bearing or timed-out evidence cannot dispatch DISM");
        }

        var wrongAction = new Host(new("DISM component store", CheckState.Repairable, "Fixture", RepairKind.SystemFiles,
            new ToolResult(0, "The component store is repairable", "", false)));
        var wrongActionResult = await ServicingRepairExecution.RunAsync(RepairKind.ComponentStore, folder, wrongAction);
        Require(!wrongActionResult.Executed && wrongActionResult.Eligibility.State == CheckState.Unknown &&
            wrongActionResult.Eligibility.Repair is null && wrongAction.Repairs == 0,
            "A repair proposal for another action cannot dispatch servicing");

        var wrongResult = new Host(new("DISM component store", CheckState.Repairable, "Fixture", RepairKind.ComponentStore,
            new ToolResult(0, "The component store is repairable", "", false)))
        { ResultKind = RepairKind.SystemFiles };
        try
        {
            await ServicingRepairExecution.RunAsync(RepairKind.ComponentStore, folder, wrongResult);
            throw new Exception("A mismatched servicing result was accepted");
        }
        catch (InvalidDataException) { }

        var canceled = new Host(new("SFC system files", CheckState.Repairable, "Fixture", RepairKind.SystemFiles,
            new ToolResult(0, "Windows Resource Protection found integrity violations", "", false)));
        try
        {
            await ServicingRepairExecution.RunAsync(RepairKind.SystemFiles, folder, canceled, token: new CancellationToken(true));
            throw new Exception("Canceled servicing eligibility was evaluated");
        }
        catch (OperationCanceledException) { }
        Require(canceled.Checks == 0 && canceled.Repairs == 0, "Cancellation prevents servicing inspection and repair");

        try
        {
            await ServicingRepairExecution.RunAsync(RepairKind.PrintSpooler, folder, eligible);
            throw new Exception("An unrelated repair entered the servicing gate");
        }
        catch (ArgumentOutOfRangeException) { }

        var dispatchEligible = new CommandHost(new("SFC system files", CheckState.Repairable,
            "Confirmed integrity violations.", RepairKind.SystemFiles,
            new ToolResult(0, "Windows Resource Protection found integrity violations", "", false)));
        var dispatchResult = await ServicingRepairExecution.RunCommandAsync(RepairKind.SystemFiles,
            Repairs.Plan(RepairKind.SystemFiles).Steps.Single(step => !step.Verification), dispatchEligible);
        Require(dispatchResult.ExitCode == 0 && dispatchEligible.Checks == 1 && dispatchEligible.Commands == 1,
            "Exact fresh SFC eligibility is repeated immediately before command dispatch");

        var changed = new CommandHost(new("DISM component store", CheckState.Attention,
            "Managed repair-source policy no longer permits RestoreHealth.", Evidence:
            new ToolResult(0, "The component store is repairable", "", false)));
        try
        {
            await ServicingRepairExecution.RunCommandAsync(RepairKind.ComponentStore,
                Repairs.Plan(RepairKind.ComponentStore).Steps.Single(step => !step.Verification), changed);
            throw new Exception("Changed DISM repair-source eligibility reached the native command");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(error.Message.Contains("no longer authorizes") && changed.Checks == 1 && changed.Commands == 0,
                "Changed DISM repair-source eligibility blocks the native command at dispatch time");
        }

        var inconsistentDispatch = new CommandHost(new("SFC system files", CheckState.Repairable,
            "Claimed integrity violations.", RepairKind.SystemFiles,
            new ToolResult(0, "The operation completed successfully.", "", false)));
        try
        {
            await ServicingRepairExecution.RunCommandAsync(RepairKind.SystemFiles,
                Repairs.Plan(RepairKind.SystemFiles).Steps.Single(step => !step.Verification), inconsistentDispatch);
            throw new Exception("Unrecognized raw SFC evidence reached the native command");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(error.Message.Contains("not recognized", StringComparison.OrdinalIgnoreCase) &&
                inconsistentDispatch.Checks == 1 && inconsistentDispatch.Commands == 0,
                "The final dispatch gate reports independently interpreted raw servicing evidence");
        }

        var nowClear = new CommandHost(new("SFC system files", CheckState.Clear,
            "Windows Resource Protection did not find any integrity violations.", Evidence:
            new ToolResult(0, "Windows Resource Protection did not find any integrity violations", "", false)));
        try
        {
            await ServicingRepairExecution.RunCommandAsync(RepairKind.SystemFiles,
                Repairs.Plan(RepairKind.SystemFiles).Steps.Single(step => !step.Verification), nowClear);
            throw new Exception("A clear SFC result reached the native command");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.Output.Contains("did not find") && nowClear.Checks == 1 && nowClear.Commands == 0,
                "A repair that became unnecessary is skipped with retained evidence at the final SFC dispatch boundary");
        }

        try
        {
            await ServicingRepairExecution.RunCommandAsync(RepairKind.SystemFiles,
                Repairs.Plan(RepairKind.SystemFiles).Steps.Single(step => step.Verification), dispatchEligible);
            throw new Exception("A servicing verification step entered the mutation gate");
        }
        catch (ArgumentException) { }

        Console.WriteLine("PASS: direct DISM and SFC execution requires fresh exact servicing eligibility before dispatch; simulated providers only.");
    }

    private sealed class Host(WorkflowCheck eligibility) : IWorkflowHost
    {
        internal WorkflowCheck Eligibility => eligibility;
        internal int Checks;
        internal int Repairs;
        internal RepairKind ResultKind = eligibility.Repair ?? RepairKind.ComponentStore;

        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            throw new NotSupportedException();

        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
        {
            Checks++;
            return Task.FromResult(eligibility);
        }

        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        {
            Repairs++;
            return Task.FromResult(new RepairResult("fixture", ResultKind, DateTimeOffset.UtcNow,
                "Fixture verification", "fixture.json", []));
        }
    }

    private sealed class CommandHost(WorkflowCheck eligibility) : IServicingRepairCommandHost
    {
        internal int Checks;
        internal int Commands;

        public Task<WorkflowCheck> CheckAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
        {
            Checks++;
            return Task.FromResult(eligibility);
        }

        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token)
        {
            Commands++;
            return Task.FromResult(new ToolResult(0, "Fixture command completed", "", false));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
