using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsRecoveryReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "windows-recovery-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(200, new DateTimeOffset(2026, 9, 14, 2, 0, 0, TimeSpan.Zero));
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

        static Task<WorkflowCheck> Enabled(CancellationToken _) => Task.FromResult(new WorkflowCheck("Windows Recovery Environment",
            CheckState.Clear, "Enabled"));
        static Task<WorkflowCheck> Disabled(CancellationToken _) => Task.FromResult(new WorkflowCheck("Windows Recovery Environment",
            CheckState.Repairable, "Disabled", RepairKind.WindowsRecoveryEnvironment));

        var sameBoot = await Pending("same-boot", RepairKind.WindowsRecoveryEnvironment);
        var same = await WindowsRecoveryReconciliation.ReconcileAsync(sameBoot, Enabled);
        Require(same.Outcome == WindowsRecoveryReconciliationOutcome.Inconclusive && (await sameBoot.ReadAsync())?.Pending == true,
            "Same-boot recovery evidence cannot clear or repeat interrupted enablement");

        boot = firstBoot;
        var verifiedStore = await Pending("verified", RepairKind.WindowsRecoveryEnvironment);
        boot = new(201, firstBoot.StartedUtc.AddMinutes(10));
        var verified = await WindowsRecoveryReconciliation.ReconcileAsync(verifiedStore, Enabled);
        Require(verified.Outcome == WindowsRecoveryReconciliationOutcome.Verified && (await verifiedStore.ReadAsync())?.Pending == false,
            "Later-boot enabled evidence resolves the exact pending guard without another enable command");

        boot = firstBoot;
        var disabledStore = await Pending("not-applied", RepairKind.WindowsRecoveryEnvironment);
        boot = new(202, firstBoot.StartedUtc.AddMinutes(10));
        var disabled = await WindowsRecoveryReconciliation.ReconcileAsync(disabledStore, Disabled);
        Require(disabled.Outcome == WindowsRecoveryReconciliationOutcome.NotApplied && (await disabledStore.ReadAsync())?.Pending == false,
            "Later-boot disabled evidence records no applied change without repeating enablement");

        boot = firstBoot;
        var unknownStore = await Pending("unknown", RepairKind.WindowsRecoveryEnvironment);
        boot = new(203, firstBoot.StartedUtc.AddMinutes(10));
        var unknown = await WindowsRecoveryReconciliation.ReconcileAsync(unknownStore, _ => Task.FromResult(new WorkflowCheck("WinRE", CheckState.Unknown, "Unavailable")));
        Require(unknown.Outcome == WindowsRecoveryReconciliationOutcome.Inconclusive && (await unknownStore.ReadAsync())?.Pending == true,
            "Incomplete current recovery evidence retains the pending guard");

        boot = firstBoot;
        var unrelatedStore = await Pending("unrelated", RepairKind.SystemFiles);
        boot = new(204, firstBoot.StartedUtc.AddMinutes(10));
        var unrelated = await WindowsRecoveryReconciliation.ReconcileAsync(unrelatedStore, _ => throw new Exception("provider dispatched"));
        Require(unrelated.Outcome == WindowsRecoveryReconciliationOutcome.NoPending && (await unrelatedStore.ReadAsync())?.Pending == true,
            "Windows Recovery reconciliation preserves unrelated pending repairs without querying REAgentC");

        Console.WriteLine("PASS: interrupted Windows Recovery enablement requires later-boot exact evidence and never repeats the command; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
