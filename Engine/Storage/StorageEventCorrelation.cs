using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Storage;

public static class StorageEventCorrelation
{
    private static readonly XNamespace EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";

    public static WorkflowCheck Check(WorkflowCheck topology, WorkflowCheck history, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Storage event correlation";
        const string scope = " Historical event identity does not prove a current fault or authorize filesystem, disk, driver or controller repair. No storage changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + scope,
            Evidence: history.Evidence ?? topology.Evidence);
        if (topology.EffectiveState == CheckState.Unknown || topology.Evidence is null)
            return Unknown("A complete storage topology is required before event identities can be mapped.");
        if (history.EffectiveState == CheckState.Unknown || history.Evidence is null)
            return Unknown("Complete recent storage event evidence is required before identities can be mapped.");

        try
        {
            var partitions = ReadPartitions(topology.Evidence.Output);
            var source = EventEvidence.FromXml("System", history.Evidence.Output);
            var references = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int unresolved = 0;
            foreach (string line in source.Evidence!.Lines)
            {
                token.ThrowIfCancellationRequested();
                var root = XElement.Parse(line, LoadOptions.None);
                var values = (root.Element(EventNamespace + "EventData")?.Elements(EventNamespace + "Data") ?? [])
                    .Where(field => string.Equals((string?)field.Attribute("Name"), "DriveName", StringComparison.OrdinalIgnoreCase))
                    .Select(field => field.Value.Trim())
                    .Concat((root.Element(EventNamespace + "UserData")?.Descendants() ?? [])
                        .Where(field => !field.HasElements && string.Equals(field.Name.LocalName, "Volume", StringComparison.OrdinalIgnoreCase))
                        .Select(field => field.Value.Trim()))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (values.Length == 0) { unresolved++; continue; }
                foreach (string value in values)
                {
                    string? letter = DriveLetter(value);
                    if (letter is null) { unresolved++; continue; }
                    references[letter] = references.GetValueOrDefault(letter) + 1;
                }
            }

            var mapped = references.Where(pair => partitions.ContainsKey(pair.Key)).OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            var missing = references.Keys.Where(letter => !partitions.ContainsKey(letter)).OrderBy(letter => letter, StringComparer.OrdinalIgnoreCase).ToArray();
            string detail = references.Count == 0
                ? $"Reviewed {source.Evidence.Lines.Length} recent storage event record{(source.Evidence.Lines.Length == 1 ? "" : "s")}. No drive-letter identity was available for disk and partition mapping."
                : "Mapped event identities: " + string.Join("; ", mapped.Select(pair =>
                    $"{pair.Key}: to disk {partitions[pair.Key].Disk}, partition {partitions[pair.Key].Partition} ({pair.Value} record{(pair.Value == 1 ? "" : "s")})")) + ".";
            if (missing.Length > 0) detail += " Unmapped drive letters: " + string.Join(", ", missing.Select(letter => letter + ":")) + ". Recollect after storage identity changes.";
            if (unresolved > 0) detail += $" {unresolved} event identity field{(unresolved == 1 ? " was" : "s were")} absent or not a drive letter; raw evidence remains available for manual correlation.";
            detail += " Drive letters and disk numbers are point-in-time identities." + scope;
            return new(name, missing.Length > 0 ? CheckState.Unknown : CheckState.Information, detail, Evidence: history.Evidence);
        }
        catch (Exception error) when (error is JsonException or XmlException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Storage topology and event evidence could not be correlated safely.");
        }
    }

    private static Dictionary<string, (int Disk, int Partition)> ReadPartitions(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("partitions", out var partitions) || partitions.ValueKind != JsonValueKind.Array || partitions.GetArrayLength() > 256)
            throw new InvalidDataException("Storage topology is incomplete.");
        var result = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
        foreach (var partition in partitions.EnumerateArray())
        {
            if (!partition.TryGetProperty("disk", out var diskValue) || !diskValue.TryGetInt32(out int disk) || disk < 0 ||
                !partition.TryGetProperty("number", out var partitionValue) || !partitionValue.TryGetInt32(out int number) || number < 1 ||
                !partition.TryGetProperty("letter", out var letterValue) || letterValue.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Storage partition identity is invalid.");
            string letter = letterValue.GetString() ?? "";
            if (letter.Length == 0) continue;
            if (letter.Length != 1 || !char.IsAsciiLetter(letter[0]) || !result.TryAdd(letter.ToUpperInvariant(), (disk, number)))
                throw new InvalidDataException("Storage drive-letter identity is ambiguous.");
        }
        return result;
    }

    private static string? DriveLetter(string value) => value.Length >= 2 && char.IsAsciiLetter(value[0]) && value[1] == ':' &&
        (value.Length == 2 || value.Length == 3 && (value[2] == '\\' || value[2] == '/')) ? char.ToUpperInvariant(value[0]).ToString() : null;
}
