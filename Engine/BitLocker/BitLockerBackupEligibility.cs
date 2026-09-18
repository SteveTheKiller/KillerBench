namespace KillerBench.Engine;

public enum BitLockerBackupDestinationState
{
    Available,
    Existing,
    MissingParent,
    UnsafeParent,
    Unavailable
}

public static class BitLockerBackupEligibility
{
    public static WorkflowCheck Assess(BitLockerOptions options, Guid? protector, bool isAdministrator,
        BitLockerBackupDestinationState destination, string? volumeIdentity,
        BitLockerProtectorInventory? inventory, bool pendingRecovery)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (options.Operation != BitLockerOperation.BackupRecovery || options.Backup != RecoveryDestination.File)
            return new("BitLocker recovery backup", CheckState.Information,
                "Select BackupRecovery and a new file destination to evaluate exact recovery-key backup. No backup was selected.");
        if (protector is null || protector == Guid.Empty)
            return new("BitLocker recovery backup", CheckState.Information,
                "Discover and select one exact recovery-password protector for the selected volume. No protector was selected.");
        if (pendingRecovery)
            return new("BitLocker recovery backup", CheckState.Attention,
                "A previous backup attempt requires reconciliation. Check the selected existing file without repeating the write before another backup attempt.");
        if (!isAdministrator)
            return new("BitLocker recovery backup", CheckState.Attention,
                "Administrator access is required to read the selected recovery protector and write a verified recovery backup.");
        if (destination == BitLockerBackupDestinationState.Existing)
            return new("BitLocker recovery backup", CheckState.Attention,
                "The selected destination already exists. Existing files are never overwritten; choose a new filename.");
        if (destination is BitLockerBackupDestinationState.MissingParent or BitLockerBackupDestinationState.UnsafeParent)
            return new("BitLocker recovery backup", CheckState.Attention,
                "The selected destination must use an existing non-reparse directory outside the protected volume.");
        if (destination == BitLockerBackupDestinationState.Unavailable)
            return new("BitLocker recovery backup", CheckState.Unknown,
                "The selected destination could not be inspected. No recovery backup is eligible.");
        if (string.IsNullOrWhiteSpace(volumeIdentity))
            return new("BitLocker recovery backup", CheckState.Unknown,
                "The selected volume identity is unavailable. No recovery backup is eligible.");
        if (inventory is null || !inventory.Complete ||
            !inventory.Volume.Equals(options.Volume, StringComparison.OrdinalIgnoreCase))
            return new("BitLocker recovery backup", CheckState.Unknown,
                "Recovery protector evidence for the exact selected volume is incomplete. No recovery backup is eligible.");
        int matches = inventory.Protectors.Count(candidate =>
            candidate.Id == protector.Value && candidate.Type == "RecoveryPassword");
        if (matches == 0)
            return new("BitLocker recovery backup", CheckState.Attention,
                "The selected recovery-password protector is no longer present on the exact selected volume. Discover protectors again.");
        if (matches != 1)
            return new("BitLocker recovery backup", CheckState.Unknown,
                "The selected recovery protector identity is ambiguous. No recovery backup is eligible.");
        return new("BitLocker recovery backup", CheckState.Repairable,
            "The exact selected volume, recovery-password protector and new file destination are eligible for guarded backup. Execution still requires separate confirmation and fresh identity verification.",
            RepairKind.BitLockerRecoveryBackup);
    }

    public static async Task<WorkflowCheck> CheckAsync(BitLockerOptions options, Guid? protector,
        string folder, IBitLockerBackupHost host, bool isAdministrator, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        options.Validate();
        BitLockerBackupDestinationState destination = InspectDestination(options);
        bool pendingRecovery;
        try { pendingRecovery = File.Exists(Path.Combine(Path.GetFullPath(folder), "backup.pending.json")); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return new("BitLocker recovery backup", CheckState.Unknown,
                "Pending backup state could not be inspected. No recovery backup is eligible.");
        }
        var selectedInventory = protector is { } selected && selected != Guid.Empty
            ? new BitLockerProtectorInventory(options.Volume, [new(selected, "RecoveryPassword")], true)
            : null;
        var preliminary = Assess(options, protector, isAdministrator, destination, "pending", selectedInventory, pendingRecovery);
        if (preliminary.State != CheckState.Repairable)
            return preliminary;
        try
        {
            string? identity = await (host.VolumeIdentityAsync(options.Volume, token) ??
                throw new InvalidOperationException("Volume identity provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(50), token).ConfigureAwait(false);
            var inventory = await (host.ProtectorsAsync(options.Volume, token) ??
                throw new InvalidOperationException("Recovery protector provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(50), token).ConfigureAwait(false);
            return Assess(options, protector, isAdministrator, destination, identity, inventory, false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new("BitLocker recovery backup", CheckState.Unknown,
                "Exact volume or recovery protector evidence could not be collected. No recovery backup is eligible.");
        }
    }

    public static BitLockerBackupDestinationState InspectDestination(BitLockerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (options.Operation != BitLockerOperation.BackupRecovery || options.Backup != RecoveryDestination.File)
            return BitLockerBackupDestinationState.Unavailable;
        try
        {
            string path = Path.GetFullPath(options.BackupFile!);
            if (File.Exists(path) || Directory.Exists(path)) return BitLockerBackupDestinationState.Existing;
            var parent = new DirectoryInfo(Path.GetDirectoryName(path)!);
            if (!parent.Exists) return BitLockerBackupDestinationState.MissingParent;
            while (parent is not null)
            {
                if (!parent.Exists) return BitLockerBackupDestinationState.MissingParent;
                if ((parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    return BitLockerBackupDestinationState.UnsafeParent;
                parent = parent.Parent;
            }
            return BitLockerBackupDestinationState.Available;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or System.Security.SecurityException)
        {
            return BitLockerBackupDestinationState.Unavailable;
        }
    }
}
