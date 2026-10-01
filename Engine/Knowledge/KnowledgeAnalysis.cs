namespace KillerBench.Engine.Knowledge;

public sealed record ReferencedCode(CodeObservation Observation, KnowledgeEntry? Reference)
{
    public string? MatchedContext { get; init; }
    public string? MatchedCode { get; init; }
    public bool DerivedFromHResult { get; init; }
}
public sealed record KnowledgeLogAnalysis(LogAnalysis Analysis, string Context, string PackId, long Revision,
    DateOnly PublishedOn, string License, IReadOnlyList<ReferencedCode> Codes, string Limitation);
public sealed record KnowledgeResolution(string RequestedContext, string RequestedCode, KnowledgeEntry? Match,
    string? MatchedContext, string? MatchedCode, bool DerivedFromHResult)
{
    public string? PackId { get; init; }
    public long? Revision { get; init; }
    public bool Ambiguous { get; init; }
    public IReadOnlyList<string> MatchingPackIds { get; init; } = [];
    public bool Found => Match is not null && !Ambiguous;
    public static string Limitation => "The exact selected context is checked first. Only a standard 0x8007xxxx HRESULT can fall back to its embedded decimal Win32 code. Other aliases and code families are not inferred. A reference match does not establish a diagnosis or authorize a repair.";
}

public static class KnowledgeResolver
{
    public static KnowledgeResolution Resolve(KnowledgeCatalog catalog, string context, string code)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!DriverReference.ValidText(context, 128) || context != context.Trim())
            throw new ArgumentException("A product context is required without surrounding whitespace.", nameof(context));
        if (!DriverReference.ValidText(code, 512) || code != code.Trim())
            throw new ArgumentException("An exact code is required without surrounding whitespace.", nameof(code));
        context = context.Trim();
        code = code.Trim();
        if (catalog.Find(context, code) is { } direct)
            return new(context, code, direct, context, code, false)
            { PackId = catalog.PackId, Revision = catalog.Revision, MatchingPackIds = [catalog.PackId] };
        if (TryGetWin32Code(code, out string win32Code) && catalog.Find("win32", win32Code) is { } win32)
            return new(context, code, win32, "win32", win32Code, true)
            { PackId = catalog.PackId, Revision = catalog.Revision, MatchingPackIds = [catalog.PackId] };
        return new(context, code, null, null, null, false);
    }

    public static KnowledgeResolution Resolve(KnowledgeCatalogCollection catalogs, string context, string code)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        var direct = catalogs.Resolve(context, code);
        if (direct.Ambiguous)
            return Ambiguous(context, code, context, code, false, direct.MatchingPackIds);
        if (direct.Found)
            return Matched(context, code, context, code, false, direct);
        if (!TryGetWin32Code(code, out string win32Code))
            return new(context, code, null, null, null, false);
        var fallback = catalogs.Resolve("win32", win32Code);
        if (fallback.Ambiguous)
            return Ambiguous(context, code, "win32", win32Code, true, fallback.MatchingPackIds);
        return fallback.Found
            ? Matched(context, code, "win32", win32Code, true, fallback)
            : new(context, code, null, null, null, false);
    }

    private static KnowledgeResolution Matched(string requestedContext, string requestedCode,
        string matchedContext, string matchedCode, bool derived, KnowledgeCatalogMatch match) =>
        new(requestedContext, requestedCode, match.Entry, matchedContext, matchedCode, derived)
        { PackId = match.PackId, Revision = match.Revision, MatchingPackIds = match.MatchingPackIds };

    private static KnowledgeResolution Ambiguous(string requestedContext, string requestedCode,
        string matchedContext, string matchedCode, bool derived, IReadOnlyList<string> packIds) =>
        new(requestedContext, requestedCode, null, matchedContext, matchedCode, derived)
        { Ambiguous = true, MatchingPackIds = packIds };

    private static bool TryGetWin32Code(string value, out string code)
    {
        code = string.Empty;
        if (value.Length != 10 || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            !uint.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.AllowHexSpecifier,
                System.Globalization.CultureInfo.InvariantCulture, out uint hresult) ||
            (hresult & 0xFFFF0000u) != 0x80070000u)
            return false;
        code = (hresult & 0xFFFFu).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }
}

public static class KnowledgeAnalysis
{
    public static KnowledgeLogAnalysis Analyze(TextEvidence source, KnowledgeCatalog catalog, string context,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!DriverReference.ValidText(context, 128)) throw new ArgumentException("A product context is required.", nameof(context));
        var analysis = LogAnalyzer.Analyze(source, token);
        var codes = new List<ReferencedCode>();
        foreach (var observation in analysis.Observations)
        {
            token.ThrowIfCancellationRequested();
            var resolved = KnowledgeResolver.Resolve(catalog, context, observation.Code);
            codes.Add(new(observation, resolved.Match)
            {
                MatchedContext = resolved.MatchedContext,
                MatchedCode = resolved.MatchedCode,
                DerivedFromHResult = resolved.DerivedFromHResult
            });
        }
        return new(analysis, context.Trim(), catalog.PackId, catalog.Revision, catalog.PublishedOn, catalog.License,
            codes.AsReadOnly(), "References first use the caller-selected context and exact code spelling. An unmatched 0x8007xxxx HRESULT can resolve its standard embedded Win32 code in the win32 context, with the derived match identified in the result. Other aliases, context and source authenticity are not inferred or verified. A reference match does not establish a diagnosis or authorize a repair.");
    }

}
