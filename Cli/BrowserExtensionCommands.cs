using System.Text.Json;
using KillerBench.Engine;
internal static class BrowserExtensionCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length != 1) { Console.Error.WriteLine("Usage: browser-extensions (read-only Chrome, Edge, Brave and Firefox extension identity review; no removal)"); return 2; }
        try { var review = BrowserExtensionReview.Read(); Console.WriteLine(JsonSerializer.Serialize(review, Capture.JsonOptions)); return review.Complete ? 0 : 3; }
        catch (OperationCanceledException) { return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException) { Console.Error.WriteLine("Browser extension review unavailable: " + error.Message); return 1; }
    }
}
