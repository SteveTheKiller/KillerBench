using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KillerBench.Engine;

/// <summary>Interactive Microsoft Entra sign-in for one typed tenant, built without an authentication library.
/// Authorization code flow with PKCE, a one-shot loopback redirect on 127.0.0.1, state and nonce checks, and a tenant
/// pinned from the typed domain's published OpenID configuration. No client secret exists, nothing is written to disk,
/// no refresh token is requested, and the access token never appears in output or journals.</summary>
public static class EntraSignIn
{
    public const string Authority = "https://login.microsoftonline.com/";
    public const string IntuneScope = "https://graph.microsoft.com/DeviceManagementApps.ReadWrite.All";
    /// <summary>The Killer Tools multi-tenant public client. Empty until the app registration exists; sign-in refuses to start without it.</summary>
    public const string KillerToolsClientId = "";
    private static readonly Guid PersonalAccountsTenant = Guid.Parse("9188040d-6c67-4c5b-b112-36a304b66dad");

    public sealed record Request(string TenantDomain, Guid TenantId, string ClientId, Uri AuthorizeUri, string RedirectUri, string State, string Nonce, string Verifier);

    public static Uri OpenIdConfigurationUri(string tenantDomain) =>
        new(Authority + Uri.EscapeDataString(NormalizeTenant(tenantDomain)) + "/v2.0/.well-known/openid-configuration");

