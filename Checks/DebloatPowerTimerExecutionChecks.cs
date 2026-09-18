using System.IO;
using KillerBench.Engine;

internal static class DebloatPowerTimerExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var request = new DebloatPowerRequest(0, 60, 60, 15);
        string root = Path.Combine(scratch, "debloat-power-timers");
        var preview = await DebloatPowerTimerExecution.RunAsync(request, false, Path.Combine(root, "preview"), new Host([]));
        Require(preview.Outcome == DebloatPowerTimerOutcome.Preview, "Power timers default to preview");

        var before = Evidence(10, 20, 30, 40);
        var after = Evidence(0, 60, 60, 15);
        var selection = new DebloatOptions(DebloatAreas.SleepAndDisplayTimers,
            DebloatUserScope.RunningAccount, PowerRequest: request);
        Require(DebloatPowerTimerEligibility.Assess(selection, false, false, before, true) is
            { State: CheckState.Repairable, Repair: RepairKind.PowerTimers },
            "Exact unmanaged power evidence exposes guarded timer eligibility");
        Require(DebloatPowerTimerEligibility.Assess(selection, false, false, after, true) is
            { State: CheckState.Clear, Repair: null }, "Matching timer values are clear");
        foreach (var blocked in new[]
        {
            DebloatPowerTimerEligibility.Assess(selection, true, false, before, true),
            DebloatPowerTimerEligibility.Assess(selection, false, true, before, true),
            DebloatPowerTimerEligibility.Assess(selection, false, false, before, false)
        })
            Require(blocked.State != CheckState.Repairable && blocked.Repair is null,
                "Blocked timer evidence cannot select a change");
        foreach (var incomplete in new[]
        {
            DebloatPowerTimerEligibility.Assess(selection, false, null, before, true),
            DebloatPowerTimerEligibility.Assess(selection, false, false, before with { Complete = false }, true),
            DebloatPowerTimerEligibility.Assess(selection, false, false, before with { HibernationEnabled = null }, true)
        })
            Require(incomplete.State == CheckState.Unknown && incomplete.Repair is null,
                "Incomplete timer eligibility fails closed");
        var eligibilityHost = new Host([before]);
        var collected = await DebloatPowerTimerEligibility.CheckAsync(selection,
            Path.Combine(root, "eligibility"), eligibilityHost);
        Require(collected is { State: CheckState.Repairable, Repair: RepairKind.PowerTimers } && eligibilityHost.Settings.Count == 0,
            "Read-only timer eligibility uses current policy and power evidence without mutation");
        var workflowEligibilityHost = new Host([before]);
        var workflowEligibility = await new WindowsWorkflowHost
        {
            DebloatOptions = selection,
            DebloatPowerTimerHost = workflowEligibilityHost,
            DebloatChangeFolder = Path.Combine(root, "workflow-eligibility")
        }.CheckIntegrityAsync(RepairKind.PowerTimers, null, CancellationToken.None);
        Require(workflowEligibility is { State: CheckState.Repairable, Repair: RepairKind.PowerTimers } &&
            workflowEligibilityHost.Settings.Count == 0,
            "Shared workflow host exposes exact timer eligibility without dispatching commands");
        var host = new Host([before, before, after]);
        var verified = await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "verified"), host);
        Require(verified.Outcome == DebloatPowerTimerOutcome.Verified && verified.GetExitCode() == 0 && host.Settings.SequenceEqual(new[]
        {
            DebloatPowerTimerSetting.SleepAc, DebloatPowerTimerSetting.SleepDc,
            DebloatPowerTimerSetting.DisplayAc, DebloatPowerTimerSetting.DisplayDc
        }) && !File.Exists(Path.Combine(root, "verified", "debloat-power-timers.pending.json")),
            "All four selected timers require exact same-plan verification");

        var managed = new Host([before]) { Managed = true };
        Require((await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "managed"), managed)).Outcome == DebloatPowerTimerOutcome.Blocked && managed.Settings.Count == 0,
            "Managed power policy blocks timer changes");
        var denied = new Host([before]) { Administrator = false };
        Require((await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "denied"), denied)).Outcome == DebloatPowerTimerOutcome.Blocked && denied.Settings.Count == 0,
            "Administrator gate precedes timer changes");
        var already = new Host([after]);
        Require((await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "already"), already)).Outcome == DebloatPowerTimerOutcome.Blocked && already.Settings.Count == 0,
            "Matching timers are not rewritten");
        var finalMatch = new Host([before, after]);
        var finalBlocked = await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "final-match"), finalMatch);
        Require(finalBlocked.Outcome == DebloatPowerTimerOutcome.Blocked && finalMatch.Settings.Count == 0 &&
            finalBlocked.JournalPath is not null && File.Exists(finalBlocked.JournalPath) &&
            File.Exists(finalBlocked.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(root, "final-match", "debloat-power-timers.pending.json")),
            "Timers changed after preparation are not rewritten and close the prepared guard");
        foreach (var fixture in new[]
        {
            (Error: (Exception)new IOException("final timer evidence unavailable"), Outcome: DebloatPowerTimerOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final timer evidence canceled"), Outcome: DebloatPowerTimerOutcome.Stopped)
        })
        {
            string folder = Path.Combine(root, "final-unavailable-" + Guid.NewGuid().ToString("N"));
            var unavailable = new Host([before]) { ReadErrorAt = 2, ReadError = fixture.Error };
            var result = await DebloatPowerTimerExecution.RunAsync(request, true, folder, unavailable);
            Require(result.Outcome == fixture.Outcome && unavailable.Settings.Count == 0 &&
                !File.Exists(Path.Combine(folder, "debloat-power-timers.pending.json")) &&
                File.Exists(result.JournalPath + ".prepared.json"),
                "Unavailable or canceled final timer evidence records an exact no-command outcome");
        }
        var partial = new Host([before, before]) { FailAt = 2 };
        var uncertain = await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "partial"), partial);
        Require(uncertain.Outcome == DebloatPowerTimerOutcome.Uncertain && partial.Settings.Count == 2 &&
            File.Exists(Path.Combine(root, "partial", "debloat-power-timers.pending.json")),
            "Partial command sequence remains uncertain and blocks retry");
        var canceledHost = new Host([before, before]) { ApplyTask = Task.FromCanceled<ToolResult>(new(true)) };
        var canceled = await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "command-canceled"), canceledHost);
        Require(canceled.Outcome == DebloatPowerTimerOutcome.Stopped && canceledHost.Settings.Count == 1 &&
            File.Exists(Path.Combine(root, "command-canceled", "debloat-power-timers.pending.json")),
            "Canceled power-timer command after dispatch retains pending reconciliation");
        var changedPlan = new Host([before, before, after with { ActiveSchemeId = "a1841308-3541-4fab-bc81-f71556f20b4a" }]);
        Require((await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "changed-plan"), changedPlan)).Outcome == DebloatPowerTimerOutcome.Uncertain,
            "Verification rejects an active-plan identity change");
        var changedHibernation = new Host([before, before, after with { HibernationEnabled = true, HibernationFilePresent = true }]);
        Require((await DebloatPowerTimerExecution.RunAsync(request, true, Path.Combine(root, "changed-hibernation"), changedHibernation)).Outcome == DebloatPowerTimerOutcome.Uncertain,
            "Verification rejects collateral hibernation changes");
        Console.WriteLine("PASS: power timers use managed-policy and administrator gates, durable preparation, final eligibility, bounded commands and exact same-plan verification; simulated provider only.");
    }

    private static DebloatPowerEvidence Evidence(uint sleepAc, uint sleepDc, uint displayAc, uint displayDc) => new(
        "381b4222-f694-41f0-9685-ff5bb260df2e", sleepAc * 60, sleepDc * 60, displayAc * 60, displayDc * 60,
        false, false, true, "fixture");

    private sealed class Host(IEnumerable<DebloatPowerEvidence> values) : IDebloatPowerTimerHost
    {
        private readonly Queue<DebloatPowerEvidence> evidence = new(values);
        public bool Administrator = true;
        public bool? Managed = false;
        public int FailAt;
        public int ReadCalls;
        public int ReadErrorAt;
        public Exception? ReadError;
        public Task<ToolResult>? ApplyTask;
        public List<DebloatPowerTimerSetting> Settings = [];
        public bool IsAdministrator => Administrator;
        public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => Task.FromResult(Managed);
        public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token)
        {
            ReadCalls++;
            return ReadCalls == ReadErrorAt && ReadError is not null
                ? Task.FromException<DebloatPowerEvidence>(ReadError)
                : Task.FromResult(evidence.Dequeue());
        }
        public Task<ToolResult> ApplyAsync(DebloatPowerTimerSetting setting, uint minutes, CancellationToken token)
        {
            Settings.Add(setting);
            return ApplyTask ?? Task.FromResult(Settings.Count == FailAt
                ? new ToolResult(5, "", "failed", false)
                : new ToolResult(0, "", "", false));
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
