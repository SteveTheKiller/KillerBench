using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine;

public static class CrashBugCheckReferences
{
    private static readonly XNamespace EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static WorkflowCheck Check(ToolResult evidence, KnowledgeCatalog catalog, CancellationToken token = default)
        => Check(evidence, new KnowledgeCatalogCollection([catalog]), token);

    public static WorkflowCheck Check(ToolResult evidence, KnowledgeCatalogCollection catalogs,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(catalogs);
        token.ThrowIfCancellationRequested();
        const string name = "Bug-check code references";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Bug-check reference coverage is unavailable because crash-event evidence is incomplete.");
        try
        {
            var source = EventEvidence.FromXml("System", evidence.Output);
            if (source.Evidence is null || source.Evidence.Truncated)
                return Unknown("Bug-check reference coverage is unavailable because crash-event evidence is incomplete.");
            var codes = new List<string>();
            foreach (string line in source.Evidence.Lines)
            {
                token.ThrowIfCancellationRequested();
                var entry = Read(line);
                var system = entry.Element(EventNamespace + "System");
                var data = entry.Element(EventNamespace + "EventData")?.Elements(EventNamespace + "Data")
                    .Where(field => (string?)field.Attribute("Name") == "param1").ToArray() ?? [];
                if (entry.Name != EventNamespace + "Event" || system is null ||
                    (string?)system.Element(EventNamespace + "Provider")?.Attribute("Name") != CrashEvents.Provider ||
                    (string?)system.Element(EventNamespace + "EventID") != "1001" ||
                    (string?)system.Element(EventNamespace + "Level") != "2" || data.Length != 1 ||
                    !TryNormalize(data[0].Value, out string code))
                    return Unknown("At least one crash report has an unsupported or missing bug-check code. No partial code-reference result is presented.");
                codes.Add(code);
            }
            if (codes.Count == 0)
                return new(name, CheckState.Information,
                    $"No bug-check codes were present in the retained crash reports. {catalogs.Count} session knowledge pack(s) were available.",
                    Evidence: evidence);
            var groups = codes.GroupBy(code => code, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).ToArray();
            var resolved = groups.Select(group => (Group: group, Match: catalogs.Resolve("bugcheck", group.Key))).ToArray();
            if (resolved.Any(item => item.Match.Ambiguous))
                return Unknown("More than one session knowledge pack defines the same bug-check identity. Remove or replace the conflicting packs before using automatic references.");
            var lines = resolved.Take(8).Select(item =>
            {
                var reference = item.Match.Entry;
                return reference is null
                    ? $"{item.Group.Key} | {item.Group.Count()} report(s) | No exact reference in the session packs."
                    : $"{item.Group.Key} | {reference.Title} | {item.Group.Count()} report(s) | Pack {item.Match.PackId} revision {item.Match.Revision} | {reference.SourceUrl}";
            });
            string detail = $"{codes.Count} crash report(s), {groups.Length} distinct bug-check code(s), {catalogs.Count} session knowledge pack(s).\n" +
                string.Join("\n", lines);
            if (groups.Length > 8) detail += $"\n{groups.Length - 8} additional distinct code(s) are retained in the event evidence.";
            detail += "\nExact code references identify Microsoft bug-check pages only. They do not analyze dump parameters, identify a cause or authorize a repair.";
            return new(name, CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return Unknown("Bug-check reference coverage is unavailable because crash-event evidence is malformed.");
        }
    }

    private static XElement Read(string text)
    {
        using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = TextEvidence.MaximumCharacters
        });
        return XElement.Load(reader);
    }

    private static bool TryNormalize(string value, out string code)
    {
        code = string.Empty;
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) value = value[2..];
        if (value.Length is < 1 or > 8 || !uint.TryParse(value, NumberStyles.AllowHexSpecifier,
            CultureInfo.InvariantCulture, out uint number))
            return false;
        code = "0x" + number.ToString("X8", CultureInfo.InvariantCulture);
        return true;
    }
}
