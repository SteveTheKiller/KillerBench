using KillerBench.Engine;

internal static class UpdateExecutionContextChecks
{
    internal static void Run()
    {
        var ready = UpdateExecutionContextDiagnostics.Assess(new(true, true, true, 2));
        Require(ready is { State: CheckState.Information, Repair: null } && ready.Detail.Contains("current KillerBench process", StringComparison.Ordinal),
            "Interactive elevated context is informational and scoped to the current process");

        foreach (var review in new[]
        {
            new UpdateExecutionContextSnapshot(true, false, true, 2),
            new UpdateExecutionContextSnapshot(true, true, false, 2),
            new UpdateExecutionContextSnapshot(true, true, true, 0)
        })
            Require(UpdateExecutionContextDiagnostics.Assess(review) is { State: CheckState.Attention, Repair: null },
                "Standard, noninteractive and session-zero contexts request review without repair");

        foreach (var invalid in new[]
        {
            new UpdateExecutionContextSnapshot(false, false, false, -1),
            new UpdateExecutionContextSnapshot(true, true, true, -1),
            new UpdateExecutionContextSnapshot(true, true, true, 2, "Unavailable")
        })
        {
            var check = UpdateExecutionContextDiagnostics.Assess(invalid);
            Require(check is { State: CheckState.Unknown, Repair: null } && check.Evidence is not null,
                "Incomplete execution context remains unknown with evidence");
        }
        Console.WriteLine("PASS: update execution context retains elevation and session evidence without account identifiers or changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
