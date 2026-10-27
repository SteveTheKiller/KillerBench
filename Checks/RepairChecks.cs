using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class RepairChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string folder = Path.Combine(scratch, "repair-journals");
        Require(typeof(Repairs).GetMethod(nameof(Repairs.RunAsync), [typeof(RepairKind), typeof(string), typeof(IProgress<string>),
            typeof(IRepairHost), typeof(RepairRecoveryStore), typeof(IReadOnlyList<string>), typeof(CancellationToken)]) is not null,
            "Original compiled repair entry point remains available");
        var brokenDisplay = new ThrowingProgress();
        var displayedHost = new FakeHost();
        var displayedResult = await Repairs.RunAsync(RepairKind.ComponentStore, folder, brokenDisplay, host: displayedHost);
        Require(displayedResult.GetExitCode() == 0 && displayedHost.Calls == 2 && brokenDisplay.Calls >= 6,
            "Broken display observers cannot interrupt command dispatch, verification or completion");
        using (var displayedJournal = JsonDocument.Parse(await File.ReadAllTextAsync(displayedResult.JournalPath)))
            Require(displayedJournal.RootElement.GetProperty("status").GetString() == displayedResult.Status &&
                displayedJournal.RootElement.GetProperty("steps").GetArrayLength() == 2,
                "Repair evidence is durable even when every progress callback fails");
        foreach (string mode in new[] { "stalled", "missing-task", "missing-result", "caller-stop", "provider-stop" })
        {
            using var callerStop = new CancellationTokenSource();
            var unfinished = new TaskCompletionSource<ToolResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new FakeHost { Execute = () =>
            {
                if (mode == "caller-stop") { callerStop.Cancel(); return unfinished.Task; }
                if (mode == "provider-stop") return Task.FromCanceled<ToolResult>(new CancellationToken(true));
                return mode == "stalled" ? unfinished.Task : mode == "missing-task" ? null! : Task.FromResult<ToolResult>(null!);
            } };
            var pendingStore = new RepairRecoveryStore(Path.Combine(scratch, "provider-" + mode),
                _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var incomplete = await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: provider,
                recoveryStore: pendingStore, cancellationToken: callerStop.Token,
                commandTimeout: TimeSpan.FromMilliseconds(20)).WaitAsync(TimeSpan.FromSeconds(3));
            Require(elapsed.Elapsed < TimeSpan.FromSeconds(2) && provider.Calls == 1 && incomplete.GetExitCode() == (mode == "caller-stop" ? 130 : 3) &&
                incomplete.Steps.Count == 0 && (await pendingStore.ReadAsync())?.Pending == true,
                "Incomplete repair provider stops promptly and retains pending recovery without fabricated evidence");
            string journalBefore = await File.ReadAllTextAsync(incomplete.JournalPath);
            unfinished.TrySetResult(new(0, "late completion", "", false));
            Require(await File.ReadAllTextAsync(incomplete.JournalPath) == journalBefore,
                "Late provider completion cannot replace the incomplete journal");
            var retry = new FakeHost();
            try { await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: retry, recoveryStore: pendingStore); throw new Exception("Unsafe provider retry"); }
            catch (InvalidOperationException) { }
            Require(retry.Calls == 0, "Incomplete provider recovery blocks same-boot retry");
        }
        var canceledNative = new WindowsRepairHost(new CancellationToken(true));
        foreach (var step in Enum.GetValues<RepairKind>().SelectMany(kind => Repairs.Plan(kind).Steps))
        {
            try { await canceledNative.RunAsync(step, null); throw new Exception("Canceled native host dispatched a command"); }
            catch (OperationCanceledException) { }
        }
        try { canceledNative.CheckPrerequisites(); throw new Exception("Canceled native host inspected prerequisites"); }
        catch (OperationCanceledException) { }
        var stoppedWorkstation = new ToolResult(0, "SERVICE_NAME: LanmanWorkstation\nSTATE : 1 STOPPED", "", false);
        Require(ServiceRecovery.Interpret(RepairKind.Workstation, stoppedWorkstation, 4).Repair is null &&
            ServiceRecovery.Interpret(RepairKind.Workstation, stoppedWorkstation, 2).Repair == RepairKind.Workstation,
            "SMB client recovery preserves disabled configuration and recognizes an enabled stopped service");
        Require(RecipeCatalog.ForWorkflow(WorkflowKind.Network).Any(recipe => recipe.Id == "network.workstation") &&
            Repairs.Plan(RepairKind.Workstation).Effects.Contains("signing, encryption and guest-access policy"),
            "SMB client recipe is reachable and describes preserved security settings");
        Require(ServiceRecovery.ServiceName(RepairKind.NetworkLocationAwareness) == "NlaSvc" &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Network).Any(recipe => recipe.Id == "network.location-awareness") &&
            Repairs.Plan(RepairKind.NetworkLocationAwareness).Effects.Contains("different existing firewall profile"),
            "Network Location Awareness recovery is reachable and describes its network-category effect");
        Require(ServiceRecovery.ServiceName(RepairKind.NetworkListService) == "netprofm" &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Network).Single(recipe => recipe.Id == "network.list-service").Dependencies.SequenceEqual(["network.location-awareness"]) &&
            Repairs.Plan(RepairKind.NetworkListService).Effects.Contains("different existing firewall profile"),
            "Network List Service recovery requires Network Location Awareness and describes its network-category effect");
        foreach (var kind in Enum.GetValues<RepairKind>().Where(k => ServiceRecovery.ServiceName(k) is not null))
        {
            var service = new FakeHost { Output = "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\nSTATE : 4 RUNNING" };
            var started = await Repairs.RunAsync(kind, folder, host: service);
            Require(service.Calls == 2 && started.Status.StartsWith("Verification reported service running"), "Service start requires running-state verification");
            Require(service.Steps.All(s => s.Arguments[1] == ServiceRecovery.ServiceName(kind)), "Service action remains scoped to selected target");
            var pending = new FakeHost { Output = "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\nSTATE : 2 START_PENDING" };
            Require(!(await Repairs.RunAsync(kind, folder, host: pending)).Status.StartsWith("Verification reported"), "Start pending is not a successful verification");
        }
        var alreadyRunningStore = new RepairRecoveryStore(Path.Combine(scratch, "service-already-running-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        int alreadyRunningCall = 0;
        var alreadyRunningHost = new FakeHost
        {
            Execute = () => ++alreadyRunningCall == 1
                ? Task.FromException<ToolResult>(new RepairStepSkippedException("Service already running.",
                    new ToolResult(0, "SERVICE_NAME: LanmanWorkstation\nSTATE : 4 RUNNING", "", false)))
                : Task.FromResult(new ToolResult(0, "SERVICE_NAME: LanmanWorkstation\nSTATE : 4 RUNNING", "", false))
        };
        var alreadyRunning = await Repairs.RunAsync(RepairKind.Workstation, folder, host: alreadyRunningHost,
            recoveryStore: alreadyRunningStore);
        Require(alreadyRunningHost.Calls == 2 && alreadyRunning.Steps.Count == 2 &&
            !alreadyRunning.Steps[0].CommandDispatched && alreadyRunning.Steps[1].CommandDispatched &&
            alreadyRunning.GetExitCode() == 3 && (await alreadyRunningStore.ReadAsync()) is { Pending: false },
            "An already-running service records no start dispatch, closes the prepared guard and cannot claim a repair");
        var host = new FakeHost();
        var partialHost = new FakeHost { TruncateFirst = true };
        var partialResult = await Repairs.RunAsync(RepairKind.ComponentStore, folder, host: partialHost);
        Require(partialHost.Calls == 2 && partialResult.Status.StartsWith("Verification incomplete") && partialResult.GetExitCode() == 3,
            "Truncated command output remains incomplete even after the verification command succeeds");
        var result = await Repairs.RunAsync(RepairKind.ComponentStore, folder, host: host);
        Require(host.Calls == 2 && host.Steps[1].Verification && result.Status.StartsWith("Verification reported"), "Repair followed by verification");
        Require(result.GetExitCode() == 0 && result.GetExitCode(canceled: true) == 130, "Verified repair succeeds and cancellation takes precedence");
        Require((result with { Steps = [] }).GetExitCode() == 3, "Success text without verification evidence cannot return success");
        for (int index = 0; index < result.Steps.Count; index++)
        {
            var partialSteps = result.Steps.ToArray();
            partialSteps[index] = partialSteps[index] with { Result = partialSteps[index].Result with { Truncated = true } };
            Require(!Repairs.IsVerified(result with { Steps = partialSteps }) && (result with { Steps = partialSteps }).GetExitCode() == 3,
                "Truncated evidence from any step prevents verified success");
        }
        Require(!Repairs.IsVerified(result with { Kind = (RepairKind)int.MaxValue }), "Unknown repair identity is not verified");
        foreach (var incomplete in new[] { result with { Steps = null! }, result with { Steps = [null!] },
            result with { Steps = [new(result.Steps[0].Step, null!)] }, result with { Status = null!, Steps = [] } })
        {
            Require(!incomplete.RequiresRestart && incomplete.GetExitCode() == 3 && incomplete.GetExitCode(canceled: true) == 130,
                "Incomplete imported repair results remain unverified without throwing during exit classification");
        }
        Require((result with { Steps = [new(Repairs.Plan(RepairKind.ComponentStore).Steps[0], new(3010, "", "", false))] }).GetExitCode() == 4,
            "Restart evidence has a distinct exit code");
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(result.JournalPath));
        Require(json.RootElement.GetProperty("steps").GetArrayLength() == 2, "Journal retains both command results");
        var denied = new FakeHost { Denied = true };
        try { await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: denied); throw new Exception("Missing preflight"); }
        catch (UnauthorizedAccessException) { }
        Require(denied.Calls == 0, "Preflight prevents execution");
        var deferredStore = new RepairRecoveryStore(Path.Combine(scratch, "deferred-servicing-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        var deferred = new FakeHost
        {
            Execute = () => Task.FromException<ToolResult>(new RepairDispatchDeferredException("Fresh evidence became clear."))
        };
        var deferredResult = await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: deferred,
            recoveryStore: deferredStore);
        Require(deferredResult.Steps.Count == 0 && deferredResult.GetExitCode() == 3 &&
            deferredResult.Status.StartsWith("Repair skipped before command dispatch") &&
            (await deferredStore.ReadAsync()) is { Pending: false },
            "A final eligibility change clears the prepared recovery guard without claiming a mutation");
        var compoundStore = new RepairRecoveryStore(Path.Combine(scratch, "compound-repair-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        int compoundCall = 0;
        var compoundHost = new FakeHost
        {
            Execute = () => Task.FromResult(++compoundCall == 1
                ? new ToolResult(0, "SERVICE_NAME: W32Time\nSTATE : 4 RUNNING", "", false)
                : new ToolResult(0, "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0", "", false))
        };
        var compound = await Repairs.RunAsync(RepairKind.TimeSynchronization, folder, host: compoundHost,
            recoveryStore: compoundStore);
        Require(compoundHost.Calls == 3 && compound.Steps.Count == 3 && compound.GetExitCode() == 0 &&
            compound.Steps.Take(2).All(step => step.CommandDispatched) &&
            (await compoundStore.ReadAsync()) is { Pending: false },
            "A compound repair retains one recovery guard across multiple dispatched mutations and verification");
        var skippedCompoundStore = new RepairRecoveryStore(Path.Combine(scratch, "compound-skip-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        int skippedCall = 0;
        var skippedCompoundHost = new FakeHost
        {
            Execute = () => ++skippedCall == 1
                ? Task.FromException<ToolResult>(new RepairStepSkippedException("Service already running.",
                    new ToolResult(0, "SERVICE_NAME: W32Time\nSTATE : 4 RUNNING", "", false)))
                : Task.FromResult(new ToolResult(0, "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0", "", false))
        };
        var skippedCompound = await Repairs.RunAsync(RepairKind.TimeSynchronization, folder, host: skippedCompoundHost,
            recoveryStore: skippedCompoundStore);
        Require(skippedCompoundHost.Calls == 3 && skippedCompound.Steps.Count == 3 && skippedCompound.GetExitCode() == 0 &&
            !skippedCompound.Steps[0].CommandDispatched && skippedCompound.Steps[1].CommandDispatched &&
            (await skippedCompoundStore.ReadAsync()) is { Pending: false },
            "A safely skipped first step is journaled without blocking the later mutation or verification");
        var lateSkipStore = new RepairRecoveryStore(Path.Combine(scratch, "compound-late-skip-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        int lateSkipCall = 0;
        var lateSkipHost = new FakeHost
        {
            Execute = () => ++lateSkipCall == 2
                ? Task.FromException<ToolResult>(new RepairStepSkippedException("Synchronization became current.",
                    new ToolResult(0, "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0", "", false)))
                : Task.FromResult(lateSkipCall == 1
                    ? new ToolResult(0, "SERVICE_NAME: W32Time\nSTATE : 4 RUNNING", "", false)
                    : new ToolResult(0, "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0", "", false))
        };
        var lateSkip = await Repairs.RunAsync(RepairKind.TimeSynchronization, folder, host: lateSkipHost,
            recoveryStore: lateSkipStore);
        Require(lateSkipHost.Calls == 3 && lateSkip.Steps.Count == 3 && lateSkip.GetExitCode() == 0 &&
            lateSkip.Steps[0].CommandDispatched && !lateSkip.Steps[1].CommandDispatched &&
            (await lateSkipStore.ReadAsync()) is { Pending: false },
            "Fresh clear evidence can skip the second mutation while later verification closes the existing guard");
        var compoundDeferredStore = new RepairRecoveryStore(Path.Combine(scratch, "compound-deferred-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        int compoundDeferredCall = 0;
        var compoundDeferredHost = new FakeHost
        {
            Execute = () => ++compoundDeferredCall == 2
                ? Task.FromException<ToolResult>(new RepairDispatchDeferredException("Fresh synchronization evidence is unavailable."))
                : Task.FromResult(new ToolResult(0, "SERVICE_NAME: W32Time\nSTATE : 4 RUNNING", "", false))
        };
        var compoundDeferred = await Repairs.RunAsync(RepairKind.TimeSynchronization, folder, host: compoundDeferredHost,
            recoveryStore: compoundDeferredStore);
        Require(compoundDeferredHost.Calls == 2 && compoundDeferred.Steps.Count == 1 && compoundDeferred.GetExitCode() == 3 &&
            compoundDeferred.Status.StartsWith("Repair stopped before the next command") &&
            (await compoundDeferredStore.ReadAsync()) is { Pending: true },
            "A deferred later step retains recovery state after an earlier mutation was dispatched");
        var failed = new FakeHost { ExitCode = 5 };
        var failure = await Repairs.RunAsync(RepairKind.ComponentStore, folder, host: failed);
        Require(failed.Calls == 1 && failure.Status.Contains("failed"), "Failure skips verification");
        var reboot = new FakeHost { ExitCode = 3010 };
        Require((await Repairs.RunAsync(RepairKind.ComponentStore, folder, host: reboot)).Status.StartsWith("Restart required") && reboot.Calls == 1, "Reboot result defers verification");
        using var stop = new CancellationTokenSource();
        var canceled = new FakeHost { AfterStep = () => stop.Cancel() };
        var canceledResult = await Repairs.RunAsync(RepairKind.SystemFiles, folder, cancellationToken: stop.Token, host: canceled);
        Require(canceled.Calls == 1 && canceledResult.Status.StartsWith("Stopped"), "Stop occurs between commands");
        using var interruptedStop = new CancellationTokenSource();
        var recovery = new RepairRecoveryStore(Path.Combine(scratch, "interrupted-command-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        var interruptedHost = new FakeHost { AfterStep = () =>
        { interruptedStop.Cancel(); throw new OperationCanceledException(interruptedStop.Token); } };
        var interruptedResult = await Repairs.RunAsync(RepairKind.SystemFiles, folder,
            cancellationToken: interruptedStop.Token, host: interruptedHost, recoveryStore: recovery);
        Require(interruptedResult.GetExitCode() == 130 && interruptedResult.Steps.Count == 0 &&
            interruptedResult.Status.Contains("machine changes may have occurred") && (await recovery.ReadAsync())?.Pending == true,
            "Interrupted command retains pending recovery without fabricating command evidence");
        using (var interruptedJournal = JsonDocument.Parse(await File.ReadAllTextAsync(interruptedResult.JournalPath)))
            Require(interruptedJournal.RootElement.GetProperty("status").GetString() == interruptedResult.Status,
                "Interrupted outcome is durable in the repair journal");
        var failedProviderRecovery = new RepairRecoveryStore(Path.Combine(scratch, "failed-provider-recovery"),
            _ => Task.FromResult<BootStamp?>(new(1, DateTimeOffset.Parse("2026-09-13T00:00:00Z"))));
        var failedProvider = new FakeHost { AfterStep = () => throw new IOException("Fixture pipe closed") };
        var failedProviderResult = await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: failedProvider,
            recoveryStore: failedProviderRecovery);
        Require(failedProvider.Calls == 1 && failedProviderResult.Status.Contains("Fixture pipe closed") &&
            failedProviderResult.Status.Contains("Machine changes may have occurred") &&
            failedProviderResult.Status.Contains("reassessment required") &&
            (await failedProviderRecovery.ReadAsync())?.Pending == true,
            "A provider failure on a mutation step warns of possible changes and keeps the recovery record pending");
        var prematureRetry = new FakeHost();
        try
        {
            await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: prematureRetry, recoveryStore: recovery);
            throw new Exception("Interrupted command was retried in the same boot");
        }
        catch (InvalidOperationException) { }
        Require(prematureRetry.Calls == 0, "Pending interruption prevents retry before another command");
        var unknown = new FakeHost { Output = "Localized verification text" };
        Require((await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: unknown)).Status.Contains("review"), "Unknown output never claims healthy");
        var contradictory = new FakeHost { Output = "No component store corruption detected\nThe component store is repairable" };
        Require(!Repairs.IsVerified(await Repairs.RunAsync(RepairKind.ComponentStore, folder, host: contradictory)), "Contradictory servicing output cannot verify a repair");
        var beforeCancel = new FakeHost();
        var timedOut = new FakeHost { TimedOut = true };
        var timedResult = await Repairs.RunAsync(RepairKind.SystemFiles, folder, host: timedOut);
        Require(timedOut.Calls == 1 && timedResult.Status.StartsWith("Timed out") && !Repairs.IsVerified(timedResult), "Timeout stops verification even if a host supplies exit zero");
        try { await Repairs.RunAsync(RepairKind.SystemFiles, folder, cancellationToken: stop.Token, host: beforeCancel); throw new Exception("Canceled repair started"); }
        catch (OperationCanceledException) { }
        Require(beforeCancel.Calls == 0, "Canceled request never executes");
        Require(Repairs.Plan(RepairKind.SystemFiles).Steps[1].Arguments.SequenceEqual(["/verifyonly"]), "SFC verification is read-only");
        Require(Repairs.Plan(RepairKind.ComponentStore).Steps.All(s => s.Arguments.Contains("/NoRestart")), "DISM never restarts automatically");
        string lockedFolder = Path.Combine(scratch, "locked-repair-journal");
        FileStream? lockedJournal = null;
        string? lockedPath = null;
        var blockedSave = new FakeHost { AfterStep = () =>
        {
            lockedPath = Directory.GetFiles(lockedFolder, "repair-*.json").Single();
            lockedJournal = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);
        } };
        try
        {
            await Repairs.RunAsync(RepairKind.ComponentStore, lockedFolder, host: blockedSave);
            throw new Exception("Locked repair journal accepted another step");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        finally { lockedJournal?.Dispose(); }
        Require(blockedSave.Calls == 1, "Journal publication failure prevents the next command");
        using (var retained = JsonDocument.Parse(await File.ReadAllTextAsync(lockedPath!)))
            Require(retained.RootElement.GetProperty("status").GetString()!.StartsWith("Running:"),
                "Failed save preserves the pre-command journal without claiming completion");
        Console.WriteLine("PASS: repair preflight, command plans, verification, failure, reboot, cancellation and journals (simulated execution only).");
    }
    private sealed class ThrowingProgress : IProgress<string>
    {
        internal int Calls;
        public void Report(string value) { Calls++; throw new InvalidOperationException("Fixture display unavailable"); }
    }
    private sealed class FakeHost : IRepairHost
    {
        public bool Denied;
        public bool TimedOut;
        public bool TruncateFirst;
        public int Calls;
        public int ExitCode;
        public string Output = "No component store corruption detected";
        public Action? AfterStep;
        public Func<Task<ToolResult>>? Execute;
        public List<RepairStep> Steps = [];
        public void CheckPrerequisites() { if (Denied) throw new UnauthorizedAccessException(); }
        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress)
        { Calls++; Steps.Add(step); progress?.Report("Fixture command output"); AfterStep?.Invoke(); return Execute is not null ? Execute() : Task.FromResult(new ToolResult(ExitCode, Output, "", TruncateFirst && Calls == 1) { TimedOut = TimedOut }); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
