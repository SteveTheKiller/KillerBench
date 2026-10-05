using System.Text.Json;

namespace KillerBench.Engine.BitLocker;

public enum BitLockerBackupReconciliationOutcome { NoPendingRecord, Verified, Missing, Invalid, Inconclusive }

public sealed record BitLockerBackupReconciliationResult(BitLockerBackupReconciliationOutcome Outcome,
    string Detail, string PendingPath, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is BitLockerBackupReconciliationOutcome.NoPendingRecord or
        BitLockerBackupReconciliationOutcome.Verified ? 0 : 3;
}

public static class BitLockerBackupReconciliation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<BitLockerBackupReconciliationResult> RunAsync(BitLockerOptions options, Guid protector,
        string folder, IBitLockerBackupHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        if (options.Operation != BitLockerOperation.BackupRecovery || options.Backup != RecoveryDestination.File || protector == Guid.Empty)
            throw new ArgumentException("Select recovery backup, a file destination and an existing recovery protector.");
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(folder);
        string pendingPath = Path.Combine(root, "backup.pending.json");
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pendingPath)) return new(BitLockerBackupReconciliationOutcome.NoPendingRecord,
            "No interrupted BitLocker backup record is present.", pendingPath);
        using var gate = new FileStream(Path.Combine(root, "backup.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        BitLockerBackupPendingRecord pending;
        try
        {
            pending = await ReadAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        {
            return new(BitLockerBackupReconciliationOutcome.Inconclusive,
                "The interrupted backup record could not be verified for this computer. The record was retained and no file was written.", pendingPath);
        }
        if (!string.Equals(pending.Volume, options.Volume, StringComparison.OrdinalIgnoreCase) || pending.Protector != protector ||
            pending.DestinationHash != BitLockerBackupExecution.DestinationHash(options.BackupFile!))
            return new(BitLockerBackupReconciliationOutcome.Inconclusive,
                "The selected volume, protector or destination does not match the pending backup. The record was retained and no file was written.", pendingPath);
        Report(progress, "Checking the existing recovery backup without writing it again.");
        BitLockerBackupFileState state;
        try
        {
            var task = host.InspectBackupFileAsync(options, protector, token) ??
                throw new InvalidOperationException("Backup inspection provider returned no task.");
            state = await task.WaitAsync(TimeSpan.FromSeconds(50), token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(BitLockerBackupReconciliationOutcome.Inconclusive,
                "The existing backup could not be inspected. The pending record was retained and no file was written.", pendingPath);
        }
        if (state == BitLockerBackupFileState.Unavailable)
            return new(BitLockerBackupReconciliationOutcome.Inconclusive,
                "The existing backup could not be verified. The pending record was retained and no file was written.", pendingPath);
        var outcome = state switch
        {
            BitLockerBackupFileState.Verified => BitLockerBackupReconciliationOutcome.Verified,
            BitLockerBackupFileState.Missing => BitLockerBackupReconciliationOutcome.Missing,
            _ => BitLockerBackupReconciliationOutcome.Invalid
        };
        string detail = outcome switch
        {
            BitLockerBackupReconciliationOutcome.Verified => "The existing file matches the current exact recovery protector and restricted access requirements. The pending record was archived without rewriting the file.",
            BitLockerBackupReconciliationOutcome.Missing => "The selected backup file is absent. The failed attempt was archived without writing a file; choose a destination before another explicit attempt.",
            _ => "The selected file does not match the current exact recovery protector or restricted access requirements. The attempt was archived without changing the file."
        };
        string archive = Path.Combine(root, "backup-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new BitLockerBackupReconciliationResult(outcome, detail, pendingPath, archive);
        await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(result, Capture.JsonOptions), token: token).ConfigureAwait(false);
        File.Move(pendingPath, archive + ".prepared.json");
        Report(progress, detail);
        return result;
    }

    private static async Task<BitLockerBackupPendingRecord> ReadAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 32_768) throw new InvalidDataException("BitLocker backup record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("BitLocker backup record contains conflicting fields.");
            var record = document.RootElement.Deserialize<BitLockerBackupPendingRecord>(StrictJson);
            if (record is null || record.Schema != 1 || record.StartedUtc == default || record.Protector == Guid.Empty ||
                !record.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(record.Volume) || record.Volume.Length != 2 || !char.IsAsciiLetter(record.Volume[0]) || record.Volume[1] != ':' ||
                string.IsNullOrEmpty(record.DestinationHash) || record.DestinationHash.Length != 64 ||
                record.DestinationHash.Any(character => !Uri.IsHexDigit(character)) || string.IsNullOrWhiteSpace(record.JournalPath))
                throw new InvalidDataException("BitLocker backup record is invalid.");
            return record;
        }
        catch (JsonException error) { throw new InvalidDataException("BitLocker backup record is not valid JSON.", error); }
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
