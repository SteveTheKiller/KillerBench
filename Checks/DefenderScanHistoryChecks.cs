using KillerBench.Engine;

internal static class DefenderScanHistoryChecks
{
    internal static void Run()
    {
        var now = new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);
        var evidence = new ToolResult(0, Json(now, false, "2026-09-16T10:00:00.0000000Z", "2026-09-16T10:30:00.0000000Z", 1, false,
            "2026-09-10T08:00:00.0000000Z", "2026-09-10T10:00:00.0000000Z", 7, false), "", false);
        var healthy = DefenderScanHistory.Interpret(evidence, now);
        Require(healthy is { Name: "Microsoft Defender scan history", State: CheckState.Information, Repair: null } &&
            healthy.Detail.Contains("provider age 1 day") && healthy.Detail.Contains("not a malware scan"),
            "Complete scan timestamps remain bounded informational evidence.");

        var overdue = DefenderScanHistory.Interpret(evidence with
        {
            Output = Json(now, true, "2026-09-01T10:00:00.0000000Z", "2026-09-01T10:30:00.0000000Z", 16, true,
                "2026-08-01T08:00:00.0000000Z", "2026-08-01T10:00:00.0000000Z", 47, true)
        }, now);
        Require(overdue is { State: CheckState.Attention, Repair: null } && overdue.Detail.Contains("currently in progress") &&
            overdue.Detail.Contains("Review scan policy"), "Provider-overdue state requests review without selecting a scan.");

        var none = DefenderScanHistory.Interpret(evidence with { Output = Json(now, false, null, null, null, false, null, null, null, false) }, now);
        Require(none.State == CheckState.Information && none.Detail.Contains("No completed quick") && none.Detail.Contains("No completed full"),
            "Absent complete history stays informational without inventing scan coverage.");

        foreach (string output in new[]
        {
            "{}", "not JSON",
            Json(now.AddDays(2), false, null, null, null, false, null, null, null, false),
            Json(now, false, null, "2026-09-16T10:30:00.0000000Z", 1, false, null, null, null, false),
            Json(now, false, "2026-09-16T11:00:00.0000000Z", "2026-09-16T10:30:00.0000000Z", 1, false, null, null, null, false),
            Json(now, false, null, null, null, true, null, null, null, false)
        }) Require(DefenderScanHistory.Interpret(evidence with { Output = output }, now).State == CheckState.Unknown,
            "Malformed, future, mismatched or impossible scan evidence fails closed.");

        foreach (var bad in new[] { evidence with { ExitCode = 1 }, evidence with { Error = "provider error" }, evidence with { Truncated = true }, evidence with { TimedOut = true } })
            Require(DefenderScanHistory.Interpret(bad, now).State == CheckState.Unknown, "Incomplete scan evidence fails closed.");
        string script = DefenderScanHistory.QueryStep().Arguments.Last();
        Require(script.Contains("Get-MpComputerStatus") && !script.Contains("Get-MpThreat") && !script.Contains("Exclusion") && !script.Contains("Resources"),
            "Native scan query is bounded to aggregate status and excludes sensitive threat fields.");
        Console.WriteLine("PASS: Defender scan history timing, overdue review, privacy boundary and incomplete evidence; no scan action.");
    }

    private static string Json(DateTimeOffset collected, bool active, string? quickStart, string? quickEnd, int? quickAge,
        bool quickOverdue, string? fullStart, string? fullEnd, int? fullAge, bool fullOverdue) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            complete = true,
            collectedUtc = collected.ToString("o"),
            scanInProgress = active,
            quickStartUtc = quickStart,
            quickEndUtc = quickEnd,
            quickAgeDays = quickAge,
            quickOverdue,
            fullStartUtc = fullStart,
            fullEndUtc = fullEnd,
            fullAgeDays = fullAge,
            fullOverdue
        });

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
