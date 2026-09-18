using System.IO;
using KillerBench.Engine;

internal static class DebloatHibernationExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "debloat-hibernation");
        Require((await DebloatHibernationExecution.RunAsync(false, Path.Combine(root, "preview"), new Host([]))).Outcome == DebloatHibernationOutcome.Preview,
            "Hibernation change defaults to preview");
        var enabled = Evidence(true, true);
        var disabled = Evidence(false, false);
        var selection = new DebloatOptions(DebloatAreas.Hibernation, DebloatUserScope.RunningAccount);
        Require(DebloatHibernationEligibility.Assess(selection, false, false, enabled, true) is
            { State: CheckState.Repairable, Repair: RepairKind.HibernationDisable },
            "Exact unmanaged hibernation evidence exposes guarded recipe eligibility");
        Require(DebloatHibernationEligibility.Assess(selection, false, false, disabled, true) is
            { State: CheckState.Clear, Repair: null }, "Already disabled hibernation is clear");
        foreach (var blocked in new[]
        {
            DebloatHibernationEligibility.Assess(selection, true, false, enabled, true),
            DebloatHibernationEligibility.Assess(selection, false, true, enabled, true),
            DebloatHibernationEligibility.Assess(selection, false, false, enabled, false),
            DebloatHibernationEligibility.Assess(selection, false, false, Evidence(true, false), true)
        })
            Require(blocked.State != CheckState.Repairable && blocked.Repair is null,
                "Blocked hibernation evidence cannot select a change");
        foreach (var incomplete in new[]
        {
            DebloatHibernationEligibility.Assess(selection, false, null, enabled, true),
            DebloatHibernationEligibility.Assess(selection, false, false, enabled with { Complete = false }, true)
        })
            Require(incomplete.State == CheckState.Unknown && incomplete.Repair is null,
                "Incomplete hibernation eligibility fails closed");
        var eligibilityHost = new Host([enabled]);
        var collected = await DebloatHibernationEligibility.CheckAsync(selection,
            Path.Combine(root, "eligibility"), eligibilityHost);
        Require(collected is { State: CheckState.Repairable, Repair: RepairKind.HibernationDisable } && eligibilityHost.Commands == 0,
            "Read-only hibernation eligibility uses current policy and power evidence without mutation");
        var workflowEligibilityHost = new Host([enabled]);
        var workflowEligibility = await new WindowsWorkflowHost
        {
            DebloatOptions = selection,
            DebloatHibernationHost = workflowEligibilityHost,
            DebloatChangeFolder = Path.Combine(root, "workflow-eligibility")
        }.CheckIntegrityAsync(RepairKind.HibernationDisable, null, CancellationToken.None);
        Require(workflowEligibility is { State: CheckState.Repairable, Repair: RepairKind.HibernationDisable } &&
            workflowEligibilityHost.Commands == 0,
            "Shared workflow host exposes exact hibernation eligibility without dispatching a command");
        var host = new Host([enabled, enabled, disabled]);
        var verified = await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "verified"), host);
        Require(verified.Outcome == DebloatHibernationOutcome.Verified && verified.GetExitCode() == 0 && host.Commands == 1 &&
            !File.Exists(Path.Combine(root, "verified", "debloat-hibernation.pending.json")),
            "Exact enabled state is disabled once and verified with power settings preserved");
        var already = new Host([disabled]);
        Require((await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "already"), already)).Outcome == DebloatHibernationOutcome.Blocked && already.Commands == 0,
            "Already disabled hibernation is not rewritten");
        var contradictory = new Host([Evidence(true, false)]);
        Require((await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "contradictory"), contradictory)).Outcome == DebloatHibernationOutcome.Blocked && contradictory.Commands == 0,
            "Contradictory hibernation evidence blocks execution");
        var managed = new Host([enabled]) { Managed = true };
        Require((await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "managed"), managed)).Outcome == DebloatHibernationOutcome.Blocked && managed.Commands == 0,
            "Managed policy blocks hibernation changes");
        var denied = new Host([enabled]) { Administrator = false };
        Require((await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "denied"), denied)).Outcome == DebloatHibernationOutcome.Blocked && denied.Commands == 0,
            "Administrator gate precedes hibernation change");
        var finalDisabled = new Host([enabled, disabled]);
        var finalBlocked = await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "final-disabled"), finalDisabled);
        Require(finalBlocked.Outcome == DebloatHibernationOutcome.Blocked && finalDisabled.Commands == 0 &&
            finalBlocked.JournalPath is not null && File.Exists(finalBlocked.JournalPath) &&
            File.Exists(finalBlocked.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(root, "final-disabled", "debloat-hibernation.pending.json")),
            "Hibernation disabled after preparation is not rewritten and closes the prepared guard");
        foreach (var fixture in new[]
        {
            (Error: (Exception)new IOException("final hibernation evidence unavailable"), Outcome: DebloatHibernationOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final hibernation evidence canceled"), Outcome: DebloatHibernationOutcome.Stopped)
        })
        {
            string folder = Path.Combine(root, "final-unavailable-" + Guid.NewGuid().ToString("N"));
            var unavailable = new Host([enabled]) { ReadErrorAt = 2, ReadError = fixture.Error };
            var result = await DebloatHibernationExecution.RunAsync(true, folder, unavailable);
            Require(result.Outcome == fixture.Outcome && unavailable.Commands == 0 &&
                !File.Exists(Path.Combine(folder, "debloat-hibernation.pending.json")) &&
                File.Exists(result.JournalPath + ".prepared.json"),
                "Unavailable or canceled final hibernation evidence records an exact no-command outcome");
        }
        var changedTimer = new Host([enabled, enabled, disabled with { SleepAcSeconds = 1 }]);
        Require((await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "changed-timer"), changedTimer)).Outcome == DebloatHibernationOutcome.Uncertain,
            "Verification rejects collateral timer changes");
        var failed = new Host([enabled, enabled]) { Command = new(5, "", "failed", false) };
        var uncertain = await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "failed"), failed);
        Require(uncertain.Outcome == DebloatHibernationOutcome.Uncertain && File.Exists(Path.Combine(root, "failed", "debloat-hibernation.pending.json")),
            "Incomplete command outcome retains pending reconciliation");
        var canceledHost = new Host([enabled, enabled]) { DisableTask = Task.FromCanceled<ToolResult>(new(true)) };
        var canceled = await DebloatHibernationExecution.RunAsync(true, Path.Combine(root, "command-canceled"), canceledHost);
        Require(canceled.Outcome == DebloatHibernationOutcome.Stopped && canceledHost.Commands == 1 &&
            File.Exists(Path.Combine(root, "command-canceled", "debloat-hibernation.pending.json")),
            "Canceled hibernation command after dispatch retains pending reconciliation");
        Console.WriteLine("PASS: hibernation disable uses exact state, managed-policy and administrator gates, durable preparation, final eligibility and collateral-setting verification; simulated provider only.");
    }

    private static DebloatPowerEvidence Evidence(bool enabled, bool file) => new(
        "381b4222-f694-41f0-9685-ff5bb260df2e", 0, 3600, 3600, 900, enabled, file, true, "fixture");

    private sealed class Host(IEnumerable<DebloatPowerEvidence> values) : IDebloatHibernationHost
    {
        private readonly Queue<DebloatPowerEvidence> evidence = new(values);
        public bool Administrator = true;
        public bool? Managed = false;
        public int Commands;
        public int ReadCalls;
        public int ReadErrorAt;
        public Exception? ReadError;
        public ToolResult Command = new(0, "", "", false);
        public Task<ToolResult>? DisableTask;
        public bool IsAdministrator => Administrator;
        public Task<bool?> HasManagedPowerPolicyAsync(CancellationToken token) => Task.FromResult(Managed);
        public Task<DebloatPowerEvidence> ReadAsync(CancellationToken token)
        {
            ReadCalls++;
            return ReadCalls == ReadErrorAt && ReadError is not null
                ? Task.FromException<DebloatPowerEvidence>(ReadError)
                : Task.FromResult(evidence.Dequeue());
        }
        public Task<ToolResult> DisableAsync(CancellationToken token)
        {
            Commands++;
            return DisableTask ?? Task.FromResult(Command);
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
