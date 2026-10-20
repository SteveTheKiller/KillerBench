using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;
using KillerBench.Engine.Core.Symptoms;

internal static class GoalOptionValidationChecks
{
    internal static async Task RunAsync()
    {
        var observed = new List<string>();
        var probe = new FixtureProbe(observed);
        var adapter = new RecipeDefinition("test.adapter", 1, "Adapter", "Test", "fixture", Probe("${adapter}"))
        {
            Options = [new RecipeOption("adapter", "Selected adapter") { Default = "Wi-Fi", Pattern = "[A-Za-z -]+" }]
        };
        var unrelated = adapter with { Id = "test.unrelated", Options = [new RecipeOption("volume", "Selected volume") { Default = "C" }] };
        var tree = new TriageTree("test.tree", "start", [
            new TriageNode("start", TriageNodeKind.Probe) { Probe = Probe("root"), Next = new Dictionary<string, string> { ["default"] = "adapter" } },
            new TriageNode("adapter", TriageNodeKind.Recipe) { RecipeId = adapter.Id, Next = new Dictionary<string, string> { ["default"] = "end" } },
            new TriageNode("end", TriageNodeKind.Terminal) { Terminal = TriageTerminal.RuledOut },
            new TriageNode("unreachable", TriageNodeKind.Recipe) { RecipeId = unrelated.Id }
        ]);
        var catalog = new SymptomCatalog(1, "test.catalog", 1, "Test", "Test",
            [new Symptom("test.symptom", "Symptom", "Test", "Fixture") { TreeId = tree.Id, Confirm = Probe("confirm") }], [tree]);
        var runner = new GoalRunner(catalog, [new RecipePack(1, "test.pack", 1, "Test", "Test", [adapter, unrelated])],
            new PrimitiveLibrary("Test").AddProbe(probe), new MemoryRecipeJournal(), new MemoryRecipeOperationStore());

        var rejected = await runner.RunAsync("test.symptom", Options("adpater", "Other Adapter"));
        if (rejected.Outcome != GoalOutcome.Blocked || rejected.Steps.Count != 0 || observed.Count != 0 || !rejected.Detail.Contains("adpater", StringComparison.Ordinal))
            throw new Exception("Misspelled symptom option was not rejected before every probe: " + rejected.Detail);

        var unrelatedReport = await runner.RunAsync("test.symptom", Options("volume", "D"));
        if (unrelatedReport.Outcome != GoalOutcome.Blocked || unrelatedReport.Steps.Count != 0 || observed.Count != 0)
            throw new Exception("An unrelated or unreachable recipe option was accepted before probing.");

        var valid = await runner.RunAsync("test.symptom", Options("adapter", "Other Adapter"));
        if (valid.Outcome != GoalOutcome.RuledOut || !observed.SequenceEqual(["confirm", "root", "Other Adapter"]))
            throw new Exception("A reachable recipe lost its explicit adapter target.");

        observed.Clear();
        var invalidValue = await runner.RunAsync("test.symptom", Options("adapter", "invalid!"));
        if (!invalidValue.Unresolved.Contains(adapter.Id) || observed.Contains("invalid!", StringComparer.Ordinal) ||
            invalidValue.Steps.All(step => step.Recipe?.EffectiveOutcome != RecipeRunOutcome.Blocked))
            throw new Exception("Recipe-specific option value validation was bypassed.");

        observed.Clear();
        var noTree = catalog with { Symptoms = [catalog.Symptoms[0] with { TreeId = null }] };
        var noTreeReport = await new GoalRunner(noTree, [new RecipePack(1, "test.pack", 1, "Test", "Test", [adapter])],
            new PrimitiveLibrary("Test").AddProbe(probe), new MemoryRecipeJournal(), new MemoryRecipeOperationStore()).RunAsync("test.symptom", Options("adapter", "Other Adapter"));
        if (noTreeReport.Outcome != GoalOutcome.Blocked || observed.Count != 0)
            throw new Exception("A symptom without reachable recipes accepted an unused target before confirmation.");

        Console.WriteLine("PASS: symptom options reject misspelled and unrelated keys before all probes, preserve explicit reachable targets, and retain recipe value validation; fixtures only.");

        static GoalRunOptions Options(string key, string value) => new() { RecipeOptions = new Dictionary<string, string> { [key] = value } };
    }

    private static ProbeSpec Probe(string target) => new("fixture.observe", new Dictionary<string, string> { ["target"] = target },
        ExpectSpec.ClearWhenEquals("clear", ProbeVerdict.Attention));

    private sealed class FixtureProbe(List<string> observed) : IProbePrimitive
    {
        public string Name => "fixture.observe";
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("target", true, "Fixture target")];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
        {
            observed.Add(parameters["target"]);
            return Task.FromResult(new PrimitiveResult(true, parameters["target"] == "confirm" ? "present" : "clear", ""));
        }
    }
}
