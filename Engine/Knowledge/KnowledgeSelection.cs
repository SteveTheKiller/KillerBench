using System.Security.Cryptography;
using System.Text.Json;

namespace KillerBench.Engine.Knowledge;

public sealed record SignedKnowledgePack(byte[] Payload, byte[] Signature);
public sealed record KnowledgeSelectionResult(KnowledgeCatalog Catalog, bool UsingExternal, string Detail)
{
    public KnowledgeComparison? Comparison { get; init; }
}

public static class KnowledgeSelection
{
    public static KnowledgeSelectionResult Select(SignedKnowledgePack bundled, SignedKnowledgePack? external,
        ReadOnlySpan<byte> trustedKey, string packId, long minimumRevision = 1)
    {
        ArgumentNullException.ThrowIfNull(bundled);
        bundled = Snapshot(bundled);
        var baseline = KnowledgeTrust.Verify(bundled.Payload, bundled.Signature, trustedKey, packId, minimumRevision);
        if (external is null) return new(baseline, false, "Using verified bundled reference data.");
        try
        {
            external = Snapshot(external);
            var candidate = KnowledgeTrust.Verify(external.Payload, external.Signature, trustedKey, packId,
                Math.Max(minimumRevision, baseline.Revision));
            if (candidate.Revision == baseline.Revision)
            {
                if (!external.Payload.AsSpan().SequenceEqual(bundled.Payload))
                    return new(baseline, false, "External pack conflicts with the bundled revision. Using verified bundled data.");
                return new(baseline, false, "External pack matches the bundled revision. Using verified bundled data.");
            }
            return new(candidate, true, "Using a newer verified external reference pack.")
            { Comparison = baseline.Compare(candidate) };
        }
        catch (Exception error) when (error is CryptographicException or InvalidDataException or JsonException or ArgumentException)
        {
            return new(baseline, false, "External pack rejected. Using verified bundled data. " + error.Message);
        }
    }

    private static SignedKnowledgePack Snapshot(SignedKnowledgePack pack)
    {
        if (pack.Payload is null || pack.Payload.Length is 0 or > 32_000_000 || pack.Signature is null || pack.Signature.Length != 64)
            throw new InvalidDataException("Unsupported signed knowledge pack size.");
        return new((byte[])pack.Payload.Clone(), (byte[])pack.Signature.Clone());
    }
}
