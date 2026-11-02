using System.Text.Json;
using Microsoft.Win32;

namespace KillerBench.Engine.Security;

public enum PrivacyRegistryScope { CurrentUser, LocalMachine }
public enum PrivacyBooleanValueKind { DWord, ConsentString }

public sealed record PrivacyBooleanSettingDefinition(string Key, string Title, string Effects,
    string UserPath, string UserValueName, PrivacyRegistryScope PolicyScope, string PolicyPath, string PolicyValueName,
    PrivacyBooleanValueKind ValueKind = PrivacyBooleanValueKind.DWord, string? MachinePath = null, string? MachineValueName = null);

public static class PrivacyBooleanSettingCatalog
{
    public static PrivacyBooleanSettingDefinition AdvertisingId { get; } = new("advertising-id", "Advertising ID",
        "Changes the Advertising ID setting for the account running KillerBench. Applications may use this identifier for personalized advertising. Machine policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy");
    public static PrivacyBooleanSettingDefinition TailoredExperiences { get; } = new("tailored-experiences", "Tailored experiences",
        "Changes whether diagnostic data can be used for personalized recommendations, tips and offers for the account running KillerBench. Diagnostic collection settings are separate. User policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", PrivacyRegistryScope.CurrentUser,
        @"Software\Policies\Microsoft\Windows\CloudContent", "DisableTailoredExperiencesWithDiagnosticData");
    public static PrivacyBooleanSettingDefinition OnlineSpeech { get; } = new("online-speech", "Online speech recognition",
        "Changes whether Microsoft online speech recognition is accepted for the account running KillerBench. Dictation and applications that use cloud speech services can be affected. Machine policy is preserved.",
        @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization");
    public static PrivacyBooleanSettingDefinition ClipboardSync { get; } = new("clipboard-sync", "Clipboard synchronization",
        "Changes whether Clipboard contents can synchronize across devices for the account running KillerBench. Local Clipboard history and stored Clipboard contents are separate and are not cleared. Machine policy is preserved.",
        @"Software\Microsoft\Clipboard", "EnableClipboardRoaming", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard");
    public static PrivacyBooleanSettingDefinition AppLaunchTracking { get; } = new("app-launch-tracking", "Application launch tracking",
        "Changes whether Windows tracks application launches for Start and search personalization for the account running KillerBench. Pinned applications are not changed. User policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", PrivacyRegistryScope.CurrentUser,
        @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoInstrumentation");
    public static PrivacyBooleanSettingDefinition WindowsTips { get; } = new("windows-tips", "Windows tips",
        "Changes whether Windows can show contextual tips for the account running KillerBench. Diagnostic collection settings and other suggestion categories are separate. Machine policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding");
    public static PrivacyBooleanSettingDefinition SuggestedNotifications { get; } = new("suggested-notifications", "Suggested notifications",
        "Changes whether Windows can show suggested notification toasts for the account running KillerBench. Ordinary application notifications and notification history are separate. Machine consumer-content policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested", "Enabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
    public static PrivacyBooleanSettingDefinition ConsumerContent { get; } = new("consumer-content", "Consumer content",
        "Changes whether Windows can deliver Microsoft and partner suggestions, recommendations and promoted content for the account running KillerBench. Installed applications, ordinary application notifications and diagnostic collection settings are separate. Machine consumer-content policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "ContentDeliveryAllowed", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
    public static PrivacyBooleanSettingDefinition LockScreenSpotlight { get; } = new("lock-screen-spotlight", "Lock screen Spotlight",
        "Changes whether Windows can rotate Spotlight images and content on the lock screen for the account running KillerBench. The selected lock-screen image, sign-in background and other suggestion categories are separate. Machine Spotlight policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures");
    public static PrivacyBooleanSettingDefinition LockScreenSpotlightOverlay { get; } = new("lock-screen-spotlight-overlay", "Lock screen Spotlight overlay",
        "Changes whether Windows can show Spotlight overlay content on the lock screen for the account running KillerBench. Rotating images, the selected lock-screen image and sign-in background are separate. Machine Spotlight policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures");
    public static PrivacyBooleanSettingDefinition StartSuggestions { get; } = new("start-suggestions", "Start suggestions",
        "Changes whether Windows can show suggested content in Start for the account running KillerBench. Pinned and installed applications are not removed. Machine consumer-content policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
    public static PrivacyBooleanSettingDefinition PreinstalledAppSuggestions { get; } = new("preinstalled-app-suggestions", "Preinstalled app suggestions",
        "Changes whether Windows can offer preinstalled application suggestions for the account running KillerBench. Applications already installed are not removed. Machine consumer-content policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
    public static PrivacyBooleanSettingDefinition SilentAppSuggestions { get; } = new("silent-app-suggestions", "Silent app suggestions",
        "Changes whether Windows can silently deliver suggested applications for the account running KillerBench. Applications already installed are not removed. Machine consumer-content policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
    public static PrivacyBooleanSettingDefinition TypingAndInkingPersonalization { get; } = new("typing-inking-personalization", "Typing and inking personalization",
        "Changes whether Windows can use typing and inking activity to personalize suggestions for the account running KillerBench. Keyboard layouts, dictionaries and cloud speech recognition are separate. Machine policy is preserved.",
        @"Software\Microsoft\Input\TIPC", "Enabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization");
    public static PrivacyBooleanSettingDefinition WebSearchResults { get; } = new("web-search-results", "Web search results",
        "Changes whether Windows Search can include web results for the account running KillerBench. Local indexing, browser settings and search history are separate. Machine policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch");
    public static PrivacyBooleanSettingDefinition OemAppSuggestions { get; } = new("oem-app-suggestions", "OEM app suggestions",
        "Changes whether Windows can offer computer-manufacturer application suggestions for the account running KillerBench. Applications already installed and manufacturer support software are not removed. Machine consumer-content policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "OemPreInstalledAppsEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
    public static PrivacyBooleanSettingDefinition CameraAccess { get; } = new("camera-access", "Camera access",
        "Changes camera access for the account running KillerBench. Per-application exceptions, camera drivers and machine-wide access are separate and are not changed. Machine policy and a machine-wide denial are preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", "Value", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCamera", PrivacyBooleanValueKind.ConsentString,
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", "Value");
    public static PrivacyBooleanSettingDefinition MicrophoneAccess { get; } = new("microphone-access", "Microphone access",
        "Changes microphone access for the account running KillerBench. Per-application exceptions, audio drivers and machine-wide access are separate and are not changed. Machine policy and a machine-wide denial are preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone", "Value", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMicrophone", PrivacyBooleanValueKind.ConsentString,
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone", "Value");
    public static PrivacyBooleanSettingDefinition LocationAccess { get; } = Consent("location-access", "Location access", "location", "LetAppsAccessLocation", "the device location", "Location services, geofences and per-application exceptions");
    public static PrivacyBooleanSettingDefinition ContactsAccess { get; } = Consent("contacts-access", "Contacts access", "contacts", "LetAppsAccessContacts", "contacts", "Stored contacts and per-application exceptions");
    public static PrivacyBooleanSettingDefinition CalendarAccess { get; } = Consent("calendar-access", "Calendar access", "appointments", "LetAppsAccessCalendar", "calendar appointments", "Stored appointments and per-application exceptions");
    public static PrivacyBooleanSettingDefinition CallHistoryAccess { get; } = Consent("call-history-access", "Call history access", "phoneCallHistory", "LetAppsAccessCallHistory", "call history", "Stored call history and per-application exceptions");
    public static PrivacyBooleanSettingDefinition EmailAccess { get; } = Consent("email-access", "Email access", "email", "LetAppsAccessEmail", "email", "Mail accounts, stored messages and per-application exceptions");
    public static PrivacyBooleanSettingDefinition NotificationsAccess { get; } = Consent("notifications-access", "Notification access", "userNotificationListener", "LetAppsAccessNotifications", "notifications from other apps", "Ordinary notification delivery and per-application exceptions");
    public static PrivacyBooleanSettingDefinition AccountInfoAccess { get; } = Consent("account-info-access", "Account info access", "userAccountInformation", "LetAppsAccessAccountInfo", "the account name and picture", "The account itself and per-application exceptions");
    public static PrivacyBooleanSettingDefinition MessagingAccess { get; } = Consent("messaging-access", "Messaging access", "chat", "LetAppsAccessMessaging", "text and MMS messages", "Stored messages and per-application exceptions");
    public static PrivacyBooleanSettingDefinition RadiosAccess { get; } = Consent("radios-access", "Radio control", "radios", "LetAppsAccessRadios", "Wi-Fi and Bluetooth radio control", "Radio state, adapters and per-application exceptions");
    public static PrivacyBooleanSettingDefinition OtherDevicesAccess { get; } = Consent("other-devices-access", "Communication with unpaired devices", "bluetoothSync", "LetAppsSyncWithDevices", "communication with unpaired devices", "Paired devices, Bluetooth itself and per-application exceptions");
    public static PrivacyBooleanSettingDefinition AppDiagnosticsAccess { get; } = Consent("app-diagnostics-access", "App diagnostics access", "appDiagnostics", "LetAppsGetDiagnosticInfo", "diagnostic information about other apps", "Windows diagnostic data settings and per-application exceptions");
    public static PrivacyBooleanSettingDefinition TasksAccess { get; } = Consent("tasks-access", "Tasks access", "userDataTasks", "LetAppsAccessTasks", "task items", "Stored tasks and per-application exceptions");
    public static PrivacyBooleanSettingDefinition PhoneCallsAccess { get; } = Consent("phone-calls-access", "Phone calls access", "phoneCall", "LetAppsAccessPhone", "making phone calls", "Call history and per-application exceptions");
    public static PrivacyBooleanSettingDefinition MotionAccess { get; } = Consent("motion-access", "Motion activity access", "activity", "LetAppsAccessMotion", "motion activity", "Sensors, drivers and per-application exceptions");
    public static PrivacyBooleanSettingDefinition EyeTrackerAccess { get; } = Consent("eye-tracker-access", "Eye tracker access", "gazeInput", "LetAppsAccessGazeInput", "an eye tracker", "Eye control accessibility features and per-application exceptions");
    public static PrivacyBooleanSettingDefinition ScreenshotAccess { get; } = Consent("screenshot-access", "Programmatic screenshots", "graphicsCaptureProgrammatic", "LetAppsAccessGraphicsCaptureProgrammatic", "taking screenshots without a picker", "Screenshots you take yourself and per-application exceptions");
    public static PrivacyBooleanSettingDefinition ScreenshotBorderAccess { get; } = Consent("screenshot-border-access", "Screenshots without a border", "graphicsCaptureWithoutBorder", "LetAppsAccessGraphicsCaptureWithoutBorder", "capturing the screen without the capture border", "Screenshots you take yourself and per-application exceptions");
    public static PrivacyBooleanSettingDefinition CloudSearchMicrosoftAccount { get; } = new("cloud-search-msa", "Cloud content search (Microsoft account)",
        "Changes whether Windows Search can include OneDrive, Outlook and other Microsoft account content for the account running KillerBench. Local indexing and web results are separate. Machine policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsMSACloudSearchEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCloudSearch");
    public static PrivacyBooleanSettingDefinition CloudSearchWorkAccount { get; } = new("cloud-search-work", "Cloud content search (work or school)",
        "Changes whether Windows Search can include OneDrive for work or school, SharePoint and other organization content for the account running KillerBench. Local indexing and web results are separate. Machine policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsAADCloudSearchEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCloudSearch");
    public static PrivacyBooleanSettingDefinition SearchHighlights { get; } = new("search-highlights", "Search highlights",
        "Changes whether the Start and taskbar search boxes show online search highlights for the account running KillerBench. Search results and web results are separate. Machine policy is preserved.",
        @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", PrivacyRegistryScope.LocalMachine,
        @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "EnableDynamicContentInWSB");
    public static IReadOnlyList<PrivacyBooleanSettingDefinition> All { get; } = [AdvertisingId, TailoredExperiences, OnlineSpeech, ClipboardSync, AppLaunchTracking, WindowsTips, SuggestedNotifications, ConsumerContent, LockScreenSpotlight, LockScreenSpotlightOverlay, StartSuggestions, PreinstalledAppSuggestions, SilentAppSuggestions, TypingAndInkingPersonalization, WebSearchResults, OemAppSuggestions, CameraAccess, MicrophoneAccess,
        LocationAccess, ContactsAccess, CalendarAccess, CallHistoryAccess, EmailAccess, NotificationsAccess, AccountInfoAccess, MessagingAccess, RadiosAccess, OtherDevicesAccess, AppDiagnosticsAccess, TasksAccess, PhoneCallsAccess, MotionAccess, EyeTrackerAccess, ScreenshotAccess, ScreenshotBorderAccess,
        CloudSearchMicrosoftAccount, CloudSearchWorkAccount, SearchHighlights];

    private static PrivacyBooleanSettingDefinition Consent(string key, string title, string capability, string policy, string subject, string separate)
    {
        string path = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + capability;
        return new(key, title,
            "Changes whether apps can use " + subject + " for the account running KillerBench. " + separate + " are separate and are not changed. Machine policy and a machine-wide denial are preserved.",
            path, "Value", PrivacyRegistryScope.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", policy, PrivacyBooleanValueKind.ConsentString, path, "Value");
    }
    public static PrivacyBooleanSettingDefinition? Find(string key) => All.SingleOrDefault(value => value.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
}

public sealed record PrivacyBooleanEvidence(bool Complete, bool UserValuePresent, int? UserValue,
    bool PolicyValuePresent, int? PolicyValue, string Detail, bool MachineValuePresent = false, int? MachineValue = null);
public sealed record PrivacyBooleanResult(PrivacyChangeOutcome Outcome, string SettingKey, bool RequestedEnabled,
    string Detail, string? JournalPath = null, PrivacyBooleanEvidence? Before = null, ToolResult? Command = null,
    PrivacyBooleanEvidence? After = null)
{
    [System.Text.Json.Serialization.JsonRequired]
    public string Machine { get; init; } = Environment.MachineName;
    public int GetExitCode() => Outcome is PrivacyChangeOutcome.Preview or PrivacyChangeOutcome.Verified ? 0 :
        Outcome == PrivacyChangeOutcome.Stopped ? 130 : 3;
}
public sealed record PrivacyBooleanReconciliationResult(PrivacyChangeReconciliationOutcome Outcome, string SettingKey,
    string Detail, string PendingPath, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is PrivacyChangeReconciliationOutcome.NoPendingRecord or PrivacyChangeReconciliationOutcome.Verified ? 0 : 3;
}

public interface IPrivacyBooleanSettingHost
{
    Task<PrivacyBooleanEvidence> ReadAsync(CancellationToken token);
    Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token);
}

public static class PrivacyBooleanSettingExecution
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true
    };

    public static async Task<PrivacyBooleanResult> RunAsync(PrivacyBooleanSettingDefinition definition, bool requestedEnabled,
        bool execute, string journalFolder, IPrivacyBooleanSettingHost host, IProgress<string>? progress = null,
        TimeSpan? commandTimeout = null, CancellationToken token = default)
    {
        Validate(definition, journalFolder, host);
        TimeSpan limit = Timeout(commandTimeout);
        token.ThrowIfCancellationRequested();
        if (!execute) return new(PrivacyChangeOutcome.Preview, definition.Key, requestedEnabled,
            definition.Effects + " Execution repeats exact current-value and policy checks before changing the setting.");
        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, definition.Key + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, definition.Key + ".pending.json");
        if (File.Exists(pendingPath)) return new(PrivacyChangeOutcome.Blocked, definition.Key, requestedEnabled,
            "A prior " + definition.Title + " change needs reconciliation before another change can start.", pendingPath);
        Report(progress, "Checking " + definition.Title + " and policy.");
        var before = await Required(host.ReadAsync, definition.Title + " inspection", TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
        string? blocker = Blocker(definition, before, requestedEnabled);
        if (blocker is not null) return new(PrivacyChangeOutcome.Blocked, definition.Key, requestedEnabled, blocker, Before: before);
        if (before.UserValuePresent && before.UserValue == (requestedEnabled ? 1 : 0))
            return new(PrivacyChangeOutcome.Blocked, definition.Key, requestedEnabled,
                definition.Title + " already has the requested current-user value. No change was made.", Before: before);
        string journalPath = Path.Combine(root, definition.Key + "-" + Guid.NewGuid().ToString("N") + ".json");
        var pending = new Pending(1, Environment.MachineName, definition.Key, DateTimeOffset.UtcNow,
            true, requestedEnabled ? 1 : 0, before, journalPath);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        ToolResult? command = null;
        bool writeDispatched = false;
        PrivacyBooleanResult result;
        try
        {
            Report(progress, "Confirming final " + definition.Title + " eligibility.");
            var current = await Required(host.ReadAsync, definition.Title + " final inspection", TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
            int target = requestedEnabled ? 1 : 0;
            if (Blocker(definition, current, requestedEnabled) is not null || !Equivalent(current, before) ||
                current.UserValuePresent && current.UserValue == target)
            {
                string detail = Blocker(definition, current, requestedEnabled) is not null
                    ? "Value or policy evidence changed after preparation. " + definition.Title + " was not changed."
                    : current.UserValuePresent && current.UserValue == target
                        ? definition.Title + " reached the requested value after preparation. No write was required."
                        : definition.Title + " changed after preparation. The later value was preserved.";
                result = new(PrivacyChangeOutcome.Blocked, definition.Key, requestedEnabled, detail,
                    journalPath, before, After: current);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, (requestedEnabled ? "Enabling " : "Disabling ") + definition.Title + ".");
            writeDispatched = true;
            command = await Required(ct => host.WriteUserValueAsync(requestedEnabled, ct), definition.Title + " change", limit, token: token).ConfigureAwait(false);
            if (!Successful(command)) result = Uncertain(definition, requestedEnabled, journalPath, before, command,
                "The setting change did not provide complete success evidence. Current state may have changed; reconcile before retrying.");
            else
            {
                var after = await Required(host.ReadAsync, definition.Title + " verification", TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
                bool verified = Blocker(definition, after, requestedEnabled) is null && after.UserValuePresent && after.UserValue == (requestedEnabled ? 1 : 0);
                result = new(verified ? PrivacyChangeOutcome.Verified : PrivacyChangeOutcome.Uncertain, definition.Key, requestedEnabled,
                    verified ? "Fresh evidence confirms the requested " + definition.Title + " value and unchanged policy state."
                        : "The setting command completed, but fresh evidence did not confirm the requested unmanaged value.",
                    journalPath, before, command, after);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(PrivacyChangeOutcome.Stopped, definition.Key, requestedEnabled,
                writeDispatched
                    ? "The operation stopped after the registry write may have been dispatched. Reconcile before retrying."
                    : "The final setting check was canceled before any registry write was dispatched.",
                journalPath, before, command);
        }
        catch (Exception error) when (Expected(error))
        {
            result = writeDispatched
                ? Uncertain(definition, requestedEnabled, journalPath, before, command,
                    "The change was interrupted: " + error.Message + ". Reconcile before retrying.")
                : new(PrivacyChangeOutcome.Blocked, definition.Key, requestedEnabled,
                    "Final setting evidence was unavailable: " + error.Message + ". No registry write was dispatched.",
                    journalPath, before, command);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == PrivacyChangeOutcome.Verified || !writeDispatched) File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<PrivacyBooleanReconciliationResult> ReconcileAsync(PrivacyBooleanSettingDefinition definition,
        string journalFolder, IPrivacyBooleanSettingHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        Validate(definition, journalFolder, host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, definition.Key + ".pending.json");
        if (!File.Exists(pendingPath)) return new(PrivacyChangeReconciliationOutcome.NoPendingRecord, definition.Key,
            "No pending " + definition.Title + " change is present.", pendingPath);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, definition.Key + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            var pending = await ReadAsync<Pending>(pendingPath, token).ConfigureAwait(false);
            if (pending.Schema != 1 || pending.SettingKey != definition.Key || !CurrentMachine(pending.Machine) ||
                pending.StartedUtc == default || string.IsNullOrWhiteSpace(pending.JournalPath) ||
                pending.TargetValuePresent && pending.TargetValue is not (0 or 1) ||
                !pending.TargetValuePresent && pending.TargetValue is not null || Blocker(definition, pending.Before, pending.TargetValuePresent && pending.TargetValue == 1) is not null)
                throw new InvalidDataException("Pending privacy-setting record is invalid.");
            Report(progress, "Rechecking " + definition.Title + " without repeating the change.");
            var current = await Required(host.ReadAsync, definition.Title + " reconciliation", TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
            if (Blocker(definition, current, pending.TargetValuePresent && pending.TargetValue == 1) is not null) return new(PrivacyChangeReconciliationOutcome.Inconclusive,
                definition.Key, "Current value or policy evidence is incomplete or managed. The pending record was retained.", pendingPath);
            PrivacyChangeReconciliationOutcome outcome;
            string detail;
            if (current.UserValuePresent == pending.TargetValuePresent &&
                (!current.UserValuePresent || current.UserValue == pending.TargetValue))
            { outcome = PrivacyChangeReconciliationOutcome.Verified; detail = "Fresh evidence confirms the requested value. The prior attempt was reconciled without repeating it."; }
            else if (Equivalent(current, pending.Before))
            { outcome = PrivacyChangeReconciliationOutcome.CurrentStateDiffers; detail = "Fresh evidence confirms the original value remains. The prior attempt was archived without repeating it."; }
            else return new(PrivacyChangeReconciliationOutcome.Inconclusive, definition.Key,
                "Current state differs from both the requested and original values. The pending record was retained.", pendingPath);
            string archive = Path.Combine(root, definition.Key + "-reconciliation-" + Guid.NewGuid().ToString("N") + ".json");
            var result = new PrivacyBooleanReconciliationResult(outcome, definition.Key, detail, pendingPath, archive);
            await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(result, Capture.JsonOptions), token: token).ConfigureAwait(false);
            File.Move(pendingPath, archive + ".prepared.json");
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Expected(error))
        { return new(PrivacyChangeReconciliationOutcome.Inconclusive, definition.Key, "Current state could not be established: " + error.Message + " The pending record was retained.", pendingPath); }
    }

    public static async Task<PrivacyBooleanResult> UndoAsync(PrivacyBooleanSettingDefinition definition, string completedJournalPath,
        string journalFolder, IPrivacyBooleanSettingHost host, TimeSpan? commandTimeout = null, CancellationToken token = default)
    {
        Validate(definition, journalFolder, host);
        var prior = await ReadAsync<PrivacyBooleanResult>(completedJournalPath, token).ConfigureAwait(false);
        if (prior.Outcome != PrivacyChangeOutcome.Verified || prior.SettingKey != definition.Key || !CurrentMachine(prior.Machine) ||
            prior.Before is null || prior.After is null || Blocker(definition, prior.Before, prior.RequestedEnabled) is not null ||
            Blocker(definition, prior.After, prior.RequestedEnabled) is not null || !prior.After.UserValuePresent || prior.After.UserValue != (prior.RequestedEnabled ? 1 : 0))
            return new(PrivacyChangeOutcome.Blocked, definition.Key, prior.RequestedEnabled,
                "Only a complete same-machine verified " + definition.Title + " change can be undone.", completedJournalPath);
        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, definition.Key + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, definition.Key + ".pending.json");
        if (File.Exists(pendingPath)) return new(PrivacyChangeOutcome.Blocked, definition.Key, prior.RequestedEnabled,
            "A pending change needs reconciliation before undo can start.", pendingPath);
        var current = await Required(host.ReadAsync, definition.Title + " undo inspection", TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
        if (Blocker(definition, current, prior.RequestedEnabled) is not null || !current.UserValuePresent || current.UserValue != (prior.RequestedEnabled ? 1 : 0))
            return new(PrivacyChangeOutcome.Blocked, definition.Key, prior.RequestedEnabled,
                "The current value no longer matches this journal. Undo will not overwrite a later or managed change.", Before: current);
        string journal = Path.Combine(root, definition.Key + "-undo-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new Pending(1, Environment.MachineName,
            definition.Key, DateTimeOffset.UtcNow, prior.Before.UserValuePresent, prior.Before.UserValue,
            current, journal), Capture.JsonOptions), token: token).ConfigureAwait(false);
        ToolResult? command = null;
        bool writeDispatched = false;
        PrivacyBooleanResult result;
        try
        {
            var finalCurrent = await Required(host.ReadAsync, definition.Title + " final undo inspection", TimeSpan.FromSeconds(15),
                token: token).ConfigureAwait(false);
            if (Blocker(definition, finalCurrent, prior.Before.UserValuePresent && prior.Before.UserValue == 1) is not null || !Equivalent(finalCurrent, current))
            {
                result = new(PrivacyChangeOutcome.Blocked, definition.Key, prior.RequestedEnabled,
                    "The value or policy changed after undo preparation. The later state was preserved.",
                    journal, current, After: finalCurrent);
                await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journal + ".prepared.json");
                return result;
            }
            bool? restore = prior.Before.UserValuePresent ? prior.Before.UserValue == 1 : null;
            writeDispatched = true;
            command = await Required(ct => host.WriteUserValueAsync(restore, ct), definition.Title + " undo", Timeout(commandTimeout), token: token).ConfigureAwait(false);
            var after = Successful(command) ? await Required(host.ReadAsync, definition.Title + " undo verification", TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false) : null;
            bool verified = after is not null && Blocker(definition, after, prior.Before.UserValuePresent && prior.Before.UserValue == 1) is null && after.UserValuePresent == prior.Before.UserValuePresent &&
                (!after.UserValuePresent || after.UserValue == prior.Before.UserValue);
            result = new(verified ? PrivacyChangeOutcome.Verified : PrivacyChangeOutcome.Uncertain, definition.Key, prior.RequestedEnabled,
                verified ? "Fresh evidence confirms the exact prior " + definition.Title + " value was restored." : "Undo did not confirm the exact prior value.",
                journal, current, command, after);
        }
        catch (OperationCanceledException)
        {
            result = new(PrivacyChangeOutcome.Stopped, definition.Key, prior.RequestedEnabled,
                writeDispatched ? "Undo stopped after the registry write may have been dispatched. Reconcile before retrying."
                    : "The final undo check was canceled before any registry write was dispatched.",
                journal, current, command);
        }
        catch (Exception error) when (Expected(error))
        {
            result = writeDispatched
                ? Uncertain(definition, prior.RequestedEnabled, journal, current, command, "Undo was interrupted: " + error.Message)
                : new(PrivacyChangeOutcome.Blocked, definition.Key, prior.RequestedEnabled,
                    "Final undo evidence was unavailable: " + error.Message + ". No registry write was dispatched.",
                    journal, current, command);
        }
        await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == PrivacyChangeOutcome.Verified || !writeDispatched) File.Move(pendingPath, journal + ".prepared.json");
        return result;
    }

    private static string? Blocker(PrivacyBooleanSettingDefinition definition, PrivacyBooleanEvidence evidence, bool requestedEnabled)
    {
        bool unsupportedPolicy = evidence.PolicyValuePresent && (definition.ValueKind == PrivacyBooleanValueKind.ConsentString
            ? evidence.PolicyValue is < 0 or > 2
            : evidence.PolicyValue is not (0 or 1));
        bool unsupportedMachine = evidence.MachineValuePresent && (definition.ValueKind != PrivacyBooleanValueKind.ConsentString || evidence.MachineValue is not (0 or 1));
        if (!evidence.Complete || evidence.UserValuePresent && evidence.UserValue is not (0 or 1) || unsupportedPolicy || unsupportedMachine)
            return "Complete supported " + definition.Title + " evidence is required before a change.";
        if (evidence.PolicyValuePresent) return "Policy controls " + definition.Title + ". KillerBench will not override it.";
        return requestedEnabled && evidence.MachineValuePresent && evidence.MachineValue == 0
            ? "Machine-wide access is denied. KillerBench will not claim that enabling this account setting is effective."
            : null;
    }
    private static bool Equivalent(PrivacyBooleanEvidence left, PrivacyBooleanEvidence right) => left.UserValuePresent == right.UserValuePresent &&
        (!left.UserValuePresent || left.UserValue == right.UserValue) && left.PolicyValuePresent == right.PolicyValuePresent &&
        (!left.PolicyValuePresent || left.PolicyValue == right.PolicyValue) && left.MachineValuePresent == right.MachineValuePresent &&
        (!left.MachineValuePresent || left.MachineValue == right.MachineValue);
    private static PrivacyBooleanResult Uncertain(PrivacyBooleanSettingDefinition definition, bool requested, string path,
        PrivacyBooleanEvidence before, ToolResult? command, string detail) => new(PrivacyChangeOutcome.Uncertain, definition.Key, requested, detail, path, before, command);
    private static bool Successful(ToolResult value) => value.ExitCode == 0 && !value.TimedOut && !value.Truncated && string.IsNullOrWhiteSpace(value.Error);
    private static bool CurrentMachine(string machine) => machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    private static TimeSpan Timeout(TimeSpan? value) { var result = value ?? TimeSpan.FromSeconds(15); if (result <= TimeSpan.Zero || result > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(value)); return result; }
    private static void Validate(PrivacyBooleanSettingDefinition definition, string folder, IPrivacyBooleanSettingHost host) { ArgumentNullException.ThrowIfNull(definition); ArgumentException.ThrowIfNullOrWhiteSpace(folder); ArgumentNullException.ThrowIfNull(host); }
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name, TimeSpan limit, CancellationToken token) =>
        await (operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.")).WaitAsync(limit, token).ConfigureAwait(false)
        ?? throw new InvalidDataException(name + " provider returned no evidence.");
    private static async Task<T> ReadAsync<T>(string path, CancellationToken token)
    {
        await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("Privacy-setting journal size is invalid.");
        var bytes = new byte[checked((int)stream.Length)]; await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Privacy-setting journal contains conflicting fields.");
            return document.RootElement.Deserialize<T>(StrictJson) ?? throw new InvalidDataException("Privacy-setting journal is empty.");
        }
        catch (JsonException error) { throw new InvalidDataException("Privacy-setting journal is invalid.", error); }
    }
    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
    private static bool Expected(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException;
    private static void Report(IProgress<string>? progress, string message) { try { progress?.Report(message); } catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { } }
    private sealed record Pending(int Schema, string Machine, string SettingKey, DateTimeOffset StartedUtc,
        bool TargetValuePresent, int? TargetValue, PrivacyBooleanEvidence Before, string JournalPath);
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WindowsPrivacyBooleanSettingHost(PrivacyBooleanSettingDefinition definition) : IPrivacyBooleanSettingHost
{
    public Task<PrivacyBooleanEvidence> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var (userPresent, userValue, userSupported) = definition.ValueKind == PrivacyBooleanValueKind.ConsentString
                ? ReadConsent(RegistryHive.CurrentUser, definition.UserPath, definition.UserValueName)
                : ReadDWord(RegistryHive.CurrentUser, definition.UserPath, definition.UserValueName, 1);
            var (policyPresent, policyValue, policySupported) = ReadDWord(definition.PolicyScope == PrivacyRegistryScope.CurrentUser ? RegistryHive.CurrentUser : RegistryHive.LocalMachine,
                definition.PolicyPath, definition.PolicyValueName, definition.ValueKind == PrivacyBooleanValueKind.ConsentString ? 2 : 1);
            var (machinePresent, machineValue, machineSupported) = definition.ValueKind == PrivacyBooleanValueKind.ConsentString &&
                definition.MachinePath is not null && definition.MachineValueName is not null
                ? ReadConsent(RegistryHive.LocalMachine, definition.MachinePath, definition.MachineValueName)
                : (Present: false, Value: (int?)null, Supported: true);
            bool complete = userSupported && policySupported && machineSupported;
            return Task.FromResult(new PrivacyBooleanEvidence(complete, userPresent, userValue, policyPresent, policyValue,
                complete ? "Exact current-user, machine-default and policy values were read." : "A value has an unsupported type or range.",
                machinePresent, machineValue));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return Task.FromResult(new PrivacyBooleanEvidence(false, false, null, false, null, "Privacy-setting evidence is unavailable: " + error.Message)); }
    }
    public Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            if (enabled.HasValue)
            {
                using var key = root.CreateSubKey(definition.UserPath, true);
                if (definition.ValueKind == PrivacyBooleanValueKind.ConsentString)
                    key.SetValue(definition.UserValueName, enabled.Value ? "Allow" : "Deny", RegistryValueKind.String);
                else key.SetValue(definition.UserValueName, enabled.Value ? 1 : 0, RegistryValueKind.DWord);
                key.Flush();
            }
            else { using var key = root.OpenSubKey(definition.UserPath, true); key?.DeleteValue(definition.UserValueName, false); key?.Flush(); }
            return Task.FromResult(new ToolResult(0, "Current-user value written.", "", false));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return Task.FromResult(new ToolResult(-1, "", "Privacy-setting write failed: " + error.Message, false)); }
    }
    private static (bool Present, int? Value, bool Supported) ReadDWord(RegistryHive hive, string path, string name, int maximum)
    {
        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64); using var key = root.OpenSubKey(path, false);
        object? raw = key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (raw is null) return (false, null, true);
        return raw is int value && value >= 0 && value <= maximum && key!.GetValueKind(name) == RegistryValueKind.DWord
            ? (true, value, true) : (true, null, false);
    }
    private static (bool Present, int? Value, bool Supported) ReadConsent(RegistryHive hive, string path, string name)
    {
        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64); using var key = root.OpenSubKey(path, false);
        object? raw = key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (raw is null) return (false, null, true);
        if (key!.GetValueKind(name) != RegistryValueKind.String || raw is not string value) return (true, null, false);
        return value.Equals("Allow", StringComparison.OrdinalIgnoreCase) ? (true, 1, true) :
            value.Equals("Deny", StringComparison.OrdinalIgnoreCase) ? (true, 0, true) : (true, null, false);
    }
}
