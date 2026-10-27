using System.Text.Json;

namespace KillerBench.Engine.Workflows;

public enum RepairKind { ComponentStore, SystemFiles, UpdateDataStoreReset, PrintSpooler, PrintSpoolDirectory, WindowsTime, WindowsInstaller, InstallerRegistration, MsiProduct, StorageFileSystem, VolumeOptimization, SystemRestorePoint, ComputerRename, SearchIndexRebuild, OrcaCacheReset, PreviousWindowsInstallationRemoval, BitLockerProtectionResume, BitLockerRecoveryBackup, HighPerformancePowerPlan, PowerTimers, HibernationDisable, PrintQueues, WmiRepository, TimeSynchronization, TimeSourceConfiguration, DhcpClient, DhcpLease, PrinterOffline, DefaultPrinter, UpdateService, BackgroundTransfer, BitsJobRetry, CryptographicServices, ModulesInstaller, WindowsUpdateMedicService, UpdateOrchestrator, DeliveryOptimization, AudioEndpoints, WindowsAudio, WindowsSearch, DefenderSignatures, ComponentCleanup, WindowsManagement, WmiProvider, WindowsEventLog, EventChannelEnable, DnsClient, DnsCache, Workstation, VolumeShadowCopy, SoftwareShadowProvider, WindowsBackupService, BlockLevelBackupEngineService, BaseFilteringEngine, WindowsFirewall, FirewallProfiles, WirelessAutoConfig, WiredAutoConfig, NetworkLocationAwareness, NetworkListService, IpHelper, BluetoothSupport, CameraFrameServer, SmartCardService, BiometricService, ServerService, TaskSchedulerService, WindowsRecoveryEnvironment, DiagnosticPolicyService, WindowsErrorReportingService, WindowsEventCollectorService, RemoteAccessConnectionManagerService, RemoteDesktopServices, WindowsRemoteManagement, SecurityCenterService, NetworkConnectionsService, WinHttpAutoProxyService, WindowsConnectionManagerService, NetworkSetupService, AppReadinessService, StateRepositoryService, UserProfileService, CredentialManagerService, ShellHardwareDetectionService, DeviceInstallService, AppxDeploymentService, ClientLicenseService, StoreInstallService, WindowsLicenseManagerService, PushToInstallService, WebAccountManagerService, MicrosoftAccountSignInService, StorageService, OptimizeDrivesService, WindowsImageAcquisitionService, FunctionDiscoveryProviderHostService, FunctionDiscoveryResourcePublicationService, HumanInterfaceDeviceService, PortableDeviceEnumeratorService, WindowsFontCacheService, DeviceAssociationService, TextInputManagementService, DisplayEnhancementService, ComPlusEventSystem, SystemEventNotificationService, PowerService, PlugAndPlayService, UserManagerService, GroupPolicyClientService, NetworkStoreInterfaceService, RpcEndpointMapperService, RemoteProcedureCallService, DcomServerProcessLauncherService, BackgroundTasksInfrastructureService, CoreMessagingService, PrintJobCancellation }
public sealed record RepairStep(string Tool, string[] Arguments, bool Verification)
{
    /// <summary>Set only when the focused tool for this exact service selected the stopped demand-start repair. Never journaled.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool DemandStartAuthorized { get; init; }
}
public sealed record RepairPlan(RepairKind Kind, string Title, string Effects, IReadOnlyList<RepairStep> Steps);
public sealed record RepairStepResult(RepairStep Step, ToolResult Result)
{
    public bool CommandDispatched { get; init; } = true;
}
public sealed class RepairStepSkippedException(string message, ToolResult evidence) : InvalidOperationException(message)
{
    public ToolResult Evidence { get; } = evidence ?? throw new ArgumentNullException(nameof(evidence));
}
public sealed record RepairResult(string Machine, RepairKind Kind, DateTimeOffset StartedUtc, string Status,
    string JournalPath, IReadOnlyList<RepairStepResult> Steps)
{
    public bool RequiresRestart => Steps?.Any(s => s is { CommandDispatched: true, Result: { TimedOut: false, ExitCode: 3010 } }) == true;
    public int GetExitCode(bool canceled = false) => canceled || Status?.StartsWith("Stopped", StringComparison.Ordinal) == true ? 130 :
        RequiresRestart ? 4 : Repairs.IsVerified(this) ? 0 : 3;
    public IReadOnlyList<string>? SelectedPrinterQueues { get; init; }
    public DnsCacheRecoveryResult? DnsCacheRecovery { get; init; }
    public DnsCacheReconciliationResult? DnsCacheReconciliation { get; init; }
}
public interface IRepairHost
{
    void CheckPrerequisites();
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress);
}
public interface IRepairHostFactory
{
    IRepairHost Create(RepairKind kind, IReadOnlyList<string>? selectedPrinterQueues, CancellationToken token);
}

