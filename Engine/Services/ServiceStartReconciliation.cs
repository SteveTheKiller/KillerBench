using System.Text.Json;

namespace KillerBench.Engine;

public enum ServiceStartReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record ServiceStartReconciliationResult(ServiceStartReconciliationOutcome Outcome, string Detail);

public static class ServiceStartReconciliation
{
    public static async Task<ServiceStartReconciliationResult> ReconcileAsync(RepairKind kind, RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        string service = ServiceRecovery.ServiceName(kind) ?? throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(ServiceStartReconciliationOutcome.NoPending, "No pending service start was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(ServiceStartReconciliationOutcome.Inconclusive, "Pending service-start evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, kind))
            return Result(ServiceStartReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, kind))
            return Result(ServiceStartReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(ServiceStartReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted service start can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Service provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(45), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(ServiceStartReconciliationOutcome.Inconclusive, "Current service evidence is unavailable; the guard remains active."); }
        if (current.Evidence is not { } evidence || !ServiceRecovery.MatchesService(evidence, service))
            return Result(ServiceStartReconciliationOutcome.Inconclusive, "Current evidence does not identify the exact service; the guard remains active.");
        if (current.EffectiveState == CheckState.Clear && ServiceRecovery.State(evidence) == 4)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(ServiceStartReconciliationOutcome.Verified,
                "The interrupted service start is reconciled. The exact service is running, and another start was not requested.");
        }
        if (ServiceRecovery.State(evidence) == 1)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(ServiceStartReconciliationOutcome.NotApplied,
                "The interrupted service start is reconciled. The exact service is stopped, so a stable start was not verified and another start was not requested in this run.");
        }
        return Result(ServiceStartReconciliationOutcome.Inconclusive,
            "The exact service is paused, transitioning or otherwise unverified; the guard remains active.");
    }

    private static ServiceStartReconciliationResult Result(ServiceStartReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
