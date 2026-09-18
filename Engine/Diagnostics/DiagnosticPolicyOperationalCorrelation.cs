namespace KillerBench.Engine;

public static class DiagnosticPolicyOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Recent Diagnostic Policy events",
        "Diagnostic Policy service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Diagnostic Policy readiness";
        const string boundary = " This bounded conclusion does not establish a current fault, root cause, scenario applicability, troubleshooter result, diagnosis completion or repair outcome. It does not start a service, run a troubleshooter, accept a suggested fix, change policy or troubleshooting data, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>();
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Diagnostic Policy inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        int currentAttention = selected.Count(check => check.Name == "Focused service readiness" && check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalFindings = selected.Count(check => check.Name != "Focused service readiness" && check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed bounded Diagnostic Policy operational history, exact service-failure history and current service readiness. Current results needing attention: {currentAttention}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete event or service evidence before interpreting Diagnostic Policy readiness.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention,
                detail + " Review current exact-service readiness. Historical records cannot authorize service recovery or a troubleshooting action." + boundary);
        return new(name, CheckState.Information,
            detail + " No current service concern was established within the collected evidence. Retained events and service failures remain historical context." + boundary);
    }
}
