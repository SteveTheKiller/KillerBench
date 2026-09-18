using System.Text.Json;
using KillerBench.Engine;

internal static class StorageEventCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        { Console.Error.WriteLine("Usage: storage-events (read-only recent storage history; JSON output)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            Console.Error.WriteLine("Reading recent storage events...");
            var check = await StorageEvents.CheckAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(check, Capture.JsonOptions));
            return check.State == CheckState.Information ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Storage event collection stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Storage event collection unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
