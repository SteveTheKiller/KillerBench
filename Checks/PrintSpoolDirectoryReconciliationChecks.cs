using System.Text.Json;
using System.IO;
using KillerBench.Engine;

internal static class PrintSpoolDirectoryReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "spool-directory-reconciliation");
        Directory.CreateDirectory(root);
        string path = Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS");
        PrintSpoolDirectorySnapshot Snapshot(bool exists) => new(true, path, path, exists, exists ? false : null,
            DriveType.Fixed, 10L * 1024 * 1024 * 1024, true);
        var firstBoot = new BootStamp(100, new DateTimeOffset(2026, 9, 14, 1, 0, 0, TimeSpan.Zero));

        async Task<(RepairRecoveryStore Store, string Journal)> Pending(string name, RepairKind kind, Func<BootStamp?> boot)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            string journal = Path.Combine(folder, "repair.json");
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new RepairResult(Environment.MachineName, kind,
                DateTimeOffset.UtcNow, "Running", journal, []), Capture.JsonOptions));
            var store = new RepairRecoveryStore(folder, _ => Task.FromResult(boot()));
            await store.BeginAsync(journal);
            return (store, journal);
        }

        BootStamp? boot = firstBoot;
        var sameBoot = await Pending("same-boot", RepairKind.PrintSpoolDirectory, () => boot);
        var sameResult = await PrintSpoolDirectoryRecovery.ReconcileAsync(sameBoot.Store, () => Snapshot(true));
        Require(sameResult.Outcome == PrintSpoolDirectoryReconciliationOutcome.Inconclusive &&
            (await sameBoot.Store.ReadAsync())?.Pending == true,
            "Same-boot evidence cannot clear or repeat an interrupted directory operation");

        boot = firstBoot;
        var verified = await Pending("verified", RepairKind.PrintSpoolDirectory, () => boot);
        boot = new(101, firstBoot.StartedUtc.AddMinutes(5));
        var verifiedResult = await PrintSpoolDirectoryRecovery.ReconcileAsync(verified.Store, () => Snapshot(true));
        Require(verifiedResult.Outcome == PrintSpoolDirectoryReconciliationOutcome.Verified &&
            (await verified.Store.ReadAsync())?.Pending == false,
            "Later-boot exact directory evidence resolves the pending guard without repeating creation");

        boot = firstBoot;
        var absent = await Pending("not-applied", RepairKind.PrintSpoolDirectory, () => boot);
        boot = new(102, firstBoot.StartedUtc.AddMinutes(5));
        var absentResult = await PrintSpoolDirectoryRecovery.ReconcileAsync(absent.Store, () => Snapshot(false));
        Require(absentResult.Outcome == PrintSpoolDirectoryReconciliationOutcome.NotApplied &&
            (await absent.Store.ReadAsync())?.Pending == false,
            "Later-boot exact absent evidence records no applied change without retrying it");

        boot = firstBoot;
        var unrelated = await Pending("unrelated", RepairKind.SystemFiles, () => boot);
        boot = new(103, firstBoot.StartedUtc.AddMinutes(5));
        var unrelatedResult = await PrintSpoolDirectoryRecovery.ReconcileAsync(unrelated.Store, () => throw new Exception("reader dispatched"));
        Require(unrelatedResult.Outcome == PrintSpoolDirectoryReconciliationOutcome.NoPending &&
            (await unrelated.Store.ReadAsync())?.Pending == true,
            "Spool-directory reconciliation preserves unrelated repair guards without reading directory state");

        Console.WriteLine("PASS: interrupted print spool directory recovery requires later-boot exact evidence and never repeats creation; scratch records only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
