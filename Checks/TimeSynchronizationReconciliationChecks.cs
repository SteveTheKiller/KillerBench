using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class TimeSynchronizationReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "time-synchronization-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(1000, new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero));
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
        const string Synced = "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0";
        static Task<WorkflowCheck> Status(string output) => Task.FromResult(TimeRecovery.Interpret(new(0, output, "", false)));

        var sameBoot = await Pending("same-boot", RepairKind.TimeSynchronization);
        Require((await TimeSynchronizationReconciliation.ReconcileAsync(sameBoot, _ => Status(Synced))).Outcome == TimeSynchronizationReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot time evidence retains the guard");
        boot = firstBoot;
        var verified = await Pending("verified", RepairKind.TimeSynchronization);
        boot = new(1001, firstBoot.StartedUtc.AddMinutes(10));
        Require((await TimeSynchronizationReconciliation.ReconcileAsync(verified, _ => Status(Synced))).Outcome == TimeSynchronizationReconciliationOutcome.Verified &&
            (await verified.ReadAsync())?.Pending == false, "Synchronized later-boot evidence resolves without another resync");
        boot = firstBoot;
        var notApplied = await Pending("not-applied", RepairKind.TimeSynchronization);
        boot = new(1002, firstBoot.StartedUtc.AddMinutes(10));
        Require((await TimeSynchronizationReconciliation.ReconcileAsync(notApplied, _ => Status(Synced.Replace("dc.example.test", "Local CMOS Clock")))).Outcome == TimeSynchronizationReconciliationOutcome.NotApplied &&
            (await notApplied.ReadAsync())?.Pending == false, "Unsuitable later-boot source resolves without another resync");
        boot = firstBoot;
        var unrelated = await Pending("unrelated", RepairKind.WmiRepository);
        boot = new(1003, firstBoot.StartedUtc.AddMinutes(10));
        Require((await TimeSynchronizationReconciliation.ReconcileAsync(unrelated, _ => throw new Exception("provider dispatched"))).Outcome == TimeSynchronizationReconciliationOutcome.NoPending &&
            (await unrelated.ReadAsync())?.Pending == true, "Time reconciliation preserves unrelated repair guards");
        Console.WriteLine("PASS: interrupted time synchronization uses later-boot status and never repeats resync; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
