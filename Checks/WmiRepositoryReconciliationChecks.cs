using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WmiRepositoryReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "wmi-repository-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(900, new DateTimeOffset(2026, 9, 14, 9, 0, 0, TimeSpan.Zero));
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
        static Task<WorkflowCheck> Consistent(CancellationToken _) => Task.FromResult(WmiRecovery.Interpret(new(0, "WMI repository is consistent", "", false)));
        static Task<WorkflowCheck> Inconsistent(CancellationToken _) => Task.FromResult(WmiRecovery.Interpret(new(1358, "WMI repository is inconsistent", "", false)));

        var sameBoot = await Pending("same-boot", RepairKind.WmiRepository);
        Require((await WmiRepositoryReconciliation.ReconcileAsync(sameBoot, Consistent)).Outcome == WmiRepositoryReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot WMI evidence retains the guard");
        boot = firstBoot;
        var verified = await Pending("verified", RepairKind.WmiRepository);
        boot = new(901, firstBoot.StartedUtc.AddMinutes(10));
        Require((await WmiRepositoryReconciliation.ReconcileAsync(verified, Consistent)).Outcome == WmiRepositoryReconciliationOutcome.Verified &&
            (await verified.ReadAsync())?.Pending == false, "Consistent later-boot WMI evidence resolves without salvage");
        boot = firstBoot;
        var notApplied = await Pending("not-applied", RepairKind.WmiRepository);
        boot = new(902, firstBoot.StartedUtc.AddMinutes(10));
        Require((await WmiRepositoryReconciliation.ReconcileAsync(notApplied, Inconsistent)).Outcome == WmiRepositoryReconciliationOutcome.NotApplied &&
            (await notApplied.ReadAsync())?.Pending == false, "Inconsistent later-boot WMI evidence resolves without salvage");
        boot = firstBoot;
        var unrelated = await Pending("unrelated", RepairKind.SystemFiles);
        boot = new(903, firstBoot.StartedUtc.AddMinutes(10));
        Require((await WmiRepositoryReconciliation.ReconcileAsync(unrelated, _ => throw new Exception("provider dispatched"))).Outcome == WmiRepositoryReconciliationOutcome.NoPending &&
            (await unrelated.ReadAsync())?.Pending == true, "WMI reconciliation preserves unrelated repair guards");
        Console.WriteLine("PASS: interrupted WMI salvage uses later-boot exact verdicts and never repeats salvage; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
