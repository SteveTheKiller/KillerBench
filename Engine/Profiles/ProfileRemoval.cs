using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public interface IProfileRemovalHost
{
    Task<ProfileInventoryResult> InventoryAsync(CancellationToken token);
    Task<ProfileDependencyResult> DependenciesAsync(CancellationToken token);
    Task<ProfileRegistrationResult> RegistrationsAsync(CancellationToken token);
    Task<bool> ReadyAsync(CancellationToken token);
    Task<ProfileBackupReceipt> VerifyBackupAsync(ProfileSnapshot target, CancellationToken token);
    Task RemoveAsync(ProfileSnapshot target, CancellationToken token);
    Task<bool> VerifyAbsentAsync(ProfileSnapshot target, CancellationToken token);
}

public sealed record ProfileRemovalResult(string Status, string JournalPath);

public static class ProfileRemoval
{
    public static async Task<ProfileRemovalResult> RunAsync(ProfileSnapshot target, int minimumDays, string folder,
        IProfileRemovalHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(host);
        ProfilePlanning.ValidateSelection([target.Sid]);
        if (minimumDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(minimumDays));
        token.ThrowIfCancellationRequested();
        CancellationToken callerToken = token;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        token = deadline.Token;
        Directory.CreateDirectory(folder);
        using var gate = new FileStream(Path.Combine(folder, "removal.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "pending-removal.json");
        if (File.Exists(pending)) throw new InvalidOperationException("An earlier profile removal is unresolved. No retry started.");
        string journal = Path.Combine(folder, "profile-removal-" + Guid.NewGuid().ToString("N") + ".jsonl");
        using var stream = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        void Record(string stage, ProfileBackupReceipt? backup = null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Stage = stage, TimeUtc = DateTimeOffset.UtcNow, Target = target, Backup = backup }) + "\n");
            stream.Write(bytes);
            stream.Flush(true);
        }
        async Task<bool> CheckAsync()
        {
            if (!await (host.ReadyAsync(token) ?? throw new InvalidOperationException("Readiness provider returned no operation.")).WaitAsync(token).ConfigureAwait(false)) return false;
            var plan = await ProfileReview.RefreshAsync([target.Sid], minimumDays, host.InventoryAsync, host.DependenciesAsync, host.RegistrationsAsync, token: token).ConfigureAwait(false);
            return plan.ReviewPassed && plan.Targets.Single() == target;
        }
        bool prepared = false;
        bool preparationCompleted = false;
        bool removalDispatched = false;
        void ClosePreparedRecord()
        {
            if (!prepared || !File.Exists(pending)) return;
            using var record = RecoveryRecordFiles.OpenLocked(pending);
            RecoveryRecordFiles.Archive(record, journal + ".prepared.json");
            prepared = false;
        }
        Record("Started");
        try
        {
            if (!await CheckAsync().ConfigureAwait(false))
            { Record("Blocked"); return new("Blocked", journal); }
            var backup = await (host.VerifyBackupAsync(target, token) ?? throw new InvalidOperationException("Backup provider returned no operation.")).WaitAsync(token).ConfigureAwait(false);
            if (!ProfileBackupEvidence.Matches(target, backup, DateTimeOffset.UtcNow) || !await CheckAsync().ConfigureAwait(false))
            { Record("Blocked", backup); return new("Blocked", journal); }
            Record("Backup verified", backup);
            token.ThrowIfCancellationRequested();
            using (var marker = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(marker, new
                {
                    Schema = 1,
                    Machine = Environment.MachineName,
                    StartedUtc = DateTimeOffset.UtcNow,
                    Target = target,
                    Backup = backup,
                    Journal = journal
                }, Capture.JsonOptions);
                marker.Flush(true);
            }
            prepared = true;
            preparationCompleted = true;
            Record("Prepared");
            using var pendingRecord = RecoveryRecordFiles.OpenLocked(pending);
            var finalBackup = await (host.VerifyBackupAsync(target, token) ?? throw new InvalidOperationException("Backup provider returned no operation.")).WaitAsync(token).ConfigureAwait(false);
            bool finalEligibility = ProfileBackupEvidence.Matches(target, finalBackup, DateTimeOffset.UtcNow) &&
                finalBackup == backup && await CheckAsync().ConfigureAwait(false);
            if (!finalEligibility)
            {
                Record("Final eligibility blocked", finalBackup);
                RecoveryRecordFiles.Archive(pendingRecord, journal + ".prepared.json");
                prepared = false;
                return new("Blocked", journal);
            }
            Record("Final eligibility verified", finalBackup);
            removalDispatched = true;
            await (host.RemoveAsync(target, token) ?? throw new InvalidOperationException("Removal provider returned no operation.")).WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (!await (host.VerifyAbsentAsync(target, token) ?? throw new InvalidOperationException("Absence provider returned no operation.")).WaitAsync(token).ConfigureAwait(false))
            { Record("Verification incomplete"); return new("Needs review", journal); }
            token.ThrowIfCancellationRequested();
            Record("Removal verified");
            RecoveryRecordFiles.Archive(pendingRecord, Path.Combine(folder, "verified-" + Guid.NewGuid().ToString("N") + ".json"));
            prepared = false;
            return new("Removal verified", journal);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !callerToken.IsCancellationRequested)
        {
            if (!removalDispatched) ClosePreparedRecord();
            Record(removalDispatched || !preparationCompleted
                ? "Timed out; verification incomplete"
                : "Timed out before removal; no change requested");
            return new(!removalDispatched && preparationCompleted ? "Blocked" : "Needs review", journal);
        }
        catch (OperationCanceledException)
        {
            if (!removalDispatched) ClosePreparedRecord();
            Record(removalDispatched || !preparationCompleted
                ? "Stopped; verification incomplete"
                : "Stopped before removal; no change requested");
            return new("Stopped", journal);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException)
        {
            if (!removalDispatched) ClosePreparedRecord();
            Record(removalDispatched || !preparationCompleted
                ? "Failed; verification incomplete"
                : "Final eligibility unavailable; no change requested");
            return new(!removalDispatched && preparationCompleted ? "Blocked" : "Needs review", journal);
        }
    }
}
