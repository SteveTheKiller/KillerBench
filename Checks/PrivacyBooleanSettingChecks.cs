using System.IO;
using KillerBench.Engine;

internal static class PrivacyBooleanSettingChecks
{
    public static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "privacy-boolean-settings");
        Require(PrivacyBooleanSettingCatalog.All.Count == 38 &&
            PrivacyBooleanSettingCatalog.Find("ADVERTISING-ID") == PrivacyBooleanSettingCatalog.AdvertisingId &&
            PrivacyBooleanSettingCatalog.TailoredExperiences.PolicyScope == PrivacyRegistryScope.CurrentUser &&
            PrivacyBooleanSettingCatalog.Find("ONLINE-SPEECH") == PrivacyBooleanSettingCatalog.OnlineSpeech &&
            PrivacyBooleanSettingCatalog.OnlineSpeech.PolicyScope == PrivacyRegistryScope.LocalMachine &&
            PrivacyBooleanSettingCatalog.Find("CLIPBOARD-SYNC") == PrivacyBooleanSettingCatalog.ClipboardSync &&
            PrivacyBooleanSettingCatalog.Find("APP-LAUNCH-TRACKING") == PrivacyBooleanSettingCatalog.AppLaunchTracking &&
            PrivacyBooleanSettingCatalog.AppLaunchTracking.PolicyScope == PrivacyRegistryScope.CurrentUser &&
            PrivacyBooleanSettingCatalog.Find("WINDOWS-TIPS") == PrivacyBooleanSettingCatalog.WindowsTips &&
            PrivacyBooleanSettingCatalog.WindowsTips.PolicyScope == PrivacyRegistryScope.LocalMachine &&
            PrivacyBooleanSettingCatalog.Find("SUGGESTED-NOTIFICATIONS") == PrivacyBooleanSettingCatalog.SuggestedNotifications &&
            PrivacyBooleanSettingCatalog.SuggestedNotifications.PolicyValueName == "DisableWindowsConsumerFeatures" &&
            PrivacyBooleanSettingCatalog.Find("CONSUMER-CONTENT") == PrivacyBooleanSettingCatalog.ConsumerContent &&
            PrivacyBooleanSettingCatalog.ConsumerContent.UserValueName == "ContentDeliveryAllowed" &&
            PrivacyBooleanSettingCatalog.ConsumerContent.PolicyValueName == "DisableWindowsConsumerFeatures" &&
            PrivacyBooleanSettingCatalog.Find("LOCK-SCREEN-SPOTLIGHT") == PrivacyBooleanSettingCatalog.LockScreenSpotlight &&
            PrivacyBooleanSettingCatalog.LockScreenSpotlight.UserValueName == "RotatingLockScreenEnabled" &&
            PrivacyBooleanSettingCatalog.LockScreenSpotlight.PolicyValueName == "DisableWindowsSpotlightFeatures" &&
            PrivacyBooleanSettingCatalog.Find("LOCK-SCREEN-SPOTLIGHT-OVERLAY") == PrivacyBooleanSettingCatalog.LockScreenSpotlightOverlay &&
            PrivacyBooleanSettingCatalog.LockScreenSpotlightOverlay.UserValueName == "RotatingLockScreenOverlayEnabled" &&
            PrivacyBooleanSettingCatalog.Find("START-SUGGESTIONS") == PrivacyBooleanSettingCatalog.StartSuggestions &&
            PrivacyBooleanSettingCatalog.StartSuggestions.UserValueName == "SystemPaneSuggestionsEnabled" &&
            PrivacyBooleanSettingCatalog.Find("PREINSTALLED-APP-SUGGESTIONS") == PrivacyBooleanSettingCatalog.PreinstalledAppSuggestions &&
            PrivacyBooleanSettingCatalog.PreinstalledAppSuggestions.UserValueName == "PreInstalledAppsEnabled" &&
            PrivacyBooleanSettingCatalog.Find("SILENT-APP-SUGGESTIONS") == PrivacyBooleanSettingCatalog.SilentAppSuggestions &&
            PrivacyBooleanSettingCatalog.SilentAppSuggestions.UserValueName == "SilentInstalledAppsEnabled" &&
            PrivacyBooleanSettingCatalog.Find("TYPING-INKING-PERSONALIZATION") == PrivacyBooleanSettingCatalog.TypingAndInkingPersonalization &&
            PrivacyBooleanSettingCatalog.TypingAndInkingPersonalization.UserValueName == "Enabled" &&
            PrivacyBooleanSettingCatalog.TypingAndInkingPersonalization.PolicyValueName == "AllowInputPersonalization" &&
            PrivacyBooleanSettingCatalog.Find("WEB-SEARCH-RESULTS") == PrivacyBooleanSettingCatalog.WebSearchResults &&
            PrivacyBooleanSettingCatalog.WebSearchResults.UserValueName == "BingSearchEnabled" &&
            PrivacyBooleanSettingCatalog.WebSearchResults.PolicyValueName == "DisableWebSearch" &&
            PrivacyBooleanSettingCatalog.Find("OEM-APP-SUGGESTIONS") == PrivacyBooleanSettingCatalog.OemAppSuggestions &&
            PrivacyBooleanSettingCatalog.OemAppSuggestions.UserValueName == "OemPreInstalledAppsEnabled" &&
            PrivacyBooleanSettingCatalog.Find("CAMERA-ACCESS") == PrivacyBooleanSettingCatalog.CameraAccess &&
            PrivacyBooleanSettingCatalog.CameraAccess.ValueKind == PrivacyBooleanValueKind.ConsentString &&
            PrivacyBooleanSettingCatalog.CameraAccess.PolicyValueName == "LetAppsAccessCamera" &&
            PrivacyBooleanSettingCatalog.Find("MICROPHONE-ACCESS") == PrivacyBooleanSettingCatalog.MicrophoneAccess &&
            PrivacyBooleanSettingCatalog.MicrophoneAccess.ValueKind == PrivacyBooleanValueKind.ConsentString &&
            PrivacyBooleanSettingCatalog.MicrophoneAccess.PolicyValueName == "LetAppsAccessMicrophone",
            "Privacy setting definitions retain distinct current-user and policy identities");
        var all = PrivacyBooleanSettingCatalog.All;
        Require(all.Select(s => s.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() == all.Count &&
            all.Select(s => s.UserPath + "|" + s.UserValueName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == all.Count &&
            all.Where(s => s.ValueKind == PrivacyBooleanValueKind.ConsentString).All(s =>
                s.UserPath.StartsWith(@"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\", StringComparison.Ordinal) &&
                s.UserValueName == "Value" && s.MachinePath == s.UserPath && s.MachineValueName == "Value" &&
                s.PolicyScope == PrivacyRegistryScope.LocalMachine && s.PolicyPath == @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" && s.PolicyValueName.StartsWith("LetApps", StringComparison.Ordinal)) &&
            all.Count(s => s.ValueKind == PrivacyBooleanValueKind.ConsentString) == 19 &&
            PrivacyBooleanSettingCatalog.Find("location-access")!.PolicyValueName == "LetAppsAccessLocation" &&
            PrivacyBooleanSettingCatalog.Find("search-highlights")!.PolicyValueName == "EnableDynamicContentInWSB" &&
            PrivacyBooleanSettingCatalog.Find("cloud-search-work")!.UserValueName == "IsAADCloudSearchEnabled",
            "Expanded privacy catalog keeps unique identities, one consent-store pattern and documented policy names");
        if (OperatingSystem.IsWindows())
            foreach (var definition in all)
            {
                var live = await new WindowsPrivacyBooleanSettingHost(definition).ReadAsync(CancellationToken.None);
                Require(live.Complete || live.Detail.StartsWith("Privacy-setting evidence is unavailable", StringComparison.Ordinal),
                    definition.Title + " live read-only evidence is complete or explicitly unavailable: " + live.Detail);
            }

        var previewHost = new Host(Evidence(false, null));
        var preview = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            false, false, Path.Combine(root, "preview"), previewHost);
        Require(preview.Outcome == PrivacyChangeOutcome.Preview && previewHost.Reads == 0 && previewHost.Writes.Count == 0,
            "Privacy setting defaults to preview without provider calls");

        string enableRoot = Path.Combine(root, "enable");
        var enableHost = new Host(Evidence(false, null), Evidence(true, 1));
        var enabled = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            true, true, enableRoot, enableHost);
        Require(enabled.Outcome == PrivacyChangeOutcome.Verified && enableHost.Writes.SequenceEqual<bool?>([true]) &&
            enabled.JournalPath is not null && File.Exists(enabled.JournalPath), "Advertising ID enablement verifies exact fresh state");

        var undoHost = new Host(Evidence(true, 1), Evidence(false, null));
        var undone = await PrivacyBooleanSettingExecution.UndoAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            enabled.JournalPath!, Path.Combine(root, "undo"), undoHost);
        Require(undone.Outcome == PrivacyChangeOutcome.Verified && undoHost.Writes.SequenceEqual<bool?>([null]),
            "Guarded undo restores exact value absence");

        string interruptedUndoRoot = Path.Combine(root, "interrupted-undo");
        var interruptedUndoHost = new Host(Evidence(true, 1)) { FailWrite = true };
        var interruptedUndo = await PrivacyBooleanSettingExecution.UndoAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            enabled.JournalPath!, interruptedUndoRoot, interruptedUndoHost);
        Require(interruptedUndo.Outcome == PrivacyChangeOutcome.Uncertain &&
            File.Exists(Path.Combine(interruptedUndoRoot, "advertising-id.pending.json")),
            "Interrupted undo retains its exact restoration target");
        var undoRecoveryHost = new Host(Evidence(false, null));
        var recoveredUndo = await PrivacyBooleanSettingExecution.ReconcileAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            interruptedUndoRoot, undoRecoveryHost);
        Require(recoveredUndo.Outcome == PrivacyChangeReconciliationOutcome.Verified && undoRecoveryHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(interruptedUndoRoot, "advertising-id.pending.json")),
            "Interrupted undo reconciliation verifies restored absence without repeating a write");

        var disabledHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var disabled = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.TailoredExperiences,
            false, true, Path.Combine(root, "disable"), disabledHost);
        Require(disabled.Outcome == PrivacyChangeOutcome.Verified && disabled.SettingKey == "tailored-experiences" &&
            disabledHost.Writes.SequenceEqual<bool?>([false]), "Tailored experiences uses the shared guarded coordinator");

        var speechHost = new Host(Evidence(false, null), Evidence(true, 1));
        var speech = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.OnlineSpeech,
            true, true, Path.Combine(root, "online-speech"), speechHost);
        Require(speech.Outcome == PrivacyChangeOutcome.Verified && speech.SettingKey == "online-speech" &&
            speechHost.Writes.SequenceEqual<bool?>([true]), "Online speech recognition uses the shared guarded coordinator");

        var syncHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var sync = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.ClipboardSync,
            false, true, Path.Combine(root, "clipboard-sync"), syncHost);
        Require(sync.Outcome == PrivacyChangeOutcome.Verified && sync.SettingKey == "clipboard-sync" &&
            syncHost.Writes.SequenceEqual<bool?>([false]), "Clipboard synchronization uses the shared guarded coordinator");

        var launchTrackingHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var launchTracking = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.AppLaunchTracking,
            false, true, Path.Combine(root, "app-launch-tracking"), launchTrackingHost);
        Require(launchTracking.Outcome == PrivacyChangeOutcome.Verified && launchTracking.SettingKey == "app-launch-tracking" &&
            launchTrackingHost.Writes.SequenceEqual<bool?>([false]), "Application launch tracking uses the shared guarded coordinator");

        var tipsHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var tips = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.WindowsTips,
            false, true, Path.Combine(root, "windows-tips"), tipsHost);
        Require(tips.Outcome == PrivacyChangeOutcome.Verified && tips.SettingKey == "windows-tips" &&
            tipsHost.Writes.SequenceEqual<bool?>([false]), "Windows tips uses the shared guarded coordinator");

        var suggestedHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var suggested = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.SuggestedNotifications,
            false, true, Path.Combine(root, "suggested-notifications"), suggestedHost);
        Require(suggested.Outcome == PrivacyChangeOutcome.Verified && suggested.SettingKey == "suggested-notifications" &&
            suggestedHost.Writes.SequenceEqual<bool?>([false]), "Suggested notifications use the shared guarded coordinator");

        var consumerContentHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var consumerContent = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.ConsumerContent,
            false, true, Path.Combine(root, "consumer-content"), consumerContentHost);
        Require(consumerContent.Outcome == PrivacyChangeOutcome.Verified && consumerContent.SettingKey == "consumer-content" &&
            consumerContentHost.Writes.SequenceEqual<bool?>([false]), "Consumer content uses the shared guarded coordinator");

        var spotlightHost = new Host(Evidence(true, 1), Evidence(true, 0));
        var spotlight = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.LockScreenSpotlight,
            false, true, Path.Combine(root, "lock-screen-spotlight"), spotlightHost);
        Require(spotlight.Outcome == PrivacyChangeOutcome.Verified && spotlight.SettingKey == "lock-screen-spotlight" &&
            spotlightHost.Writes.SequenceEqual<bool?>([false]), "Lock screen Spotlight uses the shared guarded coordinator");

        foreach (var definition in new[]
        {
            PrivacyBooleanSettingCatalog.LockScreenSpotlightOverlay,
            PrivacyBooleanSettingCatalog.StartSuggestions,
            PrivacyBooleanSettingCatalog.PreinstalledAppSuggestions,
            PrivacyBooleanSettingCatalog.SilentAppSuggestions,
            PrivacyBooleanSettingCatalog.TypingAndInkingPersonalization,
            PrivacyBooleanSettingCatalog.WebSearchResults,
            PrivacyBooleanSettingCatalog.OemAppSuggestions
        })
        {
            var contentHost = new Host(Evidence(true, 1), Evidence(true, 0));
            var content = await PrivacyBooleanSettingExecution.RunAsync(definition, false, true,
                Path.Combine(root, definition.Key), contentHost);
            Require(content.Outcome == PrivacyChangeOutcome.Verified && content.SettingKey == definition.Key &&
                contentHost.Writes.SequenceEqual<bool?>([false]), definition.Title + " uses the shared guarded coordinator");
        }

        var managed = new Host(Evidence(true, 1, true, 0));
        var blocked = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.TailoredExperiences,
            false, true, Path.Combine(root, "managed"), managed);
        Require(blocked.Outcome == PrivacyChangeOutcome.Blocked && managed.Writes.Count == 0, "Any explicit policy value blocks a local change");

        var forcedDevicePolicy = new Host(Evidence(true, 1, true, 2, true, 1));
        blocked = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.CameraAccess,
            false, true, Path.Combine(root, "camera-managed"), forcedDevicePolicy);
        Require(blocked.Outcome == PrivacyChangeOutcome.Blocked && forcedDevicePolicy.Writes.Count == 0,
            "Camera three-state machine policy blocks a current-user change");

        var machineDenied = new Host(Evidence(true, 0, false, null, true, 0));
        blocked = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.MicrophoneAccess,
            true, true, Path.Combine(root, "microphone-machine-denied"), machineDenied);
        Require(blocked.Outcome == PrivacyChangeOutcome.Blocked && machineDenied.Writes.Count == 0,
            "Machine-wide denial blocks ineffective current-user enablement");

        var cameraDisableHost = new Host(Evidence(true, 1, false, null, true, 1), Evidence(true, 0, false, null, true, 1));
        var cameraDisabled = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.CameraAccess,
            false, true, Path.Combine(root, "camera-disable"), cameraDisableHost);
        Require(cameraDisabled.Outcome == PrivacyChangeOutcome.Verified && cameraDisableHost.Writes.SequenceEqual<bool?>([false]),
            "Camera consent uses the guarded coordinator while preserving the machine-wide value");

        var unsupported = new Host(new PrivacyBooleanEvidence(false, true, 4, false, null, "fixture"));
        blocked = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            false, true, Path.Combine(root, "unsupported"), unsupported);
        Require(blocked.Outcome == PrivacyChangeOutcome.Blocked && unsupported.Writes.Count == 0, "Unsupported evidence blocks before mutation");

        string failedRoot = Path.Combine(root, "failed");
        var failedHost = new Host(Evidence(true, 0)) { FailWrite = true };
        var failed = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            true, true, failedRoot, failedHost);
        Require(failed.Outcome == PrivacyChangeOutcome.Uncertain && File.Exists(Path.Combine(failedRoot, "advertising-id.pending.json")),
            "Failed write retains durable no-repeat evidence");
        var recoveryHost = new Host(Evidence(true, 1));
        var recovered = await PrivacyBooleanSettingExecution.ReconcileAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            failedRoot, recoveryHost);
        Require(recovered.Outcome == PrivacyChangeReconciliationOutcome.Verified && recoveryHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(failedRoot, "advertising-id.pending.json")), "Reconciliation verifies without repeating a write");

        string ambiguousRoot = Path.Combine(root, "ambiguous");
        await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.TailoredExperiences, true, true,
            ambiguousRoot, new Host(Evidence(true, 0)) { FailWrite = true });
        var ambiguous = await PrivacyBooleanSettingExecution.ReconcileAsync(PrivacyBooleanSettingCatalog.TailoredExperiences,
            ambiguousRoot, new Host(Evidence(false, null)));
        Require(ambiguous.Outcome == PrivacyChangeReconciliationOutcome.Inconclusive &&
            File.Exists(Path.Combine(ambiguousRoot, "tailored-experiences.pending.json")), "Ambiguous state retains its guard");

        var laterChange = new Host(Evidence(true, 0));
        var refusedUndo = await PrivacyBooleanSettingExecution.UndoAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            enabled.JournalPath!, Path.Combine(root, "later-change"), laterChange);
        Require(refusedUndo.Outcome == PrivacyChangeOutcome.Blocked && laterChange.Writes.Count == 0,
            "Undo refuses to overwrite a later current-user change");

        string finalPolicyRoot = Path.Combine(root, "final-policy");
        var finalPolicyHost = new Host(Evidence(true, 1))
        {
            FinalEligibility = Evidence(true, 1, true, 0)
        };
        var finalPolicy = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.WindowsTips,
            false, true, finalPolicyRoot, finalPolicyHost);
        Require(finalPolicy.Outcome == PrivacyChangeOutcome.Blocked && finalPolicyHost.Writes.Count == 0 &&
            finalPolicy.JournalPath is not null && File.Exists(finalPolicy.JournalPath) &&
            File.Exists(finalPolicy.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(finalPolicyRoot, "windows-tips.pending.json")),
            "Policy appearing after preparation prevents the registry write and closes the prepared guard");

        string changedMachineRoot = Path.Combine(root, "changed-machine-consent");
        var changedMachineHost = new Host(Evidence(true, 1, false, null, true, 1))
        {
            FinalEligibility = Evidence(true, 1, false, null, true, 0)
        };
        var changedMachine = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.CameraAccess,
            false, true, changedMachineRoot, changedMachineHost);
        Require(changedMachine.Outcome == PrivacyChangeOutcome.Blocked && changedMachineHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(changedMachineRoot, "camera-access.pending.json")) &&
            File.Exists(changedMachine.JournalPath + ".prepared.json"),
            "Machine-wide camera consent changing after preparation prevents the account write");

        string unavailableFinalRoot = Path.Combine(root, "unavailable-final");
        var unavailableFinalHost = new Host(Evidence(true, 0)) { ReadErrorAt = 2, ReadError = new IOException("fixture") };
        var unavailableFinal = await PrivacyBooleanSettingExecution.RunAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            true, true, unavailableFinalRoot, unavailableFinalHost);
        Require(unavailableFinal.Outcome == PrivacyChangeOutcome.Blocked && unavailableFinalHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(unavailableFinalRoot, "advertising-id.pending.json")) &&
            File.Exists(unavailableFinal.JournalPath + ".prepared.json") &&
            unavailableFinal.Detail.Contains("No registry write was dispatched", StringComparison.Ordinal),
            "Unavailable final privacy evidence archives a prepared no-write result");

        var finalUndoChange = new Host(Evidence(true, 1)) { FinalEligibility = Evidence(true, 0) };
        var finalUndoBlocked = await PrivacyBooleanSettingExecution.UndoAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            enabled.JournalPath!, Path.Combine(root, "final-undo-change"), finalUndoChange);
        Require(finalUndoBlocked.Outcome == PrivacyChangeOutcome.Blocked && finalUndoChange.Writes.Count == 0,
            "A value changed after undo preparation is preserved");

        string unavailableUndoRoot = Path.Combine(root, "unavailable-final-undo");
        var unavailableUndoHost = new Host(Evidence(true, 1)) { ReadErrorAt = 2, ReadError = new IOException("fixture") };
        var unavailableUndo = await PrivacyBooleanSettingExecution.UndoAsync(PrivacyBooleanSettingCatalog.AdvertisingId,
            enabled.JournalPath!, unavailableUndoRoot, unavailableUndoHost);
        Require(unavailableUndo.Outcome == PrivacyChangeOutcome.Blocked && unavailableUndoHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(unavailableUndoRoot, "advertising-id.pending.json")) &&
            File.Exists(unavailableUndo.JournalPath + ".prepared.json") &&
            unavailableUndo.Detail.Contains("No registry write was dispatched", StringComparison.Ordinal),
            "Unavailable final undo evidence archives a prepared no-write result");

        Console.WriteLine("PASS: 38 reusable privacy-setting definitions preserve policy, machine consent, durable evidence, final eligibility, exact verification, no-repeat recovery and guarded undo; simulated changes and read-only live evidence only.");
    }

    private static PrivacyBooleanEvidence Evidence(bool present, int? value, bool policyPresent = false, int? policyValue = null,
        bool machinePresent = false, int? machineValue = null) =>
        new(true, present, value, policyPresent, policyValue, "fixture", machinePresent, machineValue);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host(params PrivacyBooleanEvidence[] evidence) : IPrivacyBooleanSettingHost
    {
        private readonly Queue<PrivacyBooleanEvidence> values = new(evidence);
        public int Reads { get; private set; }
        public List<bool?> Writes { get; } = [];
        public bool FailWrite { get; init; }
        public PrivacyBooleanEvidence? FinalEligibility { get; init; }
        public int ReadErrorAt { get; init; }
        public Exception? ReadError { get; init; }
        public Task<PrivacyBooleanEvidence> ReadAsync(CancellationToken token)
        {
            Reads++;
            if (Reads == ReadErrorAt && ReadError is not null) return Task.FromException<PrivacyBooleanEvidence>(ReadError);
            if (Writes.Count == 0)
                return Task.FromResult(Reads > 1 && FinalEligibility is not null ? FinalEligibility : values.Peek());
            return Task.FromResult(values.Peek());
        }
        public Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token)
        {
            Writes.Add(enabled);
            if (values.Count > 1) values.Dequeue();
            return Task.FromResult(FailWrite ? new ToolResult(1, "", "fixture", false) : new ToolResult(0, "", "", false));
        }
    }
}
