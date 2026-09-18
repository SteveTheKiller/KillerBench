using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private sealed record ToolCategory(string Title, string Description, params WorkflowKind[] Tools)
    {
        public override string ToString() => Title;
    }

    private static readonly ToolCategory[] ToolCategories =
    [
        new("Diagnostics", "Check the machine first, then read the report for what needs attention and the next safe step.", WorkflowKind.CommonProblems, WorkflowKind.Vitals, WorkflowKind.ServicingActivity, WorkflowKind.ReliabilityHistory, WorkflowKind.StartupPerformance, WorkflowKind.HardwareErrors, WorkflowKind.DriverInstallHistory, WorkflowKind.SystemRestore, WorkflowKind.WindowsRecovery, WorkflowKind.WindowsActivation, WorkflowKind.Storage, WorkflowKind.StorageSpaces, WorkflowKind.Services, WorkflowKind.Applications, WorkflowKind.Display, WorkflowKind.Input, WorkflowKind.DeviceProblems),
        new("Repair", "Use this after a diagnosis identifies a problem. Repairs require confirmed evidence and explicit approval.", WorkflowKind.CoreWindowsServices, WorkflowKind.SystemFiles, WorkflowKind.WindowsUpdate, WorkflowKind.Printing, WorkflowKind.Audio, WorkflowKind.Search, WorkflowKind.Wmi, WorkflowKind.Installer, WorkflowKind.StorageService, WorkflowKind.OptimizeDrivesService, WorkflowKind.WindowsImageAcquisitionService, WorkflowKind.FunctionDiscoveryProviderHostService, WorkflowKind.FunctionDiscoveryResourcePublicationService, WorkflowKind.HumanInterfaceDeviceService, WorkflowKind.PortableDeviceEnumeratorService, WorkflowKind.WindowsFontCacheService, WorkflowKind.DeviceAssociationService, WorkflowKind.TextInputManagementService, WorkflowKind.DisplayEnhancementService, WorkflowKind.SystemNotifications, WorkflowKind.AppReadinessService, WorkflowKind.StateRepositoryService, WorkflowKind.AppxDeploymentService, WorkflowKind.ClientLicenseService, WorkflowKind.StoreInstallService, WorkflowKind.WindowsLicenseManagerService, WorkflowKind.PushToInstallService, WorkflowKind.UserProfileService, WorkflowKind.TimeService, WorkflowKind.EventLog, WorkflowKind.BackupServices, WorkflowKind.FileSharing, WorkflowKind.TaskSchedulerService, WorkflowKind.WindowsUpdateService, WorkflowKind.DeliveryOptimizationService, WorkflowKind.BitsService, WorkflowKind.CryptographicService, WorkflowKind.ModulesInstallerService, WorkflowKind.WindowsUpdateMedicService, WorkflowKind.UpdateOrchestratorService, WorkflowKind.DiagnosticPolicyService, WorkflowKind.WindowsErrorReportingService, WorkflowKind.WindowsEventCollectorService, WorkflowKind.RemoteAccessConnectionManagerService, WorkflowKind.RemoteDesktopServices, WorkflowKind.WindowsRemoteManagement, WorkflowKind.NetworkConnectionsService, WorkflowKind.WinHttpAutoProxyService, WorkflowKind.WindowsConnectionManagerService, WorkflowKind.NetworkSetupService, WorkflowKind.ShellHardwareDetectionService, WorkflowKind.DeviceInstallService, WorkflowKind.Wireless, WorkflowKind.WiredAuthentication, WorkflowKind.Bluetooth, WorkflowKind.Camera, WorkflowKind.SmartCard, WorkflowKind.Biometric),
        new("Network", "Inspect connections, DNS, routes and network readiness.", WorkflowKind.Network),
        new("Maintenance", "Review cleanup, updates, profiles, applications and scheduled tasks.", WorkflowKind.Cleanup, WorkflowKind.ComponentCleanup, WorkflowKind.AvailableUpdates, WorkflowKind.Profiles, WorkflowKind.ComputerName, WorkflowKind.Foxit, WorkflowKind.ScheduledTasks, WorkflowKind.AppPackages, WorkflowKind.InstalledApps, WorkflowKind.Mace, WorkflowKind.Orca, WorkflowKind.Depot, WorkflowKind.Beam, WorkflowKind.Debloat, WorkflowKind.StartupApplications, WorkflowKind.BackgroundTransfers),
        new("Security", "Inspect protection, BitLocker and privacy settings.", WorkflowKind.Defender, WorkflowKind.SecurityCenterService, WorkflowKind.CredentialManagerService, WorkflowKind.WebAccountManagerService, WorkflowKind.MicrosoftAccountSignInService, WorkflowKind.FirewallServices, WorkflowKind.Certificates, WorkflowKind.Privacy, WorkflowKind.BitLocker)
    ];

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
        if (synchronizingNavigation || CategoryWorkflow.SelectedItem is not WorkflowDefinition workflow) return;
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
