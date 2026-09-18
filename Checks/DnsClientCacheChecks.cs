using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DnsClientCacheChecks
{
    internal static async Task RunAsync()
    {
        object Group(long status = 0, long type = 1, long count = 2) => new { status, type, count };
        ToolResult Evidence(object[] groups, long? entryCount = null, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new
            {
                complete,
                entryCount = entryCount ?? groups.Sum(group => (long)group.GetType().GetProperty("count")!.GetValue(group)!),
                groups
            }), "", false);

        var ordinaryEvidence = Evidence([Group(), Group(type: 28, count: 1), Group(status: 9003, type: 6, count: 1),
            Group(status: 9501, type: 15, count: 1), Group(status: 9701, type: 33, count: 1)]);
        var ordinary = DnsClientCacheDiagnostics.Interpret(ordinaryEvidence);
        Require(ordinary is { State: CheckState.Information, Repair: null } && ReferenceEquals(ordinary.Evidence, ordinaryEvidence) &&
            ordinary.Detail.Contains("3 successful, 1 name-not-found, 2 no-records", StringComparison.Ordinal) &&
            ordinary.Detail.Contains("fresh lookup for one exact hostname", StringComparison.Ordinal),
            "Successful and negative DNS cache aggregates remain informational without selecting a flush");
        Require(DnsClientCacheDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "An empty complete DNS cache remains informational");
        Require(DnsClientCacheDiagnostics.Interpret(Evidence([Group(status: 12345)])).State == CheckState.Unknown,
            "An unknown provider status remains incomplete rather than being guessed");

        foreach (var invalid in new[]
        {
            Evidence([Group()], complete: false), Evidence([Group(), Group(count: 1)]), Evidence([Group()], entryCount: 99),
            Evidence([Group(type: 65536)]), Evidence([Group(status: -1)]), Evidence([Group(count: 0)]),
            Evidence([Group(count: 4096), Group(type: 28, count: 1)], entryCount: 4096),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            ordinaryEvidence with { ExitCode = 5 }, ordinaryEvidence with { Error = "denied" },
            ordinaryEvidence with { TimedOut = true }, ordinaryEvidence with { Truncated = true }
        }) Require(DnsClientCacheDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete, invalid, duplicate, inconsistent, excessive, malformed and failed DNS cache evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await DnsClientCacheDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(ordinaryEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Last().Contains("Get-DnsClientCache", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Clear-DnsClientCache", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains(".Entry", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains(".Data", StringComparison.Ordinal),
            "Native plan groups bounded DNS cache state without retaining names or record data and without clearing the cache");
        Require((await DnsClientCacheDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "DNS cache provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DnsClientCacheDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents DNS cache collection");
        Console.WriteLine("PASS: privacy-bounded DNS client cache aggregates, negative-entry handling and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
