using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record CertificateChainObservation(string Store, string Thumbprint, string Subject,
    bool Trusted, IReadOnlyList<string> Statuses);

public sealed record CertificateChainInventory(bool Complete, IReadOnlyList<CertificateChainObservation> Certificates);

public static class CertificateChainDiagnostics
{
    private const int MaximumCertificates = 256;
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);
    private static readonly X509ChainStatusFlags KnownStatusFlags = Enum.GetValues<X509ChainStatusFlags>()
        .Aggregate(X509ChainStatusFlags.NoError, (current, value) => current | value);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Certificate-chain inspection requires Windows.");
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
                    trusted = item.Trusted,
                    statuses = item.Statuses
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

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail = "Local personal-certificate chain evidence was unavailable, incomplete or malformed. No certificate, trust setting or private key was changed.") =>
            new("Personal certificate chain trust", CheckState.Unknown, detail, Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !Unique(root, "complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "certificates", out var certificates) || certificates.ValueKind != JsonValueKind.Array ||
                certificates.GetArrayLength() > MaximumCertificates) return Unknown();
            if (!complete.GetBoolean()) return Unknown("More than 256 personal certificates were reported. The bounded chain evidence is incomplete and nothing was changed.");

            var observations = new List<CertificateChainObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in certificates.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 5 ||
                    !Text(item, "store", 32, out string store) || store is not ("LocalMachine/My" or "CurrentUser/My") ||
                    !Text(item, "thumbprint", 128, out string thumbprint) || thumbprint.Length != 40 || !thumbprint.All(Uri.IsHexDigit) ||
                    !Text(item, "subject", 2048, out string subject) ||
                    !Unique(item, "trusted", out var trusted) || trusted.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !Unique(item, "statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array || statuses.GetArrayLength() > 32 ||
                    !identities.Add(store + "|" + thumbprint)) return Unknown();
                var statusNames = new List<string>();
                foreach (var status in statuses.EnumerateArray())
                {
                    if (status.ValueKind != JsonValueKind.String || status.GetString() is not string name || name.Length is 0 or > 128 ||
                        !Enum.TryParse<X509ChainStatusFlags>(name, false, out var parsed) || parsed == X509ChainStatusFlags.NoError ||
                        (parsed & ~KnownStatusFlags) != 0 || !statusNames.AddUnique(name)) return Unknown();
                }
                if (trusted.GetBoolean() != (statusNames.Count == 0)) return Unknown();
                observations.Add(new(store, thumbprint.ToUpperInvariant(), subject, trusted.GetBoolean(), statusNames));
            }

            var failures = observations.Where(item => !item.Trusted).ToArray();
            string detail = observations.Count == 0
                ? "No certificates were present in LocalMachine/My or CurrentUser/My. No local chain was available to evaluate."
                : $"Built local, cache-only chains for {observations.Count} personal certificates without revocation checks or certificate downloads. {failures.Length} did not build to a locally trusted chain.";
            if (failures.Length > 0)
            {
                detail += "\n" + string.Join("\n", failures.Take(32).Select(item =>
                    $"{item.Store} | {item.Thumbprint} | {DisplaySubject(item.Subject)} | {string.Join(", ", item.Statuses)}"));
                if (failures.Length > 32) detail += $"\n{failures.Length - 32} additional failures remain in raw evidence.";
            }
            detail += "\nA local chain result does not establish revocation, server presentation, binding, policy ownership or private-key usability. No certificate, trust setting, enrollment, binding or private key was exported, changed or deleted.";
            return new("Personal certificate chain trust", failures.Length == 0 ? CheckState.Information : CheckState.Attention,
                detail, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            return Unknown();
        }
    }

    private static CertificateChainInventory Collect(CancellationToken token)
    {
        var observations = new List<CertificateChainObservation>();
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
                    if (observations.Count == MaximumCertificates)
                        return new(false, observations);
                    using var chain = new X509Chain();
                    chain.ChainPolicy.DisableCertificateDownloads = true;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.Zero;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    bool trusted = chain.Build(certificate);
                    string[] statuses = chain.ChainStatus.Select(item => item.Status.ToString()).Distinct(StringComparer.Ordinal).ToArray();
                    if (!trusted && statuses.Length == 0) statuses = [X509ChainStatusFlags.PartialChain.ToString()];
                    observations.Add(new(location + "/My", certificate.Thumbprint ?? "", certificate.Subject ?? "", trusted, statuses));
                }
            }
        }
        return new(true, observations);
    }

    private static string DisplaySubject(string subject) => string.IsNullOrWhiteSpace(subject) ? "No subject" : subject;

    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!Unique(owner, name, out var field) || field.ValueKind != JsonValueKind.String) return false;
        value = field.GetString() ?? "";
        return value.Length <= maximum && !value.Any(char.IsControl);
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }

    private static bool AddUnique(this List<string> values, string value)
    {
        if (values.Contains(value, StringComparer.Ordinal)) return false;
        values.Add(value);
        return true;
    }
}
