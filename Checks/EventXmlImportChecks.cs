using KillerBench.Engine;
using System.IO;

internal static class EventXmlImportChecks
{
    internal static async Task RunAsync(string scratch)
    {
        const string entry = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-PrintService'/><EventID>372</EventID><EventRecordID>1</EventRecordID><Level>2</Level><TimeCreated SystemTime='2026-09-12T12:00:00Z'/></System></Event>";
        string path = Path.Combine(scratch, "print-events.xml");
        string xml = "<Events>\n" + entry + "\n</Events>";
        await File.WriteAllTextAsync(path, xml);
        var report = await Investigation.AnalyzeFileAsync(path);
        var imported = EventXmlImport.Import(await TextEvidence.ReadAsync(path, CancellationToken.None));
        if (imported.Evidence.Source != path || imported.Evidence.Lines.Length != 1 ||
            !imported.Evidence.Lines[0].Contains("<EventID>372</EventID>") ||
            imported.Report.Findings.SelectMany(f => f.Evidence).Any(reference => reference.LineNumber != 1 || reference.Source != path))
            throw new Exception("Normalized XML display and finding references disagree.");
        if (report.Source != path || report.ParsedLines != 1 || report.Partial ||
            !report.Findings.Any(f => f.RuleId == "KB-PRINT-EVENT-372-2") || !report.Limitation.Contains("first 200 records in file order") ||
            await File.ReadAllTextAsync(path) != xml)
            throw new Exception("Event XML file investigation lost findings, scope or source preservation.");
        var capped = EventXmlImport.Analyze(new(path, ["<Events>" + string.Concat(Enumerable.Repeat(entry, 201)) + "</Events>"], false));
        if (!capped.Partial || capped.ParsedLines != 200) throw new Exception("XML record limit is not explicit.");
        foreach (var invalid in new[] { new TextEvidence(path, ["<Events/>"], true),
            new(path, ["<!DOCTYPE Events [<!ENTITY x SYSTEM 'file:///unread-fixture'>]><Events>&x;</Events>"], false),
            new(path, ["<Other/>"], false), new(path, ["<Events>"], false) })
        {
            try { EventXmlImport.Analyze(invalid); throw new Exception("Invalid XML import accepted."); }
            catch (InvalidDataException) { }
        }
        try { EventXmlImport.Analyze(new(path, ["<Events/>"], false), new CancellationToken(true)); throw new Exception("Canceled XML import accepted."); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: exported event XML findings, record bounds, DTD rejection, cancellation and source preservation.");
    }
}
