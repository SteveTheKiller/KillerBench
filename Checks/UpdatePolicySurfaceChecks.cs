using KillerBench.Engine;

internal static class UpdatePolicySurfaceChecks
{
    internal static void Run()
    {
        var none = UpdatePolicySurfaceDiagnostics.Assess(new(true, [], [], [], 0));
        Require(none.State == CheckState.Information && none.Detail.Contains("does not prove the device is unmanaged") && none.Repair is null,
            "Empty recognized policy surfaces remain descriptive without claiming the device is unmanaged");

        var classic = UpdatePolicySurfaceDiagnostics.Assess(new(true,
            ["WUServer", "TargetReleaseVersion"], ["UseWUServer"], [], 0));
        Require(classic.State == CheckState.Information && classic.Detail.Contains("3 recognized") &&
            classic.Detail.Contains("not the originating domain object") && classic.Repair is null,
            "Classic policy values identify their surface without inventing Group Policy ownership");

        var csp = UpdatePolicySurfaceDiagnostics.Assess(new(true, [], [],
            ["AllowAutoUpdate", "ActiveHoursStart"], 1));
        Require(csp.State == CheckState.Information && csp.Detail.Contains("2 recognized") &&
            csp.Detail.Contains("1 include provider metadata") && csp.Detail.Contains("does not identify the tenant"),
            "Policy CSP values and provider metadata remain privacy-bounded configuration evidence");

        var mixed = UpdatePolicySurfaceDiagnostics.Assess(new(true, ["WUServer"], ["UseWUServer"],
            ["SetPolicyDrivenUpdateSourceForQualityUpdates"], 1));
        Require(mixed.State == CheckState.Information && mixed.Detail.Contains("both the classic policy surface") &&
            mixed.Detail.Contains("require Resultant Set of Policy or MDM review"),
            "Mixed policy surfaces request independent precedence review without declaring a conflict");

        foreach (var snapshot in new[]
        {
            new UpdatePolicySurfaceSnapshot(false, [], [], [], 0),
            new UpdatePolicySurfaceSnapshot(true, ["UnknownValue"], [], [], 0),
            new UpdatePolicySurfaceSnapshot(true, [], ["WUServer"], [], 0),
            new UpdatePolicySurfaceSnapshot(true, [], [], ["UnknownValue"], 0),
            new UpdatePolicySurfaceSnapshot(true, [], [], ["AllowAutoUpdate"], 2),
            new UpdatePolicySurfaceSnapshot(true, ["WUServer", "WUServer"], [], [], 0),
            new UpdatePolicySurfaceSnapshot(true, [], [], [], 0, "Access unavailable")
        })
        {
            var unknown = UpdatePolicySurfaceDiagnostics.Assess(snapshot);
            Require(unknown is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Incomplete, inconsistent or malformed policy-surface evidence remains unknown");
        }
        Console.WriteLine("PASS: classic and Policy CSP update surfaces are identified without retaining values, provider identities or inventing authority.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
