using System.Text.Json;

namespace KillerBench.Engine;

public static class DnsClientCacheDiagnostics
{
    private const int MaximumEntries = 4096;
    private const int MaximumGroups = 4096;
    private const string Scope = " DNS names, record data and provider instance identifiers are excluded. Negative entries can be ordinary cached lookup results and do not authorize a flush. No cache entry, resolver, adapter, route, service, Hosts file, policy or network setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-DnsClientCache -ErrorAction Stop|Select-Object -First 4097);" +
        "$rows=@($all|Select-Object -First 4096|Group-Object Status,Type|ForEach-Object{$x=$_.Group[0];" +
        "[pscustomobject]@{status=[uint64]$x.Status;type=[uint64]$x.Type;count=[uint64]$_.Count}});" +
        "[pscustomobject]@{complete=($all.Count-le 4096);entryCount=[uint64]([Math]::Min($all.Count,4096));groups=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DNS client cache inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("DNS client cache provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("DNS client cache provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("DNS client cache inspection exceeded its overall deadline.",
                new(-1, "", "DNS client cache inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("DNS client cache inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("DNS client cache collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Integer(root, "entryCount", 0, MaximumEntries, out long entryCount) ||
                !root.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array ||
                groups.GetArrayLength() > MaximumGroups)
                return Unknown("DNS client cache evidence was malformed, incomplete or exceeded its bound.", evidence);

            var identities = new HashSet<string>(StringComparer.Ordinal);
            long total = 0, successful = 0, notExist = 0, noRecords = 0, other = 0;
            foreach (var item in groups.EnumerateArray())
            {
                if (!Unique(item, 3) || !Integer(item, "status", 0, uint.MaxValue, out long status) ||
                    !Integer(item, "type", 0, ushort.MaxValue, out long type) ||
                    !Integer(item, "count", 1, MaximumEntries, out long count) ||
                    !identities.Add(status + "\0" + type))
                    return Unknown("DNS client cache evidence contained an invalid or duplicate aggregate.", evidence);
                total += count;
                if (total > MaximumEntries)
                    return Unknown("DNS client cache aggregate counts exceeded their evidence bound.", evidence);
                switch (status)
                {
                    case 0: successful += count; break;
                    case 9003: notExist += count; break;
                    case 9501:
                    case 9701: noRecords += count; break;
                    default: other += count; break;
                }
            }
            if (total != entryCount)
                return Unknown("DNS client cache aggregate counts did not match the reported entry count.", evidence);

            string detail = $"Reviewed {entryCount} DNS client cache entries across {groups.GetArrayLength()} status and record-type groups. {successful} successful, {notExist} name-not-found, {noRecords} no-records and {other} with another provider status.";
            if (notExist + noRecords > 0)
                detail += " Negative entries remain informational until a fresh lookup for one exact hostname also fails with complete evidence.";
            if (other > 0)
                return Unknown(detail + " One or more provider status values are not recognized by this diagnostic.", evidence);
            return new("DNS client cache summary", CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("DNS client cache evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("DNS client cache summary", CheckState.Unknown, detail + Scope, Evidence: evidence);
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
}
