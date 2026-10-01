using System.Text.Json;

namespace KillerBench.Engine.Services;

public sealed record ServicePathQuotingObservation(string Name, string DisplayName, string ServiceType,
    string? BinaryFile, string Status, bool Complete, string? Issue);

public static class ServicePathQuotingDiagnostics
{
    private const int MaximumServices = 1024;
    private static readonly HashSet<string> Statuses = new([
        "Quoted", "NoSpaces", "UnquotedWithSpaces", "NotApplicable", "Unsupported", "Unavailable"
    ], StringComparer.Ordinal);
    private const string Scope = " Full executable paths, command arguments, service accounts and process IDs are excluded. Driver registrations are outside this check. Quoting evidence does not establish binary trust, compatibility or service function. No service, executable, driver, startup setting, account, permission or registry value changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$source=@(Get-CimInstance Win32_Service -ErrorAction Stop|Sort-Object Name|Select-Object -First 1025);$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($service in $source){try{$type=[string]$service.ServiceType;if($type -match 'Driver'){$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=$type;binaryFile=$null;status='NotApplicable';complete=$true;issue=$null});continue};" +
            "$raw=[string]$service.PathName;if([string]::IsNullOrWhiteSpace($raw)){$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=$type;binaryFile=$null;status='Unsupported';complete=$true;issue=$null});continue};" +
            "$text=[Environment]::ExpandEnvironmentVariables($raw.Trim());if($text.StartsWith('\\??\\')){$text=$text.Substring(4)};if($text.StartsWith('\\SystemRoot\\',[StringComparison]::OrdinalIgnoreCase)){$text=Join-Path $env:SystemRoot $text.Substring(12)}elseif($text.StartsWith('System32\\',[StringComparison]::OrdinalIgnoreCase)){$text=Join-Path $env:SystemRoot $text};" +
            "$quoted=$text.StartsWith([string][char]34);$match=if($quoted){[regex]::Match($text,'^\"([^\"]+)\"')}else{[regex]::Match($text,'^(.+?\\.exe)(?=\\s|$)',[Text.RegularExpressions.RegexOptions]::IgnoreCase)};" +
            "if(-not $match.Success){$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=$type;binaryFile=$null;status='Unsupported';complete=$true;issue=$null});continue};" +
            "$path=$match.Groups[1].Value;$file=[IO.Path]::GetFileName($path);$status=if($quoted){'Quoted'}elseif($path.Contains(' ')){'UnquotedWithSpaces'}else{'NoSpaces'};" +
            "$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=$type;binaryFile=$file;status=$status;complete=$true;issue=$null})}" +
            "catch{$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName='';serviceType='';binaryFile=$null;status='Unavailable';complete=$false;issue=$_.Exception.GetType().Name})}};" +
            "[pscustomobject]@{complete=($source.Count-le 1024);services=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        WorkflowCheck Unknown(string detail) => new("Service executable path quoting", CheckState.Unknown, detail + Scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Service executable path quoting evidence was unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("services", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumServices)
                return Unknown("Service executable path quoting evidence exceeded its bound or was malformed.");
            var parsed = new List<ServicePathQuotingObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 7 ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("displayName", out var displayValue) || displayValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("serviceType", out var typeValue) || typeValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("binaryFile", out var fileValue) || fileValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("status", out var statusValue) || statusValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("complete", out var rowComplete) || rowComplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("issue", out var issueValue) || issueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return Unknown("One or more service executable path quoting records were invalid.");
                string name = nameValue.GetString()!;
                string displayName = displayValue.GetString()!;
                string serviceType = typeValue.GetString()!;
                string status = statusValue.GetString()!;
                string? file = fileValue.GetString();
                string? issue = issueValue.GetString();
                bool completeRow = rowComplete.GetBoolean();
                if (!ValidServiceName(name) || !identities.Add(name) || !Statuses.Contains(status) ||
                    completeRow && (!DriverReference.ValidText(displayName, 512) || !DriverReference.ValidText(serviceType, 64) || issue is not null ||
                        file is not null && (!DriverReference.ValidText(file, 260) || Path.GetFileName(file) != file || !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) ||
                        status is "Quoted" or "NoSpaces" or "UnquotedWithSpaces" && file is null ||
                        status is "NotApplicable" or "Unsupported" && file is not null) ||
                    !completeRow && (displayName.Length != 0 || serviceType.Length != 0 || file is not null || status != "Unavailable" || issue is null || !DriverReference.ValidText(issue, 128)))
                    return Unknown("One or more service executable path quoting records were internally inconsistent.");
                parsed.Add(new(name, displayName, serviceType, file, status, completeRow, issue));
            }
            if (parsed.Count == 0) return new("Service executable path quoting", CheckState.Information,
                "No installed Win32 service records were returned." + Scope, Evidence: evidence);
            int quoted = parsed.Count(value => value.Status == "Quoted");
            int noSpaces = parsed.Count(value => value.Status == "NoSpaces");
            int exposed = parsed.Count(value => value.Status == "UnquotedWithSpaces");
            int notApplicable = parsed.Count(value => value.Status == "NotApplicable");
            int unsupported = parsed.Count(value => value.Status == "Unsupported");
            int unavailable = parsed.Count(value => !value.Complete || value.Status == "Unavailable");
            string summary = $"Reviewed {parsed.Count} service executable registrations: {quoted} quote the executable path, {noSpaces} do not require quotes because the executable path contains no spaces, {exposed} are unquoted paths containing spaces, {notApplicable} are driver registrations, {unsupported} could not be parsed, and {unavailable} records are unavailable.";
            if (unavailable > 0 || unsupported > 0)
                return Unknown(summary + " Complete user-mode service path coverage could not be established.");
            if (exposed > 0)
                return new("Service executable path quoting", CheckState.Attention,
                    summary + " Review each affected service against its approved package and vendor command line before quoting the executable path. No automatic registry change is selected because arguments and vendor-specific launch syntax must be preserved." + Scope, Evidence: evidence);
            return new("Service executable path quoting", CheckState.Information,
                summary + " No unquoted user-mode service executable path containing spaces was established within this bounded inventory." + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return Unknown("Service executable path quoting evidence was malformed (" + error.GetType().Name + ").");
        }
    }

    private static bool ValidServiceName(string value) => DriverReference.ValidText(value, 256) &&
        !value.Any(character => char.IsControl(character) || character is '\\' or '/');
}
