using KillerBench.Engine;

internal static class DiskDiagnosticTaskChecks
{
    internal static void Run()
    {
        const string collector = "{\"found\":true,\"ambiguous\":false,\"path\":\"\\\\Microsoft\\\\Windows\\\\DiskDiagnostic\\\\\",\"name\":\"Microsoft-Windows-DiskDiagnosticDataCollector\",\"state\":\"Ready\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-15T01:02:03.0000000\"}";
        const string resolver = "{\"found\":true,\"ambiguous\":false,\"path\":\"\\\\Microsoft\\\\Windows\\\\DiskDiagnostic\\\\\",\"name\":\"Microsoft-Windows-DiskDiagnosticResolver\",\"state\":\"Disabled\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":267011,\"missedRuns\":0,\"lastRunLocalTime\":null}";
        var result = new ToolResult(0, collector, "", false);
        var clear = DiskDiagnosticTaskDiagnostics.InterpretCollector(result);
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Evidence == result && clear.Detail.Contains("does not prove a disk is healthy"),
            "Successful disk diagnostic collector history remains bounded read-only information");
        var failure = DiskDiagnosticTaskDiagnostics.InterpretCollector(
            result with { Output = collector.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147942405") },
            code => new(code, "Access is denied.", "test", "test"));
        Require(failure.State == CheckState.Attention && failure.Repair is null && failure.Detail.Contains("0x80070005") &&
            failure.Detail.Contains("Installed Windows message: Access is denied.") && failure.Detail.Contains("does not establish a disk fault"),
            "Enabled disk diagnostic failure retains exact code and supporting message without inventing a disk fault");
        var disabled = DiskDiagnosticTaskDiagnostics.InterpretResolver(result with { Output = resolver });
        Require(disabled.State == CheckState.Information && disabled.Detail.Contains("currently disabled") && !disabled.Detail.Contains("Installed Windows message:"),
            "Disabled disk diagnostic resolver history remains descriptive");
        Require(DiskDiagnosticTaskDiagnostics.InterpretCollector(result with { Output = "{\"found\":false}" }).State == CheckState.Information,
            "Missing disk diagnostic task remains descriptive across Windows variants");
        Require(DiskDiagnosticTaskDiagnostics.InterpretCollector(result with { Output = "{\"found\":true,\"ambiguous\":true}" }).State == CheckState.Unknown,
            "Ambiguous disk diagnostic task identity remains unknown");
        foreach (var invalid in new[]
        {
            result with { ExitCode = 5 }, result with { TimedOut = true }, result with { Truncated = true }, result with { Error = "failed" },
            result with { Output = "{" }, result with { Output = collector.Replace("DataCollector", "Other") },
            result with { Output = collector.Replace("DiskDiagnostic", "Other") }, result with { Output = collector.Replace("\"actionCount\":1", "\"actionCount\":33") }
        })
            Require(DiskDiagnosticTaskDiagnostics.InterpretCollector(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed or inconsistent disk diagnostic task evidence remains unknown");
        var steps = new[] { DiskDiagnosticTaskDiagnostics.CreateCollectorReadStep(), DiskDiagnosticTaskDiagnostics.CreateResolverReadStep() };
        Require(steps.All(step => step.Verification && step.Tool == PrintRecovery.Tool && step.Arguments.Contains("-NonInteractive") &&
            step.Arguments[^1].Contains("Get-ScheduledTask") && !step.Arguments[^1].Contains("Start-ScheduledTask") && !step.Arguments[^1].Contains("Set-Disk")),
            "Native plans read only the exact built-in disk diagnostic tasks and contain no execution or disk command");
        Console.WriteLine("PASS: exact disk diagnostic task pair remains bounded and read only; no task or disk changed.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
