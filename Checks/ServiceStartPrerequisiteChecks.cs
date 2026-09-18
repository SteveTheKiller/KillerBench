using KillerBench.Engine;

internal static class ServiceStartPrerequisiteChecks
{
    internal static void Run()
    {
        var raw = new ToolResult(0, "SERVICE_NAME: Audiosrv\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : 0 (0x0)", "", false);
        var eligible = ServiceRecovery.AssessGraph(
            ServiceRecovery.Interpret(RepairKind.WindowsAudio, raw, 2),
            new ServiceDependencyGraphResult([], true) { TargetStartup = 2 });
        var ready = new ServiceStartRegistrationEvidence("Audiosrv", ServiceStartRegistrationStatus.Ready, true, "svchost.exe", "AudioSrv.dll");
        Require(ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible, ready) &&
            ServiceStartPrerequisite.Assess(RepairKind.WindowsAudio, eligible, ready) == eligible,
            "Exact present executable and optional DLL evidence preserves guarded start eligibility");

        foreach (var blocked in new[]
        {
            ready with { Status = ServiceStartRegistrationStatus.MissingExecutable },
            ready with { Status = ServiceStartRegistrationStatus.MissingServiceDll },
            ready with { Status = ServiceStartRegistrationStatus.UnsupportedLocation },
            ready with { Status = ServiceStartRegistrationStatus.InvalidRegistration },
            ready with { Status = ServiceStartRegistrationStatus.Unavailable, Complete = false },
            ready with { ServiceName = "Spooler" },
            ready with { ExecutableFile = @"C:\Windows\System32\svchost.exe" },
            ready with { ServiceDllFile = "not-a-library.exe" }
        })
            Require(!ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible, blocked) &&
                ServiceStartPrerequisite.Assess(RepairKind.WindowsAudio, eligible, blocked).Repair is null,
                "Missing, unsupported, unavailable, mismatched and malformed executable evidence blocks a service start");

        var unavailable = ServiceStartPrerequisite.Assess(RepairKind.WindowsAudio, eligible,
            ready with { Status = ServiceStartRegistrationStatus.Unavailable, Complete = false });
        Require(unavailable.State == CheckState.Unknown && unavailable.Detail.Contains("immediately before start", StringComparison.Ordinal),
            "Incomplete executable evidence remains unknown with fresh-preflight guidance");
        var missing = ServiceStartPrerequisite.Assess(RepairKind.WindowsAudio, eligible,
            ready with { Status = ServiceStartRegistrationStatus.MissingExecutable });
        Require(missing.State == CheckState.Attention && missing.Detail.Contains("owning Windows component", StringComparison.Ordinal) &&
            !missing.Detail.Contains(@"C:\", StringComparison.Ordinal),
            "Missing executable guidance preserves privacy and never proposes an untrusted replacement");
        Require(ServiceStartPrerequisite.Assess(RepairKind.PrintSpooler, eligible, ready).State == CheckState.Unknown,
            "A different repair identity cannot reuse executable evidence");
        Console.WriteLine("PASS: automatic service starts require fresh exact local executable registration evidence (fixtures only).");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
