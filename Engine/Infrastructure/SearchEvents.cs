using System.Xml;

namespace KillerBench.Engine;

public static class SearchEvents
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Search event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", "Application",
            "/q:*[System[Provider[@Name='Microsoft-Windows-Search'] and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Recent Windows Search events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Search event history is unavailable or incomplete. No index state or repair was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml("Application", evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing Search event evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => item.Component != "Microsoft-Windows-Search");
            var problems = report.Timeline.Where(item => item.Component == "Microsoft-Windows-Search" && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} records retained; {problems.Length} warning, error or critical records. Counts are records, not unique failures.\n";
            detail += string.Join("\n", patterns.Take(8).Select(group => $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (patterns.Length > 8) detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            detail += "\n" + string.Join("\n", problems.Take(8).Select(item => item.Timestamp + " | " + item.Message + EventPayload.Preview(retained.Lines[item.LineNumber - 1])));
            detail += "\nHistory does not establish current index corruption or search-result completeness. Compare reported content sources and times with current symptoms and indexing status. No index rebuild, deletion, location or policy change is selected from these records.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Search event XML could not be interpreted. Current index health remains unverified.", Evidence: evidence); }
    }
}
