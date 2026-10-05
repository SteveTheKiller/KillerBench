using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Storage;

public static class DiskDiagnosticEvents
{
    private sealed record ChannelDefinition(string Channel, string Provider, string Label);

    private static readonly ChannelDefinition[] Channels =
    [
        new("Microsoft-Windows-DiskDiagnostic/Operational", "Microsoft-Windows-DiskDiagnostic", "Disk diagnostic operational history"),
        new("Microsoft-Windows-DiskDiagnosticDataCollector/Operational", "Microsoft-Windows-DiskDiagnosticDataCollector", "Disk diagnostic collector history"),
        new("Microsoft-Windows-DiskDiagnosticResolver/Operational", "Microsoft-Windows-DiskDiagnosticResolver", "Disk diagnostic resolver history")
    ];

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Disk diagnostic event collection requires Windows.");
        execute ??= (step, stop) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(15), token: stop);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        var checks = new List<WorkflowCheck>(Channels.Length);
        try
        {
            foreach (var definition in Channels)
            {
                var configuration = await execute(CreateConfigurationStep(definition.Channel), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
                bool? enabled = ReadEnabled(definition, configuration);
                ToolResult? history = null;
                if (enabled == true)
                    history = await execute(CreateHistoryStep(definition.Channel), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
                checks.Add(Interpret(definition, configuration, history, token));
            }
            return checks.AsReadOnly();
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            checks.Add(new("Disk diagnostic event coverage", CheckState.Unknown,
                "Disk diagnostic event collection exceeded its 60-second deadline. Remaining channel coverage is incomplete. No logging, task, disk, file or storage setting changed.",
                Evidence: new ToolResult(-1, "", "Disk diagnostic event collection deadline expired.", false) { TimedOut = true }));
            return checks.AsReadOnly();
        }
    }

    public static RepairStep CreateConfigurationStep(string channel)
    {
        Definition(channel);
        return new("wevtutil.exe", ["gl", channel, "/f:xml"], true);
    }

    public static RepairStep CreateHistoryStep(string channel)
    {
        Definition(channel);
        return new("wevtutil.exe", ["qe", channel, "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
    }

    public static WorkflowCheck Interpret(string channel, ToolResult configuration, ToolResult? history,
        CancellationToken token = default) => Interpret(Definition(channel), configuration, history, token);

    private static WorkflowCheck Interpret(ChannelDefinition definition, ToolResult configuration, ToolResult? history,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        const string unchanged = " No logging, task, disk, file or storage setting changed.";
        bool? enabled = ReadEnabled(definition, configuration);
        if (enabled is null)
            return new(definition.Label, CheckState.Unknown,
                "The exact event channel configuration is unavailable, malformed or incomplete." + unchanged, Evidence: configuration);
        if (!enabled.Value)
            return new(definition.Label, CheckState.Information,
                "This disk diagnostic event channel is disabled, so its history is unavailable. Windows configuration or policy can intentionally disable it. No disk-health conclusion was made." + unchanged,
                Evidence: configuration);
        if (history is null || history.ExitCode != 0 || history.TimedOut || history.Truncated || !string.IsNullOrWhiteSpace(history.Error))
            return new(definition.Label, CheckState.Unknown,
                "The event channel is enabled, but its history is unavailable or incomplete." + unchanged, Evidence: history ?? configuration);
        try
        {
            var retained = EventEvidence.FromXml(definition.Channel, history.Output).Evidence
                ?? throw new InvalidDataException("Missing disk diagnostic history.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != definition.Provider);
            var problems = report.Timeline.Where(item => item.Component == definition.Provider && item.Level is "1" or "2" or "3").ToArray();
            var groups = problems.GroupBy(item => (item.EventId, item.Level)).OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).Take(8).ToArray();
            string detail = $"Operational logging is enabled. Last 7 days, newest first: {report.ParsedLines} record{(report.ParsedLines == 1 ? "" : "s")} retained; {problems.Length} warning, error or critical records.";
            if (groups.Length > 0)
                detail += "\n" + string.Join("\n", groups.Select(group =>
                    $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} record{(group.Count() == 1 ? "" : "s")}."));
            detail += "\nRaw event XML can contain disk, volume, device, path and hardware identifiers. Ordinary summaries exclude those values. Historical records do not establish current disk health, a failing component or repair eligibility." + unchanged;
            if (retained.Truncated)
                detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid)
                detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(definition.Label, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail, Evidence: history);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException)
        {
            return new(definition.Label, CheckState.Unknown,
                "Disk diagnostic event XML could not be interpreted." + unchanged, Evidence: history);
        }
    }

    private static bool? ReadEnabled(ChannelDefinition definition, ToolResult configuration)
    {
        if (configuration.ExitCode != 0 || configuration.TimedOut || configuration.Truncated || !string.IsNullOrWhiteSpace(configuration.Error))
            return null;
        try
        {
            using var reader = XmlReader.Create(new StringReader(configuration.Output), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 });
            var root = XElement.Load(reader);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events";
            if (root.Name != ns + "channel" || (string?)root.Attribute("name") != definition.Channel ||
                (string?)root.Attribute("owningPublisher") != definition.Provider)
                return null;
            return bool.TryParse((string?)root.Attribute("enabled"), out bool enabled) ? enabled : null;
        }
        catch (XmlException) { return null; }
    }

    private static ChannelDefinition Definition(string channel) => Channels.SingleOrDefault(item => item.Channel == channel)
        ?? throw new ArgumentException("Unsupported disk diagnostic event channel.", nameof(channel));
}
