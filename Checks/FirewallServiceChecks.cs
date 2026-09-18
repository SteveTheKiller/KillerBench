using KillerBench.Engine;

internal static class FirewallServiceChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var recipes = RecipeCatalog.ForWorkflow(WorkflowKind.FirewallServices);
        Require(recipes.Select(r => r.Action).SequenceEqual(new[] { RepairKind.BaseFilteringEngine, RepairKind.WindowsFirewall }) &&
            recipes[1].Dependencies.SequenceEqual(new[] { recipes[0].Id }), "Firewall recipes require filtering engine first");
        Require(WorkflowCatalog.Find("firewall-services")?.Kind == WorkflowKind.FirewallServices &&
            recipes.All(firewall => RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Any(common => common.Id == firewall.Id)),
            "Full system scan includes the dependency-ordered firewall recovery pair");
        foreach (var recipe in recipes)
        {
            string service = ServiceRecovery.ServiceName(recipe.Action)!;
            var raw = new ToolResult(0, "SERVICE_NAME: " + service + "\nSTATE : 1 STOPPED", "", false);
            Require(ServiceRecovery.Interpret(recipe.Action, raw, 4).Repair is null &&
                ServiceRecovery.Interpret(recipe.Action, raw, 2).Repair == recipe.Action, "Disabled services remain unchanged");
            var enabled = ServiceRecovery.Interpret(recipe.Action, raw, 2);
            Require(ServiceRecovery.AssessDependencies(enabled, [new("dependency", 4)], true).Repair is null &&
                ServiceRecovery.AssessDependencies(enabled, [], false).Repair is null, "Dependency uncertainty prevents starts");
            Require(Repairs.Plan(recipe.Action).Steps.All(s => s.Arguments[0] is "start" or "query") &&
                Repairs.Plan(recipe.Action).Effects.Contains("interrupt network"), "Plans preserve configuration and disclose network effects");
        }
        foreach (bool blocked in new[] { false, true })
        {
            var host = new Host(blocked);
            var result = await Workflows.RunAsync(WorkflowKind.FirewallServices, true, scratch, host: host);
            Require(host.Repaired.Count == (blocked ? 0 : 2), "Failed filtering-engine check blocks dependent firewall dispatch");
            if (!blocked) Require(result.Repairs.All(Repairs.IsVerified), "Both service starts require verification");
        }
        var readOnly = new Host(false);
        await Workflows.RunAsync(WorkflowKind.FirewallServices, false, scratch, host: readOnly);
        Require(readOnly.Repaired.Count == 0, "Inspection never starts firewall services");
        foreach (string failure in new[] { "not-running", "restart", "timeout", "wrong-service" })
        {
            var interrupted = new Host(false) { FirstFailure = failure };
            var result = await Workflows.RunAsync(WorkflowKind.FirewallServices, true, scratch, host: interrupted);
            Require(interrupted.Repaired.SequenceEqual(new[] { RepairKind.BaseFilteringEngine }) &&
                interrupted.Checked.SequenceEqual(new[] { RepairKind.BaseFilteringEngine }) &&
                result.Recipes[1].Outcome == RecipeOutcome.Blocked && result.GetExitCode() != 0,
                "Unverified filtering-engine recovery prevents firewall inspection and start: " + failure);
            Require(result.Repairs.Count == 1 && !Repairs.IsVerified(result.Repairs[0]),
                "Failed filtering-engine evidence remains available without a success claim: " + failure);
            if (failure == "restart") Require(result.RequiresRestart, "Restart requirement survives workflow aggregation");
        }
        Console.WriteLine("PASS: firewall service selection, dependency ordering, disabled guards and simulated verification; no services changed.");
    }
    private sealed class Host(bool blocked) : IWorkflowHost
    {
        internal List<RepairKind> Repaired { get; } = [];
        internal List<RepairKind> Checked { get; } = [];
        internal string? FirstFailure { get; init; }
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([new("Restart", CheckState.Clear, "Fixture")]);
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
        {
            Checked.Add(kind);
            return Task.FromResult(new WorkflowCheck(kind.ToString(), blocked ? CheckState.Unknown : CheckState.Repairable, "Fixture", blocked ? null : kind));
        }
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        {
            Repaired.Add(kind);
            var result = RecipeChecks.Success(kind);
            if (kind == RepairKind.BaseFilteringEngine && FirstFailure is not null)
            {
                var steps = result.Steps.ToArray();
                var raw = steps[^1].Result;
                raw = FirstFailure switch
                {
                    "restart" => raw with { ExitCode = 3010 },
                    "timeout" => raw with { TimedOut = true },
                    "wrong-service" => raw with { Output = "SERVICE_NAME: MpsSvc\nSTATE : 4 RUNNING" },
                    _ => raw with { Output = "SERVICE_NAME: BFE\nSTATE : 2 START_PENDING" }
                };
                steps[^1] = steps[^1] with { Result = raw };
                result = result with { Steps = steps };
            }
            return Task.FromResult(result);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
