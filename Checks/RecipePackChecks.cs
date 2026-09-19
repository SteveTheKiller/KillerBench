using System.IO;
using KillerBench.Engine.Core;

internal static class RecipePackChecks
{
    private sealed class FakeProbe(string name, Func<IReadOnlyDictionary<string, string>, PrimitiveResult> observe) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("name", true, "Target name") { Pattern = "[A-Za-z0-9_-]+" }];
        public int Calls;
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
        {
            Calls++;
            token.ThrowIfCancellationRequested();
            return Task.FromResult(observe(parameters));
        }
    }

    private sealed class FakeAction(string name, Func<IReadOnlyDictionary<string, string>, ActionResult> execute, RecipeRisk risk = RecipeRisk.Reversible, bool elevation = false) : IActionPrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("name", true, "Target name") { Pattern = "[A-Za-z0-9_-]+" }];
        public RecipeRisk Risk => risk;
        public bool RequiresElevation => elevation;
        public int Calls;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        {
            Calls++;
            token.ThrowIfCancellationRequested();
            return Task.FromResult(execute(parameters));
        }
    }

    private static Dictionary<string, string> Target(string name = "Spooler") => new() { ["name"] = name };

    private static RecipeDefinition ServiceRecipe(bool withAction = true, bool elevation = false, RecipeRisk risk = RecipeRisk.Reversible) =>
        new("test.service-running", 1, "Test service running", "Test", "Service running state; feature operation excluded",
            new ProbeSpec("service.state", Target(), ExpectSpec.ClearWhenEquals("Running")))
        {
            Preconditions = [new ProbeSpec("service.registered", Target(), ExpectSpec.ClearWhenEquals("true", ProbeVerdict.Attention)) { Name = "Registration" }],
            Action = withAction ? new ActionSpec("service.start", Target()) : null,
            Risk = withAction ? risk : RecipeRisk.ReadOnly,
            RequiresElevation = elevation,
            Limitations = "Feature operation is untested."
        };

    private static RecipePack Pack(params RecipeDefinition[] recipes) => new(1, "test.pack", 1, "Test", "Test pack", recipes);

    internal static async Task RunAsync()
    {
        // Validation without a library.
        var valid = RecipePackValidation.Validate(Pack(ServiceRecipe()));
        if (!valid.IsValid) throw new Exception("Valid pack reported issues: " + string.Join("; ", valid.Issues.Select(i => i.Message)));
        var badId = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Id = "BadId" }));
        if (!badId.Issues.Any(i => i.Message.Contains("lower-case dotted"))) throw new Exception("Bad recipe id was accepted.");
        var duplicate = RecipePackValidation.Validate(Pack(ServiceRecipe(), ServiceRecipe()));
        if (!duplicate.Issues.Any(i => i.Message.Contains("Duplicate"))) throw new Exception("Duplicate id was accepted.");
        var a = ServiceRecipe() with { Id = "test.a", Dependencies = ["test.b"] };
        var b = ServiceRecipe() with { Id = "test.b", Dependencies = ["test.a"] };
        var cycle = RecipePackValidation.Validate(Pack(a, b));
        if (!cycle.Issues.Any(i => i.Message.Contains("cycle"))) throw new Exception("Dependency cycle was accepted.");
        var missingDependency = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Dependencies = ["test.missing"] }));
        if (!missingDependency.Issues.Any(i => i.Message.Contains("not in this pack"))) throw new Exception("Missing dependency was accepted.");
        var riskMismatch = RecipePackValidation.Validate(Pack(ServiceRecipe(withAction: false) with { Risk = RecipeRisk.Persistent }));
        if (!riskMismatch.Issues.Any(i => i.Message.Contains("ReadOnly"))) throw new Exception("Diagnostic-only recipe with risk was accepted.");
        var badRegex = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Detect = new ProbeSpec("service.state", Target(), new ExpectSpec([new ExpectClause("matches", "(", ProbeVerdict.Clear)])) }));
        if (!badRegex.Issues.Any(i => i.Message.Contains("regular expression"))) throw new Exception("Invalid regex was accepted.");
        var badOperator = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Detect = new ProbeSpec("service.state", Target(), new ExpectSpec([new ExpectClause("like", "x", ProbeVerdict.Clear)])) }));
        if (!badOperator.Issues.Any(i => i.Message.Contains("unknown operator"))) throw new Exception("Unknown operator was accepted.");

        // Validation against a library.
        var library = new PrimitiveLibrary("Test")
            .AddProbe(new FakeProbe("service.state", _ => new PrimitiveResult(true, "Running", "{}")))
            .AddProbe(new FakeProbe("service.registered", _ => new PrimitiveResult(true, "true", "{}")))
            .AddAction(new FakeAction("service.start", _ => new ActionResult(true, true, "started")))
            .AddAction(new FakeAction("service.delete", _ => new ActionResult(true, true, "deleted"), RecipeRisk.Destructive, elevation: true));
        if (!RecipePackValidation.Validate(Pack(ServiceRecipe()), library).IsValid) throw new Exception("Library-backed valid pack reported issues.");
        var unknownPrimitive = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Detect = new ProbeSpec("service.missing", Target(), ExpectSpec.ClearWhenEquals("x")) }), library);
        if (!unknownPrimitive.Issues.Any(i => i.Message.Contains("not available"))) throw new Exception("Unknown primitive was accepted.");
        var badParameter = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Detect = new ProbeSpec("service.state", Target("bad name!"), ExpectSpec.ClearWhenEquals("Running")) }), library);
        if (!badParameter.Issues.Any(i => i.Message.Contains("allowed pattern"))) throw new Exception("Parameter pattern violation was accepted.");
        var extraParameter = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Detect = new ProbeSpec("service.state", new Dictionary<string, string> { ["name"] = "x", ["extra"] = "y" }, ExpectSpec.ClearWhenEquals("Running")) }), library);
        if (!extraParameter.Issues.Any(i => i.Message.Contains("not accepted"))) throw new Exception("Unknown parameter was accepted.");
        var riskEscalation = RecipePackValidation.Validate(Pack(ServiceRecipe() with { Action = new ActionSpec("service.delete", Target()) }), library);
        if (!riskEscalation.Issues.Any(i => i.Message.Contains("exceeds")) || !riskEscalation.Issues.Any(i => i.Message.Contains("elevation")))
            throw new Exception("Primitive risk or elevation escalation was accepted.");
        var wrongPlatform = RecipePackValidation.Validate(Pack(ServiceRecipe()) with { Platform = "Other" }, library);
        if (!wrongPlatform.Issues.Any(i => i.Message.Contains("platform"))) throw new Exception("Platform mismatch was accepted.");

        // JSON round trip and strictness.
        var original = Pack(ServiceRecipe() with { Symptoms = ["printing.nothing-prints"], Rollback = new ActionSpec("service.start", Target()) });
        string json = RecipePackJson.Serialize(original);
        var restored = RecipePackJson.Deserialize(json);
        if (RecipePackJson.Serialize(restored) != json || restored.Recipes[0].Symptoms[0] != "printing.nothing-prints" || restored.Recipes[0].Rollback is null)
            throw new Exception("Recipe pack did not round-trip through JSON.");
        try { RecipePackJson.Deserialize(json.Replace("\"Revision\"", "\"Revision\": 1, \"Unknown\"")); throw new Exception("Unknown JSON property was accepted."); }
        catch (InvalidDataException) { }
        try { RecipePackJson.Deserialize("{\"SchemaVersion\":1,\"SchemaVersion\":1}"); throw new Exception("Duplicate JSON property was accepted."); }
        catch (InvalidDataException) { }
        try { RecipePackJson.Deserialize("null"); throw new Exception("Null JSON was accepted."); }
        catch (InvalidDataException) { }

        // Expectation evaluation.
        var incomplete = Expectation.Evaluate(ExpectSpec.ClearWhenEquals("Running"), new PrimitiveResult(false, "Running", "", "denied"));
        if (incomplete.Verdict != ProbeVerdict.Unknown) throw new Exception("Incomplete evidence was not Unknown.");
        var timedOut = Expectation.Evaluate(ExpectSpec.ClearWhenEquals("Running"), new PrimitiveResult(true, "Running", "") { TimedOut = true });
        if (timedOut.Verdict != ProbeVerdict.Unknown) throw new Exception("Timed-out evidence was not Unknown.");
        var noMatch = Expectation.Evaluate(new ExpectSpec([new ExpectClause("equals", "Running", ProbeVerdict.Clear)]), new PrimitiveResult(true, "Stopped", ""));
        if (noMatch.Verdict != ProbeVerdict.Unknown) throw new Exception("Unmatched value was not Unknown.");
        if (!Expectation.Matches(new ExpectClause("less-than", "10", ProbeVerdict.Clear), "9.5") || Expectation.Matches(new ExpectClause("less-than", "10", ProbeVerdict.Clear), "x"))
            throw new Exception("Numeric comparison misbehaved.");
        if (!Expectation.Matches(new ExpectClause("absent", "", ProbeVerdict.Clear), null) || Expectation.Matches(new ExpectClause("equals", "", ProbeVerdict.Clear), null))
            throw new Exception("Absent handling misbehaved.");

        // Runner: clear, unauthorized, blocked, recheck cleared, repaired, unverified, not dispatched, timeout, canceled, elevation.
        await RunnerAsync();
        Console.WriteLine("PASS: recipe pack schema, validation, strict JSON, expectation evaluation and runner paths; fixtures only.");
    }

    private static async Task RunnerAsync()
    {
        string serviceState = "Running";
        string registered = "true";
        var stateProbe = new FakeProbe("service.state", _ => new PrimitiveResult(true, serviceState, "{\"state\":\"" + serviceState + "\"}"));
        var registeredProbe = new FakeProbe("service.registered", _ => new PrimitiveResult(true, registered, "{}"));
        ActionResult actionResult = new(true, true, "started");
        var start = new FakeAction("service.start", _ => { serviceState = "Running"; return actionResult; });
        var library = new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(start);
        var journal = new MemoryRecipeJournal();
        var runner = new RecipeRunner(library, journal);
        var recipe = ServiceRecipe();

        var clear = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (clear.Outcome != RecipeRunOutcome.Clear || start.Calls != 0) throw new Exception("Clear recipe ran an action.");

        serviceState = "Stopped";
        var unauthorized = await runner.RunAsync(recipe, new RecipeRunOptions());
        if (unauthorized.Outcome != RecipeRunOutcome.RepairAvailable || start.Calls != 0) throw new Exception("Unauthorized repair was dispatched.");

        var diagnosticOnly = await runner.RunAsync(ServiceRecipe(withAction: false), new RecipeRunOptions { AuthorizeRepair = true });
        if (diagnosticOnly.Outcome != RecipeRunOutcome.NeedsAttention || start.Calls != 0) throw new Exception("Diagnostic-only recipe did not stay review-only.");

        var elevationRequired = await runner.RunAsync(ServiceRecipe(elevation: true), new RecipeRunOptions { AuthorizeRepair = true, Elevated = false });
        if (elevationRequired.Outcome != RecipeRunOutcome.Blocked || start.Calls != 0) throw new Exception("Missing elevation did not block.");

        registered = "false";
        var blocked = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (blocked.Outcome != RecipeRunOutcome.Blocked || start.Calls != 0 || !blocked.Detail.Contains("Registration")) throw new Exception("Failed precondition did not block.");
        registered = "true";

        int calls = 0;
        var flapping = new FakeProbe("service.state", _ => new PrimitiveResult(true, ++calls == 1 ? "Stopped" : "Running", "{}"));
        var flappingRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(flapping).AddProbe(registeredProbe).AddAction(start), journal);
        var recheckCleared = await flappingRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (recheckCleared.Outcome != RecipeRunOutcome.Clear || start.Calls != 0) throw new Exception("Condition clearing before dispatch still dispatched.");

        journal = new MemoryRecipeJournal();
        runner = new RecipeRunner(library, journal);
        serviceState = "Stopped";
        var repaired = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (repaired.Outcome != RecipeRunOutcome.Repaired || start.Calls != 1 || repaired.ExecutionUncertain) throw new Exception("Repair was not verified.");
        var stages = journal.Entries.Select(entry => entry.Stage).ToArray();
        RecipeJournalStage[] expectedStages = [RecipeJournalStage.Detected, RecipeJournalStage.PreconditionsChecked, RecipeJournalStage.Prepared, RecipeJournalStage.Dispatched, RecipeJournalStage.Verified, RecipeJournalStage.Completed];
        if (!stages.SequenceEqual(expectedStages)) throw new Exception("Journal stages were " + string.Join(",", stages));
        if (Array.IndexOf(stages, RecipeJournalStage.Prepared) > Array.IndexOf(stages, RecipeJournalStage.Dispatched)) throw new Exception("Prepared was not journaled before dispatch.");

        var stuck = new FakeAction("service.start", _ => new ActionResult(true, true, "requested"));
        var unverifiedRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(stuck), journal);
        serviceState = "Stopped";
        var unverified = await unverifiedRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (unverified.Outcome != RecipeRunOutcome.NeedsAttention || !unverified.ExecutionUncertain || unverified.EffectiveOutcome != RecipeRunOutcome.Unknown)
            throw new Exception("Unverified repair was not reported as uncertain.");

        var declined = new FakeAction("service.start", _ => new ActionResult(false, false, "", "provider declined"));
        var declinedRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(declined), journal);
        var notDispatched = await declinedRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (notDispatched.Outcome != RecipeRunOutcome.Blocked || notDispatched.ExecutionUncertain) throw new Exception("Undispatched action was not reported as blocked.");

        var timedOutAction = new FakeAction("service.start", _ => new ActionResult(true, false, "", "timeout") { TimedOut = true });
        var timeoutRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(timedOutAction), journal);
        var timedOut = await timeoutRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (timedOut.Outcome != RecipeRunOutcome.Unknown || !timedOut.ExecutionUncertain) throw new Exception("Timed-out action was not reported as uncertain.");

        var restart = new FakeAction("service.start", _ => new ActionResult(true, true, "queued") { RestartRequired = true });
        var restartRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(restart), journal);
        var restartRequired = await restartRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (restartRequired.Outcome != RecipeRunOutcome.RestartRequired) throw new Exception("Restart requirement was not reported.");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var canceledResult = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }, canceled.Token);
        if (canceledResult.Outcome != RecipeRunOutcome.Canceled) throw new Exception("Cancellation was not reported.");

        var missingPrimitive = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(registeredProbe), journal);
        try { await missingPrimitive.RunAsync(recipe, new RecipeRunOptions()); throw new Exception("Missing primitive did not fail."); }
        catch (InvalidOperationException) { }
    }
}
