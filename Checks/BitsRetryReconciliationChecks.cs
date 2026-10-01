using KillerBench.Engine;
using System.IO;

internal static class BitsRetryReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        Guid absentId = Guid.NewGuid();
        var absentHost = new Host([]);
        var absent = await BitsRetryReconciliation.RunAsync(Path.Combine(scratch, "bits-reconcile-absent"), absentId,
            absentHost, TimeSpan.Zero);
        Require(absent.Outcome == BitsRetryReconciliationOutcome.NoPendingRecord && absentHost.Reads == 0,
            "Missing retry record does not inspect or change BITS");

        var (activeId, activeFolder, activePending) = await PreparedAsync(scratch, "active", "Queued", includeResult: true);
        var activeHost = new Host([Snapshot(activeId, "Transferring", 20), Snapshot(activeId, "Transferring", 20)]);
        var activeResult = await BitsRetryReconciliation.RunAsync(activeFolder, activeId, activeHost, TimeSpan.Zero);
        Require(activeResult.Outcome == BitsRetryReconciliationOutcome.Active && activeResult.GetExitCode() == 4 &&
            File.Exists(activePending) && activeHost.Requests == 0, "Active retry remains guarded without another request");

        var (transferredId, transferredFolder, transferredPending) = await PreparedAsync(scratch, "transferred", "Transferred", includeResult: true);
        var transferredHost = new Host([Snapshot(transferredId, "Transferred", 100), Snapshot(transferredId, "Transferred", 100)]);
        var transferredResult = await BitsRetryReconciliation.RunAsync(transferredFolder, transferredId, transferredHost, TimeSpan.Zero);
        Require(transferredResult.Outcome == BitsRetryReconciliationOutcome.TransferReported && transferredResult.GetExitCode() == 0 &&
            !File.Exists(transferredPending) && File.Exists(transferredResult.ArchivePath) &&
            File.Exists(transferredResult.ArchivePath + ".prepared.json") && File.Exists(transferredResult.ArchivePath + ".attempt.json") &&
            transferredHost.Requests == 0, "Stable transferred evidence archives the guard and original attempt");

        var (failedId, failedFolder, failedPending) = await PreparedAsync(scratch, "failed", "Error");
        var failedHost = new Host([Snapshot(failedId, "Error", 5), Snapshot(failedId, "Error", 5)]);
        var failedResult = await BitsRetryReconciliation.RunAsync(failedFolder, failedId, failedHost, TimeSpan.Zero);
        Require(failedResult.Outcome == BitsRetryReconciliationOutcome.StableFailure && failedResult.GetExitCode() == 3 &&
            !File.Exists(failedPending) && failedHost.Requests == 0, "Stable failed evidence archives the attempt without retrying");

        var (removedId, removedFolder, removedPending) = await PreparedAsync(scratch, "removed", "Transferred");
        var removedHost = new Host([Empty(), Empty()]);
        var removedResult = await BitsRetryReconciliation.RunAsync(removedFolder, removedId, removedHost, TimeSpan.Zero);
        Require(removedResult.Outcome == BitsRetryReconciliationOutcome.TerminalOrRemoved && !File.Exists(removedPending) &&
            removedHost.Requests == 0, "Two complete absent snapshots archive the guard without claiming completion");

        var (changedId, changedFolder, changedPending) = await PreparedAsync(scratch, "changed", "Transferred");
        var changedHost = new Host([Snapshot(changedId, "Transferred", 99), Snapshot(changedId, "Transferred", 100)]);
        var changedResult = await BitsRetryReconciliation.RunAsync(changedFolder, changedId, changedHost, TimeSpan.Zero);
        Require(changedResult.Outcome == BitsRetryReconciliationOutcome.Inconclusive && File.Exists(changedPending) &&
            changedHost.Requests == 0, "Changing transfer evidence retains the retry guard");

        var (partialId, partialFolder, partialPending) = await PreparedAsync(scratch, "partial", "Transferred");
        var partialHost = new Host([Snapshot(partialId, "Transferred", 100, false), Snapshot(partialId, "Transferred", 100)]);
        var partialResult = await BitsRetryReconciliation.RunAsync(partialFolder, partialId, partialHost, TimeSpan.Zero);
        Require(partialResult.Outcome == BitsRetryReconciliationOutcome.Inconclusive && File.Exists(partialPending) &&
            partialHost.Requests == 0, "Incomplete transfer coverage retains the retry guard");

        var (foreignId, foreignFolder, foreignPending) = await PreparedAsync(scratch, "foreign-machine", "Transferred");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        var foreignHost = new Host([Snapshot(foreignId, "Transferred", 100), Snapshot(foreignId, "Transferred", 100)]);
        var foreignResult = await BitsRetryReconciliation.RunAsync(foreignFolder, foreignId, foreignHost, TimeSpan.Zero);
        Require(foreignResult.Outcome == BitsRetryReconciliationOutcome.Inconclusive && foreignHost.Reads == 0 &&
            foreignHost.Requests == 0 && File.Exists(foreignPending),
            "A foreign-machine BITS retry record remains guarded without reading or retrying the transfer");

        Console.WriteLine("PASS: BITS retry reconciliation uses two exact read-only snapshots, archives stable terminal outcomes and never repeats a retry; fixtures only.");
    }

    private static async Task<(Guid Id, string Folder, string Pending)> PreparedAsync(string scratch, string name,
        string resultState, bool includeResult = false)
    {
        Guid id = Guid.NewGuid();
        string folder = Path.Combine(scratch, "bits-reconcile-" + name);
        var before = Snapshot(id, "Error", 1);
        var journal = new BitsRetryJournal(folder);
        await journal.BeginAsync(id, before, CancellationToken.None);
        if (includeResult)
            await journal.CompleteAsync(new(id, resultState == "Transferred" ? BitsRetryOutcome.TransferReported : BitsRetryOutcome.RetryRequested,
                "fixture", before, After: Snapshot(id, resultState, resultState == "Transferred" ? 100UL : 20UL)), CancellationToken.None);
        return (id, folder, Path.Combine(folder, id.ToString("N") + ".pending.json"));
    }

    private static BitsInventoryResult Snapshot(Guid id, string state, ulong transferred, bool complete = true) =>
        BitsDiagnostics.Parse(new(0, System.Text.Json.JsonSerializer.Serialize(new
        {
            scope = "CurrentAccount", complete,
            jobs = new[] { new { id, name = "Fixture", state, error = state.Contains("Error") ? "Fixture error" : "", owner = "Fixture",
                errorCode = state.Contains("Error") ? -1 : 0, bytesTotal = "100", bytesTransferred = transferred.ToString(System.Globalization.CultureInfo.InvariantCulture) } }
        }), "", false));
    private static BitsInventoryResult Empty() => BitsDiagnostics.Parse(new(0,
        "{\"scope\":\"CurrentAccount\",\"complete\":true,\"jobs\":[]}", "", false));
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Host(IEnumerable<BitsInventoryResult> reads) : IBitsRetryHost
    {
        private readonly Queue<BitsInventoryResult> values = new(reads);
        public string OwnerAccount => "Fixture";
        public int Reads, Requests;
        public Task<BitsInventoryResult> ReadAsync(CancellationToken token)
        {
            Reads++;
            return Task.FromResult(values.Dequeue());
        }
        public Task<ToolResult> RequestRetryAsync(BitsJobSnapshot expected, CancellationToken token)
        {
            Requests++;
            throw new Exception("Reconciliation requested another retry.");
        }
    }
}
