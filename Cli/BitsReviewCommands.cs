using System.Text.Json;
using KillerBench.Engine;

internal static class BitsReviewCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2 || args.Length > 4 || !Guid.TryParse(args[1], out var id) || id == Guid.Empty ||
            args.Skip(2).Any(option => option is not ("--all-users" or "--observe")) || args.Skip(2).Distinct().Count() != args.Length - 2)
        { Console.Error.WriteLine("Usage: bits-job-review JOB_GUID [--all-users] [--observe] (two snapshots five seconds apart; no transfer changes)"); return 2; }
        bool observe = args.Contains("--observe"), allUsers = args.Contains("--all-users");
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(observe ? 95 : 35));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (observe)
            {
                Console.Error.WriteLine("Observing two transfer snapshots five seconds apart. No retry, completion or cancellation requested.");
                var observation = await BitsObservation.ObserveAsync(id, t => BitsDiagnostics.ReadAsync(allUsers, t), TimeSpan.FromSeconds(5), stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(observation, Capture.JsonOptions));
                return observation.Comparison.Comparable ? 0 : 3;
            }
            var inventory = await BitsDiagnostics.ReadAsync(allUsers, stop.Token);
            var review = BitsJobReview.Review(inventory, id, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new { Review = review, inventory.Evidence }, Capture.JsonOptions));
            return review.Complete && review.Selected is not null ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Job review stopped or timed out."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
        { Console.Error.WriteLine("Job review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
