using System.Text.Json;
using Microsoft.Win32;
using System.Net.NetworkInformation;

namespace KillerBench.Engine;

public enum WorkflowKind { WindowsUpdate, SystemFiles, Vitals, Network, Printing, TimeService, Installer, Storage, Wmi, Audio, Search, Cleanup, Defender, AvailableUpdates, Privacy, BitLocker, ComponentCleanup, Profiles, ComputerName, Foxit, ScheduledTasks, Services, EventLog, Applications, Display, Input, DeviceProblems, CommonProblems, AppPackages, InstalledApps, BackupServices, BackgroundTransfers, FirewallServices, Wireless, WiredAuthentication, Bluetooth, Camera, SmartCard, Biometric, ReliabilityHistory, HardwareErrors, DriverInstallHistory, SystemRestore, WindowsRecovery, Certificates, FileSharing, TaskSchedulerService, WindowsUpdateService, DeliveryOptimizationService, BitsService, CryptographicService, ModulesInstallerService, WindowsUpdateMedicService, UpdateOrchestratorService, DiagnosticPolicyService, WindowsErrorReportingService, WindowsEventCollectorService, RemoteAccessConnectionManagerService, RemoteDesktopServices, WindowsRemoteManagement, SecurityCenterService, NetworkConnectionsService, WinHttpAutoProxyService, WindowsConnectionManagerService, NetworkSetupService, AppReadinessService, StateRepositoryService, UserProfileService, CredentialManagerService, ShellHardwareDetectionService, DeviceInstallService, AppxDeploymentService, ClientLicenseService, StoreInstallService, WindowsLicenseManagerService, PushToInstallService, WebAccountManagerService, MicrosoftAccountSignInService, StorageService, OptimizeDrivesService, WindowsImageAcquisitionService, FunctionDiscoveryProviderHostService, FunctionDiscoveryResourcePublicationService, HumanInterfaceDeviceService, PortableDeviceEnumeratorService, WindowsFontCacheService, DeviceAssociationService, TextInputManagementService, DisplayEnhancementService, SystemNotifications, CoreWindowsServices, StorageSpaces, WindowsActivation, StartupPerformance, StartupApplications, Mace, Orca, Depot, Debloat, ServicingActivity, BrowserExtensions }
public enum CheckState { Clear, Repairable, Attention, Unknown, Information }
public sealed record WorkflowCheck(string Name, CheckState State, string Detail, RepairKind? Repair = null, ToolResult? Evidence = null)
{
    public ServiceDependencyGraphResult? ServiceDependencies { get; init; }
    public bool DemandStartAuthorized { get; init; }
    public CheckState EffectiveState => State == CheckState.Clear &&
        (Evidence is { ExitCode: not 0 } or { TimedOut: true } or { Truncated: true } || !string.IsNullOrWhiteSpace(Evidence?.Error))
        ? CheckState.Unknown : State;
}
public sealed record WorkflowAction(string Name, string Status, string Detail, string NextAction, bool Verified,
    bool RequiresRestart = false);
public sealed record WorkflowResult(WorkflowKind Kind, string Machine, DateTimeOffset StartedUtc,
    string Status, IReadOnlyList<WorkflowCheck> Checks, IReadOnlyList<RepairResult> Repairs, string JournalPath)
{
    public IReadOnlyList<RecipeResult> Recipes { get; init; } = [];
    public IReadOnlyList<WorkflowAction> Actions { get; init; } = [];
    public bool RequiresRestart => Repairs.Any(r => r.RequiresRestart) || Recipes.Any(recipe => recipe.Outcome == RecipeOutcome.RestartRequired) ||
        Actions.Any(action => action.RequiresRestart);
    public WorkflowSummary Summary => WorkflowSummary.From(this);
    public WorkflowRecoveryPlan RecoveryPlan => WorkflowRecoveryPlan.From(this);
    public IReadOnlyList<string>? RequestedRecipeIds { get; init; }
    public IReadOnlyList<string> PlannedRecipeIds { get; init; } = [];
    public IReadOnlyList<CommonDiagnosticScope>? SelectedDiagnosticScopes { get; init; }
    public int GetExitCode()
    {
        if (Status.StartsWith("Stopped", StringComparison.Ordinal)) return 130;
        if (Status == "Running") return 3;
        if (RequiresRestart) return 4;
        if (Status == "Needs attention" || Checks.Count == 0 || Summary.ReviewItems > 0 ||
            Checks.Any(check => check.State == CheckState.Clear && (check.Evidence is { ExitCode: not 0 } or { TimedOut: true } or { Truncated: true } ||
                !string.IsNullOrWhiteSpace(check.Evidence?.Error))) || Recipes.Any(recipe => recipe.ExecutionUncertain) ||
            Repairs.Any(repair => !KillerBench.Engine.Repairs.IsVerified(repair)) || Actions.Any(action => !action.Verified)) return 3;
        return 0;
    }
}
public interface IWorkflowHost
{
    Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token);
    Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token,
        Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint) => InspectAsync(kind, token);
    Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token,
        Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint, IReadOnlyList<CommonDiagnosticScope>? selectedScopes) =>
        selectedScopes is null ? InspectAsync(kind, token, checkpoint) :
        throw new NotSupportedException("This provider does not support focused diagnostic scopes.");
    Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token);
    Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token);
}

public static class Workflows
{
    internal static bool HasBlockingPrerequisite(WorkflowKind kind, IReadOnlyList<WorkflowCheck> checks) =>
        checks.Any(check => check.Evidence?.TimedOut == true) ||
        checks.Any(check => check.Name == "Administrator"
            ? check.EffectiveState is CheckState.Attention or CheckState.Unknown or CheckState.Repairable
            : ((kind != WorkflowKind.Storage && check.Name is "Restart" or "System disk") ||
                (kind == WorkflowKind.Installer && check.Name == "Installer readiness")) && check.EffectiveState != CheckState.Clear);

