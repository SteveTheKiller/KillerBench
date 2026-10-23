using System.IO;
using KillerBench.Engine.Core;
using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;

/// <summary>
/// A restart-required repair is verified after a later boot without repeating the action. Each run uses a new store
/// instance over the same folder, as a new process would. Uncertain and malformed records stay pending.
/// </summary>
internal static class RestartRecoveryChecks
{
    internal static async Task RunAsync(string scratch)
    {
        if (OperatingSystem.IsWindows())
        {
            string? firstIdentity = WindowsBootIdentity.Read();
            Require(firstIdentity is not null && firstIdentity == WindowsBootIdentity.Read(), "The read-only Windows boot identifier must be available and stable within one boot.");
        }
        string root = Path.Combine(scratch, "restart-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var boot = new DateTimeOffset(2026, 10, 21, 8, 0, 0, TimeSpan.Zero);
        var later = boot.AddMinutes(20);
        var recipe = new RecipeDefinition("fixture.restart", 1, "Restart fixture", "Test", "fixture state",
            new ProbeSpec("fixture.state", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("clear")))
        { Action = new ActionSpec("fixture.repair", new Dictionary<string, string>()), Risk = RecipeRisk.Persistent };
        string state = "repairable";
        int dispatches = 0;
        ActionResult Outcome() => new(true, true, "") { RestartRequired = true };
        Func<ActionResult> action = Outcome;
        string? bootIdentity = "boot-one";
        var library = new PrimitiveLibrary("Fixture").AddProbe(new StateProbe(() => state)).AddAction(new CountingAction(() => { dispatches++; return action(); }));
        Task<RecipeRunResult> Run(DateTimeOffset bootTime, bool authorize = true) =>
            new RecipeRunner(library, new MemoryRecipeJournal(), new FileRecipeOperationStore(root))
                .RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = authorize, BootTimeUtc = () => bootTime, BootIdentity = () => bootIdentity });
        var identity = RecipeOperationIdentity.FromResolvedRecipe(recipe);

        var first = await Run(boot);
        Require(first.Outcome == RecipeRunOutcome.RestartRequired && dispatches == 1 && await new FileRecipeOperationStore(root).IsPendingAsync(identity),
            "A restart-required repair keeps its barrier: " + first.Outcome);
        state = "clear";
        var sameBoot = await Run(boot);
        Require(sameBoot.Outcome == RecipeRunOutcome.Unknown && dispatches == 1 && sameBoot.Detail.Contains("Restart Windows", StringComparison.Ordinal) &&
            await new FileRecipeOperationStore(root).IsPendingAsync(identity), "Before a restart the barrier stays and says to restart: " + sameBoot.Detail);
        var clockShift = await Run(boot.AddSeconds(60));
        Require(clockShift.Outcome == RecipeRunOutcome.Unknown && await new FileRecipeOperationStore(root).IsPendingAsync(identity),
            "A same-boot wall-clock correction must not verify a restart-required repair: " + clockShift.Outcome);
        var backward = await Run(boot.AddHours(-2));
        Require(backward.Outcome == RecipeRunOutcome.Unknown, "A backward clock correction must not count as a restart.");
        bootIdentity = null;
        var unavailable = await Run(later);
        Require(unavailable.Outcome == RecipeRunOutcome.Unknown, "Unavailable identity must retain recovery uncertainty.");
        bootIdentity = "boot-two";
        state = "repairable";
        var notFixed = await Run(later);
        Require(notFixed.Outcome != RecipeRunOutcome.Repaired && dispatches == 1 && await new FileRecipeOperationStore(root).IsPendingAsync(identity),
            "A failed verification after restart keeps the barrier and does not repeat the action: " + notFixed.Outcome);
        state = "clear";
        var recovered = await Run(later, authorize: false);
        Require(recovered.Outcome == RecipeRunOutcome.Repaired && recovered.Action is null && dispatches == 1 &&
            !await new FileRecipeOperationStore(root).IsPendingAsync(identity) && recovered.Detail.Contains("after the restart", StringComparison.Ordinal),
            "After a later boot the earlier repair is verified without dispatch and the barrier clears: " + recovered.Outcome + " " + recovered.Detail);
        var next = await Run(later.AddMinutes(5));
        Require(next.Outcome == RecipeRunOutcome.Clear && dispatches == 1, "The next diagnostic run is no longer blocked");

        // An uncertain post-dispatch record is not a restart record and stays pending across a boot.
        state = "repairable";
        action = () => new ActionResult(true, false, "", "failed after the change");
        var uncertain = await Run(boot);
        state = "clear";
        var afterBoot = await Run(later);
        Require(uncertain.ExecutionUncertain && afterBoot.Outcome == RecipeRunOutcome.Unknown && dispatches == 2 &&
            await new FileRecipeOperationStore(root).IsPendingAsync(identity), "An uncertain record is never cleared by a later boot and a clear detection");

        // A damaged record stays pending.
        string pending = Directory.GetFiles(root, "*.pending.json").Single();
        File.WriteAllText(pending, "{\"Schema\":1,\"RestartRequiredBootUtc\":\"not a date\"}");
        var damaged = await Run(later.AddHours(1));
        Require(damaged.Outcome == RecipeRunOutcome.Unknown && dispatches == 2 && File.Exists(pending), "A malformed record stays pending");
        Console.WriteLine("PASS: restart-required repairs are verified after a later boot without repeating the action; uncertain and malformed records stay pending.");
    }

    private sealed class StateProbe(Func<string> state) : IProbePrimitive
    {
        public string Name => "fixture.state";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(new PrimitiveResult(true, state(), ""));
    }

    private sealed class CountingAction(Func<ActionResult> run) : IActionPrimitive
    {
        public string Name => "fixture.repair";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public RecipeRisk Risk => RecipeRisk.Persistent;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(run());
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
