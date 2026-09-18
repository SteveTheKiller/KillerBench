using System.Xml;

namespace KillerBench.Engine;

public static class ShutdownEvents
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Shutdown history requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", "System",
            "/q:*[System[((Provider[@Name='Microsoft-Windows-Kernel-Power'] and EventID=41 and Level=1) or (Provider[@Name='EventLog'] and EventID=6008 and Level=2)) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent unexpected shutdowns";
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, "Shutdown history is unavailable or incomplete.", Evidence: result);
        try
        {
            var source = EventEvidence.FromXml("System", result.Output);
            var report = EventEvidence.Analyze(source.Evidence!, token);
            bool invalid = report.Partial || report.Timeline.Any(entry =>
                !((entry.Component == "Microsoft-Windows-Kernel-Power" && entry.EventId == 41 && entry.Level == "1") ||
                  (entry.Component == "EventLog" && entry.EventId == 6008 && entry.Level == "2")));
            string detail = $"Last 7 days, newest first: {report.ParsedLines} retained shutdown records. Counts are events, not unique incidents.\n";
            detail += string.Join("\n", report.Findings.Where(f => f.RuleId.StartsWith("KB-SHUTDOWN-", StringComparison.Ordinal))
                .Select(f => f.Title + ": " + f.NextStep));
            detail += "\nHistory does not establish current health or a root cause. No repair selected.";
            if (invalid) detail += "\nCoverage is incomplete or contains unexpected records.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Shutdown history XML could not be interpreted.", Evidence: result); }
    }
}
