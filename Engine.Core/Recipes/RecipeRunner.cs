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
    public RecipeRunOutcome EffectiveOutcome => ExecutionUncertain ? RecipeRunOutcome.Unknown : Outcome;
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
}

/// <summary>
/// Runs one declarative recipe: detect, then when authorized check preconditions, re-detect immediately before the change,
/// journal, dispatch the action once, verify and report. It never retries and fails closed on incomplete evidence.
/// </summary>
public sealed class RecipeRunner(PrimitiveLibrary library, IRecipeJournal journal)
{
    private readonly PrimitiveLibrary library = library ?? throw new ArgumentNullException(nameof(library));
    private readonly IRecipeJournal journal = journal ?? throw new ArgumentNullException(nameof(journal));

    public async Task<RecipeRunResult> RunAsync(RecipeDefinition recipe, RecipeRunOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(options);
        var probes = new List<ProbeOutcome>();
        RecipeRunResult Result(RecipeRunOutcome outcome, string detail, ActionResult? action = null, bool uncertain = false) =>
            new(recipe.Id, recipe.Version, outcome, detail, probes.AsReadOnly()) { Action = action, ExecutionUncertain = uncertain };

        var problems = RecipeOptions.Resolve(recipe, options.Options, out var optionValues);
        if (problems.Count > 0)
        {
            if (!recipe.RequiresExplicitOptions || options.ExplicitOptionsConfirmed || options.Options is { Count: > 0 })
                return Result(RecipeRunOutcome.Blocked, "Options are incomplete or invalid: " + string.Join(" ", problems) + " Nothing was inspected or changed.");
            return Result(RecipeRunOutcome.RepairAvailable, "This recipe needs options before it can run: " + string.Join(" ", problems));
        }
        recipe = RecipeOptions.Apply(recipe, optionValues);

        try
        {
            options.Progress?.Report(recipe.Title + ": checking current state...");
            var detect = await ProbeAsync("detect", recipe.Detect, recipe.InspectionTimeout, token).ConfigureAwait(false);
            probes.Add(detect);
            await RecordAsync(recipe, RecipeJournalStage.Detected, detect.Evaluation.Verdict + ": " + detect.Evaluation.Detail, detect.Result.Raw).ConfigureAwait(false);
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
                    actionResult = await action.ExecuteAsync(recipe.Action.Parameters, recipe.Action.Timeout, options.Progress, deadline.Token).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("The action primitive returned no result.");
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    actionResult = new ActionResult(true, false, "", "The action exceeded its time limit.") { TimedOut = true };
                }
            }
            await RecordAsync(recipe, RecipeJournalStage.Dispatched,
                (actionResult.Dispatched ? "Dispatched" : "Not dispatched") + (actionResult.Succeeded ? ", reported success." : ", reported failure. ") + (actionResult.Error ?? ""),
                actionResult.Raw).ConfigureAwait(false);
            if (!actionResult.Dispatched)
                return Result(RecipeRunOutcome.Blocked, "The repair was not dispatched: " + (actionResult.Error ?? "the provider declined.") + " No change was made.", actionResult);
            if (actionResult.TimedOut)
                return Result(RecipeRunOutcome.Unknown, "The repair timed out; the change may still be in progress. Verification was not attempted.", actionResult, uncertain: true);
            if (actionResult.RestartRequired)
                return Result(RecipeRunOutcome.RestartRequired, "The repair requires a restart before it can be verified.", actionResult);

            token.ThrowIfCancellationRequested();
            var verify = await ProbeAsync("verify", recipe.Verify ?? recipe.Detect, recipe.InspectionTimeout, token).ConfigureAwait(false);
            probes.Add(verify);
            await RecordAsync(recipe, RecipeJournalStage.Verified, verify.Evaluation.Verdict + ": " + verify.Evaluation.Detail, verify.Result.Raw).ConfigureAwait(false);
            if (verify.Evaluation.Verdict is ProbeVerdict.Clear or ProbeVerdict.Information)
            {
                await RecordAsync(recipe, RecipeJournalStage.Completed, "Repaired and verified within scope: " + recipe.VerificationScope, null).ConfigureAwait(false);
                return Result(RecipeRunOutcome.Repaired, "Verified: " + recipe.VerificationScope + ". " + recipe.Limitations, actionResult);
            }
            return Result(RecipeRunOutcome.NeedsAttention,
                "The repair ran but verification did not confirm the expected state: " + verify.Evaluation.Detail + " Review before retrying.",
                actionResult, uncertain: true);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return Result(RecipeRunOutcome.Canceled, "Canceled. Review the journal before retrying; a dispatched change may have completed.");
        }
    }

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
