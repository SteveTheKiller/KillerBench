namespace KillerBench.Engine.Network;

public static class WinHttpAutoProxyOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "WinHTTP default proxy",
        "Current-account Internet proxy",
        "WinHTTP Auto-Proxy service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "WinHTTP Auto-Proxy readiness";
        const string boundary = " This bounded conclusion does not establish PAC or WPAD resolution, proxy reachability, authentication, policy ownership, application overrides or Internet access. It does not fetch a URL, start discovery, clear a cache, change a proxy, credential, network, firewall, service or policy setting, or select a repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>();
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("WinHTTP Auto-Proxy inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        bool serviceAttention = selected.Single(check => check.Name == "Focused service readiness").EffectiveState is CheckState.Attention or CheckState.Repairable;
        int historicalFindings = selected.Count(check => check.Name == "WinHTTP Auto-Proxy service failure history" &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed stored WinHTTP defaults, current-account Internet settings, exact service-failure history and current service readiness. Current service results needing attention: {(serviceAttention ? 1 : 0)}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete proxy, history or service evidence before interpreting WinHTTP Auto-Proxy readiness.");
        if (serviceAttention)
            return new(name, CheckState.Attention, detail + " Review current exact-service readiness. Proxy configuration and historical failures cannot authorize service recovery or discovery." + boundary);
        return new(name, CheckState.Information, detail + " No current exact-service concern was established. Stored proxy settings and retained failures remain context for an exact application or destination test." + boundary);
    }
}
