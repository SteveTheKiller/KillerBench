namespace KillerBench.Engine.Workflows;

public enum CommonDiagnosticScope { DeviceProblems, Vitals, PrinterDrivers, Security, TimeConfiguration, StorageHistory, InstallerHistory, PrintingHistory, NetworkConfiguration, ProxyConfiguration, ApplicationHistory, ServiceHistory, TimeHistory, SystemVolumeCapacity, ShutdownHistory, BackupHistory, VssWriters, BackgroundTransfers, PrinterPorts, AppPackages, ScheduledTasks, PrintJobs, StorageReliability, WindowsLicensing, EventChannels, ReliabilityHistory, HardwareErrorHistory, DriverInstallHistory, CrashEvidence, FirewallProfiles, WindowsUpdateContext, SystemVolumeDirtyBit, InstallerActivity, SearchHealth, SystemRestore, WindowsRecovery, Certificates, RestartState, ServicingTransitions, NetworkPathReadiness, StorageReadiness, PrintingReadiness, InstallationReadiness, ServiceReadiness, WindowsUpdateReadiness, BackupReadiness, SystemStabilityReadiness, SecurityReadiness, ApplicationDeploymentReadiness }

public static class CommonDiagnostics
{
    internal static IReadOnlyList<CommonDiagnosticScope> SelectScopes(IReadOnlyCollection<CommonDiagnosticScope>? selectedScopes)
    {
        var requested = selectedScopes?.ToArray();
        if (requested is not null && (requested.Length == 0 || requested.Length > 64 || requested.Any(scope => !Enum.IsDefined(scope))))
            throw new ArgumentException("Select one or more supported diagnostic scopes, up to 64 selections.", nameof(selectedScopes));
        return Array.AsReadOnly(Enum.GetValues<CommonDiagnosticScope>().Where(scope => requested is null || requested.Contains(scope)).ToArray());
    }
    public static Task<IReadOnlyList<WorkflowCheck>> CheckAsync(IProgress<string>? progress = null,
        Func<CommonDiagnosticScope, CancellationToken, Task<IReadOnlyList<WorkflowCheck>>>? collect = null,
        Func<IReadOnlyList<WorkflowCheck>, Task>? checkpoint = null,
        TimeSpan? scopeTimeout = null, IReadOnlyCollection<CommonDiagnosticScope>? selectedScopes = null,
        KnowledgeCatalogCollection? knowledgeCatalogs = null, CancellationToken token = default) =>
        CheckAsync(TimeSpan.FromSeconds(15), progress, collect, checkpoint, scopeTimeout, selectedScopes, knowledgeCatalogs, token: token);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(TimeSpan checkpointTimeout, IProgress<string>? progress = null,
        Func<CommonDiagnosticScope, CancellationToken, Task<IReadOnlyList<WorkflowCheck>>>? collect = null,
        Func<IReadOnlyList<WorkflowCheck>, Task>? checkpoint = null,
        TimeSpan? scopeTimeout = null, IReadOnlyCollection<CommonDiagnosticScope>? selectedScopes = null,
        KnowledgeCatalogCollection? knowledgeCatalogs = null, CancellationToken token = default)
    {
        if (checkpointTimeout <= TimeSpan.Zero || checkpointTimeout > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(checkpointTimeout));
        var limit = scopeTimeout ?? TimeSpan.FromMinutes(2);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(scopeTimeout));
        var scopes = SelectScopes(selectedScopes);
        var completed = new Dictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>>();
        bool usingDefaultCollector = collect is null;
        collect ??= (scope, innerToken) => CollectAsync(scope, knowledgeCatalogs, completed, token: innerToken);
        progress = progress is null ? null : new DiagnosticProgress(progress);
        var findings = new List<WorkflowCheck>();
        for (int scopeIndex = 0; scopeIndex < scopes.Count; scopeIndex++)
        {
            var scope = scopes[scopeIndex];
            token.ThrowIfCancellationRequested();
            string label = CommonDiagnosticCatalog.Get(scope).Title;
            int firstFinding = findings.Count;
            progress?.Report($"Diagnostic area {scopeIndex + 1} of {scopes.Count}: {label}...");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(limit);
            try
            {
                var pending = collect(scope, deadline.Token) ?? throw new InvalidOperationException("Collector returned no task.");
                var checks = await pending.WaitAsync(deadline.Token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (checks is null || checks.Count == 0 || checks.Any(check => check is null))
                    throw new InvalidOperationException("Collector returned no usable coverage result.");
                if (usingDefaultCollector) completed[scope] = checks;
                foreach (var check in checks.Select(check => check with
                {
                    Repair = null,
                    State = check.Evidence is { TimedOut: true } or { Truncated: true } || !string.IsNullOrWhiteSpace(check.Evidence?.Error) ||
                        (check.State == CheckState.Clear && check.Evidence is { ExitCode: not 0 })
                        ? CheckState.Unknown : check.State == CheckState.Repairable ? CheckState.Attention : check.State
                }))
                    if (!findings.Contains(check)) findings.Add(check);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException
                or NotSupportedException or TimeoutException or System.Security.SecurityException
                or System.ComponentModel.Win32Exception or OperationCanceledException)
            {
                findings.Add(new(scope.ToString(), CheckState.Unknown,
                    "Collection was unavailable or incomplete. Other scopes are inspected independently; no repair selected from this snapshot.",
                    Evidence: new ToolResult(-1, "", error.GetType().Name + ": " + error.Message, false)
                        { TimedOut = deadline.IsCancellationRequested || error is TimeoutException }));
            }
            if (checkpoint is not null)
            {
                token.ThrowIfCancellationRequested();
                var save = checkpoint(Array.AsReadOnly(findings.ToArray()))
                    ?? throw new InvalidOperationException("The diagnostic checkpoint provider returned no task.");
                await save.WaitAsync(checkpointTimeout, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
            var collected = findings.Skip(firstFinding).ToArray();
            progress?.Report(label + ": " + collected.Length + " findings; " +
                collected.Count(check => check.State is CheckState.Attention or CheckState.Unknown) + " require review.");
            foreach (var finding in collected.OrderByDescending(check => check.State is CheckState.Attention or CheckState.Unknown).Take(20))
            {
                string detail = finding.Detail.Length <= 2000 ? finding.Detail : finding.Detail[..2000] + " [shortened; full finding in journal]";
                progress?.Report(finding.Name + ": " + finding.State + "\n" + detail);
            }
            if (collected.Length > 20) progress?.Report("Additional findings are retained in the workflow journal and final report.");
        }
        return findings.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectAsync(CommonDiagnosticScope scope, KnowledgeCatalogCollection? knowledgeCatalogs,
        IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed, CancellationToken token) => scope switch
    {
        CommonDiagnosticScope.DeviceProblems => await DeviceDiagnostics.CheckAsync(DeviceCategory.Problems, knowledgeCatalogs, token).ConfigureAwait(false),
        CommonDiagnosticScope.Vitals => await CollectVitalsAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.PrinterDrivers => await PrinterDriverDiagnostics.CheckAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.PrinterPorts => await PrinterPortDiagnostics.CheckAsync(token: token).ConfigureAwait(false),
        CommonDiagnosticScope.PrintJobs => await PrintJobDiagnostics.CheckAsync(token: token).ConfigureAwait(false),
        CommonDiagnosticScope.AppPackages => AppPackageDiagnostics.Describe(await AppPackageInventory.ReadAsync(token: token).ConfigureAwait(false)),
        CommonDiagnosticScope.Security => await CollectSecurityAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.TimeConfiguration => [await TimeConfigurationDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.StorageHistory => [await StorageEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.StorageReliability => await StorageReliability.CheckAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.InstallerHistory => [await InstallerEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.PrintingHistory => await CollectPrintingHistoryAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.NetworkConfiguration => NetworkSnapshot.Check(token),
        CommonDiagnosticScope.ProxyConfiguration => ProxyDiagnostics.Check(),
        CommonDiagnosticScope.ApplicationHistory => [await ApplicationEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.ServiceHistory => [await ServiceEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.TimeHistory => [await TimeServiceEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.SystemVolumeCapacity => [VolumeCapacity.Check(StorageDiagnostics.SystemVolume())],
        CommonDiagnosticScope.ShutdownHistory => [await ShutdownEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.BackupHistory => await BackupEvents.CheckAsync(token: token).ConfigureAwait(false),
        CommonDiagnosticScope.VssWriters => [await VssWriters.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.BackgroundTransfers => [await BitsDiagnostics.CheckAsync(token: token).ConfigureAwait(false)],
        CommonDiagnosticScope.ScheduledTasks => await ScheduledTaskDiagnostics.CheckAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.WindowsLicensing => [await WindowsLicensingDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.EventChannels => await EventLogCapacityDiagnostics.CheckAsync(token: token).ConfigureAwait(false),
        CommonDiagnosticScope.ReliabilityHistory => await ReliabilityHistory.CheckAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.HardwareErrorHistory => [await HardwareErrorEvents.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.DriverInstallHistory => await SetupApiDriverInstall.CheckAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.CrashEvidence => await CollectCrashEvidenceAsync(knowledgeCatalogs, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.FirewallProfiles => await CollectFirewallConfigurationAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.WindowsUpdateContext => await CollectWindowsUpdateContextAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.SystemVolumeDirtyBit => [await VolumeDirtyStatus.CheckAsync(StorageDiagnostics.SystemVolume(), token).ConfigureAwait(false)],
        CommonDiagnosticScope.InstallerActivity => [InstallerLoggingDiagnostics.Check(), await InstallerActivityDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.SearchHealth => await CollectSearchHealthAsync(token).ConfigureAwait(false),
        CommonDiagnosticScope.SystemRestore => [await SystemRestoreDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.WindowsRecovery => [await WindowsRecoveryDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        CommonDiagnosticScope.Certificates =>
        [
            await CertificateDiagnostics.CheckAsync(token).ConfigureAwait(false),
            await CertificateChainDiagnostics.CheckAsync(token).ConfigureAwait(false)
        ],
        CommonDiagnosticScope.RestartState => [RestartDiagnostics.Check(), PendingFileOperationDiagnostics.Check()],
        CommonDiagnosticScope.ServicingTransitions => await CollectServicingTransitionsAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.NetworkPathReadiness => await CollectNetworkPathReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.StorageReadiness => await CollectStorageReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.PrintingReadiness => await CollectPrintingReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.InstallationReadiness => await CollectInstallationReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.ServiceReadiness => await CollectServiceReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.WindowsUpdateReadiness => await CollectWindowsUpdateReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.BackupReadiness => await CollectBackupReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.SystemStabilityReadiness => await CollectSystemStabilityReadinessAsync(knowledgeCatalogs, completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.SecurityReadiness => await CollectSecurityReadinessAsync(completed, token: token).ConfigureAwait(false),
        CommonDiagnosticScope.ApplicationDeploymentReadiness => await CollectApplicationDeploymentReadinessAsync(completed, token: token).ConfigureAwait(false),
        _ => throw new ArgumentOutOfRangeException(nameof(scope))
    };

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectVitalsAsync(CancellationToken token)
    {
        List<WorkflowCheck> checks = [.. await VitalsDiagnostics.CheckAsync(token).ConfigureAwait(false),
            await MemoryDiagnosticResults.CheckAsync(token).ConfigureAwait(false)];
        return checks.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectSecurityAsync(CancellationToken token)
    {
        var checks = new List<WorkflowCheck>(await SecurityDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        checks.AddRange(UacPolicyDiagnostics.Check());
        checks.AddRange(await AppLockerPolicyDiagnostics.CheckAsync(token).ConfigureAwait(false));
        checks.AddRange(await DefenderProtectionDiagnostics.CheckAsync(token).ConfigureAwait(false));
        return checks.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectServicingTransitionsAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var packages = await ServicingPackageDiagnostics.CheckAsync(token).ConfigureAwait(false);
        var features = await OptionalFeatureDiagnostics.CheckAsync(token).ConfigureAwait(false);
        var capabilities = await WindowsCapabilityDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
        var restart = Completed(completed, CommonDiagnosticScope.RestartState)?.FirstOrDefault(check => check.Name == "Restart") ?? RestartDiagnostics.Check();
        return WithReadinessSummary(CommonDiagnosticScope.ServicingTransitions,
            [packages, features, capabilities, restart, ServicingTransitionCorrelation.Check(packages, features, restart, token)], token);
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectNetworkPathReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        checks.AddRange(Completed(completed, CommonDiagnosticScope.NetworkConfiguration) ?? NetworkSnapshot.Check(token));
        var ipAddresses = await IpAddressDiagnostics.CheckAsync(token).ConfigureAwait(false);
        checks.AddRange(ipAddresses);
        var profileTask = NetworkProfileDiagnostics.CheckAsync(token);
        var ncsiPolicyTask = NcsiPolicyDiagnostics.CheckAsync(token: token);
        var firewallTask = Completed(completed, CommonDiagnosticScope.FirewallProfiles) is { } savedFirewalls
            ? Task.FromResult(savedFirewalls) : FirewallProfiles.CheckAsync(token);
        var firewallEventsTask = FirewallEvents.CheckAsync(token: token);
        var firewallServiceEventsTask = FirewallServiceFailureEvents.CheckAsync(token: token);
        var networkServiceEventsTask = NetworkServiceFailureEvents.CheckAsync(token: token);
        var dnsServiceTask = ServiceInspection.ReadAsync([RepairKind.DnsClient], token: token);
        var dhcpServiceTask = ServiceInspection.ReadAsync([RepairKind.DhcpClient], token: token);
        var dnsTask = DnsServerDiagnostics.CheckAsync(token);
        var dnsClientTask = DnsClientConfigurationDiagnostics.CheckAsync(token);
        var dnsPolicyTask = DnsNrptPolicyDiagnostics.CheckAsync(token: token);
        var dnsCacheTask = DnsClientCacheDiagnostics.CheckAsync(token: token);
        var dnsEventsTask = DnsClientEvents.CheckAsync(token: token);
        var routeTask = RouteDiagnostics.CheckAsync(token);
        var routePriorityTask = RoutePriorityDiagnostics.CheckAsync(token: token);
        var interfaceConfigurationTask = IpInterfaceConfigurationDiagnostics.CheckAsync(token: token);
        var dhcpLeaseTask = DhcpLeaseDiagnostics.CheckAsync(token: token);
        var dhcpEventsTask = DhcpClientEvents.CheckAsync(token: token);
        var neighborCacheTask = NeighborCacheDiagnostics.CheckAsync(token: token);
        var adapterBindingTask = AdapterBindingDiagnostics.CheckAsync(token: token);
        var adapterStatisticsTask = AdapterStatisticsDiagnostics.CheckAsync(token: token);
        var tcpPortPressureTask = TcpPortPressureDiagnostics.CheckAsync(token: token);
        var tcpSettingTask = TcpSettingDiagnostics.CheckAsync(token: token);
        var networkOffloadTask = NetworkOffloadDiagnostics.CheckAsync(token: token);
        var winsockCatalogTask = WinsockCatalogDiagnostics.CheckAsync(token: token);
        var winsockNamespaceTask = WinsockNamespaceDiagnostics.CheckAsync(token: token);
        var winsockProviderFileTask = WinsockProviderFileDiagnostics.CheckAsync(token: token);
        var hostsTask = HostsFileDiagnostics.CheckAsync(token);
        var profiles = await profileTask.ConfigureAwait(false);
        checks.AddRange(profiles);
        checks.Add(await ncsiPolicyTask.ConfigureAwait(false));
        var firewalls = await firewallTask.ConfigureAwait(false);
        checks.AddRange(firewalls);
        checks.Add(await firewallEventsTask.ConfigureAwait(false));
        checks.AddRange(await firewallServiceEventsTask.ConfigureAwait(false));
        checks.AddRange(await networkServiceEventsTask.ConfigureAwait(false));
        var firewallCorrelation = NetworkFirewallCorrelation.Check(profiles, firewalls, token);
        checks.Add(firewallCorrelation);
        var dnsService = (await dnsServiceTask.ConfigureAwait(false)).Single();
        checks.Add(dnsService);
        var dns = await dnsTask.ConfigureAwait(false);
        checks.Add(dns);
        checks.Add(await dnsClientTask.ConfigureAwait(false));
        var dnsPolicy = await dnsPolicyTask.ConfigureAwait(false);
        checks.Add(dnsPolicy);
        checks.Add(await dnsCacheTask.ConfigureAwait(false));
        var dnsEvents = await dnsEventsTask.ConfigureAwait(false);
        checks.AddRange(dnsEvents);
        var routes = await routeTask.ConfigureAwait(false);
        checks.Add(routes);
        checks.Add(await routePriorityTask.ConfigureAwait(false));
        checks.Add(await interfaceConfigurationTask.ConfigureAwait(false));
        var dhcpService = (await dhcpServiceTask.ConfigureAwait(false)).Single();
        checks.Add(dhcpService);
        var dhcpLease = await dhcpLeaseTask.ConfigureAwait(false);
        checks.Add(dhcpLease);
        var dhcpEvents = await dhcpEventsTask.ConfigureAwait(false);
        checks.AddRange(dhcpEvents);
        var dhcpCorrelation = DhcpOperationalCorrelation.Check(dhcpService, dhcpLease, ipAddresses, dhcpEvents, token);
        checks.Add(dhcpCorrelation);
        checks.Add(await neighborCacheTask.ConfigureAwait(false));
        checks.Add(await adapterBindingTask.ConfigureAwait(false));
        checks.Add(await adapterStatisticsTask.ConfigureAwait(false));
        checks.AddRange(await tcpPortPressureTask.ConfigureAwait(false));
        checks.Add(await tcpSettingTask.ConfigureAwait(false));
        checks.Add(await networkOffloadTask.ConfigureAwait(false));
        var winsockProtocols = await winsockCatalogTask.ConfigureAwait(false);
        var winsockNamespaces = await winsockNamespaceTask.ConfigureAwait(false);
        checks.Add(winsockProtocols);
        checks.Add(winsockNamespaces);
        var winsockProviderFiles = await winsockProviderFileTask.ConfigureAwait(false);
        checks.Add(winsockProviderFiles);
        var winsockCorrelation = WinsockReadinessCorrelation.Check(winsockProtocols, winsockNamespaces, winsockProviderFiles, token);
        var pathCorrelation = NetworkPathCorrelation.Check(routes, dns, token);
        checks.Add(winsockCorrelation);
        checks.Add(pathCorrelation);
        var dnsCorrelation = DnsOperationalCorrelation.Check(dnsService, dns, dnsPolicy, dnsEvents, pathCorrelation, token);
        checks.Add(dnsCorrelation);
        var gateways = await GatewayReachabilityDiagnostics.CheckAsync(routes, token: token).ConfigureAwait(false);
        checks.Add(gateways);
        var connectivityCorrelation = NetworkConnectivityCorrelation.Check(profiles, routes, gateways, token);
        checks.Add(connectivityCorrelation);
        checks.Add(NetworkOperationalCorrelation.Check(firewallCorrelation, pathCorrelation, connectivityCorrelation,
            winsockCorrelation, dnsCorrelation, dhcpCorrelation, token));
        checks.Add(await hostsTask.ConfigureAwait(false));
        checks.AddRange(Completed(completed, CommonDiagnosticScope.ProxyConfiguration) ?? ProxyDiagnostics.Check());
        return WithReadinessSummary(CommonDiagnosticScope.NetworkPathReadiness, checks, token);
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectStorageReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var topologyTask = StorageTopologyDiagnostics.CheckAsync(token);
        var reliabilityTask = Completed(completed, CommonDiagnosticScope.StorageReliability) is { } savedReliability
            ? Task.FromResult(savedReliability) : StorageReliability.CheckAsync(token);
        var spacesTask = StorageSpacesDiagnostics.CheckAsync(token);
        var shadowStorageTask = ShadowStorageDiagnostics.CheckAsync(token: token);
        var historyTask = Completed(completed, CommonDiagnosticScope.StorageHistory)?.SingleOrDefault() is { } savedHistory
            ? Task.FromResult(savedHistory) : StorageEvents.CheckAsync(token);
        var dirtyTask = Completed(completed, CommonDiagnosticScope.SystemVolumeDirtyBit)?.SingleOrDefault() is { } savedDirty
            ? Task.FromResult(savedDirty) : VolumeDirtyStatus.CheckAsync(StorageDiagnostics.SystemVolume(), token);
        var selfHealingTask = NtfsSelfHealingDiagnostics.CheckAsync(StorageDiagnostics.SystemVolume(), token: token);
        var deleteNotificationTask = DeleteNotificationDiagnostics.CheckAsync(token: token);
        var pageFileTask = PageFileDiagnostics.CheckAsync(token: token);
        var diskQuotaTask = DiskQuotaDiagnostics.CheckAsync(token: token);
        var storageSenseTask = StorageSenseTaskDiagnostics.CheckAsync(token);
        var diskCleanupTask = DiskCleanupTaskDiagnostics.CheckAsync(token);
        var diskDiagnosticTask = DiskDiagnosticCorrelation.CheckAsync(token);
        var storageJobTask = StorageJobDiagnostics.CheckAsync(token: token);
        var physicalDiskTask = PhysicalDiskDiagnostics.CheckAsync(token);
        var serviceHistoryTask = StorageServiceFailureEvents.CheckAsync(token: token);
        var topology = await topologyTask.ConfigureAwait(false);
        var history = await historyTask.ConfigureAwait(false);
        var reliability = await reliabilityTask.ConfigureAwait(false);
        var physicalDisks = await physicalDiskTask.ConfigureAwait(false);
        var capacity = StorageVolumeCapacityCorrelation.Check(topology, token);
        var eventCorrelation = StorageEventCorrelation.Check(topology, history, token);
        var dirtyBit = await dirtyTask.ConfigureAwait(false);
        var storageJobs = await storageJobTask.ConfigureAwait(false);
        var physicalCorrelation = PhysicalDiskCorrelation.Check(physicalDisks, reliability, token);
        var serviceHistory = await serviceHistoryTask.ConfigureAwait(false);
        var spaces = await spacesTask.ConfigureAwait(false);
        var shadowStorage = await shadowStorageTask.ConfigureAwait(false);
        var selfHealing = await selfHealingTask.ConfigureAwait(false);
        var deleteNotifications = await deleteNotificationTask.ConfigureAwait(false);
        var pageFile = await pageFileTask.ConfigureAwait(false);
        var diskQuotas = await diskQuotaTask.ConfigureAwait(false);
        var storageSensePolicy = StorageSensePolicyDiagnostics.Check();
        var storageSense = await storageSenseTask.ConfigureAwait(false);
        var diskCleanup = await diskCleanupTask.ConfigureAwait(false);
        var diskDiagnostics = await diskDiagnosticTask.ConfigureAwait(false);
        var storageEnvironment = StorageEnvironmentCorrelation.Check(spaces, shadowStorage, selfHealing,
            deleteNotifications, pageFile, diskQuotas, storageSensePolicy, storageSense, diskCleanup,
            diskDiagnostics.Single(check => check.Name == "Windows disk diagnostic correlation"), token);
        var checks = new List<WorkflowCheck> { topology, capacity, history, eventCorrelation };
        checks.AddRange(reliability);
        checks.AddRange(spaces);
        checks.Add(shadowStorage);
        checks.Add(dirtyBit);
        checks.Add(selfHealing);
        checks.AddRange(deleteNotifications);
        checks.Add(pageFile);
        checks.Add(diskQuotas);
        checks.Add(storageSensePolicy);
        checks.Add(storageSense);
        checks.Add(diskCleanup);
        checks.AddRange(diskDiagnostics);
        checks.Add(storageEnvironment);
        checks.Add(storageJobs);
        checks.AddRange(physicalDisks);
        checks.Add(physicalCorrelation);
        checks.AddRange(serviceHistory);
        checks.Add(StorageOperationalCorrelation.Check(capacity, dirtyBit, storageJobs, physicalCorrelation,
            eventCorrelation, serviceHistory, token));
        return WithReadinessSummary(CommonDiagnosticScope.StorageReadiness, checks, token);
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectPrintingReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var driverTask = Completed(completed, CommonDiagnosticScope.PrinterDrivers) is { } savedDrivers
            ? Task.FromResult(savedDrivers) : PrinterDriverDiagnostics.CheckAsync(token);
        var driverFileTask = PrinterDriverFileDiagnostics.CheckAsync(token: token);
        var driverModelTask = PrinterDriverModelDiagnostics.CheckAsync(token: token);
        var defaultPrinterTask = DefaultPrinterDiagnostics.CheckAsync(token);
        var conditionTask = PrinterConditionDiagnostics.CheckAsync(token: token);
        var attributeTask = PrinterQueueAttributeDiagnostics.CheckAsync(token: token);
        var portTask = Completed(completed, CommonDiagnosticScope.PrinterPorts) is { } savedPorts
            ? Task.FromResult(savedPorts) : PrinterPortDiagnostics.CheckAsync(token: token);
        var endpointTask = PrinterEndpointDiagnostics.CheckAsync(token);
        var extensionTask = PrintExtensionDiagnostics.CheckAsync(token);
        var extensionFileTask = PrintExtensionFileDiagnostics.CheckAsync(token: token);
        var jobTask = Completed(completed, CommonDiagnosticScope.PrintJobs) is { } savedJobs
            ? Task.FromResult(savedJobs) : PrintJobDiagnostics.CheckAsync(token: token);
        var spoolerStateTask = ServiceInspection.ReadAsync([RepairKind.PrintSpooler], token: token);
        var savedPrintingHistory = Completed(completed, CommonDiagnosticScope.PrintingHistory);
        var historyTask = savedPrintingHistory?.SingleOrDefault(check => check.Name == "Recent print-service events") is { } savedPrintHistory
            ? Task.FromResult(savedPrintHistory) : PrintServiceEvents.CheckAsync(token);
        var operationalHistoryTask = savedPrintingHistory?.SingleOrDefault(check => check.Name == "PrintService operational history") is { } savedOperationalHistory
            ? Task.FromResult(savedOperationalHistory) : PrintServiceOperationalEvents.CheckAsync(token: token);
        var spoolerFailureTask = PrintSpoolerFailureEvents.CheckAsync(token: token);
        var spool = PrintSpoolDirectoryDiagnostics.Check();
        var drivers = await driverTask.ConfigureAwait(false);
        var history = await historyTask.ConfigureAwait(false);
        var defaultPrinter = await defaultPrinterTask.ConfigureAwait(false);
        var conditions = await conditionTask.ConfigureAwait(false);
        var attributes = await attributeTask.ConfigureAwait(false);
        var jobs = await jobTask.ConfigureAwait(false);
        var spooler = (await spoolerStateTask.ConfigureAwait(false)).Single();
        var spoolerHistory = await spoolerFailureTask.ConfigureAwait(false);
        var pointAndPrint = PrintPolicyDiagnostics.Check();
        var rendering = PrintRenderingPolicyDiagnostics.Check();
        var driverFiles = await driverFileTask.ConfigureAwait(false);
        var driverModels = await driverModelTask.ConfigureAwait(false);
        var ports = await portTask.ConfigureAwait(false);
        var endpoints = await endpointTask.ConfigureAwait(false);
        var reachability = await PrinterEndpointReachability.CheckAsync(endpoints, token: token).ConfigureAwait(false);
        var extensions = await extensionTask.ConfigureAwait(false);
        var extensionFiles = await extensionFileTask.ConfigureAwait(false);
        var environment = PrintingEnvironmentCorrelation.Check(pointAndPrint, rendering, drivers, driverFiles,
            driverModels, ports, endpoints, reachability, extensions, extensionFiles, token);
        List<WorkflowCheck> checks = [spooler, defaultPrinter, pointAndPrint, rendering];
        checks.AddRange(conditions);
        checks.AddRange(attributes);
        checks.AddRange(drivers);
        checks.AddRange(driverFiles);
        checks.Add(driverModels);
        checks.AddRange(ports);
        checks.AddRange(endpoints);
        checks.AddRange(reachability);
        checks.Add(extensions);
        checks.AddRange(extensionFiles);
        checks.Add(environment);
        checks.AddRange(jobs);
        checks.Add(history);
        checks.Add(await operationalHistoryTask.ConfigureAwait(false));
        checks.Add(spoolerHistory);
        checks.Add(PrintQueueEventCorrelation.Check(drivers, history, token));
        checks.Add(spool);
        checks.Add(PrintingOperationalCorrelation.Check(spooler, defaultPrinter, conditions, attributes, jobs,
            spool, environment, spoolerHistory, token));
        return WithReadinessSummary(CommonDiagnosticScope.PrintingReadiness, checks, token);
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectInstallationReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var historyTask = Completed(completed, CommonDiagnosticScope.InstallerHistory)?.SingleOrDefault() is { } savedHistory
            ? Task.FromResult(savedHistory) : InstallerEvents.CheckAsync(token);
        var activity = Completed(completed, CommonDiagnosticScope.InstallerActivity) is { } savedActivity
            ? savedActivity : [InstallerLoggingDiagnostics.Check(), await InstallerActivityDiagnostics.CheckAsync(token).ConfigureAwait(false)];
        var deploymentTask = AppDeploymentEvents.CheckAsync(token);
        var storeTask = StoreInstallEvents.CheckAsync(token);
        var serviceFailureTask = WindowsInstallerFailureEvents.CheckAsync(token: token);
        var engineFileTask = InstallerEngineFileDiagnostics.CheckAsync(token: token);
        var serviceStateTask = ServiceInspection.ReadAsync([RepairKind.WindowsInstaller], token: token);
        var history = await historyTask.ConfigureAwait(false);
        var pendingOperations = Completed(completed, CommonDiagnosticScope.RestartState)?
            .FirstOrDefault(check => check.Name == "Pending file operations") ?? PendingFileOperationDiagnostics.Check();
        var installerActivity = activity.Single(check => check.Name == "Installer activity");
        var engineFiles = await engineFileTask.ConfigureAwait(false);
        var serviceState = (await serviceStateTask.ConfigureAwait(false)).Single();
        var serviceRegistration = InstallerServiceRegistrationDiagnostics.Check();
        var serviceHistory = await serviceFailureTask.ConfigureAwait(false);
        var readiness = InstallerDiagnostics.Check();
        var securityPolicy = InstallerSecurityPolicyDiagnostics.Check();
        var loggingPolicy = activity.Single(check => check.Name == "Installer logging policy");
        var cache = await MsiCacheDiagnostics.CheckAsync(token).ConfigureAwait(false);
        var sources = await MsiSourceDiagnostics.CheckAsync(token).ConfigureAwait(false);
        var outcome = InstallerOutcomeCorrelation.Check(history, token);
        var appDeployment = await deploymentTask.ConfigureAwait(false);
        var storeHistory = await storeTask.ConfigureAwait(false);
        var environment = InstallationEnvironmentCorrelation.Check(readiness, securityPolicy, loggingPolicy,
            cache, sources, outcome, appDeployment, storeHistory, token);
        List<WorkflowCheck> checks = [readiness, serviceState, pendingOperations, securityPolicy, engineFiles, serviceRegistration];
        checks.AddRange(activity);
        checks.Add(InstallerPendingOperationCorrelation.Check(pendingOperations, installerActivity, token));
        checks.AddRange(cache);
        checks.AddRange(sources);
        checks.Add(history);
        checks.Add(outcome);
        checks.Add(serviceHistory);
        checks.Add(InstallerOperationalCorrelation.Check(serviceState, engineFiles, serviceRegistration,
            installerActivity, pendingOperations, history, serviceHistory, token));
        checks.Add(appDeployment);
        checks.Add(storeHistory);
        checks.Add(environment);
        return WithReadinessSummary(CommonDiagnosticScope.InstallationReadiness, checks, token);
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectServiceReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var inventoryTask = ServiceInventoryDiagnostics.CheckAsync(token);
        var historyTask = Completed(completed, CommonDiagnosticScope.ServiceHistory)?.SingleOrDefault() is { } savedHistory
            ? Task.FromResult(savedHistory) : ServiceEvents.CheckAsync(token);
        var currentTask = ServiceInspection.ReadAsync(Enum.GetValues<RepairKind>(), token: token);
        var inventory = await inventoryTask.ConfigureAwait(false);
        var binaryTask = ServiceBinaryDiagnostics.CheckAsync(token);
        var pathQuotingTask = ServicePathQuotingDiagnostics.CheckAsync(token);
        var logonTask = ServiceLogonDiagnostics.CheckAsync(token);
        var dllTask = ServiceDllDiagnostics.CheckAsync(token);
        var current = await currentTask.ConfigureAwait(false);
        var history = await historyTask.ConfigureAwait(false);
        var startup = ServiceStartupConfigurationDiagnostics.Check(inventory, token);
        var binary = await binaryTask.ConfigureAwait(false);
        var pathQuoting = await pathQuotingTask.ConfigureAwait(false);
        var logon = await logonTask.ConfigureAwait(false);
        var dll = await dllTask.ConfigureAwait(false);
        var dependencyConfigurationTask = ServiceDependencyConfigurationDiagnostics.CheckAsync(inventory, token: token);
        var transitionTask = ServiceTransitionDiagnostics.CheckAsync(inventory, token: token);
        var recoveryPolicyTask = ServiceRecoveryPolicyDiagnostics.CheckAsync(inventory, token: token);
        var triggerTask = ServiceTriggerDiagnostics.CheckAsync(inventory, token: token);
        var recoveryPolicy = await recoveryPolicyTask.ConfigureAwait(false);
        var dependencyConfiguration = await dependencyConfigurationTask.ConfigureAwait(false);
        var triggers = await triggerTask.ConfigureAwait(false);
        var transitions = await transitionTask.ConfigureAwait(false);
        WorkflowCheck[] structural = [inventory, startup, binary, pathQuoting, logon, dll, recoveryPolicy,
            dependencyConfiguration, triggers, transitions];
        var checks = new List<WorkflowCheck>(structural);
        checks.AddRange(current);
        checks.Add(history);
        var eventCorrelation = ServiceEventCorrelation.Check(inventory, current, history, token);
        checks.Add(eventCorrelation);
        checks.Add(ServiceOperationalCorrelation.Check(structural, eventCorrelation, token));
        return WithReadinessSummary(CommonDiagnosticScope.ServiceReadiness, checks, token);
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<WorkflowCheck> WithReadinessSummary(CommonDiagnosticScope scope,
        IReadOnlyList<WorkflowCheck> checks, CancellationToken token)
    {
        var completed = new List<WorkflowCheck> { DiagnosticReadinessSummary.Check(scope, checks, token) };
        completed.AddRange(checks);
        return completed.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectWindowsUpdateReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        var updateContext = Completed(completed, CommonDiagnosticScope.WindowsUpdateContext) ??
            await CollectWindowsUpdateContextAsync(token).ConfigureAwait(false);
        var serviceStates = await ServicingServiceStateDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
        var serviceDlls = await ServiceDllDiagnostics.CheckSelectedAsync("Windows Update service DLLs",
            ["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"], token).ConfigureAwait(false);
        var servicingChain = ServicingChainCorrelation.Check(serviceStates, serviceDlls, updateContext, token);
        var restartState = Completed(completed, CommonDiagnosticScope.RestartState) ?? [RestartDiagnostics.Check()];
        var servicingTransitions = Completed(completed, CommonDiagnosticScope.ServicingTransitions) ??
            await CollectServicingTransitionsAsync(completed, token: token).ConfigureAwait(false);
        var systemVolumeCapacity = Completed(completed, CommonDiagnosticScope.SystemVolumeCapacity) ??
            [VolumeCapacity.Check(StorageDiagnostics.SystemVolume())];
        var reservedStorage = await ReservedStorageDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
        var backgroundTransfers = Completed(completed, CommonDiagnosticScope.BackgroundTransfers) ??
            [await BitsDiagnostics.CheckAsync(token: token).ConfigureAwait(false)];
        var networkPathReadiness = Completed(completed, CommonDiagnosticScope.NetworkPathReadiness) ??
            await CollectNetworkPathReadinessAsync(completed, token: token).ConfigureAwait(false);
        Add(updateContext);
        Add(serviceStates);
        Add([serviceDlls, servicingChain]);
        Add(restartState);
        Add(servicingTransitions);
        Add(systemVolumeCapacity);
        Add([reservedStorage]);
        Add(backgroundTransfers);
        Add(networkPathReadiness);
        var correlationEvidence = new List<WorkflowCheck> { servicingChain, reservedStorage };
        AddNamed(correlationEvidence, restartState, "Restart");
        AddNamed(correlationEvidence, servicingTransitions, "Servicing transition readiness");
        AddNamed(correlationEvidence, systemVolumeCapacity, "Volume capacity");
        AddNamed(correlationEvidence, backgroundTransfers, "BITS transfer jobs");
        AddNamed(correlationEvidence, networkPathReadiness, "Network operational correlation");
        checks.Add(WindowsUpdateOperationalCorrelation.Check(correlationEvidence, token));
        return WithReadinessSummary(CommonDiagnosticScope.WindowsUpdateReadiness, checks, token);

        void Add(IReadOnlyList<WorkflowCheck> evidence) =>
            checks.AddRange(evidence.Where(check => !check.Name.EndsWith(" verdict", StringComparison.Ordinal)));
        static void AddNamed(List<WorkflowCheck> target, IReadOnlyList<WorkflowCheck> source, string name) =>
            target.AddRange(source.Where(check => check.Name == name));
    }

    private static IReadOnlyList<WorkflowCheck>? Completed(
        IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed, CommonDiagnosticScope scope) =>
        completed.TryGetValue(scope, out var checks) ? checks : null;

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectBackupReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        var windowsBackup = await WindowsBackupSummary.CheckAsync(token: token).ConfigureAwait(false);
        var fileHistoryState = await FileHistoryStatus.CheckAsync(token: token).ConfigureAwait(false);
        var fileHistoryEvents = await FileHistoryEvents.CheckAsync(token: token).ConfigureAwait(false);
        var backupEvents = Completed(completed, CommonDiagnosticScope.BackupHistory) ??
            await BackupEvents.CheckAsync(token: token).ConfigureAwait(false);
        var writerEvidence = Completed(completed, CommonDiagnosticScope.VssWriters) ??
            [await VssWriters.CheckAsync(token).ConfigureAwait(false)];
        var providers = await VssProviderDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
        var serviceHistory = await BackupServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false);
        var shadowCopies = await ShadowCopyDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
        var restorePointEvidence = Completed(completed, CommonDiagnosticScope.SystemRestore) ??
            [await SystemRestoreDiagnostics.CheckAsync(token).ConfigureAwait(false)];
        var storageReadiness = Completed(completed, CommonDiagnosticScope.StorageReadiness) ??
            await CollectStorageReadinessAsync(completed, token: token).ConfigureAwait(false);
        var restartState = Completed(completed, CommonDiagnosticScope.RestartState) ?? [RestartDiagnostics.Check()];
        Add([windowsBackup, fileHistoryState, fileHistoryEvents]);
        Add(backupEvents);
        Add(writerEvidence);
        Add([providers]);
        Add(serviceHistory);
        Add([shadowCopies]);
        Add(restorePointEvidence);
        Add(storageReadiness);
        Add(restartState);
        checks.Add(BackupOperationalCorrelation.Check(windowsBackup, fileHistoryState, fileHistoryEvents,
            backupEvents, writerEvidence, providers, serviceHistory, shadowCopies, restorePointEvidence, storageReadiness,
            restartState, token));
        return WithReadinessSummary(CommonDiagnosticScope.BackupReadiness, checks, token);

        void Add(IReadOnlyList<WorkflowCheck> evidence) =>
            checks.AddRange(evidence.Where(check => !check.Name.EndsWith(" verdict", StringComparison.Ordinal)));
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectSystemStabilityReadinessAsync(KnowledgeCatalogCollection? knowledgeCatalogs,
        IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        var vitals = Completed(completed, CommonDiagnosticScope.Vitals) ?? await CollectVitalsAsync(token).ConfigureAwait(false);
        var devices = Completed(completed, CommonDiagnosticScope.DeviceProblems) ??
            await DeviceDiagnostics.CheckAsync(DeviceCategory.Problems, knowledgeCatalogs, token).ConfigureAwait(false);
        var reliability = Completed(completed, CommonDiagnosticScope.ReliabilityHistory) ?? await ReliabilityHistory.CheckAsync(token).ConfigureAwait(false);
        var hardwareErrors = Completed(completed, CommonDiagnosticScope.HardwareErrorHistory) ?? [await HardwareErrorEvents.CheckAsync(token).ConfigureAwait(false)];
        var crashes = Completed(completed, CommonDiagnosticScope.CrashEvidence) ?? await CollectCrashEvidenceAsync(knowledgeCatalogs, token: token).ConfigureAwait(false);
        var shutdowns = Completed(completed, CommonDiagnosticScope.ShutdownHistory) ?? [await ShutdownEvents.CheckAsync(token).ConfigureAwait(false)];
        Add(vitals); Add(devices); Add(reliability); Add(hardwareErrors); Add(crashes); Add(shutdowns);
        checks.Add(SystemStabilityOperationalCorrelation.Check(vitals, devices, reliability, hardwareErrors, crashes, shutdowns, token));
        return WithReadinessSummary(CommonDiagnosticScope.SystemStabilityReadiness, checks, token);

        void Add(IReadOnlyList<WorkflowCheck> evidence) =>
            checks.AddRange(evidence.Where(check => !check.Name.EndsWith(" verdict", StringComparison.Ordinal)));
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectSecurityReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        var security = Completed(completed, CommonDiagnosticScope.Security) ?? await CollectSecurityAsync(token).ConfigureAwait(false);
        var certificates = Completed(completed, CommonDiagnosticScope.Certificates) ??
        [
            await CertificateDiagnostics.CheckAsync(token).ConfigureAwait(false),
            await CertificateChainDiagnostics.CheckAsync(token).ConfigureAwait(false)
        ];
        var licensing = Completed(completed, CommonDiagnosticScope.WindowsLicensing) ?? [await WindowsLicensingDiagnostics.CheckAsync(token).ConfigureAwait(false)];
        var products = await SecurityProductDiagnostics.ReadAsync(token).ConfigureAwait(false);
        var productChecks = SecurityProductDiagnostics.Describe(products);
        var threats = await DefenderThreatSummary.CheckAsync(token).ConfigureAwait(false);
        var scanHistory = await DefenderScanHistory.CheckAsync(token).ConfigureAwait(false);
        var defenderEvents = await DefenderEvents.CheckAsync(token).ConfigureAwait(false);
        var restartState = Completed(completed, CommonDiagnosticScope.RestartState) ?? [RestartDiagnostics.Check()];
        Add(security); Add(certificates); Add(licensing); Add(productChecks);
        checks.Add(threats); checks.Add(scanHistory); checks.Add(defenderEvents); Add(restartState);
        checks.Add(SecurityOperationalCorrelation.Check([.. security.Append(scanHistory)], certificates, licensing, productChecks, threats,
            defenderEvents, restartState, token));
        return WithReadinessSummary(CommonDiagnosticScope.SecurityReadiness, checks, token);

        void Add(IReadOnlyList<WorkflowCheck> evidence) =>
            checks.AddRange(evidence.Where(check => !check.Name.EndsWith(" verdict", StringComparison.Ordinal)));
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectApplicationDeploymentReadinessAsync(IReadOnlyDictionary<CommonDiagnosticScope, IReadOnlyList<WorkflowCheck>> completed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var machineApplicationsTask = InstalledApplications.ReadAsync(InstalledApplicationScope.Machine, token);
        var accountApplicationsTask = InstalledApplications.ReadAsync(InstalledApplicationScope.CurrentAccount, token);
        var provisionedPackagesTask = ProvisionedPackages.ReadAsync(token);
        var checks = new List<WorkflowCheck>();
        var registeredPackages = AppPackageDiagnostics.Summarize(Completed(completed, CommonDiagnosticScope.AppPackages) ??
            AppPackageDiagnostics.Describe(await AppPackageInventory.ReadAsync(token: token).ConfigureAwait(false)));
        var provisionedPackages = ProvisionedPackages.Describe(await provisionedPackagesTask.ConfigureAwait(false))[0];
        var machineApplications = InstalledApplications.Describe(await machineApplicationsTask.ConfigureAwait(false))[0];
        var accountApplications = InstalledApplications.Describe(await accountApplicationsTask.ConfigureAwait(false))[0];
        var installationReadiness = Completed(completed, CommonDiagnosticScope.InstallationReadiness) ??
            await CollectInstallationReadinessAsync(completed, token: token).ConfigureAwait(false);
        var applicationHistory = Completed(completed, CommonDiagnosticScope.ApplicationHistory) ??
            [await ApplicationEvents.CheckAsync(token).ConfigureAwait(false)];
        var restartState = Completed(completed, CommonDiagnosticScope.RestartState) ?? [RestartDiagnostics.Check()];
        checks.AddRange([registeredPackages, provisionedPackages, machineApplications, accountApplications]);
        Add(installationReadiness); Add(applicationHistory); Add(restartState);
        checks.Add(ApplicationDeploymentOperationalCorrelation.Check(registeredPackages, provisionedPackages,
            machineApplications, accountApplications, installationReadiness, applicationHistory, restartState, token));
        return WithReadinessSummary(CommonDiagnosticScope.ApplicationDeploymentReadiness, checks, token);

        void Add(IReadOnlyList<WorkflowCheck> evidence) =>
            checks.AddRange(evidence.Where(check => !check.Name.EndsWith(" verdict", StringComparison.Ordinal)));
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectCrashEvidenceAsync(KnowledgeCatalogCollection? knowledgeCatalogs,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck> { CrashDumpConfiguration.Check(token) };
        var dumps = await CrashDumpInventory.ReadAsync(token).ConfigureAwait(false);
        checks.Add(CrashDumpInventory.Interpret(dumps));
        var analyses = new Dictionary<string, MinidumpSummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var dump in dumps.Files.OrderByDescending(file => file.LastWriteUtc).Take(3))
        {
            token.ThrowIfCancellationRequested();
            var analysis = await MinidumpAnalyzer.AnalyzeFileAsync(dump.Path, token).ConfigureAwait(false);
            analyses[dump.Path] = analysis;
            checks.Add(MinidumpAnalyzer.Interpret(analysis));
        }
        var history = await CrashEvents.CheckAsync(token).ConfigureAwait(false);
        checks.Add(history);
        if (history.Evidence is { } evidence)
        {
            checks.Add(CrashDumpCorrelation.Check(evidence, dumps, token));
            checks.Add(CrashIncidentCorrelation.Check(evidence, dumps, analyses, token));
            if (knowledgeCatalogs is { Count: > 0 }) checks.Add(CrashBugCheckReferences.Check(evidence, knowledgeCatalogs, token));
        }
        return checks.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectPrintingHistoryAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var adminTask = PrintServiceEvents.CheckAsync(token);
        var operationalTask = PrintServiceOperationalEvents.CheckAsync(token: token);
        return
        [
            await adminTask.ConfigureAwait(false),
            await operationalTask.ConfigureAwait(false)
        ];
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectWindowsUpdateContextAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        List<WorkflowCheck> checks =
        [
            UpdateExecutionContextDiagnostics.Check(),
            UpdateApplicabilityDiagnostics.Check(),
            UpdatePolicyDiagnostics.Check(),
            FeatureUpdatePolicyDiagnostics.Check(),
            UpdatePolicySurfaceDiagnostics.Check(),
            UpdateRestartPolicyDiagnostics.Check(),
            ComponentRepairSourceDiagnostics.Check(),
            DeliveryOptimizationPolicyDiagnostics.Check(),
            await UpdateServiceDiagnostics.CheckAsync(token).ConfigureAwait(false),
            await WindowsUpdateEngineFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false)
        ];
        checks.AddRange(await ServicingServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false));
        checks.Add(await WindowsUpdateScheduledTaskDiagnostics.CheckAsync(token).ConfigureAwait(false));
        checks.Add(await UpdateHistoryDiagnostics.CheckAsync(token).ConfigureAwait(false));
        checks.Add(await InstalledHotfixDiagnostics.CheckAsync(token).ConfigureAwait(false));
        checks.Add(await DeliveryOptimizationDiagnostics.CheckAsync(token).ConfigureAwait(false));
        checks.Add(await DeliveryOptimizationActivityDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        checks.Add(await UpdateCacheDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        checks.AddRange(await ServicingLogDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        return checks.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectSearchHealthAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>
        {
            await SearchCatalog.CheckAsync(token).ConfigureAwait(false),
            await SearchEvents.CheckAsync(token).ConfigureAwait(false)
        };
        return checks.AsReadOnly();
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> CollectFirewallConfigurationAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck>();
        checks.AddRange(await FirewallProfiles.CheckAsync(token).ConfigureAwait(false));
        checks.Add(await FirewallRuleSummaryDiagnostics.CheckAsync(token).ConfigureAwait(false));
        return checks.AsReadOnly();
    }
}
