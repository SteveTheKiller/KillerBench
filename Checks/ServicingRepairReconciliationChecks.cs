using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class ServicingRepairReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        foreach (var kind in new[] { RepairKind.ComponentStore, RepairKind.SystemFiles })
            await RunKindAsync(Path.Combine(scratch, "servicing-reconciliation", kind.ToString()), kind);
        Console.WriteLine("PASS: interrupted DISM and SFC repairs use later-boot evidence and never repeat servicing; scratch records only.");
    }

    private static async Task RunKindAsync(string root, RepairKind kind)
    {
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(500, new DateTimeOffset(2026, 9, 14, 5, 0, 0, TimeSpan.Zero));
        BootStamp? boot = firstBoot;
        async Task<RepairRecoveryStore> Pending(string name, RepairKind journalKind)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            string journal = Path.Combine(folder, "repair.json");
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, journalKind,
                DateTimeOffset.UtcNow, "Running", journal, []), Capture.JsonOptions));
            var store = new RepairRecoveryStore(folder, _ => Task.FromResult<BootStamp?>(boot));
            await store.BeginAsync(journal);
            return store;
        }
        Task<WorkflowCheck> Clear(CancellationToken _) => Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Clear, "Scope clear."));
        Task<WorkflowCheck> Repairable(CancellationToken _) => Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Repairable, "Damage remains.", kind));

        var sameBoot = await Pending("same-boot", kind);
        Require((await ServicingRepairReconciliation.ReconcileAsync(kind, sameBoot, Clear)).Outcome == ServicingRepairReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, kind + " same-boot evidence retains its guard");
        boot = firstBoot;
        var verified = await Pending("verified", kind);
        boot = new(501, firstBoot.StartedUtc.AddMinutes(10));
        Require((await ServicingRepairReconciliation.ReconcileAsync(kind, verified, Clear)).Outcome == ServicingRepairReconciliationOutcome.Verified &&
            (await verified.ReadAsync())?.Pending == false, kind + " clear evidence resolves without retrying");
        boot = firstBoot;
        var notApplied = await Pending("not-applied", kind);
        boot = new(502, firstBoot.StartedUtc.AddMinutes(10));
        Require((await ServicingRepairReconciliation.ReconcileAsync(kind, notApplied, Repairable)).Outcome == ServicingRepairReconciliationOutcome.NotApplied &&
            (await notApplied.ReadAsync())?.Pending == false, kind + " remaining damage resolves without retrying");
        boot = firstBoot;
        var unrelated = await Pending("unrelated", RepairKind.DefenderSignatures);
        boot = new(503, firstBoot.StartedUtc.AddMinutes(10));
        Require((await ServicingRepairReconciliation.ReconcileAsync(kind, unrelated, _ => throw new Exception("provider dispatched"))).Outcome == ServicingRepairReconciliationOutcome.NoPending &&
            (await unrelated.ReadAsync())?.Pending == true, kind + " preserves unrelated guards");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
