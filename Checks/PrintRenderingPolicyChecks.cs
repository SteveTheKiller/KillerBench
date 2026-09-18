using KillerBench.Engine;

internal static class PrintRenderingPolicyChecks
{
    internal static void Run()
    {
        var absent = PrintRenderingPolicyDiagnostics.Assess(new(null, null, null, null, null));
        Require(absent is { State: CheckState.Information, Repair: null } &&
            absent.Detail.Contains("No recognized", StringComparison.Ordinal),
            "Absent rendering policy remains descriptive");

        var isolated = PrintRenderingPolicyDiagnostics.Assess(new(1, 1, 0, 0, 0));
        Require(isolated is { State: CheckState.Information, Repair: null } &&
            isolated.Detail.Contains("Application driver isolation: Enabled", StringComparison.Ordinal) &&
            isolated.Detail.Contains("Spooler driver isolation: Enabled", StringComparison.Ordinal),
            "Enabled isolation and ordinary rendering policy remain informational");

        var exposed = PrintRenderingPolicyDiagnostics.Assess(new(0, 0, 1, 1, 1));
        Require(exposed is { State: CheckState.Attention, Repair: null } &&
            exposed.Detail.Contains("application processes", StringComparison.Ordinal) &&
            exposed.Detail.Contains("Print Spooler service", StringComparison.Ordinal) &&
            exposed.Detail.Contains("software rasterization", StringComparison.OrdinalIgnoreCase),
            "Disabled isolation requests review while rendering choices remain context");

        foreach (var malformed in new[]
        {
            new PrintRenderingPolicySnapshot(2, null, null, null, null),
            new PrintRenderingPolicySnapshot(null, "enabled", null, null, null),
            new PrintRenderingPolicySnapshot(null, null, -1, null, null),
            new PrintRenderingPolicySnapshot(null, null, null, 3L, null),
            new PrintRenderingPolicySnapshot(null, null, null, null, new byte[] { 1 })
        })
            Require(PrintRenderingPolicyDiagnostics.Assess(malformed).State == CheckState.Unknown,
                "Unsupported rendering policy values remain unknown");

        var failed = PrintRenderingPolicyDiagnostics.Check(() => throw new UnauthorizedAccessException("fixture denied"));
        Require(failed is { State: CheckState.Unknown, Repair: null } && failed.Evidence?.ExitCode == -1,
            "Policy collection failures remain unknown and select no repair");

        var collected = PrintRenderingPolicyDiagnostics.Check(() => new(1, 1, 1, 1, 1));
        Require(collected is { State: CheckState.Information, Repair: null } &&
            collected.Evidence?.Output.Contains("forceSoftwareRasterization", StringComparison.Ordinal) == true,
            "Collected policy retains structured local evidence without a repair");

        Console.WriteLine("PASS: print rendering and isolation policy defaults, exposures, configured behavior, malformed evidence and repair isolation.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
