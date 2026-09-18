using System.IO;
using KillerBench.Engine;

internal static class FileSystemRepairExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var draft = FileSystemRepairPlanning.Create("D:", FileSystemRepairMode.SpotFix,
            new(true, true, "NTFS"), false, true, true);
        var previewHost = new Host();
        var preview = await FileSystemRepairExecution.RunAsync(draft, false, Path.Combine(scratch, "fs-preview"), previewHost);
        Require(preview.Outcome == FileSystemRepairOutcome.Preview && previewHost.Calls == 0, "Preview does not inspect or repair");

        var noBackup = FileSystemRepairPlanning.Create("D:", FileSystemRepairMode.SpotFix,
            new(true, true, "NTFS"), false, true);
        var noBackupHost = new Host();
        var noBackupResult = await FileSystemRepairExecution.RunAsync(noBackup, true,
            Path.Combine(scratch, "fs-no-backup"), noBackupHost);
        Require(noBackupResult.Outcome == FileSystemRepairOutcome.Blocked && noBackupHost.Calls == 0 &&
            noBackupResult.Detail.Contains("backed up", StringComparison.OrdinalIgnoreCase),
            "Missing backup confirmation blocks filesystem repair before native inspection");

        var system = FileSystemRepairPlanning.Create("C:", FileSystemRepairMode.SpotFix,
            new(true, true, "NTFS"), true, false, true);
        string systemFolder = Path.Combine(scratch, "fs-system");
        var systemHost = new Host { Before = Broken("C:"), After = Clean("C:"),
            Boot = new(10, DateTimeOffset.UtcNow.AddHours(-2)) };
        var scheduled = await FileSystemRepairExecution.RunAsync(system, true, systemFolder, systemHost);
        Require(scheduled.Outcome == FileSystemRepairOutcome.RestartRequired && scheduled.GetExitCode() == 4 &&
            systemHost.Schedules == 1 && systemHost.Repairs == 0 && systemHost.RestartChecks == 2 &&
            File.Exists(Path.Combine(systemFolder, "filesystem-repair.pending.json")),
            "System-volume repair is durably scheduled without restarting or claiming completion");
        var awaiting = await FileSystemRepairReconciliation.RunAsync(systemFolder, systemHost);
        Require(awaiting.Outcome == FileSystemRepairReconciliationOutcome.AwaitingRestart && awaiting.GetExitCode() == 4 &&
            systemHost.Inspections == 2 && File.Exists(Path.Combine(systemFolder, "filesystem-repair.pending.json")),
            "System-volume reconciliation retains the guard before a newer startup");
        systemHost.Boot = new(11, DateTimeOffset.UtcNow.AddMinutes(1));
        systemHost.Diagnoses = 0;
        systemHost.Before = Clean("C:");
        var systemVerified = await FileSystemRepairReconciliation.RunAsync(systemFolder, systemHost);
        Require(systemVerified.Outcome == FileSystemRepairReconciliationOutcome.Verified && systemHost.Schedules == 1 &&
            !File.Exists(Path.Combine(systemFolder, "filesystem-repair.pending.json")) && File.Exists(systemVerified.ArchivePath),
            "A newer startup and fresh clean exact-volume evidence reconcile a scheduled system repair without repeating it");

        var restartBlockedHost = new Host { Before = Broken("C:"), Restart = RestartPending() };
        var restartBlocked = await FileSystemRepairExecution.RunAsync(system, true, Path.Combine(scratch, "fs-system-restart-blocked"), restartBlockedHost);
        Require(restartBlocked.Outcome == FileSystemRepairOutcome.Blocked && restartBlockedHost.Schedules == 0,
            "An existing restart requirement blocks system-volume scheduling");

        foreach (var host in new[] { new Host { State = new(false, true, "NTFS") }, new Host { Administrator = false }, new Host { Before = Clean() } })
        {
            var result = await FileSystemRepairExecution.RunAsync(draft, true,
                Path.Combine(scratch, "fs-blocked-" + Guid.NewGuid().ToString("N")), host);
            Require(result.Outcome == FileSystemRepairOutcome.Blocked && host.Repairs == 0, "Changed volume, clean evidence and standard user block repair");
        }

        string verifiedFolder = Path.Combine(scratch, "fs-verified");
        var verifiedHost = new Host();
        var verified = await FileSystemRepairExecution.RunAsync(draft, true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == FileSystemRepairOutcome.Verified && verified.GetExitCode() == 0 && verifiedHost.Repairs == 1 &&
            verifiedHost.Inspections == 2 && verifiedHost.Diagnoses == 3 &&
            !File.Exists(Path.Combine(verifiedFolder, "filesystem-repair.pending.json")) &&
            File.Exists(verified.JournalPath) && File.Exists(verified.JournalPath + ".prepared.json"),
            "Exact volume repair requires fresh diagnosis and clean verification");

        string finalCleanFolder = Path.Combine(scratch, "fs-final-clean");
        var finalCleanHost = new Host { FinalBefore = Clean() };
        var finalClean = await FileSystemRepairExecution.RunAsync(draft, true, finalCleanFolder, finalCleanHost);
        Require(finalClean.Outcome == FileSystemRepairOutcome.Blocked && finalCleanHost.Repairs == 0 &&
            finalClean.JournalPath is not null && File.Exists(finalClean.JournalPath) &&
            File.Exists(finalClean.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(finalCleanFolder, "filesystem-repair.pending.json")),
            "Volume that became clean after preparation is not repaired and closes the prepared guard");
        var finalChangedVolume = new Host { FinalState = new(false, true, "NTFS") };
        Require((await FileSystemRepairExecution.RunAsync(draft, true, Path.Combine(scratch, "fs-final-volume"), finalChangedVolume)).Outcome ==
            FileSystemRepairOutcome.Blocked && finalChangedVolume.Repairs == 0,
            "Volume readiness changed after preparation blocks repair");
        var finalDenied = new Host { AdministratorAfterPreparation = false };
        Require((await FileSystemRepairExecution.RunAsync(draft, true, Path.Combine(scratch, "fs-final-admin"), finalDenied)).Outcome ==
            FileSystemRepairOutcome.Blocked && finalDenied.Repairs == 0,
            "Administrator access changed after preparation blocks repair");

        foreach (var fixture in new[]
        {
            (Error: (Exception)new IOException("final inspection unavailable"), Outcome: FileSystemRepairOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final inspection canceled"), Outcome: FileSystemRepairOutcome.Stopped)
        })
        {
            string folder = Path.Combine(scratch, "fs-final-unavailable-" + Guid.NewGuid().ToString("N"));
            var host = new Host { FinalInspectionError = fixture.Error };
            var result = await FileSystemRepairExecution.RunAsync(draft, true, folder, host);
            Require(result.Outcome == fixture.Outcome && host.Repairs == 0 && host.Schedules == 0 &&
                !File.Exists(Path.Combine(folder, "filesystem-repair.pending.json")) &&
                File.Exists(result.JournalPath + ".prepared.json") &&
                result.Detail.Contains("No repair was started or scheduled", StringComparison.Ordinal),
                "Unavailable or canceled final volume inspection records an exact no-command outcome");
        }

        foreach (var command in new[] { new ToolResult(3, "", "", false), new ToolResult(0, "", "", true) })
        {
            string folder = Path.Combine(scratch, "fs-command-" + Guid.NewGuid().ToString("N"));
            var result = await FileSystemRepairExecution.RunAsync(draft, true, folder, new Host { Command = command });
            Require(result.Outcome is FileSystemRepairOutcome.Failed or FileSystemRepairOutcome.Uncertain &&
                File.Exists(Path.Combine(folder, "filesystem-repair.pending.json")), "Failed or incomplete repair remains pending");
        }

        string timeoutFolder = Path.Combine(scratch, "fs-timeout");
        var stalled = new Host { RepairTask = new TaskCompletionSource<ToolResult>().Task };
        var timed = await FileSystemRepairExecution.RunAsync(draft, true, timeoutFolder, stalled,
            executionTimeout: TimeSpan.FromMilliseconds(20));
        Require(timed.Outcome == FileSystemRepairOutcome.Uncertain && File.Exists(Path.Combine(timeoutFolder, "filesystem-repair.pending.json")),
            "Stalled repair provider is bounded and retained for reconciliation");

        string canceledFolder = Path.Combine(scratch, "fs-command-canceled");
        var canceledHost = new Host { RepairTask = Task.FromCanceled<ToolResult>(new(true)) };
        var canceled = await FileSystemRepairExecution.RunAsync(draft, true, canceledFolder, canceledHost);
        Require(canceled.Outcome == FileSystemRepairOutcome.Stopped && canceledHost.Repairs == 1 &&
            File.Exists(Path.Combine(canceledFolder, "filesystem-repair.pending.json")),
            "Canceled repair after dispatch retains pending reconciliation");

        string recoveredFolder = Path.Combine(scratch, "fs-reconcile-clean");
        await FileSystemRepairExecution.RunAsync(draft, true, recoveredFolder, new Host { Command = new(3, "", "", false) });
        var recoveredHost = new Host { Before = Clean() };
        var recovered = await FileSystemRepairReconciliation.RunAsync(recoveredFolder, recoveredHost);
        Require(recovered.Outcome == FileSystemRepairReconciliationOutcome.Verified && recovered.GetExitCode() == 0 &&
            recoveredHost.Repairs == 0 && !File.Exists(Path.Combine(recoveredFolder, "filesystem-repair.pending.json")) &&
            File.Exists(recovered.ArchivePath) && File.Exists(recovered.ArchivePath + ".prepared.json"),
            "Reconciliation archives a clean exact volume without repeating repair");

        string damagedFolder = Path.Combine(scratch, "fs-reconcile-damaged");
        await FileSystemRepairExecution.RunAsync(draft, true, damagedFolder, new Host { Command = new(3, "", "", false) });
        var damagedHost = new Host();
        var damaged = await FileSystemRepairReconciliation.RunAsync(damagedFolder, damagedHost);
        Require(damaged.Outcome == FileSystemRepairReconciliationOutcome.StillDamaged && damaged.GetExitCode() == 3 &&
            damagedHost.Repairs == 0 && !File.Exists(Path.Combine(damagedFolder, "filesystem-repair.pending.json")),
            "Complete damaged state is archived without claiming success or repeating repair");

        string incompleteFolder = Path.Combine(scratch, "fs-reconcile-incomplete");
        await FileSystemRepairExecution.RunAsync(draft, true, incompleteFolder, new Host { Command = new(3, "", "", false) });
        var incompleteHost = new Host { State = new(false, true, "NTFS") };
        var incomplete = await FileSystemRepairReconciliation.RunAsync(incompleteFolder, incompleteHost);
        Require(incomplete.Outcome == FileSystemRepairReconciliationOutcome.Inconclusive && incompleteHost.Repairs == 0 &&
            File.Exists(Path.Combine(incompleteFolder, "filesystem-repair.pending.json")),
            "Unavailable exact volume retains pending reconciliation without repair");

        string foreignFolder = Path.Combine(scratch, "fs-reconcile-foreign-machine");
        await FileSystemRepairExecution.RunAsync(draft, true, foreignFolder, new Host { Command = new(3, "", "", false) });
        string foreignPath = Path.Combine(foreignFolder, "filesystem-repair.pending.json");
        string foreignJson = await File.ReadAllTextAsync(foreignPath);
        await File.WriteAllTextAsync(foreignPath, foreignJson.Replace(Environment.MachineName, "OTHER-MACHINE",
            StringComparison.OrdinalIgnoreCase));
        var foreignHost = new Host();
        var foreign = await FileSystemRepairReconciliation.RunAsync(foreignFolder, foreignHost);
        Require(foreign.Outcome == FileSystemRepairReconciliationOutcome.Inconclusive && foreignHost.Calls == 0 &&
            File.Exists(foreignPath), "A foreign-machine filesystem record remains guarded without inspecting or repairing a volume");

        var noneHost = new Host();
        var none = await FileSystemRepairReconciliation.RunAsync(Path.Combine(scratch, "fs-reconcile-none"), noneHost);
        Require(none.Outcome == FileSystemRepairReconciliationOutcome.NoPendingRecord && noneHost.Calls == 0,
            "Absent pending record does not inspect or repair a volume");

        Console.WriteLine("PASS: exact-volume filesystem repair preview, eligibility, durable preparation, final eligibility, bounded execution, clean verification and no-repeat reconciliation; simulated provider only.");
    }

    private static WorkflowCheck Broken(string volume = "D:") => new("Filesystem", CheckState.Attention,
        "Volume: " + volume + ". fixture", Evidence: new(1, "fixture", "", false));
    private static WorkflowCheck Clean(string volume = "D:") => new("Filesystem", CheckState.Clear,
        "Volume: " + volume + ". fixture", Evidence: new(0, "Windows has checked the file system and found no problems", "", false));
    private static WorkflowCheck RestartClear() => new("Restart", CheckState.Clear, "fixture",
        Evidence: new(0, "fixture", "", false));
    private static WorkflowCheck RestartPending() => new("Restart", CheckState.Attention, "fixture",
        Evidence: new(0, "fixture", "", false));
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Host : IFileSystemRepairHost
    {
        public bool Administrator = true;
        public bool? AdministratorAfterPreparation;
        public bool IsAdministrator => Inspections > 1 && AdministratorAfterPreparation.HasValue
            ? AdministratorAfterPreparation.Value : Administrator;
        public StorageVolumeState State = new(true, true, "NTFS");
        public StorageVolumeState? FinalState;
        public WorkflowCheck Before = Broken();
        public WorkflowCheck? FinalBefore;
        public WorkflowCheck After = Clean();
        public WorkflowCheck Restart = RestartClear();
        public Exception? FinalInspectionError;
        public BootStamp? Boot = new(1, DateTimeOffset.UtcNow.AddHours(-1));
        public ToolResult Command = new(0, "fixture", "", false);
        public Task<ToolResult>? RepairTask;
        public int Inspections, Diagnoses, Repairs, Schedules, RestartChecks, BootReads;
        public int Calls => Inspections + Diagnoses + Repairs + Schedules + RestartChecks + BootReads;
        public Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token)
        {
            Inspections++;
            if (Inspections == 2 && FinalInspectionError is not null)
                return Task.FromException<StorageVolumeState>(FinalInspectionError);
            return Task.FromResult(Inspections == 1 ? State : FinalState ?? State);
        }
        public Task<WorkflowCheck> DiagnoseAsync(string volume, CancellationToken token)
        {
            Diagnoses++;
            return Task.FromResult(Diagnoses == 1 ? Before : Diagnoses == 2 ? FinalBefore ?? Before : After);
        }
        public Task<WorkflowCheck> CheckRestartAsync(CancellationToken token) { RestartChecks++; return Task.FromResult(Restart); }
        public Task<BootStamp?> ReadBootAsync(CancellationToken token) { BootReads++; return Task.FromResult(Boot); }
        public Task<ToolResult> RepairAsync(RepairStep step, CancellationToken token) { Repairs++; return RepairTask ?? Task.FromResult(Command); }
        public Task<ToolResult> ScheduleAsync(RepairStep step, CancellationToken token) { Schedules++; return RepairTask ?? Task.FromResult(Command); }
    }
}
