using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DeliveryOptimizationActivityChecks
{
    internal static async Task RunAsync()
    {
        object Group(string status = "Downloading", ulong count = 1, ulong fileBytes = 1000,
            ulong downloadedBytes = 400, ulong peerBytes = 100, ulong httpBytes = 300, ulong cacheServerBytes = 50) =>
            new { status, count, fileBytes, downloadedBytes, peerBytes, httpBytes, cacheServerBytes };
        ToolResult Evidence(object[] groups, ulong? entryCount = null, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new
            {
                complete,
                entryCount = entryCount ?? groups.Aggregate(0UL, (sum, item) => sum + (ulong)item.GetType().GetProperty("count")!.GetValue(item)!),
                groups
            }), "", false);

        var liveEvidence = Evidence([Group(count: 2), Group("Caching", fileBytes: 500, downloadedBytes: 500, peerBytes: 0, httpBytes: 500, cacheServerBytes: 0)]);
        var live = DeliveryOptimizationActivityDiagnostics.Interpret(liveEvidence);
        Require(live is { State: CheckState.Information, Repair: null } && ReferenceEquals(live.Evidence, liveEvidence) &&
            live.Detail.Contains("2 downloading, 0 complete, 1 caching and 0 paused", StringComparison.Ordinal) &&
            live.Detail.Contains("1500 bytes", StringComparison.Ordinal),
            "Current Delivery Optimization activity stays informational and retains aggregate evidence");
        var paused = DeliveryOptimizationActivityDiagnostics.Interpret(Evidence([Group("Paused")]));
        Require(paused is { State: CheckState.Information, Repair: null } && paused.Detail.Contains("paused by its caller", StringComparison.Ordinal),
            "Paused transfer activity remains context and cannot select a repair");
        Require(DeliveryOptimizationActivityDiagnostics.Interpret(Evidence([])) is { State: CheckState.Information, Repair: null },
            "An empty complete activity snapshot remains informational");

        foreach (var invalid in new[]
        {
            Evidence([Group()], complete: false), Evidence([Group("Failed")]),
            Evidence([Group(), Group(count: 2)]), Evidence([Group()], entryCount: 2),
            Evidence([Group(count: 4096), Group("Caching")], entryCount: 4096),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            liveEvidence with { ExitCode = 5 }, liveEvidence with { Error = "denied" },
            liveEvidence with { TimedOut = true }, liveEvidence with { Truncated = true }
        }) Require(DeliveryOptimizationActivityDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
            "Incomplete, invalid, duplicate, inconsistent, excessive, malformed and failed activity evidence remains unknown");
        var overflow = Evidence([Group(fileBytes: ulong.MaxValue), Group("Complete", fileBytes: 1)]);
        Require(DeliveryOptimizationActivityDiagnostics.Interpret(overflow).State == CheckState.Unknown,
            "Overflowing activity aggregates remain unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await DeliveryOptimizationActivityDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(liveEvidence);
        });
        string script = planned?.Arguments.LastOrDefault() ?? "";
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            script.Contains("Get-DeliveryOptimizationStatus -AsObject", StringComparison.Ordinal) &&
            !script.Contains("SourceURL", StringComparison.OrdinalIgnoreCase) && !script.Contains("FileId", StringComparison.OrdinalIgnoreCase) &&
            !script.Contains("CacheHost", StringComparison.OrdinalIgnoreCase) && !script.Contains("PeerInfo", StringComparison.OrdinalIgnoreCase) &&
            !script.Contains("Set-DeliveryOptimization", StringComparison.OrdinalIgnoreCase) && !script.Contains("Delete-DeliveryOptimization", StringComparison.OrdinalIgnoreCase),
            "Native plan aggregates bounded live activity without retaining transfer identities or changing Delivery Optimization");
        Require((await DeliveryOptimizationActivityDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Activity provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DeliveryOptimizationActivityDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents activity collection");
        Console.WriteLine("PASS: privacy-bounded Delivery Optimization activity aggregates and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
