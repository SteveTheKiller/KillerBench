using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class StorageJobDiagnostics
{
    private const int MaximumJobs = 256;
    private const int MaximumGroups = 256;
    private static readonly HashSet<string> KnownStates = new([
        "New", "Starting", "Running", "Suspended", "ShuttingDown", "Completed", "Terminated", "Killed", "Exception", "Service", "QueryPending"
    ], StringComparer.Ordinal);
    private const string Scope = " Storage object identities, unique job identifiers, paths and provider-specific payloads are excluded. A completed snapshot does not prove the underlying storage result succeeded. No job was stopped, started, resumed, removed or changed, and no disk, pool, virtual disk, volume, filesystem or storage setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-StorageJob -ErrorAction Stop|Select-Object -First 257);" +
        "$rows=@($all|Select-Object -First 256|Group-Object Name,JobState,IsBackgroundTask|ForEach-Object{" +
        "$x=$_.Group[0];$percents=@($_.Group|ForEach-Object{if($null-eq$_.PercentComplete){0}else{[uint64]$_.PercentComplete}});" +
        "$elapsed=@($_.Group|ForEach-Object{if($null-eq$_.ElapsedTime){0}else{[uint64][Math]::Floor($_.ElapsedTime.TotalSeconds)}});" +
        "[pscustomobject]@{name=[string]$x.Name;state=[string]$x.JobState;background=[bool]$x.IsBackgroundTask;count=[uint64]$_.Count;" +
        "maximumPercent=[uint64](($percents|Measure-Object -Maximum).Maximum);maximumElapsedSeconds=[uint64](($elapsed|Measure-Object -Maximum).Maximum)}});" +
        "[pscustomobject]@{complete=($all.Count-le 256);jobCount=[uint64]([Math]::Min($all.Count,256));groups=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Storage job inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Storage job provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Storage job provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Storage job inspection exceeded its overall deadline.",
                new(-1, "", "Storage job inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Storage job inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Storage job collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Integer(root, "jobCount", 0, MaximumJobs, out long jobCount) ||
                !root.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array ||
                groups.GetArrayLength() > MaximumGroups)
                return Unknown("Storage job evidence was malformed, incomplete or exceeded its bound.", evidence);

            var identities = new HashSet<string>(StringComparer.Ordinal);
            long total = 0, active = 0, suspended = 0, failed = 0, completed = 0;
            var summaries = new List<string>();
            foreach (var item in groups.EnumerateArray())
            {
                if (!Unique(item, 6) || !Text(item, "name", 128, out string name) ||
                    !Text(item, "state", 32, out string state) || !KnownStates.Contains(state) ||
                    !Boolean(item, "background", out bool background) ||
                    !Integer(item, "count", 1, MaximumJobs, out long count) ||
                    !Integer(item, "maximumPercent", 0, 100, out long percent) ||
                    !Integer(item, "maximumElapsedSeconds", 0, uint.MaxValue, out long elapsed) ||
                    !identities.Add(name + "\0" + state + "\0" + background))
                    return Unknown("Storage job evidence contained an invalid or duplicate aggregate.", evidence);
                total += count;
                if (total > MaximumJobs)
                    return Unknown("Storage job aggregate counts exceeded their evidence bound.", evidence);
                if (state is "New" or "Starting" or "Running" or "Service" or "QueryPending") active += count;
                else if (state is "Suspended" or "ShuttingDown") suspended += count;
                else if (state is "Terminated" or "Killed" or "Exception") failed += count;
                else if (state == "Completed") completed += count;
                summaries.Add($"{name}: {state}, {percent}% maximum progress, {elapsed}s maximum elapsed, {(background ? "background" : "foreground")}, {count} job{(count == 1 ? "" : "s")}");
            }
            if (total != jobCount)
                return Unknown("Storage job aggregate counts did not match the reported job count.", evidence);
            if (jobCount == 0)
                return new("Storage jobs", CheckState.Information, "Windows reported no current Storage module jobs." + Scope, Evidence: evidence);

            string detail = $"Reviewed {jobCount} current Storage module job{(jobCount == 1 ? "" : "s")}: {active} active, {suspended} suspended or shutting down, {failed} failed or terminated and {completed} completed. " +
                string.Join("; ", summaries) + ".";
            if (failed > 0)
                detail += " Failed or terminated jobs require correlation with current disk, pool, virtual-disk, volume and event evidence before selecting any repair.";
            if (suspended > 0)
                detail += " Suspended or shutting-down work requires current provider and maintenance-state review before intervention.";
            return new("Storage jobs", failed + suspended > 0 ? CheckState.Attention : CheckState.Information,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Storage job evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Storage jobs", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, long minimum, long maximum, out long value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt64(out value) && value >= minimum && value <= maximum;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String ||
            item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
