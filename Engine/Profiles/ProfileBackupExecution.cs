using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public interface IProfileBackupHost
{
    Task<ProfileInventoryResult> InventoryAsync(CancellationToken token);
    Task<ProfileDependencyResult> DependenciesAsync(CancellationToken token);
    Task<ProfileRegistrationResult> RegistrationsAsync(CancellationToken token);
    Task<bool> ReadyAsync(CancellationToken token);
    Task<bool> DestinationAbsentAsync(string destinationPath, CancellationToken token);
    Task CopyAsync(ProfileSnapshot target, string destinationPath, IProgress<string>? progress, CancellationToken token);
}

public sealed record ProfileBackupExecutionResult(string Status, string JournalPath, ProfileBackupVerification? Verification);

public static class ProfileBackupExecution
{
    public static async Task<ProfileBackupExecutionResult> RunAsync(ProfileSnapshot target, int minimumDays,
        string destinationPath, string folder, IProfileBackupHost host, IProgress<string>? progress = null,
        TimeSpan? operationLimit = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(host);
        ProfilePlanning.ValidateSelection([target.Sid]);
        if (minimumDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(minimumDays));
        TimeSpan limit = operationLimit ?? TimeSpan.FromHours(2);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromHours(4)) throw new ArgumentOutOfRangeException(nameof(operationLimit));
        string destination = ValidateDestination(target, destinationPath);
        token.ThrowIfCancellationRequested();
        CancellationToken callerToken = token;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        token = deadline.Token;

