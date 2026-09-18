using System.Text.Json;
using KillerBench.Engine;

internal static class ServiceLogonCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: service-logons (read-only service account-class and desktop-interaction review; account identities excluded)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            Console.Error.WriteLine("Checking service logon account classes and desktop interaction. Account names, domains, user identities, passwords, paths and arguments are excluded; no account or service will change.");
            var check = await ServiceLogonDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.EffectiveState is CheckState.Attention or CheckState.Unknown ? 3 : 0;
        }
        catch (OperationCanceledException) { return 130; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
