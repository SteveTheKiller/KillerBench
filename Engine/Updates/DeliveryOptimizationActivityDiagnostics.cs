using System.Text.Json;

namespace KillerBench.Engine;

public static class DeliveryOptimizationActivityDiagnostics
{
    private const int MaximumEntries = 4096;
    private const string Scope = " File IDs, source URLs, cache hosts, peer addresses and caller identities are excluded. A current snapshot does not prove eventual download, installation or application success. No transfer, cache, service, peer, bandwidth, source or policy setting changed.";
    private const string Reference = " Microsoft documents Get-DeliveryOptimizationStatus as a real-time snapshot of current Delivery Optimization jobs: https://learn.microsoft.com/windows/deployment/do/waas-delivery-optimization-monitor";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-DeliveryOptimizationStatus -AsObject -ErrorAction Stop|Select-Object -First 4097);$limited=@($all|Select-Object -First 4096);" +
        "$rows=@($limited|Group-Object Status|ForEach-Object{$file=[decimal]0;$downloaded=[decimal]0;$peer=[decimal]0;$http=[decimal]0;$cache=[decimal]0;" +
        "foreach($item in $_.Group){if($null-eq$item.FileSize-or$null-eq$item.TotalBytesDownloaded-or$null-eq$item.BytesFromPeers-or$null-eq$item.BytesFromHTTP-or$null-eq$item.BytesFromCacheServer){throw 'Delivery Optimization returned an incomplete transfer record'};" +
        "$file+=[uint64]$item.FileSize;$downloaded+=[uint64]$item.TotalBytesDownloaded;$peer+=[uint64]$item.BytesFromPeers;$http+=[uint64]$item.BytesFromHTTP;$cache+=[uint64]$item.BytesFromCacheServer};" +
        "if($file-gt[uint64]::MaxValue-or$downloaded-gt[uint64]::MaxValue-or$peer-gt[uint64]::MaxValue-or$http-gt[uint64]::MaxValue-or$cache-gt[uint64]::MaxValue){throw 'Delivery Optimization aggregate exceeded UInt64'};" +
        "[pscustomobject]@{status=[string]$_.Name;count=[uint64]$_.Count;fileBytes=[uint64]$file;downloadedBytes=[uint64]$downloaded;peerBytes=[uint64]$peer;httpBytes=[uint64]$http;cacheServerBytes=[uint64]$cache}});" +
        "[pscustomobject]@{complete=($all.Count-le 4096);entryCount=[uint64]$limited.Count;groups=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Delivery Optimization activity inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Delivery Optimization activity provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Delivery Optimization activity provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Delivery Optimization activity inspection exceeded its overall deadline.",
                new(-1, "", "Delivery Optimization activity inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Delivery Optimization activity inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Delivery Optimization activity collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Unsigned(root, "entryCount", MaximumEntries, out ulong entryCount) ||
                !root.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array || groups.GetArrayLength() > 4)
                return Unknown("Delivery Optimization activity evidence was malformed, incomplete or exceeded its bound.", evidence);

            var counts = new Dictionary<string, ulong>(StringComparer.Ordinal);
            ulong totalCount = 0;
            ulong totalFileBytes = 0;
            ulong totalDownloadedBytes = 0;
            ulong totalPeerBytes = 0;
            ulong totalHttpBytes = 0;
            ulong totalCacheServerBytes = 0;
            foreach (var item in groups.EnumerateArray())
            {
                if (!Unique(item, 7) || !Text(item, "status", 16, out string status) ||
                    status is not ("Downloading" or "Complete" or "Caching" or "Paused") ||
                    !Unsigned(item, "count", MaximumEntries, out ulong count) || count == 0 || !counts.TryAdd(status, count) ||
                    !Unsigned(item, "fileBytes", ulong.MaxValue, out ulong fileBytes) ||
                    !Unsigned(item, "downloadedBytes", ulong.MaxValue, out ulong downloadedBytes) ||
                    !Unsigned(item, "peerBytes", ulong.MaxValue, out ulong peerBytes) ||
                    !Unsigned(item, "httpBytes", ulong.MaxValue, out ulong httpBytes) ||
                    !Unsigned(item, "cacheServerBytes", ulong.MaxValue, out ulong cacheServerBytes))
                    return Unknown("Delivery Optimization activity evidence contained an invalid or duplicate aggregate.", evidence);
                checked
                {
                    totalCount += count;
                    totalFileBytes += fileBytes;
                    totalDownloadedBytes += downloadedBytes;
                    totalPeerBytes += peerBytes;
                    totalHttpBytes += httpBytes;
                    totalCacheServerBytes += cacheServerBytes;
                }
                if (totalCount > MaximumEntries)
                    return Unknown("Delivery Optimization activity aggregate counts exceeded their evidence bound.", evidence);
            }
            if (totalCount != entryCount)
                return Unknown("Delivery Optimization activity aggregate counts did not match the reported entry count.", evidence);

            ulong downloading = Count(counts, "Downloading");
            ulong completeJobs = Count(counts, "Complete");
            ulong caching = Count(counts, "Caching");
            ulong paused = Count(counts, "Paused");
            string detail = entryCount == 0
                ? "No current Delivery Optimization jobs were reported. This does not establish update health or historical transfer success."
                : $"Reviewed {entryCount} current Delivery Optimization jobs: {downloading} downloading, {completeJobs} complete, {caching} caching and {paused} paused. Aggregate file size is {totalFileBytes} bytes; {totalDownloadedBytes} bytes downloaded, including {totalPeerBytes} from peers, {totalHttpBytes} from HTTP sources and {totalCacheServerBytes} from a Connected Cache server.";
            if (downloading > 0) detail += " Active downloads are in progress under the current caller and policy configuration.";
            if (paused > 0) detail += " A paused job was paused by its caller; this snapshot alone does not establish a fault or authorize a service or cache action.";
            return new("Delivery Optimization activity", CheckState.Information, detail + Scope + Reference, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Delivery Optimization activity evidence could not be interpreted.", evidence);
        }
    }

    private static ulong Count(IReadOnlyDictionary<string, ulong> counts, string status) =>
        counts.TryGetValue(status, out ulong count) ? count : 0;
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Delivery Optimization activity", CheckState.Unknown, detail + Scope + Reference, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Unsigned(JsonElement owner, string name, ulong maximum, out ulong value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetUInt64(out value) && value <= maximum;
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
