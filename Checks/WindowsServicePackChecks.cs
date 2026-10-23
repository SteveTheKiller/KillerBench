using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsServicePackChecks
{
    private sealed class FakeProbe(string name, Func<string, PrimitiveResult> observe) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsServicePrimitives.NameParameter];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(observe(parameters["name"]));
    }

    private sealed class FakeStart : IActionPrimitive
    {
        public string Name => "service.start";
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsServicePrimitives.NameParameter];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => true;
        public List<string> Started { get; } = [];
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        {
            Started.Add(parameters["name"]);
            return Task.FromResult(new ActionResult(true, true, "[SC] StartService"));
        }
    }

    internal static async Task RunAsync()
    {
        await WindowsReviewRegressionChecks.ServiceDispatchAsync();
        var sources = WindowsServicePack.SourceRecipes;
        if (sources.Count < 60) throw new Exception("Expected the catalog to hold at least 60 plain service-start recipes; found " + sources.Count + ".");
        if (sources.Any(recipe => recipe.DiagnosticOnly || recipe.RequiresExplicitOptions)) throw new Exception("A diagnostic-only or option-dependent recipe entered the service pack.");
        if (WindowsServicePack.IsServiceStart(RepairKind.ComponentStore) || WindowsServicePack.IsServiceStart(RepairKind.DhcpLease))
            throw new Exception("A non-service repair was classified as a service start.");

        var library = WindowsPrimitiveLibrary.Create();
        var pack = WindowsServicePack.Build();
        var validation = RecipePackValidation.Validate(pack, library);
        if (!validation.IsValid) throw new Exception("Generated service pack is invalid: " + string.Join("; ", validation.Issues.Select(i => i.RecipeId + ": " + i.Message)));
        if (pack.Recipes.Count != sources.Count) throw new Exception("Pack recipe count does not match the catalog.");
        var packIds = new HashSet<string>(pack.Recipes.Select(recipe => recipe.Id), StringComparer.Ordinal);
        foreach (var source in sources)
        {
            var converted = pack.Recipes.Single(recipe => recipe.Id == source.Id);
            if (converted.Version != source.Version || converted.Title != source.Title || converted.VerificationScope != source.VerificationScope || converted.Area != source.Area)
                throw new Exception("Pack recipe " + source.Id + " drifted from the catalog entry.");
            foreach (string dependency in source.Dependencies)
                if (!packIds.Contains(dependency)) throw new Exception("Service recipe " + source.Id + " depends on " + dependency + ", which is not a service recipe.");
            if (!converted.Dependencies.SequenceEqual(source.Dependencies)) throw new Exception("Pack recipe " + source.Id + " lost a dependency.");
            string service = ServiceRecovery.ServiceName(source.Action)!;
            if (converted.Detect.Parameters["name"] != service || converted.Action!.Parameters["name"] != service || converted.Verify!.Parameters["name"] != service)
                throw new Exception("Pack recipe " + source.Id + " targets the wrong service.");
            if (WindowsServicePrimitives.ResolveKind(service) != source.Action) throw new Exception("Service name " + service + " does not resolve back to " + source.Action + ".");
        }
        if (WindowsServicePrimitives.ResolveKind("NoSuchService") is not null || WindowsServicePrimitives.ResolveKind("") is not null)
            throw new Exception("Unknown service names resolved to a catalog kind.");

        string json = RecipePackJson.Serialize(pack);
        var restored = RecipePackJson.Deserialize(json);
        if (RecipePackJson.Serialize(restored) != json || !RecipePackValidation.Validate(restored, library).IsValid)
            throw new Exception("Service pack did not round-trip through JSON.");

        // Primitive value mapping from Engine checks.
        var running = new ToolResult(0, "SERVICE_NAME: Spooler\n        STATE              : 4  RUNNING", "", false);
        var clear = WindowsServicePrimitives.ToPrimitiveResult(new WorkflowCheck("PrintSpooler", CheckState.Clear, "Service is running.", Evidence: running));
        if (clear is not { Complete: true, Value: "Clear" }) throw new Exception("Clear service check did not map to Clear.");
        var repairable = WindowsServicePrimitives.ToPrimitiveResult(new WorkflowCheck("PrintSpooler", CheckState.Repairable, "Stopped.", RepairKind.PrintSpooler, running with { Output = running.Output.Replace("4  RUNNING", "1  STOPPED") }));
        if (repairable is not { Complete: true, Value: "Repairable" }) throw new Exception("Repairable service check did not map to Repairable.");
        var timedOut = WindowsServicePrimitives.ToPrimitiveResult(new WorkflowCheck("PrintSpooler", CheckState.Unknown, "Timed out.", Evidence: new ToolResult(-1, "", "", false) { TimedOut = true }));
        if (timedOut is not { Complete: false, TimedOut: true }) throw new Exception("Timed-out service check did not stay incomplete.");
        var clearWithBadEvidence = WindowsServicePrimitives.ToPrimitiveResult(new WorkflowCheck("PrintSpooler", CheckState.Clear, "Odd.", Evidence: new ToolResult(1, "x", "", false)));
        if (clearWithBadEvidence.Complete) throw new Exception("Clear verdict over failed evidence was accepted as complete.");

        // Substituted-library run of the generated Spooler recipe: detect Repairable, start once, verify Running.
        string state = "Repairable";
        var start = new FakeStart();
        var substituted = new PrimitiveLibrary("Windows")
            .AddProbe(new FakeProbe("service.check", name => new PrimitiveResult(true, name == "Spooler" ? state : "Clear", "{}")))
            .AddProbe(new FakeProbe("service.running", name => new PrimitiveResult(true, start.Started.Contains(name) ? "Running" : "NotRunning", "{}")))
            .AddAction(start);
        var journal = new MemoryRecipeJournal();
        var spooler = pack.Recipes.Single(recipe => recipe.Id == "printing.spooler");
        var runner = new RecipeRunner(substituted, journal, new MemoryRecipeOperationStore());
        var repaired = await runner.RunAsync(spooler, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (repaired.Outcome != RecipeRunOutcome.Repaired || start.Started.Count != 1 || start.Started[0] != "Spooler")
            throw new Exception("Generated Spooler recipe did not repair through the runner: " + repaired.Detail);
        var standardUser = await runner.RunAsync(spooler, new RecipeRunOptions { AuthorizeRepair = true, Elevated = false });
        if (standardUser.Outcome != RecipeRunOutcome.Blocked || start.Started.Count != 1) throw new Exception("Standard-user run dispatched a service start.");
        state = "Information";
        var idle = await runner.RunAsync(spooler, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (idle.Outcome != RecipeRunOutcome.Clear || start.Started.Count != 1) throw new Exception("Informational service state dispatched a start.");
        state = "Attention";
        var attention = await runner.RunAsync(spooler, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (attention.Outcome != RecipeRunOutcome.NeedsAttention || start.Started.Count != 1) throw new Exception("Attention service state dispatched a start.");

        Console.WriteLine("PASS: Windows service pack generated from " + sources.Count + " catalog recipes with exact identity, dependency and target parity; substituted runs only, no service changed.");
    }
}
