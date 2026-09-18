using System.Globalization;
using System.Text;
using KillerBench.Engine;

internal sealed record UefiVendorIdSource(string Registry, byte[] Bytes);
internal sealed record UefiVendorIdImport(KnowledgePack Pack, IReadOnlyList<UefiVendorIdSource> Sources);

internal static class UefiVendorIdImporter
{
    private sealed record SourceDefinition(string Registry, string IdColumn, int IdLength, string Context, string Url);

    internal static async Task<UefiVendorIdImport> BuildAsync(string pnpPath, string acpiPath, long revision,
        DateOnly publishedOn, string license, CancellationToken token)
    {
        if (revision < 1 || publishedOn == default || string.IsNullOrWhiteSpace(license) || license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("UEFI vendor ID import metadata is incomplete or unsupported.");
        var definitions = new[]
        {
            new SourceDefinition("PNP", "PNP ID", 3, "pnp-vendor", "https://uefi.org/PNP_ID_List"),
            new SourceDefinition("ACPI", "ACPI ID", 4, "acpi-vendor", "https://uefi.org/ACPI_ID_List")
        };
        var paths = new[] { pnpPath, acpiPath };
        var entries = new List<KnowledgeEntry>();
        var sources = new List<UefiVendorIdSource>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < definitions.Length; index++)
        {
            token.ThrowIfCancellationRequested();
            byte[] bytes = await ReadAsync(paths[index], token).ConfigureAwait(false);
            sources.Add(new(definitions[index].Registry, bytes));
            Import(definitions[index], bytes, publishedOn, entries, keys, token);
        }
        if (entries.Count is 0 or > 25_000) throw new InvalidDataException("UEFI vendor ID source entry count is unsupported.");
        var pack = new KnowledgePack(1, "uefi-pnp-acpi-vendors", revision, publishedOn, license,
            entries.OrderBy(entry => entry.Context, StringComparer.Ordinal).ThenBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, sources.AsReadOnly());
    }

    private static async Task<byte[]> ReadAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 8_000_000) throw new InvalidDataException("UEFI vendor ID source exceeds the supported size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        return bytes;
    }

    private static void Import(SourceDefinition source, byte[] bytes, DateOnly verifiedOn,
        List<KnowledgeEntry> entries, HashSet<string> keys, CancellationToken token)
    {
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error) { throw new InvalidDataException(source.Registry + " registry CSV is not valid UTF-8.", error); }
        var rows = IeeeOuiImporter.ParseCsv(text, token);
        if (rows.Count < 2) throw new InvalidDataException(source.Registry + " registry CSV has no data rows.");
        var header = rows[0].Select((name, index) => (Name: name.TrimStart('\uFEFF').Trim(), Index: index))
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);
        if (!header.TryGetValue("Company", out int companyColumn) || !header.TryGetValue(source.IdColumn, out int idColumn) ||
            !header.TryGetValue("Approved On Date", out int dateColumn))
            throw new InvalidDataException(source.Registry + " registry CSV lacks required UEFI columns.");
        int requiredColumn = Math.Max(companyColumn, Math.Max(idColumn, dateColumn));
        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            token.ThrowIfCancellationRequested();
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Count <= requiredColumn) throw new InvalidDataException(source.Registry + " registry CSV contains a short row.");
            string company = row[companyColumn].Trim();
            string id = row[idColumn].Trim().ToUpperInvariant();
            string approved = row[dateColumn].Trim();
            bool validId = id.Length == source.IdLength && (source.Registry == "PNP"
                ? id.All(character => character is >= 'A' and <= 'Z')
                : id.All(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9'));
            if (!validId || company.Length > 512 || company.Any(char.IsControl) || !TryDate(approved))
                throw new InvalidDataException(source.Registry + " registry CSV contains unsupported company, ID or approval-date data.");
            string title = company.Length == 0 ? source.Registry + " ID " + id + " registry holder (company not listed)" : company;
            if (!keys.Add(source.Context + "\0" + id))
                throw new InvalidDataException(source.Registry + " registry CSV contains a duplicate contextual identity.");
            entries.Add(new(source.Context, id, title,
                source.Registry + " vendor ID assignment from the reviewed UEFI Forum registry snapshot. Registry approval date: " + approved + ".",
                source.Url, verifiedOn, source.Registry + " hardware and firmware vendor IDs. The registry holder does not establish the current device manufacturer or select a driver."));
        }
    }

    private static bool TryDate(string value) => DateOnly.TryParseExact(value,
        ["yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
