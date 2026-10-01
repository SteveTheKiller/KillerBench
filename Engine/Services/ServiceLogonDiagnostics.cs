using System.Text.Json;

namespace KillerBench.Engine.Services;

public sealed record ServiceLogonObservation(string Name, string DisplayName, string ServiceType,
    string AccountClass, bool Interactive, bool Complete, string? Issue);

public static class ServiceLogonDiagnostics
{
    private const int MaximumServices = 1024;
    private static readonly HashSet<string> AccountClasses = new([
        "LocalSystem", "LocalService", "NetworkService", "VirtualAccount", "ManagedOrComputerAccount",
        "LocalUser", "DomainOrLocalAccount", "UpnAccount", "UserAccount", "NotApplicable", "Unavailable"
    ], StringComparer.Ordinal);
    private const string Scope = " Account names, domains, user identities, passwords, executable paths, arguments and process IDs are excluded. Account class does not establish whether credentials, logon rights, permissions, SPNs or network access are correct. No account, credential, group membership, permission, service, startup setting or registry value changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "function Get-AccountClass([string]$name){if([string]::IsNullOrWhiteSpace($name)){return 'LocalSystem'};$value=$name.Trim();" +
            "if($value -ieq 'LocalSystem' -or $value -ieq 'NT AUTHORITY\\SYSTEM'){return 'LocalSystem'};if($value -ieq 'NT AUTHORITY\\LocalService'){return 'LocalService'};if($value -ieq 'NT AUTHORITY\\NetworkService'){return 'NetworkService'};" +
            "if($value.StartsWith('NT SERVICE\\',[StringComparison]::OrdinalIgnoreCase)){return 'VirtualAccount'};if($value.EndsWith('$',[StringComparison]::Ordinal)){return 'ManagedOrComputerAccount'};if($value.StartsWith('.\\',[StringComparison]::Ordinal)){return 'LocalUser'};if($value.Contains('\\')){return 'DomainOrLocalAccount'};if($value.Contains('@')){return 'UpnAccount'};return 'UserAccount'};" +
            "$source=@(Get-CimInstance Win32_Service -ErrorAction Stop|Sort-Object Name|Select-Object -First 1025);$rows=[Collections.Generic.List[object]]::new();foreach($service in $source){try{$type=[string]$service.ServiceType;$accountClass=if($type -match 'Driver'){'NotApplicable'}else{Get-AccountClass ([string]$service.StartName)};" +
            "$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName=[string]$service.DisplayName;serviceType=$type;accountClass=$accountClass;interactive=[bool]$service.DesktopInteract;complete=$true;issue=$null})}" +
            "catch{$rows.Add([pscustomobject]@{name=[string]$service.Name;displayName='';serviceType='';accountClass='Unavailable';interactive=$false;complete=$false;issue=$_.Exception.GetType().Name})}};" +
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
        WorkflowCheck Unknown(string detail) => new("Service logon configuration", CheckState.Unknown, detail + Scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Service logon configuration evidence was unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("services", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumServices)
                return Unknown("Service logon configuration evidence exceeded its bound or was malformed.");
            var parsed = new List<ServiceLogonObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 7 ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("displayName", out var displayValue) || displayValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("serviceType", out var typeValue) || typeValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("accountClass", out var accountValue) || accountValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("interactive", out var interactiveValue) || interactiveValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("complete", out var rowComplete) || rowComplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("issue", out var issueValue) || issueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return Unknown("One or more service logon configuration records were invalid.");
                string name = nameValue.GetString()!;
                string displayName = displayValue.GetString()!;
                string serviceType = typeValue.GetString()!;
                string accountClass = accountValue.GetString()!;
                bool interactive = interactiveValue.GetBoolean();
                bool completeRow = rowComplete.GetBoolean();
                string? issue = issueValue.GetString();
                if (!ValidServiceName(name) || !identities.Add(name) || !AccountClasses.Contains(accountClass) ||
                    completeRow && (!DriverReference.ValidText(displayName, 512) || !DriverReference.ValidText(serviceType, 64) || accountClass == "Unavailable" || issue is not null || accountClass == "NotApplicable" && interactive) ||
                    !completeRow && (displayName.Length != 0 || serviceType.Length != 0 || accountClass != "Unavailable" || interactive || issue is null || !DriverReference.ValidText(issue, 128)))
                    return Unknown("One or more service logon configuration records were internally inconsistent.");
                parsed.Add(new(name, displayName, serviceType, accountClass, interactive, completeRow, issue));
            }
            if (parsed.Count == 0) return new("Service logon configuration", CheckState.Information,
                "No installed Win32 service records were returned." + Scope, Evidence: evidence);
            int unavailable = parsed.Count(value => !value.Complete);
            int drivers = parsed.Count(value => value.AccountClass == "NotApplicable");
            int localSystem = parsed.Count(value => value.AccountClass == "LocalSystem");
            int localService = parsed.Count(value => value.AccountClass == "LocalService");
            int networkService = parsed.Count(value => value.AccountClass == "NetworkService");
            int managed = parsed.Count(value => value.AccountClass is "VirtualAccount" or "ManagedOrComputerAccount");
            int named = parsed.Count(value => value.AccountClass is "LocalUser" or "DomainOrLocalAccount" or "UpnAccount" or "UserAccount");
            int interactiveCount = parsed.Count(value => value.Interactive);
            int invalidInteractive = parsed.Count(value => value.Interactive && value.AccountClass != "LocalSystem");
            string summary = $"Reviewed {parsed.Count} service logon configurations: {localSystem} use LocalSystem, {localService} use LocalService, {networkService} use NetworkService, {managed} use virtual, managed or computer accounts, {named} use named user accounts, {drivers} are driver registrations, {interactiveCount} request desktop interaction, and {unavailable} records are unavailable.";
            if (unavailable > 0) return Unknown(summary + " Complete service logon coverage could not be established.");
            if (invalidInteractive > 0) return new("Service logon configuration", CheckState.Attention,
                summary + $" {invalidInteractive} interactive service{(invalidInteractive == 1 ? " is" : "s are")} not configured for LocalSystem. Review the owning package and effective service configuration before changing its account or interaction setting." + Scope, Evidence: evidence);
            return new("Service logon configuration", CheckState.Information,
                summary + " Account classes are configuration context and do not establish least privilege or successful logon." + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return Unknown("Service logon configuration evidence was malformed (" + error.GetType().Name + ").");
        }
    }

    private static bool ValidServiceName(string value) => DriverReference.ValidText(value, 256) &&
        !value.Any(character => char.IsControl(character) || character is '\\' or '/');
}
