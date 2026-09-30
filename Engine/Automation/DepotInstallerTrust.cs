using System.Text;
using System.Text.Json;

namespace KillerBench.Engine;

public enum DepotInstallerKind { Msi, Exe }

/// <summary>One vendor installer source. The URL and silent arguments come from the DEPOT script; the signer organization is the trust anchor and is set only after it was confirmed from a known-good signed vendor binary.</summary>
public sealed record DepotInstallerSource(DepotApplications Application, string Url, DepotInstallerKind Kind, string SilentArguments,
    string? SignerOrganization, string TrustNote);

public enum DepotInstallerTrustOutcome { Trusted, Untrusted, Unknown }
public sealed record DepotInstallerTrustResult(DepotApplications Application, DepotInstallerTrustOutcome Outcome, string Detail,
    string? Status = null, string? Signer = null, string? Thumbprint = null);

public static class DepotInstallerTrust
{
    public const long MaximumInstallerBytes = 4L * 1024 * 1024 * 1024;

    public static IReadOnlyList<DepotInstallerSource> Sources { get; } =
    [
        new(DepotApplications.OneDrive, "https://go.microsoft.com/fwlink/?linkid=844652", DepotInstallerKind.Exe, "/allusers /silent",
            "Microsoft Corporation", "Microsoft signs its installers as O=Microsoft Corporation, confirmed from signed Microsoft binaries."),
        new(DepotApplications.Teams, "https://go.microsoft.com/fwlink/?linkid=2196106", DepotInstallerKind.Exe, "/silent",
            "Microsoft Corporation", "Microsoft signs its installers as O=Microsoft Corporation, confirmed from signed Microsoft binaries."),
        new(DepotApplications.Chrome, "https://dl.google.com/dl/chrome/install/googlechromestandaloneenterprise64.msi", DepotInstallerKind.Msi, "/qn /norestart ALLUSERS=1",
            "Google LLC", "Google signs Chrome as O=Google LLC, confirmed from a signed Chrome binary."),
        new(DepotApplications.AcrobatReader, "https://ardownload2.adobe.com/pub/adobe/reader/win/AcrobatDC/2300820555/AcroRdrDC2300820555_en_US.exe", DepotInstallerKind.Exe,
            "/sAll /rs /msi EULA_ACCEPT=YES /qn REBOOT=ReallySuppress", null,
            "Adobe's signer organization has not been confirmed from a known-good signed Adobe binary yet, so no Adobe installer is trusted. The script's pinned 2023 version is also stale."),
        new(DepotApplications.Zoom, "https://zoom.us/client/latest/ZoomInstallerFull.msi?archType=x64", DepotInstallerKind.Msi, "/qn /norestart ALLUSERS=1 REBOOT=ReallySuppress", null,
            "Zoom's signer organization has not been confirmed from a known-good signed Zoom binary yet, so no Zoom installer is trusted."),
        new(DepotApplications.SevenZip, "https://www.7-zip.org/a/7z2407-x64.exe", DepotInstallerKind.Exe, "/S", null,
            "7-Zip installers are not Authenticode signed and the vendor publishes no hashes, so no 7-Zip download can be verified.")
    ];

    public static DepotInstallerSource? Find(DepotApplications application) => Sources.SingleOrDefault(source => source.Application == application);

