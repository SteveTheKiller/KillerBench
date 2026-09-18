using System.Text.Json;
using KillerBench.Engine;

internal static class WorkflowJournalCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: workflow-journal FILE.json (historical review only)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var review = await WorkflowJournalReview.ReadAsync(args[1], stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(review, Capture.JsonOptions));
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Journal review stopped or timed out."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        { Console.Error.WriteLine("Journal review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
