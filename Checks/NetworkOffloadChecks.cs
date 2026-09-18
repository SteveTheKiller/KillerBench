using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class NetworkOffloadChecks
{
    internal static async Task RunAsync()
    {
        object Row(int rss = 1, int rsc = 1, int chimney = 0, int task = 1, int direct = 1, int coalescing = 1, int subnets = 0) => new
        {
            receiveSideScaling = rss,
            receiveSegmentCoalescing = rsc,
            chimney,
            taskOffload = task,
            networkDirect = direct,
            packetCoalescingFilter = coalescing,
            networkDirectAcrossIpSubnets = subnets
        };
        ToolResult Evidence(object[] settings, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, settings }), "", false);

        var evidence = Evidence([Row()]);
        var result = NetworkOffloadDiagnostics.Interpret(evidence);
        Require(result is { State: CheckState.Information, Repair: null } && ReferenceEquals(result.Evidence, evidence) &&
            result.Detail.Contains("RSS 1; RSC 1; Chimney 0", StringComparison.Ordinal) &&
            result.Detail.Contains("Numeric values are retained exactly", StringComparison.Ordinal) &&
            result.Detail.Contains("Get-NetOffloadGlobalSetting", StringComparison.Ordinal) &&
            result.Detail.Contains("No offload, adapter, TCP template", StringComparison.Ordinal),
            "Valid global offload evidence remains configuration context without selecting a change");

        foreach (var invalid in new[]
        {
            Evidence([], true), Evidence([Row()], false), Evidence([Row(), Row()]), Evidence([Row(rss: 256)]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            evidence with { ExitCode = 5 }, evidence with { Error = "denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true }
        })
            Require(NetworkOffloadDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Missing, ambiguous, incomplete, unsupported, malformed and failed offload evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await NetworkOffloadDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(evidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") &&
            planned.Arguments.Last().Contains("Get-NetOffloadGlobalSetting", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetOffloadGlobalSetting", StringComparison.Ordinal),
            "Native plan reads global offload settings without changing them");
        Require((await NetworkOffloadDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Offload provider failure remains unknown");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await NetworkOffloadDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents offload collection");
        Console.WriteLine("PASS: global network offload values, bounds, failures and read-only collection; fixtures only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
