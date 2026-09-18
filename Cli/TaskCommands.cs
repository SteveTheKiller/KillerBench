using System.Text.Json;
using KillerBench.Engine;

internal static class TaskCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args is not ["task-inventory"])
        { Console.Error.WriteLine("Usage: killerbench-cli task-inventory (read-only JSON inventory)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var result = await ScheduledTaskInventory.ReadAsync(stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { return 130; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine(e.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
