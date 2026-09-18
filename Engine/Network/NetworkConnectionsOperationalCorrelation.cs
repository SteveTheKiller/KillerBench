namespace KillerBench.Engine;

public static class NetworkConnectionsOperationalCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Network Connections readiness";
        const string boundary = " This bounded conclusion does not establish adapter availability, address assignment, route selection, name resolution, firewall access, Internet reachability or connection success. It does not enable, disable, reset or reconfigure an adapter, profile, address, route, DNS server, firewall rule, service or policy, and it selects no repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var profiles = checks.Where(check => check is not null && (check.Name == "Network profiles" ||
            check.Name.StartsWith("Network profile: ", StringComparison.Ordinal))).ToArray();
        var history = checks.Where(check => check is not null && check.Name == "Network Connections service failure history").ToArray();
        var service = checks.Where(check => check is not null && check.Name == "Focused service readiness").ToArray();
        if (profiles.Length == 0 || history.Length != 1 || service.Length != 1 ||
            profiles.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() != profiles.Length)
            return Unknown("Network Connections inputs were missing, duplicated or carried an unexpected identity.");

        int incomplete = profiles.Count(check => check.EffectiveState == CheckState.Unknown) +
            (history[0].EffectiveState == CheckState.Unknown ? 1 : 0) + (service[0].EffectiveState == CheckState.Unknown ? 1 : 0);
        bool serviceAttention = service[0].EffectiveState is CheckState.Attention or CheckState.Repairable;
        int profileAttention = profiles.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalFindings = history[0].EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0;
        string detail = $"Reviewed {profiles.Length} current network profile result{(profiles.Length == 1 ? "" : "s")}, exact service-failure history and current service readiness. Profile results needing review: {profileAttention}; current service results needing attention: {(serviceAttention ? 1 : 0)}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete profile, history or service evidence before interpreting Network Connections readiness.");
        if (serviceAttention)
            return new(name, CheckState.Attention, detail + " Review current exact-service readiness. Profile assignments and historical failures cannot authorize service recovery or a network change." + boundary);
        return new(name, CheckState.Information, detail +
            " No current exact-service concern was established. Public profiles and retained failures remain context for narrower network review." + boundary);
    }
}
