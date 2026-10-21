using System.Security.Cryptography;
using System.Text.Json;

namespace KillerBench.Engine.Knowledge;

public sealed record ActiveKnowledgePointer(string PackId, long Revision, string FileName, string PayloadSha256, DateTimeOffset ActivatedUtc);
public sealed record KnowledgeActivationResult(KnowledgeCatalog Catalog, string ActivePath, string DataPath, bool Changed, string Detail);
public sealed record KnowledgeRecoveryResult(KnowledgeCatalog Catalog, string ActivePath, string DataPath,
    int FilesInspected, int FilesRejected, string Detail);

public static class KnowledgeActivation
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = false };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly byte[] Magic = "KBKP1"u8.ToArray();

    public static async Task<KnowledgeActivationResult> ActivateAsync(string directory, SignedKnowledgePack candidate,
        ReadOnlyMemory<byte> trustedPublicKey, string expectedPackId, long minimumRevision = 1, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A knowledge storage directory is required.", nameof(directory));
        ArgumentNullException.ThrowIfNull(candidate);
        var catalog = KnowledgeTrust.Verify(candidate.Payload, candidate.Signature, trustedPublicKey.Span, expectedPackId, minimumRevision);
        string hash = Convert.ToHexString(SHA256.HashData(candidate.Payload));
        string fileName = $"pack-{catalog.Revision}-{hash}.kbpack";
        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(directory);
            string root = Path.GetFullPath(directory);
            await using var processLock = new FileStream(Path.Combine(root, "activation.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None, 1, FileOptions.Asynchronous);
            string activePath = Path.Combine(root, "active.json");
            if (File.Exists(activePath))
            {
                var (currentCatalog, currentPointer, currentDataPath) = await LoadActiveAsync(root, trustedPublicKey, expectedPackId, minimumRevision, token).ConfigureAwait(false);
                if (catalog.Revision < currentCatalog.Revision) throw new InvalidDataException("Knowledge activation would roll back the active revision.");
                if (catalog.Revision == currentCatalog.Revision)
                {
                    if (!string.Equals(hash, currentPointer.PayloadSha256, StringComparison.Ordinal))
                        throw new InvalidDataException("Knowledge activation conflicts with the active revision.");
                    return new(catalog, activePath, currentDataPath, false, "The same verified revision is already active.");
                }
            }

            string dataPath = Path.Combine(root, fileName);
            byte[] container = Container(candidate);
            if (File.Exists(dataPath))
            {
                byte[] existing = await ReadBoundedAsync(dataPath, 32_000_073, token).ConfigureAwait(false);
                if (!existing.AsSpan().SequenceEqual(container)) throw new InvalidDataException("An immutable knowledge file conflicts with the verified candidate.");
            }
            else await WriteNewAsync(dataPath, container, token).ConfigureAwait(false);

            var pointer = new ActiveKnowledgePointer(catalog.PackId, catalog.Revision, fileName, hash, DateTimeOffset.UtcNow);
            byte[] pointerBytes = JsonSerializer.SerializeToUtf8Bytes(pointer);
            string temporary = Path.Combine(root, ".active-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                await WriteNewAsync(temporary, pointerBytes, token).ConfigureAwait(false);
                var verified = await ReadContainerAsync(dataPath, trustedPublicKey, expectedPackId, catalog.Revision, token).ConfigureAwait(false);
                if (verified.Revision != catalog.Revision) throw new InvalidDataException("Staged knowledge revision changed before activation.");
                File.Move(temporary, activePath, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return new(catalog, activePath, dataPath, true, "Activated a newer verified local knowledge revision. Prior immutable revisions were retained.");
        }
        finally { Gate.Release(); }
    }

    public static async Task<(KnowledgeCatalog Catalog, ActiveKnowledgePointer Pointer, string DataPath)> LoadActiveAsync(
        string directory, ReadOnlyMemory<byte> trustedPublicKey, string expectedPackId, long minimumRevision = 1,
        CancellationToken token = default)
    {
        string root = Path.GetFullPath(directory);
        byte[] pointerBytes = await ReadBoundedAsync(Path.Combine(root, "active.json"), 16_384, token).ConfigureAwait(false);
        using var document = JsonDocument.Parse(pointerBytes, new JsonDocumentOptions { MaxDepth = 4 });
        RejectDuplicateFields(document.RootElement);
        var pointer = document.RootElement.Deserialize<ActiveKnowledgePointer>(Json) ?? throw new InvalidDataException("Active knowledge pointer is missing.");
        // JSON can omit or null any field, so check presence before reading the strings.
        if (pointer.FileName is null || pointer.PayloadSha256 is null ||
            pointer.PackId != expectedPackId || pointer.Revision < minimumRevision || pointer.ActivatedUtc == default ||
            pointer.FileName != Path.GetFileName(pointer.FileName) || !pointer.FileName.StartsWith("pack-", StringComparison.Ordinal) ||
            pointer.PayloadSha256.Length != 64 || pointer.PayloadSha256.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException("Active knowledge pointer is invalid.");
        string dataPath = Path.Combine(root, pointer.FileName);
        var signed = await ReadSignedPackAsync(dataPath, token).ConfigureAwait(false);
        var catalog = KnowledgeTrust.Verify(signed.Payload, signed.Signature, trustedPublicKey.Span, expectedPackId,
            Math.Max(minimumRevision, pointer.Revision));
        string hash = Convert.ToHexString(SHA256.HashData(signed.Payload));
        if (catalog.Revision != pointer.Revision || !hash.Equals(pointer.PayloadSha256, StringComparison.Ordinal))
            throw new InvalidDataException("Active knowledge pointer does not match its verified data.");
        return (catalog, pointer, dataPath);
    }

    public static async Task<KnowledgeRecoveryResult> RecoverAsync(string directory, ReadOnlyMemory<byte> trustedPublicKey,
        string expectedPackId, long minimumRevision = 1, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(expectedPackId) || minimumRevision < 1 ||
            trustedPublicKey.Length is 0 or > 1024)
            throw new ArgumentException("Knowledge recovery requires storage, identity, revision and a trusted key.");
        string root = Path.GetFullPath(directory);
        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await using var processLock = new FileStream(Path.Combine(root, "activation.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None, 1, FileOptions.Asynchronous);
            string[] files = [.. Directory.EnumerateFiles(root, "pack-*.kbpack", SearchOption.TopDirectoryOnly)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Take(257)];
            if (files.Length > 256) throw new InvalidDataException("Knowledge recovery has more than 256 retained versions.");
            var accepted = new List<(KnowledgeCatalog Catalog, string Path, string Hash)>();
            int rejected = 0;
            foreach (string path in files)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var signed = await ReadSignedPackAsync(path, token).ConfigureAwait(false);
                    var catalog = KnowledgeTrust.Verify(signed.Payload, signed.Signature, trustedPublicKey.Span,
                        expectedPackId, minimumRevision);
                    string hash = Convert.ToHexString(SHA256.HashData(signed.Payload));
                    string expectedName = $"pack-{catalog.Revision}-{hash}.kbpack";
                    if (!string.Equals(Path.GetFileName(path), expectedName, StringComparison.Ordinal))
                        throw new InvalidDataException("Retained knowledge file name does not match its verified payload.");
                    accepted.Add((catalog, path, hash));
                }
                catch (Exception error) when (error is InvalidDataException or CryptographicException or JsonException or IOException)
                { rejected++; }
            }
            if (accepted.Count == 0) throw new InvalidDataException("No retained knowledge version passed recovery verification.");
            long newest = accepted.Max(item => item.Catalog.Revision);
            var finalists = accepted.Where(item => item.Catalog.Revision == newest).ToArray();
            if (finalists.Select(item => item.Hash).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new InvalidDataException("Retained knowledge versions conflict at the newest verified revision.");
            var selected = finalists[0];
            string activePath = Path.Combine(root, "active.json");
            var pointer = new ActiveKnowledgePointer(selected.Catalog.PackId, selected.Catalog.Revision,
                Path.GetFileName(selected.Path), selected.Hash, DateTimeOffset.UtcNow);
            string temporary = Path.Combine(root, ".active-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                await WriteNewAsync(temporary, JsonSerializer.SerializeToUtf8Bytes(pointer), token).ConfigureAwait(false);
                var verified = await ReadContainerAsync(selected.Path, trustedPublicKey, expectedPackId,
                    selected.Catalog.Revision, token).ConfigureAwait(false);
                if (verified.Revision != selected.Catalog.Revision)
                    throw new InvalidDataException("Recovered knowledge data changed before selection.");
                File.Move(temporary, activePath, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return new(selected.Catalog, activePath, selected.Path, files.Length, rejected,
                $"Recovered the newest verified retained database revision {selected.Catalog.Revision}. Rejected {rejected} retained files.");
        }
        finally { Gate.Release(); }
    }

    private static async Task<KnowledgeCatalog> ReadContainerAsync(string path, ReadOnlyMemory<byte> key, string id, long revision, CancellationToken token)
    {
        var pack = await ReadSignedPackAsync(path, token).ConfigureAwait(false);
        return KnowledgeTrust.Verify(pack.Payload, pack.Signature, key.Span, id, revision);
    }

    private static async Task<SignedKnowledgePack> ReadSignedPackAsync(string path, CancellationToken token)
    {
        byte[] bytes = await ReadBoundedAsync(path, 32_000_073, token).ConfigureAwait(false);
        if (bytes.Length < 73 || !bytes.AsSpan(0, Magic.Length).SequenceEqual(Magic)) throw new InvalidDataException("Knowledge container header is invalid.");
        int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(Magic.Length, 4));
        if (length is <= 0 or > 32_000_000 || bytes.Length != Magic.Length + 4 + length + 64) throw new InvalidDataException("Knowledge container length is invalid.");
        return new(bytes.AsSpan(9, length).ToArray(), bytes.AsSpan(9 + length, 64).ToArray());
    }

    private static byte[] Container(SignedKnowledgePack pack)
    {
        byte[] bytes = new byte[Magic.Length + 4 + pack.Payload.Length + pack.Signature.Length];
        Magic.CopyTo(bytes, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(Magic.Length, 4), pack.Payload.Length);
        pack.Payload.CopyTo(bytes, 9);
        pack.Signature.CopyTo(bytes, 9 + pack.Payload.Length);
        return bytes;
    }

    private static async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(true);
    }

    private static async Task<byte[]> ReadBoundedAsync(string path, int maximum, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is <= 0 || stream.Length > maximum) throw new InvalidDataException("Knowledge activation file size is invalid.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        return bytes;
    }

    private static void RejectDuplicateFields(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Active knowledge pointer is invalid.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (!value.EnumerateObject().All(property => names.Add(property.Name))) throw new InvalidDataException("Active knowledge pointer contains duplicate fields.");
    }
}
