using System.Text.Json;
using KillerBench.Engine;

internal static class BitsRetryCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length > 0 && args[0] == "bits-retry-recover") return await RecoverAsync(args);
        if ((args.Length != 2 && !(args.Length == 3 && args[2] == "--execute")) ||
            !Guid.TryParse(args[1], out var id) || id == Guid.Empty)
        { Console.Error.WriteLine("Usage: bits-retry JOB_GUID [--execute] (preview by default; running-account jobs only)"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(70));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitsRetry");
        try
        {
            var host = new WindowsBitsRetryHost();
            Console.Error.WriteLine("Reading running-account BITS transfers.");
            var inventory = await host.ReadAsync(stop.Token);
            var review = BitsJobReview.Review(inventory, id, stop.Token);
            if (args.Length == 2)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Mode = "Preview", Review = review, inventory.Evidence,
                    Effects = "Retry requests may resume network transfers for this job. No completion, removal, ownership or retry-policy changes. The owning application may act concurrently. Suspended jobs are excluded.",
                    Execution = "Use --execute only after reviewing the job and its owning application. Execution rechecks evidence; this preview is not authorization or a success prediction.",
                    JournalDirectory = directory
                }, Capture.JsonOptions));
                return review.Complete && review.Selected is not null ? 0 : 3;
            }
            Console.Error.WriteLine("Requesting a retry for selected running-account job " + id + ". Journal: " + directory);
            var result = await BitsRetry.RunAsync(inventory, id, host, new BitsRetryJournal(directory), new ConsoleProgress(), token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new { Result = result, JournalDirectory = directory }, Capture.JsonOptions));
            return result.Outcome is BitsRetryOutcome.RetryRequested or BitsRetryOutcome.TransferReported ? 0 :
                result.Outcome == BitsRetryOutcome.Stopped ? 130 : 3;
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("BITS operation stopped or timed out. A request may still be active; retained attempts must be reviewed before another retry. Journal: " + directory); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BITS operation unavailable: " + error.Message + ". No successful outcome established. Retained journal: " + directory); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> RecoverAsync(string[] args)
    {
        if (args.Length != 2 || !Guid.TryParse(args[1], out var id) || id == Guid.Empty)
        { Console.Error.WriteLine("Usage: bits-retry-recover JOB_GUID (two read-only snapshots; never retries)"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitsRetry");
        try
        {
            var result = await BitsRetryReconciliation.RunAsync(directory, id, new WindowsBitsRetryHost(),
                progress: new ConsoleProgress(), token: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new { Result = result, JournalDirectory = directory }, Capture.JsonOptions));
            return result.GetExitCode();
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("BITS retry recovery stopped or timed out. The retry guard was retained and no retry was requested."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException or
            InvalidOperationException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BITS retry recovery unavailable: " + error.Message + ". The retry guard was retained and no retry was requested."); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
