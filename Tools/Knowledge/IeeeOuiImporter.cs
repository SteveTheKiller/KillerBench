using System.Text;
using KillerBench.Engine;

internal sealed record IeeeOuiSource(string Registry, byte[] Bytes);
internal sealed record IeeeOuiImport(KnowledgePack Pack, IReadOnlyList<IeeeOuiSource> Sources);

internal static class IeeeOuiImporter
{
    private sealed record SourceDefinition(string Registry, int HexLength, string Url, string Path);

    internal static async Task<IeeeOuiImport> BuildAsync(string maL, string maM, string maS, long revision,
        DateOnly publishedOn, string license, CancellationToken token)
    {
        if (revision < 1 || publishedOn == default || string.IsNullOrWhiteSpace(license) || license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("IEEE import metadata is incomplete or unsupported.");
        var definitions = new[]
        {
            new SourceDefinition("MA-L", 6, "https://standards-oui.ieee.org/oui/oui.csv", maL),
            new SourceDefinition("MA-M", 7, "https://standards-oui.ieee.org/oui28/mam.csv", maM),
            new SourceDefinition("MA-S", 9, "https://standards-oui.ieee.org/oui36/oui36.csv", maS)
        };
        var entries = new List<KnowledgeEntry>();
        var sources = new List<IeeeOuiSource>();
        long totalBytes = 0;
        foreach (var definition in definitions)
        {
            token.ThrowIfCancellationRequested();
            await using var stream = new FileStream(definition.Path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length is 0 or > 16_000_000 || (totalBytes += stream.Length) > 32_000_000)
                throw new InvalidDataException("IEEE source files exceed the supported size limit.");
            var bytes = new byte[(int)stream.Length];
            await stream.ReadExactlyAsync(bytes, token);
            sources.Add(new(definition.Registry, bytes));
            Import(definition, bytes, publishedOn, entries, token);
        }
        if (entries.Count is 0 or > 100_000) throw new InvalidDataException("IEEE source entry count is unsupported.");
        var pack = new KnowledgePack(1, "ieee-mac-vendors", revision, publishedOn, license,
            entries.OrderBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, sources.AsReadOnly());
    }

    private static void Import(SourceDefinition source, byte[] bytes, DateOnly verifiedOn,
        List<KnowledgeEntry> entries, CancellationToken token)
    {
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error) { throw new InvalidDataException("IEEE CSV is not valid UTF-8.", error); }
        var rows = ParseCsv(text, token);
        if (rows.Count < 2) throw new InvalidDataException(source.Registry + " CSV has no data rows.");
        var header = rows[0].Select((name, index) => (Name: name.TrimStart('\uFEFF').Trim(), Index: index))
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);
        if (!header.TryGetValue("Registry", out int registryColumn) || !header.TryGetValue("Assignment", out int assignmentColumn) ||
            !header.TryGetValue("Organization Name", out int organizationColumn))
            throw new InvalidDataException(source.Registry + " CSV lacks required IEEE columns.");
        int requiredColumn = Math.Max(registryColumn, Math.Max(assignmentColumn, organizationColumn));
        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            token.ThrowIfCancellationRequested();
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Count <= requiredColumn) throw new InvalidDataException(source.Registry + " CSV contains a short row.");
            string registry = row[registryColumn].Trim();
            string assignment = row[assignmentColumn].Trim().ToUpperInvariant();
            string organization = row[organizationColumn].Trim();
            if (!registry.Equals(source.Registry, StringComparison.OrdinalIgnoreCase) || assignment.Length != source.HexLength ||
                assignment.Any(character => !Uri.IsHexDigit(character)) || string.IsNullOrWhiteSpace(organization) ||
                organization.Length > 512 || organization.Any(char.IsControl))
                throw new InvalidDataException(source.Registry + " CSV contains unsupported registry, assignment or organization data.");
            int bits = source.Registry == "MA-L" ? 24 : source.Registry == "MA-M" ? 28 : 36;
            entries.Add(new("mac-vendor", assignment, organization,
                "IEEE " + source.Registry + " registry assignment for " + organization + ".", source.Url, verifiedOn,
                "MAC addresses beginning with this " + bits + "-bit prefix. Organization names describe the registry assignment, not the current device manufacturer."));
        }
    }

    internal static IReadOnlyList<IReadOnlyList<string>> ParseCsv(string text, CancellationToken token)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int index = 0; index < text.Length; index++)
        {
            if ((index & 4095) == 0) token.ThrowIfCancellationRequested();
            char character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                else if (character == '"') quoted = false;
                else field.Append(character);
            }
            else if (character == '"' && field.Length == 0) quoted = true;
            else if (character == ',') AddField();
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                AddField(); AddRow();
            }
            else field.Append(character);
            if (field.Length > 4096) throw new InvalidDataException("IEEE CSV field exceeds the supported limit.");
        }
        if (quoted) throw new InvalidDataException("IEEE CSV contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) { AddField(); AddRow(); }
        return rows;

        void AddField() { row.Add(field.ToString()); field.Clear(); if (row.Count > 32) throw new InvalidDataException("IEEE CSV row exceeds the supported column limit."); }
        void AddRow() { rows.Add(row.ToArray()); row.Clear(); if (rows.Count > 100_001) throw new InvalidDataException("IEEE CSV exceeds the supported row limit."); }
    }
}
