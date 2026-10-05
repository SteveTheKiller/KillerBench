using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class DiskQuotaDiagnostics
{
    private const int MaximumSettings = 64;
    private const string Scope = " Disk quotas are per-volume, per-user controls. User identities and individual usage are excluded. This snapshot does not establish that a user reached a warning or limit. No quota, notification, volume, file, registry value or policy changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-CimInstance Win32_QuotaSetting -ErrorAction Stop|Sort-Object VolumePath|Select-Object -First 65);$rows=@();$unresolved=0;" +
        "foreach($x in @($all|Select-Object -First 64)){$v=[string]$x.VolumePath;if($v-notmatch '^[A-Za-z]:\\\\$'){$unresolved++;continue};" +
        "$rows+=[pscustomobject]@{drive=$v.Substring(0,2).ToUpperInvariant();state=[uint32]$x.State;defaultLimitBytes=[int64]$x.DefaultLimit;" +
        "defaultWarningBytes=[int64]$x.DefaultWarningLimit;limitNotification=[bool]$x.ExceededNotification;warningNotification=[bool]$x.WarningExceededNotification}};" +
        "[pscustomobject]@{complete=($all.Count-le 64);unresolved=[uint32]$unresolved;settings=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Disk quota inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Disk quota provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Disk quota provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Disk quota inspection exceeded its overall deadline.",
                new(-1, "", "Disk quota inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Disk quota inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Disk quota collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Integer(root, "unresolved", 0, MaximumSettings, out long unresolved) || unresolved != 0 ||
                !root.TryGetProperty("settings", out var settings) || settings.ValueKind != JsonValueKind.Array ||
                settings.GetArrayLength() > MaximumSettings)
                return Unknown("Disk quota evidence was malformed, incomplete or included an unresolved volume identity.", evidence);

            var drives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var summaries = new List<string>();
            foreach (var item in settings.EnumerateArray())
            {
                if (!Unique(item, 6) || !Text(item, "drive", out string drive) ||
                    drive.Length != 2 || !char.IsAsciiLetterUpper(drive[0]) || drive[1] != ':' ||
                    !Integer(item, "state", 0, 2, out long state) ||
                    !Integer(item, "defaultLimitBytes", -1, long.MaxValue, out long limit) ||
                    !Integer(item, "defaultWarningBytes", -1, long.MaxValue, out long warning) ||
                    !Boolean(item, "limitNotification", out bool limitNotification) ||
                    !Boolean(item, "warningNotification", out bool warningNotification) ||
                    warning < -1 || limit < -1 || limit >= 0 && warning > limit || !drives.Add(drive))
                    return Unknown("Disk quota evidence contained invalid, contradictory or duplicate volume settings.", evidence);
                string stateName = state switch { 0 => "disabled", 1 => "tracked", _ => "enforced" };
                string limitText = limit < 0 ? "unlimited" : FormatBytes(limit);
                string warningText = warning < 0 ? "unlimited" : FormatBytes(warning);
                summaries.Add($"{drive} {stateName}, default limit {limitText}, warning {warningText}, limit events {(limitNotification ? "on" : "off")}, warning events {(warningNotification ? "on" : "off")}");
            }
            if (summaries.Count == 0)
                return new("Disk quotas", CheckState.Information, "Windows reported no drive-letter disk quota settings." + Scope, Evidence: evidence);
            return new("Disk quotas", CheckState.Information,
                $"Reviewed {summaries.Count} drive-letter disk quota setting{(summaries.Count == 1 ? "" : "s")}: " + string.Join("; ", summaries) + "." + Scope,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Disk quota evidence could not be interpreted.", evidence);
        }
    }

    private static string FormatBytes(long value) => value >= 1L << 30
        ? $"{value / (double)(1L << 30):0.##} GiB"
        : value >= 1L << 20 ? $"{value / (double)(1L << 20):0.##} MiB" : $"{value} bytes";
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Disk quotas", CheckState.Unknown, detail + Scope, Evidence: evidence);
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
    private static bool Text(JsonElement owner, string name, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 or > 8 || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
