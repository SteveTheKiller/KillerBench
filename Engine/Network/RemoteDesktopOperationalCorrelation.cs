namespace KillerBench.Engine;

public static class RemoteDesktopOperationalCorrelation
{
    private static readonly string[] CompleteConfigurationNames =
    [
        "Remote Desktop enablement",
        "Remote Desktop Network Level Authentication",
        "Remote Desktop listener port"
    ];

    private static readonly string[] RequiredNames =
    [
        "Recent Remote Desktop connection events",
        "Recent Remote Desktop session events",
        "Remote Desktop Services failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Remote Desktop readiness";
        const string boundary = " This bounded conclusion does not establish effective policy, listener binding, firewall access, certificate identity, permissions, authentication, network reachability, connection success or session creation. It does not enable Remote Desktop, start a service, create or change a listener, firewall rule, certificate, permission or session, disconnect a user, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>();
        var configurationFailure = checks.Where(check => check is not null && check.Name == "Remote Desktop configuration").ToArray();
        var configuration = CompleteConfigurationNames.Select(required =>
            checks.Where(check => check is not null && check.Name == required).ToArray()).ToArray();
        bool completeConfiguration = configuration.All(matches => matches.Length == 1) && configurationFailure.Length == 0;
        bool incompleteConfiguration = configurationFailure.Length == 1 && configuration.All(matches => matches.Length == 0);
        if (!completeConfiguration && !incompleteConfiguration)
            return Unknown("Remote Desktop configuration inputs were missing, duplicated or carried an unexpected identity.");
        if (completeConfiguration) selected.AddRange(configuration.Select(matches => matches[0]));
        else selected.Add(configurationFailure[0]);

        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Remote Desktop history or service inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        int currentAttention = selected.Count(check =>
            (check.Name is "Remote Desktop Network Level Authentication" or "Focused service readiness") &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalFindings = selected.Count(check =>
            (check.Name is "Recent Remote Desktop connection events" or "Recent Remote Desktop session events" or "Remote Desktop Services failure history") &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed local Remote Desktop configuration, bounded connection and session history, exact service-failure history and current service readiness. Current results needing attention: {currentAttention}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete configuration, history or service evidence before interpreting Remote Desktop readiness.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention,
                detail + " Review current NLA configuration or service readiness. Historical records cannot authorize service recovery or configuration changes." + boundary);
        return new(name, CheckState.Information,
            detail + " No current configuration or service concern was established within the collected evidence. Retained connection, session and service failures remain historical context." + boundary);
    }
}
