using System.Globalization;

namespace KillerBench.Engine;

public static class WorkflowOptionProjection
{
    public static WindowsWorkflowHost CreateHost(WorkflowOptionValidationResult validation,
        IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(validation);
        EnsureReadOnly(validation);
        WorkflowOptionSet set = WorkflowOptionCatalog.Find(validation.WorkflowKey)
            ?? throw new ArgumentException("Unknown workflow option set.", nameof(validation));
        IReadOnlyDictionary<string, string> values = validation.Values;

        var host = new WindowsWorkflowHost
        {
            Progress = progress,
            OptionPlanCheck = CreatePlanCheck(set, values),
            NetworkLookupHost = Text(values, "lookupHost"),
            NetworkEndpoint = Endpoint(values),
            NetworkTimeout = TimeSpan.FromSeconds(Integer(values, "timeoutSeconds", 10)),
            AudioEndpointSelection = AudioSelection(values),
            StorageVolume = StorageVolume(values),
            FileSystemRepairSelection = FileSystemRepairSelection(values),
            VolumeOptimizationRequest = VolumeOptimizationSelection(values),
            SystemRestoreRequest = SystemRestoreSelection(values),
            PrintingTargetSelection = PrintingSelection(values),
            MsiProductCode = Text(values, "productCode"),
            ApplicationScope = EnumValue(values, "scope", InstalledApplicationScope.Machine),
            TimeOptions = TimeSelection(values),
            EventChannel = Text(values, "channel"),
            FirewallProfileSelection = FirewallSelection(values),
            ProfileMinimumDays = Integer(values, "minimumInactiveDays", 90),
            CheckProfileDependencies = Boolean(values, "checkDependencies"),
            ProposedComputerName = Text(values, "proposedName"),
            BitsAllUsers = values.GetValueOrDefault("scope") == "AllUsers",
            PackageScope = EnumValue(values, "scope", AppPackageScope.CurrentAccount),
            IncludeProvisionedPackages = Boolean(values, "includeProvisioned"),
            CleanupOptions = CleanupSelection(values),
            UpdateOptions = UpdateSelection(values),
            IncludeThirdPartyUpdates = Boolean(values, "thirdPartyApplications"),
            PrivacyOptions = PrivacySelection(values),
            BitLockerOptions = BitLockerSelection(values),
            BitLockerRecoveryProtector = GuidValue(values, "protectorId"),
            SecurityOptions = new(),
            MaceOptions = MaceSelection(values),
            OrcaOptions = OrcaSelection(values),
            DepotOptions = DepotSelection(values),
            BeamOptions = BeamSelection(values),
            DebloatOptions = DebloatSelection(values)
        };

        ValidateProjectedOptions(set.Kind, host);
        return host;
    }

    public static WorkflowCheck CreatePlanCheck(WorkflowOptionValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        EnsureReadOnly(validation);
        WorkflowOptionSet set = WorkflowOptionCatalog.Find(validation.WorkflowKey)
            ?? throw new ArgumentException("Unknown workflow option set.", nameof(validation));
        return CreatePlanCheck(set, validation.Values);
    }

    public static ServicingMonitorOptions CreateServicingOptions(WorkflowOptionValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        EnsureReadOnly(validation);
        if (WorkflowOptionCatalog.Find(validation.WorkflowKey)?.Kind != WorkflowKind.ServicingActivity)
            throw new ArgumentException("The option plan is not a servicing monitor plan.", nameof(validation));
        IReadOnlyDictionary<string, string> values = validation.Values;
        return new ServicingMonitorOptions(
            Integer(values, "refreshSeconds", 3), Integer(values, "samples", 3),
            Boolean(values, "includeEvents"), Boolean(values, "includeHistory"),
            Boolean(values, "includeServices"), Boolean(values, "includeRestart"),
            Boolean(values, "includeSetup"), Boolean(values, "includeCompatibility"),
            Boolean(values, "includeReadiness"), Boolean(values, "includeTransfers"),
            Boolean(values, "includeDeliveryOptimization"), Boolean(values, "includeInstallerActivity"),
            Boolean(values, "includeScheduledTask")).Validate();
    }

    private static void EnsureReadOnly(WorkflowOptionValidationResult validation)
    {
        if (!validation.IsValid)
            throw new ArgumentException("The workflow option plan must pass validation before use.", nameof(validation));
        if (validation.ChangesMachineRequested)
            throw new ArgumentException("Machine-changing options require their dedicated guarded command.", nameof(validation));
    }

    private static void ValidateProjectedOptions(WorkflowKind kind, WindowsWorkflowHost host)
    {
        switch (kind)
        {
            case WorkflowKind.Mace: host.MaceOptions.Validate(); break;
            case WorkflowKind.Orca: host.OrcaOptions.Validate(); break;
            case WorkflowKind.Depot: host.DepotOptions.Validate(); break;
            case WorkflowKind.Beam: host.BeamOptions.Validate(); break;
            case WorkflowKind.Debloat: host.DebloatOptions.Validate(); break;
            case WorkflowKind.BitLocker: host.BitLockerOptions.Validate(); break;
        }
    }

