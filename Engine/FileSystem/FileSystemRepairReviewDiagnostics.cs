namespace KillerBench.Engine;

public sealed record FileSystemRepairSelection(string Volume, FileSystemRepairMode Mode,
    bool AllowDismount, bool BackupConfirmed);

public sealed record FileSystemRepairReviewEvidence(FileSystemRepairDraft Draft,
    WorkflowCheck Diagnosis, FileSystemRepairReview Review);

public static class FileSystemRepairReviewDiagnostics
{
    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(FileSystemRepairSelection selection,
        Func<string, CancellationToken, Task<StorageVolumeState>> inspect,
        Func<string, CancellationToken, Task<WorkflowCheck>> diagnose,
        string systemVolume, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(inspect);
        ArgumentNullException.ThrowIfNull(diagnose);
        string volume = StorageDiagnostics.NormalizeVolume(selection.Volume);
        systemVolume = StorageDiagnostics.NormalizeVolume(systemVolume);
        if (!Enum.IsDefined(selection.Mode)) throw new ArgumentOutOfRangeException(nameof(selection));
        token.ThrowIfCancellationRequested();

        StorageVolumeState state;
        try
        {
            progress?.Report("Reading exact-volume eligibility for the selected filesystem repair review...");
            Task<StorageVolumeState> pending = inspect(volume, token) ??
                throw new InvalidOperationException("Volume inspection returned no task.");
            state = await pending.WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false) ??
                throw new InvalidDataException("Volume inspection returned no evidence.");
        }
        catch (Exception error) when (!token.IsCancellationRequested && IsUnavailable(error))
        {
            return [new("Filesystem repair review", CheckState.Unknown,
                "Exact-volume eligibility could not be established. No filesystem repair was requested or scheduled.",
                Evidence: new(-1, "", error.Message, false))];
        }

        progress?.Report("Running the selected volume's bounded read-only filesystem diagnosis...");
        WorkflowCheck diagnosis;
        try
        {
            diagnosis = await (diagnose(volume, token) ??
                throw new InvalidOperationException("Filesystem diagnosis returned no task.")).ConfigureAwait(false) ??
                throw new InvalidDataException("Filesystem diagnosis returned no evidence.");
        }
        catch (Exception error) when (!token.IsCancellationRequested && IsUnavailable(error))
        {
            diagnosis = new("Filesystem", CheckState.Unknown,
                "The selected volume could not produce a complete read-only filesystem diagnosis. No repair was requested or scheduled.",
                Evidence: new(-1, "", error.Message, false));
        }

        FileSystemRepairDraft draft = FileSystemRepairPlanning.Create(volume, selection.Mode, state,
            volume.Equals(systemVolume, StringComparison.OrdinalIgnoreCase), selection.AllowDismount,
            selection.BackupConfirmed);
        FileSystemRepairReview review = FileSystemRepairPlanning.Review(draft, diagnosis);
        return [diagnosis, ToWorkflowCheck(new(draft, diagnosis, review))];
    }

    public static WorkflowCheck ToWorkflowCheck(FileSystemRepairReviewEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        FileSystemRepairDraft draft = evidence.Draft;
        WorkflowCheck diagnosis = evidence.Diagnosis;
        FileSystemRepairReview review = evidence.Review;
        string boundary = draft.RequiresRestart ? "A restart would be required." :
            draft.RequiresDismount ? "The volume would need to be dismounted." : "The execution boundary is unavailable.";
        string backup = draft.BackupConfirmed ? "Backup confirmation is recorded." : "Backup confirmation is not recorded.";
        string conclusion = diagnosis.EffectiveState == CheckState.Clear
            ? "Current read-only evidence did not report repairable filesystem corruption."
            : review.ExecutionEligible
                ? "Fresh evidence matched this exact plan. A separate guarded execution request would repeat every prerequisite before dispatch."
                : review.NextAction;
        CheckState state = diagnosis.EffectiveState == CheckState.Unknown ? CheckState.Unknown :
            diagnosis.EffectiveState == CheckState.Clear ? CheckState.Information : CheckState.Attention;
        return new("Filesystem repair review", state,
            $"Selected {draft.Mode} for {draft.Volume}. {boundary} {backup} {draft.Effects} {conclusion} " +
            "This review does not authorize, request or schedule filesystem repair.");
    }

    private static bool IsUnavailable(Exception error) => error is IOException or InvalidDataException or
        UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException or
        System.ComponentModel.Win32Exception or System.Security.SecurityException or OperationCanceledException;
}
