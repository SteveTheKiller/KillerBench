namespace KillerBench.Engine;

public sealed record ServicingRepairExecutionResult(WorkflowCheck Eligibility, RepairResult? Repair)
{
    public bool Executed => Repair is not null;

    public int GetExitCode(bool canceled = false) =>
        canceled ? 130 : Repair?.GetExitCode() ?? 3;
}

public sealed class RepairDispatchDeferredException(string message) : InvalidOperationException(message);

public interface IServicingRepairCommandHost
{
    Task<WorkflowCheck> CheckAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token);
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token);
}

public static class ServicingRepairExecution
{
    public static bool IsEligible(RepairKind kind, WorkflowCheck? eligibility)
    {
        if (kind is not (RepairKind.ComponentStore or RepairKind.SystemFiles))
            throw new ArgumentOutOfRangeException(nameof(kind), "Only DISM and SFC servicing repairs are supported.");
        if (eligibility is null || eligibility.Evidence is not { } evidence) return false;
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated ||
            !string.IsNullOrWhiteSpace(evidence.Error) || eligibility.EffectiveState != CheckState.Repairable ||
            eligibility.Repair != kind) return false;
        var interpreted = Workflows.Interpret(kind, evidence);
        return interpreted.EffectiveState == CheckState.Repairable && interpreted.Repair == kind;
    }

    public static async Task<ServicingRepairExecutionResult> RunAsync(RepairKind kind, string folder,
        IWorkflowHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        if (kind is not (RepairKind.ComponentStore or RepairKind.SystemFiles))
            throw new ArgumentOutOfRangeException(nameof(kind), "Only DISM and SFC servicing repairs are supported.");
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        token.ThrowIfCancellationRequested();

        progress?.Report("Collecting fresh servicing evidence before repair...");
        var eligibility = await host.CheckIntegrityAsync(kind, progress, token).ConfigureAwait(false)
            ?? throw new InvalidDataException("The servicing provider returned no eligibility result.");
        token.ThrowIfCancellationRequested();
        if (!IsEligible(kind, eligibility))
            return new(NormalizeIneligible(kind, eligibility), null);

        progress?.Report("Fresh evidence confirmed the selected servicing repair.");
        var repair = await host.RepairAsync(kind, folder, progress, token).ConfigureAwait(false)
            ?? throw new InvalidDataException("The servicing provider returned no repair result.");
        if (repair.Kind != kind)
            throw new InvalidDataException("The servicing provider returned a result for a different repair.");
        return new(eligibility, repair);
    }

    public static async Task<ToolResult> RunCommandAsync(RepairKind kind, RepairStep step,
        IServicingRepairCommandHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        if (kind is not (RepairKind.ComponentStore or RepairKind.SystemFiles))
            throw new ArgumentOutOfRangeException(nameof(kind), "Only DISM and SFC servicing repairs are supported.");
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        var expected = Repairs.Plan(kind).Steps.Single(candidate => !candidate.Verification);
        if (step.Verification || step.Tool != expected.Tool || !step.Arguments.SequenceEqual(expected.Arguments))
            throw new ArgumentException("Command does not match the selected servicing repair.", nameof(step));
        token.ThrowIfCancellationRequested();

        progress?.Report("Repeating exact servicing eligibility at command dispatch...");
        var eligibility = await host.CheckAsync(kind, progress, token).ConfigureAwait(false)
            ?? throw new InvalidDataException("The servicing provider returned no dispatch eligibility result.");
        token.ThrowIfCancellationRequested();
        var normalized = NormalizeIneligible(kind, eligibility);
        if (normalized is { EffectiveState: CheckState.Clear, Repair: null, Evidence: { } clearEvidence })
            throw new RepairStepSkippedException("Fresh servicing evidence reports that the selected repair is no longer required. No repair command was run.", clearEvidence);
        if (!IsEligible(kind, eligibility))
            throw new RepairDispatchDeferredException("Fresh servicing evidence no longer authorizes this repair. " + normalized.Detail);

        progress?.Report("Fresh dispatch evidence confirmed the selected servicing repair.");
        return await host.RunAsync(step, progress, token).ConfigureAwait(false)
            ?? throw new InvalidDataException("The servicing command provider returned no result.");
    }

    private static WorkflowCheck NormalizeIneligible(RepairKind kind, WorkflowCheck eligibility)
    {
        if (eligibility.Evidence is not { } evidence) return eligibility with { Repair = null };
        var interpreted = Workflows.Interpret(kind, evidence);
        if (eligibility.EffectiveState == CheckState.Repairable && eligibility.Repair != kind)
            return new(kind.ToString(), CheckState.Unknown,
                "The servicing provider proposed a different repair than the retained evidence scope. No repair command was run.", Evidence: evidence);
        if (eligibility.EffectiveState == CheckState.Repairable &&
            interpreted is not { EffectiveState: CheckState.Repairable, Repair: not null })
            return interpreted with { Repair = null };
        return eligibility with { Repair = null };
    }
}
