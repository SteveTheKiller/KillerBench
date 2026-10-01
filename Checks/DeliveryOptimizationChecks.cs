using System.IO;
using KillerBench.Engine;

internal static class DeliveryOptimizationChecks
{
    private static readonly string[] QueryArguments = ["query", "DoSvc"];
    private static readonly string[] ContinueArguments = ["continue", "DoSvc"];

    internal static async Task RunAsync(string scratch)
    {
        static ToolResult State(int state, string label) => new(0, $"SERVICE_NAME: DoSvc\nSTATE : {state} {label}", "", false);
        Require(DeliveryOptimizationDiagnostics.Step() is { Tool: "sc.exe", Verification: true } step &&
            step.Arguments.SequenceEqual(QueryArguments), "Delivery Optimization uses an exact read-only service query");
        Require(DeliveryOptimizationDiagnostics.Interpret(State(1, "STOPPED")) is { State: CheckState.Information, Repair: null },
            "Stopped demand-start Delivery Optimization is informational");
        Require(DeliveryOptimizationDiagnostics.Interpret(State(4, "RUNNING")) is { State: CheckState.Information, Repair: null },
            "Running Delivery Optimization does not imply transfer or cache health");
        var paused = DeliveryOptimizationDiagnostics.Interpret(State(7, "PAUSED"));
        Require(paused is { State: CheckState.Repairable, Repair: RepairKind.DeliveryOptimization } &&
            DeliveryOptimizationRecovery.CanContinue(paused), "Only exact paused Delivery Optimization evidence selects continue recovery");
        Require(DeliveryOptimizationDiagnostics.AsReadOnly(paused) is { State: CheckState.Attention, Repair: null } readOnly &&
            readOnly.Detail.Contains("did not continue", StringComparison.Ordinal) && readOnly.Evidence == paused.Evidence,
            "Focused read-only inspection retains exact paused evidence without granting repair authority");
        Require(DeliveryOptimizationDiagnostics.Interpret(State(2, "START_PENDING")) is { State: CheckState.Information, Repair: null },
            "Transitional Delivery Optimization state remains read-only");
        foreach (var invalid in new[]
        {
            State(4, "RUNNING") with { Error = "Synthetic query error" },
            State(4, "RUNNING") with { Truncated = true },
            State(4, "RUNNING") with { TimedOut = true },
            State(4, "RUNNING") with { Output = "SERVICE_NAME: Other\nSTATE : 4 RUNNING" },
            State(4, "STOPPED")
        })
            Require(DeliveryOptimizationDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Incomplete or mismatched Delivery Optimization evidence remains unknown");
        var plan = Repairs.Plan(RepairKind.DeliveryOptimization);
        Require(plan.Steps.Count == 2 && plan.Steps[0].Arguments.SequenceEqual(ContinueArguments) &&
            plan.Steps[1].Arguments.SequenceEqual(QueryArguments) &&
            !RecipeCatalog.All.Single(recipe => recipe.Action == RepairKind.DeliveryOptimization).DiagnosticOnly,
            "Delivery Optimization recipe uses exact continue and verification commands");
        Require(WorkflowCatalog.Find("delivery-optimization-service")?.Kind == WorkflowKind.DeliveryOptimizationService &&
            Workflows.Scopes(WorkflowKind.DeliveryOptimizationService).SequenceEqual([RepairKind.DeliveryOptimization]) &&
            Workflows.Preview(WorkflowKind.DeliveryOptimizationService).Contains("Only an exact Paused state", StringComparison.Ordinal),
            "Focused Delivery Optimization is discoverable and exposes only the paused-only recipe");

        var calls = new List<RepairStep>();
        var queue = new Queue<ToolResult>([State(7, "PAUSED"), new(0, "", "", false)]);
        var continued = await DeliveryOptimizationRecovery.ExecuteAsync(plan.Steps[0], execute: (command, token) =>
        { token.ThrowIfCancellationRequested(); calls.Add(command); return Task.FromResult(queue.Dequeue()); });
        Require(continued.ExitCode == 0 && calls.Count == 2 && calls[0].Verification && !calls[1].Verification,
            "Continue execution repeats exact paused-state evidence before mutation");
        calls.Clear();
        try
        {
            await DeliveryOptimizationRecovery.ExecuteAsync(plan.Steps[0], execute: (command, token) =>
            { calls.Add(command); return Task.FromResult(State(4, "RUNNING")); });
            throw new Exception("Running Delivery Optimization returned fabricated continue evidence");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(skipped.Evidence.ExitCode == 0 && calls.Count == 1,
                "A changed state to Running skips the continue request with retained fresh evidence");
        }
        calls.Clear();
        try
        {
            await DeliveryOptimizationRecovery.ExecuteAsync(plan.Steps[0], execute: (command, token) =>
            { calls.Add(command); return Task.FromResult(State(1, "STOPPED")); });
            throw new Exception("Stopped Delivery Optimization returned command evidence");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(calls.Count == 1 && error.Message.Contains("No continue request was sent"),
                "A changed ineligible state defers the continue request without dispatch");
        }

        string recoveryFolder = Path.Combine(scratch, "delivery-optimization-recovery");
        var boot = new BootStamp(1, DateTimeOffset.Parse("2026-09-14T00:00:00Z"));
        var store = new RepairRecoveryStore(recoveryFolder, _ => Task.FromResult<BootStamp?>(boot));
        string journal = Path.Combine(recoveryFolder, "repair.json");
        Directory.CreateDirectory(recoveryFolder);
        await File.WriteAllTextAsync(journal, System.Text.Json.JsonSerializer.Serialize(
            new RepairResult(Environment.MachineName, RepairKind.DeliveryOptimization, DateTimeOffset.UtcNow,
                "Running", journal, []), Capture.JsonOptions));
        await store.BeginAsync(journal);
        var sameBoot = await DeliveryOptimizationRecovery.ReconcileAsync(store,
            _ => Task.FromResult(DeliveryOptimizationDiagnostics.Interpret(State(4, "RUNNING"))));
        Require(sameBoot.Outcome == DeliveryOptimizationReconciliationOutcome.Inconclusive,
            "Same-boot recovery does not infer completion");
        boot = new(2, DateTimeOffset.Parse("2026-09-15T00:00:00Z"));
        var reconciled = await DeliveryOptimizationRecovery.ReconcileAsync(store,
            _ => Task.FromResult(DeliveryOptimizationDiagnostics.Interpret(State(4, "RUNNING"))));
        Require(reconciled.Outcome == DeliveryOptimizationReconciliationOutcome.Verified &&
            (await store.ReadAsync())?.Pending == false, "Later-boot exact running state resolves without another continue request");
        Console.WriteLine("PASS: Delivery Optimization paused-only recovery, fresh eligibility, exact continue, verification plan and no-repeat reconciliation; simulated provider only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
