using System.Text;
using KillerBench.Engine;

internal sealed record DriverReferenceImport(KnowledgePack Pack, byte[] Source);

internal static class DriverReferenceImporter
{
    private static readonly string[] Required = ["Context", "Code", "Title", "Explanation", "SourceUrl", "AppliesTo",
        "HardwareId", "Architecture", "WindowsProduct", "MinimumBuild", "MaximumBuild", "Vendor", "DriverVersion", "DownloadUrl"];

    internal static async Task<DriverReferenceImport> BuildAsync(string path, string packId, long revision,
        DateOnly publishedOn, string license, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(packId) || packId.Length > 128 || packId.Any(char.IsControl) || revision < 1 ||
            publishedOn == default || string.IsNullOrWhiteSpace(license) || license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("Driver import metadata is incomplete or unsupported.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 32_000_000) throw new InvalidDataException("Driver source exceeds the supported size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error) { throw new InvalidDataException("Driver CSV is not valid UTF-8.", error); }
        var rows = IeeeOuiImporter.ParseCsv(text, token);
        if (rows.Count < 2) throw new InvalidDataException("Driver CSV has no data rows.");
        var columns = rows[0].Select((name, index) => (Name: name.TrimStart('\uFEFF').Trim(), Index: index))
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);
        if (Required.Any(name => !columns.ContainsKey(name)) || columns.Count != Required.Length)
            throw new InvalidDataException("Driver CSV columns must exactly match the documented schema.");
        var entries = new List<KnowledgeEntry>();
        foreach (var row in rows.Skip(1))
        {
            token.ThrowIfCancellationRequested();
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Count != Required.Length) throw new InvalidDataException("Driver CSV contains a short or extra row.");
            string Get(string name) => row[columns[name]].Trim();
            if (!int.TryParse(Get("MinimumBuild"), out int minimum) || !int.TryParse(Get("MaximumBuild"), out int maximum))
                throw new InvalidDataException("Driver CSV contains an invalid build range.");
            entries.Add(new(Get("Context"), Get("Code"), Get("Title"), Get("Explanation"), Get("SourceUrl"), publishedOn,
                Get("AppliesTo"), new(Get("HardwareId"), Get("Architecture"), Get("WindowsProduct"), minimum, maximum,
                    Get("Vendor"), Get("DriverVersion"), Get("DownloadUrl"))));
        }
        if (entries.Count is 0 or > 100_000) throw new InvalidDataException("Driver source entry count is unsupported.");
        var pack = new KnowledgePack(2, packId.Trim(), revision, publishedOn, license,
            entries.OrderBy(entry => entry.Context, StringComparer.Ordinal).ThenBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, bytes);
    }
}