public static class Repairs
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public static RepairPlan Plan(RepairKind kind) => kind switch
    {
        RepairKind.ComponentStore => new(kind, "Repair component store",
            "DISM may replace component-store content and download repair files using configured Windows repair sources. No automatic restart. There is no one-click rollback.",
            [new("dism.exe", ["/Online", "/Cleanup-Image", "/RestoreHealth", "/NoRestart", "/English"], false),
             new("dism.exe", ["/Online", "/Cleanup-Image", "/ScanHealth", "/NoRestart", "/English"], true)]),
        RepairKind.SystemFiles => new(kind, "Repair system files",
            "SFC scans protected system files and replaces damaged versions where possible, then performs a verification-only scan. A restart may still be needed. There is no one-click rollback.",
            [new("sfc.exe", ["/scannow"], false), new("sfc.exe", ["/verifyonly"], true)]),
        RepairKind.ComponentCleanup => new(kind, "Clean up superseded Windows components",
            "DISM removes previous versions of updated components immediately, without the scheduled task's grace period. No ResetBase, manual WinSxS deletion, policy edits or automatic restart. No automatic undo. A later analysis verifies whether cleanup is still recommended.",
            [ComponentCleanup.Step(false), ComponentCleanup.Step(true)]),
        RepairKind.DeliveryOptimization => new(kind, "Continue Delivery Optimization",
            "Continues only an exact currently paused Delivery Optimization service. The stopped idle state remains unchanged. Startup type, cache contents, peer selection, bandwidth limits, update source and managed policy are preserved. Verification covers stable running state only; download or installation success remains untested.",
            [DeliveryOptimizationRecovery.ContinueStep(), DeliveryOptimizationRecovery.VerifyStep()]),
        RepairKind.DnsCache => new(kind, "Recover a failed hostname lookup",
            "Flushes only the local DNS resolver cache after a fresh HostNotFound or NoData result for the exact selected hostname, then resolves that hostname again. Authoritative records, adapters and DNS settings remain unchanged.", [DnsCacheRecovery.FlushStep()]),
        RepairKind.FirewallProfiles => new(kind, "Enable selected Windows Firewall profiles",
            "Requires explicit Domain, Private or Public profile selections and separate confirmation. The guarded firewall coordinator enables only selected profiles that fresh evidence confirms are disabled and unmanaged. Rules, default actions, logging, IPsec configuration and managed policy are preserved. The generic recipe runner never selects profiles or executes this repair automatically.", []),
        RepairKind.DefaultPrinter => new(kind, "Set one exact default printer",
            "Requires one exact installed queue and separate confirmation. The guarded default-printer coordinator runs only when complete evidence reports no default or conflicting defaults. A coherent different default remains a user preference. Queue inventory and automatic printer management are preserved. The generic recipe runner never selects a queue or executes this repair automatically.", []),
        RepairKind.PrintJobCancellation => new(kind, "Cancel one exact stuck print job",
            "Requires one exact Error or Blocked print job and separate confirmation. The guarded print-job coordinator requires three matching complete inventories, writes durable pending state, cancels only the selected provider identity and verifies every other observed job remains. Queue settings, the spooler, drivers, ports and all other jobs are preserved. The generic recipe runner never selects a job or executes cancellation automatically.", []),
        RepairKind.InstallerRegistration => new(kind, "Repair Windows Installer registration",
            "Requires separate confirmation. The guarded Installer registration coordinator runs only with complete fresh evidence of an exact service-registration defect, trusted engine files, idle Installer activity, clear restart state and administrator access. It uses only the available protected msiexec.exe /regserver commands, writes durable pending state before execution and verifies exact registration afterward. Packages, cached installers, policies and user data are preserved. The generic recipe runner never executes this repair automatically.", []),
        RepairKind.MsiProduct => new(kind, "Repair one exact MSI product",
            "Requires one exact MSI product code and separate confirmation. The guarded MSI coordinator runs only when fresh evidence reports that exact product as Broken or Bad configuration, Installer activity is idle, restart state is clear and administrator access is present. It requests a bounded silent full-file repair, retains a verbose local log and verifies the same product registration. The vendor package may change product files, settings and user data. Other products, cached registrations and Installer policy are preserved. The generic recipe runner never selects a product or executes this repair automatically.", []),
        RepairKind.VolumeOptimization => new(kind, "Optimize one exact fixed volume",
            "Requires one exact drive volume, an explicit Recommended, ReTrim or Defragment method and separate confirmation. The guarded volume coordinator validates identity, mounted fixed-volume state, NTFS or ReFS, healthy operational state and media compatibility, performs a bounded read-only analysis before mutation, writes durable pending state, runs one bounded optimization and verifies the same volume with fresh inspection and analysis. Files, partitions, volume layout, drive letters, Optimize Drives schedule and restart state are preserved. The generic recipe runner never selects a volume or method and never executes this repair automatically.", []),
        RepairKind.SystemRestorePoint => new(kind, "Create one System Restore point",
            "Requires a description, an explicit change reason and separate confirmation. The guarded restore-point coordinator confirms a supported Windows client, complete current inventory, the configured creation interval and administrator access, writes durable pending state, requests one bounded restore point and verifies one new exact inventory entry. Existing restore points, protection settings, frequency policy and restart state are preserved. The generic recipe runner never selects a description or reason and never executes this repair automatically.", []),
        RepairKind.ComputerRename => new(kind, "Rename one standalone computer",
            "Requires one exact proposed name and separate confirmation. The guarded coordinator requires a standalone workgroup role, complete matching active and configured identity, clear restart state and administrator access, writes durable pending state, requests the exact local rename without credentials or domain operations, and verifies the configured target. Windows may require restart, but KillerBench never restarts the machine. Accounts, domain or Entra membership, network settings, applications, files and user data are preserved. Name uniqueness, organizational policy, application dependencies and network propagation remain unverified. The generic recipe runner never selects a name or executes this repair automatically.", []),
        RepairKind.SearchIndexRebuild => new(kind, "Rebuild the Windows Search index",
            "Requires separate confirmation. The guarded Search coordinator requires idle servicing, clear restart state, a running enabled exact service and a safe fixed SystemIndex directory, writes durable pending state, stops only Windows Search, retains the prior index, restarts the service and verifies the new directory and retained data. Indexed locations, policy, startup mode and retained data are preserved. The generic recipe runner never executes this repair automatically.", []),
        RepairKind.OrcaCacheReset => new(kind, "Retain and reset selected Outlook caches",
            "Requires ClearCaches as the sole running-account Outlook reset action and separate confirmation. The guarded coordinator requires complete exact non-reparse cache evidence and three closed-process checks, writes durable pending state, retains each present cache under a unique recovery name and verifies every source and retained directory. Profiles, OST files, credentials, add-ins, packages, settings and retained caches are preserved. The generic recipe runner never selects an Outlook edition or executes this repair automatically.", []),
        RepairKind.PreviousWindowsInstallationRemoval => new(kind, "Remove previous Windows rollback",
            "Requires separate backup confirmation. The guarded coordinator requires complete stable evidence for the exact local Windows.old directory, available operating-system rollback, clear restart state, idle servicing and administrator access. It uses the supported DISM rollback-removal request, never recursively deletes Windows.old, and verifies that Windows reports rollback unavailable. Windows controls any remaining file cleanup. The generic recipe runner never executes this action automatically.", []),
        RepairKind.EventChannelEnable => new(kind, "Enable one diagnostic event channel",
            "Requires one exact supported channel and separate confirmation. The guarded coordinator requires a disabled unmanaged channel and administrator access, writes durable pending state, enables only that channel once and verifies its enabled state while retention, automatic backup, maximum size and every other reported configuration field remain unchanged. Existing events, permissions and all other channels are preserved. Event delivery remains unverified. The generic recipe runner never selects a channel or executes this repair automatically.", []),
        RepairKind.BitLockerProtectionResume => new(kind, "Resume BitLocker protection on one exact volume",
            "Requires one exact drive volume and separate confirmation. The guarded BitLocker coordinator requires a fully encrypted, unlocked volume with suspended protection, a complete stable protector identity set, clear restart state and administrator access, writes durable pending state, requests protection resume once and verifies the same volume and protectors with protection on. Encryption, protectors, recovery backup, policy and restart state are preserved. The generic recipe runner never selects a volume or executes this repair automatically.", []),
        RepairKind.BitLockerRecoveryBackup => new(kind, "Back up one exact BitLocker recovery protector",
            "Requires one exact drive volume, recovery-password protector, new file destination and separate confirmation. The guarded BitLocker coordinator verifies the exact volume and protector before and after one create-new durable write, restricts file access, records pending state before execution and reconciles interrupted attempts without repeating the write. Existing files, protectors, encryption, escrow, policy and restart state are preserved. The generic recipe runner never selects a protector or destination and never executes this repair automatically.", []),
        RepairKind.HighPerformancePowerPlan => new(kind, "Activate the High performance power plan",
            "Requires HighPerformancePower as the only selected declutter area and separate confirmation. The guarded power coordinator requires unmanaged policy, a complete exact plan inventory, the registered standard High performance plan in an inactive state and administrator access, writes durable pending state, requests one activation and verifies that exact plan is active with the registered plan set unchanged. Plan settings, registrations and managed policy are preserved. The generic recipe runner never activates a plan automatically.", []),
        RepairKind.HibernationDisable => new(kind, "Disable Windows hibernation",
            "Requires Hibernation as the only selected declutter area and separate confirmation. The guarded power coordinator requires unmanaged policy, complete active-plan and timer evidence, enabled hibernation, a present hiberfil.sys and administrator access, writes durable pending state, requests disablement once and verifies hibernation off with the file absent and the prior plan and timers preserved. Fast Startup may become unavailable. The generic recipe runner never selects or executes this repair automatically.", []),
        RepairKind.PowerTimers => new(kind, "Change sleep and display timers",
            "Requires SleepAndDisplayTimers as the only selected declutter area, all four AC and battery timeout values and separate confirmation. The guarded power coordinator requires unmanaged policy, complete exact active-plan, timer and hibernation evidence and administrator access, writes durable pending state, applies each selected timer once, and verifies all four values on the same plan with hibernation unchanged. Zero means Never. The generic recipe runner never selects timer values or executes this repair automatically.", []),
        RepairKind.UpdateDataStoreReset => new(kind, "Recover the Windows Update data store",
            "Requires a fresh Windows Update search with one recognized structural data-store HRESULT and separate confirmation. The guarded coordinator rechecks idle Installer activity, clear restart state, administrator access, exact enabled update services and the exact SoftwareDistribution directory, then repeats the same failing search immediately before mutation. It writes durable pending state, stops only services that were running, retains the prior store under a unique recovery name, restores only services it stopped and verifies the replacement store, service states and a fresh successful search. Update source, policy, approvals, packages, startup modes and the retained prior store are preserved. The generic recipe runner never executes this repair automatically.", []),
        RepairKind.BitsJobRetry => new(kind, "Retry one exact BITS transfer",
            "Requires one exact errored running-account BITS job and separate confirmation. The guarded coordinator requires complete owner and progress evidence, repeats the exact job identity, owner, name, total-byte and error-state checks immediately before mutation, writes durable pending state, requests one asynchronous retry and observes the same job afterward. Other jobs, ownership, priority, proxy settings, retry policy, files and completion state are preserved. Transfer completion and owning-application acknowledgment remain unverified. The generic recipe runner never selects a job or executes this repair automatically.", []),
        RepairKind.WindowsRecoveryEnvironment => new(kind, "Enable Windows Recovery Environment",
            "Requests REAgentC to enable the currently configured Windows Recovery Environment, then reads its status again. Existing recovery image location and BCD configuration are retained. No image is downloaded, relocated or replaced, no boot is initiated, and Windows is not restarted. Enabled status does not prove the recovery image can boot or repair this PC.",
            [WindowsRecoveryDiagnostics.EnableStep(), WindowsRecoveryDiagnostics.Step()]),
        RepairKind.PrintSpoolDirectory => new(kind, "Create the missing Windows print spool directory",
            "Creates only the exact Windows default print spool directory when fresh evidence proves it is absent on a fixed drive with at least 1 GiB available. Configuration, permissions, queued documents, printers, ports, drivers and print settings are preserved. Verification confirms the directory now exists at the same non-reparse default path; physical printing remains untested.",
            [PrintSpoolDirectoryRecovery.Step(false), PrintSpoolDirectoryRecovery.Step(true)]),
        _ when ServiceRecovery.ServiceName(kind) is not null => new(kind, "Start " + ServiceRecovery.ServiceName(kind),
            kind == RepairKind.WiredAutoConfig ? "Starts enabled Wired AutoConfig and configured dependencies. Existing wired authentication profiles may activate and change network access. Preserves certificates, credentials, profiles, policy and startup settings. No adapter reset. Verification covers service running state only; 802.1X authentication and connectivity remain untested." :
            kind == RepairKind.WirelessAutoConfig ? "Starts the enabled WLAN AutoConfig service and configured dependencies. Existing wireless profiles may reconnect automatically and change the active network. Preserves profiles, credentials, policy, radio settings and startup type. No profile deletion, adapter reset or driver installation. Verification covers service running state only; wireless authentication and connectivity remain untested." :
            kind is RepairKind.BaseFilteringEngine or RepairKind.WindowsFirewall ? "Starts the selected firewall service and its configured dependencies. Existing filtering policy may take effect and interrupt network or remote access. Preserves firewall rules, profile settings, IPsec policy, service permissions and startup types. Does not reset firewall configuration or stop services. Verification covers running state only, not effective protection or connectivity." :
            kind is RepairKind.VolumeShadowCopy or RepairKind.SoftwareShadowProvider ? "Starts the selected shadow-copy service and its configured dependencies. Stopped demand-start services can be normal when idle. Preserves startup types, provider registrations, shadow copies, shadow storage and backup schedules. Does not stop or restart running services, delete snapshots or re-register components. Verification covers running state only; VSS writers and backup/restore functionality remain unverified." :
            kind is RepairKind.WindowsBackupService or RepairKind.BlockLevelBackupEngineService ? "Starts the selected enabled Windows backup runtime service and its configured dependencies. Existing scheduled or interrupted backup work may resume and can read source volumes, create shadow copies or write to its configured destination. Preserves backup plans, catalogs, schedules, targets, credentials, retention, shadow copies, provider registrations, policy, service permissions and startup type. No backup is created, selected, reconfigured or deleted by KillerBench. Verification covers stable running state only; backup completion, destination access and restore success remain unverified." :
            kind is RepairKind.NetworkLocationAwareness or RepairKind.NetworkListService ? "Starts the selected enabled network-identification service and its configured dependencies. Windows may recalculate network identity and apply a different existing firewall profile. Preserves network profiles, firewall rules, adapter settings, policies and startup types. Verification covers service running state only; network identification and connectivity remain untested." :
            kind == RepairKind.IpHelper ? "Starts the enabled IP Helper service and its configured dependencies. Existing IPv6 transition, port-proxy and network-notification features may become active. Preserves transition configuration, port-proxy rules, adapter settings, policy and startup type. Verification covers service running state only; tunnel, port-proxy and network connectivity remain untested." :
            kind == RepairKind.BluetoothSupport ? "Starts the enabled Bluetooth Support Service and its configured dependencies. Existing paired devices may reconnect. Preserves pairings, radio settings, device configuration, policy and startup type. No device removal, pairing reset or driver installation. Verification covers service running state only; discovery, pairing, audio and data transfer remain untested." :
            kind == RepairKind.CameraFrameServer ? "Starts the enabled Windows Camera Frame Server service and its configured dependencies. An application already requesting camera access may resume using existing permissions. Preserves camera privacy settings, device configuration, drivers, application permissions, policy and startup type. No device reset, permission change or driver installation. Verification covers service running state only; image capture, privacy access and physical camera operation remain untested." :
            kind == RepairKind.SmartCardService ? "Starts the enabled Smart Card service and its configured dependencies. Applications may resume detecting inserted cards under existing policy. Preserves certificates, PINs, credentials, reader and card configuration, drivers, policy and startup type. No card initialization, certificate enrollment, PIN operation, reader reset or driver installation. Verification covers service running state only; card detection, authentication and application access remain untested." :
            kind == RepairKind.BiometricService ? "Starts the enabled Windows Biometric Service and its configured dependencies. Existing Windows Hello and application requests may resume under current policy. Preserves biometric templates, Windows Hello data, credentials, device configuration, application permissions, policy, drivers and startup type. No enrollment, template deletion, sensor reset, permission change or driver installation. Verification covers service running state only; sensor capture, matching, sign-in and application access remain untested." :
            kind == RepairKind.ServerService ? "Starts the enabled Windows Server service and its configured dependencies. Existing local file and printer shares may become reachable immediately under their current permissions and firewall policy. Preserves shares, permissions, SMB signing and encryption settings, firewall rules, credentials, policy and startup type. No share is created or removed. Verification covers stable service running state only; client access, authentication and file transfer remain untested." :
            kind == RepairKind.TaskSchedulerService ? "Starts the enabled Task Scheduler service and its configured dependencies. Existing enabled tasks can begin running under their configured accounts, including tasks whose triggers occurred while the service was unavailable. Preserves every task definition, principal, credential reference, history setting, policy and startup type. No task is enabled, registered, changed, started directly or deleted. Verification covers stable service running state only; individual task execution remains untested." :
            kind == RepairKind.UpdateService ? "Starts the enabled Windows Update service and its configured dependencies. Existing update activity may resume under current policy. Preserves update policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type. KillerBench selects, approves or installs no update. Verification covers stable service running state only; scan, download, installation and restart outcomes remain untested." :
            kind == RepairKind.WindowsUpdateMedicService ? "Starts the enabled Windows Update Medic Service and its configured dependencies. Windows remediation may resume under current policy and can repair update components or trigger update activity. Preserves update policy, source enrollment, schedules, packages, BITS jobs, restart settings, service permissions and startup type. KillerBench selects no update and changes no update setting. Verification covers stable service running state only; remediation, scan, download and installation outcomes remain untested." :
            kind == RepairKind.UpdateOrchestrator ? "Starts the enabled Update Orchestrator service and its configured dependencies. Existing scheduled update activity may resume under current policy and can download or install already approved updates. Preserves update policy, source enrollment, schedules, restart settings, packages, transfer jobs and startup type. No update is selected or approved by this recipe. Verification covers stable service running state only; scan, download and installation outcomes remain untested." :
            kind == RepairKind.DiagnosticPolicyService ? "Starts the enabled Diagnostic Policy Service and its configured dependencies. Existing Windows diagnostic scenarios may resume under current policy. Preserves diagnostic policy, collected troubleshooting data, service permissions and startup type. No troubleshooter is launched and no suggested fix is accepted. Verification covers stable service running state only; problem detection and repair remain untested." :
            kind == RepairKind.WindowsErrorReportingService ? "Starts the enabled Windows Error Reporting service and its configured dependencies. Existing queued reports may resume processing under current policy. Preserves report archives, dump files, consent settings, upload policy, service permissions and startup type. No report or dump is deleted and no upload is explicitly requested. Verification covers stable service running state only; report submission and crash diagnosis remain untested." :
            kind == RepairKind.WindowsEventCollectorService ? "Starts the enabled Windows Event Collector service and its configured dependencies. Existing event subscriptions may resume collecting remote events under their configured credentials and policy. Preserves subscriptions, forwarded events, credentials, channel settings, collector policy, service permissions and startup type. No subscription is created, changed or deleted. Verification covers stable service running state only; remote delivery and subscription health remain untested." :
            kind == RepairKind.RemoteAccessConnectionManagerService ? "Starts the enabled Remote Access Connection Manager service and its configured dependencies. Existing dial-up and VPN requests may resume under current configuration. Preserves connection profiles, credentials, routes, adapters, authentication settings, policy, service permissions and startup type. No connection is initiated and no profile, route or adapter is changed. Verification covers stable service running state only; authentication and connectivity remain untested." :
            kind == RepairKind.RemoteDesktopServices ? "Starts the enabled Remote Desktop Services service and its configured dependencies. An existing listener may become available immediately under current policy, Network Level Authentication, firewall rules and permissions. Preserves Remote Desktop enablement, listeners, certificates, ports, firewall rules, NLA, permissions, sessions, policy, service permissions and startup type. No listener, firewall rule, account access, certificate or session is created or changed. Verification covers stable service running state only; remote reachability, authentication and session creation remain untested." :
            kind == RepairKind.WindowsRemoteManagement ? "Starts the enabled Windows Remote Management service and its configured dependencies. Existing WS-Management listeners may become reachable immediately under current authentication, certificate, firewall, TrustedHosts and policy settings. Preserves listeners, addresses, ports, certificates, authentication, encryption, TrustedHosts, firewall rules, permissions, quotas, policy, service permissions and startup type. No listener, firewall rule, trust entry, certificate, credential or remote session is created or changed. Verification covers stable service running state only; listener reachability, authentication and remote command execution remain untested." :
            kind == RepairKind.SecurityCenterService ? "Starts the enabled Windows Security Center service and its configured dependencies. Existing security status monitoring and notifications may resume under current configuration. Preserves antivirus products, provider registrations, protection settings, exclusions, notifications, policy, service permissions and startup type. No scan, signature update, product registration or protection change is requested. Verification covers stable service running state only; security-product health remains untested." :
            kind == RepairKind.NetworkConnectionsService ? "Starts the enabled Network Connections service and its configured dependencies. Existing network-management interfaces may resume showing and configuring connections under current policy. Preserves adapters, addresses, routes, DNS settings, connection profiles, credentials, firewall configuration, policy, service permissions and startup type. No connection, adapter, address, route, profile or network setting is changed. Verification covers stable service running state only; adapter operation and connectivity remain untested." :
            kind == RepairKind.WinHttpAutoProxyService ? "Starts the exact stopped WinHTTP Web Proxy Auto-Discovery service and its configured dependencies for this focused recovery. Existing applications may resume WPAD or PAC discovery under their current proxy and policy settings. Preserves proxy definitions, PAC URLs, credentials, network settings, firewall configuration, policy, service permissions and demand-start startup type. No URL is fetched and no proxy, cache, credential or network setting is changed. Verification covers stable service running state only; discovery, reachability, authentication and application use remain untested." :
            kind == RepairKind.WindowsConnectionManagerService ? "Starts the enabled Windows Connection Manager service and its configured dependencies. Windows may resume automatic connection decisions using existing profiles and policy. Preserves adapters, connection profiles, credentials, metering, cost, roaming, firewall, VPN, wireless, wired-authentication and policy settings, service permissions and startup type. No profile, adapter or setting is created, removed or reset. Verification covers stable service running state only; connection selection, authentication and connectivity remain untested." :
            kind == RepairKind.NetworkSetupService ? "Starts the enabled Network Setup Service and its configured dependencies. Existing pending network-driver installation or configuration work may resume under current Windows policy. Preserves adapters, drivers, driver packages, bindings, addresses, routes, DNS settings, connection profiles, credentials, firewall configuration, policy, service permissions and startup type. No driver, device, adapter, binding or network setting is selected or changed by KillerBench. Verification covers stable service running state only; pending setup completion and connectivity remain untested." :
            kind == RepairKind.AppReadinessService ? "Starts the enabled App Readiness service and its configured dependencies. Existing first-sign-in and application provisioning work may resume under current Windows policy. Preserves installed and provisioned packages, registrations, licenses, user profiles, application data, policy, service permissions and startup type. No package is installed, removed, reset, re-registered or selected by KillerBench. Verification covers stable service running state only; sign-in, provisioning and application launch remain untested." :
            kind == RepairKind.StateRepositoryService ? "Starts the enabled State Repository Service and its configured dependencies. Existing application-model state requests may resume under current Windows configuration. Preserves application registrations, packages, licenses, user profiles, browser and application data, repository content, policy, service permissions and startup type. No repository reset, re-registration, package change or data deletion is requested. Verification covers stable service running state only; repository consistency and application operation remain untested." :
            kind == RepairKind.AppxDeploymentService ? "Starts the enabled AppX Deployment Service and its configured dependencies. Existing deployment, registration or servicing requests may resume under current Windows policy. Preserves installed and provisioned packages, registrations, licenses, user profiles, application data, repository content, policy, service permissions and startup type. No package is installed, removed, reset, re-registered or selected by KillerBench. Verification covers stable service running state only; deployment, registration and application launch remain untested." :
            kind == RepairKind.ClientLicenseService ? "Starts the enabled Client License Service and its configured dependencies. Existing Store application license requests may resume under current Windows policy. Preserves installed and provisioned packages, registrations, licenses, tokens, accounts, user profiles, application data, policy, service permissions and startup type. No package, license, token, account or registration is created, changed, reset, removed or selected by KillerBench. Verification covers stable service running state only; license validation and application launch remain untested." :
            kind == RepairKind.StoreInstallService ? "Starts the enabled Microsoft Store Install Service and its configured dependencies. Existing Store download and installation requests may resume under current Windows policy. Preserves installed and provisioned packages, registrations, licenses, accounts, application data, download state, sources, policy, service permissions and startup type. No package, download, license, account, source or installation is created, changed, reset, removed, approved or selected by KillerBench. Verification covers stable service running state only; download, installation, license and application outcomes remain untested." :
            kind == RepairKind.WindowsLicenseManagerService ? "Starts the enabled Windows License Manager Service and its configured dependencies. Existing application license checks may resume under current Windows policy. Preserves installed and provisioned packages, registrations, licenses, tokens, accounts, application data, policy, service permissions and startup type. No package, license, token, account, registration or entitlement is created, changed, reset, removed or selected by KillerBench. Verification covers stable service running state only; license validation, entitlement and application launch remain untested." :
            kind == RepairKind.PushToInstallService ? "Starts the enabled Windows PushToInstall Service and its configured dependencies. An existing previously approved remote installation request may resume under current Windows policy. Preserves installed and provisioned packages, registrations, licenses, accounts, application data, queued requests, sources, policy, service permissions and startup type. KillerBench creates, approves and selects no remote request, package, source, account or installation. Verification covers stable service running state only; request processing, download, installation and application outcomes remain untested." :
            kind == RepairKind.WebAccountManagerService ? "Starts the enabled Web Account Manager service and its configured dependencies. Existing applications may resume account and token requests under current Windows policy. Preserves accounts, credentials, tokens, cookies, application registrations, permissions, sign-in state, policy, service permissions and startup type. No account, credential, token, cookie, permission or sign-in operation is collected, displayed, created, changed, reset, removed or selected by KillerBench. Verification covers stable service running state only; authentication, token issuance and application access remain untested." :
            kind == RepairKind.MicrosoftAccountSignInService ? "Starts the enabled Microsoft Account Sign-in Assistant service and its configured dependencies. Existing Microsoft account sign-in requests may resume under current Windows policy. Preserves accounts, credentials, tokens, device registration, application data, permissions, sign-in state, policy, service permissions and startup type. No account, credential, token, registration, permission or sign-in operation is collected, displayed, created, changed, reset, removed or selected by KillerBench. Verification covers stable service running state only; authentication, account connectivity and application access remain untested." :
            kind == RepairKind.StorageService ? "Starts the enabled Storage Service and its configured dependencies. Existing Windows storage notifications and policy-managed storage activity may resume. Preserves volumes, partitions, filesystems, mount points, devices, drivers, Storage Sense settings, maintenance policy, service permissions and startup type. No volume, partition, mount point, file, device, driver or storage setting is created, changed, removed, formatted or selected by KillerBench. Verification covers stable service running state only; device, volume and storage-policy operation remain untested." :
            kind == RepairKind.OptimizeDrivesService ? "Starts the enabled Optimize Drives service and its configured dependencies. Existing Windows-scheduled optimization activity may resume under current maintenance policy. Preserves volumes, filesystems, data, optimization schedules, maintenance policy, service permissions and startup type. KillerBench does not select a volume, analyze, trim, defragment, optimize or schedule any drive. Verification covers stable service running state only; analysis, optimization and device health remain untested." :
            kind == RepairKind.WindowsImageAcquisitionService ? "Starts the enabled Windows Image Acquisition service and its configured dependencies. Existing scanner and still-image acquisition requests may resume under current application permissions. Preserves devices, drivers, image data, application permissions, privacy settings, transfer settings, policy, service permissions and startup type. No device, driver, image, permission, transfer or acquisition operation is selected by KillerBench. Verification covers stable service running state only; scanner, camera, transfer and application operation remain untested." :
            kind == RepairKind.FunctionDiscoveryProviderHostService ? "Starts the enabled Function Discovery Provider Host service and its configured dependencies. Existing applications may resume discovering network devices through installed providers. Preserves network profiles, shares, devices, providers, firewall rules, discovery settings, credentials, policy, service permissions and startup type. No device, share, provider, firewall rule or discovery setting is created or changed. Verification covers stable service running state only; provider health, device discovery and connectivity remain untested." :
            kind == RepairKind.FunctionDiscoveryResourcePublicationService ? "Starts the enabled Function Discovery Resource Publication service and its configured dependencies. This PC may become discoverable through existing network discovery and firewall settings. Preserves network profiles, shares, published resources, firewall rules, discovery settings, credentials, policy, service permissions and startup type. No share, resource, firewall rule or discovery setting is created or changed. Verification covers stable service running state only; publication, visibility, access and connectivity remain untested." :
            kind == RepairKind.HumanInterfaceDeviceService ? "Starts the enabled Human Interface Device Service and its configured dependencies. Existing hot buttons, media controls and other HID features may resume. Preserves devices, drivers, button mappings, accessibility settings, input settings, policy, service permissions and startup type. No device, driver, mapping or input setting is changed. Verification covers stable service running state only; button, control and application behavior remain untested." :
            kind == RepairKind.PortableDeviceEnumeratorService ? "Starts the enabled Portable Device Enumerator Service and its configured dependencies. Existing applications may resume detecting and accessing already connected portable devices under current permissions and policy. Preserves devices, drivers, files, libraries, synchronization settings, permissions, policy, service permissions and startup type. No device, file, library, synchronization or transfer operation is selected. Verification covers stable service running state only; enumeration, access, synchronization and transfer remain untested." :
            kind == RepairKind.WindowsFontCacheService ? "Starts the enabled Windows Font Cache Service and its configured dependencies. Existing applications may resume using the current font cache. Preserves installed fonts, font files, application data, cache files, policy, service permissions and startup type. No font is installed or removed and no cache is deleted or rebuilt. Verification covers stable service running state only; text rendering and application behavior remain untested." :
            kind == RepairKind.DeviceAssociationService ? "Starts the enabled Device Association Service and its configured dependencies. Existing Windows-requested device association work may resume under current policy. Preserves paired and associated devices, drivers, credentials, metadata, permissions, policy, service permissions and startup type. No device is paired, removed, reset or selected and no driver is installed. Verification covers stable service running state only; discovery, pairing, association and device operation remain untested." :
            kind == RepairKind.TextInputManagementService ? "Starts the enabled Text Input Management Service and its configured dependencies. Existing touch keyboard, handwriting, emoji and advanced text-input requests may resume under current settings. Preserves dictionaries, handwriting data, language and keyboard settings, accessibility settings, privacy settings, devices, drivers, policy, service permissions and startup type. No text input, keystroke or handwriting content is collected and no setting is changed. Verification covers stable service running state only; text entry, recognition and device operation remain untested." :
            kind == RepairKind.DisplayEnhancementService ? "Starts the enabled Display Enhancement Service and its configured dependencies. Existing display brightness, color and enhancement requests may resume under current hardware and policy. Preserves display configuration, brightness, color profiles, HDR settings, devices, drivers, power settings, policy, service permissions and startup type. No display, color, brightness, HDR, driver or power setting is changed. Verification covers stable service running state only; enhancement behavior, picture quality and physical display operation remain untested." :
            kind == RepairKind.ComPlusEventSystem ? "Starts the enabled COM+ Event System service and its configured dependencies. Existing COM+ subscriptions and notification consumers may resume under current configuration. Preserves COM+ catalogs, subscriptions, registrations, consumers, accounts, permissions, policy, service permissions and startup type. No subscription, registration, catalog, account, permission or policy is changed. Verification covers stable service running state only; event publication and notification delivery remain untested." :
            kind == RepairKind.SystemEventNotificationService ? "Starts the enabled System Event Notification Service after the COM+ Event System prerequisite is verified. Existing logon, network and power notification consumers may resume under current configuration. Preserves COM+ catalogs, subscriptions, notification registrations, consumers, accounts, permissions, policy, service permissions and startup types. No subscription, registration, consumer, account, permission or policy is changed. Verification covers stable service running state only; end-to-end notification delivery remains untested." :
            kind == RepairKind.PowerService ? "Starts the enabled Power service and its configured dependencies. Existing Windows power notifications and policy application may resume. Preserves power plans, timer values, hibernation, device power settings, policy, service permissions and startup type. No power setting or plan is changed. Verification covers stable service running state only; sleep, resume and hardware power behavior remain untested." :
            kind == RepairKind.PlugAndPlayService ? "Starts the enabled Plug and Play service and its configured dependencies. Existing Windows device enumeration and configuration requests may resume. Preserves devices, drivers, packages, configuration, policy, service permissions and startup type. No device scan, install, removal, reset or driver action is requested. Verification covers stable service running state only; device detection and operation remain untested." :
            kind == RepairKind.UserManagerService ? "Starts the enabled User Manager service and its configured dependencies. Existing sign-in and account requests may resume. Preserves accounts, credentials, tokens, profiles, permissions, policy, service permissions and startup type. No account, credential, token, profile or permission is changed. Verification covers stable service running state only; sign-in and account operation remain untested." :
            kind == RepairKind.GroupPolicyClientService ? "Starts the enabled Group Policy Client service and its configured dependencies. Existing policy processing may resume under the current domain and local configuration. Preserves policy objects, registry settings, scripts, accounts, permissions, service permissions and startup type. No policy refresh or policy change is requested. Verification covers stable service running state only; policy application remains untested." :
            kind == RepairKind.NetworkStoreInterfaceService ? "Starts the enabled Network Store Interface service and its configured dependencies. Existing network-state notifications may resume. Preserves adapters, addresses, routes, DNS settings, profiles, firewall configuration, policy, service permissions and startup type. No network setting or connection is changed. Verification covers stable service running state only; notification delivery and connectivity remain untested." :
            kind == RepairKind.RpcEndpointMapperService ? "Starts the enabled RPC Endpoint Mapper service and its configured dependencies. Existing endpoint lookup requests may resume under current Windows configuration. Preserves endpoint registrations, bindings, accounts, permissions, firewall configuration, policy, service permissions and startup type. No endpoint, binding, account, permission, firewall or policy setting is changed. Verification covers stable service running state only; endpoint resolution and dependent application behavior remain untested." :
            kind == RepairKind.RemoteProcedureCallService ? "Starts the enabled Remote Procedure Call service and its configured dependencies. Existing Windows and application RPC requests may resume under current configuration. Preserves registrations, endpoints, bindings, accounts, permissions, firewall configuration, policy, service permissions and startup type. No registration, endpoint, binding, account, permission, firewall or policy setting is changed. Verification covers stable service running state only; RPC calls and dependent application behavior remain untested." :
            kind == RepairKind.DcomServerProcessLauncherService ? "Starts the enabled DCOM Server Process Launcher service and its configured dependencies. Existing Windows-requested COM and DCOM activation may resume under current configuration. Preserves COM registrations, launch and access permissions, accounts, firewall configuration, policy, service permissions and startup type. No component is launched by KillerBench and no registration, permission, account, firewall or policy setting is changed. Verification covers stable service running state only; component activation and dependent application behavior remain untested." :
            kind == RepairKind.BackgroundTasksInfrastructureService ? "Starts the enabled Background Tasks Infrastructure Service and its configured dependencies. Existing Windows background task requests may resume under current configuration. Preserves task registrations, schedules, application data, accounts, permissions, policy, service permissions and startup type. No task is created, changed, selected or launched by KillerBench. Verification covers stable service running state only; background task execution and application behavior remain untested." :
            kind == RepairKind.CoreMessagingService ? "Starts the enabled CoreMessaging service and its configured dependencies. Existing Windows message-routing requests may resume under current configuration. Preserves registrations, endpoints, application data, accounts, permissions, policy, service permissions and startup type. No registration, endpoint, application, account, permission or policy setting is changed. Verification covers stable service running state only; message delivery and dependent application behavior remain untested." :
            kind == RepairKind.UserProfileService ? "Starts the enabled User Profile Service and its configured dependencies. Existing sign-in and profile requests may resume under current Windows configuration. Preserves profile folders, registry hives, permissions, profile-list registrations, credentials, user data, policy, service permissions and startup type. No profile is created, loaded, unloaded, copied, renamed, reset or deleted. Verification covers stable service running state only; sign-in, profile loading and user-data access remain untested." :
            kind == RepairKind.CredentialManagerService ? "Starts the enabled Credential Manager service and its configured dependencies. Existing applications may resume requests for credentials already stored under current Windows policy. Preserves vault contents, passwords, passkeys, certificates, tokens, credential registrations, permissions, policy, service permissions and startup type. No credential is collected, displayed, exported, created, changed, validated or deleted. Verification covers stable service running state only; credential retrieval, authentication and application access remain untested." :
            kind == RepairKind.ShellHardwareDetectionService ? "Starts the enabled Shell Hardware Detection service and its configured dependencies. Windows may resume existing AutoPlay and hardware-arrival notifications under current policy. Preserves devices, drivers, volumes, mount points, AutoPlay settings, handlers, policies, service permissions and startup type. No device, driver, volume, mount point, handler or AutoPlay setting is created, changed, opened or removed. Verification covers stable service running state only; notification delivery and device operation remain untested." :
            kind == RepairKind.DeviceInstallService ? "Starts the enabled Device Install Service and its configured dependencies. Existing Windows-requested device installation work may resume under current policy. Preserves devices, drivers, driver packages, device configuration, bindings, class settings, policy, service permissions and startup type. KillerBench selects no device, driver or package and requests no installation, removal, rollback or rescan. Verification covers stable service running state only; device installation and operation remain untested." :
            kind == RepairKind.Workstation ? "Starts the Windows SMB client service; Windows may start its configured dependencies and applications may reconnect to network resources. Preserves startup type, credentials, mapped drives, signing, encryption and guest-access policy. Verification covers service running state, not remote share access." :
            "Starts only the selected service and verifies it is running. Preserves startup type, print jobs, time-server settings and installer configuration. Does not prove the feature works end to end.",
            [new("sc.exe", ["start", ServiceRecovery.ServiceName(kind)!], false), new("sc.exe", ["query", ServiceRecovery.ServiceName(kind)!], true)]),
        RepairKind.StorageFileSystem => new(kind, "Repair an exact NTFS volume", "Requires an exact fixed NTFS volume, current complete corruption evidence, an explicit SpotFix or OfflineScanAndFix mode and confirmation of either dismount or restart effects. Execution uses the separate guarded filesystem coordinator with durable preparation, bounded CHKDSK and fresh read-only verification. The generic recipe runner never selects these options or executes this repair automatically.", []),
        RepairKind.WmiProvider => new(kind, "Read-only WMI provider check", "No provider registration, namespace changes or automatic repair.", []),
        RepairKind.DefenderSignatures => new(kind, "Refresh Defender signatures", "Updates outdated security intelligence only while Defender reports active protection. Uses configured sources and preserves protection settings, exclusions and policy. No automatic rollback or malware scan.", [DefenderRecovery.Step(false), DefenderRecovery.Step(true)]),
        RepairKind.PrintQueues => new(kind, "Resume selected paused local print queues", "Requires complete fresh queue and print-job evidence that ties a visible waiting job to each exact selected paused local queue. The guarded selected-queue plan preserves jobs and verifies only those queues. The generic recipe runner never resumes every paused queue.", []),
        RepairKind.WmiRepository => new(kind, "Salvage WMI repository", "Rebuilds an inconsistent repository and merges readable content. WMI clients may be interrupted, and unreadable provider registrations may need reinstalling. No repository reset or deletion. There is no automatic rollback.", [WmiRecovery.Step(false), WmiRecovery.Step(true)]),
        RepairKind.TimeSynchronization => new(kind, "Synchronize Windows Time", "Starts Windows Time only when a focused current status check proves the demand-start service is required, then requests synchronization from existing configured sources and may adjust the system clock. Preserves startup type, domain hierarchy, policy and server configuration. Clock changes are not rolled back.", [TimeRecovery.ServiceStartStep(), TimeRecovery.Step(false), TimeRecovery.Step(true)]),
        RepairKind.TimeSourceConfiguration => new(kind, "Configure Windows Time source",
            "Requires an explicit Domain hierarchy or Custom NTP peer selection and separate confirmation. The guarded coordinator requires complete stable domain-role, policy-ownership and local-mode evidence, writes durable pending state before mutation, applies only the selected source, requests one synchronization and verifies the exact stored configuration plus Windows-reported synchronization. Startup type, unselected peers, managed policy and unrelated time settings are preserved. Absolute clock accuracy and source trust remain unverified. The generic recipe runner never selects a source or executes this repair automatically.", []),
        RepairKind.DhcpLease => new(kind, "Renew missing or expired DHCP leases", "Renews only connected physical DHCP adapters lacking a usable IPv4 address or holding a coherent expired lease with DHCP server evidence. May change the leased address and interrupt connections. No release, static address edits, DNS override or network reset. A server may supply different lease settings.", [DhcpRecovery.Step(false), DhcpRecovery.Step(true)]),
        RepairKind.PrinterOffline => new(kind, "Clear local printer offline settings", "Clears WorkOffline on affected local queues and may release waiting jobs. Remote printer connections are excluded. Jobs are not deleted. Physical device availability remains untested.", [PrinterOfflineRecovery.Step(false), PrinterOfflineRecovery.Step(true)]),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string Preview(RepairKind kind)
    {
        var plan = Plan(kind);
        return $"{plan.Title}\nTARGET: THIS PC ({Environment.MachineName})\n\n{plan.Effects}\n\n" +
            string.Join("\n", plan.Steps.Select(s => s.Tool + " " + string.Join(" ", s.Arguments))) +
            "\n\nAdministrator required. Pending reboot blocks execution. Stop requests cancellation and skips remaining steps. Windows may continue work after interruption; changes are not rolled back. Output is saved locally. This targets only the PC named above.";
    }

    public static Task<RepairResult> RunAsync(RepairKind kind, string journalDirectory, IProgress<string>? progress = null,
        IRepairHost? host = null, RepairRecoveryStore? recoveryStore = null, IReadOnlyList<string>? selectedPrinterQueues = null,
        CancellationToken cancellationToken = default)
        => RunAsync(kind, journalDirectory, TimeSpan.FromMinutes(65), progress, host, recoveryStore, selectedPrinterQueues, cancellationToken: cancellationToken);

    /// <summary>Runs a repair selected by its focused tool. demandStartAuthorized carries that selection to a stopped demand-start service start.</summary>
    public static Task<RepairResult> RunAsync(RepairKind kind, string journalDirectory, bool demandStartAuthorized,
        IProgress<string>? progress = null, IRepairHost? host = null, CancellationToken cancellationToken = default)
        => RunAsync(kind, journalDirectory, TimeSpan.FromMinutes(65), progress, host, null, null, cancellationToken, demandStartAuthorized);

    public static Task<RepairResult> RunAsync(RepairKind kind, string journalDirectory, TimeSpan commandTimeout,
        IProgress<string>? progress = null, IRepairHost? host = null, RepairRecoveryStore? recoveryStore = null,
        IReadOnlyList<string>? selectedPrinterQueues = null, CancellationToken cancellationToken = default)
        => RunAsync(kind, journalDirectory, commandTimeout, progress, host, recoveryStore, selectedPrinterQueues, cancellationToken, false);

    private static async Task<RepairResult> RunAsync(RepairKind kind, string journalDirectory, TimeSpan commandTimeout,
        IProgress<string>? progress, IRepairHost? host, RepairRecoveryStore? recoveryStore,
        IReadOnlyList<string>? selectedPrinterQueues, CancellationToken cancellationToken, bool demandStartAuthorized)
    {
        var commandLimit = commandTimeout;
        if (commandLimit <= TimeSpan.Zero || commandLimit > TimeSpan.FromHours(2))
            throw new ArgumentOutOfRangeException(nameof(commandTimeout));
        progress = progress is null ? null : new DiagnosticProgress(progress);
        var selected = selectedPrinterQueues?.ToArray();
        if (selected is not null && kind is not (RepairKind.PrinterOffline or RepairKind.PrintQueues))
            throw new ArgumentException("Queue selection applies only to selected printer recovery.");
        var selectedPlan = selected is null ? null : kind == RepairKind.PrintQueues
            ? PrintRecovery.Plan(selected) : PrinterOfflineRecovery.Plan(selected);
        if (!await Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("A repair is already running.");
        try
        {
            var plan = selectedPlan ?? Plan(kind);
            if (demandStartAuthorized && ServiceRecovery.IsDemandStartKind(kind))
                plan = plan with { Steps = [.. plan.Steps.Select(step => step.Verification ? step : step with { DemandStartAuthorized = true })] };
            if (plan.Steps.Count == 0) throw new InvalidOperationException("This recipe is diagnostic only and has no repair command.");
            using var executionLock = host is null ? AcquireExecutionLock() : null;
            recoveryStore ??= host is null ? RepairRecoveryStore.ForCurrentUser() : null;
            using var nativeStop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            host ??= PlatformProviders.Create<IRepairHostFactory>().Create(kind, selected, nativeStop.Token);
            host.CheckPrerequisites();
            if (selected is not null)
            {
                var diagnosis = await RunCommand(kind == RepairKind.PrintQueues
                    ? PrintRecovery.Step(true, selected) : PrinterOfflineRecovery.Step(true, selected)).ConfigureAwait(false);
                if (kind == RepairKind.PrintQueues)
                {
                    var snapshot = PrintRecovery.ReadIdentities(diagnosis);
                    if (snapshot is null || snapshot.Targets.Length != 0 || snapshot.Names.Length != selected.Length ||
                        snapshot.PausedNames.Length != selected.Length || !selected.All(snapshot.PausedNames.Contains))
                        throw new InvalidOperationException("Selected queues are no longer exact paused local queues. No change started.");
                }
                else
                {
                    var snapshot = PrinterOfflineRecovery.Read(diagnosis);
                    if (snapshot is null || snapshot.Targets.Length != 0 || snapshot.Queues.Count != selected.Length ||
                        !selected.All(snapshot.Queues.ContainsKey) || !snapshot.Queues.Values.Any(offline => offline))
                        throw new InvalidOperationException("Selected queues do not have confirmed offline settings requiring recovery. No change started.");
                }
                host.CheckPrerequisites();
            }
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(journalDirectory);
            string path = Path.Combine(journalDirectory, "repair-" + Guid.NewGuid().ToString("N") + ".json");
            var results = new List<RepairStepResult>();
            var started = DateTimeOffset.UtcNow;
            string status = "Prepared";
            bool recoveryPrepared = false;
            bool mutationDispatched = false;
            await Save();
            progress?.Report("Repair journal: " + path);
            foreach (var step in plan.Steps)
            {
                if (cancellationToken.IsCancellationRequested) { status = "Stopped before next step; verification may be incomplete"; break; }
                if (recoveryStore is not null && !step.Verification)
                {
                    if (!recoveryPrepared)
                    {
                        await recoveryStore.BeginAsync(path, cancellationToken).ConfigureAwait(false);
                        recoveryPrepared = true;
                    }
                }
                status = "Running: " + step.Tool + " " + string.Join(" ", step.Arguments);
                await Save();
                progress?.Report(status);
                ToolResult result;
                try { result = await RunCommand(step).ConfigureAwait(false); }
                catch (RepairStepSkippedException skipped)
                {
                    results.Add(new(step, skipped.Evidence) { CommandDispatched = false });
                    status = "Repair step skipped before command dispatch: " + skipped.Message;
                    if (recoveryStore is not null && recoveryPrepared && !mutationDispatched)
                    {
                        await recoveryStore.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
                        recoveryPrepared = false;
                    }
                    await Save();
                    continue;
                }
                catch (RepairDispatchDeferredException error)
                {
                    status = mutationDispatched
                        ? "Repair stopped before the next command: " + error.Message
                        : "Repair skipped before command dispatch: " + error.Message;
                    if (recoveryStore is not null && recoveryPrepared && !mutationDispatched)
                    {
                        await recoveryStore.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
                        recoveryPrepared = false;
                    }
                    await Save();
                    return Current();
                }
                catch (TimeoutException)
                {
                    status = "Timed out waiting for command evidence; work may still be running. Remaining steps skipped; reassessment required.";
                    await Save();
                    return Current();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    status = "Stopped during command execution; machine changes may have occurred. Review the pending recovery record before retrying.";
                    await Save();
                    return Current();
                }
                catch (OperationCanceledException)
                {
                    status = "Execution interrupted by command provider; completion is unverified. Remaining steps skipped; reassessment required.";
                    await Save();
                    return Current();
                }
                catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    status = "Execution failed: " + e.Message;
                    if (!step.Verification)
                        status += (status.EndsWith('.') ? "" : ".") + " Machine changes may have occurred; remaining steps skipped; reassessment required.";
                    await Save();
                    return Current();
                }
                results.Add(new(step, result));
                if (!step.Verification) mutationDispatched = true;
                if (result.TimedOut) { status = "Timed out; Windows may still be completing work. Remaining steps skipped; reassessment required"; break; }
                if (result.ExitCode == 3010) { status = "Restart required; verification deferred"; break; }
                if (result.ExitCode != 0) { status = $"Command failed with exit {result.ExitCode}; remaining steps skipped"; break; }
                status = step.Verification ? VerificationStatus(kind, result) : "Repair command completed; verification pending";
                if (step.Verification && kind == RepairKind.DhcpLease)
                    status = DhcpRecovery.Verify(results) ? "Verification reported renewed adapters satisfy their retained address or lease-expiry conditions; connectivity remains untested" : "Renewed adapter lease state remains unverified; review required";
                if (step.Verification && kind == RepairKind.PrinterOffline)
                    status = (selected is null ? PrinterOfflineRecovery.Verify(results) : PrinterOfflineRecovery.VerifySelected(selected, results)) ? "Verification reported changed queues no longer use offline mode; physical printing remains untested" : "Changed queue settings remain unverified; review required";
                if (step.Verification && kind == RepairKind.PrintQueues)
                    status = (selected is not null && PrintRecovery.VerifySelected(selected, results)) ? "Verification reported selected resumed queues remain present and unpaused; physical printing remains untested" : "Selected resumed queues remain unverified; review required";
                if (step.Verification && results.Any(r => r.Result.Truncated))
                    status = "Verification incomplete: command output was truncated. Review the journal and reassess before another repair.";
                await Save();
                if (step.Verification && recoveryStore is not null && recoveryPrepared && IsVerified(Current()))
                {
                    await recoveryStore.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    recoveryPrepared = false;
                }
            }
            await Save();
            progress?.Report(status);
            return Current();

            RepairResult Current() => new(Environment.MachineName, kind, started, status, path, [.. results])
                { SelectedPrinterQueues = selected is null ? null : Array.AsReadOnly(selected) };
            async Task Save()
            {
                await JournalPersistence.WriteAsync(path, JsonSerializer.Serialize(Current(), Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
            }
            async Task<ToolResult> RunCommand(RepairStep step)
            {
                var pending = host.RunAsync(step, progress) ?? throw new InvalidOperationException("Repair provider returned no command task; execution is unverified.");
                try
                {
                    return await pending.WaitAsync(commandLimit, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Repair provider returned no command evidence; execution is unverified.");
                }
                catch (TimeoutException) { nativeStop.Cancel(); throw; }
            }
        }
        finally { Gate.Release(); }
    }

    private static FileStream AcquireExecutionLock()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs");
        Directory.CreateDirectory(folder);
        return new FileStream(Path.Combine(folder, "execution.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    public static bool IsVerified(RepairResult result)
    {
        if (result is null || !Enum.IsDefined(result.Kind) || result.Steps is null || result.Steps.Any(step =>
            step is null || step.Step is null || step.Result is null || step.Step.Arguments is null || step.Result.Truncated ||
            !string.IsNullOrWhiteSpace(step.Result.Error))) return false;
        if (result.Kind == RepairKind.DnsCache)
        {
            if (result.DnsCacheReconciliation is { Outcome: DnsCacheReconciliationOutcome.Resolved,
                    Pending.Before: { } before, Current.State: DnsLookupState.Resolved, ArchivePath: not null } review)
                return result.DnsCacheRecovery is null && result.Steps.Count == 0 && review.Current.Addresses is { Length: > 0 } &&
                    string.Equals(before.Host, review.Current.Host, StringComparison.OrdinalIgnoreCase);
            return result.DnsCacheReconciliation is null &&
                result.DnsCacheRecovery is { Outcome: DnsCacheRecoveryOutcome.Verified, FlushDispatched: true, Flush: { TimedOut: false, Truncated: false, ExitCode: 0 }, After.State: DnsLookupState.Resolved } recovery &&
                    string.IsNullOrWhiteSpace(recovery.Flush.Error) && recovery.After.Addresses is { Length: > 0 } &&
                    string.Equals(recovery.Host, recovery.After.Host, StringComparison.OrdinalIgnoreCase) &&
                    result.Steps.Count == 1 && result.Steps[0].Step.Tool == DnsCacheRecovery.FlushStep().Tool &&
                    result.Steps[0].Step.Arguments.SequenceEqual(DnsCacheRecovery.FlushStep().Arguments) &&
                    !result.Steps[0].Step.Verification && result.Steps[0].Result == recovery.Flush;
        }
        if (result.SelectedPrinterQueues is not null)
        {
            try
            {
                return result.Kind == RepairKind.PrinterOffline
                    ? PrinterOfflineRecovery.VerifySelected(result.SelectedPrinterQueues, result.Steps)
                    : result.Kind == RepairKind.PrintQueues && PrintRecovery.VerifySelected(result.SelectedPrinterQueues, result.Steps);
            }
            catch (ArgumentException) { return false; }
        }
        var expected = Plan(result.Kind).Steps;
        if (expected.Count == 0) return false;
        if (result.Steps.Count != expected.Count) return false;
        if (!result.Steps.Any(step => !step.Step.Verification && step.CommandDispatched) ||
            result.Steps.Any(step => step.Step.Verification && !step.CommandDispatched)) return false;
        for (int i = 0; i < expected.Count; i++)
        {
            var actual = result.Steps[i];
            if (actual.Result.TimedOut || actual.Result.ExitCode != 0 || !string.IsNullOrWhiteSpace(actual.Result.Error) || actual.Step.Tool != expected[i].Tool ||
                !actual.Step.Arguments.SequenceEqual(expected[i].Arguments) || actual.Step.Verification != expected[i].Verification)
                return false;
        }
        return result.Kind == RepairKind.PrintQueues ? PrintRecovery.Verify(result.Steps) : result.Kind == RepairKind.PrinterOffline ? PrinterOfflineRecovery.Verify(result.Steps) : result.Kind == RepairKind.DhcpLease ? DhcpRecovery.Verify(result.Steps) : VerificationStatus(result.Kind, result.Steps[^1].Result).StartsWith("Verification reported", StringComparison.Ordinal);
    }

    private static string VerificationStatus(RepairKind kind, ToolResult result)
    {
        if (kind == RepairKind.ComponentCleanup)
            return ComponentCleanup.Interpret(result).State == CheckState.Clear ? "Verification reported component-store cleanup no longer recommended; reclaimed space unmeasured" : "Component-store cleanup remains unverified or recommended; review required";
        if (kind == RepairKind.DefenderSignatures)
            return DefenderRecovery.Interpret(result).State == CheckState.Clear ? "Verification reported Defender signatures no longer out of date; malware scan excluded" : "Defender signature verification incomplete; review required";
        if (kind == RepairKind.TimeSynchronization)
            return TimeRecovery.Interpret(result).State == CheckState.Clear ? "Verification reported Windows Time synchronized; absolute accuracy remains unmeasured" : "Time synchronization remains unverified; review required";
        if (kind == RepairKind.WmiRepository)
            return WmiRecovery.Interpret(result).State == CheckState.Clear ? "Verification reported WMI repository consistent; provider operation remains untested" : "WMI repository remains unverified; review required";
        if (kind == RepairKind.PrintQueues)
            return PrintRecovery.Read(result) is { Count: > 0, Paused: 0 } ? "Verification reported local queues unpaused; physical printing remains untested" : "Print queue verification incomplete; review required";
        if (kind == RepairKind.WindowsRecoveryEnvironment)
            return WindowsRecoveryDiagnostics.Interpret(result).State == CheckState.Clear
                ? "Verification reported Windows Recovery Environment enabled; boot and repair operation remain untested"
                : "Windows Recovery Environment remains disabled or unverified; review required";
        if (kind == RepairKind.PrintSpoolDirectory)
            return PrintSpoolDirectoryRecovery.Verify(result)
                ? "Verification reported the Windows default print spool directory present on its fixed drive; write access and physical printing remain untested"
                : "Print spool directory creation remains unverified; review required";
        if (kind == RepairKind.DeliveryOptimization)
            return ServiceRecovery.MatchesService(result, DeliveryOptimizationRecovery.ServiceName) && ServiceRecovery.State(result) == 4
                ? "Verification reported Delivery Optimization running; transfer and update completion remain untested"
                : "Delivery Optimization state remains unverified; review required";
        if (ServiceRecovery.ServiceName(kind) is { } service)
            return ServiceRecovery.MatchesService(result, service) && ServiceRecovery.State(result) == 4
                ? "Verification reported service running; feature operation remains untested" : "Service identity or state remains unverified; review required";
        return Workflows.Interpret(kind, result).State == CheckState.Clear
            ? "Verification reported no corruption in the checked scope; not an overall health verdict"
            : "Verification command completed; review its output for the result";
    }
}
