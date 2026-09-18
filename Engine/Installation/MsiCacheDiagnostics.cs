using System.Text.Json;

namespace KillerBench.Engine;

public sealed record MsiCacheObservation(string ProductCode, string Name, string Context, bool Complete,
    bool PackageConfigured, string? PackageFile, bool? PackageVisible, string? Issue);

public static class MsiCacheDiagnostics
{
    private const int MaximumProducts = 512;

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();$installer=$null;" +
            "try{$installer=New-Object -ComObject WindowsInstaller.Installer;foreach($p in @($installer.ProductsEx('',$null,7))){if($rows.Count-ge 513){break};" +
            "$code=[string]$p.ProductCode;$context=switch([int]$p.Context){1{'UserManaged'}2{'UserUnmanaged'}4{'Machine'}default{'Unknown'}};" +
            "try{$name=[string]$p.InstallProperty('ProductName');$path=[string]$p.InstallProperty('LocalPackage');$configured=-not[string]::IsNullOrWhiteSpace($path);" +
            "$rows.Add([pscustomobject]@{productCode=$code;name=$name;context=$context;complete=$true;packageConfigured=$configured;" +
            "packageFile=$(if($configured){[IO.Path]::GetFileName($path)}else{$null});packageVisible=$(if($configured){[IO.File]::Exists($path)}else{$null});issue=$null})}" +
            "catch{$rows.Add([pscustomobject]@{productCode=$code;name='';context=$context;complete=$false;packageConfigured=$false;packageFile=$null;packageVisible=$null;issue=$_.Exception.GetType().Name})}" +
            "finally{if($null-ne$p){[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($p)}}};" +
            "[pscustomobject]@{complete=($rows.Count-le 512);products=$rows}|ConvertTo-Json -Depth 4 -Compress}" +
            "finally{if($null-ne$installer){[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)}}";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? evidence)
    {
        const string scope = " Cached package visibility does not prove package integrity, source availability or application health. No installer, package, cache, registration, source, application data or policy setting changed.";
        WorkflowCheck Unknown(string detail) => new("MSI cached packages", CheckState.Unknown, detail + scope, Evidence: evidence);
        if (evidence is null || evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("The Windows Installer product and cache inventory was unavailable or incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("products", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumProducts)
                return [Unknown("The Windows Installer product inventory exceeded its evidence bound or was malformed.")];
            var parsed = new List<MsiCacheObservation>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 8 ||
                    !row.TryGetProperty("productCode", out var codeValue) || codeValue.ValueKind != JsonValueKind.String ||
                    !Guid.TryParseExact(codeValue.GetString(), "B", out var product) ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !row.TryGetProperty("context", out var contextValue) || contextValue.ValueKind != JsonValueKind.String ||
                    contextValue.GetString() is not ("Machine" or "UserManaged" or "UserUnmanaged" or "Unknown") ||
                    !row.TryGetProperty("complete", out var rowComplete) || rowComplete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("packageConfigured", out var configuredValue) || configuredValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !row.TryGetProperty("packageFile", out var fileValue) || fileValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null) ||
                    !row.TryGetProperty("packageVisible", out var visibleValue) || visibleValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null) ||
                    !row.TryGetProperty("issue", out var issueValue) || issueValue.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return [Unknown("One or more Windows Installer product cache records were invalid.")];
                string code = product.ToString("B").ToUpperInvariant();
                string context = contextValue.GetString()!;
                if (!identities.Add(code + "\0" + context)) return [Unknown("Windows Installer returned duplicate product and context identities.")];
                bool configured = configuredValue.GetBoolean();
                string? file = fileValue.GetString();
                bool? visible = visibleValue.ValueKind == JsonValueKind.Null ? null : visibleValue.GetBoolean();
                string? issue = issueValue.GetString();
                bool completeRow = rowComplete.GetBoolean();
                if (completeRow && (!DriverReference.ValidText(nameValue.GetString(), 512) || context == "Unknown" || issue is not null || configured != (file is not null) || configured != visible.HasValue ||
                    file is not null && (!DriverReference.ValidText(file, 260) || Path.GetFileName(file) != file || !file.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))) ||
                    !completeRow && (issue is null || !DriverReference.ValidText(issue, 128) || configured || file is not null || visible is not null))
                    return [Unknown("One or more Windows Installer product cache records were internally inconsistent.")];
                parsed.Add(new(code, nameValue.GetString()!, context, completeRow, configured, file, visible, issue));
            }
            if (parsed.Count == 0) return [new("MSI cached packages", CheckState.Information,
                "Windows Installer reported no products in the current machine and user contexts." + scope, Evidence: evidence)];
            var checks = new List<WorkflowCheck>();
            foreach (var product in parsed)
            {
                string label = string.IsNullOrWhiteSpace(product.Name) ? product.ProductCode : product.Name;
                if (!product.Complete)
                {
                    checks.Add(new("MSI cache: " + label, CheckState.Unknown,
                        $"Windows Installer cache properties were unavailable for {product.ProductCode} in {product.Context} context ({product.Issue})." + scope, Evidence: evidence));
                    continue;
                }
                if (!product.PackageConfigured)
                {
                    checks.Add(new("MSI cache: " + label, CheckState.Attention,
                        $"Windows Installer reports {product.ProductCode} in {product.Context} context without a cached LocalPackage path. Repair, update or removal may require the exact original source." + scope, Evidence: evidence));
                    continue;
                }
                if (product.PackageVisible != true)
                {
                    checks.Add(new("MSI cache: " + label, CheckState.Attention,
                        $"The registered cached package {product.PackageFile} for {product.ProductCode} in {product.Context} context was not visible to this process. Confirm permissions and the exact vendor source before repair, update or removal." + scope, Evidence: evidence));
                    continue;
                }
                checks.Add(new("MSI cache: " + label, CheckState.Information,
                    $"The registered cached package {product.PackageFile} is visible for {product.ProductCode} in {product.Context} context." + scope, Evidence: evidence));
            }
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return [Unknown("The Windows Installer product and cache inventory could not be interpreted safely.")];
        }
    }
}
