namespace KillerBench.Engine.Workflows;

public static class FullSystemScan
{
    public static IReadOnlyList<DiagnosticArea> DiagnosticAreas => CommonDiagnosticCatalog.All;
    public static IReadOnlyList<RepairRecipe> RepairRecipes => RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems);

    public static Task<WorkflowResult> RunAsync(string journalFolder, bool autoRepair = false,
        IProgress<string>? progress = null, CancellationToken token = default, IWorkflowHost? host = null,
        IReadOnlyCollection<string>? selectedRecipes = null,
        IReadOnlyCollection<CommonDiagnosticScope>? selectedDiagnosticAreas = null) =>
        Workflows.RunAsync(WorkflowKind.CommonProblems, autoRepair, journalFolder, progress, token, host,
            selectedRecipes, selectedDiagnosticAreas);
}
