using KillerBench.Engine;

internal static class BackupServiceChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var scopes = Workflows.Scopes(WorkflowKind.BackupServices);
        if (!scopes.SequenceEqual(new[] { RepairKind.VolumeShadowCopy, RepairKind.SoftwareShadowProvider,
                RepairKind.WindowsBackupService, RepairKind.BlockLevelBackupEngineService }) ||
            WorkflowCatalog.Find("backup-services")?.Kind != WorkflowKind.BackupServices ||
            !Workflows.Preview(WorkflowKind.BackupServices).Contains("current snapshots and shadow-storage allocation", StringComparison.Ordinal) ||
            !scopes.All(scope => RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Any(recipe => recipe.Action == scope)))
            throw new Exception("Backup recovery scope is missing from its focused workflow or Full system scan.");
        foreach (var kind in scopes)
        {
            var raw = new ToolResult(0, "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\nSTATE : 1 STOPPED", "", false);
            if (ServiceRecovery.Interpret(kind, raw, 4).Repair is not null || ServiceRecovery.Interpret(kind, raw, 3).Repair is not null ||
                ServiceRecovery.Interpret(kind, raw with { Output = raw.Output + "\nWIN32_EXIT_CODE : 5" }, 2).Repair is not null)
                throw new Exception("Backup recovery ignored disabled configuration or access failure.");
            var enabled = ServiceRecovery.Interpret(kind, raw, 2);
            if (ServiceRecovery.AssessDependencies(enabled, [new("dependency", 4)], true).Repair is not null ||
                ServiceRecovery.AssessDependencies(enabled, [], false).Repair is not null)
                throw new Exception("Backup recovery ignored dependency blockers.");
            if ((!Repairs.Plan(kind).Effects.Contains("shadow copies") && !Repairs.Plan(kind).Effects.Contains("backup plans")) ||
                Repairs.Plan(kind).Steps.Any(step => step.Arguments[0] is not ("start" or "query")))
                throw new Exception("Backup recovery exceeded service-only scope.");
        }
        foreach (bool blocked in new[] { false, true })
        {
            var host = new Host(blocked);
            var result = await Workflows.RunAsync(WorkflowKind.BackupServices, true, scratch, host: host);
            if (host.Repaired.Count != (blocked ? 0 : scopes.Length) || (!blocked && result.Repairs.Any(repair => !Repairs.IsVerified(repair))))
                throw new Exception("Backup workflow lost prerequisite or verification gating.");
        }
        var inspectOnly = new Host(false);
        await Workflows.RunAsync(WorkflowKind.BackupServices, false, scratch, host: inspectOnly);
        if (inspectOnly.Repaired.Count != 0) throw new Exception("Backup inspection executed repair without selection.");
        Console.WriteLine("PASS: backup service scope, disabled/dependency blockers, workflow opt-in, prerequisite gating and verified simulated starts.");
    }

    private sealed class Host(bool blocked) : IWorkflowHost
    {
        public List<RepairKind> Repaired { get; } = [];
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([new("Restart", blocked ? CheckState.Attention : CheckState.Clear, "Fixture")]);
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Repairable, "Fixture", kind));
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        { Repaired.Add(kind); return Task.FromResult(RecipeChecks.Success(kind)); }
    }
}
