namespace KillerBench.Engine.Workflows;

public enum RecipeOutcome { Clear, RepairAvailable, Repaired, NeedsAttention, Unknown, Blocked, Canceled, RestartRequired }
public sealed record RepairRecipe(string Id, int Version, string Title, RepairKind Action,
    IReadOnlyList<string> Dependencies, string VerificationScope)
{
    public bool DiagnosticOnly { get; init; }
    public bool RequiresExplicitOptions { get; init; }
    public bool CanAutoExecute => !DiagnosticOnly && !RequiresExplicitOptions;
    public string Area => GetArea(Id);
    public RepairRecoveryContract RecoveryContract => RepairRecoveryCatalog.For(Action);
    public string Effects => DiagnosticOnly ? "Diagnostic only. This recipe does not execute a repair." :
        RequiresExplicitOptions ? "Explicit repair options and confirmation are required. " + Repairs.Plan(Action).Effects :
        Action == RepairKind.PrintQueues ? "Resumes only exact paused local queues with fresh visible waiting print jobs. Jobs, idle intentional pauses and remote printer connections are preserved. Physical printing remains unverified." :
        Repairs.Plan(Action).Effects;
    public TimeSpan InspectionTimeout { get; init; } = TimeSpan.FromMinutes(65);
    public TimeSpan ExecutionTimeout { get; init; } = TimeSpan.FromHours(3);

    private static string GetArea(string? id)
    {
        string prefix = id?.Split('.')[0] ?? "";
        return prefix switch
        {
            "applications" or "profiles" or "accounts" => "Applications",
            "audio" => "Audio",
            "backup" => "Backup",
            "devices" or "input" or "display" => "Devices",
            "installer" => "Installation",
            "network" => "Network",
            "power" => "Power",
            "printing" => "Printing",
            "remote" => "Remote access",
            "security" or "firewall" => "Security",
            "eventlog" or "events" or "diagnostics" or "tasks" or "notifications" or "search" or "core" => "Windows services",
            "storage" => "Storage",
            "time" => "Time",
            "update" => "Windows Update",
            "windows" => "Windows",
            "wmi" => "Windows management",
            _ => "Other"
        };
    }
}
public sealed record RecipeResult(string RecipeId, int RecipeVersion, RecipeOutcome Outcome,
    WorkflowCheck? Check, RepairResult? Repair, string Detail)
{
    public IReadOnlyList<string> BlockingRecipes { get; init; } = [];
    public bool ExecutionUncertain { get; init; }
    public RecipeOutcome EffectiveOutcome => ExecutionUncertain ||
        Outcome == RecipeOutcome.Clear && Check?.EffectiveState != CheckState.Clear
        ? RecipeOutcome.Unknown : Outcome;
}

