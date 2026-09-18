namespace KillerBench.Engine;

public sealed record DiagnosticArea(CommonDiagnosticScope Scope, string Title, string Description);

public static class CommonDiagnosticCatalog
{
    private static readonly HashSet<CommonDiagnosticScope> SummaryScopes =
    [
        CommonDiagnosticScope.ServicingTransitions,
        CommonDiagnosticScope.NetworkPathReadiness,
        CommonDiagnosticScope.StorageReadiness,
        CommonDiagnosticScope.PrintingReadiness,
        CommonDiagnosticScope.InstallationReadiness,
        CommonDiagnosticScope.ServiceReadiness,
        CommonDiagnosticScope.WindowsUpdateReadiness,
        CommonDiagnosticScope.BackupReadiness,
        CommonDiagnosticScope.SystemStabilityReadiness,
        CommonDiagnosticScope.SecurityReadiness,
        CommonDiagnosticScope.ApplicationDeploymentReadiness
    ];

    public static IReadOnlyList<DiagnosticArea> All { get; } = Array.AsReadOnly(new DiagnosticArea[]
    {
        new(CommonDiagnosticScope.DeviceProblems, "Device faults", "Reported device configuration errors and driver identities."),
        new(CommonDiagnosticScope.Vitals, "Memory and disk health", "Current memory availability and physical-disk provider health states."),
        new(CommonDiagnosticScope.PrinterDrivers, "Printer drivers", "Print queue assignments matched to registered driver names."),
        new(CommonDiagnosticScope.Security, "Security state", "Reported TPM, Secure Boot and virtualization protection states."),
        new(CommonDiagnosticScope.TimeConfiguration, "Time configuration", "Configured time sources and policy ownership evidence."),
        new(CommonDiagnosticScope.StorageHistory, "Storage history", "Recent storage warnings and errors, without inferring current media failure."),
        new(CommonDiagnosticScope.InstallerHistory, "Installer history", "Recent installer events and recorded outcomes."),
        new(CommonDiagnosticScope.PrintingHistory, "Printing history", "PrintService Admin history plus Operational channel state and available job or rendering history."),
        new(CommonDiagnosticScope.NetworkConfiguration, "Network configuration", "Adapter link states and reported address, gateway and DNS settings."),
        new(CommonDiagnosticScope.ProxyConfiguration, "Proxy configuration", "Reported proxy settings without changing managed configuration."),
        new(CommonDiagnosticScope.ApplicationHistory, "Application history", "Recent application error events; current application operation is not tested."),
        new(CommonDiagnosticScope.ServiceHistory, "Service failure history", "Recent recorded service failures, not proof of a current outage."),
        new(CommonDiagnosticScope.TimeHistory, "Clock service history", "Recent Windows Time service events."),
        new(CommonDiagnosticScope.SystemVolumeCapacity, "System volume capacity", "Available space on the Windows volume."),
        new(CommonDiagnosticScope.ShutdownHistory, "Unexpected shutdown history", "Recorded unexpected shutdown events without inferring their cause."),
        new(CommonDiagnosticScope.BackupHistory, "Backup history", "Recent VSS and volume snapshot events."),
        new(CommonDiagnosticScope.VssWriters, "VSS writer state", "Reported VSS writer states; backup success remains untested."),
        new(CommonDiagnosticScope.BackgroundTransfers, "Background transfers", "Current-account BITS inventory without resuming or deleting jobs."),
        new(CommonDiagnosticScope.PrinterPorts, "Printer port registrations", "Print queue assignments matched to registered port names; reachability is not tested."),
        new(CommonDiagnosticScope.AppPackages, "Registered app packages", "Current-account package status and protection flags; no removal or application launch test."),
        new(CommonDiagnosticScope.ScheduledTasks, "Scheduled tasks", "Visible task states and action counts; execution success is not tested."),
        new(CommonDiagnosticScope.PrintJobs, "Print job status", "Visible job states and intervention needs; document titles are excluded and jobs are preserved."),
        new(CommonDiagnosticScope.StorageReliability, "Storage reliability", "Provider temperature, wear and uncorrected I/O counters; no repair writes."),
        new(CommonDiagnosticScope.WindowsLicensing, "Windows licensing", "Installed Windows licensing states without product keys or activation attempts."),
        new(CommonDiagnosticScope.EventChannels, "Event channel health", "Enabled state, retention mode, size, record count and saturation for core diagnostic event channels."),
        new(CommonDiagnosticScope.ReliabilityHistory, "Reliability history", "Recent application crashes, hangs and unexpected shutdown records grouped by exact identity."),
        new(CommonDiagnosticScope.HardwareErrorHistory, "Hardware error history", "Recent WHEA records for correlation with firmware and vendor diagnostics."),
        new(CommonDiagnosticScope.DriverInstallHistory, "Driver installation history", "Recent SetupAPI failures correlated with the current Plug and Play inventory."),
        new(CommonDiagnosticScope.CrashEvidence, "Crash evidence", "Crash-report events correlated with configured and discovered Windows dump-file metadata."),
        new(CommonDiagnosticScope.FirewallProfiles, "Firewall profiles", "Domain, Private and Public profile enabled settings from the active policy store."),
        new(CommonDiagnosticScope.WindowsUpdateContext, "Windows Update context", "Current execution, applicability, update and component repair source policy, source registration, exact scheduled-task history, update history, Delivery Optimization service state, privacy-bounded current activity and recognized policy surfaces."),
        new(CommonDiagnosticScope.SystemVolumeDirtyBit, "Windows volume dirty bit", "Current dirty-bit state for the Windows volume without scheduling repair or restart."),
        new(CommonDiagnosticScope.InstallerActivity, "Installer activity", "Windows Installer logging policy, current processes and transaction-marker evidence without interrupting deployment work."),
        new(CommonDiagnosticScope.SearchHealth, "Windows Search health", "Current SystemIndex activity and recent Search event history without rebuilding or changing the index."),
        new(CommonDiagnosticScope.SystemRestore, "System Restore points", "Available local restore-point inventory without creating, deleting or applying a point."),
        new(CommonDiagnosticScope.WindowsRecovery, "Windows Recovery Environment", "Current Windows RE status and configured recovery location without changing boot or recovery settings."),
        new(CommonDiagnosticScope.Certificates, "Certificate expiration", "Expired, near-expiry and not-yet-valid personal certificates without exporting keys or changing stores."),
        new(CommonDiagnosticScope.RestartState, "Pending restart state", "Supported Windows restart indicators plus privacy-bounded pending file operation counts without requesting a restart or clearing state."),
        new(CommonDiagnosticScope.ServicingTransitions, "Servicing transitions", "Pending package and optional-feature transitions correlated with restart indicators without changing servicing state."),
        new(CommonDiagnosticScope.NetworkPathReadiness, "Network path readiness", "Active IP address, DHCP lease and Client event history, privacy-bounded neighbor state, per-interface MTU, forwarding, discovery and metrics, protocol bindings, adapter counters, TCP settings, native Winsock protocol and namespace-provider catalog structure, registered provider file presence and signatures, profiles, firewall alignment and operational history, exact firewall and core network service failures, routes, exact gateway probes, DNS client and effective namespace policy, Hosts mappings and proxy settings combined into a bounded operational result without changing network configuration."),
        new(CommonDiagnosticScope.StorageReadiness, "Storage readiness", "Disk, partition, volume, capacity, reliability, page-file, Storage Spaces, VSS shadow storage, dirty-bit, current jobs, exact service history and event evidence combined into a bounded operational result without changing storage."),
        new(CommonDiagnosticScope.PrintingReadiness, "Printing readiness", "Point and Print plus rendering and driver-isolation policy, current Print Spooler state, default selection, queue conditions and operating attributes, driver registrations, models and files, ports, bounded exact-endpoint connections, jobs, extension registrations and binaries, spool storage, Admin and available Operational history, and exact Print Spooler failures correlated without changing printing."),
        new(CommonDiagnosticScope.InstallationReadiness, "Installation readiness", "Windows Installer policy, current service state, engine files, service registration and transaction state, pending file operations correlated with current Installer activity, registered MSI cache visibility and privacy-bounded source metadata, MSI, AppX and Store outcomes, and exact service failures combined into a bounded operational result without changing packages, services or deployment state."),
        new(CommonDiagnosticScope.ServiceReadiness, "Service readiness", "Installed services, startup, dependency, binary, path, account, DLL, recovery, trigger and transition evidence plus focused current states and recent Service Control Manager failures combined into a bounded operational result without changing services or files."),
        new(CommonDiagnosticScope.WindowsUpdateReadiness, "Windows Update readiness", "Update context, exact servicing-chain state, registered service DLLs, exact service failure history, restart, servicing, component repair source, reserved storage, capacity, transfer and network evidence combined into a bounded operational result without searching for or installing updates."),
        new(CommonDiagnosticScope.BackupReadiness, "Backup readiness", "Windows Backup, File History, VSS history, writer state, provider registration, current shadow-copy inventory, exact service history, restore points, storage and restart evidence combined into a bounded operational result without starting or changing a backup."),
        new(CommonDiagnosticScope.SystemStabilityReadiness, "System stability readiness", "Current vitals and device faults combined with reliability, hardware-error, crash and shutdown history in a bounded operational result without changing the machine."),
        new(CommonDiagnosticScope.SecurityReadiness, "Security readiness", "Security hardware, policy, certificates, licensing, registered protection products, Defender history and restart evidence combined into a bounded operational result without changing protection."),
        new(CommonDiagnosticScope.ApplicationDeploymentReadiness, "Application deployment readiness", "Registered and provisioned packaged-app and machine and current-account Win32 registrations, Installer readiness, application failures and restart evidence combined into a bounded operational result without launching or changing applications."),
    });

    public static IReadOnlyList<DiagnosticArea> SummaryAreas { get; } =
        Array.AsReadOnly(All.Where(area => SummaryScopes.Contains(area.Scope)).ToArray());

    public static DiagnosticArea Get(CommonDiagnosticScope scope) => All.FirstOrDefault(area => area.Scope == scope)
        ?? throw new ArgumentOutOfRangeException(nameof(scope));
}
