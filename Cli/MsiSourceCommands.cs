using System.Text.Json;
using KillerBench.Engine;

internal static class MsiSourceCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: msi-sources (read-only privacy-bounded Windows Installer source metadata)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            Console.Error.WriteLine("Reading registered Windows Installer source types and package filenames. Source paths, URLs, media labels, prompts, user identities and credentials are excluded; no source will be contacted or changed.");
            var checks = await MsiSourceDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(checks, Capture.JsonOptions));
            return checks.Any(check => check.EffectiveState is CheckState.Attention or CheckState.Unknown) ? 3 : 0;
        }
        catch (OperationCanceledException) { return 130; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
