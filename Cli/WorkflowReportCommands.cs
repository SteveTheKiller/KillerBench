using System.Text.Json;
using KillerBench.Engine;

internal static class WorkflowReportCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 3 || args.Length > 9)
        {
            Console.Error.WriteLine("Usage: workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]");
            return 2;
        }
        bool includeRepairs = args.Contains("--include-repairs", StringComparer.Ordinal);
        bool overwrite = args.Contains("--overwrite", StringComparer.Ordinal);
        int themeIndex = Array.IndexOf(args, "--theme");
        if (themeIndex >= 0 && themeIndex + 1 >= args.Length) { Console.Error.WriteLine("--theme requires a theme name."); return 2; }
        int accentIndex = Array.IndexOf(args, "--accent");
        if (accentIndex >= 0 && accentIndex + 1 >= args.Length) { Console.Error.WriteLine("--accent requires an accent name."); return 2; }
        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] is "--theme" or "--accent") { i++; continue; }
            if (args[i] is not ("--include-repairs" or "--overwrite"))
            { Console.Error.WriteLine("Unknown option: " + args[i]); return 2; }
        }
        string theme = themeIndex >= 0 ? args[themeIndex + 1] : "dark";
        string accent = accentIndex >= 0 ? args[accentIndex + 1] : "orange";
        string extension = Path.GetExtension(args[2]);
        if (!extension.Equals(".html", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        { Console.Error.WriteLine("Output must use .html or .txt."); return 2; }
        if (File.Exists(args[2]) && !overwrite) { Console.Error.WriteLine("Output already exists. Use --overwrite to replace it."); return 2; }
        try
        {
            var snapshot = await WorkflowJournalReview.ReadAsync(args[1]);
            var workflow = snapshot.Record.Deserialize<WorkflowResult>(Capture.JsonOptions)
                ?? throw new InvalidDataException("Workflow record is empty.");
            var scope = includeRepairs ? WorkflowReportScope.DiagnosticsAndRepairs : WorkflowReportScope.Diagnostics;
            string output = extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                ? WorkflowReportExport.BuildHtml(workflow, scope, theme, accent)
                : WorkflowReportExport.BuildText(workflow, scope);
            await File.WriteAllTextAsync(args[2], output);
            Console.WriteLine(Path.GetFullPath(args[2]));
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or ArgumentException)
        { Console.Error.WriteLine("Report export unavailable: " + error.Message); return 1; }
    }
}
