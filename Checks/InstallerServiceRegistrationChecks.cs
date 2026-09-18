using KillerBench.Engine;

internal static class InstallerServiceRegistrationChecks
{
    internal static void Run()
    {
        var expected = new InstallerServiceRegistrationSnapshot(true, true, true, true, true, true, true, 3, 16, 1);
        var clear = InstallerServiceRegistrationDiagnostics.Assess(expected);
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Evidence is not null &&
            clear.Detail.Contains("Raw executable paths and account values are excluded", StringComparison.Ordinal),
            "Expected Installer service registration remains informational and private");
        Require(InstallerServiceRegistrationDiagnostics.Assess(expected with { ServiceKeyPresent = false }).State == CheckState.Attention,
            "Missing Installer service registration requests review");
        foreach (var changed in new[]
        {
            expected with { ImagePathPresent = false }, expected with { ImageTargetExpected = false },
            expected with { VerificationSwitchExpected = false }, expected with { AccountPresent = false },
            expected with { AccountExpected = false }, expected with { StartMode = 4 }, expected with { ServiceType = 32 },
            expected with { ErrorControl = 0 }, expected with { StartMode = null }, expected with { ServiceType = null },
            expected with { ErrorControl = null }
        })
            Require(InstallerServiceRegistrationDiagnostics.Assess(changed) is { State: CheckState.Attention, Repair: null },
                "Changed or missing Installer service registration value requests review without repair");
        foreach (var invalid in new[]
        {
            expected with { Complete = false }, expected with { StartMode = 5 }, expected with { ServiceType = 0 },
            expected with { ServiceType = 512 }, expected with { ErrorControl = 4 }
        })
            Require(InstallerServiceRegistrationDiagnostics.Assess(invalid) is { State: CheckState.Unknown, Repair: null },
                "Incomplete or invalid Installer service registration remains unknown");
        Require(InstallerServiceRegistrationDiagnostics.Check(() => expected).State == CheckState.Information,
            "Substituted Installer service registration is assessed");
        Require(InstallerServiceRegistrationDiagnostics.Check(() => throw new UnauthorizedAccessException("fixture")) is
            { State: CheckState.Unknown, Evidence.ExitCode: -1, Repair: null }, "Installer service registration access failure remains unknown");
        Console.WriteLine("PASS: Windows Installer service registration image, account and service values remain bounded and read only; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
