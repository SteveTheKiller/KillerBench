using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine;

public static class BootPerformanceEvents
{
    private const string Channel = "Microsoft-Windows-Diagnostics-Performance/Operational";
    private const string Provider = "Microsoft-Windows-Diagnostics-Performance";
    private static readonly HashSet<string> SafeDurations = new(StringComparer.Ordinal)
    {
        "BootDuration", "MainPathBootTime", "BootPostBootTime", "ShutdownDuration"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Startup performance event collection requires Windows.");
        var raw = await BoundedTool.RunAsync(new("wevtutil.exe", ["qe", Channel,
            "/q:*[System[Provider[@Name='Microsoft-Windows-Diagnostics-Performance'] and TimeCreated[timediff(@SystemTime) <= 1209600000]]]",
            "/rd:true", "/c:201", "/f:xml", "/e:Events", "/uni:false"], true),
            TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(Sanitize(raw), token);
    }

    public static ToolResult Sanitize(ToolResult raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.ExitCode != 0 || raw.TimedOut || raw.Truncated || !string.IsNullOrWhiteSpace(raw.Error)) return raw with { Output = "" };
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = 4 * 1024 * 1024, MaxCharactersFromEntities = 0,
                IgnoreComments = true, IgnoreProcessingInstructions = true
            };
            using var text = new StringReader(raw.Output);
            using var reader = XmlReader.Create(text, settings);
            var document = XDocument.Load(reader, LoadOptions.None);
            if (document.Root?.Name.LocalName != "Events") throw new InvalidDataException("Unexpected event root.");
            foreach (var eventElement in document.Root.Elements().ToArray())
            {
                if (eventElement.Name.LocalName != "Event") throw new InvalidDataException("Unexpected event element.");
                var systems = eventElement.Elements().Where(element => element.Name.LocalName == "System").ToArray();
                if (systems.Length != 1) throw new InvalidDataException("Event system identity was unavailable.");
                XNamespace ns = eventElement.Name.Namespace;
                var safe = eventElement.Descendants().Where(element => element.Name.LocalName == "Data")
                    .Select(element => (Name: (string?)element.Attribute("Name"), Value: element.Value.Trim()))
                    .Where(item => item.Name is not null && SafeDurations.Contains(item.Name) &&
                        long.TryParse(item.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long value) && value is >= 0 and <= 86_400_000)
                    .GroupBy(item => item.Name!, StringComparer.Ordinal).Select(group => group.SingleOrDefault())
                    .Where(item => item.Name is not null).ToArray();
                eventElement.RemoveNodes();
                eventElement.Add(new XElement(systems[0]));
                if (safe.Length > 0)
                    eventElement.Add(new XElement(ns + "EventData", safe.Select(item =>
                        new XElement(ns + "Data", new XAttribute("Name", item.Name!), item.Value))));
            }
            return raw with { Output = document.ToString(SaveOptions.DisableFormatting) };
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or InvalidOperationException)
        {
            return raw with { Output = "", Error = "Startup performance event metadata could not be sanitized." };
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Startup and shutdown performance";
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(name, CheckState.Unknown, "Startup performance history is unavailable or incomplete. Current performance and repair eligibility were not inferred.", Evidence: evidence);
        try
        {
            var retained = EventEvidence.FromXml(Channel, evidence.Output).Evidence ?? throw new InvalidDataException("Missing startup performance evidence.");
            var report = EventEvidence.Analyze(retained, token);
            bool duplicate = report.Timeline.Select(item => (item.Timestamp, item.EventId, item.Message)).Distinct().Count() != report.Timeline.Count;
            bool invalid = report.Partial || report.ParsedLines != retained.Lines.Length || duplicate || report.Timeline.Any(item => item.Component != Provider);
            var review = report.Timeline.Where(item => item.Level is "1" or "2" or "3").ToArray();
            var groups = review.GroupBy(item => (item.EventId, item.Level)).OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.EventId).ThenBy(group => group.Key.Level).ToArray();
            string detail = $"Last 14 days, newest first: {report.ParsedLines} record{(report.ParsedLines == 1 ? "" : "s")} retained; {review.Length} warning, error or critical records. Event IDs 100 through 199 describe startup observations; 200 through 299 describe shutdown observations.";
            if (groups.Length > 0) detail += "\n" + string.Join("\n", groups.Take(10).Select(group => $"Event {group.Key.EventId}, level {group.Key.Level}: {group.Count()} records."));
            if (groups.Length > 10) detail += $"\n{groups.Length - 10} additional event and level patterns remain in sanitized evidence.";
            detail += "\nOnly provider metadata and bounded numeric duration fields are retained. Paths, process names, accounts and other event payload are removed. Historical degradation records do not identify a current bottleneck or authorize startup, service, application, driver, task, power or policy changes.";
            if (retained.Truncated) detail += "\nThe 200-event limit omitted older records; counts cover retained evidence only.";
            else if (invalid) detail += "\nSome records were malformed, duplicated or unexpected; coverage is incomplete.";
            return new(name, invalid ? CheckState.Unknown : review.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return new(name, CheckState.Unknown, "Sanitized startup performance event XML could not be interpreted. Current performance remains unverified.", Evidence: evidence);
        }
    }
}
