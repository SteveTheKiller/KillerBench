using System.Text.Json;

namespace KillerBench.Engine;

public enum DeliveryOptimizationReconciliationOutcome { NoPending, Verified, NotApplied, Inconclusive }
public sealed record DeliveryOptimizationReconciliationResult(DeliveryOptimizationReconciliationOutcome Outcome, string Detail);

public static class DeliveryOptimizationRecovery
{
    public const string ServiceName = "DoSvc";

    public static RepairStep ContinueStep() => new("sc.exe", ["continue", ServiceName], false);
    public static RepairStep VerifyStep() => DeliveryOptimizationDiagnostics.Step();

    public static bool CanContinue(WorkflowCheck check) => check is
    {
        State: CheckState.Repairable,
        Repair: RepairKind.DeliveryOptimization,
        Evidence: { } evidence
    } && ServiceRecovery.MatchesService(evidence, ServiceName) && ServiceRecovery.State(evidence) == 7;

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress = null,
        CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        execute ??= (command, cancellationToken) => NativeTool.RunAsync(command.Tool, command.Arguments,
            progress: progress, token: cancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            if (step.Verification)
            {
                if (step.Tool != "sc.exe" || !step.Arguments.SequenceEqual(VerifyStep().Arguments))
                    throw new ArgumentException("Unsupported Delivery Optimization verification command.", nameof(step));
                return await ServiceRecovery.ObserveRunningAsync(ServiceName,
                    cancellationToken => execute(VerifyStep(), cancellationToken), deadline.Token).ConfigureAwait(false);
            }
            if (step.Tool != "sc.exe" || !step.Arguments.SequenceEqual(ContinueStep().Arguments))
                throw new ArgumentException("Unsupported Delivery Optimization recovery command.", nameof(step));
            progress?.Report("Rechecking Delivery Optimization before continuing it...");
            var current = await execute(VerifyStep(), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Delivery Optimization state provider returned no evidence.");
            if (IsRunning(current))
                throw new RepairStepSkippedException("Delivery Optimization is already running. No continue request was sent.", current);
            if (!CanContinue(DeliveryOptimizationDiagnostics.Interpret(current)))
                throw new RepairDispatchDeferredException(
                    "Delivery Optimization is no longer confirmed paused. No continue request was sent.");
            return await execute(ContinueStep(), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Delivery Optimization recovery provider returned no evidence.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(-1, "", "Delivery Optimization recovery timed out. Current state is unverified.", false) { TimedOut = true };
        }
    }

    private static bool IsRunning(ToolResult evidence) => evidence.ExitCode == 0 && !evidence.TimedOut &&
        !evidence.Truncated && string.IsNullOrWhiteSpace(evidence.Error) &&
        ServiceRecovery.MatchesService(evidence, ServiceName) && ServiceRecovery.State(evidence) == 4;

    public static async Task<DeliveryOptimizationReconciliationResult> ReconcileAsync(RepairRecoveryStore store,
        Func<CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(inspect);
        var pending = await store.ReadAsync(token).ConfigureAwait(false);
        if (pending is not { Pending: true }) return Result(DeliveryOptimizationReconciliationOutcome.NoPending,
            "No pending Delivery Optimization recovery was found.");
        RepairResult? journal;
        try
        {
            var info = new FileInfo(pending.JournalPath);
            if (!info.Exists || info.Length is <= 0 or > 1_048_576) throw new InvalidDataException();
            await using var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous);
            journal = await JsonSerializer.DeserializeAsync<RepairResult>(stream, Capture.JsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return Result(DeliveryOptimizationReconciliationOutcome.Inconclusive,
                "Pending Delivery Optimization evidence is unavailable or invalid; the guard remains active.");
        }
        if (!RepairRecoveryStore.IsRelatedJournal(pending, journal, RepairKind.DeliveryOptimization))
            return Result(DeliveryOptimizationReconciliationOutcome.NoPending, "The pending record belongs to another repair.");
        if (!RepairRecoveryStore.MatchesPendingJournal(pending, journal, RepairKind.DeliveryOptimization))
            return Result(DeliveryOptimizationReconciliationOutcome.Inconclusive,
                "The pending record does not match this machine, repair or journal; the guard remains active.");
        var currentBoot = await store.ReadBootStampAsync(token).ConfigureAwait(false);
        if (pending.Boot is null || currentBoot is null || currentBoot.RecordId == pending.Boot.RecordId ||
            currentBoot.StartedUtc <= pending.Boot.StartedUtc)
            return Result(DeliveryOptimizationReconciliationOutcome.Inconclusive,
                "A later Windows boot is required before the interrupted continue request can be reconciled; the guard remains active.");
        WorkflowCheck current;
        try
        {
            current = await (inspect(token) ?? throw new InvalidOperationException("Delivery Optimization provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(45), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        {
            return Result(DeliveryOptimizationReconciliationOutcome.Inconclusive,
                "Current Delivery Optimization evidence is unavailable; the guard remains active.");
        }
        if (current.Evidence is not { } evidence || !ServiceRecovery.MatchesService(evidence, ServiceName))
            return Result(DeliveryOptimizationReconciliationOutcome.Inconclusive,
                "Current evidence does not identify Delivery Optimization; the guard remains active.");
        int? state = ServiceRecovery.State(evidence);
        if (state == 4)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(DeliveryOptimizationReconciliationOutcome.Verified,
                "The interrupted continue request is reconciled. Delivery Optimization is running, and another request was not sent.");
        }
        if (state is 1 or 7)
        {
            await store.ResolvePendingAsync(pending.JournalPath, token).ConfigureAwait(false);
            return Result(DeliveryOptimizationReconciliationOutcome.NotApplied,
                "The interrupted continue request is reconciled without a retry. Delivery Optimization is not running; collect fresh evidence before another recovery.");
        }
        return Result(DeliveryOptimizationReconciliationOutcome.Inconclusive,
            "Delivery Optimization is transitioning or otherwise unverified; the guard remains active.");
    }

    private static DeliveryOptimizationReconciliationResult Result(DeliveryOptimizationReconciliationOutcome outcome,
        string detail) => new(outcome, detail);
}
