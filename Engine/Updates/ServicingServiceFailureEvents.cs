namespace KillerBench.Engine.Updates;

public static class ServicingServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition Bits = new(
        "BITS",
        "Background Intelligent Transfer Service",
        "BITS service failure history",
        " Records are historical observations tied to the exact local BITS identity. They do not establish a current outage, identify a failed transfer or authorize a service start, transfer retry, job change, cache reset, policy change or repair.");

    private static readonly ExactServiceFailureEventDefinition CryptographicServices = new(
        "CryptSvc",
        "Cryptographic Services",
        "Cryptographic Services failure history",
        " Records are historical observations tied to the exact local Cryptographic Services identity. They do not establish a current outage, identify a catalog or certificate failure or authorize a service start, catalog change, certificate change, cache reset, policy change or repair.");

    private static readonly ExactServiceFailureEventDefinition WindowsModulesInstaller = new(
        "TrustedInstaller",
        "Windows Modules Installer",
        "Windows Modules Installer failure history",
        " Records are historical observations tied to the exact local Windows Modules Installer identity. They do not establish a current outage, identify a failed package or authorize a service start, package operation, source change, component-store change, policy change or repair.");

    private static readonly ExactServiceFailureEventDefinition WindowsUpdateMedic = new(
        "WaaSMedicSvc",
        "Windows Update Medic Service",
        "Windows Update Medic failure history",
        " Records are historical observations tied to the exact local Windows Update Medic identity. They do not establish a current outage, identify a failed update or authorize a service start, update retry, source change, cache reset, policy change or repair.");

    private static readonly ExactServiceFailureEventDefinition UpdateOrchestrator = new(
        "UsoSvc",
        "Update Orchestrator Service",
        "Update Orchestrator failure history",
        " Records are historical observations tied to the exact local Update Orchestrator identity. They do not establish a current outage, identify a failed update or authorize a service start, scan, download, installation, restart, policy change or repair.");

    public static Task<WorkflowCheck> CheckBitsAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null) =>
        ExactServiceFailureEvents.CheckAsync(Bits, token, execute);

    public static Task<WorkflowCheck> CheckMedicAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null) =>
        ExactServiceFailureEvents.CheckAsync(WindowsUpdateMedic, token, execute);

    public static Task<WorkflowCheck> CheckFocusedAsync(WorkflowKind kind, CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null) => kind switch
    {
        WorkflowKind.WindowsUpdateService => WindowsUpdateServiceFailureEvents.CheckAsync(token, execute),
        WorkflowKind.BitsService => ExactServiceFailureEvents.CheckAsync(Bits, token, execute),
        WorkflowKind.CryptographicService => ExactServiceFailureEvents.CheckAsync(CryptographicServices, token, execute),
        WorkflowKind.ModulesInstallerService => ExactServiceFailureEvents.CheckAsync(WindowsModulesInstaller, token, execute),
        WorkflowKind.WindowsUpdateMedicService => CheckMedicAsync(token, execute),
        WorkflowKind.UpdateOrchestratorService => ExactServiceFailureEvents.CheckAsync(UpdateOrchestrator, token, execute),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), "The workflow is not a focused servicing service.")
    };

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        var checks = await Task.WhenAll(
            WindowsUpdateServiceFailureEvents.CheckAsync(token, execute),
            CheckBitsAsync(token, execute),
            ExactServiceFailureEvents.CheckAsync(CryptographicServices, token, execute),
            ExactServiceFailureEvents.CheckAsync(WindowsModulesInstaller, token, execute),
            CheckMedicAsync(token, execute),
            ExactServiceFailureEvents.CheckAsync(UpdateOrchestrator, token, execute)).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(checks);
    }
}
