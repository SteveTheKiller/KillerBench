using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record InstallerEngineFileEntry(string Architecture, string File, bool Exists, string Signature);
public sealed record InstallerEngineFileSnapshot(bool Complete, IReadOnlyList<InstallerEngineFileEntry> Files);

public static class InstallerEngineFileDiagnostics
{
    private static readonly string[] RequiredFiles = ["msiexec.exe", "msi.dll", "msihnd.dll"];
    private static readonly HashSet<string> Architectures = new(["Native", "WOW64"], StringComparer.Ordinal);
    private static readonly HashSet<string> Signatures = new([
        "Valid", "UnknownError", "NotSigned", "HashMismatch", "NotTrusted", "NotSupportedFileFormat", "Incompatible", "Missing", "Unavailable"
    ], StringComparer.Ordinal);
    private const string Scope = " Paths and signer identities are excluded. This point-in-time check does not establish Windows Installer registration, package integrity, installation success or future file identity. No file, package, registration, service, policy or Installer setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
        "$names=@('msiexec.exe','msi.dll','msihnd.dll');$roots=@([pscustomobject]@{architecture='Native';path=[Environment]::SystemDirectory});" +
        "$wow=Join-Path $env:windir 'SysWOW64';if(Test-Path -LiteralPath $wow -PathType Container){$roots+=@([pscustomobject]@{architecture='WOW64';path=$wow})};" +
        "$complete=$true;$rows=@(foreach($root in $roots){foreach($name in $names){$path=Join-Path $root.path $name;try{" +
        "$exists=Test-Path -LiteralPath $path -PathType Leaf;if($exists){$signature=[string](Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop).Status}else{$signature='Missing'}" +
        "}catch{$exists=$false;$signature='Unavailable';$complete=$false};[pscustomobject]@{architecture=$root.architecture;file=$name;exists=[bool]$exists;signature=$signature}}});" +
        "[pscustomobject]@{complete=$complete;files=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows Installer engine file inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Installer engine file provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Installer engine file provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Installer engine file inspection exceeded its overall deadline.",
                new(-1, "", "Installer engine file inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Installer engine file inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Installer engine file collection failed or was incomplete.", evidence);
        try
        {
            var snapshot = Parse(evidence.Output);
            if (!snapshot.Complete || snapshot.Files.Any(file => file.Signature == "Unavailable"))
                return Unknown("One or more Installer engine files could not be inspected.", evidence);
            var review = snapshot.Files.Where(file => !file.Exists || file.Signature != "Valid").ToArray();
            string detail = "Reviewed " + snapshot.Files.Count + " required Windows Installer engine files across " +
                snapshot.Files.Select(file => file.Architecture).Distinct(StringComparer.Ordinal).Count() + " available architecture set(s).";
            if (review.Length == 0)
                detail += " Every inspected file is present and reported a Valid Authenticode status.";
            else
                detail += " Review: " + string.Join("; ", review.Select(file => file.Architecture + " " + file.File + ": " +
                    (file.Exists ? file.Signature : "Missing"))) + ". Use the System files workflow to evaluate protected Windows files before considering recovery.";
            return new("Windows Installer engine files", review.Length == 0 ? CheckState.Information : CheckState.Attention,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Installer engine file evidence was malformed or exceeded its bounds.", evidence);
        }
    }

    public static InstallerEngineFileSnapshot Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 6 });
        var root = document.RootElement;
        if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
            !root.TryGetProperty("files", out var filesValue) || filesValue.ValueKind != JsonValueKind.Array ||
            filesValue.GetArrayLength() is < 3 or > 6)
            throw new InvalidDataException("Installer engine file root is invalid.");
        var files = new List<InstallerEngineFileEntry>(filesValue.GetArrayLength());
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in filesValue.EnumerateArray())
        {
            if (!Unique(item, 4) || !Text(item, "architecture", 16, out string architecture) || !Architectures.Contains(architecture) ||
                !Text(item, "file", 32, out string file) || !RequiredFiles.Contains(file, StringComparer.OrdinalIgnoreCase) ||
                !Boolean(item, "exists", out bool exists) || !Text(item, "signature", 32, out string signature) || !Signatures.Contains(signature) ||
                !keys.Add(architecture + "|" + file.ToLowerInvariant()) || exists == (signature == "Missing"))
                throw new InvalidDataException("Installer engine file entry is invalid.");
            files.Add(new(architecture, file, exists, signature));
        }
        foreach (string architecture in files.Select(file => file.Architecture).Distinct(StringComparer.Ordinal))
            if (!RequiredFiles.All(required => files.Any(file => file.Architecture == architecture && file.File.Equals(required, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidDataException("Installer engine file set is incomplete.");
        if (!files.Any(file => file.Architecture == "Native")) throw new InvalidDataException("Native Installer engine files are missing.");
        return new(complete, files.AsReadOnly());
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Windows Installer engine files", CheckState.Unknown, detail + Scope, Evidence: evidence);
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
}
