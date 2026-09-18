using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PrintQueueReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "print-queue-reconciliation");
        Directory.CreateDirectory(root);
        var firstBoot = new BootStamp(1200, new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero));
        BootStamp? boot = firstBoot;
        static ToolResult Snapshot(string[] names, string[] paused, string[] targets, string[]? waiting = null) => new(0, JsonSerializer.Serialize(new
        {
            count = names.Length,
            paused = paused.Length,
            names,
            pausedNames = paused,
            targets,
            waitingNames = waiting ?? Array.Empty<string>()
        }, Capture.JsonOptions), "", false);
        async Task<RepairRecoveryStore> Pending(string name, bool complete = true)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            string journal = Path.Combine(folder, "repair.json");
            IReadOnlyList<RepairStepResult> steps = complete ? [new(PrintRecovery.Step(false, ["Queue A"]), Snapshot(["Queue A"], [], ["Queue A"], ["Queue A"]))] : [];
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, RepairKind.PrintQueues,
                DateTimeOffset.UtcNow, "Running", journal, steps) { SelectedPrinterQueues = ["Queue A"] }, Capture.JsonOptions));
            var store = new RepairRecoveryStore(folder, _ => Task.FromResult<BootStamp?>(boot));
            await store.BeginAsync(journal);
            return store;
        }

        Require(PrintRecovery.ReadIdentities(Snapshot(["Queue A", "Queue B"], ["Queue B"], [])) is { PausedNames: ["Queue B"] },
            "Print snapshot retains exact paused queue identities");
        var sameBoot = await Pending("same-boot");
        Require((await PrintQueueReconciliation.ReconcileAsync(sameBoot, _ => Task.FromResult(Snapshot(["Queue A", "Queue B"], [], [])))).Outcome == PrintQueueReconciliationOutcome.Inconclusive &&
            (await sameBoot.ReadAsync())?.Pending == true, "Same-boot print evidence retains the guard");
        boot = firstBoot;
        var verified = await Pending("verified");
        boot = new(1201, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrintQueueReconciliation.ReconcileAsync(verified, _ => Task.FromResult(Snapshot(["Queue A", "Queue B"], ["Queue B"], [])))).Outcome == PrintQueueReconciliationOutcome.Verified &&
            (await verified.ReadAsync())?.Pending == false, "Exact unpaused target resolves without another resume");
        boot = firstBoot;
        var notApplied = await Pending("not-applied");
        boot = new(1202, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrintQueueReconciliation.ReconcileAsync(notApplied, _ => Task.FromResult(Snapshot(["Queue A", "Queue B"], ["Queue A"], [])))).Outcome == PrintQueueReconciliationOutcome.NotApplied &&
            (await notApplied.ReadAsync())?.Pending == false, "Exact paused target records no verified resume");
        boot = firstBoot;
        var missing = await Pending("missing");
        boot = new(1203, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrintQueueReconciliation.ReconcileAsync(missing, _ => Task.FromResult(Snapshot(["Queue B"], [], [])))).Outcome == PrintQueueReconciliationOutcome.Inconclusive &&
            (await missing.ReadAsync())?.Pending == true, "Missing target queue retains the guard");
        boot = firstBoot;
        var incomplete = await Pending("incomplete", false);
        boot = new(1204, firstBoot.StartedUtc.AddMinutes(10));
        Require((await PrintQueueReconciliation.ReconcileAsync(incomplete, _ => throw new Exception("provider dispatched"))).Outcome == PrintQueueReconciliationOutcome.Inconclusive &&
            (await incomplete.ReadAsync())?.Pending == true, "Missing retained targets preserve the print guard");
        Console.WriteLine("PASS: interrupted queue resume requires exact paused identities and never resumes again; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
