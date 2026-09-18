namespace KillerBench.Engine;

public enum BitsRetryOutcome { Blocked, RetryRequested, TransferReported, Failed, Uncertain, Stopped }
public sealed record BitsRetryResult(Guid JobId, BitsRetryOutcome Outcome, string Detail, BitsInventoryResult Before,
    ToolResult? Request = null, BitsInventoryResult? After = null);

public interface IBitsRetryHost
{
    string OwnerAccount { get; }
    Task<BitsInventoryResult> ReadAsync(CancellationToken token);
    // The provider must recheck the exact job and owner before requesting an asynchronous retry.
    Task<ToolResult> RequestRetryAsync(BitsJobSnapshot expected, CancellationToken token);
}

public interface IBitsRetryJournal
{
    // Persist a pending operation and reject unresolved attempts for this job before returning.
    Task BeginAsync(Guid jobId, BitsInventoryResult before, CancellationToken token);
    Task AbandonAsync(BitsRetryResult result, CancellationToken token);
    Task CompleteAsync(BitsRetryResult result, CancellationToken token);
}

public static class BitsRetry
{
    public static WorkflowCheck ToWorkflowCheck(BitsInventoryResult inventory, Guid? jobId, string owner,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (jobId is null || jobId == Guid.Empty)
            return new("BITS job retry", CheckState.Information,
                "Select one exact running-account transfer to evaluate guarded retry. No transfer action was selected.",
                Evidence: inventory.Evidence);
        if (!inventory.Complete || inventory.Scope != "CurrentAccount")
            return new("BITS job retry", CheckState.Unknown,
                "Complete running-account transfer evidence is required before retry eligibility can be determined. No retry is eligible.",
                Evidence: inventory.Evidence);
        var job = inventory.Jobs.SingleOrDefault(candidate => candidate.Id == jobId.Value);
        if (job is null)
            return new("BITS job retry", CheckState.Attention,
                "The selected transfer is not present in the complete current running-account inventory. No retry is eligible.",
                Evidence: inventory.Evidence);
        if (!IsEligible(inventory, jobId.Value, owner, token))
            return new("BITS job retry", CheckState.Attention,
                "The selected transfer is not an exact errored running-account job with complete owner and progress evidence. Suspended, active, completed and changed jobs are preserved.",
                Evidence: inventory.Evidence);
        return new("BITS job retry", CheckState.Repairable,
            "The selected exact errored running-account transfer is eligible for a guarded retry request after separate confirmation. Transfer completion and owning-application acknowledgment remain unverified.",
            RepairKind.BitsJobRetry, inventory.Evidence);
    }

