using System.Xml;

namespace KillerBench.Engine;

public static class HardwareErrorEvents
{
    public const string Provider = "Microsoft-Windows-WHEA-Logger";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Hardware error history requires Windows.");
        var command = new RepairStep("wevtutil.exe", ["qe", "System",
            "/q:*[System[Provider[@Name='" + Provider + "'] and TimeCreated[timediff(@SystemTime) <= 1209600000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(command, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Recent Windows hardware error records";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown,
            detail + " Hardware error history does not establish a failing component, current health or repair eligibility. No hardware, firmware, drivers or settings changed.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Windows hardware error history is unavailable or incomplete. Review the retained provider output.");
        try
        {
            var source = EventEvidence.FromXml("System", evidence.Output);
            var retained = source.Evidence ?? throw new InvalidDataException("Missing hardware event evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool incomplete = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => item.Component != Provider || item.EventId <= 0 || item.Level is not ("1" or "2" or "3" or "4"));
            if (report.ParsedLines == 0)
                return new(name, incomplete ? CheckState.Unknown : CheckState.Information,
                    "No WHEA records were returned for the last 14 days. Absence of retained events does not establish fault-free hardware. No hardware, firmware, drivers or settings changed.", Evidence: evidence);
            var groups = report.Timeline.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 14 days, newest first: {report.ParsedLines} retained WHEA records.\n" +
                "Grouped event identities (counts are records, not unique faults):\n" +
                string.Join("\n", groups.Take(12).Select(group => $"Event {group.Key.EventId} | level {group.Key.Level} | {group.Count()} records")) +
                "\nRecent records:\n" +
                string.Join("\n", report.Timeline.Take(8).Select(item => item.Timestamp + " | Event " + item.EventId + " | " + item.Message +
                    EventPayload.Preview(retained.Lines[item.LineNumber - 1]))) +
                "\nWHEA records warrant correlation with the exact event payload, firmware diagnostics and vendor hardware tests. They do not identify a replaceable component by themselves. No automatic repair is available.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (incomplete) detail += "\nSome records were malformed or outside the expected provider and level scope.";
            return new(name, incomplete ? CheckState.Unknown : CheckState.Attention, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return Unknown("Hardware error event XML could not be interpreted."); }
    }
}
