using System.IO;
using KillerBench.Engine;

internal static class UpdateDataStoreResetChecks
{
    public static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "update-store-reset");
        Directory.CreateDirectory(root);
        var options = new UpdateSearchOptions();
        var rawFailure = new ToolResult(0, "{\"failureHresult\":-2145091581}", "", false);
        UpdateSearchResult Failed(int code) => new(options, [], true, "fixture")
            { FailureHResult = code, Evidence = rawFailure };
        var structural = Failed(unchecked((int)0x80248003));
        var complete = new UpdateSearchResult(options, [], false, "complete") { Evidence = new(0, "{}", "", false) };
        FixtureHost ReadyHost(params UpdateSearchResult[] searches) => new() { SearchResults = new(searches) };
        Require(UpdateDataStoreReset.IsEligible(structural, out _), "Current structural store error is eligible");
        foreach (uint code in new uint[] { 0x80248003, 0x80248004, 0x80248005, 0x80248006, 0x80248008, 0x80248009, 0x8024800E, 0x8024801B })
            Require(UpdateDataStoreReset.IsEligible(Failed(unchecked((int)code)), out _), "Documented structural data-store code is eligible");
        foreach (uint code in new uint[] { 0x80248000, 0x80248001, 0x8024800C, 0x8024800F, 0x80248017, 0x80248018, 0x8024801C, 0x80070005 })
            Require(!UpdateDataStoreReset.IsEligible(Failed(unchecked((int)code)), out _), "Transient, session, lock and access errors remain ineligible");
        foreach (var invalid in new[]
        {
            structural with { Partial = false }, structural with { Updates = [new(Guid.NewGuid().ToString(), 1, "fixture", "Software", false, true)] },
            structural with { FailureHResult = null }, structural with { Evidence = rawFailure with { ExitCode = 1 } },
            structural with { Evidence = rawFailure with { Error = "failure" } }, structural with { Evidence = rawFailure with { TimedOut = true } },
            structural with { Evidence = rawFailure with { Truncated = true } }
        }) Require(!UpdateDataStoreReset.IsEligible(invalid, out _), "Incomplete or conflicting search evidence cannot authorize reset");
        var eligibleCheck = UpdateDataStoreReset.ToWorkflowCheck(structural);
        Require(eligibleCheck is { State: CheckState.Repairable, Repair: RepairKind.UpdateDataStoreReset } &&
            ReferenceEquals(eligibleCheck.Evidence, structural.Evidence),
            "Structural search failure becomes exact guarded recipe eligibility with retained evidence");
        var clearCheck = UpdateDataStoreReset.ToWorkflowCheck(complete);
        Require(clearCheck is { State: CheckState.Clear, Repair: null } &&
            ReferenceEquals(clearCheck.Evidence, complete.Evidence),
            "Successful complete search reports no structural data-store fault");
        var ineligibleCheck = UpdateDataStoreReset.ToWorkflowCheck(Failed(unchecked((int)0x80070005)));
        Require(ineligibleCheck is { State: CheckState.Attention, Repair: null } &&
            ineligibleCheck.Detail.Contains("not eligible"),
            "Nonstructural search failure remains visible without authorizing data-store reset");
        var unknownSearch = structural with
        {
            FailureHResult = null,
            Evidence = rawFailure with { TimedOut = true }
        };
        var unknownCheck = UpdateDataStoreReset.ToWorkflowCheck(unknownSearch);
        Require(unknownCheck is { State: CheckState.Unknown, Repair: null } &&
            ReferenceEquals(unknownCheck.Evidence, unknownSearch.Evidence),
            "Incomplete search evidence cannot classify data-store health or authorize reset");

        var previewHost = new FixtureHost();
        var preview = await UpdateDataStoreReset.RunAsync(structural, false, Path.Combine(root, "preview"), previewHost);
        Require(preview.Outcome == UpdateDataStoreResetOutcome.Preview && previewHost.Calls.Count == 0 &&
            preview.Detail.Contains("will not delete") && preview.Detail.Contains("startup modes"), "Preview is explicit and side-effect free");

        var blockedHost = new FixtureHost { Administrator = false };
        var blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "admin"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && !blockedHost.Calls.Contains("retain"), "Administrator gate precedes mutation");
        blockedHost = new FixtureHost { Activity = new("Installer activity", CheckState.Attention, "busy") };
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "activity"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && blockedHost.Calls.SequenceEqual(["activity", "restart"]), "Active installation blocks before service inspection");
        blockedHost = new FixtureHost { Restart = new("Restart", CheckState.Unknown, "unknown") };
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "restart"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && !blockedHost.Calls.Contains("services"), "Incomplete restart evidence blocks mutation");
        blockedHost = new FixtureHost { Activity = new("Installer activity", CheckState.Information, "idle", Evidence: new(0, "{}", "", false) { Truncated = true }) };
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "activity-evidence"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && !blockedHost.Calls.Contains("services"), "Truncated Installer activity evidence blocks mutation");
        blockedHost = new FixtureHost { Restart = new("Restart", CheckState.Clear, "clear", Evidence: new(0, "{}", "provider error", false)) };
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "restart-evidence"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && !blockedHost.Calls.Contains("services"), "Error-bearing restart evidence blocks mutation");
        blockedHost = new FixtureHost { Services = ServicesWithBits(running: true, startMode: "Disabled") };
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "disabled"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && !blockedHost.Calls.Contains("retain"), "Disabled service is preserved and blocks reset");
        blockedHost = new FixtureHost { Store = new(false, @"C:\Windows\SoftwareDistribution", true) };
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "store"), blockedHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked && !blockedHost.Calls.Contains("retain"), "Incomplete store identity blocks reset");
        var changedFailureHost = new FixtureHost();
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "changed-failure"), changedFailureHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            changedFailureHost.Calls.SequenceEqual(["activity", "restart", "services", "store", "search"]) &&
            !changedFailureHost.Calls.Any(call => call.StartsWith("stop:") || call == "retain"),
            "A changed fresh search blocks before the first mutation");

        var finalSearchHost = ReadyHost(structural, complete);
        string finalSearchRoot = Path.Combine(root, "final-search-cleared");
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, finalSearchRoot, finalSearchHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            !finalSearchHost.Calls.Any(call => call.StartsWith("stop:") || call.StartsWith("start:") || call == "retain") &&
            !File.Exists(Path.Combine(finalSearchRoot, "update-store-reset.pending.json")) &&
            Directory.GetFiles(finalSearchRoot, "*.prepared.json").Length == 1 &&
            File.ReadAllText(blocked.JournalPath!).Contains("FinalEligibility"),
            "A structural failure that clears after preparation closes the guard without mutation");

        var finalServiceHost = ReadyHost(structural, structural);
        finalServiceHost.FinalServices = ServicesWithBits(running: false, startMode: "Manual");
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "final-service-change"), finalServiceHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            !finalServiceHost.Calls.Any(call => call.StartsWith("stop:") || call == "retain"),
            "A required service state change after preparation blocks every mutation");

        var finalActivityHost = ReadyHost(structural, structural);
        finalActivityHost.FinalActivity = new("Installer activity", CheckState.Attention, "busy", Evidence: new(0, "{}", "", false));
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "final-activity-change"), finalActivityHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            !finalActivityHost.Calls.Any(call => call.StartsWith("stop:") || call == "retain"),
            "Installer activity that changes after preparation blocks every mutation");

        var finalRestartHost = ReadyHost(structural, structural);
        finalRestartHost.FinalRestart = new("Restart", CheckState.Attention, "pending", Evidence: new(0, "{}", "", false));
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "final-restart-change"), finalRestartHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            !finalRestartHost.Calls.Any(call => call.StartsWith("stop:") || call == "retain"),
            "A restart requirement that appears after preparation blocks every mutation");

        var finalStoreHost = ReadyHost(structural, structural);
        finalStoreHost.FinalStore = new(true, @"D:\Windows\SoftwareDistribution", true);
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "final-store-change"), finalStoreHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            !finalStoreHost.Calls.Any(call => call.StartsWith("stop:") || call == "retain"),
            "A changed SoftwareDistribution identity after preparation blocks every mutation");

        var finalAdministratorHost = ReadyHost(structural, structural);
        finalAdministratorHost.FinalAdministrator = false;
        blocked = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "final-administrator-loss"), finalAdministratorHost);
        Require(blocked.Outcome == UpdateDataStoreResetOutcome.Blocked &&
            !finalAdministratorHost.Calls.Any(call => call.StartsWith("stop:") || call == "retain"),
            "Administrator access lost after preparation blocks every mutation");

        var verifiedHost = ReadyHost(structural, structural, complete);
        string verifiedRoot = Path.Combine(root, "verified");
        var verified = await UpdateDataStoreReset.RunAsync(structural, true, verifiedRoot, verifiedHost);
        Require(verified.Outcome == UpdateDataStoreResetOutcome.Verified && verified.RetainedStorePath is not null &&
            verified.RetainedStorePath.StartsWith(@"C:\Windows\SoftwareDistribution.KillerBench.", StringComparison.Ordinal) &&
            verifiedHost.Calls.SequenceEqual(["activity", "restart", "services", "store", "search",
                "activity", "restart", "services", "store", "search", "stop:bits", "stop:wuauserv", "stop:cryptsvc",
                "retain", "start:cryptsvc", "start:bits", "start:wuauserv", "search", "store", "services"]),
            "Verified reset preserves bounded service and store order");
        Require(!File.Exists(Path.Combine(verifiedRoot, "update-store-reset.pending.json")) &&
            Directory.GetFiles(verifiedRoot, "*.prepared.json").Length == 1 &&
            File.ReadAllText(verified.JournalPath!).Contains("RetainStore"), "Verified reset archives durable preparation and retains stage evidence");

        var stoppedHost = ReadyHost(structural, structural, complete);
        stoppedHost.Services = ServicesWithBits(running: false, startMode: "Manual");
        var stoppedServiceResult = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "already-stopped"), stoppedHost);
        Require(stoppedServiceResult.Outcome == UpdateDataStoreResetOutcome.Verified &&
            !stoppedHost.Calls.Contains("stop:bits") && !stoppedHost.Calls.Contains("start:bits"),
            "A service that was already stopped is neither stopped nor started by the coordinator");

        var changedStoppedHost = ReadyHost(structural, structural, complete);
        changedStoppedHost.Services = ServicesWithBits(running: false, startMode: "Manual");
        changedStoppedHost.AfterServices = DefaultServices();
        var changedStoppedResult = await UpdateDataStoreReset.RunAsync(structural, true,
            Path.Combine(root, "changed-stopped-service"), changedStoppedHost);
        Require(changedStoppedResult.Outcome == UpdateDataStoreResetOutcome.Uncertain,
            "Verification rejects a service that changed from stopped to running");

        var changedModeHost = ReadyHost(structural, structural, complete);
        changedModeHost.AfterServices = ServicesWithBits(running: true, startMode: "Automatic");
        var changedModeResult = await UpdateDataStoreReset.RunAsync(structural, true,
            Path.Combine(root, "changed-service-mode"), changedModeHost);
        Require(changedModeResult.Outcome == UpdateDataStoreResetOutcome.Uncertain,
            "Verification rejects a changed service startup mode");

        var stopFailureHost = ReadyHost(structural, structural);
        stopFailureHost.FailOperation = "stop:wuauserv";
        var stopFailure = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "stop-failure"), stopFailureHost);
        Require(stopFailure.Outcome == UpdateDataStoreResetOutcome.Uncertain && !stopFailureHost.Calls.Contains("retain") &&
            stopFailureHost.Calls.Contains("start:bits"), "Partial stop failure restores only the service already stopped and never renames the store");

        var retainFailureHost = ReadyHost(structural, structural);
        retainFailureHost.FailOperation = "retain";
        var retainFailure = await UpdateDataStoreReset.RunAsync(structural, true, Path.Combine(root, "retain-failure"), retainFailureHost);
        Require(retainFailure.Outcome == UpdateDataStoreResetOutcome.Uncertain &&
            retainFailureHost.Calls.TakeLast(3).SequenceEqual(["start:cryptsvc", "start:bits", "start:wuauserv"]),
            "Rename failure restores all services that the coordinator stopped");

        var verifyFailureHost = ReadyHost(structural, structural, structural);
        string uncertainRoot = Path.Combine(root, "verify-failure");
        var uncertain = await UpdateDataStoreReset.RunAsync(structural, true, uncertainRoot, verifyFailureHost);
        Require(uncertain.Outcome == UpdateDataStoreResetOutcome.Uncertain &&
            File.Exists(Path.Combine(uncertainRoot, "update-store-reset.pending.json")), "Failed fresh search retains pending reconciliation guard");
        var repeated = await UpdateDataStoreReset.RunAsync(structural, true, uncertainRoot, new FixtureHost());
        Require(repeated.Outcome == UpdateDataStoreResetOutcome.Blocked, "Pending operation prevents a repeated reset");
        var reconcileHost = ReadyHost(complete);
        reconcileHost.RetainedExists = true;
        var reconciled = await UpdateDataStoreResetReconciliation.RunAsync(uncertainRoot, reconcileHost);
        Require(reconciled.Outcome == UpdateDataStoreReconciliationOutcome.Verified &&
            reconcileHost.Calls.SequenceEqual(["activity", "restart", "services", "store", "exists", "search"]) &&
            !reconcileHost.Calls.Any(call => call.StartsWith("stop:") || call.StartsWith("start:") || call == "retain") &&
            !File.Exists(Path.Combine(uncertainRoot, "update-store-reset.pending.json")),
            "Reconciliation verifies current state and archives the guard without repeating mutation");

        string incompleteReconcileRoot = Path.Combine(root, "reconcile-incomplete-prerequisite");
        await UpdateDataStoreReset.RunAsync(structural, true, incompleteReconcileRoot, ReadyHost(structural, structural, structural));
        var incompleteReconcileHost = ReadyHost(complete);
        incompleteReconcileHost.Activity = new("Installer activity", CheckState.Information, "idle",
            Evidence: new ToolResult(0, "{}", "", false) { Truncated = true });
        var incompleteReconcile = await UpdateDataStoreResetReconciliation.RunAsync(incompleteReconcileRoot, incompleteReconcileHost);
        Require(incompleteReconcile.Outcome == UpdateDataStoreReconciliationOutcome.Inconclusive &&
            File.Exists(Path.Combine(incompleteReconcileRoot, "update-store-reset.pending.json")),
            "Reconciliation retains the guard when prerequisite evidence is incomplete");

        string foreignRoot = Path.Combine(root, "reconcile-foreign-machine");
        await UpdateDataStoreReset.RunAsync(structural, true, foreignRoot, ReadyHost(structural, structural, structural));
        string foreignPending = Path.Combine(foreignRoot, "update-store-reset.pending.json");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        var foreignHost = ReadyHost(complete);
        var foreign = await UpdateDataStoreResetReconciliation.RunAsync(foreignRoot, foreignHost);
        Require(foreign.Outcome == UpdateDataStoreReconciliationOutcome.Inconclusive && foreignHost.Calls.Count == 0 &&
            File.Exists(foreignPending), "A foreign-machine reset record retains its guard without inspecting or changing the host");

        string unchangedRoot = Path.Combine(root, "unchanged");
        var unchangedFailureHost = ReadyHost(structural, structural);
        unchangedFailureHost.FailOperation = "stop:wuauserv";
        var failedBeforeRename = await UpdateDataStoreReset.RunAsync(structural, true, unchangedRoot, unchangedFailureHost);
        Require(failedBeforeRename.Outcome == UpdateDataStoreResetOutcome.Uncertain, "Pre-rename failure leaves reconciliation evidence");
        var unchanged = await UpdateDataStoreResetReconciliation.RunAsync(unchangedRoot, new FixtureHost { RetainedExists = false });
        Require(unchanged.Outcome == UpdateDataStoreReconciliationOutcome.NotChanged,
            "Reconciliation can prove the original store remained in place without rerunning the reset");

        string canceledAfterMutationRoot = Path.Combine(root, "canceled-after-mutation");
        var canceledAfterMutationHost = ReadyHost(structural, structural);
        canceledAfterMutationHost.CancelOperation = "retain";
        var canceledAfterMutation = await UpdateDataStoreReset.RunAsync(structural, true, canceledAfterMutationRoot, canceledAfterMutationHost);
        using (var lastStage = System.Text.Json.JsonDocument.Parse(File.ReadAllLines(canceledAfterMutation.JournalPath!).Last()))
            Require(canceledAfterMutation.Outcome == UpdateDataStoreResetOutcome.Stopped &&
                lastStage.RootElement.GetProperty("Stage").GetString() == "Finished" &&
                lastStage.RootElement.GetProperty("Result").GetProperty("Outcome").GetInt32() == (int)UpdateDataStoreResetOutcome.Stopped &&
                File.Exists(Path.Combine(canceledAfterMutationRoot, "update-store-reset.pending.json")),
                "Cancellation after a mutation journals a final Stopped record and keeps the pending guard");

        string missingRetainedRoot = Path.Combine(root, "retained-missing-after-retention");
        var retainedThenUncertainHost = ReadyHost(structural, structural, complete);
        retainedThenUncertainHost.AfterStore = new(true, @"C:\Windows\SoftwareDistribution", false);
        var retainedThenUncertain = await UpdateDataStoreReset.RunAsync(structural, true, missingRetainedRoot, retainedThenUncertainHost);
        Require(retainedThenUncertain.Outcome == UpdateDataStoreResetOutcome.Uncertain &&
            File.ReadAllText(retainedThenUncertain.JournalPath!).Contains("\"Stage\":\"RetainStore\"", StringComparison.Ordinal),
            "Fixture reset journals store retention before uncertain verification");
        var missingRetained = await UpdateDataStoreResetReconciliation.RunAsync(missingRetainedRoot, new FixtureHost { RetainedExists = false });
        Require(missingRetained.Outcome == UpdateDataStoreReconciliationOutcome.Inconclusive &&
            missingRetained.Detail.Contains("retention may have occurred", StringComparison.Ordinal) &&
            !missingRetained.Detail.Contains("did not reach store retention", StringComparison.Ordinal) &&
            File.Exists(Path.Combine(missingRetainedRoot, "update-store-reset.pending.json")),
            "A missing retained store after a journaled retention is not reported as an unchanged reset");

        Console.WriteLine("PASS: guarded Windows Update data-store reset planning, eligibility, ordering, retention and verification.");
    }

    private static UpdateResetService[] DefaultServices() =>
    [
        new("bits", true, true, true, "Manual"),
        new("wuauserv", true, true, true, "Manual"),
        new("cryptsvc", true, true, true, "Automatic")
    ];

    private static UpdateResetService[] ServicesWithBits(bool running, string startMode) =>
    [
        new("bits", true, true, running, startMode),
        new("wuauserv", true, true, true, "Manual"),
        new("cryptsvc", true, true, true, "Automatic")
    ];

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class FixtureHost : IUpdateDataStoreResetHost
    {
        private int administratorReads;
        public bool Administrator { get; init; } = true;
        public bool? FinalAdministrator { get; set; }
        public bool IsAdministrator => ++administratorReads > 1 && FinalAdministrator.HasValue
            ? FinalAdministrator.Value : Administrator;
        public WorkflowCheck Activity { get; set; } = new("Installer activity", CheckState.Information, "idle", Evidence: new(0, "{}", "", false));
        public WorkflowCheck? FinalActivity { get; set; }
        public WorkflowCheck Restart { get; set; } = new("Restart", CheckState.Clear, "clear", Evidence: new(0, "{}", "", false));
        public WorkflowCheck? FinalRestart { get; set; }
        public UpdateResetService[] Services { get; set; } = DefaultServices();
        public UpdateResetService[]? FinalServices { get; set; }
        public UpdateResetService[]? AfterServices { get; set; }
        public UpdateStoreSnapshot Store { get; set; } = new(true, @"C:\Windows\SoftwareDistribution", true);
        public UpdateStoreSnapshot? FinalStore { get; set; }
        public UpdateStoreSnapshot? AfterStore { get; set; }
        public string? FailOperation { get; set; }
        public string? CancelOperation { get; set; }
        public Queue<UpdateSearchResult> SearchResults { get; init; } = new();
        public bool RetainedExists { get; set; } = true;
        public List<string> Calls { get; } = [];

        public Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token)
        {
            Calls.Add("activity");
            return Task.FromResult(Calls.Count(call => call == "activity") > 1 && FinalActivity is not null ? FinalActivity : Activity);
        }
        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
        {
            Calls.Add("restart");
            return Task.FromResult(Calls.Count(call => call == "restart") > 1 && FinalRestart is not null ? FinalRestart : Restart);
        }
        public Task<IReadOnlyList<UpdateResetService>> ReadServicesAsync(CancellationToken token)
        {
            Calls.Add("services");
            int read = Calls.Count(call => call == "services");
            var value = read > 2 && AfterServices is not null ? AfterServices :
                read > 1 && FinalServices is not null ? FinalServices : Services;
            return Task.FromResult<IReadOnlyList<UpdateResetService>>(value);
        }
        public Task<UpdateStoreSnapshot> ReadStoreAsync(CancellationToken token)
        {
            Calls.Add("store");
            int read = Calls.Count(call => call == "store");
            return Task.FromResult(read > 2 && AfterStore is not null ? AfterStore :
                read > 1 && FinalStore is not null ? FinalStore : Store);
        }
        public Task<bool> DirectoryExistsAsync(string path, CancellationToken token) { Calls.Add("exists"); return Task.FromResult(RetainedExists); }
        public Task<ToolResult> StopServiceAsync(string name, CancellationToken token) => Result("stop:" + name);
        public Task<ToolResult> StartServiceAsync(string name, CancellationToken token) => Result("start:" + name);
        public Task<ToolResult> RetainStoreAsync(string sourcePath, string retainedPath, CancellationToken token) => Result("retain");
        public Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, CancellationToken token)
        {
            Calls.Add("search");
            return Task.FromResult(SearchResults.Count == 0
                ? new UpdateSearchResult(options, [], false, "complete") { Evidence = new(0, "{}", "", false) }
                : SearchResults.Dequeue());
        }
        private Task<ToolResult> Result(string operation)
        {
            Calls.Add(operation);
            if (CancelOperation == operation) return Task.FromCanceled<ToolResult>(new CancellationToken(true));
            return Task.FromResult(FailOperation == operation ? new ToolResult(1, "", "fixture failure", false) : new ToolResult(0, "", "", false));
        }
    }
}
