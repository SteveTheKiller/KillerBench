using System.IO;
using KillerBench.Engine;

internal static class DebloatPowerPlanExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "debloat-power-plan");
        var change = DebloatPowerPlanExecution.CreateHighPerformance();
        var preview = await DebloatPowerPlanExecution.RunAsync(change, false, Path.Combine(root, "preview"), new Host([]));
        Require(preview.Outcome == DebloatPowerPlanOutcome.Preview, "Power-plan change defaults to preview");

        var highOff = Evidence(false);
        var highOn = Evidence(true);
        var host = new Host([highOff, highOff, highOn]);
        var verified = await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "verified"), host);
        Require(verified.Outcome == DebloatPowerPlanOutcome.Verified && verified.GetExitCode() == 0 && host.Activations == 1 &&
            host.ActivatedId == WindowsDebloatPowerPlanReader.HighPerformanceId && File.Exists(verified.JournalPath) &&
            !File.Exists(Path.Combine(root, "verified", "debloat-power-plan.pending.json")),
            "Exact eligible plan activates once and requires fresh exact verification");

        var active = new Host([highOn]);
        Require((await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "active"), active)).Outcome == DebloatPowerPlanOutcome.Blocked && active.Activations == 0,
            "Already active plan is not changed");
        var managed = new Host([highOff]) { Managed = true };
        Require((await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "managed"), managed)).Outcome == DebloatPowerPlanOutcome.Blocked && managed.Activations == 0,
            "Managed power policy blocks activation");
        var unknownPolicy = new Host([highOff]) { Managed = null };
        Require((await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "unknown-policy"), unknownPolicy)).Outcome == DebloatPowerPlanOutcome.Blocked && unknownPolicy.Activations == 0,
            "Unknown power-policy ownership fails closed");
        var denied = new Host([highOff]) { Administrator = false };
        Require((await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "denied"), denied)).Outcome == DebloatPowerPlanOutcome.Blocked && denied.Activations == 0,
            "Administrator gate precedes activation");
        var finalActive = new Host([highOff, highOn]);
        var finalBlocked = await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "final-active"), finalActive);
        Require(finalBlocked.Outcome == DebloatPowerPlanOutcome.Blocked && finalActive.Activations == 0 &&
            finalBlocked.JournalPath is not null && File.Exists(finalBlocked.JournalPath) &&
            File.Exists(finalBlocked.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(root, "final-active", "debloat-power-plan.pending.json")),
            "Plan activated after preparation is not rewritten and closes the prepared guard");
        foreach (var fixture in new[]
        {
            (Error: (Exception)new IOException("final inventory unavailable"), Outcome: DebloatPowerPlanOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final inventory canceled"), Outcome: DebloatPowerPlanOutcome.Stopped)
        })
        {
            string folder = Path.Combine(root, "final-unavailable-" + Guid.NewGuid().ToString("N"));
            var unavailable = new Host([highOff]) { ReadErrorAt = 2, ReadError = fixture.Error };
            var result = await DebloatPowerPlanExecution.RunAsync(change, true, folder, unavailable);
            Require(result.Outcome == fixture.Outcome && unavailable.Activations == 0 &&
                !File.Exists(Path.Combine(folder, "debloat-power-plan.pending.json")) &&
                File.Exists(result.JournalPath + ".prepared.json"),
                "Unavailable or canceled final power-plan evidence records an exact no-command outcome");
        }
        var failed = new Host([highOff, highOff]) { Command = new(5, "", "denied", false) };
        var failure = await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "failed"), failed);
        Require(failure.Outcome == DebloatPowerPlanOutcome.Uncertain && File.Exists(Path.Combine(root, "failed", "debloat-power-plan.pending.json")),
            "Incomplete command evidence retains pending reconciliation");
        var canceledHost = new Host([highOff, highOff]) { ActivationTask = Task.FromCanceled<ToolResult>(new(true)) };
        var canceled = await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "command-canceled"), canceledHost);
        Require(canceled.Outcome == DebloatPowerPlanOutcome.Stopped && canceledHost.Activations == 1 &&
            File.Exists(Path.Combine(root, "command-canceled", "debloat-power-plan.pending.json")),
            "Canceled power-plan activation after dispatch retains pending reconciliation");
        var mismatch = new Host([highOff, highOff, highOff]);
        var uncertain = await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "mismatch"), mismatch);
        Require(uncertain.Outcome == DebloatPowerPlanOutcome.Uncertain && uncertain.GetExitCode() == 3,
            "Completed command without exact active-plan verification remains uncertain");
        var changedRegistrations = new Host([highOff, highOff, new([
            new DebloatPowerPlan(WindowsDebloatPowerPlanReader.HighPerformanceId, "High performance", true)],
            new DebloatPowerPlan(WindowsDebloatPowerPlanReader.HighPerformanceId, "High performance", true), true,
            new(0, "fixture", "", false), "changed")]);
        Require((await DebloatPowerPlanExecution.RunAsync(change, true, Path.Combine(root, "changed-registrations"), changedRegistrations)).Outcome == DebloatPowerPlanOutcome.Uncertain,
            "Changed plan registrations prevent verified activation");
        Require(DebloatPowerPlanEligibility.Assess(new(DebloatAreas.HighPerformancePower, DebloatUserScope.RunningAccount), false, false, highOff, true) is
            { State: CheckState.Repairable, Repair: RepairKind.HighPerformancePowerPlan },
            "Fresh unmanaged exact evidence exposes focused plan activation");
        var eligibilityHost = new Host([highOff]);
        var collected = await DebloatPowerPlanEligibility.CheckAsync(
            new(DebloatAreas.HighPerformancePower, DebloatUserScope.RunningAccount), Path.Combine(root, "eligibility"), eligibilityHost);
        Require(collected is { State: CheckState.Repairable, Repair: RepairKind.HighPerformancePowerPlan } && eligibilityHost.Activations == 0,
            "Eligibility collection remains read only");
        var workflowHost = new WindowsWorkflowHost
        {
            DebloatOptions = new(DebloatAreas.HighPerformancePower, DebloatUserScope.RunningAccount),
            DebloatPowerPlanHost = new Host([highOff]),
            DebloatChangeFolder = Path.Combine(root, "workflow-eligibility")
        };
        Require(await workflowHost.CheckIntegrityAsync(RepairKind.HighPerformancePowerPlan, null, CancellationToken.None) is
            { State: CheckState.Repairable, Repair: RepairKind.HighPerformancePowerPlan },
            "Shared workflow host exposes focused plan eligibility without activation");
        Console.WriteLine("PASS: High performance activation uses managed-policy and administrator gates, durable preparation, final eligibility, exact registered-plan verification and focused recipe eligibility; simulated provider only.");
    }

    private static DebloatPowerPlanEvidence Evidence(bool active)
    {
        var high = new DebloatPowerPlan(WindowsDebloatPowerPlanReader.HighPerformanceId, "High performance", active);
        var balanced = new DebloatPowerPlan("381b4222-f694-41f0-9685-ff5bb260df2e", "Balanced", !active);
        return new([balanced, high], high, true, new(0, "fixture", "", false), active ? "active" : "available");
    }

    private sealed class Host(Queue<DebloatPowerPlanEvidence> evidence) : IDebloatPowerPlanChangeHost
    {
        public Host(IEnumerable<DebloatPowerPlanEvidence> evidence) : this(new Queue<DebloatPowerPlanEvidence>(evidence)) { }
        public bool Administrator = true;
        public bool? Managed = false;
        public bool IsAdministrator => Administrator;
        public int Activations;
        public string? ActivatedId;
        public ToolResult Command = new(0, "", "", false);
        public int ReadCalls;
        public int ReadErrorAt;
        public Exception? ReadError;
        public Task<ToolResult>? ActivationTask;
        public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => Task.FromResult(Managed);
        public Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ReadCalls++;
            if (ReadCalls == ReadErrorAt && ReadError is not null)
                return Task.FromException<DebloatPowerPlanEvidence>(ReadError);
            return Task.FromResult(evidence.Dequeue());
        }
        public Task<ToolResult> ActivateAsync(string planId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Activations++;
            ActivatedId = planId;
            return ActivationTask ?? Task.FromResult(Command);
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
