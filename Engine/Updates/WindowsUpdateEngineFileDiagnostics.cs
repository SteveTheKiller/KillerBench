using System.Text.Json;

namespace KillerBench.Engine;

public sealed record WindowsUpdateEngineFileEntry(string File, bool Exists, string Signature, string Version);
public sealed record WindowsUpdateEngineFileSnapshot(bool Complete, IReadOnlyList<WindowsUpdateEngineFileEntry> Files);

public static class WindowsUpdateEngineFileDiagnostics
{
    private static readonly string[] RequiredFiles = ["wuapi.dll", "wuaueng.dll", "wups.dll", "wups2.dll"];
    private static readonly HashSet<string> Signatures = new([
        "Valid", "UnknownError", "NotSigned", "HashMismatch", "NotTrusted", "NotSupportedFileFormat", "Incompatible", "Missing", "Unavailable"
    ], StringComparer.Ordinal);
    private const string Scope = " Paths and signer identities are excluded. This point-in-time check does not establish component registration, update applicability, source reachability or future file identity. No file, package, service, policy or update setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
        "$names=@('wuapi.dll','wuaueng.dll','wups.dll','wups2.dll');$root=[Environment]::SystemDirectory;$complete=$true;" +
        "$rows=@(foreach($name in $names){$path=Join-Path $root $name;try{$exists=Test-Path -LiteralPath $path -PathType Leaf;" +
        "if($exists){$signature=[string](Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop).Status;$version=[string](Get-Item -LiteralPath $path -ErrorAction Stop).VersionInfo.FileVersion}else{$signature='Missing';$version='Missing'}}" +
        "catch{$exists=$false;$signature='Unavailable';$version='Unavailable';$complete=$false};[pscustomobject]@{file=$name;exists=[bool]$exists;signature=$signature;version=$version}});" +
        "[pscustomobject]@{complete=$complete;files=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows Update engine file inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Windows Update engine file provider returned no task."))
                .WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Windows Update engine file provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Windows Update engine file inspection exceeded its overall deadline.",
                new(-1, "", "Windows Update engine file inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Windows Update engine file inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Windows Update engine file collection failed or was incomplete.", evidence);
        try
        {
            var snapshot = Parse(evidence.Output);
            if (!snapshot.Complete || snapshot.Files.Any(file => file.Signature == "Unavailable"))
                return Unknown("One or more Windows Update engine files could not be inspected.", evidence);
            var review = snapshot.Files.Where(file => !file.Exists || file.Signature != "Valid").ToArray();
            var versions = snapshot.Files.Where(file => file.Exists).Select(file => file.Version).Distinct(StringComparer.Ordinal).ToArray();
            bool versionMismatch = versions.Length > 1;
            string detail = "Reviewed " + snapshot.Files.Count + " required Windows Update engine files in the native Windows system directory.";
            detail += review.Length == 0 && !versionMismatch
                ? " Every inspected file is present, reported a Valid Authenticode status and has version " + versions.Single() + "."
                : " Review: " + string.Join("; ", review.Select(file => file.File + ": " + (file.Exists ? file.Signature : "Missing"))) +
                  (versionMismatch ? (review.Length == 0 ? " Version mismatch: " : "; version mismatch: ") +
                    string.Join("; ", snapshot.Files.Where(file => file.Exists).Select(file => file.File + ": " + file.Version)) : "") +
                  ". Use the System files workflow to evaluate protected Windows files before considering recovery.";
            return new("Windows Update engine files", review.Length == 0 && !versionMismatch ? CheckState.Information : CheckState.Attention,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Windows Update engine file evidence was malformed or exceeded its bounds.", evidence);
        }
    }

    public static WindowsUpdateEngineFileSnapshot Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 5 });
        var root = document.RootElement;
        if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
            !root.TryGetProperty("files", out var values) || values.ValueKind != JsonValueKind.Array || values.GetArrayLength() != RequiredFiles.Length)
            throw new InvalidDataException("Windows Update engine file root is invalid.");
        var files = new List<WindowsUpdateEngineFileEntry>(RequiredFiles.Length);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in values.EnumerateArray())
        {
            if (!Unique(item, 4) || !Text(item, "file", 32, out string file) || !RequiredFiles.Contains(file, StringComparer.OrdinalIgnoreCase) ||
                !Boolean(item, "exists", out bool exists) || !Text(item, "signature", 32, out string signature) || !Signatures.Contains(signature) ||
                !Text(item, "version", 64, out string version) || !ValidVersion(version, exists, signature) ||
                !names.Add(file) || exists == (signature == "Missing"))
                throw new InvalidDataException("Windows Update engine file entry is invalid.");
            files.Add(new(file, exists, signature, version));
        }
        if (!RequiredFiles.All(names.Contains)) throw new InvalidDataException("Windows Update engine file set is incomplete.");
        return new(complete, files.AsReadOnly());
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Windows Update engine files", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
    private static bool ValidVersion(string version, bool exists, string signature) => exists
        ? version.Any(char.IsDigit) && version.All(character => char.IsDigit(character) || character == '.') && !version.StartsWith('.') && !version.EndsWith('.')
        : version == (signature == "Unavailable" ? "Unavailable" : "Missing");
}
