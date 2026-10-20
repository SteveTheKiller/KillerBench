using System.IO;
using KillerBench.Engine;

internal static class NativeScenarioRunnerChecks
{
    internal static async Task RunAsync()
    {
        const string fixtureMachine = "FixtureMachine";
        if (!NativeScenarioRunner.MatchesDisposableIdentity(fixtureMachine, fixtureMachine, NativeScenarioRunner.DisposableValue))
            throw new Exception("Exact fixture gate inputs were refused.");
        if (NativeScenarioRunner.MatchesDisposableIdentity(fixtureMachine, fixtureMachine, null) ||
            NativeScenarioRunner.MatchesDisposableIdentity(fixtureMachine, fixtureMachine, "yes") ||
            NativeScenarioRunner.MatchesDisposableIdentity("some-other-machine", fixtureMachine, NativeScenarioRunner.DisposableValue) ||
            NativeScenarioRunner.MatchesDisposableIdentity("", fixtureMachine, NativeScenarioRunner.DisposableValue))
            throw new Exception("Incomplete fixture gate inputs were accepted.");

            string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-native-" + Guid.NewGuid().ToString("N"));
            try
            {
                await new NativeScenarioRunner().RunAsync("printing.spooler", "healthy", scratch, "Not-" + Environment.MachineName);
                throw new Exception("The runner injected without the disposable gate.");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("Refusing", StringComparison.Ordinal)) { }
            if (Directory.Exists(scratch)) throw new Exception("The refused run created output.");

            foreach (var scenario in NativeValidationCatalog.Find("printing.spooler")!.Scenarios)
                if (NativeScenarioRunner.Injectable.Contains(scenario.Kind) && scenario.Kind == NativeValidationScenarioKind.VerifiedExecution && !scenario.AllowsNativeMutation)
                    throw new Exception("Verified execution is injectable but the catalog forbids native mutation.");
            Console.WriteLine("PASS: native scenario runner refuses without the exact machine name and disposable marker; nothing was injected on this machine.");
    }
}
