using System.Text.Json;
using KillerBench.Engine;

internal static class ApplicationFolderCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length is not (2 or 3) || args.Length == 3 && args[2] != "--report")
        {
            Console.Error.WriteLine("Usage: app-folder-review FOLDER_NAME [--report] (read-only exact local folder review; no deletion)");
            return 2;
        }
        try
        {
            var result = ApplicationFolderReview.Review(args[1]);
            Console.WriteLine(args.Length == 3 ? FormatReport(result) : JsonSerializer.Serialize(result, Capture.JsonOptions));
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Application-folder review stopped."); return 130; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Console.Error.WriteLine("Application-folder review did not finish: " + error.Message); return 1; }
    }

    internal static string FormatReport(ApplicationFolderReviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var lines = new List<string>
        {
            "APPLICATION FOLDER REVIEW",
            "Folder name: " + result.FolderName,
            "Coverage: " + (result.Complete ? "Complete" : "Incomplete"),
            "",
            "WHAT WAS FOUND"
        };
        lines.AddRange(result.Locations.Select(location => location.Area + ": " + location.State +
            (location.ReparsePoint is true ? " (reparse point)" : "") + ". " + location.Detail));
        lines.AddRange(["", "SCOPE AND LIMITS", result.Detail]);
        return string.Join(Environment.NewLine, lines);
    }
}
