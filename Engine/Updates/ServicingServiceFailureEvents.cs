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

    public static Task<WorkflowCheck> CheckBitsAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        ExactServiceFailureEvents.CheckAsync(Bits, execute, token: token);

    public static Task<WorkflowCheck> CheckMedicAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        ExactServiceFailureEvents.CheckAsync(WindowsUpdateMedic, execute, token: token);

    public static Task<WorkflowCheck> CheckFocusedAsync(WorkflowKind kind, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) => kind switch
    {
        WorkflowKind.WindowsUpdateService => WindowsUpdateServiceFailureEvents.CheckAsync(execute, token: token),
        WorkflowKind.BitsService => ExactServiceFailureEvents.CheckAsync(Bits, execute, token: token),
        WorkflowKind.CryptographicService => ExactServiceFailureEvents.CheckAsync(CryptographicServices, execute, token: token),
        WorkflowKind.ModulesInstallerService => ExactServiceFailureEvents.CheckAsync(WindowsModulesInstaller, execute, token: token),
        WorkflowKind.WindowsUpdateMedicService => CheckMedicAsync(execute, token: token),
        WorkflowKind.UpdateOrchestratorService => ExactServiceFailureEvents.CheckAsync(UpdateOrchestrator, execute, token: token),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), "The workflow is not a focused servicing service.")
    };

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var checks = await Task.WhenAll(
            WindowsUpdateServiceFailureEvents.CheckAsync(execute, token: token),
            CheckBitsAsync(execute, token: token),
            ExactServiceFailureEvents.CheckAsync(CryptographicServices, execute, token: token),
            ExactServiceFailureEvents.CheckAsync(WindowsModulesInstaller, execute, token: token),
            CheckMedicAsync(execute, token: token),
            ExactServiceFailureEvents.CheckAsync(UpdateOrchestrator, execute, token: token)).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(checks);
    }
}
