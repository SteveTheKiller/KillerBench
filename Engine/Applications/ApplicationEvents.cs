using System.Xml;

namespace KillerBench.Engine.Applications;

public static class ApplicationEvents
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Application history requires Windows.");
        var command = new RepairStep("wevtutil.exe", ["qe", "Application",
            "/q:*[System[(Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(command, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent application errors";
        if (evidence.ExitCode != 0 || evidence.Truncated || evidence.TimedOut || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Application history is unavailable or incomplete. No applications changed.", Evidence: evidence);
        try
        {
            var source = EventEvidence.FromXml("Application", evidence.Output);
            var retained = source.Evidence ?? throw new InvalidDataException("Missing application evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool incomplete = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => string.IsNullOrWhiteSpace(item.Component) || item.Level is not ("1" or "2"));
            var groups = report.Timeline.GroupBy(item => (item.Component, item.EventId))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.Component, StringComparer.Ordinal).ThenBy(group => group.Key.EventId).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} retained Application error or critical records.\n" +
                "Most frequent provider/event pairs in retained records (not unique crashes):\n" +
                string.Join("\n", groups.Take(8).Select(group => $"{group.Key.Component} | Event {group.Key.EventId} | {group.Count()} records")) +
                "\nRecent records:\n" +
                string.Join("\n", report.Timeline.Take(8).Select(item => item.Timestamp + " | " + item.Component + " | " + item.Message +
                    EventPayload.Preview(retained.Lines[item.LineNumber - 1]))) +
                "\nHistorical errors do not establish a current failure or its cause. Match application, time and reported module details to the symptom before further diagnosis. No process was stopped and no application was repaired or removed.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (incomplete) detail += "\nSome records were malformed or outside the requested severity scope.";
            return new(name, incomplete ? CheckState.Unknown : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Application history XML could not be interpreted. No repair selected.", Evidence: evidence); }
    }
}
