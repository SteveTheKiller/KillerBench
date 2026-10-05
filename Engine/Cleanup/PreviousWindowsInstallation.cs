using System.Security.Principal;
using System.Text.Json;

namespace KillerBench.Engine.Cleanup;

public enum PreviousWindowsRemovalOutcome { Preview, Blocked, Verified, Uncertain, Stopped }
public enum PreviousWindowsReconciliationOutcome { NoPendingRecord, Verified, NotChanged, Inconclusive }

public sealed record PreviousWindowsInstallationSnapshot(bool Complete, bool DirectoryExists,
    bool DirectoryReparsePoint, bool SameSystemVolume, bool? RollbackAvailable, string Detail);

public sealed record PreviousWindowsRemovalPendingRecord(int Schema, string OperationId,
    DateTimeOffset StartedUtc, string Machine, string WindowsDirectory, string PreviousDirectory,
    bool BackupConfirmed);

public sealed record PreviousWindowsRemovalResult(PreviousWindowsRemovalOutcome Outcome, string Detail,
    string? JournalPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        PreviousWindowsRemovalOutcome.Preview or PreviousWindowsRemovalOutcome.Verified => 0,
        PreviousWindowsRemovalOutcome.Stopped => 130,
        _ => 3
    };
}

public sealed record PreviousWindowsReconciliationResult(PreviousWindowsReconciliationOutcome Outcome,
    string Detail, string PendingPath, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is PreviousWindowsReconciliationOutcome.NoPendingRecord or
        PreviousWindowsReconciliationOutcome.Verified or PreviousWindowsReconciliationOutcome.NotChanged ? 0 : 3;
}

public interface IPreviousWindowsInstallationHost
{
    bool IsAdministrator { get; }
    Task<PreviousWindowsInstallationSnapshot> ReadAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<WorkflowCheck> ReadServicingActivityAsync(CancellationToken token);
    Task<ToolResult> RemoveRollbackAsync(CancellationToken token);
}

public static class PreviousWindowsInstallationRemoval
{
    public const string PendingFileName = "previous-windows-removal.pending.json";

    public static WorkflowCheck Assess(PreviousWindowsInstallationSnapshot? snapshot, bool isAdministrator,
        bool pendingRecovery)
    {
        if (pendingRecovery)
            return new("Previous Windows installation", CheckState.Attention,
                "A prior rollback-removal request requires read-only reconciliation before another attempt.");
        if (snapshot is null || !snapshot.Complete || !snapshot.RollbackAvailable.HasValue)
            return new("Previous Windows installation", CheckState.Unknown,
                "Complete previous-installation and rollback evidence is unavailable. No removal is eligible.");
        if (!snapshot.DirectoryExists && snapshot.RollbackAvailable == false)
            return new("Previous Windows installation", CheckState.Clear,
                "Windows reports no previous-installation directory and no available operating-system rollback.");
        if (snapshot.DirectoryReparsePoint || !snapshot.SameSystemVolume)
            return new("Previous Windows installation", CheckState.Attention,
                "The previous-installation path is redirected or is not on the Windows volume. KillerBench will not remove it.");
        if (snapshot.RollbackAvailable == false)
            return new("Previous Windows installation", CheckState.Information,
                "Windows reports that operating-system rollback is unavailable. No rollback-removal request is needed. Windows may retain files for its own cleanup.");
        if (!snapshot.DirectoryExists)
            return new("Previous Windows installation", CheckState.Unknown,
                "Windows reports rollback availability without the expected previous-installation directory. No removal is eligible.");
        if (!isAdministrator)
            return new("Previous Windows installation", CheckState.Attention,
                "Administrator access is required to remove operating-system rollback capability.");
        return new("Previous Windows installation", CheckState.Repairable,
            "Windows reports an available operating-system rollback and an exact local Windows.old directory. Removal is permanent and requires explicit backup confirmation.",
            RepairKind.PreviousWindowsInstallationRemoval);
    }

