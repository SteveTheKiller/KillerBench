using Microsoft.Win32;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdatePolicySnapshot(bool Complete,
    object? WUServer = null, object? WUStatusServer = null, object? UseWUServer = null,
    object? DoNotConnectToWindowsUpdateInternetLocations = null, object? DisableWindowsUpdateAccess = null,
    object? NoAutoUpdate = null, object? AUOptions = null,
    object? UseUpdateClassPolicySource = null,
    object? SetPolicyDrivenUpdateSourceForQualityUpdates = null,
    object? SetPolicyDrivenUpdateSourceForFeatureUpdates = null,
    object? SetPolicyDrivenUpdateSourceForDriverUpdates = null,
    object? SetPolicyDrivenUpdateSourceForOtherUpdates = null,
    object? SetActiveHours = null, object? ActiveHoursStart = null, object? ActiveHoursEnd = null,
    object? ActiveHoursMaxRange = null, object? ScheduledInstallDay = null, object? ScheduledInstallTime = null,
    object? NoAutoRebootWithLoggedOnUsers = null,
    string? CollectionIssue = null);

public static class UpdatePolicyDiagnostics
{
    public static WorkflowCheck Check() => Assess(Read());

    public static UpdatePolicySnapshot Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update policy collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
            using var au = policy?.OpenSubKey("AU");
            return new(true,
                policy?.GetValue("WUServer"), policy?.GetValue("WUStatusServer"), au?.GetValue("UseWUServer"),
                policy?.GetValue("DoNotConnectToWindowsUpdateInternetLocations"), policy?.GetValue("DisableWindowsUpdateAccess"),
                au?.GetValue("NoAutoUpdate"), au?.GetValue("AUOptions"),
                policy?.GetValue("UseUpdateClassPolicySource"),
                policy?.GetValue("SetPolicyDrivenUpdateSourceForQualityUpdates"),
                policy?.GetValue("SetPolicyDrivenUpdateSourceForFeatureUpdates"),
                policy?.GetValue("SetPolicyDrivenUpdateSourceForDriverUpdates"),
                policy?.GetValue("SetPolicyDrivenUpdateSourceForOtherUpdates"),
                policy?.GetValue("SetActiveHours"), policy?.GetValue("ActiveHoursStart"), policy?.GetValue("ActiveHoursEnd"),
                policy?.GetValue("ActiveHoursMaxRange"), au?.GetValue("ScheduledInstallDay"), au?.GetValue("ScheduledInstallTime"),
                au?.GetValue("NoAutoRebootWithLoggedOnUsers"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, CollectionIssue: error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(UpdatePolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool validText = ValidText(snapshot.WUServer) && ValidText(snapshot.WUStatusServer);
        object?[] classSources = [snapshot.SetPolicyDrivenUpdateSourceForQualityUpdates,
            snapshot.SetPolicyDrivenUpdateSourceForFeatureUpdates, snapshot.SetPolicyDrivenUpdateSourceForDriverUpdates,
            snapshot.SetPolicyDrivenUpdateSourceForOtherUpdates];
        bool validSwitches = new[] { snapshot.UseWUServer, snapshot.DoNotConnectToWindowsUpdateInternetLocations,
            snapshot.DisableWindowsUpdateAccess, snapshot.NoAutoUpdate, snapshot.UseUpdateClassPolicySource,
            snapshot.SetActiveHours, snapshot.NoAutoRebootWithLoggedOnUsers }.All(ValidSwitch) &&
            classSources.All(ValidSwitch);
        bool validOptions = snapshot.AUOptions is null or int and >= 2 and <= 5 &&
            ValidOptionalInt(snapshot.ActiveHoursStart, 0, 23) && ValidOptionalInt(snapshot.ActiveHoursEnd, 0, 23) &&
            ValidOptionalInt(snapshot.ActiveHoursMaxRange, 8, 18) && ValidOptionalInt(snapshot.ScheduledInstallDay, 0, 7) &&
            ValidOptionalInt(snapshot.ScheduledInstallTime, 0, 23);
        bool complete = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) && validText && validSwitches && validOptions;
        string detail;
        CheckState state;
        if (!complete)
        {
            state = CheckState.Unknown;
            detail = "Windows Update policy evidence was unavailable or malformed. Recollect before diagnosing update source or access behavior.";
        }
        else
        {
            bool internalSource = snapshot.UseWUServer is 1;
            bool hasServer = snapshot.WUServer is string { Length: > 0 };
            bool hasStatusServer = snapshot.WUStatusServer is string { Length: > 0 };
            bool inconsistentSource = internalSource && (!hasServer || !hasStatusServer) || hasServer != hasStatusServer;
            bool restricted = snapshot.DisableWindowsUpdateAccess is 1 || snapshot.NoAutoUpdate is 1;
            bool classRoutingEnabled = snapshot.UseUpdateClassPolicySource is 1;
            bool anyClassSource = classSources.Any(value => value is not null);
            bool allClassSources = classSources.All(value => value is not null);
            bool incompleteClassRouting = classRoutingEnabled && !allClassSources || anyClassSource && !classRoutingEnabled;
            bool classNeedsInternalSource = classRoutingEnabled && classSources.Any(value => value is 1);
            bool unavailableInternalClassSource = classNeedsInternalSource && (!internalSource || !hasServer || !hasStatusServer);
            bool publicClassSourceBlocked = classRoutingEnabled && classSources.Any(value => value is 0) &&
                snapshot.DoNotConnectToWindowsUpdateInternetLocations is 1;
            bool activeHoursEnabled = snapshot.SetActiveHours is 1;
            bool anyActiveHours = snapshot.ActiveHoursStart is not null || snapshot.ActiveHoursEnd is not null;
            bool incompleteActiveHours = activeHoursEnabled != anyActiveHours || activeHoursEnabled &&
                (snapshot.ActiveHoursStart is null || snapshot.ActiveHoursEnd is null);
            int activeHoursLength = activeHoursEnabled && snapshot.ActiveHoursStart is int start && snapshot.ActiveHoursEnd is int end
                ? (end - start + 24) % 24 : 0;
            bool invalidActiveHoursLength = activeHoursEnabled && (activeHoursLength == 0 || activeHoursLength > 18 ||
                snapshot.ActiveHoursMaxRange is int maximum && activeHoursLength > maximum);
            bool timingConflict = incompleteActiveHours || invalidActiveHoursLength;
            bool sourceConflict = inconsistentSource || incompleteClassRouting || unavailableInternalClassSource || publicClassSourceBlocked;
            state = sourceConflict || restricted ? CheckState.Attention : CheckState.Information;
            if (timingConflict) state = CheckState.Attention;
            detail = inconsistentSource
                ? "Managed update-source settings are incomplete: the internal-source selection and server endpoints do not form a complete pair. Review the controlling policy authority."
                : incompleteClassRouting
                    ? "Update-class source policy is incomplete or is present without its enable switch. Configure all four update classes through the controlling policy authority before relying on class-specific routing."
                    : unavailableInternalClassSource
                        ? "At least one update class selects WSUS, but a complete enabled internal update service was not found. Review the controlling policy authority."
                        : publicClassSourceBlocked
                            ? "At least one update class selects Windows Update while managed policy blocks connections to Windows Update internet locations. Review the controlling policy authority."
                        : incompleteActiveHours
                            ? "Managed active-hours settings are incomplete or are present without their enable switch. Review the controlling policy authority."
                        : invalidActiveHoursLength
                            ? "Managed active hours describe an empty, overlong or maximum-range-conflicting window. Review the controlling policy authority."
                : restricted
                    ? "Managed policy restricts Windows Update access or automatic updating. This can be intentional; review the controlling policy authority before troubleshooting the client."
                    : classRoutingEnabled
                        ? "Managed update-class routing is complete. " + ClassRouteSummary(snapshot) + " Source reachability, authentication and synchronization were not tested."
                    : internalSource
                        ? "Managed policy selects an internal update service. Source reachability, authentication and synchronization were not tested."
                        : "No complete internal update-service selection was found. The configured update agent determines the effective source.";
            detail += " " + TimingSummary(snapshot, activeHoursLength);
        }
        detail += " Managed settings were preserved. No registry, service, update-source or scan setting changed.";
        return new("Update policy", state, detail,
            Evidence: new(complete ? 0 : -1, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), snapshot.CollectionIssue ?? "", false));
    }

