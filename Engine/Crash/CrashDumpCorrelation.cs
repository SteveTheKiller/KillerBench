using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Crash;

public static class CrashDumpCorrelation
{
    public static WorkflowCheck Check(ToolResult events, CrashDumpInventoryResult inventory, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Reported dump file matches";
        var history = CrashEvents.Interpret(events, token);
        if (history.State == CheckState.Unknown)
            return new(name, CheckState.Unknown, "Crash history is incomplete. Dump matching was not attempted.", Evidence: events);
        try
        {
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var source = EventEvidence.FromXml("System", events.Output).Evidence!;
            var details = new List<string>();
            int reported = 0, matched = 0;
            foreach (string line in source.Lines)
            {
                token.ThrowIfCancellationRequested();
                var root = XElement.Parse(line);
                var fields = (root.Element(ns + "EventData")?.Elements(ns + "Data") ?? [])
                    .Concat(root.Element(ns + "UserData")?.Descendants().Where(e => !e.HasElements) ?? []);
                foreach (string path in fields.Select(e => e.Value).Where(IsDumpPath).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    reported++;
                    bool found = inventory.Files.Any(file => string.Equals(file.Path, path, StringComparison.OrdinalIgnoreCase));
                    if (found) matched++;
                    if (details.Count < 8) details.Add(path + (found ? " | Metadata collected." : " | Not in collected inventory; availability unverified."));
                }
            }
            string detail = $"{matched} of {reported} reported path references match collected file metadata.\n" + string.Join("\n", details);
            if (reported > details.Count) detail += "\nAdditional references remain in raw event evidence.";
            if (!inventory.Complete) detail += "\nFile inventory coverage is incomplete.";
            detail += "\nOnly exact absolute path values ending in .dmp are compared, ignoring case. Other payload formats are not interpreted. " +
                "No files are opened. Matching paths do not prove file validity or that the file belongs to that event. " +
                "Repeated paths can refer to overwritten dumps. Unmatched paths do not establish missing files or an absence of crashes.";
            return new(name, inventory.Complete ? CheckState.Information : CheckState.Unknown, detail, Evidence: events);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        { return new(name, CheckState.Unknown, "Crash evidence could not be matched to the file inventory.", Evidence: events); }
    }

    private static bool IsDumpPath(string value) => value.Length is >= 7 and <= 32767 &&
        char.IsAsciiLetter(value[0]) && value[1] == ':' && value[2] == '\\' &&
        value.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase) &&
        !value.Skip(2).Any(c => char.IsControl(c) || c is ':' or '%' or '*' or '?' or '"' or '<' or '>' or '|');
}
