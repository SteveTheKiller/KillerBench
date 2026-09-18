using System.Text.Json;

namespace KillerBench.Engine;

public enum WindowsRecoveryReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record WindowsRecoveryReconciliationResult(WindowsRecoveryReconciliationOutcome Outcome, string Detail);

public static class WindowsRecoveryReconciliation
{
    public static async Task<WindowsRecoveryReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(WindowsRecoveryReconciliationOutcome.NoPending,
            "No pending Windows Recovery Environment repair was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(WindowsRecoveryReconciliationOutcome.Inconclusive, "Pending recovery evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.WindowsRecoveryEnvironment))
            return Result(WindowsRecoveryReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.WindowsRecoveryEnvironment))
            return Result(WindowsRecoveryReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId ||
            currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(WindowsRecoveryReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before interrupted recovery enablement can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Recovery provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(WindowsRecoveryReconciliationOutcome.Inconclusive, "Current Windows Recovery Environment evidence is unavailable; the guard remains active."); }
        if (current.EffectiveState == CheckState.Clear)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(WindowsRecoveryReconciliationOutcome.Verified,
                "Interrupted enablement is reconciled. Windows reports the Recovery Environment enabled, and the enable command was not repeated.");
        }
        if (current.EffectiveState == CheckState.Repairable && current.Repair == RepairKind.WindowsRecoveryEnvironment)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(WindowsRecoveryReconciliationOutcome.NotApplied,
                "Interrupted enablement is reconciled. Windows still reports the Recovery Environment disabled, so no change was verified and the enable command was not repeated in this run.");
        }
        return Result(WindowsRecoveryReconciliationOutcome.Inconclusive,
            "Current Windows Recovery Environment evidence is incomplete or changed; the guard remains active.");
    }

    private static WindowsRecoveryReconciliationResult Result(WindowsRecoveryReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
