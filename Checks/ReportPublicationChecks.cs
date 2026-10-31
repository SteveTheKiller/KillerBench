using System.IO;
using KillerBench.Engine;
using KillerBench.Engine.Workflows;

internal static class ReportPublicationChecks
{
    internal static void Run(string scratch)
    {
        string folder = Path.Combine(scratch, "report-publication-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "report.txt");
        WorkflowReportExport.PublishAsync(path, "first", false).GetAwaiter().GetResult();
        bool refused = false;
        try { WorkflowReportExport.PublishAsync(path, "second", false).GetAwaiter().GetResult(); }
        catch (IOException) { refused = true; }
        if (!refused || File.ReadAllText(path) != "first") throw new Exception("A no-overwrite report replaced a competing file.");
        WorkflowReportExport.PublishAsync(path, "third", true).GetAwaiter().GetResult();
        if (File.ReadAllText(path) != "third" || Directory.GetFiles(folder).Length != 1)
            throw new Exception("Explicit overwrite failed or left a temporary report behind.");

        var machine = "fixture";
        WorkflowResult Result(RecipeOutcome outcome) => new(WorkflowKind.Profiles, machine, DateTimeOffset.UtcNow, "Needs attention", [], [], "fixture.json")
        { Recipes = [new RecipeResult("profiles.fixture", 1, outcome, null, null, "UNIQUE-RECIPE-DETAIL")] };
        foreach (var outcome in new[] { RecipeOutcome.Blocked, RecipeOutcome.Canceled, RecipeOutcome.Unknown, RecipeOutcome.RestartRequired })
        {
            var result = Result(outcome);
            if (!WorkflowReportExport.BuildText(result, WorkflowReportScope.DiagnosticsAndRepairs).Contains("UNIQUE-RECIPE-DETAIL") ||
                !WorkflowReportExport.BuildHtml(result, WorkflowReportScope.DiagnosticsAndRepairs).Contains("UNIQUE-RECIPE-DETAIL"))
                throw new Exception("A recipe-only outcome is missing from an export: " + outcome);
        }
        var repeated = Result(RecipeOutcome.Blocked) with { Checks = [new WorkflowCheck("fixture check", CheckState.Unknown, "UNIQUE-RECIPE-DETAIL")] };
        if (WorkflowReportExport.BuildText(repeated, WorkflowReportScope.DiagnosticsAndRepairs).Split("UNIQUE-RECIPE-DETAIL").Length != 2)
            throw new Exception("A recipe detail already shown by a check was repeated.");

        var saved = Result(RecipeOutcome.Blocked);
        string text = WorkflowReportExport.BuildText(saved, WorkflowReportScope.Diagnostics, "<journal>.json");
        string html = WorkflowReportExport.BuildHtml(saved, WorkflowReportScope.Diagnostics, importedFrom: "<journal>.json");
        foreach (string report in new[] { text, html })
            if (!report.Contains("RECORDED", StringComparison.OrdinalIgnoreCase) || !report.Contains("no current verification", StringComparison.OrdinalIgnoreCase))
                throw new Exception("An imported journal export does not identify itself as recorded history.");
        if (html.Contains("<journal>", StringComparison.Ordinal) || !html.Contains("&lt;journal&gt;", StringComparison.Ordinal))
            throw new Exception("The imported source name was not escaped.");
        if (html.Contains("Verified repairs", StringComparison.Ordinal))
            throw new Exception("An imported report kept the live verified heading.");
        if (WorkflowReportExport.BuildText(saved, WorkflowReportScope.Diagnostics).Contains("no current verification", StringComparison.OrdinalIgnoreCase))
            throw new Exception("A live report carries the imported label.");
        Console.WriteLine("PASS: reports publish without replacement, keep recipe-only outcomes in both formats, and label imported journals as recorded history.");
    }
}
