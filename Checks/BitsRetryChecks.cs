using KillerBench.Engine;

internal static class BitsRetryChecks
{
    private static readonly Guid JobId = Guid.Parse("7b757c02-3116-48b1-b616-d2ff9104f0b5");
    private static BitsInventoryResult Snapshot(string state, string owner = "Fixture", bool complete = true) => BitsDiagnostics.Parse(new(0,
        System.Text.Json.JsonSerializer.Serialize(new { scope = "CurrentAccount", complete, jobs = new[] { new { id = JobId, name = "Test", state, error = "", owner, errorCode = 0, bytesTotal = "100", bytesTransferred = "1" } } }), "", false));

    internal static async Task RunAsync()
    {
        var eligibleInventory = Snapshot("Error");
        var eligibleCheck = BitsRetry.ToWorkflowCheck(eligibleInventory, JobId, "Fixture");
        if (eligibleCheck is not { State: CheckState.Repairable, Repair: RepairKind.BitsJobRetry } ||
            !ReferenceEquals(eligibleCheck.Evidence, eligibleInventory.Evidence))
            throw new Exception("Exact errored transfer did not become guarded recipe eligibility.");
        var noSelection = BitsRetry.ToWorkflowCheck(Snapshot("Error"), null, "Fixture");
        if (noSelection is not { State: CheckState.Information, Repair: null })
            throw new Exception("Missing transfer selection did not remain informational.");
        var activeSelection = BitsRetry.ToWorkflowCheck(Snapshot("Queued"), JobId, "Fixture");
        if (activeSelection is not { State: CheckState.Attention, Repair: null })
            throw new Exception("Active transfer incorrectly authorized retry.");
        var incompleteSelection = BitsRetry.ToWorkflowCheck(Snapshot("Error", complete: false), JobId, "Fixture");
        if (incompleteSelection is not { State: CheckState.Unknown, Repair: null })
            throw new Exception("Incomplete transfer inventory classified exact retry eligibility.");
        foreach (var pair in new[] { ("Queued", BitsRetryOutcome.RetryRequested), ("Transferring", BitsRetryOutcome.RetryRequested),
            ("Transferred", BitsRetryOutcome.TransferReported), ("Error", BitsRetryOutcome.Failed), ("Suspended", BitsRetryOutcome.Uncertain) })
        {
            var journal = new Journal();
            var host = new Host(journal, Snapshot(pair.Item1));
            var progress = new Observer();
            var result = await BitsRetry.RunAsync(Snapshot("TransientError"), JobId, host, journal, progress: progress);
            if (progress.Messages.Count != 8 || progress.Messages[^1] != result.Detail || !progress.Messages[4].Contains(JobId.ToString()))
                throw new Exception("Retry progress is missing or out of order.");
            if (result.Outcome != pair.Item2 || host.Requests != 1 || journal.Result != result || !journal.Began)
                throw new Exception("Retry outcome or journal ordering failed.");
        }
        foreach (var selection in new[] { Snapshot("Suspended"), Snapshot("Queued"), Snapshot("Error", "Other"), Snapshot("Error", complete: false) })
        {
            var journal = new Journal();
            var host = new Host(journal, Snapshot("Queued"));
            if ((await BitsRetry.RunAsync(selection, JobId, host, journal)).Outcome != BitsRetryOutcome.Blocked || host.Reads != 0 || host.Requests != 0 || journal.Began)
                throw new Exception("Ineligible transfer reached retry provider.");
        }
        var changedJournal = new Journal();
        foreach (var failure in new Exception?[] { null, new TimeoutException("Fixture read timeout"),
            new UnauthorizedAccessException("Fixture read denied"), new System.IO.IOException("Fixture read failure") })
        {
            var preflightJournal = new Journal();
            var preflightHost = new Host(preflightJournal, Snapshot("Queued")) { FailPreflight = true, Failure = failure };
            var preflightResult = await BitsRetry.RunAsync(Snapshot("Error"), JobId, preflightHost, preflightJournal);
            if (preflightResult.Outcome != BitsRetryOutcome.Blocked || preflightHost.Requests != 0 ||
                preflightHost.Reads != 1 || preflightJournal.Began || preflightJournal.Result is not null ||
                !preflightResult.Detail.Contains("historical evidence only"))
                throw new Exception("Failed fresh transfer check reached repair preparation or misrepresented old evidence.");
        }
        foreach (bool canceled in new[] { false, true })
        {
            var finalJournal = new Journal();
            var finalHost = new Host(finalJournal, Snapshot("Queued"))
            {
                FinalFailure = canceled ? new OperationCanceledException("fixture final cancellation") :
                    new System.IO.InvalidDataException("fixture final inspection failure")
            };
            var finalResult = await BitsRetry.RunAsync(Snapshot("Error"), JobId, finalHost, finalJournal);
            if (finalResult.Outcome != (canceled ? BitsRetryOutcome.Stopped : BitsRetryOutcome.Blocked) ||
                finalHost.Requests != 0 || finalJournal.Result != finalResult || !finalJournal.Abandoned)
                throw new Exception("Unavailable final transfer evidence did not close its no-request preparation.");
        }
        var finalChangedJournal = new Journal();
        var finalChangedHost = new Host(finalChangedJournal, Snapshot("Queued")) { Final = Snapshot("Queued") };
        var finalChanged = await BitsRetry.RunAsync(Snapshot("Error"), JobId, finalChangedHost, finalChangedJournal);
        if (finalChanged.Outcome != BitsRetryOutcome.Blocked || finalChangedHost.Requests != 0 ||
            finalChangedJournal.Result != finalChanged || !finalChangedJournal.Abandoned)
            throw new Exception("Changed final transfer evidence reached retry dispatch.");
        foreach (var failure in new Exception[] { new UnauthorizedAccessException("Fixture access denied"),
            new System.IO.InvalidDataException("Fixture invalid evidence"), new NotSupportedException("Fixture unavailable provider"),
            new System.Security.SecurityException("Fixture security failure"), new TimeoutException("Fixture provider timeout") })
        foreach (bool duringObservation in new[] { false, true })
        {
            var failedJournal = new Journal();
            var failedHost = new Host(failedJournal, Snapshot("Queued")) { Failure = failure, FailObservation = duringObservation };
            var failedResult = await BitsRetry.RunAsync(Snapshot("Error"), JobId, failedHost, failedJournal);
            if (failedResult.Outcome != BitsRetryOutcome.Uncertain || failedJournal.Result != failedResult || failedHost.Requests != 1 ||
                (failedResult.Request is not null) != duringObservation || !failedResult.Detail.Contains(failure.Message))
                throw new Exception("Provider failure after preparation lost its uncertain journal outcome.");
        }
        foreach (bool duringObservation in new[] { false, true })
        {
            var missingJournal = new Journal();
            var missingHost = new Host(missingJournal, Snapshot("Queued")) { MissingEvidence = true, FailObservation = duringObservation };
            var missingResult = await BitsRetry.RunAsync(Snapshot("Error"), JobId, missingHost, missingJournal);
            if (missingResult.Outcome != BitsRetryOutcome.Uncertain || missingJournal.Result != missingResult ||
                missingHost.Requests != 1 || (missingResult.Request is not null) != duringObservation)
                throw new Exception("Missing provider evidence lost the uncertain retry outcome.");
        }
        var throwingJournal = new Journal();
        var throwingResult = await BitsRetry.RunAsync(Snapshot("Error"), JobId, new Host(throwingJournal, Snapshot("Queued")), throwingJournal, progress: new Observer { Throw = true });
        if (throwingJournal.Result != throwingResult || throwingResult.Outcome != BitsRetryOutcome.RetryRequested)
            throw new Exception("Presentation failure interrupted outcome persistence.");
        var changed = new Host(changedJournal, Snapshot("Queued")) { Before = Snapshot("Suspended") };
        if ((await BitsRetry.RunAsync(Snapshot("Error"), JobId, changed, changedJournal)).Outcome != BitsRetryOutcome.Blocked || changed.Requests != 0)
            throw new Exception("Retry ignored changed state.");
        var interruptedJournal = new Journal();
        var interrupted = new Host(interruptedJournal, Snapshot("Queued")) { Interrupt = true };
        if ((await BitsRetry.RunAsync(Snapshot("Error"), JobId, interrupted, interruptedJournal)).Outcome != BitsRetryOutcome.Uncertain || interruptedJournal.Result?.Outcome != BitsRetryOutcome.Uncertain)
            throw new Exception("Interrupted request claimed success or lost uncertainty.");
        var deniedJournal = new Journal { Deny = true };
        var denied = new Host(deniedJournal, Snapshot("Queued"));
        try { await BitsRetry.RunAsync(Snapshot("Error"), JobId, denied, deniedJournal); throw new Exception("Journal rejection ignored."); }
        catch (System.IO.IOException) { }
        if (denied.Requests != 0) throw new Exception("Retry started without journal preparation.");
        var changedOwnerJournal = new Journal();
        using (var stopDuringBegin = new CancellationTokenSource())
        {
            var beginJournal = new Journal { StopDuringBegin = stopDuringBegin };
            var beginHost = new Host(beginJournal, Snapshot("Queued"));
            var beginStopped = await BitsRetry.RunAsync(Snapshot("Error"), JobId, beginHost, beginJournal, token: stopDuringBegin.Token);
            if (beginStopped.Outcome != BitsRetryOutcome.Stopped || !beginJournal.Abandoned || beginHost.Requests != 0)
                throw new Exception("Cancellation during retry preparation left the retry guard set.");
        }
        var hungJournal = new Journal { HangCompletion = true };
        var hungHost = new Host(hungJournal, Snapshot("Queued"));
        var hungCompletion = await BitsRetry.RunAsync(Snapshot("Error"), JobId, hungHost, hungJournal);
        if (hungCompletion.Outcome != BitsRetryOutcome.RetryRequested || hungHost.Requests != 1 ||
            !hungCompletion.Detail.Contains("journal completion unconfirmed", StringComparison.Ordinal))
            throw new Exception("A slow journal completion discarded the observed retry result.");
        var timedOutJournal = new Journal { CompletionFailure = new TimeoutException("Fixture result write timed out") };
        var timedOutHost = new Host(timedOutJournal, Snapshot("Queued"));
        var timedOutCompletion = await BitsRetry.RunAsync(Snapshot("Error"), JobId, timedOutHost, timedOutJournal);
        if (!timedOutJournal.Began || timedOutJournal.Result is not null || timedOutHost.Requests != 1 ||
            !timedOutCompletion.Detail.Contains("journal completion unconfirmed", StringComparison.Ordinal))
            throw new Exception("A timed-out journal completion lost the observed result or reported the guard as cleared.");
        foreach (var persistenceFailure in new Exception[] { new System.IO.IOException("Fixture result write failed") })
        {
            var persistenceJournal = new Journal { CompletionFailure = persistenceFailure };
            var persistenceHost = new Host(persistenceJournal, Snapshot("Queued"));
            try
            {
                await BitsRetry.RunAsync(Snapshot("Error"), JobId, persistenceHost, persistenceJournal);
                throw new Exception("Failed outcome persistence was reported as complete.");
            }
            catch (Exception error) when (ReferenceEquals(error, persistenceFailure)) { }
            if (!persistenceJournal.Began || persistenceJournal.Result is not null ||
                persistenceHost.Requests != 1 || persistenceHost.Reads != 3)
                throw new Exception("Persistence failure changed retry sequencing or lost pending state.");
        }
        var changedOwner = new Host(changedOwnerJournal, Snapshot("Transferred", "Other"));
        if ((await BitsRetry.RunAsync(Snapshot("Error"), JobId, changedOwner, changedOwnerJournal)).Outcome != BitsRetryOutcome.Uncertain)
            throw new Exception("Changed owner verified a different transfer.");
        Console.WriteLine("PASS: exact-job retry sequencing, owner/state guards, fresh recheck, pending journal and distinct request/transfer outcomes; simulated providers only.");
    }

