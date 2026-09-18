using System.Xml;

namespace KillerBench.Engine;

public static class InstallerEvents
{
    internal static IEnumerable<DiagnosticFinding> Findings(IReadOnlyList<ParsedLogEntry> entries, string source)
    {
        foreach (var group in entries.Where(e => e.Component == "MsiInstaller" && e.Level is "1" or "2" or "3" &&
            e.EventId is 1001 or 1002 or 1003 or 1004 or 10005 or 11708).GroupBy(e => e.EventId).OrderBy(g => g.Key))
        {
            var (title, next) = group.Key switch
            {
                1001 or 1004 => ("Installer feature detection failures recorded", "Match the reported product, feature and component to its package log. Confirm the expected resource before considering a supported product repair."),
                1002 or 1003 => ("Installer configuration data errors recorded", "Review the reported registry location and the owning product's installation log. Preserve configuration data; do not delete Installer registrations from an event alone."),
                10005 => ("Installer internal package errors recorded", "Review the reported package error and arguments with the package vendor. Correlate with its detailed installation log before changing the machine."),
                _ => ("Product installation failures recorded", "Find the matching product installation log and its first actionable failure. Check subsequent attempts before deciding whether the product still needs attention.")
            };
            yield return new("KB-MSI-" + group.Key, 1, title, "Warning", "Recorded event",
                "Windows Installer recorded this event. It does not identify a current root cause or authorize product repair or removal.", next,
                "https://learn.microsoft.com/en-us/windows/win32/msi/event-logging", group.Count(),
                group.Take(8).Select(e => new EvidenceReference(source, e.LineNumber, e.Timestamp + " | " + e.Message)).ToArray());
        }
    }

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Installer event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", "Application",
            "/q:*[System[Provider[@Name='MsiInstaller'] and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent installer events";
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, "Installer event collection failed or was incomplete. No current installation state was inferred.", Evidence: result);
        try
        {
            var source = EventEvidence.FromXml("Application", result.Output);
            var report = EventEvidence.Analyze(source.Evidence!, token);
            bool capped = source.Evidence!.Truncated;
            bool unexpected = report.ParsedLines != source.Evidence.Lines.Length || report.Timeline.Any(e => e.Component != "MsiInstaller" || e.Level is not ("0" or "1" or "2" or "3" or "4" or "5"));
            bool invalid = report.Partial || unexpected;
            var failures = report.Timeline.Where(e => e.Component == "MsiInstaller" && e.Level is "1" or "2").ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} events retained; {failures.Length} error or critical records. Counts describe events, not unique installations.\n";
            detail += string.Join("\n", failures.Take(8).Select(e => e.Timestamp + " | " + e.Message + EventPayload.Preview(source.Evidence.Lines[e.LineNumber - 1])));
            detail += "\nHistorical events do not prove an active failure. Review the matching package log and later outcomes. No repair is selected from these events.";
            foreach (var finding in report.Findings.Where(f => f.RuleId.StartsWith("KB-MSI-", StringComparison.Ordinal)))
                detail += "\n" + finding.Title + ": " + finding.NextStep;
            if (capped) detail += "\nHistory reached the 200-event limit. Older matching events were omitted; counts cover retained records only.";
            if (unexpected || (report.Partial && !capped)) detail += "\nSome records were malformed or unexpected and could not be assessed.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return new(name, CheckState.Unknown, "Installer event XML could not be interpreted. No current installation state was inferred.", Evidence: result);
        }
    }
}
