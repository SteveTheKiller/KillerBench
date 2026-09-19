using System.IO;
using KillerBench.Engine;

internal static class NativeScenarioRunnerChecks
{
    internal static async Task RunAsync()
    {
        string? saved = Environment.GetEnvironmentVariable(NativeScenarioRunner.DisposableVariable);
        try
        {
            Environment.SetEnvironmentVariable(NativeScenarioRunner.DisposableVariable, null);
            if (NativeScenarioRunner.IsDisposable(Environment.MachineName)) throw new Exception("Machine was treated as disposable without the environment marker.");
            Environment.SetEnvironmentVariable(NativeScenarioRunner.DisposableVariable, NativeScenarioRunner.DisposableValue);
            if (NativeScenarioRunner.IsDisposable("some-other-machine")) throw new Exception("A different machine name was accepted as disposable.");
            if (NativeScenarioRunner.IsDisposable("")) throw new Exception("An empty machine name was accepted as disposable.");
            Environment.SetEnvironmentVariable(NativeScenarioRunner.DisposableVariable, "yes");
            if (NativeScenarioRunner.IsDisposable(Environment.MachineName)) throw new Exception("A partial environment marker was accepted.");
            Environment.SetEnvironmentVariable(NativeScenarioRunner.DisposableVariable, null);

            string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-native-" + Guid.NewGuid().ToString("N"));
            try
            {
                await new NativeScenarioRunner().RunAsync("printing.spooler", "healthy", scratch, Environment.MachineName);
                throw new Exception("The runner injected without the disposable gate.");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("Refusing", StringComparison.Ordinal)) { }
            if (Directory.Exists(scratch)) throw new Exception("The refused run created output.");

            foreach (var scenario in NativeValidationCatalog.Find("printing.spooler")!.Scenarios)
                if (NativeScenarioRunner.Injectable.Contains(scenario.Kind) && scenario.Kind == NativeValidationScenarioKind.VerifiedExecution && !scenario.AllowsNativeMutation)
                    throw new Exception("Verified execution is injectable but the catalog forbids native mutation.");
            Console.WriteLine("PASS: native scenario runner refuses without the exact machine name and disposable marker; nothing was injected on this machine.");
        }
        finally { Environment.SetEnvironmentVariable(NativeScenarioRunner.DisposableVariable, saved); }
    }
}
