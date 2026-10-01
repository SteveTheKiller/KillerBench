namespace KillerBench.Engine.Workflows;

public static class DiagnosticReadinessSummary
{
    public static WorkflowCheck Check(CommonDiagnosticScope scope, IReadOnlyList<WorkflowCheck> checks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        if (checks.Count is 0 or > 512 || checks.Any(check => check is null))
            throw new ArgumentException("Readiness summaries require 1 to 512 completed checks.", nameof(checks));
        string boundary = scope switch
        {
            CommonDiagnosticScope.ServicingTransitions => "package, optional-feature and restart evidence",
            CommonDiagnosticScope.NetworkPathReadiness => "adapter, profile, firewall, route, gateway, DNS configuration, DNS cache, DNS Client event, Winsock, TCP setting, Hosts, proxy and operational correlation evidence",
            CommonDiagnosticScope.StorageReadiness => "topology, capacity, quota, reliability, Storage Spaces, current job, dirty-bit, exact service history, event evidence and operational correlation",
            CommonDiagnosticScope.PrintingReadiness => "policy, default selection, queue, driver, port, endpoint, job, extension registration, extension binary, spool storage and event evidence, current Print Spooler state and operational correlation",
            CommonDiagnosticScope.InstallationReadiness => "Installer policy, current service state, engine, service registration, logging, activity, pending file operation, exact service history, MSI cache and source, AppX deployment and Store installation evidence and operational correlation",
            CommonDiagnosticScope.ServiceReadiness => "installed service, startup and dependency configuration, executable presence and path quoting, logon class, registered DLL presence, recovery policy, trigger, repeated transition state, focused current state, Service Control Manager evidence and operational correlation",
            CommonDiagnosticScope.WindowsUpdateReadiness => "update context, exact servicing-chain state, registered service DLL, exact service failure history, restart, servicing, component repair source, capacity, reserved storage, transfer, network and operational correlation evidence",
            CommonDiagnosticScope.BackupReadiness => "Windows Backup summary, File History state and events, VSS history, writer, provider, current shadow-copy, exact service history, restore-point, storage, restart and operational correlation evidence",
            CommonDiagnosticScope.SystemStabilityReadiness => "current vital, device, reliability, hardware-error, crash, shutdown and operational correlation evidence",
            CommonDiagnosticScope.SecurityReadiness => "security hardware, certificate, licensing, protection-product, Defender, restart and operational correlation evidence",
            CommonDiagnosticScope.ApplicationDeploymentReadiness => "registered and provisioned packaged-app, machine and current-account Win32 registration, Installer, AppX, Store, application-failure, restart and operational correlation evidence",
            _ => throw new ArgumentOutOfRangeException(nameof(scope), "The selected area does not define an aggregate readiness verdict.")
        };
        var states = checks.Select(check => check.EffectiveState).ToArray();
        int incomplete = states.Count(state => state == CheckState.Unknown);
        int attention = states.Count(state => state is CheckState.Attention or CheckState.Repairable);
        int clear = states.Count(state => state is CheckState.Clear or CheckState.Information);
        string title = CommonDiagnosticCatalog.Get(scope).Title + " verdict";
        string counts = $"Reviewed {checks.Count} results: {attention} need attention, {incomplete} incomplete and {clear} clear or informational.";
        if (incomplete > 0)
            return new(title, CheckState.Unknown, counts + " Readiness is unknown because required " + boundary + " is incomplete. No repair is authorized by this summary.");
        if (attention > 0)
            return new(title, CheckState.Attention, counts + " One or more findings require review within the bounded " + boundary + ". No repair is selected by this summary.");
        return new(title, CheckState.Clear, counts + " No issue was established within the bounded " + boundary + ". This does not establish overall machine or feature health.");
    }
}
