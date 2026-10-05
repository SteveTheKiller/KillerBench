using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KillerBench.Engine.Scheduling;

public enum TaskRegistrationRecoveryOutcome
{
    NoPendingRecord,
    Verified,
    Inconclusive,
    Conflict
}

public sealed record TaskRegistrationRecoveryResult(
    TaskRegistrationRecoveryOutcome Outcome,
    ScheduledTaskIdentity Target,
    string Detail,
    string? PendingRecord,
    string? RecoveryRecord,
    IReadOnlyList<ToolResult> Evidence);

public interface ITaskRegistrationRecoveryHost
{
    Task<ScheduledTaskDefinitionResult> ReadAsync(ScheduledTaskIdentity target, CancellationToken token);
}

/// <summary>Reconciles an interrupted create-only request by inspection. It never creates, changes, runs or removes a task.</summary>
public static class TaskRegistrationRecovery
{
    private const int MaximumRecordBytes = 1024 * 1024;

    public static async Task<TaskRegistrationRecoveryResult> RecoverAsync(
        ScheduledTaskPlan draft,
        string journalDirectory,
        ITaskRegistrationRecoveryHost? host = null,
        TimeSpan? timeout = null,
        IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        var plan = draft.Validate();
        string pendingPath = FileTaskRegistrationJournal.GetPendingPath(journalDirectory, plan.Identity);
        var evidence = new List<ToolResult>();
        TaskRegistrationRecoveryResult Result(TaskRegistrationRecoveryOutcome outcome, string detail, string? recovery = null) =>
            new(outcome, plan.Identity, detail,
                outcome == TaskRegistrationRecoveryOutcome.NoPendingRecord ? null : pendingPath,
                recovery, evidence.AsReadOnly());

        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(30);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(timeout));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(limit);

