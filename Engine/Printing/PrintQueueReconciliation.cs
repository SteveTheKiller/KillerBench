using System.Text.Json;

namespace KillerBench.Engine.Printing;

public enum PrintQueueReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record PrintQueueReconciliationResult(PrintQueueReconciliationOutcome Outcome, string Detail);

public static class PrintQueueReconciliation
{
    public static async Task<PrintQueueReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<ToolResult>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(PrintQueueReconciliationOutcome.NoPending, "No pending print queue resume was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(PrintQueueReconciliationOutcome.Inconclusive, "Pending print queue evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.PrintQueues))
            return Result(PrintQueueReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.PrintQueues))
            return Result(PrintQueueReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        RepairStep expected;
        try
        {
            expected = journal.SelectedPrinterQueues is { Count: > 0 } selected
                ? PrintRecovery.Step(false, selected) : PrintRecovery.Step(false);
        }
        catch (ArgumentException)
        {
            return Result(PrintQueueReconciliationOutcome.Inconclusive,
                "The interrupted resume has invalid retained queue selection; the guard remains active.");
        }
        if (journal.Steps is not { Count: > 0 } || journal.Steps[0] is not { Step: { } step, Result: { } result } ||
            step.Verification || step.Tool != expected.Tool || !step.Arguments.SequenceEqual(expected.Arguments) ||
            PrintRecovery.ReadIdentities(result) is not { Targets.Length: > 0 } before ||
            journal.SelectedPrinterQueues is { Count: > 0 } retained &&
                (before.Targets.Length != retained.Count || before.WaitingNames.Length != retained.Count ||
                 retained.Any(target => !before.Targets.Contains(target, StringComparer.OrdinalIgnoreCase) ||
                     !before.WaitingNames.Contains(target, StringComparer.OrdinalIgnoreCase))))
            return Result(PrintQueueReconciliationOutcome.Inconclusive,
                "The interrupted resume has no complete retained target snapshot; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(PrintQueueReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted queue resume can be reconciled; the guard remains active.");
        ToolResult currentResult;
        try
        {
            currentResult = await (inspect(token) ?? throw new InvalidOperationException("Print provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(100), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(PrintQueueReconciliationOutcome.Inconclusive, "Current print queue evidence is unavailable; the guard remains active."); }
        var current = PrintRecovery.ReadIdentities(currentResult);
        if (current is null || before.Targets.Any(target => !current.Names.Contains(target, StringComparer.OrdinalIgnoreCase)))
            return Result(PrintQueueReconciliationOutcome.Inconclusive,
                "A target print queue is missing or current identities are incomplete; the guard remains active.");
        if (before.Targets.All(target => !current.PausedNames.Contains(target, StringComparer.OrdinalIgnoreCase)))
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(PrintQueueReconciliationOutcome.Verified,
                "The interrupted queue resume is reconciled. Every exact target is present and unpaused, and no queue was resumed again.");
        }
        await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
        return Result(PrintQueueReconciliationOutcome.NotApplied,
            "The interrupted queue resume is reconciled. At least one exact target remains paused, so resume was not verified and no queue was resumed again in this run.");
    }

    private static PrintQueueReconciliationResult Result(PrintQueueReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
