namespace KillerBench.Engine.Storage;

public static class StorageServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition[] StorageDefinitions =
    [
        Definition("StorSvc", "Storage Service", "Storage Service failure history",
            "storage notifications, Storage Sense availability or current storage health"),
        Definition("defragsvc", "Optimize drives", "Optimize Drives failure history",
            "current optimization eligibility, scheduled optimization or volume health")
    ];

    private static readonly ExactServiceFailureEventDefinition[] ShadowCopyDefinitions =
    [
        Definition("VSS", "Volume Shadow Copy", "Volume Shadow Copy failure history",
            "current writer state, snapshot availability, backup completion or restore success"),
        Definition("swprv", "Microsoft Software Shadow Copy Provider", "Software Shadow Copy Provider failure history",
            "current provider availability, snapshot creation, backup completion or restore success")
    ];

    public static Task<IReadOnlyList<WorkflowCheck>> CheckStorageAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        CheckAsync(StorageDefinitions, execute, token: token);

    public static Task<IReadOnlyList<WorkflowCheck>> CheckShadowCopyAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        CheckAsync(ShadowCopyDefinitions, execute, token: token);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var storage = CheckStorageAsync(execute, token: token);
        var shadowCopy = CheckShadowCopyAsync(execute, token: token);
        var groups = await Task.WhenAll(storage, shadowCopy).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(groups.SelectMany(group => group).ToArray());
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(
        IReadOnlyList<ExactServiceFailureEventDefinition> definitions, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = await Task.WhenAll(definitions.Select(definition =>
            ExactServiceFailureEvents.CheckAsync(definition, execute, token: token))).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(checks);
    }

    private static ExactServiceFailureEventDefinition Definition(string service, string display, string check, string excludedClaim) =>
        new(service, display, check,
            $" Records are historical observations tied to the exact local {display} identity. They do not establish a current outage, prove {excludedClaim} or authorize a service start, restart, startup change, optimization, snapshot change, provider change, backup change, policy change or repair.");
}
