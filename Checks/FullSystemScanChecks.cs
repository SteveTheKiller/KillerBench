using KillerBench.Engine;

internal static class FullSystemScanChecks
{
    internal static async Task RunAsync(string folder)
    {
        var commonScopes = Workflows.Scopes(WorkflowKind.CommonProblems);
        var serviceScopes = RecipeCatalog.All.Where(recipe => ServiceRecovery.ServiceName(recipe.Action) is not null &&
                recipe.Action != RepairKind.WindowsUpdateMedicService)
            .Select(recipe => recipe.Action).Distinct().ToArray();
        Require(FullSystemScan.DiagnosticAreas.Count == 49 &&
            FullSystemScan.RepairRecipes.Count == commonScopes.Length &&
            FullSystemScan.RepairRecipes.Select(recipe => recipe.Action).SequenceEqual(commonScopes) &&
            serviceScopes.All(scope => commonScopes.Contains(scope)) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.BaseFilteringEngine) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.WindowsFirewall) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.TaskSchedulerService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.DiagnosticPolicyService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.WindowsErrorReportingService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.WindowsEventCollectorService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.PrintJobCancellation) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.WindowsImageAcquisitionService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.SystemEventNotificationService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.PowerService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.NetworkStoreInterfaceService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.RpcEndpointMapperService) &&
            FullSystemScan.RepairRecipes.Any(recipe => recipe.Action == RepairKind.CoreMessagingService) &&
            FullSystemScan.RepairRecipes.All(recipe => recipe.Action != RepairKind.WindowsUpdateMedicService) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.RestartState) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.ServicingTransitions) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.NetworkPathReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.StorageReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.PrintingReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.InstallationReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.ServiceReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.WindowsUpdateReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.BackupReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.SystemStabilityReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.SecurityReadiness) &&
            FullSystemScan.DiagnosticAreas.Any(area => area.Scope == CommonDiagnosticScope.ApplicationDeploymentReadiness),
            "Named full scan exposes the complete diagnostic and repair catalogs");
        var host = new Host();
        var progress = new List<string>();
        var result = await FullSystemScan.RunAsync(folder, progress: new InlineProgress(progress.Add), host: host,
            selectedDiagnosticAreas: [CommonDiagnosticScope.SystemVolumeCapacity]);
        Require(result.Kind == WorkflowKind.CommonProblems && result.SelectedDiagnosticScopes?.SequenceEqual(
                [CommonDiagnosticScope.SystemVolumeCapacity]) == true && result.PlannedRecipeIds.Count == 0 &&
            result.Repairs.Count == 0 && result.Checks.Count == 1 && host.Inspections == 1 &&
            progress.Any(message => message.Contains("Checking this PC's current state")),
            "Named full scan uses the journaled workflow and preserves focused read-only selection");
        Console.WriteLine("PASS: named full-system Engine entry point exposes complete catalogs and the journaled workflow without widening selections.");
    }

    private sealed class Host : IWorkflowHost
    {
        internal int Inspections;
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            throw new Exception("Scope-aware inspection expected");
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint,
            IReadOnlyList<CommonDiagnosticScope>? selectedScopes, CancellationToken token)
        {
            Inspections++;
            Require(kind == WorkflowKind.CommonProblems && selectedScopes?.SequenceEqual(
                [CommonDiagnosticScope.SystemVolumeCapacity]) == true, "Named full scan lost its selected area");
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>(
                [new("Volume capacity", CheckState.Information, "Fixture read-only capacity evidence")]);
        }
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Focused scan reached a repair check");
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Focused scan reached repair execution");
    }

    private sealed class InlineProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
