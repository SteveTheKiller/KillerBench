namespace KillerBench.Engine;

public sealed record PrintQueueRepairAssessment(WorkflowCheck Check, IReadOnlyList<string> Targets);

public static class PrintQueueRepairEligibility
{
    private const uint Paused = 1;
    private const uint Spooling = 8;
    private const uint Deleting = 4;
    private const uint Printed = 128;
    private const uint Deleted = 256;

    public static PrintQueueRepairAssessment Assess(ToolResult queueEvidence, ToolResult jobEvidence)
    {
        var queues = PrintRecovery.ReadIdentities(queueEvidence);
        var jobs = PrintJobDiagnostics.ReadInventory(jobEvidence);
        if (queues is null || jobs is null || !jobs.Complete)
            return new(new("Print queues", CheckState.Unknown,
                "Complete local queue and print-job evidence is required before selecting a queue resume. No resume selected.",
                Evidence: queueEvidence), []);
        if (queues.PausedNames.Length == 0)
            return new(new("Print queues", CheckState.Clear,
                $"{queues.Names.Length} local queues checked; none paused. This does not verify physical printing.",
                Evidence: queueEvidence), []);

        var targets = queues.PausedNames.Where(queue => jobs.Jobs.Any(job =>
            string.Equals(job.QueueName, queue, StringComparison.OrdinalIgnoreCase) && IsWaiting(job.StatusMask)))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (targets.Length == 0)
            return new(new("Print queues", CheckState.Attention,
                $"{queues.PausedNames.Length} local queues are paused, but no exact paused queue has a visible waiting job. The pause may be intentional, so automatic resume is unavailable.",
                Evidence: queueEvidence), []);

        int reviewOnly = queues.PausedNames.Length - targets.Length;
        string detail = $"{targets.Length} paused local queues have exact visible waiting-job evidence and are eligible for guarded resume." +
            (reviewOnly > 0 ? $" {reviewOnly} other paused queues remain review-only because no qualifying waiting job was visible." : "") +
            " Only the retained exact queue identities may be resumed; jobs are preserved and physical printing remains unverified.";
        return new(new("Print queues", CheckState.Repairable, detail, RepairKind.PrintQueues, queueEvidence), Array.AsReadOnly(targets));
    }

    private static bool IsWaiting(uint mask) => mask != 0 && (mask & (Paused | Spooling)) != 0 &&
        (mask & (Deleting | Printed | Deleted)) == 0;
}
