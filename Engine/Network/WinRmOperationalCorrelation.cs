namespace KillerBench.Engine;

public static class WinRmOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "WinRM listeners",
        "Recent Windows Remote Management events",
        "Windows Remote Management service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Remote Management readiness";
        const string boundary = " This bounded conclusion does not establish effective policy, listener binding, firewall access, certificate validity, authentication, permissions, client reachability or remote command execution. It does not start a service, create or change a listener, firewall rule, TrustedHosts entry, certificate, credential, permission or remote session, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>();
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Windows Remote Management inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }
        var listenerDetails = checks.Where(check => check is not null &&
            check.Name.StartsWith("WinRM ", StringComparison.Ordinal) && check.Name.EndsWith(" listener", StringComparison.Ordinal)).ToArray();
        if (listenerDetails.Length > 32)
            return Unknown("Windows Remote Management listener detail exceeded the bounded inventory.");
        selected.AddRange(listenerDetails);

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        int currentAttention = selected.Count(check =>
            (check.Name == "Focused service readiness" || listenerDetails.Contains(check)) &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalFindings = selected.Count(check =>
            (check.Name is "Recent Windows Remote Management events" or "Windows Remote Management service failure history") &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed bounded listener configuration, WinRM operational history, exact service-failure history and current service readiness. Current results needing attention: {currentAttention}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete listener, history or service evidence before interpreting Windows Remote Management readiness.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention,
                detail + " Review disabled listener registration or current service readiness. Historical records cannot authorize service recovery or configuration changes." + boundary);
        return new(name, CheckState.Information,
            detail + " No current listener-registration or service concern was established within the collected evidence. An empty listener inventory can be intentional, and retained events remain historical context." + boundary);
    }
}
