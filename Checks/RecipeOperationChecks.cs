using System.Diagnostics;
using System.IO;
using System.Reflection;
using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;

internal static class RecipeOperationChecks
{
    internal static async Task RunAsync()
    {
        int dispatches = 0;
        var library = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddAction(new TimeoutAction(() => dispatches++));
        var journal = new MemoryRecipeJournal();
        var recipe = new RecipeDefinition("fixture.pending", 1, "Pending fixture", "Test", "fixture state",
            new ProbeSpec("fixture.state", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("clear")))
        { Action = new ActionSpec("fixture.repair", new Dictionary<string, string>()), Risk = RecipeRisk.Reversible };
        var sharedStore = new MemoryRecipeOperationStore();
        var first = await new RecipeRunner(library, journal, sharedStore).RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        var second = await new RecipeRunner(library, new MemoryRecipeJournal(), sharedStore).RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        if (!first.ExecutionUncertain || !second.ExecutionUncertain || dispatches != 1 || second.Action is not null)
            throw new Exception("An unresolved timed-out recipe was dispatched again: " + dispatches);
        foreach (string scenario in new[] { "canceled", "verification", "exception" })
        {
            int attempts = 0;
            var store = new MemoryRecipeOperationStore();
            using var stop = new CancellationTokenSource();
            var boundary = new CallbackAction(_ =>
            {
                attempts++;
                if (scenario == "canceled") { stop.Cancel(); throw new OperationCanceledException(stop.Token); }
                if (scenario == "exception") throw new IOException("Simulated interrupted provider.");
                return Task.FromResult(new ActionResult(true, true, ""));
            });
            var live = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddAction(boundary);
            try { await new RecipeRunner(live, new MemoryRecipeJournal(), store).RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }, stop.Token); }
            catch (IOException) when (scenario == "exception") { }
            var rerun = await new RecipeRunner(live, new MemoryRecipeJournal(), store).RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
            if (attempts != 1 || !rerun.ExecutionUncertain || rerun.Action is not null)
                throw new Exception("Unresolved " + scenario + " permitted another dispatch.");
        }
        foreach (bool dispatched in new[] { false, true })
        {
            var store = new MemoryRecipeOperationStore();
            int attempts = 0;
            var live = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddProbe(new VerifiedProbe())
                .AddAction(new CallbackAction(_ => { attempts++; return Task.FromResult(new ActionResult(dispatched, dispatched, "")); }));
            var verifiable = recipe with { Verify = new ProbeSpec("fixture.verified", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("clear")) };
            var result = await new RecipeRunner(live, new MemoryRecipeJournal(), store).RunAsync(verifiable, new RecipeRunOptions { AuthorizeRepair = true });
            var identity = RecipeOperationIdentity.FromResolvedRecipe(verifiable);
            if (await store.IsPendingAsync(identity) || attempts != 1 || result.Outcome != (dispatched ? RecipeRunOutcome.Repaired : RecipeRunOutcome.Blocked))
                throw new Exception("Definitive non-dispatch or verified completion retained a stale barrier.");
        }
        {
            // A pending record that cannot be closed must not replace a verified result with an exception.
            var live = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddProbe(new VerifiedProbe())
                .AddAction(new CallbackAction(_ => Task.FromResult(new ActionResult(true, true, ""))));
            var verifiable = recipe with { Verify = new ProbeSpec("fixture.verified", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("clear")) };
            RecipeRunResult result;
            try { result = await new RecipeRunner(live, new MemoryRecipeJournal(), new UnclosableStore()).RunAsync(verifiable, new RecipeRunOptions { AuthorizeRepair = true }); }
            catch (IOException error) { throw new Exception("A failure closing the pending record replaced the verified result: " + error.Message); }
            if (result.Outcome != RecipeRunOutcome.Repaired || !result.Detail.Contains("pending record could not be closed", StringComparison.Ordinal))
                throw new Exception("An unclosable pending record was not surfaced on the verified result: " + result.Outcome + " " + result.Detail);
        }
        using (var stopped = new CancellationTokenSource())
        {
            int attempts = 0;
            var store = new MemoryRecipeOperationStore();
            var live = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddAction(new TimeoutAction(() => attempts++));
            var result = await new RecipeRunner(live, new CancelPreparedJournal(stopped), store)
                .RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true }, stopped.Token);
            if (attempts != 0 || result.Outcome != RecipeRunOutcome.Canceled || await store.IsPendingAsync(RecipeOperationIdentity.FromResolvedRecipe(recipe)))
                throw new Exception("Cancellation after Prepared dispatched an action or retained an undispatched barrier.");
        }
        Console.WriteLine("PASS: declarative recipe timeout retains an unresolved operation barrier across runner instances; fixtures only.");
    }

    internal static async Task RunFileAsync(string scratchRoot)
    {
        string root = Path.Combine(scratchRoot, "recipe-operations-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var recipe = FixtureRecipe();
        int attempts = 0;
        var library = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddAction(new TimeoutAction(() => attempts++));
        var first = await new RecipeRunner(library, new FileRecipeJournal(Path.Combine(root, "first.jsonl")), new FileRecipeOperationStore(root))
            .RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true });
        var changed = recipe with { Version = 2, Action = recipe.Action! with { Parameters = new Dictionary<string, string> { ["desired"] = "different" } } };
        var second = await new RecipeRunner(library, new FileRecipeJournal(Path.Combine(root, "second.jsonl")), new FileRecipeOperationStore(root))
            .RunAsync(changed, new RecipeRunOptions { AuthorizeRepair = true });
        if (!first.ExecutionUncertain || !second.ExecutionUncertain || attempts != 1)
            throw new Exception("A new journal, version or desired parameter bypassed the durable pending barrier.");
        var identity = RecipeOperationIdentity.FromResolvedRecipe(recipe);
        var inspection = new FileRecipeOperationStore(root);
        if (!await inspection.IsPendingAsync(identity) || !await inspection.IsPendingAsync(identity))
            throw new Exception("Read-only inspection cleared unresolved recovery state.");

        string processRoot = Path.Combine(root, "processes");
        Directory.CreateDirectory(processRoot);
        using var owner = StartChild(processRoot);
        try
        {
            using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!File.Exists(Path.Combine(processRoot, "entered")))
            {
                if (owner.HasExited) throw new Exception("The operation owner exited before its provider was reached: " + await owner.StandardError.ReadToEndAsync());
                await Task.Delay(25, wait.Token);
            }
            using (var contender = StartChild(processRoot))
            {
                await contender.WaitForExitAsync(wait.Token);
                if (contender.ExitCode != 0) throw new Exception("The contender was not blocked: " + await contender.StandardError.ReadToEndAsync());
            }
            owner.Kill(entireProcessTree: true);
            await owner.WaitForExitAsync(wait.Token);
            using var afterInterruption = StartChild(processRoot);
            await afterInterruption.WaitForExitAsync(wait.Token);
            if (afterInterruption.ExitCode != 0 || File.ReadAllLines(Path.Combine(processRoot, "dispatches")).Length != 1)
                throw new Exception("Process interruption allowed a repeated dispatch.");
        }
        finally
        {
            if (!owner.HasExited) { owner.Kill(entireProcessTree: true); await owner.WaitForExitAsync(); }
        }
        Console.WriteLine("PASS: durable recipe pending state survives new journals, changed desired values, two-process contention and process termination; fixtures only. Artifacts: " + root);
    }

    internal static async Task<bool> TryRunChildAsync(string[] args)
    {
        if (args is not ["--recipe-operation-child", var root]) return false;
        var library = new PrimitiveLibrary("Fixture").AddProbe(new RepairableProbe()).AddAction(new CallbackAction(async token =>
        {
            await File.AppendAllTextAsync(Path.Combine(root, "dispatches"), "dispatch\n", token);
            await File.WriteAllTextAsync(Path.Combine(root, "entered"), "entered", token);
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            return new ActionResult(true, false, "") { TimedOut = true };
        }));
        var result = await new RecipeRunner(library, new MemoryRecipeJournal(), new FileRecipeOperationStore(root))
            .RunAsync(FixtureRecipe(), new RecipeRunOptions { AuthorizeRepair = true });
        if (!result.ExecutionUncertain || result.Action is not null) throw new Exception("The competing runner did not retain pending uncertainty.");
        return true;
    }

    private static Process StartChild(string root)
    {
        var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            info.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
        info.ArgumentList.Add("--recipe-operation-child");
        info.ArgumentList.Add(root);
        return Process.Start(info) ?? throw new Exception("The fixture child could not start.");
    }

    private static RecipeDefinition FixtureRecipe() => new("fixture.process", 1, "Process fixture", "Test", "fixture state",
        new ProbeSpec("fixture.state", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("clear")))
    { Action = new ActionSpec("fixture.repair", new Dictionary<string, string>()), Risk = RecipeRisk.Reversible };

    private sealed class RepairableProbe : IProbePrimitive
    {
        public string Name => "fixture.state";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(new PrimitiveResult(true, "repairable", ""));
    }

    private sealed class VerifiedProbe : IProbePrimitive
    {
        public string Name => "fixture.verified";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(new PrimitiveResult(true, "clear", ""));
    }

    private sealed class UnclosableStore : IRecipeOperationStore
    {
        public Task<IRecipeOperationLease?> TryBeginAsync(RecipeOperationIdentity identity, CancellationToken token = default) => Task.FromResult<IRecipeOperationLease?>(new Lease());
        public Task<bool> IsPendingAsync(RecipeOperationIdentity identity, CancellationToken token = default) => Task.FromResult(false);
        public Task<IRecipeRestartRecovery?> TryOpenRestartRecoveryAsync(RecipeOperationIdentity identity, CancellationToken token = default) => Task.FromResult<IRecipeRestartRecovery?>(null);

        private sealed class Lease : IRecipeOperationLease
        {
            public Task CompleteAsync(CancellationToken token = default) => throw new IOException("Simulated sharing violation.");
            public Task RecordRestartRequiredAsync(string? bootIdentity, IReadOnlyDictionary<string, string> recoveryParameters, CancellationToken token = default) => Task.CompletedTask;
            public void Dispose() { }
        }
    }

    private sealed class CancelPreparedJournal(CancellationTokenSource stop) : IRecipeJournal
    {
        public Task RecordAsync(RecipeJournalEntry entry, CancellationToken token)
        {
            if (entry.Stage == RecipeJournalStage.Prepared) stop.Cancel();
            return Task.CompletedTask;
        }
    }

    private sealed class TimeoutAction(Action dispatched) : IActionPrimitive
    {
        public string Name => "fixture.repair";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
        {
            dispatched();
            return Task.FromResult(new ActionResult(true, false, "") { TimedOut = true });
        }
    }

    private sealed class CallbackAction(Func<CancellationToken, Task<ActionResult>> callback) : IActionPrimitive
    {
        public string Name => "fixture.repair";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) => callback(token);
    }
}
