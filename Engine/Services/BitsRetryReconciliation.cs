using System.Text.Json;

namespace KillerBench.Engine.Services;

public sealed record BitsRetryPendingRecord(Guid JobId, DateTimeOffset Started, BitsInventoryResult Before)
{
    public string Machine { get; init; } = "";
}

public enum BitsRetryReconciliationOutcome
{
    NoPendingRecord, Active, TransferReported, StableFailure, TerminalOrRemoved, Inconclusive
}

public sealed record BitsRetryReconciliationResult(Guid JobId, BitsRetryReconciliationOutcome Outcome,
    string Detail, string PendingPath, BitsRetryPendingRecord? Pending = null,
    BitsInventoryResult? First = null, BitsInventoryResult? Second = null, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is BitsRetryReconciliationOutcome.NoPendingRecord or
        BitsRetryReconciliationOutcome.TransferReported ? 0 : Outcome == BitsRetryReconciliationOutcome.Active ? 4 : 3;
}

public static class BitsRetryReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<BitsRetryReconciliationResult> RunAsync(string directory, Guid jobId,
        IBitsRetryHost host, TimeSpan? observationInterval = null, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (jobId == Guid.Empty) throw new ArgumentException("An exact transfer ID is required.", nameof(jobId));
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan interval = observationInterval ?? TimeSpan.FromSeconds(5);
        if (interval < TimeSpan.Zero || interval > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(observationInterval));
        string root = Path.GetFullPath(directory);
        string pendingPath = Path.Combine(root, jobId.ToString("N") + ".pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new(jobId, BitsRetryReconciliationOutcome.NoPendingRecord,
            "No pending retry record exists for this transfer.", pendingPath);

        await using var stream = new FileStream(pendingPath, FileMode.Open, FileAccess.Read,
            FileShare.Read | FileShare.Delete, 4096, FileOptions.Asynchronous);
        BitsRetryPendingRecord pending;
        try
        {
            pending = await ReadAsync(stream, jobId, host.OwnerAccount, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or System.Security.SecurityException or InvalidOperationException)
        {
            return new(jobId, BitsRetryReconciliationOutcome.Inconclusive,
                "The pending retry record could not be verified for this computer and account. The retry guard was retained and no retry was requested.",
                pendingPath);
        }
        Report(progress, "Reading the selected transfer twice without requesting another retry.");
        BitsInventoryResult first;
        BitsInventoryResult second;
        try
        {
            first = await ReadCurrentAsync(host, token).ConfigureAwait(false);
            if (interval > TimeSpan.Zero) await Task.Delay(interval, token).ConfigureAwait(false);
            second = await ReadCurrentAsync(host, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            NotSupportedException or System.Security.SecurityException or InvalidOperationException or TimeoutException or
            System.ComponentModel.Win32Exception)
        {
            return new(jobId, BitsRetryReconciliationOutcome.Inconclusive,
                "Current transfer evidence is unavailable: " + error.Message + ". The retry guard was retained and no retry was requested.",
                pendingPath, pending);
        }

        if (!Complete(first) || !Complete(second)) return new(jobId, BitsRetryReconciliationOutcome.Inconclusive,
            "Two complete running-account snapshots are required. The retry guard was retained and no retry was requested.",
            pendingPath, pending, first, second);
        var expected = pending.Before.Jobs.Single(job => job.Id == jobId);
        var one = first.Jobs.SingleOrDefault(job => job.Id == jobId);
        var two = second.Jobs.SingleOrDefault(job => job.Id == jobId);
        if (one is null || two is null)
        {
            if (one is not null || two is not null) return new(jobId, BitsRetryReconciliationOutcome.Inconclusive,
                "The transfer appeared in only one snapshot. The retry guard was retained and no retry was requested.",
                pendingPath, pending, first, second);
            return await ArchiveAsync(BitsRetryReconciliationOutcome.TerminalOrRemoved,
                "The exact transfer was absent from two complete snapshots. Its retry guard was archived without claiming transfer or application completion.",
                root, pendingPath, pending, first, second, stream, token).ConfigureAwait(false);
        }
        bool identity = SameIdentity(expected, one, host.OwnerAccount) && SameIdentity(expected, two, host.OwnerAccount);
        bool stable = identity && one.State == two.State && one.BytesTransferred == two.BytesTransferred &&
            one.ErrorCode == two.ErrorCode && one.Error == two.Error;
        if (!stable) return new(jobId, BitsRetryReconciliationOutcome.Inconclusive,
            "The exact transfer changed between snapshots or no longer matches the prepared identity. The retry guard was retained and no retry was requested.",
            pendingPath, pending, first, second);
        if (two.State is "Queued" or "Connecting" or "Transferring")
            return new(jobId, BitsRetryReconciliationOutcome.Active,
                "BITS still reports the exact transfer queued, connecting or transferring. The retry guard was retained; completion remains unverified.",
                pendingPath, pending, first, second);
        if (two.State == "Transferred")
            return await ArchiveAsync(BitsRetryReconciliationOutcome.TransferReported,
                "BITS reported the exact transfer as transferred in two snapshots. The retry guard was archived. Owning-application acknowledgment and destination use remain unverified.",
                root, pendingPath, pending, first, second, stream, token).ConfigureAwait(false);
        if (two.State is "Error" or "TransientError")
            return await ArchiveAsync(BitsRetryReconciliationOutcome.StableFailure,
                "BITS reported the same error state in two snapshots. The attempt was archived without another retry; review the error and owning application before a new explicit attempt.",
                root, pendingPath, pending, first, second, stream, token).ConfigureAwait(false);
        if (two.State is "Acknowledged" or "Cancelled")
            return await ArchiveAsync(BitsRetryReconciliationOutcome.TerminalOrRemoved,
                "BITS reported a terminal job state in two snapshots. The retry guard was archived without claiming application completion.",
                root, pendingPath, pending, first, second, stream, token).ConfigureAwait(false);
        return new(jobId, BitsRetryReconciliationOutcome.Inconclusive,
            "BITS reports the exact transfer suspended. The retry guard was retained and no retry was requested.",
            pendingPath, pending, first, second);
    }

    private static async Task<BitsInventoryResult> ReadCurrentAsync(IBitsRetryHost host, CancellationToken token)
    {
        var task = host.ReadAsync(token) ?? throw new InvalidOperationException("BITS provider returned no task.");
        var current = await task.WaitAsync(TimeSpan.FromSeconds(35), token).ConfigureAwait(false);
        if (current?.Evidence is null) throw new InvalidDataException("BITS provider returned no evidence.");
        return BitsDiagnostics.Parse(current.Evidence, token);
    }

    private static bool Complete(BitsInventoryResult value) => value.Complete && value.Scope == "CurrentAccount";
    private static bool SameIdentity(BitsJobSnapshot expected, BitsJobSnapshot current, string owner) =>
        current.Id == expected.Id && current.Owner == expected.Owner && current.Owner == owner &&
        current.Name == expected.Name && current.BytesTotal == expected.BytesTotal;

    private static async Task<BitsRetryPendingRecord> ReadAsync(FileStream stream, Guid jobId, string owner, CancellationToken token)
    {
        if (stream.Length is <= 0 or > 262_144) throw new InvalidDataException("BITS retry record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("BITS retry record contains conflicting fields.");
            var record = document.RootElement.Deserialize<BitsRetryPendingRecord>(StrictJson);
            var parsed = record is null ? null : BitsDiagnostics.Parse(record.Before.Evidence, token);
            var selected = parsed?.Jobs.SingleOrDefault(job => job.Id == jobId);
            if (record is null || record.JobId != jobId || record.Started == default ||
                !record.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                parsed is not { Complete: true, Scope: "CurrentAccount" } ||
                selected is null || selected.Owner != owner || selected.State is not ("Error" or "TransientError") ||
                selected.ErrorCode is null || selected.BytesTransferred is null)
                throw new InvalidDataException("BITS retry record is invalid or belongs to a different transfer or account.");
            return record with { Before = parsed };
        }
        catch (JsonException error) { throw new InvalidDataException("BITS retry record is not valid JSON.", error); }
    }

    private static async Task<BitsRetryReconciliationResult> ArchiveAsync(BitsRetryReconciliationOutcome outcome,
        string detail, string root, string pendingPath, BitsRetryPendingRecord pending, BitsInventoryResult first,
        BitsInventoryResult second, FileStream stream, CancellationToken token)
    {
        string archivePath = Path.Combine(root, "bits-retry-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new BitsRetryReconciliationResult(pending.JobId, outcome, detail, pendingPath, pending, first, second, archivePath);
        await JournalPersistence.WriteAsync(archivePath, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        string oldResult = Path.Combine(root, pending.JobId.ToString("N") + ".result.json");
        if (File.Exists(oldResult)) File.Move(oldResult, archivePath + ".attempt.json");
        File.Move(pendingPath, archivePath + ".prepared.json");
        await stream.DisposeAsync().ConfigureAwait(false);
        Report(null, detail);
        return result;
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}
