using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class NeighborCacheDiagnostics
{
    private const int MaximumEntries = 4096;
    private const int MaximumGroups = 4096;
    private const string Scope = " Neighbor addresses and link-layer identifiers are excluded. Incomplete can be transient while address resolution is in progress. No neighbor, ARP, interface, route, address or network setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-NetNeighbor -ErrorAction Stop|Select-Object -First 4097);$rows=@($all|Select-Object -First 4096|Group-Object InterfaceIndex,AddressFamily,State|ForEach-Object{" +
        "$x=$_.Group[0];[pscustomobject]@{interfaceIndex=[uint64]$x.InterfaceIndex;addressFamily=[string]$x.AddressFamily;state=[string]$x.State;count=[uint64]$_.Count}});" +
        "[pscustomobject]@{complete=($all.Count-le 4096);entryCount=[uint64]([Math]::Min($all.Count,4096));groups=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Neighbor cache inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Neighbor cache provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Neighbor cache provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Neighbor cache inspection exceeded its overall deadline.",
                new(-1, "", "Neighbor cache inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Neighbor cache inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Neighbor cache collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Integer(root, "entryCount", 0, MaximumEntries, out long entryCount) ||
                !root.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array ||
                groups.GetArrayLength() > MaximumGroups)
                return Unknown("Neighbor cache evidence was malformed, incomplete or exceeded its bound.", evidence);

            var counts = new Dictionary<string, long>(StringComparer.Ordinal);
            long total = 0;
            foreach (var item in groups.EnumerateArray())
            {
                if (!Unique(item, 4) || !Integer(item, "interfaceIndex", 1, uint.MaxValue, out long index) ||
                    !Text(item, "addressFamily", 4, out string family) || family is not ("IPv4" or "IPv6") ||
                    !Text(item, "state", 16, out string state) || state is not ("Unreachable" or "Incomplete" or "Probe" or "Delay" or "Stale" or "Reachable" or "Permanent") ||
                    !Integer(item, "count", 1, MaximumEntries, out long count) || !counts.TryAdd(index + "\0" + family + "\0" + state, count))
                    return Unknown("Neighbor cache evidence contained an invalid or duplicate aggregate.", evidence);
                total += count;
                if (total > MaximumEntries) return Unknown("Neighbor cache aggregate counts exceeded their evidence bound.", evidence);
            }
            if (total != entryCount)
                return Unknown("Neighbor cache aggregate counts did not match the reported entry count.", evidence);

            long unreachable = CountState(counts, "Unreachable");
            long incomplete = CountState(counts, "Incomplete");
            long reachable = CountState(counts, "Reachable");
            long permanent = CountState(counts, "Permanent");
            long usableDynamic = reachable + CountState(counts, "Stale") + CountState(counts, "Probe") + CountState(counts, "Delay");
            bool noUsableDynamicNeighbor = unreachable + incomplete > 0 && usableDynamic == 0;
            string detail = $"Reviewed {entryCount} neighbor cache entries across {groups.GetArrayLength()} interface, address-family and state groups. {reachable} reachable, {permanent} permanent, {incomplete} incomplete and {unreachable} unreachable.";
            if (unreachable > 0 || incomplete > 0)
                detail += " Unreachable entries indicate failed on-link resolution. Incomplete entries need correlation with repeated traffic, adapter state and gateway evidence before selecting a repair.";
            if (noUsableDynamicNeighbor)
                detail += " No reachable, stale, probe or delay entry shows a usable dynamic neighbor path in this snapshot.";
            return new("Neighbor cache state", noUsableDynamicNeighbor ? CheckState.Attention : CheckState.Information,
                detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Neighbor cache evidence could not be interpreted.", evidence);
        }
    }

    private static long CountState(IReadOnlyDictionary<string, long> counts, string state) =>
        counts.Where(item => item.Key.EndsWith("\0" + state, StringComparison.Ordinal)).Sum(item => item.Value);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Neighbor cache state", CheckState.Unknown, detail + Scope, Evidence: evidence);
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
