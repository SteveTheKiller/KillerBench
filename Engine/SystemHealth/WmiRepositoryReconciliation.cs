using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public enum WmiRepositoryReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record WmiRepositoryReconciliationResult(WmiRepositoryReconciliationOutcome Outcome, string Detail);

public static class WmiRepositoryReconciliation
{
    public static async Task<WmiRepositoryReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(WmiRepositoryReconciliationOutcome.NoPending, "No pending WMI repository salvage was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(WmiRepositoryReconciliationOutcome.Inconclusive, "Pending WMI salvage evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.WmiRepository))
            return Result(WmiRepositoryReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.WmiRepository))
            return Result(WmiRepositoryReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(WmiRepositoryReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted WMI salvage can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("WMI provider returned no task."))
                .WaitAsync(TimeSpan.FromMinutes(6), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(WmiRepositoryReconciliationOutcome.Inconclusive, "Current WMI repository evidence is unavailable; the guard remains active."); }
        if (current.EffectiveState == CheckState.Clear)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(WmiRepositoryReconciliationOutcome.Verified,
                "The interrupted WMI salvage is reconciled. The repository is consistent, and salvage was not started again.");
        }
        if (current.EffectiveState == CheckState.Repairable && current.Repair == RepairKind.WmiRepository)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(WmiRepositoryReconciliationOutcome.NotApplied,
                "The interrupted WMI salvage is reconciled. The repository remains inconsistent, so repair was not verified and salvage was not started again in this run.");
        }
        return Result(WmiRepositoryReconciliationOutcome.Inconclusive,
            "Current WMI repository evidence is incomplete or changed; the guard remains active.");
    }

    private static WmiRepositoryReconciliationResult Result(WmiRepositoryReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
