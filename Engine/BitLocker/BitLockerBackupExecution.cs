using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace KillerBench.Engine.BitLocker;

public sealed record BitLockerBackupResult(string Status, string JournalPath, RecoveryBackupReceipt? Receipt);
public sealed record BitLockerBackupPendingRecord(int Schema, DateTimeOffset StartedUtc, string Volume,
    Guid Protector, string DestinationHash, string JournalPath)
{
    public string Machine { get; init; } = "";
}

public static class BitLockerBackupExecution
{
    public static async Task<BitLockerBackupResult> RunAsync(BitLockerOptions options, Guid protector, string folder,
        IBitLockerBackupHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        ArgumentNullException.ThrowIfNull(host);
        if (options.Operation != BitLockerOperation.BackupRecovery || options.Backup != RecoveryDestination.File || protector == Guid.Empty)
            throw new ArgumentException("Select recovery backup, a file destination and an existing recovery protector.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(folder);
        using var gate = new FileStream(Path.Combine(folder, "backup.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "backup.pending.json");
        if (File.Exists(pending)) return new("A previous backup requires recovery before another write", pending, null);
        string journal = Path.Combine(folder, "backup-" + Guid.NewGuid().ToString("N") + ".jsonl");
        using var stream = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        void Record(string stage, RecoveryBackupReceipt? receipt = null)
        {
            var entry = new { Stage = stage, TimeUtc = DateTimeOffset.UtcNow, options.Volume, Protector = protector, Destination = options.Backup, Receipt = receipt };
            stream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry) + "\n"));
            stream.Flush(true);
        }
        Record("Started");
        var marker = new BitLockerBackupPendingRecord(1, DateTimeOffset.UtcNow, options.Volume.ToUpperInvariant(), protector,
            DestinationHash(options.BackupFile!), journal) { Machine = Environment.MachineName };
        bool backupDispatched = false;
        void CloseNoWriteRecord()
        {
            if (!backupDispatched && File.Exists(pending)) File.Move(pending, journal + ".prepared.json");
        }
        try
        {
            var receipt = await BitLockerBackupWorkflow.BackupExistingAsync(options, protector, host, progress: progress, prepare: ct => JournalPersistence.WriteAsync(pending, JsonSerializer.Serialize(marker, Capture.JsonOptions), token: ct),
                mutationStarting: () => backupDispatched = true,
                token: token).ConfigureAwait(false);
            string status = receipt.Verified ? "Backup verified" : backupDispatched ? "Backup not verified" : "Backup blocked before write";
            Record(status, receipt);
            if (receipt.Verified) File.Move(pending, journal + ".prepared.json");
            else if (!backupDispatched) CloseNoWriteRecord();
            return new(status, journal, receipt);
        }
        catch (OperationCanceledException)
        {
            CloseNoWriteRecord();
            string status = backupDispatched ? "Stopped; backup may exist but is not verified" : "Stopped before backup write; no file was requested";
            Record(status);
            return new(status, journal, null);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            CloseNoWriteRecord();
            // Provider errors may contain sensitive command data; keep ordinary journals free of error text.
            string status = backupDispatched ? "Backup failed; inspect the selected destination before another attempt" : "Backup eligibility failed before write; no file was requested";
            Record(status);
            return new(status, journal, null);
        }
    }

    public static string DestinationHash(string path)
    {
        string canonical = Path.GetFullPath(path).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