    public static async Task<BitsRetryResult> RunAsync(BitsInventoryResult selectedInventory, Guid jobId,
        IBitsRetryHost host, IBitsRetryJournal journal, CancellationToken token = default, IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(selectedInventory);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(journal);
        if (jobId == Guid.Empty) throw new ArgumentException("An exact transfer ID is required.", nameof(jobId));
        token.ThrowIfCancellationRequested();
        Report(progress, "Checking selected transfer evidence.");
        var selected = BitsDiagnostics.Parse(selectedInventory.Evidence, token);
        var target = Eligible(selected, jobId, host.OwnerAccount);
        if (target is null) return new(jobId, BitsRetryOutcome.Blocked, "An errored job owned by the running account, complete evidence and owner metadata are required. Suspended jobs are preserved.", selected);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        Report(progress, "Rechecking the selected transfer on this machine.");
        BitsInventoryResult before;
        try
        {
            var observed = await host.ReadAsync(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            if (observed?.Evidence is null) throw new InvalidDataException("Fresh transfer evidence is missing.");
            before = BitsDiagnostics.Parse(observed.Evidence, deadline.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return new(jobId, BitsRetryOutcome.Blocked, "Fresh transfer check timed out. No retry requested; retained selection is historical evidence only.", selected);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or System.Security.SecurityException or InvalidOperationException or TimeoutException or
            System.ComponentModel.Win32Exception)
        {
            return new(jobId, BitsRetryOutcome.Blocked, "Fresh transfer check unavailable: " + error.Message +
                ". No retry requested; retained selection is historical evidence only.", selected);
        }
        var current = Eligible(before, jobId, host.OwnerAccount);
        if (current is null || current.Owner != target.Owner || current.Name != target.Name || current.BytesTotal != target.BytesTotal)
            return new(jobId, BitsRetryOutcome.Blocked, "The selected job changed, is unavailable or is no longer eligible. No retry requested.", before);
        Report(progress, "Saving retry preparation and checking for prior attempts.");
        await journal.BeginAsync(jobId, before, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        ToolResult? request = null;
        BitsRetryResult result;
        bool requestDispatched = false;
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            Report(progress, "Repeating exact transfer eligibility immediately before the retry request.");
            var finalRaw = await host.ReadAsync(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            if (finalRaw?.Evidence is null) throw new InvalidDataException("Final transfer evidence is missing.");
            var final = BitsDiagnostics.Parse(finalRaw.Evidence, deadline.Token);
            var finalJob = Eligible(final, jobId, host.OwnerAccount);
            if (finalJob is null || finalJob.Owner != current.Owner || finalJob.Name != current.Name || finalJob.BytesTotal != current.BytesTotal)
            {
                result = new(jobId, BitsRetryOutcome.Blocked,
                    "The exact transfer changed or is no longer eligible after preparation. No retry was requested.",
                    before, After: final);
                await journal.AbandonAsync(result, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Requesting an asynchronous retry for " + jobId + ".");
            requestDispatched = true;
            request = await host.RequestRetryAsync(finalJob, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            if (request is null) throw new InvalidDataException("Retry provider returned no request evidence.");
            if (request.TimedOut || request.Truncated)
                result = new(jobId, BitsRetryOutcome.Uncertain, "Retry request evidence is incomplete. Reconcile the job before another request.", before, request);
            else if (request.ExitCode != 0 || !string.IsNullOrWhiteSpace(request.Error))
                result = new(jobId, BitsRetryOutcome.Failed, "The provider did not confirm the retry request. Review the retained error; transfer state remains unverified.", before, request);
            else
            {
                Report(progress, "Observing transfer state after the request.");
                var afterRaw = await host.ReadAsync(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
                if (afterRaw?.Evidence is null) throw new InvalidDataException("Retry observation returned no evidence.");
                var after = BitsDiagnostics.Parse(afterRaw.Evidence, deadline.Token);
                var job = after.Jobs.SingleOrDefault(item => item.Id == jobId);
                bool same = after.Complete && after.Scope == "CurrentAccount" && job?.Owner == current.Owner && job?.Name == current.Name && job?.BytesTotal == current.BytesTotal;
                var outcome = !same ? BitsRetryOutcome.Uncertain : job!.State switch
                {
                    "Queued" or "Connecting" or "Transferring" => BitsRetryOutcome.RetryRequested,
                    "Transferred" => BitsRetryOutcome.TransferReported,
                    "Error" or "TransientError" => BitsRetryOutcome.Failed,
                    _ => BitsRetryOutcome.Uncertain
                };
                result = new(jobId, outcome,
                    outcome == BitsRetryOutcome.TransferReported ? "BITS reports transfer completed. Owning-application acknowledgment and destination use remain unverified; the job was not completed or removed." :
                    outcome == BitsRetryOutcome.RetryRequested ? "BITS reports the job queued, connecting or transferring after the request. Transfer completion remains unverified." :
                    "The retry did not establish a confirmed active transfer. Review the current job and owning application before retrying.", before, request, after);
            }
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or InvalidDataException or
            UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException or
            InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            if (!requestDispatched)
            {
                bool stopped = error is OperationCanceledException;
                result = new(jobId, stopped ? BitsRetryOutcome.Stopped : BitsRetryOutcome.Blocked,
                    stopped ? "BITS retry stopped after durable preparation and before any retry request was sent." :
                    "Final transfer eligibility could not be established: " + error.Message + ". No retry was requested.",
                    before);
                await journal.AbandonAsync(result, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                Report(progress, result.Detail);
                return result;
            }
            result = new(jobId, BitsRetryOutcome.Uncertain, "Request or observation interrupted: " + error.Message + ". A retry may still be active; reconcile before another request.", before, request);
        }
        Report(progress, "Saving the observed outcome. The retry guard remains in place.");
        await journal.CompleteAsync(result, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Report(progress, result.Detail);
        return result;
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        // Presentation failures must not interrupt persistence after a transfer request.
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }

    public static bool IsEligible(BitsInventoryResult inventory, Guid id, string owner, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return id != Guid.Empty && Eligible(BitsDiagnostics.Parse(inventory.Evidence, token), id, owner) is not null;
    }

    private static BitsJobSnapshot? Eligible(BitsInventoryResult inventory, Guid id, string owner)
    {
        if (!inventory.Complete || inventory.Scope != "CurrentAccount" || string.IsNullOrWhiteSpace(owner)) return null;
        return inventory.Jobs.SingleOrDefault(job => job.Id == id && job.Owner == owner && job.ErrorCode is not null &&
            job.BytesTransferred is not null && job.State is "Error" or "TransientError");
    }
}
