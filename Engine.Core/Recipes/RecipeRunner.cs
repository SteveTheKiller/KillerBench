namespace KillerBench.Engine.Core.Recipes;

public sealed record ProbeOutcome(string Role, ProbeSpec Probe, PrimitiveResult Result, Expectation.Evaluation Evaluation);

public sealed record RecipeRunResult(
    string RecipeId,
    int RecipeVersion,
    RecipeRunOutcome Outcome,
    string Detail,
    IReadOnlyList<ProbeOutcome> Probes)
{
    public ActionResult? Action { get; init; }
    /// <summary>True when a change was dispatched but verification did not confirm the expected state.</summary>
    public bool ExecutionUncertain { get; init; }
    /// <summary>True when a journal write failed after action dispatch; callers must stop further repairs.</summary>
    public bool JournalFailure { get; init; }
    public RecipeRunOutcome EffectiveOutcome => Outcome == RecipeRunOutcome.Canceled ? Outcome : ExecutionUncertain ? RecipeRunOutcome.Unknown : Outcome;
}

public sealed record RecipeRunOptions
{
    /// <summary>Authorizes the recipe's action. Without it the runner only detects and reports RepairAvailable.</summary>
    public bool AuthorizeRepair { get; init; }
    /// <summary>Whether the current session is elevated. The runner never elevates on its own.</summary>
    public bool Elevated { get; init; }
    public IProgress<string>? Progress { get; init; }
    /// <summary>Values for the recipe's declared options. Unknown names are rejected before any probe runs.</summary>
    public IReadOnlyDictionary<string, string>? Options { get; init; }
    /// <summary>Confirms a recipe that declares RequiresExplicitOptions. Without it such a recipe only reports RepairAvailable.</summary>
    public bool ExplicitOptionsConfirmed { get; init; }
    /// <summary>When the current boot began. Defaults to the system uptime; fixtures supply a fixed value.</summary>
    public Func<DateTimeOffset>? BootTimeUtc { get; init; }
}

/// <summary>
/// Runs one declarative recipe: detect, then when authorized check preconditions, re-detect immediately before the change,
/// journal, dispatch the action once, verify and report. It never retries and fails closed on incomplete evidence.
/// </summary>
public sealed class RecipeRunner(PrimitiveLibrary library, IRecipeJournal journal, IRecipeOperationStore? operationStore = null)
{
    private readonly PrimitiveLibrary library = library ?? throw new ArgumentNullException(nameof(library));
    private readonly IRecipeJournal journal = journal ?? throw new ArgumentNullException(nameof(journal));
    private readonly IRecipeOperationStore operationStore = operationStore ?? FileRecipeOperationStore.ForCurrentUser();

