namespace KillerBench.Engine.Crash;

public static class CrashReview
{
    public static async Task<IReadOnlyList<WorkflowCheck>> ReadAsync(CancellationToken token = default)
        => await ReadCoreAsync(null, token).ConfigureAwait(false);

    public static async Task<IReadOnlyList<WorkflowCheck>> ReadWithKnowledgeAsync(KnowledgeCatalog catalog,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return await ReadCoreAsync(new KnowledgeCatalogCollection([catalog]), token).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> ReadWithKnowledgeAsync(KnowledgeCatalogCollection catalogs,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        return await ReadCoreAsync(catalogs, token).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<WorkflowCheck>> ReadCoreAsync(KnowledgeCatalogCollection? catalogs,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var checks = new List<WorkflowCheck> { CrashDumpConfiguration.Check(token) };
        var dumps = await CrashDumpInventory.ReadAsync(token).ConfigureAwait(false);
        checks.Add(CrashDumpInventory.Interpret(dumps));
        var analyses = new Dictionary<string, MinidumpSummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var dump in dumps.Files.OrderByDescending(file => file.LastWriteUtc).Take(3))
        {
            token.ThrowIfCancellationRequested();
            var analysis = await MinidumpAnalyzer.AnalyzeFileAsync(dump.Path, token).ConfigureAwait(false);
            analyses[dump.Path] = analysis;
            checks.Add(MinidumpAnalyzer.Interpret(analysis));
        }
        var history = await CrashEvents.CheckAsync(token).ConfigureAwait(false);
        checks.Add(history);
        if (history.Evidence is { } evidence)
        {
            checks.Add(CrashDumpCorrelation.Check(evidence, dumps, token));
            checks.Add(CrashIncidentCorrelation.Check(evidence, dumps, analyses, token));
            if (catalogs is { Count: > 0 }) checks.Add(CrashBugCheckReferences.Check(evidence, catalogs, token));
        }
        checks.Add(await ShutdownEvents.CheckAsync(token).ConfigureAwait(false));
        return checks.AsReadOnly();
    }
}
