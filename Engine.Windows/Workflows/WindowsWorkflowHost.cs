using System.Text.Json;
using Microsoft.Win32;
using System.Net.NetworkInformation;

namespace KillerBench.Engine.Windows.Workflows;

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
        (host, timeout, token) => DnsDiagnostics.CheckAsync(host, timeout, token: token);
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

    public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) => InspectAsync(kind, _ => Task.CompletedTask, token);

    public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint,
        CancellationToken token)
        => InspectAsync(kind, checkpoint, null, token);

    public async Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint,
        IReadOnlyList<CommonDiagnosticScope>? selectedScopes, CancellationToken token)
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
            return await CommonDiagnostics.CheckAsync(Progress, checkpoint: checkpoint, selectedScopes: selectedScopes, knowledgeCatalogs: KnowledgeCatalogs,
                token: token).ConfigureAwait(false);
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
                InfrastructureServiceFailureArea.TaskScheduler, token: token).ConfigureAwait(false);
            findings.AddRange(serviceHistory);
            Progress?.Report("Correlating task runtime results with exact service history...");
            findings.Add(ScheduledTaskOperationalCorrelation.Check([.. findings
                .Where(check => check.Name != "Task Scheduler service failure history")], serviceHistory, token));
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
            var registeredSummary = AppPackageDiagnostics.Summarize([.. findings
                .Where(check => check.Name != "Provisioned app packages" && !check.Name.StartsWith("Provisioned: ", StringComparison.Ordinal))]);
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
        if (kind == WorkflowKind.Debloat)
        {
            LastDebloatReview = null;
            Progress?.Report("Reviewing the selected declutter areas and preservation boundaries...");
            LastDebloatReview = await DebloatPlanning.ReviewAsync(DebloatOptions, Progress, token: token).ConfigureAwait(false);
            return LastDebloatReview.Checks;
        }
        if (kind == WorkflowKind.BackgroundTransfers)
        {
            LastBitsInventory = null;
            Progress?.Report("Reading BITS transfer inventory...");
            LastBitsInventory = await BitsDiagnostics.ReadAsync(BitsAllUsers, token).ConfigureAwait(false);
            var jobs = BitsDiagnostics.Interpret(LastBitsInventory.Evidence, token);
            Progress?.Report("Reading current BITS service state...");
            var service = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["BITS"], token: token).ConfigureAwait(false)).Single();
            Progress?.Report("Reading exact BITS service-failure history...");
            var history = await ServicingServiceFailureEvents.CheckBitsAsync(token: token).ConfigureAwait(false);
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
            findings.Add(await FoxitUpdaterSettings.CheckAsync(token: token).ConfigureAwait(false));
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
                UpdateHistoryDiagnostics.RecentReferences(history.Evidence), Progress, token: token).ConfigureAwait(false);
            if (sourceReadiness.State is CheckState.Unknown or CheckState.Attention)
                return [featurePolicy, policySurfaces, restartPolicy, source, sourceReadiness, history, installedHotfixes, .. followups,
                    new("Available updates", CheckState.Unknown, "Update discovery was not started because the selected source could not be verified."), .. thirdParty];
            var updates = await UpdateDiscovery.SearchAsync(UpdateOptions, Progress, token: token).ConfigureAwait(false);
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
            var transitionTask = ServiceTransitionDiagnostics.CheckAsync(serviceInventory, token: token);
            var recoveryPolicyTask = ServiceRecoveryPolicyDiagnostics.CheckAsync(serviceInventory, token: token);
            Progress?.Report("Checking direct service dependency identities and cycles...");
            var dependencyConfigurationTask = ServiceDependencyConfigurationDiagnostics.CheckAsync(serviceInventory, token: token);
            Progress?.Report("Reading privacy-bounded service trigger configuration...");
            var triggerTask = ServiceTriggerDiagnostics.CheckAsync(serviceInventory, token: token);
            serviceChecks.Add(await recoveryPolicyTask.ConfigureAwait(false));
            serviceChecks.Add(await dependencyConfigurationTask.ConfigureAwait(false));
            serviceChecks.Add(await triggerTask.ConfigureAwait(false));
            serviceChecks.Add(await transitionTask.ConfigureAwait(false));
            var structuralServiceChecks = serviceChecks.ToArray();
            var currentServiceChecks = await ServiceInspection.ReadAsync(Enum.GetValues<RepairKind>(), Progress, token: token).ConfigureAwait(false);
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
            checks.AddRange(await SecurityDiagnostics.CheckAsync(SecurityOptions, token: token).ConfigureAwait(false));
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
            checks.AddRange(await CommonDiagnostics.CheckAsync(Progress, checkpoint: checkpoint, selectedScopes: selectedScopes, knowledgeCatalogs: KnowledgeCatalogs,
                token: token).ConfigureAwait(false));
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
            var selfHealing = await NtfsSelfHealingDiagnostics.CheckAsync(volume, token: token).ConfigureAwait(false);
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
            var deleteNotifications = await DeleteNotificationDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(deleteNotifications);
            Progress?.Report("Reading current paging-file allocation and usage without changing memory settings...");
            var pageFile = await PageFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(pageFile);
            Progress?.Report("Reading per-volume disk quota settings without collecting user quota rows...");
            var diskQuotas = await DiskQuotaDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
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
            checks.Add(await StorageJobDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
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
            var shadowStorage = await ShadowStorageDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
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
            checks.AddRange(await StorageServiceFailureEvents.CheckStorageAsync(token: token).ConfigureAwait(false));
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
            checks.Add(WindowsFeatureVersionDiagnostics.Check());
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
            checks.AddRange(await FirewallServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Printing)
        {
            Progress?.Report("Reading current Print Spooler state without changing the service...");
            var printSpooler = (await ServiceInspection.ReadAsync([RepairKind.PrintSpooler], token: token).ConfigureAwait(false)).Single();
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
            var printerConditions = await PrinterConditionDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(printerConditions);
            Progress?.Report("Reading local printer queue operating attributes without changing queues...");
            var printerAttributes = await PrinterQueueAttributeDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
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
            var printerDriverFiles = await PrinterDriverFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(printerDriverFiles);
            Progress?.Report("Reading printer driver models and package-awareness without changing drivers...");
            var printerDriverModels = await PrinterDriverModelDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(printerDriverModels);
            Progress?.Report("Matching print queues to registered ports...");
            var printerPorts = await PrinterPortDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(printerPorts);
            Progress?.Report("Mapping local queues to Standard TCP/IP endpoint configuration...");
            var printerEndpoints = await PrinterEndpointDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(printerEndpoints);
            Progress?.Report("Testing exact assigned printer endpoints with bounded TCP connections...");
            var printerReachability = await PrinterEndpointReachability.CheckAsync(printerEndpoints, token: token).ConfigureAwait(false);
            checks.AddRange(printerReachability);
            Progress?.Report("Reading print processor and monitor registrations without loading extensions...");
            var printExtensions = await PrintExtensionDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(printExtensions);
            Progress?.Report("Checking registered print processor and monitor binaries and signatures...");
            var printExtensionFiles = await PrintExtensionFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(printExtensionFiles);
            Progress?.Report("Correlating printing policy, drivers, ports, endpoints and extensions...");
            var printingEnvironment = PrintingEnvironmentCorrelation.Check(pointAndPrint, rendering, printerDrivers,
                printerDriverFiles, printerDriverModels, printerPorts, printerEndpoints, printerReachability,
                printExtensions, printExtensionFiles, token);
            checks.Add(printingEnvironment);
            Progress?.Report("Reading visible print job states...");
            ToolResult printJobEvidence = await PrintJobDiagnostics.CaptureAsync(token: token).ConfigureAwait(false);
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
            checks.Add(await PrintServiceOperationalEvents.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading Service Control Manager failures tied to the local Print Spooler identity...");
            var printSpoolerHistory = await PrintSpoolerFailureEvents.CheckAsync(token: token).ConfigureAwait(false);
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
            var eventLogCapacity = await EventLogCapacityDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(eventLogCapacity);
            Progress?.Report("Reading exact Windows Event Log service failure history...");
            var eventLogHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.EventLog, token: token).ConfigureAwait(false);
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
            var timePreview = await TimePlanning.PreviewAsync(TimeOptions, token: token).ConfigureAwait(false);
            var timeConfiguration = TimeConfigurationDiagnostics.ReviewSelection(
                TimeOptions, timePreview.Environment, timePreview.Plan);
            checks.Add(timeConfiguration);
            Progress?.Report("Reading recent Windows Time history...");
            var timeEvents = await TimeServiceEvents.CheckAsync(token).ConfigureAwait(false);
            checks.Add(timeEvents);
            Progress?.Report("Reading exact Windows Time service failure history...");
            var timeServiceHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Time, token: token).ConfigureAwait(false);
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
            checks.Add(await InstallerEngineFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
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
            checks.Add(await WindowsInstallerFailureEvents.CheckAsync(token: token).ConfigureAwait(false));
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
            var wmiHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Wmi, token: token).ConfigureAwait(false);
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
            var searchHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Search, token: token).ConfigureAwait(false);
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
            checks.AddRange(await PlatformServiceFailureEvents.CheckAsync(platformArea, token: token).ConfigureAwait(false));
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
                InfrastructureServiceFailureArea.TaskScheduler, token: token).ConfigureAwait(false);
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
            checks.AddRange(await InfrastructureServiceFailureEvents.CheckManyAsync(infrastructureServiceHistory, token: token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.Audio)
        {
            var audioDevices = await AudioDeviceDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(audioDevices);
            Progress?.Report("Reading default audio input and output endpoints...");
            var audioEndpoints = await DefaultAudioEndpoints.CheckAsync(AudioEndpointSelection, token).ConfigureAwait(false);
            checks.AddRange(audioEndpoints);
            Progress?.Report("Reading exact Windows audio service failure history...");
            var audioHistory = await FeatureServiceFailureEvents.CheckAsync(FeatureServiceFailureGroup.Audio, token: token).ConfigureAwait(false);
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
            checks.Add(await NcsiPolicyDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading matching Windows firewall profile configuration...");
            var firewallProfiles = await FirewallProfiles.CheckAsync(token).ConfigureAwait(false);
            checks.AddRange(firewallProfiles);
            Progress?.Report("Reading Windows Firewall logging configuration without opening log contents...");
            checks.AddRange(await FirewallLoggingDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Summarizing effective Windows Firewall rules without collecting rule identities or endpoints...");
            checks.Add(await FirewallRuleSummaryDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading recent Windows Firewall warning and failure history...");
            checks.Add(await FirewallEvents.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading exact firewall service failure history...");
            checks.AddRange(await FirewallServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading exact network service failure history...");
            checks.AddRange(await NetworkServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Correlating active network assignments with firewall profile enablement...");
            var firewallCorrelation = NetworkFirewallCorrelation.Check(networkProfiles, firewallProfiles, token);
            checks.Add(firewallCorrelation);
            Progress?.Report("Reading current DNS Client service state without starting or changing it...");
            var dnsService = (await ServiceInspection.ReadAsync([RepairKind.DnsClient], Progress, token: token).ConfigureAwait(false)).Single();
            checks.Add(dnsService);
            Progress?.Report("Reading per-interface DNS server configuration without changing adapters...");
            var dnsServers = await DnsServerDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(dnsServers);
            Progress?.Report("Reading DNS suffix, registration and policy configuration without changing adapters...");
            checks.Add(await DnsClientConfigurationDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading effective DNS namespace policy without changing DNS or network settings...");
            var dnsPolicy = await DnsNrptPolicyDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(dnsPolicy);
            Progress?.Report("Reading privacy-bounded DNS client cache aggregates without retaining names or record data...");
            checks.Add(await DnsClientCacheDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading recent DNS Client warning and failure history without changing resolver state...");
            var dnsEvents = await DnsClientEvents.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(dnsEvents);
            Progress?.Report("Reading the current IP routing table without changing routes...");
            var routes = await RouteDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(routes);
            Progress?.Report("Comparing default-route and interface metrics without changing priority...");
            checks.Add(await RoutePriorityDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading per-interface MTU, DHCP, forwarding, discovery and metric configuration...");
            checks.Add(await IpInterfaceConfigurationDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading current DHCP Client service state without starting or changing it...");
            var dhcpService = (await ServiceInspection.ReadAsync([RepairKind.DhcpClient], Progress, token: token).ConfigureAwait(false)).Single();
            checks.Add(dhcpService);
            Progress?.Report("Reading DHCP lease timestamps, expiry and server evidence without renewing leases...");
            var dhcpLease = await DhcpLeaseDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(dhcpLease);
            Progress?.Report("Reading recent IPv4 and IPv6 DHCP Client events without changing network state...");
            var dhcpEvents = await DhcpClientEvents.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(dhcpEvents);
            Progress?.Report("Correlating current DHCP readiness with recent client history...");
            var dhcpCorrelation = DhcpOperationalCorrelation.Check(dhcpService, dhcpLease, ipAddresses, dhcpEvents, token);
            checks.Add(dhcpCorrelation);
            Progress?.Report("Reading privacy-bounded neighbor reachability states without changing the cache...");
            checks.Add(await NeighborCacheDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading core network adapter protocol bindings without changing adapters...");
            checks.Add(await AdapterBindingDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading network adapter error and discard counters without changing adapters...");
            checks.Add(await AdapterStatisticsDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading aggregate dynamic TCP port pressure without retaining endpoints or processes...");
            checks.AddRange(await TcpPortPressureDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading bounded Windows TCP template settings without changing the network stack...");
            checks.Add(await TcpSettingDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading global Windows network offload settings without changing the network stack...");
            checks.Add(await NetworkOffloadDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading the native Winsock protocol catalog without changing providers...");
            var winsockProtocols = await WinsockCatalogDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(winsockProtocols);
            Progress?.Report("Reading native Winsock namespace-provider registrations without changing providers...");
            var winsockNamespaces = await WinsockNamespaceDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(winsockNamespaces);
            Progress?.Report("Checking registered Winsock protocol provider files without loading providers...");
            var winsockProviderFiles = await WinsockProviderFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
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
                targetTcp = TcpDiagnostics.ToWorkflowCheck(await TcpDiagnostics.CheckAsync(endpoint.Host, endpoint.Port, NetworkTimeout, token: token).ConfigureAwait(false));
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
                checks.AddRange(await ServiceInspection.ReadAsync(KillerBench.Engine.Workflows.Workflows.Scopes(kind).Where(scope => scope is not (RepairKind.DnsClient or RepairKind.DhcpClient)), ServiceRecovery.CheckAsync, Progress, token: token).ConfigureAwait(false));
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
            checks.Add(await WindowsBackupSummary.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading current File History protection state...");
            checks.Add(await FileHistoryStatus.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading recent File History warnings and errors...");
            checks.Add(await FileHistoryEvents.CheckAsync(token: token).ConfigureAwait(false));
            checks.AddRange(await BackupEvents.CheckAsync(Progress, token: token).ConfigureAwait(false));
            Progress?.Report("Reading VSS writer status...");
            checks.Add(await VssWriters.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading VSS provider registrations...");
            checks.Add(await VssProviderDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading exact shadow-copy and backup service failure history...");
            checks.AddRange(await BackupServiceFailureEvents.CheckAllAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading current VSS shadow copy inventory...");
            checks.Add(await ShadowCopyDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading VSS shadow storage allocation and configured limits...");
            checks.Add(await ShadowStorageDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
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
            checks.Add(await AdapterBindingDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.StorageService)
        {
            Progress?.Report("Reading current Storage module jobs without changing or stopping them...");
            checks.Add(await StorageJobDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        }
        if (kind == WorkflowKind.StorageService)
        {
            Progress?.Report("Reading exact Storage Service failure history...");
            var storageHistory = await StorageServiceFailureEvents.CheckStorageAsync(token: token).ConfigureAwait(false);
            checks.Add(storageHistory.Single(check => check.Name == "Storage Service failure history"));
        }
        if (kind == WorkflowKind.WindowsUpdateService)
        {
            Progress?.Report("Reading exact Windows Update Service state...");
            var focusedUpdateService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["wuauserv"], token: token).ConfigureAwait(false)).Single();
            checks.Add(focusedUpdateService);
            Progress?.Report("Checking the registered Windows Update service DLL and signature...");
            var focusedUpdateRegistration = await ServiceDllDiagnostics.CheckSelectedAsync(
                "Windows Update Service registration", ["wuauserv"], token).ConfigureAwait(false);
            checks.Add(focusedUpdateRegistration);
            Progress?.Report("Reading bounded current Windows Update activity...");
            var focusedUpdateActivity = await WindowsUpdateActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(focusedUpdateActivity);
            Progress?.Report("Reading exact Windows Update Service failure history...");
            var focusedUpdateHistory = await WindowsUpdateServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false);
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
            var bitsService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["BITS"], token: token).ConfigureAwait(false)).Single();
            checks.Add(bitsService);
            Progress?.Report("Reading exact BITS service-failure history...");
            var bitsHistory = await ServicingServiceFailureEvents.CheckBitsAsync(token: token).ConfigureAwait(false);
            checks.Add(bitsHistory);
            Progress?.Report("Correlating BITS jobs, service state and failure history...");
            checks.Add(BitsOperationalCorrelation.Check(bitsJobs, bitsService, bitsHistory, token));
        }
        if (kind == WorkflowKind.CryptographicService)
        {
            Progress?.Report("Reading exact Cryptographic Services state...");
            var cryptographicService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["CryptSvc"], token: token).ConfigureAwait(false)).Single();
            checks.Add(cryptographicService);
            Progress?.Report("Checking the registered Cryptographic Services DLL and signature...");
            var cryptographicRegistration = await ServiceDllDiagnostics.CheckSelectedAsync(
                "Cryptographic Services registration", ["CryptSvc"], token).ConfigureAwait(false);
            checks.Add(cryptographicRegistration);
            Progress?.Report("Reading exact Cryptographic Services failure history...");
            var cryptographicHistory = await ServicingServiceFailureEvents.CheckFocusedAsync(kind, token: token).ConfigureAwait(false);
            checks.Add(cryptographicHistory);
            Progress?.Report("Correlating Cryptographic Services state, registration and history...");
            checks.Add(CryptographicServiceOperationalCorrelation.Check(
                cryptographicService, cryptographicRegistration, cryptographicHistory, token));
        }
        if (kind == WorkflowKind.ModulesInstallerService)
        {
            Progress?.Report("Reading exact Windows Modules Installer state...");
            var modulesInstallerService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["TrustedInstaller"], token: token).ConfigureAwait(false)).Single();
            checks.Add(modulesInstallerService);
            Progress?.Report("Reading bounded servicing package transitions...");
            var servicingPackages = await ServicingPackageDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(servicingPackages);
            Progress?.Report("Reading exact Windows Modules Installer failure history...");
            var modulesInstallerHistory = await ServicingServiceFailureEvents.CheckFocusedAsync(kind, token: token).ConfigureAwait(false);
            checks.Add(modulesInstallerHistory);
            Progress?.Report("Correlating Windows Modules Installer state, packages and history...");
            checks.Add(ModulesInstallerOperationalCorrelation.Check(
                modulesInstallerService, servicingPackages, modulesInstallerHistory, token));
        }
        if (kind == WorkflowKind.WindowsUpdateMedicService)
        {
            Progress?.Report("Reading exact Windows Update Medic Service state...");
            var medicService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["WaaSMedicSvc"], token: token).ConfigureAwait(false)).Single();
            checks.Add(medicService);
            Progress?.Report("Checking the registered Windows Update Medic service DLL and signature...");
            var medicRegistration = await ServiceDllDiagnostics.CheckSelectedAsync(
                "Windows Update Medic registration", ["WaaSMedicSvc"], token).ConfigureAwait(false);
            checks.Add(medicRegistration);
            Progress?.Report("Reading bounded current Windows Update activity...");
            var medicActivity = await WindowsUpdateActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(medicActivity);
            Progress?.Report("Reading exact Windows Update Medic failure history...");
            var medicHistory = await ServicingServiceFailureEvents.CheckMedicAsync(token: token).ConfigureAwait(false);
            checks.Add(medicHistory);
            Progress?.Report("Correlating Windows Update Medic state, registration, activity and history...");
            checks.Add(WindowsUpdateMedicOperationalCorrelation.Check(
                medicService, medicRegistration, medicActivity, medicHistory, token));
        }
        if (kind == WorkflowKind.UpdateOrchestratorService)
        {
            Progress?.Report("Reading exact Update Orchestrator Service state...");
            var orchestratorService = (await ServicingServiceStateDiagnostics.CheckSelectedAsync(["UsoSvc"], token: token).ConfigureAwait(false)).Single();
            checks.Add(orchestratorService);
            Progress?.Report("Reading the exact built-in Windows Update scheduled task...");
            var updateTask = await WindowsUpdateScheduledTaskDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(updateTask);
            Progress?.Report("Reading bounded current Windows Update activity...");
            var updateActivity = await WindowsUpdateActivityDiagnostics.CheckAsync(token).ConfigureAwait(false);
            checks.Add(updateActivity);
            Progress?.Report("Reading exact Update Orchestrator failure history...");
            var orchestratorHistory = await ServicingServiceFailureEvents.CheckFocusedAsync(kind, token: token).ConfigureAwait(false);
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
            var deliveryActivity = await DeliveryOptimizationActivityDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(deliveryActivity);
            Progress?.Report("Reading Delivery Optimization policy surfaces...");
            var deliveryPolicy = DeliveryOptimizationPolicyDiagnostics.Check();
            checks.Add(deliveryPolicy);
            Progress?.Report("Reading exact Delivery Optimization failure history...");
            var deliveryHistory = await DeliveryOptimizationServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false);
            checks.Add(deliveryHistory);
            Progress?.Report("Correlating Delivery Optimization service, activity, policy and history evidence...");
            checks.Add(DeliveryOptimizationOperationalCorrelation.Check(
                deliveryService, deliveryActivity, deliveryPolicy, deliveryHistory, token));
        }
        bool administrator = WindowsState.IsAdministrator();
        if (kind == WorkflowKind.WindowsUpdate)
        {
            Progress?.Report("Checking required Windows Update engine files and signatures...");
            checks.Add(await WindowsUpdateEngineFileDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Checking exact Windows Update service DLL registrations and signatures...");
            var updateServiceDlls = await ServiceDllDiagnostics.CheckSelectedAsync("Windows Update service DLLs",
                ["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"], token).ConfigureAwait(false);
            checks.Add(updateServiceDlls);
            Progress?.Report("Checking all six current Windows Update servicing-chain service states...");
            var updateServiceStates = await ServicingServiceStateDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
            checks.AddRange(updateServiceStates);
            Progress?.Report("Reading exact servicing service failure histories...");
            var updateServiceHistory = await ServicingServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false);
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
            checks.Add(await DeliveryOptimizationActivityDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading Delivery Optimization classic and Policy CSP settings...");
            checks.Add(DeliveryOptimizationPolicyDiagnostics.Check());
            Progress?.Report("Reading managed Windows Update policy...");
            checks.Add(UpdatePolicyDiagnostics.Check());
            Progress?.Report("Reading managed feature update targeting and safeguard policy...");
            checks.Add(FeatureUpdatePolicyDiagnostics.Check());
            Progress?.Report("Comparing the installed Windows feature version with the newest release...");
            checks.Add(WindowsFeatureVersionDiagnostics.Check());
            Progress?.Report("Identifying Windows Update policy configuration surfaces...");
            checks.Add(UpdatePolicySurfaceDiagnostics.Check());
            Progress?.Report("Reading managed update deadlines and notification behavior...");
            checks.Add(UpdateRestartPolicyDiagnostics.Check());
            Progress?.Report("Reading managed component repair source policy...");
            checks.Add(ComponentRepairSourceDiagnostics.Check());
            Progress?.Report("Measuring Windows Update cache areas without reading filenames or file contents...");
            checks.Add(await UpdateCacheDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        }
        if (kind is WorkflowKind.WindowsUpdate or WorkflowKind.SystemFiles)
        {
            Progress?.Report("Reading the newest bounded portions of CBS, DISM and applicable Windows Setup logs...");
            checks.AddRange(await ServicingLogDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading Windows reserved-storage state without changing servicing configuration...");
            checks.Add(await ReservedStorageDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
            Progress?.Report("Reading Windows servicing package states without changing packages...");
            checks.Add(await ServicingPackageDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading optional Windows feature states without changing features...");
            checks.Add(await OptionalFeatureDiagnostics.CheckAsync(token).ConfigureAwait(false));
            Progress?.Report("Reading Windows capability states without changing capabilities...");
            checks.Add(await WindowsCapabilityDiagnostics.CheckAsync(token: token).ConfigureAwait(false));
        }
        var scopes = KillerBench.Engine.Workflows.Workflows.Scopes(kind);
        bool focusedService = kind == WorkflowKind.DeliveryOptimizationService ||
            scopes.Length > 0 && scopes.All(scope => ServiceRecovery.ServiceName(scope) is not null);
        if (!administrator && kind is not (WorkflowKind.Printing or WorkflowKind.BitsService or WorkflowKind.CryptographicService or WorkflowKind.ModulesInstallerService or WorkflowKind.UpdateOrchestratorService))
        {
            Progress?.Report("Reading service state without elevation...");
            bool focusedDemandStart = kind is WorkflowKind.WinHttpAutoProxyService or WorkflowKind.WindowsUpdateMedicService;
            checks.AddRange(await ServiceInspection.ReadAsync(scopes,
                (scope, cancellationToken) => ServiceRecovery.CheckAsync(scope, focusedDemandStart && scope is RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService,
                    token: cancellationToken),
                Progress, token:                 token).ConfigureAwait(false));
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
            ToolResult evidence = await PrintJobDiagnostics.CaptureAsync(token: token).ConfigureAwait(false);
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
            var search = await UpdateDiscovery.SearchAsync(UpdateOptions, progress, token: token).ConfigureAwait(false);
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
            Task<WorkflowCheck> Inspect(CancellationToken cancellationToken) => ServiceRecovery.CheckAsync(kind, kind is RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService,
                token: cancellationToken);
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
            var preview = await TimePlanning.PreviewAsync(TimeOptions, token: token).ConfigureAwait(false);
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
                await PrintJobDiagnostics.CaptureAsync(token: token).ConfigureAwait(false));
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
        return KillerBench.Engine.Workflows.Workflows.Interpret(kind, result) with { Evidence = result };
    }

    private static async Task<WorkflowCheck> InspectDefenderAsync(IProgress<string>? progress, CancellationToken token) =>
        DefenderRecovery.Interpret(await DefenderRecovery.ExecuteAsync(DefenderRecovery.Step(true), progress, token).ConfigureAwait(false));
    private static async Task<WorkflowCheck> InspectComponentCleanupAsync(IProgress<string>? progress, CancellationToken token) =>
        ComponentCleanup.Interpret(await BoundedTool.RunAsync(ComponentCleanup.Step(true), TimeSpan.FromHours(1), progress, token).ConfigureAwait(false));
    private static async Task<WorkflowCheck> InspectServicingAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
    {
        var step = Repairs.Plan(kind).Steps.Single(candidate => candidate.Verification);
        return KillerBench.Engine.Workflows.Workflows.Interpret(kind, await BoundedTool.RunAsync(step, TimeSpan.FromHours(1), progress, token).ConfigureAwait(false));
    }
    private static async Task<WorkflowCheck> InspectWmiRepositoryAsync(IProgress<string>? progress, CancellationToken token) =>
        WmiRecovery.Interpret(await WmiRecovery.ExecuteAsync(WmiRecovery.Step(true), progress, token).ConfigureAwait(false));
    public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
        RepairAsync(kind, folder, false, progress, token);

    public async Task<RepairResult> RepairAsync(RepairKind kind, string folder, bool demandStartAuthorized, IProgress<string>? progress, CancellationToken token)
    {
        if (kind == RepairKind.PrintQueues)
        {
            if (LastPrintQueueTargets is not { Count: > 0 })
                throw new InvalidOperationException("Fresh exact paused-queue and waiting-job evidence is required before queue resume.");
            return await Repairs.RunAsync(kind, folder, progress, selectedPrinterQueues: LastPrintQueueTargets,
                cancellationToken: token).ConfigureAwait(false);
        }
        if (kind != RepairKind.DnsCache)
            return await Repairs.RunAsync(kind, folder, demandStartAuthorized && ServiceRecovery.IsDemandStartKind(kind), progress,
                cancellationToken: token).ConfigureAwait(false);
        if (LastDnsLookup is null || !DnsCacheRecovery.IsEligible(LastDnsLookup))
            throw new InvalidOperationException("Fresh exact-hostname evidence is required before DNS cache recovery.");
        string dnsFolder = Path.Combine(folder, "dns-cache");
        if (File.Exists(Path.Combine(dnsFolder, "dns-cache.pending.json")))
        {
            var reconciliation = await DnsCacheReconciliation.RunAsync(dnsFolder,
                DnsRecoveryHost.ResolveAsync, progress, token: token).ConfigureAwait(false);
            return new(Environment.MachineName, kind, DateTimeOffset.UtcNow, reconciliation.Detail,
                reconciliation.ArchivePath ?? reconciliation.PendingPath, [])
            {
                DnsCacheReconciliation = reconciliation
            };
        }
        var recovery = await DnsCacheRecovery.RunAsync(LastDnsLookup, true, DnsRecoveryHost,
            DnsRecoveryJournalFactory(dnsFolder), progress, token: token).ConfigureAwait(false);
        IReadOnlyList<RepairStepResult> steps = recovery.Flush is null
            ? [] : [new(DnsCacheRecovery.FlushStep(), recovery.Flush)];
        return new(Environment.MachineName, kind, DateTimeOffset.UtcNow, recovery.Detail,
            recovery.JournalPath ?? Path.Combine(dnsFolder, "dns-cache.pending.json"), steps)
        {
            DnsCacheRecovery = recovery
        };
    }

    public static WorkflowCheck ReadNetworkConfiguration(string name,
        Func<(string[] Addresses, string[] Gateways, string[] Dns)> read) =>
        NetworkAssessment.ReadNetworkConfiguration(name, read);

    public static WorkflowCheck AssessNetwork(string name, string[] addresses, string[] gateways, string[] dns) =>
        NetworkAssessment.AssessNetwork(name, addresses, gateways, dns);
}
