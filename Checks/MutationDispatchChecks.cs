using System.IO;
using KillerBench.Engine.Core;
using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Windows.Packs;
using KillerBench.Engine.Windows.Primitives;

/// <summary>A command that fails after its change was sent must stay dispatched and uncertain, never "no change".</summary>
internal static class MutationDispatchChecks
{
    private const string Marker = "KB-MUTATION-DISPATCH";

    internal static async Task RunAsync()
    {
        var parameters = new Dictionary<string, string> { ["name"] = "Ethernet" };
        foreach (bool powerSaving in new[] { false, true })
        {
            const string adapter = "function Get-NetAdapter { param($Name,[switch]$Physical,$ErrorAction) [pscustomobject]@{Count=1;Name=$Name;AdminStatus='Down'} };" +
                "function Get-NetAdapterPowerManagement { param($Name,$ErrorAction) [pscustomobject]@{Name=$Name;AllowComputerToTurnOffDevice='Enabled'} };";
            Task<ToolResult> AfterChange(string script, CancellationToken token, string[] arguments) => WindowsNetAdapterPrimitives.RunAsync(adapter +
                "function Enable-NetAdapter { throw 'failed after the change' }; function Set-NetAdapterPowerManagement { throw 'failed after the change' };" + script, token, arguments);
            Task<ToolResult> BeforeChange(string script, CancellationToken token, string[] arguments) => WindowsNetAdapterPrimitives.RunAsync(
                "function Get-NetAdapter { throw 'not found' }; function Get-NetAdapterPowerManagement { throw 'not found' };" + script, token, arguments);
            IActionPrimitive Create(Func<string, CancellationToken, string[], Task<ToolResult>> run) =>
                powerSaving ? new WindowsAdapterPowerSavingDisableAction(run) : new WindowsAdapterEnableAction(run);
            var failedAfter = await Create(AfterChange).ExecuteAsync(parameters, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            Require(failedAfter is { Dispatched: true, Succeeded: false }, "A failure after the adapter command was sent stays dispatched: " + failedAfter);
            var failedBefore = await Create(BeforeChange).ExecuteAsync(parameters, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            Require(failedBefore is { Dispatched: false, Succeeded: false }, "A failure before the adapter command is a refusal: " + failedBefore);
        }

        Require(WindowsTpmInitializeAction.Interpret(new ToolResult(1, Marker + "\r\n", "Initialize-Tpm failed", false)) is { Dispatched: true, Succeeded: false },
            "A TPM initialization that fails after dispatch stays dispatched");
        Require(WindowsTpmInitializeAction.Interpret(new ToolResult(0, Marker + "\r\nTrue|False|False|False|False\r\n", "", false)) is { Dispatched: true, Succeeded: true },
            "The dispatch line does not disturb a successful TPM result");
        Require(WindowsWinHttpProxyResetAction.Interpret(new ToolResult(1, Marker + "\r\n", "netsh exited with 1", false)) is { Dispatched: true, Succeeded: false },
            "A proxy reset that fails after netsh started stays dispatched");
        Require(WindowsWinHttpProxyResetAction.Interpret(new ToolResult(1, "", "Access denied", false)) is { Dispatched: false },
            "A proxy reset refused before netsh is not dispatched");

        Require(WindowsRegistryPrimitives.WriteFailed(true, true, new IOException("read-back failed")) is { Dispatched: true, Succeeded: false } &&
            WindowsRegistryPrimitives.WriteFailed(true, false, new IOException("write interrupted")) is { Dispatched: true } &&
            WindowsRegistryPrimitives.WriteFailed(true, false, new UnauthorizedAccessException("denied")) is { Dispatched: false } &&
            WindowsRegistryPrimitives.WriteFailed(false, false, new IOException("prior read failed")) is { Dispatched: false },
            "Registry failures after or during a write stay dispatched; refusals before it do not");

        var recipe = WindowsNetworkPack.Build().Recipes.Single(candidate => candidate.Id == "network.adapter-enable");
        var options = new Dictionary<string, string> { ["adapter"] = "TestAdapter" };
        RecipeOptions.Resolve(recipe, options, out var resolvedOptions);
        var identity = RecipeOperationIdentity.FromResolvedRecipe(RecipeOptions.Apply(recipe, resolvedOptions));
        foreach (var (output, error, uncertain) in new[] { (Marker + "\n", "provider failed after the change", true), ("", "Exact adapter not found.", false) })
        {
            var store = new MemoryRecipeOperationStore();
            var action = new WindowsAdapterEnableAction((_, _, _) => Task.FromResult(new ToolResult(1, output, error, false)));
            var runner = new RecipeRunner(new PrimitiveLibrary("Windows").AddProbe(new DownProbe()).AddAction(action), new MemoryRecipeJournal(), store);
            var result = await runner.RunAsync(recipe, new RecipeRunOptions
            { AuthorizeRepair = true, Elevated = true, ExplicitOptionsConfirmed = true, Options = options });
            bool pending = await store.IsPendingAsync(identity);
            Require(result.ExecutionUncertain == uncertain && pending == uncertain,
                (uncertain ? "A post-dispatch failure must stay uncertain with its recovery barrier" : "A pre-dispatch refusal must not leave a barrier") +
                ": uncertain=" + result.ExecutionUncertain + " pending=" + pending + " " + result.Detail);
        }
        Console.WriteLine("PASS: post-dispatch command failures stay dispatched and uncertain with a recovery barrier; pre-dispatch refusals do not.");

        // A primitive that throws after dispatch began must still be journaled as dispatched and reported as uncertain.
        var fixture = new RecipeDefinition("fixture.throws", 1, "Throwing fixture", "Test", "fixture state",
            new ProbeSpec("netadapter.admin-status", new Dictionary<string, string> { ["name"] = "Fixture" }, ExpectSpec.ClearWhenEquals("Up")))
        { Action = new ActionSpec("fixture.recorder", new Dictionary<string, string>()) { Timeout = TimeSpan.FromMinutes(10) }, ExecutionTimeout = TimeSpan.FromSeconds(30), Risk = RecipeRisk.Reversible };
        var throwingJournal = new MemoryRecipeJournal();
        var thrown = await new RecipeRunner(new PrimitiveLibrary("Fixture").AddProbe(new DownProbe()).AddAction(new RecordingAction(_ => throw new IOException("provider vanished"))),
            throwingJournal, new MemoryRecipeOperationStore()).RunAsync(fixture, new RecipeRunOptions { AuthorizeRepair = true });
        Require(thrown is { Outcome: RecipeRunOutcome.Unknown, ExecutionUncertain: true, Action.Dispatched: true } &&
            throwingJournal.Entries.Any(entry => entry.Stage == RecipeJournalStage.Dispatched),
            "An action that throws after dispatch must be journaled as dispatched and reported uncertain: " + thrown.Outcome + " " + thrown.Detail);

        // The primitive is told the same effective timeout the runner enforces.
        TimeSpan given = TimeSpan.Zero;
        await new RecipeRunner(new PrimitiveLibrary("Fixture").AddProbe(new DownProbe()).AddAction(new RecordingAction(timeout => { given = timeout; return new ActionResult(true, true, ""); })),
            new MemoryRecipeJournal(), new MemoryRecipeOperationStore()).RunAsync(fixture, new RecipeRunOptions { AuthorizeRepair = true });
        Require(given == fixture.ExecutionTimeout, "The action was given " + given + " instead of the effective timeout " + fixture.ExecutionTimeout);
        Console.WriteLine("PASS: a throwing action is journaled as dispatched and uncertain, and the action receives the effective timeout.");
    }

    private sealed class RecordingAction(Func<TimeSpan, ActionResult> execute) : IActionPrimitive
    {
        public string Name => "fixture.recorder";
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(execute(timeout));
    }

    private sealed class DownProbe : IProbePrimitive
    {
        public string Name => "netadapter.admin-status";
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsNetAdapterPrimitives.NameParameter];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(new PrimitiveResult(true, "Down", "fixture disabled adapter"));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
