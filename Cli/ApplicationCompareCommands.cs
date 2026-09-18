using System.Text.Json;
using KillerBench.Engine;

internal static class ApplicationCompareCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 3) { Console.Error.WriteLine("Usage: installed-apps-compare BEFORE.json AFTER.json (saved installed-apps output)"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var result = await ApplicationInventoryFiles.CompareAsync(args[1], args[2], stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Comparable ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Inventory comparison stopped or exceeded 30 seconds."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or NotSupportedException or FormatException)
        { Console.Error.WriteLine("Cannot compare inventories: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

}
