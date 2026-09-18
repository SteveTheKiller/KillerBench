using KillerBench.Engine;

internal static class InstalledHotfixChecks
{
    internal static void Run()
    {
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        ToolResult Evidence(string entries, int total) => new(0, $"{{\"total\":{total},\"entries\":{entries}}}", "", false);
        string Entry(string id = "KB5069999", string description = "Security Update", string date = "2026-09-14T12:00:00.0000000+00:00") =>
            $"{{\"id\":\"{id}\",\"description\":\"{description}\",\"installedUtc\":{(date == "null" ? "null" : $"\"{date}\"")}}}";

        var step = InstalledHotfixDiagnostics.Step();
        Require(step.Verification && step.Arguments[^1].Contains("Win32_QuickFixEngineering", StringComparison.Ordinal) &&
            step.Arguments[^1].Contains("Select-Object -First 512", StringComparison.Ordinal) &&
            !step.Arguments[^1].Contains("Install-Package", StringComparison.OrdinalIgnoreCase),
            "Installed hotfix collection is bounded and read only");
        var complete = InstalledHotfixDiagnostics.Interpret(Evidence("[" + Entry() + "," + Entry("KB5070000", "Update", "null") + "]", 2), now);
        Require(complete is { State: CheckState.Information, Repair: null, Evidence: not null } &&
            complete.Detail.Contains("KB5069999", StringComparison.Ordinal) && complete.Detail.Contains("date unavailable", StringComparison.Ordinal) &&
            complete.Detail.Contains("does not establish update applicability", StringComparison.Ordinal),
            "Complete installed hotfix inventory remains informational without repair inference");
        Require(InstalledHotfixDiagnostics.Interpret(Evidence("[]", 0), now) is { State: CheckState.Information, Repair: null },
            "Empty complete hotfix inventory does not claim that Windows is unpatched");
        var cappedEntries = string.Join(",", Enumerable.Range(0, InstalledHotfixDiagnostics.EntryLimit)
            .Select(index => Entry("KB" + (6000000 + index), "Update", "null")));
        Require(InstalledHotfixDiagnostics.Interpret(Evidence("[" + cappedEntries + "]", InstalledHotfixDiagnostics.EntryLimit + 1), now).State == CheckState.Unknown,
            "A bounded partial hotfix inventory stays unknown");
        foreach (var invalid in new[]
        {
            new ToolResult(1, "", "Unavailable", false), Evidence("[]", 1),
            Evidence("[" + Entry(date: "not-a-date") + "]", 1),
            Evidence("[" + Entry() + "," + Entry() + "]", 2),
            Evidence("[{\"id\":\"KB1\",\"id\":\"KB2\",\"description\":\"Update\",\"installedUtc\":null}]", 1),
            Evidence("[" + Entry(date: "2026-09-16T12:00:00.0000000+00:00") + "]", 1) with { Truncated = true }
        })
            Require(InstalledHotfixDiagnostics.Interpret(invalid, now) is { State: CheckState.Unknown, Repair: null, Evidence: not null },
                "Failed, mismatched, malformed, duplicate, future or truncated hotfix evidence remains unknown");
        Console.WriteLine("PASS: installed Windows update inventory is bounded, strict and read only; no update changed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
