namespace KillerBench.Engine.Knowledge;

public sealed record KnowledgeCatalogMatch(KnowledgeEntry? Entry, string? PackId, long? Revision,
    bool Ambiguous, IReadOnlyList<string> MatchingPackIds)
{
    public bool Found => Entry is not null && !Ambiguous;
}

public sealed record KnowledgeDriverMatch(KnowledgeEntry Entry, string PackId, long Revision);
public sealed record KnowledgeDriverSearchResult(int TotalMatches, IReadOnlyList<KnowledgeDriverMatch> Matches,
    IReadOnlyList<string> ConflictingIdentities)
{
    public bool Truncated => TotalMatches > Matches.Count;
    public bool Ambiguous => ConflictingIdentities.Count > 0;
}

public sealed class KnowledgeCatalogCollection
{
    private readonly Lock sync = new();
    private readonly Dictionary<string, KnowledgeCatalog> catalogs = new(StringComparer.Ordinal);

    public int Count { get { lock (sync) return catalogs.Count; } }

    public KnowledgeCatalogCollection(IEnumerable<KnowledgeCatalog>? initial = null)
    {
        if (initial is null) return;
        foreach (var catalog in initial) AddOrReplace(catalog);
    }

    public void AddOrReplace(KnowledgeCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        lock (sync)
        {
            if (!catalogs.ContainsKey(catalog.PackId) && catalogs.Count >= 32)
                throw new InvalidOperationException("No more than 32 knowledge packs can be active together.");
            catalogs[catalog.PackId] = catalog;
        }
    }

    public IReadOnlyList<KnowledgeCatalog> Snapshot()
    {
        lock (sync)
            return Array.AsReadOnly(catalogs.Values.OrderBy(catalog => catalog.PackId, StringComparer.Ordinal).ToArray());
    }

    public KnowledgeCatalogMatch Resolve(string context, string code)
    {
        if (!DriverReference.ValidText(context, 128) || context != context.Trim() ||
            !DriverReference.ValidText(code, 512) || code != code.Trim())
            throw new ArgumentException("An exact context and code are required without surrounding whitespace.");
        var matches = Snapshot().Select(catalog => (Catalog: catalog, Entry: catalog.Find(context, code)))
            .Where(result => result.Entry is not null).ToArray();
        if (matches.Length == 0) return new(null, null, null, false, []);
        if (matches.Length > 1)
            return new(null, null, null, true,
                Array.AsReadOnly(matches.Select(match => match.Catalog.PackId).Order(StringComparer.Ordinal).ToArray()));
        return new(matches[0].Entry, matches[0].Catalog.PackId, matches[0].Catalog.Revision, false,
            Array.AsReadOnly<string>([matches[0].Catalog.PackId]));
    }

    public KnowledgeDriverSearchResult FindDriverReferences(string hardwareId, string architecture,
        string windowsProduct, int build, int maximumResults = 64, CancellationToken token = default)
    {
        if (maximumResults is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(maximumResults));
        var matches = new List<KnowledgeDriverMatch>();
        var owners = new Dictionary<(string Context, string Code), HashSet<string>>();
        int total = 0;
        foreach (var catalog in Snapshot())
        {
            token.ThrowIfCancellationRequested();
            foreach (var entry in catalog.FindDriverReferences(hardwareId, architecture, windowsProduct, build))
            {
                token.ThrowIfCancellationRequested();
                total++;
                var key = (entry.Context.Trim().ToUpperInvariant(), entry.Code.Trim().ToUpperInvariant());
                if (!owners.TryGetValue(key, out var packIds)) owners.Add(key, packIds = new(StringComparer.Ordinal));
                packIds.Add(catalog.PackId);
                if (matches.Count < maximumResults) matches.Add(new(entry, catalog.PackId, catalog.Revision));
            }
        }
        var conflicts = owners.Where(item => item.Value.Count > 1)
            .Select(item => item.Key.Context + "/" + item.Key.Code)
            .Order(StringComparer.Ordinal).ToArray();
        return new(total, matches.AsReadOnly(), Array.AsReadOnly(conflicts));
    }
}
