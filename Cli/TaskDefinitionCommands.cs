using System.Text.Json;
using KillerBench.Engine;

internal static class TaskDefinitionCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 3) { Console.Error.WriteLine("Usage: task-definition TASK_FOLDER TASK_NAME (read-only stored XML)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var result = await ScheduledTaskDefinition.ReadAsync(new(args[1], args[2]), stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Observed ? 0 : 3;
        }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (OperationCanceledException) { Console.Error.WriteLine("Task definition inspection canceled."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine(error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
