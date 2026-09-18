namespace KillerBench.Engine;

public enum RepairRecoveryContract
{
    None,
    ExactServiceState,
    ServicingHealth,
    ComponentCleanupRecommendation,
    DefenderSignatureFreshness,
    WmiRepositoryConsistency,
    TimeSynchronizationState,
    TimeSourceState,
    DhcpAdapterState,
    PausedQueueState,
    PrinterOfflineState,
    PrintSpoolDirectoryState,
    WindowsRecoveryEnvironmentState,
    DnsLookupState,
    DeliveryOptimizationState,
    FileSystemState,
    FirewallProfileState,
    DefaultPrinterState,
    PrintJobState,
    InstallerRegistrationState,
    MsiProductState,
    VolumeOptimizationState,
    SystemRestorePointState,
    ComputerNameState,
    SearchIndexState,
    OrcaCacheState,
    PreviousWindowsInstallationState,
    EventChannelState,
    BitLockerProtectionState,
    BitLockerRecoveryBackupState,
    PowerPlanState,
    HibernationState,
    PowerTimerState,
    UpdateDataStoreState,
    BitsJobState
}

public static class RepairRecoveryCatalog
{
    public static RepairRecoveryContract For(RepairKind kind)
    {
        if (!Enum.IsDefined(kind)) return RepairRecoveryContract.None;
        if (ServiceRecovery.ServiceName(kind) is not null) return RepairRecoveryContract.ExactServiceState;
        return kind switch
        {
            RepairKind.ComponentStore or RepairKind.SystemFiles => RepairRecoveryContract.ServicingHealth,
            RepairKind.ComponentCleanup => RepairRecoveryContract.ComponentCleanupRecommendation,
            RepairKind.DefenderSignatures => RepairRecoveryContract.DefenderSignatureFreshness,
            RepairKind.WmiRepository => RepairRecoveryContract.WmiRepositoryConsistency,
            RepairKind.TimeSynchronization => RepairRecoveryContract.TimeSynchronizationState,
            RepairKind.TimeSourceConfiguration => RepairRecoveryContract.TimeSourceState,
            RepairKind.DhcpLease => RepairRecoveryContract.DhcpAdapterState,
            RepairKind.PrintQueues => RepairRecoveryContract.PausedQueueState,
            RepairKind.PrinterOffline => RepairRecoveryContract.PrinterOfflineState,
            RepairKind.PrintSpoolDirectory => RepairRecoveryContract.PrintSpoolDirectoryState,
            RepairKind.WindowsRecoveryEnvironment => RepairRecoveryContract.WindowsRecoveryEnvironmentState,
            RepairKind.DnsCache => RepairRecoveryContract.DnsLookupState,
            RepairKind.DeliveryOptimization => RepairRecoveryContract.DeliveryOptimizationState,
            RepairKind.StorageFileSystem => RepairRecoveryContract.FileSystemState,
            RepairKind.FirewallProfiles => RepairRecoveryContract.FirewallProfileState,
            RepairKind.DefaultPrinter => RepairRecoveryContract.DefaultPrinterState,
            RepairKind.PrintJobCancellation => RepairRecoveryContract.PrintJobState,
            RepairKind.InstallerRegistration => RepairRecoveryContract.InstallerRegistrationState,
            RepairKind.MsiProduct => RepairRecoveryContract.MsiProductState,
            RepairKind.VolumeOptimization => RepairRecoveryContract.VolumeOptimizationState,
            RepairKind.SystemRestorePoint => RepairRecoveryContract.SystemRestorePointState,
            RepairKind.ComputerRename => RepairRecoveryContract.ComputerNameState,
            RepairKind.SearchIndexRebuild => RepairRecoveryContract.SearchIndexState,
            RepairKind.OrcaCacheReset => RepairRecoveryContract.OrcaCacheState,
            RepairKind.PreviousWindowsInstallationRemoval => RepairRecoveryContract.PreviousWindowsInstallationState,
            RepairKind.EventChannelEnable => RepairRecoveryContract.EventChannelState,
            RepairKind.BitLockerProtectionResume => RepairRecoveryContract.BitLockerProtectionState,
            RepairKind.BitLockerRecoveryBackup => RepairRecoveryContract.BitLockerRecoveryBackupState,
            RepairKind.HighPerformancePowerPlan => RepairRecoveryContract.PowerPlanState,
            RepairKind.HibernationDisable => RepairRecoveryContract.HibernationState,
            RepairKind.PowerTimers => RepairRecoveryContract.PowerTimerState,
            RepairKind.UpdateDataStoreReset => RepairRecoveryContract.UpdateDataStoreState,
            RepairKind.BitsJobRetry => RepairRecoveryContract.BitsJobState,
            _ => RepairRecoveryContract.None
        };
    }
}
