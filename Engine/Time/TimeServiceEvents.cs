using System.Xml;

namespace KillerBench.Engine;

public static class TimeServiceEvents
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Time event collection requires Windows.");
        var command = new RepairStep("wevtutil.exe", ["qe", "Microsoft-Windows-Time-Service/Operational",
            "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(command, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent time-service events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Time history is unavailable or incomplete. The channel may be disabled or inaccessible; its configuration was not changed.", Evidence: evidence);
        try
        {
            var source = EventEvidence.FromXml("Microsoft-Windows-Time-Service/Operational", evidence.Output);
            var retained = source.Evidence ?? throw new InvalidDataException("Missing time event evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length ||
                report.Timeline.Any(item => item.Component != "Microsoft-Windows-Time-Service" || item.Level is not ("0" or "1" or "2" or "3" or "4" or "5"));
            var problems = report.Timeline.Where(item => item.Component == "Microsoft-Windows-Time-Service" && item.Level is "1" or "2" or "3").ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} records retained; {problems.Length} warning, error or critical records.\n";
            detail += string.Join("\n", problems.Take(8).Select(item => item.Timestamp + " | " + item.Message +
                EventPayload.Preview(retained.Lines[item.LineNumber - 1])));
            detail += "\nClock history does not establish current synchronization or clock accuracy. Compare it with the live synchronization check. No clock or source settings were changed.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Time history XML could not be interpreted. Current synchronization remains unverified by this check.", Evidence: evidence); }
    }
}
