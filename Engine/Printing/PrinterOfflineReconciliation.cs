using System.Text.Json;

namespace KillerBench.Engine.Printing;

public enum PrinterOfflineReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record PrinterOfflineReconciliationResult(PrinterOfflineReconciliationOutcome Outcome, string Detail);

public static class PrinterOfflineReconciliation
{
    public static async Task<PrinterOfflineReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<IReadOnlyList<string>, CancellationToken, Task<ToolResult>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(PrinterOfflineReconciliationOutcome.NoPending, "No pending printer offline recovery was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(PrinterOfflineReconciliationOutcome.Inconclusive, "Pending printer evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.PrinterOffline))
            return Result(PrinterOfflineReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.PrinterOffline))
            return Result(PrinterOfflineReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var selected = journal.SelectedPrinterQueues?.ToArray();
        if (!ValidSelection(selected) || journal.Steps is not { Count: > 0 } || journal.Steps[0] is not { Step: { } step, Result: { } result })
            return Result(PrinterOfflineReconciliationOutcome.Inconclusive,
                "The interrupted printer change has no complete retained queue selection; the guard remains active.");
        RepairStep expected;
        try { expected = PrinterOfflineRecovery.Step(false, selected!); }
        catch (ArgumentException)
        {
            return Result(PrinterOfflineReconciliationOutcome.Inconclusive,
                "The interrupted printer change has an invalid retained queue selection; the guard remains active.");
        }
        var before = PrinterOfflineRecovery.Read(result);
        if (step.Verification || step.Tool != expected.Tool || !step.Arguments.SequenceEqual(expected.Arguments) ||
            before is null || before.Queues.Count != selected!.Length ||
            before.Targets.Length != selected.Length ||
            selected.Any(id => !before.Targets.Contains(id, StringComparer.OrdinalIgnoreCase) ||
                !before.Queues.TryGetValue(id, out bool offline) || offline))
            return Result(PrinterOfflineReconciliationOutcome.Inconclusive,
                "The interrupted printer change has no complete retained mutation snapshot; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(PrinterOfflineReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted printer change can be reconciled; the guard remains active.");
        ToolResult currentResult;
        try
        {
            currentResult = await (inspect(selected, token) ?? throw new InvalidOperationException("Printer provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(100), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(PrinterOfflineReconciliationOutcome.Inconclusive, "Current printer evidence is unavailable; the guard remains active."); }
        var current = PrinterOfflineRecovery.Read(currentResult);
        if (current is null || current.Targets.Length != 0 || current.Queues.Count != selected.Length ||
            selected.Any(id => !current.Queues.ContainsKey(id)))
            return Result(PrinterOfflineReconciliationOutcome.Inconclusive,
                "A selected printer queue is missing or current identities are incomplete; the guard remains active.");
        await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
        if (selected.All(id => !current.Queues[id]))
            return Result(PrinterOfflineReconciliationOutcome.Verified,
                "The interrupted printer change is reconciled. Every exact selected queue is present and online, and no setting was changed again.");
        return Result(PrinterOfflineReconciliationOutcome.NotApplied,
            "The interrupted printer change is reconciled. At least one exact selected queue remains offline, so recovery was not verified and no setting was changed again in this run.");
    }

    private static bool ValidSelection(string[]? selected) => selected is { Length: >= 1 and <= 64 } &&
        selected.All(id => !string.IsNullOrWhiteSpace(id) && id.Length <= 256 && !id.Any(char.IsControl)) &&
        selected.Distinct(StringComparer.OrdinalIgnoreCase).Count() == selected.Length;

    private static PrinterOfflineReconciliationResult Result(PrinterOfflineReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
