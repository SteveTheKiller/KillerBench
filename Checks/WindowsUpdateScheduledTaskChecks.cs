using KillerBench.Engine;

internal static class WindowsUpdateScheduledTaskChecks
{
    internal static void Run()
    {
        const string ready = "{\"found\":true,\"ambiguous\":false,\"path\":\"\\\\Microsoft\\\\Windows\\\\WindowsUpdate\\\\\",\"name\":\"Scheduled Start\",\"state\":\"Ready\",\"actionCount\":1,\"runtimeAvailable\":true,\"lastTaskResult\":0,\"missedRuns\":0,\"lastRunLocalTime\":\"2026-09-16T01:02:03.0000000\"}";
        var result = new ToolResult(0, ready, "", false);
        var clear = WindowsUpdateScheduledTaskDiagnostics.Interpret(result);
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Evidence == result &&
            clear.Detail.Contains("complements live PEEK activity"),
            "Successful Windows Update task history remains read-only supporting evidence");

        var failure = WindowsUpdateScheduledTaskDiagnostics.Interpret(
            result with { Output = ready.Replace("\"lastTaskResult\":0", "\"lastTaskResult\":2147942405") },
            code => new(code, "Access is denied.", "test", "test"));
        Require(failure.State == CheckState.Attention && failure.Repair is null && failure.Detail.Contains("0x80070005") &&
            failure.Detail.Contains("Installed Windows message: Access is denied.") && failure.Detail.Contains("PEEK activity"),
            "Enabled Windows Update task failure retains exact correlation evidence without selecting a repair");
        Require(WindowsUpdateScheduledTaskDiagnostics.Interpret(result with { Output = ready.Replace("\"missedRuns\":0", "\"missedRuns\":2") }).State == CheckState.Attention,
            "Missed Windows Update task runs request review");
        Require(WindowsUpdateScheduledTaskDiagnostics.Interpret(result with { Output = ready.Replace("\"state\":\"Ready\"", "\"state\":\"Disabled\"") }).State == CheckState.Information,
            "Disabled Windows Update task remains descriptive because policy can make it intentional");
        Require(WindowsUpdateScheduledTaskDiagnostics.Interpret(result with { Output = "{\"found\":false}" }).State == CheckState.Information,
            "Missing Windows Update task remains descriptive across Windows variants");
        Require(WindowsUpdateScheduledTaskDiagnostics.Interpret(result with { Output = "{\"found\":true,\"ambiguous\":true}" }).State == CheckState.Unknown,
            "Ambiguous Windows Update task identity remains unknown");
        foreach (var invalid in new[]
        {
            result with { ExitCode = 5 }, result with { TimedOut = true }, result with { Truncated = true }, result with { Error = "failed" },
            result with { Output = "{" }, result with { Output = ready.Replace("Scheduled Start", "Other") },
            result with { Output = ready.Replace("WindowsUpdate", "Other") }, result with { Output = ready.Replace("\"actionCount\":1", "\"actionCount\":33") }
        })
            Require(WindowsUpdateScheduledTaskDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed or inconsistent Windows Update task evidence remains unknown");
        try { WindowsUpdateScheduledTaskDiagnostics.Interpret(result, new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        var step = WindowsUpdateScheduledTaskDiagnostics.CreateReadStep();
        Require(step.Verification && step.Tool == PrintRecovery.Tool && step.Arguments.Contains("-NonInteractive") &&
            step.Arguments[^1].Contains("Get-ScheduledTask") && step.Arguments[^1].Contains("Scheduled Start") &&
            !step.Arguments[^1].Contains("Start-ScheduledTask") && !step.Arguments[^1].Contains("Register-ScheduledTask"),
            "Native plan reads only the exact built-in Windows Update task and contains no execution or recreation command");
        Console.WriteLine("PASS: exact Windows Update task evidence complements PEEK and remains bounded and read only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
