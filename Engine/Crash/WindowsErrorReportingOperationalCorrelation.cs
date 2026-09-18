namespace KillerBench.Engine;

public static class WindowsErrorReportingOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Crash dump configuration",
        "Crash dump files",
        "Recent system crash reports",
        "Windows Error Reporting service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Error Reporting conclusion";
        const string boundary = " This bounded conclusion does not establish report creation, upload, delivery, problem resolution, dump validity, crash time or root cause. It does not start a service, submit or delete a report, open dump contents, change consent or upload policy, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>(RequiredNames.Length);
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Windows Error Reporting inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        int currentAttention = selected.Count(check =>
            (check.Name is "Crash dump configuration" or "Focused service readiness") &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalAttention = selected.Count(check => check.Name == "Windows Error Reporting service failure history" &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed crash-dump configuration and metadata, bounded system crash-report history, exact service-failure history and current service readiness. Current results needing attention: {currentAttention}; historical service findings: {historicalAttention}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete configuration, metadata, event or service evidence before interpreting error-reporting readiness.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention,
                detail + " Review current dump configuration or service readiness. Historical records remain context and cannot authorize service recovery or report handling." + boundary);
        return new(name, CheckState.Information,
            detail + " No current configuration or service concern was established within the collected evidence. Retained reports and failures remain historical context." + boundary);
    }
}
