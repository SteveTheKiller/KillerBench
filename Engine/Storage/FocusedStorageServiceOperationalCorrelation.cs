namespace KillerBench.Engine;

public static class FocusedStorageServiceOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Storage jobs",
        "Storage Service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Storage Service readiness";
        const string boundary = " This bounded conclusion does not establish device, pool, virtual-disk, partition, volume, filesystem, Storage Sense, notification or application I/O health. A completed job state is not an operation-result verdict. It does not start, stop, resume or remove a job, change a disk, pool, virtual disk, partition, volume, filesystem, file, mount point, service, policy or setting, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>();
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Storage Service inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        bool jobAttention = selected.Single(check => check.Name == "Storage jobs").EffectiveState is CheckState.Attention or CheckState.Repairable;
        bool serviceAttention = selected.Single(check => check.Name == "Focused service readiness").EffectiveState is CheckState.Attention or CheckState.Repairable;
        int historicalFindings = selected.Count(check => check.Name == "Storage Service failure history" &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed current bounded Storage module jobs, exact service-failure history and current service readiness. Job results needing review: {(jobAttention ? 1 : 0)}; current service results needing attention: {(serviceAttention ? 1 : 0)}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete job, history or service evidence before interpreting Storage Service readiness.");
        if (jobAttention || serviceAttention)
            return new(name, CheckState.Attention, detail + " Review the current job or exact-service concern. Job state and historical failures cannot authorize service recovery or storage changes." + boundary);
        return new(name, CheckState.Information, detail + " No current Storage-module job or exact-service concern was established. Retained failures remain context for narrower storage review." + boundary);
    }
}
