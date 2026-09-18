namespace KillerBench.Engine;

public static class BackupOperationalCorrelation
{
    private static readonly string[] EventNames = ["Recent VSS events", "Recent volsnap events"];
    private static readonly string[] ServiceHistoryNames =
        ["Windows Backup service failure history", "Block Level Backup Engine failure history"];

    public static WorkflowCheck Check(WorkflowCheck windowsBackup, WorkflowCheck fileHistoryState,
        WorkflowCheck fileHistoryEvents, IReadOnlyList<WorkflowCheck> backupEvents, IReadOnlyList<WorkflowCheck> writerEvidence,
        WorkflowCheck providers, IReadOnlyList<WorkflowCheck> serviceHistory, WorkflowCheck shadowCopies,
        IReadOnlyList<WorkflowCheck> restorePointEvidence, IReadOnlyList<WorkflowCheck> storageReadiness,
        IReadOnlyList<WorkflowCheck> restartState, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(windowsBackup);
        ArgumentNullException.ThrowIfNull(fileHistoryState);
        ArgumentNullException.ThrowIfNull(fileHistoryEvents);
        ArgumentNullException.ThrowIfNull(backupEvents);
        ArgumentNullException.ThrowIfNull(writerEvidence);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        ArgumentNullException.ThrowIfNull(shadowCopies);
        ArgumentNullException.ThrowIfNull(restorePointEvidence);
        ArgumentNullException.ThrowIfNull(storageReadiness);
        ArgumentNullException.ThrowIfNull(restartState);
        token.ThrowIfCancellationRequested();

        const string name = "Backup operational correlation";
        const string boundary = " This is a bounded prerequisite review, not proof that a backup is configured, current, restorable or successful. No backup, restore, snapshot, writer, provider, service, storage, File History, policy, destination or restart state changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var current = new[]
        {
            (Check: windowsBackup, Name: "Windows Backup history"),
            (Check: fileHistoryState, Name: "File History protection state"),
            (Check: fileHistoryEvents, Name: "Recent File History events"),
            (Check: providers, Name: "VSS provider registrations"),
            (Check: shadowCopies, Name: "VSS shadow copies")
        };
        var writers = writerEvidence.Where(item => item.Name == "VSS writers").ToArray();
        var restorePoints = restorePointEvidence.Where(item => item.Name == "System Restore points").ToArray();
        var storage = storageReadiness.Where(item => item.Name == "Storage operational correlation").ToArray();
        var restart = restartState.Where(item => item.Name == "Restart").ToArray();
        if (current.Any(item => item.Check.Name != item.Name) || !ExactNames(backupEvents, EventNames) ||
            !ExactNames(serviceHistory, ServiceHistoryNames) || writers.Length != 1 || restorePoints.Length != 1 ||
            storage.Length != 1 || restart.Length != 1)
            return Unknown("Backup correlation inputs were missing, duplicated or carried an unexpected identity.");

        var all = current.Select(item => item.Check).Concat(backupEvents).Concat(writers).Concat(serviceHistory)
            .Concat(restorePoints).Concat(storage).Concat(restart).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed Windows Backup and File History observations, exact VSS and volsnap history, writer and provider state, current shadow copies, restore points, two exact backup service histories, storage readiness and restart state. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before drawing a combined backup-readiness conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the detailed current and historical results with the backup application's own job and restore-test evidence before choosing an action." + boundary);
        return new(name, CheckState.Information, detail + "No correlated prerequisite requested review. A successful backup and restore remain unverified." + boundary);
    }

    private static bool ExactNames(IReadOnlyList<WorkflowCheck> checks, IReadOnlyList<string> names) =>
        checks.Count == names.Count && checks.Select(item => item.Name).OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(names.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
}
