using KillerBench.Engine;

internal static class OptimizeDrivesDiagnosticChecks
{
    internal static void Run()
    {
        const string clear = "{\"found\":true,\"ambiguous\":false,\"path\":\"\\\\Microsoft\\\\Windows\\\\Defrag\\\\\",\"name\":\"ScheduledDefrag\",\"state\":\"Ready\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-14T01:02:03.0000000\"}";
        var result = new ToolResult(0, clear, "", false);
        var check = OptimizeDrivesDiagnostics.Interpret(result);
        Require(check.State == CheckState.Information && check.Evidence == result && check.Detail.Contains("0x00000000") &&
            check.Detail.Contains("never authorizes task execution"), "Exact successful task history remains read-only information");
        Require(OptimizeDrivesDiagnostics.Interpret(result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147750671") }).State == CheckState.Attention,
            "Scheduler-defined configuration failure requests review without selecting repair");
        Require(OptimizeDrivesDiagnostics.Interpret(result with { Output = clear.Replace("\"missedRuns\":0", "\"missedRuns\":2") }).State == CheckState.Attention,
            "Missed runs request review without selecting repair");
        Require(OptimizeDrivesDiagnostics.Interpret(result with { Output = clear.Replace("\"state\":\"Ready\"", "\"state\":\"Disabled\"") }).State == CheckState.Information,
            "Disabled scheduled maintenance remains descriptive because it can be intentional");
        Require(OptimizeDrivesDiagnostics.Interpret(result with { Output = "{\"found\":false}" }).State == CheckState.Attention,
            "Missing exact built-in task is visible without offering creation");
        Require(OptimizeDrivesDiagnostics.Interpret(result with { Output = "{\"found\":true,\"ambiguous\":true}" }).State == CheckState.Unknown,
            "Ambiguous exact identity remains unknown");
        string unavailable = clear.Replace("\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-14T01:02:03.0000000\"",
            "\"runtimeAvailable\":false,\"lastTaskResult\":null,\"missedRuns\":null,\"lastRunLocalTime\":null");
        Require(OptimizeDrivesDiagnostics.Interpret(result with { Output = unavailable }).State == CheckState.Unknown,
            "Unavailable runtime evidence does not invent an execution result");
        foreach (var invalid in new[]
        {
            result with { ExitCode = 5 }, result with { TimedOut = true }, result with { Truncated = true }, result with { Error = "failed" },
            result with { Output = "{" }, result with { Output = clear.Replace("\"found\":true", "\"found\":true,\"found\":false") },
            result with { Output = clear.Replace("ScheduledDefrag", "Other") }, result with { Output = clear.Replace("Defrag", "Other") },
            result with { Output = clear.Replace("\"state\":\"Ready\"", "\"state\":\"Broken\"") },
            result with { Output = clear.Replace("\"actionCount\":1", "\"actionCount\":33") },
            result with { Output = clear.Replace("\"runtimeAvailable\":true", "\"runtimeAvailable\":false") }
        })
            Require(OptimizeDrivesDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed or inconsistent task evidence remains unknown");
        try { OptimizeDrivesDiagnostics.Interpret(result, new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        var step = OptimizeDrivesDiagnostics.CreateReadStep();
        Require(step.Verification && step.Tool == PrintRecovery.Tool && step.Arguments.Contains("-NonInteractive") &&
            step.Arguments[^1].Contains("Get-ScheduledTask") && step.Arguments[^1].Contains("ScheduledDefrag") &&
            !step.Arguments[^1].Contains("Start-ScheduledTask") && !step.Arguments[^1].Contains("Optimize-Volume"),
            "Native plan reads only the exact built-in task and contains no execution or optimization command");
        Console.WriteLine("PASS: exact ScheduledDefrag state and runtime evidence remain bounded and read only; no task or drive changed.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
