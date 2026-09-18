using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public static class FileHistoryEvents
{
    private const string Name = "Recent File History events";
    private const string Channel = "Microsoft-Windows-FileHistory-Engine/BackupLog";
    private const string Boundary = " This covers only warning, error and critical records from the local File History backup log during the last 7 days. Event payloads, configuration paths, account names, computer names and backup destinations are excluded. Historical events do not establish current configuration, destination availability, recoverability or a repair prerequisite. No backup, file, schedule, retention setting, event channel or service changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$channel='" + Channel + "';" +
        "function Emit($available,$complete,$retained,$overflow,$critical,$errors,$warnings,$newest,$eventId,$level){[pscustomobject]@{channelAvailable=$available;complete=$complete;retained=$retained;overflow=$overflow;critical=$critical;errors=$errors;warnings=$warnings;newestUtc=$newest;newestEventId=$eventId;newestLevel=$level}|ConvertTo-Json -Compress};" +
        "try{$items=@(Get-WinEvent -FilterHashtable @{LogName=$channel;Level=1,2,3;StartTime=(Get-Date).AddDays(-7)} -MaxEvents 201 -ErrorAction Stop)}catch{if($_.FullyQualifiedErrorId-like'NoMatchingEventsFound*'){Emit $true $true 0 $false 0 0 0 $null $null $null;exit 0};if($_.FullyQualifiedErrorId-like'NoMatchingLogsFound*'){Emit $false $true 0 $false 0 0 0 $null $null $null;exit 0};Emit $true $false 0 $false 0 0 0 $null $null $null;exit 0};" +
        "$overflow=$items.Count-gt200;$kept=@($items|Select-Object -First 200);$newest=if($kept.Count-eq0){$null}else{([DateTimeOffset]$kept[0].TimeCreated).ToUniversalTime().ToString('o')};$eventId=if($kept.Count-eq0){$null}else{[int64]$kept[0].Id};$level=if($kept.Count-eq0){$null}else{[int]$kept[0].Level};" +
        "Emit $true $true $kept.Count $overflow @($kept|Where-Object Level -eq 1).Count @($kept|Where-Object Level -eq 2).Count @($kept|Where-Object Level -eq 3).Count $newest $eventId $level";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("File History event review requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("File History collector returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("File History collector returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "File History event deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset? observedUtc = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (!Unique(root, 10) || !Boolean(root, "channelAvailable", out bool available) ||
                !Boolean(root, "complete", out bool complete) ||
                !Integer(root, "retained", out int retained) || !Boolean(root, "overflow", out bool overflow) ||
                !Integer(root, "critical", out int critical) || !Integer(root, "errors", out int errors) ||
                !Integer(root, "warnings", out int warnings) || !NullableTime(root, "newestUtc", out var newest) ||
                !NullableInt64(root, "newestEventId", out long? eventId) || !NullableInt32(root, "newestLevel", out int? level))
                return Unknown("Evidence was malformed.", evidence);
            if (!complete) return Unknown("The event channel did not return complete evidence.", evidence);
            if (!available)
            {
                if (retained != 0 || overflow || critical != 0 || errors != 0 || warnings != 0 || newest is not null || eventId is not null || level is not null)
                    return Unknown("Channel availability evidence was contradictory.", evidence);
                return new(Name, CheckState.Information,
                    "The File History backup event channel is not registered on this Windows installation." + Boundary,
                    Evidence: evidence);
            }
            if (retained is < 0 or > 200 || critical is < 0 or > 200 || errors is < 0 or > 200 || warnings is < 0 or > 200 ||
                critical + errors + warnings != retained || overflow && retained != 200)
                return Unknown("Event counts were contradictory.", evidence);
            if (retained == 0)
            {
                if (overflow || newest is not null || eventId is not null || level is not null)
                    return Unknown("Empty event evidence included a newest record.", evidence);
                return new(Name, CheckState.Information,
                    "No File History warning, error or critical records were retained for the last 7 days. An empty event window is not a backup-health verdict." + Boundary,
                    Evidence: evidence);
            }
            DateTimeOffset now = observedUtc ?? DateTimeOffset.UtcNow;
            if (newest is null || newest > now.AddMinutes(5) || newest < now.AddDays(-8) || eventId is < 0 or > 65535 || level is not (1 or 2 or 3))
                return Unknown("Newest-event evidence was incomplete or contradictory.", evidence);
            string newestText = newest.Value.ToString("u", CultureInfo.InvariantCulture);
            string detail = "Last 7 days, newest first: retained " + retained + " severity records (" + critical + " critical, " + errors + " error, " + warnings + " warning). The newest retained record is event " + eventId + " at " + newestText + ". Counts are events, not unique backup failures.";
            if (overflow)
                return new(Name, CheckState.Unknown,
                    detail + " More than 200 matching records exist, so the bounded review is incomplete." + Boundary,
                    Evidence: evidence);
            return new(Name, CheckState.Attention,
                detail + " Review the matching event in Event Viewer together with current File History configuration and destination state." + Boundary,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new(Name, CheckState.Unknown, detail + Boundary, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value);
    }
    private static bool NullableInt32(JsonElement owner, string name, out int? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out int number)) return false;
        value = number;
        return true;
    }
    private static bool NullableInt64(JsonElement owner, string name, out long? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt64(out long number)) return false;
        value = number;
        return true;
    }
    private static bool NullableTime(JsonElement owner, string name, out DateTimeOffset? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            !DateTimeOffset.TryParseExact(text, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)) return false;
        value = timestamp.ToUniversalTime();
        return true;
    }
}
