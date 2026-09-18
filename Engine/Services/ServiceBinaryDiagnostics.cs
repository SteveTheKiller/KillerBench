using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ServiceBinaryObservation(string Name, string DisplayName, string ServiceType,
    string? BinaryFile, string Status, string? Signature, string? SignatureIssue, bool Complete, string? Issue);

public static class ServiceBinaryDiagnostics
{
    private const int MaximumServices = 1024;
    private static readonly HashSet<string> Statuses = new(["Present", "Missing", "Unsupported", "Unavailable"], StringComparer.Ordinal);
    private static readonly HashSet<string> SignatureStatuses = new(["Valid", "UnknownError", "NotSigned", "HashMismatch", "NotTrusted", "NotSupportedFileFormat", "Incompatible", "Unavailable"], StringComparer.Ordinal);
    private const string Scope = " Full executable paths, command arguments, service accounts, process IDs and signer identities are excluded. Authenticode status is point-in-time evidence and does not establish publisher approval or safe behavior. A present shared-process host does not verify its registered service DLL or service behavior. No service, executable, driver, startup setting, account, permission or recovery configuration changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
            "function Resolve-ServiceBinary([string]$raw){if([string]::IsNullOrWhiteSpace($raw)){return $null};$text=[Environment]::ExpandEnvironmentVariables($raw.Trim());" +
            "if($text.StartsWith('\\??\\')){$text=$text.Substring(4)};if($text.StartsWith('\\SystemRoot\\',[StringComparison]::OrdinalIgnoreCase)){$suffix=$text.Substring(12);$text=Join-Path $env:SystemRoot $suffix}" +
            "elseif($text.StartsWith('System32\\',[StringComparison]::OrdinalIgnoreCase)){$text=Join-Path $env:SystemRoot $text};" +
            "$match=if($text.StartsWith([string][char]34)){[regex]::Match($text,'^\"([^\"]+)\"')}else{[regex]::Match($text,'^(.+?\\.(?:exe|sys))(?=\\s|$)',[Text.RegularExpressions.RegexOptions]::IgnoreCase)};" +
            "if(-not $match.Success){return $null};return $match.Groups[1].Value};" +
            "$source=@(Get-CimInstance Win32_Service -ErrorAction Stop|Sort-Object Name|Select-Object -First 1025);$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($service in $source){try{$path=Resolve-ServiceBinary ([string]$service.PathName);if([string]::IsNullOrWhiteSpace($path)){$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=[string]$service.ServiceType;binaryFile=$null;status='Unsupported';signature=$null;signatureIssue=$null;complete=$true;issue=$null});continue};" +
            "$file=[IO.Path]::GetFileName($path);if($path.StartsWith('\\\\') -or -not [IO.Path]::IsPathRooted($path)){$status='Unsupported'}else{$drive=$null;try{$drive=[IO.DriveInfo]::new([IO.Path]::GetPathRoot($path));$status=if($drive.DriveType -ne [IO.DriveType]::Fixed){'Unsupported'}elseif([IO.File]::Exists($path)){'Present'}else{'Missing'}}catch{$status='Unavailable'}};" +
            "$signatureIssue=$null;$signature=if($status -eq 'Present'){try{[string](Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop).Status}catch{$signatureIssue=$_.Exception.GetType().Name;'Unavailable'}}else{$null};" +
            "$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=[string]$service.ServiceType;binaryFile=$file;status=$status;signature=$signature;signatureIssue=$signatureIssue;complete=$true;issue=$null})}" +
            "catch{$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName='';serviceType='';binaryFile=$null;status='Unavailable';signature=$null;signatureIssue=$null;complete=$false;issue=$_.Exception.GetType().Name})}};" +
            "[pscustomobject]@{complete=($source.Count-le 1024);services=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        WorkflowCheck Unknown(string detail) => new("Service binary registrations", CheckState.Unknown, detail + Scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Service binary registration evidence was unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("services", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumServices)
                return Unknown("Service binary registration evidence exceeded its bound or was malformed.");
            var parsed = new List<ServiceBinaryObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 9 ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("displayName", out var displayValue) || displayValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("serviceType", out var typeValue) || typeValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("binaryFile", out var fileValue) || fileValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("status", out var statusValue) || statusValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("signature", out var signatureValue) || signatureValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("signatureIssue", out var signatureIssueValue) || signatureIssueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("complete", out var rowComplete) || rowComplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("issue", out var issueValue) || issueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return Unknown("One or more service binary records were invalid.");
                string name = nameValue.GetString()!;
                string displayName = displayValue.GetString()!;
                string serviceType = typeValue.GetString()!;
                string status = statusValue.GetString()!;
                string? file = fileValue.GetString();
                string? signature = signatureValue.GetString();
                string? signatureIssue = signatureIssueValue.GetString();
                string? issue = issueValue.GetString();
                bool completeRow = rowComplete.GetBoolean();
                if (!ValidServiceName(name) || !identities.Add(name) || !Statuses.Contains(status) ||
                    completeRow && (!DriverReference.ValidText(displayName, 512) || !DriverReference.ValidText(serviceType, 64) || issue is not null ||
                        file is not null && (!DriverReference.ValidText(file, 260) || Path.GetFileName(file) != file || !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".sys", StringComparison.OrdinalIgnoreCase)) ||
                        status is "Present" or "Missing" && file is null ||
                        status == "Present" && (signature is null || !SignatureStatuses.Contains(signature)) || status != "Present" && signature is not null ||
                        signature is "Unavailable" or "UnknownError" && (signatureIssue is null || !DriverReference.ValidText(signatureIssue, 128)) ||
                        signature is not ("Unavailable" or "UnknownError") && signatureIssue is not null) ||
                    !completeRow && (displayName.Length != 0 || serviceType.Length != 0 || file is not null || signature is not null || signatureIssue is not null || status != "Unavailable" || issue is null || !DriverReference.ValidText(issue, 128)))
                    return Unknown("One or more service binary records were internally inconsistent.");
                parsed.Add(new(name, displayName, serviceType, file, status, signature, signatureIssue, completeRow, issue));
            }
            if (parsed.Count == 0) return new("Service binary registrations", CheckState.Information,
                "No installed Win32 service records were returned." + Scope, Evidence: evidence);
            int missing = parsed.Count(value => value.Complete && value.Status == "Missing");
            int unsupported = parsed.Count(value => value.Complete && value.Status == "Unsupported");
            int unavailable = parsed.Count(value => !value.Complete || value.Status == "Unavailable");
            int present = parsed.Count(value => value.Complete && value.Status == "Present");
            int signatureValid = parsed.Count(value => value.Signature == "Valid");
            int signatureUnavailable = parsed.Count(value => value.Status == "Present" && value.Signature is "Unavailable" or "UnknownError");
            int signatureReview = parsed.Count(value => value.Status == "Present" && value.Signature is not ("Valid" or "Unavailable" or "UnknownError"));
            string summary = $"Reviewed {parsed.Count} service binary registrations: {present} local binaries are present, {missing} are missing, {unsupported} paths were intentionally not accessed, and {unavailable} records are unavailable. Authenticode reported {signatureValid} Valid, {signatureReview} requiring review and {signatureUnavailable} unavailable.";
            if (unavailable > 0 || signatureUnavailable > 0) return Unknown(summary + " Complete binary coverage could not be established.");
            if (missing > 0 || signatureReview > 0) return new("Service binary registrations", CheckState.Attention,
                summary + " Review missing or non-Valid registrations against the owning Windows component, driver package or approved application package before changing a service or replacing a file." + Scope, Evidence: evidence);
            if (unsupported > 0) return Unknown(summary + " Nonlocal, unparseable or non-fixed paths were not contacted, so complete binary coverage could not be established.");
            return new("Service binary registrations", CheckState.Information,
                summary + " Valid Authenticode status and file presence do not verify compatibility, loadability or service function." + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return Unknown("Service binary registration evidence was malformed (" + error.GetType().Name + ").");
        }
    }

    private static bool ValidServiceName(string value) => DriverReference.ValidText(value, 256) &&
        !value.Any(character => char.IsControl(character) || character is '\\' or '/');
}
