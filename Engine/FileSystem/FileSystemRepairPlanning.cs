namespace KillerBench.Engine.FileSystem;

public enum FileSystemRepairMode { SpotFix, OfflineScanAndFix }

public sealed record FileSystemRepairDraft(string Volume, FileSystemRepairMode Mode, bool VolumeEligible,
    bool RequiresRestart, bool RequiresDismount, bool BackupConfirmed, string Effects, string? Blocker, RepairStep? Step);

public sealed record FileSystemRepairReview(FileSystemRepairDraft Draft, bool EvidenceMatched,
    bool ExecutionEligible, string NextAction);

public sealed record FileSystemRepairReviewRecord(string Machine, DateTimeOffset CreatedUtc,
    FileSystemRepairDraft Draft, WorkflowCheck Diagnosis, FileSystemRepairReview Review, string JournalPath);

public static class FileSystemRepairPlanning
{
    public static async Task<FileSystemRepairReviewRecord> SaveReviewAsync(FileSystemRepairDraft draft,
        WorkflowCheck diagnosis, string folder, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        token.ThrowIfCancellationRequested();
        var review = Review(draft, diagnosis);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(Path.GetFullPath(folder), "filesystem-review-" + Guid.NewGuid().ToString("N") + ".json");
        var record = new FileSystemRepairReviewRecord(Environment.MachineName, DateTimeOffset.UtcNow,
            draft, diagnosis, review, path);
        await JournalPersistence.WriteAsync(path, System.Text.Json.JsonSerializer.Serialize(record, Capture.JsonOptions), token).ConfigureAwait(false);
        return record;
    }

    public static FileSystemRepairReview Review(FileSystemRepairDraft draft, WorkflowCheck diagnosis)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(diagnosis);
        string prefix = "Volume: " + draft.Volume + ".";
        bool completeEvidence = diagnosis.Evidence is { Truncated: false, TimedOut: false } evidence &&
            string.IsNullOrWhiteSpace(evidence.Error) && evidence.ExitCode is 1 or 2 or 3;
        bool matched = draft.VolumeEligible && draft.Step is not null &&
            diagnosis.Name.Equals("Filesystem", StringComparison.Ordinal) && diagnosis.State == CheckState.Attention &&
            diagnosis.Detail.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && completeEvidence;
        bool executionEligible = matched && draft.BackupConfirmed && (draft.RequiresRestart ^ draft.RequiresDismount);
        string next = !draft.VolumeEligible ? draft.Blocker ?? "The selected volume is not compatible with this repair." :
            !matched ? "Collect a current complete filesystem diagnosis for this exact volume before reviewing repair." :
            !draft.BackupConfirmed ? "Confirm that important data on this volume is backed up before enabling filesystem repair." :
            draft.RequiresRestart ? "Back up important data before explicitly enabling filesystem repair. Windows must restart to run the scheduled repair." :
            "Back up important data before explicitly enabling filesystem repair. The selected volume will be dismounted.";
        return new(draft, matched, executionEligible, next);
    }

    public static FileSystemRepairDraft Create(string volume, FileSystemRepairMode mode, StorageVolumeState state,
        bool systemVolume, bool allowDismount, bool backupConfirmed = false)
    {
        volume = StorageDiagnostics.NormalizeVolume(volume);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        ArgumentNullException.ThrowIfNull(state);
        bool requiresRestart = systemVolume;
        bool requiresDismount = !systemVolume;
        string effects = mode == FileSystemRepairMode.SpotFix
            ? "CHKDSK would repair previously identified NTFS corruption. The volume must be taken offline briefly, or the system volume must be scheduled for repair at restart. Open files can be interrupted. No user files are intentionally removed, but filesystem repair has no automatic rollback."
            : "CHKDSK would take the volume offline and scan its full NTFS metadata before repairing identified corruption. The system volume requires a restart. Open files can be interrupted. No user files are intentionally removed, but filesystem repair has no automatic rollback.";
        string? blocker = !state.Ready ? "The selected volume is not ready." :
            !state.Fixed ? "Only fixed local volumes are supported." :
            !string.Equals(state.FileSystem, "NTFS", StringComparison.OrdinalIgnoreCase) ? "Only NTFS volumes are supported." :
            requiresDismount && !allowDismount ? "Explicit dismount consent is required for a non-system volume." : null;
        string[] arguments = mode == FileSystemRepairMode.SpotFix ? [volume, "/spotfix"] : [volume, "/offlinescanandfix"];
        return new(volume, mode, blocker is null, requiresRestart, requiresDismount, backupConfirmed, effects, blocker,
            blocker is null ? new RepairStep("chkdsk.exe", arguments, false) : null);
    }
}
