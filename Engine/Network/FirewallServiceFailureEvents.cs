namespace KillerBench.Engine.Network;

public static class FirewallServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition BaseFilteringEngine = new(
        "BFE",
        "Base Filtering Engine",
        "Base Filtering Engine failure history",
        " Records are historical observations tied to the exact local Base Filtering Engine identity. They do not establish a current outage, prove firewall protection is ineffective or authorize a service start, restart, startup change, rule change, filter change, IPsec change, policy change or repair.");

    private static readonly ExactServiceFailureEventDefinition WindowsFirewall = new(
        "MpsSvc",
        "Windows Defender Firewall",
        "Windows Defender Firewall service failure history",
        " Records are historical observations tied to the exact local Windows Defender Firewall service identity. They do not establish a current outage, prove firewall protection is ineffective or authorize a service start, restart, startup change, profile change, rule change, filter change, IPsec change, policy change or repair.");

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var checks = await Task.WhenAll(
            ExactServiceFailureEvents.CheckAsync(BaseFilteringEngine, execute, token: token),
            ExactServiceFailureEvents.CheckAsync(WindowsFirewall, execute, token: token)).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(checks);
    }
}
