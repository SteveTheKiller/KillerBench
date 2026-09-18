using System.Xml;

namespace KillerBench.Engine;

public static class PrintServiceEvents
{
    internal static IEnumerable<DiagnosticFinding> Findings(IReadOnlyList<ParsedLogEntry> entries, string source)
    {
        foreach (var group in entries.Where(item => item.Component == "Microsoft-Windows-PrintService" &&
            item.EventId is not null && item.Level is "1" or "2" or "3")
            .GroupBy(item => (item.EventId, item.Level)).OrderBy(group => group.Key.EventId).ThenBy(group => group.Key.Level))
        {
            yield return new("KB-PRINT-EVENT-" + group.Key.EventId + "-" + group.Key.Level, 1,
                "Print-service event " + group.Key.EventId + " recorded", "Warning", "Recorded event",
                "PrintService recorded warning or failure evidence. Counts describe records, not unique print jobs. This does not establish a current queue, driver or physical-printer failure.",
                "Review the event payload for the queue, client and reported error. Match its time to the reported printing problem, then inspect that queue's current state and driver registration. Preserve queued jobs and managed configuration; do not select a repair from this event alone.",
                "https://learn.microsoft.com/en-us/troubleshoot/windows-server/printing/troubleshoot-printer-guidance",
                group.Count(), group.Take(8).Select(item => new EvidenceReference(source, item.LineNumber,
                    item.Timestamp + " | " + item.Message)).ToArray());
        }
    }

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Print event collection requires Windows.");
        var command = new RepairStep("wevtutil.exe", ["qe", "Microsoft-Windows-PrintService/Admin",
            "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(command, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent print-service events";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("PrintService Admin history is unavailable or incomplete. Logging and access settings were not changed.");
        try
        {
            var retained = EventEvidence.FromXml("Microsoft-Windows-PrintService/Admin", evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing print history.");
            var report = EventEvidence.Analyze(retained, token);
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => item.Component != "Microsoft-Windows-PrintService" || item.Level is not ("0" or "1" or "2" or "3" or "4" or "5"));
            var problems = report.Timeline.Where(item => item.Component == "Microsoft-Windows-PrintService" && item.Level is "1" or "2" or "3").ToArray();
            var groups = problems.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string patterns = groups.Length == 0 ? "No warning/error patterns in retained records." :
                "Most frequent retained warning/error patterns:\n" + string.Join("\n", groups.Take(8).Select(group =>
                    $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} record{(group.Count() == 1 ? "" : "s")}."));
            if (groups.Length > 8) patterns += $"\n{groups.Length - 8} additional event/level patterns remain in the raw evidence.";
            string detail = $"Last 7 days, newest first: {report.ParsedLines} records retained; {problems.Length} warning, error or critical records.\n" +
                patterns + "\nRecord counts do not count unique failed print jobs.\n" +
                string.Join("\n", problems.Take(8).Select(item => item.Timestamp + " | " + item.Message + EventPayload.Preview(retained.Lines[item.LineNumber - 1]))) +
                "\nHistorical events do not establish a current queue failure or authorize driver replacement, job deletion or service changes. This covers this PC's Admin channel, not remote print servers or physical printing.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nUnexpected or malformed records make coverage incomplete.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return Unknown("Print history XML could not be interpreted. No changes selected."); }
    }

}