    /// <summary>Decides trust from exact signature evidence. Only a valid Authenticode signature whose subject organization equals the confirmed vendor organization is trusted.</summary>
    public static DepotInstallerTrustResult Evaluate(DepotInstallerSource source, long length, string? status, string? signer, string? thumbprint)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (length <= 0 || length > MaximumInstallerBytes)
            return new(source.Application, DepotInstallerTrustOutcome.Untrusted, "The installer file is empty or larger than any supported vendor installer.", status, signer, thumbprint);
        if (status is null)
            return new(source.Application, DepotInstallerTrustOutcome.Unknown, "Signature evidence was unavailable, so the installer is not trusted.", status, signer, thumbprint);
        if (source.SignerOrganization is null)
            return new(source.Application, DepotInstallerTrustOutcome.Untrusted, source.TrustNote, status, signer, thumbprint);
        if (status != "Valid")
            return new(source.Application, DepotInstallerTrustOutcome.Untrusted, "Windows reports the signature as " + status + ". The installer is not trusted.", status, signer, thumbprint);
        string? organization = Organization(signer);
        if (!string.Equals(organization, source.SignerOrganization, StringComparison.Ordinal))
            return new(source.Application, DepotInstallerTrustOutcome.Untrusted,
                "The installer is signed by " + (organization ?? "an unidentified organization") + ", not " + source.SignerOrganization + ". It is not trusted.", status, signer, thumbprint);
        return new(source.Application, DepotInstallerTrustOutcome.Trusted,
            "Windows validates the signature and the signer is " + source.SignerOrganization + ". This establishes the publisher, not that the version is current or compatible.", status, signer, thumbprint);
    }

    /// <summary>Returns the O= value from a certificate subject, honoring quoted values. Returns null when O is missing or repeated.</summary>
    public static string? Organization(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 2048) return null;
        var parts = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        foreach (char c in subject)
        {
            if (c == '"') { quoted = !quoted; current.Append(c); }
            else if (c == ',' && !quoted) { parts.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(c);
        }
        if (quoted) return null;
        parts.Add(current.ToString().Trim());
        var organizations = parts.Where(p => p.StartsWith("O=", StringComparison.Ordinal)).Select(p => p[2..].Trim()).ToList();
        if (organizations.Count != 1) return null;
        string value = organizations[0];
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') value = value[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
        return value.Length == 0 || value.Any(char.IsControl) ? null : value;
    }

    public static RepairStep SignatureStep(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) throw new ArgumentException("Choose an absolute installer file path.");
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(Path.GetFullPath(path)));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop; " +
            "$path=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encoded + "')); " +
            "$item=Get-Item -LiteralPath $path -ErrorAction Stop; $s=Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop; " +
            "[pscustomobject]@{length=[long]$item.Length;status=$s.Status.ToString();signer=$s.SignerCertificate.Subject;thumbprint=$s.SignerCertificate.Thumbprint}|ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<DepotInstallerTrustResult> VerifyFileAsync(DepotApplications application, string path, CancellationToken token = default)
    {
        var source = Find(application) ?? throw new ArgumentException("DEPOT has no installer source for that application.");
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Authenticode inspection requires Windows.");
        return Interpret(source, await BoundedTool.RunAsync(SignatureStep(path), TimeSpan.FromSeconds(60), token: token).ConfigureAwait(false));
    }

    public static DepotInstallerTrustResult Interpret(DepotInstallerSource source, ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(evidence);
        DepotInstallerTrustResult Unknown() => new(source.Application, DepotInstallerTrustOutcome.Unknown, "Signature evidence was unavailable or malformed, so the installer is not trusted.");
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            string[] names = ["length", "status", "signer", "thumbprint"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != names.Length || !names.All(n => root.TryGetProperty(n, out _)) ||
                root.GetProperty("length").ValueKind != JsonValueKind.Number) return Unknown();
            string? Text(string name) => root.GetProperty(name).ValueKind == JsonValueKind.Null ? null : root.GetProperty(name).GetString();
            string? status = Text("status"), signer = Text("signer"), thumbprint = Text("thumbprint");
            if (status is not ("Valid" or "UnknownError" or "NotSigned" or "HashMismatch" or "NotTrusted" or "NotSupportedFileFormat" or "Incompatible")) return Unknown();
            if (thumbprint is { Length: > 0 } && (thumbprint.Length != 40 || !thumbprint.All(char.IsAsciiHexDigit))) return Unknown();
            return Evaluate(source, root.GetProperty("length").GetInt64(), status, signer, thumbprint);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return Unknown(); }
    }
}
