using System.Xml;

namespace KillerBench.Engine.SystemHealth;

public static class MemoryDiagnosticResults
{
    public const string Provider = "Microsoft-Windows-MemoryDiagnostics-Results";

    public static RepairStep Step() => new("wevtutil.exe", ["qe", "System",
        "/q:*[System[Provider[@Name='" + Provider + "'] and TimeCreated[timediff(@SystemTime) <= 1209600000]]]",
        "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Memory Diagnostic history requires Windows.");
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Memory Diagnostic results";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown,
            detail + " Historical results do not establish current memory health or repair eligibility. No test was scheduled or run and no hardware or setting changed.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Windows Memory Diagnostic result history is unavailable or incomplete.");
        try
        {
            var source = EventEvidence.FromXml("System", evidence.Output);
            var retained = source.Evidence ?? throw new InvalidDataException("Missing memory diagnostic event evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool incomplete = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => item.Component != Provider || item.EventId <= 0 || item.Level is not ("1" or "2" or "3" or "4"));
            if (report.ParsedLines == 0)
                return new(name, incomplete ? CheckState.Unknown : CheckState.Information,
                    "No Windows Memory Diagnostic result records were returned for the last 14 days. This does not prove that a test ran or that memory is healthy. No test was scheduled or run and no hardware or setting changed.", Evidence: evidence);
            var groups = report.Timeline.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            bool warning = report.Timeline.Any(item => item.Level is "1" or "2" or "3");
            string detail = $"Last 14 days, newest first: {report.ParsedLines} retained Windows Memory Diagnostic result record(s).\n" +
                "Grouped event identities:\n" + string.Join("\n", groups.Take(12).Select(group =>
                    $"Event {group.Key.EventId} | level {group.Key.Level} | {group.Count()} records")) +
                "\nRecent records:\n" + string.Join("\n", report.Timeline.Take(8).Select(item =>
                    item.Timestamp + " | Event " + item.EventId + " | level " + item.Level + EventPayload.Preview(retained.Lines[item.LineNumber - 1]))) +
                "\nReview the exact event payload before interpreting the result. A historical pass does not establish current memory health, and a warning or error does not identify a replaceable module by itself. No automatic repair is available.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (incomplete) detail += "\nSome records were malformed or outside the expected provider and level scope.";
            return new(name, incomplete ? CheckState.Unknown : warning ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return Unknown("Windows Memory Diagnostic result XML could not be interpreted.");
        }
    }
}
