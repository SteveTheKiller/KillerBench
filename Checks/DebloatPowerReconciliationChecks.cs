using System.IO;
using KillerBench.Engine;

internal static class DebloatPowerReconciliationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "debloat-power-reconciliation");
        var highOff = Plans(false);
        var highOn = Plans(true);
        string planFolder = Path.Combine(root, "plan");
        var failedPlan = new PlanHost([highOff, highOff]) { Command = new(5, "", "failed", false) };
        await DebloatPowerPlanExecution.RunAsync(DebloatPowerPlanExecution.CreateHighPerformance(), true, planFolder, failedPlan);
        var planReconciled = await DebloatPowerReconciliation.RunPlanAsync(planFolder, new PlanHost([highOn]));
        Require(planReconciled.Outcome == DebloatPowerReconciliationOutcome.Verified && planReconciled.ArchivePath is not null &&
            !File.Exists(Path.Combine(planFolder, "debloat-power-plan.pending.json")),
            "Pending power-plan activation is resolved from fresh evidence without another command");

        string timerFolder = Path.Combine(root, "timers");
        var request = new DebloatPowerRequest(0, 60, 60, 15);
        var before = Power(true, true, 10, 20, 30, 40);
        var target = Power(true, true, 0, 60, 60, 15);
        var partialTimers = new TimerHost([before, before]) { FailAt = 2 };
        await DebloatPowerTimerExecution.RunAsync(request, true, timerFolder, partialTimers);
        var timerReconciled = await DebloatPowerReconciliation.RunTimersAsync(timerFolder, new TimerHost([target]));
        Require(timerReconciled.Outcome == DebloatPowerReconciliationOutcome.Verified && !File.Exists(Path.Combine(timerFolder, "debloat-power-timers.pending.json")),
            "Pending timer sequence is resolved from all four current values without another command");

        string hibernationFolder = Path.Combine(root, "hibernation");
        var enabled = Power(true, true, 0, 60, 60, 15);
        var failedHibernation = new HibernationHost([enabled, enabled]) { Command = new(5, "", "failed", false) };
        await DebloatHibernationExecution.RunAsync(true, hibernationFolder, failedHibernation);
        var hibernationReconciled = await DebloatPowerReconciliation.RunHibernationAsync(hibernationFolder,
            new HibernationHost([Power(false, false, 0, 60, 60, 15)]));
        Require(hibernationReconciled.Outcome == DebloatPowerReconciliationOutcome.Verified &&
            !File.Exists(Path.Combine(hibernationFolder, "debloat-hibernation.pending.json")),
            "Pending hibernation change is resolved with collateral settings preserved and no repeated command");

        string changedFolder = Path.Combine(root, "changed");
        var changedTimers = new TimerHost([before, before]) { FailAt = 1 };
        await DebloatPowerTimerExecution.RunAsync(request, true, changedFolder, changedTimers);
        var changed = await DebloatPowerReconciliation.RunTimersAsync(changedFolder, new TimerHost([before]));
        Require(changed.Outcome == DebloatPowerReconciliationOutcome.CurrentStateDiffers &&
            !File.Exists(Path.Combine(changedFolder, "debloat-power-timers.pending.json")),
            "Complete differing timer state is archived without retrying the change");

        string malformedFolder = Path.Combine(root, "malformed");
        Directory.CreateDirectory(malformedFolder);
        string malformed = Path.Combine(malformedFolder, "debloat-power-timers.pending.json");
        await File.WriteAllTextAsync(malformed, "{\"Schema\":1,\"Schema\":1}");
        var rejected = await DebloatPowerReconciliation.RunTimersAsync(malformedFolder, new TimerHost([]));
        Require(rejected.Outcome == DebloatPowerReconciliationOutcome.Inconclusive && File.Exists(malformed),
            "Malformed or conflicting pending records remain retained and never dispatch a command");

        string nullFolder = Path.Combine(root, "null-request");
        await DebloatPowerTimerExecution.RunAsync(request, true, nullFolder, new TimerHost([before, before]) { FailAt = 1 });
        string nullPending = Path.Combine(nullFolder, "debloat-power-timers.pending.json");
        var nullRecord = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(nullPending))!.AsObject();
        nullRecord["request"] = null;
        await File.WriteAllTextAsync(nullPending, nullRecord.ToJsonString());
        byte[] invalidBytes = await File.ReadAllBytesAsync(nullPending);
        var nullHost = new TimerHost([]);
        var nullResult = await DebloatPowerReconciliation.RunTimersAsync(nullFolder, nullHost);
        byte[] retainedBytes = await File.ReadAllBytesAsync(nullPending);
        Require(nullResult.Outcome == DebloatPowerReconciliationOutcome.Inconclusive && nullHost.Reads == 0 &&
            invalidBytes.SequenceEqual(retainedBytes),
            "An explicit null power request is retained without provider calls or an escaping exception");

        string foreignFolder = Path.Combine(root, "foreign-machine");
        await DebloatPowerTimerExecution.RunAsync(request, true, foreignFolder, new TimerHost([before, before]) { FailAt = 1 });
        string foreignPending = Path.Combine(foreignFolder, "debloat-power-timers.pending.json");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        var foreignHost = new TimerHost([target]);
        var foreign = await DebloatPowerReconciliation.RunTimersAsync(foreignFolder, foreignHost);
        Require(foreign.Outcome == DebloatPowerReconciliationOutcome.Inconclusive && foreignHost.Reads == 0 &&
            File.Exists(foreignPending), "A foreign-machine power record remains guarded without inspecting or changing the host");
        Console.WriteLine("PASS: pending declutter power operations reconcile from bounded fresh evidence without repeating changes; scratch records and simulated providers only.");
    }

    private static DebloatPowerPlanEvidence Plans(bool active)
    {
        var high = new DebloatPowerPlan(WindowsDebloatPowerPlanReader.HighPerformanceId, "High performance", active);
        var balanced = new DebloatPowerPlan("381b4222-f694-41f0-9685-ff5bb260df2e", "Balanced", !active);
        return new([balanced, high], high, true, new(0, "fixture", "", false), "fixture");
    }
    private static DebloatPowerEvidence Power(bool enabled, bool file, uint sleepAc, uint sleepDc, uint displayAc, uint displayDc) => new(
        "381b4222-f694-41f0-9685-ff5bb260df2e", sleepAc * 60, sleepDc * 60, displayAc * 60, displayDc * 60,
        enabled, file, true, "fixture");

    private sealed class PlanHost(IEnumerable<DebloatPowerPlanEvidence> values) : IDebloatPowerPlanChangeHost
    {
        private readonly Queue<DebloatPowerPlanEvidence> evidence = new(values);
        public bool IsAdministrator => true;
        public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => Task.FromResult<bool?>(false);
        public ToolResult Command = new(0, "", "", false);
        public Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token) => Task.FromResult(evidence.Dequeue());
        public Task<ToolResult> ActivateAsync(string planId, CancellationToken token) => Task.FromResult(Command);
    }
    private sealed class TimerHost(IEnumerable<DebloatPowerEvidence> values) : IDebloatPowerTimerHost
    {
        private readonly Queue<DebloatPowerEvidence> evidence = new(values);
        private int commands;
        public int Reads;
        public int FailAt;
        public bool IsAdministrator => true;
        public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => Task.FromResult<bool?>(false);
        public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token) { Reads++; return Task.FromResult(evidence.Dequeue()); }
        public Task<ToolResult> ApplyAsync(DebloatPowerTimerSetting setting, uint minutes, CancellationToken token) =>
            Task.FromResult(++commands == FailAt ? new ToolResult(5, "", "failed", false) : new ToolResult(0, "", "", false));
    }
    private sealed class HibernationHost(IEnumerable<DebloatPowerEvidence> values) : IDebloatHibernationHost
    {
        private readonly Queue<DebloatPowerEvidence> evidence = new(values);
        public ToolResult Command = new(0, "", "", false);
        public bool IsAdministrator => true;
        public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => Task.FromResult<bool?>(false);
        public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token) => Task.FromResult(evidence.Dequeue());
        public Task<ToolResult> DisableAsync(CancellationToken token) => Task.FromResult(Command);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
