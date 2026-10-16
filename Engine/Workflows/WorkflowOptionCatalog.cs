using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace KillerBench.Engine.Workflows;

public enum WorkflowOptionControl
{
    Toggle,
    Slider,
    Choice,
    Text
}

public sealed record WorkflowOptionChoice(string Value, string Label);

public sealed record WorkflowOptionCondition(string OptionKey, string Value);

public sealed record WorkflowOptionDefinition(
    string Key,
    string Label,
    WorkflowOptionControl Control,
    string DefaultValue,
    string Description,
    int? Minimum = null,
    int? Maximum = null,
    int? Step = null,
    IReadOnlyList<WorkflowOptionChoice>? Choices = null,
    bool Advanced = false,
    bool Required = false,
    bool ChangesMachine = false,
    WorkflowOptionCondition? EnabledWhen = null);

public sealed record WorkflowOptionSet(
    WorkflowKind Kind,
    string WorkflowKey,
    string Title,
    IReadOnlyList<WorkflowOptionDefinition> Options,
    string ExecutionBoundary);

public static partial class WorkflowOptionCatalog
{
    private static readonly Dictionary<WorkflowKind, WorkflowOptionSet> ByKind = Build();

    public static IReadOnlyList<WorkflowOptionSet> All { get; } = Array.AsReadOnly(
        WorkflowCatalog.All.Where(item => ByKind.ContainsKey(item.Kind)).Select(item => ByKind[item.Kind]).ToArray());

    public static WorkflowOptionSet? ForWorkflow(WorkflowKind kind) => ByKind.GetValueOrDefault(kind);

