using KillerBench.Engine;

internal static class MemoryDiagnosticResultChecks
{
    internal static void Run()
    {
        string record = Event(1201, 4, 42, "2026-09-14T10:00:00Z", "<EventData><Data Name='CompletionType'>Pass</Data><Data Name='NumBadPages'>0</Data></EventData>");
        var evidence = new ToolResult(0, "<Events>" + record + "</Events>", "", false);
        var information = MemoryDiagnosticResults.Interpret(evidence);
        Require(information is { State: CheckState.Information, Repair: null } && information.Evidence == evidence &&
            information.Detail.Contains("Event 1201 | level 4 | 1 records", StringComparison.Ordinal) &&
            information.Detail.Contains("CompletionType=Pass", StringComparison.Ordinal) &&
            information.Detail.Contains("historical pass does not establish current memory health", StringComparison.Ordinal),
            "Informational memory diagnostic history retains exact event evidence without claiming current health");
        var warning = MemoryDiagnosticResults.Interpret(evidence with { Output = "<Events>" + Event(1202, 3, 43, "2026-09-14T11:00:00Z", "<EventData><Data Name='NumBadPages'>1</Data></EventData>") + "</Events>" });
        Require(warning is { State: CheckState.Attention, Repair: null } && warning.Detail.Contains("does not identify a replaceable module", StringComparison.Ordinal),
            "Warning memory diagnostic history requests review without identifying hardware or selecting repair");
        Require(MemoryDiagnosticResults.Interpret(new(0, "<Events/>", "", false)) is { State: CheckState.Information, Repair: null },
            "Empty complete memory diagnostic history does not claim that a test ran or memory is healthy");
        var step = MemoryDiagnosticResults.Step();
        Require(step.Verification && step.Tool == "wevtutil.exe" && step.Arguments.Contains("/c:201") &&
            step.Arguments.Any(argument => argument.Contains(MemoryDiagnosticResults.Provider, StringComparison.Ordinal)),
            "Memory diagnostic history uses one exact provider and a bounded read-only event query");
        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 5 }, evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Error = "fixture failure" }, evidence with { Output = "broken" },
            evidence with { Output = evidence.Output.Replace(MemoryDiagnosticResults.Provider, "Other", StringComparison.Ordinal) },
            evidence with { Output = evidence.Output.Replace("<EventID>1201", "<EventID>0", StringComparison.Ordinal) },
            evidence with { Output = evidence.Output.Replace("<Level>4", "<Level>5", StringComparison.Ordinal) },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Repeat(record, 201)) + "</Events>" }
        })
            Require(MemoryDiagnosticResults.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed, unexpected or capped memory diagnostic evidence remains unknown");
        try { MemoryDiagnosticResults.Interpret(evidence, new CancellationToken(true)); throw new Exception("Memory diagnostic cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: bounded Windows Memory Diagnostic history remains read only and never schedules a test or selects repair.");
    }

    private static string Event(int id, int level, long recordId, string time, string payload) =>
        "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='" + MemoryDiagnosticResults.Provider + "'/><EventID>" + id +
        "</EventID><Level>" + level + "</Level><TimeCreated SystemTime='" + time + "'/><EventRecordID>" + recordId + "</EventRecordID></System>" + payload + "</Event>";

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
