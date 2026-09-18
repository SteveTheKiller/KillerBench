using System.Xml;

namespace KillerBench.Engine;

public static class ServiceEvents
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Service event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", "System",
            "/q:*[System[Provider[@Name='Service Control Manager'] and (Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent service failures";
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, "Service event collection failed or was incomplete.", Evidence: result);
        try
        {
            var source = EventEvidence.FromXml("System", result.Output);
            var report = EventEvidence.Analyze(source.Evidence!, token);
            bool invalid = report.Partial || report.Timeline.Any(e => e.Component != "Service Control Manager" || e.Level is not ("1" or "2"));
            var failures = report.Timeline.Where(e => e.Component == "Service Control Manager" && e.Level is "1" or "2").ToArray();
            string detail = $"Last 7 days, newest first: {failures.Length} retained service error or critical records. Counts are events, not unique services.\n" +
                string.Join("\n", failures.Take(8).Select(e => e.Timestamp + " | " + e.Message + EventPayload.Preview(source.Evidence!.Lines[e.LineNumber - 1]))) +
                "\nHistorical failures do not establish the current service state. Review the raw payload, dependencies and subsequent starts before selecting a repair. These records do not authorize service restarts or startup changes.";
            foreach (var finding in report.Findings.Where(f => f.RuleId.StartsWith("KB-SCM-", StringComparison.Ordinal)))
                detail += "\n" + finding.Title + ": " + finding.NextStep;
            if (invalid) detail += "\nCoverage is incomplete or includes unexpected records.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Service event XML could not be interpreted.", Evidence: result); }
    }

}
