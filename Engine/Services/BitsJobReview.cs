namespace KillerBench.Engine.Services;

public sealed record BitsJobReviewResult(Guid JobId, string? Scope, bool Complete, BitsJobSnapshot? Selected, IReadOnlyList<string> Notes);
public sealed record BitsJobComparison(Guid JobId, bool Comparable, ulong? BytesAdvanced, string Detail,
    BitsJobReviewResult Before, BitsJobReviewResult After);

public static class BitsJobReview
{
    public static BitsJobComparison Compare(BitsInventoryResult before, BitsInventoryResult after, Guid jobId,
        CancellationToken token = default)
    {
        var first = Review(before, jobId, token);
        var second = Review(after, jobId, token);
        var oldJob = first.Selected;
        var newJob = second.Selected;
        if (!first.Complete || !second.Complete || first.Scope != second.Scope || oldJob is null || newJob is null ||
            string.IsNullOrWhiteSpace(oldJob.Owner) || oldJob.Owner != newJob.Owner || oldJob.Name != newJob.Name ||
            oldJob.BytesTotal != newJob.BytesTotal || oldJob.BytesTransferred is null || newJob.BytesTransferred is null ||
            newJob.BytesTransferred < oldJob.BytesTransferred)
            return new(jobId, false, null, "Incomplete evidence or changed job metadata prevents a reliable progress comparison. No retry is authorized.", first, second);
        ulong advanced = newJob.BytesTransferred.Value - oldJob.BytesTransferred.Value;
        return new(jobId, true, advanced, advanced > 0
            ? "Transferred byte count increased between the supplied snapshots. Completion and destination use remain unverified."
            : "No byte-count increase between the supplied snapshots. This does not establish a stalled transfer or authorize a retry.", first, second);
    }

    public static BitsJobReviewResult Review(BitsInventoryResult inventory, Guid jobId, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (jobId == Guid.Empty) throw new ArgumentException("An exact job ID is required.", nameof(jobId));
        var observed = BitsDiagnostics.Parse(inventory.Evidence, token);
        var selected = observed.Jobs.SingleOrDefault(job => job.Id == jobId);
        var notes = new List<string>();
        if (!observed.Complete) notes.Add("Inventory coverage is incomplete. Recollect before considering changes.");
        if (selected is null) notes.Add("The exact job was not found in retained evidence. Its absence does not establish completion or deletion.");
        else
        {
            notes.Add(selected.State switch
            {
                "Suspended" => "This job is suspended, which may be intentional. Do not automatically resume it.",
                "Error" or "TransientError" => "Review the reported error and the owning application's retry policy. One snapshot does not establish that a retry will fix the problem.",
                "Transferred" => "BITS reports files transferred. Application completion and use of the destination files remain unverified.",
                _ => "The reported state does not establish a stalled job or require a retry."
            });
            if (selected.Owner is null) notes.Add("Owner and transfer metadata are missing from this saved evidence.");
        }
        notes.Add("Account ownership and job name do not identify the controlling application or authorize changes. This review does not resume, complete, cancel, delete or take ownership of a job.");
        return new(jobId, observed.Scope, observed.Complete, selected, notes.AsReadOnly());
    }
}
