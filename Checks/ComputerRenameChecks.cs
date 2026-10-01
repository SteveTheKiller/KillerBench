using System.IO;
using KillerBench.Engine;

internal static class ComputerRenameChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string previewFolder = Folder(scratch, "preview");
        var previewHost = new Host();
        var preview = await ComputerRename.RunAsync("NEW-PC", false, previewFolder, previewHost);
        Require(preview.Outcome == ComputerRenameOutcome.Preview && preview.GetExitCode() == 0 && previewHost.Calls == 0,
            "Rename preview validates target without inspecting or changing the machine");
        string command = string.Join(" ", ComputerRename.Step("NEW-PC").Arguments);
        Require(command.Contains("Rename-Computer", StringComparison.Ordinal) && command.Contains("NEW-PC", StringComparison.Ordinal) &&
            !command.Contains("Restart-Computer", StringComparison.Ordinal) && !command.Contains("Credential", StringComparison.OrdinalIgnoreCase),
            "Rename plan uses the exact local name without restart, domain or credential actions");
        var clearRestart = new WorkflowCheck("Restart", CheckState.Clear, "No restart pending.", Evidence: new(0, "{}", "", false));
        Require(ComputerRename.ToWorkflowCheck("NEW-PC", Context("OLD-PC", "OLD-PC", 0, false), true, clearRestart) is
            { State: CheckState.Repairable, Repair: RepairKind.ComputerRename },
            "Complete standalone rename evidence exposes the guarded exact target");
        var selectedReview = ComputerRename.ReviewSelection("NEW-PC", Context("OLD-PC", "OLD-PC", 0, false), true, clearRestart);
        Require(selectedReview is { State: CheckState.Attention, Repair: null } &&
            selectedReview.Detail.Contains("does not authorize or request", StringComparison.Ordinal),
            "Shared computer-name review exposes exact readiness without granting rename authority");
        Require(ComputerRename.ReviewSelection("NEW-PC", Context("NEW-PC", "NEW-PC", 0, false), true, clearRestart) is
            { State: CheckState.Clear, Repair: null } &&
            ComputerRename.ReviewSelection("NEW-PC", new(null, null, null), true, clearRestart) is
            { State: CheckState.Unknown, Repair: null },
            "Shared computer-name review preserves clear and incomplete outcomes without repair authority");
        Require(ComputerRename.ToWorkflowCheck("NEW-PC", Context("NEW-PC", "NEW-PC", 0, false), true, clearRestart) is
            { State: CheckState.Clear, Repair: null },
            "An already active exact target is clear without a rename");
        Require(ComputerRename.ToWorkflowCheck("NEW-PC", new(null, null, null), true, clearRestart) is
            { State: CheckState.Unknown, Repair: null } &&
            ComputerRename.ToWorkflowCheck("NEW-PC", Context("OLD-PC", "OLD-PC", 1, false), true, clearRestart) is
            { State: CheckState.Attention, Repair: null } &&
            ComputerRename.ToWorkflowCheck("NEW-PC", Context("OLD-PC", "OLD-PC", 0, false), false, clearRestart) is
            { State: CheckState.Attention, Repair: null } &&
            ComputerRename.ToWorkflowCheck("NEW-PC", Context("OLD-PC", "NEW-PC", 0, false), true, clearRestart) is
            { State: CheckState.Unknown, Repair: null },
            "Incomplete identity, domain role, elevation and inconsistent configured identity never select rename");

        foreach (var host in new[]
        {
            new Host { Before = new(null, 0, null) },
            new Host { Before = Context("OLD-PC", "OLD-PC", 1, false) },
            new Host { Before = Context("OLD-PC", "NEW-PC", 0, true) },
            new Host { Before = Context("NEW-PC", "NEW-PC", 0, false) },
            new Host { RestartState = CheckState.Attention },
            new Host { RestartEvidence = new(0, "{}", "", false) { Truncated = true } },
            new Host { IsAdministrator = false }
        })
        {
            var result = await ComputerRename.RunAsync("NEW-PC", true, Folder(scratch, "blocked-" + Guid.NewGuid().ToString("N")), host);
            Require(result.Outcome == ComputerRenameOutcome.Blocked && host.Renames == 0,
                "Incomplete identity, domain role, existing rename, same name, restart and administrator gates block mutation");
        }

        string changedFolder = Folder(scratch, "changed-before-request");
        var changedHost = new Host { Immediate = Context("NEW-PC", "NEW-PC", 0, false) };
        var changed = await ComputerRename.RunAsync("NEW-PC", true, changedFolder, changedHost);
        Require(changed.Outcome == ComputerRenameOutcome.Blocked && changedHost.ContextReads == 2 &&
            changedHost.RestartReads == 2 && changedHost.Renames == 0 && changed.After?.CurrentName == "NEW-PC" &&
            !File.Exists(Path.Combine(changedFolder, "computer-rename.pending.json")) &&
            Directory.GetFiles(changedFolder, "*.prepared.json").Length == 1,
            "A target that becomes active after preparation records final evidence without another rename request");

        foreach (var (error, outcome) in new[]
        {
            (Error: (Exception)new IOException("final identity unavailable"), Outcome: ComputerRenameOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final identity canceled"), Outcome: ComputerRenameOutcome.Stopped)
        })
        {
            string folder = Folder(scratch, "final-unavailable-" + Guid.NewGuid().ToString("N"));
            var host = new Host { FinalContextError = error };
            var result = await ComputerRename.RunAsync("NEW-PC", true, folder, host);
            Require(result.Outcome == outcome && host.Renames == 0 &&
                !File.Exists(Path.Combine(folder, "computer-rename.pending.json")) &&
                Directory.GetFiles(folder, "*.prepared.json").Length == 1 &&
                result.Detail.Contains("No rename was requested", StringComparison.Ordinal),
                "Unavailable or canceled final rename evidence records an exact no-request outcome");
        }

        string restartFolder = Folder(scratch, "restart");
        var restartHost = new Host { After = Context("OLD-PC", "NEW-PC", 0, true) };
        var requested = await ComputerRename.RunAsync("NEW-PC", true, restartFolder, restartHost);
        Require(requested.Outcome == ComputerRenameOutcome.RestartRequired && requested.GetExitCode() == 4 && restartHost.Renames == 1 &&
            File.Exists(Path.Combine(restartFolder, "computer-rename.pending.json")),
            "Verified configured target reports restart required and retains the no-repeat guard");
        var repeat = await ComputerRename.RunAsync("OTHER-PC", true, restartFolder, new Host());
        Require(repeat.Outcome == ComputerRenameOutcome.Blocked, "Pending rename blocks a different target before inspection");
        var reconciled = await ComputerRename.ReconcileAsync(restartFolder, new Host
        {
            Before = Context("NEW-PC", "NEW-PC", 0, false), RestartState = CheckState.Clear
        });
        Require(reconciled.Outcome == ComputerRenameOutcome.Verified && reconciled.GetExitCode() == 0 &&
            !File.Exists(Path.Combine(restartFolder, "computer-rename.pending.json")),
            "Post-restart exact active and configured target clears the guard without repeating rename");

        string failedFolder = Folder(scratch, "failed");
        var failed = await ComputerRename.RunAsync("NEW-PC", true, failedFolder, new Host { Command = new(5, "", "denied", false) });
        Require(failed.Outcome == ComputerRenameOutcome.Uncertain && File.Exists(Path.Combine(failedFolder, "computer-rename.pending.json")),
            "Failed command retains pending reconciliation because dispatch occurred");
        var unchangedHost = new Host { Before = Context("OLD-PC", "OLD-PC", 0, false) };
        var unchanged = await ComputerRename.ReconcileAsync(failedFolder, unchangedHost);
        Require(unchanged.Outcome == ComputerRenameOutcome.NotChanged && unchangedHost.Renames == 0 &&
            !File.Exists(Path.Combine(failedFolder, "computer-rename.pending.json")),
            "Complete unchanged identity archives failed attempt without repeating rename");

        string incompleteFolder = Folder(scratch, "incomplete");
        await ComputerRename.RunAsync("NEW-PC", true, incompleteFolder, new Host { Command = new(5, "", "denied", false) });
        var incomplete = await ComputerRename.ReconcileAsync(incompleteFolder, new Host { Before = new(null, null, null) });
        Require(incomplete.Outcome == ComputerRenameOutcome.Uncertain && File.Exists(Path.Combine(incompleteFolder, "computer-rename.pending.json")),
            "Incomplete reconciliation retains guard and never repeats rename");

        string foreignFolder = Folder(scratch, "foreign-machine");
        await ComputerRename.RunAsync("NEW-PC", true, foreignFolder, new Host { Command = new(5, "", "denied", false) });
        string foreignPending = Path.Combine(foreignFolder, "computer-rename.pending.json");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        var foreignHost = new Host { Before = Context("NEW-PC", "NEW-PC", 0, false) };
        var foreign = await ComputerRename.ReconcileAsync(foreignFolder, foreignHost);
        Require(foreign.Outcome == ComputerRenameOutcome.Uncertain && foreignHost.Calls == 0 && File.Exists(foreignPending),
            "A foreign-machine rename record remains guarded without inspecting or renaming the computer");

        string stalledFolder = Folder(scratch, "stalled");
        var stalledHost = new Host { RenameTask = new TaskCompletionSource<ToolResult>().Task };
        var stalled = await ComputerRename.RunAsync("NEW-PC", true, stalledFolder, stalledHost, executionTimeout: TimeSpan.FromMilliseconds(20));
        Require(stalled.Outcome == ComputerRenameOutcome.Uncertain && stalledHost.Renames == 1 &&
            File.Exists(Path.Combine(stalledFolder, "computer-rename.pending.json")),
            "Bounded rename provider retains durable recovery when execution evidence never arrives");

        string canceledFolder = Folder(scratch, "request-canceled");
        var canceledHost = new Host { RenameTask = Task.FromCanceled<ToolResult>(new(true)) };
        var canceled = await ComputerRename.RunAsync("NEW-PC", true, canceledFolder, canceledHost);
        Require(canceled.Outcome == ComputerRenameOutcome.Stopped && canceledHost.Renames == 1 &&
            File.Exists(Path.Combine(canceledFolder, "computer-rename.pending.json")),
            "Canceled rename after request dispatch retains pending reconciliation");

        Console.WriteLine("PASS: local workgroup computer rename preview, repeated eligibility, durable dispatch, restart outcome and no-repeat reconciliation; simulated provider only.");
    }

    private static ComputerNameContext Context(string active, string configured, int role, bool pending) =>
        new(active, role, pending) { ConfiguredName = configured };
    private static string Folder(string scratch, string name) => Path.Combine(scratch, "computer-rename-" + name);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Host : IComputerRenameHost
    {
        public bool IsAdministrator { get; set; } = true;
        public ComputerNameContext Before { get; set; } = Context("OLD-PC", "OLD-PC", 0, false);
        public ComputerNameContext Immediate { get; set; } = Context("OLD-PC", "OLD-PC", 0, false);
        public ComputerNameContext After { get; set; } = Context("OLD-PC", "NEW-PC", 0, true);
        public CheckState RestartState { get; set; } = CheckState.Clear;
        public CheckState? ImmediateRestartState { get; set; }
        public ToolResult RestartEvidence { get; set; } = new(0, "{}", "", false);
        public Exception? FinalContextError { get; set; }
        public ToolResult Command { get; set; } = new(0, "fixture", "", false);
        public Task<ToolResult>? RenameTask { get; set; }
        public int ContextReads, RestartReads, Renames;
        public int Calls => ContextReads + RestartReads + Renames;
        public Task<ComputerNameContext> ReadContextAsync(CancellationToken token)
        {
            ContextReads++;
            if (ContextReads == 2 && FinalContextError is not null)
                return Task.FromException<ComputerNameContext>(FinalContextError);
            return Task.FromResult(ContextReads == 1 ? Before : ContextReads == 2 ? Immediate : After);
        }
        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
        {
            RestartReads++;
            var state = RestartReads == 1 ? RestartState : ImmediateRestartState ?? RestartState;
            return Task.FromResult(new WorkflowCheck("Restart", state, "fixture", Evidence: RestartEvidence));
        }
        public Task<ToolResult> RenameAsync(string targetName, CancellationToken token)
        {
            if (targetName != "NEW-PC" && targetName != "OTHER-PC") throw new Exception("Wrong rename target.");
            Renames++;
            return RenameTask ?? Task.FromResult(Command);
        }
    }
}
