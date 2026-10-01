namespace KillerBench.Engine.Backup;

public static class FocusedBackupOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Windows Backup history",
        "File History protection state",
        "Recent File History events",
        "Recent VSS events",
        "Recent volsnap events",
        "VSS writers",
        "VSS provider registrations",
        "Volume Shadow Copy failure history",
        "Software Shadow Copy Provider failure history",
        "Windows Backup service failure history",
        "Block Level Backup Engine failure history",
        "VSS shadow copies",
        "VSS shadow storage",
        "Administrator",
        "Restart"
    ];

    private static readonly HashSet<string> HistoricalNames = new(StringComparer.Ordinal)
    {
        "Windows Backup history",
        "Recent File History events",
        "Recent VSS events",
        "Recent volsnap events",
        "Volume Shadow Copy failure history",
        "Software Shadow Copy Provider failure history",
        "Windows Backup service failure history",
        "Block Level Backup Engine failure history"
    };

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Focused backup services conclusion";
        const string boundary = " This bounded local review does not prove that a backup is configured, current, restorable or successful. Backup destinations, application-specific jobs and restore tests are outside this focused workflow. No backup, restore, snapshot, writer, provider, service, shadow storage, File History, policy, destination or restart state changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = evidence.Where(item => RequiredNames.Contains(item.Name, StringComparer.Ordinal)).ToArray();
        bool exact = selected.Length == RequiredNames.Length && RequiredNames.All(required =>
            selected.Count(item => string.Equals(item.Name, required, StringComparison.Ordinal)) == 1);
        if (!exact)
            return Unknown("Focused backup evidence was missing, duplicated or carried an unexpected identity.");

        int incomplete = selected.Count(item => item.EffectiveState == CheckState.Unknown);
        int currentAttention = selected.Count(item => !HistoricalNames.Contains(item.Name) &&
            item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalAttention = selected.Count(item => HistoricalNames.Contains(item.Name) &&
            item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed 15 exact backup-service observations. Current evidence: {currentAttention} need attention and {incomplete} incomplete. Historical observations needing review: {historicalAttention}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before drawing a focused backup-services conclusion.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention, detail + "Review the current writer, provider, snapshot, shadow-storage, File History, access and restart results before choosing an action." + boundary);
        if (historicalAttention > 0)
            return new(name, CheckState.Information, detail + "Historical records alone do not establish a current fault. Correlate them with the backup application's job history and a controlled restore test." + boundary);
        return new(name, CheckState.Information, detail + "No focused prerequisite requested review. Backup completion and restore readiness remain unverified." + boundary);
    }
}
