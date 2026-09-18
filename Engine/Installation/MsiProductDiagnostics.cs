using System.Text.Json;

namespace KillerBench.Engine;

public sealed record MsiProductCandidate(string ProductCode, string Name, string? Version, string View)
{
    public override string ToString() => Name + (string.IsNullOrWhiteSpace(Version) ? "" : " " + Version);
}

public static class MsiProductDiagnostics
{
    public static IReadOnlyList<MsiProductCandidate> SelectCandidates(InstalledApplicationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var registrations = new List<MsiProductCandidate>();
        foreach (var application in inventory.Applications)
        {
            if (!Guid.TryParseExact(application.Key, "B", out var product)) continue;
            string code = product.ToString("B").ToUpperInvariant();
            registrations.Add(new(code, application.Name, application.Version, application.View));
        }
        return registrations.GroupBy(candidate => candidate.ProductCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(candidate => candidate.View == "Registry64").ThenBy(candidate => candidate.Name, StringComparer.CurrentCultureIgnoreCase).First())
            .OrderBy(candidate => candidate.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(candidate => candidate.Version, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static string NormalizeProductCode(string productCode)
    {
        if (!Guid.TryParseExact(productCode, "B", out var parsed))
            throw new ArgumentException("MSI product code must be a GUID enclosed in braces.", nameof(productCode));
        return parsed.ToString("B").ToUpperInvariant();
    }

    public static RepairStep Step(string productCode)
    {
        string normalized = NormalizeProductCode(productCode);
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $code='" + normalized + "'; " +
            "$installer=New-Object -ComObject WindowsInstaller.Installer; try {$state=[int]$installer.ProductState($code); " +
            "[pscustomobject]@{productCode=$code;state=$state}|ConvertTo-Json -Compress} finally {if ($null -ne $installer) {[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)}}";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<WorkflowCheck> CheckAsync(string productCode, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("MSI product registration collection requires Windows.");
        return Interpret(NormalizeProductCode(productCode),
            await BoundedTool.RunAsync(Step(productCode), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(string productCode, ToolResult result)
    {
        string normalized = NormalizeProductCode(productCode);
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown() => new("MSI product " + normalized, CheckState.Unknown,
            "The exact MSI product registration state was unavailable, incomplete or malformed. Product presence and health were not inferred. No installation, repair or removal was attempted.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !Unique(root, "productCode", out var code) || code.ValueKind != JsonValueKind.String ||
                !string.Equals(code.GetString(), normalized, StringComparison.OrdinalIgnoreCase) ||
                !Unique(root, "state", out var stateValue) || !stateValue.TryGetInt32(out int state) || state is < -7 or > 5)
                return Unknown();
            var (classification, label, guidance) = state switch
            {
                -7 => (CheckState.Information, "Not used", "Windows Installer reports the product state as not used."),
                -6 => (CheckState.Attention, "Bad configuration", "Windows Installer reports damaged configuration for this registration. Use only the product vendor's supported recovery path."),
                -5 => (CheckState.Attention, "Incomplete", "Windows Installer reports an incomplete installation. Correlate current activity, event history and the package log before recovery."),
                -4 => (CheckState.Attention, "Source absent", "Windows Installer reports an unavailable source. Confirm the exact vendor source without editing cached package registrations."),
                -3 => (CheckState.Unknown, "More data required", "The Installer API did not return a complete product state."),
                -2 => (CheckState.Unknown, "Invalid argument", "The Installer API rejected the exact product query even though its GUID syntax was valid."),
                -1 => (CheckState.Information, "Unknown product", "Windows Installer does not report this product code as registered in the queried context."),
                0 => (CheckState.Attention, "Broken", "Windows Installer reports a broken registration. Confirm product identity and use its supported recovery procedure."),
                1 => (CheckState.Information, "Advertised", "Windows Installer reports an advertised product. This does not prove local feature installation."),
                2 => (CheckState.Information, "Absent", "Windows Installer reports the product as absent in the queried context."),
                3 => (CheckState.Information, "Local", "Windows Installer reports the product installed locally."),
                4 => (CheckState.Information, "Source", "Windows Installer reports the product installed to run from source."),
                5 => (CheckState.Information, "Default", "Windows Installer reports the product in its default installed state."),
                _ => throw new InvalidOperationException()
            };
            return new("MSI product " + normalized, classification,
                $"Reported state: {label} ({state}). {guidance} Registration state does not prove application health, ownership, source availability or current version suitability. No package data or settings changed.", Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException) { return Unknown(); }
    }

    public static bool TryGetReportedState(string productCode, WorkflowCheck check, out int state)
    {
        string normalized = NormalizeProductCode(productCode);
        ArgumentNullException.ThrowIfNull(check);
        state = default;
        var result = check.Evidence;
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return false;
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = json.RootElement;
            return root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Count() == 2 &&
                Unique(root, "productCode", out var code) && code.ValueKind == JsonValueKind.String &&
                string.Equals(code.GetString(), normalized, StringComparison.OrdinalIgnoreCase) &&
                Unique(root, "state", out var value) && value.TryGetInt32(out state) && state is >= -7 and <= 5;
        }
        catch (JsonException) { return false; }
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
