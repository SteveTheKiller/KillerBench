using System.Text.Json;
using KillerBench.Engine;

internal static class ServiceDependencyConfigurationCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: service-dependencies (read-only direct dependency integrity review; no service changes)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            Console.Error.WriteLine("Checking service dependency identities and direct cycles. No service or dependency configuration will change.");
            var inventory = await ServiceInventoryDiagnostics.CheckAsync(stop.Token);
            var check = await ServiceDependencyConfigurationDiagnostics.CheckAsync(inventory, token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.EffectiveState is CheckState.Attention or CheckState.Unknown ? 3 : 0;
        }
        catch (OperationCanceledException) { return 130; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
