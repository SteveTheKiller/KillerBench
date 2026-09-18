using System.Text.Json;

namespace KillerBench.Engine;

public static class PrintExtensionFileDiagnostics
{
    private const int MaximumRegistrations = 384;
    private const string Scope = " This is point-in-time registration evidence. Full paths and signer identities are excluded. No extension was loaded, and no file, queue, driver, processor, monitor or registry setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
        "$items=[Collections.Generic.List[object]]::new();$complete=$true;" +
        "function Add-Extension([string]$kind,[string]$name,[string]$environment,[string]$driver,[string]$base){" +
        "if($items.Count-ge 384){$script:complete=$false;return};$file=[IO.Path]::GetFileName($driver);$exists=$false;$status='Unavailable';$errorType='';" +
        "try{$expanded=[Environment]::ExpandEnvironmentVariables($driver);$path=if([IO.Path]::IsPathRooted($expanded)){$expanded}else{Join-Path $base $expanded};" +
        "$exists=Test-Path -LiteralPath $path -PathType Leaf;if($exists){$signature=Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop;$status=[string]$signature.Status}else{$status='Missing'}}" +
        "catch{$script:complete=$false;$errorType=$_.Exception.GetType().FullName};" +
        "$items.Add([pscustomobject]@{kind=$kind;name=$name;environment=$environment;file=$file;exists=$exists;signature=$status;error=$errorType})};" +
        "$environmentRoot='Registry::HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Print\\Environments';" +
        "foreach($environmentKey in @(Get-ChildItem -LiteralPath $environmentRoot -ErrorAction Stop)){" +
        "$environment=[string]$environmentKey.PSChildName;$directory=[string](Get-ItemProperty -LiteralPath $environmentKey.PSPath -Name Directory -ErrorAction Stop).Directory;" +
        "$processorRoot=Join-Path $environmentKey.PSPath 'Print Processors';if(Test-Path -LiteralPath $processorRoot){foreach($processor in @(Get-ChildItem -LiteralPath $processorRoot -ErrorAction Stop)){" +
        "$driver=[string](Get-ItemProperty -LiteralPath $processor.PSPath -Name Driver -ErrorAction Stop).Driver;Add-Extension 'Processor' ([string]$processor.PSChildName) $environment $driver (Join-Path $env:windir ('System32\\spool\\prtprocs\\'+$directory))}}};" +
        "$monitorRoot='Registry::HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors';foreach($monitor in @(Get-ChildItem -LiteralPath $monitorRoot -ErrorAction Stop)){" +
        "$driver=[string](Get-ItemProperty -LiteralPath $monitor.PSPath -Name Driver -ErrorAction Stop).Driver;Add-Extension 'Monitor' ([string]$monitor.PSChildName) 'Windows system directory' $driver (Join-Path $env:windir 'System32')};" +
        "[pscustomobject]@{complete=$complete;items=$items}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Print extension file inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(35), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Print extension file provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Print extension file provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return [Unknown("Print extension file inspection exceeded its overall deadline.",
                new(-1, "", "Print extension file inspection deadline expired.", false) { TimedOut = true })];
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return [Unknown("Print extension file inspection was unavailable.", new(-1, "", error.Message, false))];
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("Print extension file collection failed or was incomplete.", evidence)];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > MaximumRegistrations)
                return [Unknown("Print extension file evidence was malformed or exceeded its registration limit.", evidence)];
            if (items.GetArrayLength() == 0)
                return [new("Print extension files", CheckState.Information,
                    "No print processor or monitor registrations were reported for this process account." + Scope, Evidence: evidence)];

            var checks = new List<WorkflowCheck>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 7) || !Choice(item, "kind", ["Processor", "Monitor"], out string kind) ||
                    !Text(item, "name", 512, out string name) || !Text(item, "environment", 128, out string environment) ||
                    !FileName(item, "file", out string file) || !Boolean(item, "exists", out bool exists) ||
                    !Text(item, "signature", 64, out string signature) || !OptionalText(item, "error", 256, out string error) ||
                    !identities.Add(kind + "\0" + environment + "\0" + name) ||
                    (signature == "Missing") != !exists || (error.Length > 0) != (signature == "Unavailable"))
                    return [Unknown("Print extension file evidence contained an invalid or duplicate registration.", evidence)];

                CheckState state = !complete || signature == "Unavailable" ? CheckState.Unknown :
                    !exists || !string.Equals(signature, "Valid", StringComparison.Ordinal) ? CheckState.Attention : CheckState.Information;
                string detail = $"Type: {kind}; environment: {environment}; registered file: {file}; present: {(exists ? "yes" : "no")}; Authenticode: {signature}.";
                if (!exists) detail += " The registered binary was not visible at collection time.";
                else if (state == CheckState.Attention) detail += " The registered binary did not report a Valid Authenticode status. Review its approved package source before replacement.";
                if (error.Length > 0) detail += " Inspection failed with " + error + ".";
                if (!complete) detail += " Overall collection was incomplete, so this registration cannot pass.";
                checks.Add(new($"Print {kind.ToLowerInvariant()} file: {name}", state, detail + Scope, Evidence: evidence));
            }
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return [Unknown("Print extension file evidence could not be interpreted.", evidence)];
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Print extension files", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Choice(JsonElement owner, string name, IReadOnlyList<string> choices, out string value) =>
        Text(owner, name, 32, out value) && choices.Contains(value, StringComparer.Ordinal);
    private static bool FileName(JsonElement owner, string name, out string value) =>
        Text(owner, name, 260, out value) && !value.Contains('/') && !value.Contains('\\') && value is not "." and not "..";
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
    private static bool OptionalText(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
