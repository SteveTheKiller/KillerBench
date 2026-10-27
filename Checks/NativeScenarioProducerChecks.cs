using System.IO;
using System.Text.Json;
using KillerBench.Engine.Windows.Primitives;

internal static class NativeScenarioProducerChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string folder = Path.Combine(scratch, "native-producer-fixture");
        Directory.CreateDirectory(folder);
        try
        {
            foreach (string scenario in new[] { "healthy", "eligible-preview", "rejected-prerequisite", "cancellation-before-dispatch" })
            {
                var host = new FixtureHost();
                var library = new PrimitiveLibrary(WindowsPrimitiveLibrary.Platform)
                    .AddProbe(new FixtureProbe(host));
                var results = await new NativeScenarioRunner(library, host).RunAsync("printing.spooler", scenario, folder, host.MachineName);
                var result = results.Single();
                Require(result.Outcome == NativeValidationOutcome.Passed && result.RestorationSucceeded,
                    "A supported no-dispatch scenario must retain genuine passing evidence: " + scenario);
                var record = RepairCapabilityInventory.ReadRecord(result.RecordPath, WindowsRepairCapabilities.All);
                foreach (string kind in new[] { "native result", "before snapshot", "after snapshot", "workflow journal", "restoration evidence" })
                    Require(record.Artifacts.Any(item => item.Kind == kind && File.Exists(Path.Combine(result.ArtifactFolder, item.Name))),
                        "The actual native producer must retain required artifact kind " + kind + " for " + scenario);
                string native = File.ReadAllText(Path.Combine(result.ArtifactFolder, record.Artifacts.Single(item => item.Kind == "native result").Name));
                Require(native.Contains("\"Dispatched\": false", StringComparison.Ordinal) && host.Starts == (scenario == "healthy" ? 0 : 1),
                    "Scenario evidence must explicitly prove no recipe dispatch; only restoration may request a service start");
            }
            Console.WriteLine("PASS: actual native scenario producer retains result, final pre-restoration state and no-dispatch proof for all four supported non-mutating scenarios; OS boundaries substituted.");

            // A dispatch that leaves a pending barrier must stay inside its own scenario: a later scenario for the same
            // recipe still runs, and the per-user operation store that real repairs use is never written.
            string userStore = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "RecipeOperations");
            string[] UserRecords() => Directory.Exists(userStore) ? [.. Directory.GetFiles(userStore).Order(StringComparer.Ordinal)] : [];
            var userBefore = UserRecords();
            var plan = NativeValidationCatalog.Find("printing.spooler")!;
            string verified = plan.Scenarios.Single(scenario => scenario.Kind == NativeValidationScenarioKind.VerifiedExecution).Id["printing.spooler.".Length..];
            string primitive = WindowsPacks.All().SelectMany(pack => pack.Recipes).Single(recipe => recipe.Id == "printing.spooler").Action!.Primitive;
            var scoped = new FixtureHost();
            var scopedLibrary = new PrimitiveLibrary(WindowsPrimitiveLibrary.Platform).AddProbe(new FixtureProbe(scoped)).AddAction(new TimedOutAction(primitive));
            string scopedFolder = Path.Combine(folder, "scoped");
            try
            {
                var runner = new NativeScenarioRunner(scopedLibrary, scoped);
                var left = (await runner.RunAsync("printing.spooler", verified, scopedFolder, scoped.MachineName)).Single();
                var healthy = (await runner.RunAsync("printing.spooler", "healthy", scopedFolder, scoped.MachineName)).Single();
                Require(left.Outcome == NativeValidationOutcome.Failed && healthy.Outcome == NativeValidationOutcome.Passed,
                    "A barrier left by one scenario leaked into the next: " + left.Outcome + ", then healthy " + healthy.Outcome + " " + healthy.Notes);
                Require(Directory.EnumerateFiles(scopedFolder, "*.pending.json", SearchOption.AllDirectories).Any(),
                    "The timed-out scenario's barrier must be retained under the validation output folder.");
            }
            finally
            {
                // Before the fix the barrier landed in the real per-user store; remove only records this fixture created.
                var created = UserRecords().Except(userBefore, StringComparer.Ordinal).ToArray();
                foreach (string path in created) File.Delete(path);
                Require(created.Length == 0, "Native validation wrote to the per-user recipe operation store: " + string.Join(", ", created));
            }
            Console.WriteLine("PASS: native scenarios use a scenario-scoped operation store; a retained barrier neither blocks the next scenario nor touches the per-user store.");
        }
        finally { Directory.Delete(folder, true); }
    }

    private sealed class FixtureHost : INativeScenarioHost, INativeScenarioService
    {
        internal int State = 4, Start = 3, Starts;
        public bool IsWindows => true;
        public string MachineName => "NativeProducerFixture";
        public string? DisposableMarker => NativeScenarioRunner.DisposableValue;
        public INativeScenarioService Service(string name) => this;
        public Task<NativeScenarioMachine> ReadMachineAsync(CancellationToken token) => Task.FromResult(new NativeScenarioMachine("Fixture Windows 11", "24H2", "26100.1", "x64"));
        public Task<NativeServiceSnapshot> SnapshotAsync(CancellationToken token) => Task.FromResult(new NativeServiceSnapshot(State, Start, false, "Fixture state=" + State + "; start=" + Start));
        public Task<bool> StopAsync(CancellationToken token) { State = 1; return Task.FromResult(true); }
        public Task<bool> SetStartAsync(string start, CancellationToken token) { Start = start == "disabled" ? 4 : 3; return Task.FromResult(true); }
        public Task<bool> StartAsync(CancellationToken token) { Starts++; State = 4; return Task.FromResult(true); }
    }

    private sealed class FixtureProbe(FixtureHost host) : IProbePrimitive
    {
        public string Name => "service.check";
        public IReadOnlyList<PrimitiveParameter> Parameters => [new("name", true, "Exact fixture service")];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new PrimitiveResult(true, host.Start == 4 ? "Attention" : host.State == 4 ? "Clear" : "Repairable", "Controlled provider observation"));
        }
    }

    private sealed class TimedOutAction(string name) : IActionPrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public RecipeRisk Risk => RecipeRisk.Reversible;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new ActionResult(true, false, "") { TimedOut = true });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
