using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class ServiceStartReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "service-start-reconciliation");
        Directory.CreateDirectory(root);
        int sequence = 600;
        foreach (var kind in Enum.GetValues<RepairKind>().Where(kind => ServiceRecovery.ServiceName(kind) is not null))
        {
            string service = ServiceRecovery.ServiceName(kind)!;
            var firstBoot = new BootStamp(sequence++, new DateTimeOffset(2026, 9, 14, 6, 0, 0, TimeSpan.Zero));
            BootStamp? boot = firstBoot;
            async Task<RepairRecoveryStore> Pending(string outcome, RepairKind journalKind)
            {
                string folder = Path.Combine(root, kind + "-" + outcome);
                Directory.CreateDirectory(folder);
                string journal = Path.Combine(folder, "repair.json");
                await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, journalKind,
                    DateTimeOffset.UtcNow, "Running", journal, []), Capture.JsonOptions));
                var store = new RepairRecoveryStore(folder, _ => Task.FromResult<BootStamp?>(boot));
                await store.BeginAsync(journal);
                return store;
            }
            Task<WorkflowCheck> State(int state, CheckState checkState) => Task.FromResult(new WorkflowCheck(kind.ToString(), checkState, "Current state.",
                checkState == CheckState.Repairable ? kind : null, new ToolResult(0, "SERVICE_NAME: " + service + "\nSTATE : " + state + (state == 4 ? " RUNNING" : " STOPPED"), "", false)));

            var verified = await Pending("verified", kind);
            boot = new(sequence++, firstBoot.StartedUtc.AddMinutes(10));
            Require((await ServiceStartReconciliation.ReconcileAsync(kind, verified, _ => State(4, CheckState.Clear))).Outcome == ServiceStartReconciliationOutcome.Verified &&
                (await verified.ReadAsync())?.Pending == false, kind + " running evidence resolves without another start");
            boot = firstBoot;
            var stopped = await Pending("not-applied", kind);
            boot = new(sequence++, firstBoot.StartedUtc.AddMinutes(10));
            Require((await ServiceStartReconciliation.ReconcileAsync(kind, stopped, _ => State(1, CheckState.Attention))).Outcome == ServiceStartReconciliationOutcome.NotApplied &&
                (await stopped.ReadAsync())?.Pending == false, kind + " stopped evidence resolves without another start");
        }

        var sameBoot = new BootStamp(sequence++, new DateTimeOffset(2026, 9, 14, 6, 0, 0, TimeSpan.Zero));
        string sameFolder = Path.Combine(root, "same-boot");
        Directory.CreateDirectory(sameFolder);
        string sameJournal = Path.Combine(sameFolder, "repair.json");
        await File.WriteAllTextAsync(sameJournal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, RepairKind.PrintSpooler,
            DateTimeOffset.UtcNow, "Running", sameJournal, []), Capture.JsonOptions));
        var sameStore = new RepairRecoveryStore(sameFolder, _ => Task.FromResult<BootStamp?>(sameBoot));
        await sameStore.BeginAsync(sameJournal);
        Require((await ServiceStartReconciliation.ReconcileAsync(RepairKind.PrintSpooler, sameStore, _ => throw new Exception("provider dispatched"))).Outcome == ServiceStartReconciliationOutcome.Inconclusive &&
            (await sameStore.ReadAsync())?.Pending == true, "Same-boot service evidence retains the guard");
        Console.WriteLine("PASS: every service-start recipe reconciles exact later-boot running or stopped evidence without another start; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
