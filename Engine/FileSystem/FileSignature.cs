using System.Text;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record FileSignatureResult(string Path, string? Status, string? Signer, string? Thumbprint, string Detail, ToolResult Evidence)
{
    public string Limitation => "Signature observation only. File identity can change after inspection. This does not establish executable safety, permissions or registration eligibility.";
}

public static class FileSignature
{
    public static RepairStep CreateReadStep(ScheduledTaskAction action)
    {
        action.Validate();
        string path = Convert.ToBase64String(Encoding.UTF8.GetBytes(action.Executable));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop; " +
            "$path=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + path + "')); " +
            "$signature=Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop; " +
            "[pscustomobject]@{path=$signature.Path;status=$signature.Status.ToString();" +
            "signer=$signature.SignerCertificate.Subject;thumbprint=$signature.SignerCertificate.Thumbprint;" +
            "message=$signature.StatusMessage}|ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<FileSignatureResult> InspectAsync(ScheduledTaskAction action, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Authenticode inspection requires Windows.");
        return Interpret(action, await BoundedTool.RunAsync(CreateReadStep(action), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false));
    }

    public static FileSignatureResult Interpret(ScheduledTaskAction action, ToolResult evidence)
    {
        action.Validate();
        ArgumentNullException.ThrowIfNull(evidence);
        FileSignatureResult Unknown() => new(action.Executable, null, null, null, "Signature observation unavailable or incomplete.", evidence);
        if (!TaskRegistration.Complete(evidence)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            string[] names = ["path", "status", "signer", "thumbprint", "message"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != names.Length ||
                root.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != names.Length ||
                root.EnumerateObject().Any(p => !names.Contains(p.Name))) return Unknown();
            string? Read(string name, int max)
            {
                var value = root.GetProperty(name);
                if (value.ValueKind == JsonValueKind.Null) return null;
                string text = value.GetString()!;
                if (text.Length > max || text.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
                    throw new InvalidDataException("Signature field exceeds its display bounds.");
                return text;
            }
            string? path = Read("path", 32768), status = Read("status", 32), signer = Read("signer", 2048),
                thumbprint = Read("thumbprint", 128), message = Read("message", 4096);
            if (!string.Equals(path, action.Executable, StringComparison.OrdinalIgnoreCase) ||
                status is not ("Valid" or "UnknownError" or "NotSigned" or "HashMismatch" or "NotTrusted" or "NotSupportedFileFormat" or "Incompatible")) return Unknown();
            if (status == "Valid" && (string.IsNullOrWhiteSpace(signer) || string.IsNullOrWhiteSpace(thumbprint))) return Unknown();
            if (thumbprint is { Length: > 0 } && (thumbprint.Length != 40 || !thumbprint.All(char.IsAsciiHexDigit))) return Unknown();
            return new(action.Executable, status, signer, thumbprint, message ?? "No provider description.", evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or InvalidDataException or KeyNotFoundException)
        { return Unknown(); }
    }
}
