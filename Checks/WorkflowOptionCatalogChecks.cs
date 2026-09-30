using KillerBench.Engine;

internal static class WorkflowOptionCatalogChecks
{
    internal static void Run()
    {
        Require(WorkflowOptionCatalog.All.Count == 27, "Option catalog exposes the twenty-seven option-rich workflows");
        Require(WorkflowOptionCatalog.All.Select(item => item.Kind).SequenceEqual(
            WorkflowCatalog.All.Where(item => WorkflowOptionCatalog.ForWorkflow(item.Kind) is not null).Select(item => item.Kind)),
            "Option sets follow the canonical workflow order");
        Require(WorkflowOptionCatalog.All.Select(item => item.WorkflowKey).Distinct(StringComparer.Ordinal).Count() == WorkflowOptionCatalog.All.Count,
            "Workflow option keys are unique");

        foreach (WorkflowOptionSet set in WorkflowOptionCatalog.All)
        {
            Require(WorkflowCatalog.Find(set.WorkflowKey)?.Kind == set.Kind, "Option set references one installed workflow");
            Require(set.Options.Count > 0 && !string.IsNullOrWhiteSpace(set.ExecutionBoundary), "Option set is usable and declares its boundary");
            Require(set.Options.Select(option => option.Key).Distinct(StringComparer.Ordinal).Count() == set.Options.Count,
                "Option keys are unique within " + set.WorkflowKey);

            foreach (WorkflowOptionDefinition option in set.Options)
            {
                Require(!string.IsNullOrWhiteSpace(option.Key) && !string.IsNullOrWhiteSpace(option.Label) &&
                    !string.IsNullOrWhiteSpace(option.Description), "Option metadata is complete");
                switch (option.Control)
                {
                    case WorkflowOptionControl.Toggle:
                        Require(option.DefaultValue is "true" or "false" && option.Minimum is null && option.Maximum is null &&
                            option.Step is null && option.Choices is null, "Toggle contract is valid");
                        break;
                    case WorkflowOptionControl.Slider:
                        Require(option.Minimum.HasValue && option.Maximum.HasValue && option.Step is > 0 &&
                            option.Minimum <= option.Maximum && int.TryParse(option.DefaultValue, out int value) &&
                            value >= option.Minimum && value <= option.Maximum && option.Choices is null,
                            "Slider contract is bounded and has a valid default");
                        break;
                    case WorkflowOptionControl.Choice:
                        Require(option.Choices is { Count: > 0 } && option.Choices.Select(choice => choice.Value)
                            .Distinct(StringComparer.Ordinal).Count() == option.Choices.Count &&
                            option.Choices.Any(choice => choice.Value == option.DefaultValue) &&
                            option.Minimum is null && option.Maximum is null && option.Step is null,
                            "Choice contract has unique values and a valid default");
                        break;
                    case WorkflowOptionControl.Text:
                        Require(option.Minimum is null && option.Maximum is null && option.Step is null && option.Choices is null,
                            "Text contract has no numeric or choice metadata");
                        break;
                    default:
                        throw new Exception("Unknown workflow option control.");
                }

                if (option.ChangesMachine)
                    Require(option.Control == WorkflowOptionControl.Toggle && option.DefaultValue == "false" && option.Advanced,
                        "Machine-changing controls are advanced and off by default");
                if (option.EnabledWhen is { } condition)
                    Require(set.Options.Any(candidate => candidate.Key == condition.OptionKey),
                        "Conditional option references another option in the same workflow");
            }
        }

        WorkflowOptionSet cleanup = Required("cleanup");
        Require(cleanup.Options.Count(option => option.Key.StartsWith("target.", StringComparison.Ordinal)) ==
            Enum.GetValues<CleanupTargets>().Count(value => value != CleanupTargets.None), "Cleanup exposes every supported target");
        Require(Value(cleanup, "minimumAgeDays") == "7" && Value(cleanup, "maximumFilesPerTarget") == "100",
            "Cleanup metadata matches Engine defaults");
        Require(Value(cleanup, "target.userTemp") == "true", "Cleanup starts with the bounded current-user target selected");

        WorkflowOptionSet updates = Required("updates");
        Require(Value(updates, "software") == "true" && Value(updates, "drivers") == "false" &&
            Value(updates, "thirdPartyApplications") == "false" && Value(updates, "source") == nameof(UpdateSearchSource.Configured),
            "Update discovery metadata matches Engine defaults");

        WorkflowOptionSet network = Required("network");
        Require(Value(network, "lookupHost") == "" && Value(network, "tcpHost") == "" &&
            Value(network, "tcpPort") == "" && Value(network, "timeoutSeconds") == "10",
            "Network metadata matches desktop defaults");

        WorkflowOptionSet audio = Required("audio");
        Require(Value(audio, "direction") == "All" && Value(audio, "role") == "All" &&
            audio.Options.Single(option => option.Key == "direction").Choices?.Count == 3 &&
            audio.Options.Single(option => option.Key == "role").Choices?.Count == 4,
            "Audio metadata matches the complete desktop endpoint scope");

        WorkflowOptionSet restore = Required("system-restore");
        Require(Value(restore, "prepare") == "false" && Value(restore, "execute") == "false" &&
            Value(restore, "description") == "Before KillerBench maintenance" &&
            Value(restore, "reason") == nameof(SystemRestorePointReason.ModifySettings),
            "System Restore metadata defaults to read-only inventory");

        WorkflowOptionSet storage = Required("storage");
        Require(Value(storage, "volume") == "Windows volume" && Value(storage, "repairReview") == "false" &&
            Value(storage, "optimizationReview") == "false" && Value(storage, "execute") == "false",
            "Storage metadata defaults to diagnosis of the Windows volume");

        WorkflowOptionSet printing = Required("printing");
        Require(Value(printing, "queue") == "" && Value(printing, "clearOffline") == "false" &&
            Value(printing, "makeDefault") == "false" && Value(printing, "cancelJob") == "false",
            "Printing metadata defaults to general read-only diagnosis");

        WorkflowOptionSet installer = Required("installer");
        Require(Value(installer, "scope") == nameof(InstalledApplicationScope.Machine) &&
            Value(installer, "productCode") == "" && Value(installer, "execute") == "false",
            "Installer metadata defaults to general machine-scope diagnosis");

        WorkflowOptionSet time = Required("time");
        Require(Value(time, "source") == nameof(TimeSourceMode.Existing) && Value(time, "peers") == "" &&
            Value(time, "execute") == "false", "Time metadata defaults to existing-source diagnosis");

        WorkflowOptionSet eventLog = Required("eventlog");
        Require(Value(eventLog, "channel") == "System" && Value(eventLog, "execute") == "false" &&
            eventLog.Options.Single(option => option.Key == "channel").Choices?.Count == EventLogCapacityDiagnostics.Channels.Count,
            "Event Log metadata defaults to read-only supported-channel diagnosis");

        WorkflowOptionSet profiles = Required("profiles");
        Require(Value(profiles, "minimumInactiveDays") == "90" && Value(profiles, "checkDependencies") == "true",
            "Profile metadata matches the desktop read-only review defaults");

        WorkflowOptionSet computerName = Required("computer-name");
        Require(Value(computerName, "proposedName") == "" && Value(computerName, "execute") == "false" &&
            computerName.Options.Single(option => option.Key == "execute") is { Advanced: true, ChangesMachine: true },
            "Computer Name metadata defaults to review with explicit advanced execution");

        WorkflowOptionSet transfers = Required("background-transfers");
        Require(Value(transfers, "scope") == "RunningAccount" &&
            transfers.Options.Single().Choices?.Select(choice => choice.Value).SequenceEqual(new[] { "RunningAccount", "AllUsers" }) == true,
            "BITS metadata defaults to running-account inventory and exposes the administrator scope");

        WorkflowOptionSet packages = Required("app-packages");
        Require(Value(packages, "scope") == nameof(AppPackageScope.CurrentAccount) &&
            Value(packages, "includeProvisioned") == "false",
            "App package metadata defaults to running-account inventory without provisioned packages");

        WorkflowOptionSet installedApps = Required("installed-apps");
        Require(Value(installedApps, "scope") == nameof(InstalledApplicationScope.Machine),
            "Installed application metadata defaults to machine-wide registrations");

        WorkflowOptionSet search = Required("search");
        Require(Value(search, "rebuildIndex") == "false" &&
            search.Options.Single() is { Advanced: true, ChangesMachine: true },
            "Search metadata defaults to diagnosis and exposes explicit guarded index rebuild");

        WorkflowOptionSet componentCleanup = Required("component-cleanup");
        Require(Value(componentCleanup, "execute") == "false" &&
            componentCleanup.Options.Single() is { Advanced: true, ChangesMachine: true },
            "Component cleanup metadata defaults to analysis and exposes explicit guarded execution");

        WorkflowOptionSet recovery = Required("windows-recovery");
        Require(Value(recovery, "execute") == "false" &&
            recovery.Options.Single() is { Advanced: true, ChangesMachine: true },
            "Windows Recovery metadata defaults to inspection and exposes explicit guarded enablement");

        WorkflowOptionSet firewall = Required("firewall-services");
        Require(new[] { "profile.domain", "profile.private", "profile.public", "execute" }
                .All(key => Value(firewall, key) == "false") &&
            firewall.Options.Single(option => option.Key == "execute") is { Advanced: true, ChangesMachine: true },
            "Firewall metadata defaults to diagnosis with no selected profile or execution");

        WorkflowOptionSet privacy = Required("privacy");
        Require(privacy.Options.Count == PrivacyCatalog.All.Count && privacy.Options.All(option => option.DefaultValue == "false"),
            "Privacy exposes every review area without preselecting a change");

        WorkflowOptionSet bitLocker = Required("bitlocker");
        Require(Value(bitLocker, "volume") == "C:" && Value(bitLocker, "operation") == nameof(BitLockerOperation.Inspect) &&
            Value(bitLocker, "encryption") == nameof(BitLockerEncryption.XtsAes128) &&
            Value(bitLocker, "backup") == nameof(RecoveryDestination.None) && Value(bitLocker, "protectorId") == "" &&
            new[] { "executeBackup", "executeEncryption", "executeResume" }.All(key => Value(bitLocker, key) == "false"),
            "BitLocker metadata matches inspection defaults and keeps every action explicit");

        WorkflowOptionSet defender = Required("defender");
        Require(Value(defender, "scanType") == "None" && Value(defender, "startScan") == "false" &&
            defender.Options.Single(option => option.Key == "startScan") is { Advanced: true, ChangesMachine: true },
            "Defender metadata defaults to diagnosis and keeps scan execution explicit");

        WorkflowOptionSet servicing = Required("servicing-monitor");
        Require(Value(servicing, "refreshSeconds") == "3" && Value(servicing, "samples") == "3" &&
            servicing.Options.Count(option => option.Key.StartsWith("include", StringComparison.Ordinal)) == 11,
            "Servicing monitor metadata matches the desktop monitor defaults and all evidence groups");

        Require(Value(Required("orca"), "action.clearCaches") == "true",
            "Review defaults match the existing desktop workflow defaults");

        foreach (WorkflowOptionSet set in WorkflowOptionCatalog.All)
        {
            if (!KillerBench.DesktopWorkflowOptionBindings.All.TryGetValue(set.Kind, out var bindings))
                throw new Exception("Desktop does not declare bindings for " + set.WorkflowKey);
            Require(bindings.Select(binding => binding.Key).Order(StringComparer.Ordinal).SequenceEqual(
                set.Options.Select(option => option.Key).Order(StringComparer.Ordinal)),
                "Desktop bindings cover every Engine option for " + set.WorkflowKey);
            Require(bindings.All(binding => !string.IsNullOrWhiteSpace(binding.ControlName)),
                "Desktop bindings name a control for every option in " + set.WorkflowKey);
        }

        string[] changing = WorkflowOptionCatalog.All.SelectMany(set => set.Options)
            .Where(option => option.ChangesMachine).Select(option => option.Key).Order(StringComparer.Ordinal).ToArray();
        Require(changing.SequenceEqual(new[] { "applyHighPerformance", "applyPowerTimers", "cancelJob", "clearOffline", "disableHibernation", "execute", "execute", "execute", "execute", "execute", "execute", "execute", "execute", "execute", "execute", "executeBackup", "executeEncryption", "executeResume", "makeDefault", "rebuildIndex", "startScan" }.Order(StringComparer.Ordinal)),
            "Only implemented explicit execution controls are marked as machine-changing");

        Require(WorkflowOptionCatalog.Find(" NETWORK ")?.Kind == WorkflowKind.Network &&
            WorkflowCatalog.Find(" WINDOWS-UPDATE-SERVICE ")?.Kind == WorkflowKind.WindowsUpdateService &&
            WorkflowOptionCatalog.Find("missing") is null && WorkflowOptionCatalog.ForWorkflow(WorkflowKind.ScheduledTasks) is null,
            "Workflows without host-option metadata remain explicit");

        WorkflowOptionValidationResult normalized = WorkflowOptionValidation.Validate("servicing-monitor", new[]
        {
            KeyValuePair.Create("refreshSeconds", "05"),
            KeyValuePair.Create("includeEvents", "False")
        });
        Require(normalized is { IsValid: true, ChangesMachineRequested: false } &&
            normalized.Values["refreshSeconds"] == "5" && normalized.Values["includeEvents"] == "false" &&
            normalized.Values["samples"] == "3", "Validation normalizes supplied values and fills defaults");
        Require(!WorkflowOptionValidation.Validate("updates", new[]
        {
            KeyValuePair.Create("software", "false"), KeyValuePair.Create("drivers", "false"),
            KeyValuePair.Create("thirdPartyApplications", "false")
        }).IsValid && WorkflowOptionValidation.Validate("updates", new[]
        {
            KeyValuePair.Create("software", "false"), KeyValuePair.Create("thirdPartyApplications", "true")
        }).IsValid && !WorkflowOptionValidation.Validate("updates", new[]
        {
            KeyValuePair.Create("software", "false"), KeyValuePair.Create("thirdPartyApplications", "true"),
            KeyValuePair.Create("source", nameof(UpdateSearchSource.MicrosoftUpdate))
        }).IsValid, "Update discovery requires a selected source and applies Windows source choice only to Windows discovery");
        WorkflowOptionValidationResult caseInsensitiveOption = WorkflowOptionValidation.Validate("SERVICING-MONITOR", new[]
        {
            KeyValuePair.Create(" REFRESHSECONDS ", "05"),
            KeyValuePair.Create("INCLUDEEVENTS", "False")
        });
        Require(caseInsensitiveOption is { IsValid: true } && caseInsensitiveOption.Values["refreshSeconds"] == "5" &&
            !WorkflowOptionValidation.Validate("servicing-monitor", new[]
            {
                KeyValuePair.Create("refreshSeconds", "5"), KeyValuePair.Create("REFRESHSECONDS", "6")
            }).IsValid, "Workflow option keys trim input, accept case-insensitive values and reject duplicates across casing");
        WorkflowOptionValidationResult caseInsensitiveChoice = WorkflowOptionValidation.Validate("audio", new[]
        {
            KeyValuePair.Create("direction", " output "), KeyValuePair.Create("role", "communications")
        });
        Require(caseInsensitiveChoice is { IsValid: true } && caseInsensitiveChoice.Values["direction"] == "Output" &&
            caseInsensitiveChoice.Values["role"] == "Communications",
            "Workflow choices trim input, accept case-insensitive values and retain canonical values");

        WorkflowOptionValidationResult invalid = WorkflowOptionValidation.Validate("cleanup", new[]
        {
            KeyValuePair.Create("minimumAgeDays", "0"),
            KeyValuePair.Create("unknown", "true"),
            KeyValuePair.Create("execute", "yes"),
            KeyValuePair.Create("execute", "true")
        });
        Require(!invalid.IsValid && invalid.Errors.Any(error => error.Key == "minimumAgeDays") &&
            invalid.Errors.Any(error => error.Key == "unknown") && invalid.Errors.Any(error => error.Key == "execute"),
            "Validation rejects bounds, unknown keys, invalid toggles and duplicates");

        WorkflowOptionValidationResult conditional = WorkflowOptionValidation.Validate("bitlocker", new[]
        {
            KeyValuePair.Create("backupFile", "C:\\fixture.txt")
        });
        Require(!conditional.IsValid && conditional.Errors.Any(error => error.Key == "backupFile"),
            "Validation rejects a disabled conditional value");

        WorkflowOptionValidationResult bitLockerBackup = WorkflowOptionValidation.Validate("bitlocker", new[]
        {
            KeyValuePair.Create("volume", "c:"),
            KeyValuePair.Create("operation", nameof(BitLockerOperation.BackupRecovery)),
            KeyValuePair.Create("backup", nameof(RecoveryDestination.File)),
            KeyValuePair.Create("backupFile", "D:\\recovery.txt"),
            KeyValuePair.Create("protectorId", "{00000000-0000-0000-0000-000000000001}"),
            KeyValuePair.Create("executeBackup", "true")
        });
        Require(bitLockerBackup is { IsValid: true, ChangesMachineRequested: true } &&
            bitLockerBackup.Values["volume"] == "C:" &&
            bitLockerBackup.Values["protectorId"] == "00000000-0000-0000-0000-000000000001" &&
            !WorkflowOptionValidation.Validate("bitlocker", new[]
            {
                KeyValuePair.Create("operation", nameof(BitLockerOperation.Encrypt)),
                KeyValuePair.Create("backup", nameof(RecoveryDestination.File)),
                KeyValuePair.Create("backupFile", "D:\\recovery.txt"),
                KeyValuePair.Create("executeEncryption", "true")
            }).IsValid,
            "BitLocker validation requires one exact protector and matching explicit action");

        WorkflowOptionValidationResult defenderExecution = WorkflowOptionValidation.Validate("defender", new[]
        {
            KeyValuePair.Create("scanType", nameof(DefenderScanType.Full)),
            KeyValuePair.Create("startScan", "true")
        });
        Require(defenderExecution is { IsValid: true, ChangesMachineRequested: true } &&
            !WorkflowOptionValidation.Validate("defender", new[] { KeyValuePair.Create("startScan", "true") }).IsValid,
            "Validation identifies explicit Defender execution and requires a scan type");

        WorkflowOptionValidationResult required = WorkflowOptionValidation.Validate("computer-name", Array.Empty<KeyValuePair<string, string>>());
        Require(!required.IsValid && required.Errors.Select(error => error.Key).ToHashSet(StringComparer.Ordinal)
            .Contains("proposedName"),
            "Validation reports active required text fields");

        WorkflowOptionValidationResult networkTarget = WorkflowOptionValidation.Validate("network", new[]
        {
            KeyValuePair.Create("lookupHost", " example.com "),
            KeyValuePair.Create("tcpHost", "127.0.0.1"),
            KeyValuePair.Create("tcpPort", "00443"),
            KeyValuePair.Create("timeoutSeconds", "15")
        });
        Require(networkTarget.IsValid && networkTarget.Values["lookupHost"] == "example.com" &&
            networkTarget.Values["tcpPort"] == "443" && networkTarget.Values["timeoutSeconds"] == "15",
            "Network target validation normalizes one exact diagnostic target");
        Require(!WorkflowOptionValidation.Validate("network", new[]
        {
            KeyValuePair.Create("tcpHost", "example.com")
        }).IsValid && !WorkflowOptionValidation.Validate("network", new[]
        {
            KeyValuePair.Create("tcpHost", "example.com"), KeyValuePair.Create("tcpPort", "70000")
        }).IsValid, "Network validation requires a complete bounded TCP target");

        WorkflowOptionValidationResult audioScope = WorkflowOptionValidation.Validate("audio", new[]
        {
            KeyValuePair.Create("direction", nameof(AudioEndpointDirection.Output)),
            KeyValuePair.Create("role", nameof(AudioEndpointRole.Communications))
        });
        Require(audioScope.IsValid && audioScope.Values["direction"] == "Output" &&
            audioScope.Values["role"] == "Communications" &&
            !WorkflowOptionValidation.Validate("audio", new[] { KeyValuePair.Create("role", "Games") }).IsValid,
            "Audio scope validation accepts only supported endpoint directions and roles");

        WorkflowOptionValidationResult restoreExecution = WorkflowOptionValidation.Validate("system-restore", new[]
        {
            KeyValuePair.Create("prepare", "true"),
            KeyValuePair.Create("description", "Before maintenance"),
            KeyValuePair.Create("reason", nameof(SystemRestorePointReason.ApplicationInstall)),
            KeyValuePair.Create("execute", "true")
        });
        Require(restoreExecution is { IsValid: true, ChangesMachineRequested: true } &&
            !WorkflowOptionValidation.Validate("system-restore", new[] { KeyValuePair.Create("execute", "true") }).IsValid,
            "System Restore validation keeps execution behind an explicit prepared request");

        WorkflowOptionValidationResult storageRepair = WorkflowOptionValidation.Validate("storage", new[]
        {
            KeyValuePair.Create("volume", "d:\\"),
            KeyValuePair.Create("repairReview", "true"),
            KeyValuePair.Create("repairMode", nameof(FileSystemRepairMode.OfflineScanAndFix)),
            KeyValuePair.Create("allowDismount", "true"),
            KeyValuePair.Create("backupConfirmed", "true"),
            KeyValuePair.Create("execute", "true")
        });
        Require(storageRepair is { IsValid: true, ChangesMachineRequested: true } && storageRepair.Values["volume"] == "D:" &&
            !WorkflowOptionValidation.Validate("storage", new[]
            {
                KeyValuePair.Create("repairReview", "true"), KeyValuePair.Create("execute", "true")
            }).IsValid && !WorkflowOptionValidation.Validate("storage", new[]
            {
                KeyValuePair.Create("repairReview", "true"), KeyValuePair.Create("optimizationReview", "true")
            }).IsValid, "Storage validation enforces one exact action and backup confirmation before execution");

        WorkflowOptionValidationResult printingAction = WorkflowOptionValidation.Validate("printing", new[]
        {
            KeyValuePair.Create("jobQueue", "Office Printer"),
            KeyValuePair.Create("jobId", "00042"),
            KeyValuePair.Create("cancelJob", "true")
        });
        Require(printingAction is { IsValid: true, ChangesMachineRequested: true } && printingAction.Values["jobId"] == "42" &&
            !WorkflowOptionValidation.Validate("printing", new[] { KeyValuePair.Create("makeDefault", "true") }).IsValid &&
            !WorkflowOptionValidation.Validate("printing", new[]
            {
                KeyValuePair.Create("queue", "Office Printer"), KeyValuePair.Create("clearOffline", "true"),
                KeyValuePair.Create("makeDefault", "true")
            }).IsValid, "Printing validation requires one exact target and one explicit action");

        WorkflowOptionValidationResult installerAction = WorkflowOptionValidation.Validate("installer", new[]
        {
            KeyValuePair.Create("scope", nameof(InstalledApplicationScope.CurrentAccount)),
            KeyValuePair.Create("productCode", "{00000000-0000-0000-0000-000000000001}"),
            KeyValuePair.Create("execute", "true")
        });
        Require(installerAction is { IsValid: true, ChangesMachineRequested: true } &&
            installerAction.Values["productCode"] == "{00000000-0000-0000-0000-000000000001}" &&
            !WorkflowOptionValidation.Validate("installer", new[]
            {
                KeyValuePair.Create("productCode", "not-a-product-code")
            }).IsValid, "Installer validation accepts only an exact optional MSI product code");

        WorkflowOptionValidationResult timeAction = WorkflowOptionValidation.Validate("time", new[]
        {
            KeyValuePair.Create("source", nameof(TimeSourceMode.CustomPeers)),
            KeyValuePair.Create("peers", "time.example.test 192.0.2.1"),
            KeyValuePair.Create("execute", "true")
        });
        Require(timeAction is { IsValid: true, ChangesMachineRequested: true } &&
            !WorkflowOptionValidation.Validate("time", new[] { KeyValuePair.Create("execute", "true") }).IsValid &&
            !WorkflowOptionValidation.Validate("time", new[]
            {
                KeyValuePair.Create("source", nameof(TimeSourceMode.CustomPeers)),
                KeyValuePair.Create("peers", "time.example.test time.example.test")
            }).IsValid, "Time validation requires a non-default source and valid unique custom peers");

        WorkflowOptionValidationResult eventChannelAction = WorkflowOptionValidation.Validate("eventlog", new[]
        {
            KeyValuePair.Create("channel", "Microsoft-Windows-WindowsUpdateClient/Operational"),
            KeyValuePair.Create("execute", "true")
        });
        Require(eventChannelAction is { IsValid: true, ChangesMachineRequested: true } &&
            !WorkflowOptionValidation.Validate("eventlog", new[]
            {
                KeyValuePair.Create("channel", "Security")
            }).IsValid, "Event Log validation accepts only one supported diagnostic channel");

        WorkflowOptionValidationResult computerRename = WorkflowOptionValidation.Validate("computer-name", new[]
        {
            KeyValuePair.Create("proposedName", "NEW-PC"),
            KeyValuePair.Create("execute", "true")
        });
        Require(computerRename is { IsValid: true, ChangesMachineRequested: true } &&
            computerRename.Values["proposedName"] == "NEW-PC" &&
            !WorkflowOptionValidation.Validate("computer-name", new[]
            {
                KeyValuePair.Create("proposedName", "12345")
            }).IsValid && !WorkflowOptionValidation.Validate("computer-name", Array.Empty<KeyValuePair<string, string>>()).IsValid,
            "Computer Name validation requires one valid exact name and identifies explicit rename execution");

        WorkflowOptionValidationResult transferScope = WorkflowOptionValidation.Validate("background-transfers", new[]
        {
            KeyValuePair.Create("scope", "AllUsers")
        });
        Require(transferScope is { IsValid: true, ChangesMachineRequested: false } &&
            !WorkflowOptionValidation.Validate("background-transfers", new[]
            {
                KeyValuePair.Create("scope", "Everyone")
            }).IsValid, "BITS option validation accepts only the two implemented inventory scopes");

        WorkflowOptionValidationResult packageScope = WorkflowOptionValidation.Validate("app-packages", new[]
        {
            KeyValuePair.Create("scope", nameof(AppPackageScope.AllUsers)),
            KeyValuePair.Create("includeProvisioned", "true")
        });
        Require(packageScope is { IsValid: true, ChangesMachineRequested: false } &&
            !WorkflowOptionValidation.Validate("app-packages", new[]
            {
                KeyValuePair.Create("scope", "Machine")
            }).IsValid, "App package validation accepts only implemented inventory scopes");

        WorkflowOptionValidationResult applicationScope = WorkflowOptionValidation.Validate("installed-apps", new[]
        {
            KeyValuePair.Create("scope", nameof(InstalledApplicationScope.CurrentAccount))
        });
        Require(applicationScope is { IsValid: true, ChangesMachineRequested: false } &&
            !WorkflowOptionValidation.Validate("installed-apps", new[]
            {
                KeyValuePair.Create("scope", "AllUsers")
            }).IsValid, "Installed application validation accepts only implemented registration scopes");

        Require(WorkflowOptionValidation.Validate("search", new[]
        {
            KeyValuePair.Create("rebuildIndex", "true")
        }) is { IsValid: true, ChangesMachineRequested: true },
            "Search validation identifies an explicit index rebuild request");

        Require(WorkflowOptionValidation.Validate("component-cleanup", new[]
        {
            KeyValuePair.Create("execute", "true")
        }) is { IsValid: true, ChangesMachineRequested: true },
            "Component cleanup validation identifies explicit maintenance execution");

        Require(WorkflowOptionValidation.Validate("windows-recovery", new[]
        {
            KeyValuePair.Create("execute", "true")
        }) is { IsValid: true, ChangesMachineRequested: true },
            "Windows Recovery validation identifies explicit enablement");

        WorkflowOptionValidationResult firewallEnable = WorkflowOptionValidation.Validate("firewall-services", new[]
        {
            KeyValuePair.Create("profile.private", "true"),
            KeyValuePair.Create("profile.public", "true"),
            KeyValuePair.Create("execute", "true")
        });
        Require(firewallEnable is { IsValid: true, ChangesMachineRequested: true } &&
            !WorkflowOptionValidation.Validate("firewall-services", new[]
            {
                KeyValuePair.Create("execute", "true")
            }).IsValid, "Firewall validation requires at least one exact profile before enablement");

        WindowsWorkflowHost projectedNetwork = WorkflowOptionProjection.CreateHost(networkTarget);
        Require(projectedNetwork.NetworkLookupHost == "example.com" &&
            projectedNetwork.NetworkEndpoint == ("127.0.0.1", 443) &&
            projectedNetwork.NetworkTimeout == TimeSpan.FromSeconds(15) &&
            projectedNetwork.OptionPlanCheck?.Detail.Contains("Timeout: 15", StringComparison.Ordinal) == true,
            "Validated network options project into the shared workflow host");

        WindowsWorkflowHost projectedFirewall = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("firewall-services", new[]
            {
                KeyValuePair.Create("profile.private", "true"),
                KeyValuePair.Create("profile.public", "true")
            }));
        Require(projectedFirewall.FirewallProfileSelection?.Profiles.SequenceEqual(
                new[] { FirewallProfileTarget.Private, FirewallProfileTarget.Public }) == true,
            "Selected firewall profiles project into the shared read-only review host");