public static class RecipeCatalog
{
    public static IReadOnlyList<RepairRecipe> SelectForWorkflow(WorkflowKind kind, IReadOnlyCollection<string> recipeIds)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentNullException.ThrowIfNull(recipeIds);
        var available = ForWorkflow(kind);
        var byId = available.ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in recipeIds.ToArray())
        {
            if (string.IsNullOrWhiteSpace(id) || !byId.ContainsKey(id))
                throw new ArgumentException("A selected recipe is not part of this workflow.", nameof(recipeIds));
            Include(id);
        }
        return Array.AsReadOnly(available.Where(recipe => selected.Contains(recipe.Id))
            .Select(recipe => recipe with { Dependencies = Array.AsReadOnly(recipe.Dependencies.ToArray()) }).ToArray());

        void Include(string id)
        {
            if (!selected.Add(id)) return;
            foreach (string dependency in byId[id].Dependencies) Include(dependency);
        }
    }

    private static readonly string[] ComponentStoreDependency = ["windows.component-store"];

    public static IReadOnlyList<RepairRecipe> All { get; } = Array.AsReadOnly<RepairRecipe>(
    [
        new RepairRecipe("windows.component-store", 1, "Windows component store", RepairKind.ComponentStore, [], "Component-store corruption"),
        new RepairRecipe("windows.system-files", 1, "Protected system files", RepairKind.SystemFiles, [], "Protected system-file integrity"),
        new RepairRecipe("printing.spooler", 1, "Print Spooler availability", RepairKind.PrintSpooler, [], "Print Spooler running state"),
        new RepairRecipe("printing.spool-directory", 1, "Windows print spool directory", RepairKind.PrintSpoolDirectory, [], "Exact Windows default directory presence on a fixed drive; write access, queued documents and physical printing excluded"),
        new RepairRecipe("installer.service", 1, "Windows Installer availability", RepairKind.WindowsInstaller, [], "Windows Installer running state"),
        new RepairRecipe("installer.registration", 1, "Windows Installer service registration", RepairKind.InstallerRegistration, [], "Exact Windows Installer service registration matches the expected native configuration; package installation and application behavior excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("installer.msi-product", 1, "Selected MSI product registration", RepairKind.MsiProduct, [], "Exact selected MSI product reports a local or default installed state after repair; application functionality, vendor settings and user data excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("storage.filesystem", 2, "Selected-volume filesystem", RepairKind.StorageFileSystem, [], "Exact selected NTFS volume reports clean after repair; physical media health excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("storage.optimization", 1, "Selected-volume optimization", RepairKind.VolumeOptimization, [], "Exact selected healthy fixed volume completes the chosen operation and a fresh analysis; performance improvement and physical media health excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("windows.restore-point", 1, "Create a System Restore point", RepairKind.SystemRestorePoint, [], "One new exact restore-point inventory entry matches the selected description and reason; future restore success and volume protection excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("windows.computer-name", 1, "Standalone computer name", RepairKind.ComputerRename, [], "The exact proposed name becomes the configured name and later the active name after restart; name uniqueness, organizational policy, application dependencies and network propagation excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("search.index-rebuild", 1, "Windows Search index rebuild", RepairKind.SearchIndexRebuild, ["search.service"], "Exact Windows Search service and SystemIndex directory return with the prior index retained; search result completeness excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("applications.outlook-cache-reset", 1, "Selected Outlook cache retention", RepairKind.OrcaCacheReset, [], "Every exact selected cache source is absent and its unique retained directory exists; Outlook launch, synchronization and cache recreation excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("windows.previous-installation-removal", 1, "Previous Windows rollback removal", RepairKind.PreviousWindowsInstallationRemoval, [], "Windows reports operating-system rollback unavailable after one supported removal request; immediate space reclamation and future recovery success excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("security.bitlocker-resume", 1, "Resume BitLocker protection", RepairKind.BitLockerProtectionResume, [], "Exact fully encrypted volume retains its identity and protector set with protection reported on; recovery escrow and future boot recovery excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("security.bitlocker-recovery-backup", 1, "Back up a BitLocker recovery protector", RepairKind.BitLockerRecoveryBackup, [], "One new restricted file contains the exact selected recovery-password protector for the unchanged selected volume; off-device retention, escrow and future boot recovery excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("power.high-performance-plan", 1, "Activate the High performance power plan", RepairKind.HighPerformancePowerPlan, [], "The exact standard High performance plan is active and the registered plan identity set is unchanged; plan-setting values, energy use, performance and application behavior excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("power.hibernation-disable", 1, "Disable Windows hibernation", RepairKind.HibernationDisable, [], "Hibernation is disabled and hiberfil.sys is absent while the exact active plan and all four sleep and display timers remain unchanged; Fast Startup availability and later re-enablement excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("power.sleep-display-timers", 1, "Change sleep and display timers", RepairKind.PowerTimers, [], "All four selected AC and battery sleep and display timers match on the unchanged active plan with hibernation state unchanged; resulting battery life, energy use and application behavior excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("printing.paused-queues", 2, "Paused local print queues", RepairKind.PrintQueues, ["printing.spooler"], "Exact selected queues with visible waiting jobs are present and unpaused; job completion and physical printing excluded"),
        new RepairRecipe("printing.default-printer", 1, "Missing or conflicting default printer", RepairKind.DefaultPrinter, [], "One exact selected installed queue becomes the running account's only default; automatic printer management, queue inventory and physical printing excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("printing.stuck-job", 1, "Selected stuck print job", RepairKind.PrintJobCancellation, ["printing.spooler"], "One exact selected Error or Blocked print job is absent while every other observed job remains present; physical printing and application behavior excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("wmi.service", 1, "Windows Management Instrumentation availability", RepairKind.WindowsManagement, [], "WMI service running state; namespace/provider functionality excluded"),
        new RepairRecipe("wmi.repository", 2, "WMI repository consistency", RepairKind.WmiRepository, ["wmi.service"], "Repository consistency; individual providers excluded"),
        new RepairRecipe("wmi.os-provider", 1, "WMI operating-system provider", RepairKind.WmiProvider, ["wmi.service"], "Local root/cimv2 Win32_OperatingSystem query only") { DiagnosticOnly = true },
        new RepairRecipe("time.synchronization", 2, "Windows Time synchronization", RepairKind.TimeSynchronization, [], "Windows-reported synchronization; absolute accuracy excluded"),
        new RepairRecipe("time.source-configuration", 1, "Windows Time source configuration", RepairKind.TimeSourceConfiguration, [], "The exact selected source is stored and Windows reports synchronization through that source; absolute clock accuracy, source trust and future reachability excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("network.dhcp-client", 1, "DHCP Client availability", RepairKind.DhcpClient, [], "DHCP Client running state"),
        new RepairRecipe("network.dns-client", 1, "DNS Client availability", RepairKind.DnsClient, [], "DNS Client running state; name resolution and registration excluded"),
        new RepairRecipe("network.dns-cache", 1, "Local DNS cache recovery", RepairKind.DnsCache, ["network.dns-client"], "Exact selected hostname resolves after a guarded local cache flush; authoritative DNS and service reachability excluded"),
        new RepairRecipe("network.firewall-profiles", 1, "Windows Firewall profile protection", RepairKind.FirewallProfiles, [], "Exact selected unmanaged profiles are enabled; firewall rules, default actions, logging, IPsec configuration and traffic reachability excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("network.workstation", 1, "SMB client service", RepairKind.Workstation, [], "Workstation service running state; remote share access and authentication excluded"),
        new RepairRecipe("network.location-awareness", 1, "Network Location Awareness", RepairKind.NetworkLocationAwareness, [], "NLA service running state; network identification, firewall profile and connectivity excluded"),
        new RepairRecipe("network.list-service", 1, "Network List Service", RepairKind.NetworkListService, ["network.location-awareness"], "Network List Service running state; network identification, firewall profile and connectivity excluded"),
        new RepairRecipe("network.ip-helper", 1, "IP Helper availability", RepairKind.IpHelper, [], "IP Helper running state; IPv6 transition, port proxy and connectivity excluded"),
        new RepairRecipe("backup.vss", 1, "Volume Shadow Copy availability", RepairKind.VolumeShadowCopy, [], "VSS service running state; writers, snapshots and backup/restore excluded"),
        new RepairRecipe("backup.software-provider", 1, "Software Shadow Copy Provider availability", RepairKind.SoftwareShadowProvider, [], "Software provider service running state; provider operation and backup/restore excluded"),
        new RepairRecipe("backup.windows-backup", 1, "Windows Backup availability", RepairKind.WindowsBackupService, [], "Windows Backup service running state; plan execution, destination access, completion and restore excluded"),
        new RepairRecipe("backup.block-level-engine", 1, "Block Level Backup Engine availability", RepairKind.BlockLevelBackupEngineService, [], "Block Level Backup Engine running state; block transfer, destination access, completion and restore excluded"),
        new RepairRecipe("network.dhcp-lease", 2, "Missing or expired DHCP IPv4 leases", RepairKind.DhcpLease, ["network.dhcp-client"], "Renewed adapters satisfy their retained address or lease-expiry conditions; connectivity excluded"),
        new RepairRecipe("printing.offline-setting", 1, "Local printer offline settings", RepairKind.PrinterOffline, ["printing.spooler"], "Changed queues no longer use offline mode; physical printing excluded"),
        new RepairRecipe("update.service", 1, "Windows Update availability", RepairKind.UpdateService, [], "Windows Update running state; update installation excluded"),
        new RepairRecipe("update.transfers", 1, "Background Intelligent Transfer Service", RepairKind.BackgroundTransfer, [], "BITS running state; transfer completion excluded"),
        new RepairRecipe("update.bits-job-retry", 1, "Selected BITS transfer retry", RepairKind.BitsJobRetry, ["update.transfers"], "The exact selected job retains its owner, name and total-byte identity and reports queued, connecting, transferring or transferred after one request; destination use and owning-application acknowledgment excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("update.cryptography", 1, "Cryptographic Services availability", RepairKind.CryptographicServices, [], "Cryptographic Services running state; certificate validation excluded"),
        new RepairRecipe("update.data-store-reset", 1, "Windows Update data-store recovery", RepairKind.UpdateDataStoreReset, ["update.service", "update.transfers", "update.cryptography"], "The replacement SoftwareDistribution store exists, exact prior update-service states are restored and a fresh same-source search completes; update installation and retained-store removal excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("update.modules-installer", 1, "Windows Modules Installer availability", RepairKind.ModulesInstaller, [], "Windows Modules Installer running state; package installation excluded"),
        new RepairRecipe("update.medic-service", 1, "Windows Update Medic Service availability", RepairKind.WindowsUpdateMedicService, [], "Windows Update Medic running state; remediation, scan, download and installation outcomes excluded"),
        new RepairRecipe("update.orchestrator", 1, "Update Orchestrator availability", RepairKind.UpdateOrchestrator, [], "Update Orchestrator running state; scan, download and installation outcomes excluded"),
        new RepairRecipe("update.delivery-optimization", 2, "Delivery Optimization state", RepairKind.DeliveryOptimization, [], "Paused state continued and stable running state; transfer activity, cache health and update completion excluded"),
        new RepairRecipe("audio.endpoints", 1, "Audio Endpoint Builder", RepairKind.AudioEndpoints, [], "Endpoint Builder running state; device routing excluded"),
        new RepairRecipe("audio.service", 1, "Windows Audio", RepairKind.WindowsAudio, ["audio.endpoints"], "Windows Audio running state; audible playback excluded"),
        new RepairRecipe("search.service", 1, "Windows Search availability", RepairKind.WindowsSearch, [], "Windows Search running state; index completeness excluded"),
        new RepairRecipe("eventlog.service", 1, "Windows Event Log availability", RepairKind.WindowsEventLog, [], "Event Log running state; event recording and channel health excluded"),
        new RepairRecipe("eventlog.channel-enable", 1, "Selected diagnostic event channel", RepairKind.EventChannelEnable, ["eventlog.service"], "The exact selected channel is enabled with retention, automatic backup, maximum size and every other reported configuration field unchanged; event delivery excluded") { RequiresExplicitOptions = true },
        new RepairRecipe("security.defender-signatures", 1, "Defender security intelligence", RepairKind.DefenderSignatures, [], "Defender-reported signature freshness; malware detection excluded"),
        new RepairRecipe("windows.component-cleanup", 1, "Component-store maintenance", RepairKind.ComponentCleanup, [], "Windows no longer recommends cleanup; reclaimed space and corruption excluded"),
        new RepairRecipe("firewall.filtering-engine", 1, "Base Filtering Engine availability", RepairKind.BaseFilteringEngine, [], "BFE running state; effective filtering and connectivity excluded"),
        new RepairRecipe("firewall.service", 1, "Windows Defender Firewall availability", RepairKind.WindowsFirewall, ["firewall.filtering-engine"], "Firewall service running state; profile protection and connectivity excluded"),
        new RepairRecipe("network.wireless", 1, "WLAN AutoConfig availability", RepairKind.WirelessAutoConfig, [], "WLAN service running state; wireless authentication and connectivity excluded"),
        new RepairRecipe("network.wired-authentication", 1, "Wired AutoConfig availability", RepairKind.WiredAutoConfig, [], "Wired service running state; 802.1X authentication and connectivity excluded"),
        new RepairRecipe("devices.bluetooth-support", 1, "Bluetooth Support Service availability", RepairKind.BluetoothSupport, [], "Bluetooth service running state; discovery, pairing and device operation excluded"),
        new RepairRecipe("devices.camera-frame-server", 1, "Windows Camera Frame Server availability", RepairKind.CameraFrameServer, [], "Camera service running state; capture, permissions and physical operation excluded"),
        new RepairRecipe("devices.smart-card-service", 1, "Smart Card service availability", RepairKind.SmartCardService, [], "Smart Card service running state; reader operation, card detection and authentication excluded"),
        new RepairRecipe("devices.biometric-service", 1, "Windows Biometric Service availability", RepairKind.BiometricService, [], "Biometric service running state; sensor capture, matching and sign-in excluded"),
        new RepairRecipe("network.smb-server", 1, "Windows SMB server availability", RepairKind.ServerService, [], "Server service running state; share access, authentication and data transfer excluded"),
        new RepairRecipe("tasks.scheduler-service", 1, "Task Scheduler service availability", RepairKind.TaskSchedulerService, [], "Task Scheduler running state; individual task execution and outcomes excluded"),
        new RepairRecipe("windows.recovery-environment", 1, "Windows Recovery Environment", RepairKind.WindowsRecoveryEnvironment, [], "REAgentC reports enabled; recovery boot, image integrity and repair operation excluded"),
        new RepairRecipe("diagnostics.policy-service", 1, "Diagnostic Policy Service availability", RepairKind.DiagnosticPolicyService, [], "Diagnostic Policy Service running state; troubleshooter detection and repair excluded"),
        new RepairRecipe("diagnostics.error-reporting-service", 1, "Windows Error Reporting availability", RepairKind.WindowsErrorReportingService, [], "Windows Error Reporting running state; report submission and crash diagnosis excluded"),
        new RepairRecipe("events.collector-service", 1, "Windows Event Collector availability", RepairKind.WindowsEventCollectorService, [], "Windows Event Collector running state; remote delivery and subscription health excluded"),
        new RepairRecipe("network.remote-access-service", 1, "Remote Access Connection Manager availability", RepairKind.RemoteAccessConnectionManagerService, [], "Remote Access Connection Manager running state; authentication and connectivity excluded"),
        new RepairRecipe("remote.desktop-services", 1, "Remote Desktop Services availability", RepairKind.RemoteDesktopServices, [], "Remote Desktop Services running state; listener reachability, authentication and sessions excluded"),
        new RepairRecipe("remote.winrm-service", 1, "Windows Remote Management availability", RepairKind.WindowsRemoteManagement, [], "WinRM running state; listener reachability, authentication and remote execution excluded"),
        new RepairRecipe("security.center-service", 1, "Windows Security Center availability", RepairKind.SecurityCenterService, [], "Security Center running state; security-product health and protection excluded"),
        new RepairRecipe("network.connections-service", 1, "Network Connections service availability", RepairKind.NetworkConnectionsService, [], "Network Connections service running state; adapter operation and connectivity excluded"),
        new RepairRecipe("network.winhttp-autoproxy-service", 1, "WinHTTP Web Proxy Auto-Discovery availability", RepairKind.WinHttpAutoProxyService, [], "WinHTTP Auto-Proxy service running state; WPAD, PAC, proxy reachability, authentication and application use excluded"),
        new RepairRecipe("network.connection-manager-service", 1, "Windows Connection Manager availability", RepairKind.WindowsConnectionManagerService, [], "Windows Connection Manager running state; connection selection, authentication and connectivity excluded"),
        new RepairRecipe("network.setup-service", 1, "Network Setup Service availability", RepairKind.NetworkSetupService, [], "Network Setup Service running state; pending setup completion and connectivity excluded"),
        new RepairRecipe("applications.readiness-service", 1, "App Readiness service availability", RepairKind.AppReadinessService, [], "App Readiness running state; sign-in, provisioning and application launch excluded"),
        new RepairRecipe("applications.state-repository-service", 1, "State Repository Service availability", RepairKind.StateRepositoryService, [], "State Repository running state; repository consistency and application operation excluded"),
        new RepairRecipe("profiles.user-profile-service", 1, "User Profile Service availability", RepairKind.UserProfileService, [], "User Profile Service running state; sign-in, profile loading and user-data access excluded"),
        new RepairRecipe("security.credential-manager-service", 1, "Credential Manager service availability", RepairKind.CredentialManagerService, [], "Credential Manager running state; credential retrieval, authentication and application access excluded"),
        new RepairRecipe("devices.shell-hardware-detection-service", 1, "Shell Hardware Detection availability", RepairKind.ShellHardwareDetectionService, [], "Shell Hardware Detection running state; notification delivery and device operation excluded"),
        new RepairRecipe("devices.install-service", 1, "Device Install Service availability", RepairKind.DeviceInstallService, [], "Device Install Service running state; device installation and operation excluded"),
        new RepairRecipe("applications.appx-deployment-service", 1, "AppX Deployment Service availability", RepairKind.AppxDeploymentService, [], "AppX Deployment Service running state; package deployment, registration and application launch excluded"),
        new RepairRecipe("applications.client-license-service", 1, "Client License Service availability", RepairKind.ClientLicenseService, [], "Client License Service running state; license validation and application launch excluded"),
        new RepairRecipe("applications.store-install-service", 1, "Microsoft Store Install Service availability", RepairKind.StoreInstallService, [], "Store Install Service running state; download, installation, license and application outcomes excluded"),
        new RepairRecipe("applications.license-manager-service", 1, "Windows License Manager Service availability", RepairKind.WindowsLicenseManagerService, [], "Windows License Manager running state; license validation, entitlement and application launch excluded"),
        new RepairRecipe("applications.push-to-install-service", 1, "Windows PushToInstall Service availability", RepairKind.PushToInstallService, [], "Windows PushToInstall running state; request processing, download, installation and application outcomes excluded"),
        new RepairRecipe("accounts.web-account-manager-service", 1, "Web Account Manager availability", RepairKind.WebAccountManagerService, [], "Web Account Manager running state; authentication, token issuance and application access excluded"),
        new RepairRecipe("accounts.microsoft-account-sign-in-service", 1, "Microsoft Account Sign-in Assistant availability", RepairKind.MicrosoftAccountSignInService, [], "Microsoft Account Sign-in Assistant running state; authentication, account connectivity and application access excluded"),
        new RepairRecipe("storage.service", 1, "Storage Service availability", RepairKind.StorageService, [], "Storage Service running state; device, volume and storage-policy operation excluded"),
        new RepairRecipe("storage.optimize-drives-service", 1, "Optimize Drives service availability", RepairKind.OptimizeDrivesService, [], "Optimize Drives running state; analysis, trim, defragmentation, optimization and device health excluded"),
        new RepairRecipe("devices.image-acquisition-service", 1, "Windows Image Acquisition availability", RepairKind.WindowsImageAcquisitionService, [], "Windows Image Acquisition running state; scanner, camera, transfer and application operation excluded"),
        new RepairRecipe("network.function-discovery-provider-host", 1, "Function Discovery Provider Host availability", RepairKind.FunctionDiscoveryProviderHostService, [], "Function Discovery Provider Host running state; provider health, device discovery and connectivity excluded"),
        new RepairRecipe("network.function-discovery-resource-publication", 1, "Function Discovery Resource Publication availability", RepairKind.FunctionDiscoveryResourcePublicationService, [], "Function Discovery Resource Publication running state; publication, visibility, access and connectivity excluded"),
        new RepairRecipe("devices.human-interface-service", 1, "Human Interface Device Service availability", RepairKind.HumanInterfaceDeviceService, [], "Human Interface Device Service running state; buttons, controls and application behavior excluded"),
        new RepairRecipe("devices.portable-device-enumerator", 1, "Portable Device Enumerator Service availability", RepairKind.PortableDeviceEnumeratorService, [], "Portable Device Enumerator running state; enumeration, access, synchronization and transfer excluded"),
        new RepairRecipe("display.font-cache-service", 1, "Windows Font Cache Service availability", RepairKind.WindowsFontCacheService, [], "Windows Font Cache running state; text rendering and application behavior excluded"),
        new RepairRecipe("devices.association-service", 1, "Device Association Service availability", RepairKind.DeviceAssociationService, [], "Device Association Service running state; discovery, pairing, association and device operation excluded"),
        new RepairRecipe("input.text-management-service", 1, "Text Input Management Service availability", RepairKind.TextInputManagementService, [], "Text Input Management Service running state; text entry, recognition and device operation excluded"),
        new RepairRecipe("display.enhancement-service", 1, "Display Enhancement Service availability", RepairKind.DisplayEnhancementService, [], "Display Enhancement Service running state; enhancement behavior, picture quality and physical display operation excluded"),
        new RepairRecipe("notifications.com-event-system", 1, "COM+ Event System availability", RepairKind.ComPlusEventSystem, [], "COM+ Event System running state; event publication and notification delivery excluded"),
        new RepairRecipe("notifications.system-event-notification", 1, "System Event Notification Service availability", RepairKind.SystemEventNotificationService, ["notifications.com-event-system"], "System Event Notification Service running state; end-to-end notification delivery excluded"),
        new RepairRecipe("core.power-service", 1, "Power service availability", RepairKind.PowerService, [], "Power service running state; sleep, resume and hardware power behavior excluded"),
        new RepairRecipe("core.plug-and-play-service", 1, "Plug and Play service availability", RepairKind.PlugAndPlayService, [], "Plug and Play running state; device detection and operation excluded"),
        new RepairRecipe("core.user-manager-service", 1, "User Manager service availability", RepairKind.UserManagerService, [], "User Manager running state; sign-in and account operation excluded"),
        new RepairRecipe("core.group-policy-client-service", 1, "Group Policy Client availability", RepairKind.GroupPolicyClientService, [], "Group Policy Client running state; policy application excluded"),
        new RepairRecipe("core.network-store-interface-service", 1, "Network Store Interface availability", RepairKind.NetworkStoreInterfaceService, [], "Network Store Interface running state; notification delivery and connectivity excluded"),
        new RepairRecipe("core.rpc-endpoint-mapper", 1, "RPC Endpoint Mapper availability", RepairKind.RpcEndpointMapperService, [], "RPC Endpoint Mapper running state; endpoint resolution and dependent application behavior excluded"),
        new RepairRecipe("core.remote-procedure-call", 1, "Remote Procedure Call availability", RepairKind.RemoteProcedureCallService, ["core.rpc-endpoint-mapper"], "Remote Procedure Call running state; RPC calls and dependent application behavior excluded"),
        new RepairRecipe("core.dcom-launcher", 1, "DCOM Server Process Launcher availability", RepairKind.DcomServerProcessLauncherService, ["core.remote-procedure-call"], "DCOM Server Process Launcher running state; component activation and dependent application behavior excluded"),
        new RepairRecipe("core.background-tasks-infrastructure", 1, "Background Tasks Infrastructure availability", RepairKind.BackgroundTasksInfrastructureService, ["core.remote-procedure-call"], "Background Tasks Infrastructure running state; background task execution and application behavior excluded"),
        new RepairRecipe("core.messaging", 1, "CoreMessaging availability", RepairKind.CoreMessagingService, ["core.remote-procedure-call"], "CoreMessaging running state; message delivery and dependent application behavior excluded")
    ]);

    public static IReadOnlyList<RepairRecipe> ForWorkflow(WorkflowKind kind)
    {
        var recipes = Workflows.Scopes(kind).Select(action => All.Single(r => r.Action == action)).ToArray();
        if (kind is WorkflowKind.WindowsUpdate or WorkflowKind.CommonProblems)
        {
            int files = Array.FindIndex(recipes, r => r.Action == RepairKind.SystemFiles);
            recipes[files] = recipes[files] with { Dependencies = Array.AsReadOnly(ComponentStoreDependency) };
        }
        return recipes;
    }
}

public static class RecipeEngine
{
    public static async Task<IReadOnlyList<RecipeResult>> RunAsync(IReadOnlyList<RepairRecipe> recipes,
        bool autoRepair, string journalFolder, IWorkflowHost host, IProgress<string>? progress = null,
        Func<IReadOnlyList<RecipeResult>, Task>? checkpoint = null, TimeSpan? checkpointTimeout = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(recipes);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan saveLimit = checkpointTimeout ?? TimeSpan.FromSeconds(15);
        if (saveLimit <= TimeSpan.Zero || saveLimit > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(checkpointTimeout));
        async Task SaveAsync(IReadOnlyList<RecipeResult> snapshot)
        {
            if (checkpoint is null) return;
            var pending = checkpoint(snapshot)
                ?? throw new InvalidOperationException("The checkpoint provider returned no task.");
            await pending.WaitAsync(saveLimit).ConfigureAwait(false);
        }
        progress = progress is null ? null : new DiagnosticProgress(progress);
        recipes = [.. recipes.Select(recipe => recipe is null || recipe.Dependencies is null
            ? throw new ArgumentException("Recipe definitions and dependency lists cannot be null.", nameof(recipes))
            : recipe with { Dependencies = [.. recipe.Dependencies] })];
        Validate(recipes);
        var results = new List<RecipeResult>();
        for (int recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
        {
            var recipe = recipes[recipeIndex];
            progress?.Report($"Repair check {recipeIndex + 1} of {recipes.Count}: {recipe.Title}...");
            if (token.IsCancellationRequested)
                results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Canceled, null, null, "Stopped before this recipe."));
            else if (results.Any(result => result.ExecutionUncertain))
                results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Blocked, null, null,
                    "An earlier repair ended without an execution result. Machine changes may have occurred; inspect its recovery journal before continuing."));
            else if (results.Any(r => r.Repair is not null && r.Repair.Kind != r.Check?.Repair))
                results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Blocked, null, null, "An earlier provider returned an unexpected repair identity. Review its journal before continuing."));
            else if (results.Any(r => r.Check?.Evidence?.TimedOut == true || r.Repair?.Steps.Any(s => s.Result.TimedOut) == true))
                results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Blocked, null, null, "An earlier command timed out. Windows may still be completing work; reassess before continuing."));
            else if (results.Any(r => r.Repair?.RequiresRestart == true))
                results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Blocked, null, null, "Windows requested a restart. Restart when convenient, then run this workflow again to verify and continue."));
            else if (recipe.Dependencies.Any(id => !results.Any(r => r.RecipeId == id && r.Outcome is RecipeOutcome.Clear or RecipeOutcome.Repaired)))
            {
                var blockers = results.Where(result => recipe.Dependencies.Contains(result.RecipeId) &&
                    result.Outcome is not (RecipeOutcome.Clear or RecipeOutcome.Repaired)).ToArray();
                results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Blocked, null, null,
                    "Waiting for prerequisites: " + string.Join("; ", blockers.Select(result =>
                        recipes.Single(definition => definition.Id == result.RecipeId).Title + " (" + result.Outcome + ")")) +
                    ". Resolve those results, then run this workflow again.")
                { BlockingRecipes = Array.AsReadOnly(blockers.Select(result => result.RecipeId).ToArray()) });
            }
            else
            {
                WorkflowCheck? check = null;
                RepairResult? repair = null;
                bool repairAttempted = false;
                bool incompleteRepairEvidence = false;
                bool savingCheckpoint = false;
                try
                {
                    progress?.Report("Checking " + recipe.Title + "...");
                    check = await InspectAsync(recipe, host, progress, token).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("The diagnostic provider returned no result.");
                    if (check.EffectiveState != check.State || check.Evidence is { TimedOut: true } or { Truncated: true } || !string.IsNullOrWhiteSpace(check.Evidence?.Error))
                        check = check with { State = CheckState.Unknown, Repair = null, Detail = "Diagnostic evidence is incomplete. No automatic repair selected; collect a complete result before proceeding." };
                    progress?.Report(check.Name + ": " + check.Detail);
                    token.ThrowIfCancellationRequested();
                    var outcome = check.State switch
                    {
                        CheckState.Clear => RecipeOutcome.Clear,
                        CheckState.Information when recipe.DiagnosticOnly => RecipeOutcome.Clear,
                        CheckState.Repairable when check.Repair == recipe.Action => RecipeOutcome.RepairAvailable,
                        CheckState.Attention => RecipeOutcome.NeedsAttention,
                        _ => RecipeOutcome.Unknown
                    };
                    if (recipe.DiagnosticOnly && outcome == RecipeOutcome.RepairAvailable) outcome = RecipeOutcome.NeedsAttention;
                    if (autoRepair && recipe.CanAutoExecute && outcome == RecipeOutcome.RepairAvailable)
                    {
                        // Persist the diagnosis before a mutation can start.
                        if (checkpoint is not null)
                        {
                            savingCheckpoint = true;
                            await SaveAsync([.. results.Append(new RecipeResult(recipe.Id, recipe.Version, outcome, check, null, "Repair selected; execution pending."))]).ConfigureAwait(false);
                            savingCheckpoint = false;
                        }
                        token.ThrowIfCancellationRequested();
                        progress?.Report("Repairing " + recipe.Title + "...");
                        repairAttempted = true;
                        using var executionDeadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                        executionDeadline.CancelAfter(recipe.ExecutionTimeout);
                        var pendingRepair = host.RepairAsync(recipe.Action, journalFolder, progress, executionDeadline.Token)
                            ?? throw new InvalidOperationException("The repair provider returned no task.");
                        try { repair = await pendingRepair.WaitAsync(executionDeadline.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) when (!token.IsCancellationRequested && executionDeadline.IsCancellationRequested)
                        { throw new TimeoutException("Repair execution deadline expired. Provider work may still be running; review the recovery journal before continuing."); }
                        if (repair is null) throw new InvalidOperationException("The repair provider returned no result.");
                        bool completedRecoveryReview = repair.Kind == RepairKind.DnsCache &&
                            repair.DnsCacheReconciliation is not null && repair.Steps is { Count: 0 };
                        if (repair.Steps is null || (!completedRecoveryReview && repair.Steps.Count == 0) ||
                            repair.Steps.Any(step => step is null || step.Step is null || step.Result is null))
                        {
                            incompleteRepairEvidence = true;
                            throw new InvalidOperationException("The repair provider returned incomplete command evidence. Review its journal before continuing.");
                        }
                        if (repair.Kind != recipe.Action)
                        {
                            results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Unknown, check, repair,
                                "The provider returned a different repair identity. The requested repair is unverified; review the retained result and journal."));
                            progress?.Report(recipe.Title + ": " + results[^1].Outcome + ". " + results[^1].Detail);
                            if (checkpoint is not null)
                            {
                                savingCheckpoint = true;
                                await SaveAsync([.. results]).ConfigureAwait(false);
                                savingCheckpoint = false;
                            }
                            continue;
                        }
                        outcome = repair.RequiresRestart ? RecipeOutcome.RestartRequired : repair.Kind == recipe.Action && Repairs.IsVerified(repair) ? RecipeOutcome.Repaired : RecipeOutcome.NeedsAttention;
                    }
                    results.Add(new(recipe.Id, recipe.Version, outcome, check, repair, repair?.Status ?? check.Detail));
                }
                catch (OperationCanceledException) when (!savingCheckpoint && token.IsCancellationRequested)
                { results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Canceled, check, repair, "Recipe stopped; verification may be incomplete.")); }
                catch (OperationCanceledException) when (!savingCheckpoint)
                { results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Unknown, check, repair, "The operation ended before a result was available. Verification is incomplete.")); }
                catch (Exception e) when (!savingCheckpoint && e is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception or TimeoutException)
                { results.Add(new(recipe.Id, recipe.Version, RecipeOutcome.Unknown, check, repair, e.Message)); }
                if (repairAttempted && (repair is null || incompleteRepairEvidence))
                    results[^1] = results[^1] with { ExecutionUncertain = true,
                        Detail = results[^1].Detail + " Repair execution was attempted without complete command evidence; machine changes may have occurred." };
            }
            progress?.Report(recipe.Title + ": " + results[^1].Outcome + ". " + results[^1].Detail);
            if (checkpoint is not null) await SaveAsync([.. results]).ConfigureAwait(false);
        }
        return [.. results];
    }

    private static void Validate(IReadOnlyList<RepairRecipe> recipes)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var recipe in recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.Id) || recipe.Version < 1 || seen.Contains(recipe.Id) ||
                recipe.Dependencies.Any(id => !seen.Contains(id)) || !Enum.IsDefined(recipe.Action) ||
                recipe.DiagnosticOnly && recipe.RequiresExplicitOptions ||
                !recipe.DiagnosticOnly && recipe.RecoveryContract == RepairRecoveryContract.None ||
                recipe.InspectionTimeout <= TimeSpan.Zero || recipe.InspectionTimeout > TimeSpan.FromHours(2) ||
                recipe.ExecutionTimeout <= TimeSpan.Zero || recipe.ExecutionTimeout > TimeSpan.FromHours(4))
                throw new ArgumentException("Recipe IDs must be unique, versioned, and ordered after their dependencies.");
            seen.Add(recipe.Id);
        }
    }

    private static async Task<WorkflowCheck> InspectAsync(RepairRecipe recipe, IWorkflowHost host,
        IProgress<string>? progress, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(recipe.InspectionTimeout);
        try
        {
            var pending = host.CheckIntegrityAsync(recipe.Action, progress, deadline.Token)
                ?? throw new InvalidOperationException("The diagnostic provider returned no task.");
            return await pending.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return new(recipe.Title, CheckState.Unknown,
                "The diagnostic deadline expired. Provider work may still be running; no repair selected. Reassess before continuing.",
                Evidence: new ToolResult(-1, "", "Diagnostic deadline expired.", false) { TimedOut = true });
        }
    }
}
