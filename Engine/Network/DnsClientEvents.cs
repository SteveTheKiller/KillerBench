using System.Xml;

namespace KillerBench.Engine.Network;

public static class DnsClientEvents
{
    private const string Provider = "Microsoft-Windows-DNS-Client";
    private sealed record SourceDefinition(string Channel, string Label, string Query);
    private static readonly SourceDefinition[] Definitions =
    [
        new("System", "system", "*[System[Provider[@Name='Microsoft-Windows-DNS-Client'] and (Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 604800000]]]") ,
        new("Microsoft-Windows-DNS-Client/Operational", "operational", "*[System[(Level=1 or Level=2 or Level=3) and TimeCreated[timediff(@SystemTime) <= 604800000]]]")
    ];

    public static IReadOnlyList<string> Channels { get; } =
        Array.AsReadOnly(Definitions.Select(definition => definition.Channel).ToArray());

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DNS Client event collection requires Windows.");
        execute ??= (step, cancellationToken) =>
            BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: cancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            return await Task.WhenAll(Definitions.Select(definition =>
                    CollectAsync(definition, execute, deadline.Token)))
                .WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            var evidence = new ToolResult(-1, "", "DNS Client event deadline expired.", false) { TimedOut = true };
            return [.. Definitions.Select(definition => Interpret(definition.Channel, evidence))];
        }
    }

    public static WorkflowCheck Interpret(string channel, ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        var definition = Definitions.SingleOrDefault(item => item.Channel == channel)
            ?? throw new ArgumentException("Choose a supported DNS Client event source.", nameof(channel));
        string name = "Recent DNS Client " + definition.Label + " events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown,
                "DNS Client failure history is unavailable or incomplete. No resolver, server, cache, suffix, registration, policy, connectivity or repair state was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(channel, evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing DNS Client evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message))
                .Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != Provider || item.Level is not ("1" or "2" or "3"));
            var patterns = report.Timeline.GroupBy(item => (item.EventId, item.Level))
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.EventId)
                .ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} warning, error or critical record{(report.ParsedLines == 1 ? "" : "s")} retained. Counts are records, not unique names, servers, adapters, lookups or incidents.";
            if (patterns.Length > 0)
                detail += "\n" + string.Join("\n", patterns.Take(8).Select(group =>
                    $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} record{(group.Count() == 1 ? "" : "s")}."));
            if (patterns.Length > 8)
                detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            detail += "\nRaw event payload can contain queried names, resolver addresses, adapter or interface identities, namespace policy details and process context. Ordinary summaries exclude those values. Historical records do not establish a current DNS failure or its cause. Correlate exact records with current DNS server, client configuration, namespace policy, cache, route, gateway and adapter evidence. No cache flush, service action, adapter change, server change, registration change or policy change is selected from these records.";
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
                "DNS Client event XML could not be interpreted. Current resolver, server, cache, policy and connectivity state remain unverified.", Evidence: evidence);
        }
    }

    private static async Task<WorkflowCheck> CollectAsync(SourceDefinition definition,
        Func<RepairStep, CancellationToken, Task<ToolResult>> execute, CancellationToken token)
    {
        var step = new RepairStep("wevtutil.exe", ["qe", definition.Channel, "/q:" + definition.Query,
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        try
        {
            var task = execute(step, token) ?? throw new InvalidOperationException("DNS Client event provider returned no task.");
            var evidence = await task.ConfigureAwait(false)
                ?? throw new InvalidOperationException("DNS Client event provider returned no evidence.");
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
