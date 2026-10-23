using System.IO;

internal static class MemoryDiagnosticRecoveryChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var recipe = WindowsSystemPack.Build().Recipes.Single(item => item.Id == "hardware.memory-diagnostic");
        string? boot = "before";
        string completion = "";
        int dispatches = 0;
        var library = new PrimitiveLibrary("Fixture")
            .AddProbe(new Probe("bcd.bootsequence", _ => new(true, null, "")))
            .AddProbe(new WindowsMemoryDiagnosticResultProbe((_, _) => Task.FromResult(new ToolResult(0, completion, "", false))))
            .AddAction(new WindowsMemoryDiagnosticScheduleAction(_ => Task.FromResult<long?>(42), (_, _) =>
            { dispatches++; return Task.FromResult(new ToolResult(0, "Scheduled", "", false)); }));
        string root = Path.Combine(scratch, "memory-recovery-" + Guid.NewGuid().ToString("N"));
        var store = new FileRecipeOperationStore(root);
        Task<RecipeRunResult> Run() => new RecipeRunner(library, new MemoryRecipeJournal(), store).RunAsync(recipe,
            new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, ExplicitOptionsConfirmed = true, BootIdentity = () => boot });
        var first = await Run();
        Require(first.Outcome == RecipeRunOutcome.RestartRequired, "Scheduling must retain restart recovery.");
        boot = "after";
        var missing = await Run();
        Require(missing.ExecutionUncertain && dispatches == 1, "Missing completion must retain uncertainty without scheduling again.");
        completion = Event(41, 1201);
        var stale = await Run();
        Require(stale.ExecutionUncertain && dispatches == 1, "An older completion event must not close recovery.");
        completion = Event(43, 1103);
        var canceled = await Run();
        Require(canceled.ExecutionUncertain && dispatches == 1, "Canceled memory tests are not completed memory tests.");
        completion = Event(44, 1201);
        var completed = await Run();
        Require(completed.Outcome == RecipeRunOutcome.Repaired && !completed.ExecutionUncertain && dispatches == 1 &&
            !await store.IsPendingAsync(RecipeOperationIdentity.FromResolvedRecipe(recipe)),
            "A consumed BCD entry with matching completion evidence must close recovery without redispatch: " + completed.Outcome);
        foreach (int eventId in new[] { 1101, 1102, 1201, 1202 })
            Require(WindowsMemoryDiagnosticResultProbe.Interpret(42, 44, eventId).Complete, "Every installed completion event version must be recognized.");
        Require(WindowsMemoryDiagnosticResultProbe.Interpret(42, 44, 1202).Detail!.Contains("memory errors"), "Hardware errors must remain explicit after scheduling recovery completes.");
        Console.WriteLine("PASS: real Memory Diagnostic recipe recovers from consumed BCD only with operation-specific completion evidence; fixture, not native boot.");
    }

    private sealed class Probe(string name, Func<IReadOnlyDictionary<string, string>, PrimitiveResult> observe) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) => Task.FromResult(observe(parameters));
    }
    private static string Event(long id, int eventId) => "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-MemoryDiagnostics-Results'/><EventID>" + eventId + "</EventID><EventRecordID>" + id + "</EventRecordID></System></Event>";
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
