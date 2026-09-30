using System.IO;
using System.Text;
using System.Text.Json;
using KillerBench.Engine;

internal static class EntraSignInChecks
{
    public static void Run()
    {
        var tenant = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        const string client = "12345678-1234-1234-1234-1234567890ab";

        Require(EntraSignIn.NormalizeTenant(" Contoso.OnMicrosoft.com. ") == "contoso.onmicrosoft.com", "Tenant domains normalize");
        foreach (string bad in new[] { "contoso", "contoso..com", "-a.com", "a b.com", "contoso.com/evil", "", "x@contoso.com" })
            Throws<ArgumentException>(() => EntraSignIn.NormalizeTenant(bad), "Rejects tenant " + bad);
        Require(EntraSignIn.ParseTenantId("{\"issuer\":\"https://login.microsoftonline.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0\"}") == tenant, "Tenant id comes from the Entra issuer");
        Throws<InvalidDataException>(() => EntraSignIn.ParseTenantId("{\"issuer\":\"https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0\"}"), "Personal accounts tenant is refused");
        Throws<InvalidDataException>(() => EntraSignIn.ParseTenantId("{\"issuer\":\"https://evil.example/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0\"}"), "Foreign issuer is refused");
        Throws<InvalidOperationException>(() => EntraSignIn.CreateRequest("contoso.com", tenant, "", 50000), "Sign-in refuses to start without an app registration");

        var request = EntraSignIn.CreateRequest("contoso.com", tenant, client, 50000);
        string query = request.AuthorizeUri.Query;
        Require(request.AuthorizeUri.Host == "login.microsoftonline.com" && request.AuthorizeUri.AbsolutePath == "/" + tenant.ToString("D") + "/oauth2/v2.0/authorize" &&
            query.Contains("code_challenge_method=S256", StringComparison.Ordinal) && query.Contains("state=" + request.State, StringComparison.Ordinal) &&
            !query.Contains(request.Verifier, StringComparison.Ordinal) && !query.Contains("offline_access", StringComparison.Ordinal) &&
            request.RedirectUri == "http://localhost:50000/" && request.State.Length >= 40 && request.Verifier.Length >= 80,
            "The authorize request pins the tenant, uses PKCE and state, and asks for no refresh token");
        Require(EntraSignIn.CreateRequest("contoso.com", tenant, client, 50000).State != request.State, "Each request has a fresh state");

        Require(EntraSignIn.ParseRedirect("GET /?code=abc%2B1&state=" + request.State + " HTTP/1.1", request) == "abc+1", "A matching redirect yields the code");
        Throws<InvalidDataException>(() => EntraSignIn.ParseRedirect("GET /?code=abc&state=wrong HTTP/1.1", request), "A foreign state is refused");
        Throws<InvalidDataException>(() => EntraSignIn.ParseRedirect("GET /?code=abc HTTP/1.1", request), "A missing state is refused");
        Throws<InvalidDataException>(() => EntraSignIn.ParseRedirect("GET /?code=a&code=b&state=" + request.State + " HTTP/1.1", request), "A repeated field is refused");
        Throws<InvalidOperationException>(() => EntraSignIn.ParseRedirect("GET /?error=access_denied&state=" + request.State + " HTTP/1.1", request), "A sign-in error is reported");
        Throws<InvalidDataException>(() => EntraSignIn.ParseRedirect("POST /?code=a&state=" + request.State + " HTTP/1.1", request), "Only the GET redirect is accepted");

        string Response(Guid tid, string aud, string nonce, string iss) => JsonSerializer.Serialize(new
        {
            token_type = "Bearer", access_token = "SECRET-ACCESS-TOKEN", expires_in = 3600,
            id_token = "e30." + Base64Url(JsonSerializer.Serialize(new { tid = tid.ToString("D"), aud, nonce, iss, preferred_username = "tech@contoso.com" })) + ".sig"
        });
        string issuer = "https://login.microsoftonline.com/" + tenant.ToString("D") + "/v2.0";
        var now = DateTimeOffset.UtcNow;
        var token = EntraSignIn.ParseTokenResponse(Response(tenant, client, request.Nonce, issuer), request, now);
        Require(token.Account == "tech@contoso.com" && token.TenantId == tenant && token.Authorization(now).Parameter == "SECRET-ACCESS-TOKEN" &&
            !token.ToString().Contains("SECRET", StringComparison.Ordinal) && !JsonSerializer.Serialize(token).Contains("SECRET", StringComparison.Ordinal),
            "The access token is usable but never rendered or serialized");
        Throws<InvalidOperationException>(() => token.Authorization(now.AddHours(2)), "An expired sign-in is refused");
        Throws<InvalidDataException>(() => EntraSignIn.ParseTokenResponse(Response(Guid.NewGuid(), client, request.Nonce, issuer), request, now), "Another tenant's token is refused");
        Throws<InvalidDataException>(() => EntraSignIn.ParseTokenResponse(Response(tenant, Guid.NewGuid().ToString("D"), request.Nonce, issuer), request, now), "Another client's token is refused");
        Throws<InvalidDataException>(() => EntraSignIn.ParseTokenResponse(Response(tenant, client, "replayed", issuer), request, now), "A replayed nonce is refused");
        Throws<InvalidOperationException>(() => EntraSignIn.ParseTokenResponse("{\"error\":\"invalid_grant\"}", request, now), "A refused grant is reported");
        Console.WriteLine("PASS: Entra sign-in pins the typed tenant, uses PKCE, state and nonce, refuses personal and foreign tenants, keeps the token in memory and needs a configured app registration; no network used.");
    }

    private static string Base64Url(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }
}
