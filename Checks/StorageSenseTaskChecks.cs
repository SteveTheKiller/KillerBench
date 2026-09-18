using KillerBench.Engine;

internal static class StorageSenseTaskChecks
{
    internal static void Run()
    {
        const string clear = "{\"found\":true,\"ambiguous\":false,\"path\":\"\\\\Microsoft\\\\Windows\\\\DiskFootprint\\\\\",\"name\":\"StorageSense\",\"state\":\"Ready\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-14T01:02:03.0000000\"}";
        var result = new ToolResult(0, clear, "", false);
        var check = StorageSenseTaskDiagnostics.Interpret(result);
        Require(check.State == CheckState.Information && check.Evidence == result && check.Detail.Contains("0x00000000") &&
            check.Detail.Contains("does not prove cleanup ran"), "Exact successful Storage Sense task history remains read-only information");
        Require(StorageSenseTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147750671") }).State == CheckState.Attention,
            "Scheduler-defined Storage Sense task failure requests review without selecting cleanup");
        var componentFailure = StorageSenseTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147746132") });
        Require(componentFailure.State == CheckState.Attention && componentFailure.Detail.Contains("failure-class result"),
            "Enabled Storage Sense task failure-class results request review without inventing a cause");
        var explained = StorageSenseTaskDiagnostics.Interpret(
            result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147746132") },
            code => new(code, "  Class not registered.\r\n", "test", "test"));
        Require(explained.Detail.Contains("Installed Windows message: Class not registered.") &&
            explained.Detail.Contains("correlation only") && explained.Detail.Contains("0x80040154"),
            "Storage Sense task failure retains its exact code and bounded Windows explanation");
        var mismatched = StorageSenseTaskDiagnostics.Interpret(
            result with { Output = clear.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147746132") },
            _ => new(5, "Access is denied.", "test", "test"));
        Require(!mismatched.Detail.Contains("Installed Windows message:"),
            "A mismatched Windows message result is not attached to task evidence");
        Require(StorageSenseTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"missedRuns\":0", "\"missedRuns\":2") }).State == CheckState.Attention,
            "Missed Storage Sense task runs request review without selecting cleanup");
        Require(StorageSenseTaskDiagnostics.Interpret(result with { Output = clear.Replace("\"state\":\"Ready\"", "\"state\":\"Disabled\"") }).State == CheckState.Information,
            "Disabled Storage Sense task remains descriptive because it can be intentional");
        Require(StorageSenseTaskDiagnostics.Interpret(result with { Output = "{\"found\":false}" }).State == CheckState.Information,
            "Missing Storage Sense task remains descriptive across Windows variants");
        Require(StorageSenseTaskDiagnostics.Interpret(result with { Output = "{\"found\":true,\"ambiguous\":true}" }).State == CheckState.Unknown,
            "Ambiguous exact Storage Sense task identity remains unknown");
        string unavailable = clear.Replace("\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-14T01:02:03.0000000\"",
            "\"runtimeAvailable\":false,\"lastTaskResult\":null,\"missedRuns\":null,\"lastRunLocalTime\":null");
        Require(StorageSenseTaskDiagnostics.Interpret(result with { Output = unavailable }).State == CheckState.Unknown,
            "Unavailable Storage Sense task runtime does not invent an outcome");
        foreach (var invalid in new[]
        {
            result with { ExitCode = 5 }, result with { TimedOut = true }, result with { Truncated = true }, result with { Error = "failed" },
            result with { Output = "{" }, result with { Output = clear.Replace("\"found\":true", "\"found\":true,\"found\":false") },
            result with { Output = clear.Replace("StorageSense", "Other") }, result with { Output = clear.Replace("DiskFootprint", "Other") },
            result with { Output = clear.Replace("\"state\":\"Ready\"", "\"state\":\"Broken\"") },
            result with { Output = clear.Replace("\"actionCount\":1", "\"actionCount\":33") },
            result with { Output = clear.Replace("\"runtimeAvailable\":true", "\"runtimeAvailable\":false") }
        })
            Require(StorageSenseTaskDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed or inconsistent Storage Sense task evidence remains unknown");
        try { StorageSenseTaskDiagnostics.Interpret(result, new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        var step = StorageSenseTaskDiagnostics.CreateReadStep();
        Require(step.Verification && step.Tool == PrintRecovery.Tool && step.Arguments.Contains("-NonInteractive") &&
            step.Arguments[^1].Contains("Get-ScheduledTask") && step.Arguments[^1].Contains("StorageSense") &&
            !step.Arguments[^1].Contains("Start-ScheduledTask") && !step.Arguments[^1].Contains("Remove-Item"),
            "Native plan reads only the exact built-in task and contains no execution or removal command");
        Console.WriteLine("PASS: exact StorageSense task state and runtime evidence remain bounded and read only; no task or file changed.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
