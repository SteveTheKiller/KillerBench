using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{

    private static readonly IReadOnlyDictionary<RepairKind, string> AdvancedServiceNames = new Dictionary<RepairKind, string>
    {
        [RepairKind.PrintSpooler] = "Print Spooler",
        [RepairKind.WindowsInstaller] = "Str_Workflow_Installer",
        [RepairKind.WindowsManagement] = "Windows Management Instrumentation",
        [RepairKind.DhcpClient] = "DHCP Client",
        [RepairKind.DnsClient] = "DNS Client",
        [RepairKind.IpHelper] = "IP Helper",
        [RepairKind.VolumeShadowCopy] = "Volume Shadow Copy",
        [RepairKind.SoftwareShadowProvider] = "Software Shadow Copy Provider",
        [RepairKind.WindowsBackupService] = "Windows Backup",
        [RepairKind.BlockLevelBackupEngineService] = "Block Level Backup Engine",
        [RepairKind.UpdateService] = "Str_Workflow_WindowsUpdate",
        [RepairKind.CryptographicServices] = "Str_Workflow_CryptographicService",
        [RepairKind.ModulesInstaller] = "Str_Workflow_ModulesInstallerService",
        [RepairKind.WindowsUpdateMedicService] = "Str_Workflow_WindowsUpdateMedicService",
        [RepairKind.UpdateOrchestrator] = "Str_Workflow_UpdateOrchestratorService",
        [RepairKind.WindowsSearch] = "Str_Workflow_Search",
        [RepairKind.WindowsEventLog] = "Str_Workflow_EventLog",
        [RepairKind.BaseFilteringEngine] = "Base Filtering Engine",
        [RepairKind.WindowsFirewall] = "Windows Defender Firewall",
        [RepairKind.WirelessAutoConfig] = "WLAN AutoConfig",
        [RepairKind.WiredAutoConfig] = "Wired AutoConfig",
        [RepairKind.BluetoothSupport] = "Bluetooth Support Service",
        [RepairKind.CameraFrameServer] = "Windows Camera Frame Server",
        [RepairKind.SmartCardService] = "Smart Card service",
        [RepairKind.BiometricService] = "Windows Biometric Service",
        [RepairKind.ServerService] = "Windows SMB server",
        [RepairKind.TaskSchedulerService] = "Str_Workflow_TaskSchedulerService",
        [RepairKind.DiagnosticPolicyService] = "Str_Workflow_DiagnosticPolicyService",
        [RepairKind.WindowsErrorReportingService] = "Str_Workflow_WindowsErrorReportingService",
        [RepairKind.WindowsEventCollectorService] = "Str_Workflow_WindowsEventCollectorService",
        [RepairKind.RemoteAccessConnectionManagerService] = "Str_Workflow_RemoteAccessConnectionManagerService",
        [RepairKind.RemoteDesktopServices] = "Str_Workflow_RemoteDesktopServices",
        [RepairKind.WindowsRemoteManagement] = "Str_Workflow_WindowsRemoteManagement",
        [RepairKind.SecurityCenterService] = "Str_Workflow_SecurityCenterService",
        [RepairKind.NetworkConnectionsService] = "Str_Workflow_NetworkConnectionsService",
        [RepairKind.WinHttpAutoProxyService] = "Str_Workflow_WinHttpAutoProxyService",
        [RepairKind.WindowsConnectionManagerService] = "Str_Workflow_WindowsConnectionManagerService",
        [RepairKind.NetworkSetupService] = "Str_Workflow_NetworkSetupService",
        [RepairKind.AppReadinessService] = "Str_Workflow_AppReadinessService",
        [RepairKind.StateRepositoryService] = "Str_Workflow_StateRepositoryService",
        [RepairKind.UserProfileService] = "Str_Workflow_UserProfileService",
        [RepairKind.CredentialManagerService] = "Str_Workflow_CredentialManagerService",
        [RepairKind.ShellHardwareDetectionService] = "Str_Workflow_ShellHardwareDetectionService",
        [RepairKind.DeviceInstallService] = "Str_Workflow_DeviceInstallService",
        [RepairKind.AppxDeploymentService] = "Str_Workflow_AppxDeploymentService",
        [RepairKind.ClientLicenseService] = "Str_Workflow_ClientLicenseService",
        [RepairKind.StoreInstallService] = "Str_Workflow_StoreInstallService",
        [RepairKind.WindowsLicenseManagerService] = "Str_Workflow_WindowsLicenseManagerService",
        [RepairKind.PushToInstallService] = "Str_Workflow_PushToInstallService",
        [RepairKind.WebAccountManagerService] = "Str_Workflow_WebAccountManagerService",
        [RepairKind.MicrosoftAccountSignInService] = "Str_Workflow_MicrosoftAccountSignInService",
        [RepairKind.StorageService] = "Str_Workflow_StorageService",
        [RepairKind.OptimizeDrivesService] = "Str_Workflow_OptimizeDrivesService",
        [RepairKind.WindowsImageAcquisitionService] = "Str_Workflow_WindowsImageAcquisitionService",
        [RepairKind.FunctionDiscoveryProviderHostService] = "Str_Workflow_FunctionDiscoveryProviderHostService",
        [RepairKind.FunctionDiscoveryResourcePublicationService] = "Str_Workflow_FunctionDiscoveryResourcePublicationService",
        [RepairKind.HumanInterfaceDeviceService] = "Str_Workflow_HumanInterfaceDeviceService",
        [RepairKind.PortableDeviceEnumeratorService] = "Str_Workflow_PortableDeviceEnumeratorService",
        [RepairKind.WindowsFontCacheService] = "Str_Workflow_WindowsFontCacheService",
        [RepairKind.DeviceAssociationService] = "Str_Workflow_DeviceAssociationService",
        [RepairKind.TextInputManagementService] = "Str_Workflow_TextInputManagementService",
        [RepairKind.DisplayEnhancementService] = "Str_Workflow_DisplayEnhancementService",
        [RepairKind.ComPlusEventSystem] = "COM+ Event System",
        [RepairKind.SystemEventNotificationService] = "System Event Notification Service",
        [RepairKind.PowerService] = "Power service",
        [RepairKind.PlugAndPlayService] = "Plug and Play service",
        [RepairKind.UserManagerService] = "User Manager service",
        [RepairKind.GroupPolicyClientService] = "Group Policy Client",
        [RepairKind.NetworkStoreInterfaceService] = "Network Store Interface",
        [RepairKind.RpcEndpointMapperService] = "RPC Endpoint Mapper",
        [RepairKind.RemoteProcedureCallService] = "Remote Procedure Call",
        [RepairKind.DcomServerProcessLauncherService] = "DCOM Server Process Launcher",
        [RepairKind.BackgroundTasksInfrastructureService] = "Background Tasks Infrastructure",
        [RepairKind.CoreMessagingService] = "CoreMessaging",
    };

    private static string AdvancedAreaTitle(DiagnosticArea area) => Controls.LocalizedText.Get(area.Scope switch
    {
        CommonDiagnosticScope.ScheduledTasks => "Str_Workflow_ScheduledTasks",
        CommonDiagnosticScope.ReliabilityHistory => "Str_Workflow_ReliabilityHistory",
        CommonDiagnosticScope.HardwareErrorHistory => "Str_Workflow_HardwareErrors",
        CommonDiagnosticScope.DriverInstallHistory => "Str_Workflow_DriverInstallHistory",
        CommonDiagnosticScope.SystemRestore => "Str_Workflow_SystemRestore",
        CommonDiagnosticScope.WindowsRecovery => "Str_Workflow_WindowsRecovery",
        CommonDiagnosticScope.Certificates => "Str_Workflow_Certificates",
        CommonDiagnosticScope.BackgroundTransfers => "Str_Workflow_BackgroundTransfers",
        _ => "Str_Advanced_Catalog_Area" + area.Scope
    });

    private static string AdvancedRecipeTitle(RepairRecipe recipe)
    {
        if (AdvancedServiceNames.TryGetValue(recipe.Action, out string? service))
            return Controls.LocalizedText.Format("Str_Advanced_Catalog_Availability",
                service.StartsWith("Str_", StringComparison.Ordinal) ? Controls.LocalizedText.Get(service) : service);
        return Controls.LocalizedText.Get(recipe.Action switch
        {
            RepairKind.ComponentStore => "Str_Repair_ComponentStore",
            RepairKind.SearchIndexRebuild => "Str_Search_Rebuild",
            RepairKind.HighPerformancePowerPlan => "Str_Debloat_ActionHighPerformance",
            RepairKind.HibernationDisable => "Str_Debloat_ActionDisableHibernation",
            RepairKind.WindowsRecoveryEnvironment => "Str_Workflow_WindowsRecovery",
            RepairKind.BackgroundTransfer => "Str_Workflow_BitsService",
            _ => "Str_Advanced_Catalog_Recipe" + recipe.Action
        });
    }

    private static string AdvancedGroupTitle(string area) => area switch
    {
        "Applications" => Controls.LocalizedText.Get("Str_Common_Applications"),
        "Audio" => Controls.LocalizedText.Get("Str_Workflow_Audio"),
        "Devices" => Controls.LocalizedText.Get("Str_Category_Devices"),
        "Network" => Controls.LocalizedText.Get("Str_Category_Network"),
        "Printing" => Controls.LocalizedText.Get("Str_Workflow_Printing"),
        "Security" => Controls.LocalizedText.Get("Str_Category_Security"),
        "Windows services" => Controls.LocalizedText.Get("Str_Workflow_Services"),
        "Storage" => Controls.LocalizedText.Get("Str_Category_Storage"),
        "Windows Update" => Controls.LocalizedText.Get("Str_Workflow_WindowsUpdate"),
        "Windows" => "Windows",
        "Backup" => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupBackup"),
        "Installation" => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupInstallation"),
        "Power" => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupPower"),
        "Remote access" => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupRemoteAccess"),
        "Time" => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupTime"),
        "Windows management" => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupWindowsManagement"),
        _ => Controls.LocalizedText.Get("Str_Advanced_Catalog_GroupOther")
    };

    private enum AdvancedCatalogCaption { AreaTitle, AreaReference, RecipeTitle, RecipeReference, GroupTitle }

    private sealed class AdvancedCatalogConverter(AdvancedCatalogCaption caption) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => caption switch
            {
                AdvancedCatalogCaption.AreaTitle when value is DiagnosticArea area => AdvancedAreaTitle(area),
                AdvancedCatalogCaption.AreaReference when value is DiagnosticArea area =>
                    Controls.LocalizedText.Get("Str_Advanced_Catalog_SourceReference") + "\n\n" + area.Description,
                AdvancedCatalogCaption.RecipeTitle when value is RepairRecipe recipe => AdvancedRecipeTitle(recipe),
                AdvancedCatalogCaption.RecipeReference when value is RepairRecipe recipe =>
                    Controls.LocalizedText.Get("Str_Advanced_Catalog_SourceReference") + "\n\n" + recipe.Effects,
                AdvancedCatalogCaption.GroupTitle => AdvancedGroupTitle(value?.ToString() ?? ""),
                _ => Binding.DoNothing
            };

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => Binding.DoNothing;
    }

    private static DataTemplate AdvancedChecklistTemplate(bool recipe)
    {
        var row = new FrameworkElementFactory(typeof(System.Windows.Controls.CheckBox));
        row.SetBinding(System.Windows.Controls.ContentControl.ContentProperty, new Binding
        {
            Converter = new AdvancedCatalogConverter(recipe ? AdvancedCatalogCaption.RecipeTitle : AdvancedCatalogCaption.AreaTitle)
        });
        row.SetBinding(FrameworkElement.ToolTipProperty, new Binding
        {
            Converter = new AdvancedCatalogConverter(recipe ? AdvancedCatalogCaption.RecipeReference : AdvancedCatalogCaption.AreaReference)
        });
        row.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding("IsSelected")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.ListBoxItem), 1),
            Mode = BindingMode.TwoWay
        });
        return new DataTemplate { VisualTree = row };
    }

    private void ConfigureAdvancedChooserLabels()
    {
        DiagnosticAreaList.ItemTemplate = AdvancedChecklistTemplate(false);
        RecipeSelectionList.ItemTemplate = AdvancedChecklistTemplate(true);
        if (RecipeSelectionList.GroupStyle.Count == 0) return;
        var label = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
        label.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
            new Binding("Name") { Converter = new AdvancedCatalogConverter(AdvancedCatalogCaption.GroupTitle) });
        label.SetResourceReference(System.Windows.Controls.TextBlock.FontFamilyProperty, "UiFont");
        label.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "PrimaryBrush");
        label.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 11.0);
        label.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 7, 2, 3));
        RecipeSelectionList.GroupStyle[0].HeaderTemplate = new DataTemplate { VisualTree = label };
    }
    private void OnDiagnosticAreasChanged(object sender, RoutedEventArgs e)
    {
        if (DiagnosticAreaList is null || DiagnosticAreaSummary is null || FocusDiagnosticAreas is null || MainAutoRepair is null) return;
        bool focused = FocusDiagnosticAreas.IsChecked == true;
        DiagnosticAreaSummary.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticAreaList.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
        ShowDetailedDiagnosticAreas?.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
        if (focused && DiagnosticAreaList.ItemsSource is null)
            SetDiagnosticAreaChoices(false);
        if (focused && ReferenceEquals(sender, FocusDiagnosticAreas) && DiagnosticAreaList.SelectedItems.Count == 0)
            DiagnosticAreaList.SelectAll();
        if (focused) CustomizeRecipes?.IsChecked = false;
        MainAutoRepair.IsChecked = false;
        DiagnosticAreaSummary.Text = focused
            ? Controls.LocalizedText.Format("Str_Advanced_AreasSelected", DiagnosticAreaList.SelectedItems.Count)
            : Controls.LocalizedText.Get("Str_Advanced_FullSelected");
        RecipeSelectionList?.Visibility = focused ? Visibility.Collapsed : Visibility.Visible;
        CustomizeRecipes?.IsEnabled = !focused;
        OnWorkflowOptionsChanged(this, null);
    }

    private void OnDiagnosticDetailChanged(object sender, RoutedEventArgs e)
    {
        if (DiagnosticAreaList is null || ShowDetailedDiagnosticAreas is null) return;
        SetDiagnosticAreaChoices(ShowDetailedDiagnosticAreas.IsChecked == true);
        OnDiagnosticAreasChanged(sender, e);
    }

    private void SetDiagnosticAreaChoices(bool detailed)
    {
        DiagnosticAreaList.ItemsSource = detailed ? CommonDiagnosticCatalog.All : CommonDiagnosticCatalog.SummaryAreas;
        DiagnosticAreaList.SelectAll();
    }
    private void RefreshRecipeSelection()
    {
        if (RecipeSelectionPanel is null) return;
        bool common = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.CommonProblems };
        RecipeSelectionPanel.Visibility = common ? Visibility.Visible : Visibility.Collapsed;
        if (RecipeSelectionList.ItemsSource is null)
        {
            var recipes = RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).ToList();
            var view = new ListCollectionView(recipes);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RepairRecipe.Area)));
            RecipeSelectionList.ItemsSource = view;
            RecipeSelectionList.SelectAll();
        }
        OnRecipeSelectionChanged(this, new RoutedEventArgs());
    }

    private void OnRecipeSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (RecipeSelectionSummary is null || RecipeSelectionList is null || CustomizeRecipes is null) return;
        bool showRecipes = CustomizeRecipes.IsChecked == true && FocusDiagnosticAreas?.IsChecked != true;
        RecipeSelectionList.Visibility = showRecipes ? Visibility.Visible : Visibility.Collapsed;
        RecipeActions.Visibility = RecipeSelectionList.Visibility;
        MainAutoRepair?.IsChecked = false;
        RefreshRecipeSelectionText();
    }

    private void RefreshRecipeSelectionText()
    {
        if (RecipeSelectionSummary is null || RecipeSelectionList is null || CustomizeRecipes is null) return;
        ConfigureAdvancedChooserLabels();
        if (FocusDiagnosticAreas?.IsChecked == true)
        {
            RecipeSelectionSummary.Text = Controls.LocalizedText.Get("Str_Advanced_RecipesExcluded");
            RecipeEffectsSummary?.Text = Controls.LocalizedText.Get("Str_Advanced_NoRepairs");
            return;
        }
        var ids = RecipeSelectionList.SelectedItems.Cast<RepairRecipe>().Select(recipe => recipe.Id).ToArray();
        var plan = RecipeCatalog.SelectForWorkflow(WorkflowKind.CommonProblems, ids);
        RecipeSelectionSummary.Text = CustomizeRecipes.IsChecked != true
            ? Controls.LocalizedText.Format("Str_Advanced_AllChecksSelected", CommonDiagnosticCatalog.All.Count,
                RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Count)
            : ids.Length == 0 ? Controls.LocalizedText.Get("Str_Advanced_NoRecipes")
            : Controls.LocalizedText.Format("Str_Advanced_RecipesSelected", ids.Length, plan.Count);
        if (RecipeEffectsSummary is not null)
        {
            var reviewed = CustomizeRecipes.IsChecked == true ? plan : RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems);
            RecipeEffectsSummary.Text = reviewed.Count == 0 ? Controls.LocalizedText.Get("Str_Advanced_NoRecipes") :
                Controls.LocalizedText.Get("Str_Advanced_ChecksOnly") + "\n\n" +
                Controls.LocalizedText.Get("Str_Advanced_Catalog_SourceReference") + "\n\n" +
                string.Join("\n\n", reviewed.Select(recipe => AdvancedRecipeTitle(recipe) + "\n" + recipe.Effects + "\n" +
                    Controls.LocalizedText.Format("Str_Common_Verifies", recipe.VerificationScope) +
                    "\n" + NativeCapabilityLabel(recipe.Id)));
        }
    }

    private void OnSelectAllRecipes(object sender, RoutedEventArgs e)
    {
        RecipeSelectionList?.SelectAll();
        OnRecipeSelectionChanged(sender, e);
    }

    private void OnClearRecipes(object sender, RoutedEventArgs e)
    {
        RecipeSelectionList?.UnselectAll();
        OnRecipeSelectionChanged(sender, e);
    }
}
