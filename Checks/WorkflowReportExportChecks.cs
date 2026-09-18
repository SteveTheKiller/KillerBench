using KillerBench.Engine;

internal static class WorkflowReportExportChecks
{
    internal static void Run()
    {
        var repair = RecipeChecks.Success(RepairKind.SystemFiles);
        var skippedStep = new RepairStepResult(new("sfc.exe", ["/scannow"], false),
            new(0, "Fresh evidence no longer authorizes repair.", "", false)) { CommandDispatched = false };
        var skipped = new RepairResult("FIXTURE-PC", RepairKind.SystemFiles,
            new DateTimeOffset(2026, 9, 14, 8, 31, 0, TimeSpan.Zero), "Skipped after final eligibility check",
            "private-skipped.json", [skippedStep]);
        var problem = new WorkflowCheck("System <files>", CheckState.Repairable,
            "Windows found <script>alert('fixture')</script> damage.", RepairKind.SystemFiles);
        var verdict = new WorkflowCheck("Network path readiness verdict", CheckState.Attention,
            "One network finding requires review. No repair is selected by this summary.");
        var result = new WorkflowResult(WorkflowKind.SystemFiles, "FIXTURE-PC", new DateTimeOffset(2026, 9, 14, 8, 30, 0, TimeSpan.Zero),
            "Needs attention", [verdict, problem, new("Restart", CheckState.Clear, "No restart is pending.")], [repair, skipped], @"C:\Users\fixture\private\journal.json")
        {
            Recipes = [new("windows.system-files", 1, RecipeOutcome.Repaired, problem, repair, "Repair completed and verified.")]
        };

        Require(WorkflowReport.NextAction(result) == "Review the unresolved findings below. No broader repair has been selected automatically." &&
            WorkflowReport.Format(result).Contains("NEXT: " + WorkflowReport.NextAction(result)),
            "Report next action uses the same decision without rebuilding formatted output.");

        string diagnostic = WorkflowReportExport.BuildHtml(result, WorkflowReportScope.Diagnostics, "Dark");
        Require(diagnostic.StartsWith("<!DOCTYPE html>", StringComparison.Ordinal) && diagnostic.Contains("Diagnostic report") &&
            diagnostic.Contains("<h2>Start here</h2>") && diagnostic.Contains("Review the unresolved findings below") &&
            diagnostic.Contains("Area outcomes") && diagnostic.Contains("Problems and review items") && diagnostic.Contains("Completed checks") &&
            diagnostic.Split("Network path readiness verdict", StringSplitOptions.None).Length == 2 && diagnostic.Contains("class='item verdict'"),
            "Diagnostic HTML is a structured standalone report");
        Require(diagnostic.Contains("&lt;script&gt;alert(&#39;fixture&#39;)&lt;/script&gt;") && !diagnostic.Contains("<script>alert('fixture')</script>"),
            "Report values are HTML encoded");
        Require(!diagnostic.Contains("Steps taken") && !diagnostic.Contains("private\\journal.json"),
            "Diagnostic report omits repair execution details and private journal paths");

        string full = WorkflowReportExport.BuildHtml(result, WorkflowReportScope.DiagnosticsAndRepairs, "retro98");
        Require(full.Contains("Diagnostic and repair report") && full.Contains("Actions and results") &&
            full.Contains("Steps and dispatch status") && full.Contains("Verification") && full.Contains("theme-se98") &&
            full.Contains("Command plan (not dispatched)") && full.Contains("Not dispatched. Final eligibility changed"),
            "Full HTML separates actions, results, and verification and accepts the app theme name");
        foreach (string theme in new[] { "Dark", "Light", "Black", "Blood", "Greed", "Cyanotic", "98SE", "Ectoplasm", "Decay", "Malaise", "Sepulchre", "Delirium", "Mourning" })
            Require(full.Contains("'" + theme + "'"), "HTML includes report theme: " + theme);
        foreach (string accent in new[] { "red", "orange", "green", "teal", "blue", "purple" })
            Require(full.Contains("['" + accent + "'"), "HTML includes report accent: " + accent);
        var restartRecipe = result.Recipes[0] with { Outcome = RecipeOutcome.RestartRequired, Repair = null, Detail = "Restart pending." };
        Require(WorkflowReportExport.BuildHtml(result with { Repairs = [], Recipes = [restartRecipe] }, WorkflowReportScope.DiagnosticsAndRepairs).Contains("Restart required</h2>"),
            "A recipe restart requirement is visible in the full HTML report");
        var restartRepair = repair with
        {
            Status = "Restart required",
            Steps = [repair.Steps[0] with { Result = new ToolResult(3010, "Restart required", "", false) }]
        };
        Require(WorkflowReportExport.BuildHtml(result with { Repairs = [restartRepair] }, WorkflowReportScope.DiagnosticsAndRepairs).Contains("Restart required</h2>"),
            "A conventional repair restart requirement is visible in the full HTML report");

        string text = WorkflowReportExport.BuildText(result, WorkflowReportScope.DiagnosticsAndRepairs);
        Require(text.Contains("START HERE") && text.Contains("Review the unresolved findings below") &&
            text.Contains("PROBLEMS AND REVIEW ITEMS") && text.Contains("ACTIONS AND RESULTS") &&
            text.Contains("AREA OUTCOMES") && text.Split("Network path readiness verdict", StringSplitOptions.None).Length == 2 &&
            text.Contains("Verification:") && text.Contains("Command plan (not dispatched): sfc.exe /scannow") &&
            text.Contains("Outcome: Not dispatched. Final eligibility changed") && !text.Contains(result.JournalPath) && !text.Contains(skipped.JournalPath),
            "Text report retains readable sections without exposing the journal path");
        var prioritized = result with
        {
            Checks =
            [
                new("Unknown item", CheckState.Unknown, "Fixture incomplete item"),
                new("Attention item", CheckState.Attention, "Fixture attention item"),
                new("Repairable item", CheckState.Repairable, "Fixture repairable item", RepairKind.SystemFiles)
            ]
        };
        string prioritizedHtml = WorkflowReportExport.BuildHtml(prioritized, WorkflowReportScope.Diagnostics);
        string prioritizedText = WorkflowReportExport.BuildText(prioritized, WorkflowReportScope.Diagnostics);
        Require(prioritizedHtml.IndexOf("Repairable item", StringComparison.Ordinal) < prioritizedHtml.IndexOf("Attention item", StringComparison.Ordinal) &&
            prioritizedHtml.IndexOf("Attention item", StringComparison.Ordinal) < prioritizedHtml.IndexOf("Unknown item", StringComparison.Ordinal) &&
            prioritizedText.IndexOf("Repairable item", StringComparison.Ordinal) < prioritizedText.IndexOf("Attention item", StringComparison.Ordinal) &&
            prioritizedText.IndexOf("Attention item", StringComparison.Ordinal) < prioritizedText.IndexOf("Unknown item", StringComparison.Ordinal),
            "HTML and text reports order actionable findings before attention and incomplete evidence");
        try { WorkflowReportExport.BuildHtml(result, WorkflowReportScope.Diagnostics, "unknown"); throw new Exception("Unknown theme was accepted."); }
        catch (ArgumentException) { }
        try { WorkflowReportExport.BuildHtml(result, WorkflowReportScope.Diagnostics, "dark", "unknown"); throw new Exception("Unknown accent was accepted."); }
        catch (ArgumentException) { }
        Console.WriteLine("PASS: structured workflow reports, report scope, themes, encoding, dispatch truth, verification, and path privacy.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
