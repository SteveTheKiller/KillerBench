namespace KillerBench.Engine;

public static class DnsOperationalCorrelation
{
    private static readonly string[] EventNames =
    [
        "Recent DNS Client system events",
        "Recent DNS Client operational events"
    ];

    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck servers, WorkflowCheck policy,
        IReadOnlyList<WorkflowCheck> events, WorkflowCheck path, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(servers);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(path);
        token.ThrowIfCancellationRequested();

        const string name = "DNS operational correlation";
        const string boundary = " This is a bounded current-state and recent-history correlation. It does not send a DNS query, prove resolver response, select the interface Windows will use, establish Internet or application connectivity, or identify root cause. No service, cache, server, suffix, adapter, route, namespace, VPN, DirectAccess, DNSSEC, IPsec or policy setting changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (service.Name != RepairKind.DnsClient.ToString() || servers.Name != "DNS server configuration" ||
            policy.Name != "Effective DNS policy" || path.Name != "Network path readiness" ||
            events.Count != EventNames.Length || events.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != EventNames.Length ||
            EventNames.Any(expected => events.Count(item => item.Name == expected) != 1))
            return Unknown("DNS correlation inputs were missing, duplicated or carried an unexpected identity.");

        var current = new[] { service, servers, policy, path };
        int incomplete = current.Count(item => item.EffectiveState == CheckState.Unknown) +
            events.Count(item => item.EffectiveState == CheckState.Unknown);
        int currentAttention = current.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalAttention = events.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed current DNS Client service state, per-interface server configuration, effective namespace policy, default-route alignment and {events.Count} recent event sources. Current evidence needing attention: {currentAttention}; historical sources with retained warnings or failures: {historicalAttention}; incomplete sources: {incomplete}. ";

        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before choosing a DNS action.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention,
                detail + "Review the current-state results and run a bounded lookup for the exact hostname involved before choosing recovery. Recent events remain supporting history and do not establish a current outage." + boundary);
        if (historicalAttention > 0)
            return new(name, CheckState.Information,
                detail + "Current configuration evidence did not establish a DNS readiness problem. Review recent records only when they match the time and symptom under investigation." + boundary);
        return new(name, CheckState.Information,
            detail + "No current contradiction was established within the collected scope." + boundary);
    }
}
