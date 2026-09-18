using System.Text.Json;
using KillerBench.Engine;

internal static class WmiProviderCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        { Console.Error.WriteLine("Usage: wmi-provider (read-only local operating-system query; JSON output)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Querying the local WMI operating-system provider...");
            var check = await WmiProviderDiagnostics.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.State == CheckState.Clear ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("WMI provider check stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("WMI provider check unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
