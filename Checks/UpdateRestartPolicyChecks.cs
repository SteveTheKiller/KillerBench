using KillerBench.Engine;

internal static class UpdateRestartPolicyChecks
{
    internal static void Run()
    {
        var absent = UpdateRestartPolicyDiagnostics.Assess(new(true));
        Require(absent.State == CheckState.Information && absent.Detail.Contains("No complete managed") && absent.Repair is null,
            "Absent update deadline and notification policy remains informational");

        var configured = UpdateRestartPolicyDiagnostics.Assess(new(true,
            SetComplianceDeadlineForQualityUpdates: 1, ConfigureDeadlineForQualityUpdates: 7,
            ConfigureDeadlineGracePeriod: 2, ConfigureDeadlineNoAutoRebootForQualityUpdates: 1,
            SetComplianceDeadlineForFeatureUpdates: 1, ConfigureDeadlineForFeatureUpdates: 14,
            ConfigureDeadlineGracePeriodForFeatureUpdates: 3, ConfigureDeadlineNoAutoRebootForFeatureUpdates: 0,
            SetUpdateNotificationLevel: 1, UpdateNotificationLevel: 1, NoUpdateNotificationsDuringActiveHours: 1));
        Require(configured.State == CheckState.Information && configured.Repair is null &&
            configured.Detail.Contains("7-day deadline and 2-day grace") &&
            configured.Detail.Contains("14-day deadline and 3-day grace") &&
            configured.Detail.Contains("restart warnings excluded") &&
            configured.Detail.Contains("suppressed during active hours"),
            "Complete deadline, grace, restart and notification policy is described without selecting a change");

        foreach (var snapshot in new[]
        {
            new UpdateRestartPolicySnapshot(true, SetComplianceDeadlineForQualityUpdates: 1, ConfigureDeadlineForQualityUpdates: 7),
            new UpdateRestartPolicySnapshot(true, ConfigureDeadlineForQualityUpdates: 7, ConfigureDeadlineGracePeriod: 2),
            new UpdateRestartPolicySnapshot(true, SetComplianceDeadlineForFeatureUpdates: 1, ConfigureDeadlineForFeatureUpdates: 14),
            new UpdateRestartPolicySnapshot(true, ConfigureDeadlineForFeatureUpdates: 14, ConfigureDeadlineGracePeriodForFeatureUpdates: 2),
            new UpdateRestartPolicySnapshot(true, SetUpdateNotificationLevel: 1),
            new UpdateRestartPolicySnapshot(true, UpdateNotificationLevel: 2)
        })
        {
            var attention = UpdateRestartPolicyDiagnostics.Assess(snapshot);
            Require(attention.State == CheckState.Attention && attention.Repair is null,
                "Incomplete deadline or notification policy requests authority review without repair");
        }

        foreach (var snapshot in new[]
        {
            new UpdateRestartPolicySnapshot(false),
            new UpdateRestartPolicySnapshot(true, SetComplianceDeadlineForQualityUpdates: 2),
            new UpdateRestartPolicySnapshot(true, ConfigureDeadlineForQualityUpdates: 31),
            new UpdateRestartPolicySnapshot(true, ConfigureDeadlineGracePeriod: 8),
            new UpdateRestartPolicySnapshot(true, ConfigureDeadlineNoAutoRebootForFeatureUpdates: "1"),
            new UpdateRestartPolicySnapshot(true, UpdateNotificationLevel: 3),
            new UpdateRestartPolicySnapshot(true, NoUpdateNotificationsDuringActiveHours: -1),
            new UpdateRestartPolicySnapshot(true, CollectionIssue: "Access unavailable")
        })
        {
            var unknown = UpdateRestartPolicyDiagnostics.Assess(snapshot);
            Require(unknown is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Unavailable, out-of-range or malformed restart-policy evidence remains unknown");
        }
        Console.WriteLine("PASS: update deadlines, grace periods, restart suppression and notification policy remain deterministic and read only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
