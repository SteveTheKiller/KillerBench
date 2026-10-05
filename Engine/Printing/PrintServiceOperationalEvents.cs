using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Printing;

public static class PrintServiceOperationalEvents
{
    public const string Channel = "Microsoft-Windows-PrintService/Operational";
    private const string Provider = "Microsoft-Windows-PrintService";
    private static readonly XNamespace ConfigurationNamespace = "http://schemas.microsoft.com/win/2004/08/events";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("PrintService Operational event collection requires Windows.");
        execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: cancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var configuration = await execute(new RepairStep("wevtutil.exe", ["gl", Channel, "/f:xml"], true), deadline.Token)
                .WaitAsync(deadline.Token).ConfigureAwait(false);
            bool? enabled = ReadEnabled(configuration);
            if (enabled != true)
                return Interpret(configuration, null, token);
            var history = await execute(new RepairStep("wevtutil.exe", ["qe", Channel,
                "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]", "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true), deadline.Token)
                .WaitAsync(deadline.Token).ConfigureAwait(false);
            return Interpret(configuration, history, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return new("PrintService operational history", CheckState.Unknown,
                "PrintService Operational collection exceeded its 30-second deadline. Channel state and history coverage remain unverified. No logging, queue, job, driver, port, service or print setting changed.",
                Evidence: new ToolResult(-1, "", "PrintService Operational collection deadline expired.", false) { TimedOut = true });
        }
    }

    public static WorkflowCheck Interpret(ToolResult configuration, ToolResult? history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        token.ThrowIfCancellationRequested();
        const string name = "PrintService operational history";
        const string unchanged = " No logging, queue, job, driver, port, service or print setting changed.";
        if (configuration.ExitCode != 0 || configuration.TimedOut || configuration.Truncated || !string.IsNullOrWhiteSpace(configuration.Error))
            return new(name, CheckState.Unknown, "PrintService Operational channel configuration is unavailable or incomplete." + unchanged, Evidence: configuration);
        try
        {
            bool? enabled = ReadEnabled(configuration);
            if (enabled is null)
                return new(name, CheckState.Unknown, "PrintService Operational channel configuration has an unexpected structure." + unchanged, Evidence: configuration);
            if (!enabled.Value)
                return new(name, CheckState.Information,
                    "PrintService Operational logging is disabled, so job and rendering history is unavailable. This is a common Windows configuration and is not itself a printing fault. KillerBench did not enable logging or infer current printer health." + unchanged,
                    Evidence: configuration);
            if (history is null || history.ExitCode != 0 || history.TimedOut || history.Truncated || !string.IsNullOrWhiteSpace(history.Error))
                return new(name, CheckState.Unknown, "PrintService Operational logging is enabled, but its history is unavailable or incomplete." + unchanged,
                    Evidence: history ?? configuration);
            var retained = EventEvidence.FromXml(Channel, history.Output).Evidence
                ?? throw new InvalidDataException("Missing PrintService Operational history.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != Provider);
            var problems = report.Timeline.Where(item => item.Component == Provider && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level)).OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Operational logging is enabled. Last 7 days, newest first: {report.ParsedLines} record{(report.ParsedLines == 1 ? "" : "s")} retained; {problems.Length} warning, error or critical records. Counts describe records, not unique jobs, queues, users or failures.";
            if (patterns.Length > 0)
                detail += "\n" + string.Join("\n", patterns.Take(8).Select(group =>
                    $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} record{(group.Count() == 1 ? "" : "s")}."));
            if (patterns.Length > 8)
                detail += $"\n{patterns.Length - 8} additional event/level patterns remain in raw evidence.";
            detail += "\nRaw event XML can contain document titles, user names, printer and server names, ports, paths and job details. Ordinary summaries exclude those values. Historical records do not establish a current queue, driver or physical-printer failure and cannot authorize job deletion, driver replacement, queue changes or service recovery." + unchanged;
            if (retained.Truncated)
                detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid)
                detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information,
                detail, Evidence: history);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException)
        {
            return new(name, CheckState.Unknown, "PrintService Operational evidence could not be interpreted." + unchanged,
                Evidence: history ?? configuration);
        }
    }

    private static bool? ReadEnabled(ToolResult configuration)
    {
        if (configuration.ExitCode != 0 || configuration.TimedOut || configuration.Truncated || !string.IsNullOrWhiteSpace(configuration.Error))
            return null;
        try
        {
            using var reader = XmlReader.Create(new StringReader(configuration.Output), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = TextEvidence.MaximumCharacters });
            var root = XElement.Load(reader);
            if (root.Name != ConfigurationNamespace + "channel" || (string?)root.Attribute("name") != Channel ||
                (string?)root.Attribute("owningPublisher") != Provider)
                return null;
            return bool.TryParse((string?)root.Attribute("enabled"), out bool enabled) ? enabled : null;
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return null;
        }
    }
}