        Report(progress, "Reading the pending task registration record.");
        byte[] initialBytes;
        try { initialBytes = await ReadBoundedAsync(pendingPath, FileShare.Read, stop.Token).ConfigureAwait(false); }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return Result(TaskRegistrationRecoveryOutcome.NoPendingRecord,
                "No pending registration record exists for the exact task target. No task was inspected or changed.");
        }
        byte[] initialHash = SHA256.HashData(initialBytes);
        var review = await TaskJournalReview.ReadAsync(pendingPath, stop.Token).ConfigureAwait(false);
        if (review.State != "Pending" || review.RecordedEvidence is not { } record)
            return Result(TaskRegistrationRecoveryOutcome.Inconclusive,
                "The pending registration record is unreadable or incomplete. The guard remains in place and no task was changed.");
        if (!TryReadIntent(record, plan, out Guid attemptId, out DateOnly startDate, out string intentFailure))
            return Result(TaskRegistrationRecoveryOutcome.Conflict,
                intentFailure + " The guard remains in place and no task was changed.");

        string requestedXml;
        try { requestedXml = ScheduledTaskXml.Serialize(plan, startDate); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException)
        {
            return Result(TaskRegistrationRecoveryOutcome.Inconclusive,
                "The journaled task definition cannot be reconstructed on this machine: " + error.Message + " The guard remains in place.");
        }

        stop.Token.ThrowIfCancellationRequested();
        Report(progress, "Reading the exact current disabled task definition without changing it.");
        host ??= PlatformProviders.Create<ITaskRegistrationRecoveryHost>();
        var observed = await Required(host.ReadAsync(plan.Identity, stop.Token), stop.Token).ConfigureAwait(false);
        evidence.Add(observed.Evidence);
        if (!plan.Identity.Matches(observed.Target) || observed.Xml is null || !TaskRegistration.Complete(observed.Evidence))
            return Result(TaskRegistrationRecoveryOutcome.Inconclusive,
                "The exact current task definition is unavailable or incomplete. Absence was not inferred, the guard remains in place and no retry was attempted.");
        if (!ScheduledTaskXml.MatchesRequested(requestedXml, observed.Xml))
            return Result(TaskRegistrationRecoveryOutcome.Conflict,
                "The current task definition does not match the journaled disabled definition. The guard remains in place and no task was changed.");

        stop.Token.ThrowIfCancellationRequested();
        Report(progress, "Locking and rechecking the unchanged pending record.");
        string suffix = "." + attemptId.ToString("N");
        string recoveryPath = pendingPath + suffix + ".recovered.json";
        string archivePath = pendingPath + suffix + ".recovered-prepared.json";
        await using (var pending = new FileStream(pendingPath, FileMode.Open, FileAccess.Read, FileShare.None,
            65536, FileOptions.Asynchronous))
        {
            byte[] currentBytes = await ReadBoundedAsync(pending, stop.Token).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(initialHash, SHA256.HashData(currentBytes)))
                return Result(TaskRegistrationRecoveryOutcome.Inconclusive,
                    "The pending record changed during recovery. The guard remains in place and no task was changed.");

            string pendingSha256 = Convert.ToHexString(initialHash);
            string requestedSha256 = HashText(requestedXml);
            string observedSha256 = HashText(observed.Xml);
            if (File.Exists(recoveryPath))
            {
                if (!await ExistingRecoveryMatchesAsync(recoveryPath, attemptId, plan.Identity.FullName, startDate,
                    pendingSha256, requestedSha256, stop.Token).ConfigureAwait(false))
                    return Result(TaskRegistrationRecoveryOutcome.Inconclusive,
                        "An existing recovery record does not match this exact observation. The guard remains in place and no task was changed.", recoveryPath);
            }
            else
            {
                byte[] resultBytes = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    schemaVersion = 1,
                    recordedUtc = DateTimeOffset.UtcNow,
                    state = "RecoveredDefinitionVerified",
                    machine = Environment.MachineName,
                    attemptId,
                    taskExecuted = false,
                    target = plan.Identity.FullName,
                    startDate,
                    pendingSha256,
                    requestedSha256,
                    observedSha256
                });
                await WriteNewAsync(recoveryPath, resultBytes, stop.Token).ConfigureAwait(false);
            }
        }

        if (File.Exists(archivePath))
            return Result(TaskRegistrationRecoveryOutcome.Inconclusive,
                "The verified recovery archive already exists while the pending guard is still present. No file or task was replaced.", recoveryPath);
        File.Move(pendingPath, archivePath);
        Report(progress, "Exact disabled definition verified. The pending guard was closed without retrying or running the task.");
        return new(TaskRegistrationRecoveryOutcome.Verified, plan.Identity,
            "The exact current disabled task definition matches the journaled request. The interrupted registration guard was closed. The task was not run.",
            archivePath, recoveryPath, evidence.AsReadOnly());
    }

    private static bool TryReadIntent(JsonElement record, ScheduledTaskPlan requested, out Guid attemptId,
        out DateOnly startDate, out string failure)
    {
        attemptId = Guid.Empty;
        startDate = default;
        failure = "The pending record is not eligible for exact recovery.";
        try
        {
            if (record.GetProperty("schemaVersion").GetInt32() != 2)
            { failure = "This older pending record does not contain the calendar boundary needed for exact recovery."; return false; }
            if (!record.GetProperty("attemptId").TryGetGuid(out attemptId) || attemptId == Guid.Empty)
            { failure = "The pending record has no valid attempt identity."; return false; }
            if (!string.Equals(record.GetProperty("machine").GetString(), Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            { failure = "The pending record belongs to another machine."; return false; }
            if (!DateOnly.TryParseExact(record.GetProperty("startDate").GetString(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            { failure = "The pending record has no valid calendar boundary."; return false; }
            var journaled = record.GetProperty("plan").Deserialize<ScheduledTaskPlan>()?.Validate();
            if (journaled is null || !ScheduledTaskPlanJson.Serialize(journaled).SequenceEqual(ScheduledTaskPlanJson.Serialize(requested)))
            { failure = "The pending record describes a different task plan."; return false; }
            return true;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or FormatException)
        { failure = "The pending record cannot be interpreted: " + error.Message; return false; }
    }

    private static async Task<bool> ExistingRecoveryMatchesAsync(string path, Guid attemptId, string target,
        DateOnly startDate, string pendingSha256, string requestedSha256, CancellationToken token)
    {
        try
        {
            byte[] bytes = await ReadBoundedAsync(path, FileShare.Read, token).ConfigureAwait(false);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            var properties = root.ValueKind == JsonValueKind.Object ? root.EnumerateObject().ToArray() : [];
            if (properties.Length != 11 || properties.Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 11)
                return false;
            return root.GetProperty("schemaVersion").GetInt32() == 1 &&
                root.GetProperty("recordedUtc").TryGetDateTimeOffset(out _) &&
                root.GetProperty("state").GetString() == "RecoveredDefinitionVerified" &&
                string.Equals(root.GetProperty("machine").GetString(), Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                root.GetProperty("attemptId").TryGetGuid(out Guid storedAttempt) && storedAttempt == attemptId &&
                root.GetProperty("taskExecuted").ValueKind == JsonValueKind.False &&
                string.Equals(root.GetProperty("target").GetString(), target, StringComparison.OrdinalIgnoreCase) &&
                root.GetProperty("startDate").GetString() == startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) &&
                root.GetProperty("pendingSha256").GetString() == pendingSha256 &&
                root.GetProperty("requestedSha256").GetString() == requestedSha256 &&
                ValidSha256(root.GetProperty("observedSha256").GetString());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or
            KeyNotFoundException or FormatException or OverflowException)
        { return false; }
    }

    private static async Task<byte[]> ReadBoundedAsync(string path, FileShare share, CancellationToken token)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, share, 65536, FileOptions.Asynchronous);
        return await ReadBoundedAsync(file, token).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken token)
    {
        byte[] bytes = new byte[MaximumRecordBytes + 1];
        int count = 0;
        while (count < bytes.Length)
        {
            int read = await stream.ReadAsync(bytes.AsMemory(count), token).ConfigureAwait(false);
            if (read == 0) break;
            count += read;
        }
        if (count == bytes.Length) throw new InvalidDataException("Task registration record exceeds 1 MiB.");
        return bytes.AsSpan(0, count).ToArray();
    }

    private static async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
    {
        if (bytes.Length > MaximumRecordBytes) throw new InvalidDataException("Task recovery record exceeds 1 MiB.");
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            65536, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await file.WriteAsync(bytes, token).ConfigureAwait(false);
        await file.FlushAsync(token).ConfigureAwait(false);
        file.Flush(flushToDisk: true);
    }

    private static string HashText(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool ValidSha256(string? value) => value is { Length: 64 } && value.All(character =>
        character is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static async Task<T> Required<T>(Task<T>? task, CancellationToken token) where T : class
    {
        if (task is null) throw new InvalidOperationException("Task recovery provider returned no task.");
        return await task.WaitAsync(token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Task recovery provider returned no result.");
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}
