using Microsoft.Win32;
using System.Globalization;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record FeatureUpdatePolicySnapshot(bool Complete,
    object? TargetReleaseVersion = null, object? ProductVersion = null, object? TargetReleaseVersionInfo = null,
    object? DisableWUfBSafeguards = null, object? DeferFeatureUpdates = null,
    object? DeferFeatureUpdatesPeriodInDays = null, object? PauseFeatureUpdatesStartTime = null,
    string? CollectionIssue = null);

public static class FeatureUpdatePolicyDiagnostics
{
    public static WorkflowCheck Check() => Assess(Read());

    public static FeatureUpdatePolicySnapshot Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Feature update policy collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
            return new(true, policy?.GetValue("TargetReleaseVersion"), policy?.GetValue("ProductVersion"),
                policy?.GetValue("TargetReleaseVersionInfo"), policy?.GetValue("DisableWUfBSafeguards"),
                policy?.GetValue("DeferFeatureUpdates"), policy?.GetValue("DeferFeatureUpdatesPeriodInDays"),
                policy?.GetValue("PauseFeatureUpdatesStartTime"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        { return new(false, CollectionIssue: error.GetType().Name); }
    }

    public static WorkflowCheck Assess(FeatureUpdatePolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool valid = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) &&
            ValidSwitch(snapshot.TargetReleaseVersion) && ValidText(snapshot.ProductVersion, 64) &&
            ValidText(snapshot.TargetReleaseVersionInfo, 64) && ValidSwitch(snapshot.DisableWUfBSafeguards) &&
            ValidSwitch(snapshot.DeferFeatureUpdates) && ValidInteger(snapshot.DeferFeatureUpdatesPeriodInDays, 0, 365) &&
            ValidPauseDate(snapshot.PauseFeatureUpdatesStartTime);
        if (!valid) return Result(snapshot, CheckState.Unknown,
            "Feature update policy evidence was unavailable or malformed. Recollect before diagnosing feature update targeting or safeguards.");

        bool targetEnabled = snapshot.TargetReleaseVersion is 1;
        bool hasProduct = snapshot.ProductVersion is string;
        bool hasTarget = snapshot.TargetReleaseVersionInfo is string;
        bool targetingConflict = targetEnabled && (!hasProduct || !hasTarget) || !targetEnabled && (hasProduct || hasTarget);
        bool deferralEnabled = snapshot.DeferFeatureUpdates is 1;
        bool hasDeferral = snapshot.DeferFeatureUpdatesPeriodInDays is int;
        bool deferralConflict = deferralEnabled != hasDeferral;
        bool safeguardsDisabled = snapshot.DisableWUfBSafeguards is 1;
        var state = targetingConflict || deferralConflict || safeguardsDisabled ? CheckState.Attention : CheckState.Information;
        var parts = new List<string>();
        if (targetingConflict)
            parts.Add("Managed feature update targeting is incomplete or its values remain present while targeting is disabled. Review the controlling policy authority.");
        else if (targetEnabled)
            parts.Add($"Managed targeting selects {snapshot.ProductVersion} {snapshot.TargetReleaseVersionInfo}. This records policy intent, not offer eligibility or successful installation.");
        else parts.Add("No complete managed target release selection was found.");
        if (deferralConflict)
            parts.Add("Feature update deferral settings are incomplete. Review the controlling policy authority.");
        else if (deferralEnabled) parts.Add($"Feature updates are deferred for {snapshot.DeferFeatureUpdatesPeriodInDays} days by policy.");
        if (snapshot.PauseFeatureUpdatesStartTime is string pause) parts.Add("Feature updates have a managed pause start date of " + pause + ". Current pause expiration and offer state were not inferred.");
        if (safeguardsDisabled)
            parts.Add("Managed policy disables Windows feature update safeguard holds. This can expose the device to a known compatibility issue and requires administrator review.");
        else parts.Add("No policy disabling feature update safeguards was found. This does not prove that a safeguard hold exists or is absent.");
        return Result(snapshot, state, string.Join(" ", parts));
    }

    private static WorkflowCheck Result(FeatureUpdatePolicySnapshot snapshot, CheckState state, string detail) =>
        new("Feature update policy", state, detail + " Managed settings were preserved. No registry, update, safeguard, target release or restart setting changed.",
            Evidence: new(state == CheckState.Unknown ? -1 : 0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), snapshot.CollectionIssue ?? "", false));
    private static bool ValidSwitch(object? value) => value is null or 0 or 1;
    private static bool ValidInteger(object? value, int minimum, int maximum) => value is null || value is int number && number >= minimum && number <= maximum;
    private static bool ValidText(object? value, int maximum) => value is null || value is string text && text.Length is > 0 && text.Length <= maximum && text == text.Trim() && !text.Any(char.IsControl);
    private static bool ValidPauseDate(object? value) => value is null || value is string text &&
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
