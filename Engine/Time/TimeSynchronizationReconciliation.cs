using System.Text.Json;

namespace KillerBench.Engine.Time;

public enum TimeSynchronizationReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record TimeSynchronizationReconciliationResult(TimeSynchronizationReconciliationOutcome Outcome, string Detail);

public static class TimeSynchronizationReconciliation
{
    public static async Task<TimeSynchronizationReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(TimeSynchronizationReconciliationOutcome.NoPending, "No pending Windows Time synchronization was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(TimeSynchronizationReconciliationOutcome.Inconclusive, "Pending time-synchronization evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.TimeSynchronization))
            return Result(TimeSynchronizationReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.TimeSynchronization))
            return Result(TimeSynchronizationReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(TimeSynchronizationReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted synchronization can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Windows Time provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(90), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(TimeSynchronizationReconciliationOutcome.Inconclusive, "Current Windows Time evidence is unavailable; the guard remains active."); }
        if (current.EffectiveState == CheckState.Clear)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(TimeSynchronizationReconciliationOutcome.Verified,
                "The interrupted synchronization is reconciled. Windows reports a synchronized external or provider source, and another resync was not requested.");
        }
        if (current.Evidence is { } evidence && TimeRecovery.Interpret(evidence).EffectiveState is CheckState.Repairable or CheckState.Attention)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(TimeSynchronizationReconciliationOutcome.NotApplied,
                "The interrupted synchronization is reconciled. Current status does not verify external or provider synchronization, and another resync was not requested in this run.");
        }
        return Result(TimeSynchronizationReconciliationOutcome.Inconclusive,
            "Current Windows Time configuration or status evidence is incomplete or changed; the guard remains active.");
    }

    private static TimeSynchronizationReconciliationResult Result(TimeSynchronizationReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
