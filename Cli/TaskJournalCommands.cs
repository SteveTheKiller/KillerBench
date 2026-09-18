using System.Text.Json;
using KillerBench.Engine;

internal static class TaskJournalCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args is not ["task-journal", _] && args is not ["task-journal", _, "--completion", _])
        { Console.Error.WriteLine("Usage: killerbench-cli task-journal RECORD.json [--completion COMPLETION.json] (historical review only)"); return 2; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var review = args.Length == 4
                ? await TaskJournalReview.ReadPairAsync(args[1], args[3], stop.Token)
                : await TaskJournalReview.ReadAsync(args[1], stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new { reviewOnly = true, retryAuthorized = false, review }, Capture.JsonOptions));
            return review.State switch { "DefinitionVerified" or "NotDispatched" => 0, "Pending" or "Unverified" => 3, _ => 1 };
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Task-journal review canceled or timed out."); return 130; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
