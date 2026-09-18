using System.Text.Json;
using KillerBench.Engine;

internal static class EvidenceBatchCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length is < 2 or > 13 || args.Skip(1).Any(path => string.IsNullOrWhiteSpace(path) ||
            Path.GetExtension(path).Equals(".kbench", StringComparison.OrdinalIgnoreCase)))
        { Console.Error.WriteLine("Usage: investigate-batch FILE [FILE ...] (1 to 12 logs/event files; one JSON object per line; no repairs)"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        bool incomplete = false;
        try
        {
            await foreach (var result in EvidenceBatch.ReadAsync(args[1..], stop.Token))
            {
                incomplete |= result.Error is not null || result.Report?.Partial != false;
                Console.WriteLine(JsonSerializer.Serialize(new { result.Path, result.Report, result.Error }));
            }
            return incomplete ? 3 : 0;
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Batch stopped or timed out. Previously emitted results remain valid within their stated scope."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Console.Error.WriteLine("Batch investigation unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
