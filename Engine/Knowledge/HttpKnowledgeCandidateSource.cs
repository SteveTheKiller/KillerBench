using System.Net.Http.Headers;

namespace KillerBench.Engine.Knowledge;

public sealed record KnowledgeEndpoint(string PackId, Uri PayloadUri, Uri SignatureUri);

public sealed class HttpKnowledgeCandidateSource : IKnowledgeCandidateSource
{
    private const int MaximumPayloadBytes = 32_000_000;
    private readonly HttpClient client;
    private readonly Dictionary<string, KnowledgeEndpoint> endpoints;

    public HttpKnowledgeCandidateSource(HttpClient client, IReadOnlyCollection<KnowledgeEndpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(endpoints);
        if (endpoints.Count is 0 or > 32 || endpoints.Any(endpoint => endpoint is null))
            throw new ArgumentException("Supply 1 to 32 database endpoints.", nameof(endpoints));
        var map = new Dictionary<string, KnowledgeEndpoint>(StringComparer.Ordinal);
        foreach (var endpoint in endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.PackId) || endpoint.PackId.Length > 128 ||
                endpoint.PackId.Any(char.IsControl) || !ValidUri(endpoint.PayloadUri) || !ValidUri(endpoint.SignatureUri) ||
                !map.TryAdd(endpoint.PackId, endpoint))
                throw new ArgumentException("Database endpoints require unique identities and absolute HTTPS URLs without credentials.", nameof(endpoints));
        }
        this.client = client;
        this.endpoints = map;
    }

    public async Task<SignedKnowledgePack> FetchAsync(string packId, CancellationToken token)
    {
        if (!endpoints.TryGetValue(packId, out var endpoint))
            throw new InvalidDataException("No trusted endpoint is configured for this database identity.");
        byte[] payload = await DownloadAsync(endpoint.PayloadUri, MaximumPayloadBytes, token).ConfigureAwait(false);
        byte[] signature = await DownloadAsync(endpoint.SignatureUri, 64, token).ConfigureAwait(false);
        if (signature.Length != 64) throw new InvalidDataException("The detached database signature must be exactly 64 bytes.");
        return new(payload, signature);
    }

    private async Task<byte[]> DownloadAsync(Uri uri, int maximumBytes, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        Uri? finalUri = response.RequestMessage?.RequestUri;
        if (!ValidUri(finalUri)) throw new InvalidDataException("Database retrieval did not finish at an HTTPS endpoint.");
        if (response.Content.Headers.ContentLength is long length && (length <= 0 || length > maximumBytes))
            throw new InvalidDataException("Database download size is unsupported.");
        await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var output = new MemoryStream(Math.Min(maximumBytes, 81920));
        var buffer = new byte[81920];
        while (true)
        {
            int read = await input.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maximumBytes + 1 - (int)output.Length)), token)
                .ConfigureAwait(false);
            if (read == 0) break;
            output.Write(buffer, 0, read);
            if (output.Length > maximumBytes) throw new InvalidDataException("Database download size is unsupported.");
        }
        if (output.Length == 0) throw new InvalidDataException("Database download is empty.");
        return output.ToArray();
    }

    private static bool ValidUri(Uri? uri) => uri is { IsAbsoluteUri: true } && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) && !string.IsNullOrWhiteSpace(uri.Host) && uri.Fragment.Length == 0;
}
