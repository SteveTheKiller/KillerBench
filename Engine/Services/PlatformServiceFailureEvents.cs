namespace KillerBench.Engine.Services;

public enum PlatformServiceFailureArea
{
    AppReadiness,
    StateRepository,
    UserProfile,
    CredentialManager,
    ShellHardwareDetection,
    DeviceInstall,
    AppxDeployment,
    ClientLicense,
    StoreInstall,
    WindowsLicenseManager,
    PushToInstall,
    WebAccountManager,
    MicrosoftAccountSignIn,
    OptimizeDrives,
    WindowsImageAcquisition,
    FunctionDiscoveryProviderHost,
    FunctionDiscoveryResourcePublication,
    HumanInterfaceDevice,
    PortableDeviceEnumerator,
    WindowsFontCache,
    DeviceAssociation,
    TextInputManagement,
    DisplayEnhancement
}

public static class PlatformServiceFailureEvents
{
    private static readonly Dictionary<PlatformServiceFailureArea, ExactServiceFailureEventDefinition> Definitions =
        new Dictionary<PlatformServiceFailureArea, ExactServiceFailureEventDefinition>
        {
            [PlatformServiceFailureArea.AppReadiness] = Definition("AppReadiness", "App Readiness", "App Readiness service failure history", "first sign-in provisioning or application readiness"),
            [PlatformServiceFailureArea.StateRepository] = Definition("StateRepository", "State Repository Service", "State Repository service failure history", "repository consistency or application operation"),
            [PlatformServiceFailureArea.UserProfile] = Definition("ProfSvc", "User Profile Service", "User Profile service failure history", "profile integrity, sign-in success or user data availability"),
            [PlatformServiceFailureArea.CredentialManager] = Definition("VaultSvc", "Credential Manager", "Credential Manager service failure history", "credential availability, authentication success or vault integrity"),
            [PlatformServiceFailureArea.ShellHardwareDetection] = Definition("ShellHWDetection", "Shell Hardware Detection", "Shell Hardware Detection service failure history", "AutoPlay behavior, media readiness or device operation"),
            [PlatformServiceFailureArea.DeviceInstall] = Definition("DeviceInstall", "Device Install Service", "Device Install service failure history", "driver suitability, installation success or device operation"),
            [PlatformServiceFailureArea.AppxDeployment] = Definition("AppXSvc", "AppX Deployment Service", "AppX Deployment service failure history", "package registration, deployment success or application launch"),
            [PlatformServiceFailureArea.ClientLicense] = Definition("ClipSVC", "Client License Service", "Client License service failure history", "license validity, account state or application entitlement"),
            [PlatformServiceFailureArea.StoreInstall] = Definition("InstallService", "Microsoft Store Install Service", "Store Install service failure history", "download, installation, license or application outcomes"),
            [PlatformServiceFailureArea.WindowsLicenseManager] = Definition("LicenseManager", "Windows License Manager Service", "Windows License Manager failure history", "license validity, activation state or application entitlement"),
            [PlatformServiceFailureArea.PushToInstall] = Definition("PushToInstall", "Windows PushToInstall Service", "PushToInstall service failure history", "request processing, download, installation or application outcomes"),
            [PlatformServiceFailureArea.WebAccountManager] = Definition("TokenBroker", "Web Account Manager", "Web Account Manager service failure history", "account validity, token availability or authentication success"),
            [PlatformServiceFailureArea.MicrosoftAccountSignIn] = Definition("wlidsvc", "Microsoft Account Sign-in Assistant", "Microsoft Account Sign-in service failure history", "account validity, token availability or sign-in success"),
            [PlatformServiceFailureArea.OptimizeDrives] = Definition("defragsvc", "Optimize Drives", "Optimize Drives service failure history", "task execution, volume analysis, trim, defragmentation, optimization or storage health"),
            [PlatformServiceFailureArea.WindowsImageAcquisition] = Definition("stisvc", "Windows Image Acquisition", "Windows Image Acquisition service failure history", "scanner or camera availability, acquisition success or device operation"),
            [PlatformServiceFailureArea.FunctionDiscoveryProviderHost] = Definition("fdPHost", "Function Discovery Provider Host", "Function Discovery Provider Host failure history", "device discovery, publication or network reachability"),
            [PlatformServiceFailureArea.FunctionDiscoveryResourcePublication] = Definition("FDResPub", "Function Discovery Resource Publication", "Function Discovery Resource Publication failure history", "resource publication, discovery or network reachability"),
            [PlatformServiceFailureArea.HumanInterfaceDevice] = Definition("hidserv", "Human Interface Device Service", "Human Interface Device service failure history", "input availability, hotkey behavior or device operation"),
            [PlatformServiceFailureArea.PortableDeviceEnumerator] = Definition("WPDBusEnum", "Portable Device Enumerator Service", "Portable Device Enumerator failure history", "portable-device discovery, storage access or synchronization"),
            [PlatformServiceFailureArea.WindowsFontCache] = Definition("FontCache", "Windows Font Cache Service", "Windows Font Cache service failure history", "font rendering, cache consistency or application display"),
            [PlatformServiceFailureArea.DeviceAssociation] = Definition("DeviceAssociationService", "Device Association Service", "Device Association service failure history", "pairing, association or device operation"),
            [PlatformServiceFailureArea.TextInputManagement] = Definition("TextInputManagementService", "Text Input Management Service", "Text Input Management service failure history", "keyboard, touch, handwriting or text-input operation"),
            [PlatformServiceFailureArea.DisplayEnhancement] = Definition("DisplayEnhancementService", "Display Enhancement Service", "Display Enhancement service failure history", "brightness, color enhancement or display operation")
        };

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(PlatformServiceFailureArea area,
        CancellationToken token = default, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        if (!Enum.IsDefined(area)) throw new ArgumentOutOfRangeException(nameof(area));
        token.ThrowIfCancellationRequested();
        return await ExactServiceFailureEvents.CheckManyAsync([Definitions[area]], token, execute).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAllAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        return await ExactServiceFailureEvents.CheckManyAsync(
            [.. Enum.GetValues<PlatformServiceFailureArea>().Select(area => Definitions[area])], token, execute)
            .ConfigureAwait(false);
    }

    private static ExactServiceFailureEventDefinition Definition(string service, string display, string check, string excludedClaim) =>
        new(service, display, check,
            $" Records are historical observations tied to the exact local {display} identity. They do not establish a current outage, prove {excludedClaim} or authorize a service start, restart, startup change, package operation, data reset, policy change or repair.");
}
