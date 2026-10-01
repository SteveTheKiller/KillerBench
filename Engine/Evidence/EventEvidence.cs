using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace KillerBench.Engine.Evidence;

public static class EventEvidence
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";
    private static readonly string[] RequiredSystemElements = ["Provider", "TimeCreated", "EventID", "EventRecordID", "Level"];
    private static XElement Read(string text)
    {
        using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings
        { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = TextEvidence.MaximumCharacters });
        return XElement.Load(reader);
    }

    public static CaptureSource FromXml(string channel, string xml)
    {
        var root = Read(xml);
        if (root.Name != "Events" || root.Elements().Any(e => e.Name != Ns + "Event"))
            throw new InvalidDataException("Unexpected Windows event XML structure.");
        var events = root.Elements(Ns + "Event").ToArray();
        bool partial = events.Length > 200;
        var lines = events.Take(200).Select(e => e.ToString(SaveOptions.DisableFormatting).Replace("\r", "&#xD;").Replace("\n", "&#xA;")).ToArray();
        if (lines.Sum(l => (long)l.Length + 1) > TextEvidence.MaximumCharacters) throw new InvalidDataException("Event snapshot is too large.");
        return new(channel, "Events", partial ? CollectionStatus.Truncated : CollectionStatus.Collected,
            "Newest events first, last 7 days, at most 200 events. " + (partial ? "Older matching events omitted." : $"{lines.Length} events returned. An empty result does not establish health."),
            new("events:" + channel, lines, partial));
    }

    public static InvestigationReport Analyze(TextEvidence evidence, CancellationToken cancellationToken = default)
    {
        var timeline = new List<ParsedLogEntry>();
        var errors = new List<EvidenceReference>();
        var serviceErrors = new Dictionary<string, List<EvidenceReference>>();
        bool partial = evidence.Truncated;
        for (int i = 0; i < Math.Min(evidence.Lines.Length, 200); i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var entry = Read(evidence.Lines[i]);
                var system = entry.Element(Ns + "System");
                if (entry.Name != Ns + "Event" || system is null || entry.Elements(Ns + "System").Count() != 1 ||
                    RequiredSystemElements.Any(name => system.Elements(Ns + name).Count() != 1))
                { partial = true; continue; }
                string provider = (string?)system.Element(Ns + "Provider")?.Attribute("Name") ?? "Unknown";
                string time = (string?)system.Element(Ns + "TimeCreated")?.Attribute("SystemTime") ?? "Unknown";
                string id = (string?)system.Element(Ns + "EventID") ?? "Unknown";
                string record = (string?)system.Element(Ns + "EventRecordID") ?? "Unknown";
                string level = (string?)system.Element(Ns + "Level") ?? "Unknown";
                if (!DriverReference.ValidText(provider, 256) || provider != provider.Trim() ||
                    !ushort.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out ushort eventId) ||
                    !ulong.TryParse(record, NumberStyles.None, CultureInfo.InvariantCulture, out ulong recordId) || recordId == 0 ||
                    level is not ("0" or "1" or "2" or "3" or "4" or "5"))
                { partial = true; continue; }
                try { _ = XmlConvert.ToDateTimeOffset(time); }
                catch (FormatException) { partial = true; continue; }
                timeline.Add(new(i + 1, time, level, provider, $"Event {id}, record {record}. See raw XML for payload.") { EventId = eventId });
                if (provider == "Service Control Manager" && level is "1" or "2" &&
                    id is "7000" or "7001" or "7003" or "7009" or "7011" or "7023" or "7024" or "7031" or "7034")
                {
                    if (!serviceErrors.TryGetValue(id, out var references)) serviceErrors[id] = references = [];
                    references.Add(new(evidence.Source, i + 1, $"{time} | Event {id} | Record {record}"));
                }
                if (level is "1" or "2" && provider is "Microsoft-Windows-WindowsUpdateClient" or "Microsoft-Windows-Servicing")
                    errors.Add(new(evidence.Source, i + 1, $"{time} | {provider} | Event {id} | Record {record}"));
            }
            catch (XmlException) { partial = true; }
        }
        var findings = new List<DiagnosticFinding>();
        findings.AddRange(StorageEvents.Findings(timeline, evidence.Source));
        findings.AddRange(InstallerEvents.Findings(timeline, evidence.Source));
        findings.AddRange(PrintServiceEvents.Findings(timeline, evidence.Source));
        foreach (var group in timeline.Where(entry =>
            (entry.Component == "Microsoft-Windows-Kernel-Power" && entry.EventId == 41 && entry.Level == "1") ||
            (entry.Component == "EventLog" && entry.EventId == 6008 && entry.Level == "2"))
            .GroupBy(entry => entry.EventId).OrderBy(group => group.Key))
        {
            findings.Add(new("KB-SHUTDOWN-" + group.Key, 1, "Unexpected shutdown recorded", "Warning", "Recorded event",
                "Windows recorded an unclean shutdown. These records do not identify its cause, establish a current fault or count unique shutdown incidents.",
                "Correlate nearby shutdown, restart and bugcheck records with the reported incident time. Review retained dump evidence and the event payload before attributing the interruption to power, hardware or a driver. Do not select a repair from this event alone.",
                "https://learn.microsoft.com/en-us/troubleshoot/windows-server/performance/troubleshoot-unexpected-reboots-system-event-logs",
                group.Count(), [.. group.Take(8).Select(entry => new EvidenceReference(evidence.Source, entry.LineNumber,
                    entry.Timestamp + " | " + entry.Component + " | " + entry.Message))]));
        }
        foreach (var group in serviceErrors.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var (title, next) = group.Key switch
            {
                "7001" => ("Service dependency failures recorded", "Inspect the named dependency and its current state before the dependent service. Preserve managed startup settings."),
                "7003" => ("Missing or invalid service dependencies recorded", "Compare the named dependency with the installed service inventory and product configuration. Do not remove dependency entries based on this event alone."),
                "7009" => ("Service connection timeouts recorded", "Inspect the named service, its executable and dependencies, then compare later start events and current state. Preserve the global service timeout unless component-specific evidence supports a change."),
                "7011" => ("Service control response timeouts recorded", "Inspect the named service process and its dependencies around the recorded time. Confirm current responsiveness before considering a restart or product repair."),
                "7023" => ("Services terminated with errors", "Review the named service and reported error code with its product logs, dependencies and current state. Do not infer missing files or permissions from the event ID alone."),
                "7024" => ("Services terminated with service-specific errors", "Interpret the service-specific code in the context of the named component, then inspect its current state and product logs before selecting recovery."),
                "7031" => ("Unexpected service terminations with recovery actions recorded", "Review the named service, recurrence count, configured recovery action and nearby application failures. Confirm its current state before changing recovery settings."),
                "7034" => ("Unexpected service terminations recorded", "Review the named service and nearby process or application failures, then confirm its current state. This record alone does not identify why the process ended."),
                _ => ("Service startup failures recorded", "Inspect the reported error and the service executable configuration. A startup failure alone does not establish missing files or justify changing permissions.")
            };
            findings.Add(new("KB-SCM-" + group.Key, 1, title, "Warning", "Recorded event",
                "Service Control Manager recorded this failure in the captured history. The service may have recovered; current state and cause remain unverified.", next,
                "https://learn.microsoft.com/en-us/troubleshoot/windows-server/windows-security/troubleshoot-netlogon-service-startup-failures",
                group.Value.Count, [.. group.Value.Take(8)]));
        }
        if (errors.Count > 0) findings.Add(new("KB-EVENT-001", 1, "Update or servicing error events recorded", "Warning", "Recorded event",
            "Windows Update or Servicing emitted error/critical events in the captured window. These are investigation leads, not a proven cause or a current fault.",
            "Review the raw event payload and nearby successful events alongside CBS and Setup logs.",
            "https://learn.microsoft.com/en-us/windows/deployment/update/windows-update-logs", errors.Count, [.. errors.Take(8)]));
        return new("1", evidence.Source, "Windows event XML", Math.Min(evidence.Lines.Length, 200), timeline.Count, partial || evidence.Lines.Length > 200,
            timeline, findings, LogAnalyzer.Analyze(evidence, cancellationToken),
            "Event SystemTime is retained as recorded (normally UTC). Newest events first; limited capture window. No repairs executed and no health verdict.");
    }
}
