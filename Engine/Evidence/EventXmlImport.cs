using System.Xml;

namespace KillerBench.Engine.Evidence;

public sealed record EventXmlImportResult(TextEvidence Evidence, InvestigationReport Report);

public static class EventXmlImport
{
    public static InvestigationReport Analyze(TextEvidence text, CancellationToken token = default)
        => Import(text, token).Report;

    public static EventXmlImportResult Import(TextEvidence text, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        token.ThrowIfCancellationRequested();
        if (text.Truncated) throw new InvalidDataException("Event XML exceeds the text import limit. Export a smaller event selection.");
        try
        {
            var imported = EventEvidence.FromXml(text.Source, string.Join("\n", text.Lines)).Evidence!;
            token.ThrowIfCancellationRequested();
            var normalized = new TextEvidence(text.Source, imported.Lines, imported.Truncated);
            var report = EventEvidence.Analyze(normalized, token);
            return new(normalized, report with { Limitation = report.Limitation +
                " Imported XML: first 200 records in file order, with no date filter or sorting. Evidence line numbers identify retained event records, not original XML text lines. The file may describe another machine or earlier state." });
        }
        catch (XmlException error) { throw new InvalidDataException("Invalid Windows event XML. An Events wrapper containing Windows Event elements is required.", error); }
    }
}
