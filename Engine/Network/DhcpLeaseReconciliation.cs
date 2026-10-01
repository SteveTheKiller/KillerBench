using System.Text.Json;

namespace KillerBench.Engine.Network;

public enum DhcpLeaseReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record DhcpLeaseReconciliationResult(DhcpLeaseReconciliationOutcome Outcome, string Detail);

public static class DhcpLeaseReconciliation
{
    public static async Task<DhcpLeaseReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<ToolResult>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(DhcpLeaseReconciliationOutcome.NoPending, "No pending DHCP lease renewal was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return Result(DhcpLeaseReconciliationOutcome.Inconclusive, "Pending DHCP evidence is unavailable or invalid; the guard remains active."); }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.DhcpLease))
            return Result(DhcpLeaseReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.DhcpLease))
            return Result(DhcpLeaseReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        if (journal.Steps is not { Count: > 0 } || journal.Steps[0] is not { Step: { } step, Result: { } result } ||
            step.Verification || step.Tool != DhcpRecovery.Step(false).Tool || !step.Arguments.SequenceEqual(DhcpRecovery.Step(false).Arguments) ||
            DhcpRecovery.Read(result) is not { Targets.Length: > 0 } before)
            return Result(DhcpLeaseReconciliationOutcome.Inconclusive,
                "The interrupted renewal has no complete retained target snapshot; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId || currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(DhcpLeaseReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted renewal can be reconciled; the guard remains active.");
        ToolResult currentResult;
        try
        {
            currentResult = await (inspect(token) ?? throw new InvalidOperationException("DHCP provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(45), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { return Result(DhcpLeaseReconciliationOutcome.Inconclusive, "Current DHCP evidence is unavailable; the guard remains active."); }
        var current = DhcpRecovery.Read(currentResult);
        if (current is null || before.Targets.Any(id => !before.Identities.TryGetValue(id, out var priorIdentity) ||
            !current.Identities.TryGetValue(id, out var currentIdentity) || priorIdentity != currentIdentity || !current.Adapters.ContainsKey(id)))
            return Result(DhcpLeaseReconciliationOutcome.Inconclusive,
                "A target adapter is missing, changed or unavailable; the guard remains active.");
        if (before.Targets.All(id => DhcpRecovery.TargetResolved(before, id, current)))
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(DhcpLeaseReconciliationOutcome.Verified,
                "The interrupted renewal is reconciled. Every exact target now satisfies its retained address or lease-expiry condition, and no lease was renewed again.");
        }
        await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
        return Result(DhcpLeaseReconciliationOutcome.NotApplied,
            "The interrupted renewal is reconciled. At least one exact target still fails its retained address or lease-expiry condition, so renewal was not verified and no lease was renewed again in this run.");
    }

    private static DhcpLeaseReconciliationResult Result(DhcpLeaseReconciliationOutcome outcome, string detail) => new(outcome, detail);
}
