using KillerBench.Engine;
using System.IO;

internal static class OrcaCacheResetChecks
{
    public static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "orca-cache-reset");
        var options = new OrcaReviewOptions(OrcaOutlookMode.New, OrcaScope.RunningAccount, OrcaActions.ClearCaches);
        var recipe = RecipeCatalog.All.Single(item => item.Id == "applications.outlook-cache-reset");
        Require(recipe.Action == RepairKind.OrcaCacheReset && recipe.RequiresExplicitOptions && !recipe.CanAutoExecute &&
            recipe.RecoveryContract == RepairRecoveryContract.OrcaCacheState &&
            Repairs.Plan(recipe.Action).Steps.Count == 0 && Repairs.Plan(recipe.Action).Effects.Contains("generic recipe runner never", StringComparison.Ordinal),
            "Outlook cache retention is a focused option-dependent recipe with an exact recovery contract");

        var invalid = await OrcaCacheReset.RunAsync(
            options with { Actions = OrcaActions.ClearCaches | OrcaActions.ResetProfileSettings }, false,
            Path.Combine(root, "invalid"), new FixtureHost(OrcaOutlookMode.New));
        Require(invalid.Outcome == OrcaCacheResetOutcome.Blocked, "Only cache-only running-account requests are executable");

        var activeHost = new FixtureHost(OrcaOutlookMode.New) { Processes = new([Processes(1, 0)]) };
        var activeReview = await OrcaCacheReset.ReviewAsync(options, activeHost);
        var active = await OrcaCacheReset.RunAsync(options, false, Path.Combine(root, "active"), activeHost);
        Require(activeReview.State == CheckState.Attention && activeReview.Repair is null &&
            active.Outcome == OrcaCacheResetOutcome.Blocked && activeHost.Retains == 0,
            "Active New Outlook blocks cache retention");

        var reparseHost = new FixtureHost(OrcaOutlookMode.New) { Reparse = true };
        var reparse = await OrcaCacheReset.RunAsync(options, false, Path.Combine(root, "reparse"), reparseHost);
        Require(reparse.Outcome == OrcaCacheResetOutcome.Blocked && reparseHost.Retains == 0,
            "Reparse-point cache targets are refused");

        var previewHost = new FixtureHost(OrcaOutlookMode.New);
        var workflowReview = await OrcaCacheReset.ReviewAsync(options, previewHost);
        var preview = await OrcaCacheReset.RunAsync(options, false, Path.Combine(root, "preview"), previewHost);
        Require(workflowReview.State == CheckState.Repairable && workflowReview.Repair == RepairKind.OrcaCacheReset &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Orca).Single().Id == recipe.Id &&
            preview.Outcome == OrcaCacheResetOutcome.Preview && previewHost.Retains == 0 &&
            preview.Detail.Contains("unique recovery names", StringComparison.Ordinal),
            "Preview collects eligibility without moving a cache");

        string verifiedRoot = Path.Combine(root, "verified");
        var verifiedHost = new FixtureHost(OrcaOutlookMode.New);
        var verified = await OrcaCacheReset.RunAsync(options, true, verifiedRoot, verifiedHost);
        Require(verified.Outcome == OrcaCacheResetOutcome.Verified && verifiedHost.Retains == 1 &&
            verified.Targets is { Count: 1 } && !File.Exists(Path.Combine(verifiedRoot, "orca-cache-reset.pending.json")) &&
            Directory.GetFiles(verifiedRoot, "*.prepared.json").Length == 1,
            "Verified cache retention archives its durable guard after exact source and destination checks");

        string changedRoot = Path.Combine(root, "changed");
        var changedHost = new FixtureHost(OrcaOutlookMode.New)
        {
            Processes = new([Processes(), Processes(), Processes(1, 0)])
        };
        var changed = await OrcaCacheReset.RunAsync(options, true, changedRoot, changedHost);
        Require(changed.Outcome == OrcaCacheResetOutcome.Blocked && changedHost.Retains == 0 &&
            !File.Exists(Path.Combine(changedRoot, "orca-cache-reset.pending.json")) &&
            Directory.GetFiles(changedRoot, "*.blocked.json").Length == 1,
            "A final process change closes the prepared guard without moving a cache");

        string failureRoot = Path.Combine(root, "failure");
        var failureHost = new FixtureHost(OrcaOutlookMode.New) { FailRetainAt = 1 };
        var failed = await OrcaCacheReset.RunAsync(options, true, failureRoot, failureHost);
        Require(failed.Outcome == OrcaCacheResetOutcome.Uncertain && failureHost.Retains == 1 &&
            File.Exists(Path.Combine(failureRoot, "orca-cache-reset.pending.json")),
            "Uncertain cache retention preserves the no-repeat guard");
        var repeated = await OrcaCacheReset.RunAsync(options, true, failureRoot, new FixtureHost(OrcaOutlookMode.New));
        Require(repeated.Outcome == OrcaCacheResetOutcome.Blocked, "A pending guard blocks another cache reset");

        OrcaCacheTargetPlan failedPlan = failed.Targets!.Single();
        var recoveryHost = new FixtureHost(OrcaOutlookMode.New);
        recoveryHost.SetExists(failedPlan.SourcePath, false);
        recoveryHost.SetExists(failedPlan.RetainedPath, true);
        var recovered = await OrcaCacheReset.ReconcileAsync(failureRoot, recoveryHost);
        Require(recovered.Outcome == OrcaCacheReconciliationOutcome.Verified && recoveryHost.Retains == 0 &&
            !File.Exists(Path.Combine(failureRoot, "orca-cache-reset.pending.json")),
            "Reconciliation verifies an independently completed move without repeating it");

        string unchangedRoot = Path.Combine(root, "unchanged");
        var unchangedRunHost = new FixtureHost(OrcaOutlookMode.New) { FailRetainAt = 1 };
        await OrcaCacheReset.RunAsync(options, true, unchangedRoot, unchangedRunHost);
        var unchanged = await OrcaCacheReset.ReconcileAsync(unchangedRoot, unchangedRunHost);
        Require(unchanged.Outcome == OrcaCacheReconciliationOutcome.NotChanged && unchangedRunHost.Retains == 1,
            "Reconciliation archives a proven unchanged source without retrying it");

        string partialRoot = Path.Combine(root, "partial");
        var bothOptions = options with { Mode = OrcaOutlookMode.Both };
        var partialRunHost = new FixtureHost(OrcaOutlookMode.Both) { FailRetainAt = 2 };
        var partialRun = await OrcaCacheReset.RunAsync(bothOptions, true, partialRoot, partialRunHost);
        Require(partialRun.Outcome == OrcaCacheResetOutcome.Uncertain && partialRunHost.Retains == 2,
            "A partial multi-cache move stops before later targets");
        var partial = await OrcaCacheReset.ReconcileAsync(partialRoot, partialRunHost);
        Require(partial.Outcome == OrcaCacheReconciliationOutcome.Inconclusive &&
            File.Exists(Path.Combine(partialRoot, "orca-cache-reset.pending.json")) && partialRunHost.Retains == 2,
            "Mixed retained and unchanged targets preserve the guard without repeating a move");

        string canceledRoot = Path.Combine(root, "canceled");
        var canceledHost = new FixtureHost(OrcaOutlookMode.New) { CancelRetainAt = 1 };
        var canceled = await OrcaCacheReset.RunAsync(options, true, canceledRoot, canceledHost);
        Require(canceled.Outcome == OrcaCacheResetOutcome.Stopped &&
            File.Exists(Path.Combine(canceledRoot, "orca-cache-reset.pending.json")),
            "Cancellation after preparation retains recovery state");

        var none = await OrcaCacheReset.ReconcileAsync(Path.Combine(root, "none"), new FixtureHost(OrcaOutlookMode.New));
        Require(none.Outcome == OrcaCacheReconciliationOutcome.NoPendingRecord,
            "Recovery distinguishes an absent pending record");

        Console.WriteLine("PASS: Outlook reset exact cache retention uses repeated process and path eligibility, durable preparation, exact verification and no-repeat reconciliation.");
    }

    private static OrcaProcessEvidence Processes(int modern = 0, int classic = 0) =>
        new(modern, classic, true, "Fixture process counts.");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FixtureHost : IOrcaCacheResetHost
    {
        private readonly OrcaOutlookMode mode;
        private readonly Dictionary<string, bool> existence = new(StringComparer.OrdinalIgnoreCase);

        public FixtureHost(OrcaOutlookMode mode)
        {
            this.mode = mode;
            foreach (string path in OrcaCacheReset.ExpectedPaths(mode)) existence[path] = true;
        }

        public Queue<OrcaProcessEvidence> Processes { get; init; } = new([OrcaCacheResetChecks.Processes()]);
        public bool Reparse { get; init; }
        public int FailRetainAt { get; init; }
        public int CancelRetainAt { get; init; }
        public int Retains { get; private set; }

        public void SetExists(string path, bool value) => existence[Path.GetFullPath(path)] = value;

        public Task<OrcaProcessEvidence> ReadProcessesAsync(OrcaOutlookMode selectedMode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Require(selectedMode == mode, "Fixture mode remains stable");
            return Task.FromResult(Processes.Count > 1 ? Processes.Dequeue() : Processes.Peek());
        }

        public Task<IReadOnlyList<OrcaCacheTarget>> ReadTargetsAsync(OrcaOutlookMode selectedMode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Require(selectedMode == mode, "Fixture target mode remains stable");
            IReadOnlyList<OrcaCacheTarget> result = [.. OrcaCacheReset.ExpectedPaths(mode)
                .Select(path => new OrcaCacheTarget(Path.GetFileName(path), path, Exists(path), Reparse, true))];
            return Task.FromResult(result);
        }

        public Task<bool> DirectoryExistsAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Exists(path));
        }

        public Task<ToolResult> RetainAsync(string sourcePath, string retainedPath, CancellationToken token)
        {
            Retains++;
            if (CancelRetainAt == Retains) throw new OperationCanceledException();
            if (FailRetainAt == Retains) return Task.FromResult(new ToolResult(1, "", "Fixture failure", false));
            SetExists(sourcePath, false);
            SetExists(retainedPath, true);
            return Task.FromResult(new ToolResult(0, "", "", false));
        }

        private bool Exists(string path) => existence.TryGetValue(Path.GetFullPath(path), out bool value) && value;
    }
}
