using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine;

public static class PrintQueueEventCorrelation
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> queueInventory, WorkflowCheck history,
        CancellationToken token = default)
    {
        const string name = "Print queue event correlation";
        ToolResult? retained = history.Evidence;
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + Scope(), Evidence: retained);
        token.ThrowIfCancellationRequested();
        if (history.Name != "Recent print-service events" || history.State == CheckState.Unknown || retained is not { } eventEvidence ||
            eventEvidence.ExitCode != 0 || eventEvidence.TimedOut || eventEvidence.Truncated || !string.IsNullOrWhiteSpace(eventEvidence.Error))
            return Unknown("Complete recent PrintService evidence is required before queue identities can be correlated.");
        var queueEvidence = queueInventory.Select(check => check.Evidence).Where(evidence => evidence is not null).Distinct().ToArray();
        if (queueInventory.Count == 0 || queueInventory.Any(check => check.State == CheckState.Unknown) || queueEvidence.Length != 1 ||
            queueEvidence[0] is not { ExitCode: 0, TimedOut: false, Truncated: false } inventoryEvidence || !string.IsNullOrWhiteSpace(inventoryEvidence.Error))
            return Unknown("A complete unambiguous queue inventory is required before event identities can be correlated.");
        try
        {
            using var json = JsonDocument.Parse(inventoryEvidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            if (!json.RootElement.TryGetProperty("queues", out var queues) || queues.ValueKind != JsonValueKind.Array || queues.GetArrayLength() > 256)
                return Unknown("Queue inventory is missing or exceeds its correlation bound.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var queue in queues.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (queue.ValueKind != JsonValueKind.Object || !queue.TryGetProperty("Name", out var value) || value.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(value.GetString(), 512) || !names.Add(value.GetString()!))
                    return Unknown("Queue identities are missing, invalid or duplicated.");
            }
            using var reader = XmlReader.Create(new StringReader(eventEvidence.Output), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = TextEvidence.MaximumCharacters });
            var root = XElement.Load(reader);
            if (root.Name != "Events" || root.Elements().Any(element => element.Name != Ns + "Event"))
                return Unknown("PrintService event XML has an unexpected structure.");
            int problems = 0, unresolved = 0;
            var matched = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in root.Elements(Ns + "Event"))
            {
                token.ThrowIfCancellationRequested();
                var system = item.Element(Ns + "System");
                if (system is null || (string?)system.Element(Ns + "Provider")?.Attribute("Name") != "Microsoft-Windows-PrintService")
                    return Unknown("Unexpected provider evidence prevents PrintService correlation.");
                string? level = (string?)system.Element(Ns + "Level");
                if (level is not ("1" or "2" or "3")) continue;
                problems++;
                if (!ushort.TryParse((string?)system.Element(Ns + "EventID"), NumberStyles.None, CultureInfo.InvariantCulture, out ushort eventId) ||
                    !ulong.TryParse((string?)system.Element(Ns + "EventRecordID"), NumberStyles.None, CultureInfo.InvariantCulture, out ulong recordId) || recordId == 0)
                    return Unknown("A retained PrintService problem has an invalid event identity.");
                var values = item.Element(Ns + "EventData")?.Elements(Ns + "Data").Select(value => value.Value).ToArray() ?? [];
                var matches = values.Where(names.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (matches.Length != 1) { unresolved++; continue; }
                if (!matched.TryGetValue(matches[0], out var records)) matched[matches[0]] = records = [];
                records.Add("event " + eventId + ", record " + recordId);
            }
            if (problems == 0)
                return new(name, CheckState.Information, "No warning, error or critical PrintService records required queue correlation." + Scope(), Evidence: eventEvidence);
            string detail = problems + " PrintService problem records reviewed; " + (problems - unresolved) + " matched one exact current queue and " + unresolved + " remained unresolved.\n" +
                string.Join("\n", matched.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair =>
                    pair.Key + ": " + pair.Value.Count + " retained records (" + string.Join(", ", pair.Value.Take(8)) + ")."));
            return new(name, CheckState.Attention, detail + Scope(), Evidence: eventEvidence);
        }
        catch (Exception error) when (error is JsonException or XmlException or InvalidDataException or InvalidOperationException)
        { return Unknown("Queue and PrintService evidence could not be correlated safely."); }
    }

    private static string Scope() => " Exact payload matches are historical review context, not proof that a queue is currently broken. Unmatched records can refer to removed, remote or differently formatted queues. No queue, job, driver, port, service or print setting is changed.";
}
