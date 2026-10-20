using System.IO;
using KillerBench.Engine;

internal static class AggregateCancellationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        foreach (bool afterFile in new[] { false, true })
        {
            using var stop = new CancellationTokenSource();
            var host = new Host(stop, afterFile);
            var result = await CleanupRemoval.RunAsync(new(CleanupTargets.UserTemp | CleanupTargets.RecycleBin), scratch,
                host: host, token: stop.Token);
            if (host.Aggregates != 0 || result.Status != "Stopped" || !result.Partial || result.Files.Count != (afterFile ? 1 : 0))
                throw new Exception("Canceled cleanup must preserve completed files and withhold aggregate dispatch.");
        }
        Console.WriteLine("PASS: cancellation during preview and after a file withholds aggregate cleanup.");
    }

    private sealed class Host(CancellationTokenSource stop, bool afterFile) : ICleanupRemovalHost
    {
        public int Aggregates;
        public CleanupPreviewResult Inspect(CleanupOptions options, IProgress<string>? progress, CancellationToken token)
        {
            if (!afterFile) stop.Cancel();
            var aggregate = new CleanupEstimate(RecycleBinCleanup.TargetName, RecycleBinCleanup.ShellRoot, 1, 1, 0, false, [], "fixture") { AggregateAction = true };
            return new(options, DateTimeOffset.UtcNow, afterFile
                ? [new("fixture", "fixture", 1, 1, 0, false, [new("fixture.tmp", 1, DateTime.UtcNow.AddDays(-30))], "fixture"), aggregate]
                : [aggregate]);
        }
        public CleanupFileOutcome Remove(string root, CleanupCandidate candidate, DateTime cutoffUtc)
        {
            stop.Cancel();
            return new(candidate.Path, "Deleted", "simulated");
        }
        public CleanupFileOutcome RemoveAggregate(CleanupEstimate target, int maximumItems)
        {
            Aggregates++;
            return new(target.Root, "Deleted", "simulated");
        }
    }
}
