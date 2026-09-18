using System.Text.Json;
using KillerBench.Engine;

internal static class DuplicateFileCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine("Usage: duplicate-files FOLDER [--min-bytes N] [--max-files N] [--report] (read-only exact SHA-256 duplicate review)"); return 2; }
        long minimumBytes = 1;
        int maximumFiles = 10_000;
        bool report = false;
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--min-bytes" && i + 1 < args.Length && long.TryParse(args[++i], out var bytes)) minimumBytes = bytes;
            else if (args[i] == "--max-files" && i + 1 < args.Length && int.TryParse(args[++i], out var files)) maximumFiles = files;
            else if (args[i] == "--report" && !report) report = true;
            else { Console.Error.WriteLine("Usage: duplicate-files FOLDER [--min-bytes N] [--max-files N] [--report] (read-only exact SHA-256 duplicate review)"); return 2; }
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var result = await DuplicateFileReview.ScanAsync(args[1], minimumBytes, maximumFiles, stop.Token);
            Console.WriteLine(report ? FormatReport(result) : JsonSerializer.Serialize(result, Capture.JsonOptions));
            return result.Complete ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Duplicate review stopped."); return 130; }
        catch (Exception error) when (error is ArgumentException or DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine("Duplicate review did not finish: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static string FormatReport(DuplicateFileReviewResult result)
    {
        var lines = new List<string> { "DUPLICATE FILE REVIEW", "Files examined: " + result.FilesExamined, "Coverage: " + (result.Complete ? "Complete" : "Incomplete"), "Duplicate groups: " + result.Groups.Count, "" };
        foreach (var group in result.Groups)
        {
            lines.Add("Group: " + group.Files.Count + " matching files, " + group.Length + " bytes each.");
            lines.AddRange(group.Files.Select(file => "  " + file.Path));
        }
        lines.AddRange(["", "SCOPE AND LIMITS", result.Detail, "No file was changed or selected for deletion."]);
        return string.Join(Environment.NewLine, lines);
    }
}
