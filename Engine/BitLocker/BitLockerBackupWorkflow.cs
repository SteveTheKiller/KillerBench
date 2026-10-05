namespace KillerBench.Engine.BitLocker;

public interface IBitLockerBackupHost
{
    Task<string?> VolumeIdentityAsync(string volume, CancellationToken token);
    Task<BitLockerProtectorInventory> ProtectorsAsync(string volume, CancellationToken token);
    Task<bool> BackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token);
    Task<BitLockerBackupFileState> InspectBackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token) =>
        Task.FromResult(BitLockerBackupFileState.Unavailable);
}

public static class BitLockerBackupWorkflow
{
    public static async Task<RecoveryBackupReceipt> BackupExistingAsync(BitLockerOptions options, Guid protector,
        IBitLockerBackupHost host, TimeSpan? timeout = null, IProgress<string>? progress = null, Func<CancellationToken, Task>? prepare = null,
        Action? mutationStarting = null, CancellationToken token = default)
    {
        options.Validate();
        ArgumentNullException.ThrowIfNull(host);
        TimeSpan limit = timeout ?? TimeSpan.FromMinutes(5);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(5))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        token.ThrowIfCancellationRequested();
        if (options.Backup != RecoveryDestination.File || protector == Guid.Empty)
            throw new ArgumentException("Choose a file destination and a recovery protector.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        async Task<T> ReadAsync<T>(Func<CancellationToken, Task<T>> operation)
        {
            try
            {
                deadline.Token.ThrowIfCancellationRequested();
                T value = await operation(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
                deadline.Token.ThrowIfCancellationRequested();
                return value;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new TimeoutException("Backup deadline exceeded; a file may exist but is not verified.");
            }
        }
        void Report(string message)
        {
            try { progress?.Report(message); }
            catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
        }
        Report("Reading selected volume identity.");
        string? identity = await ReadAsync(t => host.VolumeIdentityAsync(options.Volume, t)).ConfigureAwait(false);
        RecoveryBackupReceipt Failed() => new(identity ?? "", protector, options.Backup, false);
        if (string.IsNullOrWhiteSpace(identity)) return Failed();
        Report("Checking selected recovery protector.");
        var inventory = await ReadAsync(t => host.ProtectorsAsync(options.Volume, t)).ConfigureAwait(false);
        if (!inventory.Complete || !inventory.Volume.Equals(options.Volume, StringComparison.OrdinalIgnoreCase) ||
            inventory.Protectors.Count(p => p.Id == protector && p.Type == "RecoveryPassword") != 1) return Failed();
        token.ThrowIfCancellationRequested();
        if (prepare is not null)
            await ReadAsync(async t => { await prepare(t).ConfigureAwait(false); return true; }).ConfigureAwait(false);
        var finalIdentity = await ReadAsync(t => host.VolumeIdentityAsync(options.Volume, t)).ConfigureAwait(false);
        var finalInventory = await ReadAsync(t => host.ProtectorsAsync(options.Volume, t)).ConfigureAwait(false);
        if (finalIdentity != identity || !finalInventory.Complete ||
            !finalInventory.Volume.Equals(options.Volume, StringComparison.OrdinalIgnoreCase) ||
            finalInventory.Protectors.Count(p => p.Id == protector && p.Type == "RecoveryPassword") != 1) return Failed();
        token.ThrowIfCancellationRequested();
        Report("Writing and verifying a new recovery backup file.");
        mutationStarting?.Invoke();
        if (!await ReadAsync(t => host.BackupFileAsync(options, protector, t)).ConfigureAwait(false)) return Failed();
        Report("Rechecking volume identity after backup.");
        var after = await ReadAsync(t => host.VolumeIdentityAsync(options.Volume, t)).ConfigureAwait(false);
        if (after != identity) return Failed();
        Report("Rechecking recovery protector after backup.");
        var current = await ReadAsync(t => host.ProtectorsAsync(options.Volume, t)).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (!current.Complete || !current.Volume.Equals(options.Volume, StringComparison.OrdinalIgnoreCase) ||
            current.Protectors.Count(p => p.Id == protector && p.Type == "RecoveryPassword") != 1) return Failed();
        return new(identity, protector, options.Backup, true);
    }
}

public sealed class WindowsBitLockerBackupHost : IBitLockerBackupHost
{
    public Task<BitLockerProtectorInventory> ProtectorsAsync(string volume, CancellationToken token) => BitLockerProtectors.InspectAsync(volume, token);
    public Task<bool> BackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token) => BitLockerFileBackup.BackupAsync(options, protector, token);
    public Task<BitLockerBackupFileState> InspectBackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token) =>
        BitLockerFileBackup.InspectAsync(options, protector, token);
    public async Task<string?> VolumeIdentityAsync(string volume, CancellationToken token)
    {
        new BitLockerOptions(volume).Validate();
        var step = new RepairStep(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "$ErrorActionPreference='Stop'; $v=@(Get-Volume -DriveLetter '" + char.ToUpperInvariant(volume[0]) + "' -ErrorAction Stop); if($v.Count -ne 1){throw 'Volume not unique'}; [string]$v[0].UniqueId"], true);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return InterpretVolumeIdentity(result);
    }

    public static string? InterpretVolumeIdentity(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return null;
        string identity = result.Output.Trim();
        return identity.Length is > 0 and <= 260 && !identity.Any(char.IsControl) ? identity : null;
    }
}
