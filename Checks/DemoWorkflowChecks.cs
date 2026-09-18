using KillerBench.Engine;

internal static class DemoWorkflowChecks
{
    internal static void Run()
    {
        foreach (WorkflowKind kind in Enum.GetValues<WorkflowKind>())
        {
            var diagnosis = DemoWorkflow.Create(kind);
            Require(diagnosis.Kind == kind && diagnosis.Machine == DemoWorkflow.MachineName &&
                diagnosis.Status == "Needs attention" && diagnosis.Checks.Count == 2 && diagnosis.Repairs.Count == 0 &&
                diagnosis.JournalPath.StartsWith("Demo mode:", StringComparison.Ordinal),
                "Every workflow has a non-mutating diagnostic demonstration");
            Require(diagnosis.Checks.All(check => check.Evidence is null ||
                check.Evidence.Output.Contains("fabricated fixture", StringComparison.OrdinalIgnoreCase)),
                "Demo evidence is explicitly synthetic");

            var repaired = DemoWorkflow.Create(kind, simulateRepair: true);
            Require(repaired.Status == "Demo repair simulated" && repaired.Checks.Count == 3 && repaired.Repairs.Count == 0 &&
                repaired.Checks.Any(check => check.Name == "Simulated repair" && check.State == CheckState.Clear),
                "Repair demonstrations remain visibly simulated and create no repair result");
            Require(DemoWorkflow.Progress(kind, true).Last().Contains("No command was executed", StringComparison.Ordinal),
                "Demo progress states the execution boundary");
        }
        Require(KillerBench.App.IsDemoArgument("--demo") && KillerBench.App.IsDemoArgument("/DEMO") && !KillerBench.App.IsDemoArgument("demo"),
            "Only explicit family demo switches enable demo mode");
        Console.WriteLine("PASS: every workflow has deterministic synthetic diagnosis and repair progress without providers, commands or journals.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
