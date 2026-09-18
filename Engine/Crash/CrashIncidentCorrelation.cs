using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine;

public sealed record CrashIncident(DateTimeOffset OccurredUtc, ulong EventRecordId, string? BugCheckCode,
    string? ReportedDumpPath, CrashDumpFile? DumpFile, MinidumpSummary? DumpAnalysis);

public sealed record CrashIncidentResult(IReadOnlyList<CrashIncident> Incidents, bool Complete,
    int ReportedPaths, int MatchedFiles, int AnalyzedFiles);

public static class CrashIncidentCorrelation
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static WorkflowCheck Check(ToolResult events, CrashDumpInventoryResult inventory,
        IReadOnlyDictionary<string, MinidumpSummary> analyses, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(analyses);
        token.ThrowIfCancellationRequested();
        const string name = "Crash incident correlation";
        var history = CrashEvents.Interpret(events, token);
        if (history.State == CheckState.Unknown)
            return new(name, CheckState.Unknown, "Crash history is incomplete. Incident correlation was not attempted.", Evidence: events);
        try
        {
            var source = EventEvidence.FromXml("System", events.Output);
            if (source.Evidence is null || source.Evidence.Truncated)
                return new(name, CheckState.Unknown, "Crash history is incomplete. Incident correlation was not attempted.", Evidence: events);
            var incidents = new List<CrashIncident>();
            var recordIds = new HashSet<ulong>();
            bool complete = inventory.Complete;
            int reported = 0, matched = 0, analyzed = 0;
            foreach (string line in source.Evidence.Lines)
            {
                token.ThrowIfCancellationRequested();
                var entry = XElement.Parse(line);
                var system = entry.Element(Ns + "System")!;
                DateTimeOffset timestamp = XmlConvert.ToDateTimeOffset((string)system.Element(Ns + "TimeCreated")!.Attribute("SystemTime")!);
                if (!ulong.TryParse((string?)system.Element(Ns + "EventRecordID"), NumberStyles.None,
                    CultureInfo.InvariantCulture, out ulong recordId) || recordId == 0 || !recordIds.Add(recordId))
                { complete = false; continue; }
                var fields = (entry.Element(Ns + "EventData")?.Elements(Ns + "Data") ?? [])
                    .Concat(entry.Element(Ns + "UserData")?.Descendants().Where(value => !value.HasElements) ?? []).ToArray();
                var codes = fields.Where(value => (string?)value.Attribute("Name") == "param1")
                    .Select(value => NormalizeCode(value.Value)).Where(value => value is not null).Distinct(StringComparer.Ordinal).ToArray();
                if (codes.Length != 1) complete = false;
                var paths = fields.Select(value => value.Value).Where(IsDumpPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (paths.Length > 1) complete = false;
                string? reportedPath = paths.Length == 1 ? paths[0] : null;
                if (reportedPath is not null) reported++;
                CrashDumpFile? dump = reportedPath is null ? null : inventory.Files.SingleOrDefault(file =>
                    string.Equals(file.Path, reportedPath, StringComparison.OrdinalIgnoreCase));
                if (dump is not null) matched++;
                MinidumpSummary? analysis = null;
                if (dump is not null && TryGet(analyses, dump.Path, out analysis)) analyzed++;
                incidents.Add(new(timestamp, recordId, codes.SingleOrDefault(), reportedPath, dump, analysis));
            }
            var ordered = incidents.OrderByDescending(incident => incident.OccurredUtc).ThenByDescending(incident => incident.EventRecordId).ToArray();
            var result = new CrashIncidentResult(Array.AsReadOnly(ordered), complete, reported, matched, analyzed);
            string detail = $"Correlated {ordered.Length} retained crash report(s): {reported} reported dump path(s), {matched} exact file match(es), and {analyzed} bounded structural analysis result(s).";
            foreach (var incident in ordered.Take(8))
            {
                string file = incident.ReportedDumpPath is null ? "no reported dump path" :
                    Path.GetFileName(incident.ReportedDumpPath) + (incident.DumpFile is null ? " (availability unverified)" : " (metadata matched)");
                string structure = incident.DumpAnalysis is null ? "structure not analyzed" :
                    incident.DumpAnalysis.Format + (incident.DumpAnalysis.ExceptionCode is null ? "" : ", exception " + incident.DumpAnalysis.ExceptionCode) +
                    (incident.DumpAnalysis.FaultModule is null ? "" : ", containing module " + incident.DumpAnalysis.FaultModule);
                detail += $"\n{incident.OccurredUtc:O} | record {incident.EventRecordId} | {incident.BugCheckCode ?? "code unavailable"} | {file} | {structure}.";
            }
            if (ordered.Length > 8) detail += "\nAdditional incidents remain in retained JSON evidence.";
            if (!complete) detail += "\nCoverage is incomplete because an input was partial, ambiguous or duplicated.";
            detail += "\nOnly an exact reported path can link a file to an event. Timestamps are not used to guess a match. Reused paths can refer to overwritten files. Structural metadata does not identify root cause, driver responsibility or repair eligibility. Memory pages, registers, stacks and symbols are not read, and no repair is selected.";
            return new(name, complete ? CheckState.Information : CheckState.Unknown, detail,
                Evidence: new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(result, Capture.JsonOptions), "", false));
        }
        catch (Exception error) when (error is XmlException or InvalidDataException or FormatException or InvalidOperationException or ArgumentException)
        {
            return new(name, CheckState.Unknown, "Crash incident evidence could not be correlated safely. No timestamp or filename match was inferred.", Evidence: events);
        }
    }

    private static bool TryGet(IReadOnlyDictionary<string, MinidumpSummary> analyses, string path, out MinidumpSummary? summary)
    {
        foreach (var item in analyses)
            if (string.Equals(item.Key, path, StringComparison.OrdinalIgnoreCase)) { summary = item.Value; return true; }
        summary = null;
        return false;
    }

    private static string? NormalizeCode(string value)
    {
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) value = value[2..];
        return value.Length is >= 1 and <= 8 && uint.TryParse(value, NumberStyles.AllowHexSpecifier,
            CultureInfo.InvariantCulture, out uint number) ? "0x" + number.ToString("X8", CultureInfo.InvariantCulture) : null;
    }

    private static bool IsDumpPath(string value) => value.Length is >= 7 and <= 32767 &&
        char.IsAsciiLetter(value[0]) && value[1] == ':' && value[2] == '\\' &&
        value.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase) &&
        !value.Skip(2).Any(character => char.IsControl(character) || character is ':' or '%' or '*' or '?' or '"' or '<' or '>' or '|');
}