    private static bool ValidSwitch(object? value) => value is null or 0 or 1;
    private static bool ValidOptionalInt(object? value, int minimum, int maximum) =>
        value is null || value is int number && number >= minimum && number <= maximum;
    private static bool ValidText(object? value) => value is null || value is string text &&
        text.Length is > 0 and <= 2048 && !text.Any(char.IsControl);

    private static string ClassRouteSummary(UpdatePolicySnapshot snapshot) =>
        "Quality: " + SourceName(snapshot.SetPolicyDrivenUpdateSourceForQualityUpdates) +
        "; feature: " + SourceName(snapshot.SetPolicyDrivenUpdateSourceForFeatureUpdates) +
        "; drivers: " + SourceName(snapshot.SetPolicyDrivenUpdateSourceForDriverUpdates) +
        "; other: " + SourceName(snapshot.SetPolicyDrivenUpdateSourceForOtherUpdates) + ".";

    private static string SourceName(object? value) => value is 1 ? "WSUS" : "Windows Update";

    private static string TimingSummary(UpdatePolicySnapshot snapshot, int activeHoursLength)
    {
        List<string> parts =
        [
            snapshot.NoAutoUpdate is 1 ? "Automatic updating is disabled by policy." : snapshot.AUOptions switch
            {
                2 => "Automatic updates notify before download and installation.",
                3 => "Automatic updates download automatically and notify before installation.",
                4 => "Automatic updates download and use a managed installation schedule.",
                5 => "Local administrators may choose the automatic update mode.",
                _ => "Automatic update timing is not configured here."
            }
        ];
        if (snapshot.SetActiveHours is 1 && snapshot.ActiveHoursStart is int start && snapshot.ActiveHoursEnd is int end)
            parts.Add($"Managed active hours run from {start:00}:00 to {end:00}:00 ({activeHoursLength} hours)." );
        if (snapshot.AUOptions is 4 && snapshot.ScheduledInstallDay is int day && snapshot.ScheduledInstallTime is int hour)
            parts.Add($"Scheduled installation policy uses day value {day} at {hour:00}:00.");
        if (snapshot.NoAutoRebootWithLoggedOnUsers is 1)
            parts.Add("Automatic restart is suppressed while a user is signed in for applicable scheduled installations.");
        return string.Join(" ", parts);
    }
}
