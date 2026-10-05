using KillerBench.Engine;
using System.IO;

internal static class TaskRegistrationChecks
{
    private sealed class MissingHost(ScheduledTaskPlan plan, int stage, bool missingTask) : ITaskRegistrationHost
    {
        private int calls;
        private Task<T> Reply<T>(T value, int current) where T : class => current != stage ? Task.FromResult(value) : missingTask ? null! : Task.FromResult<T>(null!);
        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token) =>
            Reply(new TaskRegistrationPreflight(plan.Identity, true, "Fixture", new(0, "Fixture", "", false)), ++calls);
        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token) => Reply(new ToolResult(0, "Fixture", "", false), ++calls);
        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token) =>
            Reply(new TaskRegistrationVerification(plan, new(0, "Fixture", "", false)), ++calls);
    }
    private static readonly string[] ExpectedProgressMessages =
    [
        "Checking the exact task target, account and signed executable.",
        "Saving the pending recovery record before task creation.",
        "Repeating exact registration eligibility after preparation.",
        "Creating one disabled task definition with create-only semantics.",
        "Reading back the exact disabled task definition.",
        "Saving verified registration evidence.",
        "Disabled task definition verified. The task was not run."
    ];
    private static readonly string[] ExpectedReviewStates = ["DefinitionVerified", "Pending"];
    internal static void Run()
    {
        var plan = new ScheduledTaskPlan(new("\\Fixture\\", "Create"), new("C:\\Fixture\\tool.exe", ""),
            new(TaskScheduleKind.Startup), new(ScheduledTaskAccountKind.InteractiveUser, "fixture", false), TimeSpan.FromMinutes(1));
        var previewProgress = new ImmediateProgress();
        var previewHost = new Host(plan, "success");
        var preview = TaskRegistration.RunAsync(plan, previewHost, progress: previewProgress).GetAwaiter().GetResult();
        if (preview.Status != "Preview" || previewHost.Checks != 0 || previewHost.Creates != 0 || previewHost.Reads != 0 ||
            previewProgress.Messages.Count != 1 ||
            !previewProgress.Messages[0].Contains("No eligibility probe", StringComparison.Ordinal))
            throw new Exception("Task registration preview did not report its exact no-provider boundary.");
        var executionProgress = new ImmediateProgress();
        var progressHost = new Host(plan, "success");
        var progressResult = TaskRegistration.RunAsync(plan, progressHost, true, journal: new Journal(),
            progress: executionProgress).GetAwaiter().GetResult();
        if (progressResult.Status != "Registered" || !executionProgress.Messages.SequenceEqual(ExpectedProgressMessages))
            throw new Exception("Task registration did not report the deterministic guarded stage sequence.");
        var throwingProgressResult = TaskRegistration.RunAsync(plan, new Host(plan, "success"), true,
            journal: new Journal(), progress: new ThrowingProgress()).GetAwaiter().GetResult();
        if (throwingProgressResult.Status != "Registered")
            throw new Exception("A failed progress observer interrupted task registration.");
        foreach (string stage in new[] { "create-return", "verify-return" })
        {
            using var stop = new CancellationTokenSource();
            var host = new InterruptedHost(plan, stage, stop);
            var journal = new Journal();
            var result = TaskRegistration.RunAsync(plan, host, true, journal, token: stop.Token).GetAwaiter().GetResult();
            if (result.Status != "Unverified" || !result.CreateAttempted || !journal.Pending ||
                host.Reads != (stage == "create-return" ? 0 : 1) || result.Evidence.Count != (stage == "create-return" ? 3 : 4))
                throw new Exception("Cancellation accompanying a completed result advanced registration or lost evidence.");
        }
        foreach (bool missingTask in new[] { false, true })
        for (int stage = 1; stage <= 4; stage++)
        {
            var journal = new Journal();
            var result = TaskRegistration.RunAsync(plan, new MissingHost(plan, stage, missingTask), true, journal: journal).GetAwaiter().GetResult();
            if (result.Status != (stage <= 2 ? "Blocked" : "Unverified") || result.CreateAttempted != (stage > 2) || journal.Pending != (stage > 2))
                throw new Exception("Missing provider response lost registration uncertainty.");
        }
        foreach (Exception failure in new Exception[] { new InvalidDataException("Malformed provider reply"), new NotSupportedException("Unsupported"),
            new TimeoutException("Provider timed out"), new System.Security.SecurityException("Denied"), new System.ComponentModel.Win32Exception(5) })
        for (int stage = 1; stage <= 4; stage++)
        {
            var host = new FailedHost(plan, stage, failure);
            var journal = new Journal();
            var result = TaskRegistration.RunAsync(plan, host, true, journal: journal).GetAwaiter().GetResult();
            if (result.Status != (stage <= 2 ? "Blocked" : "Unverified") || result.CreateAttempted != (stage > 2) ||
                journal.Pending != (stage > 2) || host.Calls != stage ||
                !result.Detail.Contains(stage <= 2 ? "not dispatched" : "may have occurred"))
                throw new Exception("Provider exception lost registration uncertainty.");
        }
        for (int stage = 1; stage <= 4; stage++)
        {
            var stalled = new StalledHost(plan, stage);
            var journal = new Journal();
            var result = TaskRegistration.RunAsync(plan, stalled, true, journal: journal, timeout: TimeSpan.FromMilliseconds(30)).GetAwaiter().GetResult();
            if (result.Status != (stage <= 2 ? "Timed out" : "Unverified") || result.CreateAttempted != (stage > 2) || !result.Detail.Contains("timed out") ||
                stalled.Calls != stage || !stalled.Token.IsCancellationRequested || journal.Pending != (stage > 2))
                throw new Exception("Task deadline lost creation uncertainty or pending evidence.");
            stalled.Release.SetResult(true);
        }
        foreach (string scenario in new[] { "preview", "blocked", "wrong-target", "preflight-error", "final-blocked", "create-error", "verify-error", "create-failed", "truncated", "mismatch", "success" })
        {
            var host = new Host(plan, scenario);
            var result = TaskRegistration.RunAsync(plan, host, scenario != "preview", journal: new Journal()).GetAwaiter().GetResult();
            string expected = scenario switch { "preview" => "Preview", "blocked" or "wrong-target" or "preflight-error" or "final-blocked" => "Blocked", "success" => "Registered", _ => "Unverified" };
            if (result.Status != expected || host.Creates != (scenario is "preview" or "blocked" or "wrong-target" or "preflight-error" or "final-blocked" ? 0 : 1))
                throw new Exception("Registration coordinator failed: " + scenario);
        }
        foreach (string stage in new[] { "before", "preflight", "final-preflight", "create", "verify" })
        {
            using var stop = new CancellationTokenSource();
            var host = new InterruptedHost(plan, stage, stop);
            if (stage == "before") stop.Cancel();
            var result = TaskRegistration.RunAsync(plan, host, true, new Journal(), token: stop.Token).GetAwaiter().GetResult();
            bool attempted = stage is "create" or "verify";
            if (result.Status != (attempted ? "Unverified" : "Canceled") || result.CreateAttempted != attempted ||
                host.Creates != (attempted ? 1 : 0) || host.Reads != (stage == "verify" ? 1 : 0) ||
                (attempted && !result.Detail.Contains("may exist"))) throw new Exception("Registration interruption lost uncertainty: " + stage);
            int expectedEvidence = stage switch { "verify" => 3, "create" => 2, "final-preflight" => 1, _ => 0 };
            if (result.Evidence.Count != expectedEvidence)
                throw new Exception("Registration interruption lost completed evidence: " + stage);
        }
        foreach (string failure in new[] { "missing", "prepare", "complete", "prepare-null", "complete-null" })
        {
            var host = new Host(plan, "success");
            var journal = new Journal(failure);
            var result = TaskRegistration.RunAsync(plan, host, true, journal: failure == "missing" ? null : journal).GetAwaiter().GetResult();
            bool created = failure is "complete" or "complete-null";
            if (host.Creates != (created ? 1 : 0) || result.CreateAttempted != created ||
                result.Status != (created ? "Unverified" : "Blocked") || (created && !journal.Pending))
                throw new Exception("Registration journal failure did not preserve recovery: " + failure);
        }
        foreach (string failure in new[] { "abandon", "abandon-null" })
        {
            var host = new Host(plan, "final-blocked");
            var journal = new Journal(failure);
            var result = TaskRegistration.RunAsync(plan, host, true, journal: journal).GetAwaiter().GetResult();
            if (result.Status != "Blocked" || result.CreateAttempted || host.Creates != 0 || !journal.Pending)
                throw new Exception("Failed no-dispatch archival did not retain its conservative guard: " + failure);
        }
        string noDispatchRoot = Path.Combine(Path.GetTempPath(), "KillerBench-task-no-dispatch-" + Guid.NewGuid().ToString("N"));
        var noDispatchHost = new Host(plan, "final-blocked");
        var noDispatch = TaskRegistration.RunAsync(plan, noDispatchHost, true,
            journal: new FileTaskRegistrationJournal(noDispatchRoot)).GetAwaiter().GetResult();
        if (noDispatch.Status != "Blocked" || noDispatch.CreateAttempted || noDispatchHost.Creates != 0 ||
            Directory.GetFiles(noDispatchRoot, "*.pending.json").Length != 0 ||
            Directory.GetFiles(noDispatchRoot, "*.prepared.json").Length != 1 ||
            Directory.GetFiles(noDispatchRoot, "*.not-dispatched.json").Length != 1)
            throw new Exception("Changed final task eligibility did not produce an exact no-dispatch archive.");
        string noDispatchIntent = Directory.GetFiles(noDispatchRoot, "*.prepared.json").Single();
        string noDispatchOutcome = Directory.GetFiles(noDispatchRoot, "*.not-dispatched.json").Single();
        var noDispatchReview = TaskJournalReview.ReadPairAsync(noDispatchIntent, noDispatchOutcome).GetAwaiter().GetResult();
        if (noDispatchReview.State != "NotDispatched" || noDispatchReview.RecordedEvidence is null ||
            TaskJournalReview.ReadAsync(noDispatchOutcome).GetAwaiter().GetResult().State != "NotDispatched")
            throw new Exception("Linked no-dispatch task evidence could not be reviewed.");
        var afterNoDispatch = new Host(plan, "success");
        var retriedAfterNoDispatch = TaskRegistration.RunAsync(plan, afterNoDispatch, true,
            journal: new FileTaskRegistrationJournal(noDispatchRoot)).GetAwaiter().GetResult();
        if (retriedAfterNoDispatch.Status != "Registered" || afterNoDispatch.Creates != 1)
            throw new Exception("A closed no-dispatch record blocked a later explicit task registration attempt.");
        string journalRoot = Path.Combine(Path.GetTempPath(), "KillerBench-task-journal-" + Guid.NewGuid().ToString("N"));
        var durableHost = new Host(plan, "success");
        var durable = TaskRegistration.RunAsync(plan, durableHost, true, journal: new FileTaskRegistrationJournal(journalRoot)).GetAwaiter().GetResult();
        if (durable.Status != "Registered" || Directory.GetFiles(journalRoot).Length != 2)
            throw new Exception("Task journal did not retain intent and verified completion.");
        string intentPath = Directory.GetFiles(journalRoot, "*.pending.json").Single();
        string completionPath = Directory.GetFiles(journalRoot, "*.verified.json").Single();
        string malformedPath = Path.Combine(journalRoot, "malformed-fixture.json");
        foreach (string malformed in new[]
        {
            "{\"schemaVersion\":999999999999999999999999999999}",
            "{\"schemaVersion\":1,\"recordedUtc\":\"not a date\",\"state\":\"Pending\"}",
            "{\"schemaVersion\":1,\"schemaVersion\":1}",
            "null", "[]", "{\"schemaVersion\":1}",
            File.ReadAllText(intentPath).Replace("\"plan\":{", "\"plan\":null,\"discardedPlan\":{")
        })
        {
            File.WriteAllText(malformedPath, malformed);
            var malformedReview = TaskJournalReview.ReadAsync(malformedPath).GetAwaiter().GetResult();
            if (malformedReview.State != "Unreadable" || malformedReview.RecordedEvidence is not null)
                throw new Exception("Malformed recovery evidence escaped validation.");
        }
        using (var canceledRead = new CancellationTokenSource())
        {
            canceledRead.Cancel();
            try { TaskJournalReview.ReadAsync(intentPath, canceledRead.Token).GetAwaiter().GetResult(); throw new Exception("Canceled journal read proceeded."); }
            catch (OperationCanceledException) { }
        }
        if (TaskJournalReview.ReadPairAsync(intentPath, completionPath).GetAwaiter().GetResult().State != "DefinitionVerified")
            throw new Exception("Matching journal pair did not verify.");
        foreach (string sourcePath in new[] { intentPath, completionPath })
        {
            var conflicting = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(sourcePath))!;
            conflicting[sourcePath == intentPath ? "preflight" : "verification"]!["Evidence"]!["Error"] = "Conflicting provider error";
            File.WriteAllText(malformedPath, conflicting.ToJsonString());
            if (TaskJournalReview.ReadAsync(malformedPath).GetAwaiter().GetResult().State != "Unreadable" ||
                TaskJournalReview.ReadPairAsync(sourcePath == intentPath ? malformedPath : intentPath,
                    sourcePath == completionPath ? malformedPath : completionPath).GetAwaiter().GetResult().State != "Unverified")
                throw new Exception("Conflicting stored evidence verified a registration.");
        }
        foreach (string sourcePath in new[] { intentPath, completionPath })
        foreach (string invalidId in new[] { "null", "42", "true", "{}", "[]", "\"invalid\"", "\"00000000-0000-0000-0000-000000000000\"" })
        {
            var record = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(sourcePath))!;
            record["attemptId"] = System.Text.Json.Nodes.JsonNode.Parse(invalidId);
            File.WriteAllText(malformedPath, record.ToJsonString());
            var review = TaskJournalReview.ReadPairAsync(sourcePath == intentPath ? malformedPath : intentPath,
                sourcePath == completionPath ? malformedPath : completionPath).GetAwaiter().GetResult();
            if (review.State != "Unverified" || review.RecordedEvidence is not null)
                throw new Exception("Malformed attempt identity verified a recovery pair: " + invalidId);
        }
        string otherRoot = journalRoot + "-other-attempt";
        TaskRegistration.RunAsync(plan, new Host(plan, "success"), true,
            journal: new FileTaskRegistrationJournal(otherRoot)).GetAwaiter().GetResult();
        if (TaskJournalReview.ReadPairAsync(intentPath, Directory.GetFiles(otherRoot, "*.verified.json").Single()).GetAwaiter().GetResult().State != "Unverified")
            throw new Exception("Completion from a different attempt verified.");
        var reviews = new[] { intentPath, completionPath }.Select(path => TaskJournalReview.ReadAsync(path).GetAwaiter().GetResult()).ToArray();
        if (!reviews.Select(review => review.State).Order().SequenceEqual(ExpectedReviewStates) ||
            reviews.Any(review => review.RecordedEvidence is null)) throw new Exception("Journal review lost valid historical evidence.");
        foreach (var target in new[] { plan, plan with { Identity = new("\\fixture\\", "CREATE") } })
        {
            var retryHost = new Host(target, "success");
            var retry = TaskRegistration.RunAsync(target, retryHost, true, journal: new FileTaskRegistrationJournal(journalRoot)).GetAwaiter().GetResult();
            if (retry.Status != "Blocked" || retryHost.Creates != 0)
                throw new Exception("Task journal allowed a repeated target after reopening.");
        }
        string interruptedRoot = journalRoot + "-interrupted";
        var failed = TaskRegistration.RunAsync(plan, new Host(plan, "create-failed"), true,
            journal: new FileTaskRegistrationJournal(interruptedRoot)).GetAwaiter().GetResult();
        var afterFailure = new Host(plan, "success");
        var blocked = TaskRegistration.RunAsync(plan, afterFailure, true,
            journal: new FileTaskRegistrationJournal(interruptedRoot)).GetAwaiter().GetResult();
        if (failed.Status != "Unverified" || blocked.Status != "Blocked" || afterFailure.Creates != 0 || Directory.GetFiles(interruptedRoot).Length != 1)
            throw new Exception("Uncertain registration did not survive journal reopening.");
        string pendingFile = Directory.GetFiles(interruptedRoot).Single();
        File.WriteAllText(pendingFile, "{partial fixture");
        if (TaskJournalReview.ReadAsync(pendingFile).GetAwaiter().GetResult().State != "Unreadable")
            throw new Exception("Damaged journal did not report unreadable evidence.");
        var corruptHost = new Host(plan, "success");
        var corrupt = TaskRegistration.RunAsync(plan, corruptHost, true,
            journal: new FileTaskRegistrationJournal(interruptedRoot)).GetAwaiter().GetResult();
        if (corrupt.Status != "Blocked" || corruptHost.Creates != 0 || File.ReadAllText(pendingFile) != "{partial fixture")
            throw new Exception("Corrupt journal was overwritten or allowed creation.");

        string competingRoot = journalRoot + "-competing";
        var hosts = Enumerable.Range(0, 8).Select(_ => new Host(plan, "success")).ToArray();
        using var start = new ManualResetEventSlim(false);
        var attempts = hosts.Select(host => Task.Run(async () =>
        {
            start.Wait();
            return await TaskRegistration.RunAsync(plan, host, true,
                journal: new FileTaskRegistrationJournal(competingRoot));
        })).ToArray();
        start.Set();
        var outcomes = Task.WhenAll(attempts).GetAwaiter().GetResult();
        if (hosts.Sum(host => host.Creates) != 1 || outcomes.Count(result => result.Status == "Registered") != 1 ||
            outcomes.Count(result => result.Status == "Blocked") != 7 || Directory.GetFiles(competingRoot).Length != 2)
            throw new Exception("Competing journals did not restrict creation to one attempt.");
        Console.WriteLine("PASS: task journal restart, corrupt-record preservation and competing attempts; coordinator cancellation (substituted host only).");
    }

    private sealed class Journal(string failure = "") : ITaskRegistrationJournal
    {
        public bool Pending { get; private set; }
        public Task PrepareAsync(ScheduledTaskPlan plan, TaskRegistrationPreflight preflight, CancellationToken token)
        {
            if (failure == "prepare") throw new IOException("Fixture journal unavailable.");
            if (failure == "prepare-null") return null!;
            Pending = true;
            return Task.CompletedTask;
        }
        public Task CompleteAsync(TaskRegistrationVerification verification, CancellationToken token)
        {
            if (failure == "complete") throw new IOException("Fixture completion unavailable.");
            if (failure == "complete-null") return null!;
            Pending = false;
            return Task.CompletedTask;
        }
        public Task AbandonAsync(string detail, CancellationToken token)
        {
            if (failure == "abandon") throw new IOException("Fixture no-dispatch archive unavailable.");
            if (failure == "abandon-null") return null!;
            Pending = false;
            return Task.CompletedTask;
        }
    }

    private sealed class ImmediateProgress : IProgress<string>
    {
        internal List<string> Messages { get; } = [];
        public void Report(string value) => Messages.Add(value);
    }

    private sealed class ThrowingProgress : IProgress<string>
    {
        public void Report(string value) => throw new InvalidOperationException("Fixture display unavailable.");
    }

    private sealed class FailedHost(ScheduledTaskPlan plan, int stage, Exception failure) : ITaskRegistrationHost
    {
        internal int Calls;
        private static readonly ToolResult Good = new(0, "fixture", "", false);
        private Task<T> Next<T>(T value) => ++Calls == stage ? Task.FromException<T>(failure) : Task.FromResult(value);
        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token) =>
            Next(new TaskRegistrationPreflight(plan.Identity, true, "fixture", Good));
        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token) => Next(Good);
        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token) =>
            Next(new TaskRegistrationVerification(plan, Good));
    }

    private sealed class StalledHost(ScheduledTaskPlan plan, int stage) : ITaskRegistrationHost
    {
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Calls;
        internal CancellationToken Token;
        private static readonly ToolResult Good = new(0, "fixture", "", false);
        private async Task<T> NextAsync<T>(T result, CancellationToken token)
        {
            Token = token;
            if (++Calls == stage) await Release.Task;
            return result;
        }
        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token) =>
            NextAsync(new TaskRegistrationPreflight(plan.Identity, true, "fixture", Good), token);
        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token) => NextAsync(Good, token);
        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token) =>
            NextAsync(new TaskRegistrationVerification(plan, Good), token);
    }

    private sealed class Host(ScheduledTaskPlan plan, string scenario) : ITaskRegistrationHost
    {
        private static readonly ToolResult Good = new(0, "fixture", "", false);
        private int checks;
        public int Checks => checks;
        public int Creates { get; private set; }
        public int Reads { get; private set; }
        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token)
        {
            checks++;
            return Task.FromResult(new TaskRegistrationPreflight(
                scenario == "wrong-target" ? new("\\Other\\", "Create") : plan.Identity,
                scenario != "blocked" && !(scenario == "final-blocked" && checks == 2), "fixture",
                Good with { Error = scenario == "preflight-error" ? "Conflicting error" : "" }));
        }
        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token)
        {
            Creates++;
            return Task.FromResult(Good with { ExitCode = scenario == "create-failed" ? 1 : 0, Truncated = scenario == "truncated", Error = scenario == "create-error" ? "Conflicting error" : "" });
        }
        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token)
        {
            Reads++;
            return Task.FromResult(new TaskRegistrationVerification(
                scenario == "mismatch" ? plan with { ExecutionLimit = TimeSpan.FromMinutes(2) } : plan,
                Good with { Error = scenario == "verify-error" ? "Conflicting error" : "" }));
        }
    }

    private sealed class InterruptedHost(ScheduledTaskPlan plan, string stage, CancellationTokenSource stop) : ITaskRegistrationHost
    {
        private static readonly ToolResult Good = new(0, "completed fixture", "", false);
        private int checks;
        public int Creates { get; private set; }
        public int Reads { get; private set; }
        public Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan requested, CancellationToken token)
        {
            checks++;
            if (stage == "preflight" || stage == "final-preflight" && checks == 2)
            { stop.Cancel(); return Task.FromCanceled<TaskRegistrationPreflight>(token); }
            return Task.FromResult(new TaskRegistrationPreflight(plan.Identity, true, "fixture", Good));
        }
        public Task<ToolResult> CreateNewAsync(ScheduledTaskPlan requested, CancellationToken token)
        {
            Creates++;
            if (stage == "create") { stop.Cancel(); return Task.FromCanceled<ToolResult>(token); }
            if (stage == "create-return") stop.Cancel();
            return Task.FromResult(Good);
        }
        public Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token)
        {
            Reads++;
            stop.Cancel();
            if (stage == "verify-return") return Task.FromResult(new TaskRegistrationVerification(plan, Good));
            return Task.FromCanceled<TaskRegistrationVerification>(token);
        }
    }
}
