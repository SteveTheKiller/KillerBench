using System.Text.Json;

namespace KillerBench.Engine.Backup;

public static class ShadowStorageDiagnostics
{
    private const int MaximumAssociations = 64;
    private const string Scope = " Shadow storage allocation is point-in-time evidence and does not establish snapshot, backup or restore success. No association, limit, snapshot, volume, backup data or setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-CimInstance -ClassName Win32_ShadowStorage -ErrorAction Stop|Select-Object -First 65);" +
        "$items=@($all|ForEach-Object{$source='';$storage='';if($_.Volume){$source=[string]$_.Volume.DeviceID};if($_.DiffVolume){$storage=[string]$_.DiffVolume.DeviceID};" +
        "[pscustomobject]@{source=$source;storage=$storage;used=[uint64]$_.UsedSpace;allocated=[uint64]$_.AllocatedSpace;maximum=[uint64]$_.MaxSpace}});" +
        "[pscustomobject]@{complete=($all.Count-le 64);items=$items}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Shadow storage collection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Shadow storage provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Shadow storage provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Shadow storage collection exceeded its overall deadline.",
                new(-1, "", "Shadow storage collection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Shadow storage collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Shadow storage collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() > MaximumAssociations)
                return Unknown("Shadow storage evidence was malformed or exceeded its coverage limit.", evidence);
            if (items.GetArrayLength() == 0)
                return new("VSS shadow storage", CheckState.Information,
                    "No shadow storage association was reported. This does not establish whether every volume supports or needs shadow copies." + Scope, Evidence: evidence);

            int attention = 0, unknown = 0, unbounded = 0;
            ulong usedTotal = 0, allocatedTotal = 0;
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pressure = new List<string>();
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 5) || !Text(item, "source", out string source) || !Text(item, "storage", out string storage) ||
                    !UInt64(item, "used", out ulong used) || !UInt64(item, "allocated", out ulong allocated) ||
                    !UInt64(item, "maximum", out ulong maximum) || !identities.Add(source + "\0" + storage))
                {
                    unknown++;
                    continue;
                }
                usedTotal = Add(usedTotal, used);
                allocatedTotal = Add(allocatedTotal, allocated);
                bool unlimited = maximum == ulong.MaxValue;
                if (unlimited) unbounded++;
                if (allocated < used || !unlimited && (maximum == 0 || used > maximum || allocated > maximum))
                {
                    unknown++;
                    continue;
                }
                if (!unlimited && used >= CeilingPercent(maximum, 90))
                {
                    attention++;
                    if (pressure.Count < 8) pressure.Add(source + " uses " + Percent(used, maximum) + "% of its configured maximum");
                }
            }
            CheckState state = unknown > 0 ? CheckState.Unknown : attention > 0 ? CheckState.Attention : CheckState.Clear;
            string detail = $"{items.GetArrayLength()} association{(items.GetArrayLength() == 1 ? "" : "s")} reported; {attention} at or above 90% of the configured maximum; {unknown} could not be classified; {unbounded} report no finite maximum; {FormatBytes(usedTotal)} used; {FormatBytes(allocatedTotal)} allocated.";
            if (pressure.Count > 0) detail += " Review: " + string.Join("; ", pressure) + ".";
            if (state == CheckState.Attention) detail += " Review retention needs, recent snapshot loss and available capacity before resizing or deleting anything.";
            return new("VSS shadow storage", state, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Shadow storage evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("VSS shadow storage", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static ulong Add(ulong left, ulong right) => left > ulong.MaxValue - right ? ulong.MaxValue : left + right;
    private static ulong CeilingPercent(ulong value, uint percent)
    {
        ulong quotient = value / 100;
        ulong remainder = value % 100;
        return quotient * percent + (remainder * percent + 99) / 100;
    }
    private static uint Percent(ulong used, ulong maximum) => maximum == 0 ? 0 :
        (uint)Math.Min(100d, Math.Floor(used * 100d / maximum));
    private static string FormatBytes(ulong bytes) => bytes >= 1UL << 40
        ? $"{bytes / (double)(1UL << 40):F1} TiB"
        : $"{bytes / (double)(1UL << 30):F1} GiB";
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool UInt64(JsonElement owner, string name, out ulong value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt64(out value);
    }
    private static bool Text(JsonElement owner, string name, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 or > 512 || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
