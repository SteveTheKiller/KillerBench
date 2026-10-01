using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Security;

public static class AccountBrokerEvents
{
    private const string Channel = "Microsoft-Windows-AAD/Operational";
    private const string Provider = "Microsoft-Windows-AAD";
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Account broker event collection requires Windows.");
        var step = new RepairStep("wevtutil.exe", ["qe", Channel,
            "/q:*[System[TimeCreated[timediff(@SystemTime) <= 604800000]]]", "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true);
        var raw = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(Sanitize(raw), token);
    }

    public static ToolResult Sanitize(ToolResult raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.ExitCode != 0 || raw.TimedOut || raw.Truncated || !string.IsNullOrWhiteSpace(raw.Error))
            return raw with { Output = "" };
        try
        {
            using var text = new StringReader(raw.Output);
            using var reader = XmlReader.Create(text, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 4 * 1024 * 1024,
                MaxCharactersFromEntities = 0,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            });
            var document = XDocument.Load(reader, LoadOptions.None);
            if (document.Root?.Name.LocalName != "Events") throw new InvalidDataException("Unexpected event root.");
            foreach (var eventElement in document.Root.Elements().ToArray())
            {
                if (eventElement.Name != Ns + "Event") throw new InvalidDataException("Unexpected event element.");
                var system = eventElement.Elements(Ns + "System").SingleOrDefault()
                    ?? throw new InvalidDataException("Event system identity was unavailable.");
                var provider = system.Elements(Ns + "Provider").SingleOrDefault()
                    ?? throw new InvalidDataException("Event provider identity was unavailable.");
                var id = system.Elements(Ns + "EventID").SingleOrDefault()
                    ?? throw new InvalidDataException("Event ID was unavailable.");
                var level = system.Elements(Ns + "Level").SingleOrDefault()
                    ?? throw new InvalidDataException("Event level was unavailable.");
                var time = system.Elements(Ns + "TimeCreated").SingleOrDefault()
                    ?? throw new InvalidDataException("Event time was unavailable.");
                var record = system.Elements(Ns + "EventRecordID").SingleOrDefault()
                    ?? throw new InvalidDataException("Event record identity was unavailable.");
                eventElement.RemoveNodes();
                eventElement.Add(new XElement(Ns + "System",
                    new XElement(provider), new XElement(id), new XElement(level), new XElement(time), new XElement(record)));
            }
            return raw with { Output = document.ToString(SaveOptions.DisableFormatting) };
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException)
        {
            return raw with { Output = "", Error = "Account broker event metadata could not be sanitized." };
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Recent account broker events";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Account broker history is unavailable or incomplete. No account, token, authentication or repair state was inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(Channel, evidence.Output).Evidence
                ?? throw new InvalidDataException("Missing account broker evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate ||
                report.Timeline.Any(item => item.Component != Provider);
            var problems = report.Timeline.Where(item => item.Component == Provider && item.Level is "1" or "2" or "3").ToArray();
            var patterns = problems.GroupBy(item => (item.EventId, item.Level)).OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 7 days, newest first: {report.ParsedLines} record{(report.ParsedLines == 1 ? "" : "s")} retained; {problems.Length} warning, error or critical records. Counts are historical events, not unique accounts, requests or authentication attempts.";
            if (patterns.Length > 0)
                detail += "\n" + string.Join("\n", patterns.Take(8).Select(group => $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (patterns.Length > 8) detail += $"\n{patterns.Length - 8} additional event/level patterns remain in sanitized evidence.";
            detail += "\nEvent payload, computer identity, security identity, correlation data and process data are removed before evidence is retained. Provider, event ID, level, time and record ID remain. Historical records do not establish current account validity, token availability, authentication, connectivity, service health or application access. No account, credential, token, cookie, registration, permission, service or policy action is selected.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : problems.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return new(name, CheckState.Unknown, "Sanitized account broker event XML could not be interpreted. Current account, token and authentication state remain unverified.", Evidence: evidence);
        }
    }
}
