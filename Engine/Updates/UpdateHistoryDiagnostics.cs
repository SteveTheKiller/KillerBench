using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record UpdateHistoryEntry(string Id, int Revision, string Title, DateTimeOffset Date, int Operation, int ResultCode, int HResult);

public static class UpdateHistoryDiagnostics
{
    public static RepairStep Step() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $session=New-Object -ComObject Microsoft.Update.Session; $session.ClientApplicationID='KillerBench'; " +
        "$searcher=$session.CreateUpdateSearcher(); $total=$searcher.GetTotalHistoryCount(); $entries=@(); " +
        "if($total -gt 0){$history=$searcher.QueryHistory(0,[Math]::Min(100,$total)); for($i=0;$i -lt $history.Count;$i++){ $entry=$history.Item($i); " +
        "$entries+=[pscustomobject]@{id=$entry.UpdateIdentity.UpdateID;revision=$entry.UpdateIdentity.RevisionNumber;title=$entry.Title;date=$entry.Date.ToUniversalTime().ToString('o');operation=[int]$entry.Operation;result=[int]$entry.ResultCode;hresult=[int]$entry.HResult} }}; " +
        "[pscustomobject]@{total=$total;entries=$entries} | ConvertTo-Json -Depth 4 -Compress"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown() => new("Windows Update history", CheckState.Unknown,
            "Update history evidence is unavailable, incomplete or unrecognized. No update operation was requested.", Evidence: result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (!UniqueFields(root) || root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("total", out var totalValue) || !totalValue.TryGetInt32(out int total) || total < 0 ||
                !root.TryGetProperty("entries", out var entriesValue) || entriesValue.ValueKind != JsonValueKind.Array ||
                entriesValue.GetArrayLength() != Math.Min(100, total)) return Unknown();
            var entries = new List<UpdateHistoryEntry>();
            var identities = new HashSet<(Guid Id, int Revision, DateTimeOffset Date, int Operation, int Result, int HResult)>();
            foreach (var item in entriesValue.EnumerateArray())
            {
                if (!UniqueFields(item) || item.ValueKind != JsonValueKind.Object ||
                    !ReadGuid(item, "id", out var id) || !ReadInt(item, "revision", out int revision) || revision < 0 ||
                    !ReadText(item, "title", 4096, out string title) || !ReadDate(item, "date", out var date) ||
                    !ReadInt(item, "operation", out int operation) || operation is not (1 or 2) ||
                    !ReadInt(item, "result", out int resultCode) || resultCode is < 0 or > 5 ||
                    !ReadInt(item, "hresult", out int hresult) || !identities.Add((id, revision, date, operation, resultCode, hresult))) return Unknown();
                entries.Add(new(id.ToString("D"), revision, title, date, operation, resultCode, hresult));
            }
            var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
            if (entries.Any(entry => entry.Date > now.AddMinutes(5))) return Unknown();
            var recent = entries.Where(entry => entry.Date >= now.AddDays(-30)).ToArray();
            var unresolved = recent.Where(entry => entry.ResultCode is 0 or 1 or 3 or 4 or 5).ToArray();
            var recognizedFailures = unresolved.Select(entry => WindowsUpdateErrorCatalog.Find(entry.HResult)).Where(reference => reference is not null)
                .DistinctBy(reference => reference!.Code).Cast<WindowsUpdateCodeReference>().ToArray();
            string summary = entries.Count == 0 ? "No update history entries were reported. This is not proof that updates were never attempted. " :
                $"Retained {entries.Count} newest of {total} reported entries; {recent.Length} occurred in the last 30 days and {unresolved.Length} reported incomplete, failed, aborted or error-qualified outcomes. ";
            if (recognizedFailures.Length > 0)
                summary += "Diagnostic routes: " + string.Join("; ", recognizedFailures.Select(reference => reference.Symbol + " - " + reference.DiagnosticScope.TrimEnd('.'))) + ". ";
            string details = string.Join("\n", entries.Take(25).Select(Format));
            if (entries.Count > 25) details += "\nAdditional retained history entries are available in raw evidence.";
            return new("Windows Update history", unresolved.Length > 0 ? CheckState.Attention : CheckState.Information,
                summary + "Historical outcomes do not establish current applicability or current system health. No update operation was requested." +
                (details.Length == 0 ? "" : "\n" + details), Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException) { return Unknown(); }
    }

    public static IReadOnlyList<WindowsUpdateCodeReference> RecentReferences(ToolResult result, DateTimeOffset? nowUtc = null)
    {
        if (Interpret(result, nowUtc).State == CheckState.Unknown) return [];
        using var json = JsonDocument.Parse(result.Output);
        var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        return Array.AsReadOnly(json.RootElement.GetProperty("entries").EnumerateArray()
            .Where(item => ReadDate(item, "date", out var date) && date >= now.AddDays(-30) &&
                ReadInt(item, "result", out int resultCode) && resultCode is 0 or 1 or 3 or 4 or 5 &&
                ReadInt(item, "hresult", out _))
            .Select(item => WindowsUpdateErrorCatalog.Find(item.GetProperty("hresult").GetInt32()))
            .Where(reference => reference is not null).DistinctBy(reference => reference!.Code)
            .Cast<WindowsUpdateCodeReference>().ToArray());
    }

    private static string Format(UpdateHistoryEntry entry)
    {
        var reference = WindowsUpdateErrorCatalog.Find(entry.HResult);
        return $"{entry.Date:O} | {(entry.Operation == 1 ? "Install" : "Uninstall")} | {ResultName(entry.ResultCode)} | 0x{unchecked((uint)entry.HResult):X8}" +
            (reference is null ? "" : $" {reference.Symbol}: {reference.Meaning} Next: {reference.DiagnosticScope}") +
            $" | {entry.Title} | {entry.Id} revision {entry.Revision}";
    }

    private static string ResultName(int result) => result switch
    {
        0 => "Not started", 1 => "In progress", 2 => "Succeeded", 3 => "Succeeded with errors", 4 => "Failed", 5 => "Aborted", _ => "Unknown"
    };

    private static bool ReadGuid(JsonElement item, string name, out Guid value)
    {
        value = default;
        return item.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String &&
            Guid.TryParse(property.GetString(), out value) && value != Guid.Empty;
    }

    private static bool ReadInt(JsonElement item, string name, out int value)
    {
        value = default;
        return item.TryGetProperty(name, out var property) && property.TryGetInt32(out value);
    }

    private static bool ReadText(JsonElement item, string name, int limit, out string value)
    {
        value = "";
        if (!item.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String) return false;
        value = property.GetString() ?? "";
        return !string.IsNullOrWhiteSpace(value) && value.Length <= limit && !value.Any(char.IsControl);
    }

    private static bool ReadDate(JsonElement item, string name, out DateTimeOffset value)
    {
        value = default;
        return item.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParseExact(property.GetString(), "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value) && value.Offset == TimeSpan.Zero;
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
}
