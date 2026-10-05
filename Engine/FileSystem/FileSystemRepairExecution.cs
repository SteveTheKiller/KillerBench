using System.Text.Json;

namespace KillerBench.Engine.FileSystem;

public enum FileSystemRepairOutcome { Preview, Blocked, Verified, RestartRequired, Failed, Uncertain, Stopped }

public sealed record FileSystemRepairResult(FileSystemRepairDraft Draft, FileSystemRepairOutcome Outcome,
    string Detail, string? JournalPath = null, WorkflowCheck? Before = null, ToolResult? Command = null,
    WorkflowCheck? After = null)
{
    public int GetExitCode() => Outcome switch
    {
        FileSystemRepairOutcome.Preview or FileSystemRepairOutcome.Verified => 0,
        FileSystemRepairOutcome.RestartRequired => 4,
        FileSystemRepairOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IFileSystemRepairHost
{
    bool IsAdministrator { get; }
    Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token);
    Task<WorkflowCheck> DiagnoseAsync(string volume, CancellationToken token);
    Task<WorkflowCheck> CheckRestartAsync(CancellationToken token);
    Task<BootStamp?> ReadBootAsync(CancellationToken token);
    Task<ToolResult> RepairAsync(RepairStep step, CancellationToken token);
    Task<ToolResult> ScheduleAsync(RepairStep step, CancellationToken token);
}

public static class FileSystemRepairExecution
{
    public static async Task<FileSystemRepairResult> RunAsync(FileSystemRepairDraft draft, bool execute,
        string journalFolder, IFileSystemRepairHost host, IProgress<string>? progress = null,
        TimeSpan? executionTimeout = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = executionTimeout ?? TimeSpan.FromMinutes(65);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromHours(2)) throw new ArgumentOutOfRangeException(nameof(executionTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute) return new(draft, FileSystemRepairOutcome.Preview,
            draft.Effects + " Execution will repeat volume inspection and read-only diagnosis before any repair.");
        if (!draft.VolumeEligible || draft.Step is null)
            return new(draft, FileSystemRepairOutcome.Blocked, draft.Blocker ?? "The selected volume is not eligible.");
        if (!draft.BackupConfirmed)
            return new(draft, FileSystemRepairOutcome.Blocked,
                "Confirm that important data on the selected volume is backed up before filesystem repair. No repair was started or scheduled.");
        if (draft.RequiresRestart == draft.RequiresDismount)
            return new(draft, FileSystemRepairOutcome.Blocked, "The selected repair plan has an invalid restart or dismount boundary.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "filesystem-repair.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "filesystem-repair.pending.json");
        if (File.Exists(pendingPath)) return new(draft, FileSystemRepairOutcome.Blocked,
            "A prior filesystem repair needs reconciliation before another repair can start.", pendingPath);

        Report(progress, "Rechecking selected volume identity and readiness.");
        var state = await Required(ct => host.InspectAsync(draft.Volume, ct), "Volume inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        var fresh = FileSystemRepairPlanning.Create(draft.Volume, draft.Mode, state, draft.RequiresRestart,
            draft.RequiresDismount, draft.BackupConfirmed);
        if (!fresh.VolumeEligible || fresh.Step is null || !fresh.Step.Arguments.SequenceEqual(draft.Step.Arguments))
            return new(draft, FileSystemRepairOutcome.Blocked, fresh.Blocker ?? "The selected repair plan changed during reinspection.");
        Report(progress, "Repeating read-only filesystem diagnosis.");
        var before = await Required(ct => host.DiagnoseAsync(draft.Volume, ct), "Filesystem diagnosis", TimeSpan.FromMinutes(11), token: token).ConfigureAwait(false);
        if (!FileSystemRepairPlanning.Review(fresh, before).EvidenceMatched)
            return new(draft, FileSystemRepairOutcome.Blocked,
                "Fresh complete evidence did not confirm repair eligibility for this exact volume.", Before: before);
        if (!host.IsAdministrator)
            return new(draft, FileSystemRepairOutcome.Blocked, "Administrator access is required. No repair was started or scheduled.", Before: before);
        if (fresh.RequiresRestart)
        {
            Report(progress, "Checking for an existing Windows restart requirement.");
            var restart = await Required(host.CheckRestartAsync, "Restart inspection", TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
            if (restart.State != CheckState.Clear || restart.Evidence is not { ExitCode: 0, TimedOut: false, Truncated: false } restartEvidence ||
                !string.IsNullOrWhiteSpace(restartEvidence.Error))
                return new(draft, FileSystemRepairOutcome.Blocked,
                    "Windows already requires a restart, or restart state is unavailable. Restart and collect fresh evidence before scheduling filesystem repair.", Before: before, After: restart);
        }

        string journalPath = Path.Combine(root, "filesystem-repair-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(new
        {
            Schema = 1, Machine = Environment.MachineName, StartedUtc = DateTimeOffset.UtcNow,
            Draft = fresh, Before = before, JournalPath = journalPath
        }, Capture.JsonOptions), token: token).ConfigureAwait(false);
        ToolResult? command = null;
        bool repairDispatched = false;
        FileSystemRepairResult result;
        try
        {
            Report(progress, "Confirming final filesystem-repair eligibility.");
            var currentState = await Required(ct => host.InspectAsync(fresh.Volume, ct), "Final volume inspection", TimeSpan.FromSeconds(20),
                token: token).ConfigureAwait(false);
            var currentDraft = FileSystemRepairPlanning.Create(fresh.Volume, fresh.Mode, currentState,
                fresh.RequiresRestart, fresh.RequiresDismount, fresh.BackupConfirmed);
            var current = await Required(ct => host.DiagnoseAsync(fresh.Volume, ct), "Final filesystem diagnosis", TimeSpan.FromMinutes(11),
                token: token).ConfigureAwait(false);
            WorkflowCheck? currentRestart = null;
            if (fresh.RequiresRestart)
                currentRestart = await Required(host.CheckRestartAsync, "Final restart inspection", TimeSpan.FromSeconds(20),
                    token: token).ConfigureAwait(false);
            bool finalRestartClear = !fresh.RequiresRestart ||
                currentRestart is { State: CheckState.Clear, Evidence: { ExitCode: 0, TimedOut: false, Truncated: false } restartEvidence } &&
                string.IsNullOrWhiteSpace(restartEvidence.Error);
            bool finalEligible = currentDraft.VolumeEligible && currentDraft.Step is not null &&
                currentDraft.Step.Arguments.SequenceEqual(fresh.Step.Arguments) &&
                FileSystemRepairPlanning.Review(currentDraft, current).EvidenceMatched &&
                host.IsAdministrator && finalRestartClear;
            if (!finalEligible)
            {
                string detail = !currentDraft.VolumeEligible || currentDraft.Step is null ||
                    !currentDraft.Step.Arguments.SequenceEqual(fresh.Step.Arguments)
                    ? "The selected volume or repair plan changed after preparation. No repair was started or scheduled."
                    : !FileSystemRepairPlanning.Review(currentDraft, current).EvidenceMatched
                        ? "Fresh filesystem evidence no longer confirms repair eligibility. No repair was started or scheduled."
                        : !host.IsAdministrator
                            ? "Administrator access changed after preparation. No repair was started or scheduled."
                            : "Restart state changed after preparation. No filesystem repair was scheduled.";
                result = new(fresh, FileSystemRepairOutcome.Blocked, detail, journalPath, before, After: currentRestart ?? current);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, fresh.RequiresRestart
                ? "Scheduling filesystem repair for the next Windows startup. KillerBench will not restart the computer."
                : "Repairing " + fresh.Volume + ". The volume may be dismounted.");
            repairDispatched = true;
            command = await Required(ct => fresh.RequiresRestart ? host.ScheduleAsync(fresh.Step, ct) : host.RepairAsync(fresh.Step, ct),
                fresh.RequiresRestart ? "Filesystem repair scheduling" : "Filesystem repair", limit, token: token).ConfigureAwait(false);
            if (command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                result = new(fresh, FileSystemRepairOutcome.Uncertain,
                    "Repair evidence is incomplete. The volume may have changed; reconcile it before retrying.", journalPath, before, command);
            else if (fresh.RequiresRestart ? command.ExitCode != 0 : command.ExitCode is not (0 or 1))
                result = new(fresh, FileSystemRepairOutcome.Failed,
                    "CHKDSK did not report a supported completion result. Reconcile the volume before retrying.", journalPath, before, command);
            else if (fresh.RequiresRestart)
                result = new(fresh, FileSystemRepairOutcome.RestartRequired,
                    "Windows accepted the system-volume repair command. Restart Windows to run it, then reopen KillerBench to verify the exact volume. KillerBench did not restart the computer.",
                    journalPath, before, command);
            else
            {
                Report(progress, "Verifying the repaired volume with a read-only check.");
                var after = await Required(ct => host.DiagnoseAsync(fresh.Volume, ct), "Filesystem verification", TimeSpan.FromMinutes(11), token: token).ConfigureAwait(false);
                bool verified = after.State == CheckState.Clear && after.Evidence is { TimedOut: false, Truncated: false } evidence &&
                    evidence.ExitCode == 0 && string.IsNullOrWhiteSpace(evidence.Error);
                result = new(fresh, verified ? FileSystemRepairOutcome.Verified : FileSystemRepairOutcome.Uncertain,
                    verified ? "A fresh read-only check reported no filesystem problems. Physical media health remains untested."
                        : "CHKDSK completed, but a fresh clean filesystem result was not verified.", journalPath, before, command, after);
            }
        }
        catch (OperationCanceledException)
        {
            result = new(fresh, FileSystemRepairOutcome.Stopped,
                repairDispatched
                    ? "Filesystem repair was stopped after command dispatch. The volume may have changed; reconcile before retrying."
                    : "Final filesystem inspection was stopped before command dispatch. No repair was started or scheduled.",
                journalPath, before, command);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
            System.Security.SecurityException)
        {
            result = new(fresh, repairDispatched ? FileSystemRepairOutcome.Uncertain : FileSystemRepairOutcome.Blocked,
                repairDispatched
                    ? "Filesystem repair was interrupted after command dispatch: " + error.Message + ". Reconcile before retrying."
                    : "Final filesystem inspection was unavailable before command dispatch: " + error.Message + ". No repair was started or scheduled.",
                journalPath, before, command);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == FileSystemRepairOutcome.Verified || !repairDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        TimeSpan limit, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(limit, token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsFileSystemRepairHost : IFileSystemRepairHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var drive = new DriveInfo(StorageDiagnostics.NormalizeVolume(volume) + "\\");
        bool ready = drive.IsReady;
        return Task.FromResult(new StorageVolumeState(ready, drive.DriveType == DriveType.Fixed, ready ? drive.DriveFormat : null));
    }
    public Task<WorkflowCheck> DiagnoseAsync(string volume, CancellationToken token) => StorageDiagnostics.CheckVolumeAsync(volume, token: token);
    public Task<WorkflowCheck> CheckRestartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(RestartDiagnostics.Check());
    }
    public Task<BootStamp?> ReadBootAsync(CancellationToken token) => RepairRecoveryStore.ReadCurrentBootAsync(token);
    public Task<ToolResult> RepairAsync(RepairStep step, CancellationToken token) =>
        BoundedTool.RunAsync(step, TimeSpan.FromMinutes(65), token: token);
    public Task<ToolResult> ScheduleAsync(RepairStep step, CancellationToken token) =>
        BoundedTool.RunWithInputAsync(step, TimeSpan.FromMinutes(5), "Y" + Environment.NewLine, token: token);
}
