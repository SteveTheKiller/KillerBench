using System.Xml;

namespace KillerBench.Engine;

public static class DhcpClientEvents
{
    private sealed record ChannelDefinition(string Channel, string Provider, string Label);
    private static readonly ChannelDefinition[] Definitions =
    [
        new("Microsoft-Windows-Dhcp-Client/Admin", "Microsoft-Windows-Dhcp-Client", "IPv4 administrative"),
        new("Microsoft-Windows-Dhcp-Client/Operational", "Microsoft-Windows-Dhcp-Client", "IPv4 operational"),
        new("Microsoft-Windows-Dhcpv6-Client/Admin", "Microsoft-Windows-DHCPv6-Client", "IPv6 administrative"),
        new("Microsoft-Windows-Dhcpv6-Client/Operational", "Microsoft-Windows-DHCPv6-Client", "IPv6 operational")
    ];

    public static IReadOnlyList<string> Channels { get; } = Array.AsReadOnly(Definitions.Select(item => item.Channel).ToArray());

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DHCP Client event collection requires Windows.");
        execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: cancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            return await Task.WhenAll(Definitions.Select(definition => CollectAsync(definition, execute, deadline.Token)))
                .WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            var evidence = new ToolResult(-1, "", "DHCP Client event deadline expired.", false) { TimedOut = true };
            return Definitions.Select(definition => Interpret(definition.Channel, evidence)).ToArray();
        }
    }

    public static WorkflowCheck Interpret(string channel, ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        var definition = Definitions.SingleOrDefault(item => item.Channel == channel)
            ?? throw new ArgumentException("Choose a supported DHCP Client event channel.", nameof(channel));
        string name = "Recent DHCP Client " + definition.Label + " events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown,
                "DHCP Client history is unavailable or incomplete. No lease, address, adapter, server, connectivity or repair state was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(channel, evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing DHCP Client evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != definition.Provider);
            var problems = report.Timeline.Where(item => item.Component == definition.Provider && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level)).OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} record{(report.ParsedLines == 1 ? "" : "s")} retained; {problems.Length} warning, error or critical records. Counts are records, not unique adapters, leases, servers or acquisition attempts.";
            if (patterns.Length > 0)
                detail += "\n" + string.Join("\n", patterns.Take(8).Select(group =>
                    $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (patterns.Length > 8)
                detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            detail += "\nRaw event payload can contain addresses, adapter identities, host or domain names, server identities and lease details. Ordinary summaries exclude those values. Historical records do not establish current lease, address, server or connectivity state. Correlate exact records with current adapter, lease, route, gateway and DNS evidence. No lease renewal, release, adapter change, service start or network setting is selected from these records.";
            if (retained.Truncated)
                detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid)
                detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return new(name, CheckState.Unknown,
                "DHCP Client event XML could not be interpreted. Current lease, address, server and connectivity state remain unverified.", Evidence: evidence);
        }
    }

    private static async Task<WorkflowCheck> CollectAsync(ChannelDefinition definition,
        Func<RepairStep, CancellationToken, Task<ToolResult>> execute, CancellationToken token)
    {
        var step = new RepairStep("wevtutil.exe", ["qe", definition.Channel,
            "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]", "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        try
        {
            var task = execute(step, token) ?? throw new InvalidOperationException("DHCP Client event provider returned no task.");
            var evidence = await task.ConfigureAwait(false)
                ?? throw new InvalidOperationException("DHCP Client event provider returned no evidence.");
            return Interpret(definition.Channel, evidence, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Interpret(definition.Channel, new(-1, "", error.Message, false), token);
        }
    }
}
