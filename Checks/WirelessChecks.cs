using KillerBench.Engine;

internal static class WirelessChecks
{
    internal static async Task RunAsync(string folder)
    {
        await CheckWorkflowAsync(folder, WorkflowKind.Wireless, RepairKind.WirelessAutoConfig, "wireless");
        await CheckWorkflowAsync(folder, WorkflowKind.WiredAuthentication, RepairKind.WiredAutoConfig, "wired-authentication");
    }
    private static async Task CheckWorkflowAsync(string folder, WorkflowKind workflow, RepairKind kind, string key)
    {
        string service = ServiceRecovery.ServiceName(kind)!;
        Require(WorkflowCatalog.Find(key)?.Kind == workflow &&
            Workflows.Scopes(workflow).SequenceEqual(new[] { kind }) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Any(recipe => recipe.Action == kind),
            "Wireless recovery remains available in its focused workflow and Full system scan");
        var stopped = new ToolResult(0, "SERVICE_NAME: " + service + "\nSTATE : 1 STOPPED", "", false);
        Require(ServiceRecovery.Interpret(kind, stopped, 4).Repair is null && ServiceRecovery.Interpret(kind, stopped, 2).Repair == kind,
            "Wireless recovery preserves disabled service configuration");
        foreach (bool execute in new[] { false, true })
        foreach (bool failed in new[] { false, true })
        {
            var host = new Host(failed);
            var result = await Workflows.RunAsync(workflow, execute, folder, host: host);
            Require(host.Calls == (execute ? 1 : 0), "Wireless workflow honors diagnostic-only selection");
            if (execute) Require(result.Repairs.Count == 1 && Repairs.IsVerified(result.Repairs[0]) == !failed,
                "Wireless workflow retains verified or failed service evidence");
        }
        foreach (string failure in new[] { "pending", "wrong-service", "timeout", "restart", "truncated" })
        {
            var host = new Host(false) { VerificationFailure = failure };
            var result = await Workflows.RunAsync(workflow, true, folder, host: host);
            Require(host.Calls == 1 && result.Repairs.Count == 1 && !Repairs.IsVerified(result.Repairs[0]) && result.GetExitCode() != 0,
                "Wireless recovery retains unverified evidence: " + failure);
            if (failure == "restart") Require(result.RequiresRestart, "Wireless restart survives workflow aggregation");
        }
        Console.WriteLine("PASS: " + key + " opt-in, disabled service guard and simulated verification failures; no network changes.");
    }
    private sealed class Host(bool failed) : IWorkflowHost
    {
        internal int Calls;
        internal string? VerificationFailure { get; init; }
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([new("Administrator", CheckState.Clear, "fixture")]);
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck(ServiceRecovery.ServiceName(kind)!, CheckState.Repairable, "Enabled stopped service fixture", kind));
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        {
            Calls++;
            var steps = Repairs.Plan(kind).Steps.Select(step => new RepairStepResult(step,
                new(failed ? 5 : 0, "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\nSTATE : 4 RUNNING", "", false))).ToArray();
            if (VerificationFailure is not null)
            {
                var raw = steps[^1].Result;
                raw = VerificationFailure switch
                {
                    "wrong-service" => raw with { Output = "SERVICE_NAME: Dhcp\nSTATE : 4 RUNNING" },
                    "timeout" => raw with { TimedOut = true },
                    "restart" => raw with { ExitCode = 3010 },
                    "truncated" => raw with { Truncated = true },
                    _ => raw with { Output = "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\nSTATE : 2 START_PENDING" }
                };
                steps[^1] = steps[^1] with { Result = raw };
            }
            return Task.FromResult(new RepairResult("fixture", kind, DateTimeOffset.UtcNow, failed ? "Command failed" : "Verification reported service running", "fixture", steps));
        }
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
