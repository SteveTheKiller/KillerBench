using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class SymptomTriageChecks
{
    private sealed class FakeProbe(string name, Func<string, string?> value) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("name", true, "target")];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string? observed = value(parameters["name"]);
            return Task.FromResult(observed is null ? PrimitiveResult.Unavailable("no evidence") : new PrimitiveResult(true, observed, "{}"));
        }
    }

    private sealed class FakeAction(Action<string> execute) : IActionPrimitive
    {
        public string Name => "service.start";
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("name", true, "target")];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        {
            execute(parameters["name"]);
            return Task.FromResult(new ActionResult(true, true, "ok"));
        }
    }

    private static RecipeDefinition Recipe(string id, string service) =>
        new(id, 1, id, "Test", "running state", new ProbeSpec("service.check", new Dictionary<string, string> { ["name"] = service }, StateExpect()))
        {
            Action = new ActionSpec("service.start", new Dictionary<string, string> { ["name"] = service }),
            Verify = new ProbeSpec("service.running", new Dictionary<string, string> { ["name"] = service }, ExpectSpec.ClearWhenEquals("Running", ProbeVerdict.Attention)),
            Risk = RecipeRisk.Reversible
        };

    private static ExpectSpec StateExpect() => new(
    [
        new ExpectClause("equals", "Clear", ProbeVerdict.Clear), new ExpectClause("equals", "Repairable", ProbeVerdict.Repairable),
        new ExpectClause("equals", "Attention", ProbeVerdict.Attention), new ExpectClause("any", "", ProbeVerdict.Unknown)
    ]);

    private static TriageTree Chain(string id, params string[] recipeIds)
    {
        var nodes = new List<TriageNode>();
        for (int i = 0; i < recipeIds.Length; i++)
        {
            string next = i + 1 < recipeIds.Length ? "r" + (i + 2) : "handoff";
            nodes.Add(new TriageNode("r" + (i + 1), TriageNodeKind.Recipe)
            {
                RecipeId = recipeIds[i],
                Next = new Dictionary<string, string> { ["Clear"] = next, ["Repaired"] = next, ["RepairAvailable"] = "authorize", ["default"] = next }
            });
        }
        nodes.Add(new TriageNode("authorize", TriageNodeKind.Terminal) { Terminal = TriageTerminal.NeedsHuman, Guidance = "Authorize repairs." });
        nodes.Add(new TriageNode("handoff", TriageNodeKind.Terminal) { Terminal = TriageTerminal.NeedsHuman, Guidance = "Hand off." });
        return new TriageTree(id, "r1", nodes);
    }

    internal static async Task RunAsync()
    {
        var pack = new RecipePack(1, "test.pack", 1, "Test", "Test", [Recipe("test.alpha", "Alpha"), Recipe("test.beta", "Beta")]);
        var tree = Chain("test.symptom", "test.alpha", "test.beta");
        var catalog = new SymptomCatalog(1, "test.catalog", 1, "Test", "Test",
            [new Symptom("test.symptom", "Thing is broken", "Test", "Thing does not work.") { Aliases = ["thing broken", "broken thing"], TreeId = "test.symptom", Guidance = "Call someone." },
             new Symptom("test.other", "Other problem", "Test", "Another thing.") { Guidance = "No triage yet." }],
            [tree]);
        var library = new PrimitiveLibrary("Test").AddProbe(new FakeProbe("service.check", _ => "Clear")).AddProbe(new FakeProbe("service.running", _ => "Running")).AddAction(new FakeAction(_ => { }));

        // Validation.
        var valid = SymptomCatalogValidation.Validate(catalog, [pack], library);
        if (!valid.IsValid) throw new Exception("Valid catalog reported issues: " + string.Join("; ", valid.Issues.Select(i => i.Subject + ": " + i.Message)));
        Expect(catalog with { Symptoms = [catalog.Symptoms[0] with { TreeId = "test.missing" }] }, "does not exist", "Missing tree");
        Expect(catalog with { Trees = [tree with { RootNodeId = "nope" }] }, "Root node", "Missing root");
        Expect(catalog with { Trees = [tree with { Nodes = [.. tree.Nodes.Append(new TriageNode("orphan", TriageNodeKind.Terminal) { Terminal = TriageTerminal.RuledOut })] }] }, "unreachable", "Unreachable node");
        var loop = tree.Nodes.Select(node => node.Id == "r2" ? node with { Next = new Dictionary<string, string> { ["Clear"] = "r1", ["default"] = "handoff" } } : node).ToArray();
        Expect(catalog with { Trees = [tree with { Nodes = loop }] }, "Cycle", "Cycle");
        var badKey = tree.Nodes.Select(node => node.Id == "r1" ? node with { Next = new Dictionary<string, string> { ["Fixed"] = "r2", ["default"] = "r2" } } : node).ToArray();
        Expect(catalog with { Trees = [tree with { Nodes = badKey }] }, "not a recipe outcome", "Bad branch key");
        var dangling = tree.Nodes.Select(node => node.Id == "r1" ? node with { Next = new Dictionary<string, string> { ["default"] = "ghost" } } : node).ToArray();
        Expect(catalog with { Trees = [tree with { Nodes = dangling }] }, "missing node", "Dangling branch");
        var unknownRecipe = tree.Nodes.Select(node => node.Id == "r1" ? node with { RecipeId = "test.unknown" } : node).ToArray();
        Expect(catalog with { Trees = [tree with { Nodes = unknownRecipe }] }, "not in any supplied pack", "Unknown recipe", [pack]);
        var askNoBranch = new TriageTree("test.ask", "q", [
            new TriageNode("q", TriageNodeKind.Ask) { Question = new TriageQuestion("Which?", [new TriageOption("a", "A"), new TriageOption("b", "B")]), Next = new Dictionary<string, string> { ["a"] = "end" } },
            new TriageNode("end", TriageNodeKind.Terminal) { Terminal = TriageTerminal.RuledOut }]);
        Expect(catalog with { Trees = [tree, askNoBranch] }, "has no branch", "Ask option without branch");
        Expect(catalog with { Symptoms = [catalog.Symptoms[1] with { Guidance = "" }] }, "needs guidance", "Symptom without tree or guidance");

        string json = SymptomCatalogJson.Serialize(catalog);
        if (SymptomCatalogJson.Serialize(SymptomCatalogJson.Deserialize(json)) != json) throw new Exception("Catalog did not round-trip through JSON.");
        if (catalog.Search("thing broken") is not [{ Id: "test.symptom" }, ..] || catalog.Search("zzz").Count != 0) throw new Exception("Symptom search misbehaved.");

        // Goal runner walks: rule out alpha, repair beta, resolved by verified repair.
        var states = new Dictionary<string, string> { ["Alpha"] = "Clear", ["Beta"] = "Repairable" };
        var runningNow = new HashSet<string>();
        var live = new PrimitiveLibrary("Test")
            .AddProbe(new FakeProbe("service.check", name => states[name]))
            .AddProbe(new FakeProbe("service.running", name => runningNow.Contains(name) ? "Running" : "NotRunning"))
            .AddAction(new FakeAction(name => { runningNow.Add(name); states[name] = "Clear"; }));
        var runner = new GoalRunner(catalog, [pack], live, new MemoryRecipeJournal());
        var unauthorized = await runner.RunAsync("test.symptom", new GoalRunOptions());
        if (unauthorized.Outcome != GoalOutcome.NeedsHuman || unauthorized.RuledOut.SequenceEqual(["test.alpha"]) == false || unauthorized.Unresolved.SequenceEqual(["test.beta"]) == false || runningNow.Count != 0)
            throw new Exception("Unauthorized triage did not stop at the authorize hand-off: " + unauthorized.Outcome + " " + unauthorized.Detail);
        if (unauthorized.Guidance != "Authorize repairs. Call someone.") throw new Exception("Terminal and symptom guidance were not combined: " + unauthorized.Guidance);
        var resolved = await runner.RunAsync("test.symptom", new GoalRunOptions { AuthorizeRepair = true });
        if (resolved.Outcome != GoalOutcome.Resolved || !resolved.Repaired.SequenceEqual(["test.beta"]) || !resolved.RuledOut.SequenceEqual(["test.alpha"]) || resolved.Steps.Count != 3)
            throw new Exception("Authorized triage did not resolve: " + resolved.Outcome + " " + resolved.Detail);
        var again = await runner.RunAsync("test.symptom", new GoalRunOptions { AuthorizeRepair = true });
        if (again.Outcome != GoalOutcome.NeedsHuman || again.RuledOut.Count != 2 || again.Repaired.Count != 0) throw new Exception("All-clear walk did not hand off with everything ruled out.");
        var budget = await runner.RunAsync("test.symptom", new GoalRunOptions { AuthorizeRepair = true, MaximumSteps = 1 });
        if (budget.Outcome != GoalOutcome.BudgetExhausted) throw new Exception("Step budget was not enforced.");
        var noTriage = await runner.RunAsync("test.other", new GoalRunOptions());
        if (noTriage.Outcome != GoalOutcome.NoTriage || noTriage.Guidance != "No triage yet.") throw new Exception("Symptom without a tree did not report NoTriage.");
        var repairCap = new GoalRunner(catalog, [pack], new PrimitiveLibrary("Test")
            .AddProbe(new FakeProbe("service.check", _ => "Repairable")).AddProbe(new FakeProbe("service.running", _ => "Running")).AddAction(new FakeAction(_ => { })), new MemoryRecipeJournal());
        var capped = await repairCap.RunAsync("test.symptom", new GoalRunOptions { AuthorizeRepair = true, MaximumRepairs = 1 });
        if (capped.Repaired.Count != 1 || capped.Outcome != GoalOutcome.NeedsHuman) throw new Exception("Repair cap was not enforced: " + capped.Outcome + " repaired " + capped.Repaired.Count);

        // Confirm probe and questions.
        var confirmed = catalog with { Symptoms = [catalog.Symptoms[0] with { Confirm = new ProbeSpec("service.check", new Dictionary<string, string> { ["name"] = "Symptom" }, StateExpect()) }, catalog.Symptoms[1]] };
        var confirmStates = new Dictionary<string, string> { ["Symptom"] = "Clear", ["Alpha"] = "Repairable", ["Beta"] = "Clear" };
        var confirmLibrary = new PrimitiveLibrary("Test").AddProbe(new FakeProbe("service.check", name => confirmStates[name])).AddProbe(new FakeProbe("service.running", _ => "Running")).AddAction(new FakeAction(name => confirmStates[name] = "Clear"));
        var confirmRunner = new GoalRunner(confirmed, [pack], confirmLibrary, new MemoryRecipeJournal());
        var absent = await confirmRunner.RunAsync("test.symptom", new GoalRunOptions { AuthorizeRepair = true });
        if (absent.Outcome != GoalOutcome.Resolved || absent.Steps.Count != 1) throw new Exception("Absent symptom still ran triage.");
        confirmStates["Symptom"] = "Repairable";
        var stillPresent = await confirmRunner.RunAsync("test.symptom", new GoalRunOptions { AuthorizeRepair = true });
        if (stillPresent.Outcome != GoalOutcome.NeedsHuman || stillPresent.Repaired.Count != 1 || !stillPresent.Steps.Any(step => step.Summary.StartsWith("Symptom recheck after repair")))
            throw new Exception("Repair with a still-present symptom was reported as resolved: " + stillPresent.Outcome);
        var askTree = new TriageTree("test.ask", "q", [
            new TriageNode("q", TriageNodeKind.Ask) { Question = new TriageQuestion("Which?", [new TriageOption("a", "A"), new TriageOption("b", "B")]), Next = new Dictionary<string, string> { ["a"] = "yes", ["b"] = "no" } },
            new TriageNode("yes", TriageNodeKind.Terminal) { Terminal = TriageTerminal.OutOfScope, Guidance = "Vendor." },
            new TriageNode("no", TriageNodeKind.Terminal) { Terminal = TriageTerminal.RuledOut }]);
        var asking = new SymptomCatalog(1, "test.ask", 1, "Test", "Ask", [new Symptom("test.asked", "Asked", "Test", "Asks.") { TreeId = "test.ask" }], [askTree]);
        if (!SymptomCatalogValidation.Validate(asking, [pack], library).IsValid) throw new Exception("Ask catalog was rejected.");
        var askRunner = new GoalRunner(asking, [pack], library, new MemoryRecipeJournal());
        var unanswered = await askRunner.RunAsync("test.asked", new GoalRunOptions());
        if (unanswered.Outcome != GoalOutcome.NeedsHuman || unanswered.OpenQuestions.Count != 1) throw new Exception("Unanswered question was not surfaced.");
        var answered = await askRunner.RunAsync("test.asked", new GoalRunOptions { Answer = (_, _, _) => Task.FromResult<string?>("a") });
        if (answered.Outcome != GoalOutcome.OutOfScope || !answered.Guidance.StartsWith("Vendor.", StringComparison.Ordinal)) throw new Exception("Answered question did not branch.");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        if ((await runner.RunAsync("test.symptom", new GoalRunOptions(), canceled.Token)).Outcome != GoalOutcome.Canceled) throw new Exception("Cancellation was not reported.");

        // Built-in Windows catalog against the built-in pack and library.
        var windows = WindowsSymptomCatalog.Build();
        var windowsValidation = SymptomCatalogValidation.Validate(windows, WindowsPacks.All(), WindowsPrimitiveLibrary.Create());
        if (!windowsValidation.IsValid) throw new Exception("Windows symptom catalog is invalid: " + string.Join("; ", windowsValidation.Issues.Select(i => i.Subject + ": " + i.Message)));
        if (windows.Symptoms.Count < 15 || windows.Symptoms.Count(symptom => symptom.TreeId is not null) < 14) throw new Exception("Windows symptom catalog is incomplete.");
        if (windows.Search("no sound") is not [{ Id: "audio.no-sound" }, ..] || windows.Search("can't print") is not [{ Id: "printing.nothing-prints" }, ..])
            throw new Exception("Windows symptom search did not rank the expected symptom first.");
        if (SymptomCatalogJson.Serialize(SymptomCatalogJson.Deserialize(SymptomCatalogJson.Serialize(windows))) != SymptomCatalogJson.Serialize(windows)) throw new Exception("Windows catalog did not round-trip.");

        Console.WriteLine("PASS: symptom catalog validation, JSON, search, goal runner walks with budgets, confirmation, questions and cancellation, and the built-in Windows catalog of " + windows.Symptoms.Count + " symptoms; fixtures only.");

        static void Expect(SymptomCatalog broken, string fragment, string what, IReadOnlyList<RecipePack>? packs = null)
        {
            var result = SymptomCatalogValidation.Validate(broken, packs, null);
            if (!result.Issues.Any(issue => issue.Message.Contains(fragment, StringComparison.Ordinal)))
                throw new Exception(what + " was accepted. Issues: " + string.Join("; ", result.Issues.Select(i => i.Message)));
        }
    }
}
