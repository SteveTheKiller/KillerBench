using Microsoft.Win32;

namespace KillerBench.Engine;

public sealed record PrivacyRegistryTarget(PrivacyArea Area, bool Machine, string Path, string Name, bool Policy);
public sealed record PrivacyRegistryValue(PrivacyRegistryTarget Target, string State, string? Value, string? Kind);
public sealed record PrivacyServiceTarget(PrivacyArea Area, string Name);
public sealed record PrivacyServiceValue(PrivacyServiceTarget Target, string State, ToolResult Evidence);

public static class PrivacyInspection
{
    private const string UserWindows = @"Software\Microsoft\Windows\CurrentVersion\";
    private const string PolicyWindows = @"SOFTWARE\Policies\Microsoft\Windows\";
    public static IReadOnlyList<PrivacyRegistryTarget> Targets { get; } = Array.AsReadOnly(new PrivacyRegistryTarget[]
    {
        new(PrivacyArea.Location, false, UserWindows + @"CapabilityAccessManager\ConsentStore\location", "Value", false),
        new(PrivacyArea.Location, true, PolicyWindows + "LocationAndSensors", "DisableLocation", true),
        new(PrivacyArea.Location, true, UserWindows + @"CapabilityAccessManager\ConsentStore\location", "Value", false),
        new(PrivacyArea.Location, true, PolicyWindows + "LocationAndSensors", "DisableLocationScripting", true),
        new(PrivacyArea.Location, true, @"SOFTWARE\Policies\Microsoft\FindMyDevice", "AllowFindMyDevice", true),
        new(PrivacyArea.Location, false, UserWindows + "Search", "SearchPlatformContext", false),
        new(PrivacyArea.Sensors, true, PolicyWindows + "LocationAndSensors", "DisableSensors", true),
        new(PrivacyArea.DiagnosticData, true, PolicyWindows + "DataCollection", "AllowTelemetry", true),
        new(PrivacyArea.DiagnosticData, true, UserWindows + @"Policies\DataCollection", "AllowTelemetry", true),
        new(PrivacyArea.DiagnosticData, true, UserWindows + @"Policies\DataCollection", "MaxTelemetryAllowed", true),
        new(PrivacyArea.DiagnosticData, true, @"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", false),
        new(PrivacyArea.DiagnosticData, true, PolicyWindows + "Windows Error Reporting", "Disabled", true),
        new(PrivacyArea.DiagnosticData, true, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", true),
        new(PrivacyArea.DiagnosticData, true, PolicyWindows + "AppCompat", "DisableInventory", true),
        new(PrivacyArea.DiagnosticData, true, PolicyWindows + "AppCompat", "DisableUAR", true),
        new(PrivacyArea.Advertising, false, UserWindows + "AdvertisingInfo", "Enabled", false),
        new(PrivacyArea.Advertising, true, PolicyWindows + "AdvertisingInfo", "DisabledByGroupPolicy", true),
        new(PrivacyArea.Advertising, false, UserWindows + "Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", false),
        new(PrivacyArea.Advertising, false, @"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SystemPaneSuggestionsEnabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SoftLandingEnabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "OemPreInstalledAppsEnabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "PreInstalledAppsEnabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SilentInstalledAppsEnabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-338387Enabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-338388Enabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-338389Enabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-353694Enabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-353696Enabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-338393Enabled", false),
        new(PrivacyArea.Advertising, false, UserWindows + "ContentDeliveryManager", "SubscribedContent-310093Enabled", false),
        new(PrivacyArea.Advertising, true, UserWindows + "Device Metadata", "PreventDeviceMetadataFromNetwork", false),
        new(PrivacyArea.Camera, false, UserWindows + @"CapabilityAccessManager\ConsentStore\webcam", "Value", false),
        new(PrivacyArea.Camera, true, UserWindows + @"CapabilityAccessManager\ConsentStore\webcam", "Value", false),
        new(PrivacyArea.Camera, true, PolicyWindows + "AppPrivacy", "LetAppsAccessCamera", true),
        new(PrivacyArea.Microphone, false, UserWindows + @"CapabilityAccessManager\ConsentStore\microphone", "Value", false),
        new(PrivacyArea.Microphone, true, UserWindows + @"CapabilityAccessManager\ConsentStore\microphone", "Value", false),
        new(PrivacyArea.Microphone, true, PolicyWindows + "AppPrivacy", "LetAppsAccessMicrophone", true),
        new(PrivacyArea.ActivityHistory, true, PolicyWindows + "System", "EnableActivityFeed", true),
        new(PrivacyArea.ActivityHistory, true, PolicyWindows + "System", "PublishUserActivities", true),
        new(PrivacyArea.ActivityHistory, true, PolicyWindows + "System", "UploadUserActivities", true),
        new(PrivacyArea.AppActivity, false, UserWindows + @"Explorer\Advanced", "Start_TrackProgs", false),
        new(PrivacyArea.InputPersonalization, false, @"Software\Microsoft\Input\TIPC", "Enabled", false),
        new(PrivacyArea.InputPersonalization, true, @"SOFTWARE\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization", true),
        new(PrivacyArea.InputPersonalization, true, @"SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", true),
        new(PrivacyArea.InputPersonalization, true, @"SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", true),
        new(PrivacyArea.ClipboardHistory, false, @"Software\Microsoft\Clipboard", "EnableClipboardHistory", false),
        new(PrivacyArea.ClipboardHistory, true, PolicyWindows + "System", "AllowClipboardHistory", true),
        new(PrivacyArea.ClipboardSync, false, @"Software\Microsoft\Clipboard", "EnableClipboardRoaming", false),
        new(PrivacyArea.ClipboardSync, true, PolicyWindows + "System", "AllowCrossDeviceClipboard", true),
        new(PrivacyArea.Feedback, true, PolicyWindows + "DataCollection", "DoNotShowFeedbackNotifications", true),
        new(PrivacyArea.Feedback, false, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", false),
        new(PrivacyArea.Feedback, false, @"Software\Microsoft\Siuf\Rules", "PeriodInNanoSeconds", false),
        new(PrivacyArea.Spotlight, true, PolicyWindows + "CloudContent", "DisableWindowsSpotlightFeatures", true),
        new(PrivacyArea.Spotlight, true, PolicyWindows + "CloudContent", "DisableWindowsConsumerFeatures", true),
        new(PrivacyArea.Spotlight, true, PolicyWindows + "CloudContent", "DisableSoftLanding", true),
        new(PrivacyArea.DeliveryOptimization, true, PolicyWindows + "DeliveryOptimization", "DODownloadMode", true),
        new(PrivacyArea.DeliveryOptimization, false, UserWindows + @"DeliveryOptimization\Settings", "DownloadMode", false),
        new(PrivacyArea.OnlineSpeech, false, @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", false),
        new(PrivacyArea.WebSearch, true, PolicyWindows + "Windows Search", "DisableWebSearch", true),
        new(PrivacyArea.WebSearch, true, PolicyWindows + "Windows Search", "ConnectedSearchUseWeb", true),
        new(PrivacyArea.WebSearch, false, UserWindows + "Search", "BingSearchEnabled", false),
        new(PrivacyArea.WebSearch, false, UserWindows + "Search", "CortanaConsent", false),
        new(PrivacyArea.WebSearch, false, UserWindows + "Search", "CanCortanaSeeBrowserHistory", false),
        new(PrivacyArea.NetworkDiscovery, true, PolicyWindows + "NetworkConnectivityStatusIndicator", "NoActiveProbe", true),
        new(PrivacyArea.NetworkDiscovery, true, @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "EnableMulticast", true),
        new(PrivacyArea.NetworkDiscovery, true, @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters", "NodeType", false),
        new(PrivacyArea.NetworkDiscovery, true, PolicyWindows + @"WCN\UI", "DisableWcnUi", true),
        new(PrivacyArea.NetworkDiscovery, true, @"SOFTWARE\Policies\Microsoft\Peernet", "Disabled", true),
        new(PrivacyArea.NetworkDiscovery, true, PolicyWindows + @"Wireless\GPTWirelessPolicy", "EnableAutoConfig", true)
    });
    public static IReadOnlyList<PrivacyServiceTarget> Services { get; } = Array.AsReadOnly(new PrivacyServiceTarget[]
    {
        new(PrivacyArea.Location, "lfsvc"),
        new(PrivacyArea.DiagnosticData, "DiagTrack"),
        new(PrivacyArea.DiagnosticData, "dmwappushservice"),
        new(PrivacyArea.DiagnosticData, "WerSvc"),
        new(PrivacyArea.DiagnosticData, "PcaSvc"),
        new(PrivacyArea.DeliveryOptimization, "DoSvc"),
        new(PrivacyArea.NetworkDiscovery, "WlanSvc")
    });

    public static IReadOnlyList<PrivacyRegistryValue> Inspect(PrivacyOptions options,
        Func<PrivacyRegistryTarget, PrivacyRegistryValue>? read = null, CancellationToken token = default)
    {
        var selected = options.Plan().Select(c => c.Area).ToHashSet();
        read ??= Read;
        var result = new List<PrivacyRegistryValue>();
        foreach (var target in Targets.Where(t => selected.Contains(t.Area)))
        {
            token.ThrowIfCancellationRequested();
            var value = read(target);
            if (value.Target != target) throw new InvalidOperationException("Privacy evidence target mismatch.");
            result.Add(value);
        }
        token.ThrowIfCancellationRequested();
        return result;
    }

    private static PrivacyRegistryValue Read(PrivacyRegistryTarget target)
    {
        if (!OperatingSystem.IsWindows()) return new(target, "Unavailable", null, null);
        try
        {
            using var root = RegistryKey.OpenBaseKey(target.Machine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser, RegistryView.Registry64);
            using var key = root.OpenSubKey(target.Path, writable: false);
            object? value = key?.GetValue(target.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value is null) return new(target, "Not configured", null, null);
            string? text = value switch { int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture), string s when s.Length <= 256 && !s.Any(char.IsControl) => s, _ => null };
            return new(target, text is null ? "Unsupported value" : "Recorded", text, key!.GetValueKind(target.Name).ToString());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return new(target, "Unavailable", null, null); }
    }

    public static async Task<IReadOnlyList<PrivacyServiceValue>> InspectServicesAsync(PrivacyOptions options,
        Func<PrivacyServiceTarget, CancellationToken, Task<ToolResult>>? query = null,
        CancellationToken token = default)
    {
        var selected = options.Plan().Select(choice => choice.Area).ToHashSet();
        query ??= QueryServiceAsync;
        var values = new List<PrivacyServiceValue>();
        foreach (var target in Services.Where(service => selected.Contains(service.Area)))
        {
            token.ThrowIfCancellationRequested();
            ToolResult evidence;
            try
            {
                var pending = query(target, token);
                evidence = pending is null
                    ? new(-1, "", "The privacy service provider returned no task.", false)
                    : await pending.WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false)
                        ?? new(-1, "", "The privacy service provider returned no evidence.", false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (TimeoutException)
            {
                evidence = new(-1, "", "The privacy service query timed out.", false) { TimedOut = true };
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
                NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
            {
                evidence = new(-1, "", "The privacy service query failed: " + error.Message, false);
            }
            string state = ServiceRecovery.MatchesService(evidence, target.Name) ? ServiceRecovery.State(evidence) switch
            {
                1 => "Stopped",
                4 => "Running",
                7 => "Paused",
                2 or 3 or 5 or 6 => "Transitioning",
                _ => "Unavailable"
            } : "Unavailable";
            values.Add(new(target, state, evidence));
        }
        return values;
    }

    private static Task<ToolResult> QueryServiceAsync(PrivacyServiceTarget target, CancellationToken token) =>
        BoundedTool.RunAsync(new("sc.exe", ["query", target.Name], true), TimeSpan.FromSeconds(15), token: token);

    public static string Format(IReadOnlyList<PrivacyRegistryValue> values,
        IReadOnlyList<PrivacyServiceValue>? services = null) =>
        "PRIVACY REGISTRY SNAPSHOT / READ ONLY\nCurrent-user values belong to the account running KillerBench.\n" +
        string.Join("\n", values.Select(v => $"{v.Target.Area} | {(v.Target.Machine ? "HKLM" : "HKCU")}\\{v.Target.Path}\\{v.Target.Name} | {v.State}: {v.Value ?? "unknown"} {v.Kind}")) +
        (services is null || services.Count == 0 ? "" : "\nPRIVACY SERVICE SNAPSHOT / READ ONLY\n" +
            string.Join("\n", services.Select(service => $"{service.Target.Area} | {service.Target.Name} | {service.State}"))) +
        "\nRegistry and service values do not prove effective permissions or policy ownership. MDM, application exceptions and cache contents are not evaluated. No settings changed.";
}
