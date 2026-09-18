using System.Text.Json;
using Microsoft.Win32;
using System.Net.NetworkInformation;

namespace KillerBench.Engine;

public enum WorkflowKind { WindowsUpdate, SystemFiles, Vitals, Network, Printing, TimeService, Installer, Storage, Wmi, Audio, Search, Cleanup, Defender, AvailableUpdates, Privacy, BitLocker, ComponentCleanup, Profiles, ComputerName, Foxit, ScheduledTasks, Services, EventLog, Applications, Display, Input, DeviceProblems, CommonProblems, AppPackages, InstalledApps, BackupServices, BackgroundTransfers, FirewallServices, Wireless, WiredAuthentication, Bluetooth, Camera, SmartCard, Biometric, ReliabilityHistory, HardwareErrors, DriverInstallHistory, SystemRestore, WindowsRecovery, Certificates, FileSharing, TaskSchedulerService, WindowsUpdateService, DeliveryOptimizationService, BitsService, CryptographicService, ModulesInstallerService, WindowsUpdateMedicService, UpdateOrchestratorService, DiagnosticPolicyService, WindowsErrorReportingService, WindowsEventCollectorService, RemoteAccessConnectionManagerService, RemoteDesktopServices, WindowsRemoteManagement, SecurityCenterService, NetworkConnectionsService, WinHttpAutoProxyService, WindowsConnectionManagerService, NetworkSetupService, AppReadinessService, StateRepositoryService, UserProfileService, CredentialManagerService, ShellHardwareDetectionService, DeviceInstallService, AppxDeploymentService, ClientLicenseService, StoreInstallService, WindowsLicenseManagerService, PushToInstallService, WebAccountManagerService, MicrosoftAccountSignInService, StorageService, OptimizeDrivesService, WindowsImageAcquisitionService, FunctionDiscoveryProviderHostService, FunctionDiscoveryResourcePublicationService, HumanInterfaceDeviceService, PortableDeviceEnumeratorService, WindowsFontCacheService, DeviceAssociationService, TextInputManagementService, DisplayEnhancementService, SystemNotifications, CoreWindowsServices, StorageSpaces, WindowsActivation, StartupPerformance, StartupApplications, Mace, Orca, Depot, Beam, Debloat, ServicingActivity, BrowserExtensions }
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

    public static bool SupportsRepair(WorkflowKind kind) => Enum.IsDefined(kind) && kind is not (WorkflowKind.Vitals or WorkflowKind.Storage or WorkflowKind.StorageSpaces or WorkflowKind.WindowsActivation or WorkflowKind.StartupPerformance or WorkflowKind.StartupApplications or WorkflowKind.Cleanup or WorkflowKind.AvailableUpdates or WorkflowKind.Privacy or WorkflowKind.Profiles or WorkflowKind.ComputerName or WorkflowKind.Foxit or WorkflowKind.ScheduledTasks or WorkflowKind.Services or WorkflowKind.Applications or WorkflowKind.Display or WorkflowKind.Input or WorkflowKind.DeviceProblems or WorkflowKind.AppPackages or WorkflowKind.InstalledApps or WorkflowKind.BackgroundTransfers or WorkflowKind.ReliabilityHistory or WorkflowKind.HardwareErrors or WorkflowKind.DriverInstallHistory or WorkflowKind.Certificates or WorkflowKind.Mace or WorkflowKind.Orca or WorkflowKind.Depot or WorkflowKind.Beam or WorkflowKind.Debloat or WorkflowKind.ServicingActivity or WorkflowKind.BrowserExtensions);
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
            WorkflowKind.Beam => "Review an Intune Win32 deployment plan for one local or hosted EXE/MSI and explicit device hostnames. Validate the source shape, package metadata, SYSTEM install command, required group assignment, marker detection, restart behavior and important limitations. Local files receive a bounded header check; hosted installers are not fetched. No authentication, download, packaging, upload, application change, group change, assignment, device sync or installer execution occurs.",
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
        host ??= new WindowsWorkflowHost { Progress = progress };
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

public sealed class WindowsWorkflowHost : IWorkflowHost
{
    public CleanupOptions CleanupOptions { get; init; } = new(CleanupTargets.UserTemp);
    public WorkflowCheck? OptionPlanCheck { get; init; }
    public FileSystemRepairSelection? FileSystemRepairSelection { get; init; }
    public VolumeOptimizationRequest? VolumeOptimizationRequest { get; init; }
    public SystemRestoreCreationRequest? SystemRestoreRequest { get; init; }
    public PrintingTargetSelection? PrintingTargetSelection { get; init; }
    private string? storageVolume;
    private string? networkLookupHost;
    private string? msiProductCode;
    private (string Host, int Port)? networkEndpoint;
    private TimeSpan networkTimeout = TimeSpan.FromSeconds(10);
    public TimeSpan NetworkTimeout
    {
        get => networkTimeout;
        init
        {
            if (value <= TimeSpan.Zero || value > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(NetworkTimeout));
            networkTimeout = value;
        }
    }
    public (string Host, int Port)? NetworkEndpoint
    {
        get => networkEndpoint;
        init
        {
            if (value is { } endpoint) TcpDiagnostics.Validate(endpoint.Host, endpoint.Port);
            networkEndpoint = value;
        }
    }
    public string? NetworkLookupHost
    {
        get => networkLookupHost;
        init => networkLookupHost = value is null ? null : DnsDiagnostics.ValidateHost(value);
    }
    public string? StorageVolume
    {
        get => storageVolume;
        init => storageVolume = value is null ? null : StorageDiagnostics.NormalizeVolume(value);
    }
    public string? MsiProductCode
    {
        get => msiProductCode;
        init => msiProductCode = value is null ? null : MsiProductDiagnostics.NormalizeProductCode(value);
    }
    public SecurityOptions SecurityOptions { get; init; } = new();
    public AudioEndpointSelection AudioEndpointSelection { get; init; } = new();
    public PrivacyOptions PrivacyOptions { get; init; } = new([]);
    public int ProfileMinimumDays { get; init; } = 90;
    public bool CheckProfileDependencies { get; init; }
    public ProfileInventoryResult? LastProfileInventory { get; private set; }
    public ProfileRegistrationResult? LastProfileRegistrations { get; private set; }
    public AppPackageInventoryResult? LastAppPackageInventory { get; private set; }
    public InstalledApplicationInventory? LastApplicationInventory { get; private set; }
    public MaceRemovalReview? LastMaceReview { get; private set; }
    public OrcaReview? LastOrcaReview { get; private set; }
    public DepotReview? LastDepotReview { get; private set; }
    public BeamReview? LastBeamReview { get; private set; }
    public DebloatReview? LastDebloatReview { get; private set; }
    public BitsInventoryResult? LastBitsInventory { get; private set; }
    public DnsLookupResult? LastDnsLookup { get; private set; }
    public WorkflowCheck? LastMsiProduct { get; private set; }
    public IReadOnlyList<string>? LastPrintQueueTargets { get; private set; }
    public bool BitsAllUsers { get; init; }
    public Guid? BitsSelectedJobId { get; init; }
    public TimeOptions TimeOptions { get; init; } = new();
    public AppPackageScope PackageScope { get; init; } = AppPackageScope.CurrentAccount;
    public InstalledApplicationScope ApplicationScope { get; init; } = InstalledApplicationScope.Machine;
    public Func<CancellationToken, BrowserExtensionReviewResult> BrowserExtensionReader { get; init; } =
        token => BrowserExtensionReview.Read(token);
    public MaceReviewOptions MaceOptions { get; init; } = new(MaceTargets.None);
    public OrcaReviewOptions OrcaOptions { get; init; } = new(OrcaOutlookMode.Both, OrcaScope.RunningAccount);
    public DepotOptions DepotOptions { get; init; } = new(DepotApplications.None, DepotConfiguration.None);
    public BeamOptions BeamOptions { get; init; } = new(BeamSourceKind.LocalFile, "", BeamInstallerType.Exe,
        "", "", "", [], "");
    public DebloatOptions DebloatOptions { get; init; } = new(DebloatAreas.None, DebloatUserScope.RunningAccount);
    public IDebloatPowerPlanChangeHost DebloatPowerPlanHost { get; init; } = new WindowsDebloatPowerPlanChangeHost();
    public IDebloatHibernationHost DebloatHibernationHost { get; init; } = new WindowsDebloatHibernationHost();
    public IDebloatPowerTimerHost DebloatPowerTimerHost { get; init; } = new WindowsDebloatPowerTimerHost();
    public string DebloatChangeFolder { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Debloat");
    public IPreviousWindowsInstallationHost PreviousWindowsInstallationHost { get; init; } = new WindowsPreviousWindowsInstallationHost();
    public string PreviousWindowsInstallationFolder { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "PreviousWindowsInstallation");
    public bool IncludeProvisionedPackages { get; init; }
    public ProfileDependencyResult? LastProfileDependencies { get; private set; }
    public BitLockerOptions BitLockerOptions { get; init; } = new("C:");
    public Guid? BitLockerRecoveryProtector { get; init; }
    public IBitLockerBackupHost BitLockerBackupHost { get; init; } = new WindowsBitLockerBackupHost();
    public string BitLockerBackupFolder { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitLockerBackup");
    public UpdateSearchOptions UpdateOptions { get; init; } = new();
    public bool IncludeThirdPartyUpdates { get; init; }
    public Func<CancellationToken, Task<ThirdPartyUpdateResult>> ThirdPartyUpdates { get; init; } =
        token => ThirdPartyUpdateDiscovery.ReadAsync(token: token);
    public string? ProposedComputerName { get; init; }
    public IProgress<string>? Progress { get; init; }
    public KnowledgeCatalogCollection? KnowledgeCatalogs { get; init; }
    public Func<string, TimeSpan, CancellationToken, Task<DnsLookupResult>> DnsLookup { get; init; } =
        (host, timeout, token) => DnsDiagnostics.CheckAsync(host, timeout, token);
    public Func<System.Net.IPAddress, TimeSpan, CancellationToken, Task<GatewayProbeObservation>> GatewayProbe { get; init; } =
        GatewayReachabilityDiagnostics.ProbeAsync;
    public IDnsCacheRecoveryHost DnsRecoveryHost { get; init; } = new WindowsDnsCacheRecoveryHost();
    public Func<string, IDnsCacheRecoveryJournal> DnsRecoveryJournalFactory { get; init; } = folder => new DnsCacheRecoveryJournal(folder);
    public string? EventChannel { get; init; }
    public FirewallProfileEnableOptions? FirewallProfileSelection { get; init; }
    public string EventChannelChangeFolder { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "EventChannels");
    public Func<string, CancellationToken, Task<StorageVolumeState>> FileSystemVolumeInspection { get; init; } =
        (volume, token) => new WindowsFileSystemRepairHost().InspectAsync(volume, token);
    public Func<string, CancellationToken, Task<WorkflowCheck>> FileSystemDiagnosis { get; init; } =
        (volume, token) => new WindowsFileSystemRepairHost().DiagnoseAsync(volume, token);

    public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) => InspectAsync(kind, token, _ => Task.CompletedTask);

    public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token,
        Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint)
        => InspectAsync(kind, token, checkpoint, null);

