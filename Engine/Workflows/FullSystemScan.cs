namespace KillerBench.Engine.Workflows;

public static class FullSystemScan
{
    public static IReadOnlyList<DiagnosticArea> DiagnosticAreas => CommonDiagnosticCatalog.All;
    public static IReadOnlyList<RepairRecipe> RepairRecipes => RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems);

    public static Task<WorkflowResult> RunAsync(string journalFolder, bool autoRepair = false,
        IProgress<string>? progress = null, IWorkflowHost? host = null, IReadOnlyCollection<string>? selectedRecipes = null,
        IReadOnlyCollection<CommonDiagnosticScope>? selectedDiagnosticAreas = null,
        CancellationToken token = default) =>
        Workflows.RunAsync(WorkflowKind.CommonProblems, autoRepair, journalFolder, progress, host, selectedRecipes,
            selectedDiagnosticAreas, token: token);
}
