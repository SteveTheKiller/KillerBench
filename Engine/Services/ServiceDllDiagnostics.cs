using System.Text.Json;

namespace KillerBench.Engine.Services;

public sealed record ServiceDllObservation(string Name, string? DllFile, string Status, string? Signature, string? SignatureIssue, bool Complete, string? Issue);

public static class ServiceDllDiagnostics
{
    private const int MaximumServices = 1024;
    private static readonly HashSet<string> Statuses = new(["Present", "Missing", "Unsupported", "Invalid", "Unavailable"], StringComparer.Ordinal);
    private static readonly HashSet<string> SignatureStatuses = new(["Valid", "UnknownError", "NotSigned", "HashMismatch", "NotTrusted", "NotSupportedFileFormat", "Incompatible", "Unavailable"], StringComparer.Ordinal);
    private const string Scope = " Full DLL paths, service arguments, accounts, process IDs and signer identities are excluded. Only registered ServiceDll values are inspected. Authenticode status is point-in-time evidence and does not establish publisher approval or safe behavior. No DLL is loaded and no service, executable, DLL, startup setting, account, permission or registry value changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
        => await CheckCoreAsync(null, "Registered service DLLs", token).ConfigureAwait(false);

    public static async Task<WorkflowCheck> CheckSelectedAsync(string title, IReadOnlyCollection<string> serviceNames,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(serviceNames);
        if (!DriverReference.ValidText(title, 128) || serviceNames.Count is < 1 or > 32 ||
            serviceNames.Any(name => !ValidServiceName(name)) ||
            serviceNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != serviceNames.Count)
            throw new ArgumentException("Selected service DLL scope is invalid.", nameof(serviceNames));
        return await CheckCoreAsync(serviceNames, title, token).ConfigureAwait(false);
    }

