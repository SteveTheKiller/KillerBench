using System.Xml;

namespace KillerBench.Engine.Network;

public static class FirewallEvents
{
    public const string Channel = "Microsoft-Windows-Windows Firewall With Advanced Security/Firewall";
    private const string Provider = "Microsoft-Windows-Windows Firewall With Advanced Security";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows Firewall event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", Channel,
            "/q:*[System[(Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            execute ??= (plan, cancellationToken) =>
                BoundedTool.RunAsync(plan, TimeSpan.FromSeconds(20), token: cancellationToken);
            var pending = execute(step, deadline.Token)
                ?? throw new InvalidOperationException("Windows Firewall event provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Windows Firewall event provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Interpret(new(-1, "", "Windows Firewall event deadline expired.", false) { TimedOut = true }, token);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Interpret(new(-1, "", error.Message, false), token);
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Recent Windows Firewall events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown,
                "Windows Firewall operational history is unavailable or incomplete. No profile, rule, filter, connection, service or repair state was inferred.",
                Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(Channel, evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing Windows Firewall event evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message))
                .Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != Provider || item.Level is not ("1" or "2" or "3"));
            var patterns = report.Timeline.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId)
                .ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} warning, error or critical record{(report.ParsedLines == 1 ? "" : "s")} retained. Counts are records, not unique rules, applications, addresses, ports, profiles, filters or incidents.";
            if (patterns.Length > 0)
                detail += "\n" + string.Join("\n", patterns.Take(8).Select(group =>
                    $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} record{(group.Count() == 1 ? "" : "s")}."));
            if (patterns.Length > 8)
                detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            detail += "\nRaw event payload can contain application paths, process IDs, local and remote addresses, ports, rule names, filter identifiers, profiles and interface details. Ordinary summaries exclude those values. Historical records do not establish current firewall protection, traffic reachability, rule intent or cause. Correlate exact records with current profile, policy, logging, service, application and connection evidence. No profile, rule, filter, IPsec, logging, service or network change is selected from these records.";
            if (retained.Truncated)
                detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid)
                detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : report.ParsedLines > 0 ? CheckState.Attention : CheckState.Information,
                detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return new(name, CheckState.Unknown,
                "Windows Firewall event XML could not be interpreted. Current profile, rule, connection, service and policy state remain unverified.",
                Evidence: evidence);
        }
    }
}
