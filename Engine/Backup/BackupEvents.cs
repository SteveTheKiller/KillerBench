using System.Xml;

namespace KillerBench.Engine.Backup;

public enum BackupEventSource { Vss, Volsnap }

public static class BackupEvents
{
    private static (string Channel, string Provider) Identity(BackupEventSource source) => source switch
    {
        BackupEventSource.Vss => ("Application", "VSS"),
        BackupEventSource.Volsnap => ("System", "volsnap"),
        _ => throw new ArgumentOutOfRangeException(nameof(source))
    };

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default, IProgress<string>? progress = null)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Backup event history requires Windows.");
        var checks = new List<WorkflowCheck>();
        foreach (var source in Enum.GetValues<BackupEventSource>())
        {
            token.ThrowIfCancellationRequested();
            var (channel, provider) = Identity(source);
            progress?.Report("Reading " + provider + " backup event history...");
            var step = new RepairStep("wevtutil.exe", ["qe", channel,
                "/q:*[System[Provider[@Name='" + provider + "'] and (Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
                "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
            try { checks.Add(Interpret(source, await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false), token)); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            { checks.Add(new("Recent " + provider + " events", CheckState.Unknown, "History unavailable: " + error.Message)); }
        }
        return checks.AsReadOnly();
    }

    public static WorkflowCheck Interpret(BackupEventSource source, ToolResult result, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var (channel, provider) = Identity(source);
        string name = "Recent " + provider + " events";
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, "Backup event history is unavailable or incomplete. No repair selected from history.", Evidence: result);
        try
        {
            var capture = EventEvidence.FromXml(channel, result.Output);
            var report = EventEvidence.Analyze(capture.Evidence!, token);
            bool invalid = report.Partial || report.Timeline.Any(entry => !entry.Component.Equals(provider, StringComparison.OrdinalIgnoreCase) || entry.Level is not ("1" or "2" or "3"));
            string detail = $"Last 7 days, newest first: {report.ParsedLines} retained {provider} warning, error or critical records. Counts are events, not unique backup failures.\n" +
                string.Join("\n", report.Timeline.Take(8).Select(entry => entry.Timestamp + " | Event " + entry.EventId + " | " + entry.Message +
                    EventPayload.Preview(capture.Evidence!.Lines[entry.LineNumber - 1]))) +
                "\nCorrelate event times and reported volumes, writers or providers with the backup application's own result. Historical errors do not establish a current fault, writer health or a repair prerequisite. No snapshots, services, shadow storage or provider registrations changed.";
            if (invalid) detail += "\nCoverage is incomplete or includes unexpected records.";
            return new(name, invalid ? CheckState.Unknown : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Backup event XML could not be interpreted.", Evidence: result); }
    }
}
