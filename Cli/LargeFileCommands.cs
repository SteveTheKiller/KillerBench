using System.Text.Json;
using KillerBench.Engine;

internal static class LargeFileCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine("Usage: large-files FOLDER [--min-bytes N] [--max-files N] [--limit N] (read-only local file review)"); return 2; }
        long minimum = 100 * 1024 * 1024;
        int maxFiles = 10_000, limit = 500;
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--min-bytes" && i + 1 < args.Length && long.TryParse(args[++i], out var value)) minimum = value;
            else if (args[i] == "--max-files" && i + 1 < args.Length && int.TryParse(args[++i], out var maximum)) maxFiles = maximum;
            else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var count)) limit = count;
            else { Console.Error.WriteLine("Usage: large-files FOLDER [--min-bytes N] [--max-files N] [--limit N] (read-only local file review)"); return 2; }
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try { var result = await LargeFileReview.ScanAsync(args[1], minimum, maxFiles, limit, stop.Token); Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions)); return result.Complete ? 0 : 3; }
        catch (OperationCanceledException) { Console.Error.WriteLine("Large-file review stopped."); return 130; }
        catch (Exception error) when (error is ArgumentException or DirectoryNotFoundException or IOException or UnauthorizedAccessException) { Console.Error.WriteLine("Large-file review did not finish: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
