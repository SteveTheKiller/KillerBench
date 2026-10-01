using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace KillerBench.Engine.Security;

public sealed record CertificateObservation(string Store, string Thumbprint, string Subject, string Issuer,
    DateTimeOffset NotBeforeUtc, DateTimeOffset NotAfterUtc);

public sealed record CertificateInventory(bool Complete, IReadOnlyList<CertificateObservation> Certificates);

public static class CertificateDiagnostics
{
    private const int MaximumCertificates = 256;
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Certificate-store inspection requires Windows.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(Limit);
        try
        {
            var inventory = await Task.Run(() => Collect(timeout.Token), CancellationToken.None)
                .WaitAsync(timeout.Token).ConfigureAwait(false);
            string output = JsonSerializer.Serialize(new
            {
                complete = inventory.Complete,
                certificates = inventory.Certificates.Select(item => new
                {
                    store = item.Store,
                    thumbprint = item.Thumbprint,
                    subject = item.Subject,
                    issuer = item.Issuer,
                    notBefore = item.NotBeforeUtc.ToString("O", CultureInfo.InvariantCulture),
                    notAfter = item.NotAfterUtc.ToString("O", CultureInfo.InvariantCulture)
                })
            });
            return Interpret(new ToolResult(0, output, "", false));
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return Interpret(new ToolResult(-1, "", "", false) { TimedOut = true });
        }
        catch (Exception error) when (error is CryptographicException or UnauthorizedAccessException or IOException)
        {
            return Interpret(new ToolResult(1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult result, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail = "Personal certificate expiration evidence was unavailable, incomplete or malformed. No certificate or private key was changed.") =>
            new("Personal certificate expiration", CheckState.Unknown, detail, Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !Unique(root, "complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "certificates", out var certificates) || certificates.ValueKind != JsonValueKind.Array || certificates.GetArrayLength() > MaximumCertificates)
                return Unknown();
            if (!complete.GetBoolean()) return Unknown("More than 256 expired, expiring or not-yet-valid personal certificates were reported. The bounded evidence is incomplete and nothing was changed.");

            var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
            var cutoff = now.AddDays(30);
            var observations = new List<CertificateObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in certificates.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 6 ||
                    !Text(item, "store", 32, out string store) || store is not ("LocalMachine/My" or "CurrentUser/My") ||
                    !Text(item, "thumbprint", 128, out string thumbprint) || thumbprint.Length != 40 || !thumbprint.All(Uri.IsHexDigit) ||
                    !Text(item, "subject", 2048, out string subject) || !Text(item, "issuer", 2048, out string issuer) ||
                    !Date(item, "notBefore", out var notBefore) || !Date(item, "notAfter", out var notAfter) || notAfter <= notBefore ||
                    !identities.Add(store + "|" + thumbprint) || (notAfter > cutoff && notBefore <= now)) return Unknown();
                observations.Add(new(store, thumbprint.ToUpperInvariant(), subject, issuer, notBefore, notAfter));
            }

            int expired = observations.Count(item => item.NotAfterUtc <= now);
            int notYetValid = observations.Count(item => item.NotBeforeUtc > now);
            int expiring = observations.Count - expired - notYetValid;
            string detail = observations.Count == 0
                ? "No expired, next-30-day expiry or not-yet-valid certificates were reported in LocalMachine/My or CurrentUser/My. This does not evaluate trust chains, revocation, private-key access or other stores."
                : $"Reported personal certificate observations: {expired} expired, {expiring} expiring within 30 days, {notYetValid} not yet valid.";
            if (observations.Count > 0)
            {
                detail += "\n" + string.Join("\n", observations.Take(32).Select(item =>
                    $"{item.Store} | {item.NotAfterUtc:O} | {item.Thumbprint} | {(string.IsNullOrWhiteSpace(item.Subject) ? "No subject" : item.Subject)}"));
                if (observations.Count > 32) detail += $"\n{observations.Count - 32} additional observations remain in raw evidence.";
            }
            detail += "\nNo certificate, trust setting, enrollment, binding or private key was exported, changed or deleted.";
            return new("Personal certificate expiration", observations.Count == 0 ? CheckState.Information : CheckState.Attention, detail, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException) { return Unknown(); }
    }

    private static CertificateInventory Collect(CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(30);
        var observations = new List<CertificateObservation>();
        foreach (var location in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
        {
            token.ThrowIfCancellationRequested();
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            foreach (X509Certificate2 certificate in store.Certificates)
            {
                using (certificate)
                {
                    token.ThrowIfCancellationRequested();
                    var notBefore = new DateTimeOffset(certificate.NotBefore).ToUniversalTime();
                    var notAfter = new DateTimeOffset(certificate.NotAfter).ToUniversalTime();
                    if (notAfter > cutoff && notBefore <= now) continue;
                    if (observations.Count == MaximumCertificates) return new(false, observations);
                    observations.Add(new(location + "/My", certificate.Thumbprint ?? "", certificate.Subject ?? "",
                        certificate.Issuer ?? "", notBefore, notAfter));
                }
            }
        }
        return new(true, [.. observations.OrderBy(item => item.NotAfterUtc).ThenBy(item => item.Store)
            .ThenBy(item => item.Thumbprint)]);
    }

    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!Unique(owner, name, out var field) || field.ValueKind != JsonValueKind.String) return false;
        value = field.GetString() ?? "";
        return value.Length <= maximum && !value.Any(char.IsControl);
    }

    private static bool Date(JsonElement owner, string name, out DateTimeOffset value)
    {
        value = default;
        return Unique(owner, name, out var field) && field.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParseExact(field.GetString(), "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value) && value.Offset == TimeSpan.Zero;
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
