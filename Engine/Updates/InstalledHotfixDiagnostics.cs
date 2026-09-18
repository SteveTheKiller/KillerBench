using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record InstalledHotfixEntry(string Id, string Description, DateTimeOffset? InstalledUtc);

public static class InstalledHotfixDiagnostics
{
    public const int EntryLimit = 512;

    public static RepairStep Step() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $all=@(Get-CimInstance -ClassName Win32_QuickFixEngineering -OperationTimeoutSec 20); $entries=@(); " +
        "foreach($q in @($all | Select-Object -First 512)){ $date=$null; $parsed=[datetime]::MinValue; " +
        "if($q.InstalledOn -is [datetime]){$date=$q.InstalledOn.ToUniversalTime().ToString('o')} " +
        "elseif(-not [string]::IsNullOrWhiteSpace([string]$q.InstalledOn) -and [datetime]::TryParse([string]$q.InstalledOn,[ref]$parsed)){$date=$parsed.ToUniversalTime().ToString('o')} " +
        "$entries+=[pscustomobject]@{id=[string]$q.HotFixID;description=[string]$q.Description;installedUtc=$date} }; " +
        "[pscustomobject]@{total=$all.Count;entries=$entries} | ConvertTo-Json -Depth 3 -Compress"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail = "Installed Windows update evidence is unavailable, incomplete or unrecognized.") =>
            new("Installed Windows updates", CheckState.Unknown, detail + " No update, package or system setting changed.", Evidence: result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = json.RootElement;
            if (!UniqueFields(root) || root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("total", out var totalValue) || !totalValue.TryGetInt32(out int total) || total < 0 ||
                !root.TryGetProperty("entries", out var entriesValue) || entriesValue.ValueKind != JsonValueKind.Array ||
                entriesValue.GetArrayLength() != Math.Min(total, EntryLimit)) return Unknown();
            var entries = new List<InstalledHotfixEntry>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in entriesValue.EnumerateArray())
            {
                if (!UniqueFields(item) || item.ValueKind != JsonValueKind.Object ||
                    !ReadText(item, "id", 128, out string id) || !ReadText(item, "description", 256, out string description) ||
                    !identities.Add(id) || !ReadDate(item, "installedUtc", out var installedUtc)) return Unknown();
                entries.Add(new(id, description, installedUtc));
            }
            var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
            if (entries.Any(entry => entry.InstalledUtc > now.AddMinutes(5))) return Unknown();
            var ordered = entries.OrderByDescending(entry => entry.InstalledUtc).ThenBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase).ToArray();
            string detail = total == 0
                ? "Windows reported no installed hotfix records. This source does not include every update technology and does not prove that Windows is unpatched."
                : $"Windows reported {total} installed hotfix record(s). Retained {ordered.Length}{(total > EntryLimit ? $" within the {EntryLimit}-record bound" : "")}.";
            if (ordered.Length > 0)
                detail += "\n" + string.Join("\n", ordered.Take(50).Select(entry =>
                    $"{entry.Id} | {entry.Description} | {(entry.InstalledUtc is null ? "date unavailable" : entry.InstalledUtc.Value.ToString("O", CultureInfo.InvariantCulture))}"));
            if (ordered.Length > 50) detail += "\nAdditional retained records are available in raw evidence.";
            detail += " Inventory does not establish update applicability, supersedence, support status or successful remediation. No update operation was requested.";
            return new("Installed Windows updates", total > EntryLimit ? CheckState.Unknown : CheckState.Information, detail, Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException) { return Unknown(); }
    }

    private static bool ReadText(JsonElement item, string name, int limit, out string value)
    {
        value = "";
        if (!item.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String) return false;
        value = property.GetString() ?? "";
        return !string.IsNullOrWhiteSpace(value) && value.Length <= limit && !value.Any(char.IsControl);
    }

    private static bool ReadDate(JsonElement item, string name, out DateTimeOffset? value)
    {
        value = null;
        if (!item.TryGetProperty(name, out var property)) return false;
        if (property.ValueKind == JsonValueKind.Null) return true;
        if (property.ValueKind != JsonValueKind.String ||
            !DateTimeOffset.TryParseExact(property.GetString(), "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ||
            parsed.Offset != TimeSpan.Zero) return false;
        value = parsed;
        return true;
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
}
