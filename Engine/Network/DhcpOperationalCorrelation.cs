namespace KillerBench.Engine;

public static class DhcpOperationalCorrelation
{
    private static readonly string[] EventNames =
    [
        "Recent DHCP Client IPv4 administrative events",
        "Recent DHCP Client IPv4 operational events",
        "Recent DHCP Client IPv6 administrative events",
        "Recent DHCP Client IPv6 operational events"
    ];

    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck lease,
        IReadOnlyList<WorkflowCheck> addresses, IReadOnlyList<WorkflowCheck> events,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(addresses);
        ArgumentNullException.ThrowIfNull(events);
        token.ThrowIfCancellationRequested();

        const string name = "DHCP operational correlation";
        const string boundary = " This is a bounded current-state and recent-history correlation. It does not contact a DHCP server, prove lease renewal, select an adapter, test gateway, DNS, Internet or application connectivity, or identify root cause. No service, lease, address, adapter, route, resolver, firewall or policy setting changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        bool validAddresses = addresses.Count is > 0 and <= 512 &&
            addresses.All(item => item.Name == "IP address inventory" || item.Name.StartsWith("IP address: ", StringComparison.Ordinal)) &&
            addresses.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == addresses.Count;
        bool validEvents = events.Count == EventNames.Length &&
            events.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == EventNames.Length &&
            EventNames.All(expected => events.Count(item => item.Name == expected) == 1);
        if (service.Name != RepairKind.DhcpClient.ToString() || lease.Name != "DHCP lease state" || !validAddresses || !validEvents)
            return Unknown("DHCP correlation inputs were missing, duplicated, exceeded their bound or carried an unexpected identity.");

        var current = new[] { service, lease }.Concat(addresses).ToArray();
        int incomplete = current.Count(item => item.EffectiveState == CheckState.Unknown) +
            events.Count(item => item.EffectiveState == CheckState.Unknown);
        int currentAttention = current.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historicalAttention = events.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed current DHCP Client service state, lease evidence, {addresses.Count} IP address result{(addresses.Count == 1 ? "" : "s")} and {events.Count} recent event sources. Current evidence needing attention: {currentAttention}; historical sources with retained warnings or failures: {historicalAttention}; incomplete sources: {incomplete}. ";

        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before choosing a DHCP action.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention,
                detail + "Review the current adapter and lease results before considering a bounded renewal. Recent events remain supporting history and do not establish a current DHCP failure." + boundary);
        if (historicalAttention > 0)
            return new(name, CheckState.Information,
                detail + "Current lease and address evidence did not establish a DHCP readiness problem. Review recent records only when they match the time and adapter symptom under investigation." + boundary);
        return new(name, CheckState.Information,
            detail + "No current contradiction was established within the collected scope." + boundary);
    }
}
