using KillerBench.Engine;

internal static class ValidateNativeCommands
{
    private const string Usage = "Usage: validate-native RECIPE_ID SCENARIO_SUFFIX|all OUTPUT_DIR --disposable MACHINE_NAME " +
        "(injects service faults on a disposable machine only; requires " + NativeScenarioRunner.DisposableVariable + "=" + NativeScenarioRunner.DisposableValue + " and administrator access; restores the injected state afterwards)";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 6 || args[4] != "--disposable") { Console.Error.WriteLine(Usage); return 2; }
        string recipeId = args[1], suffix = args[2], output = Path.GetFullPath(args[3]), machine = args[5];
        if (!NativeScenarioRunner.IsDisposable(machine))
        {
            Console.Error.WriteLine("Refused: '" + machine + "' must be this machine's name and " + NativeScenarioRunner.DisposableVariable + " must be set to " + NativeScenarioRunner.DisposableValue + ". Never run this on a development or customer machine.");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        try
        {
            var results = await new NativeScenarioRunner().RunAsync(recipeId, suffix == "all" ? null : suffix, output, machine,
                new Progress<string>(line => Console.Error.WriteLine(line)), stop.Token).ConfigureAwait(false);
            foreach (var result in results)
                Console.WriteLine(result.ScenarioId + "\t" + result.Outcome + "\t" + result.RecordPath + "\n  " + result.Notes);
            int failed = results.Count(result => result.Outcome == NativeValidationOutcome.Failed);
            Console.WriteLine(results.Count + " scenario(s): " + results.Count(r => r.Outcome == NativeValidationOutcome.Passed) + " passed, " + failed + " failed, " +
                results.Count(r => r.Outcome == NativeValidationOutcome.Blocked) + " blocked. Use validation-report with the record files in " + output + ".");
            return failed == 0 ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Canceled. Review the service state manually before another run."); return 130; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine(error.Message); return 2; }
    }
}
