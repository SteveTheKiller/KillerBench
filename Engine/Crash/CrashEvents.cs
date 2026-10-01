using System.Xml;

namespace KillerBench.Engine.Crash;

public static class CrashEvents
{
    public const string Provider = "Microsoft-Windows-WER-SystemErrorReporting";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("System crash history requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", "System",
            "/q:*[System[Provider[@Name='" + Provider + "'] and EventID=1001 and Level=2 and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent system crash reports";
        WorkflowCheck Unknown() => new(name, CheckState.Unknown, "System crash-report coverage is unavailable or incomplete.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            var source = EventEvidence.FromXml("System", evidence.Output);
            var report = EventEvidence.Analyze(source.Evidence!, token);
            bool partial = report.Partial || report.Timeline.Any(item => item.Component != Provider || item.EventId != 1001 || item.Level != "2");
            string detail = "Last 7 days, newest first: " + report.ParsedLines + " retained system error reports.\n" +
                string.Join("\n", report.Timeline.Take(8).Select(item => item.Timestamp + " | " + item.Message + EventPayload.Preview(source.Evidence!.Lines[item.LineNumber - 1]))) +
                "\nReview reported codes and dump paths with shutdown history and dump metadata. Event counts are not unique crash counts. A reported dump path does not prove the file still exists or is valid. No root cause or repair is inferred.";
            if (partial) detail += "\nCoverage is incomplete or contains unexpected records.";
            return new(name, partial ? CheckState.Unknown : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException) { return Unknown(); }
    }
}
