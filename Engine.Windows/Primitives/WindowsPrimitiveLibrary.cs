using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>The whitelisted primitives Windows packs may reference. Every registered primitive is used by at least one built-in pack.</summary>
public static class WindowsPrimitiveLibrary
{
    public const string Platform = "Windows";

    public static PrimitiveLibrary Create() => new PrimitiveLibrary(Platform)
        .AddProbe(new WindowsServiceCheckProbe())
        .AddProbe(new WindowsServiceRunningProbe())
        .AddProbe(new WindowsRegistryValueProbe())
        .AddProbe(new WindowsRegistrySubkeysProbe())
        .AddProbe(new WindowsBcdSafebootProbe())
        .AddProbe(new WindowsBcdBootSequenceProbe())
        .AddProbe(new WindowsProcessRunningProbe())
        .AddProbe(new WindowsTopCpuProbe())
        .AddProbe(new WindowsTopMemoryProbe())
        .AddProbe(new WindowsDiskBusyProbe())
        .AddProbe(new WindowsVssWritersProbe())
        .AddProbe(new WindowsShadowStorageProbe())
        .AddProbe(new WindowsSecureChannelProbe())
        .AddProbe(new WindowsEntraStatusProbe())
        .AddProbe(new WindowsGroupPolicyAgeProbe())
        .AddProbe(new WindowsDisabledAdaptersProbe())
        .AddProbe(new WindowsAdapterAdminStatusProbe())
        .AddProbe(new WindowsAdapterPowerSavingProbe())
        .AddProbe(new WindowsFileCountProbe())
        .AddProbe(new WindowsInterfaceMtuProbe())
        .AddProbe(new WindowsWakeTimersProbe())
        .AddProbe(new WindowsProblemDevicesProbe())
        .AddProbe(new WindowsDeviceStatusProbe())
        .AddProbe(new WindowsUnletteredVolumesProbe())
        .AddProbe(new WindowsAppxStatusProbe())
        .AddAction(new WindowsServiceStartAction())
        .AddAction(new WindowsRegistrySetDwordAction())
        .AddAction(new WindowsBcdSafebootClearAction())
        .AddAction(new WindowsMemoryDiagnosticScheduleAction())
        .AddAction(new WindowsExplorerStartAction())
        .AddAction(new WindowsExplorerRestartAction())
        .AddAction(new WindowsAdapterEnableAction())
        .AddAction(new WindowsAdapterPowerSavingDisableAction())
        .AddAction(new WindowsRetainFolderFilesAction())
        .AddAction(new WindowsSpoolerClearAction())
        .AddAction(new WindowsInterfaceMtuSetAction())
        .AddAction(new WindowsDeviceRestartAction())
        .AddAction(new WindowsAssignDriveLetterAction())
        .AddAction(new WindowsAppxReregisterAction());
}
