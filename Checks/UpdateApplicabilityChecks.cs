using KillerBench.Engine;

internal static class UpdateApplicabilityChecks
{
    internal static void Run()
    {
        var complete = UpdateApplicabilityDiagnostics.Assess(new(true, "Windows 11 Pro", "Professional", "25H2", "26200", 7171, "Client", "X64"));
        Require(complete is { State: CheckState.Information, Repair: null } && complete.Detail.Contains("26200.7171", StringComparison.Ordinal) &&
            complete.Detail.Contains("does not prove", StringComparison.Ordinal) && complete.Evidence is not null,
            "Complete Windows identity is retained as scoped applicability evidence");

        foreach (var invalid in new[]
        {
            new UpdateApplicabilitySnapshot(false, null, null, null, null, null, null, null),
            new UpdateApplicabilitySnapshot(true, "Windows", "Pro", "25H2", "bad", 1, "Client", "X64"),
            new UpdateApplicabilitySnapshot(true, "Windows", "Pro", "25H2", "26200", -1, "Client", "X64"),
            new UpdateApplicabilitySnapshot(true, "Windows\nInjected", "Pro", "25H2", "26200", 1, "Client", "X64"),
            new UpdateApplicabilitySnapshot(true, "Windows", "Pro", "25H2", "26200", 1, "Client", "X64", "Unavailable")
        })
            Require(UpdateApplicabilityDiagnostics.Assess(invalid) is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Incomplete or malformed applicability evidence remains unknown");
        Console.WriteLine("PASS: update applicability retains bounded Windows identity without claiming a specific update applies.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
