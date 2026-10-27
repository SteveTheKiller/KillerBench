using System.Diagnostics;

namespace KillerBench.Engine.Core.Symptoms;

/// <summary>
/// Resolved means the symptom itself was confirmed absent. RepairedUnconfirmed means a component repair verified but
/// nothing confirmed the original symptom is gone, so the job stays open until someone checks it.
/// </summary>
public enum GoalOutcome { Resolved, Unresolved, RuledOut, NeedsHuman, OutOfScope, Blocked, BudgetExhausted, RestartRequired, Canceled, NoTriage, RepairedUnconfirmed }

/// <summary>One step the runner took while walking a triage tree.</summary>
public sealed record GoalStep(int Index, string NodeId, TriageNodeKind Kind, string Summary)
{
    public RecipeRunResult? Recipe { get; init; }
    public ProbeOutcome? Probe { get; init; }
    public string? Answer { get; init; }
    public string? BranchKey { get; init; }
}

public sealed record GoalReport(string SymptomId, string SymptomTitle, GoalOutcome Outcome, string Detail, IReadOnlyList<GoalStep> Steps)
{
    /// <summary>Recipes whose detection was Clear: their condition is not the cause.</summary>
    public IReadOnlyList<string> RuledOut { get; init; } = [];
    /// <summary>Recipes that dispatched a repair and verified it.</summary>
    public IReadOnlyList<string> Repaired { get; init; } = [];
    /// <summary>Recipes that could not be evaluated or repaired: unknown, blocked, unverified or awaiting authorization.</summary>
    public IReadOnlyList<string> Unresolved { get; init; } = [];
    /// <summary>Questions the runner could not answer because no answer source was supplied.</summary>
    public IReadOnlyList<TriageQuestion> OpenQuestions { get; init; } = [];
    public string Guidance { get; init; } = "";
    public TimeSpan Elapsed { get; init; }

    /// <summary>0 only when the symptom was confirmed absent or every cause was ruled out; 5 for a repair the symptom has not confirmed.</summary>
    public int GetExitCode() => Outcome switch
    {
        GoalOutcome.Resolved or GoalOutcome.RuledOut => 0,
        GoalOutcome.Canceled => 130,
        GoalOutcome.RestartRequired => 4,
        GoalOutcome.RepairedUnconfirmed => 5,
        _ => 3
    };
}

public sealed record GoalRunOptions
{
    public bool AuthorizeRepair { get; init; }
    public bool Elevated { get; init; }
    public int MaximumSteps { get; init; } = 40;
    public int MaximumRepairs { get; init; } = 10;
    public TimeSpan TimeBudget { get; init; } = TimeSpan.FromMinutes(30);
    public IProgress<string>? Progress { get; init; }
    /// <summary>Answers questions by node id or by option key. Null or an unanswered question ends the walk as NeedsHuman with the open question recorded.</summary>
    public Func<TriageNode, TriageQuestion, CancellationToken, Task<string?>>? Answer { get; init; }
    /// <summary>Recipe option values by option name, offered to every recipe that declares that name.</summary>
    public IReadOnlyDictionary<string, string>? RecipeOptions { get; init; }
    /// <summary>Confirms recipes that require explicit options. Applies to every such recipe in the walk, so callers should pair it with a narrow symptom.</summary>
    public bool ExplicitOptionsConfirmed { get; init; }
}

