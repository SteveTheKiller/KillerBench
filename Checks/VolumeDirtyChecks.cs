using KillerBench.Engine;

internal static class VolumeDirtyChecks
{
    internal static void Run()
    {
        var cleanEvidence = new ToolResult(0, "Volume - C: is NOT Dirty", "", false);
        var dirtyEvidence = new ToolResult(0, "Volume - C: is Dirty", "", false);
        var clean = VolumeDirtyStatus.Interpret("c:\\", cleanEvidence);
        var dirty = VolumeDirtyStatus.Interpret("C:", dirtyEvidence);
        Require(clean is { State: CheckState.Information, Repair: null } && clean.Evidence == cleanEvidence && clean.Detail.Contains("does not establish"), "Clean dirty bit remains a limited observation");
        Require(dirty is { State: CheckState.Attention, Repair: null } && dirty.Evidence == dirtyEvidence && dirty.Detail.Contains("nothing was scheduled"), "Dirty volume requests review without repair");
        Require(VolumeDirtyStatus.Step("d:\\") is { Tool: "fsutil.exe", Verification: true } step && step.Arguments.SequenceEqual(["dirty", "query", "D:"]), "Dirty-bit command is read-only and binds the selected volume");
        foreach (var invalid in new[]
        {
            cleanEvidence with { Output = "Example: Volume - C: is NOT Dirty" },
            cleanEvidence with { Output = "Volume - D: is NOT Dirty" },
            cleanEvidence with { Output = cleanEvidence.Output + "\n" + cleanEvidence.Output },
            cleanEvidence with { Output = cleanEvidence.Output + "\nVolume - C: is Dirty" },
            cleanEvidence with { ExitCode = 1 }, cleanEvidence with { Error = "denied" },
            cleanEvidence with { TimedOut = true }, cleanEvidence with { Truncated = true }
        })
            Require(VolumeDirtyStatus.Interpret("C:", invalid) is { State: CheckState.Unknown, Repair: null }, "Incomplete, mismatched or ambiguous dirty-bit evidence remains unknown");
        Console.WriteLine("PASS: selected-volume dirty-bit identity, ambiguity and incomplete evidence; fixtures only.");
    }

    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
