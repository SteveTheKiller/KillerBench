using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.Crash;

public sealed record ReliabilityRecord(DateTimeOffset TimeUtc, string Source, string Product, uint EventId, string Message);

public static class ReliabilityHistory
{
    private const int MaximumRecords = 200;
    private const string Scope = "Reliability records are historical observations. They do not establish current health, root cause, application ownership or repair eligibility. No applications, services or settings changed.";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Reliability history requires Windows.");
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$rows=@(Get-CimInstance -ClassName Win32_ReliabilityRecords -OperationTimeoutSec 15 | Where-Object TimeGenerated -ge (Get-Date).AddDays(-14) | Sort-Object TimeGenerated -Descending | Select-Object -First 201 | ForEach-Object { " +
            "$source=([string]$_.SourceName) -replace '[\x00-\x1F\x7F]',' '; $product=([string]$_.ProductName) -replace '[\x00-\x1F\x7F]',' '; $message=([string]$_.Message) -replace '[\x00-\x1F\x7F]',' '; " +
            "if($source.Length -gt 256){$source=$source.Substring(0,256)}; if($product.Length -gt 256){$product=$product.Substring(0,256)}; if($message.Length -gt 1024){$message=$message.Substring(0,1024)}; " +
            "[pscustomobject]@{time=$_.TimeGenerated.ToUniversalTime().ToString('O');source=$source;product=$product;eventId=[uint32]$_.EventIdentifier;message=$message} }); " +
            "[pscustomobject]@{complete=($rows.Count -le 200);records=@($rows | Select-Object -First 200)}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence, DateTimeOffset.UtcNow, token);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence, DateTimeOffset nowUtc, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        WorkflowCheck Unknown(string detail) => new("Reliability history coverage", CheckState.Unknown, detail + " " + Scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return [Unknown("Windows reliability history could not be read completely. Review the retained provider output.")];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (!Unique(root, 2) || !root.TryGetProperty("complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("records", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaximumRecords)
                return [Unknown("The reliability provider returned an unrecognized result.")];
            bool complete = completeValue.GetBoolean();
            var records = new List<ReliabilityRecord>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(row, 5) || !Text(row, "time", 64, out string timeText) ||
                    !DateTimeOffset.TryParseExact(timeText, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ||
                    time > nowUtc.AddMinutes(5) || time < nowUtc.AddDays(-15) ||
                    !Text(row, "source", 256, out string source) || !Text(row, "product", 256, out string product, allowEmpty: true) ||
                    !row.TryGetProperty("eventId", out var eventValue) || eventValue.ValueKind != JsonValueKind.Number || !eventValue.TryGetUInt32(out uint eventId) ||
                    !Text(row, "message", 1024, out string message, allowEmpty: true))
                    return [Unknown("A reliability record was malformed or outside the requested time range.")];
                string identity = time.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) + "\n" + source + "\n" + product + "\n" + eventId;
                if (!identities.Add(identity)) return [Unknown("Duplicate reliability records prevented complete interpretation.")];
                records.Add(new(time.ToUniversalTime(), source, product, eventId, message));
            }
            if (records.Count == 0)
                return [new("Reliability history", complete ? CheckState.Information : CheckState.Unknown,
                    "No reliability records were returned for the last 14 days. Absence of retained records does not establish fault-free operation. " + Scope, Evidence: evidence)];

            var checks = records.GroupBy(record => (record.Source, record.Product, record.EventId))
                .OrderByDescending(group => group.Max(record => record.TimeUtc))
                .ThenBy(group => group.Key.Source, StringComparer.Ordinal)
                .ThenBy(group => group.Key.Product, StringComparer.Ordinal)
                .Take(64)
                .Select(group => Describe(group, evidence)).ToList();
            if (!complete || records.GroupBy(record => (record.Source, record.Product, record.EventId)).Count() > 64)
                checks.Add(Unknown(!complete
                    ? "Only the newest 200 records were evaluated; older records in the selected period were omitted."
                    : "Only the newest 64 reliability groups were displayed; additional groups were omitted."));
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { return [Unknown("Malformed reliability history evidence could not be interpreted.")]; }
    }

    private static WorkflowCheck Describe(IGrouping<(string Source, string Product, uint EventId), ReliabilityRecord> group, ToolResult evidence)
    {
        var newest = group.OrderByDescending(record => record.TimeUtc).First();
        bool failure = (group.Key.Source.Equals("Application Error", StringComparison.OrdinalIgnoreCase) && group.Key.EventId == 1000) ||
            (group.Key.Source.Equals("Application Hang", StringComparison.OrdinalIgnoreCase) && group.Key.EventId == 1002) ||
            (group.Key.Source.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && group.Key.EventId == 6008);
        string subject = string.IsNullOrWhiteSpace(group.Key.Product) ? group.Key.Source : group.Key.Product;
        string preview = string.IsNullOrWhiteSpace(newest.Message) ? "No message was reported." : newest.Message.Replace('\r', ' ').Replace('\n', ' ');
        string label = group.Key.Source.Equals("Application Hang", StringComparison.OrdinalIgnoreCase) && group.Key.EventId == 1002 ? "Application stopped responding" :
            group.Key.Source.Equals("Application Error", StringComparison.OrdinalIgnoreCase) && group.Key.EventId == 1000 ? "Application terminated unexpectedly" :
            group.Key.Source.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && group.Key.EventId == 6008 ? "Unexpected Windows shutdown" : "Reliability record";
        return new(label + ": " + subject, failure ? CheckState.Attention : CheckState.Information,
            $"{group.Count()} retained record(s) in the last 14 days. Newest: {newest.TimeUtc:O}. Source: {group.Key.Source}; event: {group.Key.EventId}. Latest reported message: {preview} " + Scope,
            Evidence: evidence);
    }

    private static bool Unique(JsonElement value, int count) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == count && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == count;

    private static bool Text(JsonElement row, string name, int maximum, out string value, bool allowEmpty = false)
    {
        value = "";
        if (!row.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String) return false;
        value = item.GetString() ?? "";
        return value.Length <= maximum && (allowEmpty || value.Length > 0) && !value.Any(char.IsControl);
    }
}
