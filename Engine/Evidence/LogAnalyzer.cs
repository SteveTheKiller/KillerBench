using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public sealed record EvidenceReference(string Source, int LineNumber, string Excerpt);
public sealed record CodeObservation(string Code, int Occurrences, IReadOnlyList<EvidenceReference> Evidence);
public sealed record LogAnalysis(string SchemaVersion, string Source, int LinesAnalyzed,
    bool SourceTruncated, bool ObservationsTruncated, IReadOnlyList<CodeObservation> Observations,
    string Limitation);

/// <summary>Extracts code observations from supplied text without diagnosing machine health.</summary>
public static partial class LogAnalyzer
{
    public const int MaximumCodes = 256;
    public const int MaximumReferencesPerCode = 8;
    public const string Limitation = "Hexadecimal tokens are observations, not confirmed errors or root causes. No diagnostic rules or repairs were run.";

    [GeneratedRegex(@"\b0x[0-9a-fA-F]{8}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    public static async Task<LogAnalysis> AnalyzeFileAsync(string path, CancellationToken cancellationToken = default)
        => Analyze(await TextEvidence.ReadAsync(path, cancellationToken).ConfigureAwait(false), cancellationToken);

    public static LogAnalysis Analyze(TextEvidence source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var codes = new Dictionary<string, (int Count, List<EvidenceReference> References)>(StringComparer.Ordinal);
        bool truncated = false;
        int characters = 0, lines = 0;
        foreach (string original in source.Lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (characters >= TextEvidence.MaximumCharacters) { truncated = true; break; }
            int remaining = TextEvidence.MaximumCharacters - characters;
            string line = original.Length > remaining ? original[..remaining] : original;
            truncated |= original.Length > remaining;
            characters += line.Length + 1;
            lines++;
            foreach (Match match in CodePattern().Matches(line))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string code = "0x" + match.Value[2..].ToUpperInvariant();
                if (!codes.TryGetValue(code, out var item))
                {
                    if (codes.Count == MaximumCodes) { truncated = true; continue; }
                    item = (0, []);
                }
                item.Count++;
                if (item.References.Count < MaximumReferencesPerCode &&
                    (item.References.Count == 0 || item.References[^1].LineNumber != lines))
                {
                    int start = Math.Max(0, match.Index - 120);
                    item.References.Add(new(source.Source, lines, line.Substring(start, Math.Min(320, line.Length - start))));
                }
                codes[code] = item;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new("1", source.Source, lines, source.Truncated, truncated,
            [.. codes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new CodeObservation(pair.Key, pair.Value.Count, pair.Value.References.AsReadOnly()))], Limitation);
    }
}
