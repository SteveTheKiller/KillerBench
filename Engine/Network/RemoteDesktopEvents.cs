using System.Xml;

namespace KillerBench.Engine.Network;

public static class RemoteDesktopEvents
{
    public const string ConnectionChannel = "Microsoft-Windows-TerminalServices-RemoteConnectionManager/Operational";
    public const string SessionChannel = "Microsoft-Windows-TerminalServices-LocalSessionManager/Operational";
    public const string ConnectionProvider = "Microsoft-Windows-TerminalServices-RemoteConnectionManager";
    public const string SessionProvider = "Microsoft-Windows-TerminalServices-LocalSessionManager";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Remote Desktop event collection requires Windows.");
        var checks = new List<WorkflowCheck>();
        foreach (var source in new[]
        {
            (ConnectionChannel, ConnectionProvider, "Recent Remote Desktop connection events"),
            (SessionChannel, SessionProvider, "Recent Remote Desktop session events")
        })
        {
            token.ThrowIfCancellationRequested();
            var step = new RepairStep("wevtutil.exe", ["qe", source.Item1,
                "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]", "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
            var evidence = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
            checks.Add(Interpret(source.Item1, source.Item2, source.Item3, evidence, token));
        }
        return checks.AsReadOnly();
    }

    public static WorkflowCheck Interpret(string channel, string provider, string name, ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        if ((channel, provider) is not (ConnectionChannel, ConnectionProvider) and not (SessionChannel, SessionProvider))
            throw new ArgumentException("Unsupported Remote Desktop event source.");
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Remote Desktop history is unavailable or incomplete. No connection, authentication, session or repair state was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(channel, evidence.Output).Evidence ?? throw new InvalidDataException("Missing Remote Desktop evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate || report.Timeline.Any(item => item.Component != provider);
            var problems = report.Timeline.Where(item => item.Component == provider && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level)).OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} record{(report.ParsedLines == 1 ? "" : "s")} retained; {problems.Length} warning, error or critical records. Counts are records, not unique clients, accounts, connections or sessions.";
            if (patterns.Length > 0) detail += "\n" + string.Join("\n", patterns.Take(8).Select(group => $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (patterns.Length > 8) detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            if (problems.Length > 0) detail += "\n" + string.Join("\n", problems.Take(8).Select(item => item.Timestamp + " | " + item.Message));
            detail += "\nRaw event payload can contain account, client address, session, certificate and authentication details. Historical records do not establish current listener, firewall, authentication, connection or session state. No enablement, listener, firewall, certificate, permission, service start, disconnect or session action is selected from these records.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Remote Desktop event XML could not be interpreted. Current listener, authentication, connection and session state remain unverified.", Evidence: evidence); }
    }
}
