using System.Security.Cryptography;
using System.Text.Json;

namespace KillerBench.Engine.Knowledge;

public sealed record KnowledgeDatabaseConfiguration(string PackId, long MinimumRevision, Uri PayloadUri,
    Uri SignatureUri, ReadOnlyMemory<byte> TrustedPublicKey);

public sealed record KnowledgeUpdatePlan(IReadOnlyList<KnowledgeUpdateRequest> Requests,
    IReadOnlyList<KnowledgeEndpoint> Endpoints);

public sealed record KnowledgeDatabaseStatus(string PackId, bool Installed, bool Verified, long? Revision,
    int? EntryCount, DateOnly? PublishedOn, DateTimeOffset? ActivatedUtc, string Detail);

public sealed class KnowledgeUpdateConfiguration
{
    private const int MaximumConfigurationBytes = 256 * 1024;
    private readonly IReadOnlyList<KnowledgeDatabaseConfiguration> databases;

    private KnowledgeUpdateConfiguration(IReadOnlyList<KnowledgeDatabaseConfiguration> databases) =>
        this.databases = databases;

    public IReadOnlyList<KnowledgeDatabaseConfiguration> Databases => databases;

    public static async Task<KnowledgeUpdateConfiguration> ReadFileAsync(string path, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A database update configuration path is required.", nameof(path));
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length is <= 0 or > MaximumConfigurationBytes)
            throw new InvalidDataException("Database update configuration size is unsupported.");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        return Read(bytes, token);
    }

    public static KnowledgeUpdateConfiguration Read(ReadOnlyMemory<byte> bytes, CancellationToken token = default)
    {
        if (bytes.Length is <= 0 or > MaximumConfigurationBytes)
            throw new InvalidDataException("Database update configuration size is unsupported.");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        RejectDuplicateFields(document.RootElement, token);
        JsonElement root = document.RootElement;
        RequireObject(root, "Database update configuration must be an object.");
        RejectUnknownFields(root, ["schemaVersion", "databases"]);
        if (!root.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number ||
            !schema.TryGetInt32(out int schemaVersion) || schemaVersion != 1)
            throw new InvalidDataException("Database update configuration schemaVersion must be 1.");
        if (!root.TryGetProperty("databases", out var databaseArray) || databaseArray.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Database update configuration requires a databases array.");

        var parsed = new List<KnowledgeDatabaseConfiguration>();
        foreach (JsonElement item in databaseArray.EnumerateArray())
        {
            token.ThrowIfCancellationRequested();
            if (parsed.Count == 32) throw new InvalidDataException("Database update configuration supports at most 32 databases.");
            RequireObject(item, "Every database update entry must be an object.");
            RejectUnknownFields(item, ["packId", "minimumRevision", "payloadUrl", "signatureUrl", "trustedPublicKey"]);
            string packId = RequiredString(item, "packId", 128);
            if (!ValidPackId(packId)) throw new InvalidDataException("Database pack identities must use lowercase ASCII letters, digits, periods and hyphens.");
            if (!item.TryGetProperty("minimumRevision", out var revisionValue) || revisionValue.ValueKind != JsonValueKind.Number ||
                !revisionValue.TryGetInt64(out long minimumRevision) || minimumRevision < 1)
                throw new InvalidDataException("Database minimumRevision must be a positive integer.");
            Uri payload = RequiredHttpsUri(item, "payloadUrl");
            Uri signature = RequiredHttpsUri(item, "signatureUrl");
            byte[] key;
            try { key = Convert.FromBase64String(RequiredString(item, "trustedPublicKey", 2048)); }
            catch (FormatException error) { throw new InvalidDataException("Database trustedPublicKey must be valid base64.", error); }
            ValidateKey(key);
            parsed.Add(new(packId, minimumRevision, payload, signature, key));
        }
        if (parsed.Count == 0) throw new InvalidDataException("Database update configuration requires 1 to 32 databases.");
        if (parsed.Select(item => item.PackId).Distinct(StringComparer.Ordinal).Count() != parsed.Count)
            throw new InvalidDataException("Database update configuration contains duplicate pack identities.");
        return new(Array.AsReadOnly(parsed.ToArray()));
    }

    public KnowledgeUpdatePlan CreatePlan(string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(storageRoot)) throw new ArgumentException("A database storage root is required.", nameof(storageRoot));
        string root = Path.GetFullPath(storageRoot);
        var requests = databases.Select(item => new KnowledgeUpdateRequest(item.PackId, item.MinimumRevision,
            Path.Combine(root, item.PackId), item.TrustedPublicKey)).ToArray();
        var endpoints = databases.Select(item => new KnowledgeEndpoint(item.PackId, item.PayloadUri, item.SignatureUri)).ToArray();
        return new(Array.AsReadOnly(requests), Array.AsReadOnly(endpoints));
    }

    public async Task<IReadOnlyList<KnowledgeDatabaseStatus>> InspectAsync(string storageRoot,
        CancellationToken token = default)
    {
        var plan = CreatePlan(storageRoot);
        var results = new List<KnowledgeDatabaseStatus>(plan.Requests.Count);
        foreach (var request in plan.Requests)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var (activeCatalog, activePointer, _) = await KnowledgeActivation.LoadActiveAsync(request.StorageDirectory, request.TrustedPublicKey,
                    request.PackId, request.MinimumRevision, token).ConfigureAwait(false);
                results.Add(new(request.PackId, true, true, activeCatalog.Revision, activeCatalog.Count,
                    activeCatalog.PublishedOn, activePointer.ActivatedUtc, "Installed database is verified."));
            }
            catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
            { results.Add(new(request.PackId, false, false, null, null, null, null, "Database is not installed.")); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
                CryptographicException or JsonException)
            { results.Add(new(request.PackId, true, false, null, null, null, null, "Installed database could not be verified: " + error.Message)); }
        }
        return results.AsReadOnly();
    }

    private static void ValidateKey(byte[] key)
    {
        if (key.Length is 0 or > 1024) throw new InvalidDataException("Database trusted public key size is unsupported.");
        try
        {
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(key, out int consumed);
            if (consumed != key.Length || verifier.KeySize != 256 ||
                verifier.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
                throw new InvalidDataException("Database trusted public key must be an ECDSA P-256 public key.");
        }
        catch (CryptographicException error)
        { throw new InvalidDataException("Database trusted public key is invalid.", error); }
    }

    private static Uri RequiredHttpsUri(JsonElement item, string name)
    {
        string value = RequiredString(item, name, 2048);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) || uri.Fragment.Length != 0)
            throw new InvalidDataException($"Database {name} must be an absolute HTTPS URL without credentials or a fragment.");
        return uri;
    }

    private static string RequiredString(JsonElement item, string name, int maximum)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"Database {name} is required.");
        string text = value.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximum || text.Any(char.IsControl))
            throw new InvalidDataException($"Database {name} is invalid.");
        return text;
    }

    private static bool ValidPackId(string value) => value.Length <= 128 &&
        value[0] is >= 'a' and <= 'z' && value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-');

    private static void RejectUnknownFields(JsonElement value, IReadOnlyCollection<string> allowed)
    {
        foreach (JsonProperty property in value.EnumerateObject())
            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
                throw new InvalidDataException("Database update configuration contains an unknown field: " + property.Name);
    }

    private static void RequireObject(JsonElement value, string message)
    { if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException(message); }

    private static void RejectDuplicateFields(JsonElement value, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Database update configuration contains duplicate fields.");
                RejectDuplicateFields(property.Value, token);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in value.EnumerateArray()) RejectDuplicateFields(item, token);
    }
}
