using System.Text.Json;
using KillerBench.Engine;

internal static class FoxitCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args is not ["foxit-inventory"])
        {
            Console.Error.WriteLine("Usage: killerbench-cli foxit-inventory (read-only machine inventory; JSON output)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            var result = await FoxitInventory.ReadAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { return 130; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine(e.Message); return 1; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
