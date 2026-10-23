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

    private sealed class FailingJournal(RecipeJournalStage failureStage) : IRecipeJournal
    {
        public Task RecordAsync(RecipeJournalEntry entry, CancellationToken token)
        {
            if (entry.Stage == failureStage) throw new IOException("Simulated journal failure.");
            return Task.CompletedTask;
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
        ValidationBounds();
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
        var detailed = Expectation.Evaluate(ExpectSpec.ClearWhenEquals("Running"), new PrimitiveResult(true, "Running", "") { Detail = "Service is running." });
        if (detailed.Detail != "Service is running.") throw new Exception("Primitive detail was not carried into the evaluation.");
        var clauseDetail = Expectation.Evaluate(new ExpectSpec([new ExpectClause("any", "", ProbeVerdict.Clear) { Detail = "Clause wins." }]), new PrimitiveResult(true, "x", "") { Detail = "Primitive." });
        if (clauseDetail.Detail != "Clause wins.") throw new Exception("Clause detail did not take precedence.");
        var noMatch = Expectation.Evaluate(new ExpectSpec([new ExpectClause("equals", "Running", ProbeVerdict.Clear)]), new PrimitiveResult(true, "Stopped", ""));
        if (noMatch.Verdict != ProbeVerdict.Unknown) throw new Exception("Unmatched value was not Unknown.");
        if (!Expectation.Matches(new ExpectClause("less-than", "10", ProbeVerdict.Clear), "9.5") || Expectation.Matches(new ExpectClause("less-than", "10", ProbeVerdict.Clear), "x"))
            throw new Exception("Numeric comparison misbehaved.");
        if (!Expectation.Matches(new ExpectClause("absent", "", ProbeVerdict.Clear), null) || Expectation.Matches(new ExpectClause("equals", "", ProbeVerdict.Clear), null))
            throw new Exception("Absent handling misbehaved.");

        // Options: declarations, references, resolution, substitution and runner gating.
        await OptionsAsync(library);

        // Runner: clear, unauthorized, blocked, recheck cleared, repaired, unverified, not dispatched, timeout, canceled, elevation.
        await RunnerAsync();
        Console.WriteLine("PASS: recipe pack schema, validation, strict JSON, expectation evaluation and runner paths; fixtures only.");
    }

    internal static void ValidationBounds()
    {
        var recipe = ServiceRecipe(withAction: false) with
        {
            Options = [new RecipeOption("target", "Target to inspect") { Default = new string('a', 513), Pattern = "a+" }]
        };
        var result = RecipePackValidation.Validate(Pack(recipe));
        if (result.IsValid || !result.Issues.Any(issue => issue.Message.Contains("default", StringComparison.OrdinalIgnoreCase)))
            throw new Exception("An option default beyond the runtime length bound was accepted by pack validation.");
        recipe = recipe with { Options = [recipe.Options[0] with { Default = new string('a', 64) + "!", Pattern = "(a+)+" }] };
        result = RecipePackValidation.Validate(Pack(recipe));
        if (result.IsValid || !result.Issues.Any(issue => issue.Message.Contains("time", StringComparison.OrdinalIgnoreCase)))
            throw new Exception("An expensive valid option pattern must produce a controlled pack validation issue.");
        recipe = recipe with { Options = [recipe.Options[0] with { Default = null }] };
        var problems = RecipeOptions.Resolve(recipe, new Dictionary<string, string> { ["target"] = new string('a', 64) + "!" }, out var values);
        if (!problems.Any(problem => problem.Contains("time", StringComparison.OrdinalIgnoreCase)) || values.Count != 0)
            throw new Exception("An expensive caller-supplied option must remain unresolved with a controlled problem.");
        foreach (int length in new[] { 511, 512, 513 })
        {
            recipe = recipe with { Options = [recipe.Options[0] with { Default = new string('a', length), Pattern = "a+" }] };
            result = RecipePackValidation.Validate(Pack(recipe));
            if (result.IsValid != (length <= 512)) throw new Exception("Pack default length boundary was incorrect.");
            problems = RecipeOptions.Resolve(recipe, null, out values);
            if ((problems.Count == 0) != (length <= 512)) throw new Exception("Runtime option length boundary was incorrect.");
        }
        Console.WriteLine("PASS: pack and runtime options retain controlled timeout issues and default length bounds.");
    }

    private static async Task OptionsAsync(PrimitiveLibrary library)
    {
        var option = new RecipeOption("target", "Service to check") { Pattern = "[A-Za-z0-9_-]+" };
        var recipe = ServiceRecipe() with
        {
            Id = "test.option-service",
            Options = [option],
            RequiresExplicitOptions = true,
            Detect = new ProbeSpec("service.state", new Dictionary<string, string> { ["name"] = "${target}" }, ExpectSpec.ClearWhenEquals("Running")),
            Preconditions = [],
            Action = new ActionSpec("service.start", new Dictionary<string, string> { ["name"] = "${target}" }),
            Verify = null
        };
        var packValidation = RecipePackValidation.Validate(Pack(recipe), library);
        if (!packValidation.IsValid) throw new Exception("Recipe with options reported issues: " + string.Join("; ", packValidation.Issues.Select(i => i.Message)));
        if (!RecipeOptions.References(recipe).SetEquals(["target"])) throw new Exception("Option references were not detected.");
        Check(Pack(recipe with { Options = [option with { Name = "Bad Name" }] }), "short lower-case", "Bad option name");
        Check(Pack(recipe with { Options = [option, option] }), "Duplicate option", "Duplicate option");
        Check(Pack(recipe with { Options = [option with { Pattern = "(" }] }), "invalid pattern", "Invalid option pattern");
        Check(Pack(recipe with { Options = [option with { Default = "bad name!" }] }), "default that does not match", "Default outside pattern");
        Check(Pack(recipe with { Options = [] }), "no matching option declaration", "Undeclared reference");
        Check(Pack(recipe with { RequiresExplicitOptions = false }), "must declare RequiresExplicitOptions", "Required option without explicit flag");
        if (!RecipePackValidation.Validate(Pack(recipe with { RequiresExplicitOptions = false, Options = [option with { Default = "Spooler" }] }), library).IsValid)
            throw new Exception("An option with a default was rejected without the explicit flag.");

        var problems = RecipeOptions.Resolve(recipe, null, out _);
        if (problems.Count != 1 || !problems[0].Contains("required")) throw new Exception("Missing required option was not reported.");
        if (RecipeOptions.Resolve(recipe, new Dictionary<string, string> { ["target"] = "bad name!" }, out _).Count != 1) throw new Exception("Option pattern was not enforced at run time.");
        if (RecipeOptions.Resolve(recipe, new Dictionary<string, string> { ["target"] = "Spooler", ["extra"] = "x" }, out _).Count != 1) throw new Exception("Unknown option was accepted.");
        if (RecipeOptions.Resolve(recipe, new Dictionary<string, string> { ["target"] = "Spooler" }, out var values).Count != 0 || values["target"] != "Spooler") throw new Exception("Valid option did not resolve.");
        var applied = RecipeOptions.Apply(recipe, values);
        if (applied.Detect.Parameters["name"] != "Spooler" || applied.Action!.Parameters["name"] != "Spooler") throw new Exception("Option substitution did not reach the parameters.");
        if (recipe.Detect.Parameters["name"] != "${target}") throw new Exception("Substitution mutated the original recipe.");
        var defaulted = RecipeOptions.Resolve(recipe with { Options = [option with { Default = "Spooler", Required = false }] }, null, out var defaults);
        if (defaulted.Count != 0 || defaults["target"] != "Spooler") throw new Exception("Option default was not applied.");

        var seen = new List<string>();
        var start = new FakeAction("service.start", parameters => { seen.Add(parameters["name"]); return new ActionResult(true, true, "ok"); });
        string state = "Stopped";
        var runner = new RecipeRunner(new PrimitiveLibrary("Test")
            .AddProbe(new FakeProbe("service.state", parameters => new PrimitiveResult(true, parameters["name"] == "Spooler" ? state : "Running", "")))
            .AddAction(start), new MemoryRecipeJournal(), new MemoryRecipeOperationStore());
        var needsOptions = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (needsOptions.Outcome != RecipeRunOutcome.RepairAvailable || needsOptions.Probes.Count != 0) throw new Exception("Missing options did not stop before probing: " + needsOptions.Outcome);
        var badOptions = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Options = new Dictionary<string, string> { ["target"] = "bad name!" }, ExplicitOptionsConfirmed = true });
        if (badOptions.Outcome != RecipeRunOutcome.Blocked || badOptions.Probes.Count != 0) throw new Exception("Invalid options did not block before probing.");
        var unconfirmed = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Options = new Dictionary<string, string> { ["target"] = "Spooler" } });
        if (unconfirmed.Outcome != RecipeRunOutcome.RepairAvailable || seen.Count != 0) throw new Exception("Unconfirmed explicit recipe dispatched.");
        state = "Stopped";
        var confirmed = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Options = new Dictionary<string, string> { ["target"] = "Spooler" }, ExplicitOptionsConfirmed = true });
        if (confirmed.Outcome != RecipeRunOutcome.NeedsAttention || seen.SequenceEqual(["Spooler"]) == false) throw new Exception("Confirmed explicit recipe did not dispatch with the substituted target: " + confirmed.Outcome + " " + confirmed.Detail);

        static void Check(RecipePack broken, string fragment, string what)
        {
            var result = RecipePackValidation.Validate(broken);
            if (!result.Issues.Any(issue => issue.Message.Contains(fragment, StringComparison.Ordinal)))
                throw new Exception(what + " was accepted. Issues: " + string.Join("; ", result.Issues.Select(i => i.Message)));
        }
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
        var runner = new RecipeRunner(library, journal, new MemoryRecipeOperationStore());
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
        var flappingRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(flapping).AddProbe(registeredProbe).AddAction(start), journal, new MemoryRecipeOperationStore());
        var recheckCleared = await flappingRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (recheckCleared.Outcome != RecipeRunOutcome.Clear || start.Calls != 0) throw new Exception("Condition clearing before dispatch still dispatched.");

        journal = new MemoryRecipeJournal();
        runner = new RecipeRunner(library, journal, new MemoryRecipeOperationStore());
        serviceState = "Stopped";
        var repaired = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (repaired.Outcome != RecipeRunOutcome.Repaired || start.Calls != 1 || repaired.ExecutionUncertain) throw new Exception("Repair was not verified.");
        var stages = journal.Entries.Select(entry => entry.Stage).ToArray();
        RecipeJournalStage[] expectedStages = [RecipeJournalStage.Detected, RecipeJournalStage.PreconditionsChecked, RecipeJournalStage.Prepared, RecipeJournalStage.Dispatched, RecipeJournalStage.Verified, RecipeJournalStage.Completed];
        if (!stages.SequenceEqual(expectedStages)) throw new Exception("Journal stages were " + string.Join(",", stages));
        if (Array.IndexOf(stages, RecipeJournalStage.Prepared) > Array.IndexOf(stages, RecipeJournalStage.Dispatched)) throw new Exception("Prepared was not journaled before dispatch.");

        foreach (var failureStage in new[] { RecipeJournalStage.Dispatched, RecipeJournalStage.Verified, RecipeJournalStage.Completed })
        {
            serviceState = "Stopped";
            var actionAfterJournal = new FakeAction("service.start", _ =>
            {
                serviceState = "Running";
                return new ActionResult(true, true, "started");
            });
            var failedJournalRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(actionAfterJournal), new FailingJournal(failureStage), new MemoryRecipeOperationStore());
            var journalFailure = await failedJournalRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
            if (actionAfterJournal.Calls != 1 || journalFailure.Outcome != RecipeRunOutcome.Unknown || !journalFailure.ExecutionUncertain ||
                !journalFailure.JournalFailure || journalFailure.Action is not { Dispatched: true })
                throw new Exception("Post-dispatch journal failure lost action evidence at " + failureStage + ".");
        }
        serviceState = "Stopped";
        var blockedBeforeAction = new FakeAction("service.start", _ => new ActionResult(true, true, "started"));
        var preparedFailureRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(blockedBeforeAction), new FailingJournal(RecipeJournalStage.Prepared), new MemoryRecipeOperationStore());
        try { await preparedFailureRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }); throw new Exception("Prepared journal failure did not stop dispatch."); }
        catch (IOException) { }
        if (blockedBeforeAction.Calls != 0) throw new Exception("Action ran after Prepared journal failure.");

        var stuck = new FakeAction("service.start", _ => new ActionResult(true, true, "requested"));
        var unverifiedRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(stuck), journal, new MemoryRecipeOperationStore());
        serviceState = "Stopped";
        var unverified = await unverifiedRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (unverified.Outcome != RecipeRunOutcome.NeedsAttention || !unverified.ExecutionUncertain || unverified.EffectiveOutcome != RecipeRunOutcome.Unknown)
            throw new Exception("Unverified repair was not reported as uncertain.");

        var declined = new FakeAction("service.start", _ => new ActionResult(false, false, "", "provider declined"));
        var declinedRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(declined), journal, new MemoryRecipeOperationStore());
        var notDispatched = await declinedRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (notDispatched.Outcome != RecipeRunOutcome.Blocked || notDispatched.ExecutionUncertain) throw new Exception("Undispatched action was not reported as blocked.");

        var failedAction = new FakeAction("service.start", _ =>
        {
            serviceState = "Running";
            return new ActionResult(true, false, "partial change", "provider failed");
        });
        var failedRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(failedAction), journal, new MemoryRecipeOperationStore());
        var failedResult = await failedRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (failedResult.Outcome != RecipeRunOutcome.Unknown || !failedResult.ExecutionUncertain || failedResult.Action is not { Succeeded: false })
            throw new Exception("A failed dispatched action was reported as verified.");
        serviceState = "Stopped";

        var timedOutAction = new FakeAction("service.start", _ => new ActionResult(true, false, "", "timeout") { TimedOut = true });
        var timeoutRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(timedOutAction), journal, new MemoryRecipeOperationStore());
        var timedOut = await timeoutRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (timedOut.Outcome != RecipeRunOutcome.Unknown || !timedOut.ExecutionUncertain) throw new Exception("Timed-out action was not reported as uncertain.");

        var restart = new FakeAction("service.start", _ => new ActionResult(true, true, "queued") { RestartRequired = true });
        var restartRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(restart), journal, new MemoryRecipeOperationStore());
        var restartRequired = await restartRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (restartRequired.Outcome != RecipeRunOutcome.RestartRequired) throw new Exception("Restart requirement was not reported.");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var canceledResult = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }, canceled.Token);
        if (canceledResult.Outcome != RecipeRunOutcome.Canceled) throw new Exception("Cancellation was not reported.");

        using var canceledAfterDispatch = new CancellationTokenSource();
        var cancelAfterAction = new FakeAction("service.start", _ =>
        {
            canceledAfterDispatch.Cancel();
            return new ActionResult(true, true, "dispatched");
        });
        var cancelAfterRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(cancelAfterAction), journal, new MemoryRecipeOperationStore());
        var afterDispatch = await cancelAfterRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }, canceledAfterDispatch.Token);
        if (afterDispatch.Outcome != RecipeRunOutcome.Canceled || afterDispatch.EffectiveOutcome != RecipeRunOutcome.Canceled ||
            afterDispatch.Action is not { Dispatched: true } || !afterDispatch.ExecutionUncertain)
            throw new Exception("Cancellation after dispatch lost the action or uncertainty.");

        using var canceledDuringDispatch = new CancellationTokenSource();
        var cancelInsideAction = new FakeAction("service.start", _ =>
        {
            canceledDuringDispatch.Cancel();
            throw new OperationCanceledException(canceledDuringDispatch.Token);
        });
        var cancelInsideRunner = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(stateProbe).AddProbe(registeredProbe).AddAction(cancelInsideAction), journal, new MemoryRecipeOperationStore());
        var duringDispatch = await cancelInsideRunner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }, canceledDuringDispatch.Token);
        if (duringDispatch.Outcome != RecipeRunOutcome.Canceled || !duringDispatch.ExecutionUncertain || duringDispatch.Action is not null)
            throw new Exception("Cancellation during dispatch was not marked uncertain.");

        var missingPrimitive = new RecipeRunner(new PrimitiveLibrary("Test").AddProbe(registeredProbe), journal, new MemoryRecipeOperationStore());
        try { await missingPrimitive.RunAsync(recipe, new RecipeRunOptions()); throw new Exception("Missing primitive did not fail."); }
        catch (InvalidOperationException) { }
    }
}
