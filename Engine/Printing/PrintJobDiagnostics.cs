using System.Text.Json;

namespace KillerBench.Engine.Printing;

public sealed record PrintJobSnapshot(string QueueName, string Name, uint JobId, uint StatusMask);
public sealed record PrintJobInventory(bool Complete, IReadOnlyList<PrintJobSnapshot> Jobs);

public static class PrintJobDiagnostics
{
    private const string Scope = " Snapshot only; job completion and physical printing are unverified. No jobs changed.";
    private static readonly (uint Bit, string Label)[] States =
    [(1, "Paused"), (2, "Error"), (4, "Deleting"), (8, "Spooling"), (16, "Printing"),
     (32, "Offline"), (64, "Paper out"), (128, "Printed"), (256, "Deleted"),
     (512, "Blocked"), (1024, "User intervention"), (2048, "Restart")];

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        TimeSpan? timeout = null, CancellationToken token = default)
        => Interpret(await CaptureAsync(execute, timeout, token: token).ConfigureAwait(false));

    public static async Task<ToolResult> CaptureAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        TimeSpan? timeout = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(40);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromSeconds(40)) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Print job collection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$jobs=@(Get-CimInstance Win32_PrintJob -ErrorAction Stop | Select-Object -First 257 Name,JobId,StatusMask); " +
            "[pscustomobject]@{jobs=$jobs} | ConvertTo-Json -Depth 4 -Compress";
        execute ??= (step, ct) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: ct);
        try
        {
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), deadline.Token)
                ?? throw new InvalidOperationException("Print job provider returned no task.");
            var result = await pending.WaitAsync(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            return result;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return new(-1, "", "Print job collection deadline expired.", false) { TimedOut = true }; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return new(-1, "", error.Message, false); }
    }

    public static PrintJobInventory? ReadInventory(ToolResult? result)
    {
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return null;
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("jobs", out var jobs) || jobs.ValueKind != JsonValueKind.Array || jobs.GetArrayLength() > 257) return null;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var snapshots = new List<PrintJobSnapshot>();
            foreach (var job in jobs.EnumerateArray().Take(256))
            {
                if (job.ValueKind != JsonValueKind.Object || job.EnumerateObject().Count() != 3 ||
                    job.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3 ||
                    !job.TryGetProperty("Name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    nameValue.GetString() is not { Length: > 0 and <= 512 } name || name.Any(char.IsControl) || !seen.Add(name) ||
                    !job.TryGetProperty("JobId", out var idValue) || !idValue.TryGetUInt32(out uint id) ||
                    !job.TryGetProperty("StatusMask", out var maskValue) || !maskValue.TryGetUInt32(out uint mask) ||
                    (mask & ~4095u) != 0 || !TryReadQueueName(name, id, out string queueName)) return null;
                snapshots.Add(new(queueName, name, id, mask));
            }
            return new(jobs.GetArrayLength() < 257, snapshots.AsReadOnly());
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return null; }
    }

    private static bool TryReadQueueName(string name, uint id, out string queueName)
    {
        queueName = "";
        int split = name.LastIndexOf(", ", StringComparison.Ordinal);
        if (split <= 0 || !uint.TryParse(name.AsSpan(split + 2), out uint suffix) || suffix != id) return false;
        string candidate = name[..split];
        if (!DriverReference.ValidText(candidate, 512)) return false;
        queueName = candidate;
        return true;
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        WorkflowCheck Unknown(string detail) => new("Print jobs", CheckState.Unknown, detail + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Collection failed or was incomplete.")];
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("jobs", out var jobs) || jobs.ValueKind != JsonValueKind.Array || jobs.GetArrayLength() > 257)
                return [Unknown("Job inventory envelope was invalid.")];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var checks = new List<WorkflowCheck>();
            foreach (var job in jobs.EnumerateArray().Take(256))
            {
                if (job.ValueKind != JsonValueKind.Object || job.EnumerateObject().Count() != 3 ||
                    job.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3 ||
                    !job.TryGetProperty("Name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    nameValue.GetString() is not { Length: > 0 and <= 512 } name || name.Any(char.IsControl) || !seen.Add(name) ||
                    !job.TryGetProperty("JobId", out var idValue) || !idValue.TryGetUInt32(out uint id) ||
                    !job.TryGetProperty("StatusMask", out var maskValue) || !maskValue.TryGetUInt32(out uint mask))
                    return [Unknown("Job identities or statuses were missing, duplicated or malformed.")];
                var labels = States.Where(s => (mask & s.Bit) != 0).Select(s => s.Label);
                bool attention = (mask & (1 | 2 | 32 | 64 | 512 | 1024)) != 0;
                var state = mask == 0 || (mask & ~4095u) != 0 ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information;
                string next = (mask & 64) != 0 ? "Check the device's requested paper and tray selection." :
                    (mask & (32 | 1024)) != 0 ? "Inspect the device and its connection before retrying." :
                    (mask & 1) != 0 ? "Confirm why the job was paused before resuming it." :
                    attention ? "Review this job and preceding queue entries; preserve queued documents." :
                    "Compare a later observation before deciding whether progress has stalled.";
                checks.Add(new("Print job: " + name, state,
                    $"Job {id}; status mask {mask}: " + string.Join(", ", labels) + ". " + next + Scope, Evidence: result));
            }
            if (jobs.GetArrayLength() == 257) checks.Add(Unknown("The 256-job limit was reached; additional jobs were omitted."));
            if (checks.Count == 0) checks.Add(new("Print jobs", CheckState.Information, "No jobs were returned for this account's visible scope." + Scope, Evidence: result));
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        { return [Unknown("Job inventory could not be interpreted.")]; }
    }
}