        Directory.CreateDirectory(folder);
        using var gate = new FileStream(Path.Combine(folder, "backup.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "pending-backup.json");
        if (File.Exists(pending)) throw new InvalidOperationException("An earlier profile backup is unresolved. No copy retry started.");
        string journal = Path.Combine(folder, "profile-backup-" + Guid.NewGuid().ToString("N") + ".jsonl");
        using var stream = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        void Record(string stage, ProfileBackupVerification? verification = null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                Stage = stage,
                TimeUtc = DateTimeOffset.UtcNow,
                Target = target,
                DestinationPath = destination,
                Verification = verification
            }) + "\n");
            stream.Write(bytes);
            stream.Flush(true);
            progress?.Report(stage);
        }
        async Task<bool> CheckAsync(bool requireAbsent)
        {
            var readiness = host.ReadyAsync(token) ?? throw new InvalidOperationException("Readiness provider returned no operation.");
            if (!await readiness.WaitAsync(token).ConfigureAwait(false)) return false;
            var plan = await ProfileReview.RefreshAsync([target.Sid], minimumDays, host.InventoryAsync, host.DependenciesAsync,
                host.RegistrationsAsync, token: token).ConfigureAwait(false);
            if (!plan.ReviewPassed || plan.Targets.Single() != target) return false;
            if (!requireAbsent) return true;
            var absent = host.DestinationAbsentAsync(destination, token) ??
                throw new InvalidOperationException("Backup destination provider returned no operation.");
            return await absent.WaitAsync(token).ConfigureAwait(false);
        }

        bool prepared = false;
        bool copyDispatched = false;
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
            if (!await CheckAsync(requireAbsent: true).ConfigureAwait(false))
            {
                Record("Blocked before preparation");
                return new("Blocked", journal, null);
            }

            using (var marker = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(marker, new PendingProfileBackup(1, Environment.MachineName,
                    DateTimeOffset.UtcNow, target, destination, journal), Capture.JsonOptions);
                marker.Flush(true);
            }
            prepared = true;
            Record("Prepared");
            using var pendingRecord = RecoveryRecordFiles.OpenLocked(pending);

            if (!await CheckAsync(requireAbsent: true).ConfigureAwait(false))
            {
                Record("Final eligibility blocked; no copy requested");
                RecoveryRecordFiles.Archive(pendingRecord, journal + ".prepared.json");
                prepared = false;
                return new("Blocked", journal, null);
            }

            Record("Final eligibility verified");
            copyDispatched = true;
            var copy = host.CopyAsync(target, destination, progress, token) ??
                throw new InvalidOperationException("Profile copy provider returned no operation.");
            await copy.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Record("Copy provider completed; verifying source and destination");
            var verification = await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(target, destination,
                token: token).ConfigureAwait(false);
            if (!verification.Verified)
            {
                Record("Backup verification incomplete", verification);
                return new("Needs review", journal, verification);
            }

            Record("Backup verified", verification);
            RecoveryRecordFiles.Archive(pendingRecord, Path.Combine(folder, "verified-backup-" + Guid.NewGuid().ToString("N") + ".json"));
            prepared = false;
            return new("Backup verified", journal, verification);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !callerToken.IsCancellationRequested)
        {
            if (!copyDispatched) ClosePreparedRecord();
            Record(copyDispatched ? "Timed out; backup verification incomplete" : "Timed out before copy; no copy requested");
            return new(copyDispatched ? "Needs review" : "Blocked", journal, null);
        }
        catch (OperationCanceledException)
        {
            if (!copyDispatched) ClosePreparedRecord();
            Record(copyDispatched ? "Stopped; backup verification incomplete" : "Stopped before copy; no copy requested");
            return new("Stopped", journal, null);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or ArgumentException)
        {
            if (!copyDispatched) ClosePreparedRecord();
            Record(copyDispatched ? "Failed; backup verification incomplete" : "Final eligibility unavailable; no copy requested");
            return new(copyDispatched ? "Needs review" : "Blocked", journal, null);
        }
    }

    internal static string ValidateDestination(ProfileSnapshot target, string destinationPath)
    {
        if (!ProfilePlanning.IsSid(target.Sid) || !ProfilePlanning.IsLocalProfilePath(target.Path) ||
            string.IsNullOrWhiteSpace(destinationPath) || !Path.IsPathFullyQualified(destinationPath))
            throw new ArgumentException("The profile or backup destination identity is invalid.", nameof(destinationPath));
        string source = Path.GetFullPath(target.Path).TrimEnd('\\');
        string destination = Path.GetFullPath(destinationPath).TrimEnd('\\');
        if (!ProfilePlanning.IsLocalProfilePath(destination) || source.Equals(destination, StringComparison.OrdinalIgnoreCase) ||
            destination.StartsWith(source + "\\", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith(destination + "\\", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The backup destination must be a separate non-nested local path.", nameof(destinationPath));
        return destination;
    }
}

internal sealed record PendingProfileBackup(int Schema, string Machine, DateTimeOffset StartedUtc,
    ProfileSnapshot Target, string DestinationPath, string Journal);

public static class ProfileBackupRecovery
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<ProfileBackupVerification> TryResolveAsync(string folder, TimeSpan? verificationLimit = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        TimeSpan limit = verificationLimit ?? TimeSpan.FromMinutes(30);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromHours(2)) throw new ArgumentOutOfRangeException(nameof(verificationLimit));
        if (!Directory.Exists(folder)) return Failed("No unresolved profile backup is present.");
        using var gate = new FileStream(Path.Combine(folder, "backup.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "pending-backup.json");
        if (!File.Exists(pending)) return Failed("No unresolved profile backup is present.");
        using var stream = RecoveryRecordFiles.OpenLocked(pending);
        PendingProfileBackup record;
        try
        {
            if (stream.Length is <= 0 or > 65_536) return Failed("The pending backup record is outside its size bound.");
            using var json = JsonDocument.Parse(stream);
            if (!UniqueFields(json.RootElement)) return Failed("The pending backup record contains ambiguous fields.");
            record = json.RootElement.Deserialize<PendingProfileBackup>(StrictJson)!;
            if (record is null || record.Schema != 1 || record.StartedUtc == default ||
                !record.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(record.Journal) ||
                ProfileBackupExecution.ValidateDestination(record.Target, record.DestinationPath) != record.DestinationPath)
                return Failed("The pending backup record identity is invalid.");
            string root = Path.GetFullPath(folder);
            string journal = Path.GetFullPath(record.Journal);
            if (!Path.GetDirectoryName(journal)!.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(journal).StartsWith("profile-backup-", StringComparison.OrdinalIgnoreCase) ||
                !journal.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                return Failed("The pending backup journal identity is invalid.");
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        { return Failed("The pending backup record is malformed."); }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        ProfileBackupVerification verification;
        try
        {
            string receipt = Path.Combine(record.DestinationPath, ProfileBackupArtifactVerifier.ReceiptFileName);
            verification = await ProfileBackupArtifactVerifier.VerifyAsync(record.Target, receipt, token: deadline.Token)
                .WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !token.IsCancellationRequested)
        { return Failed("Backup recovery verification timed out. The pending record remains."); }
        token.ThrowIfCancellationRequested();
        if (!verification.Verified) return verification;
        RecoveryRecordFiles.Archive(stream, Path.Combine(folder, "resolved-backup-" + Guid.NewGuid().ToString("N") + ".json"));
        return verification;
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static ProfileBackupVerification Failed(string detail) => new(null, false, detail);
}
