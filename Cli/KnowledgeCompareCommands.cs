using System.Text.Json;
using KillerBench.Engine;

internal static class KnowledgeCompareCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 3)
        { Console.Error.WriteLine("Usage: knowledge-compare BEFORE_JSON AFTER_JSON. Local reference comparison only; no installation or signature verification."); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var before = await KnowledgeCatalog.ReadFileAsync(args[1], stop.Token);
            var after = await KnowledgeCatalog.ReadFileAsync(args[2], stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Comparison = before.Compare(after, token: stop.Token),
                Scope = "Local validated-schema comparison. Up to 100 changes shown; totals include omitted changes. No signature, factual accuracy or update eligibility verification. No pack installed."
            }, Capture.JsonOptions));
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Pack comparison stopped or timed out."); return 130; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or JsonException)
        { Console.Error.WriteLine("Pack comparison failed: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
