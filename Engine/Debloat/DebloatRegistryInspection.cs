using Microsoft.Win32;

namespace KillerBench.Engine;

public sealed record DebloatRegistryTarget(DebloatAreas Area, bool Machine, string Path,
    string Name, int ProposedValue, bool Policy);
public sealed record DebloatRegistryValue(DebloatRegistryTarget Target, string State,
    int? CurrentValue, string? Kind);

public static class DebloatRegistryInspection
{
    public static IReadOnlyList<DebloatRegistryTarget> Targets { get; } = Array.AsReadOnly(new DebloatRegistryTarget[]
    {
        new(DebloatAreas.RecallAndAi, true, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1, true),
        new(DebloatAreas.RecallAndAi, true, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1, true),
        new(DebloatAreas.SearchAndSuggestions, false, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, true),
        new(DebloatAreas.SearchAndSuggestions, false, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxPresent", 0, false),
        new(DebloatAreas.ConsumerContent, false, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenEnabled", 0, false),
        new(DebloatAreas.ConsumerContent, false, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 0, false),
        new(DebloatAreas.ConsumerContent, false, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "ContentDeliveryAllowed", 0, false),
        new(DebloatAreas.ConsumerContent, true, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata", "PreventDeviceMetadataFromNetwork", 1, false),
        new(DebloatAreas.TaskbarAndStart, false, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0, false),
        new(DebloatAreas.TaskbarAndStart, false, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0, false),
        new(DebloatAreas.TaskbarAndStart, false, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0, false),
        new(DebloatAreas.TaskbarAndStart, false, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0, false),
        new(DebloatAreas.TaskbarAndStart, false, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, false),
        new(DebloatAreas.TaskbarAndStart, true, @"SOFTWARE\Policies\Microsoft\Windows\Windows Chat", "ChatIcon", 3, true),
        new(DebloatAreas.TaskbarAndStart, true, @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0, true),
        new(DebloatAreas.TaskbarAndStart, true, @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "HideRecentlyAddedApps", 1, true),
        new(DebloatAreas.Notifications, false, @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested", "Enabled", 0, false),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "CopilotPageContext", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeEntraCopilotPageContext", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeSidebarEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "ShowMicrosoftRewards", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeShoppingAssistantEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "PersonalizationReportingEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeFollowEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "ShowRecommendationsEnabled", 0, true),
        new(DebloatAreas.Edge, true, @"SOFTWARE\Policies\Microsoft\Edge", "DiscoverPageContextEnabled", 0, true)
    });

    public static IReadOnlyList<DebloatRegistryValue> Inspect(DebloatOptions options,
        Func<DebloatRegistryTarget, DebloatRegistryValue>? reader = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        reader ??= Read;
        var values = new List<DebloatRegistryValue>();
        foreach (var target in Targets.Where(target => options.Areas.HasFlag(target.Area)))
        {
            token.ThrowIfCancellationRequested();
            DebloatRegistryValue value = reader(target);
            if (value.Target != target) throw new InvalidOperationException("Declutter registry evidence target mismatch.");
            values.Add(value);
        }
        return values.AsReadOnly();
    }

    private static DebloatRegistryValue Read(DebloatRegistryTarget target)
    {
        if (!OperatingSystem.IsWindows()) return new(target, "Unavailable", null, null);
        try
        {
            using var root = RegistryKey.OpenBaseKey(target.Machine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
                RegistryView.Registry64);
            using var key = root.OpenSubKey(target.Path, writable: false);
            object? raw = key?.GetValue(target.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is null) return new(target, "Not configured", null, null);
            return raw is int value
                ? new(target, value == target.ProposedValue ? "Already proposed" : "Different", value,
                    key!.GetValueKind(target.Name).ToString())
                : new(target, "Unsupported value", null, key!.GetValueKind(target.Name).ToString());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return new(target, "Unavailable", null, null); }
    }
}
