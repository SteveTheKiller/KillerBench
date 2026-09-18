using System.Security.Cryptography;

namespace KillerBench.Engine;

public static class KnowledgeTrust
{
    public static KnowledgeCatalog Verify(ReadOnlyMemory<byte> payload, ReadOnlySpan<byte> signature,
        ReadOnlySpan<byte> trustedPublicKey, string expectedPackId, long minimumRevision)
    {
        if (payload.Length is 0 or > 32_000_000 || signature.Length != 64 || trustedPublicKey.Length is 0 or > 1024)
            throw new InvalidDataException("Unsupported signed knowledge pack size.");
        if (string.IsNullOrWhiteSpace(expectedPackId) || minimumRevision < 1)
            throw new ArgumentException("Expected pack identity and minimum revision are required.");
        // Keep verification and parsing on the same privately owned byte snapshot.
        var snapshot = payload.ToArray();
        using var key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(trustedPublicKey, out int consumed);
        if (consumed != trustedPublicKey.Length || key.KeySize != 256 ||
            key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7" ||
            !key.VerifyData(snapshot, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new CryptographicException("Knowledge pack signature does not match the trusted publisher key.");
        var catalog = KnowledgeCatalog.Read(snapshot);
        if (!string.Equals(catalog.PackId, expectedPackId, StringComparison.Ordinal) || catalog.Revision < minimumRevision)
            throw new InvalidDataException("Knowledge pack identity or revision is not acceptable.");
        return catalog;
    }
}
