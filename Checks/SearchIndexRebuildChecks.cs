using System.IO;
using KillerBench.Engine;

internal static class SearchIndexRebuildChecks
{
    public static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "search-index-rebuild");
        var previewHost = new FixtureHost();
        var preview = await SearchIndexRebuild.RunAsync(false, Path.Combine(root, "preview"), previewHost);
        Require(preview.Outcome == SearchIndexRebuildOutcome.Preview && previewHost.Calls.Count == 0 &&
            preview.Detail.Contains("will not be deleted"), "Search rebuild preview is side-effect free and explicit");

        var reviewHost = new FixtureHost();
        var review = await SearchIndexRebuild.ReviewAsync(reviewHost);
        var reviewCheck = SearchIndexRebuild.ToWorkflowCheck(review);
        Require(review.Outcome == SearchIndexRebuildOutcome.Preview &&
            reviewHost.Calls.SequenceEqual(["activity", "restart", "service", "index"]) &&
            reviewCheck.State == CheckState.Repairable && reviewCheck.Repair == RepairKind.SearchIndexRebuild,
            "Complete fresh Search evidence exposes explicit index rebuild without mutation");
        var unavailableReview = await SearchIndexRebuild.ReviewAsync(new FixtureHost { Index = new(false, SearchIndexRebuild.IndexPath, false, false) });
        var unavailableCheck = SearchIndexRebuild.ToWorkflowCheck(unavailableReview);
        Require(unavailableReview.Outcome == SearchIndexRebuildOutcome.Blocked && unavailableCheck.State == CheckState.Attention &&
            unavailableCheck.Repair is null,
            "Incomplete index identity cannot expose a rebuild");

        var admin = new FixtureHost { Administrator = false };
        var blocked = await SearchIndexRebuild.RunAsync(true, Path.Combine(root, "admin"), admin);
        Require(blocked.Outcome == SearchIndexRebuildOutcome.Blocked && admin.Calls.Count == 0, "Administrator gate precedes collection and mutation");

        var busy = new FixtureHost { Activity = Check(CheckState.Attention) };
        blocked = await SearchIndexRebuild.RunAsync(true, Path.Combine(root, "busy"), busy);
        Require(blocked.Outcome == SearchIndexRebuildOutcome.Blocked && busy.Calls.SequenceEqual(["activity"]), "Active servicing blocks before Search inspection");

        var restart = new FixtureHost { Restart = Check(CheckState.Unknown) };
        blocked = await SearchIndexRebuild.RunAsync(true, Path.Combine(root, "restart"), restart);
        Require(blocked.Outcome == SearchIndexRebuildOutcome.Blocked && restart.Calls.SequenceEqual(["activity", "restart"]), "Incomplete restart evidence blocks mutation");

        var disabled = new FixtureHost { Services = new([new(true, true, false, "Disabled")]) };
        blocked = await SearchIndexRebuild.RunAsync(true, Path.Combine(root, "disabled"), disabled);
        Require(blocked.Outcome == SearchIndexRebuildOutcome.Blocked && !disabled.Calls.Contains("stop"), "Disabled Search service is preserved");

        var reparse = new FixtureHost { Index = new(true, SearchIndexRebuild.IndexPath, true, true) };
        blocked = await SearchIndexRebuild.RunAsync(true, Path.Combine(root, "reparse"), reparse);
        Require(blocked.Outcome == SearchIndexRebuildOutcome.Blocked && !reparse.Calls.Contains("stop"), "Reparse-point index is never moved");

        string changedRoot = Path.Combine(root, "changed-before-stop");
        var changed = new FixtureHost
        {
            Services = new([Running(), new(true, true, false, "Disabled")]),
            Indexes = new([Index(), Index()])
        };
        blocked = await SearchIndexRebuild.RunAsync(true, changedRoot, changed);
        Require(blocked.Outcome == SearchIndexRebuildOutcome.Blocked &&
            changed.Calls.SequenceEqual(["activity", "restart", "service", "index", "activity", "restart", "service", "index"]) &&
            !changed.Calls.Contains("stop") && !File.Exists(Path.Combine(changedRoot, "search-index-rebuild.pending.json")) &&
            Directory.GetFiles(changedRoot, "*.blocked.json").Length == 1 &&
            Directory.GetFiles(changedRoot, "*.prepared.json").Length == 1,
            "Changed final Search eligibility records evidence and closes the no-mutation guard");

        string canceledFinalRoot = Path.Combine(root, "canceled-final-check");
        var canceledFinalHost = new FixtureHost { ReadErrorCall = 5, ReadError = new OperationCanceledException("fixture") };
        var canceledFinal = await SearchIndexRebuild.RunAsync(true, canceledFinalRoot, canceledFinalHost);
        Require(canceledFinal.Outcome == SearchIndexRebuildOutcome.Stopped && !canceledFinalHost.Calls.Contains("stop") &&
            !File.Exists(Path.Combine(canceledFinalRoot, "search-index-rebuild.pending.json")) &&
            Directory.GetFiles(canceledFinalRoot, "*.stopped.json").Length == 1 &&
            Directory.GetFiles(canceledFinalRoot, "*.prepared.json").Length == 1,
            "Canceled final Search evidence archives a prepared no-change result without stopping the service");

        string unavailableFinalRoot = Path.Combine(root, "unavailable-final-check");
        var unavailableFinalHost = new FixtureHost { ReadErrorCall = 6, ReadError = new IOException("fixture") };
        var unavailableFinal = await SearchIndexRebuild.RunAsync(true, unavailableFinalRoot, unavailableFinalHost);
        Require(unavailableFinal.Outcome == SearchIndexRebuildOutcome.Blocked && !unavailableFinalHost.Calls.Contains("stop") &&
            !File.Exists(Path.Combine(unavailableFinalRoot, "search-index-rebuild.pending.json")) &&
            Directory.GetFiles(unavailableFinalRoot, "*.blocked.json").Length == 1 &&
            Directory.GetFiles(unavailableFinalRoot, "*.prepared.json").Length == 1,
            "Unavailable final Search evidence archives a prepared no-change result without stopping the service");

        string verifiedRoot = Path.Combine(root, "verified");
        var verifiedHost = new FixtureHost
        {
            Services = new([Running(), Running(), Stopped(), Running()]),
            Indexes = new([Index(), Index(), Index()])
        };
        var verified = await SearchIndexRebuild.RunAsync(true, verifiedRoot, verifiedHost);
        Require(verified.Outcome == SearchIndexRebuildOutcome.Verified && verified.RetainedPath is not null &&
            verifiedHost.Calls.SequenceEqual(["activity", "restart", "service", "index", "activity", "restart", "service", "index", "stop", "service", "retain", "start", "service", "index", "exists"]),
            "Verified Search rebuild preserves evidence, mutation and verification order");
        Require(!File.Exists(Path.Combine(verifiedRoot, "search-index-rebuild.pending.json")) &&
            Directory.GetFiles(verifiedRoot, "*.verified.json").Length == 1, "Verified Search rebuild archives and clears its pending guard");

        string retainRoot = Path.Combine(root, "retain-failure");
        var retainFailure = new FixtureHost { Services = new([Running(), Running(), Stopped()]), Fail = "retain" };
        var failed = await SearchIndexRebuild.RunAsync(true, retainRoot, retainFailure);
        Require(failed.Outcome == SearchIndexRebuildOutcome.Uncertain && retainFailure.Calls.TakeLast(2).SequenceEqual(["retain", "start"]) &&
            File.Exists(Path.Combine(retainRoot, "search-index-rebuild.pending.json")), "Retain failure attempts service restoration and preserves the guard");

        string canceledRoot = Path.Combine(root, "canceled");
        var canceledHost = new FixtureHost { Services = new([Running(), Running()]), Cancel = "stop" };
        var canceled = await SearchIndexRebuild.RunAsync(true, canceledRoot, canceledHost);
        Require(canceled.Outcome == SearchIndexRebuildOutcome.Stopped &&
            File.Exists(Path.Combine(canceledRoot, "search-index-rebuild.pending.json")), "Cancellation after preparation preserves the no-repeat guard");

        string uncertainRoot = Path.Combine(root, "uncertain");
        var uncertainHost = new FixtureHost { Services = new([Running(), Running(), Stopped(), new(true, true, false, "Automatic")]) };
        var uncertain = await SearchIndexRebuild.RunAsync(true, uncertainRoot, uncertainHost);
        Require(uncertain.Outcome == SearchIndexRebuildOutcome.Uncertain && File.Exists(Path.Combine(uncertainRoot, "search-index-rebuild.pending.json")),
            "Incomplete verification retains a reconciliation guard");
        var repeated = await SearchIndexRebuild.RunAsync(true, uncertainRoot, new FixtureHost());
        Require(repeated.Outcome == SearchIndexRebuildOutcome.Blocked, "Pending Search rebuild prevents repeated mutation");

        var reconcileHost = new FixtureHost();
        var reconciled = await SearchIndexRebuildReconciliation.RunAsync(uncertainRoot, reconcileHost);
        Require(reconciled.Outcome == SearchIndexReconciliationOutcome.Verified && reconcileHost.Calls.SequenceEqual(["service", "index", "exists"]) &&
            !reconcileHost.Calls.Any(call => call is "stop" or "start" or "retain"), "Reconciliation verifies without repeating mutation");

        string unchangedRoot = Path.Combine(root, "unchanged");
        var beforeMove = new FixtureHost { Services = new([Running(), Running()]), Fail = "stop" };
        await SearchIndexRebuild.RunAsync(true, unchangedRoot, beforeMove);
        var unchangedHost = new FixtureHost { RetainedExists = false };
        var unchanged = await SearchIndexRebuildReconciliation.RunAsync(unchangedRoot, unchangedHost);
        Require(unchanged.Outcome == SearchIndexReconciliationOutcome.NotChanged, "Reconciliation can prove the original index remained unchanged");

        string ambiguousRoot = Path.Combine(root, "ambiguous");
        var ambiguousRun = new FixtureHost { Services = new([Running(), Running(), Stopped(), new(true, true, false, "Automatic")]) };
        await SearchIndexRebuild.RunAsync(true, ambiguousRoot, ambiguousRun);
        var ambiguousHost = new FixtureHost { Index = new(false, SearchIndexRebuild.IndexPath, false, false) };
        var ambiguous = await SearchIndexRebuildReconciliation.RunAsync(ambiguousRoot, ambiguousHost);
        Require(ambiguous.Outcome == SearchIndexReconciliationOutcome.Inconclusive &&
            File.Exists(Path.Combine(ambiguousRoot, "search-index-rebuild.pending.json")), "Ambiguous reconciliation retains its no-repeat guard");

        // Every exit after the stop request and before the index move must try once to bring Windows Search back.
        foreach (var (name, host) in new (string, FixtureHost)[]
        {
            ("restore-canceled", new FixtureHost { Services = new([Running(), Running()]), Cancel = "stop" }),
            ("restore-stop-failed", new FixtureHost { Services = new([Running(), Running()]), Fail = "stop" }),
            ("restore-still-running", new FixtureHost { Services = new([Running(), Running(), Running()]) }),
            ("restore-read-error", new FixtureHost { Services = new([Running(), Running()]), ReadErrorCall = 10, ReadError = new IOException("fixture") })
        })
        {
            var stoppedEarly = await SearchIndexRebuild.RunAsync(true, Path.Combine(root, name), host);
            Require(host.Calls.Contains("stop") && host.Calls.Count(call => call == "start") == 1 && !host.Calls.Contains("retain") &&
                stoppedEarly.Detail.Contains("restart was attempted", StringComparison.Ordinal) &&
                File.Exists(Path.Combine(root, name, "search-index-rebuild.pending.json")),
                "Search rebuild exit after stopping the service attempts one restart: " + name);
        }

        Console.WriteLine("PASS: guarded Windows Search index retention, repeated eligibility, rebuild verification and no-repeat reconciliation.");
    }

    private static WorkflowCheck Check(CheckState state) => new("fixture", state, "fixture", Evidence: new(0, "{}", "", false));
    private static SearchIndexServiceSnapshot Running() => new(true, true, true, "Automatic");
    private static SearchIndexServiceSnapshot Stopped() => new(true, true, false, "Automatic");
    private static SearchIndexDirectorySnapshot Index() => new(true, SearchIndexRebuild.IndexPath, true, false);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class FixtureHost : ISearchIndexRebuildHost
    {
        public bool Administrator { get; init; } = true;
        public bool IsAdministrator => Administrator;
        public WorkflowCheck Activity { get; init; } = Check(CheckState.Information);
        public WorkflowCheck Restart { get; init; } = Check(CheckState.Clear);
        public Queue<SearchIndexServiceSnapshot> Services { get; init; } = new([Running()]);
        public Queue<SearchIndexDirectorySnapshot> Indexes { get; init; } = new();
        public SearchIndexDirectorySnapshot Index { get; init; } = SearchIndexRebuildChecks.Index();
        public bool RetainedExists { get; init; } = true;
        public string? Fail { get; init; }
        public string? Cancel { get; init; }
        public int ReadErrorCall { get; init; }
        public Exception? ReadError { get; init; }
        public List<string> Calls { get; } = [];
        public Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token)
        { Calls.Add("activity"); return ReadResult(Activity); }
        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
        { Calls.Add("restart"); return ReadResult(Restart); }
        public Task<SearchIndexServiceSnapshot> ReadServiceAsync(CancellationToken token)
        { Calls.Add("service"); return ReadResult(Services.Count > 1 ? Services.Dequeue() : Services.Peek()); }
        public Task<SearchIndexDirectorySnapshot> ReadIndexAsync(CancellationToken token)
        { Calls.Add("index"); return ReadResult(Indexes.Count > 0 ? Indexes.Dequeue() : Index); }
        public Task<bool> DirectoryExistsAsync(string path, CancellationToken token) { Calls.Add("exists"); return Task.FromResult(RetainedExists); }
        public Task<ToolResult> StopServiceAsync(CancellationToken token) => Result("stop");
        public Task<ToolResult> StartServiceAsync(CancellationToken token) => Result("start");
        public Task<ToolResult> RetainIndexAsync(string sourcePath, string retainedPath, CancellationToken token) => Result("retain");
        private Task<T> ReadResult<T>(T value) => Calls.Count == ReadErrorCall && ReadError is not null
            ? Task.FromException<T>(ReadError)
            : Task.FromResult(value);
        private Task<ToolResult> Result(string name)
        {
            Calls.Add(name);
            if (Cancel == name) return Task.FromException<ToolResult>(new OperationCanceledException());
            return Task.FromResult(Fail == name ? new ToolResult(1, "", "fixture", false) : new ToolResult(0, "", "", false));
        }
    }
}
