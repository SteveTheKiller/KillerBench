using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class StorageJobChecks
{
    internal static async Task RunAsync()
    {
        object Group(string name = "Regeneration", string state = "Running", bool background = true,
            long count = 1, long maximumPercent = 50, long maximumElapsedSeconds = 120) =>
            new { name, state, background, count, maximumPercent, maximumElapsedSeconds };
        ToolResult Evidence(object[] groups, long? jobCount = null, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new
            {
                complete,
                jobCount = jobCount ?? groups.Sum(group => (long)group.GetType().GetProperty("count")!.GetValue(group)!),
                groups
            }), "", false);

        var activeEvidence = Evidence([Group(), Group("Optimization", "Completed", false, 2, 100, 900)]);
        var active = StorageJobDiagnostics.Interpret(activeEvidence);
        Require(active is { State: CheckState.Information, Repair: null } && ReferenceEquals(active.Evidence, activeEvidence) &&
            active.Detail.Contains("1 active", StringComparison.Ordinal) && active.Detail.Contains("2 completed", StringComparison.Ordinal),
            "Active and completed storage jobs remain informational and retain bounded evidence");
        Require(StorageJobDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "An empty complete storage job inventory remains informational");
        Require(StorageJobDiagnostics.Interpret(Evidence([Group(state: "Suspended")])).State == CheckState.Attention &&
            StorageJobDiagnostics.Interpret(Evidence([Group(state: "Exception")])).State == CheckState.Attention,
            "Suspended and failed storage jobs request current provider review");

        foreach (var invalid in new[]
        {
            Evidence([Group()], complete: false), Evidence([Group(), Group(count: 2)]), Evidence([Group()], jobCount: 99),
            Evidence([Group(state: "Future")]), Evidence([Group(maximumPercent: 101)]), Evidence([Group(maximumElapsedSeconds: -1)]),
            Evidence([Group(count: 256), Group("Second", count: 1)], jobCount: 256),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            activeEvidence with { ExitCode = 5 }, activeEvidence with { Error = "denied" },
            activeEvidence with { TimedOut = true }, activeEvidence with { Truncated = true }
        }) Require(StorageJobDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete, invalid, duplicate, inconsistent, excessive, malformed and failed storage job evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await StorageJobDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(activeEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Last().Contains("Get-StorageJob", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Stop-StorageJob", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("UniqueId", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("ObjectId", StringComparison.Ordinal),
            "Native plan aggregates bounded storage job state without identities and without changing jobs");
        Require((await StorageJobDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Storage job provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await StorageJobDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents storage job collection");
        Console.WriteLine("PASS: bounded Storage module job states, progress, privacy and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
