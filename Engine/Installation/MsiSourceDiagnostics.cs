using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record MsiSourceObservation(string ProductKey, string Name, bool PackageConfigured,
    string? PackageFile, string LastUsedType, int NetworkSources, int UrlSources, int MediaSources,
    bool Complete, string? Issue);

public static class MsiSourceDiagnostics
{
    private const int MaximumProducts = 512;
    private const int MaximumSourcesPerType = 256;
    private const string Scope = " Source locations, URLs, media labels, prompts, user identities and credentials are excluded. Registered source metadata does not prove that a source is currently reachable or trusted. No source, package, cache, registration or Installer setting changed.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();$root=$null;" +
            "try{$root=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry64).OpenSubKey('SOFTWARE\\Classes\\Installer\\Products');" +
            "if($null-eq$root){throw[IO.IOException]::new('Installer product source registry is unavailable.')}foreach($id in @($root.GetSubKeyNames()|Sort-Object)){if($rows.Count-ge 513){break};$product=$null;$source=$null;" +
            "try{$product=$root.OpenSubKey($id);$source=$product.OpenSubKey('SourceList');$name=[string]$product.GetValue('ProductName','');if($null-eq$source){$rows.Add([pscustomobject]@{productKey=$id;name=$name;packageConfigured=$false;packageFile=$null;lastUsedType='';networkSources=0;urlSources=0;mediaSources=0;complete=$true;issue=$null});continue};" +
            "$package=[string]$source.GetValue('PackageName','');$last=[string]$source.GetValue('LastUsedSource','');$type=if($last-match '^[nmu];'){[string]$last[0]}else{''};" +
            "$counts=@{};foreach($kind in @('Net','URL','Media')){$child=$null;try{$child=$source.OpenSubKey($kind);$counts[$kind]=if($null-eq$child){0}else{@($child.GetValueNames()).Count}}finally{if($null-ne$child){$child.Dispose()}}};" +
            "$rows.Add([pscustomobject]@{productKey=$id;name=$name;packageConfigured=(-not[string]::IsNullOrWhiteSpace($package));packageFile=$(if([string]::IsNullOrWhiteSpace($package)){$null}else{[IO.Path]::GetFileName($package)});lastUsedType=$type;networkSources=[int]$counts.Net;urlSources=[int]$counts.URL;mediaSources=[int]$counts.Media;complete=$true;issue=$null})}" +
            "catch{$rows.Add([pscustomobject]@{productKey=$id;name='';packageConfigured=$false;packageFile=$null;lastUsedType='';networkSources=0;urlSources=0;mediaSources=0;complete=$false;issue=$_.Exception.GetType().Name})}" +
            "finally{if($null-ne$source){$source.Dispose()};if($null-ne$product){$product.Dispose()}}};[pscustomobject]@{complete=($rows.Count-le 512);products=$rows}|ConvertTo-Json -Depth 4 -Compress}" +
            "finally{if($null-ne$root){$root.Dispose()}}";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? evidence)
    {
        WorkflowCheck Unknown(string detail) => new("MSI source metadata", CheckState.Unknown, detail + Scope, Evidence: evidence);
        if (evidence is null || evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("The registered Windows Installer source inventory was unavailable or incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("products", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumProducts)
                return [Unknown("The registered Windows Installer source inventory exceeded its evidence bound or was malformed.")];
            var parsed = new List<MsiSourceObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 10 ||
                    !row.TryGetProperty("productKey", out var keyValue) || keyValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("packageConfigured", out var configuredValue) || configuredValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("packageFile", out var fileValue) || fileValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("lastUsedType", out var typeValue) || typeValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("networkSources", out var networkValue) || !networkValue.TryGetInt32(out int network) ||
                    !row.TryGetProperty("urlSources", out var urlValue) || !urlValue.TryGetInt32(out int url) ||
                    !row.TryGetProperty("mediaSources", out var mediaValue) || !mediaValue.TryGetInt32(out int media) ||
                    !row.TryGetProperty("complete", out var rowComplete) || rowComplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("issue", out var issueValue) || issueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return [Unknown("One or more registered Windows Installer source records were invalid.")];
                string key = keyValue.GetString()!;
                string name = nameValue.GetString()!;
                string type = typeValue.GetString()!;
                string? file = fileValue.GetString();
                string? issue = issueValue.GetString();
                bool configured = configuredValue.GetBoolean();
                bool completeRow = rowComplete.GetBoolean();
                if (!ValidProductKey(key) || !identities.Add(key) || network is < 0 or > MaximumSourcesPerType || url is < 0 or > MaximumSourcesPerType || media is < 0 or > MaximumSourcesPerType ||
                    completeRow && (!DriverReference.ValidText(name, 512) || configured != (file is not null) || file is not null && (!DriverReference.ValidText(file, 260) || Path.GetFileName(file) != file || !file.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)) || type is not ("" or "n" or "u" or "m") || issue is not null) ||
                    !completeRow && (name.Length != 0 || configured || file is not null || type.Length != 0 || network != 0 || url != 0 || media != 0 || issue is null || !DriverReference.ValidText(issue, 128)))
                    return [Unknown("One or more registered Windows Installer source records were internally inconsistent.")];
                parsed.Add(new(key, name, configured, file, type, network, url, media, completeRow, issue));
            }
            if (parsed.Count == 0) return [new("MSI source metadata", CheckState.Information,
                "No Windows Installer product source registrations were visible in the machine product catalog." + Scope, Evidence: evidence)];
            int unavailable = parsed.Count(value => !value.Complete);
            int missingPackage = parsed.Count(value => value.Complete && !value.PackageConfigured);
            int noSources = parsed.Count(value => value.Complete && value.NetworkSources + value.UrlSources + value.MediaSources == 0 && value.LastUsedType.Length == 0);
            int networkProducts = parsed.Count(value => value.Complete && (value.NetworkSources > 0 || value.LastUsedType == "n"));
            int urlProducts = parsed.Count(value => value.Complete && (value.UrlSources > 0 || value.LastUsedType == "u"));
            int mediaProducts = parsed.Count(value => value.Complete && (value.MediaSources > 0 || value.LastUsedType == "m"));
            string summary = $"Reviewed {parsed.Count} registered product source record{(parsed.Count == 1 ? "" : "s")}: {networkProducts} include network source metadata, {urlProducts} include URL source metadata, and {mediaProducts} include media source metadata.";
            if (unavailable > 0) return [Unknown(summary + $" {unavailable} product record{(unavailable == 1 ? " was" : "s were")} unavailable.")];
            if (missingPackage > 0 || noSources > 0)
                return [new("MSI source metadata", CheckState.Attention,
                    summary + $" {missingPackage} record{(missingPackage == 1 ? " is" : "s are")} missing the registered package filename and {noSources} record{(noSources == 1 ? " has" : "s have")} no registered source category or last-used source type. Review affected products before repair, modify or install-on-demand operations that may need original media." + Scope, Evidence: evidence)];
            return [new("MSI source metadata", CheckState.Information,
                summary + " Every complete record includes a package filename; source reachability was intentionally not tested." + Scope, Evidence: evidence)];
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return [Unknown("The registered Windows Installer source inventory was malformed (" + error.GetType().Name + ").")];
        }
    }

    private static bool ValidProductKey(string value) => value.Length == 32 && value.All(character => char.IsAsciiHexDigit(character));
}
