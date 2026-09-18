using System.Text.Json;

namespace KillerBench.Engine;

public sealed record KnowledgeEntry(string Context, string Code, string Title, string Explanation,
    string SourceUrl, DateOnly VerifiedOn, string AppliesTo, DriverReference? Driver = null);
public sealed record KnowledgePack(int SchemaVersion, string Id, long Revision, DateOnly PublishedOn,
    string License, IReadOnlyList<KnowledgeEntry> Entries);
public sealed record KnowledgeSearchResult(int TotalMatches, IReadOnlyList<KnowledgeEntry> Matches)
{
    public bool Truncated => TotalMatches > Matches.Count;
}
public sealed record KnowledgeContextSummary(string Context, int Entries, int DriverReferences);
public sealed record KnowledgeCatalogSummary(string PackId, int SchemaVersion, long Revision, DateOnly PublishedOn,
    string License, int Entries, IReadOnlyList<KnowledgeContextSummary> Contexts);

public sealed record KnowledgeChange(string Kind, KnowledgeEntry? Before, KnowledgeEntry? After);
public sealed record KnowledgeMetadata(int SchemaVersion, DateOnly PublishedOn, string License);
public sealed record KnowledgeComparison(string PackId, long BeforeRevision, long AfterRevision, bool MetadataChanged,
    int Added, int Removed, int Changed, IReadOnlyList<KnowledgeChange> Changes)
{
    public bool Truncated => Added + Removed + Changed > Changes.Count;
    public KnowledgeMetadata? BeforeMetadata { get; init; }
    public KnowledgeMetadata? AfterMetadata { get; init; }
}

public sealed class KnowledgeCatalog
{
    private readonly Dictionary<(string Context, string Code), KnowledgeEntry> index;
    public string PackId { get; }
    public int SchemaVersion { get; }
    public long Revision { get; }
    public DateOnly PublishedOn { get; }
    public string License { get; }
    public int Count => index.Count;

    public static async Task<KnowledgeCatalog> ReadFileAsync(string path, CancellationToken token = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        deadline.Token.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is 0 or > 32_000_000) throw new InvalidDataException("Knowledge pack size is unsupported.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, deadline.Token).ConfigureAwait(false);
        deadline.Token.ThrowIfCancellationRequested();
        var catalog = Read(bytes, deadline.Token);
        deadline.Token.ThrowIfCancellationRequested();
        return catalog;
    }

    private KnowledgeCatalog(KnowledgePack pack, Dictionary<(string, string), KnowledgeEntry> entries)
    {
        PackId = pack.Id;
        SchemaVersion = pack.SchemaVersion;
        Revision = pack.Revision;
        PublishedOn = pack.PublishedOn;
        License = pack.License;
        index = entries;
    }

    public KnowledgeEntry? Find(string context, string code) =>
        index.GetValueOrDefault((context.Trim().ToUpperInvariant(), code.Trim().ToUpperInvariant()));

