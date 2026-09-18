using System.Text;
using KillerBench.Engine;

internal sealed record ReferenceCsvImport(KnowledgePack Pack, byte[] Source);

internal static class ReferenceCsvImporter
{
    private static readonly string[] Required = ["Context", "Code", "Title", "Explanation", "SourceUrl", "AppliesTo"];

    internal static async Task<ReferenceCsvImport> BuildAsync(string path, string packId, long revision,
        DateOnly publishedOn, string license, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(packId) || packId.Length > 128 || packId.Any(char.IsControl) || revision < 1 ||
            publishedOn == default || string.IsNullOrWhiteSpace(license) || license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("Reference import metadata is incomplete or unsupported.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 32_000_000) throw new InvalidDataException("Reference source exceeds the supported size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error) { throw new InvalidDataException("Reference CSV is not valid UTF-8.", error); }
        var rows = IeeeOuiImporter.ParseCsv(text, token);
        if (rows.Count < 2) throw new InvalidDataException("Reference CSV has no data rows.");
        IReadOnlyList<string> header = rows[0];
        if (header.Count != Required.Length) throw new InvalidDataException("Reference CSV columns must exactly match the documented schema.");
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < header.Count; index++)
        {
            string name = header[index].TrimStart('\uFEFF').Trim();
            if (!columns.TryAdd(name, index)) throw new InvalidDataException("Reference CSV contains duplicate columns.");
        }
        if (Required.Any(name => !columns.ContainsKey(name)))
            throw new InvalidDataException("Reference CSV columns must exactly match the documented schema.");

        var entries = new List<KnowledgeEntry>();
        foreach (var row in rows.Skip(1))
        {
            token.ThrowIfCancellationRequested();
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Count != Required.Length) throw new InvalidDataException("Reference CSV contains a short or extra row.");
            string Get(string name) => row[columns[name]].Trim();
            entries.Add(new(Get("Context"), Get("Code"), Get("Title"), Get("Explanation"), Get("SourceUrl"),
                publishedOn, Get("AppliesTo")));
        }
        if (entries.Count is 0 or > 100_000) throw new InvalidDataException("Reference source entry count is unsupported.");
        var pack = new KnowledgePack(1, packId.Trim(), revision, publishedOn, license,
            entries.OrderBy(entry => entry.Context, StringComparer.Ordinal).ThenBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, bytes);
    }
}
