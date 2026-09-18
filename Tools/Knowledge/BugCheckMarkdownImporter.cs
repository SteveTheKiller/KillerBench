using System.Text;
using System.Text.RegularExpressions;
using KillerBench.Engine;

internal sealed record BugCheckMarkdownImport(KnowledgePack Pack, byte[] Source);

internal static partial class BugCheckMarkdownImporter
{
    private const string SourceBase = "https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/";

    internal static async Task<BugCheckMarkdownImport> BuildAsync(string path, long revision, DateOnly publishedOn,
        string license, CancellationToken token)
    {
        if (revision < 1 || publishedOn == default || string.IsNullOrWhiteSpace(license) ||
            license.Length > 2000 || license.Any(char.IsControl))
            throw new ArgumentException("Bug-check import metadata is incomplete or unsupported.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 4_000_000) throw new InvalidDataException("Bug-check source exceeds the supported size limit.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException error) { throw new InvalidDataException("Bug-check source is not valid UTF-8.", error); }

        var entries = new List<KnowledgeEntry>();
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (string raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            token.ThrowIfCancellationRequested();
            var match = TableRow().Match(raw);
            if (!match.Success)
            {
                if (raw.TrimStart().StartsWith("| 0x", StringComparison.OrdinalIgnoreCase) &&
                    raw.Contains("bug-check-", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Bug-check source contains an unsupported table row.");
                continue;
            }
            if (!uint.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.AllowHexSpecifier,
                System.Globalization.CultureInfo.InvariantCulture, out uint value))
                throw new InvalidDataException("Bug-check source contains an invalid code.");
            string code = "0x" + value.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            string title = match.Groups[2].Value + match.Groups[3].Value.Replace("\\_", "_", StringComparison.Ordinal);
            string link = match.Groups[4].Value;
            if (!ValidTitle(title) || !ValidLink(link) || !codes.Add(code))
                throw new InvalidDataException("Bug-check source contains an invalid or duplicate identity.");
            string sourceUrl = SourceBase + link[..^3];
            entries.Add(new("bugcheck", code, title,
                "Microsoft bug-check identity from the reviewed code-reference snapshot. Analyze the matching dump and four parameters before diagnosing a cause.",
                sourceUrl, publishedOn,
                "Windows kernel bug checks. Code identity alone does not identify a driver, device, component or repair."));
        }
        if (entries.Count is 0 or > 4096) throw new InvalidDataException("Bug-check source entry count is unsupported.");
        var pack = new KnowledgePack(1, "windows-bug-checks", revision, publishedOn, license,
            entries.OrderBy(entry => entry.Code, StringComparer.Ordinal).ToArray());
        KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(pack), token);
        return new(pack, bytes);
    }

    private static bool ValidTitle(string value) => value.Length is > 0 and <= 128 &&
        value.All(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_') &&
        value.Any(char.IsLetter);

    private static bool ValidLink(string value) => value.Length is > 3 and <= 200 &&
        value.EndsWith(".md", StringComparison.Ordinal) &&
        value.StartsWith("bug-check-", StringComparison.Ordinal) &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '_');

    [GeneratedRegex(@"^\s*\|\s*0x([0-9A-Fa-f]{1,8})\s*\|\s*(I?)\[\*\*((?:[A-Z0-9_]|\\_)+)\*\*\]\(([^)]+\.md)\)\s*\|\s*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TableRow();
}
