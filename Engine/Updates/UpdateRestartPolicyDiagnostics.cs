using Microsoft.Win32;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdateRestartPolicySnapshot(bool Complete,
    object? SetComplianceDeadlineForQualityUpdates = null, object? ConfigureDeadlineForQualityUpdates = null,
    object? ConfigureDeadlineGracePeriod = null, object? ConfigureDeadlineNoAutoRebootForQualityUpdates = null,
    object? SetComplianceDeadlineForFeatureUpdates = null, object? ConfigureDeadlineForFeatureUpdates = null,
    object? ConfigureDeadlineGracePeriodForFeatureUpdates = null, object? ConfigureDeadlineNoAutoRebootForFeatureUpdates = null,
    object? SetUpdateNotificationLevel = null, object? UpdateNotificationLevel = null,
    object? NoUpdateNotificationsDuringActiveHours = null, string? CollectionIssue = null);

public static class UpdateRestartPolicyDiagnostics
{
    public static WorkflowCheck Check() => Assess(Read());

    public static UpdateRestartPolicySnapshot Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update restart-policy collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
            return new(true,
                policy?.GetValue("SetComplianceDeadlineForQU"), policy?.GetValue("ConfigureDeadlineForQualityUpdates"),
                policy?.GetValue("ConfigureDeadlineGracePeriod"), policy?.GetValue("ConfigureDeadlineNoAutoRebootForQualityUpdates"),
                policy?.GetValue("SetComplianceDeadlineForFU"), policy?.GetValue("ConfigureDeadlineForFeatureUpdates"),
                policy?.GetValue("ConfigureDeadlineGracePeriodForFeatureUpdates"), policy?.GetValue("ConfigureDeadlineNoAutoRebootForFeatureUpdates"),
                policy?.GetValue("SetUpdateNotificationLevel"), policy?.GetValue("UpdateNotificationLevel"),
                policy?.GetValue("NoUpdateNotificationsDuringActiveHours"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, CollectionIssue: error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(UpdateRestartPolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool valid = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) &&
            ValidSwitch(snapshot.SetComplianceDeadlineForQualityUpdates) &&
            ValidInteger(snapshot.ConfigureDeadlineForQualityUpdates, 0, 30) &&
            ValidInteger(snapshot.ConfigureDeadlineGracePeriod, 0, 7) &&
            ValidSwitch(snapshot.ConfigureDeadlineNoAutoRebootForQualityUpdates) &&
            ValidSwitch(snapshot.SetComplianceDeadlineForFeatureUpdates) &&
            ValidInteger(snapshot.ConfigureDeadlineForFeatureUpdates, 0, 30) &&
            ValidInteger(snapshot.ConfigureDeadlineGracePeriodForFeatureUpdates, 0, 7) &&
            ValidSwitch(snapshot.ConfigureDeadlineNoAutoRebootForFeatureUpdates) &&
            ValidSwitch(snapshot.SetUpdateNotificationLevel) && ValidInteger(snapshot.UpdateNotificationLevel, 0, 2) &&
            ValidSwitch(snapshot.NoUpdateNotificationsDuringActiveHours);
        if (!valid) return Result(snapshot, CheckState.Unknown,
            "Windows Update deadline and notification policy evidence was unavailable or malformed. Recollect before diagnosing restart behavior.");

        bool qualityIncomplete = IncompleteGroup(snapshot.SetComplianceDeadlineForQualityUpdates,
            snapshot.ConfigureDeadlineForQualityUpdates, snapshot.ConfigureDeadlineGracePeriod,
            snapshot.ConfigureDeadlineNoAutoRebootForQualityUpdates);
        bool featureIncomplete = IncompleteGroup(snapshot.SetComplianceDeadlineForFeatureUpdates,
            snapshot.ConfigureDeadlineForFeatureUpdates, snapshot.ConfigureDeadlineGracePeriodForFeatureUpdates,
            snapshot.ConfigureDeadlineNoAutoRebootForFeatureUpdates);
        bool notificationIncomplete = IncompleteNotification(snapshot.SetUpdateNotificationLevel,
            snapshot.UpdateNotificationLevel, snapshot.NoUpdateNotificationsDuringActiveHours);
        var parts = new List<string>();
        if (qualityIncomplete) parts.Add("Quality-update deadline settings are incomplete or remain present while their enable switch is off. Review the controlling policy authority.");
        else if (snapshot.SetComplianceDeadlineForQualityUpdates is 1)
            parts.Add($"Quality updates use a {snapshot.ConfigureDeadlineForQualityUpdates}-day deadline and {snapshot.ConfigureDeadlineGracePeriod}-day grace period. " +
                (snapshot.ConfigureDeadlineNoAutoRebootForQualityUpdates is 1 ? "Windows is configured not to restart automatically until the deadline ends." : "No deadline-specific automatic-restart suppression is configured."));
        if (featureIncomplete) parts.Add("Feature-update deadline settings are incomplete or remain present while their enable switch is off. Review the controlling policy authority.");
        else if (snapshot.SetComplianceDeadlineForFeatureUpdates is 1)
            parts.Add($"Feature updates use a {snapshot.ConfigureDeadlineForFeatureUpdates}-day deadline and {snapshot.ConfigureDeadlineGracePeriodForFeatureUpdates}-day grace period. " +
                (snapshot.ConfigureDeadlineNoAutoRebootForFeatureUpdates is 1 ? "Windows is configured not to restart automatically until the deadline ends." : "No deadline-specific automatic-restart suppression is configured."));
        if (notificationIncomplete) parts.Add("Update notification settings are incomplete or remain present while their enable switch is off. Review the controlling policy authority.");
        else if (snapshot.SetUpdateNotificationLevel is 1)
        {
            parts.Add("Managed update notifications: " + NotificationName((int)snapshot.UpdateNotificationLevel!) + "." +
                (snapshot.NoUpdateNotificationsDuringActiveHours is 1 ? " Notifications are suppressed during active hours." : ""));
        }
        if (parts.Count == 0) parts.Add("No complete managed update deadline or notification selection was found.");
        var state = qualityIncomplete || featureIncomplete || notificationIncomplete ? CheckState.Attention : CheckState.Information;
        return Result(snapshot, state, string.Join(" ", parts));
    }

    private static bool IncompleteGroup(object? enabled, object? deadline, object? grace, object? noRestart)
    {
        bool anySettings = deadline is not null || grace is not null || noRestart is not null;
        return enabled is 1 ? deadline is null || grace is null : anySettings;
    }

    private static bool IncompleteNotification(object? enabled, object? level, object? suppressDuringActiveHours)
    {
        bool anySettings = level is not null || suppressDuringActiveHours is not null;
        return enabled is 1 ? level is null : anySettings;
    }

    private static string NotificationName(int value) => value switch
    {
        0 => "default notifications",
        1 => "restart warnings excluded",
        2 => "all notifications, including restart warnings, disabled",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static WorkflowCheck Result(UpdateRestartPolicySnapshot snapshot, CheckState state, string detail) =>
        new("Update deadlines and notifications", state,
            detail + " Policy presence does not prove that an update is pending or calculate its actual deadline. No policy, registry, update, notification or restart setting changed.",
            Evidence: new(state == CheckState.Unknown ? -1 : 0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions),
                snapshot.CollectionIssue ?? "", false));

    private static bool ValidSwitch(object? value) => value is null or 0 or 1;
    private static bool ValidInteger(object? value, int minimum, int maximum) =>
        value is null || value is int number && number >= minimum && number <= maximum;
}
