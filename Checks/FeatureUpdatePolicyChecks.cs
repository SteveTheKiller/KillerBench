using KillerBench.Engine;

internal static class FeatureUpdatePolicyChecks
{
    internal static void Run()
    {
        Require(FeatureUpdatePolicyDiagnostics.Assess(new(true)) is { State: CheckState.Information, Repair: null },
            "Unconfigured feature update policy remains informational");
        var targeted = FeatureUpdatePolicyDiagnostics.Assess(new(true, 1, "Windows 11", "25H2"));
        Require(targeted.State == CheckState.Information && targeted.Detail.Contains("Windows 11 25H2") && targeted.Repair is null,
            "Complete target release policy is reported without claiming eligibility");
        Require(FeatureUpdatePolicyDiagnostics.Assess(new(true, 1, "Windows 11")).State == CheckState.Attention &&
            FeatureUpdatePolicyDiagnostics.Assess(new(true, 0, "Windows 11", "25H2")).State == CheckState.Attention,
            "Incomplete and disabled-but-retained target settings request policy review");
        var deferred = FeatureUpdatePolicyDiagnostics.Assess(new(true, DeferFeatureUpdates: 1, DeferFeatureUpdatesPeriodInDays: 30,
            PauseFeatureUpdatesStartTime: "2026-09-01"));
        Require(deferred.State == CheckState.Information && deferred.Detail.Contains("30 days") && deferred.Detail.Contains("2026-09-01"),
            "Valid feature deferral and pause settings remain descriptive");
        Require(FeatureUpdatePolicyDiagnostics.Assess(new(true, DisableWUfBSafeguards: 1)).State == CheckState.Attention,
            "Disabled compatibility safeguards request administrator review without repair");
        Require(FeatureUpdatePolicyDiagnostics.Assess(new(true, DeferFeatureUpdates: 1)).State == CheckState.Attention,
            "Incomplete feature deferral requests policy review");
        foreach (var invalid in new[]
        {
            new FeatureUpdatePolicySnapshot(false), new FeatureUpdatePolicySnapshot(true, TargetReleaseVersion: 2),
            new FeatureUpdatePolicySnapshot(true, ProductVersion: "Windows\n11"),
            new FeatureUpdatePolicySnapshot(true, DeferFeatureUpdatesPeriodInDays: 366),
            new FeatureUpdatePolicySnapshot(true, PauseFeatureUpdatesStartTime: "09/01/2026"),
            new FeatureUpdatePolicySnapshot(true, CollectionIssue: "Access unavailable")
        }) Require(FeatureUpdatePolicyDiagnostics.Assess(invalid) is { State: CheckState.Unknown, Repair: null, Evidence: not null },
            "Unavailable and malformed feature update policy stays unknown");
        Console.WriteLine("PASS: feature update targeting, deferral, pause, safeguard and malformed policy evidence remains read only.");
    }

    private static void Require(bool condition, string detail) { if (!condition) throw new Exception(detail); }
}
