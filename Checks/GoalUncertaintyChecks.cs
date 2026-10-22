using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;
using KillerBench.Engine.Core.Symptoms;

internal static class GoalUncertaintyChecks
{
    internal static async Task RunAsync()
    {
        var dispatched = new List<string>();
        var library = new PrimitiveLibrary("Fixture").AddProbe(new StateProbe()).AddAction(new RepairAction(dispatched));
        var first = Recipe("fixture.first", "first");
        var second = Recipe("fixture.second", "second");
        var tree = new TriageTree("fixture.tree", "first", [
            new TriageNode("first", TriageNodeKind.Recipe) { RecipeId = first.Id, Next = new Dictionary<string, string> { ["default"] = "second" } },
            new TriageNode("second", TriageNodeKind.Recipe) { RecipeId = second.Id, Next = new Dictionary<string, string> { ["default"] = "end" } },
            new TriageNode("end", TriageNodeKind.Terminal) { Terminal = TriageTerminal.NeedsHuman }
        ]);
        var catalog = new SymptomCatalog(1, "fixture.catalog", 1, "Fixture", "Fixture",
            [new Symptom("fixture.symptom", "Fixture", "Test", "Fixture") { TreeId = tree.Id }], [tree]);
        var runner = new GoalRunner(catalog, [new RecipePack(1, "fixture.pack", 1, "Fixture", "Fixture", [first, second])], library,
            new MemoryRecipeJournal(), new MemoryRecipeOperationStore());
        var report = await runner.RunAsync("fixture.symptom", new GoalRunOptions { AuthorizeRepair = true });
        if (report.Outcome != GoalOutcome.Unresolved || !dispatched.SequenceEqual(["first"]) ||
            report.Steps is not [{ Recipe: { ExecutionUncertain: true, Action: { TimedOut: true } } }] ||
            !report.Unresolved.SequenceEqual([first.Id]))
            throw new Exception("An uncertain first repair did not stop the symptom: dispatched " + string.Join(",", dispatched) + "; outcome " + report.Outcome);
        var pending = await runner.RunAsync("fixture.symptom", new GoalRunOptions { AuthorizeRepair = true });
        if (pending.Outcome != GoalOutcome.Unresolved || !dispatched.SequenceEqual(["first"]) ||
            pending.Steps is not [{ Recipe: { ExecutionUncertain: true, Action: null } }] ||
            !pending.Detail.Contains("pending operation", StringComparison.Ordinal))
            throw new Exception("A pending earlier operation advanced triage or lost recovery guidance.");

        foreach (string scenario in new[] { "failed-verification", "failed-action", "canceled", "clear" })
        {
            var actions = new List<string>();
            var repaired = new HashSet<string>(StringComparer.Ordinal);
            var observed = new List<string>();
            using var stop = new CancellationTokenSource();
            var live = new PrimitiveLibrary("Fixture")
                .AddProbe(new StateProbe(target =>
                {
                    observed.Add(target);
                    return target == "first" && scenario == "clear" || repaired.Contains(target) ? "clear" : "repairable";
                }))
                .AddAction(new RepairAction(actions, target =>
                {
                    if (target == "first")
                    {
                        if (scenario == "canceled") stop.Cancel();
                        return new ActionResult(true, scenario != "failed-action", "");
                    }
                    repaired.Add(target);
                    return new ActionResult(true, true, "");
                }));
            var walk = new GoalRunner(catalog, [new RecipePack(1, "fixture.pack", 1, "Fixture", "Fixture", [first, second])], live,
                new MemoryRecipeJournal(), new MemoryRecipeOperationStore());
            var result = await walk.RunAsync("fixture.symptom", new GoalRunOptions { AuthorizeRepair = true }, stop.Token);
            if (scenario == "clear")
            {
                if (result.Outcome != GoalOutcome.RepairedUnconfirmed || !actions.SequenceEqual(["second"]) ||
                    !result.RuledOut.SequenceEqual([first.Id]) || !result.Repaired.SequenceEqual([second.Id]))
                    throw new Exception("A conclusive clear result no longer permits the next repair.");
            }
            else if (result.Outcome != (scenario == "canceled" ? GoalOutcome.Canceled : GoalOutcome.Unresolved) ||
                !actions.SequenceEqual(["first"]) || observed.Contains("second", StringComparer.Ordinal) ||
                result.Steps is not [{ Recipe: { ExecutionUncertain: true, Action: { Dispatched: true } } }] ||
                !result.Unresolved.SequenceEqual([first.Id]))
                throw new Exception("Symptom progression lost pending evidence or continued after " + scenario + ".");
        }
        Console.WriteLine("PASS: symptom triage stops after an uncertain first repair and retains its unresolved evidence; fixtures only.");
    }

    private static RecipeDefinition Recipe(string id, string target) => new(id, 1, id, "Test", "fixture state",
        new ProbeSpec("fixture.state", new Dictionary<string, string> { ["target"] = target }, ExpectSpec.ClearWhenEquals("clear")))
    { Action = new ActionSpec("fixture.repair", new Dictionary<string, string> { ["target"] = target }), Risk = RecipeRisk.Reversible };

    private sealed class StateProbe(Func<string, string>? value = null) : IProbePrimitive
    {
        public string Name => "fixture.state";
        public IReadOnlyList<PrimitiveParameter> Parameters => [new("target", true, "Fixture target")];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(new PrimitiveResult(true, value?.Invoke(parameters["target"]) ?? "repairable", ""));
    }

    private sealed class RepairAction(List<string> dispatched, Func<string, ActionResult>? result = null) : IActionPrimitive
    {
        public string Name => "fixture.repair";
        public IReadOnlyList<PrimitiveParameter> Parameters => [new("target", true, "Fixture target")];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        {
            dispatched.Add(parameters["target"]);
            return Task.FromResult(result?.Invoke(parameters["target"]) ?? new ActionResult(true, false, "") { TimedOut = true });
        }
    }
}
