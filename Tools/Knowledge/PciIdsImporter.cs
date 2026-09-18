using System.Text;
using KillerBench.Engine;

internal sealed record PciIdsImport(KnowledgePack Pack, byte[] Source);

internal static class PciIdsImporter
{
    internal static async Task<PciIdsImport> BuildAsync(string path, long revision, DateOnly publishedOn,
        string license, CancellationToken token)
    {
        if (revision < 1 || publishedOn == default || string.IsNullOrWhiteSpace(license) || license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("PCI ID import metadata is incomplete or unsupported.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 32_000_000) throw new InvalidDataException("PCI ID source exceeds the supported size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error) { throw new InvalidDataException("PCI ID source is not valid UTF-8.", error); }

        const string sourceUrl = "https://pci-ids.ucw.cz/";
        var entries = new List<KnowledgeEntry>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        string? vendorCode = null, vendorName = null;
        foreach (string raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            token.ThrowIfCancellationRequested();
            if (raw.Length == 0 || raw[0] == '#') continue;
            if (raw.StartsWith("C ", StringComparison.Ordinal)) break;
            if (raw.StartsWith("\t\t", StringComparison.Ordinal)) continue;
            bool device = raw[0] == '\t';
            string line = device ? raw[1..] : raw;
            if (line.Length < 6 || line[4] is not (' ' or '\t')) throw new InvalidDataException("PCI ID source contains an unsupported record.");
            string code = line[..4].ToUpperInvariant();
            string title = line[5..].Trim();
            if (!code.All(Uri.IsHexDigit) || !ValidTitle(title)) throw new InvalidDataException("PCI ID source contains an invalid code or title.");
            if (!device)
            {
                vendorCode = code;
                vendorName = title;
                Add("pci-vendor", "VEN_" + code, title, "PCI vendor assignment from the reviewed pci.ids snapshot.", "PCI vendor IDs");
            }
            else
            {
                if (vendorCode is null || vendorName is null) throw new InvalidDataException("PCI device appears before its vendor.");
                Add("pci-device", "VEN_" + vendorCode + "&DEV_" + code, title,
                    "PCI device assignment under " + vendorName + " in the reviewed pci.ids snapshot.", "PCI device IDs");
            }
        }
        if (entries.Count is 0 or > 100_000) throw new InvalidDataException("PCI ID source entry count is unsupported.");
        var pack = new KnowledgePack(1, "pci-hardware-vendors", revision, publishedOn, license,
            entries.OrderBy(entry => entry.Context, StringComparer.Ordinal).ThenBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, bytes);

        void Add(string context, string code, string title, string explanation, string appliesTo)
        {
            if (!keys.Add(context + "\0" + code)) throw new InvalidDataException("PCI ID source contains a duplicate contextual identity.");
            entries.Add(new(context, code, title, explanation, sourceUrl, publishedOn, appliesTo));
        }
    }

    private static bool ValidTitle(string value) => value.Length is > 0 and <= 512 && value == value.Trim() &&
        !value.Any(char.IsControl) && value.Any(char.IsLetterOrDigit);
}
