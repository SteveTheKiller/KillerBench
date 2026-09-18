using System.Text.Json;
using KillerBench.Engine;

internal static class ReliabilityHistoryChecks
{
    internal static void Run()
    {
        var now = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
        var crash = Row(now.AddHours(-1), "Application Error", "editor.exe", 1000, "Faulting module editor.dll");
        var repeated = Evidence(true, crash, Row(now.AddHours(-2), "Application Error", "editor.exe", 1000, "Earlier failure"),
            Row(now.AddHours(-3), "Windows Error Reporting", "editor.exe", 1001, "Report submitted"),
            Row(now.AddHours(-4), "Application Hang", "browser.exe", 1002, "Stopped responding"),
            Row(now.AddHours(-5), "EventLog", "Windows", 6008, "Previous shutdown was unexpected"));
        var checks = ReliabilityHistory.Interpret(repeated, now);
        Require(checks.Count == 4 && checks.Count(check => check.State == CheckState.Attention) == 3 && checks.All(check => check.Repair is null),
            "Known crash, hang and shutdown records require review without exposing repairs");
        Require(checks.Single(check => check.Name.Contains("editor.exe") && check.Name.Contains("terminated")).Detail.Contains("2 retained record(s)") &&
            checks.Any(check => check.Name.Contains("Windows Error Reporting") || check.Detail.Contains("Windows Error Reporting")),
            "Repeated exact identities are grouped while unrelated reporting records remain separate");
        Require(ReliabilityHistory.Interpret(Evidence(true), now).Single() is { State: CheckState.Information, Repair: null },
            "Empty complete history is not a health verdict or repair trigger");
        Require(!Workflows.SupportsRepair(WorkflowKind.ReliabilityHistory) && Workflows.Scopes(WorkflowKind.ReliabilityHistory).Length == 0 &&
            RecipeCatalog.ForWorkflow(WorkflowKind.ReliabilityHistory).Count == 0,
            "Reliability history is read-only through workflow and recipe APIs");

        var partial = ReliabilityHistory.Interpret(Evidence(false, crash), now);
        Require(partial.Any(check => check.State == CheckState.Unknown) && partial.All(check => check.Repair is null),
            "Overflow keeps retained findings but reports incomplete coverage");
        foreach (var invalid in new[]
        {
            repeated with { ExitCode = 5 }, repeated with { TimedOut = true }, repeated with { Truncated = true }, repeated with { Error = "fixture error" },
            repeated with { Output = "broken" }, Evidence(true, Row(now.AddMinutes(6), "Application Error", "future.exe", 1000, "future")),
            Evidence(true, crash, crash), Evidence(true, Row(now.AddDays(-16), "Application Error", "old.exe", 1000, "old")),
            repeated with { Output = repeated.Output.Replace("\"source\":\"Application Error\"", "\"source\":\"Application Error\",\"source\":\"Other\"") }
        })
            Require(ReliabilityHistory.Interpret(invalid, now).Any(check => check.State == CheckState.Unknown) &&
                ReliabilityHistory.Interpret(invalid, now).All(check => check.Repair is null),
                "Incomplete, duplicate, future, stale or malformed reliability evidence remains unknown");
        try { ReliabilityHistory.Interpret(repeated, now, new CancellationToken(true)); throw new Exception("Reliability cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: bounded reliability history grouping, coverage and read-only workflow behavior.");
    }

    private static object Row(DateTimeOffset time, string source, string product, uint eventId, string message) =>
        new { time = time.ToString("O"), source, product, eventId, message };

    private static ToolResult Evidence(bool complete, params object[] records) =>
        new(0, JsonSerializer.Serialize(new { complete, records }), "", false);

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
