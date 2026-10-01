using System.Xml;

namespace KillerBench.Engine.Updates;

public static class WindowsUpdateEvents
{
    public const string Channel = "Microsoft-Windows-WindowsUpdateClient/Operational";
    public const string Provider = "Microsoft-Windows-WindowsUpdateClient";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Update event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", Channel,
            "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Recent Windows Update Client events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Windows Update Client history is unavailable or incomplete. No update state or repair was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(Channel, evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing Windows Update Client evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => item.Component != Provider);
            var problems = report.Timeline.Where(item => item.Component == Provider && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} records retained; {problems.Length} warning, error or critical records. Counts are records, not unique update attempts.\n";
            detail += string.Join("\n", patterns.Take(8).Select(group => $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (patterns.Length > 8) detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            if (problems.Length > 0)
                detail += "\n" + string.Join("\n", problems.Take(8).Select(item => item.Timestamp + " | " + item.Message));
            detail += "\nHistory does not establish current applicability, policy, service, download, installation, restart or servicing health. Compare the retained event payload with Windows Update history, current service and policy evidence, CBS, DISM and Setup logs. No scan, download, installation, cache reset, service recovery, policy change or servicing repair is selected from these records.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Windows Update Client event XML could not be interpreted. Current update and servicing state remain unverified.", Evidence: evidence); }
    }
}
