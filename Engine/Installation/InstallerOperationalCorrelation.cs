namespace KillerBench.Engine;

public static class InstallerOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck engineFiles,
        WorkflowCheck registration, WorkflowCheck activity, WorkflowCheck pendingOperations,
        WorkflowCheck installerHistory, WorkflowCheck serviceHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(engineFiles);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(pendingOperations);
        ArgumentNullException.ThrowIfNull(installerHistory);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();
        if (service.Name != RepairKind.WindowsInstaller.ToString() ||
            engineFiles.Name != "Windows Installer engine files" ||
            registration.Name != "Windows Installer service registration" ||
            activity.Name != "Installer activity" || pendingOperations.Name != "Pending file operations" ||
            installerHistory.Name != "Recent installer events" ||
            serviceHistory.Name != "Windows Installer service failure history")
            return Unknown("The expected Windows Installer service, engine, registration, activity, pending-operation or exact history evidence was incomplete or inconsistent.");

        var current = new[] { service, engineFiles, registration, activity, pendingOperations };
        var history = new[] { installerHistory, serviceHistory };
        int currentAttention = current.Count(check => NeedsAttention(check.EffectiveState));
        int currentUnknown = current.Count(check => check.EffectiveState == CheckState.Unknown);
        int historyAttention = history.Count(check => NeedsAttention(check.EffectiveState));
        int historyUnknown = history.Count(check => check.EffectiveState == CheckState.Unknown);
        string counts = $"Reviewed current Windows Installer service state, engine files, service registration, deployment activity and pending file operations with MSI and exact service history. Current evidence: {currentAttention} need attention and {currentUnknown} incomplete. History: {historyAttention} need attention and {historyUnknown} incomplete.";
        const string boundary = " A stopped demand-start Installer service can be normal while idle. Historical failures do not establish a current package or service fault. This does not test application behavior, package source reachability, vendor custom actions or every cached payload. No process, service, package, cache, source, registry value, pending operation, file, policy or restart state changed, and no repair is selected.";
        if (currentUnknown > 0 || historyUnknown > 0)
            return new("Windows Installer operational correlation", CheckState.Unknown,
                counts + " Installer operation cannot be fully correlated until every required observation is complete." + boundary);
        if (currentAttention > 0 || historyAttention > 0)
            return new("Windows Installer operational correlation", CheckState.Attention,
                counts + " Review the exact underlying current and historical results together before selecting a product, registration, service or restart action." + boundary);
        return new("Windows Installer operational correlation", CheckState.Clear,
            counts + " No issue was established within this bounded Installer evidence." + boundary);
    }

    private static bool NeedsAttention(CheckState state) => state is CheckState.Attention or CheckState.Repairable;

    private static WorkflowCheck Unknown(string detail) => new("Windows Installer operational correlation",
        CheckState.Unknown, detail + " No repair is selected and no process, service, package, cache, source, registry value, pending operation, file, policy or restart state changed.");
}
