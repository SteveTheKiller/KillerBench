using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PreviousWindowsInstallationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "previous-windows");
        var eligible = Snapshot(true, true);
        var removed = Snapshot(false, false);
        Require(PreviousWindowsInstallationRemoval.Assess(eligible, true, false) is
            { State: CheckState.Repairable, Repair: RepairKind.PreviousWindowsInstallationRemoval },
            "Stable local rollback evidence is repairable");
        Require(PreviousWindowsInstallationRemoval.Assess(removed, true, false).State == CheckState.Clear,
            "Absent previous installation and rollback is clear");
        Require(PreviousWindowsInstallationRemoval.Assess(eligible with { DirectoryReparsePoint = true }, true, false).State == CheckState.Attention,
            "Reparse-point previous installation is blocked");
        Require(PreviousWindowsInstallationRemoval.Assess(eligible, false, false).State == CheckState.Attention,
            "Administrator access is required");
        Require(PreviousWindowsInstallationRemoval.Assess(eligible with { Complete = false }, true, false).State == CheckState.Unknown,
            "Incomplete rollback evidence fails closed");

        var previewHost = new Host([eligible]);
        var preview = await PreviousWindowsInstallationRemoval.RunAsync(false, false,
            Path.Combine(root, "preview"), previewHost);
        Require(preview.Outcome == PreviousWindowsRemovalOutcome.Preview && previewHost.Commands == 0,
            "Preview never requests rollback removal");

        var unconfirmedHost = new Host([eligible]);
        var unconfirmed = await PreviousWindowsInstallationRemoval.RunAsync(true, false,
            Path.Combine(root, "unconfirmed"), unconfirmedHost);
        Require(unconfirmed.Outcome == PreviousWindowsRemovalOutcome.Blocked && unconfirmedHost.Commands == 0,
            "Execution requires backup confirmation");

        var verifiedHost = new Host([eligible, eligible, eligible, removed]);
        string verifiedFolder = Path.Combine(root, "verified");
        var verified = await PreviousWindowsInstallationRemoval.RunAsync(true, true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == PreviousWindowsRemovalOutcome.Verified && verifiedHost.Commands == 1 &&
            !File.Exists(Path.Combine(verifiedFolder, PreviousWindowsInstallationRemoval.PendingFileName)),
            "One exact rollback-removal request is verified and archives its guard");

        var changedHost = new Host([eligible, eligible, removed]);
        string changedFolder = Path.Combine(root, "changed");
        var changed = await PreviousWindowsInstallationRemoval.RunAsync(true, true, changedFolder, changedHost);
        Require(changed.Outcome == PreviousWindowsRemovalOutcome.Blocked && changedHost.Commands == 0 &&
            !File.Exists(Path.Combine(changedFolder, PreviousWindowsInstallationRemoval.PendingFileName)),
            "Final eligibility changes close prepared state without dispatch");

        var failedHost = new Host([eligible, eligible, eligible])
        { Command = new(5, "", "failed", false) };
        string failedFolder = Path.Combine(root, "failed");
        var failed = await PreviousWindowsInstallationRemoval.RunAsync(true, true, failedFolder, failedHost);
        Require(failed.Outcome == PreviousWindowsRemovalOutcome.Uncertain && failedHost.Commands == 1 &&
            File.Exists(Path.Combine(failedFolder, PreviousWindowsInstallationRemoval.PendingFileName)),
            "Unverified dispatched removal retains pending recovery state");
        var recovered = await PreviousWindowsInstallationRemoval.ReconcileAsync(failedFolder, new Host([removed]));
        Require(recovered.Outcome == PreviousWindowsReconciliationOutcome.Verified &&
            !File.Exists(Path.Combine(failedFolder, PreviousWindowsInstallationRemoval.PendingFileName)),
            "Later evidence verifies removal without repeating the command");

        string unchangedFolder = Path.Combine(root, "unchanged");
        Directory.CreateDirectory(unchangedFolder);
        string unchangedPending = Path.Combine(unchangedFolder, PreviousWindowsInstallationRemoval.PendingFileName);
        await File.WriteAllTextAsync(unchangedPending, JsonSerializer.Serialize(new PreviousWindowsRemovalPendingRecord(
            1, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, Environment.MachineName, @"C:\Windows",
            @"C:\Windows.old", true), Capture.JsonOptions));
        var unchangedHost = new Host([eligible]);
        var unchanged = await PreviousWindowsInstallationRemoval.ReconcileAsync(unchangedFolder, unchangedHost);
        Require(unchanged.Outcome == PreviousWindowsReconciliationOutcome.NotChanged && unchangedHost.Commands == 0,
            "Later unchanged evidence resolves without retrying removal");

        var canceledHost = new Host([eligible, eligible, eligible])
        { RemoveTask = Task.FromCanceled<ToolResult>(new CancellationToken(true)) };
        string canceledFolder = Path.Combine(root, "canceled");
        var canceled = await PreviousWindowsInstallationRemoval.RunAsync(true, true, canceledFolder, canceledHost);
        Require(canceled.Outcome == PreviousWindowsRemovalOutcome.Stopped && canceledHost.Commands == 1 &&
            File.Exists(Path.Combine(canceledFolder, PreviousWindowsInstallationRemoval.PendingFileName)),
            "Cancellation after dispatch retains recovery state");

        var workflowHost = new Host([eligible]);
        var workflowCheck = await new WindowsWorkflowHost
        {
            PreviousWindowsInstallationHost = workflowHost,
            PreviousWindowsInstallationFolder = Path.Combine(root, "workflow")
        }.CheckIntegrityAsync(RepairKind.PreviousWindowsInstallationRemoval, null, CancellationToken.None);
        Require(workflowCheck is { State: CheckState.Repairable, Repair: RepairKind.PreviousWindowsInstallationRemoval } &&
            workflowHost.Commands == 0, "Cleanup workflow exposes read-only exact eligibility");
        Require(RecipeCatalog.ForWorkflow(WorkflowKind.Cleanup).Single().Id == "windows.previous-installation-removal" &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Cleanup).Single().RequiresExplicitOptions,
            "Cleanup catalog exposes explicit previous-Windows recipe");
        Console.WriteLine("PASS: previous Windows rollback removal uses exact eligibility, backup confirmation, durable preparation, no-repeat reconciliation and substituted execution only.");
    }

    private static PreviousWindowsInstallationSnapshot Snapshot(bool exists, bool rollback) =>
        new(true, exists, false, true, rollback, "fixture");

    private sealed class Host(IEnumerable<PreviousWindowsInstallationSnapshot> snapshots) : IPreviousWindowsInstallationHost
    {
        private readonly Queue<PreviousWindowsInstallationSnapshot> values = new(snapshots);
        public bool Administrator = true;
        public int Commands;
        public ToolResult Command = new(0, "", "", false);
        public Task<ToolResult>? RemoveTask;
        public bool IsAdministrator => Administrator;
        public Task<PreviousWindowsInstallationSnapshot> ReadAsync(CancellationToken token) => Task.FromResult(values.Dequeue());
        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token) =>
            Task.FromResult(new WorkflowCheck("Restart", CheckState.Clear, "fixture"));
        public Task<WorkflowCheck> ReadServicingActivityAsync(CancellationToken token) =>
            Task.FromResult(new WorkflowCheck("Servicing", CheckState.Information, "fixture"));
        public Task<ToolResult> RemoveRollbackAsync(CancellationToken token)
        {
            Commands++;
            return RemoveTask ?? Task.FromResult(Command);
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
