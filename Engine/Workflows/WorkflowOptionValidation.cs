using System.Collections.ObjectModel;
using System.Globalization;

namespace KillerBench.Engine;

public sealed record WorkflowOptionValidationError(string Key, string Message);

public sealed record WorkflowOptionValidationResult(
    string WorkflowKey,
    bool IsValid,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<WorkflowOptionValidationError> Errors,
    bool ChangesMachineRequested);

public static class WorkflowOptionValidation
{
    public const int MaximumTextLength = 4096;

    public static WorkflowOptionValidationResult Validate(string workflowKey,
        IEnumerable<KeyValuePair<string, string>> supplied)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowKey);
        ArgumentNullException.ThrowIfNull(supplied);

        WorkflowOptionSet set = WorkflowOptionCatalog.Find(workflowKey)
            ?? throw new ArgumentException("Unknown workflow option set.", nameof(workflowKey));
        var provided = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<WorkflowOptionValidationError>();
        foreach ((string key, string value) in supplied)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add(new("", "Option keys cannot be empty."));
                continue;
            }
            string normalizedKey = key.Trim();
            if (!provided.TryAdd(normalizedKey, value ?? ""))
                errors.Add(new(normalizedKey, "The option was supplied more than once."));
        }

        var definitions = set.Options.ToDictionary(option => option.Key, StringComparer.OrdinalIgnoreCase);
        foreach (string key in provided.Keys.Where(key => !definitions.ContainsKey(key)).Order(StringComparer.Ordinal))
            errors.Add(new(key, "The workflow does not define this option."));

        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (WorkflowOptionDefinition option in set.Options)
        {
            string value = provided.GetValueOrDefault(option.Key, option.DefaultValue);
            if (TryNormalize(option, value, out string result, out string? message))
                normalized.Add(option.Key, result);
            else
            {
                normalized.Add(option.Key, option.DefaultValue);
                errors.Add(new(option.Key, message!));
            }
        }

        foreach (WorkflowOptionDefinition option in set.Options)
        {
            bool enabled = option.EnabledWhen is null ||
                normalized.GetValueOrDefault(option.EnabledWhen.OptionKey) == option.EnabledWhen.Value;
            if (!enabled && provided.ContainsKey(option.Key) && normalized[option.Key] != option.DefaultValue)
                errors.Add(new(option.Key, "The option is not available with the selected values."));
            if (enabled && option.Required && string.IsNullOrWhiteSpace(normalized[option.Key]))
                errors.Add(new(option.Key, "A value is required."));
        }

        if (set.Kind == WorkflowKind.Defender && normalized.GetValueOrDefault("startScan") == "true" &&
            normalized.GetValueOrDefault("scanType") == "None")
            errors.Add(new("startScan", "Choose Quick or Full before requesting a scan."));

        if (set.Kind == WorkflowKind.Network)
            ValidateNetwork(normalized, errors);

        if (set.Kind == WorkflowKind.AvailableUpdates)
            ValidateUpdates(normalized, errors);

        if (set.Kind == WorkflowKind.SystemRestore && normalized.GetValueOrDefault("prepare") == "true")
        {
            try
            {
                SystemRestoreCreation.CreateRequest(normalized["description"],
                    Enum.Parse<SystemRestorePointReason>(normalized["reason"], ignoreCase: false));
            }
            catch (ArgumentException error)
            {
                errors.Add(new(error.ParamName == "reason" ? "reason" : "description", error.Message));
            }
        }

        if (set.Kind == WorkflowKind.Storage)
            ValidateStorage(normalized, errors);

        if (set.Kind == WorkflowKind.Printing)
            ValidatePrinting(normalized, errors);

        if (set.Kind == WorkflowKind.Installer)
            ValidateInstaller(normalized, errors);

        if (set.Kind == WorkflowKind.TimeService)
            ValidateTime(normalized, errors);

        if (set.Kind == WorkflowKind.EventLog)
        {
            try { normalized["channel"] = EventChannelEnable.ValidateChannel(normalized["channel"]); }
            catch (ArgumentException error) { errors.Add(new("channel", error.Message)); }
        }

        if (set.Kind == WorkflowKind.ComputerName)
        {
            try
            {
                normalized["proposedName"] = ComputerNameReview.Review(normalized["proposedName"],
                    new(null, null, null)).ProposedName;
            }
            catch (ArgumentException error)
            {
                errors.Add(new("proposedName", error.Message));
            }
        }

        if (set.Kind == WorkflowKind.FirewallServices && normalized.GetValueOrDefault("execute") == "true" &&
            !new[] { "profile.domain", "profile.private", "profile.public" }
                .Any(key => normalized.GetValueOrDefault(key) == "true"))
            errors.Add(new("execute", "Choose at least one firewall profile before requesting enablement."));

        if (set.Kind == WorkflowKind.BitLocker)
            ValidateBitLocker(normalized, errors);

        bool changesMachine = set.Options.Any(option => option.ChangesMachine &&
            normalized.GetValueOrDefault(option.Key) == "true");
        return new(set.WorkflowKey, errors.Count == 0,
            new ReadOnlyDictionary<string, string>(normalized), errors.AsReadOnly(), changesMachine);
    }

    private static void ValidateNetwork(Dictionary<string, string> values, List<WorkflowOptionValidationError> errors)
    {
        string lookup = values["lookupHost"].Trim();
        string tcpHost = values["tcpHost"].Trim();
        string tcpPort = values["tcpPort"].Trim();
        values["lookupHost"] = lookup;
        values["tcpHost"] = tcpHost;
        values["tcpPort"] = tcpPort;

        if (lookup.Length > 0)
        {
            try { values["lookupHost"] = DnsDiagnostics.ValidateHost(lookup); }
            catch (ArgumentException error) { errors.Add(new("lookupHost", error.Message)); }
        }

        if ((tcpHost.Length == 0) != (tcpPort.Length == 0))
        {
            errors.Add(new(tcpHost.Length == 0 ? "tcpHost" : "tcpPort",
                "Supply both a TCP destination and port, or leave both empty."));
            return;
        }
        if (tcpHost.Length == 0) return;
        if (!int.TryParse(tcpPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
        {
            errors.Add(new("tcpPort", "Use a whole-number port from 1 through 65535."));
            return;
        }
        try
        {
            TcpDiagnostics.Validate(tcpHost, port);
            values["tcpPort"] = port.ToString(CultureInfo.InvariantCulture);
        }
        catch (ArgumentException error)
        {
            errors.Add(new(error.ParamName == "port" ? "tcpPort" : "tcpHost", error.Message));
        }
    }

    private static void ValidateUpdates(IReadOnlyDictionary<string, string> values,
        List<WorkflowOptionValidationError> errors)
    {
        bool windowsDiscovery = values.GetValueOrDefault("software") == "true" || values.GetValueOrDefault("drivers") == "true";
        bool thirdPartyDiscovery = values.GetValueOrDefault("thirdPartyApplications") == "true";
        if (!windowsDiscovery && !thirdPartyDiscovery)
            errors.Add(new("software", "Select software, drivers or third-party applications before discovery."));
        if (!windowsDiscovery && values.GetValueOrDefault("source") != nameof(UpdateSearchSource.Configured))
            errors.Add(new("source", "A Windows Update source applies only when software or driver discovery is selected."));
    }

    private static void ValidateStorage(Dictionary<string, string> values, List<WorkflowOptionValidationError> errors)
    {
        string volume = values["volume"].Trim();
        if (volume.Equals("Windows volume", StringComparison.OrdinalIgnoreCase))
            values["volume"] = "Windows volume";
        else
        {
            try { values["volume"] = StorageDiagnostics.NormalizeVolume(volume); }
            catch (ArgumentException error) { errors.Add(new("volume", error.Message)); }
        }

        bool repair = values["repairReview"] == "true";
        bool optimization = values["optimizationReview"] == "true";
        bool execute = values["execute"] == "true";
        if (repair && optimization)
            errors.Add(new("optimizationReview", "Choose filesystem repair or drive optimization, not both."));
        if (execute && !repair && !optimization)
            errors.Add(new("execute", "Select one storage action before requesting execution."));
        if (execute && repair && values["backupConfirmed"] != "true")
            errors.Add(new("backupConfirmed", "Confirm that important data is backed up before filesystem repair execution."));
    }

    private static void ValidatePrinting(Dictionary<string, string> values, List<WorkflowOptionValidationError> errors)
    {
        values["queue"] = values["queue"].Trim();
        values["jobQueue"] = values["jobQueue"].Trim();
        values["jobId"] = values["jobId"].Trim();

        bool clearOffline = values["clearOffline"] == "true";
        bool makeDefault = values["makeDefault"] == "true";
        bool cancelJob = values["cancelJob"] == "true";
        if ((clearOffline ? 1 : 0) + (makeDefault ? 1 : 0) + (cancelJob ? 1 : 0) > 1)
            errors.Add(new("cancelJob", "Choose only one printing action."));

        if (values["queue"].Length > 0)
        {
            try { values["queue"] = DefaultPrinterSelection.ValidateQueue(values["queue"]); }
            catch (ArgumentException error) { errors.Add(new("queue", error.Message)); }
        }
        if ((clearOffline || makeDefault) && values["queue"].Length == 0)
            errors.Add(new("queue", "Choose one exact printer queue before requesting a queue change."));

        bool hasJobQueue = values["jobQueue"].Length > 0;
        bool hasJobId = values["jobId"].Length > 0;
        if (hasJobQueue != hasJobId)
            errors.Add(new(hasJobQueue ? "jobId" : "jobQueue", "Supply both the blocked job queue and job ID, or leave both empty."));
        if (hasJobQueue)
        {
            try { values["jobQueue"] = PrintJobCancellation.ValidateQueue(values["jobQueue"]); }
            catch (ArgumentException error) { errors.Add(new("jobQueue", error.Message)); }
        }
        if (hasJobId)
        {
            if (!uint.TryParse(values["jobId"], NumberStyles.None, CultureInfo.InvariantCulture, out uint jobId) || jobId == 0)
                errors.Add(new("jobId", "Use a nonzero whole-number print job ID."));
            else
                values["jobId"] = jobId.ToString(CultureInfo.InvariantCulture);
        }
        if (cancelJob && (!hasJobQueue || !hasJobId))
            errors.Add(new("cancelJob", "Choose one exact blocked print job before requesting cancellation."));
    }

    private static void ValidateInstaller(Dictionary<string, string> values, List<WorkflowOptionValidationError> errors)
    {
        string productCode = values["productCode"].Trim();
        values["productCode"] = productCode;
        if (productCode.Length == 0) return;
        try { values["productCode"] = MsiProductDiagnostics.NormalizeProductCode(productCode); }
        catch (ArgumentException error) { errors.Add(new("productCode", error.Message)); }
    }

    private static void ValidateTime(Dictionary<string, string> values, List<WorkflowOptionValidationError> errors)
    {
        string peersText = values["peers"].Trim();
        values["peers"] = peersText;
        var source = Enum.Parse<TimeSourceMode>(values["source"], ignoreCase: false);
        string[] peers = peersText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (source != TimeSourceMode.CustomPeers) peers = [];
        try { _ = TimePlanning.Create(new(source, peers), new(null, null, null, false)); }
        catch (ArgumentException error) { errors.Add(new(source == TimeSourceMode.CustomPeers ? "peers" : "source", error.Message)); }
        if (values["execute"] == "true" && source == TimeSourceMode.Existing)
            errors.Add(new("execute", "Choose DomainHierarchy or CustomPeers before requesting a time-source change."));
    }

    private static void ValidateBitLocker(Dictionary<string, string> values, List<WorkflowOptionValidationError> errors)
    {
        values["volume"] = values["volume"].Trim().ToUpperInvariant();
        values["backupFile"] = values["backupFile"].Trim();
        values["protectorId"] = values["protectorId"].Trim();
        try
        {
            new BitLockerOptions(values["volume"],
                Enum.Parse<BitLockerOperation>(values["operation"], ignoreCase: false),
                Enum.Parse<BitLockerEncryption>(values["encryption"], ignoreCase: false),
                values["usedSpaceOnly"] == "true",
                Enum.Parse<RecoveryDestination>(values["backup"], ignoreCase: false),
                values["backupFile"].Length == 0 ? null : values["backupFile"]).Validate();
        }
        catch (ArgumentException error)
        {
            string key = error.Message.Contains("volume", StringComparison.OrdinalIgnoreCase) ? "volume" :
                error.Message.Contains("file", StringComparison.OrdinalIgnoreCase) || error.Message.Contains("destination", StringComparison.OrdinalIgnoreCase)
                    ? "backupFile" : "operation";
            errors.Add(new(key, error.Message));
        }

        bool backup = values["executeBackup"] == "true";
        bool encryption = values["executeEncryption"] == "true";
        bool resume = values["executeResume"] == "true";
        if ((backup ? 1 : 0) + (encryption ? 1 : 0) + (resume ? 1 : 0) > 1)
            errors.Add(new("executeBackup", "Choose only one BitLocker action."));

        var operation = Enum.Parse<BitLockerOperation>(values["operation"], ignoreCase: false);
        var destination = Enum.Parse<RecoveryDestination>(values["backup"], ignoreCase: false);
        if (backup && (operation != BitLockerOperation.BackupRecovery || destination != RecoveryDestination.File))
            errors.Add(new("executeBackup", "Choose BackupRecovery with a File destination before requesting backup."));
        if (encryption && (operation != BitLockerOperation.Encrypt || destination != RecoveryDestination.File))
            errors.Add(new("executeEncryption", "Choose Encrypt with a File destination before requesting encryption."));
        if (resume && operation != BitLockerOperation.ResumeProtection)
            errors.Add(new("executeResume", "Choose ResumeProtection before requesting protection resume."));

        bool protectorRequired = backup || encryption;
        if (values["protectorId"].Length > 0)
        {
            if (!Guid.TryParse(values["protectorId"], out Guid protector) || protector == Guid.Empty)
                errors.Add(new("protectorId", "Use one nonempty recovery protector GUID."));
            else
                values["protectorId"] = protector.ToString("D");
        }
        else if (protectorRequired)
            errors.Add(new("protectorId", "Choose one discovered recovery-password protector before execution."));
    }

    private static bool TryNormalize(WorkflowOptionDefinition option, string value,
        out string normalized, out string? error)
    {
        normalized = value;
        error = null;
        switch (option.Control)
        {
            case WorkflowOptionControl.Toggle:
                if (!bool.TryParse(value, out bool toggle))
                {
                    error = "Use true or false.";
                    return false;
                }
                normalized = toggle ? "true" : "false";
                return true;
            case WorkflowOptionControl.Slider:
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ||
                    option.Minimum is not int minimum || option.Maximum is not int maximum || option.Step is not int step ||
                    number < minimum || number > maximum || (number - minimum) % step != 0)
                {
                    error = $"Use a whole number from {option.Minimum} to {option.Maximum} in steps of {option.Step}.";
                    return false;
                }
                normalized = number.ToString(CultureInfo.InvariantCulture);
                return true;
            case WorkflowOptionControl.Choice:
                string choiceValue = value.Trim();
                WorkflowOptionChoice? selected = option.Choices?.FirstOrDefault(choice =>
                    choice.Value.Equals(choiceValue, StringComparison.OrdinalIgnoreCase));
                if (selected is null)
                {
                    error = "Choose one of: " + string.Join(", ", option.Choices?.Select(choice => choice.Value) ?? [] ) + ".";
                    return false;
                }
                normalized = selected.Value;
                return true;
            case WorkflowOptionControl.Text:
                if (value.Length > MaximumTextLength || value.IndexOf('\0') >= 0)
                {
                    error = $"Text must contain at most {MaximumTextLength} characters and no null character.";
                    return false;
                }
                return true;
            default:
                error = "The option control is unsupported.";
                return false;
        }
    }
}
