using System.Text.Json;
using KillerBench.Engine;

internal static class MsiCacheCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: msi-cache (read-only registered Windows Installer cache visibility)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            var checks = await MsiCacheDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
            return checks.Any(check => check.EffectiveState is CheckState.Attention or CheckState.Unknown) ? 3 : 0;
        }
        catch (OperationCanceledException) { return 130; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
