using KillerBench.Engine;

internal static class DiskCleanupTaskChecks
{
    internal static void Run()
    {
        const string clear = "{\"found\":true,\"ambiguous\":false,\"path\":\"\\\\Microsoft\\\\Windows\\\\DiskCleanup\\\\\",\"name\":\"SilentCleanup\",\"state\":\"Ready\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-14T01:02:03.0000000\"}";
        var result = new ToolResult(0, clear, "", false);
        var check = DiskCleanupTaskDiagnostics.Interpret(result);
        Require(check.State == CheckState.Information && check.Evidence == result && check.Detail.Contains("0x00000000") &&
            check.Detail.Contains("does not prove cleanup ran"), "Exact successful SilentCleanup history remains read-only information");
        var componentFailure = DiskCleanupTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147746132") });
        Require(componentFailure.State == CheckState.Attention && componentFailure.Detail.Contains("failure-class result"),
            "Enabled SilentCleanup failure-class results request review without inventing a cause");
        var explained = DiskCleanupTaskDiagnostics.Interpret(
            result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147946720") },
            code => new(code, "The operator or administrator has refused the request.", "test", "test"));
        Require(explained.Detail.Contains("Installed Windows message: The operator or administrator has refused the request.") &&
            explained.Detail.Contains("correlation only") && explained.Detail.Contains("0x800710E0"),
            "SilentCleanup failure retains its exact code and bounded Windows explanation");
        var oversized = DiskCleanupTaskDiagnostics.Interpret(
            result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147946720") },
            code => new(code, new string('x', 4097), "test", "test"));
        Require(!oversized.Detail.Contains("Installed Windows message:"),
            "Oversized Windows message text is not attached to task evidence");
        Require(DiskCleanupTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"missedRuns\":0", "\"missedRuns\":2") }).State == CheckState.Attention,
            "Missed SilentCleanup runs request review without selecting cleanup");
        Require(DiskCleanupTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"state\":\"Ready\"", "\"state\":\"Disabled\"") }).State == CheckState.Information,
            "Disabled SilentCleanup remains descriptive because it can be intentional");
        Require(DiskCleanupTaskDiagnostics.Interpret(result with { Output = "{\"found\":false}" }).State == CheckState.Information,
            "Missing SilentCleanup remains descriptive across Windows variants");
        Require(DiskCleanupTaskDiagnostics.Interpret(result with { Output = "{\"found\":true,\"ambiguous\":true}" }).State == CheckState.Unknown,
            "Ambiguous exact SilentCleanup identity remains unknown");
        string unavailable = clear.Replace("\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-14T01:02:03.0000000\"",
            "\"runtimeAvailable\":false,\"lastTaskResult\":null,\"missedRuns\":null,\"lastRunLocalTime\":null");
        Require(DiskCleanupTaskDiagnostics.Interpret(result with { Output = unavailable }).State == CheckState.Unknown,
            "Unavailable SilentCleanup runtime does not invent an outcome");
        foreach (var invalid in new[]
        {
            result with { ExitCode = 5 }, result with { TimedOut = true }, result with { Truncated = true }, result with { Error = "failed" },
            result with { Output = "{" }, result with { Output = clear.Replace("\"found\":true", "\"found\":true,\"found\":false") },
            result with { Output = clear.Replace("SilentCleanup", "Other") }, result with { Output = clear.Replace("DiskCleanup", "Other") },
            result with { Output = clear.Replace("\"state\":\"Ready\"", "\"state\":\"Broken\"") },
            result with { Output = clear.Replace("\"actionCount\":1", "\"actionCount\":33") },
            result with { Output = clear.Replace("\"runtimeAvailable\":true", "\"runtimeAvailable\":false") }
        })
            Require(DiskCleanupTaskDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed or inconsistent SilentCleanup evidence remains unknown");
        try { DiskCleanupTaskDiagnostics.Interpret(result, new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        var step = DiskCleanupTaskDiagnostics.CreateReadStep();
        Require(step.Verification && step.Tool == PrintRecovery.Tool && step.Arguments.Contains("-NonInteractive") &&
            step.Arguments[^1].Contains("Get-ScheduledTask") && step.Arguments[^1].Contains("SilentCleanup") &&
            !step.Arguments[^1].Contains("Start-ScheduledTask") && !step.Arguments[^1].Contains("Remove-Item"),
            "Native plan reads only the exact built-in task and contains no execution or removal command");
        RequireThrows(() => ExactScheduledTaskDiagnostics.CreateReadStep("invalid", "task"), "Shared reader rejects an unrooted task path");
        RequireThrows(() => ExactScheduledTaskDiagnostics.CreateReadStep("\\Valid\\", "bad\nname"), "Shared reader rejects control characters");
        Console.WriteLine("PASS: reusable exact-task reader and SilentCleanup state remain bounded and read only; no task or file changed.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void RequireThrows(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception(message);
    }
}
