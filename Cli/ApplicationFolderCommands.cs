using System.Text.Json;
using KillerBench.Engine;

internal static class ApplicationFolderCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: app-folder-review FOLDER_NAME (read-only exact local folder review; no deletion)");
            return 2;
        }
        try
        {
            Console.WriteLine(JsonSerializer.Serialize(ApplicationFolderReview.Review(args[1]), Capture.JsonOptions));
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Application-folder review stopped."); return 130; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Console.Error.WriteLine("Application-folder review did not finish: " + error.Message); return 1; }
    }
}