    public KnowledgeCatalogSummary Summarize(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var contexts = index.Values
            .GroupBy(entry => entry.Context.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                token.ThrowIfCancellationRequested();
                return new KnowledgeContextSummary(group.Key.ToLowerInvariant(), group.Count(),
                    group.Count(entry => entry.Driver is not null));
            })
            .OrderBy(context => context.Context, StringComparer.Ordinal)
            .ToArray();
        token.ThrowIfCancellationRequested();
        return new(PackId, SchemaVersion, Revision, PublishedOn, License, Count, contexts);
    }

    public KnowledgeComparison Compare(KnowledgeCatalog next, int maximumResults = 100, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (PackId != next.PackId) throw new ArgumentException("Compare revisions of the same pack.", nameof(next));
        if (maximumResults is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(maximumResults));
        token.ThrowIfCancellationRequested();
        int added = 0, removed = 0, changed = 0;
        var changes = new List<KnowledgeChange>();
        foreach (var key in index.Keys.Union(next.index.Keys).OrderBy(key => key.Context, StringComparer.Ordinal).ThenBy(key => key.Code, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            index.TryGetValue(key, out var before);
            next.index.TryGetValue(key, out var after);
            string kind;
            if (before is null) { added++; kind = "Added"; }
            else if (after is null) { removed++; kind = "Removed"; }
            else if (before != after && JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after)) { changed++; kind = "Changed"; }
            else continue;
            if (changes.Count < maximumResults) changes.Add(new(kind, before, after));
        }
        return new(PackId, Revision, next.Revision, SchemaVersion != next.SchemaVersion || PublishedOn != next.PublishedOn || License != next.License,
            added, removed, changed, changes.AsReadOnly())
        {
            BeforeMetadata = new(SchemaVersion, PublishedOn, License),
            AfterMetadata = new(next.SchemaVersion, next.PublishedOn, next.License)
        };
    }

    public KnowledgeSearchResult Search(string context, string query, int maximumResults = 50, CancellationToken token = default)
    {
        if (!Text(context, 128) || !Text(query, 512)) throw new ArgumentException("A context and search text are required.");
        if (maximumResults is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(maximumResults));
        context = context.Trim();
        query = query.Trim();
        token.ThrowIfCancellationRequested();
        var matches = new List<KnowledgeEntry>();
        int total = 0;
        foreach (var entry in index.Values)
        {
            token.ThrowIfCancellationRequested();
            if (!string.Equals(entry.Context.Trim(), context, StringComparison.OrdinalIgnoreCase) ||
                !(entry.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                  entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                  entry.Explanation.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                  entry.AppliesTo.Contains(query, StringComparison.OrdinalIgnoreCase))) continue;
            total++;
            matches.Add(entry);
            matches.Sort((left, right) => StringComparer.Ordinal.Compare(left.Code, right.Code));
            if (matches.Count > maximumResults) matches.RemoveAt(matches.Count - 1);
        }
        return new(total, matches.AsReadOnly());
    }

    public IReadOnlyList<KnowledgeEntry> FindDriverReferences(string hardwareId, string architecture,
        string windowsProduct, int build)
    {
        if (!DriverReference.ValidText(hardwareId, 512) || hardwareId != hardwareId.Trim() ||
            architecture?.ToLowerInvariant() is not ("x86" or "x64" or "arm64") ||
            windowsProduct?.ToLowerInvariant() is not ("client" or "server") || build <= 0)
            throw new ArgumentException("An exact hardware ID, architecture, Windows product and positive build are required.");
        return index.Values.Where(entry => entry.Driver?.Matches(hardwareId, architecture, windowsProduct, build) == true)
            .OrderBy(entry => entry.Context, StringComparer.Ordinal).ThenBy(entry => entry.Code, StringComparer.Ordinal).ToArray();
    }

    public static KnowledgeCatalog Read(ReadOnlyMemory<byte> utf8, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (utf8.Length is 0 or > 32_000_000) throw new InvalidDataException("Knowledge pack size is unsupported.");
        using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions { MaxDepth = 16 });
        RejectDuplicateFields(document.RootElement, token);
        var pack = document.RootElement.Deserialize<KnowledgePack>() ?? throw new InvalidDataException("Missing knowledge pack.");
        token.ThrowIfCancellationRequested();
        if (pack.SchemaVersion is not (1 or 2) || pack.Revision < 1 || !Text(pack.Id, 128) || !Text(pack.License, 2000) ||
            pack.PublishedOn == default || pack.Entries is null || pack.Entries.Count is 0 or > 100_000)
            throw new InvalidDataException("Unsupported or incomplete knowledge pack metadata.");
        var entries = new Dictionary<(string, string), KnowledgeEntry>();
        foreach (var entry in pack.Entries)
        {
            token.ThrowIfCancellationRequested();
            if (entry is null || !Text(entry.Context, 128) || !Text(entry.Code, 512) || !Text(entry.Title, 512) ||
                !Text(entry.Explanation, 8000) || !Text(entry.AppliesTo, 2000) || entry.VerifiedOn == default ||
                entry.VerifiedOn > pack.PublishedOn || !Uri.TryCreate(entry.SourceUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0 || string.IsNullOrWhiteSpace(uri.Host))
                throw new InvalidDataException("Knowledge entry lacks valid context, provenance or applicability.");
            if (entry.Driver is not null && (pack.SchemaVersion != 2 || !entry.Driver.IsValid()))
                throw new InvalidDataException("Driver references require schema 2 and explicit valid applicability and download metadata.");
            if (!entries.TryAdd((entry.Context.Trim().ToUpperInvariant(), entry.Code.Trim().ToUpperInvariant()), entry))
                throw new InvalidDataException("Duplicate contextual knowledge key.");
        }
        return new(pack, entries);
    }

    private static bool Text(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum &&
        !value.Any(char.IsControl);

    private static void RejectDuplicateFields(JsonElement value, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON field in knowledge pack.");
                RejectDuplicateFields(property.Value, token);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RejectDuplicateFields(item, token);
    }
}
