using System.Text.Json;

namespace KillerBench.Engine.Security;

public enum DefenderRefreshReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record DefenderRefreshReconciliationResult(DefenderRefreshReconciliationOutcome Outcome, string Detail);

public static class DefenderRefreshReconciliation
{
    public static async Task<DefenderRefreshReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(DefenderRefreshReconciliationOutcome.NoPending, "No pending Defender signature refresh was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(DefenderRefreshReconciliationOutcome.Inconclusive, "Pending refresh evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.DefenderSignatures))
            return Result(DefenderRefreshReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.DefenderSignatures))
            return Result(DefenderRefreshReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(DefenderRefreshReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted refresh can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Defender provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(45), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(DefenderRefreshReconciliationOutcome.Inconclusive, "Current Defender signature evidence is unavailable; the guard remains active."); }
        if (current.EffectiveState == CheckState.Clear)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(DefenderRefreshReconciliationOutcome.Verified,
                "The interrupted refresh is reconciled. Defender reports current signatures, and another refresh was not requested.");
        }
        if (current.EffectiveState == CheckState.Repairable && current.Repair == RepairKind.DefenderSignatures)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(DefenderRefreshReconciliationOutcome.NotApplied,
                "The interrupted refresh is reconciled. Defender still reports stale signatures, so no update was verified and another refresh was not requested in this run.");
        }
        return Result(DefenderRefreshReconciliationOutcome.Inconclusive,
            "Current Defender protection or signature evidence is incomplete or changed; the guard remains active.");
    }

    private static DefenderRefreshReconciliationResult Result(DefenderRefreshReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