    public async Task<RecipeRunResult> RunAsync(RecipeDefinition recipe, RecipeRunOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(options);
        var probes = new List<ProbeOutcome>();
        RecipeRunResult Result(RecipeRunOutcome outcome, string detail, ActionResult? action = null, bool uncertain = false, bool journalFailure = false) =>
            new(recipe.Id, recipe.Version, outcome, detail, probes.AsReadOnly()) { Action = action, ExecutionUncertain = uncertain, JournalFailure = journalFailure };
        async Task<bool> RecordAfterDispatchAsync(RecipeJournalStage stage, string detail, string? raw)
        {
            try { await RecordAsync(recipe, stage, detail, raw).ConfigureAwait(false); return true; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
        }

        var problems = RecipeOptions.Resolve(recipe, options.Options, out var optionValues);
        if (problems.Count > 0)
        {
            if (!recipe.RequiresExplicitOptions || options.ExplicitOptionsConfirmed || options.Options is { Count: > 0 })
                return Result(RecipeRunOutcome.Blocked, "Options are incomplete or invalid: " + string.Join(" ", problems) + " Nothing was inspected or changed.");
            return Result(RecipeRunOutcome.RepairAvailable, "This recipe needs options before it can run: " + string.Join(" ", problems));
        }
        recipe = RecipeOptions.Apply(recipe, optionValues);
        ActionResult? dispatchedAction = null;
        bool dispatchAttempted = false;
        IRecipeOperationLease? operation = null;
        bool verifiedOperation = false;

        try
        {
            options.Progress?.Report(recipe.Title + ": checking current state...");
            var detect = await ProbeAsync("detect", recipe.Detect, recipe.InspectionTimeout, token).ConfigureAwait(false);
            probes.Add(detect);
            await RecordAsync(recipe, RecipeJournalStage.Detected, detect.Evaluation.Verdict + ": " + detect.Evaluation.Detail, detect.Result.Raw).ConfigureAwait(false);
            if (recipe.Action is not null && await operationStore.IsPendingAsync(RecipeOperationIdentity.FromResolvedRecipe(recipe), token).ConfigureAwait(false))
            {
                using var restart = await operationStore.TryOpenRestartRecoveryAsync(RecipeOperationIdentity.FromResolvedRecipe(recipe), token).ConfigureAwait(false);
                if (restart is null)
                    return Result(RecipeRunOutcome.Unknown, "A prior operation for this recipe is unresolved. Current detection is read-only evidence and cannot establish that the earlier operation has stopped. No new repair was dispatched; operation-specific recovery review is required.", uncertain: true);
                // Uptime-derived boot times drift by well under a second within one boot.
                if (BootTime(options) - restart.RestartRequiredBootUtc < TimeSpan.FromSeconds(30))
                    return Result(RecipeRunOutcome.Unknown, "The earlier repair is waiting for a restart. Restart Windows, then run this again; KillerBench verifies it without repeating it.", uncertain: true);
                var afterRestart = await ProbeAsync("verify", recipe.Verify ?? recipe.Detect, recipe.InspectionTimeout, token).ConfigureAwait(false);
                probes.Add(afterRestart);
                if (afterRestart.Evaluation.Verdict is not (ProbeVerdict.Clear or ProbeVerdict.Information))
                    return Result(RecipeRunOutcome.NeedsAttention, "Windows restarted, but verification did not confirm the earlier repair: " + afterRestart.Evaluation.Detail +
                        " The action was not repeated and the record stays open; review before retrying.", uncertain: true);
                await RecordAsync(recipe, RecipeJournalStage.Verified, "Verified after the restart without repeating the action: " + afterRestart.Evaluation.Detail, afterRestart.Result.Raw).ConfigureAwait(false);
                await restart.CompleteAsync(token).ConfigureAwait(false);
                return Result(RecipeRunOutcome.Repaired, "Verified after the restart: " + recipe.VerificationScope + ". The action was not repeated. " + recipe.Limitations);
            }
            switch (detect.Evaluation.Verdict)
            {
                case ProbeVerdict.Clear:
                case ProbeVerdict.Information:
                    return Result(RecipeRunOutcome.Clear, detect.Evaluation.Detail + " " + recipe.Limitations);
                case ProbeVerdict.Unknown:
                    return Result(RecipeRunOutcome.Unknown, detect.Evaluation.Detail + " No repair selected.");
                case ProbeVerdict.Attention:
                    return Result(RecipeRunOutcome.NeedsAttention, detect.Evaluation.Detail + " No automatic repair applies.");
            }

            if (recipe.Action is null)
                return Result(RecipeRunOutcome.NeedsAttention, detect.Evaluation.Detail + " This recipe is diagnostic only.");
            if (!options.AuthorizeRepair)
                return Result(RecipeRunOutcome.RepairAvailable, detect.Evaluation.Detail + " Repair available: " + Describe(recipe.Action));
            if (recipe.RequiresExplicitOptions && !options.ExplicitOptionsConfirmed)
                return Result(RecipeRunOutcome.RepairAvailable, detect.Evaluation.Detail + " This repair requires explicit confirmation" +
                    (recipe.Options.Count == 0 ? "." : " and options: " + string.Join(", ", recipe.Options.Select(option => option.Name + (option.Required ? "" : " (optional)"))) + "."));
            if (recipe.RequiresElevation && !options.Elevated)
                return Result(RecipeRunOutcome.Blocked, "Administrator access is required for this repair. No change was made.");
            var action = library.FindAction(recipe.Action.Primitive)
                ?? throw new InvalidOperationException("Action primitive " + recipe.Action.Primitive + " is not available on " + library.Platform + ".");

            operation = await operationStore.TryBeginAsync(RecipeOperationIdentity.FromResolvedRecipe(recipe), token).ConfigureAwait(false);
            if (operation is null)
                return Result(RecipeRunOutcome.Unknown, "A previous operation for this recipe is active or unresolved. No repair was dispatched. Read-only inspection does not clear this barrier; operation-specific recovery review is required.", uncertain: true);

            foreach (var precondition in recipe.Preconditions)
            {
                token.ThrowIfCancellationRequested();
                var outcome = await ProbeAsync("precondition", precondition, recipe.InspectionTimeout, token).ConfigureAwait(false);
                probes.Add(outcome);
                if (outcome.Evaluation.Verdict is not (ProbeVerdict.Clear or ProbeVerdict.Information))
                {
                    await RecordAsync(recipe, RecipeJournalStage.PreconditionsChecked, "Blocked by " + (precondition.Name ?? precondition.Primitive) + ": " + outcome.Evaluation.Detail, outcome.Result.Raw).ConfigureAwait(false);
                    return Result(RecipeRunOutcome.Blocked, "Prerequisite " + (precondition.Name ?? precondition.Primitive) + " is not satisfied: " + outcome.Evaluation.Detail + " No change was made.");
                }
            }
            await RecordAsync(recipe, RecipeJournalStage.PreconditionsChecked, recipe.Preconditions.Count + " prerequisite(s) satisfied.", null).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();
            var recheck = await ProbeAsync("recheck", recipe.Detect, recipe.InspectionTimeout, token).ConfigureAwait(false);
            probes.Add(recheck);
            if (recheck.Evaluation.Verdict != ProbeVerdict.Repairable)
            {
                await RecordAsync(recipe, RecipeJournalStage.Completed, "Skipped before dispatch: " + recheck.Evaluation.Detail, recheck.Result.Raw).ConfigureAwait(false);
                return recheck.Evaluation.Verdict is ProbeVerdict.Clear or ProbeVerdict.Information
                    ? Result(RecipeRunOutcome.Clear, "The condition cleared before the repair ran. No change was made.")
                    : Result(RecipeRunOutcome.Unknown, "Fresh evidence no longer authorizes the repair: " + recheck.Evaluation.Detail + " No change was made.");
            }

            await RecordAsync(recipe, RecipeJournalStage.Prepared, "Dispatching " + Describe(recipe.Action), null).ConfigureAwait(false);
            options.Progress?.Report(recipe.Title + ": " + Describe(recipe.Action));
            ActionResult actionResult;
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(recipe.Action.Timeout > recipe.ExecutionTimeout ? recipe.ExecutionTimeout : recipe.Action.Timeout);
                try
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    dispatchAttempted = true;
                    actionResult = await action.ExecuteAsync(recipe.Action.Parameters, recipe.Action.Timeout, options.Progress, deadline.Token).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("The action primitive returned no result.");
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    actionResult = new ActionResult(true, false, "", "The action exceeded its time limit.") { TimedOut = true };
                }
            }
            dispatchedAction = actionResult;
            if (!await RecordAfterDispatchAsync(RecipeJournalStage.Dispatched,
                (actionResult.Dispatched ? "Dispatched" : "Not dispatched") + (actionResult.Succeeded ? ", reported success." : ", reported failure. ") + (actionResult.Error ?? ""),
                actionResult.Raw).ConfigureAwait(false))
                return Result(RecipeRunOutcome.Unknown, "The journal could not be saved after the action. Stop further repairs and inspect the current state before retrying.", actionResult, uncertain: true, journalFailure: true);
            if (!actionResult.Dispatched)
                return Result(RecipeRunOutcome.Blocked, "The repair was not dispatched: " + (actionResult.Error ?? "the provider declined.") + " No change was made.", actionResult);
            if (actionResult.TimedOut)
                return Result(RecipeRunOutcome.Unknown, "The repair timed out; the change may still be in progress. Verification was not attempted.", actionResult, uncertain: true);
            if (!actionResult.Succeeded)
                return Result(RecipeRunOutcome.Unknown, "The repair reported a failure; its final state is uncertain. Review the current state before retrying: " + (actionResult.Error ?? "no failure detail was provided."), actionResult, uncertain: true);
            if (actionResult.RestartRequired)
            {
                // The barrier stays; recording the boot is what lets a run after the restart verify instead of staying blocked.
                try { await operation.RecordRestartRequiredAsync(BootTime(options), CancellationToken.None).ConfigureAwait(false); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    return Result(RecipeRunOutcome.RestartRequired, "The repair requires a restart, but its recovery record could not be updated. Restart Windows, then review this repair before running it again.", actionResult);
                }
                return Result(RecipeRunOutcome.RestartRequired, "The repair requires a restart before it can be verified. Restart Windows, then run this again; KillerBench verifies it without repeating it.", actionResult);
            }

            token.ThrowIfCancellationRequested();
            var verify = await ProbeAsync("verify", recipe.Verify ?? recipe.Detect, recipe.InspectionTimeout, token).ConfigureAwait(false);
            probes.Add(verify);
            if (!await RecordAfterDispatchAsync(RecipeJournalStage.Verified, verify.Evaluation.Verdict + ": " + verify.Evaluation.Detail, verify.Result.Raw).ConfigureAwait(false))
                return Result(RecipeRunOutcome.Unknown, "The journal could not be saved after the action. Stop further repairs and inspect the current state before retrying.", actionResult, uncertain: true, journalFailure: true);
            if (verify.Evaluation.Verdict is ProbeVerdict.Clear or ProbeVerdict.Information)
            {
                if (!await RecordAfterDispatchAsync(RecipeJournalStage.Completed, "Repaired and verified within scope: " + recipe.VerificationScope, null).ConfigureAwait(false))
                    return Result(RecipeRunOutcome.Unknown, "The journal could not be saved after the action. Stop further repairs and inspect the current state before retrying.", actionResult, uncertain: true, journalFailure: true);
                verifiedOperation = true;
                return Result(RecipeRunOutcome.Repaired, "Verified: " + recipe.VerificationScope + ". " + recipe.Limitations, actionResult);
            }
            return Result(RecipeRunOutcome.NeedsAttention,
                "The repair ran but verification did not confirm the expected state: " + verify.Evaluation.Detail + " Review before retrying.",
                actionResult, uncertain: true);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            bool uncertain = dispatchAttempted && dispatchedAction is not { Dispatched: false };
            return Result(RecipeRunOutcome.Canceled, "Canceled. Review the journal before retrying; a dispatched change may have completed.", dispatchedAction, uncertain);
        }
        finally
        {
            if (operation is not null)
            {
                try
                {
                    if (!dispatchAttempted || dispatchedAction is { Dispatched: false } || verifiedOperation)
                        await operation.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
                }
                finally { operation.Dispose(); }
            }
        }
    }

    private static DateTimeOffset BootTime(RecipeRunOptions options) =>
        options.BootTimeUtc?.Invoke() ?? DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);

    private async Task<ProbeOutcome> ProbeAsync(string role, ProbeSpec probe, TimeSpan timeout, CancellationToken token)
    {
        var primitive = library.FindProbe(probe.Primitive)
            ?? throw new InvalidOperationException("Probe primitive " + probe.Primitive + " is not available on " + library.Platform + ".");
        PrimitiveResult result;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(timeout);
        try
        {
            result = await primitive.ObserveAsync(probe.Parameters, timeout, deadline.Token).ConfigureAwait(false)
                ?? PrimitiveResult.Unavailable("The probe primitive returned no result.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            result = new PrimitiveResult(false, null, "", "The probe exceeded its time limit.") { TimedOut = true };
        }
        return new(role, probe, result, Expectation.Evaluate(probe.Expect, result));
    }

    private Task RecordAsync(RecipeDefinition recipe, RecipeJournalStage stage, string detail, string? raw) =>
        journal.RecordAsync(new(DateTimeOffset.UtcNow, recipe.Id, recipe.Version, stage, detail) { Raw = raw }, CancellationToken.None);

    private static string Describe(ActionSpec action) =>
        action.Primitive + (action.Parameters.Count == 0 ? "" : " (" + string.Join(", ", action.Parameters.Select(pair => pair.Key + "=" + pair.Value)) + ")");
}