    public static bool SupportsRepair(WorkflowKind kind) => Enum.IsDefined(kind) && kind is not (WorkflowKind.Vitals or WorkflowKind.Storage or WorkflowKind.StorageSpaces or WorkflowKind.WindowsActivation or WorkflowKind.StartupPerformance or WorkflowKind.StartupApplications or WorkflowKind.Cleanup or WorkflowKind.AvailableUpdates or WorkflowKind.Privacy or WorkflowKind.Profiles or WorkflowKind.ComputerName or WorkflowKind.Foxit or WorkflowKind.ScheduledTasks or WorkflowKind.Services or WorkflowKind.Applications or WorkflowKind.Display or WorkflowKind.Input or WorkflowKind.DeviceProblems or WorkflowKind.AppPackages or WorkflowKind.InstalledApps or WorkflowKind.BackgroundTransfers or WorkflowKind.ReliabilityHistory or WorkflowKind.HardwareErrors or WorkflowKind.DriverInstallHistory or WorkflowKind.Certificates or WorkflowKind.Mace or WorkflowKind.Orca or WorkflowKind.Depot or WorkflowKind.Debloat or WorkflowKind.ServicingActivity or WorkflowKind.BrowserExtensions);
    public static RepairKind[] Scopes(WorkflowKind kind) => kind switch
    {
        WorkflowKind.CommonProblems => CommonProblemScopes(),
        WorkflowKind.WindowsUpdate => [RepairKind.UpdateService, RepairKind.BackgroundTransfer, RepairKind.CryptographicServices, RepairKind.UpdateDataStoreReset, RepairKind.ModulesInstaller, RepairKind.UpdateOrchestrator, RepairKind.DeliveryOptimization, RepairKind.ComponentStore, RepairKind.SystemFiles],
        WorkflowKind.BackgroundTransfers => [RepairKind.BackgroundTransfer, RepairKind.BitsJobRetry],
        WorkflowKind.SystemFiles => [RepairKind.SystemFiles],
        WorkflowKind.Printing => [RepairKind.PrintSpooler, RepairKind.PrintSpoolDirectory, RepairKind.PrintQueues, RepairKind.PrinterOffline, RepairKind.DefaultPrinter, RepairKind.PrintJobCancellation],
        WorkflowKind.TimeService => [RepairKind.TimeSynchronization, RepairKind.TimeSourceConfiguration],
        WorkflowKind.Installer => [RepairKind.WindowsInstaller, RepairKind.InstallerRegistration, RepairKind.MsiProduct],
        WorkflowKind.Storage => [RepairKind.StorageFileSystem, RepairKind.VolumeOptimization],
        WorkflowKind.SystemRestore => [RepairKind.SystemRestorePoint],
        WorkflowKind.BitLocker => [RepairKind.BitLockerProtectionResume, RepairKind.BitLockerRecoveryBackup],
        WorkflowKind.Orca => [RepairKind.OrcaCacheReset],
        WorkflowKind.Cleanup => [RepairKind.PreviousWindowsInstallationRemoval],
        WorkflowKind.Debloat => [RepairKind.HighPerformancePowerPlan, RepairKind.PowerTimers, RepairKind.HibernationDisable],
        WorkflowKind.Wmi => [RepairKind.WindowsManagement, RepairKind.WmiRepository, RepairKind.WmiProvider],
        WorkflowKind.Audio => [RepairKind.AudioEndpoints, RepairKind.WindowsAudio],
        WorkflowKind.Search => [RepairKind.WindowsSearch, RepairKind.SearchIndexRebuild],
        WorkflowKind.EventLog => [RepairKind.WindowsEventLog, RepairKind.EventChannelEnable],
        WorkflowKind.BackupServices => [RepairKind.VolumeShadowCopy, RepairKind.SoftwareShadowProvider, RepairKind.WindowsBackupService, RepairKind.BlockLevelBackupEngineService],
        WorkflowKind.FirewallServices => [RepairKind.BaseFilteringEngine, RepairKind.WindowsFirewall],
        WorkflowKind.Wireless => [RepairKind.WirelessAutoConfig],
        WorkflowKind.WiredAuthentication => [RepairKind.WiredAutoConfig],
        WorkflowKind.Bluetooth => [RepairKind.BluetoothSupport],
        WorkflowKind.Camera => [RepairKind.CameraFrameServer],
        WorkflowKind.SmartCard => [RepairKind.SmartCardService],
        WorkflowKind.Biometric => [RepairKind.BiometricService],
        WorkflowKind.FileSharing => [RepairKind.ServerService],
        WorkflowKind.TaskSchedulerService => [RepairKind.TaskSchedulerService],
        WorkflowKind.WindowsUpdateService => [RepairKind.UpdateService],
        WorkflowKind.DeliveryOptimizationService => [RepairKind.DeliveryOptimization],
        WorkflowKind.BitsService => [RepairKind.BackgroundTransfer],
        WorkflowKind.CryptographicService => [RepairKind.CryptographicServices],
        WorkflowKind.ModulesInstallerService => [RepairKind.ModulesInstaller],
        WorkflowKind.WindowsUpdateMedicService => [RepairKind.WindowsUpdateMedicService],
        WorkflowKind.UpdateOrchestratorService => [RepairKind.UpdateOrchestrator],
        WorkflowKind.DiagnosticPolicyService => [RepairKind.DiagnosticPolicyService],
        WorkflowKind.WindowsErrorReportingService => [RepairKind.WindowsErrorReportingService],
        WorkflowKind.WindowsEventCollectorService => [RepairKind.WindowsEventCollectorService],
        WorkflowKind.RemoteAccessConnectionManagerService => [RepairKind.RemoteAccessConnectionManagerService],
        WorkflowKind.RemoteDesktopServices => [RepairKind.RemoteDesktopServices],
        WorkflowKind.WindowsRemoteManagement => [RepairKind.WindowsRemoteManagement],
        WorkflowKind.SecurityCenterService => [RepairKind.SecurityCenterService],
        WorkflowKind.NetworkConnectionsService => [RepairKind.NetworkConnectionsService],
        WorkflowKind.WinHttpAutoProxyService => [RepairKind.WinHttpAutoProxyService],
        WorkflowKind.WindowsConnectionManagerService => [RepairKind.WindowsConnectionManagerService],
        WorkflowKind.NetworkSetupService => [RepairKind.NetworkSetupService],
        WorkflowKind.AppReadinessService => [RepairKind.AppReadinessService],
        WorkflowKind.StateRepositoryService => [RepairKind.StateRepositoryService],
        WorkflowKind.AppxDeploymentService => [RepairKind.AppxDeploymentService],
        WorkflowKind.ClientLicenseService => [RepairKind.ClientLicenseService],
        WorkflowKind.StoreInstallService => [RepairKind.StoreInstallService],
        WorkflowKind.WindowsLicenseManagerService => [RepairKind.WindowsLicenseManagerService],
        WorkflowKind.PushToInstallService => [RepairKind.PushToInstallService],
        WorkflowKind.WebAccountManagerService => [RepairKind.WebAccountManagerService],
        WorkflowKind.MicrosoftAccountSignInService => [RepairKind.MicrosoftAccountSignInService],
        WorkflowKind.StorageService => [RepairKind.StorageService],
        WorkflowKind.OptimizeDrivesService => [RepairKind.OptimizeDrivesService],
        WorkflowKind.WindowsImageAcquisitionService => [RepairKind.WindowsImageAcquisitionService],
        WorkflowKind.FunctionDiscoveryProviderHostService => [RepairKind.FunctionDiscoveryProviderHostService],
        WorkflowKind.FunctionDiscoveryResourcePublicationService => [RepairKind.FunctionDiscoveryResourcePublicationService],
        WorkflowKind.HumanInterfaceDeviceService => [RepairKind.HumanInterfaceDeviceService],
        WorkflowKind.PortableDeviceEnumeratorService => [RepairKind.PortableDeviceEnumeratorService],
        WorkflowKind.WindowsFontCacheService => [RepairKind.WindowsFontCacheService],
        WorkflowKind.DeviceAssociationService => [RepairKind.DeviceAssociationService],
        WorkflowKind.TextInputManagementService => [RepairKind.TextInputManagementService],
        WorkflowKind.DisplayEnhancementService => [RepairKind.DisplayEnhancementService],
        WorkflowKind.SystemNotifications => [RepairKind.ComPlusEventSystem, RepairKind.SystemEventNotificationService],
        WorkflowKind.CoreWindowsServices => [RepairKind.PowerService, RepairKind.PlugAndPlayService, RepairKind.UserManagerService, RepairKind.GroupPolicyClientService, RepairKind.NetworkStoreInterfaceService, RepairKind.RpcEndpointMapperService, RepairKind.RemoteProcedureCallService, RepairKind.DcomServerProcessLauncherService, RepairKind.BackgroundTasksInfrastructureService, RepairKind.CoreMessagingService],
        WorkflowKind.StorageSpaces => [],
        WorkflowKind.WindowsActivation => [],
        WorkflowKind.StartupPerformance => [],
        WorkflowKind.StartupApplications => [],
        WorkflowKind.UserProfileService => [RepairKind.UserProfileService],
        WorkflowKind.CredentialManagerService => [RepairKind.CredentialManagerService],
        WorkflowKind.ShellHardwareDetectionService => [RepairKind.ShellHardwareDetectionService],
        WorkflowKind.DeviceInstallService => [RepairKind.DeviceInstallService],
        WorkflowKind.WindowsRecovery => [RepairKind.WindowsRecoveryEnvironment],
        WorkflowKind.ComputerName => [RepairKind.ComputerRename],
        WorkflowKind.Defender => [RepairKind.DefenderSignatures],
        WorkflowKind.ComponentCleanup => [RepairKind.ComponentCleanup],
        WorkflowKind.Network => [RepairKind.DnsClient, RepairKind.DnsCache, RepairKind.DhcpClient, RepairKind.DhcpLease, RepairKind.Workstation, RepairKind.NetworkLocationAwareness, RepairKind.NetworkListService, RepairKind.IpHelper, RepairKind.FirewallProfiles],
        _ when Enum.IsDefined(kind) => [],
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static RepairKind[] CommonProblemScopes()
    {
        var established = new[] { WorkflowKind.WindowsUpdate, WorkflowKind.Network, WorkflowKind.Printing,
            WorkflowKind.Audio, WorkflowKind.Search, WorkflowKind.EventLog, WorkflowKind.Defender, WorkflowKind.TimeService, WorkflowKind.Wmi,
            WorkflowKind.Installer, WorkflowKind.FirewallServices, WorkflowKind.TaskSchedulerService, WorkflowKind.DiagnosticPolicyService,
            WorkflowKind.WindowsErrorReportingService, WorkflowKind.WindowsEventCollectorService }.SelectMany(Scopes);
        var inspectedServices = Enum.GetValues<RepairKind>().Where(kind =>
            ServiceRecovery.ServiceName(kind) is not null && RecipeCatalog.All.Any(recipe => recipe.Action == kind));
        return established.Concat(inspectedServices).Where(scope => scope is not (RepairKind.DnsCache or RepairKind.UpdateDataStoreReset or RepairKind.TimeSourceConfiguration or RepairKind.EventChannelEnable or RepairKind.WindowsUpdateMedicService)).Distinct().ToArray();
    }
    public static string Preview(WorkflowKind kind) =>
        $"TARGET: THIS PC ({Environment.MachineName})\n\n" + (kind switch
        {
            WorkflowKind.ServicingActivity => "Observe bounded Windows Update and Setup servicing activity over one or more snapshots. Options control refresh timing and whether update events, update history, the exact Windows Update scheduled task, exact service states, restart indicators, current-account background transfers, Delivery Optimization activity and privacy-bounded Windows Installer activity are included. Process IDs, command lines, users and paths are excluded. Activity does not prove progress, completion, a stall or a fault. No update, setup, transfer, task, service, process, policy, restart or repair action is requested.",
            WorkflowKind.BackgroundTransfers => "Read BITS transfer jobs in the selected account scope and review exact job identities. Queue names do not establish application ownership. No transfers are resumed, completed, canceled, deleted or reassigned.",
            WorkflowKind.FirewallServices => "Read Domain, Private and Public profile enabled settings from ActiveStore, then check Base Filtering Engine and Windows Defender Firewall services. Optional recovery starts only enabled, stopped services after fresh dependency checks. Existing filtering policy may take effect and interrupt network or remote access. Startup types, service permissions, firewall rules, profiles and IPsec settings are preserved. No resets, rule changes or service stops. Verification establishes service running state only; effective protection and connectivity remain unverified.",
            WorkflowKind.CommonProblems => "Inspect 49 diagnostic areas, then evaluate every installed service-recovery recipe together with the servicing, DHCP, printing, Defender, time and WMI recipes. Checks can take several minutes. Auto-repair is optional and requires each recipe's own current evidence, machine prerequisites and dependency checks. Eligible repairs can renew missing DHCP leases, release queued print jobs, resynchronize the clock, refresh Defender signatures, restore eligible services and repair confirmed Windows corruption. Automatic stopped services may be started after their startup window and dependencies are verified. A stopped demand-start service remains unchanged when idle state is the only evidence; narrowly focused workflows can require it only when current feature evidence supports the start. Startup types, task definitions, reports, dumps, subscriptions, credentials, channel settings, firewall policy and managed settings are preserved. Historical, backup and transfer snapshots are read-only. Dump contents are not opened. Dirty-bit inspection does not schedule repair or restart. This workflow does not install updates, change drivers, delete files or perform a complete hardware test.",
            WorkflowKind.BackupServices => "Check Volume Shadow Copy, Microsoft Software Shadow Copy Provider, Windows Backup and Block Level Backup Engine services, then read VSS writer status, registered provider types, current snapshots and shadow-storage allocation. Stopped demand-start services can be normal when idle. Optional recovery starts only stopped automatic services after dependency checks, then verifies running state. Existing scheduled or interrupted backup work may resume. Disabled and demand-start startup settings, snapshots, shadow storage, provider registrations, backup plans, catalogs, schedules, targets, credentials and retention are preserved. No backup is selected or reconfigured, no service is restarted, and no snapshot or catalog is deleted. Provider registration, writer status, storage allocation and stable service state do not verify backup completion, destination access or restore success.",
            WorkflowKind.WindowsUpdate => "Inventory bounded Windows servicing package, optional-feature and capability states, check restart indicators, free space and update services, observe Delivery Optimization without starting it, then scan the component store and protected system files. Pending package, feature or capability states request review but do not authorize a component action. Auto-repair can start enabled Windows Update, BITS, Cryptographic Services, Windows Modules Installer and Update Orchestrator services. Starting Update Orchestrator may resume existing scheduled update activity under current policy. Packages, optional features, capabilities, Delivery Optimization cache, peer, bandwidth, policy, source enrollment, schedules and startup settings are preserved. DISM or SFC requires live evidence of corruption. No update is selected or approved by this workflow.",
            WorkflowKind.Applications => "Read recent Application error and critical events together with bounded Reliability Monitor history, then return one stability-history conclusion. Historical records do not prove a current fault or root cause and only direct narrower diagnosis. No applications are stopped, repaired, removed or reconfigured.",
            WorkflowKind.ReliabilityHistory => "Read the last 14 days of Windows Reliability Monitor records together with recent Application errors and unexpected shutdown events, then return one bounded historical conclusion. Historical records do not establish current health or root cause. No repair is selected and no applications, services or settings are changed.",
            WorkflowKind.HardwareErrors => "Read the last 14 days of Windows Hardware Error Architecture and Memory Diagnostic history, current Plug and Play device problems, physical-disk health and provider reliability counters, then return one bounded hardware conclusion. Records require correlation with their payload, firmware diagnostics and vendor hardware tests. No failing component or root cause is inferred, no repair is selected, and no hardware, firmware, driver, disk, pool, volume, file, service, power, restart or setting is changed.",
            WorkflowKind.DriverInstallHistory => "Read the retained tail of Windows SetupAPI device-install history, correlate failed sections with exact current Plug and Play identities and return one bounded conclusion. Historical log sections do not prove a device is still present or a failure is current. No driver is installed, removed, rolled back or updated.",
            WorkflowKind.SystemRestore => "List up to 256 available System Restore points, review explicit creation eligibility and return one bounded readiness conclusion. The inventory does not prove that protection is enabled for every volume or that a restore will succeed. No restore point is created, deleted or applied during diagnosis.",
            WorkflowKind.WindowsRecovery => "Read the current Windows Recovery Environment configuration with REAgentC, correlate an enabled local location with the exact current disk and partition, and return one bounded readiness conclusion. Optional automatic repair can enable a disabled existing configuration, then verify its reported state. The recovery image is not downloaded, relocated or replaced, no recovery boot is initiated, and Windows is not restarted. Enabled status and partition presence do not prove that the recovery image can boot or repair this PC.",
            WorkflowKind.Certificates => "Inspect certificate dates and build cache-only local trust chains for the machine and current-user personal stores, then return one bounded operational conclusion. Revocation, server presentation, bindings, enrollment, policy ownership, remote trust and private-key usability remain unverified. No certificate, trust setting, enrollment, binding, private key, application, service or policy is changed.",
            WorkflowKind.FileSharing => "Check Windows SMB server availability. Optional recovery starts only an enabled, stopped Server service after fresh dependency checks. Existing file and printer shares may become reachable under their current permissions and firewall policy. No share, permission, SMB setting, firewall rule, credential, policy or startup type is changed. Verification covers stable service state only, not client access or file transfer.",
            WorkflowKind.TaskSchedulerService => "Inventory bounded scheduled-task state, runtime results and missed-run counts, then combine them with exact Task Scheduler service failure history before checking current service availability. Task findings and historical service failures cannot authorize a service start. Optional recovery starts only an enabled, stopped exact service after fresh dependency checks. Existing enabled tasks may begin under their configured accounts, including missed triggers. No task definition, principal, credential reference, policy or startup type is changed, and no task is directly started or deleted. Verification covers stable service state only, not action success, credential validity or future task outcomes.",
            WorkflowKind.WindowsUpdateService => "Check exact Windows Update Service state, its registered service DLL, bounded current update activity and exact service-failure history. DLL observations, update activity and historical failures cannot authorize a service start or update operation. Optional recovery starts only a stopped automatic service after fresh dependency and executable-registration checks. A stopped demand-start service remains informational. Existing update activity may resume under current policy. Update policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type are preserved. KillerBench selects, approves or installs no update. Verification covers stable service state only, not source reachability, applicability, download, installation, completion or restart readiness.",
            WorkflowKind.DeliveryOptimizationService => "Check the exact Delivery Optimization service state. A stopped demand-start service remains informational. Only an exact Paused state can offer recovery. Optional recovery repeats the state check, sends one bounded continue request and requires two coherent Running observations. Startup type, cache contents, peers, bandwidth, update source and managed policy are preserved. Transfer activity, cache health, update progress and completion remain unverified.",
            WorkflowKind.BitsService => "Check the exact Background Intelligent Transfer Service state, current-account transfer jobs and bounded exact service-failure history. Historical failures and transfer snapshots cannot authorize a service start. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume queued background transfers. Jobs, priorities, owners, destinations, proxy settings, policy and startup type are preserved. Verification covers stable service state only, not transfer progress, application ownership or completion.",
            WorkflowKind.CryptographicService => "Check exact Cryptographic Services state, its registered service DLL and bounded exact service-failure history. Historical failures and DLL observations cannot authorize a service start or component replacement. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing catalog, certificate and update operations may resume. Certificates, keys, catalogs, trust settings, policy and startup type are preserved. Verification covers stable service state only, not certificate, signature, catalog or trust validation.",
            WorkflowKind.ModulesInstallerService => "Check exact Windows Modules Installer state, bounded servicing package transitions and exact service-failure history. Historical failures and package states cannot authorize a service start or package operation. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing servicing activity may resume. Packages, component-store content, sources, policy and startup type are preserved. Verification covers stable service state only, not component-store health, package applicability, servicing completion or restart readiness.",
            WorkflowKind.WindowsUpdateMedicService => "Check exact Windows Update Medic Service state, its registered service DLL, bounded current update activity and exact service-failure history. DLL observations, update activity and historical failures cannot authorize a service start or component action. A stopped demand-start service can be normal. When this exact tool is selected, optional recovery can start only the stopped enabled service after fresh dependency checks. Windows remediation may resume under current policy and can repair update components or trigger update activity. Update policy, source enrollment, schedules, packages, BITS jobs, restart settings, service permissions and startup type are preserved. KillerBench selects no update and changes no update setting. Verification covers stable service state only, not remediation, component health, applicability, download, installation or restart readiness.",
            WorkflowKind.UpdateOrchestratorService => "Check exact Update Orchestrator Service state, the exact built-in Windows Update task, bounded current activity and exact service-failure history. Task results, update activity and historical failures cannot authorize a service start or update operation. Optional recovery starts only a stopped automatic service after fresh dependency and executable-registration checks. A stopped demand-start service remains informational. Existing scheduled update activity may resume under current policy and can download or install already approved updates. Update policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type are preserved. KillerBench selects or approves no update. Verification covers stable service state only, not applicability, download, installation, completion or restart readiness.",
            WorkflowKind.DiagnosticPolicyService => "Review bounded Diagnostic Policy operational history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Historical records cannot authorize service recovery or a troubleshooting action. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing diagnostic scenarios may resume. Policy, troubleshooting data and startup type are preserved. No troubleshooter or suggested fix is run. Verification covers stable service state only, not fault detection, diagnosis completion or repair outcome.",
            WorkflowKind.WindowsErrorReportingService => "Review crash-dump configuration, bounded dump-file metadata, recent system crash reports, exact Windows Error Reporting service-failure history and current service readiness. Historical reports do not establish a current fault or root cause and cannot authorize recovery. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing queued reports may resume under current policy. Report archives, dumps, consent, upload policy and startup type are preserved. No upload or deletion is requested. Verification covers stable service state only, not report creation, upload, delivery, problem resolution or crash diagnosis.",
            WorkflowKind.WindowsEventCollectorService => "Review bounded Windows Event Collector subscription inventory, exact service-failure history and current service readiness. An empty inventory can be intentional, and subscription count or historical failures cannot authorize recovery. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing subscriptions may resume remote event collection. Subscriptions, forwarded events, credentials, channel settings, policy and startup type are preserved. Verification covers stable service state only, not source reachability, authentication, remote delivery, event freshness or subscription health.",
            WorkflowKind.RemoteAccessConnectionManagerService => "Review bounded current-account VPN profile configuration, exact service-failure history and current Remote Access Connection Manager readiness without collecting secrets. An empty inventory can be intentional, and profile presence, connection labels or historical failures cannot authorize recovery or a connection attempt. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing dial-up and VPN requests may resume. Connection profiles, credentials, routes, adapters, authentication settings, policy and startup type are preserved. No connection is initiated. Verification covers stable service state only, not profile applicability, server reachability, authentication, tunnel negotiation, route installation, name resolution or connectivity.",
            WorkflowKind.RemoteDesktopServices => "Review local Remote Desktop enablement, Network Level Authentication and listener port, bounded connection and session history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Historical records cannot authorize service recovery or configuration changes. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. An existing listener may become reachable under current policy, NLA, firewall rules and permissions. Remote Desktop enablement, listeners, certificates, ports, firewall rules, NLA, permissions, sessions, policy, service permissions and startup type are preserved. No listener, firewall rule, account access, certificate or session is created or changed. Verification covers stable service state only, not effective policy, listener binding, firewall access, certificate identity, permissions, authentication, network reachability, connection success or session creation.",
            WorkflowKind.WindowsRemoteManagement => "Review bounded WinRM listener configuration, operational history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Disabled listeners or current service concerns request review, while historical records cannot authorize recovery or configuration changes. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing WS-Management listeners may become reachable under current authentication, certificate, firewall, TrustedHosts and policy settings. Listeners, addresses, ports, certificates, authentication, encryption, TrustedHosts, firewall rules, permissions, quotas, policy, service permissions and startup type are preserved. No listener, firewall rule, trust entry, certificate, credential or remote session is created or changed. Verification covers stable service state only, not effective policy, listener binding, firewall access, certificate validity, authentication, permissions, client reachability or remote command execution.",
            WorkflowKind.SecurityCenterService => "Review bounded antivirus registrations with raw provider state, exact service-failure history and current Windows Security Center readiness without interpreting protection health. An empty inventory is not a protection-health verdict, and registration count, raw state or historical failures cannot authorize recovery or a security change. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing security status monitoring and notifications may resume. Antivirus configuration, provider registrations, protection settings, exclusions, notifications, policy and startup type are preserved. No scan, update or protection change is requested. Verification covers stable service state only, not antivirus presence, protection state, signature freshness, notification delivery, registration ownership or effective policy.",
            WorkflowKind.NetworkConnectionsService => "Review bounded current network profile assignments, exact service-failure history and current Network Connections service readiness. Incomplete evidence takes precedence. Public profiles and historical failures remain context and cannot authorize recovery or a network change. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing network-management interfaces may resume. Adapters, addresses, routes, DNS settings, connection profiles, credentials, firewall configuration, policy and startup type are preserved. No connection or network setting is changed. Verification covers stable service state only, not adapter operation, address assignment, routing, name resolution or connectivity.",
            WorkflowKind.WinHttpAutoProxyService => "Review stored WinHTTP defaults, current-account Internet settings, exact service-failure history and current WinHTTP Auto-Proxy readiness. Incomplete evidence takes precedence. Proxy configuration and historical failures cannot authorize service recovery or discovery. A stopped demand-start service can be normal. When this exact tool is selected, optional recovery can start only the stopped enabled service after fresh dependency checks. Existing applications may resume WPAD or PAC discovery under current settings. Proxy definitions, PAC URLs, credentials, network settings, firewall configuration, policy, service permissions and startup type are preserved. No URL is fetched and no proxy or network setting is changed. Verification covers stable service state only, not discovery, reachability, authentication, policy ownership or application use.",
            WorkflowKind.WindowsConnectionManagerService => "Review bounded current network profile assignments, exact service-failure history and current Windows Connection Manager readiness. Incomplete evidence takes precedence. Public profiles and historical failures remain context and cannot authorize recovery or a connection decision. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Windows may resume automatic connection decisions under existing profiles and policy. Adapters, profiles, credentials, metering, cost, roaming, firewall, VPN and authentication settings and startup type are preserved. No profile, adapter or setting is changed. Verification covers stable service state only, not profile applicability, connection selection, authentication, routing, name resolution or connectivity.",
            WorkflowKind.NetworkSetupService => "Review bounded current adapter protocol bindings, exact service-failure history and current Network Setup Service readiness. Incomplete evidence takes precedence. Binding state and historical failures cannot authorize service recovery or a setup change. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing pending network setup may resume under Windows policy. Adapters, drivers, driver packages, bindings, addresses, routes, DNS settings, profiles, credentials, firewall configuration, policy and startup type are preserved. KillerBench selects no driver or configuration change. Verification covers stable service state only, not adapter availability, driver suitability, setup completion, address assignment, routing, name resolution or connectivity.",
            WorkflowKind.AppReadinessService => "Check App Readiness service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing first-sign-in and application provisioning may resume under current policy. Packages, registrations, licenses, user profiles, application data, policy and startup type are preserved. KillerBench selects no package operation. Verification covers stable service state only, not sign-in, provisioning or application launch.",
            WorkflowKind.StateRepositoryService => "Check State Repository Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing application-model state requests may resume. Registrations, packages, licenses, profiles, browser and application data, repository content, policy and startup type are preserved. No reset, re-registration or deletion is requested. Verification covers stable service state only, not repository consistency or application operation.",
            WorkflowKind.AppxDeploymentService => "Check AppX Deployment Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing deployment, registration or servicing requests may resume under current policy. Packages, registrations, licenses, profiles, application data, repository content, policy, service permissions and startup type are preserved. KillerBench selects no package operation. Verification covers stable service state only, not deployment, registration or application launch.",
            WorkflowKind.ClientLicenseService => "Check Client License Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Store application license requests may resume under current policy. Packages, registrations, licenses, tokens, accounts, profiles, application data, policy, service permissions and startup type are preserved. KillerBench selects no package, license, token or account operation. Verification covers stable service state only, not license validation or application launch.",
            WorkflowKind.StoreInstallService => "Check recent Microsoft Store installation history and exact Store Install Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Store download and installation requests may resume under current policy. Packages, registrations, licenses, accounts, application data, download state, sources, policy, service permissions and startup type are preserved. KillerBench selects no package, download, license, account, source or installation operation. Verification covers stable service state only, not download, installation, license or application outcomes.",
            WorkflowKind.WindowsLicenseManagerService => "Check Windows License Manager Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing application license checks may resume under current policy. Packages, registrations, licenses, tokens, accounts, application data, policy, service permissions and startup type are preserved. KillerBench selects no package, license, token, account, registration or entitlement operation. Verification covers stable service state only, not license validation, entitlement or application launch.",
            WorkflowKind.PushToInstallService => "Check Windows PushToInstall Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. An existing previously approved remote installation request may resume under current policy. Packages, registrations, licenses, accounts, application data, queued requests, sources, policy, service permissions and startup type are preserved. KillerBench creates, approves and selects no request, package, source, account or installation. Verification covers stable service state only, not request processing, download, installation or application outcomes.",
            WorkflowKind.WebAccountManagerService => "Review payload-sanitized account broker history and exact Web Account Manager availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume account and token requests under current policy. Historical events cannot authorize recovery or establish current account state. Accounts, credentials, tokens, cookies, application registrations, permissions, sign-in state, policy, service permissions and startup type are preserved. KillerBench selects no account, credential, token, cookie, permission or sign-in operation. Verification covers stable service state only, not authentication, token issuance or application access.",
            WorkflowKind.MicrosoftAccountSignInService => "Review payload-sanitized account broker history and exact Microsoft Account Sign-in Assistant availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Microsoft account sign-in requests may resume under current policy. Historical events cannot authorize recovery or establish current account state. Accounts, credentials, tokens, device registration, application data, permissions, sign-in state, policy, service permissions and startup type are preserved. KillerBench selects no account, credential, token, registration, permission or sign-in operation. Verification covers stable service state only, not authentication, account connectivity or application access.",
            WorkflowKind.StorageService => "Review bounded current Storage module jobs, exact service-failure history and current Storage Service readiness. Incomplete evidence takes precedence. Job state and historical failures cannot authorize service recovery or storage changes. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing storage notifications and policy-managed storage activity may resume. Volumes, partitions, filesystems, mount points, devices, drivers, Storage Sense settings, maintenance policy, service permissions and startup type are preserved. KillerBench selects no job, volume, partition, mount point, file, device, driver or storage setting operation. Verification covers stable service state only, not job outcome, device, volume or storage-policy operation.",
            WorkflowKind.OptimizeDrivesService => "Check Optimize Drives service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Windows-scheduled optimization activity may resume under current maintenance policy. Volumes, filesystems, data, optimization schedules, maintenance policy, service permissions and startup type are preserved. KillerBench does not select a volume, analyze, trim, defragment, optimize or schedule any drive. Verification covers stable service state only, not analysis, optimization or device health.",
            WorkflowKind.WindowsImageAcquisitionService => "Check Windows Image Acquisition service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing scanner and still-image acquisition requests may resume under current application permissions. Devices, drivers, image data, application permissions, privacy settings, transfer settings, policy, service permissions and startup type are preserved. KillerBench selects no device, driver, image, permission, transfer or acquisition operation. Verification covers stable service state only, not scanner, camera, transfer or application operation.",
            WorkflowKind.FunctionDiscoveryProviderHostService => "Check Function Discovery Provider Host availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume discovering network devices through installed providers. Network profiles, shares, devices, providers, firewall rules, discovery settings, credentials, policy, service permissions and startup type are preserved. KillerBench selects no device, share, provider, firewall rule or discovery setting operation. Verification covers stable service state only, not provider health, device discovery or connectivity.",
            WorkflowKind.FunctionDiscoveryResourcePublicationService => "Check Function Discovery Resource Publication availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. This PC may become discoverable through existing network discovery and firewall settings. Network profiles, shares, published resources, firewall rules, discovery settings, credentials, policy, service permissions and startup type are preserved. KillerBench selects no share, resource, firewall rule or discovery setting operation. Verification covers stable service state only, not publication, visibility, access or connectivity.",
            WorkflowKind.HumanInterfaceDeviceService => "Check Human Interface Device Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing hot buttons, media controls and other HID features may resume. Devices, drivers, button mappings, accessibility settings, input settings, policy, service permissions and startup type are preserved. KillerBench selects no device, driver, mapping or input setting operation. Verification covers stable service state only, not button, control or application behavior.",
            WorkflowKind.PortableDeviceEnumeratorService => "Check Portable Device Enumerator Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume detecting and accessing connected portable devices. Devices, drivers, files, libraries, synchronization settings, permissions, policy, service permissions and startup type are preserved. KillerBench selects no device, file, library, synchronization or transfer operation. Verification covers stable service state only, not enumeration, access, synchronization or transfer.",
            WorkflowKind.WindowsFontCacheService => "Check Windows Font Cache Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume using the current font cache. Installed fonts, font files, application data, cache files, policy, service permissions and startup type are preserved. KillerBench installs or removes no font and does not delete or rebuild the cache. Verification covers stable service state only, not text rendering or application behavior.",
            WorkflowKind.DeviceAssociationService => "Check recent device setup and Plug and Play history, then check Device Association Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Windows-requested device association work may resume. Paired and associated devices, drivers, credentials, metadata, permissions, policy, service permissions and startup type are preserved. KillerBench selects no device, pairing, removal, reset or driver operation. Verification covers stable service state only, not discovery, pairing, association or device operation.",
            WorkflowKind.TextInputManagementService => "Inspect keyboard, mouse and HID device evidence, then check Text Input Management Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing touch keyboard, handwriting, emoji and advanced text-input requests may resume. Dictionaries, handwriting data, language and keyboard settings, accessibility and privacy settings, devices, drivers, policy, service permissions and startup type are preserved. No text input, keystroke or handwriting content is collected. Verification covers stable service state only, not text entry, recognition or device operation.",
            WorkflowKind.DisplayEnhancementService => "Inspect graphics adapter and monitor evidence, then check Display Enhancement Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing brightness, color and enhancement requests may resume. Display configuration, brightness, color profiles, HDR settings, devices, drivers, power settings, policy, service permissions and startup type are preserved. No display setting is changed. Verification covers stable service state only, not enhancement behavior, picture quality or physical display operation.",
            WorkflowKind.SystemNotifications => "Read recent Service Control Manager history, then check COM+ Event System and System Event Notification Service together. Optional recovery verifies COM+ Event System readiness before starting SENS. Existing logon, network, power and COM+ notifications may resume under current configuration. COM+ catalogs, subscriptions, notification registrations, consumers, accounts, permissions, recovery settings, policy and startup types are preserved. No subscription, registration, consumer, account, permission or policy is changed. Verification covers stable service state only, not end-to-end notification delivery.",
            WorkflowKind.CoreWindowsServices => "Check Power, Plug and Play, User Manager, Group Policy Client, Network Store Interface, RPC Endpoint Mapper, Remote Procedure Call, DCOM Server Process Launcher, Background Tasks Infrastructure and CoreMessaging services together. Optional recovery starts only exact enabled automatic services that fresh evidence confirms are stopped after the startup window and complete dependency checks. Demand-start services remain unchanged when idle. Power plans, devices, drivers, accounts, credentials, profiles, policy, adapters, addresses, routes, DNS, firewall configuration, registrations, endpoints, task registrations, application data, service permissions and startup types are preserved. No policy refresh, device scan, account action, network change, power-setting change, registration change or task launch is requested. Verification covers stable service state only, not end-to-end feature behavior.",
            WorkflowKind.StorageSpaces => "Inventory non-primordial Storage Spaces pools and virtual disks, including health, operational state, capacity, allocation and resiliency, then return one bounded operational conclusion. Windows may demand-start its Storage Spaces management provider while answering this read-only query. Pools, virtual disks, physical disks, volumes, resiliency, allocation, maintenance settings, policy and startup type are preserved. No pool, disk, volume, allocation, attachment, repair, optimization or setting operation is requested. Provider results do not verify pool accessibility, redundancy, data integrity, backup recoverability or application I/O.",
            WorkflowKind.WindowsActivation => "Read current Windows license status without collecting full or partial product keys, retain the last seven days of sanitized Software Protection warning, error and critical event metadata, then return one bounded operational conclusion. Event payload is removed before evidence is saved. Historical records remain context when current licensing reports Licensed. No activation, product-key, token, license, account, service or policy action is selected. Provider status and event history do not prove subscription entitlement, activation-server reachability, token-store integrity, future validity or root cause.",
            WorkflowKind.StartupPerformance => "Read the last 14 days of Windows startup and shutdown performance event metadata, retaining only provider identity and bounded numeric duration fields, then correlate it with supported pending restart indicators. Paths, process names, accounts and other event payload are removed before evidence is saved. Historical degradation records do not identify a current bottleneck or authorize startup, service, application, driver, task, power, restart or policy changes.",
            WorkflowKind.StartupApplications => "Inventory machine and running-account Run and RunOnce registration names, Startup folder file names, Windows startup approval states, and boot or logon scheduled-task names and states, then return one startup-environment conclusion. Command lines, target paths, binary approval timestamps, task actions, arguments, accounts and credentials are not collected. Registration does not prove an item ran, caused a delay, is unwanted or is safe to disable. Approval state does not prove a matching registration exists. No registration, file, application, task, service or policy change is selected.",
            WorkflowKind.UserProfileService => "Check User Profile Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing sign-in and profile requests may resume. Profile folders, registry hives, permissions, profile-list registrations, credentials, user data, policy, service permissions and startup type are preserved. No profile operation is requested. Verification covers stable service state only, not sign-in, profile loading or user-data access.",
            WorkflowKind.CredentialManagerService => "Check Credential Manager service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing applications may resume requests for stored credentials. Vault contents, passwords, passkeys, certificates, tokens, credential registrations, permissions, policy, service permissions and startup type are preserved. No credential content or operation is requested. Verification covers stable service state only, not credential retrieval, authentication or application access.",
            WorkflowKind.ShellHardwareDetectionService => "Inspect bounded storage-controller, disk and portable-device Plug and Play evidence and installed driver references, then check Shell Hardware Detection service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing AutoPlay and hardware-arrival notifications may resume under current policy. Device observations and retained history cannot authorize recovery. Devices, drivers, volumes, mount points, AutoPlay settings, handlers, policy, service permissions and startup type are preserved. No device or setting operation is requested. Verification covers stable service state only, not notification delivery, media readiness, volume access or physical device operation.",
            WorkflowKind.DeviceInstallService => "Check Device Install Service availability. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing Windows-requested device installation work may resume under current policy. Devices, drivers, packages, configuration, bindings, class settings, policy, service permissions and startup type are preserved. KillerBench selects and installs nothing. Verification covers stable service state only, not device installation or operation.",
            WorkflowKind.Display => "Inspect graphics adapters and monitors for reported device errors, hardware IDs and installed driver versions. No driver installation, device reset or resolution change. Picture quality and physical connections are not tested.",
            WorkflowKind.Input => "Inspect keyboard, mouse and HID device errors and driver inventory. No keystrokes are captured, devices reset or drivers changed. Physical input operation is not tested.",
            WorkflowKind.DeviceProblems => "Find nonzero Plug and Play configuration error codes across device classes, then correlate present device evidence with bounded SetupAPI driver installation, device setup and Plug and Play history. Historical records alone do not establish a current fault. Review exact device identity and approved driver evidence without changing hardware, drivers or configuration. Missing devices and physical operation are not verified.",
            WorkflowKind.SystemFiles => "Inventory bounded Windows servicing package, optional-feature and capability states, check prerequisites, scan protected system files, and automatically choose SFC only when the live check confirms corruption. Pending package, feature or capability states request review but do not authorize installation, removal, cleanup, rollback, enablement or disablement.",
            WorkflowKind.Wireless => "Inspect network devices and WLAN AutoConfig availability. Optional recovery starts only an enabled, stopped service after dependency checks; saved profiles may reconnect automatically. Profiles, credentials, policy, radio settings and startup types are preserved. No adapter reset or driver installation. Verification covers running state, not wireless authentication or connectivity.",
            WorkflowKind.WiredAuthentication => "Inspect network devices and Wired AutoConfig for networks requiring 802.1X. Optional recovery starts only an enabled, stopped service after dependency checks. Existing authentication profiles may activate and change network access. Certificates, credentials, profiles, policy and startup settings are preserved. Verification covers running state only, not authentication or connectivity.",
            WorkflowKind.Bluetooth => "Inspect Bluetooth device errors, hardware IDs, installed driver evidence and Bluetooth Support Service availability. Optional recovery starts only an enabled, stopped service after dependency checks; paired devices may reconnect. Pairings, radio settings, device configuration, policy and startup type are preserved. No reset, device removal or driver installation. Verification covers reported device configuration and service running state only, not discovery, pairing, audio or data transfer.",
            WorkflowKind.Camera => "Inspect camera and imaging device errors, hardware IDs, installed driver evidence and Windows Camera Frame Server availability. Optional recovery starts only an enabled, stopped service after dependency checks. An application already requesting camera access may resume under existing permissions. Camera privacy settings, permissions, device configuration, policy, drivers and startup type are preserved. No reset, permission change or driver installation. Verification covers reported device configuration and service running state only, not image capture, privacy access or physical operation.",
            WorkflowKind.SmartCard => "Inspect smart-card reader errors, hardware IDs, installed driver evidence and Smart Card service availability. Optional recovery starts only an enabled, stopped service after dependency checks. Applications may resume detecting inserted cards under existing policy. Certificates, PINs, credentials, reader and card configuration, policy, drivers and startup type are preserved. No initialization, enrollment, PIN operation, reader reset or driver installation. Verification covers reported reader configuration and service running state only, not card detection, authentication or application access.",
            WorkflowKind.Biometric => "Inspect biometric device errors, hardware IDs, installed driver evidence and Windows Biometric Service availability. Optional recovery starts only an enabled, stopped service after dependency checks. Existing Windows Hello and application requests may resume under current policy. Biometric templates, Windows Hello data, credentials, device configuration, permissions, policy, drivers and startup type are preserved. No enrollment, template deletion, sensor reset, permission change or driver installation. Verification covers reported sensor configuration and service running state only, not capture, matching, sign-in or application access.",
            WorkflowKind.Services => "Inventory installed service names, display names, states, start modes and privacy-bounded recovery action types without collecting accounts, executable paths, recovery commands or reboot messages. Review startup-mode coherence, direct dependency identities and cycles, repeat exact pending states, inspect supported services and dependencies of stopped services, then correlate recent Service Control Manager failures only when one exact service name or unique display name is present. Load-order group use is counted without expanding membership, and ambiguous identities remain unresolved. Stopped demand-start services can be normal. No service is started and startup, dependency and recovery settings are preserved. Use the corresponding Repair workflow when a specific feature needs recovery.",
            WorkflowKind.EventLog => "Inspect four diagnostic channels for enabled state, retention mode, current size, configured limit, record count and saturation, then check Windows Event Log service availability. Auto-repair can start a stopped, enabled service after checking dependencies and verify running state. A separately confirmed focused action can enable one exact disabled unmanaged diagnostic channel while preserving every other reported setting. Logs, channel permissions, retention, size limits, startup types and managed settings are preserved. Successful event delivery and archive capacity remain unverified.",
            WorkflowKind.Vitals => "Capture available memory, provider-reported disk health and BitLocker protection status, then correlate current vitals with device problems, reliability history, hardware-error records, crash evidence, unexpected shutdowns and Windows Memory Diagnostic results. Restart indicators, system disk free space, elevation and update service configuration remain visible. Recovery passwords are not collected. No repairs run in this workflow. The combined result is not a complete hardware, software or stability test.",
            WorkflowKind.Network => "Inspect adapters, address state and origin, gateways, DNS configuration, effective DNS namespace policy, TCP settings, native Winsock protocol and namespace-provider catalog structure, the IP routing table and Hosts file mappings. Auto-repair can start enabled DNS Client, DHCP Client, SMB client, Network Location Awareness, Network List and IP Helper services and renew connected physical DHCP adapters lacking a usable IPv4 address. Starting network-identification services may recalculate network identity and apply a different existing firewall profile. IP Helper may activate existing IPv6 transition, port-proxy and network-notification features. Service recovery verifies running state, not successful name resolution, route selection, network identification or tunnel operation. Routes, Hosts entries, DNS servers, profiles, firewall rules, Winsock providers, transition configuration, startup types and policies are preserved. Renewal may change leased addresses and interrupt connections. Static settings and virtual adapters are preserved. Exact gateway ICMP responses do not verify Internet reachability.",
            WorkflowKind.Printing => "Check the Print Spooler, local queue pauses, visible print jobs, offline settings and driver-reported conditions, then inventory queue, driver, port, Standard TCP/IP endpoint, print processor and print monitor registrations. Correlate recent PrintService problems only when one exact current queue identity is present. Assigned Standard TCP/IP endpoints receive one bounded connection attempt without application data, SNMP or a print job. Auto-repair can start the enabled spooler, resume only exact paused queues with fresh waiting-job evidence, and clear offline mode. Idle intentional pauses remain review-only. Remote connections, SNMP community strings, extension registrations and job contents are preserved. A condition code or successful connection does not establish printer identity, supplies, mechanics, queue acceptance, rendering success or physical printing.",
            WorkflowKind.TimeService => "Check Windows Time configuration, service and synchronization. Auto-repair can start W32Time when the current status reports the exact stopped-service error and fresh policy, mode and dependency checks pass, then request a resync and verify fresh status. This can adjust the clock using existing configured sources. Startup type, source configuration and managed settings are preserved.",
            WorkflowKind.Installer => "Check Installer restriction, rollback, elevation, logging and debug policy, in-progress markers, engine binaries and registered cached-package visibility before service recovery. Correlate recent failed and successful outcomes only for exact reported product identities. If prerequisites pass, auto-repair can start the enabled service and verify Running. Historical outcomes, logging, cache visibility and debug findings do not authorize package or policy changes. Policies, logs, transaction markers, startup types and cached packages are preserved. A specific package installation is not tested.",
            WorkflowKind.Storage => "Map bounded disk, partition and drive-letter volume identities, evaluate 10% free-space pressure across every drive-letter volume, inspect VSS shadow-storage allocation and per-volume quota defaults, summarize current Storage module job progress, check the selected NTFS volume and its online self-healing state, and inspect disk/controller devices across this PC. Capacity thresholds, quota settings and job states are review evidence and do not select cleanup, resizing, quota changes or job control. Drive-letter mapping is point-in-time and does not cover every mount path. Device findings are not mapped to the selected volume. The Windows volume is the default. No files, snapshots, jobs or drivers are changed, no setting is changed, no volume is dismounted, and no restart is scheduled.",
            WorkflowKind.Wmi => "Check WMI service availability, then repository consistency. Auto-repair can start an eligible stopped service and salvage confirmed corruption, then verify each result. Disabled service settings are preserved. Readable registrations are merged; unreadable content may require provider reinstallation. Management clients may be interrupted. No reset, deletion or automatic rollback.",
            WorkflowKind.Audio => "Check the default multimedia input and output identities and shared formats, audio device and driver state, Audio Endpoint Builder and Windows Audio. Auto-repair starts stopped, enabled services and verifies running state. Startup settings, drivers, selected devices, exclusive mode and volume levels are preserved. Sound playback, recording and application routing remain untested.",
            WorkflowKind.Search => "Check Windows Search service availability, current SystemIndex activity and recent Search event history. Auto-repair can start a stopped, enabled service and verify running state. A separate explicit index-rebuild choice retains the prior index, starts a fresh index and verifies exact service and directory state. Indexed locations, policy, startup type and retained data are preserved. Starting the service or rebuilding the index can resume background indexing. Search result completeness remains untested.",
            WorkflowKind.Cleanup => "Estimate selected temporary-file, cache and retained diagnostic-evidence targets using the age and file-limit settings in the main panel. Windows Temp and Windows SystemTemp are independent targets. Delivery Optimization is a separate cache choice and removed content may need to be downloaded again. Optional permanent removal uses the separate cleanup checkbox, rechecks retained candidates within the selected limit and records each outcome. Deletion bypasses the Recycle Bin. Targets, age and file limits are also configurable through the cleanup CLI command.",
            WorkflowKind.Defender => "Inspect Defender protection and signature status. Auto-repair refreshes signatures only when Defender reports active protection and outdated signatures. Uses configured signature sources. No protection, exclusion or policy changes; no malware scan.",
            WorkflowKind.AvailableUpdates => "Discover selected software and driver updates using the configured source, Windows Update or an already registered Microsoft Update service. Source readiness is checked before discovery. No installation, license acceptance, source registration or policy changes. This is separate from servicing repair.",
            WorkflowKind.Privacy => "Read selected privacy registry settings for this machine and the account running KillerBench. Separate explicit controls can change Clipboard history or a supported account setting after fresh policy checks, durable preparation and exact verification. Other privacy selections remain read-only. Registry snapshots do not prove effective permissions or policy ownership.",
            WorkflowKind.Profiles => "Review Windows profiles using the selected age threshold, exact ProfileList registrations and optional service and scheduled-task account dependencies, then return one bounded review conclusion. Loaded, system and current-account profiles are excluded from review eligibility. Candidates remain review-only. No profile, folder, registry key, account, service, task, credential, policy or file is changed, and no removal is selected. Application dependencies and backup recoverability remain unverified.",
            WorkflowKind.BitLocker => "Preview BitLocker options and inspect the selected volume. No encryption or recovery backup is performed by this workflow.",
            WorkflowKind.ComputerName => "Review the proposed computer name against current identity and domain role. No rename, credential collection or restart. Name availability, permissions and application dependencies remain unverified.",
            WorkflowKind.Foxit => "Inventory machine-installed Foxit products and updater registry values in both views. User installations, effective policy and product ownership are not verified. No applications or settings are changed.",
            WorkflowKind.AppPackages => "Inventory registered Appx/MSIX packages for the selected account scope. Running account is the default; all users requires administrator access. Report exact identities, framework and non-removable flags, and dependency counts. Provisioned packages, Win32 applications, managed ownership and application data are not evaluated. No packages are removed.",
            WorkflowKind.InstalledApps => "Read application uninstall registrations in the selected machine or running-account scope, then combine that scope with bounded Application error, Reliability Monitor and restart evidence. Other accounts and portable apps are excluded. Registrations and broad history are not exact application-health or ownership proof. No installer, uninstaller, application, process, service, file, registration, account, policy or restart action is selected.",
            WorkflowKind.BrowserExtensions => "Inventory bounded Chrome, Edge, Brave and Firefox extension identities from the running account. Browser history, credentials, extension settings, manifest contents, commands and policy values are excluded. Presence does not prove enablement, trust, ownership or safe removal. No browser, extension or policy is changed.",
            WorkflowKind.Mace => "Review selected OneDrive, Outlook, Office/M365, Project and Teams registrations with account-scope and retention choices. Machine, running-account and registered-package inventories are correlated; all-user review also includes packages provisioned for new users. A supported current-account package can receive a separate exact safeguard plan in the desktop or CLI. Native removal remains disconnected. No process, application, package, file, credential, folder location, registry value or user data is changed.",
            WorkflowKind.Orca => "Review New Outlook, Classic Outlook or both with explicit choices for caches, profile settings, authentication, OST files, add-ins and New Outlook reinstall. Running-account review checks bounded known locations, exact registrations and closed-process readiness. A separately confirmed cache-only action retains exact cache directories under unique recovery names and verifies every move. All-user review and other reset areas remain review-only. No retained cache, profile, OST file, credential, add-in, package or setting is removed.",
            WorkflowKind.Depot => "Review explicitly selected provisioning applications and configuration groups. Current uninstall, app-package and Office Click-to-Run registrations are correlated without downloading or executing installers. Configuration selections describe consequences and route to separately guarded workflows when implemented. No download, installation, account, update, cleanup, policy, registry, service, profile or restart action occurs.",
            WorkflowKind.ScheduledTasks => "Inventory visible scheduled tasks, states and reported principals. No tasks are started, registered or changed. Actions, triggers and execution success are not verified.",
            WorkflowKind.ComponentCleanup => "Analyze whether Windows recommends component-store cleanup. Optional maintenance removes superseded components immediately, then analyzes again. No ResetBase or manual WinSxS deletion. No automatic undo; reclaimed disk space is not measured.",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }) + "\n\nReview the selected tool's options before GO. Only supported actions described above are available. No automatic restart. Workflow results and any repair outcomes are saved locally.";

    public static async Task<WorkflowResult> RunAsync(WorkflowKind kind, bool autoRepair, string folder,
        IProgress<string>? progress = null, CancellationToken token = default, IWorkflowHost? host = null,
        IReadOnlyCollection<string>? selectedRecipes = null,
        IReadOnlyCollection<CommonDiagnosticScope>? selectedDiagnosticScopes = null)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (selectedDiagnosticScopes is not null && (kind != WorkflowKind.CommonProblems || autoRepair || selectedRecipes is { Count: > 0 }))
            throw new ArgumentException("Focused diagnostic scopes require Common problems without repairs or additional recipes.", nameof(selectedDiagnosticScopes));
        var diagnosticScopes = selectedDiagnosticScopes is null ? null : CommonDiagnostics.SelectScopes(selectedDiagnosticScopes);
        var requestedRecipes = selectedRecipes is null ? null : Array.AsReadOnly(selectedRecipes.ToArray());
        var plannedRecipes = diagnosticScopes is not null || kind == WorkflowKind.BackgroundTransfers && requestedRecipes is null
            ? [] : requestedRecipes is null
                ? RecipeCatalog.ForWorkflow(kind).Where(recipe => kind != WorkflowKind.ComputerName || !recipe.RequiresExplicitOptions).ToArray()
                : RecipeCatalog.SelectForWorkflow(kind, requestedRecipes);
        progress = progress is null ? null : new DiagnosticProgress(progress);
        host ??= PlatformProviders.CreateWorkflowHost(progress);
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "workflow-" + Guid.NewGuid().ToString("N") + ".json");
        var checks = new List<WorkflowCheck>();
        var repairs = new List<RepairResult>();
        IReadOnlyList<RecipeResult> recipeResults = [];
        IReadOnlyList<WorkflowCheck> partialInspection = [];
        var started = DateTimeOffset.UtcNow;
        string status = "Running";
        await Save();
        try
        {
            progress?.Report("Checking this PC's current state...");
            checks.AddRange(await Inspect().ConfigureAwait(false));
            bool missingInspection = checks.Count == 0;
            if (missingInspection)
                checks.Add(new("Inspection", CheckState.Unknown, "The inspection returned no evidence. No repair selected."));
            await Save();
            bool blocked = missingInspection || HasBlockingPrerequisite(kind, checks);
            bool summarizeFocusedServices = requestedRecipes is null && FocusedServiceOperationalCorrelation.Supports(kind);
            bool focusedServiceSummaryAdded = false;
            if (plannedRecipes.Count > 0 && !blocked)
            {
                recipeResults = await RecipeEngine.RunAsync(plannedRecipes, autoRepair, folder, host, progress, token,
                    async current => { recipeResults = current; await Save(); }).ConfigureAwait(false);
                repairs.AddRange(recipeResults.Where(r => r.Repair is not null).Select(r => r.Repair!));
                if (kind is WorkflowKind.Network or WorkflowKind.Audio or WorkflowKind.Search && recipeResults.Any(r => r.Outcome == RecipeOutcome.Repaired))
                {
                    progress?.Report("Refreshing device and configuration checks after recovery...");
                    var refreshed = await Inspect().ConfigureAwait(false);
                    checks.Clear();
                    checks.AddRange(refreshed);
                    if (refreshed.Count == 0)
                        checks.Add(new("Follow-up inspection", CheckState.Unknown, "The follow-up inspection returned no evidence. Individual repair verification does not establish refreshed device or configuration health."));
                }
                checks.AddRange(recipeResults.Where(r => r.Check is not null).Select(r => r.Check!));
                checks.AddRange(recipeResults.Where(r => r.Outcome is RecipeOutcome.Blocked or RecipeOutcome.Unknown or RecipeOutcome.NeedsAttention or RecipeOutcome.RestartRequired)
                    .Select(r => new WorkflowCheck(r.RecipeId, CheckState.Attention, r.Detail)));
                if (summarizeFocusedServices)
                {
                    progress?.Report("Summarizing current service outcomes with exact failure history...");
                    var serviceHistory = checks.Where(check => check.Name.EndsWith("failure history", StringComparison.Ordinal)).ToArray();
                    checks.Add(FocusedServiceOperationalCorrelation.Check(plannedRecipes, recipeResults, serviceHistory, token));
                    focusedServiceSummaryAdded = true;
                }
            }
            if (summarizeFocusedServices && !focusedServiceSummaryAdded)
            {
                progress?.Report("Summarizing blocked service diagnosis with exact failure history...");
                var blockedResults = plannedRecipes.Select(recipe => new RecipeResult(recipe.Id, recipe.Version,
                    RecipeOutcome.Blocked, null, null, "A workflow prerequisite prevented the current service recipe check.")).ToArray();
                var serviceHistory = checks.Where(check => check.Name.EndsWith("failure history", StringComparison.Ordinal)).ToArray();
                checks.Add(FocusedServiceOperationalCorrelation.Check(plannedRecipes, blockedResults, serviceHistory, token));
            }
            if (kind == WorkflowKind.WindowsErrorReportingService && requestedRecipes is null)
            {
                progress?.Report("Summarizing crash evidence and Windows Error Reporting readiness...");
                checks.Add(WindowsErrorReportingOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.DiagnosticPolicyService && requestedRecipes is null)
            {
                progress?.Report("Summarizing Diagnostic Policy history and service readiness...");
                checks.Add(DiagnosticPolicyOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.WindowsEventCollectorService && requestedRecipes is null)
            {
                progress?.Report("Summarizing subscriptions and Windows Event Collector readiness...");
                checks.Add(EventCollectorOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.RemoteAccessConnectionManagerService && requestedRecipes is null)
            {
                progress?.Report("Summarizing VPN profiles and Remote Access readiness...");
                checks.Add(RemoteAccessOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.RemoteDesktopServices && requestedRecipes is null)
            {
                progress?.Report("Summarizing Remote Desktop configuration, history and service readiness...");
                checks.Add(RemoteDesktopOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.WindowsRemoteManagement && requestedRecipes is null)
            {
                progress?.Report("Summarizing WinRM listeners, history and service readiness...");
                checks.Add(WinRmOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.SecurityCenterService && requestedRecipes is null)
            {
                progress?.Report("Summarizing antivirus registrations and Security Center readiness...");
                checks.Add(SecurityCenterOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.NetworkConnectionsService && requestedRecipes is null)
            {
                progress?.Report("Summarizing current profiles and Network Connections service readiness...");
                checks.Add(NetworkConnectionsOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.WinHttpAutoProxyService && requestedRecipes is null)
            {
                progress?.Report("Summarizing proxy configuration and WinHTTP Auto-Proxy readiness...");
                checks.Add(WinHttpAutoProxyOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.WindowsConnectionManagerService && requestedRecipes is null)
            {
                progress?.Report("Summarizing current profiles and Windows Connection Manager readiness...");
                checks.Add(ConnectionManagerOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.NetworkSetupService && requestedRecipes is null)
            {
                progress?.Report("Summarizing adapter bindings and Network Setup service readiness...");
                checks.Add(NetworkSetupOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.StorageService && requestedRecipes is null)
            {
                progress?.Report("Summarizing current Storage module jobs and Storage Service readiness...");
                checks.Add(FocusedStorageServiceOperationalCorrelation.Check(checks, token));
            }
            if (requestedRecipes is null && PeripheralOperationalCorrelation.Supports(kind))
            {
                progress?.Report("Summarizing peripheral device, driver and service readiness...");
                checks.Add(PeripheralOperationalCorrelation.Check(kind, checks, token));
            }
            if (kind == WorkflowKind.DeviceAssociationService && requestedRecipes is null)
            {
                progress?.Report("Summarizing device setup, Plug and Play and association-service readiness...");
                checks.Add(DeviceAssociationOperationalCorrelation.Check(checks, token));
            }
            if (requestedRecipes is null && NetworkDiscoveryOperationalCorrelation.Supports(kind))
            {
                progress?.Report("Summarizing network profile and Function Discovery service readiness...");
                checks.Add(NetworkDiscoveryOperationalCorrelation.Check(kind, checks, token));
            }
            if (kind == WorkflowKind.WindowsFontCacheService && requestedRecipes is null)
            {
                progress?.Report("Summarizing font registration and Font Cache service readiness...");
                checks.Add(FontCacheOperationalCorrelation.Check(checks, token));
            }
            if (requestedRecipes is null && FeatureServiceOperationalCorrelation.Supports(kind))
            {
                progress?.Report("Summarizing feature evidence and exact service readiness...");
                checks.Add(FeatureServiceOperationalCorrelation.Check(kind, checks, token));
            }
            if (kind == WorkflowKind.SystemFiles && requestedRecipes is null)
            {
                progress?.Report("Summarizing the protected-file result with servicing prerequisites and bounded log history...");
                var systemFilesResult = recipeResults.SingleOrDefault(result => result.RecipeId == "windows.system-files") ??
                    new RecipeResult("windows.system-files", 1, RecipeOutcome.Blocked, null, null,
                        "A workflow prerequisite prevented the protected-file recipe check.");
                checks.Add(SystemFilesOperationalCorrelation.Check(systemFilesResult, checks, token));
            }
            if (kind == WorkflowKind.WindowsUpdate && requestedRecipes is null)
            {
                progress?.Report("Summarizing focused local Windows Update readiness...");
                checks.Add(FocusedWindowsUpdateOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.Defender && requestedRecipes is null)
            {
                progress?.Report("Summarizing focused Microsoft Defender readiness...");
                checks.Add(FocusedDefenderOperationalCorrelation.Check(checks, token));
            }
            if (kind == WorkflowKind.BackupServices && requestedRecipes is null)
            {
                progress?.Report("Summarizing focused backup-service readiness...");
                checks.Add(FocusedBackupOperationalCorrelation.Check(checks, token));
            }
            token.ThrowIfCancellationRequested();
            bool unresolved = checks.Any(c => c.EffectiveState is CheckState.Attention or CheckState.Unknown ||
                (c.State == CheckState.Repairable && !repairs.Any(r => r.Kind == c.Repair && Repairs.IsVerified(r))));
            bool restartRequired = repairs.Any(repair => repair.RequiresRestart) ||
                recipeResults.Any(recipe => recipe.Outcome == RecipeOutcome.RestartRequired);
            status = restartRequired ? "Restart required; run this workflow again after restarting to verify" : unresolved ? "Needs attention" : repairs.Count > 0 ? "Repairs verified in checked scopes" : "No issues detected in checked scopes";
            if (blocked && plannedRecipes.Count > 0 && repairs.Count == 0 &&
                checks.Any(check => check.Name == "Administrator" && check.State == CheckState.Attention && check.Evidence is null) &&
                checks.All(check => check.Name == "Administrator"
                    ? check.State == CheckState.Attention && check.Evidence is null
                    : check.EffectiveState is CheckState.Clear or CheckState.Information &&
                        check.Evidence is not ({ ExitCode: not 0 } or { TimedOut: true } or { Truncated: true }) &&
                        string.IsNullOrWhiteSpace(check.Evidence?.Error)))
                status = "Administrator access required; workflow incomplete";
            if (!unresolved && repairs.Count == 0 && kind is WorkflowKind.AvailableUpdates or WorkflowKind.Cleanup)
                status = kind == WorkflowKind.AvailableUpdates ? "Discovery completed" : "Preview completed";
            if (!unresolved && kind == WorkflowKind.Privacy) status = "Snapshot completed; effective privacy not evaluated";
            if (!unresolved && kind == WorkflowKind.BitLocker) status = "BitLocker inspection completed; no protection changes";
            if (!unresolved && kind == WorkflowKind.Profiles) status = "Profile review completed; no profiles removed";
            if (!unresolved && kind == WorkflowKind.ComputerName) status = "Name review completed; no rename performed";
            if (!unresolved && kind == WorkflowKind.Foxit) status = "Inventory completed; no changes performed";
            if (!unresolved && kind == WorkflowKind.AppPackages) status = "Package state and deployment history reviewed; no packages changed";
            if (!unresolved && kind == WorkflowKind.InstalledApps) status = "Application registrations and stability history reviewed; no applications changed";
            if (!unresolved && kind == WorkflowKind.BrowserExtensions) status = "Browser extension identities reviewed; no browser or extension changed";
            if (!unresolved && kind == WorkflowKind.StartupApplications) status = "Startup environment reviewed; no registrations changed";
            if (!unresolved && kind == WorkflowKind.BackgroundTransfers) status = "Transfer inventory completed; no jobs changed";
            if (!unresolved && kind == WorkflowKind.ScheduledTasks) status = "Task state and service history reviewed; no tasks changed";
            if (!unresolved && kind == WorkflowKind.Certificates) status = "Certificate validity and local chain evidence reviewed; no certificates changed";
            if (!unresolved && kind == WorkflowKind.WindowsActivation) status = "Windows licensing and sanitized activation history reviewed; no activation changes";
            if (!unresolved && kind == WorkflowKind.StorageSpaces) status = "Storage Spaces pools and virtual disks reviewed; no storage changes";
            if (!unresolved && kind == WorkflowKind.StartupPerformance) status = "Startup and shutdown history reviewed; no startup changes";
            if (!unresolved && kind == WorkflowKind.HardwareErrors) status = "Hardware history and current provider evidence reviewed; no hardware changes";
            if (!unresolved && kind == WorkflowKind.Applications) status = "Application stability history reviewed; current health not evaluated";
            if (!unresolved && kind == WorkflowKind.DeviceProblems) status = "Device state and setup history reviewed; no device or driver changes";
            if (!unresolved && requestedRecipes is { Count: 0 }) status = "Inspection completed; no recipes selected";
            if (restartRequired) status = "Restart required; run this workflow again after restarting to verify";
        }
        catch (OperationCanceledException) { status = "Stopped; checks or verification may be incomplete"; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { status = "Needs attention"; checks.Add(new("Workflow", CheckState.Unknown, e.Message)); }
        checks.AddRange(partialInspection);
        await Save();
        progress?.Report(status);
        return Current();

        async Task<IReadOnlyList<WorkflowCheck>> Inspect()
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromMinutes(5));
            var inspectionToken = deadline.Token;
            var checkpointGate = new SemaphoreSlim(1, 1);
            bool accepting = true;
            bool completed = false;
            partialInspection = [];
            try
            {
                var pending = host.InspectAsync(kind, inspectionToken, async snapshot =>
                {
                    await checkpointGate.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (!accepting || inspectionToken.IsCancellationRequested) return;
                        if (snapshot is null || snapshot.Any(check => check is null))
                            throw new InvalidOperationException("Inspection checkpoint was invalid.");
                        partialInspection = Array.AsReadOnly(snapshot.ToArray());
                        await Write(Current() with { Checks = partialInspection }).ConfigureAwait(false);
                    }
                    finally { checkpointGate.Release(); }
                }, diagnosticScopes) ?? throw new InvalidOperationException("Inspection returned no task.");
                var result = await pending.WaitAsync(inspectionToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Inspection returned no results.");
                completed = true;
                return result;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
            { throw new TimeoutException("Workflow inspection exceeded five minutes. Evidence is incomplete; no further repair selected."); }
            finally
            {
                await checkpointGate.WaitAsync().ConfigureAwait(false);
                try { accepting = false; if (completed) partialInspection = []; }
                finally { checkpointGate.Release(); }
            }
        }

        WorkflowResult Current() => new(kind, Environment.MachineName, started, status, checks.ToArray(), recipeResults.Where(r => r.Repair is not null).Select(r => r.Repair!).ToArray(), path)
        { Recipes = recipeResults, RequestedRecipeIds = requestedRecipes, PlannedRecipeIds = Array.AsReadOnly(plannedRecipes.Select(recipe => recipe.Id).ToArray()), SelectedDiagnosticScopes = diagnosticScopes };
        Task Save() => Write(Current());
        Task Write(WorkflowResult snapshot) => JournalPersistence.WriteAsync(path,
            JsonSerializer.Serialize(snapshot, Capture.JsonOptions));
    }

    public static WorkflowCheck Interpret(RepairKind kind, ToolResult result)
    {
        if (kind == RepairKind.ComponentCleanup) return ComponentCleanup.Interpret(result);
        if (kind is not (RepairKind.ComponentStore or RepairKind.SystemFiles))
            return new(kind.ToString(), CheckState.Unknown, "This action requires its own diagnostic interpreter. No integrity verdict selected.", Evidence: result);
        string name = kind.ToString();
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, $"Check incomplete (exit {result.ExitCode}, truncated: {result.Truncated}, timed out: {result.TimedOut}). No automatic repair selected.", Evidence: result);
        string clean = kind == RepairKind.ComponentStore ? "No component store corruption detected" : "Windows Resource Protection did not find any integrity violations";
        string corrupt = kind == RepairKind.ComponentStore ? "The component store is repairable" : "Windows Resource Protection found integrity violations";
        string? unrecoverable = kind == RepairKind.ComponentStore ? "The component store cannot be repaired" : null;
        bool clear = result.Output.Contains(clean, StringComparison.OrdinalIgnoreCase);
        bool damaged = result.Output.Contains(corrupt, StringComparison.OrdinalIgnoreCase);
        bool cannotRepair = unrecoverable is not null && result.Output.Contains(unrecoverable, StringComparison.OrdinalIgnoreCase);
        if (clear && !damaged && !cannotRepair) return new(name, CheckState.Clear, clean, Evidence: result);
        if (damaged && !clear && !cannotRepair) return new(name, CheckState.Repairable, corrupt, kind, result);
        if (cannotRepair && !clear && !damaged) return new(name, CheckState.Attention,
            "DISM reports that the component store cannot be repaired. Automatic RestoreHealth is not selected. Preserve current evidence and review a matching Windows repair source, in-place repair or recovery plan before making further changes.", Evidence: result);
        return new(name, CheckState.Unknown, "Check output was not recognized. No automatic repair selected.", Evidence: result);
    }
}
