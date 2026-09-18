namespace KillerBench.Engine;

public static class ConnectionManagerOperationalCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Connection Manager readiness";
        const string boundary = " This bounded conclusion does not establish adapter availability, profile applicability, connection selection, metering, roaming, authentication, route selection, name resolution or connectivity. It does not connect, disconnect, enable, disable, reset or reconfigure an adapter, profile, credential, firewall rule, VPN, service or policy, and it selects no repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var profiles = checks.Where(check => check is not null && (check.Name == "Network profiles" ||
            check.Name.StartsWith("Network profile: ", StringComparison.Ordinal))).ToArray();
        var history = checks.Where(check => check is not null && check.Name == "Windows Connection Manager failure history").ToArray();
        var service = checks.Where(check => check is not null && check.Name == "Focused service readiness").ToArray();
        if (profiles.Length == 0 || history.Length != 1 || service.Length != 1 ||
            profiles.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() != profiles.Length)
            return Unknown("Windows Connection Manager inputs were missing, duplicated or carried an unexpected identity.");

        int incomplete = profiles.Count(check => check.EffectiveState == CheckState.Unknown) +
            (history[0].EffectiveState == CheckState.Unknown ? 1 : 0) + (service[0].EffectiveState == CheckState.Unknown ? 1 : 0);
        bool serviceAttention = service[0].EffectiveState is CheckState.Attention or CheckState.Repairable;
        int profileAttention = profiles.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalFindings = history[0].EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0;
        string detail = $"Reviewed {profiles.Length} current network profile result{(profiles.Length == 1 ? "" : "s")}, exact service-failure history and current service readiness. Profile results needing review: {profileAttention}; current service results needing attention: {(serviceAttention ? 1 : 0)}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete profile, history or service evidence before interpreting Windows Connection Manager readiness.");
        if (serviceAttention)
            return new(name, CheckState.Attention, detail + " Review current exact-service readiness. Profile assignments and historical failures cannot authorize recovery or a connection decision." + boundary);
        return new(name, CheckState.Information, detail + " No current exact-service concern was established. Public profiles and retained failures remain context for narrower connection review." + boundary);
    }
}