    private static (string Host, int Port)? Endpoint(IReadOnlyDictionary<string, string> values)
    {
        string? host = Text(values, "tcpHost");
        string? port = Text(values, "tcpPort");
        return host is null || port is null ? null : (host, int.Parse(port, CultureInfo.InvariantCulture));
    }

    private static FirewallProfileEnableOptions? FirewallSelection(IReadOnlyDictionary<string, string> values)
    {
        var profiles = new List<FirewallProfileTarget>();
        if (Boolean(values, "profile.domain")) profiles.Add(FirewallProfileTarget.Domain);
        if (Boolean(values, "profile.private")) profiles.Add(FirewallProfileTarget.Private);
        if (Boolean(values, "profile.public")) profiles.Add(FirewallProfileTarget.Public);
        return profiles.Count == 0 ? null : new FirewallProfileEnableOptions(profiles.AsReadOnly());
    }

    private static AudioEndpointSelection AudioSelection(IReadOnlyDictionary<string, string> values) => new(
        NullableEnum<AudioEndpointDirection>(values, "direction"), NullableEnum<AudioEndpointRole>(values, "role"));

    private static string? StorageVolume(IReadOnlyDictionary<string, string> values)
    {
        string? value = Text(values, "volume");
        return value == "Windows volume" ? null : value;
    }

    private static VolumeOptimizationRequest? VolumeOptimizationSelection(IReadOnlyDictionary<string, string> values) =>
        Boolean(values, "optimizationReview")
            ? VolumeOptimization.CreateRequest(StorageVolume(values) ?? StorageDiagnostics.SystemVolume(),
                EnumValue(values, "optimizationMode", VolumeOptimizationMode.Recommended))
            : null;

    private static FileSystemRepairSelection? FileSystemRepairSelection(IReadOnlyDictionary<string, string> values) =>
        Boolean(values, "repairReview")
            ? new(StorageVolume(values) ?? StorageDiagnostics.SystemVolume(),
                EnumValue(values, "repairMode", FileSystemRepairMode.SpotFix),
                Boolean(values, "allowDismount"), Boolean(values, "backupConfirmed"))
            : null;

    private static SystemRestoreCreationRequest? SystemRestoreSelection(IReadOnlyDictionary<string, string> values) =>
        Boolean(values, "prepare")
            ? SystemRestoreCreation.CreateRequest(Text(values, "description") ?? "Before KillerBench maintenance",
                EnumValue(values, "reason", SystemRestorePointReason.ModifySettings))
            : null;

    private static PrintingTargetSelection? PrintingSelection(IReadOnlyDictionary<string, string> values)
    {
        string? queue = Text(values, "queue"), jobQueue = Text(values, "jobQueue"), jobId = Text(values, "jobId");
        return queue is null && jobQueue is null && jobId is null ? null :
            new(queue, jobQueue, uint.TryParse(jobId, NumberStyles.None, CultureInfo.InvariantCulture, out uint parsed) ? parsed : null);
    }

    private static WorkflowCheck CreatePlanCheck(WorkflowOptionSet set, IReadOnlyDictionary<string, string> values)
    {
        var selections = new List<string>();
        foreach (WorkflowOptionDefinition option in set.Options)
        {
            if (option.ChangesMachine || !IsEnabled(option, values) || !values.TryGetValue(option.Key, out string? value) ||
                value == option.DefaultValue)
                continue;
            if (option.Control == WorkflowOptionControl.Toggle && value == "false") continue;
            selections.Add(option.Label + ": " + DisplayValue(set.WorkflowKey, option.Key, value));
        }

        string detail = selections.Count == 0
            ? "Default read-only settings are in effect. "
            : "Settings in effect: " + string.Join("; ", selections) + ". ";
        detail += "This diagnostic run does not approve or perform machine changes.";
        return new("Selected workflow options", CheckState.Information, detail);
    }

    private static bool IsEnabled(WorkflowOptionDefinition option, IReadOnlyDictionary<string, string> values) =>
        option.EnabledWhen is null || values.GetValueOrDefault(option.EnabledWhen.OptionKey) == option.EnabledWhen.Value;

    private static string DisplayValue(string workflowKey, string key, string value)
    {
        if (key is "backupFile" or "installSwitches" or "protectorId" ||
            workflowKey == "beam" && key == "source") return "supplied";
        if (key == "targetHostnames")
            return value.Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Length.ToString(CultureInfo.InvariantCulture) + " targets";
        string singleLine = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= 160 ? singleLine : singleLine[..157] + "...";
    }

