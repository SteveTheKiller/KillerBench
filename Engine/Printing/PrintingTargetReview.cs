namespace KillerBench.Engine.Printing;

public sealed record PrintingTargetSelection(string? Queue, string? JobQueue, uint? JobId);

public static class PrintingTargetReview
{
    public static IReadOnlyList<WorkflowCheck> Check(PrintingTargetSelection selection,
        IReadOnlyList<WorkflowCheck> conditions, IReadOnlyList<WorkflowCheck> attributes,
        PrintJobInventory? jobs)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(attributes);
        var results = new List<WorkflowCheck>();
        if (selection.Queue is not null)
            results.Add(ReviewQueue(DefaultPrinterSelection.ValidateQueue(selection.Queue), conditions, attributes));
        if (selection.JobQueue is not null || selection.JobId is not null)
        {
            if (selection.JobQueue is null || selection.JobId is null or 0)
                throw new ArgumentException("A selected print job requires one exact queue and nonzero job ID.", nameof(selection));
            results.Add(ReviewJob(PrintJobCancellation.ValidateQueue(selection.JobQueue), selection.JobId.Value, jobs));
        }
        return results.AsReadOnly();
    }

    private static WorkflowCheck ReviewQueue(string queue, IReadOnlyList<WorkflowCheck> conditions,
        IReadOnlyList<WorkflowCheck> attributes)
    {
        WorkflowCheck[] conditionMatches = Exact(conditions, "Printer condition: ", queue);
        WorkflowCheck[] attributeMatches = Exact(attributes, "Printer queue attributes: ", queue);
        if (conditionMatches.Length > 1 || attributeMatches.Length > 1)
            return new("Selected printer queue", CheckState.Unknown,
                "Current provider evidence contains duplicate rows for the exact selected queue. Refresh the queue evidence before requesting a change.");
        WorkflowCheck? condition = conditionMatches.SingleOrDefault();
        WorkflowCheck? attribute = attributeMatches.SingleOrDefault();
        bool incomplete = (condition is null && conditions.Any(item => item.EffectiveState == CheckState.Unknown)) ||
            (attribute is null && attributes.Any(item => item.EffectiveState == CheckState.Unknown));
        if (incomplete)
            return new("Selected printer queue", CheckState.Unknown,
                "Complete condition and attribute evidence is unavailable for the exact selected queue. No queue change was requested.");
        if (condition is null && attribute is null)
            return new("Selected printer queue", CheckState.Attention,
                "The exact selected queue was not present in current local condition or attribute evidence. Refresh the queue selection before requesting a change.");
        CheckState state = new[] { condition?.EffectiveState, attribute?.EffectiveState }.Contains(CheckState.Unknown)
            ? CheckState.Unknown
            : new[] { condition?.EffectiveState, attribute?.EffectiveState }.Contains(CheckState.Attention)
                ? CheckState.Attention : CheckState.Information;
        return new("Selected printer queue", state,
            "Exact queue: " + queue + ". " +
            (condition?.Detail ?? "No separate condition row was returned.") + " " +
            (attribute?.Detail ?? "No separate attribute row was returned.") +
            " This target review does not authorize or perform a printer change.");
    }

    private static WorkflowCheck ReviewJob(string queue, uint jobId, PrintJobInventory? inventory)
    {
        if (inventory is not { Complete: true })
            return new("Selected print job", CheckState.Unknown,
                "Complete visible-job evidence is unavailable for the exact selected job. No job was changed.");
        PrintJobSnapshot[] matches = [.. inventory.Jobs.Where(job => job.JobId == jobId &&
            job.QueueName.Equals(queue, StringComparison.OrdinalIgnoreCase))];
        if (matches.Length != 1)
            return new("Selected print job", CheckState.Attention,
                matches.Length == 0
                    ? "The exact selected print job is no longer visible. Refresh the selection before requesting cancellation."
                    : "The exact selected print-job identity is ambiguous. No cancellation is eligible.");
        PrintJobSnapshot job = matches[0];
        bool candidate = PrintJobCancellation.IsCandidate(job);
        return new("Selected print job", candidate ? CheckState.Attention : CheckState.Information,
            $"Exact queue: {queue}; job ID: {jobId}; status mask: {job.StatusMask}. " +
            (candidate
                ? "The job is currently Error or Blocked and inactive. A separate guarded cancellation request would repeat exact evidence before changing it."
                : "The job does not currently meet the exact inactive Error or Blocked cancellation boundary.") +
            " This review does not cancel, resume, purge or otherwise change a job or queue.");
    }

    private static WorkflowCheck[] Exact(IReadOnlyList<WorkflowCheck> checks, string prefix, string queue) =>
        [.. checks.Where(item => item.Name.StartsWith(prefix, StringComparison.Ordinal) &&
            item.Name[prefix.Length..].Equals(queue, StringComparison.OrdinalIgnoreCase)).Take(2)];
}