        WindowsWorkflowHost projectedRestore = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("system-restore", new[]
            {
                KeyValuePair.Create("prepare", "true"),
                KeyValuePair.Create("description", "Before driver maintenance"),
                KeyValuePair.Create("reason", nameof(SystemRestorePointReason.DeviceDriverInstall))
            }));
        Require(projectedRestore.SystemRestoreRequest ==
                new SystemRestoreCreationRequest("Before driver maintenance", SystemRestorePointReason.DeviceDriverInstall) &&
            projectedRestore.OptionPlanCheck?.Detail.Contains("Before driver maintenance", StringComparison.Ordinal) == true,
            "System Restore preparation projects the selected description and reason into its readiness review");

        WindowsWorkflowHost projectedStorage = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("storage", new[]
            {
                KeyValuePair.Create("volume", "D:"),
                KeyValuePair.Create("optimizationReview", "true"),
                KeyValuePair.Create("optimizationMode", nameof(VolumeOptimizationMode.Defragment))
            }));
        Require(projectedStorage.StorageVolume == "D:" && projectedStorage.VolumeOptimizationRequest ==
                new VolumeOptimizationRequest("D:", VolumeOptimizationMode.Defragment) &&
            projectedStorage.OptionPlanCheck?.Detail.Contains("Defragment", StringComparison.Ordinal) == true,
            "Storage optimization selections project into the exact-volume read-only review");

        WindowsWorkflowHost projectedFileSystem = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("storage", new[]
            {
                KeyValuePair.Create("volume", "D:"),
                KeyValuePair.Create("repairReview", "true"),
                KeyValuePair.Create("repairMode", nameof(FileSystemRepairMode.OfflineScanAndFix)),
                KeyValuePair.Create("allowDismount", "true"),
                KeyValuePair.Create("backupConfirmed", "true")
            }));
        Require(projectedFileSystem.FileSystemRepairSelection ==
                new FileSystemRepairSelection("D:", FileSystemRepairMode.OfflineScanAndFix, true, true) &&
            projectedFileSystem.OptionPlanCheck?.Detail.Contains("OfflineScanAndFix", StringComparison.Ordinal) == true,
            "Filesystem repair selections project into a bounded exact-volume evidence review");

        WindowsWorkflowHost projectedPrinting = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("printing", new[]
            {
                KeyValuePair.Create("queue", "Office Printer"),
                KeyValuePair.Create("jobQueue", "Office Printer"),
                KeyValuePair.Create("jobId", "42")
            }));
        Require(projectedPrinting.PrintingTargetSelection ==
                new PrintingTargetSelection("Office Printer", "Office Printer", 42) &&
            projectedPrinting.OptionPlanCheck?.Detail.Contains("Blocked job ID: 42", StringComparison.Ordinal) == true,
            "Printer queue and job selections project into exact read-only target reviews");

        WindowsWorkflowHost projectedInventory = WorkflowOptionProjection.CreateHost(packageScope);
        Require(projectedInventory.PackageScope == AppPackageScope.AllUsers && projectedInventory.IncludeProvisionedPackages,
            "Validated package inventory scope projects into the shared workflow host");

        WindowsWorkflowHost projectedCleanup = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("cleanup", new[]
            {
                KeyValuePair.Create("target.windowsTemp", "true"),
                KeyValuePair.Create("minimumAgeDays", "30"),
                KeyValuePair.Create("maximumFilesPerTarget", "250")
            }));
        Require(projectedCleanup.CleanupOptions.Targets.HasFlag(CleanupTargets.UserTemp) &&
            projectedCleanup.CleanupOptions.Targets.HasFlag(CleanupTargets.WindowsTemp) &&
            projectedCleanup.CleanupOptions.MinimumAgeDays == 30 && projectedCleanup.CleanupOptions.MaximumFilesPerTarget == 250,
            "Validated cleanup selections project without requesting removal");

        WindowsWorkflowHost projectedMace = WorkflowOptionProjection.CreateHost(
            WorkflowOptionValidation.Validate("mace", new[]
            {
                KeyValuePair.Create("target.oneDrive", "true"),
                KeyValuePair.Create("scope", nameof(MaceScope.AllUsers)),
                KeyValuePair.Create("restoreOneDriveFolderLocations", "true")
            }));
        Require(projectedMace.MaceOptions.Targets == MaceTargets.OneDrive &&
            projectedMace.MaceOptions.Scope == MaceScope.AllUsers && projectedMace.MaceOptions.RestoreOneDriveFolderLocations,
            "Validated Microsoft app removal selections project into a read-only removal review");

        ServicingMonitorOptions projectedServicing = WorkflowOptionProjection.CreateServicingOptions(normalized);
        Require(projectedServicing.RefreshSeconds == 5 && projectedServicing.Samples == 3 &&
            !projectedServicing.IncludeEvents && projectedServicing.IncludeHistory,
            "Validated servicing monitor controls project into monitor options");

        bool changingRejected = false;
        try { WorkflowOptionProjection.CreateHost(defenderExecution); }
        catch (ArgumentException) { changingRejected = true; }
        Require(changingRejected, "Shared workflow projection rejects every machine-changing plan");

        Console.WriteLine("PASS: discoverable workflow controls, defaults, dependencies, normalized validation, read-only host projection and execution boundaries.");
    }

    private static WorkflowOptionSet Required(string key) => WorkflowOptionCatalog.Find(key) ??
        throw new Exception("Missing workflow option set: " + key);

    private static string Value(WorkflowOptionSet set, string key) => set.Options.Single(option => option.Key == key).DefaultValue;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