    public static async Task<WorkflowCheck> ReviewAsync(string journalFolder,
        IPreviousWindowsInstallationHost? host = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        host ??= new WindowsPreviousWindowsInstallationHost();
        string pending = Path.Combine(Path.GetFullPath(journalFolder), PendingFileName);
        if (File.Exists(pending)) return Assess(null, host.IsAdministrator, true);
        try { return Assess(await host.ReadAsync(token).ConfigureAwait(false), host.IsAdministrator, false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (IsExpected(error))
        { return Assess(null, host.IsAdministrator, false); }
    }

    public static async Task<PreviousWindowsRemovalResult> RunAsync(bool execute, bool backupConfirmed,
        string journalFolder, IPreviousWindowsInstallationHost? host = null,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        string directory = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(directory, PendingFileName);
        host ??= new WindowsPreviousWindowsInstallationHost();
        if (File.Exists(pendingPath))
            return new(PreviousWindowsRemovalOutcome.Blocked,
                "A prior rollback-removal request requires read-only reconciliation. No command was repeated.", pendingPath);

        progress?.Report("Checking previous Windows installation and rollback availability");
        PreviousWindowsInstallationSnapshot first;
        WorkflowCheck restart;
        WorkflowCheck servicing;
        try
        {
            first = await host.ReadAsync(token).ConfigureAwait(false);
            restart = await host.ReadRestartAsync(token).ConfigureAwait(false);
            servicing = await host.ReadServicingActivityAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        { return new(PreviousWindowsRemovalOutcome.Stopped, "The check was stopped before any change was requested."); }
        catch (Exception error) when (IsExpected(error))
        { return new(PreviousWindowsRemovalOutcome.Blocked, "Previous-installation evidence could not be collected. No change was requested."); }

        WorkflowCheck eligibility = Assess(first, host.IsAdministrator, false);
        if (eligibility.State != CheckState.Repairable)
            return new(PreviousWindowsRemovalOutcome.Blocked, eligibility.Detail + " No change was requested.");
        if (!Usable(restart, CheckState.Clear) || !Usable(servicing, CheckState.Information))
            return new(PreviousWindowsRemovalOutcome.Blocked,
                "Restart or active-servicing evidence is not clear. No rollback-removal request is eligible.");
        if (!execute)
            return new(PreviousWindowsRemovalOutcome.Preview,
                "Preview only. Windows rollback remains available and Windows.old was not changed.");
        if (!backupConfirmed)
            return new(PreviousWindowsRemovalOutcome.Blocked,
                "Confirm that needed files and settings are backed up before permanently removing Windows rollback capability.");

        progress?.Report("Repeating rollback-removal eligibility");
        PreviousWindowsInstallationSnapshot second;
        try { second = await host.ReadAsync(token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        { return new(PreviousWindowsRemovalOutcome.Stopped, "The final preview was stopped before any change was requested."); }
        catch (Exception error) when (IsExpected(error))
        { return new(PreviousWindowsRemovalOutcome.Blocked, "Final rollback evidence was unavailable. No change was requested."); }
        if (!StableEligible(first, second))
            return new(PreviousWindowsRemovalOutcome.Blocked,
                "Previous-installation or rollback evidence changed during review. No change was requested.");

        Directory.CreateDirectory(directory);
        string operationId = Guid.NewGuid().ToString("N");
        var pending = new PreviousWindowsRemovalPendingRecord(1, operationId, DateTimeOffset.UtcNow,
            Environment.MachineName, Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))!, "Windows.old"), true);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        bool dispatched = false;
        try
        {
            progress?.Report("Performing the final rollback-removal check");
            var final = await host.ReadAsync(token).ConfigureAwait(false);
            var finalRestart = await host.ReadRestartAsync(token).ConfigureAwait(false);
            var finalServicing = await host.ReadServicingActivityAsync(token).ConfigureAwait(false);
            if (!StableEligible(second, final) || !Usable(finalRestart, CheckState.Clear) ||
                !Usable(finalServicing, CheckState.Information))
                return await ClosePreparedAsync(directory, pendingPath, pending, PreviousWindowsRemovalOutcome.Blocked,
                    "Final evidence no longer authorizes rollback removal. No command was requested.").ConfigureAwait(false);

            progress?.Report("Requesting Windows rollback removal");
            dispatched = true;
            ToolResult command = await host.RemoveRollbackAsync(token).ConfigureAwait(false);
            if (!Complete(command))
                return new(PreviousWindowsRemovalOutcome.Uncertain,
                    "Windows did not report a complete rollback-removal command. The pending record remains for read-only reconciliation.", pendingPath);
            progress?.Report("Verifying rollback removal");
            var after = await host.ReadAsync(token).ConfigureAwait(false);
            if (!Verified(after))
                return new(PreviousWindowsRemovalOutcome.Uncertain,
                    "Fresh evidence does not yet prove rollback removal. The pending record remains and the command will not be repeated automatically.", pendingPath);
            string journal = Path.Combine(directory, $"previous-windows-removal.{operationId}.verified.json");
            await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
            File.Delete(pendingPath);
            return new(PreviousWindowsRemovalOutcome.Verified,
                "Windows reports that operating-system rollback is no longer available. Windows controls any remaining cleanup of Windows.old.", journal);
        }
        catch (OperationCanceledException)
        {
            if (!dispatched)
                return await ClosePreparedAsync(directory, pendingPath, pending, PreviousWindowsRemovalOutcome.Stopped,
                    "The final check was stopped before any rollback-removal command was requested.").ConfigureAwait(false);
            return new(PreviousWindowsRemovalOutcome.Stopped,
                "The request was interrupted after dispatch. The pending record remains for read-only reconciliation.", pendingPath);
        }
        catch (Exception error) when (IsExpected(error))
        {
            if (!dispatched)
                return await ClosePreparedAsync(directory, pendingPath, pending, PreviousWindowsRemovalOutcome.Blocked,
                    "Final evidence was unavailable before any rollback-removal command was requested.").ConfigureAwait(false);
            return new(PreviousWindowsRemovalOutcome.Uncertain,
                "Rollback removal ended without complete verification. The pending record remains for read-only reconciliation.", pendingPath);
        }
    }

    public static async Task<PreviousWindowsReconciliationResult> ReconcileAsync(string journalFolder,
        IPreviousWindowsInstallationHost? host = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        string directory = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(directory, PendingFileName);
        if (!File.Exists(pendingPath))
            return new(PreviousWindowsReconciliationOutcome.NoPendingRecord,
                "No pending previous-Windows removal exists.", pendingPath);
        PreviousWindowsRemovalPendingRecord? pending;
        try
        {
            string json = await File.ReadAllTextAsync(pendingPath, token).ConfigureAwait(false);
            if (json.Length > 64 * 1024) throw new InvalidDataException("Pending record is too large.");
            pending = JsonSerializer.Deserialize<PreviousWindowsRemovalPendingRecord>(json, Capture.JsonOptions);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { return new(PreviousWindowsReconciliationOutcome.Inconclusive, "The pending record could not be read safely.", pendingPath); }
        if (pending is null || pending.Schema != 1 || pending.Machine != Environment.MachineName ||
            string.IsNullOrWhiteSpace(pending.OperationId) || !pending.BackupConfirmed)
            return new(PreviousWindowsReconciliationOutcome.Inconclusive,
                "The pending record is invalid or belongs to another machine.", pendingPath);
        host ??= new WindowsPreviousWindowsInstallationHost();
        try
        {
            var current = await host.ReadAsync(token).ConfigureAwait(false);
            PreviousWindowsReconciliationOutcome outcome;
            string detail;
            if (Verified(current))
            {
                outcome = PreviousWindowsReconciliationOutcome.Verified;
                detail = "Fresh evidence confirms rollback is unavailable. No removal command was repeated.";
            }
            else if (current.Complete && current.DirectoryExists && !current.DirectoryReparsePoint &&
                current.SameSystemVolume && current.RollbackAvailable == true)
            {
                outcome = PreviousWindowsReconciliationOutcome.NotChanged;
                detail = "Fresh evidence confirms rollback remains available. No removal command was repeated.";
            }
            else return new(PreviousWindowsReconciliationOutcome.Inconclusive,
                "Fresh evidence cannot prove a completed or unchanged rollback state. No removal command was repeated.", pendingPath);
            string archive = Path.Combine(directory, $"previous-windows-removal.{pending.OperationId}.reconciled.json");
            await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
            File.Delete(pendingPath);
            return new(outcome, detail, pendingPath, archive);
        }
        catch (Exception error) when (IsExpected(error) || error is OperationCanceledException)
        { return new(PreviousWindowsReconciliationOutcome.Inconclusive,
            "Reconciliation was incomplete. The pending record remains and no removal command was repeated.", pendingPath); }
    }

    private static bool StableEligible(PreviousWindowsInstallationSnapshot first,
        PreviousWindowsInstallationSnapshot second) =>
        first.Complete && second.Complete && first.DirectoryExists && second.DirectoryExists &&
        !first.DirectoryReparsePoint && !second.DirectoryReparsePoint && first.SameSystemVolume &&
        second.SameSystemVolume && first.RollbackAvailable == true && second.RollbackAvailable == true;

    private static bool Verified(PreviousWindowsInstallationSnapshot value) =>
        value.Complete && value.RollbackAvailable == false;

    private static bool Usable(WorkflowCheck check, CheckState expected) => check.EffectiveState == expected &&
        check.Evidence is not { TimedOut: true } && check.Evidence is not { Truncated: true } &&
        string.IsNullOrWhiteSpace(check.Evidence?.Error);

    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut &&
        !result.Truncated && string.IsNullOrWhiteSpace(result.Error);

    private static bool IsExpected(Exception error) => error is IOException or InvalidDataException or
        UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException or
        System.ComponentModel.Win32Exception or System.Security.SecurityException;

    private static async Task<PreviousWindowsRemovalResult> ClosePreparedAsync(string directory,
        string pendingPath, PreviousWindowsRemovalPendingRecord pending, PreviousWindowsRemovalOutcome outcome,
        string detail)
    {
        string archive = Path.Combine(directory, $"previous-windows-removal.{pending.OperationId}.{outcome.ToString().ToLowerInvariant()}.json");
        await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        File.Move(pendingPath, archive + ".prepared.json");
        return new(outcome, detail, archive);
    }
}

public sealed class WindowsPreviousWindowsInstallationHost : IPreviousWindowsInstallationHost
{
    public bool IsAdministrator => OperatingSystem.IsWindows() &&
        new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public async Task<PreviousWindowsInstallationSnapshot> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string root = Path.GetPathRoot(windows) ?? "";
        string previous = Path.Combine(root, "Windows.old");
        try
        {
            bool exists = Directory.Exists(previous);
            bool reparse = exists && (File.GetAttributes(previous) & FileAttributes.ReparsePoint) != 0;
            bool sameVolume = string.Equals(Path.GetPathRoot(previous), root, StringComparison.OrdinalIgnoreCase);
            var result = await BoundedTool.RunAsync(new("dism.exe",
                ["/Online", "/Get-OSUninstallWindow", "/English"], true), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
            bool? rollback = result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error)
                ? true : Contains1168(result) ? false : null;
            bool complete = rollback.HasValue && !result.TimedOut && !result.Truncated;
            return new(complete, exists, reparse, sameVolume, rollback,
                rollback == true ? "Windows reports rollback availability." : rollback == false
                    ? "Windows reports no available rollback." : "Rollback availability could not be determined.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            System.Security.SecurityException)
        { return new(false, false, false, false, null, "Previous-installation filesystem evidence is unavailable."); }
    }

    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
    { token.ThrowIfCancellationRequested(); return Task.FromResult(RestartDiagnostics.Check()); }

    public Task<WorkflowCheck> ReadServicingActivityAsync(CancellationToken token) =>
        InstallerActivityDiagnostics.CheckAsync(token);

    public Task<ToolResult> RemoveRollbackAsync(CancellationToken token) =>
        BoundedTool.RunAsync(new("dism.exe", ["/Online", "/Remove-OSUninstall", "/NoRestart", "/English"], true),
            TimeSpan.FromMinutes(30), token: token);

    private static bool Contains1168(ToolResult result) =>
        (result.Output + "\n" + result.Error).Contains("Error: 1168", StringComparison.OrdinalIgnoreCase);
}