    public static RepairStep CreateReadStep(IReadOnlyCollection<string>? serviceNames = null)
    {
        if (serviceNames is not null && (serviceNames.Count is < 1 or > 32 ||
            serviceNames.Any(name => !ValidServiceName(name)) ||
            serviceNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != serviceNames.Count))
            throw new ArgumentException("Selected service DLL scope is invalid.", nameof(serviceNames));
        string names = serviceNames is null
            ? "@($root.GetSubKeyNames()|Sort-Object|Select-Object -First 1025)"
            : "@(" + string.Join(",", serviceNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => "'" + name.Replace("'", "''", StringComparison.Ordinal) + "'")) + ")";
        string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
            "$root=[Microsoft.Win32.Registry]::LocalMachine.OpenSubKey('SYSTEM\\CurrentControlSet\\Services');if($null -eq $root){throw 'Services registry unavailable'};" +
            "$names=" + names + ";$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($name in $names){try{$service=$root.OpenSubKey($name);if($null -eq $service){throw 'Service key unavailable'};$parameters=$service.OpenSubKey('Parameters');if($null -eq $parameters){continue};" +
            "$valueNames=@($parameters.GetValueNames());if(-not ($valueNames -contains 'ServiceDll')){continue};$kind=[string]$parameters.GetValueKind('ServiceDll');" +
            "$raw=[string]$parameters.GetValue('ServiceDll',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames);if($kind -notin @('String','ExpandString') -or [string]::IsNullOrWhiteSpace($raw)){$rows.Add([pscustomobject]@{name=[string]$name;dllFile=$null;status='Invalid';signature=$null;signatureIssue=$null;complete=$true;issue=$null});continue};" +
            "$path=[Environment]::ExpandEnvironmentVariables($raw.Trim());if($path.Length -ge 2 -and $path[0] -eq [char]34 -and $path[$path.Length-1] -eq [char]34){$path=$path.Substring(1,$path.Length-2)};" +
            "if($path.StartsWith('\\SystemRoot\\',[StringComparison]::OrdinalIgnoreCase)){$suffix=$path.Substring(12);$path=Join-Path $env:SystemRoot $suffix}" +
            "elseif($path.StartsWith('System32\\',[StringComparison]::OrdinalIgnoreCase)){$path=Join-Path $env:SystemRoot $path};" +
            "$file=[IO.Path]::GetFileName($path);if([string]::IsNullOrWhiteSpace($file) -or -not $file.EndsWith('.dll',[StringComparison]::OrdinalIgnoreCase)){$status='Invalid';$file=$null}" +
            "elseif($path.StartsWith('\\\\') -or -not [IO.Path]::IsPathRooted($path)){$status='Unsupported'}else{try{$drive=[IO.DriveInfo]::new([IO.Path]::GetPathRoot($path));$status=if($drive.DriveType -ne [IO.DriveType]::Fixed){'Unsupported'}elseif([IO.File]::Exists($path)){'Present'}else{'Missing'}}catch{$status='Unavailable'}};" +
            "$signatureIssue=$null;$signature=if($status -eq 'Present'){try{[string](Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop).Status}catch{$signatureIssue=$_.Exception.GetType().Name;'Unavailable'}}else{$null};" +
            "$rows.Add([pscustomobject]@{name=[string]$name;dllFile=$file;status=$status;signature=$signature;signatureIssue=$signatureIssue;complete=$true;issue=$null})}" +
            "catch{$rows.Add([pscustomobject]@{name=[string]$name;dllFile=$null;status='Unavailable';signature=$null;signatureIssue=$null;complete=$false;issue=$_.Exception.GetType().Name})}};" +
            "[pscustomobject]@{complete=($names.Count -le 1024);services=$rows}|ConvertTo-Json -Depth 4 -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    private static async Task<WorkflowCheck> CheckCoreAsync(IReadOnlyCollection<string>? serviceNames, string title,
        CancellationToken token)
    {
        var evidence = await BoundedTool.RunAsync(CreateReadStep(serviceNames),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(evidence) with { Name = title };
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        WorkflowCheck Unknown(string detail) => new("Registered service DLLs", CheckState.Unknown, detail + Scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Registered service DLL evidence was unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("services", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumServices)
                return Unknown("Registered service DLL evidence exceeded its bound or was malformed.");
            var parsed = new List<ServiceDllObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 7 ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("dllFile", out var fileValue) || fileValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("status", out var statusValue) || statusValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("signature", out var signatureValue) || signatureValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("signatureIssue", out var signatureIssueValue) || signatureIssueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("complete", out var rowComplete) || rowComplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("issue", out var issueValue) || issueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return Unknown("One or more registered service DLL records were invalid.");
                string name = nameValue.GetString()!;
                string status = statusValue.GetString()!;
                string? file = fileValue.GetString();
                string? signature = signatureValue.GetString();
                string? signatureIssue = signatureIssueValue.GetString();
                string? issue = issueValue.GetString();
                bool completeRow = rowComplete.GetBoolean();
                if (!ValidServiceName(name) || !identities.Add(name) || !Statuses.Contains(status) ||
                    completeRow && (issue is not null ||
                        file is not null && (!DriverReference.ValidText(file, 260) || Path.GetFileName(file) != file || !file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) ||
                        status is "Present" or "Missing" or "Unsupported" && file is null || status == "Invalid" && file is not null ||
                        status == "Present" && (signature is null || !SignatureStatuses.Contains(signature)) || status != "Present" && signature is not null ||
                        signature is "Unavailable" or "UnknownError" && (signatureIssue is null || !DriverReference.ValidText(signatureIssue, 128)) ||
                        signature is not ("Unavailable" or "UnknownError") && signatureIssue is not null) ||
                    !completeRow && (file is not null || signature is not null || signatureIssue is not null || status != "Unavailable" || issue is null || !DriverReference.ValidText(issue, 128)))
                    return Unknown("One or more registered service DLL records were internally inconsistent.");
                parsed.Add(new(name, file, status, signature, signatureIssue, completeRow, issue));
            }
            if (parsed.Count == 0) return new("Registered service DLLs", CheckState.Information,
                "No ServiceDll registrations were found in the inspected service registry keys." + Scope, Evidence: evidence);
            int missing = parsed.Count(value => value.Complete && value.Status == "Missing");
            int unsupported = parsed.Count(value => value.Complete && value.Status == "Unsupported");
            int invalid = parsed.Count(value => value.Complete && value.Status == "Invalid");
            int unavailable = parsed.Count(value => !value.Complete || value.Status == "Unavailable");
            int present = parsed.Count(value => value.Complete && value.Status == "Present");
            int signatureValid = parsed.Count(value => value.Signature == "Valid");
            int signatureUnavailable = parsed.Count(value => value.Status == "Present" && value.Signature is "Unavailable" or "UnknownError");
            int signatureReview = parsed.Count(value => value.Status == "Present" && value.Signature is not ("Valid" or "Unavailable" or "UnknownError"));
            string summary = $"Reviewed {parsed.Count} ServiceDll registrations: {present} local DLLs are present, {missing} are missing, {unsupported} locations were intentionally not accessed, {invalid} registrations are invalid, and {unavailable} records are unavailable. Authenticode reported {signatureValid} Valid, {signatureReview} requiring review and {signatureUnavailable} unavailable.";
            if (unavailable > 0 || invalid > 0 || signatureUnavailable > 0) return Unknown(summary + " Complete registered-DLL coverage could not be established.");
            if (missing > 0 || signatureReview > 0) return new("Registered service DLLs", CheckState.Attention,
                summary + " Review missing or non-Valid DLLs against the owning Windows component or approved application package before changing the service or replacing a file." + Scope, Evidence: evidence);
            if (unsupported > 0) return Unknown(summary + " Nonlocal and non-fixed locations were not contacted, so complete registered-DLL coverage could not be established.");
            return new("Registered service DLLs", CheckState.Information,
                summary + " Valid Authenticode status and file presence do not verify compatibility, loadability or service function." + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return Unknown("Registered service DLL evidence was malformed (" + error.GetType().Name + ").");
        }
    }

    private static bool ValidServiceName(string value) => DriverReference.ValidText(value, 256) &&
        !value.Any(character => char.IsControl(character) || character is '\\' or '/');
}
