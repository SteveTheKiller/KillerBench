using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DefenderRefreshReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "defender-refresh-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(300, new DateTimeOffset(2026, 9, 14, 3, 0, 0, TimeSpan.Zero));
        BootStamp? boot = firstBoot;
        async Task<RepairRecoveryStore> Pending(string name, RepairKind kind)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            string journal = Path.Combine(folder, "repair.json");
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, kind,
                DateTimeOffset.UtcNow, "Running", journal, []), Capture.JsonOptions));
            var store = new RepairRecoveryStore(folder, _ => Task.FromResult<BootStamp?>(boot));
            await store.BeginAsync(journal);
            return store;
        }
        static Task<WorkflowCheck> Fresh(CancellationToken _) => Task.FromResult(DefenderRecovery.Interpret(new(0, DefenderChecks.Fresh, "", false)));
        static Task<WorkflowCheck> Stale(CancellationToken _) => Task.FromResult(DefenderRecovery.Interpret(new(0,
            DefenderChecks.Fresh.Replace("\"stale\":false", "\"stale\":true"), "", false)));

        var sameBoot = await Pending("same-boot", RepairKind.DefenderSignatures);
        Require((await DefenderRefreshReconciliation.ReconcileAsync(sameBoot, Fresh)).Outcome == DefenderRefreshReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot evidence retains the refresh guard");
        boot = firstBoot;
        var verifiedStore = await Pending("verified", RepairKind.DefenderSignatures);
        boot = new(301, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DefenderRefreshReconciliation.ReconcileAsync(verifiedStore, Fresh)).Outcome == DefenderRefreshReconciliationOutcome.Verified &&
            (await verifiedStore.ReadAsync())?.Pending == false, "Fresh later-boot evidence resolves refresh without another download");
        boot = firstBoot;
        var staleStore = await Pending("not-applied", RepairKind.DefenderSignatures);
        boot = new(302, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DefenderRefreshReconciliation.ReconcileAsync(staleStore, Stale)).Outcome == DefenderRefreshReconciliationOutcome.NotApplied &&
            (await staleStore.ReadAsync())?.Pending == false, "Stale later-boot evidence records no verified update without retrying");
        boot = firstBoot;
        var unrelatedStore = await Pending("unrelated", RepairKind.SystemFiles);
        boot = new(303, firstBoot.StartedUtc.AddMinutes(10));
        Require((await DefenderRefreshReconciliation.ReconcileAsync(unrelatedStore, _ => throw new Exception("provider dispatched"))).Outcome == DefenderRefreshReconciliationOutcome.NoPending &&
            (await unrelatedStore.ReadAsync())?.Pending == true, "Defender reconciliation preserves unrelated repair guards");
        Console.WriteLine("PASS: interrupted Defender signature refresh uses later-boot evidence and never repeats the update; scratch records only.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
