using System.Windows.Controls;
using KillerBench.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    // Id is the fixed English section name; Title and Description come from the active language.
    private sealed record ToolCategory(string Id, params WorkflowKind[] Tools)
    {
        public string Title => LocalizedText.Get("Str_Category_" + Id);
        public string Description => LocalizedText.Get("Str_Category_" + Id + "_Description");

        public string Icon => ((char)(Id switch
        {
            "Diagnostics" => 0xE9D9, // Diagnostic
            "Repair" => 0xE90F, // Repair
            "Updates" => 0xE895, // Sync
            "Devices" => 0xE772, // Devices
            "Network" => 0xE968, // Network
            "Apps" => 0xE71D, // AllApps
            "Maintenance" => 0xE74D, // Clear
            "Onboarding" => 0xE8FA, // AddFriend
            "Security" => 0xEA18, // Shield
            "BitLocker" => 0xE72E, // Lock
            "Privacy" => 0xE890, // Hide
            "Storage" => 0xEDA2, // HardDrive
            _ => 0xE90F
        })).ToString();

        public override string ToString() => Title;
    }

    private static readonly ToolCategory[] ToolCategories =
    [
        new("Diagnostics", WorkflowKind.CommonProblems, WorkflowKind.Vitals, WorkflowKind.ReliabilityHistory, WorkflowKind.StartupPerformance, WorkflowKind.HardwareErrors, WorkflowKind.DriverInstallHistory, WorkflowKind.SystemRestore, WorkflowKind.WindowsRecovery, WorkflowKind.WindowsActivation, WorkflowKind.Services, WorkflowKind.Applications, WorkflowKind.Display, WorkflowKind.Input, WorkflowKind.DeviceProblems),
        new("Repair", WorkflowKind.CoreWindowsServices, WorkflowKind.SystemFiles, WorkflowKind.Wmi, WorkflowKind.SystemNotifications, WorkflowKind.DiagnosticPolicyService, WorkflowKind.WindowsErrorReportingService, WorkflowKind.WindowsEventCollectorService, WorkflowKind.EventLog, WorkflowKind.TimeService, WorkflowKind.TaskSchedulerService, WorkflowKind.UserProfileService, WorkflowKind.BackupServices),
        new("Updates", WorkflowKind.ServicingActivity, WorkflowKind.AvailableUpdates, WorkflowKind.WindowsUpdate, WorkflowKind.WindowsUpdateService, WorkflowKind.DeliveryOptimizationService, WorkflowKind.BitsService, WorkflowKind.BackgroundTransfers, WorkflowKind.CryptographicService, WorkflowKind.ModulesInstallerService, WorkflowKind.WindowsUpdateMedicService, WorkflowKind.UpdateOrchestratorService, WorkflowKind.ComponentCleanup),
        new("Devices", WorkflowKind.Printing, WorkflowKind.Audio, WorkflowKind.Camera, WorkflowKind.Bluetooth, WorkflowKind.SmartCard, WorkflowKind.Biometric, WorkflowKind.HumanInterfaceDeviceService, WorkflowKind.PortableDeviceEnumeratorService, WorkflowKind.WindowsImageAcquisitionService, WorkflowKind.ShellHardwareDetectionService, WorkflowKind.DeviceInstallService, WorkflowKind.DeviceAssociationService, WorkflowKind.DisplayEnhancementService, WorkflowKind.TextInputManagementService, WorkflowKind.WindowsFontCacheService, WorkflowKind.FunctionDiscoveryProviderHostService, WorkflowKind.FunctionDiscoveryResourcePublicationService),
        new("Network", WorkflowKind.Network, WorkflowKind.Wireless, WorkflowKind.WiredAuthentication, WorkflowKind.NetworkConnectionsService, WorkflowKind.WinHttpAutoProxyService, WorkflowKind.WindowsConnectionManagerService, WorkflowKind.NetworkSetupService, WorkflowKind.FileSharing, WorkflowKind.RemoteAccessConnectionManagerService, WorkflowKind.RemoteDesktopServices, WorkflowKind.WindowsRemoteManagement),
        new("Apps", WorkflowKind.InstalledApps, WorkflowKind.AppPackages, WorkflowKind.BrowserExtensions, WorkflowKind.StartupApplications, WorkflowKind.Mace, WorkflowKind.Orca, WorkflowKind.Foxit, WorkflowKind.Installer, WorkflowKind.Search, WorkflowKind.AppReadinessService, WorkflowKind.StateRepositoryService, WorkflowKind.AppxDeploymentService, WorkflowKind.ClientLicenseService, WorkflowKind.StoreInstallService, WorkflowKind.WindowsLicenseManagerService, WorkflowKind.PushToInstallService),
        new("Maintenance", WorkflowKind.Cleanup, WorkflowKind.Profiles, WorkflowKind.ScheduledTasks, WorkflowKind.Debloat),
        new("Onboarding", WorkflowKind.Depot, WorkflowKind.ComputerName),
        new("Security", WorkflowKind.Defender, WorkflowKind.SecurityCenterService, WorkflowKind.FirewallServices, WorkflowKind.Certificates, WorkflowKind.CredentialManagerService, WorkflowKind.WebAccountManagerService, WorkflowKind.MicrosoftAccountSignInService),
        new("BitLocker", WorkflowKind.BitLocker),
        new("Privacy", WorkflowKind.Privacy),
        new("Storage", WorkflowKind.Storage, WorkflowKind.StorageSpaces, WorkflowKind.StorageService, WorkflowKind.OptimizeDrivesService)
    ];

    // Report handoffs keep their original meaning even though tools now live in smaller sections.
    private static readonly WorkflowKind[] RepairWorkflows = [WorkflowKind.CoreWindowsServices, WorkflowKind.SystemFiles, WorkflowKind.WindowsUpdate, WorkflowKind.Printing, WorkflowKind.Audio, WorkflowKind.Search, WorkflowKind.Wmi, WorkflowKind.Installer, WorkflowKind.StorageService, WorkflowKind.OptimizeDrivesService, WorkflowKind.WindowsImageAcquisitionService, WorkflowKind.FunctionDiscoveryProviderHostService, WorkflowKind.FunctionDiscoveryResourcePublicationService, WorkflowKind.HumanInterfaceDeviceService, WorkflowKind.PortableDeviceEnumeratorService, WorkflowKind.WindowsFontCacheService, WorkflowKind.DeviceAssociationService, WorkflowKind.TextInputManagementService, WorkflowKind.DisplayEnhancementService, WorkflowKind.SystemNotifications, WorkflowKind.AppReadinessService, WorkflowKind.StateRepositoryService, WorkflowKind.AppxDeploymentService, WorkflowKind.ClientLicenseService, WorkflowKind.StoreInstallService, WorkflowKind.WindowsLicenseManagerService, WorkflowKind.PushToInstallService, WorkflowKind.UserProfileService, WorkflowKind.TimeService, WorkflowKind.EventLog, WorkflowKind.BackupServices, WorkflowKind.FileSharing, WorkflowKind.TaskSchedulerService, WorkflowKind.WindowsUpdateService, WorkflowKind.DeliveryOptimizationService, WorkflowKind.BitsService, WorkflowKind.CryptographicService, WorkflowKind.ModulesInstallerService, WorkflowKind.WindowsUpdateMedicService, WorkflowKind.UpdateOrchestratorService, WorkflowKind.DiagnosticPolicyService, WorkflowKind.WindowsErrorReportingService, WorkflowKind.WindowsEventCollectorService, WorkflowKind.RemoteAccessConnectionManagerService, WorkflowKind.RemoteDesktopServices, WorkflowKind.WindowsRemoteManagement, WorkflowKind.NetworkConnectionsService, WorkflowKind.WinHttpAutoProxyService, WorkflowKind.WindowsConnectionManagerService, WorkflowKind.NetworkSetupService, WorkflowKind.ShellHardwareDetectionService, WorkflowKind.DeviceInstallService, WorkflowKind.Wireless, WorkflowKind.WiredAuthentication, WorkflowKind.Bluetooth, WorkflowKind.Camera, WorkflowKind.SmartCard, WorkflowKind.Biometric];
    private static readonly WorkflowKind[] MaintenanceWorkflows = [WorkflowKind.Cleanup, WorkflowKind.ComponentCleanup, WorkflowKind.AvailableUpdates, WorkflowKind.Profiles, WorkflowKind.ComputerName, WorkflowKind.Foxit, WorkflowKind.ScheduledTasks, WorkflowKind.AppPackages, WorkflowKind.InstalledApps, WorkflowKind.BrowserExtensions, WorkflowKind.Mace, WorkflowKind.Orca, WorkflowKind.Depot, WorkflowKind.Debloat, WorkflowKind.StartupApplications, WorkflowKind.BackgroundTransfers];
    private static readonly WorkflowKind[] SecurityWorkflows = [WorkflowKind.Defender, WorkflowKind.SecurityCenterService, WorkflowKind.CredentialManagerService, WorkflowKind.WebAccountManagerService, WorkflowKind.MicrosoftAccountSignInService, WorkflowKind.FirewallServices, WorkflowKind.Certificates, WorkflowKind.Privacy, WorkflowKind.BitLocker];

    private static ToolCategory CategoryOf(WorkflowKind kind) => ToolCategories.Single(category => category.Tools.Contains(kind));

    private bool synchronizingNavigation;

    private void InitializeNavigation()
    {
        CategoryList.ItemsSource = ToolCategories;
        CategoryList.SelectedIndex = 0;
    }

    private void OnCategorySelected(object sender, SelectionChangedEventArgs e)
    {
        if (synchronizingNavigation || CategoryList.SelectedItem is not ToolCategory category) return;
        MainAutoRepair.IsChecked = false;
        MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(w => w.Kind == category.Tools[0]);
        SynchronizeNavigation();
    }

    private void OnCategoryWorkflowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (synchronizingNavigation || refreshingLanguage || CategoryWorkflow.SelectedItem is not WorkflowDefinition workflow) return;
        MainAutoRepair.IsChecked = false;
        MainWorkflow.SelectedItem = workflow;
    }

    private void SynchronizeNavigation()
    {
        if (CategoryWorkflow is null || CategoryList.ItemsSource is null || MainWorkflow.SelectedItem is not WorkflowDefinition workflow) return;
        synchronizingNavigation = true;
        try
        {
            var category = ToolCategories.Single(c => c.Tools.Contains(workflow.Kind));
            CategoryList.SelectedItem = category;
            CategoryTitle.Text = category.Title;
            CategoryDescription.Text = category.Description;
            CategoryWorkflow.ItemsSource = category.Tools.Select(kind => WorkflowCatalog.All.Single(w => w.Kind == kind)).ToArray();
            CategoryWorkflow.SelectedItem = workflow;
        }
        finally { synchronizingNavigation = false; }
    }
}
