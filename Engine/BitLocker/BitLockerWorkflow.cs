namespace KillerBench.Engine.BitLocker;

public sealed record BitLockerReadiness(string Volume, string VolumeIdentity, bool CanEncrypt, string Detail);
public sealed record RecoveryBackupReceipt(string VolumeIdentity, Guid ProtectorId, RecoveryDestination Destination, bool Verified);
public sealed record BitLockerStartResult(bool Started, string Detail);
public interface IBitLockerWorkflowHost
{
    Task<BitLockerReadiness> InspectAsync(BitLockerOptions options, CancellationToken token);
    Task<RecoveryBackupReceipt> PrepareAndVerifyBackupAsync(BitLockerOptions options, BitLockerReadiness readiness, CancellationToken token);
    Task<BitLockerStartResult> StartEncryptionAsync(BitLockerOptions options, RecoveryBackupReceipt receipt, CancellationToken token);
}

public static class BitLockerWorkflow
{
    // Providers own native policy checks, durable journals and bounded operations. Receipts contain no recovery secret.
    public static async Task<BitLockerStartResult> EncryptAsync(BitLockerOptions options, IBitLockerWorkflowHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        options.Validate();
        ArgumentNullException.ThrowIfNull(host);
        if (options.Operation != BitLockerOperation.Encrypt) throw new ArgumentException("Select Encrypt for this workflow.");
        token.ThrowIfCancellationRequested();
        var readiness = await host.InspectAsync(options, token).ConfigureAwait(false);
        if (!Matches(readiness, options) || !readiness.CanEncrypt)
            return new(false, "Encryption not started: " + readiness.Detail);
        progress?.Report("Preparing and verifying the selected recovery backup...");
        var receipt = await host.PrepareAndVerifyBackupAsync(options, readiness, token).ConfigureAwait(false);
        if (!receipt.Verified || receipt.ProtectorId == Guid.Empty || receipt.Destination != options.Backup || receipt.VolumeIdentity != readiness.VolumeIdentity)
            return new(false, "Recovery backup was not verified for the selected volume and destination. Encryption not started.");
        token.ThrowIfCancellationRequested();
        var fresh = await host.InspectAsync(options, token).ConfigureAwait(false);
        if (!Matches(fresh, options) || !fresh.CanEncrypt || fresh.VolumeIdentity != readiness.VolumeIdentity)
            return new(false, "Volume or readiness changed after backup. Encryption not started.");
        token.ThrowIfCancellationRequested();
        progress?.Report("Recovery backup verified. Requesting encryption startup...");
        return await host.StartEncryptionAsync(options, receipt, token).ConfigureAwait(false);
    }

    private static bool Matches(BitLockerReadiness state, BitLockerOptions options) =>
        !string.IsNullOrWhiteSpace(state.VolumeIdentity) && state.Volume.Equals(options.Volume, StringComparison.OrdinalIgnoreCase);
}
