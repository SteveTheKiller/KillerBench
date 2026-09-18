namespace KillerBench.Engine;

public static class NetworkOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck firewall, WorkflowCheck path, WorkflowCheck connectivity,
        WorkflowCheck winsock, WorkflowCheck dns, WorkflowCheck dhcp, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(firewall);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(connectivity);
        ArgumentNullException.ThrowIfNull(winsock);
        ArgumentNullException.ThrowIfNull(dns);
        ArgumentNullException.ThrowIfNull(dhcp);
        token.ThrowIfCancellationRequested();

        const string name = "Network operational correlation";
        const string boundary = " This is a bounded configuration and point-in-time correlation. It does not query a public host, prove DNS response, Internet, VPN or application connectivity, inspect third-party filtering, or authorize a reset. No adapter, route, address, lease, resolver, cache, Hosts entry, proxy, firewall, Winsock catalog, provider, service or policy setting changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        var inputs = new[]
        {
            (Check: firewall, Name: "Active network firewall alignment"),
            (Check: path, Name: "Network path readiness"),
            (Check: connectivity, Name: "Network connectivity correlation"),
            (Check: winsock, Name: "Winsock name-resolution readiness"),
            (Check: dns, Name: "DNS operational correlation"),
            (Check: dhcp, Name: "DHCP operational correlation")
        };
        if (inputs.Any(item => item.Check.Name != item.Name))
            return Unknown("Network correlation inputs were missing, duplicated or carried an unexpected identity.");

        int incomplete = inputs.Count(item => item.Check.EffectiveState == CheckState.Unknown);
        int attention = inputs.Count(item => item.Check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed active firewall alignment, default-route consistency, Windows profile and exact-gateway evidence, native Winsock structure, DNS readiness and DHCP readiness. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before selecting a network action.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the detailed results and run a bounded target lookup or connection test when relevant before choosing a repair." + boundary);
        return new(name, CheckState.Information, detail + "No contradiction or structural issue was established within the collected scope." + boundary);
    }
}
