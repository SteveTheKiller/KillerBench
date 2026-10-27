using System.Text.Json;

namespace KillerBench.Engine.Scheduling;

public enum ExactScheduledTaskReadState { Found, Missing, Ambiguous, Invalid, Unavailable }

public sealed record ExactScheduledTaskRead(ExactScheduledTaskReadState State, ScheduledTaskSnapshot? Task = null);

public static class ExactScheduledTaskDiagnostics
{
    public static async Task<(ExactScheduledTaskRead Read, ToolResult Evidence)> ReadAsync(
        string taskPath, string taskName, TimeSpan timeout, CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Scheduled task inspection requires Windows.");
        var evidence = await BoundedTool.RunAsync(CreateReadStep(taskPath, taskName), timeout, token: token).ConfigureAwait(false);
        return (Parse(evidence, taskPath, taskName, token), evidence);
    }

    public static RepairStep CreateReadStep(string taskPath, string taskName)
    {
        ValidateIdentity(taskPath, taskName);
        string path = taskPath.Replace("'", "''", StringComparison.Ordinal);
        string name = taskName.Replace("'", "''", StringComparison.Ordinal);
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            $"$task=$null; try {{$task=Get-ScheduledTask -TaskPath '{path}' -TaskName '{name}' -ErrorAction Stop}} catch {{" +
            "if($_.CategoryInfo.Category -eq 'ObjectNotFound' -or \"$($_.FullyQualifiedErrorId)\".StartsWith('CmdletizationQuery_NotFound')){" +
            "[pscustomobject]@{found=$false} | ConvertTo-Json -Compress; exit 0}; [Console]::Error.WriteLine($_.Exception.Message); exit 1}; " +
            "$items=@($task); if($items.Count -ne 1){[pscustomobject]@{found=$true;ambiguous=$true} | ConvertTo-Json -Compress; exit 0}; " +
            "$runtime=$null; try {$runtime=$items[0] | Get-ScheduledTaskInfo -ErrorAction Stop} catch {}; " +
            "$lastRun=$null; if($null -ne $runtime -and $null -ne $runtime.LastRunTime){$lastRun=$runtime.LastRunTime.ToString('yyyy-MM-ddTHH:mm:ss.fffffff',[Globalization.CultureInfo]::InvariantCulture)}; " +
            "[pscustomobject]@{found=$true;ambiguous=$false;path=$items[0].TaskPath;name=$items[0].TaskName;state=$items[0].State.ToString();actionCount=@($items[0].Actions).Count;runtimeAvailable=($null -ne $runtime);lastTaskResult=$runtime.LastTaskResult;missedRuns=$runtime.NumberOfMissedRuns;lastRunLocalTime=$lastRun} | ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static ExactScheduledTaskRead Parse(ToolResult result, string taskPath, string taskName, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ValidateIdentity(taskPath, taskName);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(ExactScheduledTaskReadState.Unavailable);
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (!Unique(root) || root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("found", out var found) ||
                found.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return new(ExactScheduledTaskReadState.Invalid);
            if (!found.GetBoolean())
                return new(root.EnumerateObject().Count() == 1 ? ExactScheduledTaskReadState.Missing : ExactScheduledTaskReadState.Invalid);
            if (!root.TryGetProperty("ambiguous", out var ambiguous) || ambiguous.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return new(ExactScheduledTaskReadState.Invalid);
            if (ambiguous.GetBoolean())
                return new(root.EnumerateObject().Count() == 2 ? ExactScheduledTaskReadState.Ambiguous : ExactScheduledTaskReadState.Invalid);
            string? path = Text(root, "path", 256), name = Text(root, "name", 256), state = Text(root, "state", 32);
            int? actionCount = Number(root, "actionCount", 0, 32);
            bool? runtimeAvailable = Boolean(root, "runtimeAvailable");
            uint? lastResult = Unsigned(root, "lastTaskResult"), missedRuns = Unsigned(root, "missedRuns");
            DateTime? lastRun = LocalTime(root, "lastRunLocalTime");
            if (path != taskPath || name != taskName || state is not ("Unknown" or "Disabled" or "Queued" or "Ready" or "Running") ||
                actionCount is null || runtimeAvailable is null || (runtimeAvailable == true && (lastResult is null || missedRuns is null)) ||
                (runtimeAvailable == false && (lastResult is not null || missedRuns is not null || lastRun is not null)))
                return new(ExactScheduledTaskReadState.Invalid);
            return new(ExactScheduledTaskReadState.Found, new ScheduledTaskSnapshot(new(path, name), state)
            {
                ActionCount = actionCount,
                RuntimeAvailable = runtimeAvailable,
                LastTaskResult = lastResult,
                MissedRuns = missedRuns,
                LastRunLocalTime = lastRun
            });
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or ArgumentException or KeyNotFoundException)
        { return new(ExactScheduledTaskReadState.Invalid); }
    }

    private static void ValidateIdentity(string taskPath, string taskName)
    {
        if (string.IsNullOrWhiteSpace(taskPath) || taskPath.Length > 256 || taskPath[0] != '\\' || taskPath[^1] != '\\' ||
            taskPath.Any(char.IsControl) || string.IsNullOrWhiteSpace(taskName) || taskName.Length > 256 || taskName.Any(char.IsControl))
            throw new ArgumentException("Scheduled task identity is invalid.");
    }

    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
    private static string? Text(JsonElement root, string name, int limit) => root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String && value.GetString() is { } text && text.Length <= limit && !text.Any(char.IsControl) ? text : null;
    private static int? Number(JsonElement root, string name, int minimum, int maximum) => root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number >= minimum && number <= maximum ? number : null;
    private static bool? Boolean(JsonElement root, string name) => root.TryGetProperty(name, out var value) ?
        value.ValueKind == JsonValueKind.True ? true : value.ValueKind == JsonValueKind.False ? false : null : null;
    private static uint? Unsigned(JsonElement root, string name) => root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number && value.TryGetUInt32(out uint number) ? number : null;
    private static DateTime? LocalTime(JsonElement root, string name) => root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String && DateTime.TryParseExact(value.GetString(), "yyyy-MM-dd'T'HH:mm:ss.fffffff",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed) ? parsed : null;
}
