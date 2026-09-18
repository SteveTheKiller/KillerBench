using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class CrashIncidentCorrelationChecks
{
    internal static void Run()
    {
        const string path = @"C:\Windows\Minidump\091526-1000-01.dmp";
        string Event(ulong record, string time, string code = "0x109", string? dumpPath = path, string extra = "") =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{CrashEvents.Provider}'/><EventID>1001</EventID><Level>2</Level><TimeCreated SystemTime='{time}'/><EventRecordID>{record}</EventRecordID></System><EventData><Data Name='param1'>{code}</Data>{(dumpPath is null ? "" : "<Data Name='param2'>" + dumpPath + "</Data>")}{extra}</EventData></Event>";
        ToolResult Evidence(params string[] entries) => new(0, "<Events>" + string.Concat(entries) + "</Events>", "", false);

        var file = new CrashDumpFile(path.ToLowerInvariant(), 4096, new DateTime(2026, 9, 15, 12, 0, 4, DateTimeKind.Utc));
        var inventory = new CrashDumpInventoryResult([file], true, []);
        var summary = new MinidumpSummary(Path.GetFileName(path), "Microsoft minidump", "x64", "10.0.26200",
            "0xC0000005", "fixture.sys", true, "Fixture structural metadata only.");
        var analyses = new Dictionary<string, MinidumpSummary>(StringComparer.OrdinalIgnoreCase) { [path.ToUpperInvariant()] = summary };

        var correlated = CrashIncidentCorrelation.Check(Evidence(Event(42, "2026-09-15T12:00:00Z")), inventory, analyses);
        Require(correlated is { State: CheckState.Information, Repair: null, Evidence: not null } &&
            correlated.Detail.Contains("1 exact file match(es), and 1 bounded structural analysis result(s)", StringComparison.Ordinal) &&
            correlated.Detail.Contains("0x00000109", StringComparison.Ordinal) &&
            correlated.Detail.Contains("fixture.sys", StringComparison.Ordinal) &&
            !correlated.Detail.Contains(@"C:\Windows", StringComparison.OrdinalIgnoreCase),
            "Exact path correlation did not retain a bounded incident without exposing its full path in the summary");
        var retained = JsonSerializer.Deserialize<CrashIncidentResult>(correlated.Evidence!.Output, Capture.JsonOptions)!;
        Require(retained is { Complete: true, ReportedPaths: 1, MatchedFiles: 1, AnalyzedFiles: 1 } &&
            retained.Incidents.Single() is { EventRecordId: 42, BugCheckCode: "0x00000109", DumpFile: not null, DumpAnalysis.Complete: true },
            "Structured crash incident evidence lost the exact event, file or analysis link");

        var noPath = CrashIncidentCorrelation.Check(Evidence(Event(43, "2026-09-15T12:00:04Z", dumpPath: null)), inventory, analyses);
        Require(noPath.State == CheckState.Information && noPath.Detail.Contains("0 reported dump path(s), 0 exact file match(es)", StringComparison.Ordinal),
            "A nearby dump timestamp was used to guess an event link");

        var unavailable = CrashIncidentCorrelation.Check(Evidence(Event(44, "2026-09-15T12:01:00Z", dumpPath: @"D:\Other\missing.dmp")), inventory, analyses);
        Require(unavailable.State == CheckState.Information && unavailable.Detail.Contains("availability unverified", StringComparison.Ordinal) &&
            !unavailable.Detail.Contains("missing file", StringComparison.OrdinalIgnoreCase),
            "An unmatched reported path was turned into a file-absence claim");

        var duplicate = CrashIncidentCorrelation.Check(Evidence(Event(45, "2026-09-15T12:02:00Z"),
            Event(45, "2026-09-15T12:02:01Z")), inventory, analyses);
        Require(duplicate.State == CheckState.Unknown && duplicate.Detail.Contains("partial, ambiguous or duplicated", StringComparison.Ordinal),
            "Duplicate event identities established complete incident coverage");

        var ambiguous = CrashIncidentCorrelation.Check(Evidence(Event(46, "2026-09-15T12:03:00Z", extra:
            "<Data Name='second'>C:\\Windows\\MEMORY.DMP</Data>")), inventory, analyses);
        Require(ambiguous.State == CheckState.Unknown, "Multiple reported dump paths established one incident link");
        Require(CrashIncidentCorrelation.Check(Evidence(Event(47, "2026-09-15T12:04:00Z")), inventory with { Complete = false }, analyses).State == CheckState.Unknown,
            "Incomplete dump inventory established complete incident coverage");
        Require(CrashIncidentCorrelation.Check(Evidence(Event(48, "2026-09-15T12:05:00Z", code: "invalid")), inventory, analyses).State == CheckState.Unknown,
            "Malformed bug-check identity established complete incident coverage");
        try { CrashIncidentCorrelation.Check(Evidence(Event(49, "2026-09-15T12:06:00Z")), inventory, analyses, new CancellationToken(true)); throw new Exception("Crash incident cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: crash incidents retain exact event, reported-path, file and structural-analysis links without timestamp guessing or root-cause claims.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
