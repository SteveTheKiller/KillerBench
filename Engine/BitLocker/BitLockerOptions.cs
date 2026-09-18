namespace KillerBench.Engine;

public enum BitLockerOperation { Inspect, BackupRecovery, Encrypt, ResumeProtection }
public enum BitLockerEncryption { Aes128, Aes256, XtsAes128, XtsAes256 }
public enum RecoveryDestination { None, File, ActiveDirectory, EntraId }

public static class BitLockerChoices
{
    public static IReadOnlyList<BitLockerOperation> Operations { get; } = Array.AsReadOnly(Enum.GetValues<BitLockerOperation>());
    public static IReadOnlyList<BitLockerEncryption> EncryptionMethods { get; } = Array.AsReadOnly(Enum.GetValues<BitLockerEncryption>());
    public static IReadOnlyList<RecoveryDestination> Destinations { get; } = Array.AsReadOnly(Enum.GetValues<RecoveryDestination>());
}

public sealed record BitLockerOptions(string Volume, BitLockerOperation Operation = BitLockerOperation.Inspect,
    BitLockerEncryption Encryption = BitLockerEncryption.XtsAes128, bool UsedSpaceOnly = false,
    RecoveryDestination Backup = RecoveryDestination.None, string? BackupFile = null)
{
    public void Validate()
    {
        if (Volume is null || Volume.Length != 2 || !char.IsAsciiLetter(Volume[0]) || Volume[1] != ':')
            throw new ArgumentException("Select a drive-letter volume such as C:.");
        if (!Enum.IsDefined(Operation) || !Enum.IsDefined(Encryption) || !Enum.IsDefined(Backup))
            throw new ArgumentException("Unrecognized BitLocker option.");
        if (Operation is BitLockerOperation.Encrypt or BitLockerOperation.BackupRecovery && Backup == RecoveryDestination.None)
            throw new ArgumentException("Choose a recovery backup destination before this operation.");
        if (Backup == RecoveryDestination.File)
        {
            if (string.IsNullOrWhiteSpace(BackupFile) || !Path.IsPathFullyQualified(BackupFile) || string.IsNullOrEmpty(Path.GetFileName(BackupFile)))
                throw new ArgumentException("Choose an absolute recovery backup file path.");
            string full = Path.GetFullPath(BackupFile);
            if (full.StartsWith(Volume, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Save recovery information outside the volume being protected.");
            if (full.StartsWith(@"\\?\", StringComparison.Ordinal) || full.StartsWith(@"\\.\", StringComparison.Ordinal) || full.AsSpan(2).Contains(':'))
                throw new ArgumentException("Device paths and alternate streams are not recovery backup destinations.");
        }
        else if (!string.IsNullOrEmpty(BackupFile)) throw new ArgumentException("A backup file applies only to file backup.");
    }

    public string Describe()
    {
        Validate();
        return $"BITLOCKER OPTIONS / PREVIEW ONLY\nVolume: {Volume.ToUpperInvariant()}\nOperation: {Operation}\n" +
            $"Encryption: {Encryption}; used space only: {UsedSpaceOnly}\nRecovery destination: {Backup}\n" +
            "No encryption or protection change performed. Execution must verify current volume state, policy and recovery backup first. Encryption selection does not establish FIPS compliance.";
    }
}