    private sealed class Observer : IProgress<string>
    {
        public readonly List<string> Messages = [];
        public bool Throw;
        public void Report(string value) { if (Throw) throw new InvalidOperationException("Fixture presentation failure"); Messages.Add(value); }
    }

    private sealed class Journal : IBitsRetryJournal
    {
        public bool Began;
        public bool Deny;
        public bool Abandoned;
        public BitsRetryResult? Result;
        public Exception? CompletionFailure;
        public CancellationTokenSource? StopDuringBegin;
        public bool HangCompletion;
        public Task BeginAsync(Guid jobId, BitsInventoryResult before, CancellationToken token)
        {
            if (Deny) throw new System.IO.IOException("Fixture journal blocked");
            Began = true;
            if (StopDuringBegin is null) return Task.CompletedTask;
            StopDuringBegin.Cancel();
            return Task.Delay(Timeout.Infinite, token);
        }
        public Task CompleteAsync(BitsRetryResult result, CancellationToken token)
        {
            if (HangCompletion) return new TaskCompletionSource().Task;
            if (CompletionFailure is not null) throw CompletionFailure;
            Result = result;
            return Task.CompletedTask;
        }
        public Task AbandonAsync(BitsRetryResult result, CancellationToken token)
        {
            if (CompletionFailure is not null) throw CompletionFailure;
            Abandoned = true;
            Result = result;
            return Task.CompletedTask;
        }
    }
    private sealed class Host(Journal journal, BitsInventoryResult after) : IBitsRetryHost
    {
        public string OwnerAccount => "Fixture";
        public BitsInventoryResult Before = Snapshot("Error");
        public int Reads, Requests;
        public bool Interrupt;
        public Exception? Failure;
        public bool FailObservation;
        public bool MissingEvidence;
        public bool FailPreflight;
        public BitsInventoryResult? Final;
        public Exception? FinalFailure;
        public Task<BitsInventoryResult> ReadAsync(CancellationToken token)
        {
            Reads++;
            if (Reads == 1 && FailPreflight)
            {
                if (Failure is not null) throw Failure;
                return Task.FromResult<BitsInventoryResult>(null!);
            }
            if (Reads == 2 && FinalFailure is not null) throw FinalFailure;
            if (Reads > 2 && FailObservation && MissingEvidence) return Task.FromResult<BitsInventoryResult>(null!);
            if (Reads > 2 && FailObservation && Failure is not null) throw Failure;
            return Task.FromResult(Reads == 1 ? Before : Reads == 2 ? Final ?? Before : after);
        }
        public Task<ToolResult> RequestRetryAsync(BitsJobSnapshot expected, CancellationToken token)
        {
            if (!journal.Began || expected.Id != JobId) throw new Exception("Retry preceded durable preparation or used wrong target.");
            Requests++;
            if (!FailObservation && MissingEvidence) return Task.FromResult<ToolResult>(null!);
            if (!FailObservation && Failure is not null) throw Failure;
            if (Interrupt) throw new OperationCanceledException("Simulated interruption");
            return Task.FromResult(new ToolResult(0, "Fixture retry accepted", "", false));
        }
    }
}