    public static WorkflowOptionSet? Find(string workflowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowKey);
        return All.FirstOrDefault(item => item.WorkflowKey.Equals(workflowKey.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<WorkflowKind, WorkflowOptionSet> Build()
    {
        var sets = new[]
        {
            Set(WorkflowKind.Network, NetworkOptions(),
                "Targets are optional and diagnostic only. A hostname lookup uses the configured resolver. A TCP check connects only to one exact host and port and sends no application data."),
            Set(WorkflowKind.Audio, AudioOptions(),
                "Direction and role limit default-endpoint detail only. Every audio device and driver remains in the diagnostic workflow, and no endpoint, format, level, route or service changes."),
            Set(WorkflowKind.SystemRestore, SystemRestoreOptions(),
                "Inventory is the default. Creating one restore point requires an explicit request and execution approval, fresh protection, policy, frequency and administrator checks, durable preparation and exact verification."),
            Set(WorkflowKind.Storage, StorageOptions(),
                "Diagnosis is the default. Filesystem repair and drive optimization are mutually exclusive exact-volume actions. Execution requires separate approval, fresh eligibility, durable preparation, bounded dispatch and verification."),
            Set(WorkflowKind.Printing, PrintingOptions(),
                "General diagnosis is the default. Queue repair, default selection and job cancellation are mutually exclusive exact-target actions. Every change requires explicit approval, fresh repeated evidence, durable preparation and exact verification."),
            Set(WorkflowKind.Installer, InstallerOptions(),
                "General Windows Installer diagnosis is the default. An optional exact MSI product code narrows product checks. Execution requires explicit approval, fresh eligibility, durable preparation, bounded repair and exact verification."),
            Set(WorkflowKind.TimeService, TimeOptions(),
                "Existing-source diagnosis is the default. Domain hierarchy or custom peers require an explicit selection. Applying a source and synchronizing requires separate approval, fresh role, policy and administrator checks, durable preparation and verification."),
            Set(WorkflowKind.EventLog, EventLogOptions(),
                "All supported channels are diagnosed by default. Enabling one exact disabled channel requires explicit approval, fresh unmanaged configuration and administrator evidence, durable preparation and collateral-setting verification."),
            Set(WorkflowKind.Profiles, ProfileOptions(),
                "Profile review is read-only. The inactivity threshold and dependency inspection affect review scope only. Backup, verification and removal remain separate exact-profile operations."),
            Set(WorkflowKind.ComputerName, ComputerNameOptions(),
                "Name review is read-only by default. Renaming requires explicit approval, fresh workgroup identity and restart evidence, administrator access, durable preparation and exact post-restart reconciliation. KillerBench never restarts Windows automatically."),
            Set(WorkflowKind.BackgroundTransfers, BackgroundTransferOptions(),
                "Transfer inventory and selected-job review are read-only. All-users inventory requires administrator access. Observation and exact-job retry remain separate guarded actions and no job is completed, canceled, deleted or reassigned."),
            Set(WorkflowKind.AppPackages, AppPackageOptions(),
                "Package inventory and selected-package review are read-only. All-users and provisioned-package inventory require administrator access. No package is launched, installed, repaired, reset or removed."),
            Set(WorkflowKind.InstalledApps, InstalledApplicationOptions(),
                "Application registration inventory and correlated stability history are read-only. The selected machine or running-account scope does not establish ownership, health or uninstall eligibility."),
            Set(WorkflowKind.Search, SearchOptions(),
                "Search diagnosis is the default. Index rebuild requires explicit approval, fresh servicing, restart, service and directory eligibility, durable preparation, bounded execution and exact verification. The prior index is retained and never deleted automatically."),
            Set(WorkflowKind.ComponentCleanup, ComponentCleanupOptions(),
                "Component-store analysis is the default. Cleanup runs only when Windows freshly recommends it, servicing is idle and explicit execution is requested. The operation is bounded, journaled and verified without ResetBase, manual WinSxS deletion or automatic restart."),
            Set(WorkflowKind.WindowsRecovery, WindowsRecoveryOptions(),
                "Recovery Environment inspection is the default. Enablement requires explicit approval, fresh exact Disabled evidence, administrator access, durable preparation, bounded execution and exact verification. Interrupted attempts require a later boot and are never repeated automatically."),
            Set(WorkflowKind.FirewallServices, FirewallOptions(),
                "Firewall diagnosis is the default. Enabling profiles requires exact selections and explicit approval. Fresh evidence must confirm every target is disabled and unmanaged before one bounded, journaled change and exact verification. Rules, default actions, logging and managed policy are preserved."),
            Set(WorkflowKind.Cleanup, CleanupOptions(),
                "Preview is read-only. Permanent file removal requires the explicit execute option, fresh candidate validation and a durable journal. Previous Windows installation removal is a separate backup-confirmed action."),
            Set(WorkflowKind.AvailableUpdates, AvailableUpdateOptions(),
                "Discovery is read-only. Update installation uses a separate exact-selection plan and never follows from discovery alone."),
            Set(WorkflowKind.Privacy, PrivacyOptions(),
                "These controls select read-only privacy inspection areas. Supported changes use separate policy-preserving coordinators."),
            Set(WorkflowKind.BitLocker, BitLockerOptions(),
                "Inspection is the default. Backup, encryption and protection resume are mutually exclusive explicit actions. Each uses its separate guarded coordinator with fresh exact volume, policy, protector, restart and administrator evidence, durable preparation, bounded execution, verification and no-repeat reconciliation."),
            Set(WorkflowKind.Defender, DefenderOptions(),
                "Diagnosis is the default. Quick and Full scans require explicit execution, fresh active-protection and idle-scan evidence, durable preparation and completion verification."),
            Set(WorkflowKind.Mace, MaceOptions(),
                "Microsoft app removal currently produces a removal review only. No application, package, credential, file or user data is changed."),
            Set(WorkflowKind.Orca, OrcaOptions(),
                "Outlook reset reviews every selected area. Running-account cache-only requests can use a separate preview-first retention coordinator. Other Outlook data, packages, credentials, profiles and add-ins remain unchanged."),
            Set(WorkflowKind.Depot, DepotOptions(),
                "Onboarding currently produces a provisioning review only. No download, installation, account or configuration change occurs."),
            Set(WorkflowKind.Debloat, DebloatOptions(),
                "Review is read-only. The three power actions require their matching area as the only selection, fresh evidence and explicit execution."),
            Set(WorkflowKind.ServicingActivity, ServicingOptions(),
                "Servicing monitor collects bounded point-in-time evidence only. It never starts, stops or changes servicing activity.")
        };
        return sets.ToDictionary(item => item.Kind);
    }

    private static WorkflowOptionSet Set(WorkflowKind kind, IReadOnlyList<WorkflowOptionDefinition> options, string boundary)
    {
        WorkflowDefinition definition = WorkflowCatalog.All.Single(item => item.Kind == kind);
        return new(kind, definition.Key, definition.Title, options, boundary);
    }

    private static ReadOnlyCollection<WorkflowOptionDefinition> NetworkOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Text("lookupHost", "Hostname lookup", "", "Optional hostname to resolve with the configured system resolver."),
        Text("tcpHost", "TCP destination", "", "Optional exact hostname or IP address for one connection attempt."),
        Text("tcpPort", "TCP port", "", "Port from 1 through 65535. Supply it together with a TCP destination."),
        Slider("timeoutSeconds", "Timeout", 10, 1, 60, 1, "Maximum seconds for each selected lookup or connection attempt.")
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> AudioOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("direction", "Default endpoint direction", "All", Array.AsReadOnly<WorkflowOptionChoice>(
        [
            new WorkflowOptionChoice("All", "Input and output"),
            new WorkflowOptionChoice(nameof(AudioEndpointDirection.Output), "Output"),
            new WorkflowOptionChoice(nameof(AudioEndpointDirection.Input), "Input")
        ]), "Limit default-endpoint detail by direction. Device and driver checks remain complete."),
        Choice("role", "Windows role", "All", Array.AsReadOnly<WorkflowOptionChoice>(
        [
            new WorkflowOptionChoice("All", "All roles"),
            new WorkflowOptionChoice(nameof(AudioEndpointRole.Console), "Console"),
            new WorkflowOptionChoice(nameof(AudioEndpointRole.Multimedia), "Multimedia"),
            new WorkflowOptionChoice(nameof(AudioEndpointRole.Communications), "Communications")
        ]), "Limit default-endpoint detail to one Windows audio role.")
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> SystemRestoreOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Toggle("prepare", "Prepare one restore point", false,
            "Collect fresh creation eligibility for one restore point."),
        Text("description", "Description", "Before KillerBench maintenance",
            "Description from 1 through 64 characters.", required: true, enabledWhen: new("prepare", "true")),
        Choice("reason", "Reason", nameof(SystemRestorePointReason.ModifySettings),
            EnumChoices<SystemRestorePointReason>(), "Choose the Windows restore-point reason.",
            enabledWhen: new("prepare", "true")),
        Toggle("execute", "Create after fresh checks", false,
            "Request one guarded restore point after current eligibility is confirmed.", advanced: true,
            changesMachine: true, enabledWhen: new("prepare", "true"))
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> StorageOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Text("volume", "Volume", "Windows volume", "Windows volume or an exact drive-letter volume such as D:."),
        Toggle("repairReview", "Review filesystem repair", false,
            "Collect current corruption evidence and review one bounded filesystem operation."),
        Choice("repairMode", "Filesystem repair mode", nameof(FileSystemRepairMode.SpotFix),
            EnumChoices<FileSystemRepairMode>(), "Choose spot repair or offline scan and repair.",
            enabledWhen: new("repairReview", "true")),
        Toggle("allowDismount", "Allow non-system dismount", false,
            "Allow an eligible non-system volume to go offline during repair.",
            enabledWhen: new("repairReview", "true")),
        Toggle("backupConfirmed", "Important data is backed up", false,
            "Confirm important data is backed up before filesystem repair execution.",
            enabledWhen: new("repairReview", "true")),
        Toggle("optimizationReview", "Analyze drive optimization", false,
            "Analyze one exact fixed volume and its media compatibility."),
        Choice("optimizationMode", "Optimization method", nameof(VolumeOptimizationMode.Recommended),
            EnumChoices<VolumeOptimizationMode>(), "Choose Windows recommended, retrim or defragment.",
            enabledWhen: new("optimizationReview", "true")),
        Toggle("execute", "Run selected storage action", false,
            "Run the one selected action only after fresh evidence and final eligibility checks.",
            advanced: true, changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> PrintingOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Text("queue", "Printer queue", "", "Exact local printer queue. Leave empty for general printing diagnosis."),
        Toggle("clearOffline", "Clear offline mode", false,
            "Repair offline mode for the exact selected queue after fresh evidence.", advanced: true, changesMachine: true),
        Toggle("makeDefault", "Make default printer", false,
            "Set the exact selected queue as the default after fresh conflict checks.", advanced: true, changesMachine: true),
        Text("jobQueue", "Blocked job queue", "", "Exact queue containing the selected blocked job."),
        Text("jobId", "Blocked job ID", "", "Exact nonzero Windows print job ID."),
        Toggle("cancelJob", "Cancel selected job", false,
            "Cancel only the exact repeatedly blocked job after durable preparation.", advanced: true, changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> InstallerOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("scope", "Application registry scope", nameof(InstalledApplicationScope.Machine),
            EnumChoices<InstalledApplicationScope>(), "Choose machine or current-account registrations for MSI application discovery."),
        Text("productCode", "MSI product code", "",
            "Optional exact MSI product GUID enclosed in braces. Leave empty for general Installer checks."),
        Toggle("execute", "Repair confirmed Installer damage", false,
            "Run only a freshly eligible Installer registration or exact selected-product repair.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> TimeOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("source", "Time source", nameof(TimeSourceMode.Existing), EnumChoices<TimeSourceMode>(),
            "Keep the configured source, use domain hierarchy or select custom NTP peers."),
        Text("peers", "Custom NTP peers", "", "One to eight unique hostnames or IP addresses separated by spaces.",
            required: true, enabledWhen: new("source", nameof(TimeSourceMode.CustomPeers))),
        Toggle("execute", "Apply source and synchronize", false,
            "Apply the selected non-default source and request synchronization after fresh safeguards.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> EventLogOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("channel", "Diagnostic event channel", "System", Array.AsReadOnly<WorkflowOptionChoice>(
        [
            new WorkflowOptionChoice("System", "System"),
            new WorkflowOptionChoice("Application", "Application"),
            new WorkflowOptionChoice("Setup", "Setup"),
            new WorkflowOptionChoice("Microsoft-Windows-WindowsUpdateClient/Operational", "Windows Update Client / Operational")
        ]), "Choose one exact supported channel for optional enablement. Diagnosis still inspects every supported channel."),
        Toggle("execute", "Enable selected channel", false,
            "Enable only the exact selected disabled and unmanaged channel after fresh checks.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> ProfileOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Slider("minimumInactiveDays", "Minimum days since last use", 90, 1, 3650, 1,
            "Review nonprotected profiles whose last use is at least this many days old."),
        Toggle("checkDependencies", "Check profile dependencies", true,
            "Include service and scheduled-task references in the read-only review.")
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> ComputerNameOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Text("proposedName", "Proposed computer name", "",
            "Use 1 to 63 ASCII letters, digits or internal hyphens, including at least one letter or hyphen.", required: true),
        Toggle("execute", "Rename after fresh checks", false,
            "Rename this workgroup PC only after fresh identity, restart and administrator checks.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> BackgroundTransferOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("scope", "BITS account scope", "RunningAccount", Array.AsReadOnly<WorkflowOptionChoice>(
        [
            new WorkflowOptionChoice("RunningAccount", "Running account"),
            new WorkflowOptionChoice("AllUsers", "All users")
        ]), "Choose current-account inventory or administrator-only all-users inventory.")
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> AppPackageOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("scope", "Package account scope", nameof(AppPackageScope.CurrentAccount),
            EnumChoices<AppPackageScope>(), "Choose the running account or all registered users."),
        Toggle("includeProvisioned", "Include packages for new users", false,
            "Also inventory packages provisioned in the Windows image. Administrator access is required.", advanced: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> InstalledApplicationOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("scope", "Application registration scope", nameof(InstalledApplicationScope.Machine),
            EnumChoices<InstalledApplicationScope>(), "Choose machine-wide or running-account uninstall registrations.")
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> SearchOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Toggle("rebuildIndex", "Rebuild the Windows Search index", false,
            "Retain the current index and rebuild only after fresh eligibility and explicit approval.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> ComponentCleanupOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Toggle("execute", "Run recommended component cleanup", false,
            "Remove superseded components only after a fresh Windows recommendation and servicing checks.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> WindowsRecoveryOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Toggle("execute", "Enable Windows Recovery Environment", false,
            "Enable WinRE only after fresh exact eligibility and administrator checks.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> FirewallOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Toggle("profile.domain", "Domain profile", false, "Select the local Domain profile for guarded enablement."),
        Toggle("profile.private", "Private profile", false, "Select the local Private profile for guarded enablement."),
        Toggle("profile.public", "Public profile", false, "Select the local Public profile for guarded enablement."),
        Toggle("execute", "Enable selected local profiles", false,
            "Enable only selected profiles confirmed disabled and unmanaged by fresh evidence.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> CleanupOptions()
    {
        var options = Flags<CleanupTargets>("target", "Include", CleanupTargets.UserTemp,
            "Include this target in the bounded cleanup preview.");
        options.Add(Slider("minimumAgeDays", "Minimum age", 7, 1, 365, 1,
            "Only ordinary candidates at least this many days old are retained. Recycle Bin cleanup ignores age."));
        options.Add(Slider("maximumFilesPerTarget", "File limit per target", 100, 1, 10_000, 1,
            "Limit retained candidates per ordinary target and bound removal review."));
        options.Add(Toggle("execute", "Permanently remove verified candidates", false,
            "Run the guarded removal after preview and fresh validation.", advanced: true, changesMachine: true));
        return options.AsReadOnly();
    }

    private static ReadOnlyCollection<WorkflowOptionDefinition> AvailableUpdateOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Toggle("software", "Software updates", true, "Search for software updates from the selected source."),
        Toggle("drivers", "Driver updates", false, "Include driver updates in discovery."),
        Toggle("thirdPartyApplications", "Third-party applications", false, "Include separate read-only application update discovery."),
        Choice("source", "Update source", nameof(UpdateSearchSource.Configured), EnumChoices<UpdateSearchSource>(),
            "Use configured policy, public Windows Update or registered Microsoft Update.")
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> PrivacyOptions() => Array.AsReadOnly(
        PrivacyCatalog.All.Select(item => Toggle("area." + Lower(item.Area), item.Title, false, item.Impact)).ToArray());

    private static ReadOnlyCollection<WorkflowOptionDefinition> BitLockerOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Text("volume", "Volume", "C:", "Drive-letter volume such as C:.", required: true),
        Choice("operation", "Operation", nameof(BitLockerOperation.Inspect), EnumChoices<BitLockerOperation>(),
            "Choose inspection, recovery backup, encryption planning or protection resume planning."),
        Choice("encryption", "Encryption method", nameof(BitLockerEncryption.XtsAes128), EnumChoices<BitLockerEncryption>(),
            "Select the requested encryption method. Policy and current volume state are verified separately."),
        Toggle("usedSpaceOnly", "Encrypt used space only", false, "Request used-space-only encryption for a new encryption operation."),
        Choice("backup", "Recovery key backup", nameof(RecoveryDestination.None), EnumChoices<RecoveryDestination>(),
            "Choose where recovery information must be backed up before an eligible operation."),
        Text("backupFile", "Recovery backup file", "", "Absolute path outside the protected volume.",
            enabledWhen: new("backup", nameof(RecoveryDestination.File))),
        Text("protectorId", "Recovery protector ID", "",
            "Exact discovered recovery-password protector GUID required for file backup or encryption execution.", advanced: true),
        Toggle("executeBackup", "Write selected recovery backup", false,
            "Write one new verified recovery backup file for the exact selected protector.", advanced: true,
            changesMachine: true),
        Toggle("executeEncryption", "Back up recovery information and start encryption", false,
            "Verify a new backup, then start encryption with the selected method.", advanced: true,
            changesMachine: true),
        Toggle("executeResume", "Resume BitLocker protection", false,
            "Resume protection only on the exact fully encrypted eligible volume.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> DefenderOptions() => Array.AsReadOnly<WorkflowOptionDefinition>(
    [
        Choice("scanType", "Action", "None", Array.AsReadOnly<WorkflowOptionChoice>(
        [
            new WorkflowOptionChoice("None", "Diagnosis only"),
            new WorkflowOptionChoice(nameof(DefenderScanType.Quick), "Quick scan"),
            new WorkflowOptionChoice(nameof(DefenderScanType.Full), "Full scan")
        ]), "Choose diagnosis only, one Quick scan or one Full scan."),
        Toggle("startScan", "Start selected scan", false,
            "Start the selected scan only after fresh eligibility and durable preparation.", advanced: true,
            changesMachine: true)
    ]);

    private static ReadOnlyCollection<WorkflowOptionDefinition> MaceOptions()
    {
        var options = Flags<MaceTargets>("target", "Review", MaceTargets.None,
            "Include this exact Microsoft application family in the removal review.");
        options.Add(Choice("scope", "User scope", nameof(MaceScope.RunningAccount), EnumChoices<MaceScope>(),
            "Review the running account or all registered users."));
        options.Add(Toggle("preserveApplicationData", "Preserve application data", true,
            "Keep application data in the review plan."));
        options.Add(Toggle("preserveCachedCredentials", "Preserve cached credentials", true,
            "Keep cached credentials in the review plan."));
        options.Add(Toggle("restoreOneDriveFolderLocations", "Review OneDrive folder restoration", false,
            "Review restoring redirected OneDrive folder locations.", advanced: true,
            enabledWhen: new("target.oneDrive", "true")));
        return options.AsReadOnly();
    }

    private static ReadOnlyCollection<WorkflowOptionDefinition> OrcaOptions()
    {
        var options = new List<WorkflowOptionDefinition>
        {
            Choice("mode", "Outlook edition", nameof(OrcaOutlookMode.Both), EnumChoices<OrcaOutlookMode>(),
                "Review New Outlook, Classic Outlook or both."),
            Choice("scope", "User scope", nameof(OrcaScope.RunningAccount), EnumChoices<OrcaScope>(),
                "Review the running account or all registered users.")
        };
        options.AddRange(Flags<OrcaActions>("action", "Review", OrcaActions.ClearCaches,
            "Include this action in the evidence review."));
        return options.AsReadOnly();
    }

    private static ReadOnlyCollection<WorkflowOptionDefinition> DepotOptions()
    {
        var options = Flags<DepotApplications>("application", "Review", DepotApplications.None,
            "Review current registration evidence for this application.");
        options.AddRange(Flags<DepotConfiguration>("configuration", "Review", DepotConfiguration.None,
            "Include this configuration group in the provisioning review."));
        return options.AsReadOnly();
    }

    private static ReadOnlyCollection<WorkflowOptionDefinition> DebloatOptions()
    {
        var options = Flags<DebloatAreas>("area", "Review", DebloatAreas.None,
            "Include this Windows cleanup area in the evidence review.");
        options.Add(Choice("userScope", "User scope", nameof(DebloatUserScope.RunningAccount), EnumChoices<DebloatUserScope>(),
            "Review the running account or all profiles plus the default profile."));
        options.Add(Text("retainedOfficeCulture", "Office language to keep", "", "Language-region value such as en-us.",
            enabledWhen: new("area.officeLanguages", "true")));
        foreach (string key in new[] { "sleepAcMinutes", "sleepDcMinutes", "displayAcMinutes", "displayDcMinutes" })
            options.Add(Slider(key, Label(key), 0, 0, 1440, 1, "Requested timeout in minutes. Zero means Never.",
                enabledWhen: new("area.sleepAndDisplayTimers", "true")));
        options.Add(Toggle("applyHighPerformance", "Apply High performance plan", false,
            "Run the guarded power-plan action when it is the only selected area.", true, true,
            new("area.highPerformancePower", "true")));
        options.Add(Toggle("applyPowerTimers", "Apply power timers", false,
            "Run the guarded timer action when it is the only selected area.", true, true,
            new("area.sleepAndDisplayTimers", "true")));
        options.Add(Toggle("disableHibernation", "Disable hibernation", false,
            "Run the guarded hibernation action when it is the only selected area.", true, true,
            new("area.hibernation", "true")));
        return options.AsReadOnly();
    }

    private static ReadOnlyCollection<WorkflowOptionDefinition> ServicingOptions()
    {
        var options = new List<WorkflowOptionDefinition>
        {
            Slider("refreshSeconds", "Refresh interval", 3, 1, 60, 1, "Seconds between live snapshots."),
            Slider("samples", "Snapshots", 3, 1, 120, 1, "Number of bounded snapshots to collect.")
        };
        foreach ((string key, string label) in new[]
        {
            ("includeEvents", "Windows Update events"), ("includeHistory", "Update history"),
            ("includeServices", "Servicing services"), ("includeRestart", "Restart indicators"),
            ("includeSetup", "Setup activity"), ("includeCompatibility", "Compatibility evidence"),
            ("includeReadiness", "Servicing readiness"), ("includeTransfers", "Background transfers"),
            ("includeDeliveryOptimization", "Delivery Optimization"), ("includeInstallerActivity", "Installer activity"),
            ("includeScheduledTask", "Windows Update scheduled task")
        }) options.Add(Toggle(key, label, true, "Include this read-only evidence group in each snapshot."));
        return options.AsReadOnly();
    }

    private static List<WorkflowOptionDefinition> Flags<T>(string prefix, string verb, T defaults, string description) where T : struct, Enum =>
        [.. Enum.GetValues<T>().Where(value => Convert.ToUInt64(value) != 0).Select(value =>
            Toggle(prefix + "." + Lower(value), verb + " " + Label(value.ToString()),
                (Convert.ToUInt64(defaults) & Convert.ToUInt64(value)) != 0, description))];

    private static WorkflowOptionDefinition Toggle(string key, string label, bool value, string description,
        bool advanced = false, bool changesMachine = false, WorkflowOptionCondition? enabledWhen = null) =>
        new(key, label, WorkflowOptionControl.Toggle, value ? "true" : "false", description,
            Advanced: advanced, ChangesMachine: changesMachine, EnabledWhen: enabledWhen);

    private static WorkflowOptionDefinition Slider(string key, string label, int value, int minimum, int maximum,
        int step, string description, WorkflowOptionCondition? enabledWhen = null) =>
        new(key, label, WorkflowOptionControl.Slider, value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            description, minimum, maximum, step, EnabledWhen: enabledWhen);

    private static WorkflowOptionDefinition Choice(string key, string label, string value,
        IReadOnlyList<WorkflowOptionChoice> choices, string description, WorkflowOptionCondition? enabledWhen = null) =>
        new(key, label, WorkflowOptionControl.Choice, value, description, Choices: choices, EnabledWhen: enabledWhen);

    private static WorkflowOptionDefinition Text(string key, string label, string value, string description,
        bool required = false, bool advanced = false, WorkflowOptionCondition? enabledWhen = null) =>
        new(key, label, WorkflowOptionControl.Text, value, description, Advanced: advanced,
            Required: required, EnabledWhen: enabledWhen);

    private static ReadOnlyCollection<WorkflowOptionChoice> EnumChoices<T>() where T : struct, Enum => Array.AsReadOnly(
        Enum.GetValues<T>().Select(value => new WorkflowOptionChoice(value.ToString(), Label(value.ToString()))).ToArray());

    private static string Lower<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex LabelBoundaryPattern();

    private static string Label(string value) => LabelBoundaryPattern().Replace(value, " ")
        .Replace("Ai", "AI", StringComparison.Ordinal)
        .Replace("Msi", "MSI", StringComparison.Ordinal)
        .Replace("Exe", "EXE", StringComparison.Ordinal);
}
