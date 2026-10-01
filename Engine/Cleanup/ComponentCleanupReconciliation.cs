using System.Text.Json;

namespace KillerBench.Engine.Cleanup;

public enum ComponentCleanupReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record ComponentCleanupReconciliationResult(ComponentCleanupReconciliationOutcome Outcome, string Detail);

public static class ComponentCleanupReconciliation
{
    public static async Task<ComponentCleanupReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(ComponentCleanupReconciliationOutcome.NoPending, "No pending component cleanup was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(ComponentCleanupReconciliationOutcome.Inconclusive, "Pending cleanup evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.ComponentCleanup))
            return Result(ComponentCleanupReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.ComponentCleanup))
            return Result(ComponentCleanupReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(ComponentCleanupReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted cleanup can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Component cleanup provider returned no task."))
                .WaitAsync(TimeSpan.FromMinutes(2), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(ComponentCleanupReconciliationOutcome.Inconclusive, "Current component cleanup evidence is unavailable; the guard remains active."); }
        if (current.EffectiveState == CheckState.Clear)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(ComponentCleanupReconciliationOutcome.Verified,
                "The interrupted cleanup is reconciled. Windows no longer recommends component cleanup, and cleanup was not started again.");
        }
        if (current.EffectiveState == CheckState.Repairable && current.Repair == RepairKind.ComponentCleanup)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(ComponentCleanupReconciliationOutcome.NotApplied,
                "The interrupted cleanup is reconciled. Windows still recommends cleanup, so completion was not verified and cleanup was not started again in this run.");
        }
        return Result(ComponentCleanupReconciliationOutcome.Inconclusive,
            "Current component cleanup evidence is incomplete or changed; the guard remains active.");
    }

    private static ComponentCleanupReconciliationResult Result(ComponentCleanupReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
