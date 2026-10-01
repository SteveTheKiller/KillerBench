using System.Xml;

namespace KillerBench.Engine.Storage;

public static class StorageEvents
{
    private static readonly string[] Providers = ["disk", "Ntfs", "Microsoft-Windows-Ntfs", "storahci", "stornvme", "volmgr"];

    internal static IEnumerable<DiagnosticFinding> Findings(IReadOnlyList<ParsedLogEntry> entries, string source)
    {
        var recognized = entries.Where(e => e.Level is "1" or "2" or "3").Select(e => (Entry: e, Key: (e.Component.ToLowerInvariant(), e.EventId) switch
        {
            ("ntfs" or "microsoft-windows-ntfs", 55 or 98) => "filesystem",
            ("storahci" or "stornvme", 129) => "reset",
            ("disk", 153) => "retry",
            ("disk", 157) => "removed",
            ("disk", 51) => "paging-io",
            ("disk", 7 or 9 or 11) => "disk-error",
            _ => null
        })).Where(e => e.Key is not null);
        foreach (var group in recognized.GroupBy(e => e.Key).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var (title, next) = group.Key switch
            {
                "filesystem" => ("Filesystem errors recorded", "Confirm backups and the exact affected volume, then collect current filesystem and storage-health evidence before planning repairs."),
                "reset" => ("Storage controller resets recorded", "Correlate controller identity and event times with storage load and connection problems. Preserve timeout settings unless the storage vendor directs a change."),
                "retry" => ("Disk I/O retries recorded", "Review the reported disk identity, storage load and driver path around these times. A retry alone does not establish a failed disk."),
                "paging-io" => ("Buffered disk I/O errors recorded", "Identify the reported device and correlate recurring events with symptoms and backups. This event can involve buffered file I/O, not just the page file, and some removable-media cases are harmless. Do not resize the page file or select filesystem repair from this event alone."),
                "disk-error" => ("Disk error events recorded", "Preserve the event ID, device identity and binary status data. Confirm backups and correlate current device, connection and storage-provider evidence before selecting a repair. This historical event does not identify a failed physical component by itself."),
                _ => ("Unexpected disk removal recorded", "Review the reported disk and connection history. Confirm current availability and backups before considering filesystem repair.")
            };
            yield return new("KB-STORAGE-" + group.Key, 1, title, "Warning", "Recorded event",
                "The captured storage history contains this event pattern. Current condition and root cause remain unverified.", next,
                group.Key is "paging-io" or "disk-error"
                    ? "https://learn.microsoft.com/en-us/troubleshoot/windows-server/backup-and-storage/event-id-51-information"
                    : "https://learn.microsoft.com/en-us/troubleshoot/windows-server/backup-and-storage/troubleshoot-data-corruption-and-disk-errors",
                group.Count(), [.. group.Take(8).Select(e => new EvidenceReference(source, e.Entry.LineNumber,
                    e.Entry.Timestamp + " | " + e.Entry.Component + " | " + e.Entry.Message))]);
        }
    }

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Storage event collection requires Windows.");
        string providers = string.Join(" or ", Providers.Select(name => "@Name='" + name + "'"));
        var step = new RepairStep("wevtutil.exe", ["qe", "System",
            "/q:*[System[Provider[" + providers + "] and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Recent storage events";
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, "Storage history could not be collected completely. No disk-health conclusion was made.", Evidence: result);
        try
        {
            var source = EventEvidence.FromXml("System", result.Output);
            var report = EventEvidence.Analyze(source.Evidence!, token);
            bool unexpected = report.ParsedLines != source.Evidence!.Lines.Length || report.Timeline.Any(e =>
                !Providers.Contains(e.Component, StringComparer.OrdinalIgnoreCase) || e.Level is not ("0" or "1" or "2" or "3" or "4" or "5"));
            var warnings = report.Timeline.Where(e => Providers.Contains(e.Component, StringComparer.OrdinalIgnoreCase) && e.Level is "1" or "2" or "3").ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} records retained; {warnings.Length} warning, error or critical records.\n" +
                string.Join("\n", warnings.Take(8).Select(e => e.Timestamp + " | " + e.Component + " | " + e.Message +
                    EventPayload.Preview(source.Evidence.Lines[e.LineNumber - 1]))) +
                "\nHistory covers selected storage providers across this PC, not every driver or only the selected volume. Historical warnings do not prove current disk failure or authorize filesystem repair. Correlate their device identities and times with symptoms and current storage checks.";
            if (source.Evidence.Truncated) detail += "\nHistory reached the 200-event limit; older records were omitted.";
            foreach (var finding in report.Findings.Where(f => f.RuleId.StartsWith("KB-STORAGE-", StringComparison.Ordinal)))
                detail += "\n" + finding.Title + ": " + finding.NextStep;
            if (unexpected || (report.Partial && !source.Evidence.Truncated)) detail += "\nSome records were malformed or unexpected; coverage is incomplete.";
            return new(name, report.Partial || unexpected ? CheckState.Unknown : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Storage event XML could not be interpreted. No repair was selected.", Evidence: result); }
    }

}
