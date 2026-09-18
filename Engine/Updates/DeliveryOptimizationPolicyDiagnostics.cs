using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record DeliveryOptimizationPolicyValue(string Name, int Value);
public sealed record DeliveryOptimizationPolicySnapshot(bool Complete,
    IReadOnlyList<DeliveryOptimizationPolicyValue> ClassicNumericValues,
    IReadOnlyList<string> ClassicSensitiveValueNames,
    IReadOnlyList<DeliveryOptimizationPolicyValue> PolicyCspNumericValues,
    IReadOnlyList<string> PolicyCspSensitiveValueNames,
    int PolicyCspProviderMetadataCount, string? CollectionIssue = null);

public static class DeliveryOptimizationPolicyDiagnostics
{
    private const int MaximumValues = 64;
    private static readonly HashSet<string> NumericNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DODownloadMode", "DOMaxCacheAge", "DOMaxCacheSize", "DOAbsoluteMaxCacheSize", "DOMinFileSizeToCache",
        "DOPercentageMaxForegroundBandwidth", "DOPercentageMaxBackgroundBandwidth",
        "DOMaxForegroundDownloadBandwidth", "DOMaxBackgroundDownloadBandwidth", "DOMinBackgroundQoS",
        "DOMonthlyUploadDataCap", "DOAllowVPNPeerCaching", "DODelayBackgroundDownloadFromHttp",
        "DODelayForegroundDownloadFromHttp", "DOCacheHostSource", "DODelayCacheServerFallbackBackground",
        "DODelayCacheServerFallbackForeground", "DORestrictPeerSelectionBy", "DOGroupIDSource",
        "DOMinBatteryPercentageAllowedToUpload", "DOMinDiskSizeAllowedToPeer", "DOMinRAMAllowedToPeer"
    };
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DOGroupID", "DOCacheHost", "DOModifyCacheDrive"
    };

    public static WorkflowCheck Check() => Assess(Read());

    public static DeliveryOptimizationPolicySnapshot Read()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Delivery Optimization policy collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var classic = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization");
            using var csp = root.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device\DeliveryOptimization");
            var classicValues = ReadSurface(classic);
            var cspValues = ReadSurface(csp);
            int providerMetadata = csp?.GetValueNames().Count(name => name.EndsWith("_ProviderSet", StringComparison.OrdinalIgnoreCase) &&
                IsRecognized(name[..^"_ProviderSet".Length])) ?? 0;
            bool complete = classicValues.Complete && cspValues.Complete && providerMetadata <= MaximumValues;
            return new(complete, classicValues.Numeric, classicValues.Sensitive, cspValues.Numeric, cspValues.Sensitive,
                Math.Min(providerMetadata, MaximumValues));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, [], [], [], [], 0, error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(DeliveryOptimizationPolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool valid = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) &&
            ValidNumeric(snapshot.ClassicNumericValues) && ValidNames(snapshot.ClassicSensitiveValueNames) &&
            ValidNumeric(snapshot.PolicyCspNumericValues) && ValidNames(snapshot.PolicyCspSensitiveValueNames) &&
            snapshot.PolicyCspProviderMetadataCount >= 0 && snapshot.PolicyCspProviderMetadataCount <= MaximumValues &&
            snapshot.PolicyCspProviderMetadataCount <= snapshot.PolicyCspNumericValues.Count + snapshot.PolicyCspSensitiveValueNames.Count;
        if (!valid) return Result(snapshot, CheckState.Unknown,
            "Delivery Optimization policy evidence was unavailable, excessive or malformed. Recollect before reasoning about peer, cache or bandwidth behavior.");

        int classicCount = snapshot.ClassicNumericValues.Count + snapshot.ClassicSensitiveValueNames.Count;
        int cspCount = snapshot.PolicyCspNumericValues.Count + snapshot.PolicyCspSensitiveValueNames.Count;
        bool deprecatedBypass = Modes(snapshot).Any(value => value == 100);
        var parts = new List<string>();
        if (classicCount == 0 && cspCount == 0)
            parts.Add("No recognized Delivery Optimization values were found on the classic policy or current Policy CSP surfaces. This does not prove the device is unmanaged or describe runtime defaults.");
        else if (classicCount > 0 && cspCount > 0)
            parts.Add($"Recognized Delivery Optimization values appear on both the classic policy surface ({classicCount}) and current Policy CSP surface ({cspCount}). {snapshot.PolicyCspProviderMetadataCount} CSP values include provider metadata. Effective precedence and the controlling authority require Resultant Set of Policy or MDM review.");
        else if (classicCount > 0)
            parts.Add($"{classicCount} recognized Delivery Optimization values appear on the classic policy surface. This does not identify the originating domain object, local policy or current winner.");
        else
            parts.Add($"{cspCount} recognized Delivery Optimization values appear on the current Policy CSP surface. {snapshot.PolicyCspProviderMetadataCount} include provider metadata. This does not identify the tenant, enrollment or current winning authority.");
        string modes = ModeSummary(snapshot);
        if (modes.Length > 0) parts.Add(modes);
        int sensitive = snapshot.ClassicSensitiveValueNames.Count + snapshot.PolicyCspSensitiveValueNames.Count;
        if (sensitive > 0)
            parts.Add($"{sensitive} configured group, cache-host or cache-drive value names were retained while their values were excluded.");
        if (deprecatedBypass)
            parts.Add("Download mode 100 bypasses Delivery Optimization and is deprecated on Windows 11. Review the controlling policy authority; no replacement was selected.");
        else if (classicCount + cspCount > 0)
            parts.Add("Configured cache, bandwidth and peer values are context and can be intentional. They do not establish transfer failure or authorize a reset.");
        return Result(snapshot, deprecatedBypass ? CheckState.Attention : CheckState.Information, string.Join(" ", parts));
    }

    private static WorkflowCheck Result(DeliveryOptimizationPolicySnapshot snapshot, CheckState state, string detail) =>
        new("Delivery Optimization policy", state, detail +
            " Managed settings were preserved. No policy, registry, transfer, cache, service, peer, bandwidth or source setting changed. Microsoft reference: https://learn.microsoft.com/windows/deployment/do/waas-delivery-optimization-reference",
            Evidence: new(state == CheckState.Unknown ? -1 : 0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions),
                snapshot.CollectionIssue ?? "", false));

    private static IEnumerable<int> Modes(DeliveryOptimizationPolicySnapshot snapshot) =>
        snapshot.ClassicNumericValues.Concat(snapshot.PolicyCspNumericValues)
            .Where(value => value.Name.Equals("DODownloadMode", StringComparison.OrdinalIgnoreCase)).Select(value => value.Value);
    private static string ModeSummary(DeliveryOptimizationPolicySnapshot snapshot)
    {
        var parts = new List<string>();
        var classic = snapshot.ClassicNumericValues.FirstOrDefault(value => value.Name.Equals("DODownloadMode", StringComparison.OrdinalIgnoreCase));
        var csp = snapshot.PolicyCspNumericValues.FirstOrDefault(value => value.Name.Equals("DODownloadMode", StringComparison.OrdinalIgnoreCase));
        if (classic is not null) parts.Add("Classic download mode: " + ModeName(classic.Value) + ".");
        if (csp is not null) parts.Add("Policy CSP download mode: " + ModeName(csp.Value) + ".");
        return string.Join(" ", parts);
    }
    private static string ModeName(int value) => value switch
    {
        0 => "0 (HTTP only)",
        1 => "1 (LAN)",
        2 => "2 (Group)",
        3 => "3 (Internet)",
        99 => "99 (Simple)",
        100 => "100 (Bypass, deprecated on Windows 11)",
        _ => value + " (unrecognized by this build)"
    };

    [SupportedOSPlatform("windows")]
    private static (bool Complete, IReadOnlyList<DeliveryOptimizationPolicyValue> Numeric, IReadOnlyList<string> Sensitive) ReadSurface(RegistryKey? key)
    {
        if (key is null) return (true, [], []);
        var numeric = new List<DeliveryOptimizationPolicyValue>();
        var sensitive = new List<string>();
        bool complete = true;
        foreach (string name in key.GetValueNames().Where(IsRecognized).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (numeric.Count + sensitive.Count >= MaximumValues) { complete = false; break; }
            if (NumericNames.Contains(name))
            {
                if (key.GetValueKind(name) == RegistryValueKind.DWord && key.GetValue(name) is int value && value >= 0)
                    numeric.Add(new(name, value));
                else complete = false;
            }
            else
            {
                var kind = key.GetValueKind(name);
                object? value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (kind is RegistryValueKind.String or RegistryValueKind.ExpandString && value is string text &&
                    text.Length is > 0 and <= 4096 && !text.Any(char.IsControl)) sensitive.Add(name);
                else complete = false;
            }
        }
        return (complete, numeric.AsReadOnly(), sensitive.AsReadOnly());
    }

    private static bool IsRecognized(string name) => NumericNames.Contains(name) || SensitiveNames.Contains(name);
    private static bool ValidNumeric(IReadOnlyList<DeliveryOptimizationPolicyValue>? values) => values is not null &&
        values.Count <= MaximumValues && values.All(value => !string.IsNullOrWhiteSpace(value.Name) && NumericNames.Contains(value.Name) && value.Value >= 0) &&
        values.Select(value => value.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Count;
    private static bool ValidNames(IReadOnlyList<string>? names) => names is not null && names.Count <= MaximumValues &&
        names.All(name => !string.IsNullOrWhiteSpace(name) && SensitiveNames.Contains(name)) &&
        names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count;
}
