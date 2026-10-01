using System.Xml;

namespace KillerBench.Engine.Security;

public static class DefenderEvents
{
    private const string Channel = "Microsoft-Windows-Windows Defender/Operational";
    private const string Provider = "Microsoft-Windows-Windows Defender";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Microsoft Defender event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", Channel,
            "/q:*[System[(Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Recent Microsoft Defender events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Microsoft Defender history is unavailable or incomplete. No protection, threat, action or repair state was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(Channel, evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing Microsoft Defender evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != Provider);
            var problems = report.Timeline.Where(item => item.Component == Provider && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string recordWord = report.ParsedLines == 1 ? "record" : "records";
            string detail = $"Last 7 days, newest first: {report.ParsedLines} matching {recordWord} retained; {problems.Length} warning, error or critical records. The native query excludes informational events. Counts are records, not unique threats, files, scans or actions.";
            if (patterns.Length > 0) detail += "\n" + string.Join("\n", patterns.Take(8).Select(group => $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (patterns.Length > 8) detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            if (problems.Length > 0) detail += "\n" + string.Join("\n", problems.Take(8).Select(item => item.Timestamp + " | " + item.Message));
            detail += "\nRaw event payload can contain threat names, file paths, account information, process details and scan results. Historical records do not establish current protection, threat remediation or file state. Correlate exact records with current Defender status and threat history. No scan, quarantine action, exclusion change, protection change or signature refresh is selected from these records.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Microsoft Defender event XML could not be interpreted. Current protection, threat and action state remain unverified.", Evidence: evidence); }
    }
}