    public static string NormalizeTenant(string tenantDomain)
    {
        string value = (tenantDomain ?? "").Trim().TrimEnd('.').ToLowerInvariant();
        if (value.Length is < 3 or > 253 || !value.Contains('.') || value.StartsWith('.') ||
            value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '.')) || value.Split('.').Any(label => label.Length is 0 or > 63 || label.StartsWith('-') || label.EndsWith('-')))
            throw new ArgumentException("Type the tenant's domain, such as contoso.onmicrosoft.com or contoso.com.");
        return value;
    }

    /// <summary>Reads the tenant GUID from the typed domain's OpenID configuration. The issuer must be the Entra v2 issuer for a work or school tenant.</summary>
    public static Guid ParseTenantId(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("issuer", out var issuer) || issuer.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("The tenant configuration has no issuer.");
        string text = issuer.GetString()!;
        const string prefix = "https://login.microsoftonline.com/";
        const string suffix = "/v2.0";
        if (!text.StartsWith(prefix, StringComparison.Ordinal) || !text.EndsWith(suffix, StringComparison.Ordinal) ||
            !Guid.TryParseExact(text[prefix.Length..^suffix.Length], "D", out Guid tenant) || tenant == Guid.Empty || tenant == PersonalAccountsTenant)
            throw new InvalidDataException("The typed domain does not resolve to a work or school tenant.");
        return tenant;
    }

    public static Request CreateRequest(string tenantDomain, Guid tenantId, string clientId, int port, string scope = IntuneScope)
    {
        if (!Guid.TryParseExact(clientId, "D", out Guid client) || client == Guid.Empty)
            throw new InvalidOperationException("No Killer Tools app registration is configured, so sign-in cannot start.");
        if (tenantId == Guid.Empty || tenantId == PersonalAccountsTenant) throw new ArgumentException("A work or school tenant is required.");
        if (port is < 1024 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        string redirect = "http://localhost:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/";
        string state = Random(32), nonce = Random(32), verifier = Random(64);
        string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var query = new Dictionary<string, string>
        {
            ["client_id"] = client.ToString("D"),
            ["response_type"] = "code",
            ["redirect_uri"] = redirect,
            ["response_mode"] = "query",
            ["scope"] = scope + " openid",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account",
            ["domain_hint"] = NormalizeTenant(tenantDomain)
        };
        var uri = new Uri(Authority + tenantId.ToString("D") + "/oauth2/v2.0/authorize?" +
            string.Join("&", query.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value))));
        return new(NormalizeTenant(tenantDomain), tenantId, client.ToString("D"), uri, redirect, state, nonce, verifier);
    }

    /// <summary>Extracts the authorization code from the loopback request line. Any state mismatch, error or extra code is rejected.</summary>
    public static string ParseRedirect(string requestLine, Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parts = (requestLine ?? "").Split(' ');
        if (parts.Length != 3 || parts[0] != "GET" || !parts[2].StartsWith("HTTP/1.", StringComparison.Ordinal) || !parts[1].StartsWith("/?", StringComparison.Ordinal))
            throw new InvalidDataException("The sign-in redirect was not recognized.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in parts[1][2..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int split = pair.IndexOf('=');
            if (split <= 0) throw new InvalidDataException("The sign-in redirect was malformed.");
            string key = Uri.UnescapeDataString(pair[..split].Replace('+', ' '));
            if (!values.TryAdd(key, Uri.UnescapeDataString(pair[(split + 1)..].Replace('+', ' ')))) throw new InvalidDataException("The sign-in redirect repeated a field.");
        }
        if (!values.TryGetValue("state", out string? state) || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(state), Encoding.ASCII.GetBytes(request.State)))
            throw new InvalidDataException("The sign-in redirect did not match this request.");
        if (values.TryGetValue("error", out string? error))
            throw new InvalidOperationException("Sign-in was not completed: " + Sanitize(error) + (values.TryGetValue("error_description", out string? detail) ? " " + Sanitize(detail) : ""));
        if (!values.TryGetValue("code", out string? code) || code.Length is 0 or > 4096 || code.Any(char.IsControl))
            throw new InvalidDataException("The sign-in redirect carried no authorization code.");
        return code;
    }

    public static FormUrlEncodedContent TokenRequest(Request request, string code, string scope = IntuneScope) => new(new Dictionary<string, string>
    {
        ["client_id"] = request.ClientId,
        ["grant_type"] = "authorization_code",
        ["code"] = code,
        ["redirect_uri"] = request.RedirectUri,
        ["code_verifier"] = request.Verifier,
        ["scope"] = scope + " openid"
    });

    /// <summary>Validates the token response against the pinned tenant, client and nonce. Returns the access token only inside an in-memory holder.</summary>
    public static EntraAccessToken ParseTokenResponse(string json, Request request, DateTimeOffset now)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("error", out var error))
            throw new InvalidOperationException("The token request was refused: " + Sanitize(error.GetString() ?? ""));
        if (!root.TryGetProperty("token_type", out var type) || !string.Equals(type.GetString(), "Bearer", StringComparison.OrdinalIgnoreCase) ||
            !root.TryGetProperty("access_token", out var access) || access.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("id_token", out var id) || id.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("expires_in", out var expires) || !expires.TryGetInt32(out int seconds) || seconds is < 60 or > 86400)
            throw new InvalidDataException("The token response was incomplete.");
        using var claims = Claims(id.GetString()!);
        var c = claims.RootElement;
        string? Claim(string name) => c.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (!Guid.TryParse(Claim("tid"), out Guid tid) || tid != request.TenantId)
            throw new InvalidDataException("The account signed in to a different tenant than the one typed.");
        if (Claim("aud") != request.ClientId || Claim("nonce") != request.Nonce ||
            Claim("iss") != "https://login.microsoftonline.com/" + request.TenantId.ToString("D") + "/v2.0")
            throw new InvalidDataException("The sign-in response did not match this request.");
        string account = Sanitize(Claim("preferred_username") ?? Claim("upn") ?? "");
        return new EntraAccessToken(access.GetString()!, request.TenantId, request.TenantDomain, account, now.AddSeconds(seconds - 30));
    }

    /// <summary>Runs the whole interactive flow on Windows: resolve the tenant, open the system browser, wait for one loopback redirect and redeem the code.</summary>
    public static async Task<EntraAccessToken> SignInAsync(string tenantDomain, HttpClient http, Action<Uri> openBrowser,
        IProgress<string>? progress = null, CancellationToken token = default, string clientId = KillerToolsClientId)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(openBrowser);
        string domain = NormalizeTenant(tenantDomain);
        if (string.IsNullOrEmpty(clientId)) throw new InvalidOperationException("No Killer Tools app registration is configured, so sign-in cannot start.");
        progress?.Report("Looking up the " + domain + " tenant.");
        Guid tenant = ParseTenantId(await http.GetStringAsync(OpenIdConfigurationUri(domain), token).ConfigureAwait(false));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(1);
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var request = CreateRequest(domain, tenant, clientId, port);
        progress?.Report("Opening the Microsoft sign-in page in your browser.");
        openBrowser(request.AuthorizeUri);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        using var client = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
        listener.Stop();
        await using var stream = client.GetStream();
        string line = await ReadRequestLineAsync(stream, timeout.Token).ConfigureAwait(false);
        string code;
        try { code = ParseRedirect(line, request); }
        finally
        {
            byte[] page = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nConnection: close\r\nCache-Control: no-store\r\n\r\nYou can close this tab and return to KillerBench.");
            await stream.WriteAsync(page, CancellationToken.None).ConfigureAwait(false);
        }
        progress?.Report("Redeeming the sign-in with Microsoft.");
        using var content = TokenRequest(request, code);
        using var response = await http.PostAsync(new Uri(Authority + tenant.ToString("D") + "/oauth2/v2.0/token"), content, token).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        return ParseTokenResponse(body, request, DateTimeOffset.UtcNow);
    }

    private static async Task<string> ReadRequestLineAsync(Stream stream, CancellationToken token)
    {
        var buffer = new byte[8192];
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total), token).ConfigureAwait(false);
            if (read == 0) break;
            total += read;
            int end = Array.IndexOf(buffer, (byte)'\n', 0, total);
            if (end >= 0) return Encoding.ASCII.GetString(buffer, 0, end).TrimEnd('\r');
        }
        throw new InvalidDataException("The sign-in redirect was too long or incomplete.");
    }

    private static JsonDocument Claims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) throw new InvalidDataException("The identity token was malformed.");
        string payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        try { return JsonDocument.Parse(Convert.FromBase64String(payload)); }
        catch (FormatException error) { throw new InvalidDataException("The identity token was malformed.", error); }
    }

    private static string Random(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Sanitize(string value) => new(value.Where(c => !char.IsControl(c)).Take(300).ToArray());
}

/// <summary>An access token held only in memory. It is excluded from serialization and never rendered by ToString.</summary>
public sealed class EntraAccessToken(string value, Guid tenantId, string tenantDomain, string account, DateTimeOffset expiresUtc)
{
    [System.Text.Json.Serialization.JsonIgnore] internal string Value { get; } = value;
    public Guid TenantId { get; } = tenantId;
    public string TenantDomain { get; } = tenantDomain;
    public string Account { get; } = account;
    public DateTimeOffset ExpiresUtc { get; } = expiresUtc;
    public bool Expired(DateTimeOffset now) => now >= ExpiresUtc;
    public System.Net.Http.Headers.AuthenticationHeaderValue Authorization(DateTimeOffset now) =>
        Expired(now) ? throw new InvalidOperationException("The sign-in has expired. Sign in again.") : new("Bearer", Value);
    public override string ToString() => "Signed in to " + TenantDomain + " as " + Account;
}
