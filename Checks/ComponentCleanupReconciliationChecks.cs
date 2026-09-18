using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class ComponentCleanupReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "component-cleanup-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(400, new DateTimeOffset(2026, 9, 14, 4, 0, 0, TimeSpan.Zero));
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
        static Task<WorkflowCheck> Clear(CancellationToken _) => Task.FromResult(new WorkflowCheck("Component-store cleanup", CheckState.Clear, "No cleanup recommended."));
        static Task<WorkflowCheck> Recommended(CancellationToken _) => Task.FromResult(new WorkflowCheck("Component-store cleanup", CheckState.Repairable, "Cleanup recommended.", RepairKind.ComponentCleanup));

        var sameBoot = await Pending("same-boot", RepairKind.ComponentCleanup);
        Require((await ComponentCleanupReconciliation.ReconcileAsync(sameBoot, Clear)).Outcome == ComponentCleanupReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot evidence retains the cleanup guard");
        boot = firstBoot;
        var verifiedStore = await Pending("verified", RepairKind.ComponentCleanup);
        boot = new(401, firstBoot.StartedUtc.AddMinutes(10));
        Require((await ComponentCleanupReconciliation.ReconcileAsync(verifiedStore, Clear)).Outcome == ComponentCleanupReconciliationOutcome.Verified &&
            (await verifiedStore.ReadAsync())?.Pending == false, "Clear later-boot evidence resolves cleanup without another mutation");
        boot = firstBoot;
        var recommendedStore = await Pending("not-applied", RepairKind.ComponentCleanup);
        boot = new(402, firstBoot.StartedUtc.AddMinutes(10));
        Require((await ComponentCleanupReconciliation.ReconcileAsync(recommendedStore, Recommended)).Outcome == ComponentCleanupReconciliationOutcome.NotApplied &&
            (await recommendedStore.ReadAsync())?.Pending == false, "Recommended later-boot evidence records no verified cleanup without retrying");
        boot = firstBoot;
        var unrelatedStore = await Pending("unrelated", RepairKind.SystemFiles);
        boot = new(403, firstBoot.StartedUtc.AddMinutes(10));
        Require((await ComponentCleanupReconciliation.ReconcileAsync(unrelatedStore, _ => throw new Exception("provider dispatched"))).Outcome == ComponentCleanupReconciliationOutcome.NoPending &&
            (await unrelatedStore.ReadAsync())?.Pending == true, "Cleanup reconciliation preserves unrelated repair guards");
        Console.WriteLine("PASS: interrupted component cleanup uses later-boot evidence and never repeats maintenance; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
