using System.Text.Json;

namespace KillerBench.Engine.Infrastructure;

public enum ServicingRepairReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record ServicingRepairReconciliationResult(ServicingRepairReconciliationOutcome Outcome, string Detail);

public static class ServicingRepairReconciliation
{
    public static async Task<ServicingRepairReconciliationResult> ReconcileAsync(RepairKind kind, RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        if (kind is not (RepairKind.ComponentStore or RepairKind.SystemFiles)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(ServicingRepairReconciliationOutcome.NoPending, "No pending servicing repair was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(ServicingRepairReconciliationOutcome.Inconclusive, "Pending servicing evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, kind))
            return Result(ServicingRepairReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, kind))
            return Result(ServicingRepairReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(ServicingRepairReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted servicing repair can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Servicing provider returned no task."))
                .WaitAsync(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(ServicingRepairReconciliationOutcome.Inconclusive, "Current servicing evidence is unavailable; the guard remains active."); }
        if (current.EffectiveState == CheckState.Clear)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(ServicingRepairReconciliationOutcome.Verified,
                "The interrupted servicing repair is reconciled. Fresh verification reports the selected scope clear, and the repair was not started again.");
        }
        if (current.EffectiveState == CheckState.Repairable && current.Repair == kind)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(ServicingRepairReconciliationOutcome.NotApplied,
                "The interrupted servicing repair is reconciled. Fresh verification still reports repairable damage, so completion was not verified and the repair was not started again in this run.");
        }
        return Result(ServicingRepairReconciliationOutcome.Inconclusive,
            "Current servicing evidence is incomplete, unrepairable or changed; the guard remains active.");
    }

    private static ServicingRepairReconciliationResult Result(ServicingRepairReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