/// <summary>
/// Drives a symptom toward resolution: confirm the symptom, walk its triage tree running recipes and probes, re-confirm after a
/// verified repair, and report what was repaired, ruled out and left unresolved. Budgets bound every walk; the runner never loops a tree.
/// </summary>
public sealed class GoalRunner(SymptomCatalog catalog, IReadOnlyList<RecipePack> packs, PrimitiveLibrary library, IRecipeJournal journal, IRecipeOperationStore? operationStore = null)
{
    private readonly SymptomCatalog catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    private readonly Dictionary<string, RecipeDefinition> recipes = (packs ?? throw new ArgumentNullException(nameof(packs)))
        .SelectMany(pack => pack.Recipes).GroupBy(recipe => recipe.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    private readonly PrimitiveLibrary library = library ?? throw new ArgumentNullException(nameof(library));
    private readonly RecipeRunner recipeRunner = new(library ?? throw new ArgumentNullException(nameof(library)), journal ?? throw new ArgumentNullException(nameof(journal)), operationStore);

    public async Task<GoalReport> RunAsync(string symptomId, GoalRunOptions options, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symptomId);
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaximumSteps < 1 || options.MaximumRepairs < 0 || options.TimeBudget <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options));
        var symptom = catalog.FindSymptom(symptomId) ?? throw new ArgumentException("Unknown symptom '" + symptomId + "'.", nameof(symptomId));
        var watch = Stopwatch.StartNew();
        var steps = new List<GoalStep>();
        var ruledOut = new List<string>();
        var repaired = new List<string>();
        var unresolved = new List<string>();
        var open = new List<TriageQuestion>();
        GoalReport Report(GoalOutcome outcome, string detail, string? guidance = null) => new(symptom.Id, symptom.Title, outcome, detail, steps.AsReadOnly())
        {
            RuledOut = [.. ruledOut.Distinct(StringComparer.Ordinal)], Repaired = [.. repaired], Unresolved = [.. unresolved.Distinct(StringComparer.Ordinal)],
            OpenQuestions = [.. open], Guidance = CombineGuidance(guidance, symptom.Guidance), Elapsed = watch.Elapsed
        };

        if (options.RecipeOptions is { Count: > 0 })
        {
            var declared = new HashSet<string>(StringComparer.Ordinal);
            if (symptom.TreeId is not null && catalog.FindTree(symptom.TreeId) is { } optionTree)
            {
                var optionNodes = optionTree.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
                var pending = new Stack<string>();
                var visited = new HashSet<string>(StringComparer.Ordinal);
                pending.Push(optionTree.RootNodeId);
                while (pending.TryPop(out string? id))
                {
                    if (!visited.Add(id) || !optionNodes.TryGetValue(id, out var node)) continue;
                    if (node.Kind == TriageNodeKind.Recipe && recipes.TryGetValue(node.RecipeId ?? "", out var recipe))
                        declared.UnionWith(recipe.Options.Select(option => option.Name));
                    if (node.Kind != TriageNodeKind.Terminal)
                        foreach (string next in node.Next.Values) pending.Push(next);
                }
            }
            var unknown = options.RecipeOptions.Keys.Where(name => !declared.Contains(name)).Order(StringComparer.Ordinal).ToArray();
            if (unknown.Length > 0)
                return Report(GoalOutcome.Blocked, "Options not declared by any reachable recipe for this symptom: " + string.Join(", ", unknown) + ". Nothing was inspected or changed.");
        }

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(options.TimeBudget);
        var runToken = budget.Token;
        try
        {
            if (symptom.Confirm is not null)
            {
                var confirm = await ProbeAsync(symptom.Confirm, runToken).ConfigureAwait(false);
                steps.Add(new(steps.Count + 1, "confirm", TriageNodeKind.Probe, "Symptom confirmation: " + confirm.Evaluation.Verdict + ". " + confirm.Evaluation.Detail) { Probe = confirm });
                if (confirm.Evaluation.Verdict is ProbeVerdict.Clear or ProbeVerdict.Information)
                    return Report(GoalOutcome.Resolved, "The symptom is not currently present. Nothing was changed.");
            }
            if (symptom.TreeId is null || catalog.FindTree(symptom.TreeId) is not { } tree)
                return Report(GoalOutcome.NoTriage, "No automated triage exists for this symptom yet.");

            var nodes = tree.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            string? current = tree.RootNodeId;
            int repairs = 0;
            while (current is not null)
            {
                runToken.ThrowIfCancellationRequested();
                if (steps.Count >= options.MaximumSteps)
                    return Report(GoalOutcome.BudgetExhausted, "The step budget of " + options.MaximumSteps + " was reached before triage finished.");
                if (!nodes.TryGetValue(current, out var node))
                    return Report(GoalOutcome.Unresolved, "Triage tree node '" + current + "' is missing. The catalog needs repair.");
                int index = steps.Count + 1;
                switch (node.Kind)
                {
                    case TriageNodeKind.Terminal:
                    {
                        steps.Add(new(index, node.Id, node.Kind, "Reached " + node.Terminal + ". " + node.Guidance));
                        if (repaired.Count > 0 && unresolved.Count == 0 && symptom.Confirm is null && node.Terminal is TriageTerminal.NeedsHuman or TriageTerminal.RuledOut)
                            return Report(GoalOutcome.RepairedUnconfirmed, Unconfirmed(repaired), node.Guidance);
                        return node.Terminal switch
                        {
                            TriageTerminal.Resolved => await ConfirmResolvedAsync(symptom, repaired, Report, steps, runToken).ConfigureAwait(false),
                            TriageTerminal.RuledOut => Report(GoalOutcome.RuledOut, "Every automated cause in this tree was ruled out.", node.Guidance),
                            TriageTerminal.OutOfScope => Report(GoalOutcome.OutOfScope, "This path is outside what KillerBench can change.", node.Guidance),
                            _ => Report(GoalOutcome.NeedsHuman, "Triage needs a person to continue.", node.Guidance)
                        };
                    }
                    case TriageNodeKind.Recipe:
                    {
                        if (!recipes.TryGetValue(node.RecipeId ?? "", out var recipe))
                            return Report(GoalOutcome.Unresolved, "Recipe '" + node.RecipeId + "' is not loaded. The catalog and packs disagree.");
                        bool authorize = options.AuthorizeRepair && repairs < options.MaximumRepairs;
                        options.Progress?.Report("Triage step " + index + ": " + recipe.Title);
                        var declared = recipe.Options.Select(option => option.Name).ToHashSet(StringComparer.Ordinal);
                        var supplied = options.RecipeOptions?.Where(pair => declared.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                        var result = await recipeRunner.RunAsync(recipe, new RecipeRunOptions
                        {
                            AuthorizeRepair = authorize, Elevated = options.Elevated, Progress = options.Progress,
                            Options = supplied is { Count: > 0 } ? supplied : null, ExplicitOptionsConfirmed = options.ExplicitOptionsConfirmed
                        }, runToken).ConfigureAwait(false);
                        var outcome = result.EffectiveOutcome;
                        if (result.Action is { Dispatched: true }) repairs++;
                        switch (outcome)
                        {
                            case RecipeRunOutcome.Clear: ruledOut.Add(recipe.Id); break;
                            case RecipeRunOutcome.Repaired: repaired.Add(recipe.Id); break;
                            default: unresolved.Add(recipe.Id); break;
                        }
                        string key = outcome.ToString();
                        string? next = Branch(node, key);
                        steps.Add(new(index, node.Id, node.Kind, recipe.Title + ": " + outcome + ". " + result.Detail) { Recipe = result, BranchKey = next is null ? null : key });
                        if (result.JournalFailure)
                            return Report(GoalOutcome.Unresolved, "The repair journal could not be saved. Stop further repairs and inspect the current state before retrying.");
                        // A budget stop during a dispatched repair keeps its uncertainty; only the caller's cancellation is reported as Canceled.
                        if (outcome == RecipeRunOutcome.Canceled && result.ExecutionUncertain && !token.IsCancellationRequested)
                            return Report(GoalOutcome.Unresolved, "The time budget of " + options.TimeBudget + " was reached while a repair was running; a dispatched change may have completed. No further triage repairs were attempted. Review the pending operation and journal before retrying.");
                        if (outcome == RecipeRunOutcome.Canceled)
                            throw new OperationCanceledException(runToken);
                        if (result.ExecutionUncertain)
                            return Report(GoalOutcome.Unresolved, "A repair is active or its final state is uncertain. No further triage repairs were attempted. Review the pending operation and journal, and use operation-specific recovery before retrying.");
                        if (outcome == RecipeRunOutcome.RestartRequired)
                            return Report(GoalOutcome.RestartRequired, "A repair requires a restart. Run this symptom again after restarting to continue triage.");
                        if (outcome == RecipeRunOutcome.Repaired && symptom.Confirm is not null)
                        {
                            var recheck = await ProbeAsync(symptom.Confirm, runToken).ConfigureAwait(false);
                            steps.Add(new(steps.Count + 1, "confirm", TriageNodeKind.Probe, "Symptom recheck after repair: " + recheck.Evaluation.Verdict + ". " + recheck.Evaluation.Detail) { Probe = recheck });
                            if (recheck.Evaluation.Verdict is ProbeVerdict.Clear or ProbeVerdict.Information)
                                return Report(GoalOutcome.Resolved, "The symptom is no longer present after " + recipe.Title + ".");
                        }
                        if (next is null && !options.AuthorizeRepair && outcome == RecipeRunOutcome.RepairAvailable)
                            return Report(GoalOutcome.Unresolved, "A repair is available but was not authorized: " + recipe.Title + ".");
                        current = next;
                        break;
                    }
                    case TriageNodeKind.Probe:
                    {
                        var probe = await ProbeAsync(node.Probe!, runToken).ConfigureAwait(false);
                        string key = probe.Evaluation.Verdict.ToString();
                        string? next = Branch(node, key);
                        steps.Add(new(index, node.Id, node.Kind, (node.Probe!.Name ?? node.Probe.Primitive) + ": " + key + ". " + probe.Evaluation.Detail) { Probe = probe, BranchKey = next is null ? null : key });
                        current = next;
                        break;
                    }
                    case TriageNodeKind.Ask:
                    {
                        var question = node.Question ?? throw new InvalidOperationException("Ask node '" + node.Id + "' has no question.");
                        string? answer = options.Answer is null ? null : await options.Answer(node, question, runToken).ConfigureAwait(false);
                        if (answer is null || !question.Options.Any(option => option.Key == answer))
                        {
                            open.Add(question);
                            steps.Add(new(index, node.Id, node.Kind, "Question not answered: " + question.Text));
                            return Report(GoalOutcome.NeedsHuman, "Triage needs an answer to continue: " + question.Text, node.Guidance);
                        }
                        string? next = Branch(node, answer);
                        steps.Add(new(index, node.Id, node.Kind, question.Text + " " + answer) { Answer = answer, BranchKey = next is null ? null : answer });
                        current = next;
                        break;
                    }
                    default:
                        return Report(GoalOutcome.Unresolved, "Unknown triage node kind.");
                }
                if (current is null)
                    return Report(GoalOutcome.NeedsHuman, "Triage reached '" + node.Id + "' with no branch for the observed outcome.", node.Guidance);
            }
            return Report(GoalOutcome.Unresolved, "Triage ended without a terminal outcome.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return Report(GoalOutcome.Canceled, "Canceled. Review the journal before retrying; a dispatched change may have completed.");
        }
        catch (OperationCanceledException)
        {
            return Report(GoalOutcome.BudgetExhausted, "The time budget of " + options.TimeBudget + " was reached before triage finished.");
        }
    }

    private static string CombineGuidance(string? terminal, string symptom)
    {
        bool hasTerminal = !string.IsNullOrWhiteSpace(terminal), hasSymptom = !string.IsNullOrWhiteSpace(symptom);
        if (hasTerminal && hasSymptom && terminal != symptom) return terminal!.TrimEnd() + " " + symptom.Trim();
        return hasTerminal ? terminal!.Trim() : symptom?.Trim() ?? "";
    }

    private static string? Branch(TriageNode node, string key) =>
        node.Next.TryGetValue(key, out string? next) ? next : node.Next.TryGetValue("default", out string? fallback) ? fallback : null;

    private static string Unconfirmed(IReadOnlyCollection<string> repaired) =>
        "Verified repairs: " + string.Join(", ", repaired) + ". The original symptom has not been confirmed fixed; check it with the user before closing the job.";

    private async Task<GoalReport> ConfirmResolvedAsync(Symptom symptom, IReadOnlyCollection<string> repaired, Func<GoalOutcome, string, string?, GoalReport> report, List<GoalStep> steps, CancellationToken token)
    {
        if (symptom.Confirm is null)
            return repaired.Count > 0
                ? report(GoalOutcome.RepairedUnconfirmed, Unconfirmed(repaired), null)
                : report(GoalOutcome.Resolved, "Triage reached a resolved outcome.", null);
        var recheck = await ProbeAsync(symptom.Confirm, token).ConfigureAwait(false);
        steps.Add(new(steps.Count + 1, "confirm", TriageNodeKind.Probe, "Symptom recheck: " + recheck.Evaluation.Verdict + ". " + recheck.Evaluation.Detail) { Probe = recheck });
        return recheck.Evaluation.Verdict is ProbeVerdict.Clear or ProbeVerdict.Information
            ? report(GoalOutcome.Resolved, "The symptom is no longer present.", null)
            : report(GoalOutcome.Unresolved, "Triage reached a resolved outcome but the symptom is still present: " + recheck.Evaluation.Detail, null);
    }

    private async Task<ProbeOutcome> ProbeAsync(ProbeSpec probe, CancellationToken token)
    {
        var primitive = library.FindProbe(probe.Primitive) ?? throw new InvalidOperationException("Probe primitive " + probe.Primitive + " is not available on " + library.Platform + ".");
        PrimitiveResult result;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            result = await primitive.ObserveAsync(probe.Parameters, TimeSpan.FromMinutes(5), deadline.Token).ConfigureAwait(false)
                ?? PrimitiveResult.Unavailable("The probe primitive returned no result.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            result = new PrimitiveResult(false, null, "", "The probe exceeded its time limit.") { TimedOut = true };
        }
        return new("triage", probe, result, Expectation.Evaluate(probe.Expect, result));
    }
}