    private static TimeOptions TimeSelection(IReadOnlyDictionary<string, string> values)
    {
        var source = EnumValue(values, "source", TimeSourceMode.Existing);
        string[] peers = source == TimeSourceMode.CustomPeers
            ? (Text(values, "peers") ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            : [];
        return new(source, peers);
    }

    private static CleanupOptions CleanupSelection(IReadOnlyDictionary<string, string> values) => new(
        Flags<CleanupTargets>(values, "target"), Integer(values, "minimumAgeDays", 7),
        Integer(values, "maximumFilesPerTarget", 100));

    private static UpdateSearchOptions UpdateSelection(IReadOnlyDictionary<string, string> values) => new(
        !values.ContainsKey("software") || Boolean(values, "software"), Boolean(values, "drivers"),
        EnumValue(values, "source", UpdateSearchSource.Configured));

    private static PrivacyOptions PrivacySelection(IReadOnlyDictionary<string, string> values) => new(
        Enum.GetValues<PrivacyArea>().Where(value => Boolean(values, "area." + Lower(value))).ToArray());

    private static BitLockerOptions BitLockerSelection(IReadOnlyDictionary<string, string> values) => new(
        Text(values, "volume") ?? "C:", EnumValue(values, "operation", BitLockerOperation.Inspect),
        EnumValue(values, "encryption", BitLockerEncryption.XtsAes128), Boolean(values, "usedSpaceOnly"),
        EnumValue(values, "backup", RecoveryDestination.None), Text(values, "backupFile"));

    private static MaceReviewOptions MaceSelection(IReadOnlyDictionary<string, string> values) => new(
        Flags<MaceTargets>(values, "target"), EnumValue(values, "scope", MaceScope.RunningAccount),
        !values.ContainsKey("preserveApplicationData") || Boolean(values, "preserveApplicationData"),
        !values.ContainsKey("preserveCachedCredentials") || Boolean(values, "preserveCachedCredentials"),
        Boolean(values, "restoreOneDriveFolderLocations"));

    private static OrcaReviewOptions OrcaSelection(IReadOnlyDictionary<string, string> values) => new(
        EnumValue(values, "mode", OrcaOutlookMode.Both), EnumValue(values, "scope", OrcaScope.RunningAccount),
        Flags<OrcaActions>(values, "action"));

    private static DepotOptions DepotSelection(IReadOnlyDictionary<string, string> values) => new(
        Flags<DepotApplications>(values, "application"), Flags<DepotConfiguration>(values, "configuration"));

    private static BeamOptions BeamSelection(IReadOnlyDictionary<string, string> values)
    {
        string source = Text(values, "source") ?? "";
        string[] targets = (Text(values, "targetHostnames") ?? "")
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new(source.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? BeamSourceKind.HttpsUrl : BeamSourceKind.LocalFile,
            source, EnumValue(values, "installerType", BeamInstallerType.Exe), Text(values, "appName") ?? "",
            Text(values, "publisher") ?? "", Text(values, "installSwitches") ?? "", targets,
            Text(values, "tenantDomain") ?? "");
    }

    private static DebloatOptions DebloatSelection(IReadOnlyDictionary<string, string> values)
    {
        DebloatAreas areas = Flags<DebloatAreas>(values, "area");
        DebloatPowerRequest? power = areas.HasFlag(DebloatAreas.SleepAndDisplayTimers)
            ? new((uint)Integer(values, "sleepAcMinutes", 0), (uint)Integer(values, "sleepDcMinutes", 0),
                (uint)Integer(values, "displayAcMinutes", 0), (uint)Integer(values, "displayDcMinutes", 0))
            : null;
        return new(areas, EnumValue(values, "userScope", DebloatUserScope.RunningAccount), true, true,
            Text(values, "retainedOfficeCulture"), power);
    }

    private static T Flags<T>(IReadOnlyDictionary<string, string> values, string prefix) where T : struct, Enum
    {
        ulong result = 0;
        foreach (T value in Enum.GetValues<T>())
            if (Convert.ToUInt64(value, CultureInfo.InvariantCulture) != 0 && Boolean(values, prefix + "." + Lower(value)))
                result |= Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        return (T)Enum.ToObject(typeof(T), result);
    }

    private static T EnumValue<T>(IReadOnlyDictionary<string, string> values, string key, T fallback) where T : struct, Enum =>
        values.TryGetValue(key, out string? value) && Enum.TryParse(value, false, out T parsed) ? parsed : fallback;

    private static T? NullableEnum<T>(IReadOnlyDictionary<string, string> values, string key) where T : struct, Enum =>
        values.TryGetValue(key, out string? value) && value != "All" && Enum.TryParse(value, false, out T parsed) ? parsed : null;

    private static Guid? GuidValue(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && Guid.TryParse(value, out Guid parsed) ? parsed : null;

    private static int Integer(IReadOnlyDictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out string? value) && int.TryParse(value, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;

    private static bool Boolean(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && value == "true";

    private static string? Text(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string Lower<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
