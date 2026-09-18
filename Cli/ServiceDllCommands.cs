using System.Text.Json;
using KillerBench.Engine;

internal static class ServiceDllCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: service-dlls (read-only registered service DLL presence and Authenticode status; paths excluded)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            Console.Error.WriteLine("Checking registered local service DLL presence and Authenticode status. Full paths, accounts and signer identities are excluded; nonlocal paths are not contacted and no service, DLL or registry value will change.");
            var check = await ServiceDllDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.EffectiveState is CheckState.Attention or CheckState.Unknown ? 3 : 0;
        }
        catch (OperationCanceledException) { return 130; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
