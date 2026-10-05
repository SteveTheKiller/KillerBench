namespace KillerBench.Engine.Network;

public static class NetworkServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition[] Definitions =
    [
        Definition("Dhcp", "DHCP Client", "DHCP Client failure history",
            "lease acquisition, address assignment or adapter connectivity"),
        Definition("Dnscache", "DNS Client", "DNS Client failure history",
            "name resolution, cache correctness or destination reachability"),
        Definition("LanmanWorkstation", "Workstation", "Workstation service failure history",
            "SMB client availability, share access or remote-server health"),
        Definition("NlaSvc", "Network Location Awareness", "Network Location Awareness failure history",
            "current network identity, connectivity classification or Internet access"),
        Definition("netprofm", "Network List Service", "Network List Service failure history",
            "current profile assignment, connectivity classification or firewall alignment"),
        Definition("iphlpsvc", "IP Helper", "IP Helper failure history",
            "IPv6 transition availability, tunnel state or current route health")
    ];

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var checks = await Task.WhenAll(Definitions.Select(definition =>
            ExactServiceFailureEvents.CheckAsync(definition, execute, token: token))).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(checks);
    }

    private static ExactServiceFailureEventDefinition Definition(string service, string display, string check, string excludedClaim) =>
        new(service, display, check,
            $" Records are historical observations tied to the exact local {display} identity. They do not establish a current outage, prove {excludedClaim} or authorize a service start, restart, startup change, adapter change, protocol change, policy change, cache reset or repair.");
}
