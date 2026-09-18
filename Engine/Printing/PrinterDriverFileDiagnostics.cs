using System.Text.Json;

namespace KillerBench.Engine;

public static class PrinterDriverFileDiagnostics
{
    private const int MaximumDrivers = 128;
    private const int MaximumFiles = 1024;
    private const string Scope = " File presence and Authenticode status are point-in-time registration evidence. They do not establish package integrity, compatibility, print rendering or physical output. Full file paths are excluded. No file, driver, queue or setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Import-Module ($PSHOME+'\\Modules\\Microsoft.PowerShell.Security\\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;" +
        "$all=@(Get-PrinterDriver -ErrorAction Stop|Select-Object -First 129);$complete=($all.Count-le 128);$remaining=1024;$items=@();" +
        "foreach($driver in @($all|Select-Object -First 128)){" +
        "$paths=@(@($driver.DriverPath,$driver.ConfigFile,$driver.DataFile,$driver.InfPath)+@($driver.DependentFiles)|Where-Object{$_ -is [string]-and $_.Length-gt 0}|Select-Object -Unique);" +
        "if($paths.Count-gt $remaining){$complete=$false;$paths=@($paths|Select-Object -First $remaining)};" +
        "$declared=$paths.Count;$missing=0;$signed=0;$review=0;$nonExecutable=0;$unknown=0;$errors=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal);" +
        "foreach($path in $paths){$remaining--;try{if(-not(Test-Path -LiteralPath $path -PathType Leaf)){$missing++;continue};" +
        "$extension=[IO.Path]::GetExtension($path);if($extension -notin @('.dll','.exe','.sys')){$nonExecutable++;continue};" +
        "$signature=Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop;if($signature.Status -eq 'Valid'){$signed++}else{$review++}}catch{$unknown++;$complete=$false;$null=$errors.Add($_.Exception.GetType().FullName)}};" +
        "$items+=[pscustomobject]@{name=[string]$driver.Name;environment=[string]$driver.PrinterEnvironment;declared=$declared;missing=$missing;signed=$signed;signatureReview=$review;nonExecutable=$nonExecutable;unknown=$unknown;errors=@($errors|Select-Object -First 8)}};" +
        "[pscustomobject]@{complete=$complete;items=$items}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Printer driver file inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(50));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(40), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Printer driver file provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Printer driver file provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return [Unknown("Printer driver file inspection exceeded its overall deadline.",
                new(-1, "", "Printer driver file inspection deadline expired.", false) { TimedOut = true })];
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return [Unknown("Printer driver file inspection was unavailable.", new(-1, "", error.Message, false))];
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("Printer driver file collection failed or was incomplete.", evidence)];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > MaximumDrivers)
                return [Unknown("Printer driver file evidence was malformed or exceeded its driver limit.", evidence)];
            if (items.GetArrayLength() == 0)
                return [new("Printer driver files", CheckState.Information,
                    "No local printer driver registrations were reported for this process account." + Scope, Evidence: evidence)];

            var checks = new List<WorkflowCheck>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalFiles = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 9) || !Text(item, "name", out string name) || !Text(item, "environment", out string environment) ||
                    !Count(item, "declared", out int declared) || !Count(item, "missing", out int missing) ||
                    !Count(item, "signed", out int signed) || !Count(item, "signatureReview", out int signatureReview) ||
                    !Count(item, "nonExecutable", out int nonExecutable) || !Count(item, "unknown", out int unknown) ||
                    !TextArray(item, "errors", out var errors) || (unknown == 0) != (errors.Count == 0) ||
                    declared != missing + signed + signatureReview + nonExecutable + unknown ||
                    !identities.Add(name + "\0" + environment) || totalFiles > MaximumFiles - declared)
                    return [Unknown("Printer driver file evidence contained an invalid or duplicate registration.", evidence)];
                totalFiles += declared;
                CheckState state = !complete || unknown > 0 ? CheckState.Unknown : missing > 0 || signatureReview > 0 ? CheckState.Attention : CheckState.Information;
                string detail = $"Environment: {environment}. Declared files: {declared}; present signed executable files: {signed}; present non-executable package files: {nonExecutable}; missing: {missing}; signature review: {signatureReview}; unavailable: {unknown}.";
                if (missing > 0) detail += " One or more registered files were not visible at collection time.";
                if (signatureReview > 0) detail += " One or more present executable files did not report a Valid Authenticode status. Review the approved package source before replacement.";
                if (errors.Count > 0) detail += " Unavailable checks reported: " + string.Join(", ", errors) + ".";
                if (!complete) detail += " Overall collection was incomplete, so this registration cannot pass.";
                checks.Add(new("Printer driver files: " + name, state, detail + Scope, Evidence: evidence));
            }
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return [Unknown("Printer driver file evidence could not be interpreted.", evidence)];
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Printer driver files", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Count(JsonElement owner, string name, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt32(out value) && value is >= 0 and <= MaximumFiles;
    }
    private static bool Text(JsonElement owner, string name, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 or > 512 || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
    private static bool TextArray(JsonElement owner, string name, out IReadOnlyList<string> values)
    {
        values = [];
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.Array || item.GetArrayLength() > 8) return false;
        var result = new List<string>();
        foreach (var entry in item.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String || entry.GetString() is not { } text || text.Length is < 1 or > 256 ||
                text != text.Trim() || text.Any(char.IsControl) || result.Contains(text, StringComparer.Ordinal)) return false;
            result.Add(text);
        }
        values = result.AsReadOnly();
        return true;
    }
}
