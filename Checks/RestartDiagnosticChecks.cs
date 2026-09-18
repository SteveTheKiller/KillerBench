using KillerBench.Engine;

internal static class RestartDiagnosticChecks
{
    internal static void Run()
    {
        var clear = new RestartSnapshot(true, false, false, false, false, false, "PC-1", "PC-1");
        var clearCheck = RestartDiagnostics.Assess(clear);
        Require(clearCheck is { State: CheckState.Clear, Repair: null, Evidence: not null },
            "Complete clear restart evidence remains non-mutating");
        Require(RestartDiagnostics.Interpret(clearCheck.Evidence!) is { State: CheckState.Clear, Repair: null },
            "Serialized clear restart evidence is independently reinterpreted");
        foreach (int index in Enumerable.Range(0, 5))
        {
            object?[] indicators = [false, false, false, false, false];
            indicators[index] = true;
            var pending = new RestartSnapshot(true, indicators[0], indicators[1], indicators[2], indicators[3], indicators[4], "PC-1", "PC-1");
            var pendingCheck = RestartDiagnostics.Assess(pending);
            Require(pendingCheck is { State: CheckState.Attention, Repair: null },
                "Each supported restart source independently requests a restart");
            Require(RestartDiagnostics.Interpret(pendingCheck.Evidence!) is { State: CheckState.Attention, Repair: null },
                "Serialized pending restart evidence preserves each exact source");
        }
        foreach (var invalid in new[]
        {
            clear with { Complete = false },
            clear with { ComponentServicing = null },
            clear with { PendingFileRenames = "True" },
            clear with { ActiveComputerName = null },
            clear with { ConfiguredComputerName = "INVALID NAME" },
            clear with { CollectionIssue = "Access unavailable" }
        })
        {
            Require(RestartDiagnostics.Assess(invalid) is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Incomplete or malformed restart evidence remains unknown");
        }
        foreach (var malformed in new[]
        {
            new ToolResult(0, "{}", "", false),
            new ToolResult(0, "{\"complete\":true,\"complete\":false}", "", false),
            new ToolResult(0, "{\"complete\":true,\"componentServicing\":\"false\",\"windowsUpdate\":false,\"pendingFileRenames\":false,\"computerRename\":false,\"updateExecutable\":false,\"activeComputerName\":\"PC-1\",\"configuredComputerName\":\"PC-1\",\"collectionIssue\":null}", "", false),
            new ToolResult(0, "{}", "provider error", false),
            new ToolResult(0, "{}", "", false) { Truncated = true }
        })
        {
            Require(RestartDiagnostics.Interpret(malformed) is { State: CheckState.Unknown, Repair: null },
                "Malformed, conflicting or incomplete raw restart evidence remains unknown");
        }
        Console.WriteLine("PASS: restart source classification, strict raw-evidence interpretation and non-mutating outcomes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
