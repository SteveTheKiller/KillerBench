namespace KillerBench.Engine.Backup;

public static class BackupServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition[] Definitions =
    [
        Definition("SDRSVC", "Windows Backup", "Windows Backup service failure history",
            "backup configuration, schedule execution, destination access, backup completion or restore success"),
        Definition("wbengine", "Block Level Backup Engine Service", "Block Level Backup Engine failure history",
            "block-level transfer, destination access, backup completion or restore success")
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

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAllAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var shadowCopy = StorageServiceFailureEvents.CheckShadowCopyAsync(execute, token: token);
        var backup = CheckAsync(execute, token: token);
        var groups = await Task.WhenAll(shadowCopy, backup).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(groups.SelectMany(group => group).ToArray());
    }

    private static ExactServiceFailureEventDefinition Definition(string service, string display, string check, string excludedClaim) =>
        new(service, display, check,
            $" Records are historical observations tied to the exact local {display} identity. They do not establish a current outage, prove {excludedClaim} or authorize a service start, restart, startup change, backup change, catalog change, destination change, policy change or repair.");
}
