using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record UpdatePolicySurfaceSnapshot(bool Complete, IReadOnlyList<string> ClassicPolicyNames,
    IReadOnlyList<string> ClassicAutomaticUpdateNames, IReadOnlyList<string> PolicyCspNames,
    int PolicyCspProviderMetadataCount, string? CollectionIssue = null);

public static class UpdatePolicySurfaceDiagnostics
{
    private const int MaximumNames = 64;
    private static readonly HashSet<string> ClassicNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "WUServer", "WUStatusServer", "DoNotConnectToWindowsUpdateInternetLocations", "DisableWindowsUpdateAccess",
        "UseUpdateClassPolicySource", "SetPolicyDrivenUpdateSourceForQualityUpdates",
        "SetPolicyDrivenUpdateSourceForFeatureUpdates", "SetPolicyDrivenUpdateSourceForDriverUpdates",
        "SetPolicyDrivenUpdateSourceForOtherUpdates", "SetActiveHours", "ActiveHoursStart", "ActiveHoursEnd",
        "ActiveHoursMaxRange", "TargetReleaseVersion", "ProductVersion", "TargetReleaseVersionInfo",
        "DisableWUfBSafeguards", "DeferFeatureUpdates", "DeferFeatureUpdatesPeriodInDays", "PauseFeatureUpdatesStartTime",
        "SetComplianceDeadlineForQU", "ConfigureDeadlineForQualityUpdates", "ConfigureDeadlineGracePeriod",
        "ConfigureDeadlineNoAutoRebootForQualityUpdates", "SetComplianceDeadlineForFU",
        "ConfigureDeadlineForFeatureUpdates", "ConfigureDeadlineGracePeriodForFeatureUpdates",
        "ConfigureDeadlineNoAutoRebootForFeatureUpdates", "SetUpdateNotificationLevel", "UpdateNotificationLevel",
        "NoUpdateNotificationsDuringActiveHours"
    };
    private static readonly HashSet<string> AutomaticUpdateNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "UseWUServer", "NoAutoUpdate", "AUOptions", "ScheduledInstallDay", "ScheduledInstallTime",
        "NoAutoRebootWithLoggedOnUsers"
    };
    private static readonly HashSet<string> PolicyCspNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AllowAutoUpdate", "ActiveHoursStart", "ActiveHoursEnd", "ActiveHoursMaxRange", "ScheduledInstallDay",
        "ScheduledInstallTime", "NoAutoRebootWithLoggedOnUsers", "SetPolicyDrivenUpdateSourceForQualityUpdates",
        "SetPolicyDrivenUpdateSourceForFeatureUpdates", "SetPolicyDrivenUpdateSourceForDriverUpdates",
        "SetPolicyDrivenUpdateSourceForOtherUpdates", "TargetReleaseVersion", "ProductVersion",
        "TargetReleaseVersionInfo", "DeferFeatureUpdatesPeriodInDays", "PauseFeatureUpdatesStartTime",
        "DisableWUfBSafeguards", "ConfigureDeadlineForQualityUpdates", "ConfigureDeadlineGracePeriod",
        "ConfigureDeadlineNoAutoRebootForQualityUpdates", "ConfigureDeadlineForFeatureUpdates",
        "ConfigureDeadlineGracePeriodForFeatureUpdates", "ConfigureDeadlineNoAutoRebootForFeatureUpdates",
        "UpdateNotificationLevel", "NoUpdateNotificationsDuringActiveHours"
    };

    public static WorkflowCheck Check() => Assess(Read());

    public static UpdatePolicySurfaceSnapshot Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update policy-surface collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var classic = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
            using var automatic = classic?.OpenSubKey("AU");
            using var csp = root.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\Update");
            string[] classicNames = RetainedNames(classic, ClassicNames);
            string[] automaticNames = RetainedNames(automatic, AutomaticUpdateNames);
            string[] cspNames = RetainedNames(csp, PolicyCspNames);
            int providerMetadata = csp?.GetValueNames().Count(name => name.EndsWith("_ProviderSet", StringComparison.OrdinalIgnoreCase) &&
                PolicyCspNames.Contains(name[..^"_ProviderSet".Length])) ?? 0;
            bool complete = classicNames.Length <= MaximumNames && automaticNames.Length <= MaximumNames &&
                cspNames.Length <= MaximumNames && providerMetadata <= MaximumNames;
            return new(complete, classicNames.Take(MaximumNames).ToArray(), automaticNames.Take(MaximumNames).ToArray(),
                cspNames.Take(MaximumNames).ToArray(), Math.Min(providerMetadata, MaximumNames));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, [], [], [], 0, error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(UpdatePolicySurfaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool valid = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) &&
            ValidNames(snapshot.ClassicPolicyNames, ClassicNames) &&
            ValidNames(snapshot.ClassicAutomaticUpdateNames, AutomaticUpdateNames) &&
            ValidNames(snapshot.PolicyCspNames, PolicyCspNames) &&
            snapshot.PolicyCspProviderMetadataCount >= 0 && snapshot.PolicyCspProviderMetadataCount <= MaximumNames &&
            snapshot.PolicyCspProviderMetadataCount <= snapshot.PolicyCspNames.Count;
        if (!valid) return Result(snapshot, CheckState.Unknown,
            "Windows Update policy-surface evidence was unavailable, excessive or malformed. Recollect before reasoning about management authority.");

        int classicCount = snapshot.ClassicPolicyNames.Count + snapshot.ClassicAutomaticUpdateNames.Count;
        int cspCount = snapshot.PolicyCspNames.Count;
        string detail = classicCount == 0 && cspCount == 0
            ? "No recognized Windows Update values were found on the classic policy or current Policy CSP surfaces. This does not prove the device is unmanaged."
            : classicCount > 0 && cspCount > 0
                ? $"Recognized Windows Update values appear on both the classic policy surface ({classicCount}) and current Policy CSP surface ({cspCount}). {snapshot.PolicyCspProviderMetadataCount} CSP values include provider metadata. Effective precedence and the controlling service still require Resultant Set of Policy or MDM review."
                : classicCount > 0
                    ? $"{classicCount} recognized Windows Update values appear on the classic policy surface. This identifies a configuration surface, not the originating domain object, local policy or current winner."
                    : $"{cspCount} recognized Windows Update values appear on the current Policy CSP surface. {snapshot.PolicyCspProviderMetadataCount} include provider metadata. This does not identify the tenant, enrollment or current winning authority.";
        return Result(snapshot, CheckState.Information, detail);
    }

    private static WorkflowCheck Result(UpdatePolicySurfaceSnapshot snapshot, CheckState state, string detail) =>
        new("Windows Update policy surfaces", state,
            detail + " Values and provider identities were not retained. No policy, registry, service, update source or scan setting changed.",
            Evidence: new(state == CheckState.Unknown ? -1 : 0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions),
                snapshot.CollectionIssue ?? "", false));

    [SupportedOSPlatform("windows")]
    private static string[] RetainedNames(RegistryKey? key, HashSet<string> allowlist) => key?.GetValueNames()
        .Where(allowlist.Contains).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];

    private static bool ValidNames(IReadOnlyList<string>? names, HashSet<string> allowlist) => names is not null &&
        names.Count <= MaximumNames && names.All(name => !string.IsNullOrWhiteSpace(name) && allowlist.Contains(name)) &&
        names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count;
}
