using System.Text.Json;
using KillerBench.Engine;

internal static class ServicePathQuotingCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: service-path-quoting (read-only service executable path quoting review; full paths and arguments excluded)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            Console.Error.WriteLine("Checking service executable path quoting. Full paths, command arguments, accounts and process IDs are excluded; no service or registry value will change.");
            var check = await ServicePathQuotingDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.EffectiveState is CheckState.Attention or CheckState.Unknown ? 3 : 0;
        }
        catch (OperationCanceledException) { return 130; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
