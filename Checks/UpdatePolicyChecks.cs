using KillerBench.Engine;

internal static class UpdatePolicyChecks
{
    internal static void Run()
    {
        Require(UpdatePolicyDiagnostics.Assess(new(true)) is { State: CheckState.Information, Repair: null },
            "Unconfigured update policy remains informational");
        Require(UpdatePolicyDiagnostics.Assess(new(true, "https://updates.example.test", "https://status.example.test", 1)) is
            { State: CheckState.Information, Repair: null }, "Complete internal update policy is preserved");
        Require(UpdatePolicyDiagnostics.Assess(new(true, "https://updates.example.test", null, 1)).State == CheckState.Attention,
            "Incomplete internal update source requests policy review");
        Require(UpdatePolicyDiagnostics.Assess(new(true, DisableWindowsUpdateAccess: 1)).State == CheckState.Attention &&
            UpdatePolicyDiagnostics.Assess(new(true, NoAutoUpdate: 1)).State == CheckState.Attention,
            "Managed update restrictions request review without repair");
        var splitSources = new UpdatePolicySnapshot(true,
            WUServer: "https://updates.example.test", WUStatusServer: "https://status.example.test", UseWUServer: 1,
            UseUpdateClassPolicySource: 1,
            SetPolicyDrivenUpdateSourceForQualityUpdates: 1,
            SetPolicyDrivenUpdateSourceForFeatureUpdates: 0,
            SetPolicyDrivenUpdateSourceForDriverUpdates: 0,
            SetPolicyDrivenUpdateSourceForOtherUpdates: 1);
        var splitCheck = UpdatePolicyDiagnostics.Assess(splitSources);
        Require(splitCheck.State == CheckState.Information && splitCheck.Detail.Contains("Quality: WSUS") &&
            splitCheck.Detail.Contains("feature: Windows Update") && splitCheck.Repair is null,
            "Complete class-specific routing identifies each managed source without offering repair");
        Require(UpdatePolicyDiagnostics.Assess(splitSources with
        {
            SetPolicyDrivenUpdateSourceForOtherUpdates = null
        }).State == CheckState.Attention, "Incomplete class-specific routing requests policy review");
        Require(UpdatePolicyDiagnostics.Assess(new(true,
            SetPolicyDrivenUpdateSourceForQualityUpdates: 0)).State == CheckState.Attention,
            "Class source values without the policy enable switch request review");
        Require(UpdatePolicyDiagnostics.Assess(splitSources with
        {
            WUServer = null,
            WUStatusServer = null,
            UseWUServer = 0
        }).State == CheckState.Attention, "WSUS class routing requires a complete enabled internal source");
        Require(UpdatePolicyDiagnostics.Assess(splitSources with
        {
            DoNotConnectToWindowsUpdateInternetLocations = 1
        }).State == CheckState.Attention, "Internet blocking conflicts with a class routed to Windows Update");
        var timed = UpdatePolicyDiagnostics.Assess(new(true, AUOptions: 4, SetActiveHours: 1,
            ActiveHoursStart: 8, ActiveHoursEnd: 18, ActiveHoursMaxRange: 12,
            ScheduledInstallDay: 0, ScheduledInstallTime: 3, NoAutoRebootWithLoggedOnUsers: 1));
        Require(timed.State == CheckState.Information && timed.Detail.Contains("08:00 to 18:00 (10 hours)") &&
            timed.Detail.Contains("day value 0 at 03:00") && timed.Detail.Contains("suppressed while a user is signed in"),
            "Managed active hours and scheduled installation behavior are reported");
        Require(UpdatePolicyDiagnostics.Assess(new(true, SetActiveHours: 1, ActiveHoursStart: 8)).State == CheckState.Attention,
            "Incomplete active hours request policy review");
        Require(UpdatePolicyDiagnostics.Assess(new(true, SetActiveHours: 1, ActiveHoursStart: 8,
            ActiveHoursEnd: 4)).State == CheckState.Attention,
            "Overlong active hours request policy review");
        Require(UpdatePolicyDiagnostics.Assess(new(true, SetActiveHours: 1, ActiveHoursStart: 8,
            ActiveHoursEnd: 18, ActiveHoursMaxRange: 8)).State == CheckState.Attention,
            "Active hours exceeding the managed maximum request policy review");
        foreach (var invalid in new[]
        {
            new UpdatePolicySnapshot(false),
            new UpdatePolicySnapshot(true, UseWUServer: "1"),
            new UpdatePolicySnapshot(true, WUServer: "https://updates.example.test\ninvalid"),
            new UpdatePolicySnapshot(true, AUOptions: 6),
            new UpdatePolicySnapshot(true, SetPolicyDrivenUpdateSourceForDriverUpdates: 2),
            new UpdatePolicySnapshot(true, ActiveHoursStart: 24),
            new UpdatePolicySnapshot(true, ScheduledInstallDay: 8),
            new UpdatePolicySnapshot(true, CollectionIssue: "Access unavailable")
        })
        {
            var check = UpdatePolicyDiagnostics.Assess(invalid);
            Require(check is { State: CheckState.Unknown, Repair: null } && check.Evidence is not null,
                "Incomplete or malformed update policy remains unknown with evidence");
        }
        Console.WriteLine("PASS: managed Windows Update source, class-routing, timing, conflict, malformed-evidence and read-only outcomes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
