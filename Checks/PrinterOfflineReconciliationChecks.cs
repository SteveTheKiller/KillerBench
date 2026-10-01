using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterOfflineReconciliationChecks
{
    private static readonly string[] SelectedQueueNames = ["Queue A"];

    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "printer-offline-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(1300, new DateTimeOffset(2026, 9, 14, 13, 0, 0, TimeSpan.Zero));
        BootStamp? boot = firstBoot;
        static ToolResult Snapshot((string Id, bool Offline)[] queues, string[] targets) => new(0, JsonSerializer.Serialize(new
        {
            targets,
            queues = queues.Select(queue => new { id = queue.Id, offline = queue.Offline })
        }, Capture.JsonOptions), "", false);
        async Task<RepairRecoveryStore> Pending(string name, bool complete = true)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            string journal = Path.Combine(folder, "repair.json");
            IReadOnlyList<RepairStepResult> steps = complete
                ? [new(PrinterOfflineRecovery.Step(false, ["Queue A"]), Snapshot([("Queue A", false)], ["Queue A"]))]
                : [];
            var repair = new RepairResult(Environment.MachineName, RepairKind.PrinterOffline,
                DateTimeOffset.UtcNow, "Running", journal, steps) { SelectedPrinterQueues = ["Queue A"] };
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(repair, Capture.JsonOptions));
            var store = new RepairRecoveryStore(folder, _ => Task.FromResult<BootStamp?>(boot));
            await store.BeginAsync(journal);
            return store;
        }

        var sameBoot = await Pending("same-boot");
        Require((await PrinterOfflineReconciliation.ReconcileAsync(sameBoot, (_, _) =>
            Task.FromResult(Snapshot([("Queue A", false)], [])))).Outcome == PrinterOfflineReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot printer evidence retains the guard");
        boot = firstBoot;
        var verified = await Pending("verified");
        boot = new(1301, firstBoot.StartedUtc.AddMinutes(10));
        bool exactSelection = false;
        Require((await PrinterOfflineReconciliation.ReconcileAsync(verified, (selected, _) =>
        {
            exactSelection = selected.SequenceEqual(SelectedQueueNames);
            return Task.FromResult(Snapshot([("Queue A", false)], []));
        })).Outcome == PrinterOfflineReconciliationOutcome.Verified && exactSelection &&
            (await verified.ReadAsync())?.Pending == false, "Exact online queue resolves without another setting change");
        boot = firstBoot;
        var notApplied = await Pending("not-applied");
        boot = new(1302, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrinterOfflineReconciliation.ReconcileAsync(notApplied, (_, _) =>
            Task.FromResult(Snapshot([("Queue A", true)], [])))).Outcome == PrinterOfflineReconciliationOutcome.NotApplied &&
            (await notApplied.ReadAsync())?.Pending == false, "Exact offline queue records no verified setting change");
        boot = firstBoot;
        var missing = await Pending("missing");
        boot = new(1303, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrinterOfflineReconciliation.ReconcileAsync(missing, (_, _) =>
            Task.FromResult(Snapshot([], [])))).Outcome == PrinterOfflineReconciliationOutcome.Inconclusive &&
            (await missing.ReadAsync())?.Pending == true, "Missing selected queue retains the guard");
        boot = firstBoot;
        var incomplete = await Pending("incomplete", false);
        boot = new(1304, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrinterOfflineReconciliation.ReconcileAsync(incomplete, (_, _) =>
            throw new Exception("provider dispatched"))).Outcome == PrinterOfflineReconciliationOutcome.Inconclusive &&
            (await incomplete.ReadAsync())?.Pending == true, "Missing retained mutation preserves the printer guard");
        Console.WriteLine("PASS: interrupted printer offline recovery requires exact queue identities and never changes settings again; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
