namespace KillerBench.Engine.Storage;

public static class StorageEnvironmentCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> spaces, WorkflowCheck shadowStorage,
        WorkflowCheck selfHealing, IReadOnlyList<WorkflowCheck> deleteNotifications, WorkflowCheck pageFile,
        WorkflowCheck quotas, WorkflowCheck storageSensePolicy, WorkflowCheck storageSenseTask,
        WorkflowCheck cleanupTask, WorkflowCheck diskDiagnosticHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(spaces);
        ArgumentNullException.ThrowIfNull(shadowStorage);
        ArgumentNullException.ThrowIfNull(selfHealing);
        ArgumentNullException.ThrowIfNull(deleteNotifications);
        ArgumentNullException.ThrowIfNull(pageFile);
        ArgumentNullException.ThrowIfNull(quotas);
        ArgumentNullException.ThrowIfNull(storageSensePolicy);
        ArgumentNullException.ThrowIfNull(storageSenseTask);
        ArgumentNullException.ThrowIfNull(cleanupTask);
        ArgumentNullException.ThrowIfNull(diskDiagnosticHistory);
        token.ThrowIfCancellationRequested();

        bool exactSpaces = spaces.Count == 2 && spaces.Count(item => item.Name == "Storage pools") == 1 &&
            spaces.Count(item => item.Name == "Storage virtual disks") == 1;
        bool exactDelete = deleteNotifications.Count is >= 1 and <= 2 && deleteNotifications.All(item =>
            item.Name is "NTFS delete notifications" or "ReFS delete notifications" or "Filesystem delete notifications") &&
            deleteNotifications.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == deleteNotifications.Count;
        if (!exactSpaces || !exactDelete || shadowStorage.Name != "VSS shadow storage" ||
            !selfHealing.Name.StartsWith("NTFS self-healing: ", StringComparison.Ordinal) || pageFile.Name != "Paging file" ||
            quotas.Name != "Disk quotas" || storageSensePolicy.Name != "Storage Sense machine policy" ||
            storageSenseTask.Name != "Storage Sense scheduled task" || cleanupTask.Name != "Disk Cleanup scheduled task" ||
            diskDiagnosticHistory.Name != "Windows disk diagnostic correlation")
            return Unknown("Storage environment inputs were missing, duplicated or carried an unexpected identity.");

        var current = spaces.Append(shadowStorage).Append(selfHealing).Concat(deleteNotifications)
            .Append(pageFile).Append(quotas).Append(storageSensePolicy).Append(storageSenseTask).Append(cleanupTask).ToArray();
        int incomplete = current.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = current.Count(item => NeedsAttention(item.EffectiveState));
        bool historyIncomplete = diskDiagnosticHistory.EffectiveState == CheckState.Unknown;
        bool historyAttention = NeedsAttention(diskDiagnosticHistory.EffectiveState);
        string detail = $"Reviewed Storage Spaces, VSS shadow storage, selected-volume self-healing, {deleteNotifications.Count} delete-notification result{(deleteNotifications.Count == 1 ? "" : "s")}, paging-file state, disk quotas, Storage Sense policy and task state, Disk Cleanup task state and disk diagnostic history. Current evidence: {attention} need attention and {incomplete} incomplete. Disk diagnostic history: {(historyIncomplete ? "incomplete" : historyAttention ? "review" : "clear or informational")}. ";
        const string boundary = "This is bounded configuration, provider and retained-history evidence. It does not prove filesystem integrity, storage media health, pool accessibility, snapshot recoverability, cleanup effectiveness, memory sufficiency or application I/O. No file, disk, pool, virtual disk, partition, volume, page file, quota, snapshot, task, policy or storage setting changed, and no repair is selected.";
        if (incomplete > 0 || historyIncomplete)
            return Unknown(detail + "Recollect incomplete evidence before choosing a storage action. " + boundary);
        if (attention > 0)
            return new("Storage environment correlation", CheckState.Attention,
                detail + "Review the exact current findings before choosing a storage, cleanup, quota, snapshot or memory action. " + boundary);
        if (historyAttention)
            return new("Storage environment correlation", CheckState.Information,
                detail + "Current environment evidence did not establish a storage problem. Review retained disk diagnostic history only when it matches the time and symptom. " + boundary);
        return new("Storage environment correlation", CheckState.Information,
            detail + "No environment contradiction was established within the collected scope. " + boundary);
    }

    private static bool NeedsAttention(CheckState state) => state is CheckState.Attention or CheckState.Repairable;
    private static WorkflowCheck Unknown(string detail) => new("Storage environment correlation", CheckState.Unknown,
        detail + " No repair is selected and no storage state changed.");
}
