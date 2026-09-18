using System.IO;
using KillerBench.Engine;

internal static class WindowsRecoveryChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string enabledText = "Windows Recovery Environment (Windows RE) and system reset configuration\r\n\r\n    Windows RE status:         Enabled\r\n    Windows RE location:       \\\\?\\GLOBALROOT\\device\\harddisk0\\partition4\\Recovery\\WindowsRE\r\n    Boot Configuration Data (BCD) identifier: 12345678-1234-1234-1234-123456789abc\r\n\r\nREAGENTC.EXE: Operation Successful.";
        var enabled = WindowsRecoveryDiagnostics.Interpret(new(0, enabledText, "", false));
        Require(enabled.State == CheckState.Clear && enabled.Repair is null && enabled.Detail.Contains("enabled") &&
            enabled.Evidence!.Output.Contains("harddisk0") && WindowsRecoveryDiagnostics.Step().Verification,
            "Enabled recovery evidence is retained without authorizing a configuration change");

        var disabled = WindowsRecoveryDiagnostics.Interpret(new(0, "Windows RE status: Disabled\nREAGENTC.EXE: Operation Successful.", "", false));
        Require(disabled.State == CheckState.Repairable && disabled.Repair == RepairKind.WindowsRecoveryEnvironment && disabled.Detail.Contains("disabled"),
            "Disabled recovery selects only the bounded Windows Recovery enable action");
        Require(WindowsRecoveryEnable.IsEligible(disabled) && !WindowsRecoveryEnable.IsEligible(enabled),
            "Only exact freshly interpreted Disabled evidence authorizes recovery enablement");
        Require(!WindowsRecoveryEnable.IsEligible(disabled with
        {
            Evidence = new ToolResult(0, "Windows RE status: Enabled\nREAGENTC.EXE: Operation Successful.", "", false)
        }), "Caller labels cannot override contradictory recovery evidence");

        var plan = Repairs.Plan(RepairKind.WindowsRecoveryEnvironment);
        Require(plan.Steps.Count == 2 && plan.Steps[0].Tool == "reagentc.exe" && plan.Steps[0].Arguments.SequenceEqual(new[] { "/enable" }) &&
            plan.Steps[1].Tool == "reagentc.exe" && plan.Steps[1].Arguments.SequenceEqual(new[] { "/info" }) && !plan.Steps[0].Verification && plan.Steps[1].Verification &&
            plan.Effects.Contains("No image is downloaded", StringComparison.Ordinal),
            "Recovery repair plan enables the existing configuration and verifies without replacing its image");
        var verified = new RepairResult("Fixture", RepairKind.WindowsRecoveryEnvironment, DateTimeOffset.UtcNow,
            "fixture", "fixture", [new(plan.Steps[0], new(0, "REAGENTC.EXE: Operation Successful.", "", false)),
                new(plan.Steps[1], new(0, enabledText, "", false))]);
        Require(Repairs.IsVerified(verified), "Recovery enablement requires a successful command and exact enabled verification");
        Require(!Repairs.IsVerified(verified with { Steps = [verified.Steps[0], new(plan.Steps[1], new(0,
            "Windows RE status: Disabled\nREAGENTC.EXE: Operation Successful.", "", false))] }),
            "Successful enable exit without enabled verification is unresolved");
        Require(Workflows.SupportsRepair(WorkflowKind.WindowsRecovery) &&
            Workflows.Scopes(WorkflowKind.WindowsRecovery).SequenceEqual(new[] { RepairKind.WindowsRecoveryEnvironment }) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.WindowsRecovery).Single().Id == "windows.recovery-environment",
            "Recovery repair is exposed through the shared workflow recipe catalog");

        var eligibleHost = new EnableHost(disabled);
        var eligibleResult = await WindowsRecoveryEnable.ExecuteAsync(WindowsRecoveryDiagnostics.EnableStep(), eligibleHost);
        Require(eligibleResult.ExitCode == 0 && eligibleHost.Inspections == 1 && eligibleHost.Commands == 1,
            "Fresh exact Disabled evidence dispatches one bounded enable command");

        var changedHost = new EnableHost(enabled);
        try
        {
            await WindowsRecoveryEnable.ExecuteAsync(WindowsRecoveryDiagnostics.EnableStep(), changedHost);
            throw new Exception("Enabled recovery evidence reached the enable command");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.Output.Contains("Enabled") && changedHost.Inspections == 1 && changedHost.Commands == 0,
                "A state change to Enabled completes with retained evidence without repeating enablement");
        }

        var unknownCheck = WindowsRecoveryDiagnostics.Interpret(new ToolResult(1, "", "provider failed", false));
        var unknownHost = new EnableHost(unknownCheck);
        try
        {
            await WindowsRecoveryEnable.ExecuteAsync(WindowsRecoveryDiagnostics.EnableStep(), unknownHost);
            throw new Exception("Unavailable recovery evidence reached the enable command");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(unknownHost.Inspections == 1 && unknownHost.Commands == 0 &&
                error.Message.Contains("deferred", StringComparison.OrdinalIgnoreCase),
                "Unavailable fresh evidence defers enablement without dispatch");
        }

        var verifyHost = new EnableHost(disabled, new ToolResult(0, enabledText, "", false));
        await WindowsRecoveryEnable.ExecuteAsync(WindowsRecoveryDiagnostics.Step(), verifyHost);
        Require(verifyHost.Inspections == 0 && verifyHost.Commands == 1,
            "Verification dispatches only the exact read-only information command");
        try
        {
            await WindowsRecoveryEnable.ExecuteAsync(new RepairStep("reagentc.exe", ["/disable"], false), eligibleHost);
            throw new Exception("Unsupported recovery command was accepted");
        }
        catch (ArgumentException)
        {
            Require(eligibleHost.Commands == 1, "Unsupported recovery command is rejected before provider dispatch");
        }

        var boot = new BootStamp(100, new DateTimeOffset(2026, 9, 14, 1, 0, 0, TimeSpan.Zero));
        string verifiedFolder = Path.Combine(scratch, "windows-recovery-verified");
        var verifiedStore = new RepairRecoveryStore(verifiedFolder, _ => Task.FromResult<BootStamp?>(boot));
        var verifiedRun = await Repairs.RunAsync(RepairKind.WindowsRecoveryEnvironment, verifiedFolder,
            host: new Host(enabledText), recoveryStore: verifiedStore);
        Require(Repairs.IsVerified(verifiedRun) && verifiedRun.Steps.Count == 2 &&
            (await verifiedStore.ReadAsync())?.Pending == false,
            "Verified recovery enablement completes its durable mutation guard");

        string failedFolder = Path.Combine(scratch, "windows-recovery-unverified");
        var failedStore = new RepairRecoveryStore(failedFolder, _ => Task.FromResult<BootStamp?>(boot));
        var failedHost = new Host("Windows RE status: Disabled\nREAGENTC.EXE: Operation Successful.");
        var failedRun = await Repairs.RunAsync(RepairKind.WindowsRecoveryEnvironment, failedFolder,
            host: failedHost, recoveryStore: failedStore);
        Require(!Repairs.IsVerified(failedRun) && failedRun.GetExitCode() == 3 &&
            (await failedStore.ReadAsync())?.Pending == true,
            "Failed recovery verification retains its durable mutation guard");
        var repeatedHost = new Host(enabledText);
        try
        {
            await Repairs.RunAsync(RepairKind.WindowsRecoveryEnvironment, failedFolder,
                host: repeatedHost, recoveryStore: new(failedFolder, _ => Task.FromResult<BootStamp?>(boot)));
            throw new Exception("Unverified recovery enablement was repeated");
        }
        catch (InvalidOperationException)
        {
            Require(repeatedHost.Calls == 0, "Recovery guard blocks a repeated command before dispatch");
        }

        foreach (var result in new[]
        {
            new ToolResult(1, "", "failed", false),
            new ToolResult(0, "Windows RE status: Enabled", "", true),
            new ToolResult(0, "Windows RE status: Enabled", "", false) { Truncated = true },
            new ToolResult(0, "Etat Windows RE: Active", "", false),
            new ToolResult(0, "Windows RE status: Enabled\nWindows RE status: Disabled", "", false),
            new ToolResult(0, "Windows RE status: Maybe", "", false)
        })
        {
            var unknown = WindowsRecoveryDiagnostics.Interpret(result);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null, "Incomplete or unrecognized recovery evidence cannot pass or authorize repair");
        }
        Console.WriteLine("PASS: Windows Recovery Environment evidence, guarded enable plan and exact verification; simulated repair only.");
    }

    private sealed class Host(string verificationOutput) : IRepairHost
    {
        public int Calls { get; private set; }
        public void CheckPrerequisites() { }
        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress)
        {
            Calls++;
            return Task.FromResult(step.Verification
                ? new ToolResult(0, verificationOutput, "", false)
                : new ToolResult(0, "REAGENTC.EXE: Operation Successful.", "", false));
        }
    }

    private sealed class EnableHost(WorkflowCheck inspection, ToolResult? commandResult = null) : IWindowsRecoveryEnableHost
    {
        public int Inspections { get; private set; }
        public int Commands { get; private set; }

        public Task<WorkflowCheck> InspectAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Inspections++;
            return Task.FromResult(inspection);
        }

        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Commands++;
            return Task.FromResult(commandResult ?? new ToolResult(0, "REAGENTC.EXE: Operation Successful.", "", false));
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
