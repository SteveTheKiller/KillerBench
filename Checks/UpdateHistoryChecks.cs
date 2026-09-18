using KillerBench.Engine;

internal static class UpdateHistoryChecks
{
    internal static void Run()
    {
        var now = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
        const string id = "11111111-2222-3333-4444-555555555555";
        ToolResult Evidence(string entries, int total = 1) => new(0, $"{{\"total\":{total},\"entries\":{entries}}}", "", false);
        string Entry(int result, int hresult = 0, string? date = null, int operation = 1, string identity = id) =>
            $"{{\"id\":\"{identity}\",\"revision\":7,\"title\":\"Fixture update\",\"date\":\"{date ?? "2026-09-13T12:00:00.0000000+00:00"}\",\"operation\":{operation},\"result\":{result},\"hresult\":{hresult}}}";

        var step = UpdateHistoryDiagnostics.Step();
        Require(step.Verification && step.Arguments[^1].Contains("QueryHistory(0", StringComparison.Ordinal) && step.Arguments[^1].Contains("Min(100", StringComparison.Ordinal),
            "Update history uses a bounded read-only newest-first query");
        var success = UpdateHistoryDiagnostics.Interpret(Evidence("[" + Entry(2) + "]"), now);
        Require(success is { State: CheckState.Information, Repair: null, Evidence: not null } && success.Detail.Contains("Succeeded") && success.Detail.Contains("0x00000000"),
            "Recent successful history remains informational with exact identity and HRESULT");
        foreach (int result in new[] { 0, 1, 3, 4, 5 })
        {
            var finding = UpdateHistoryDiagnostics.Interpret(Evidence("[" + Entry(result, unchecked((int)0x80240022)) + "]"), now);
            Require(finding is { State: CheckState.Attention, Repair: null } && finding.Detail.Contains("0x80240022") &&
                finding.Detail.Contains("WU_E_ALL_UPDATES_FAILED") && finding.Detail.Contains("CBS evidence"),
                "Recent incomplete or failed history requests review without repair");
        }
        var oldFailure = UpdateHistoryDiagnostics.Interpret(Evidence("[" + Entry(4, -1, "2026-07-01T12:00:00.0000000+00:00") + "]"), now);
        Require(oldFailure.State == CheckState.Information, "Failures older than 30 days do not create a current alert");
        Require(UpdateHistoryDiagnostics.Interpret(Evidence("[]", 0), now) is { State: CheckState.Information, Repair: null },
            "Empty complete history remains informational without claiming no attempts");
        var repeatedOperation = UpdateHistoryDiagnostics.Interpret(Evidence("[" + Entry(2) + "," + Entry(2, 2359302) + "]", 2), now);
        Require(repeatedOperation.State == CheckState.Information && repeatedOperation.Detail.Contains("0x00240006"),
            "Windows may retain same-time operation records with distinct HRESULTs");
        var unknownCode = UpdateHistoryDiagnostics.Interpret(Evidence("[" + Entry(4, unchecked((int)0xDEADBEEF)) + "]"), now);
        Require(unknownCode.State == CheckState.Attention && unknownCode.Detail.Contains("0xDEADBEEF") && !unknownCode.Detail.Contains("Recognized recent codes"),
            "Unknown update HRESULTs remain visible without an invented reference");
        var routedEvidence = Evidence("[" + Entry(4, unchecked((int)0x80240034)) + "]");
        Require(UpdateHistoryDiagnostics.RecentReferences(routedEvidence, now) is [{ Symbol: "WU_E_DOWNLOAD_FAILED" }] &&
            UpdateHistoryDiagnostics.RecentReferences(Evidence("[" + Entry(4, unchecked((int)0xDEADBEEF)) + "]"), now).Count == 0,
            "Only valid, recent, recognized failures enter automatic follow-up planning");

        foreach (var invalid in new[]
        {
            new ToolResult(1, "", "Unavailable", false),
            Evidence("[]", 1),
            Evidence("[" + Entry(2, date: "not-a-date") + "]"),
            Evidence("[" + Entry(2, operation: 3) + "]"),
            Evidence("[" + Entry(6) + "]"),
            Evidence("[" + Entry(2, identity: "bad") + "]"),
            Evidence("[" + Entry(2) + "," + Entry(2) + "]", 2),
            Evidence("[{\"id\":\"11111111-2222-3333-4444-555555555555\",\"revision\":7,\"title\":\"One\",\"title\":\"Two\",\"date\":\"2026-09-13T12:00:00.0000000+00:00\",\"operation\":1,\"result\":2,\"hresult\":0}]") with { Truncated = true },
            Evidence("[" + Entry(2, date: "2026-09-15T12:00:00.0000000+00:00") + "]")
        })
            Require(UpdateHistoryDiagnostics.Interpret(invalid, now) is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Failed, mismatched, malformed, duplicate, truncated or future history remains unknown");
        Console.WriteLine("PASS: Windows Update history bounds, outcomes, dates, identities and HRESULT evidence are read-only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
