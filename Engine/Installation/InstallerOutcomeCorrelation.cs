using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Installation;

public static class InstallerOutcomeCorrelation
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static WorkflowCheck Check(WorkflowCheck history, CancellationToken token = default)
    {
        const string name = "Installer attempt correlation";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + Scope(), Evidence: history.Evidence);
        token.ThrowIfCancellationRequested();
        if (history.Name != "Recent installer events" || history.State == CheckState.Unknown || history.Evidence is not { } evidence ||
            evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Complete recent Installer event evidence is required before attempts can be correlated.");
        try
        {
            using var reader = XmlReader.Create(new StringReader(evidence.Output), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = TextEvidence.MaximumCharacters });
            var root = XElement.Load(reader);
            if (root.Name != "Events" || root.Elements().Any(element => element.Name != Ns + "Event"))
                return Unknown("Installer event XML has an unexpected structure.");
            var attempts = new List<(string Product, bool Success, DateTimeOffset Time, ulong Record)>();
            foreach (var item in root.Elements(Ns + "Event"))
            {
                token.ThrowIfCancellationRequested();
                var system = item.Element(Ns + "System");
                if (system is null || (string?)system.Element(Ns + "Provider")?.Attribute("Name") != "MsiInstaller") continue;
                if (!ushort.TryParse((string?)system.Element(Ns + "EventID"), NumberStyles.None, CultureInfo.InvariantCulture, out ushort id) || id is not (11707 or 11708)) continue;
                if (!DateTimeOffset.TryParse((string?)system.Element(Ns + "TimeCreated")?.Attribute("SystemTime"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ||
                    !ulong.TryParse((string?)system.Element(Ns + "EventRecordID"), NumberStyles.None, CultureInfo.InvariantCulture, out ulong record) || record == 0)
                    return Unknown("A retained Installer outcome has an invalid timestamp or record identity.");
                var values = item.Element(Ns + "EventData")?.Elements(Ns + "Data").Select(value => value.Value).ToArray() ?? [];
                string? product = values.Select(ProductIdentity).FirstOrDefault(value => value is not null);
                if (product is null) return Unknown("A retained Installer outcome does not contain a bounded product identity.");
                attempts.Add((product, id == 11707, time, record));
                if (attempts.Count > 200) return Unknown("Installer outcome correlation exceeded its 200-record bound.");
            }
            if (attempts.Count == 0)
                return new(name, CheckState.Information, "No product success or failure outcomes were present in the retained Installer history." + Scope(), Evidence: evidence);
            var lines = new List<string>();
            bool attention = false;
            foreach (var group in attempts.GroupBy(item => item.Product, StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
            {
                var ordered = group.OrderByDescending(item => item.Time).ThenByDescending(item => item.Record).ToArray();
                var latestTime = ordered[0].Time;
                if (ordered.Where(item => item.Time == latestTime).Select(item => item.Success).Distinct().Count() != 1)
                    return Unknown("Contradictory Installer outcomes share the latest timestamp for " + group.Key + ".");
                attention |= !ordered[0].Success;
                lines.Add(group.Key + ": latest retained outcome " + (ordered[0].Success ? "succeeded" : "failed") + " at " + ordered[0].Time.ToUniversalTime().ToString("O") +
                    "; " + group.Count(item => item.Success) + " success and " + group.Count(item => !item.Success) + " failure records retained.");
            }
            return new(name, attention ? CheckState.Attention : CheckState.Information,
                attempts.Count + " product outcome records correlated across " + lines.Count + " exact reported product identities.\n" + string.Join("\n", lines.Take(32)) + Scope(), Evidence: evidence);
        }
        catch (Exception error) when (error is XmlException or InvalidDataException)
        {
            return Unknown("Installer event evidence could not be correlated safely.");
        }
    }

    private static string? ProductIdentity(string value)
    {
        const string prefix = "Product: ";
        const string separator = " -- ";
        if (!value.StartsWith(prefix, StringComparison.Ordinal)) return null;
        int end = value.IndexOf(separator, prefix.Length, StringComparison.Ordinal);
        if (end < 0) return null;
        string product = value[prefix.Length..end];
        return product.Length is > 0 and <= 256 && product == product.Trim() &&
            !product.Any(char.IsControl) && product.Any(char.IsLetterOrDigit) ? product : null;
    }

    private static string Scope() => " Latest retained success can supersede an earlier retained failure for review ordering, but it does not prove current product health. A latest failure does not authorize repair, removal or retry. No package, registration, policy, process, service or file is changed.";
}