    public async Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token,
        Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint, IReadOnlyList<CommonDiagnosticScope>? selectedScopes)
    {
        IReadOnlyList<WorkflowCheck> checks = await InspectCoreAsync(kind, token, checkpoint, selectedScopes).ConfigureAwait(false);
        return OptionPlanCheck is null ? checks : [OptionPlanCheck, .. checks];
    }

    private async Task<IReadOnlyList<WorkflowCheck>> InspectCoreAsync(WorkflowKind kind, CancellationToken token,
        Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint, IReadOnlyList<CommonDiagnosticScope>? selectedScopes)
    {
        if (selectedScopes is not null)
        {
            if (kind != WorkflowKind.CommonProblems) throw new ArgumentException("Focused areas require Common problems.", nameof(selectedScopes));
            return await CommonDiagnostics.CheckAsync(token, Progress, checkpoint: checkpoint, selectedScopes: selectedScopes,
                knowledgeCatalogs: KnowledgeCatalogs).ConfigureAwait(false);
        }
        if (kind == WorkflowKind.Display)
            return await DeviceDiagnostics.CheckAsync(DeviceCategory.Display, KnowledgeCatalogs, token).ConfigureAwait(false);
        if (kind == WorkflowKind.Input)
            return await DeviceDiagnostics.CheckAsync(DeviceCategory.Input, KnowledgeCatalogs, token).ConfigureAwait(false);
        if (kind == WorkflowKind.DeviceProblems)
        {
            Progress?.Report("Reading current Plug and Play device problems...");
            var devices = await DeviceDiagnostics.CheckAsync(DeviceCategory.Problems, KnowledgeCatalogs, token).ConfigureAwait(false);
            Progress?.Report("Reading bounded SetupAPI driver installation history...");
            var driverHistory = await SetupApiDriverInstall.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading recent device setup and Plug and Play history...");
            var setupHistory = await DeviceSetupEvents.CheckAsync(token).ConfigureAwait(false);
            var plugAndPlayHistory = await KernelPnpEvents.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Correlating current device problems with retained setup history...");
            return [.. devices, .. driverHistory, setupHistory, plugAndPlayHistory,
                DeviceOperationalCorrelation.Check(devices, driverHistory, setupHistory, plugAndPlayHistory, token)];
        }
        if (kind == WorkflowKind.Applications)
        {
            Progress?.Report("Reading recent Application errors...");
            var applicationEvents = await ApplicationEvents.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading recent Windows Reliability Monitor application history...");
            var reliability = await ReliabilityHistory.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Summarizing bounded application stability history...");
            return [applicationEvents, .. reliability, ApplicationStabilityCorrelation.Check(applicationEvents, reliability, token)];
        }
        if (kind == WorkflowKind.ReliabilityHistory)
        {
            Progress?.Report("Reading recent Windows reliability history...");
            var reliability = await ReliabilityHistory.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading recent Application errors...");
            var applications = await ApplicationEvents.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading recent unexpected shutdown history...");
            var shutdowns = await ShutdownEvents.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Correlating bounded reliability history...");
            return [.. reliability, applications, shutdowns,
                ReliabilityOperationalCorrelation.Check(applications, shutdowns, reliability, token)];
        }
        if (kind == WorkflowKind.HardwareErrors)
        {
            Progress?.Report("Reading recent Windows hardware error records...");
            var hardwareHistory = await HardwareErrorEvents.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading recent Windows Memory Diagnostic results...");
            var memoryHistory = await MemoryDiagnosticResults.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading current Plug and Play device problems...");
            var deviceProblems = await DeviceDiagnostics.CheckAsync(DeviceCategory.Problems, token).ConfigureAwait(false);
            Progress?.Report("Reading current physical disks and reliability counters...");
            var physicalDisks = await PhysicalDiskDiagnostics.CheckAsync(token).ConfigureAwait(false);
            var reliability = await StorageReliability.CheckAsync(token).ConfigureAwait(false);
            var physicalCorrelation = PhysicalDiskCorrelation.Check(physicalDisks, reliability, token);
            Progress?.Report("Correlating hardware history and current provider evidence...");
            var conclusion = HardwareOperationalCorrelation.Check(hardwareHistory, memoryHistory,
                deviceProblems, physicalCorrelation, token);
            return [hardwareHistory, memoryHistory, .. deviceProblems, .. physicalDisks, .. reliability,
                physicalCorrelation, conclusion];
        }
        if (kind == WorkflowKind.DriverInstallHistory)
        {
            Progress?.Report("Reading recent SetupAPI driver installation history...");
            var history = await SetupApiDriverInstall.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Summarizing driver installation history and current device correlation...");
            return [.. history, DriverHistoryOperationalCorrelation.Check(history, token)];
        }
        if (kind == WorkflowKind.SystemRestore)
        {
            Progress?.Report("Reading available System Restore points...");
            var inventory = await SystemRestoreDiagnostics.CheckAsync(token).ConfigureAwait(false);
            var request = SystemRestoreRequest ??
                SystemRestoreCreation.CreateRequest("KillerBench restore point", SystemRestorePointReason.ModifySettings);
            var review = await SystemRestoreCreation.ReviewAsync(request, new WindowsSystemRestoreCreationHost(), token).ConfigureAwait(false);
            var creation = SystemRestoreCreation.ToWorkflowCheck(review);
            Progress?.Report("Summarizing System Restore inventory and creation readiness...");
            return [inventory, creation, SystemRestoreOperationalCorrelation.Check(inventory, creation, token)];
        }
        if (kind == WorkflowKind.WindowsRecovery)
        {
            Progress?.Report("Reading Windows Recovery Environment status...");
            var recovery = await WindowsRecoveryDiagnostics.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Correlating the configured Windows Recovery partition...");
            var partition = await WindowsRecoveryPartitionCorrelation.CheckAsync(recovery, token).ConfigureAwait(false);
            Progress?.Report("Summarizing Windows Recovery readiness...");
            return [recovery, partition, WindowsRecoveryOperationalCorrelation.Check(recovery, partition, token)];
        }
        if (kind == WorkflowKind.Certificates)
        {
            Progress?.Report("Reviewing personal certificate dates and local chain trust...");
            var expiration = await CertificateDiagnostics.CheckAsync(token).ConfigureAwait(false);
            var chainTrust = await CertificateChainDiagnostics.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Correlating certificate validity and local chain evidence...");
            return [expiration, chainTrust, CertificateOperationalCorrelation.Check(expiration, chainTrust, token)];
        }
        if (kind == WorkflowKind.ScheduledTasks)
        {
            Progress?.Report("Reading scheduled task identities and principals...");
            var inventory = await ScheduledTaskInventory.ReadAsync(token).ConfigureAwait(false);
            var findings = new List<WorkflowCheck>
            {
                new("Task inventory coverage", inventory.Complete ? CheckState.Information : CheckState.Unknown,
                    $"{inventory.Tasks.Count} visible tasks collected. " + (inventory.Complete ? "Provider collection completed. " : "Collection incomplete. ") +
                    "Inaccessible tasks, action safety, triggers and execution success are not verified. No tasks changed.", Evidence: inventory.Evidence)
            };
            findings.AddRange(inventory.Tasks.Select(task => ScheduledTaskDiagnostics.Assess(task)));
            Progress?.Report("Reading exact Task Scheduler service failure history...");
            var serviceHistory = await InfrastructureServiceFailureEvents.CheckAsync(
                InfrastructureServiceFailureArea.TaskScheduler, token).ConfigureAwait(false);
            findings.AddRange(serviceHistory);
            Progress?.Report("Correlating task runtime results with exact service history...");
            findings.Add(ScheduledTaskOperationalCorrelation.Check(findings
                .Where(check => check.Name != "Task Scheduler service failure history").ToArray(), serviceHistory, token));
            return findings;
        }
        if (kind == WorkflowKind.AppPackages)
        {
            Progress?.Report("Reading registered packages: " + PackageScope + "...");
            LastAppPackageInventory = null;
            LastAppPackageInventory = await AppPackageInventory.ReadAsync(PackageScope, token).ConfigureAwait(false);
            var findings = AppPackageDiagnostics.Describe(LastAppPackageInventory).ToList();
            WorkflowCheck? provisionedSummary = null;
            if (IncludeProvisionedPackages)
            {
                Progress?.Report("Reading packages provisioned for new users...");
                try
                {
                    var provisioned = ProvisionedPackages.Describe(await ProvisionedPackages.ReadAsync(token).ConfigureAwait(false));
                    provisionedSummary = provisioned[0];
                    findings.AddRange(provisioned);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
                {
                    provisionedSummary = new("Provisioned app packages", CheckState.Unknown, "Collection unavailable: " + error.Message + ". Registered-package evidence is retained. No packages changed.");
                    findings.Add(provisionedSummary);
                }
            }
            var registeredSummary = AppPackageDiagnostics.Summarize(findings
                .Where(check => check.Name != "Provisioned app packages" && !check.Name.StartsWith("Provisioned: ", StringComparison.Ordinal)).ToArray());
            findings.Add(registeredSummary);
            Progress?.Report("Reading recent AppX deployment and Microsoft Store installation history...");
            var deploymentHistory = await AppDeploymentEvents.CheckAsync(token).ConfigureAwait(false);
            var storeHistory = await StoreInstallEvents.CheckAsync(token).ConfigureAwait(false);
            var restartState = RestartDiagnostics.Check();
            findings.AddRange([deploymentHistory, storeHistory, restartState]);
            Progress?.Report("Correlating package state, deployment history and restart evidence...");
            findings.Add(AppPackageOperationalCorrelation.Check(registeredSummary, provisionedSummary,
                deploymentHistory, storeHistory, restartState, token));
            return findings;
        }
        if (kind == WorkflowKind.InstalledApps)
        {
            LastApplicationInventory = null;
            Progress?.Report("Reading " + ApplicationScope + " application registrations...");
            LastApplicationInventory = await InstalledApplications.ReadAsync(ApplicationScope, token).ConfigureAwait(false);
            var findings = InstalledApplications.Describe(LastApplicationInventory).ToList();
            var registrations = findings[0];
            Progress?.Report("Reading bounded application failure and reliability history...");
            var applicationEvents = await ApplicationEvents.CheckAsync(token).ConfigureAwait(false);
            var reliability = await ReliabilityHistory.CheckAsync(token).ConfigureAwait(false);
            var stability = ApplicationStabilityCorrelation.Check(applicationEvents, reliability, token);
            var restartState = RestartDiagnostics.Check();
            findings.Add(applicationEvents);
            findings.AddRange(reliability);
            findings.Add(stability);
            findings.Add(restartState);
            Progress?.Report("Correlating scoped registrations, stability history and restart evidence...");
            findings.Add(InstalledApplicationOperationalCorrelation.Check(registrations, stability, restartState, token));
            return findings;
        }
        if (kind == WorkflowKind.BrowserExtensions)
        {
            Progress?.Report("Reviewing browser extension identities...");
            BrowserExtensionReviewResult review = await Task.Run(() => BrowserExtensionReader(token), token).ConfigureAwait(false);
            var findings = new List<WorkflowCheck>
            {
                new("Browser extension inventory", review.Complete ? CheckState.Information : CheckState.Unknown,
                    review.Extensions.Count + " extension identities collected. " + review.Detail)
            };
            findings.AddRange(review.Extensions.Select(extension => new WorkflowCheck(
                extension.Browser + " extension: " + extension.ExtensionId, CheckState.Information,
                "Profile: " + extension.Profile + ". Identity only; this does not prove enablement, trust, ownership or safe removal. No browser or extension changed.")));
            return findings;
        }
        if (kind == WorkflowKind.Mace)
        {
            LastMaceReview = null;
            Progress?.Report("Collecting selected Microsoft application registrations for review...");
            LastMaceReview = await MacePlanning.ReviewCurrentAsync(MaceOptions, Progress, token).ConfigureAwait(false);
            return LastMaceReview.Checks;
        }
        if (kind == WorkflowKind.Orca)
        {
            LastOrcaReview = null;
            Progress?.Report("Collecting selected Outlook registration and local-data evidence for review...");
            LastOrcaReview = OrcaPlanning.ReviewCurrent(OrcaOptions, token);
            return LastOrcaReview.Checks;
        }
        if (kind == WorkflowKind.Depot)
        {
            LastDepotReview = null;
            Progress?.Report("Collecting selected provisioning application registrations...");
            LastDepotReview = await DepotPlanning.ReviewCurrentAsync(DepotOptions, Progress, token).ConfigureAwait(false);
            return LastDepotReview.Checks;
        }
        if (kind == WorkflowKind.Beam)
        {
            LastBeamReview = null;
            Progress?.Report("Validating the selected BEAM source, package metadata and Intune targets...");
            LastBeamReview = await BeamPlanning.ReviewAsync(BeamOptions, Progress, token).ConfigureAwait(false);
            return LastBeamReview.Checks;
        }
        if (kind == WorkflowKind.Debloat)
        {
            LastDebloatReview = null;
            Progress?.Report("Reviewing the selected DEBLOAT areas and preservation boundaries...");
            LastDebloatReview = await DebloatPlanning.ReviewAsync(DebloatOptions, Progress, token).ConfigureAwait(false);
            return LastDebloatReview.Checks;
        }
        if (kind == WorkflowKind.BackgroundTransfers)
        {
            LastBitsInventory = null;
            Progress?.Report("Reading BITS transfer inventory...");
            LastBitsInventory = await BitsDiagnostics.ReadAsync(BitsAllUsers, token).ConfigureAwait(false);
            var jobs = BitsDiagnostics.Interpret(LastBitsInventory.Evidence, token);
            Progress?.Report("Reading current BITS service state...");
            var service = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["BITS"], token).ConfigureAwait(false)).Single();
            Progress?.Report("Reading exact BITS service-failure history...");
            var history = await ServicingServiceFailureEvents.CheckBitsAsync(token).ConfigureAwait(false);
            Progress?.Report("Correlating background-transfer readiness...");
            return [jobs, service, history, BitsOperationalCorrelation.Check(jobs, service, history, token)];
        }
        if (kind == WorkflowKind.Foxit)
        {
            Progress?.Report("Reading machine Foxit product inventory...");
            var inventory = await FoxitInventory.ReadAsync(token).ConfigureAwait(false);
            var findings = new List<WorkflowCheck>
            {
                new("Foxit inventory coverage", inventory.Complete ? CheckState.Information : CheckState.Unknown,
                    $"{inventory.Products.Count} matching machine entries. " + (inventory.Complete ? "Both registry views inspected. " : "Collection incomplete; absence cannot be established. ") +
                    "User installations, executable identity and update policy are not verified. No changes made.", Evidence: inventory.Evidence)
            };
            findings.AddRange(inventory.Products.Select(product => new WorkflowCheck("Foxit product: " + product.Name, CheckState.Information,
                $"Version: {product.Version ?? "unreported"}; publisher: {product.Publisher ?? "unreported"}; view: {product.View}; uninstall key: {product.Key}. Registry inventory only.", Evidence: inventory.Evidence)));
            Progress?.Report("Reading Foxit updater settings without changing them...");
            findings.Add(await FoxitUpdaterSettings.CheckAsync(token).ConfigureAwait(false));
            return findings;
        }
        if (kind == WorkflowKind.ComputerName)
        {
            if (string.IsNullOrWhiteSpace(ProposedComputerName))
                return [new("Computer name review", CheckState.Unknown, "Enter a proposed computer name. No changes made.")];
            Progress?.Report("Reviewing current and configured identity for the exact proposed name...");
            var report = await ComputerNameReview.ReviewCurrentAsync(ProposedComputerName, token).ConfigureAwait(false);
            Progress?.Report("Reading restart evidence for the computer name review...");
            var computerNameRestart = RestartDiagnostics.Check();
            return [ComputerRename.ReviewSelection(ProposedComputerName, report.Context,
                WindowsState.IsAdministrator(), computerNameRestart)];
        }
        if (kind == WorkflowKind.Profiles)
        {
            var inventory = await ProfileInventory.ReadAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading exact ProfileList registrations...");
            var registrations = await ProfileRegistrations.ReadAsync(token).ConfigureAwait(false);
            var choices = ProfilePlanning.Review(inventory.Profiles, ProfileMinimumDays, DateTimeOffset.UtcNow, inventory.Complete, token);
            ProfileDependencyResult? dependencies = null;
            if (CheckProfileDependencies)
            {
                Progress?.Report("Checking service and scheduled-task account dependencies...");
                dependencies = await ProfileDependencies.ReadAsync(token).ConfigureAwait(false);
            }
            LastProfileInventory = inventory;
            LastProfileRegistrations = registrations;
            LastProfileDependencies = dependencies;
            var inventoryCheck = new WorkflowCheck("Profile inventory", inventory.Complete ? CheckState.Information : CheckState.Unknown,
                "Minimum age: " + ProfileMinimumDays + " days. No profiles removed.\n" +
                (inventory.Complete ? choices.Count + " profiles; " + choices.Count(c => c.ReviewEligible) + " candidates for review.\n" : "Inventory unavailable or incomplete; no removal decisions possible.\n") +
                string.Join("\n", choices.Select(c => c.Profile.Path + " | Last use: " + (c.Profile.LastUsedUtc?.ToString("u") ?? "Unknown") + " | " + c.Reason +
                    (dependencies is null ? " | Dependencies not checked." : " | Matching dependencies: " + string.Join(", ", dependencies.ForSid(c.Profile.Sid).Select(d => d.Kind + " " + d.Name))))));
            var registrationCheck = ProfileRegistrations.Check(registrations);
            var dependencyCheck = ProfileDependencyAssessment.Check(choices, dependencies, token);
            Progress?.Report("Correlating profile inventory, registrations and selected dependencies...");
            var conclusion = ProfileOperationalCorrelation.Check(inventoryCheck, registrationCheck, dependencyCheck, token);
            return [inventoryCheck, registrationCheck, dependencyCheck, conclusion];
        }
        if (kind == WorkflowKind.BitLocker)
        {
            string preview = BitLockerOptions.Describe();
            var state = await BitLockerDiagnostics.CheckVolumeAsync(BitLockerOptions.Volume, token).ConfigureAwait(false);
            return [new("BitLocker options", CheckState.Information, preview), .. state];
        }
        if (kind == WorkflowKind.Privacy)
        {
            Progress?.Report("Reading selected privacy registry values...");
            var values = PrivacyInspection.Inspect(PrivacyOptions, token: token);
            Progress?.Report("Reading related privacy service states...");
            var services = await PrivacyInspection.InspectServicesAsync(PrivacyOptions, token: token).ConfigureAwait(false);
            bool incomplete = values.Any(value => value.State is "Unavailable" or "Unsupported value") ||
                services.Any(service => service.State == "Unavailable");
            return [new("Privacy snapshot", incomplete ? CheckState.Unknown : CheckState.Information,
                PrivacyInspection.Format(values, services))];
        }
        if (kind == WorkflowKind.AvailableUpdates)
        {
            var thirdParty = new List<WorkflowCheck>();
            if (IncludeThirdPartyUpdates)
            {
                var applicationUpdates = await ThirdPartyUpdates(token).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Third-party update provider returned no result.");
                thirdParty.Add(ThirdPartyUpdateDiscovery.Assess(applicationUpdates));
            }
            if (!UpdateOptions.Software && !UpdateOptions.Drivers)
                return [new("Windows Update discovery", CheckState.Information,
                    "Windows software and driver discovery were not selected. No Windows Update source, policy, history or hotfix evidence was collected."), .. thirdParty];
            var featurePolicy = FeatureUpdatePolicyDiagnostics.Check();
            var policySurfaces = UpdatePolicySurfaceDiagnostics.Check();
            var restartPolicy = UpdateRestartPolicyDiagnostics.Check();
            Progress?.Report("Reading registered Windows Update services without changing source enrollment...");
            var source = await UpdateServiceDiagnostics.CheckAsync(token).ConfigureAwait(false);
            var sourceReadiness = source.Evidence is null
                ? new WorkflowCheck("Selected update source", CheckState.Unknown, "Update source registration evidence is unavailable. The selected source was not searched.")
                : UpdateServiceDiagnostics.AssessSource(UpdateOptions, source.Evidence);
            Progress?.Report("Reading recent Windows Update operation history...");
            var history = await UpdateHistoryDiagnostics.CheckAsync(token).ConfigureAwait(false);
            Progress?.Report("Reading installed Windows hotfix inventory...");
            var installedHotfixes = await InstalledHotfixDiagnostics.CheckAsync(token).ConfigureAwait(false);
            var followups = history.Evidence is null ? [] : await UpdateFailureFollowup.CheckAsync(
                UpdateHistoryDiagnostics.RecentReferences(history.Evidence), Progress, token).ConfigureAwait(false);
            if (sourceReadiness.State is CheckState.Unknown or CheckState.Attention)
                return [featurePolicy, policySurfaces, restartPolicy, source, sourceReadiness, history, installedHotfixes, .. followups,
                    new("Available updates", CheckState.Unknown, "Update discovery was not started because the selected source could not be verified."), .. thirdParty];
            var updates = await UpdateDiscovery.SearchAsync(UpdateOptions, Progress, token).ConfigureAwait(false);
            return [featurePolicy, policySurfaces, restartPolicy, source, sourceReadiness, history, installedHotfixes, .. followups, new("Available updates", updates.Partial ? CheckState.Unknown : CheckState.Information, updates.Format()), .. thirdParty];
        }
        if (kind == WorkflowKind.Cleanup)
        {
            var preview = CleanupPreview.Inspect(CleanupOptions, token: token);
            return [new("Cleanup preview", preview.Partial ? CheckState.Unknown : CheckState.Information, preview.Format())];
        }
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Live workflows require Windows.");
        if (kind == WorkflowKind.Services)
        {
            var serviceChecks = new List<WorkflowCheck>();
            Progress?.Report("Reading bounded installed service inventory without accounts or executable paths...");
            var serviceInventory = await ServiceInventoryDiagnostics.CheckAsync(token).ConfigureAwait(false);
            serviceChecks.Add(serviceInventory);
            Progress?.Report("Reviewing service startup-mode coherence without changing configuration...");
            serviceChecks.Add(ServiceStartupConfigurationDiagnostics.Check(serviceInventory, token));
            Progress?.Report("Checking registered local service binary presence without retaining paths...");
            serviceChecks.Add(await ServiceBinaryDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Checking service executable path quoting without retaining paths or arguments...");
            serviceChecks.Add(await ServicePathQuotingDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading service logon account classes without retaining account identities...");
            serviceChecks.Add(await ServiceLogonDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Checking registered local service DLL presence without retaining paths...");
            serviceChecks.Add(await ServiceDllDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading privacy-bounded service recovery policy...");
            var transitionTask = ServiceTransitionDiagnostics.CheckAsync(serviceInventory, token);
            var recoveryPolicyTask = ServiceRecoveryPolicyDiagnostics.CheckAsync(serviceInventory, token);
            Progress?.Report("Checking direct service dependency identities and cycles...");
            var dependencyConfigurationTask = ServiceDependencyConfigurationDiagnostics.CheckAsync(serviceInventory, token);
            Progress?.Report("Reading privacy-bounded service trigger configuration...");
            var triggerTask = ServiceTriggerDiagnostics.CheckAsync(serviceInventory, token);
            serviceChecks.Add(await recoveryPolicyTask.ConfigureAwait(false));
            serviceChecks.Add(await dependencyConfigurationTask.ConfigureAwait(false));
            serviceChecks.Add(await triggerTask.ConfigureAwait(false));
            serviceChecks.Add(await transitionTask.ConfigureAwait(false));
            var structuralServiceChecks = serviceChecks.ToArray();
            var currentServiceChecks = await ServiceInspection.ReadAsync(Enum.GetValues<RepairKind>(), token, Progress).ConfigureAwait(false);
            serviceChecks.AddRange(currentServiceChecks);
            Progress?.Report("Reading recent Service Control Manager failures...");
            var serviceHistory = await ServiceEvents.CheckAsync(token).ConfigureAwait(false);
            serviceChecks.Add(serviceHistory);
            Progress?.Report("Correlating service names and unique display names with current installed state...");
            var eventCorrelation = ServiceEventCorrelation.Check(serviceInventory, currentServiceChecks, serviceHistory, token);
            serviceChecks.Add(eventCorrelation);
            Progress?.Report("Summarizing service registration, configuration, current state and failure history...");
            serviceChecks.Add(ServiceOperationalCorrelation.Check(structuralServiceChecks, eventCorrelation, token));
            return serviceChecks.AsReadOnly();
        }
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        WorkflowCheck? selectedMsiActivity = null;
        if (kind == WorkflowKind.Bluetooth)
        {
            Progress?.Report("Reading Bluetooth device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Bluetooth, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Camera)
        {
            Progress?.Report("Reading camera device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Camera, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WindowsImageAcquisitionService)
        {
            Progress?.Report("Reading camera and imaging device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Camera, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind is WorkflowKind.FunctionDiscoveryProviderHostService or WorkflowKind.FunctionDiscoveryResourcePublicationService)
        {
            Progress?.Report("Reading current network profile assignments for discovery context...");
            checks.AddRange(await NetworkProfileDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.HumanInterfaceDeviceService)
        {
            Progress?.Report("Reading keyboard, mouse and HID device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Input, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.PortableDeviceEnumeratorService)
        {
            Progress?.Report("Reading portable device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Portable, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.ShellHardwareDetectionService)
        {
            Progress?.Report("Reading storage-controller and disk errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Storage, KnowledgeCatalogs, token).ConfigureAwait(false));
            Progress?.Report("Reading portable device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Portable, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WindowsFontCacheService)
        {
            Progress?.Report("Checking bounded machine and current-user font registrations...");
            checks.AddRange(await FontRegistrationDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.DeviceAssociationService)
        {
            Progress?.Report("Reading recent Device Setup Manager events...");
            checks.Add(await DeviceSetupEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent Plug and Play configuration events...");
            checks.Add(await KernelPnpEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.TextInputManagementService)
        {
            Progress?.Report("Reading keyboard, mouse and HID device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Input, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.DisplayEnhancementService)
        {
            Progress?.Report("Reading graphics adapter and monitor errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Display, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.SystemNotifications)
        {
            Progress?.Report("Reading recent Service Control Manager failures...");
            checks.Add(await ServiceEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.StorageSpaces)
        {
            Progress?.Report("Reading Storage Spaces pools and virtual disks...");
            var storageSpaces = await StorageSpacesDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(storageSpaces);
            Progress?.Report("Correlating Storage Spaces pool and virtual-disk state...");
            checks.Add(StorageSpacesOperationalCorrelation.Check(storageSpaces[0], storageSpaces[1], token));
            return checks;
        }
        if (kind == WorkflowKind.WindowsActivation)
        {
            Progress?.Report("Reading Windows licensing state without product keys...");
            var licensing = await WindowsLicensingDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(licensing);
            Progress?.Report("Reading sanitized Software Protection event history...");
            var history = await SoftwareProtectionEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(history);
            Progress?.Report("Correlating current licensing and sanitized activation history...");
            checks.Add(WindowsActivationOperationalCorrelation.Check(licensing, history, token));
            return checks;
        }
        if (kind == WorkflowKind.StartupPerformance)
        {
            Progress?.Report("Reading sanitized startup and shutdown performance history...");
            var performanceHistory = await BootPerformanceEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(performanceHistory);
            Progress?.Report("Reading supported pending restart indicators...");
            var startupRestart = RestartDiagnostics.Check();
            checks.Add(startupRestart);
            Progress?.Report("Correlating startup history and restart state...");
            checks.Add(StartupPerformanceOperationalCorrelation.Check(performanceHistory, startupRestart, token));
            return checks;
        }
        if (kind == WorkflowKind.StartupApplications)
        {
            Progress?.Report("Reading startup registration names without command lines...");
            checks.Add(await StartupApplicationDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Windows startup approval state without command lines or timestamps...");
            checks.Add(await StartupApprovalDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading boot and logon scheduled tasks without actions or accounts...");
            checks.Add(await StartupScheduledTaskDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Summarizing the bounded startup environment...");
            checks.Add(StartupOperationalCorrelation.Check(checks, token));
            return checks;
        }
        if (kind == WorkflowKind.SmartCard)
        {
            Progress?.Report("Reading smart-card reader errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.SmartCard, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Biometric)
        {
            Progress?.Report("Reading biometric device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Biometric, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.FileSharing)
        {
            Progress?.Report("Reading SMB server protocol and protection configuration...");
            var smbConfiguration = await SmbServerConfigurationDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(smbConfiguration);
            Progress?.Report("Reading bounded SMB share inventory without paths, permissions or sessions...");
            var smbShares = await SmbShareDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(smbShares);
            Progress?.Report("Reading active network and firewall profile alignment...");
            var networkProfiles = await NetworkProfileDiagnostics.CheckAsync(token).ConfigureAwait(false);
            var firewallProfiles = await FirewallProfiles.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(networkProfiles);
            checks.AddRange(firewallProfiles);
            var firewallAlignment = NetworkFirewallCorrelation.Check(networkProfiles, firewallProfiles, token);
            checks.Add(firewallAlignment);
            Progress?.Report("Correlating SMB configuration, share coverage and active firewall alignment...");
            checks.Add(FileSharingOperationalCorrelation.Check(smbConfiguration, smbShares, firewallAlignment, token));
        }
        if (kind == WorkflowKind.Defender)
        {
            checks.AddRange(await SecurityDiagnostics.CheckAsync(token, SecurityOptions).ConfigureAwait(false));
            Progress?.Report("Reading effective AppLocker collection modes and aggregate rule counts...");
            checks.AddRange(await AppLockerPolicyDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Microsoft Defender protection preferences and recognized policy surfaces...");
            checks.AddRange(await DefenderProtectionDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading aggregate Microsoft Defender threat history and current active flags...");
            checks.Add(await DefenderThreatSummary.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading aggregate Microsoft Defender quick and full scan timing...");
            checks.Add(await DefenderScanHistory.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent Microsoft Defender events...");
            checks.Add(await DefenderEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.CommonProblems)
            checks.AddRange(await CommonDiagnostics.CheckAsync(token, Progress, checkpoint: checkpoint, selectedScopes: selectedScopes,
                knowledgeCatalogs: KnowledgeCatalogs).ConfigureAwait(false));
        if (kind == WorkflowKind.Storage)
        {
            string volume = StorageVolume ?? StorageDiagnostics.SystemVolume();
            Progress?.Report("Mapping disks, partitions and volumes without changing storage...");
            var topology = await StorageTopologyDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(topology);
            Progress?.Report("Reading Storage Spaces pools and virtual disks without changing storage...");
            var storageSpaces = await StorageSpacesDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(storageSpaces);
            Progress?.Report("Evaluating free-space pressure across all drive-letter volumes...");
            checks.Add(StorageVolumeCapacityCorrelation.Check(topology, token));
            checks.Add(VolumeCapacity.Check(volume));
            Progress?.Report("Reading the selected volume's dirty bit without scheduling repair...");
            checks.Add(await VolumeDirtyStatus.CheckAsync(volume, token).ConfigureAwait(false));
            Progress?.Report("Reading the selected volume's NTFS self-healing state...");
            var selfHealing = await NtfsSelfHealingDiagnostics.CheckAsync(volume, token).ConfigureAwait(false);
            checks.Add(selfHealing);
            if (FileSystemRepairSelection is not null)
                checks.AddRange(await FileSystemRepairReviewDiagnostics.CheckAsync(FileSystemRepairSelection,
                    FileSystemVolumeInspection, FileSystemDiagnosis, StorageDiagnostics.SystemVolume(), Progress, token)
                    .ConfigureAwait(false));
            if (VolumeOptimizationRequest is not null)
            {
                Progress?.Report("Reviewing the selected volume optimization without changing storage...");
                var optimization = await VolumeOptimization.ReviewAsync(VolumeOptimizationRequest,
                    new WindowsVolumeOptimizationHost(), Progress, token).ConfigureAwait(false);
                checks.Add(VolumeOptimization.ToWorkflowCheck(optimization));
            }
            Progress?.Report("Reading NTFS and ReFS delete-notification configuration...");
            var deleteNotifications = await DeleteNotificationDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(deleteNotifications);
            Progress?.Report("Reading current paging-file allocation and usage without changing memory settings...");
            var pageFile = await PageFileDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(pageFile);
            Progress?.Report("Reading per-volume disk quota settings without collecting user quota rows...");
            var diskQuotas = await DiskQuotaDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(diskQuotas);
            Progress?.Report("Reading Storage Sense machine policy without changing cleanup settings...");
            var storageSensePolicy = StorageSensePolicyDiagnostics.Check();
            checks.Add(storageSensePolicy);
            Progress?.Report("Reading the exact Storage Sense scheduled task without running it...");
            var storageSenseTask = await StorageSenseTaskDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(storageSenseTask);
            Progress?.Report("Reading the exact Disk Cleanup scheduled task without running it...");
            var diskCleanupTask = await DiskCleanupTaskDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(diskCleanupTask);
            Progress?.Report("Reading and correlating exact Windows disk diagnostic tasks and event history...");
            var diskDiagnostics = await DiskDiagnosticCorrelation.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(diskDiagnostics);
            Progress?.Report("Reading current Storage module jobs without changing or stopping them...");
            checks.Add(await StorageJobDiagnostics.CheckAsync(token).ConfigureAwait(false));
            checks.Add(new("Storage scope", CheckState.Information,
                "Filesystem checks target the selected volume. Disk health and device/controller inventory cover this PC and are not mapped to that volume. A device error does not establish filesystem damage."));
            Progress?.Report("Reading current physical-disk inventory and provider state...");
            var physicalDisks = await PhysicalDiskDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(physicalDisks);
            Progress?.Report("Reading physical-device reliability counters...");
            var reliability = await StorageReliability.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(reliability);
            Progress?.Report("Correlating exact physical-disk and reliability-counter identities...");
            checks.Add(PhysicalDiskCorrelation.Check(physicalDisks, reliability, token));
            Progress?.Report("Reading VSS shadow storage allocation and configured limits...");
            var shadowStorage = await ShadowStorageDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(shadowStorage);
            Progress?.Report("Correlating storage configuration, maintenance tasks and retained disk history...");
            checks.Add(StorageEnvironmentCorrelation.Check(storageSpaces, shadowStorage, selfHealing,
                deleteNotifications, pageFile, diskQuotas, storageSensePolicy, storageSenseTask, diskCleanupTask,
                diskDiagnostics.Single(check => check.Name == "Windows disk diagnostic correlation"), token));
            Progress?.Report("Reading storage device and controller errors across this PC...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Storage, KnowledgeCatalogs, token).ConfigureAwait(false));
            Progress?.Report("Reading recent storage event history...");
            var history = await StorageEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(history);
            Progress?.Report("Reading exact Storage Service and Optimize Drives failure history...");
            checks.AddRange(await StorageServiceFailureEvents.CheckStorageAsync(token).ConfigureAwait(false));
            Progress?.Report("Correlating storage event volume identities with current topology...");
            checks.Add(StorageEventCorrelation.Check(topology, history, token));
        }
        if (kind == WorkflowKind.Vitals)
        {
            Progress?.Report("Reviewing crash history, dump metadata and unexpected shutdowns...");
            var crashChecks = KnowledgeCatalogs is not { Count: > 0 }
                ? await CrashReview.ReadAsync(token).ConfigureAwait(false)
                : await CrashReview.ReadWithKnowledgeAsync(KnowledgeCatalogs, token).ConfigureAwait(false);
            checks.AddRange(crashChecks);
            var vitalChecks = await VitalsDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(vitalChecks);
            Progress?.Report("Reading recent Windows Memory Diagnostic results...");
            var memoryDiagnostic = await MemoryDiagnosticResults.CheckAsync(token).ConfigureAwait(false);
            checks.Add(memoryDiagnostic);
            Progress?.Report("Reading current device problems and recent reliability history...");
            var deviceProblems = await DeviceDiagnostics.CheckAsync(DeviceCategory.Problems, KnowledgeCatalogs, token).ConfigureAwait(false);
            checks.AddRange(deviceProblems);
            var reliability = await ReliabilityHistory.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(reliability);
            Progress?.Report("Reading recent Windows hardware-error history...");
            var hardwareErrors = await HardwareErrorEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(hardwareErrors);
            Progress?.Report("Correlating current vitals with device, reliability, hardware-error, crash and shutdown evidence...");
            var stabilityVitals = vitalChecks.Append(memoryDiagnostic).ToArray();
            var shutdowns = crashChecks.Where(check => check.Name == "Recent unexpected shutdowns").ToArray();
            var crashEvidence = crashChecks.Where(check => check.Name != "Recent unexpected shutdowns").ToArray();
            checks.Add(SystemStabilityOperationalCorrelation.Check(stabilityVitals, deviceProblems, reliability,
                [hardwareErrors], crashEvidence, shutdowns, token));
            Progress?.Report("Reading Windows licensing state without product keys...");
            checks.Add(await WindowsLicensingDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent service failures...");
            checks.Add(await ServiceEvents.CheckAsync(token).ConfigureAwait(false));
            checks.AddRange(await BitLockerDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.FirewallServices)
        {
            Progress?.Report("Reading effective firewall profile configuration...");
            checks.AddRange(await FirewallProfiles.CheckAsync(token).ConfigureAwait(false));
            if (FirewallProfileSelection is not null)
            {
                Progress?.Report("Reviewing the exact selected firewall profiles and policy ownership...");
                var selectedState = await new WindowsFirewallProfileEnableHost().InspectAsync(token).ConfigureAwait(false);
                checks.Add(FirewallProfileEnable.ReviewSelection(FirewallProfileSelection, selectedState));
            }
            Progress?.Report("Reading exact Base Filtering Engine and Windows Defender Firewall service failure history...");
            checks.AddRange(await FirewallServiceFailureEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Printing)
        {
            Progress?.Report("Reading current Print Spooler state without changing the service...");
            var printSpooler = (await ServiceInspection.ReadAsync([RepairKind.PrintSpooler], token).ConfigureAwait(false)).Single();
            checks.Add(printSpooler);
            Progress?.Report("Reading default-printer selection and automatic management without changing queues...");
            var defaultPrinter = await DefaultPrinterDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(defaultPrinter);
            Progress?.Report("Reading Point and Print restrictions and driver-installation policy without changing policy...");
            var pointAndPrint = PrintPolicyDiagnostics.Check();
            checks.Add(pointAndPrint);
            Progress?.Report("Reading print rendering and driver-isolation policy without changing policy...");
            var rendering = PrintRenderingPolicyDiagnostics.Check();
            checks.Add(rendering);
            Progress?.Report("Reading driver-reported local queue condition codes without changing queues...");
            var printerConditions = await PrinterConditionDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerConditions);
            Progress?.Report("Reading local printer queue operating attributes without changing queues...");
            var printerAttributes = await PrinterQueueAttributeDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerAttributes);
            Progress?.Report("Checking print spool directory configuration and storage without reading queued documents...");
            var spoolDirectory = PrintSpoolDirectoryDiagnostics.Check();
            checks.Add(spoolDirectory);
            Progress?.Report("Reading printer device errors and driver inventory...");
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Printing, KnowledgeCatalogs, token).ConfigureAwait(false));
            Progress?.Report("Matching print queues to registered driver names...");
            var printerDrivers = await PrinterDriverDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerDrivers);
            Progress?.Report("Checking registered printer driver files and executable signatures...");
            var printerDriverFiles = await PrinterDriverFileDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerDriverFiles);
            Progress?.Report("Reading printer driver models and package-awareness without changing drivers...");
            var printerDriverModels = await PrinterDriverModelDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(printerDriverModels);
            Progress?.Report("Matching print queues to registered ports...");
            var printerPorts = await PrinterPortDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerPorts);
            Progress?.Report("Mapping local queues to Standard TCP/IP endpoint configuration...");
            var printerEndpoints = await PrinterEndpointDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerEndpoints);
            Progress?.Report("Testing exact assigned printer endpoints with bounded TCP connections...");
            var printerReachability = await PrinterEndpointReachability.CheckAsync(printerEndpoints, token).ConfigureAwait(false);
            checks.AddRange(printerReachability);
            Progress?.Report("Reading print processor and monitor registrations without loading extensions...");
            var printExtensions = await PrintExtensionDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(printExtensions);
            Progress?.Report("Checking registered print processor and monitor binaries and signatures...");
            var printExtensionFiles = await PrintExtensionFileDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printExtensionFiles);
            Progress?.Report("Correlating printing policy, drivers, ports, endpoints and extensions...");
            var printingEnvironment = PrintingEnvironmentCorrelation.Check(pointAndPrint, rendering, printerDrivers,
                printerDriverFiles, printerDriverModels, printerPorts, printerEndpoints, printerReachability,
                printExtensions, printExtensionFiles, token);
            checks.Add(printingEnvironment);
            Progress?.Report("Reading visible print job states...");
            ToolResult printJobEvidence = await PrintJobDiagnostics.CaptureAsync(token).ConfigureAwait(false);
            var printJobs = PrintJobDiagnostics.Interpret(printJobEvidence);
            checks.AddRange(printJobs);
            if (PrintingTargetSelection is not null)
            {
                Progress?.Report("Reviewing the exact selected printer queue or job without changing it...");
                checks.AddRange(PrintingTargetReview.Check(PrintingTargetSelection, printerConditions,
                    printerAttributes, PrintJobDiagnostics.ReadInventory(printJobEvidence)));
            }
            Progress?.Report("Reading recent print-service history...");
            var printHistory = await PrintServiceEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(printHistory);
            Progress?.Report("Checking PrintService Operational logging and recent job or rendering history...");
            checks.Add(await PrintServiceOperationalEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Service Control Manager failures tied to the local Print Spooler identity...");
            var printSpoolerHistory = await PrintSpoolerFailureEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(printSpoolerHistory);
            Progress?.Report("Correlating print-service failures with exact current queue identities...");
            checks.Add(PrintQueueEventCorrelation.Check(printerDrivers, printHistory, token));
            Progress?.Report("Correlating current printing operation with exact service history...");
            checks.Add(PrintingOperationalCorrelation.Check(printSpooler, defaultPrinter, printerConditions,
                printerAttributes, printJobs, spoolDirectory, printingEnvironment, printSpoolerHistory, token));
        }
        if (kind == WorkflowKind.EventLog)
        {
            Progress?.Report("Inspecting event channel state, retention, size and saturation without reading event content...");
            var eventLogCapacity = await EventLogCapacityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(eventLogCapacity);
            Progress?.Report("Reading exact Windows Event Log service failure history...");
            var eventLogHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.EventLog, token).ConfigureAwait(false);
            checks.AddRange(eventLogHistory);
            Progress?.Report("Correlating channel capacity with exact Windows Event Log service history...");
            checks.Add(EventLogOperationalCorrelation.Check(eventLogCapacity, eventLogHistory.Single(), token));
            if (!string.IsNullOrWhiteSpace(EventChannel))
            {
                Progress?.Report("Reviewing the exact selected event channel without changing it...");
                checks.Add(await EventChannelEnable.ReviewAsync(EventChannel, new WindowsEventChannelEnableHost(), token).ConfigureAwait(false));
            }
        }
        if (kind == WorkflowKind.TimeService)
        {
            Progress?.Report("Inspecting Windows Time source configuration...");
            var timePreview = await TimePlanning.PreviewAsync(TimeOptions, token).ConfigureAwait(false);
            var timeConfiguration = TimeConfigurationDiagnostics.ReviewSelection(
                TimeOptions, timePreview.Environment, timePreview.Plan);
            checks.Add(timeConfiguration);
            Progress?.Report("Reading recent Windows Time history...");
            var timeEvents = await TimeServiceEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(timeEvents);
            Progress?.Report("Reading exact Windows Time service failure history...");
            var timeServiceHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Time, token).ConfigureAwait(false);
            checks.AddRange(timeServiceHistory);
            Progress?.Report("Correlating Windows Time configuration and exact operational histories...");
            checks.Add(TimeOperationalCorrelation.Check(timeConfiguration, timeEvents, timeServiceHistory.Single(), token));
        }
        if (kind == WorkflowKind.Installer)
        {
            LastMsiProduct = null;
            var installerReadiness = InstallerDiagnostics.Check();
            checks.Add(installerReadiness);
            Progress?.Report("Summarizing pending file rename, replacement and deletion operations without retaining paths...");
            var pendingOperations = PendingFileOperationDiagnostics.Check();
            checks.Add(pendingOperations);
            Progress?.Report("Checking required Windows Installer engine files and Authenticode status...");
            checks.Add(await InstallerEngineFileDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Checking Windows Installer service registration without changing it...");
            checks.Add(InstallerServiceRegistrationDiagnostics.Check());
            Progress?.Report("Reading Windows Installer security and patch policy without changing policy...");
            var installerSecurityPolicy = InstallerSecurityPolicyDiagnostics.Check();
            checks.Add(installerSecurityPolicy);
            Progress?.Report("Reading Windows Installer logging and debug policy without changing policy...");
            var installerLoggingPolicy = InstallerLoggingDiagnostics.Check();
            checks.Add(installerLoggingPolicy);
            Progress?.Report("Correlating Windows Installer processes and transaction markers...");
            var installerActivity = await InstallerActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            selectedMsiActivity = installerActivity;
            checks.Add(installerActivity);
            Progress?.Report("Correlating Installer activity with pending file operations...");
            checks.Add(InstallerPendingOperationCorrelation.Check(pendingOperations, installerActivity, token));
            Progress?.Report("Checking registered Windows Installer cache visibility without opening packages...");
            var installerCache = await MsiCacheDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(installerCache);
            Progress?.Report("Reviewing registered Windows Installer source metadata without reading source locations...");
            var installerSources = await MsiSourceDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(installerSources);
            Progress?.Report("Reading recent Windows Installer events...");
            var installerHistory = await InstallerEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(installerHistory);
            Progress?.Report("Correlating later Installer outcomes for exact product identities...");
            var installerOutcome = InstallerOutcomeCorrelation.Check(installerHistory, token);
            checks.Add(installerOutcome);
            Progress?.Report("Reading recent AppX deployment and Microsoft Store installation history...");
            var appDeploymentHistory = await AppDeploymentEvents.CheckAsync(token).ConfigureAwait(false);
            var storeInstallHistory = await StoreInstallEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(appDeploymentHistory);
            checks.Add(storeInstallHistory);
            Progress?.Report("Correlating installation policy, cache, source and retained deployment history...");
            checks.Add(InstallationEnvironmentCorrelation.Check(installerReadiness, installerSecurityPolicy,
                installerLoggingPolicy, installerCache, installerSources, installerOutcome,
                appDeploymentHistory, storeInstallHistory, token));
            Progress?.Report("Reading Service Control Manager failures tied to the local Windows Installer identity...");
            checks.Add(await WindowsInstallerFailureEvents.CheckAsync(token).ConfigureAwait(false));
            if (MsiProductCode is not null)
            {
                Progress?.Report("Reading the selected MSI product registration state...");
                LastMsiProduct = await MsiProductDiagnostics.CheckAsync(MsiProductCode, token).ConfigureAwait(false);
                checks.Add(LastMsiProduct);
            }
        }
        if (kind == WorkflowKind.Wmi)
        {
            Progress?.Report("Reading recent WMI activity events...");
            var wmiActivity = await WmiActivityEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(wmiActivity);
            Progress?.Report("Reading exact WMI service failure history...");
            var wmiHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Wmi, token).ConfigureAwait(false);
            checks.AddRange(wmiHistory);
            Progress?.Report("Correlating WMI activity and exact service history...");
            checks.Add(WmiHistoricalCorrelation.Check(wmiActivity, wmiHistory.Single(), token));
        }
        if (kind == WorkflowKind.Search)
        {
            Progress?.Report("Reading live Windows Search catalog status...");
            var searchCatalog = await SearchCatalog.CheckAsync(token).ConfigureAwait(false);
            checks.Add(searchCatalog);
            Progress?.Report("Reading recent Windows Search events...");
            var searchEvents = await SearchEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(searchEvents);
            Progress?.Report("Reading exact Windows Search service failure history...");
            var searchHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Search, token).ConfigureAwait(false);
            checks.AddRange(searchHistory);
            Progress?.Report("Correlating Search catalog activity and exact event history...");
            checks.Add(SearchOperationalCorrelation.Check(searchCatalog, searchEvents, searchHistory.Single(), token));
        }
        if (kind is WorkflowKind.AppReadinessService or WorkflowKind.StateRepositoryService or WorkflowKind.AppxDeploymentService)
        {
            Progress?.Report("Reading recent AppX deployment events...");
            checks.Add(await AppDeploymentEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind is WorkflowKind.ClientLicenseService or WorkflowKind.StoreInstallService or
            WorkflowKind.WindowsLicenseManagerService or WorkflowKind.PushToInstallService)
        {
            Progress?.Report("Reading recent Microsoft Store installation events...");
            checks.Add(await StoreInstallEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.OptimizeDrivesService)
        {
            Progress?.Report("Reading the exact ScheduledDefrag task state and last result...");
            checks.Add(await OptimizeDrivesDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.UserProfileService)
        {
            Progress?.Report("Reading recent User Profile Service events...");
            checks.Add(await UserProfileEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind is WorkflowKind.WebAccountManagerService or WorkflowKind.MicrosoftAccountSignInService)
        {
            Progress?.Report("Reading payload-sanitized account broker events...");
            checks.Add(await AccountBrokerEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.DeviceInstallService)
        {
            Progress?.Report("Reading recent Device Setup Manager events...");
            checks.Add(await DeviceSetupEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent Plug and Play configuration events...");
            checks.Add(await KernelPnpEvents.CheckAsync(token).ConfigureAwait(false));
        }
        PlatformServiceFailureArea? platformServiceHistory = kind switch
        {
            WorkflowKind.AppReadinessService => PlatformServiceFailureArea.AppReadiness,
            WorkflowKind.StateRepositoryService => PlatformServiceFailureArea.StateRepository,
            WorkflowKind.UserProfileService => PlatformServiceFailureArea.UserProfile,
            WorkflowKind.CredentialManagerService => PlatformServiceFailureArea.CredentialManager,
            WorkflowKind.ShellHardwareDetectionService => PlatformServiceFailureArea.ShellHardwareDetection,
            WorkflowKind.DeviceInstallService => PlatformServiceFailureArea.DeviceInstall,
            WorkflowKind.AppxDeploymentService => PlatformServiceFailureArea.AppxDeployment,
            WorkflowKind.ClientLicenseService => PlatformServiceFailureArea.ClientLicense,
            WorkflowKind.StoreInstallService => PlatformServiceFailureArea.StoreInstall,
            WorkflowKind.WindowsLicenseManagerService => PlatformServiceFailureArea.WindowsLicenseManager,
            WorkflowKind.PushToInstallService => PlatformServiceFailureArea.PushToInstall,
            WorkflowKind.WebAccountManagerService => PlatformServiceFailureArea.WebAccountManager,
            WorkflowKind.MicrosoftAccountSignInService => PlatformServiceFailureArea.MicrosoftAccountSignIn,
            WorkflowKind.OptimizeDrivesService => PlatformServiceFailureArea.OptimizeDrives,
            WorkflowKind.WindowsImageAcquisitionService => PlatformServiceFailureArea.WindowsImageAcquisition,
            WorkflowKind.FunctionDiscoveryProviderHostService => PlatformServiceFailureArea.FunctionDiscoveryProviderHost,
            WorkflowKind.FunctionDiscoveryResourcePublicationService => PlatformServiceFailureArea.FunctionDiscoveryResourcePublication,
            WorkflowKind.HumanInterfaceDeviceService => PlatformServiceFailureArea.HumanInterfaceDevice,
            WorkflowKind.PortableDeviceEnumeratorService => PlatformServiceFailureArea.PortableDeviceEnumerator,
            WorkflowKind.WindowsFontCacheService => PlatformServiceFailureArea.WindowsFontCache,
            WorkflowKind.DeviceAssociationService => PlatformServiceFailureArea.DeviceAssociation,
            WorkflowKind.TextInputManagementService => PlatformServiceFailureArea.TextInputManagement,
            WorkflowKind.DisplayEnhancementService => PlatformServiceFailureArea.DisplayEnhancement,
            _ => null
        };
        if (platformServiceHistory is { } platformArea)
        {
            Progress?.Report("Reading exact service failure history for the selected Windows feature...");
            checks.AddRange(await PlatformServiceFailureEvents.CheckAsync(platformArea, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WindowsErrorReportingService)
        {
            Progress?.Report("Reading crash-dump configuration without changing it...");
            checks.Add(CrashDumpConfiguration.Check(token));
            Progress?.Report("Reading bounded crash-dump file metadata without opening dump contents...");
            var dumpInventory = await CrashDumpInventory.ReadAsync(token).ConfigureAwait(false);
            checks.Add(CrashDumpInventory.Interpret(dumpInventory));
            Progress?.Report("Reading recent system crash-report history...");
            checks.Add(await CrashEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.TaskSchedulerService)
        {
            Progress?.Report("Reading scheduled task identities, runtime results and missed-run counts...");
            var inventory = await ScheduledTaskInventory.ReadAsync(token).ConfigureAwait(false);
            var taskChecks = new List<WorkflowCheck>
            {
                new("Task inventory coverage", inventory.Complete ? CheckState.Information : CheckState.Unknown,
                    $"{inventory.Tasks.Count} visible tasks collected. " + (inventory.Complete ? "Provider collection completed. " : "Collection incomplete. ") +
                    "Inaccessible tasks, action safety, triggers and execution success are not verified. No tasks changed.", Evidence: inventory.Evidence)
            };
            taskChecks.AddRange(inventory.Tasks.Select(task => ScheduledTaskDiagnostics.Assess(task)));
            checks.AddRange(taskChecks);
            Progress?.Report("Reading exact Task Scheduler service failure history...");
            var schedulerHistory = await InfrastructureServiceFailureEvents.CheckAsync(
                InfrastructureServiceFailureArea.TaskScheduler, token).ConfigureAwait(false);
            checks.AddRange(schedulerHistory);
            Progress?.Report("Correlating task runtime results with exact service history...");
            checks.Add(ScheduledTaskOperationalCorrelation.Check(taskChecks, schedulerHistory, token));
        }
        InfrastructureServiceFailureArea[]? infrastructureServiceHistory = kind switch
        {
            WorkflowKind.Bluetooth => [InfrastructureServiceFailureArea.Bluetooth],
            WorkflowKind.Camera => [InfrastructureServiceFailureArea.Camera],
            WorkflowKind.SmartCard => [InfrastructureServiceFailureArea.SmartCard],
            WorkflowKind.Biometric => [InfrastructureServiceFailureArea.Biometric],
            WorkflowKind.FileSharing => [InfrastructureServiceFailureArea.FileSharing],
            WorkflowKind.DiagnosticPolicyService => [InfrastructureServiceFailureArea.DiagnosticPolicy],
            WorkflowKind.WindowsErrorReportingService => [InfrastructureServiceFailureArea.WindowsErrorReporting],
            WorkflowKind.WindowsEventCollectorService => [InfrastructureServiceFailureArea.WindowsEventCollector],
            WorkflowKind.RemoteAccessConnectionManagerService => [InfrastructureServiceFailureArea.RemoteAccessConnectionManager],
            WorkflowKind.RemoteDesktopServices => [InfrastructureServiceFailureArea.RemoteDesktop],
            WorkflowKind.WindowsRemoteManagement => [InfrastructureServiceFailureArea.WindowsRemoteManagement],
            WorkflowKind.SecurityCenterService => [InfrastructureServiceFailureArea.SecurityCenter],
            WorkflowKind.NetworkConnectionsService => [InfrastructureServiceFailureArea.NetworkConnections],
            WorkflowKind.WinHttpAutoProxyService => [InfrastructureServiceFailureArea.WinHttpAutoProxy],
            WorkflowKind.WindowsConnectionManagerService => [InfrastructureServiceFailureArea.WindowsConnectionManager],
            WorkflowKind.NetworkSetupService => [InfrastructureServiceFailureArea.NetworkSetup],
            WorkflowKind.SystemNotifications => [InfrastructureServiceFailureArea.ComPlusEventSystem, InfrastructureServiceFailureArea.SystemEventNotification],
            WorkflowKind.CoreWindowsServices =>
            [
                InfrastructureServiceFailureArea.Power,
                InfrastructureServiceFailureArea.PlugAndPlay,
                InfrastructureServiceFailureArea.UserManager,
                InfrastructureServiceFailureArea.GroupPolicyClient,
                InfrastructureServiceFailureArea.NetworkStoreInterface,
                InfrastructureServiceFailureArea.RpcEndpointMapper,
                InfrastructureServiceFailureArea.RemoteProcedureCall,
                InfrastructureServiceFailureArea.DcomServerProcessLauncher,
                InfrastructureServiceFailureArea.BackgroundTasksInfrastructure,
                InfrastructureServiceFailureArea.CoreMessaging
            ],
            _ => null
        };
        if (infrastructureServiceHistory is not null)
        {
            Progress?.Report("Reading exact service failure history for the selected Windows infrastructure...");
            checks.AddRange(await InfrastructureServiceFailureEvents.CheckManyAsync(infrastructureServiceHistory, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Audio)
        {
            var audioDevices = await AudioDeviceDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(audioDevices);
            Progress?.Report("Reading default audio input and output endpoints...");
            var audioEndpoints = await DefaultAudioEndpoints.CheckAsync(AudioEndpointSelection, token).ConfigureAwait(false);
            checks.AddRange(audioEndpoints);
            Progress?.Report("Reading exact Windows audio service failure history...");
            var audioHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Audio, token).ConfigureAwait(false);
            checks.AddRange(audioHistory);
            Progress?.Report("Correlating audio devices, default endpoints and exact service history...");
            checks.Add(AudioOperationalCorrelation.Check(audioDevices, audioEndpoints, audioHistory, token));
        }
        if (kind is WorkflowKind.Wireless or WorkflowKind.WiredAuthentication)
        {
            checks.Add(new("Network authentication scope", CheckState.Information,
                kind == WorkflowKind.Wireless
                    ? "Selected WLAN service recovery only. Disabled or absent services remain unchanged. An idle service can be intentional; enable auto-repair only when wireless service availability is needed. Profiles and credentials are not collected or changed."
                    : "Selected wired 802.1X service recovery only. Ordinary Ethernet does not require this service. Disabled or absent services remain unchanged. Enable auto-repair only when wired authentication service availability is needed. Certificates, profiles and credentials are not collected or changed."));
            checks.AddRange(NetworkSnapshot.Check(token));
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Network, KnowledgeCatalogs, token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Wireless)
        {
            Progress?.Report("Reading recent wireless connection events...");
            checks.Add(await WirelessEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WiredAuthentication)
        {
            Progress?.Report("Reading recent wired authentication events...");
            checks.Add(await WiredAuthenticationEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Network)
        {
            checks.AddRange(NetworkSnapshot.Check(token));
            Progress?.Report("Reading current IP address state and origin without changing adapters...");
            var ipAddresses = await IpAddressDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(ipAddresses);
            checks.AddRange(await DeviceDiagnostics.CheckAsync(DeviceCategory.Network, KnowledgeCatalogs, token).ConfigureAwait(false));
            Progress?.Report("Reading active network profile assignments...");
            var networkProfiles = await NetworkProfileDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(networkProfiles);
            Progress?.Report("Reading Windows connectivity classification policy without retaining probe destinations...");
            checks.Add(await NcsiPolicyDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading matching Windows firewall profile configuration...");
            var firewallProfiles = await FirewallProfiles.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(firewallProfiles);
            Progress?.Report("Reading Windows Firewall logging configuration without opening log contents...");
            checks.AddRange(await FirewallLoggingDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Summarizing effective Windows Firewall rules without collecting rule identities or endpoints...");
            checks.Add(await FirewallRuleSummaryDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent Windows Firewall warning and failure history...");
            checks.Add(await FirewallEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading exact firewall service failure history...");
            checks.AddRange(await FirewallServiceFailureEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading exact network service failure history...");
            checks.AddRange(await NetworkServiceFailureEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Correlating active network assignments with firewall profile enablement...");
            var firewallCorrelation = NetworkFirewallCorrelation.Check(networkProfiles, firewallProfiles, token);
            checks.Add(firewallCorrelation);
            Progress?.Report("Reading current DNS Client service state without starting or changing it...");
            var dnsService = (await ServiceInspection.ReadAsync([RepairKind.DnsClient], token, Progress).ConfigureAwait(false)).Single();
            checks.Add(dnsService);
            Progress?.Report("Reading per-interface DNS server configuration without changing adapters...");
            var dnsServers = await DnsServerDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(dnsServers);
            Progress?.Report("Reading DNS suffix, registration and policy configuration without changing adapters...");
            checks.Add(await DnsClientConfigurationDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading effective DNS namespace policy without changing DNS or network settings...");
            var dnsPolicy = await DnsNrptPolicyDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(dnsPolicy);
            Progress?.Report("Reading privacy-bounded DNS client cache aggregates without retaining names or record data...");
            checks.Add(await DnsClientCacheDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent DNS Client warning and failure history without changing resolver state...");
            var dnsEvents = await DnsClientEvents.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(dnsEvents);
            Progress?.Report("Reading the current IP routing table without changing routes...");
            var routes = await RouteDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(routes);
            Progress?.Report("Comparing default-route and interface metrics without changing priority...");
            checks.Add(await RoutePriorityDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading per-interface MTU, DHCP, forwarding, discovery and metric configuration...");
            checks.Add(await IpInterfaceConfigurationDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading current DHCP Client service state without starting or changing it...");
            var dhcpService = (await ServiceInspection.ReadAsync([RepairKind.DhcpClient], token, Progress).ConfigureAwait(false)).Single();
            checks.Add(dhcpService);
            Progress?.Report("Reading DHCP lease timestamps, expiry and server evidence without renewing leases...");
            var dhcpLease = await DhcpLeaseDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(dhcpLease);
            Progress?.Report("Reading recent IPv4 and IPv6 DHCP Client events without changing network state...");
            var dhcpEvents = await DhcpClientEvents.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(dhcpEvents);
            Progress?.Report("Correlating current DHCP readiness with recent client history...");
            var dhcpCorrelation = DhcpOperationalCorrelation.Check(dhcpService, dhcpLease, ipAddresses, dhcpEvents, token);
            checks.Add(dhcpCorrelation);
            Progress?.Report("Reading privacy-bounded neighbor reachability states without changing the cache...");
            checks.Add(await NeighborCacheDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading core network adapter protocol bindings without changing adapters...");
            checks.Add(await AdapterBindingDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading network adapter error and discard counters without changing adapters...");
            checks.Add(await AdapterStatisticsDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading aggregate dynamic TCP port pressure without retaining endpoints or processes...");
            checks.AddRange(await TcpPortPressureDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading bounded Windows TCP template settings without changing the network stack...");
            checks.Add(await TcpSettingDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading global Windows network offload settings without changing the network stack...");
            checks.Add(await NetworkOffloadDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading the native Winsock protocol catalog without changing providers...");
            var winsockProtocols = await WinsockCatalogDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(winsockProtocols);
            Progress?.Report("Reading native Winsock namespace-provider registrations without changing providers...");
            var winsockNamespaces = await WinsockNamespaceDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(winsockNamespaces);
            Progress?.Report("Checking registered Winsock protocol provider files without loading providers...");
            var winsockProviderFiles = await WinsockProviderFileDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(winsockProviderFiles);
            Progress?.Report("Correlating Winsock protocol, namespace-provider and provider-file readiness...");
            var winsockCorrelation = WinsockReadinessCorrelation.Check(winsockProtocols, winsockNamespaces, winsockProviderFiles, token);
            checks.Add(winsockCorrelation);
            Progress?.Report("Correlating default routes with per-interface DNS configuration...");
            var pathReadiness = NetworkPathCorrelation.Check(routes, dnsServers, token);
            checks.Add(pathReadiness);
            Progress?.Report("Correlating current DNS readiness with recent client history...");
            var dnsCorrelation = DnsOperationalCorrelation.Check(dnsService, dnsServers, dnsPolicy, dnsEvents, pathReadiness, token);
            checks.Add(dnsCorrelation);
            Progress?.Report("Testing only the exact default gateway addresses reported by Windows...");
            var gateways = await GatewayReachabilityDiagnostics.CheckAsync(routes, GatewayProbe, token).ConfigureAwait(false);
            checks.Add(gateways);
            Progress?.Report("Correlating Windows connectivity labels with routes and gateway evidence...");
            var connectivityCorrelation = NetworkConnectivityCorrelation.Check(networkProfiles, routes, gateways, token);
            checks.Add(connectivityCorrelation);
            Progress?.Report("Building the top-level network operational result...");
            checks.Add(NetworkOperationalCorrelation.Check(firewallCorrelation, pathReadiness, connectivityCorrelation,
                winsockCorrelation, dnsCorrelation, dhcpCorrelation, token));
            Progress?.Report("Auditing Hosts file mappings without retaining comments...");
            checks.Add(await HostsFileDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading stored WinHTTP and current-account Internet proxy settings...");
            checks.AddRange(ProxyDiagnostics.Check());
            Progress?.Report("Reading SMB client signing, guest-access and transport configuration...");
            checks.Add(await SmbClientConfigurationDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading active SMB protocol negotiation without retaining remote identities...");
            checks.Add(await SmbConnectionDiagnostics.CheckAsync(token).ConfigureAwait(false));
            WorkflowCheck? targetLookup = null;
            WorkflowCheck? targetTcp = null;
            if (NetworkLookupHost is not null)
            {
                Progress?.Report("Resolving " + NetworkLookupHost + " with the configured system resolver...");
                LastDnsLookup = await DnsLookup(NetworkLookupHost, NetworkTimeout, token).WaitAsync(NetworkTimeout, token).ConfigureAwait(false);
                var lookup = DnsDiagnostics.ToWorkflowCheck(LastDnsLookup);
                if (DnsCacheRecovery.IsEligible(LastDnsLookup))
                    lookup = lookup with { State = CheckState.Repairable, Repair = RepairKind.DnsCache,
                        Detail = lookup.Detail + " A guarded local DNS cache recovery is eligible for this exact hostname." };
                checks.Add(lookup);
                targetLookup = lookup;
            }
            if (NetworkEndpoint is { } endpoint)
            {
                Progress?.Report($"Checking TCP connection to {endpoint.Host}:{endpoint.Port}...");
                targetTcp = TcpDiagnostics.ToWorkflowCheck(await TcpDiagnostics.CheckAsync(endpoint.Host, endpoint.Port, NetworkTimeout, token).ConfigureAwait(false));
                checks.Add(targetTcp);
            }
            if (targetLookup is not null || targetTcp is not null)
            {
                Progress?.Report("Building a layered diagnosis for the selected network target...");
                checks.Add(NetworkTargetDiagnosis.Check(pathReadiness, gateways, targetLookup, targetTcp, token));
            }
            bool networkAdministrator = WindowsState.IsAdministrator();
            if (!networkAdministrator)
            {
                Progress?.Report("Reading DHCP service and lease state without elevation...");
                checks.AddRange(await ServiceInspection.ReadAsync(Workflows.Scopes(kind).Where(scope => scope is not (RepairKind.DnsClient or RepairKind.DhcpClient)), ServiceRecovery.CheckAsync, token, Progress).ConfigureAwait(false));
                var lease = await DhcpRecovery.CheckAsync(token).ConfigureAwait(false);
                checks.Add(lease with
                {
                    Repair = null,
                    State = lease.State == CheckState.Repairable ? CheckState.Attention : lease.State,
                    Detail = lease.Detail + " Read-only inspection; elevate to run a recovery workflow. No lease was renewed."
                });
            }
            checks.Add(new("Administrator", networkAdministrator ? CheckState.Clear : CheckState.Attention, "Network recovery requires administrator access. Read-only evidence is retained without elevation."));
            checks.Add(RestartDiagnostics.Check());
            return checks;
        }
        if (kind == WorkflowKind.BackupServices)
        {
            Progress?.Report("Reading Windows Backup operation summary...");
            checks.Add(await WindowsBackupSummary.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading current File History protection state...");
            checks.Add(await FileHistoryStatus.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent File History warnings and errors...");
            checks.Add(await FileHistoryEvents.CheckAsync(token).ConfigureAwait(false));
            checks.AddRange(await BackupEvents.CheckAsync(token, Progress).ConfigureAwait(false));
            Progress?.Report("Reading VSS writer status...");
            checks.Add(await VssWriters.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading VSS provider registrations...");
            checks.Add(await VssProviderDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading exact shadow-copy and backup service failure history...");
            checks.AddRange(await BackupServiceFailureEvents.CheckAllAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading current VSS shadow copy inventory...");
            checks.Add(await ShadowCopyDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading VSS shadow storage allocation and configured limits...");
            checks.Add(await ShadowStorageDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WindowsEventCollectorService)
        {
            Progress?.Report("Reading Windows Event Collector subscriptions...");
            checks.Add(EventCollectorSubscriptions.Describe(await EventCollectorSubscriptions.ReadAsync(token).ConfigureAwait(false)));
        }
        if (kind == WorkflowKind.RemoteAccessConnectionManagerService)
        {
            Progress?.Report("Reading current-account VPN profiles...");
            checks.AddRange(VpnProfileDiagnostics.Describe(await VpnProfileDiagnostics.ReadAsync(token).ConfigureAwait(false)));
        }
        if (kind == WorkflowKind.RemoteDesktopServices)
        {
            Progress?.Report("Reading Remote Desktop enablement, NLA, listener port and machine policy...");
            checks.AddRange(await RemoteDesktopDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent Remote Desktop connection and session events...");
            checks.AddRange(await RemoteDesktopEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WindowsRemoteManagement)
        {
            Progress?.Report("Reading Windows Remote Management listener registrations...");
            checks.AddRange(WinRmListeners.Describe(await WinRmListeners.ReadAsync(token).ConfigureAwait(false)));
            Progress?.Report("Reading recent Windows Remote Management events...");
            checks.Add(await WinRmEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.DiagnosticPolicyService)
        {
            Progress?.Report("Reading recent Diagnostic Policy events...");
            checks.Add(await DiagnosticPolicyEvents.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.SecurityCenterService)
        {
            Progress?.Report("Reading Windows Security Center product registrations...");
            checks.AddRange(SecurityProductDiagnostics.Describe(await SecurityProductDiagnostics.ReadAsync(token).ConfigureAwait(false)));
        }
        if (kind == WorkflowKind.NetworkConnectionsService)
        {
            Progress?.Report("Reading current network profile assignments...");
            checks.AddRange(await NetworkProfileDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.WinHttpAutoProxyService)
        {
            Progress?.Report("Reading stored WinHTTP and current-account Internet proxy settings...");
            checks.AddRange(ProxyDiagnostics.Check());
        }
        if (kind == WorkflowKind.WindowsConnectionManagerService)
        {
            Progress?.Report("Reading current network profile assignments...");
            checks.AddRange(await NetworkProfileDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.NetworkSetupService)
        {
            Progress?.Report("Reading current network adapter protocol bindings...");
            checks.Add(await AdapterBindingDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.StorageService)
        {
            Progress?.Report("Reading current Storage module jobs without changing or stopping them...");
            checks.Add(await StorageJobDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.StorageService)
        {
            Progress?.Report("Reading exact Storage Service failure history...");
            var storageHistory = await StorageServiceFailureEvents.CheckStorageAsync(token).ConfigureAwait(false);
            checks.Add(storageHistory.Single(check => check.Name == "Storage Service failure history"));
        }
        if (kind == WorkflowKind.WindowsUpdateService)
        {
            Progress?.Report("Reading exact Windows Update Service state...");
            var focusedUpdateService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["wuauserv"], token).ConfigureAwait(false)).Single();
            checks.Add(focusedUpdateService);
            Progress?.Report("Checking the registered Windows Update service DLL and signature...");
            var focusedUpdateRegistration = await ServiceDllDiagnostics.CheckSelectedAsync(
                "Windows Update Service registration", ["wuauserv"], token).ConfigureAwait(false);
            checks.Add(focusedUpdateRegistration);
            Progress?.Report("Reading bounded current Windows Update activity...");
            var focusedUpdateActivity = await WindowsUpdateActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(focusedUpdateActivity);
            Progress?.Report("Reading exact Windows Update Service failure history...");
            var focusedUpdateHistory = await WindowsUpdateServiceFailureEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(focusedUpdateHistory);
            Progress?.Report("Correlating Windows Update Service state, registration, activity and history...");
            checks.Add(WindowsUpdateServiceOperationalCorrelation.Check(
                focusedUpdateService, focusedUpdateRegistration, focusedUpdateActivity, focusedUpdateHistory, token));
        }
        if (kind == WorkflowKind.BitsService)
        {
            LastBitsInventory = null;
            Progress?.Report("Reading current-account BITS transfer inventory...");
            LastBitsInventory = await BitsDiagnostics.ReadAsync(false, token).ConfigureAwait(false);
            var bitsJobs = BitsDiagnostics.Interpret(LastBitsInventory.Evidence, token);
            checks.Add(bitsJobs);
            Progress?.Report("Reading exact BITS service state...");
            var bitsService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["BITS"], token).ConfigureAwait(false)).Single();
            checks.Add(bitsService);
            Progress?.Report("Reading exact BITS service-failure history...");
            var bitsHistory = await ServicingServiceFailureEvents.CheckBitsAsync(token).ConfigureAwait(false);
            checks.Add(bitsHistory);
            Progress?.Report("Correlating BITS jobs, service state and failure history...");
            checks.Add(BitsOperationalCorrelation.Check(bitsJobs, bitsService, bitsHistory, token));
        }
        if (kind == WorkflowKind.CryptographicService)
        {
            Progress?.Report("Reading exact Cryptographic Services state...");
            var cryptographicService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["CryptSvc"], token).ConfigureAwait(false)).Single();
            checks.Add(cryptographicService);
            Progress?.Report("Checking the registered Cryptographic Services DLL and signature...");
            var cryptographicRegistration = await ServiceDllDiagnostics.CheckSelectedAsync(
                "Cryptographic Services registration", ["CryptSvc"], token).ConfigureAwait(false);
            checks.Add(cryptographicRegistration);
            Progress?.Report("Reading exact Cryptographic Services failure history...");
            var cryptographicHistory = await ServicingServiceFailureEvents.CheckFocusedAsync(kind, token).ConfigureAwait(false);
            checks.Add(cryptographicHistory);
            Progress?.Report("Correlating Cryptographic Services state, registration and history...");
            checks.Add(CryptographicServiceOperationalCorrelation.Check(
                cryptographicService, cryptographicRegistration, cryptographicHistory, token));
        }
        if (kind == WorkflowKind.ModulesInstallerService)
        {
            Progress?.Report("Reading exact Windows Modules Installer state...");
            var modulesInstallerService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["TrustedInstaller"], token).ConfigureAwait(false)).Single();
            checks.Add(modulesInstallerService);
            Progress?.Report("Reading bounded servicing package transitions...");
            var servicingPackages = await ServicingPackageDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(servicingPackages);
            Progress?.Report("Reading exact Windows Modules Installer failure history...");
            var modulesInstallerHistory = await ServicingServiceFailureEvents.CheckFocusedAsync(kind, token).ConfigureAwait(false);
            checks.Add(modulesInstallerHistory);
            Progress?.Report("Correlating Windows Modules Installer state, packages and history...");
            checks.Add(ModulesInstallerOperationalCorrelation.Check(
                modulesInstallerService, servicingPackages, modulesInstallerHistory, token));
        }
        if (kind == WorkflowKind.WindowsUpdateMedicService)
        {
            Progress?.Report("Reading exact Windows Update Medic Service state...");
            var medicService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["WaaSMedicSvc"], token).ConfigureAwait(false)).Single();
            checks.Add(medicService);
            Progress?.Report("Checking the registered Windows Update Medic service DLL and signature...");
            var medicRegistration = await ServiceDllDiagnostics.CheckSelectedAsync(
                "Windows Update Medic registration", ["WaaSMedicSvc"], token).ConfigureAwait(false);
            checks.Add(medicRegistration);
            Progress?.Report("Reading bounded current Windows Update activity...");
            var medicActivity = await WindowsUpdateActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(medicActivity);
            Progress?.Report("Reading exact Windows Update Medic failure history...");
            var medicHistory = await ServicingServiceFailureEvents.CheckMedicAsync(token).ConfigureAwait(false);
            checks.Add(medicHistory);
            Progress?.Report("Correlating Windows Update Medic state, registration, activity and history...");
            checks.Add(WindowsUpdateMedicOperationalCorrelation.Check(
                medicService, medicRegistration, medicActivity, medicHistory, token));
        }
        if (kind == WorkflowKind.UpdateOrchestratorService)
        {
            Progress?.Report("Reading exact Update Orchestrator Service state...");
            var orchestratorService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["UsoSvc"], token).ConfigureAwait(false)).Single();
            checks.Add(orchestratorService);
            Progress?.Report("Reading the exact built-in Windows Update scheduled task...");
            var updateTask = await WindowsUpdateScheduledTaskDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(updateTask);
            Progress?.Report("Reading bounded current Windows Update activity...");
            var updateActivity = await WindowsUpdateActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(updateActivity);
            Progress?.Report("Reading exact Update Orchestrator failure history...");
            var orchestratorHistory = await ServicingServiceFailureEvents.CheckFocusedAsync(kind, token).ConfigureAwait(false);
            checks.Add(orchestratorHistory);
            Progress?.Report("Correlating Update Orchestrator state, task, activity and history...");
            checks.Add(UpdateOrchestratorOperationalCorrelation.Check(
                orchestratorService, updateTask, updateActivity, orchestratorHistory, token));
        }
        if (kind == WorkflowKind.DeliveryOptimizationService)
        {
            Progress?.Report("Reading exact Delivery Optimization service state...");
            var deliveryService = DeliveryOptimizationDiagnostics.AsReadOnly(
                await DeliveryOptimizationDiagnostics.CheckAsync(token).ConfigureAwait(false));
            checks.Add(deliveryService);
            Progress?.Report("Reading privacy-bounded Delivery Optimization activity...");
            var deliveryActivity = await DeliveryOptimizationActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(deliveryActivity);
            Progress?.Report("Reading Delivery Optimization policy surfaces...");
            var deliveryPolicy = DeliveryOptimizationPolicyDiagnostics.Check();
            checks.Add(deliveryPolicy);
            Progress?.Report("Reading exact Delivery Optimization failure history...");
            var deliveryHistory = await DeliveryOptimizationServiceFailureEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(deliveryHistory);
            Progress?.Report("Correlating Delivery Optimization service, activity, policy and history evidence...");
            checks.Add(DeliveryOptimizationOperationalCorrelation.Check(
                deliveryService, deliveryActivity, deliveryPolicy, deliveryHistory, token));
        }
        bool administrator = WindowsState.IsAdministrator();
        if (kind == WorkflowKind.WindowsUpdate)
        {
            Progress?.Report("Checking required Windows Update engine files and signatures...");
            checks.Add(await WindowsUpdateEngineFileDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Checking exact Windows Update service DLL registrations and signatures...");
            var updateServiceDlls = await ServiceDllDiagnostics.CheckSelectedAsync("Windows Update service DLLs",
                ["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"], token).ConfigureAwait(false);
            checks.Add(updateServiceDlls);
            Progress?.Report("Checking all six current Windows Update servicing-chain service states...");
            var updateServiceStates = await ServicingServiceStateDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(updateServiceStates);
            Progress?.Report("Reading exact servicing service failure histories...");
            var updateServiceHistory = await ServicingServiceFailureEvents.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(updateServiceHistory);
            Progress?.Report("Correlating current Windows Update service, DLL and exact failure-history evidence...");
            checks.Add(ServicingChainCorrelation.Check(updateServiceStates, updateServiceDlls, updateServiceHistory, token));
            Progress?.Report("Reading recent Windows Update Client events...");
            checks.Add(await WindowsUpdateEvents.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading installed Windows hotfix inventory...");
            checks.Add(await InstalledHotfixDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading BITS transfer jobs...");
            checks.Add(await BitsDiagnostics.CheckAsync(administrator, token).ConfigureAwait(false));
            Progress?.Report("Reading privacy-bounded current Delivery Optimization activity...");
            checks.Add(await DeliveryOptimizationActivityDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Delivery Optimization classic and Policy CSP settings...");
            checks.Add(DeliveryOptimizationPolicyDiagnostics.Check());
            Progress?.Report("Reading managed Windows Update policy...");
            checks.Add(UpdatePolicyDiagnostics.Check());
            Progress?.Report("Reading managed feature update targeting and safeguard policy...");
            checks.Add(FeatureUpdatePolicyDiagnostics.Check());
            Progress?.Report("Identifying Windows Update policy configuration surfaces...");
            checks.Add(UpdatePolicySurfaceDiagnostics.Check());
            Progress?.Report("Reading managed update deadlines and notification behavior...");
            checks.Add(UpdateRestartPolicyDiagnostics.Check());
            Progress?.Report("Reading managed component repair source policy...");
            checks.Add(ComponentRepairSourceDiagnostics.Check());
            Progress?.Report("Measuring Windows Update cache areas without reading filenames or file contents...");
            checks.Add(await UpdateCacheDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        if (kind is WorkflowKind.WindowsUpdate or WorkflowKind.SystemFiles)
        {
            Progress?.Report("Reading the newest bounded portions of CBS, DISM and applicable Windows Setup logs...");
            checks.AddRange(await ServicingLogDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Windows reserved-storage state without changing servicing configuration...");
            checks.Add(await ReservedStorageDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Windows servicing package states without changing packages...");
            checks.Add(await ServicingPackageDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading optional Windows feature states without changing features...");
            checks.Add(await OptionalFeatureDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Windows capability states without changing capabilities...");
            checks.Add(await WindowsCapabilityDiagnostics.CheckAsync(token).ConfigureAwait(false));
        }
        var scopes = Workflows.Scopes(kind);
        bool focusedService = kind == WorkflowKind.DeliveryOptimizationService ||
            scopes.Length > 0 && scopes.All(scope => ServiceRecovery.ServiceName(scope) is not null);
        if (!administrator && kind is not (WorkflowKind.Printing or WorkflowKind.BitsService or WorkflowKind.CryptographicService or WorkflowKind.ModulesInstallerService or WorkflowKind.UpdateOrchestratorService))
        {
            Progress?.Report("Reading service state without elevation...");
            bool focusedDemandStart = kind is WorkflowKind.WinHttpAutoProxyService or WorkflowKind.WindowsUpdateMedicService;
            checks.AddRange(await ServiceInspection.ReadAsync(scopes,
                (scope, cancellationToken) => ServiceRecovery.CheckAsync(scope, cancellationToken,
                    focusedDemandStart && scope is RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService),
                token, Progress).ConfigureAwait(false));
        }
        checks.Add(new("Administrator", administrator ? CheckState.Clear : focusedService ? CheckState.Information : CheckState.Attention,
            administrator ? "This process is running as administrator." : focusedService
                ? "Exact service diagnosis is available as a standard user. Administrator access is required only if a repair is executed."
                : "Integrity checks and repairs require running KillerBench as administrator."));
        var restart = RestartDiagnostics.Check();
        checks.Add(restart);
        if (kind == WorkflowKind.Installer && MsiProductCode is not null &&
            LastMsiProduct is not null && selectedMsiActivity is not null)
        {
            Progress?.Report("Reviewing exact selected MSI repair eligibility without changing the package...");
            checks.Add(MsiRepairReviewDiagnostics.Check(MsiProductCode, LastMsiProduct,
                selectedMsiActivity, restart, administrator, token));
        }
        if (kind is WorkflowKind.WindowsUpdate or WorkflowKind.SystemFiles)
        {
            Progress?.Report("Correlating pending servicing transitions with restart indicators...");
            checks.Add(ServicingTransitionCorrelation.Check(
                checks.Last(check => check.Name == "Servicing packages"),
                checks.Last(check => check.Name == "Optional Windows features"), restart, token));
        }
        if (kind is WorkflowKind.Printing or WorkflowKind.TimeService or WorkflowKind.Installer or WorkflowKind.Wmi or WorkflowKind.Audio or WorkflowKind.Search or WorkflowKind.Defender or WorkflowKind.EventLog or WorkflowKind.BackupServices or WorkflowKind.Bluetooth or WorkflowKind.Camera or WorkflowKind.SmartCard or WorkflowKind.Biometric or WorkflowKind.FileSharing or WorkflowKind.TaskSchedulerService or WorkflowKind.WindowsUpdateService or WorkflowKind.DeliveryOptimizationService or WorkflowKind.BitsService or WorkflowKind.CryptographicService or WorkflowKind.ModulesInstallerService or WorkflowKind.WindowsUpdateMedicService or WorkflowKind.UpdateOrchestratorService or WorkflowKind.DiagnosticPolicyService or WorkflowKind.WindowsErrorReportingService or WorkflowKind.WindowsEventCollectorService or WorkflowKind.RemoteAccessConnectionManagerService or WorkflowKind.RemoteDesktopServices or WorkflowKind.WindowsRemoteManagement or WorkflowKind.SecurityCenterService or WorkflowKind.NetworkConnectionsService or WorkflowKind.WinHttpAutoProxyService or WorkflowKind.WindowsConnectionManagerService or WorkflowKind.NetworkSetupService or WorkflowKind.AppReadinessService or WorkflowKind.StateRepositoryService or WorkflowKind.UserProfileService or WorkflowKind.CredentialManagerService or WorkflowKind.ShellHardwareDetectionService or WorkflowKind.DeviceInstallService or WorkflowKind.AppxDeploymentService or WorkflowKind.ClientLicenseService or WorkflowKind.StoreInstallService or WorkflowKind.WindowsLicenseManagerService or WorkflowKind.PushToInstallService or WorkflowKind.WebAccountManagerService or WorkflowKind.MicrosoftAccountSignInService or WorkflowKind.StorageService or WorkflowKind.OptimizeDrivesService or WorkflowKind.WindowsImageAcquisitionService or WorkflowKind.FunctionDiscoveryProviderHostService or WorkflowKind.FunctionDiscoveryResourcePublicationService or WorkflowKind.HumanInterfaceDeviceService or WorkflowKind.PortableDeviceEnumeratorService or WorkflowKind.WindowsFontCacheService or WorkflowKind.DeviceAssociationService or WorkflowKind.TextInputManagementService or WorkflowKind.DisplayEnhancementService or WorkflowKind.SystemNotifications or WorkflowKind.CoreWindowsServices)
            return checks;
        var disk = new DriveInfo(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))!);
        checks.Add(new("System disk", disk.AvailableFreeSpace < 5L * 1024 * 1024 * 1024 ? CheckState.Attention : CheckState.Clear,
            $"{disk.AvailableFreeSpace / (1024d * 1024 * 1024):F1} GB free. KillerBench defers servicing below 5 GB; this is a conservative guard, not an update space requirement."));
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        foreach (string service in new[] { "wuauserv", "BITS", "CryptSvc", "TrustedInstaller" })
        {
            using var key = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + service);
            object? start = key?.GetValue("Start");
            checks.Add(new(service, start is not int ? CheckState.Unknown : (int)start == 4 ? CheckState.Attention : CheckState.Clear,
                start is not int ? "Service configuration unavailable." : (int)start == 4 ? "Service disabled. Review organizational policy; no setting changed." : "Service is not disabled. A stopped demand-start service can be normal."));
        }
        return checks;
    }
    public async Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
    {
        if (kind == RepairKind.InstallerRegistration)
        {
            progress?.Report("Inspecting exact Windows Installer service registration.");
            return InstallerServiceRegistrationDiagnostics.Check();
        }
        if (kind == RepairKind.MsiProduct)
        {
            if (MsiProductCode is null)
                return new("MSI product repair", CheckState.Information,
                    "Select one exact MSI product code to evaluate guarded product repair. No product action was selected.");
            progress?.Report("Inspecting exact selected MSI product registration.");
            LastMsiProduct ??= await MsiProductDiagnostics.CheckAsync(MsiProductCode, token).ConfigureAwait(false);
            return MsiRepair.AssessEligibility(MsiProductCode, LastMsiProduct);
        }
        if (kind == RepairKind.ComputerRename)
        {
            if (string.IsNullOrWhiteSpace(ProposedComputerName))
                return new("Computer name change", CheckState.Information,
                    "Enter one proposed computer name to evaluate the guarded standalone rename. No name was selected.");
            progress?.Report("Inspecting current and configured computer identity for the exact proposed name.");
            var context = await ComputerNameCollector.ReadAsync(token).ConfigureAwait(false);
            var restart = RestartDiagnostics.Check();
            return ComputerRename.ToWorkflowCheck(ProposedComputerName, context, WindowsState.IsAdministrator(), restart);
        }
        if (kind == RepairKind.DefaultPrinter)
        {
            progress?.Report("Inspecting installed queues and the current default printer.");
            var state = await new WindowsDefaultPrinterSelectionHost().InspectAsync(token).ConfigureAwait(false);
            return DefaultPrinterSelection.Assess(state);
        }
        if (kind == RepairKind.PrintJobCancellation)
        {
            progress?.Report("Inspecting exact visible print-job identities and status without changing jobs.");
            ToolResult evidence = await PrintJobDiagnostics.CaptureAsync(token).ConfigureAwait(false);
            PrintJobInventory? inventory = PrintJobDiagnostics.ReadInventory(evidence);
            return inventory is null
                ? new("Selected print-job cancellation", CheckState.Unknown,
                    "Print-job inventory was incomplete. No cancellation candidate is eligible.", Evidence: evidence)
                : PrintJobCancellation.Assess(inventory);
        }
        if (kind == RepairKind.PrintSpoolDirectory)
        {
            var reconciliation = await PrintSpoolDirectoryRecovery.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(),
                PrintSpoolDirectoryDiagnostics.Read, token).ConfigureAwait(false);
            if (reconciliation.Outcome != PrintSpoolDirectoryReconciliationOutcome.NoPending)
                return new("Print spool directory recovery", reconciliation.Outcome == PrintSpoolDirectoryReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return PrintSpoolDirectoryDiagnostics.Check();
        }
        if (kind == RepairKind.WindowsRecoveryEnvironment)
        {
            var reconciliation = await WindowsRecoveryReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(),
                WindowsRecoveryDiagnostics.CheckAsync, token).ConfigureAwait(false);
            if (reconciliation.Outcome != WindowsRecoveryReconciliationOutcome.NoPending)
                return new("Windows Recovery Environment recovery", reconciliation.Outcome == WindowsRecoveryReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return await WindowsRecoveryDiagnostics.CheckAsync(token).ConfigureAwait(false);
        }
        if (kind == RepairKind.DnsCache)
        {
            if (string.IsNullOrWhiteSpace(NetworkLookupHost))
                return new("DNS cache", CheckState.Information,
                    "Enter an exact hostname to evaluate local DNS cache recovery. No cache change was selected.");
            string target = DnsDiagnostics.ValidateHost(NetworkLookupHost);
            if (LastDnsLookup is null || !string.Equals(LastDnsLookup.Host, target, StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report("Resolving " + target + " before selecting DNS cache recovery.");
                LastDnsLookup = await DnsLookup(target, NetworkTimeout, token).WaitAsync(NetworkTimeout, token).ConfigureAwait(false);
            }
            var check = DnsDiagnostics.ToWorkflowCheck(LastDnsLookup);
            return check with
            {
                Name = "DNS cache",
                State = DnsCacheRecovery.IsEligible(LastDnsLookup) ? CheckState.Repairable : check.State,
                Repair = DnsCacheRecovery.IsEligible(LastDnsLookup) ? RepairKind.DnsCache : null,
                Detail = DnsCacheRecovery.IsEligible(LastDnsLookup)
                    ? check.Detail + " A guarded local DNS cache recovery is eligible for this exact hostname."
                    : check.Detail
            };
        }
        if (kind == RepairKind.FirewallProfiles)
        {
            progress?.Report("Inspecting Windows Firewall profile state and policy ownership.");
            var state = await new WindowsFirewallProfileEnableHost().InspectAsync(token).ConfigureAwait(false);
            return FirewallProfileEnable.Assess(state);
        }
        if (kind == RepairKind.StorageFileSystem)
        {
            if (!WindowsState.IsAdministrator()) throw new UnauthorizedAccessException("Administrator access is required for the filesystem check.");
            return await StorageDiagnostics.CheckVolumeAsync(StorageVolume ?? StorageDiagnostics.SystemVolume(), progress, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.VolumeOptimization)
        {
            string volume = StorageVolume ?? StorageDiagnostics.SystemVolume();
            var request = VolumeOptimizationRequest ??
                VolumeOptimization.CreateRequest(volume, VolumeOptimizationMode.Recommended);
            var review = await VolumeOptimization.ReviewAsync(request, new WindowsVolumeOptimizationHost(), progress, token).ConfigureAwait(false);
            return VolumeOptimization.ToWorkflowCheck(review);
        }
        if (kind == RepairKind.SystemRestorePoint)
        {
            var request = SystemRestoreRequest ??
                SystemRestoreCreation.CreateRequest("KillerBench restore point", SystemRestorePointReason.ModifySettings);
            var review = await SystemRestoreCreation.ReviewAsync(request, new WindowsSystemRestoreCreationHost(), token).ConfigureAwait(false);
            return SystemRestoreCreation.ToWorkflowCheck(review);
        }
        if (kind == RepairKind.SearchIndexRebuild)
        {
            var review = await SearchIndexRebuild.ReviewAsync(new WindowsSearchIndexRebuildHost(), token).ConfigureAwait(false);
            return SearchIndexRebuild.ToWorkflowCheck(review);
        }
        if (kind == RepairKind.OrcaCacheReset)
        {
            progress?.Report("Checking exact selected Outlook cache retention eligibility.");
            return await OrcaCacheReset.ReviewAsync(OrcaOptions, token: token).ConfigureAwait(false);
        }
        if (kind == RepairKind.PreviousWindowsInstallationRemoval)
        {
            progress?.Report("Checking previous Windows installation and rollback availability.");
            return await PreviousWindowsInstallationRemoval.ReviewAsync(PreviousWindowsInstallationFolder,
                PreviousWindowsInstallationHost, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.EventChannelEnable)
        {
            if (string.IsNullOrWhiteSpace(EventChannel))
                return new("Diagnostic event channel", CheckState.Information,
                    "Select one exact supported channel to evaluate guarded enablement. No channel change was selected.");
            progress?.Report("Inspecting the exact selected event channel and policy ownership.");
            var host = new WindowsEventChannelEnableHost();
            var eventResult = File.Exists(Path.Combine(EventChannelChangeFolder, "event-channel-enable.pending.json"))
                ? await EventChannelEnable.ReconcileAsync(EventChannelChangeFolder, host, progress, token).ConfigureAwait(false)
                : await EventChannelEnable.RunAsync(EventChannel, false, EventChannelChangeFolder, host, progress, token).ConfigureAwait(false);
            return EventChannelEnable.ToWorkflowCheck(eventResult);
        }
        if (kind == RepairKind.BitLockerProtectionResume)
        {
            var review = await BitLockerProtectionResume.ReviewAsync(BitLockerOptions.Volume,
                new WindowsBitLockerProtectionResumeHost(), token).ConfigureAwait(false);
            return BitLockerProtectionResume.ToWorkflowCheck(review);
        }
        if (kind == RepairKind.BitLockerRecoveryBackup)
        {
            progress?.Report("Inspecting the exact BitLocker volume, recovery protector and file destination.");
            return await BitLockerBackupEligibility.CheckAsync(BitLockerOptions, BitLockerRecoveryProtector,
                BitLockerBackupFolder, BitLockerBackupHost, WindowsState.IsAdministrator(), token).ConfigureAwait(false);
        }
        if (kind == RepairKind.HibernationDisable)
        {
            progress?.Report("Inspecting managed policy, hibernation, hiberfil.sys, the active plan and power timers.");
            return await DebloatHibernationEligibility.CheckAsync(DebloatOptions, DebloatChangeFolder,
                DebloatHibernationHost, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.HighPerformancePowerPlan)
        {
            progress?.Report("Inspecting managed policy and the complete power-plan inventory.");
            return await DebloatPowerPlanEligibility.CheckAsync(DebloatOptions, DebloatChangeFolder,
                DebloatPowerPlanHost, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.PowerTimers)
        {
            progress?.Report("Inspecting managed policy, the active plan, all four timers and hibernation state.");
            return await DebloatPowerTimerEligibility.CheckAsync(DebloatOptions, DebloatChangeFolder,
                DebloatPowerTimerHost, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.UpdateDataStoreReset)
        {
            progress?.Report("Verifying the selected Windows Update source before data-store diagnosis.");
            var source = await UpdateServiceDiagnostics.CheckAsync(token).ConfigureAwait(false);
            if (source.Evidence is null)
                return new("Windows Update data store", CheckState.Unknown,
                    "Update source registration evidence is unavailable. The selected source was not searched and no reset is eligible.");
            var sourceReadiness = UpdateServiceDiagnostics.AssessSource(UpdateOptions, source.Evidence);
            if (sourceReadiness.State is CheckState.Unknown or CheckState.Attention)
                return sourceReadiness with
                {
                    Name = "Windows Update data store",
                    Detail = sourceReadiness.Detail + " No data-store reset is eligible."
                };
            var search = await UpdateDiscovery.SearchAsync(UpdateOptions, progress, token).ConfigureAwait(false);
            return UpdateDataStoreReset.ToWorkflowCheck(search);
        }
        if (kind == RepairKind.BitsJobRetry)
        {
            LastBitsInventory ??= await BitsDiagnostics.ReadAsync(BitsAllUsers, token).ConfigureAwait(false);
            return BitsRetry.ToWorkflowCheck(LastBitsInventory, BitsSelectedJobId,
                new WindowsBitsRetryHost().OwnerAccount, token);
        }
        if (kind == RepairKind.DeliveryOptimization)
        {
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => DeliveryOptimizationDiagnostics.CheckAsync(cancellationToken);
            var reconciliation = await DeliveryOptimizationRecovery.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != DeliveryOptimizationReconciliationOutcome.NoPending)
                return new("Delivery Optimization recovery", reconciliation.Outcome == DeliveryOptimizationReconciliationOutcome.Verified
                    ? CheckState.Clear : CheckState.Attention, reconciliation.Detail);
            return await Inspect(token).ConfigureAwait(false);
        }
        if (ServiceRecovery.ServiceName(kind) is not null)
        {
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => ServiceRecovery.CheckAsync(kind, cancellationToken,
                kind is RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService);
            var reconciliation = await ServiceStartReconciliation.ReconcileAsync(kind, RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != ServiceStartReconciliationOutcome.NoPending)
                return new("Service start recovery", reconciliation.Outcome == ServiceStartReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return await Inspect(token).ConfigureAwait(false);
        }
        new WindowsRepairHost().CheckPrerequisites();
        if (kind == RepairKind.WindowsInstaller)
        {
            var readiness = InstallerDiagnostics.Check();
            if (readiness.State != CheckState.Clear) return readiness;
        }
        if (kind == RepairKind.ComponentCleanup)
        {
            ComponentCleanup.RequireIdleServicing();
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => InspectComponentCleanupAsync(progress, cancellationToken);
            var reconciliation = await ComponentCleanupReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != ComponentCleanupReconciliationOutcome.NoPending)
                return new("Component cleanup recovery", reconciliation.Outcome == ComponentCleanupReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return await InspectComponentCleanupAsync(progress, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.DefenderSignatures)
        {
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => InspectDefenderAsync(progress, cancellationToken);
            var reconciliation = await DefenderRefreshReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != DefenderRefreshReconciliationOutcome.NoPending)
                return new("Defender signature refresh recovery", reconciliation.Outcome == DefenderRefreshReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return await InspectDefenderAsync(progress, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.PrinterOffline)
        {
            Task<ToolResult> Inspect(IReadOnlyList<string> selected, CancellationToken cancellationToken) =>
                PrintRecovery.ExecuteAsync(PrinterOfflineRecovery.Step(true, selected), progress, cancellationToken);
            var reconciliation = await PrinterOfflineReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != PrinterOfflineReconciliationOutcome.NoPending)
                return new("Printer offline recovery", reconciliation.Outcome == PrinterOfflineReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return PrinterOfflineRecovery.Interpret(await PrintRecovery.ExecuteAsync(PrinterOfflineRecovery.Step(true), progress, token).ConfigureAwait(false));
        }
        if (kind == RepairKind.DhcpLease)
        {
            Task<ToolResult> Inspect(CancellationToken cancellationToken) => DhcpRecovery.ExecuteAsync(DhcpRecovery.Step(true), progress, cancellationToken);
            var reconciliation = await DhcpLeaseReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != DhcpLeaseReconciliationOutcome.NoPending)
                return new("DHCP lease recovery", reconciliation.Outcome == DhcpLeaseReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return DhcpRecovery.Interpret(await Inspect(token).ConfigureAwait(false));
        }
        if (kind == RepairKind.TimeSynchronization)
        {
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => TimeRecovery.CheckAsync(progress, cancellationToken);
            var reconciliation = await TimeSynchronizationReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != TimeSynchronizationReconciliationOutcome.NoPending)
                return new("Windows Time recovery", reconciliation.Outcome == TimeSynchronizationReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return await Inspect(token).ConfigureAwait(false);
        }
        if (kind == RepairKind.TimeSourceConfiguration)
        {
            progress?.Report("Inspecting domain role, policy ownership and stored Windows Time source configuration.");
            var preview = await TimePlanning.PreviewAsync(TimeOptions, token).ConfigureAwait(false);
            return TimeConfigurationDiagnostics.AssessSelection(TimeOptions, preview.Environment, preview.Plan);
        }
        if (kind == RepairKind.WmiRepository)
        {
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => InspectWmiRepositoryAsync(progress, cancellationToken);
            var reconciliation = await WmiRepositoryReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != WmiRepositoryReconciliationOutcome.NoPending)
                return new("WMI repository recovery", reconciliation.Outcome == WmiRepositoryReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            return await InspectWmiRepositoryAsync(progress, token).ConfigureAwait(false);
        }
        if (kind == RepairKind.WmiProvider) return await WmiProviderDiagnostics.CheckAsync(token).ConfigureAwait(false);
        if (kind == RepairKind.PrintQueues)
        {
            LastPrintQueueTargets = null;
            Task<ToolResult> Inspect(CancellationToken cancellationToken) => PrintRecovery.ExecuteAsync(PrintRecovery.Step(true), progress, cancellationToken);
            var reconciliation = await PrintQueueReconciliation.ReconcileAsync(RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != PrintQueueReconciliationOutcome.NoPending)
                return new("Print queue recovery", reconciliation.Outcome == PrintQueueReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            progress?.Report("Correlating exact paused queues with visible waiting print jobs...");
            var assessment = PrintQueueRepairEligibility.Assess(await Inspect(token).ConfigureAwait(false),
                await PrintJobDiagnostics.CaptureAsync(token).ConfigureAwait(false));
            LastPrintQueueTargets = assessment.Targets;
            return assessment.Check;
        }
        if (kind is RepairKind.ComponentStore or RepairKind.SystemFiles)
        {
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => InspectServicingAsync(kind, progress, cancellationToken);
            var reconciliation = await ServicingRepairReconciliation.ReconcileAsync(kind, RepairRecoveryStore.ForCurrentUser(), Inspect, token).ConfigureAwait(false);
            if (reconciliation.Outcome != ServicingRepairReconciliationOutcome.NoPending)
                return new("Servicing repair recovery", reconciliation.Outcome == ServicingRepairReconciliationOutcome.Verified
                    ? CheckState.Information : CheckState.Attention, reconciliation.Detail);
            var integrity = await InspectServicingAsync(kind, progress, token).ConfigureAwait(false);
            if (kind != RepairKind.ComponentStore || integrity.State != CheckState.Repairable)
                return integrity;
            progress?.Report("Checking component repair source policy before selecting RestoreHealth...");
            return ComponentRepairSourceDiagnostics.ApplyPrerequisite(integrity, ComponentRepairSourceDiagnostics.Check());
        }
        var step = Repairs.Plan(kind).Steps.Single(s => s.Verification);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromHours(1), progress, token).ConfigureAwait(false);
        return Workflows.Interpret(kind, result) with { Evidence = result };
    }

    private static async Task<WorkflowCheck> InspectDefenderAsync(IProgress<string>? progress, CancellationToken token) =>
        DefenderRecovery.Interpret(await DefenderRecovery.ExecuteAsync(DefenderRecovery.Step(true), progress, token).ConfigureAwait(false));
    private static async Task<WorkflowCheck> InspectComponentCleanupAsync(IProgress<string>? progress, CancellationToken token) =>
        ComponentCleanup.Interpret(await BoundedTool.RunAsync(ComponentCleanup.Step(true), TimeSpan.FromHours(1), progress, token).ConfigureAwait(false));
    private static async Task<WorkflowCheck> InspectServicingAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
    {
        var step = Repairs.Plan(kind).Steps.Single(candidate => candidate.Verification);
        return Workflows.Interpret(kind, await BoundedTool.RunAsync(step, TimeSpan.FromHours(1), progress, token).ConfigureAwait(false));
    }
    private static async Task<WorkflowCheck> InspectWmiRepositoryAsync(IProgress<string>? progress, CancellationToken token) =>
        WmiRecovery.Interpret(await WmiRecovery.ExecuteAsync(WmiRecovery.Step(true), progress, token).ConfigureAwait(false));
    public async Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
    {
        if (kind == RepairKind.PrintQueues)
        {
            if (LastPrintQueueTargets is not { Count: > 0 })
                throw new InvalidOperationException("Fresh exact paused-queue and waiting-job evidence is required before queue resume.");
            return await Repairs.RunAsync(kind, folder, progress, token,
                selectedPrinterQueues: LastPrintQueueTargets).ConfigureAwait(false);
        }
        if (kind != RepairKind.DnsCache)
            return await Repairs.RunAsync(kind, folder, progress, token).ConfigureAwait(false);
        if (LastDnsLookup is null || !DnsCacheRecovery.IsEligible(LastDnsLookup))
            throw new InvalidOperationException("Fresh exact-hostname evidence is required before DNS cache recovery.");
        string dnsFolder = Path.Combine(folder, "dns-cache");
        if (File.Exists(Path.Combine(dnsFolder, "dns-cache.pending.json")))
        {
            var reconciliation = await DnsCacheReconciliation.RunAsync(dnsFolder,
                DnsRecoveryHost.ResolveAsync, token, progress).ConfigureAwait(false);
            return new(Environment.MachineName, kind, DateTimeOffset.UtcNow, reconciliation.Detail,
                reconciliation.ArchivePath ?? reconciliation.PendingPath, [])
            {
                DnsCacheReconciliation = reconciliation
            };
        }
        var recovery = await DnsCacheRecovery.RunAsync(LastDnsLookup, true, DnsRecoveryHost,
            DnsRecoveryJournalFactory(dnsFolder), token, progress).ConfigureAwait(false);
        IReadOnlyList<RepairStepResult> steps = recovery.Flush is null
            ? [] : [new(DnsCacheRecovery.FlushStep(), recovery.Flush)];
        return new(Environment.MachineName, kind, DateTimeOffset.UtcNow, recovery.Detail,
            recovery.JournalPath ?? Path.Combine(dnsFolder, "dns-cache.pending.json"), steps)
        {
            DnsCacheRecovery = recovery
        };
    }

    public static WorkflowCheck ReadNetworkConfiguration(string name,
        Func<(string[] Addresses, string[] Gateways, string[] Dns)> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        try
        {
            var snapshot = read();
            if (snapshot.Addresses is null || snapshot.Gateways is null || snapshot.Dns is null)
                return new(name, CheckState.Unknown, "Adapter configuration was incomplete. Other interfaces can still be inspected; no repair selected.");
            return NetworkSnapshot.Assess(new(name, snapshot.Addresses, snapshot.Gateways, snapshot.Dns));
        }
        catch (Exception error) when (error is NetworkInformationException or InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(name, CheckState.Unknown, "Adapter configuration could not be read. The interface may have changed during collection or access may be unavailable. Other interfaces can still be inspected; no repair selected.");
        }
    }

    public static WorkflowCheck AssessNetwork(string name, string[] addresses, string[] gateways, string[] dns)
    {
        if (addresses.Length == 0) return new(name, CheckState.Information, "No IP binding. This may be a filter or virtual adapter; it is not treated as a failed connection.");
        if (addresses.Concat(gateways).Concat(dns).Any(a => !System.Net.IPAddress.TryParse(a, out _)))
            return new(name, CheckState.Unknown, "Adapter configuration contains an unrecognized address. Recollect configuration before drawing conclusions; no repair selected.");
        bool usable = addresses.Select(System.Net.IPAddress.Parse).Any(IsUsableHostAddress);
        var findings = new List<string>();
        if (!usable) findings.Add("No usable host address was reported. Check link state and the intended static or DHCP configuration.");
        if (gateways.Length == 0) findings.Add("No default gateway was reported for this adapter. Off-subnet traffic may use another adapter or an explicit route.");
        if (dns.Length == 0) findings.Add("No DNS server was reported for this adapter. Name resolution may use another interface or a local resolver.");
        bool attention = findings.Count > 0;
        return new(name, attention ? CheckState.Attention : CheckState.Clear,
            $"Addresses: {string.Join(", ", addresses)}; gateways: {string.Join(", ", gateways)}; DNS: {string.Join(", ", dns)}. " +
            (attention ? string.Join(" ", findings) + " This can be intentional on isolated or virtual adapters. No configuration changes were selected." : "Local configuration is present; reachability and name resolution are untested."));
    }

    private static bool IsUsableHostAddress(System.Net.IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (System.Net.IPAddress.IsLoopback(address) || address.Equals(System.Net.IPAddress.Any) ||
            address.Equals(System.Net.IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6Multicast) return false;
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length != 4 || (bytes[0] != 0 && bytes[0] < 224 && !(bytes[0] == 169 && bytes[1] == 254));
    }
}
