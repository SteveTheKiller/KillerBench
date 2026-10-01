namespace KillerBench.Engine.Storage;

public static class StorageOperationalCorrelation
{
    private static readonly string[] ServiceHistoryNames =
    [
        "Storage Service failure history",
        "Optimize Drives failure history",
        "Volume Shadow Copy failure history",
        "Software Shadow Copy Provider failure history"
    ];

    public static WorkflowCheck Check(WorkflowCheck capacity, WorkflowCheck dirtyBit, WorkflowCheck jobs,
        WorkflowCheck physicalDisks, WorkflowCheck events, IReadOnlyList<WorkflowCheck> serviceHistory,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(capacity);
        ArgumentNullException.ThrowIfNull(dirtyBit);
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(physicalDisks);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "Storage operational correlation";
        const string boundary = " This is a bounded operational review, not a surface test, filesystem proof, backup verification or failure prediction. No file, disk, pool, virtual disk, partition, volume, mount point, service, job, optimization, snapshot, policy or restart state changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var current = new[]
        {
            (Check: capacity, Name: "Drive-letter volume capacity"),
            (Check: dirtyBit, Name: "Volume dirty bit"),
            (Check: jobs, Name: "Storage jobs"),
            (Check: physicalDisks, Name: "Physical disk evidence correlation"),
            (Check: events, Name: "Storage event correlation")
        };
        if (current.Any(item => item.Check.Name != item.Name) || serviceHistory.Count != ServiceHistoryNames.Length ||
            serviceHistory.Select(item => item.Name).OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(ServiceHistoryNames.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) is false)
            return Unknown("Storage correlation inputs were missing, duplicated or carried an unexpected identity.");

        var all = current.Select(item => item.Check).Concat(serviceHistory).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed drive-letter capacity, the selected system-volume dirty bit, current Storage module jobs, physical-disk provider evidence, mapped storage events and {serviceHistory.Count} exact service histories. Current and historical evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before drawing a combined conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the detailed results together before choosing filesystem, capacity, hardware, service, job, optimization or restart action. Historical service failures do not establish a current storage fault." + boundary);
        return new(name, CheckState.Information, detail + "No correlated source requested review. Point-in-time provider evidence does not establish future disk, filesystem or application health." + boundary);
    }
}
