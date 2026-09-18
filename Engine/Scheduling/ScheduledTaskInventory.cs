using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ScheduledTaskSnapshot(ScheduledTaskIdentity Identity, string State)
{
    public string? UserId { get; init; }
    public string? GroupId { get; init; }
    public int? ActionCount { get; init; }
    public IReadOnlyList<string>? ActionTypes { get; init; }
    public IReadOnlyList<string>? TriggerTypes { get; init; }
    public string? RunLevel { get; init; }
    public uint? LastTaskResult { get; init; }
    public uint? MissedRuns { get; init; }
    public bool? RuntimeAvailable { get; init; }
    public DateTime? LastRunLocalTime { get; init; }
    public string? RuntimeError { get; init; }
}
public sealed record ScheduledTaskInventoryResult(IReadOnlyList<ScheduledTaskSnapshot> Tasks, bool Complete, ToolResult Evidence);

public static class ScheduledTaskInventory
{
    public static async Task<ScheduledTaskInventoryResult> ReadAsync(CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Task inventory requires Windows.");
        var result = await BoundedTool.RunAsync(CreateReadStep(), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static RepairStep CreateReadStep()
    {
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$tasks=@(Get-ScheduledTask -ErrorAction Stop | Select-Object -First 2049 | ForEach-Object { " +
            "$runtime=$null; $runtimeError=$null; try {$runtime=$_ | Get-ScheduledTaskInfo -ErrorAction Stop} catch {$runtimeError=$_.Exception.Message; if($runtimeError.Length -gt 2048){$runtimeError=$runtimeError.Substring(0,2048)}}; " +
            "$lastRun=$null; if($null -ne $runtime -and $null -ne $runtime.LastRunTime){$lastRun=$runtime.LastRunTime.ToString('yyyy-MM-ddTHH:mm:ss.fffffff',[Globalization.CultureInfo]::InvariantCulture)}; " +
            "[pscustomobject]@{path=$_.TaskPath; name=$_.TaskName; state=$_.State.ToString(); runtimeError=$runtimeError; lastRunLocalTime=$lastRun; runtimeAvailable=($null -ne $runtime); lastTaskResult=$runtime.LastTaskResult; missedRuns=$runtime.NumberOfMissedRuns; userId=$_.Principal.UserId; groupId=$_.Principal.GroupId; runLevel=$_.Principal.RunLevel.ToString(); actionCount=@($_.Actions).Count; actionTypes=@($_.Actions | ForEach-Object {$_.CimClass.CimClassName}); triggerTypes=@(if($null -ne $_.Triggers){$_.Triggers | ForEach-Object {$_.CimClass.CimClassName}})} }); " +
            "[pscustomobject]@{tasks=$tasks} | ConvertTo-Json -Depth 3 -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static ScheduledTaskInventoryResult Interpret(ToolResult result)
    {
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return new([], false, result);
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!UniqueFields(json.RootElement)) return new([], false, result);
            var rows = json.RootElement.GetProperty("tasks");
            if (rows.ValueKind != JsonValueKind.Array) return new([], false, result);
            bool complete = rows.GetArrayLength() <= 2048;
            var tasks = new List<ScheduledTaskSnapshot>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray().Take(2048))
            {
                try
                {
                    if (!UniqueFields(row)) { complete = false; continue; }
                    var identity = new ScheduledTaskIdentity(row.GetProperty("path").GetString()!, row.GetProperty("name").GetString()!);
                    if (!seen.Add(identity.FullName)) { ambiguous.Add(identity.FullName); complete = false; continue; }
                    string? state = row.GetProperty("state").GetString();
                    if (state is not ("Unknown" or "Disabled" or "Queued" or "Ready" or "Running"))
                    { complete = false; continue; }
                    string? userId = row.TryGetProperty("userId", out var user) && user.ValueKind == JsonValueKind.String ? user.GetString() : null;
                    string? runLevel = row.TryGetProperty("runLevel", out var level) && level.ValueKind == JsonValueKind.String ? level.GetString() : null;
                    string? groupId = row.TryGetProperty("groupId", out var group) && group.ValueKind == JsonValueKind.String ? group.GetString() : null;
                    if (string.IsNullOrWhiteSpace(groupId) || groupId.Length > 512 || groupId.Any(char.IsControl)) groupId = null;
                    if (string.IsNullOrWhiteSpace(userId) || userId.Length > 512 || userId.Any(char.IsControl)) userId = null;
                    if (runLevel is not ("Limited" or "Highest")) runLevel = null;
                    int? actionCount = row.TryGetProperty("actionCount", out var actions) && actions.ValueKind == JsonValueKind.Number &&
                        actions.TryGetInt32(out int count) && count is >= 0 and <= 32 ? count : null;
                    IReadOnlyList<string>? actionTypes = null;
                    if (row.TryGetProperty("actionTypes", out var types) && types.ValueKind == JsonValueKind.Array && types.GetArrayLength() == actionCount)
                    {
                        var names = types.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null).ToArray();
                        if (names.All(value => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && !value.Any(char.IsControl)))
                            actionTypes = Array.AsReadOnly(names.Select(value => value!).ToArray());
                    }
                    IReadOnlyList<string>? triggerTypes = null;
                    if (row.TryGetProperty("triggerTypes", out var triggers) && triggers.ValueKind == JsonValueKind.Array && triggers.GetArrayLength() <= 48)
                    {
                        var names = triggers.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : null).ToArray();
                        if (names.All(value => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && !value.Any(char.IsControl)))
                            triggerTypes = Array.AsReadOnly(names.Select(value => value!).ToArray());
                    }
                    uint? lastResult = ReadUnsigned(row, "lastTaskResult");
                    uint? missedRuns = ReadUnsigned(row, "missedRuns");
                    string? runtimeError = row.TryGetProperty("runtimeError", out var error) && error.ValueKind == JsonValueKind.String
                        ? error.GetString() : null;
                    if (runtimeError?.Length > 2048) runtimeError = runtimeError[..2048];
                    bool? runtimeAvailable = row.TryGetProperty("runtimeAvailable", out var available)
                        ? available.ValueKind == JsonValueKind.True && lastResult is not null && missedRuns is not null && string.IsNullOrWhiteSpace(runtimeError) : null;
                    tasks.Add(new(identity, state) { UserId = userId, GroupId = groupId, RunLevel = runLevel, ActionCount = actionCount, ActionTypes = actionTypes, TriggerTypes = triggerTypes,
                        LastTaskResult = lastResult, MissedRuns = missedRuns, RuntimeAvailable = runtimeAvailable, RuntimeError = runtimeError,
                        LastRunLocalTime = row.TryGetProperty("lastRunLocalTime", out var time) && time.ValueKind == JsonValueKind.String &&
                            DateTime.TryParseExact(time.GetString(), "yyyy-MM-dd'T'HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var parsedTime) ? parsedTime : null });
                }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException) { complete = false; }
            }
            return new(Array.AsReadOnly(tasks.Where(task => !ambiguous.Contains(task.Identity.FullName)).ToArray()), complete, result);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException) { return new([], false, result); }
    }

    private static bool UniqueFields(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();

    private static uint? ReadUnsigned(JsonElement row, string name) => row.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number && value.TryGetUInt32(out uint number) ? number : null;
}
