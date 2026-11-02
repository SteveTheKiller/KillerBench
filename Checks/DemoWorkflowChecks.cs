using KillerBench.Engine;

internal static class DemoWorkflowChecks
{
    internal static void Run()
    {
        foreach (bool repair in new[] { false, true })
        {
            string report = WorkflowReport.Format(DemoWorkflow.Create(WorkflowKind.CoreWindowsServices, repair, screenshotPreset: true));
            Require(!System.Text.RegularExpressions.Regex.IsMatch(report, "demo|fabricated|synthetic|simulat", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                "Screenshot findings use realistic workstation data and normal report wording");
        }
        foreach (WorkflowKind kind in Enum.GetValues<WorkflowKind>())
        {
            var diagnosis = DemoWorkflow.Create(kind);
            Require(diagnosis.Kind == kind && diagnosis.Machine == DemoWorkflow.MachineName &&
                diagnosis.Status == "Needs attention" && diagnosis.Checks.Count == 2 && diagnosis.Repairs.Count == 0 &&
                diagnosis.JournalPath == "Not saved" && diagnosis.PlannedRecipeIds.Count == 0,
                "Every workflow has a non-mutating diagnostic demonstration");
            Require(!System.Text.RegularExpressions.Regex.IsMatch(WorkflowReport.Format(diagnosis),
                "demo|fabricated|synthetic|simulat", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                "Sample evidence uses normal workstation wording");

            var repaired = DemoWorkflow.Create(kind, simulateRepair: true);
            Require(repaired.Checks.Count == 3 && repaired.Repairs.Count == 0 && repaired.PlannedRecipeIds.Count == 0 &&
                repaired.Checks.Any(check => check.Name == "Repair verification" && check.State == CheckState.Clear),
                "Repair demonstrations create no dispatched repair result or planned command");
            Require(!System.Text.RegularExpressions.Regex.IsMatch(string.Join("\n", DemoWorkflow.Progress(kind, true)),
                "demo|fabricated|synthetic|simulat", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                "Progress uses the normal workflow presentation");

            var screenshot = DemoWorkflow.Create(kind, screenshotPreset: true);
            Require(screenshot.Machine == DemoWorkflow.MachineName &&
                screenshot.StartedUtc == DemoWorkflow.ScreenshotStartedUtc && screenshot.Repairs.Count == 0 &&
                screenshot.Checks.Count >= 2,
                "Every screenshot scenario uses fixed synthetic identity, time and evidence");
        }
        foreach (WorkflowKind kind in new[] { WorkflowKind.CommonProblems, WorkflowKind.WindowsUpdate,
                     WorkflowKind.Network, WorkflowKind.Printing })
        {
            var first = DemoWorkflow.Create(kind, screenshotPreset: true);
            var second = DemoWorkflow.Create(kind, screenshotPreset: true);
            Require(first.Checks.Count >= 5 && WorkflowReport.Format(first) == WorkflowReport.Format(second),
                "Featured screenshot reports are detailed and repeatable");
        }
        Require(KillerBench.App.IsDemoArgument("--demo") && KillerBench.App.IsDemoArgument("/DEMO") && !KillerBench.App.IsDemoArgument("demo"),
            "Only explicit family demo switches enable demo mode");
        Require(KillerBench.App.IsScreenshotArgument("--demo-screenshot") &&
            KillerBench.App.IsScreenshotArgument("/DEMO-SCREENSHOT") &&
            !KillerBench.App.IsScreenshotArgument("--demo-screenshot-other"),
            "Only the explicit screenshot switch enables the fixed presentation");
        Console.WriteLine("PASS: all workflows have synthetic demonstrations and four featured reports are repeatable.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
