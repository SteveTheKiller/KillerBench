using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class NeighborCacheChecks
{
    internal static async Task RunAsync()
    {
        object Group(int index = 7, string family = "IPv4", string state = "Reachable", long count = 2) =>
            new { interfaceIndex = index, addressFamily = family, state, count };
        ToolResult Evidence(object[] groups, long? entryCount = null, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, entryCount = entryCount ?? groups.Sum(x => (long)x.GetType().GetProperty("count")!.GetValue(x)!), groups }), "", false);

        var clearEvidence = Evidence([Group(), Group(family: "IPv6", state: "Permanent", count: 1)]);
        var clear = NeighborCacheDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            clear.Detail.Contains("2 reachable, 1 permanent", StringComparison.Ordinal),
            "Stable neighbor aggregates remain informational and retain evidence");
        Require(NeighborCacheDiagnostics.Interpret(Evidence([Group(state: "Unreachable", count: 1)])).State == CheckState.Attention,
            "Unreachable neighbors request local-path review");
        Require(NeighborCacheDiagnostics.Interpret(Evidence([Group(state: "Incomplete", count: 1)])).State == CheckState.Attention,
            "Incomplete resolution requests correlation before repair");
        Require(NeighborCacheDiagnostics.Interpret(Evidence([Group(), Group(index: 8, state: "Unreachable", count: 1)])).State == CheckState.Information,
            "An isolated failed entry stays visible without overriding usable dynamic neighbor evidence");
        Require(NeighborCacheDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "An empty complete cache remains informational");

        foreach (var invalid in new[]
        {
            Evidence([Group()], complete: false), Evidence([Group(state: "Maximum")]), Evidence([Group(family: "Other")]),
            Evidence([Group(), Group(count: 1)]), Evidence([Group()], entryCount: 99),
            Evidence([Group(count: 4096), Group(index: 8, count: 1)], entryCount: 4096),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { Error = "denied" },
            clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }
        }) Require(NeighborCacheDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete, invalid, duplicate, inconsistent, excessive, malformed and failed evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await NeighborCacheDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Last().Contains("Get-NetNeighbor", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Remove-NetNeighbor", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-NetNeighbor", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("IPAddress=", StringComparison.Ordinal),
            "Native plan groups bounded neighbor states without retaining identities or changing entries");
        Require((await NeighborCacheDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await NeighborCacheDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: privacy-bounded neighbor states, actionable failures and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
