using System.IO;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private bool workflowOutputActive;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextEvidence, WorkflowResult> workflowSummaries = new();

    private void RegisterWorkflowResult(WorkflowResult result, string? reportTitle = null)
    {
        string summary = WorkflowReport.Format(result);
        AppendWorkflowOutput("Workflow completed: " + result.Status + ". Read the Report view for findings and next actions.");
        var text = new TextEvidence("Workflow " + DateTime.Now.ToString("HHmmss"), EvidenceBox.Text.Replace("\r\n", "\n").Split('\n'), false);
        reports.Add(text, Investigation.Analyze(text) with { Limitation = "Live workflow results. See the workflow journal for decisions and repair verification." });
        workflowSummaries.Add(text, result);
        workflowOutputActive = false;
        RegisterSource(text);
        RepairPreview.Text = summary;
        RenderNamedWorkflowReport(result, reportTitle);
        SetOutputView(report: true);
    }

    private void RegisterGuardedActionReport(WorkflowKind kind, string reportTitle, string actionName, string output,
        string detail, string journal, bool verified, string verifiedNextAction)
    {
        AppendWorkflowOutput(output);
        var action = new WorkflowAction(actionName, verified ? "Verified" : "Review required", detail,
            verified ? verifiedNextAction : "Review the local change journal and current state before retrying.", verified);
        RegisterWorkflowResult(new WorkflowResult(kind, Environment.MachineName, DateTimeOffset.UtcNow,
            verified ? "Guarded action verified" : "Guarded action requires review",
            [new WorkflowCheck("Guarded action outcome", verified ? CheckState.Information : CheckState.Unknown, detail)], [], journal)
        { Actions = [action] }, reportTitle);
    }
    private void OnTimePeersChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (TimeApply is not null) TimeApply.IsChecked = false;
    }

    private void OnPrivacyClipboardHistoryChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PrivacyClipboardHistoryApply is not null) PrivacyClipboardHistoryApply.IsChecked = false;
    }

    private void OnPrivacyBooleanSettingChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PrivacyBooleanApply is not null) PrivacyBooleanApply.IsChecked = false;
    }

    private void OnFirewallProfileSelectionChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (FirewallProfileApply is not null) FirewallProfileApply.IsChecked = false;
    }

    private void OnFirewallProfileApplyChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (FirewallProfileApply?.IsChecked == true && MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
    }

    private void OnDefenderScanOptionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DefenderScanConsent is null) return;
        DefenderScanConsent.IsChecked = false;
        DefenderScanConsent.IsEnabled = DefenderScanChoice?.SelectedIndex > 0;
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, e);
    }

    private void OnDefenderScanConsentChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DefenderScanConsent?.IsChecked == true && MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
    }

    private void OnEventChannelOptionChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        if (EventChannelEnableConsent is not null && sender == EventChannelChoice)
            EventChannelEnableConsent.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnEventChannelOptionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        OnEventChannelOptionChanged(sender, new System.Windows.RoutedEventArgs());

    private void OnOrcaOptionChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnOrcaOptionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        OnOrcaOptionChanged(sender, new System.Windows.RoutedEventArgs());

    private bool IsOrcaCacheRetentionSelected() =>
        MainWorkflow?.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Orca } &&
        OrcaScopeChoice?.SelectedIndex == 0 &&
        OrcaClearCaches?.IsChecked == true &&
        OrcaResetProfiles?.IsChecked != true &&
        OrcaResetAuthentication?.IsChecked != true &&
        OrcaDeleteOst?.IsChecked != true &&
        OrcaRemoveAddIns?.IsChecked != true &&
        OrcaReinstallNew?.IsChecked != true;

    private void OnUpdateDiscoverySelectionChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (UpdateSourceChoice is null || UpdateSoftware is null || UpdateDrivers is null) return;
        bool windowsDiscovery = UpdateSoftware.IsChecked == true || UpdateDrivers.IsChecked == true;
        UpdateSourceChoice.IsEnabled = windowsDiscovery;
        if (!windowsDiscovery) UpdateSourceChoice.SelectedIndex = 0;
    }

    private void OnWorkflowOptionsChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs? e)
    {
        if (CleanupSettings is null || MainAutoRepair is null) return;
        SynchronizeNavigation();
        RefreshRecipeSelection();
        bool servicingMonitor = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.ServicingActivity };
        if (ServicingMonitorSettings is not null) ServicingMonitorSettings.Visibility = servicingMonitor ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool packages = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.AppPackages };
        if (BitsSettings is not null) BitsSettings.Visibility = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.BackgroundTransfers }
            ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (InstalledApplicationSettings is not null) InstalledApplicationSettings.Visibility = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.InstalledApps }
            ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool mace = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Mace };
        if (MaceSettings is not null) MaceSettings.Visibility = mace ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool orca = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Orca };
        bool orcaCacheRetention = IsOrcaCacheRetentionSelected();
        if (OrcaSettings is not null) OrcaSettings.Visibility = orca ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool depot = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Depot };
        if (DepotSettings is not null) DepotSettings.Visibility = depot ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool beam = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Beam };
        if (BeamSettings is not null) BeamSettings.Visibility = beam ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool debloat = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Debloat };
        if (DebloatSettings is not null) DebloatSettings.Visibility = debloat ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (AppPackageSettings is not null) AppPackageSettings.Visibility = packages ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool tasks = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.ScheduledTasks };
        if (TaskSettings is not null) TaskSettings.Visibility = tasks ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool eventLog = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.EventLog };
        bool eventChannelEnable = eventLog && EventChannelEnableConsent?.IsChecked == true;
        if (EventLogSettings is not null) EventLogSettings.Visibility = eventLog ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (NetworkSettings is not null) NetworkSettings.Visibility = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Network }
            ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (AudioSettings is not null) AudioSettings.Visibility = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Audio }
            ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool cleanup = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Cleanup };
        bool printing = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Printing };
        bool installer = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Installer };
        bool selectedPrinter = printing && PrinterQueues?.SelectedItem is string;
        bool selectedPrintJob = printing && PrintJobChoice?.SelectedItem is System.Windows.Controls.ComboBoxItem
            { Tag: PrintJobSnapshot };
        if (PrinterSettings is not null) PrinterSettings.Visibility = printing ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (PrinterMakeDefault is not null)
        {
            PrinterMakeDefault.IsEnabled = selectedPrinter;
            if (!selectedPrinter) PrinterMakeDefault.IsChecked = false;
        }
        if (InstallerSettings is not null) InstallerSettings.Visibility = installer ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool time = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.TimeService };
        bool timePreview = time && TimeSourceChoice?.SelectedIndex > 0;
        if (TimeSettings is not null) TimeSettings.Visibility = time ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (TimePeers is not null) TimePeers.IsEnabled = TimeSourceChoice?.SelectedIndex == 2;
        if (TimeApply is not null)
        {
            TimeApply.IsEnabled = time && TimeSourceChoice?.SelectedIndex > 0;
            TimeApply.IsChecked = false;
        }
        bool profiles = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Profiles };
        if (ProfileSettings is not null) ProfileSettings.Visibility = profiles ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool updates = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.AvailableUpdates };
        bool search = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Search };
        bool searchRebuild = search && SearchIndexRebuildConsent?.IsChecked == true;
        if (SearchSettings is not null) SearchSettings.Visibility = search ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool privacy = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Privacy };
        bool bitlocker = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.BitLocker };
        bool components = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.ComponentCleanup };
        bool systemRestore = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.SystemRestore };
        bool systemRestoreCreate = systemRestore && SystemRestoreCreate?.IsChecked == true;
        if (SystemRestoreSettings is not null) SystemRestoreSettings.Visibility = systemRestore ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool storage = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Storage };
        bool storageRepair = storage && StorageRepairReview?.IsChecked == true;
        bool storageOptimization = storage && StorageOptimizationReview?.IsChecked == true;
        bool computerName = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.ComputerName };
        if (ComputerNameSettings is not null) ComputerNameSettings.Visibility = computerName ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (StorageSettings is not null) StorageSettings.Visibility = storage ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (storage && StorageVolumeChoice is not null)
        {
            string selected = StorageVolumeChoice.SelectedItem as string ?? "Windows volume";
            try
            {
                var choices = new[] { "Windows volume" }.Concat(DriveInfo.GetDrives().Select(d => d.Name.TrimEnd('\\')))
                    .Append(selected).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                StorageVolumeChoice.ItemsSource = choices;
                StorageVolumeChoice.SelectedItem = choices.First(choice => string.Equals(choice, selected, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                if (StorageVolumeChoice.Items.Count == 0)
                {
                    StorageVolumeChoice.ItemsSource = new[] { selected };
                    StorageVolumeChoice.SelectedIndex = 0;
                }
                Report("Drive list unavailable. Previous selection retained: " + error.Message);
            }
        }
        if (ComponentCleanupSettings is not null) ComponentCleanupSettings.Visibility = components ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        MainAutoRepair.Content = selectedPrinter ? "Clear offline mode if confirmed (can release waiting jobs)" : storageRepair ? "Repair confirmed filesystem errors" : storageOptimization ? "Run selected optimization after analysis" : systemRestoreCreate ? "Create this restore point after fresh checks" : orca ? "Retain and clear selected Outlook caches" : components ? "Run recommended cleanup" : "Auto-repair confirmed issues";
        if (BitLockerSettings is not null) BitLockerSettings.Visibility = bitlocker ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (PrivacySettings is not null) PrivacySettings.Visibility = privacy ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (UpdateSettings is not null) UpdateSettings.Visibility = updates ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (UpdateSourceChoice is not null && UpdateSoftware is not null && UpdateDrivers is not null)
            UpdateSourceChoice.IsEnabled = UpdateSoftware.IsChecked == true || UpdateDrivers.IsChecked == true;
        if (updates) RefreshUpdateStoreRecovery();
        bool defender = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Defender };
        if (SecuritySettings is not null) SecuritySettings.Visibility = defender ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        bool firewallProfiles = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.FirewallServices };
        if (FirewallProfileSettings is not null) FirewallProfileSettings.Visibility = firewallProfiles ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        if (!firewallProfiles && FirewallProfileApply is not null) FirewallProfileApply.IsChecked = false;
        CleanupSettings.Visibility = cleanup ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        MainAutoRepair.IsEnabled = MainWorkflow.SelectedItem is WorkflowDefinition definition && (Workflows.SupportsRepair(definition.Kind) || storageRepair || storageOptimization || systemRestoreCreate || orcaCacheRetention) && !packages && !tasks && !computerName && !cleanup && !updates && !privacy && !bitlocker && !profiles && !timePreview && !searchRebuild && !eventChannelEnable && !(defender && DefenderScanChoice?.SelectedIndex > 0);
        if (!MainAutoRepair.IsEnabled) MainAutoRepair.IsChecked = false;
        if (MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.CommonProblems } && FocusDiagnosticAreas?.IsChecked == true)
        { MainAutoRepair.IsEnabled = false; MainAutoRepair.IsChecked = false; }
        if (servicingMonitor || packages || tasks || computerName || (storage && !storageRepair && !storageOptimization) || (systemRestore && !systemRestoreCreate) || cleanup || updates || privacy || bitlocker || profiles || timePreview || searchRebuild || eventChannelEnable || selectedPrinter || selectedPrintJob || mace || depot || beam || debloat) MainAutoRepair.IsChecked = false;
        else if (CleanupRemove is not null) CleanupRemove.IsChecked = false;
        UpdateMainActionLabel();
    }

    private void OnMainAutoRepairChanged(object sender, System.Windows.RoutedEventArgs e) => UpdateMainActionLabel();

    private void UpdateMainActionLabel()
    {
        bool repairRequested = MainAutoRepair?.IsChecked == true;
        MainGo.Content = repairRequested ? "Run repair" : "Run diagnosis";
        MainGo.ToolTip = repairRequested
            ? "Run the selected checks and only the explicitly approved repairs. A report opens when the workflow finishes."
            : "Run the selected read-only checks and open a readable report.";
    }

    private void OnSystemRestoreOptionChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnSearchIndexRebuildChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnSystemRestoreDescriptionChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
    }

    private void OnSystemRestoreReasonChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
    }

    private void OnStorageRepairConsentChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender == StorageRepairReview && StorageRepairReview.IsChecked == true && StorageOptimizationReview is not null)
            StorageOptimizationReview.IsChecked = false;
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnStorageOptimizationChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender == StorageOptimizationReview && StorageOptimizationReview.IsChecked == true && StorageRepairReview is not null)
            StorageRepairReview.IsChecked = false;
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnStorageOptimizationModeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, e);
    }

    private void OnStorageRepairModeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, e);
    }

    private void OnPreviousWindowsChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (CleanupPreviousWindows?.IsChecked == true)
        {
            foreach (var option in new[] { CleanupUserTemp, CleanupWindowsTemp, CleanupSystemTemp, CleanupShaderCaches,
                CleanupBrowserCaches, CleanupOfficeCaches, CleanupThumbnailCache, CleanupIconCache,
                CleanupErrorReports, CleanupCrashDumps, CleanupWindowsMinidumps,
                CleanupDeliveryOptimization, CleanupRecycleBin }) option.IsChecked = false;
        }
        else if (CleanupPreviousWindowsBackup is not null) CleanupPreviousWindowsBackup.IsChecked = false;
        if (CleanupRemove is not null) CleanupRemove.IsChecked = false;
    }

    private void BeginWorkflowOutput(WorkflowKind kind)
    {
        workflowOutputActive = true;
        SourcesList.IsEnabled = false;
        SearchBox.IsEnabled = false;
        EvidenceTitle.Text = kind + " / Live";
        EvidenceBox.Text = $"{kind} / RUNNING\nTHIS PC: {Environment.MachineName}\n\nStarting live checks...\n";
        CoverageLabel.Text = "Live output. You can browse tools and export earlier reports. Stop waits for the current servicing command.";
    }

    private void AppendWorkflowOutput(string message)
    {
        if (!workflowOutputActive || string.IsNullOrWhiteSpace(message)) return;
        Report(message);
        string output = message.Replace("\r\n", "\n").Replace('\r', '\n').Trim('\0', '\uFEFF', '\n');
        if (EvidenceBox.Text.Length + output.Length > 100_000)
            EvidenceBox.Text = "Earlier live output omitted from display.\n" + EvidenceBox.Text[^Math.Min(50_000, EvidenceBox.Text.Length)..];
        EvidenceBox.AppendText(output + "\n");
        EvidenceBox.ScrollToEnd();
    }

    private async Task RunWorkflowAsync()
    {
        if (readCancellation is not null) return;
        var kind = (WorkflowKind)Math.Max(0, RepairChoice.SelectedIndex);
        if (App.DemoMode)
        {
            await RunDemoWorkflowAsync(kind, RepairConsent.IsChecked == true);
            return;
        }
        bool bitsAllUsers = BitsScopeChoice.SelectedIndex == 1;
        var audioSelection = new AudioEndpointSelection(
            AudioDirectionChoice.SelectedIndex switch { 1 => AudioEndpointDirection.Output, 2 => AudioEndpointDirection.Input, _ => null },
            AudioRoleChoice.SelectedIndex switch { 1 => AudioEndpointRole.Console, 2 => AudioEndpointRole.Multimedia, 3 => AudioEndpointRole.Communications, _ => null });
        var packageScope = AppPackageScopeChoice.SelectedIndex == 1 ? AppPackageScope.AllUsers : AppPackageScope.CurrentAccount;
        var applicationScope = InstalledApplicationScopeChoice.SelectedIndex == 1 ? InstalledApplicationScope.CurrentAccount : InstalledApplicationScope.Machine;
        var maceOptions = new MaceReviewOptions(
            (MaceOneDrive.IsChecked == true ? MaceTargets.OneDrive : MaceTargets.None) |
            (MaceOutlook.IsChecked == true ? MaceTargets.Outlook : MaceTargets.None) |
            (MaceOffice.IsChecked == true ? MaceTargets.Office : MaceTargets.None) |
            (MaceProject.IsChecked == true ? MaceTargets.Project : MaceTargets.None) |
            (MaceTeams.IsChecked == true ? MaceTargets.Teams : MaceTargets.None),
            MaceScopeChoice.SelectedIndex == 1 ? MaceScope.AllUsers : MaceScope.RunningAccount,
            MaceRemoveData.IsChecked != true, MaceRemoveCredentials.IsChecked != true, MaceRestoreFolders.IsChecked == true);
        if (kind == WorkflowKind.Mace && !macePackageReconciliationRequested)
        {
            try { maceOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        if (macePackageReconciliationRequested && kind != WorkflowKind.Mace)
        {
            Report("MACE reconciliation is available only from the MACE workflow.");
            return;
        }
        if (macePackageRemovalRequested && (kind != WorkflowKind.Mace ||
            MacePackageSelection.SelectedItem is not MacePackageChoice || MacePackageRemovalConsent.IsChecked != true))
        {
            Report("The separate MACE removal control requires one selected package and its confirmation.");
            return;
        }
        var orcaOptions = new OrcaReviewOptions(
            OrcaModeChoice.SelectedIndex switch { 0 => OrcaOutlookMode.New, 1 => OrcaOutlookMode.Classic, _ => OrcaOutlookMode.Both },
            OrcaScopeChoice.SelectedIndex == 1 ? OrcaScope.AllUsers : OrcaScope.RunningAccount,
            (OrcaClearCaches.IsChecked == true ? OrcaActions.ClearCaches : OrcaActions.None) |
            (OrcaResetProfiles.IsChecked == true ? OrcaActions.ResetProfileSettings : OrcaActions.None) |
            (OrcaResetAuthentication.IsChecked == true ? OrcaActions.ResetAuthentication : OrcaActions.None) |
            (OrcaDeleteOst.IsChecked == true ? OrcaActions.DeleteOstFiles : OrcaActions.None) |
            (OrcaRemoveAddIns.IsChecked == true ? OrcaActions.RemoveAddIns : OrcaActions.None) |
            (OrcaReinstallNew.IsChecked == true ? OrcaActions.ReinstallNewOutlook : OrcaActions.None));
        if (kind == WorkflowKind.Orca)
        {
            try { orcaOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        var depotOptions = new DepotOptions(
            (DepotMicrosoft365.IsChecked == true ? DepotApplications.Microsoft365 : DepotApplications.None) |
            (DepotOneDrive.IsChecked == true ? DepotApplications.OneDrive : DepotApplications.None) |
            (DepotTeams.IsChecked == true ? DepotApplications.Teams : DepotApplications.None) |
            (DepotChrome.IsChecked == true ? DepotApplications.Chrome : DepotApplications.None) |
            (DepotAcrobat.IsChecked == true ? DepotApplications.AcrobatReader : DepotApplications.None) |
            (DepotZoom.IsChecked == true ? DepotApplications.Zoom : DepotApplications.None) |
            (DepotSevenZip.IsChecked == true ? DepotApplications.SevenZip : DepotApplications.None),
            (DepotUpdates.IsChecked == true ? DepotConfiguration.WindowsUpdates : DepotConfiguration.None) |
            (DepotComputerName.IsChecked == true ? DepotConfiguration.ComputerName : DepotConfiguration.None) |
            (DepotLocalAdmin.IsChecked == true ? DepotConfiguration.LocalAdministrator : DepotConfiguration.None) |
            (DepotAdminReview.IsChecked == true ? DepotConfiguration.AdministratorReview : DepotConfiguration.None) |
            (DepotExtensions.IsChecked == true ? DepotConfiguration.BrowserExtensions : DepotConfiguration.None) |
            (DepotCleanup.IsChecked == true ? DepotConfiguration.Cleanup : DepotConfiguration.None) |
            (DepotEdge.IsChecked == true ? DepotConfiguration.Edge : DepotConfiguration.None) |
            (DepotPrivacy.IsChecked == true ? DepotConfiguration.Privacy : DepotConfiguration.None) |
            (DepotTaskbar.IsChecked == true ? DepotConfiguration.Taskbar : DepotConfiguration.None) |
            (DepotStartMenu.IsChecked == true ? DepotConfiguration.StartMenu : DepotConfiguration.None) |
            (DepotNotifications.IsChecked == true ? DepotConfiguration.Notifications : DepotConfiguration.None));
        if (kind == WorkflowKind.Depot)
        {
            try { depotOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        var beamOptions = new BeamOptions(
            BeamSourceKindChoice.SelectedIndex == 1 ? BeamSourceKind.HttpsUrl : BeamSourceKind.LocalFile,
            BeamSource.Text.Trim(), BeamInstallerTypeChoice.SelectedIndex == 1 ? BeamInstallerType.Msi : BeamInstallerType.Exe,
            BeamAppName.Text.Trim(), BeamPublisher.Text.Trim(), BeamInstallSwitches.Text.Trim(),
            BeamTargets.Text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            BeamTenantDomain.Text.Trim());
        if (kind == WorkflowKind.Beam)
        {
            try { beamOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        var debloatOptions = new DebloatOptions(
            (DebloatRecallAi.IsChecked == true ? DebloatAreas.RecallAndAi : DebloatAreas.None) |
            (DebloatTelemetry.IsChecked == true ? DebloatAreas.TelemetryAndReporting : DebloatAreas.None) |
            (DebloatSearch.IsChecked == true ? DebloatAreas.SearchAndSuggestions : DebloatAreas.None) |
            (DebloatConsumer.IsChecked == true ? DebloatAreas.ConsumerContent : DebloatAreas.None) |
            (DebloatStart.IsChecked == true ? DebloatAreas.TaskbarAndStart : DebloatAreas.None) |
            (DebloatNotifications.IsChecked == true ? DebloatAreas.Notifications : DebloatAreas.None) |
            (DebloatEdge.IsChecked == true ? DebloatAreas.Edge : DebloatAreas.None) |
            (DebloatPower.IsChecked == true ? DebloatAreas.HighPerformancePower : DebloatAreas.None) |
            (DebloatTimers.IsChecked == true ? DebloatAreas.SleepAndDisplayTimers : DebloatAreas.None) |
            (DebloatHibernation.IsChecked == true ? DebloatAreas.Hibernation : DebloatAreas.None) |
            (DebloatOfficeLanguages.IsChecked == true ? DebloatAreas.OfficeLanguages : DebloatAreas.None) |
            (DebloatHp.IsChecked == true ? DebloatAreas.HpSoftware : DebloatAreas.None) |
            (DebloatDell.IsChecked == true ? DebloatAreas.DellSoftware : DebloatAreas.None) |
            (DebloatAsusAcer.IsChecked == true ? DebloatAreas.AsusAcerSoftware : DebloatAreas.None) |
            (DebloatLenovo.IsChecked == true ? DebloatAreas.LenovoSoftware : DebloatAreas.None) |
            (DebloatThirdParty.IsChecked == true ? DebloatAreas.ThirdPartyApps : DebloatAreas.None) |
            (DebloatVendorData.IsChecked == true ? DebloatAreas.VendorData : DebloatAreas.None),
            DebloatScopeChoice.SelectedIndex == 1 ? DebloatUserScope.AllProfilesAndDefault : DebloatUserScope.RunningAccount,
            RetainedOfficeCulture: DebloatOfficeLanguages.IsChecked == true && DebloatOfficeCulture.SelectedItem is System.Windows.Controls.ComboBoxItem culture && culture.Tag is string value && value.Length > 0 ? value : null,
            PowerRequest: DebloatTimers.IsChecked == true ? new(PowerMinutes(DebloatSleepAc), PowerMinutes(DebloatSleepDc),
                PowerMinutes(DebloatDisplayAc), PowerMinutes(DebloatDisplayDc)) : null);
        if (kind == WorkflowKind.Debloat)
        {
            try { debloatOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        int debloatAction = kind == WorkflowKind.Debloat ? DebloatActionChoice.SelectedIndex : 0;
        if (debloatAction is < 0 or > 3) { Report("Choose a valid DEBLOAT GO action."); return; }
        if (debloatAction == 1 && debloatOptions.Areas != DebloatAreas.HighPerformancePower)
        { Report("High performance activation requires High performance power as the only selected area."); return; }
        if (debloatAction == 2 && (debloatOptions.Areas != DebloatAreas.SleepAndDisplayTimers || debloatOptions.PowerRequest is null))
        { Report("Power timer changes require Sleep and display timers as the only selected area."); return; }
        if (debloatAction == 3 && debloatOptions.Areas != DebloatAreas.Hibernation)
        { Report("Disabling hibernation requires Hibernation as the only selected area."); return; }
        bool includeProvisioned = IncludeProvisionedPackages.IsChecked == true;
        CommonDiagnosticScope[]? diagnosticScopes = kind == WorkflowKind.CommonProblems && FocusDiagnosticAreas.IsChecked == true
            ? DiagnosticAreaList.SelectedItems.Cast<DiagnosticArea>().Select(area => area.Scope).ToArray() : null;
        if (diagnosticScopes is { Length: 0 }) { Report("Select at least one diagnostic area before GO."); return; }
        string[]? selectedRecipes = kind == WorkflowKind.CommonProblems && CustomizeRecipes.IsChecked == true
            ? RecipeSelectionList.SelectedItems.Cast<RepairRecipe>().Select(recipe => recipe.Id).ToArray() : null;
        string? selectedQueue = kind == WorkflowKind.Printing ? PrinterQueues.SelectedItem as string : null;
        bool makeDefaultPrinter = selectedQueue is not null && PrinterMakeDefault.IsChecked == true;
        PrintJobSnapshot? selectedPrintJob = kind == WorkflowKind.Printing &&
            PrintJobChoice.SelectedItem is System.Windows.Controls.ComboBoxItem { Tag: PrintJobSnapshot job }
                ? job : null;
        bool cancelPrintJob = selectedPrintJob is not null && PrintJobCancelConsent.IsChecked == true;
        if (selectedPrintJob is not null && !cancelPrintJob)
        { Report("Confirm cancellation of the exact selected print job, or clear the job selection."); return; }
        string? msiProductCode = null;
        if (kind == WorkflowKind.Installer && !string.IsNullOrWhiteSpace(InstallerProductCode.Text))
        {
            try { msiProductCode = MsiProductDiagnostics.NormalizeProductCode(InstallerProductCode.Text.Trim()); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        var timeOptions = new TimeOptions((TimeSourceMode)Math.Max(0, TimeSourceChoice.SelectedIndex),
            TimeSourceChoice.SelectedIndex == 2 ? TimePeers.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) : []);
        if (kind == WorkflowKind.TimeService)
        {
            try { _ = TimePlanning.Create(timeOptions, new(null, null, null, false)); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        if (kind == WorkflowKind.Profiles)
        {
            profileInventory = null;
            profileDependenciesResult = null;
            ProfileSelection.ItemsSource = null;
        }
        var cleanupOptions = new CleanupOptions(
            (CleanupUserTemp.IsChecked == true ? CleanupTargets.UserTemp : CleanupTargets.None) |
            (CleanupWindowsTemp.IsChecked == true ? CleanupTargets.WindowsTemp : CleanupTargets.None) |
            (CleanupSystemTemp.IsChecked == true ? CleanupTargets.SystemTemp : CleanupTargets.None) |
            (CleanupShaderCaches.IsChecked == true ? CleanupTargets.ShaderCaches : CleanupTargets.None) |
            (CleanupBrowserCaches.IsChecked == true ? CleanupTargets.BrowserCaches : CleanupTargets.None) |
            (CleanupOfficeCaches.IsChecked == true ? CleanupTargets.OfficeCaches : CleanupTargets.None) |
            (CleanupThumbnailCache.IsChecked == true ? CleanupTargets.ThumbnailCache : CleanupTargets.None) |
            (CleanupIconCache.IsChecked == true ? CleanupTargets.IconCache : CleanupTargets.None) |
            (CleanupErrorReports.IsChecked == true ? CleanupTargets.ErrorReports : CleanupTargets.None) |
            (CleanupCrashDumps.IsChecked == true ? CleanupTargets.CrashDumps : CleanupTargets.None) |
            (CleanupWindowsMinidumps.IsChecked == true ? CleanupTargets.WindowsMinidumps : CleanupTargets.None) |
            (CleanupDeliveryOptimization.IsChecked == true ? CleanupTargets.DeliveryOptimizationCache : CleanupTargets.None) |
            (CleanupRecycleBin.IsChecked == true ? CleanupTargets.RecycleBin : CleanupTargets.None), (int)CleanupAge.Value, (int)CleanupLimit.Value);
        bool previousWindowsRemoval = kind == WorkflowKind.Cleanup && CleanupPreviousWindows.IsChecked == true;
        if (kind == WorkflowKind.Cleanup)
        {
            if (previousWindowsRemoval && cleanupOptions.Targets != CleanupTargets.None)
            { Report("Previous Windows installation removal must run separately from file and cache cleanup."); return; }
            try { if (!previousWindowsRemoval) cleanupOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        bool autoRepair = diagnosticScopes is null && RepairConsent.IsChecked == true;
        bool applyFirewallProfiles = kind == WorkflowKind.FirewallServices && FirewallProfileApply.IsChecked == true;
        FirewallProfileTarget[] firewallTargets =
        [
            .. FirewallDomainProfile.IsChecked == true ? [FirewallProfileTarget.Domain] : Array.Empty<FirewallProfileTarget>(),
            .. FirewallPrivateProfile.IsChecked == true ? [FirewallProfileTarget.Private] : Array.Empty<FirewallProfileTarget>(),
            .. FirewallPublicProfile.IsChecked == true ? [FirewallProfileTarget.Public] : Array.Empty<FirewallProfileTarget>()
        ];
        if (applyFirewallProfiles && firewallTargets.Length == 0) { Report("Choose at least one firewall profile before applying the change."); return; }
        string? networkLookupHost = null;
        var networkTimeout = TimeSpan.FromSeconds(NetworkTimeoutSeconds.Value);
        if (kind == WorkflowKind.Network && !string.IsNullOrWhiteSpace(NetworkLookupHost.Text))
        {
            try { networkLookupHost = DnsDiagnostics.ValidateHost(NetworkLookupHost.Text.Trim()); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        (string Host, int Port)? networkEndpoint = null;
        if (kind == WorkflowKind.Network && (!string.IsNullOrWhiteSpace(NetworkTcpHost.Text) || !string.IsNullOrWhiteSpace(NetworkTcpPort.Text)))
        {
            if (!int.TryParse(NetworkTcpPort.Text, out int port)) { Report("Enter a TCP port from 1 through 65535."); return; }
            try
            {
                string destination = NetworkTcpHost.Text.Trim();
                TcpDiagnostics.Validate(destination, port);
                networkEndpoint = (destination, port);
            }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        string proposedName = ProposedComputerName.Text;
        bool renameComputer = kind == WorkflowKind.ComputerName && ComputerRenameConsent.IsChecked == true;
        if (kind == WorkflowKind.ComputerName)
        {
            try { ComputerNameReview.Review(proposedName, new(null, null, null)); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        string? storageVolume = kind == WorkflowKind.Storage && StorageVolumeChoice.SelectedIndex > 0
            ? StorageDiagnostics.NormalizeVolume((string)StorageVolumeChoice.SelectedItem) : null;
        SystemRestoreCreationRequest? restorePointRequest = null;
        if (kind == WorkflowKind.SystemRestore && SystemRestoreCreate.IsChecked == true)
        {
            try
            {
                var reasonItem = SystemRestoreReasonChoice.SelectedItem as System.Windows.Controls.ComboBoxItem;
                if (reasonItem?.Tag is not string reasonName || !Enum.TryParse<SystemRestorePointReason>(reasonName, out var reason))
                    throw new ArgumentException("Choose a restore point reason.");
                restorePointRequest = SystemRestoreCreation.CreateRequest(SystemRestoreDescription.Text, reason);
            }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        bool applyTime = kind == WorkflowKind.TimeService && timeOptions.Source != TimeSourceMode.Existing && TimeApply.IsChecked == true;
        int profileDays = (int)ProfileAge.Value;
        bool profileDependencies = ProfileDependencyCheck.IsChecked == true;
        var securityOptions = new SecurityOptions(SecurityTpm.IsChecked == true, SecurityBoot.IsChecked == true, SecurityVirtualization.IsChecked == true);
        var updateOptions = new UpdateSearchOptions(UpdateSoftware.IsChecked == true, UpdateDrivers.IsChecked == true,
            UpdateSourceChoice.SelectedIndex switch
            {
                0 => UpdateSearchSource.Configured,
                1 => UpdateSearchSource.WindowsUpdate,
                2 => UpdateSearchSource.MicrosoftUpdate,
                _ => (UpdateSearchSource)(-1)
            });
        var privacyOptions = new PrivacyOptions(PrivacyAreas.SelectedItems.Cast<PrivacyChoice>().Select(c => c.Area).ToArray());
        var privacyItem = PrivacyClipboardHistoryChoice.SelectedItem as System.Windows.Controls.ComboBoxItem;
        string privacyAction = privacyItem?.Tag as string ?? "Inspect";
        bool applyPrivacyClipboardHistory = kind == WorkflowKind.Privacy && privacyAction is "On" or "Off" &&
            PrivacyClipboardHistoryApply.IsChecked == true;
        var privacyBooleanDefinition = PrivacyBooleanSettingChoice.SelectedItem as PrivacyBooleanSettingDefinition;
        var privacyBooleanItem = PrivacyBooleanActionChoice.SelectedItem as System.Windows.Controls.ComboBoxItem;
        string privacyBooleanAction = privacyBooleanItem?.Tag as string ?? "Inspect";
        bool applyPrivacyBoolean = kind == WorkflowKind.Privacy && privacyBooleanDefinition is not null &&
            privacyBooleanAction is "On" or "Off" && PrivacyBooleanApply.IsChecked == true;
        bool rebuildSearchIndex = kind == WorkflowKind.Search && SearchIndexRebuildConsent.IsChecked == true;
        string? selectedEventChannel = kind == WorkflowKind.EventLog && EventChannelChoice.SelectedItem is System.Windows.Controls.ComboBoxItem eventChannelItem
            ? eventChannelItem.Tag as string : null;
        bool enableEventChannel = selectedEventChannel is not null && EventChannelEnableConsent.IsChecked == true;
        var bitlockerOptions = new BitLockerOptions(BitLockerVolume.Text.Trim(),
            (BitLockerOperation)BitLockerOperationChoice.SelectedItem, (BitLockerEncryption)BitLockerEncryptionChoice.SelectedItem,
            BitLockerUsedSpace.IsChecked == true, (RecoveryDestination)BitLockerDestination.SelectedItem,
            string.IsNullOrWhiteSpace(BitLockerBackupPath.Text) ? null : BitLockerBackupPath.Text.Trim());
        if (kind == WorkflowKind.BitLocker)
        {
            try { bitlockerOptions.Validate(); }
            catch (ArgumentException error) { Report(error.Message); return; }
            if (BitLockerEncryptConsent.IsChecked == true)
            {
                await RunBitLockerEncryptionAsync(bitlockerOptions);
                return;
            }
            if (BitLockerResumeConsent.IsChecked == true)
            {
                await RunBitLockerResumeAsync(bitlockerOptions);
                return;
            }
            if (BitLockerBackupConsent.IsChecked == true)
            {
                await RunBitLockerBackupAsync(bitlockerOptions);
                return;
            }
        }
        if (kind == WorkflowKind.AvailableUpdates)
        {
            try { updateOptions.Criteria(); }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        ServicingMonitorOptions? servicingMonitorOptions = null;
        if (kind == WorkflowKind.ServicingActivity)
        {
            try
            {
                servicingMonitorOptions = new ServicingMonitorOptions((int)ServicingMonitorRefresh.Value,
                    (int)ServicingMonitorSamples.Value, ServicingMonitorEvents.IsChecked == true,
                    ServicingMonitorHistory.IsChecked == true, ServicingMonitorServices.IsChecked == true,
                    ServicingMonitorRestart.IsChecked == true, ServicingMonitorSetup.IsChecked == true,
                    ServicingMonitorCompatibility.IsChecked == true, ServicingMonitorReadiness.IsChecked == true,
                    ServicingMonitorTransfers.IsChecked == true,
                    ServicingMonitorDeliveryOptimization.IsChecked == true,
                    ServicingMonitorInstaller.IsChecked == true,
                    ServicingMonitorScheduledTask.IsChecked == true).Validate();
            }
            catch (ArgumentException error) { Report(error.Message); return; }
        }
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        ModeLabel.Text = restorePointRequest is not null ? "SYSTEM RESTORE / PREPARE POINT" : renameComputer ? "COMPUTER NAME / GUARDED RENAME" : previousWindowsRemoval ? "CLEANUP / PREVIOUS WINDOWS" : applyTime ? "TIME / APPLY CUSTOM SOURCE" : applyFirewallProfiles ? "FIREWALL / ENABLE PROFILES" : enableEventChannel ? "EVENT LOG / ENABLE CHANNEL" :
            applyPrivacyClipboardHistory ? "PRIVACY / CLIPBOARD HISTORY" : applyPrivacyBoolean ? "PRIVACY / GUARDED SETTING" : rebuildSearchIndex ? "SEARCH / GUARDED INDEX REBUILD" : debloatAction > 0 ? "DEBLOAT / GUARDED POWER CHANGE" : autoRepair ? "WORKFLOW / AUTOMATIC REPAIR" : "WORKFLOW / DIAGNOSE";
        BeginWorkflowOutput(kind);
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Workflows");
            var progress = new Progress<string>(AppendWorkflowOutput);
            if (macePackageReconciliationRequested)
            {
                string maceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "MacePackageRemoval");
                var reconciliation = await Task.Run(() => MacePackageRemoval.ReconcileAsync(maceFolder,
                    new WindowsMacePackageRemovalHost(), stop.Token));
                string output = "MACE PACKAGE REMOVAL / RECONCILIATION " + reconciliation.Outcome.ToString().ToUpperInvariant() +
                    "\n\n" + reconciliation.Detail + "\n\nRecord: " +
                    (reconciliation.ArchivePath ?? reconciliation.PendingPath) +
                    "\nNo package removal request was sent.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("MACE package reconciliation " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                SetMaceReview(null);
                Report(reconciliation.Detail);
                SetScanLight(reconciliation.Outcome is MacePackageRemovalReconciliationOutcome.Verified or
                    MacePackageRemovalReconciliationOutcome.NotApplied or MacePackageRemovalReconciliationOutcome.NoPendingRecord
                    ? "ready" : "attention", reconciliation.Detail);
                return;
            }
            if (macePackageRemovalRequested)
            {
                var selected = (MacePackageChoice)MacePackageSelection.SelectedItem;
                string maceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "MacePackageRemoval");
                var removal = await Task.Run(() => MacePackageRemoval.RunAsync(maceOptions, selected.Identity,
                    true, maceFolder, new WindowsMacePackageRemovalHost(), progress, stop.Token));
                string output = "MACE PACKAGE REMOVAL / " + removal.Outcome.ToString().ToUpperInvariant() +
                    "\nPackage: " + selected.Identity + "\n\n" + removal.Detail +
                    "\n\nRecord: " + (removal.JournalPath ?? removal.PendingPath ?? "No change record created.") +
                    "\nNo other package, user data, credential, policy or folder action was requested.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("MACE package removal " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                MacePackageRemovalConsent.IsChecked = false;
                SetMaceReview(null);
                Report(removal.Detail);
                SetScanLight(removal.Outcome == MacePackageRemovalOutcome.Verified ? "ready" : "attention", removal.Detail);
                return;
            }
            if (servicingMonitorOptions is not null)
            {
                var snapshots = await Task.Run(() => ServicingActivityMonitor.ObserveAsync(
                    servicingMonitorOptions, progress: progress, token: stop.Token));
                string output = ServicingActivityText.FormatReport(snapshots);
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("PEEK servicing activity " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                bool incomplete = snapshots.Any(snapshot => snapshot.State == CheckState.Unknown);
                bool attention = snapshots.Any(snapshot => snapshot.State == CheckState.Attention);
                string detail = $"PEEK retained {snapshots.Count} read-only servicing snapshot{(snapshots.Count == 1 ? "" : "s")}.";
                Report(detail);
                SetScanLight(incomplete ? "incomplete" : attention ? "attention" : "ready", detail);
                return;
            }
            if (enableEventChannel)
            {
                string eventFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "EventChannels");
                var eventHost = new WindowsEventChannelEnableHost();
                EventChannelEnableResult eventResult = File.Exists(Path.Combine(eventFolder, "event-channel-enable.pending.json"))
                    ? await Task.Run(() => EventChannelEnable.ReconcileAsync(eventFolder, eventHost, progress, stop.Token))
                    : await Task.Run(() => EventChannelEnable.RunAsync(selectedEventChannel!, true, eventFolder, eventHost, progress, stop.Token));
                string output = "DIAGNOSTIC EVENT CHANNEL / " + eventResult.Outcome.ToString().ToUpperInvariant() +
                    "\nChannel: " + (string.IsNullOrWhiteSpace(eventResult.Channel) ? selectedEventChannel : eventResult.Channel) +
                    "\n" + eventResult.Detail + "\nRecord: " + (eventResult.RecordPath ?? "No change record created.") +
                    "\n\nExisting events, permissions, retention, automatic backup, maximum size and every other channel were preserved. Event delivery was not tested.";
                bool verified = eventResult.Outcome == EventChannelEnableOutcome.Verified;
                RegisterGuardedActionReport(WorkflowKind.EventLog, "Diagnostic event channel",
                    "Diagnostic event channel / " + eventResult.Outcome,
                    output, eventResult.Detail, eventResult.RecordPath ?? "No change record created.", verified,
                    "Review the channel's events and retention policy before changing another diagnostic setting.");
                EventChannelEnableConsent.IsChecked = false;
                SetScanLight(eventResult.Outcome == EventChannelEnableOutcome.Verified ? "ready" :
                    eventResult.Outcome == EventChannelEnableOutcome.Blocked ? "attention" : "incomplete", eventResult.Detail);
                return;
            }
            if (rebuildSearchIndex)
            {
                string searchFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "SearchIndexRebuild");
                var searchHost = new WindowsSearchIndexRebuildHost();
                string title;
                string detail;
                string? journal;
                string? retained = null;
                bool verified;
                if (File.Exists(Path.Combine(searchFolder, "search-index-rebuild.pending.json")))
                {
                    var recovery = await Task.Run(() => SearchIndexRebuildReconciliation.RunAsync(searchFolder, searchHost, stop.Token));
                    title = "WINDOWS SEARCH INDEX / RECONCILIATION " + recovery.Outcome;
                    detail = recovery.Detail;
                    journal = recovery.JournalPath;
                    verified = recovery.Outcome == SearchIndexReconciliationOutcome.Verified;
                }
                else
                {
                    var rebuild = await Task.Run(() => SearchIndexRebuild.RunAsync(true, searchFolder, searchHost, progress, stop.Token));
                    title = "WINDOWS SEARCH INDEX / " + rebuild.Outcome;
                    detail = rebuild.Detail;
                    journal = rebuild.JournalPath;
                    retained = rebuild.RetainedPath;
                    verified = rebuild.Outcome == SearchIndexRebuildOutcome.Verified;
                }
                string output = title + "\n" + detail +
                    "\nRetained index: " + (retained ?? "See the journal when an index was retained.") +
                    "\nJournal: " + (journal ?? "No change journal created.") +
                    "\n\nNo retained index was deleted and no Search policy, indexed location or startup mode was changed.";
                RegisterGuardedActionReport(WorkflowKind.Search, "Windows Search index", title, output, detail,
                    journal ?? "No change journal created.", verified,
                    "Test Search after indexing settles before making another change.");
                SearchIndexRebuildConsent.IsChecked = false;
                SetScanLight(verified ? "ready" : "attention", detail);
                return;
            }
            if (applyPrivacyClipboardHistory)
            {
                string privacyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "Privacy");
                var privacyHost = new WindowsPrivacyClipboardHistoryHost();
                string title;
                string detail;
                string? journal;
                bool verified;
                if (File.Exists(Path.Combine(privacyFolder, "privacy-clipboard-history.pending.json")))
                {
                    var recovery = await Task.Run(() => PrivacyClipboardHistoryExecution.ReconcileAsync(
                        privacyFolder, privacyHost, progress, stop.Token));
                    title = "CLIPBOARD HISTORY / RECONCILIATION " + recovery.Outcome;
                    detail = recovery.Detail;
                    journal = recovery.ArchivePath ?? recovery.PendingPath;
                    verified = recovery.Outcome == PrivacyChangeReconciliationOutcome.Verified;
                }
                else
                {
                    var change = await Task.Run(() => PrivacyClipboardHistoryExecution.RunAsync(privacyAction == "On", true,
                        privacyFolder, privacyHost, progress, stop.Token));
                    title = "CLIPBOARD HISTORY / " + change.Outcome + "\nRequested: " + (change.RequestedEnabled ? "On" : "Off");
                    detail = change.Detail;
                    journal = change.JournalPath;
                    verified = change.Outcome == PrivacyChangeOutcome.Verified;
                }
                string output = title + "\n" + detail +
                    "\nJournal: " + (journal ?? "No change journal created.") +
                    "\n\nMachine policy, Clipboard sync and stored clipboard contents were preserved.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Clipboard history " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                Report(detail);
                SetScanLight(verified ? "ready" : "attention", detail);
                return;
            }
            if (applyPrivacyBoolean && privacyBooleanDefinition is not null)
            {
                string privacyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "Privacy");
                var privacyHost = new WindowsPrivacyBooleanSettingHost(privacyBooleanDefinition);
                string pendingPath = Path.Combine(privacyFolder, privacyBooleanDefinition.Key + ".pending.json");
                string title;
                string detail;
                string? journal;
                bool verified;
                if (File.Exists(pendingPath))
                {
                    var recovery = await Task.Run(() => PrivacyBooleanSettingExecution.ReconcileAsync(
                        privacyBooleanDefinition, privacyFolder, privacyHost, progress, stop.Token));
                    title = privacyBooleanDefinition.Title.ToUpperInvariant() + " / RECONCILIATION " + recovery.Outcome;
                    detail = recovery.Detail;
                    journal = recovery.ArchivePath ?? recovery.PendingPath;
                    verified = recovery.Outcome == PrivacyChangeReconciliationOutcome.Verified;
                }
                else
                {
                    var change = await Task.Run(() => PrivacyBooleanSettingExecution.RunAsync(privacyBooleanDefinition,
                        privacyBooleanAction == "On", true, privacyFolder, privacyHost, progress, stop.Token));
                    title = privacyBooleanDefinition.Title.ToUpperInvariant() + " / " + change.Outcome +
                        "\nRequested: " + (change.RequestedEnabled ? "On" : "Off");
                    detail = change.Detail;
                    journal = change.JournalPath;
                    verified = change.Outcome == PrivacyChangeOutcome.Verified;
                }
                string output = title + "\n" + detail +
                    "\nJournal: " + (journal ?? "No change journal created.") +
                    "\n\nMatching policy and unrelated privacy settings were preserved.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence(privacyBooleanDefinition.Title + " " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                Report(detail);
                SetScanLight(verified ? "ready" : "attention", detail);
                return;
            }
            if (kind == WorkflowKind.Debloat && debloatAction > 0)
            {
                string changeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "Debloat");
                string title;
                string detail;
                string? journal;
                bool verified;
                if (debloatAction == 1)
                {
                    var planHost = new WindowsDebloatPowerPlanChangeHost();
                    if (File.Exists(Path.Combine(changeFolder, "debloat-power-plan.pending.json")))
                    {
                        var reconciliation = await Task.Run(() => DebloatPowerReconciliation.RunPlanAsync(changeFolder, planHost, progress, stop.Token));
                        title = "HIGH PERFORMANCE POWER / RECONCILIATION " + reconciliation.Outcome;
                        detail = reconciliation.Detail;
                        journal = reconciliation.ArchivePath ?? reconciliation.PendingPath;
                        verified = reconciliation.Outcome == DebloatPowerReconciliationOutcome.Verified;
                    }
                    else
                    {
                        var planResult = await Task.Run(() => DebloatPowerPlanExecution.RunAsync(
                            DebloatPowerPlanExecution.CreateHighPerformance(), true, changeFolder, planHost, progress, stop.Token));
                        title = "HIGH PERFORMANCE POWER / " + planResult.Outcome;
                        detail = planResult.Detail;
                        journal = planResult.JournalPath;
                        verified = planResult.Outcome == DebloatPowerPlanOutcome.Verified;
                    }
                }
                else if (debloatAction == 2)
                {
                    var timerHost = new WindowsDebloatPowerTimerHost();
                    if (File.Exists(Path.Combine(changeFolder, "debloat-power-timers.pending.json")))
                    {
                        var reconciliation = await Task.Run(() => DebloatPowerReconciliation.RunTimersAsync(changeFolder, timerHost, progress, stop.Token));
                        title = "POWER TIMERS / RECONCILIATION " + reconciliation.Outcome;
                        detail = reconciliation.Detail;
                        journal = reconciliation.ArchivePath ?? reconciliation.PendingPath;
                        verified = reconciliation.Outcome == DebloatPowerReconciliationOutcome.Verified;
                    }
                    else
                    {
                        var timerResult = await Task.Run(() => DebloatPowerTimerExecution.RunAsync(
                            debloatOptions.PowerRequest!, true, changeFolder, timerHost, progress, stop.Token));
                        title = "POWER TIMERS / " + timerResult.Outcome;
                        detail = timerResult.Detail;
                        journal = timerResult.JournalPath;
                        verified = timerResult.Outcome == DebloatPowerTimerOutcome.Verified;
                    }
                }
                else
                {
                    var hibernationHost = new WindowsDebloatHibernationHost();
                    if (File.Exists(Path.Combine(changeFolder, "debloat-hibernation.pending.json")))
                    {
                        var reconciliation = await Task.Run(() => DebloatPowerReconciliation.RunHibernationAsync(changeFolder, hibernationHost, progress, stop.Token));
                        title = "HIBERNATION / RECONCILIATION " + reconciliation.Outcome;
                        detail = reconciliation.Detail;
                        journal = reconciliation.ArchivePath ?? reconciliation.PendingPath;
                        verified = reconciliation.Outcome == DebloatPowerReconciliationOutcome.Verified;
                    }
                    else
                    {
                        var hibernationResult = await Task.Run(() => DebloatHibernationExecution.RunAsync(true, changeFolder,
                            hibernationHost, progress, stop.Token));
                        title = "HIBERNATION / " + hibernationResult.Outcome;
                        detail = hibernationResult.Detail;
                        journal = hibernationResult.JournalPath;
                        verified = hibernationResult.Outcome == DebloatHibernationOutcome.Verified;
                    }
                }
                string output = title + "\n" + detail + "\nJournal: " + (journal ?? "No change journal created.") +
                    "\n\nApplies only to the selected power action. No other DEBLOAT area was changed.";
                AppendWorkflowOutput(output);
                var action = new WorkflowAction(title, verified ? "Verified" : "Review required", detail,
                    verified ? "Review the completed action and test the computer's power behavior before making another change."
                        : "Review the local change journal and current power state before retrying.", verified);
                RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Debloat, Environment.MachineName, DateTimeOffset.UtcNow,
                    verified ? "Power action verified" : "Power action requires review",
                    [new WorkflowCheck("Guarded DEBLOAT power action", verified ? CheckState.Information : CheckState.Unknown,
                        detail)], [], journal ?? "No change journal created.") { Actions = [action] }, "DEBLOAT power action");
                SetScanLight(verified ? "ready" : "attention", detail);
                return;
            }
            if (applyFirewallProfiles)
            {
                string firewallFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Firewall");
                var firewallHost = new WindowsFirewallProfileEnableHost();
                FirewallProfileEnableResult firewall;
                if (File.Exists(Path.Combine(firewallFolder, "firewall-profile-enable.pending.json")))
                    firewall = await Task.Run(() => FirewallProfileEnable.ReconcileAsync(firewallFolder, firewallHost, progress, stop.Token));
                else
                    firewall = await Task.Run(() => FirewallProfileEnable.RunAsync(new(firewallTargets), true, firewallFolder, firewallHost, progress, stop.Token));
                string output = "WINDOWS FIREWALL PROFILES / " + firewall.Outcome + "\nSelected: " + string.Join(", ", firewall.Profiles) +
                    "\n" + firewall.Detail + "\nRecord: " + (firewall.RecordPath ?? "No change record created.") +
                    "\n\nRules, default actions, logging and managed policy were preserved.";
                bool verified = firewall.Outcome == FirewallProfileEnableOutcome.Verified;
                RegisterGuardedActionReport(WorkflowKind.FirewallServices, "Windows Firewall profiles", "Windows Firewall profiles / " + firewall.Outcome,
                    output, firewall.Detail, firewall.RecordPath ?? "No change record created.", verified,
                    "Test the affected network connection and review firewall policy before making another change.");
                SetScanLight(firewall.Outcome == FirewallProfileEnableOutcome.Verified ? "ready" :
                    firewall.Outcome == FirewallProfileEnableOutcome.Blocked ? "attention" : "incomplete", firewall.Detail);
                return;
            }
            if (renameComputer)
            {
                string renameFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "ComputerRename");
                ComputerRenameResult rename;
                if (File.Exists(Path.Combine(renameFolder, "computer-rename.pending.json")))
                    rename = await Task.Run(() => ComputerRename.ReconcileAsync(renameFolder, new WindowsComputerRenameHost(), progress, stop.Token));
                else
                    rename = await Task.Run(() => ComputerRename.RunAsync(proposedName, true, renameFolder, new WindowsComputerRenameHost(), progress, stop.Token));
                string output = "COMPUTER NAME / " + rename.Outcome + "\nTarget: " + (string.IsNullOrWhiteSpace(rename.TargetName) ? proposedName : rename.TargetName) +
                    "\n" + rename.Detail + "\nRecord: " + (rename.RecordPath ?? "No operation record created.") +
                    "\n\nKillerBench did not restart Windows or join a domain.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Computer name " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                Report(rename.Detail);
                SetScanLight(rename.Outcome == ComputerRenameOutcome.Verified ? "ready" :
                    rename.Outcome == ComputerRenameOutcome.RestartRequired ? "attention" : "incomplete", rename.Detail);
                return;
            }
            if (cancelPrintJob)
            {
                string printJobFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "PrintJobs");
                AppendWorkflowOutput("Rechecking the exact selected print job before cancellation...");
                PrintJobCancellationResult cancellation;
                if (PrintJobCancellation.HasPendingRecord(printJobFolder))
                    cancellation = await Task.Run(() => PrintJobCancellation.ReconcileAsync(printJobFolder,
                        new WindowsPrintJobCancellationHost(), progress, stop.Token));
                else
                    cancellation = await Task.Run(() => PrintJobCancellation.RunAsync(selectedPrintJob!.QueueName,
                        selectedPrintJob.JobId, true, printJobFolder, new WindowsPrintJobCancellationHost(), progress, stop.Token));
                string cancellationOutput = "PRINT JOB CANCELLATION / " + cancellation.Outcome.ToString().ToUpperInvariant() +
                    "\nQueue: " + cancellation.QueueName + "\nJob ID: " + cancellation.JobId + "\n" + cancellation.Detail +
                    "\nRecord: " + (cancellation.RecordPath ?? "No operation record created.") +
                    "\n\nNo queue purge, spooler restart or printer setting change was requested.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Print job " + cancellation.JobId,
                    cancellationOutput.Split('\n'), false));
                PrintJobCancelConsent.IsChecked = false;
                Report(cancellation.Detail);
                SetScanLight(cancellation.Outcome == PrintJobCancellationOutcome.Verified ? "ready" :
                    cancellation.Outcome == PrintJobCancellationOutcome.Blocked ? "attention" : "incomplete",
                    cancellation.Detail);
                return;
            }
            if (selectedQueue is not null)
            {
                if (makeDefaultPrinter)
                {
                    string defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "KillerBench", "DefaultPrinter");
                    AppendWorkflowOutput("Checking the exact queue and current default-printer state...");
                    DefaultPrinterSelectionResult selection;
                    if (DefaultPrinterSelection.HasPendingRecord(defaultFolder))
                        selection = await Task.Run(() => DefaultPrinterSelection.ReconcileAsync(defaultFolder,
                            new WindowsDefaultPrinterSelectionHost(), progress, stop.Token));
                    else
                        selection = await Task.Run(() => DefaultPrinterSelection.RunAsync(selectedQueue, true, defaultFolder,
                            new WindowsDefaultPrinterSelectionHost(), progress, stop.Token));
                    string selectionOutput = "DEFAULT PRINTER / " + selection.Outcome.ToString().ToUpperInvariant() +
                        "\nQueue: " + selectedQueue + "\n" + selection.Detail +
                        "\nRecord: " + (selection.RecordPath ?? "No operation record created.") +
                        "\n\nNo queue, port, driver or job was changed. Printing was not tested.";
                    workflowOutputActive = false;
                    RegisterSource(new TextEvidence("Default printer: " + selectedQueue, selectionOutput.Split('\n'), false));
                    Report(selection.Detail);
                    SetScanLight(selection.Outcome == DefaultPrinterSelectionOutcome.Verified ? "ready" :
                        selection.Outcome == DefaultPrinterSelectionOutcome.Blocked ? "attention" : "incomplete", selection.Detail);
                    return;
                }
                var check = await Task.Run(() => PrinterDiagnostics.CheckOfflineAsync([selectedQueue], stop.Token, progress));
                AppendWorkflowOutput(check.Detail);
                if (autoRepair && check.State == CheckState.Repairable)
                {
                    AppendWorkflowOutput("Rechecking the selected queue before clearing offline mode. Waiting jobs may be released.");
                    var repair = await Task.Run(() => Repairs.RunAsync(RepairKind.PrinterOffline, folder, progress, stop.Token,
                        selectedPrinterQueues: [selectedQueue]));
                    bool verified = Repairs.IsVerified(repair);
                    workflowOutputActive = false;
                    RegisterSource(new TextEvidence("Printer: " + selectedQueue,
                        new[] { "SELECTED PRINTER / REPAIR", selectedQueue, check.Detail, repair.Status,
                            "Journal: " + repair.JournalPath, "Physical printer health was not tested. Jobs were not deleted." }, false));
                    Report(repair.Status + ". Journal: " + repair.JournalPath);
                    SetScanLight(verified ? "ready" : "incomplete", repair.Status);
                    return;
                }
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Printer: " + selectedQueue,
                    new[] { "SELECTED PRINTER / READ ONLY", selectedQueue, check.State.ToString(), check.Detail, "No printer settings changed. Physical printer health was not tested." }, false));
                Report("Selected printer check completed. No settings changed.");
                SetScanLight(check.State == CheckState.Clear ? "ready" : "incomplete", "Selected queue settings only.");
                return;
            }
            if (kind == WorkflowKind.TimeService && timeOptions.Source != TimeSourceMode.Existing)
            {
                AppendWorkflowOutput("Inspecting domain role and time policy for the selected source...");
                var preview = await Task.Run(() => TimePlanning.PreviewAsync(timeOptions, stop.Token));
                var plan = preview.Plan;
                if (applyTime && plan.ReviewPassed)
                {
                    string timeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Time");
                    if (TimeConfigurationRecovery.HasPendingRecord(timeFolder))
                    {
                        AppendWorkflowOutput("A pending time-source change exists. Checking it without retrying configuration or synchronization...");
                        var recovery = await Task.Run(() => TimeConfigurationRecovery.ReconcileAsync(timeFolder, stop.Token));
                        string recoverySummary = "TIME SOURCE RECOVERY / " + recovery.Outcome.ToString().ToUpperInvariant() + "\n" + recovery.Detail +
                            "\nPending record: " + recovery.PendingPath + "\nArchived record: " + (recovery.ArchivedPath ?? "None") +
                            "\nNo clock or source settings changed.";
                        workflowOutputActive = false;
                        RegisterSource(new TextEvidence("Time source recovery", recoverySummary.Split('\n'), false));
                        Report(recovery.Detail);
                        SetScanLight(recovery.Resolved ? "ready" : "incomplete", recovery.Detail);
                        return;
                    }
                    var configured = await Task.Run(() => NativeTimeConfiguration.RunAsync(timeOptions,
                        timeFolder, progress, stop.Token));
                    string timeSummary = "TIME SOURCE / " + configured.Status + "\n" + configured.Detail +
                        "\nBefore mode: " + (configured.Before?.LocalMode ?? "Unavailable") +
                        "\nBefore peers: " + (configured.Before?.LocalPeers ?? "Unavailable") +
                        "\nAfter mode: " + (configured.After?.LocalMode ?? "Unavailable") +
                        "\nAfter peers: " + (configured.After?.LocalPeers ?? "Unavailable") +
                        "\nJournal: " + configured.Journal;
                    workflowOutputActive = false;
                    RegisterSource(new TextEvidence("Time source result", timeSummary.Split('\n'), false));
                    Report(configured.Status + ". Journal: " + configured.Journal);
                    SetScanLight(configured.Status == "Verified" ? "ready" : "incomplete", configured.Detail);
                    return;
                }
                string output = plan.Format() + "\n\nLocally stored mode: " + (preview.Environment.LocalMode ?? "Unavailable") +
                    "\nLocally stored peers: " + (preview.Environment.LocalPeers ?? "Unavailable") +
                    "\nPolicy or domain configuration may override these local values.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Time source preview", output.Split('\n'), false));
                Report("Time-source preview completed. No clock or source settings changed.");
                SetScanLight(plan.ReviewPassed ? "ready" : "incomplete", "Preview only. No settings changed.");
                return;
            }
            if (kind == WorkflowKind.AvailableUpdates)
            {
                availableUpdateResult = null;
                SetThirdPartyUpdateResult(null);
                ApplyUpdateFilter();
                bool windowsSelected = updateOptions.Software || updateOptions.Drivers;
                bool thirdPartySelected = UpdateThirdParty.IsChecked == true;
                if (!windowsSelected && !thirdPartySelected)
                {
                    workflowOutputActive = false;
                    Report("Select software updates, drivers or third-party apps before pressing GO.");
                    SetScanLight("attention", "No update category was selected.");
                    return;
                }
                UpdateSearchResult? discovery = null;
                ThirdPartyUpdateResult? thirdPartyDiscovery = null;
                if (windowsSelected)
                {
                    discovery = await Task.Run(() => UpdateDiscovery.SearchAsync(updateOptions, progress, stop.Token));
                    availableUpdateResult = discovery;
                }
                if (thirdPartySelected)
                {
                    thirdPartyDiscovery = await Task.Run(() => ThirdPartyUpdateDiscovery.ReadAsync(progress, stop.Token));
                    SetThirdPartyUpdateResult(thirdPartyDiscovery);
                }
                ApplyUpdateFilter();
                workflowOutputActive = false;
                if (discovery is not null)
                    RegisterSource(new TextEvidence("Available updates " + DateTime.Now.ToString("HHmmss"), discovery.Format().Split('\n'), false));
                if (thirdPartyDiscovery is not null)
                    RegisterSource(new TextEvidence("Third-party updates " + DateTime.Now.ToString("HHmmss"), thirdPartyDiscovery.Format().Split('\n'), false));
                bool incomplete = discovery?.Partial == true || thirdPartyDiscovery is { ProviderAvailable: false } || thirdPartyDiscovery is { Complete: false };
                Report(incomplete ? "Discovery is incomplete. Review the retained evidence before preparing an installation." :
                    "Discovery completed. Select a Windows update or one third-party application version to preview.");
                SetScanLight(incomplete ? "incomplete" : "ready", "Update discovery ended. No installation performed.");
                return;
            }
            if (restorePointRequest is not null)
            {
                string restoreFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "SystemRestore");
                var restoreHost = new WindowsSystemRestoreCreationHost();
                SystemRestoreCreationResult restore;
                if (File.Exists(Path.Combine(restoreFolder, "system-restore-create.pending.json")))
                    restore = await Task.Run(() => SystemRestoreCreation.ReconcileAsync(restoreFolder, restoreHost, progress, stop.Token));
                else
                    restore = await Task.Run(() => SystemRestoreCreation.RunAsync(restorePointRequest, autoRepair,
                        restoreFolder, restoreHost, progress, stop.Token));
                string output = "SYSTEM RESTORE POINT / " + restore.Outcome +
                    "\nDescription: " + restore.Request.Description + "\nReason: " + restore.Request.Reason +
                    "\nExisting points observed: " + (restore.Before?.Points.Count.ToString() ?? "Not inspected in preview") +
                    "\n\n" + restore.Detail + "\n\nJournal: " + (restore.JournalPath ?? "No change journal created.") +
                    "\nKillerBench did not delete or apply a restore point and did not change System Protection settings.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("System Restore point " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                Report(restore.Detail);
                SetScanLight(restore.Outcome is SystemRestoreCreationOutcome.Verified or SystemRestoreCreationOutcome.Preview ? "ready" : "attention", restore.Detail);
                return;
            }
            if (kind == WorkflowKind.Storage && StorageRepairReview.IsChecked == true)
            {
                string volume = storageVolume ?? StorageDiagnostics.SystemVolume();
                var modeItem = StorageRepairModeChoice.SelectedItem as System.Windows.Controls.ComboBoxItem;
                if (modeItem?.Tag is not string modeName || !Enum.TryParse<FileSystemRepairMode>(modeName, out var repairMode))
                    throw new InvalidOperationException("Choose a filesystem repair mode.");
                var drive = new DriveInfo(volume + "\\");
                bool ready = drive.IsReady;
                var draft = FileSystemRepairPlanning.Create(volume, repairMode,
                    new(ready, drive.DriveType == DriveType.Fixed, ready ? drive.DriveFormat : null),
                    string.Equals(volume, StorageDiagnostics.SystemVolume(), StringComparison.OrdinalIgnoreCase),
                    StorageAllowDismount.IsChecked == true, StorageBackupConfirmed.IsChecked == true);
                AppendWorkflowOutput("FILESYSTEM REPAIR / REVIEW ONLY");
                AppendWorkflowOutput(draft.Effects);
                AppendWorkflowOutput(draft.VolumeEligible ? "Volume options accepted. Collecting current read-only evidence..." : draft.Blocker ?? "Volume options are not supported.");
                var diagnosis = await Task.Run(() => StorageDiagnostics.CheckVolumeAsync(volume, progress, stop.Token));
                var record = await FileSystemRepairPlanning.SaveReviewAsync(draft, diagnosis,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "StorageReviews"), stop.Token);
                var review = record.Review;
                if (autoRepair && review.ExecutionEligible)
                {
                    string repairFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "KillerBench", "FileSystemRepair");
                    if (File.Exists(Path.Combine(repairFolder, "filesystem-repair.pending.json")))
                    {
                        var recovery = await Task.Run(() => FileSystemRepairReconciliation.RunAsync(repairFolder,
                            new WindowsFileSystemRepairHost(), progress, stop.Token));
                        string recoveryOutput = "FILESYSTEM REPAIR / INTERRUPTED OPERATION\nOutcome: " + recovery.Outcome +
                            "\n" + recovery.Detail + "\nRecord: " + (recovery.ArchivePath ?? recovery.PendingPath);
                        workflowOutputActive = false;
                        RegisterSource(new TextEvidence("Filesystem repair review " + DateTime.Now.ToString("HHmmss"),
                            recoveryOutput.Split('\n'), false));
                        Report(recovery.Detail);
                        SetScanLight(recovery.Outcome == FileSystemRepairReconciliationOutcome.Verified ? "ready" : "attention", recovery.Detail);
                        return;
                    }
                    var repair = await Task.Run(() => FileSystemRepairExecution.RunAsync(draft, true, repairFolder,
                        new WindowsFileSystemRepairHost(), progress, stop.Token));
                    string repairOutput = "FILESYSTEM REPAIR\nVolume: " + draft.Volume + "\nMode: " + draft.Mode +
                        "\nOutcome: " + repair.Outcome + "\n" + repair.Detail +
                        "\nJournal: " + (repair.JournalPath ?? "No repair journal created.");
                    workflowOutputActive = false;
                    RegisterSource(new TextEvidence("Filesystem repair " + DateTime.Now.ToString("HHmmss"), repairOutput.Split('\n'), false));
                    Report(repair.Detail);
                    SetScanLight(repair.Outcome == FileSystemRepairOutcome.Verified ? "ready" : "attention", repair.Detail);
                    return;
                }
                string output = "FILESYSTEM REPAIR / REVIEW ONLY\nVolume: " + draft.Volume + "\nMode: " + draft.Mode +
                    "\nVolume options: " + (draft.VolumeEligible ? "Accepted" : "Blocked") +
                    "\nCurrent evidence matched: " + (review.EvidenceMatched ? "Yes" : "No") +
                    "\nExecution available: No\nRestart required by proposed operation: " + (draft.RequiresRestart ? "Yes" : "No") +
                    "\nDismount required by proposed operation: " + (draft.RequiresDismount ? "Yes" : "No") +
                    "\n\nDiagnosis: " + diagnosis.State + "\n" + diagnosis.Detail + "\n\nNext: " + review.NextAction +
                    "\nJournal: " + record.JournalPath + "\n\nNo repair, dismount or restart was scheduled.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Filesystem repair review " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                Report("Filesystem repair review saved. No repair or restart was scheduled. Journal: " + record.JournalPath);
                SetScanLight(review.EvidenceMatched ? "attention" : "incomplete", review.NextAction);
                return;
            }
            if (kind == WorkflowKind.Storage && StorageOptimizationReview.IsChecked == true)
            {
                string volume = storageVolume ?? StorageDiagnostics.SystemVolume();
                var modeItem = StorageOptimizationModeChoice.SelectedItem as System.Windows.Controls.ComboBoxItem;
                if (modeItem?.Tag is not string modeName || !Enum.TryParse<VolumeOptimizationMode>(modeName, out var optimizationMode))
                    throw new InvalidOperationException("Choose a drive optimization method.");
                var request = VolumeOptimization.CreateRequest(volume, optimizationMode);
                string optimizationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "VolumeOptimization");
                var optimizationHost = new WindowsVolumeOptimizationHost();
                VolumeOptimizationResult optimization;
                if (File.Exists(Path.Combine(optimizationFolder, "volume-optimization.pending.json")))
                    optimization = await Task.Run(() => VolumeOptimization.ReconcileAsync(optimizationFolder, optimizationHost, progress, stop.Token));
                else if (autoRepair)
                    optimization = await Task.Run(() => VolumeOptimization.RunAsync(request, true, optimizationFolder, optimizationHost, progress, stop.Token));
                else
                    optimization = await Task.Run(() => VolumeOptimization.ReviewAsync(request, optimizationHost, progress, stop.Token));
                string output = "DRIVE OPTIMIZATION / " + optimization.Outcome +
                    "\nVolume: " + optimization.Request.Volume + "\nMethod: " + optimization.Request.Mode +
                    "\nMedia: " + (optimization.BeforeState?.Media.ToString() ?? "Unavailable") +
                    "\nFilesystem: " + (optimization.BeforeState?.FileSystem ?? "Unavailable") +
                    "\n\n" + optimization.Detail +
                    "\n\nJournal: " + (optimization.JournalPath ?? "No change journal created.") +
                    "\nKillerBench did not alter the Optimize Drives schedule or restart Windows.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Drive optimization " + DateTime.Now.ToString("HHmmss"), output.Split('\n'), false));
                Report(optimization.Detail);
                SetScanLight(optimization.Outcome is VolumeOptimizationOutcome.Verified or VolumeOptimizationOutcome.Preview ? "ready" : "attention", optimization.Detail);
                return;
            }
            if (previousWindowsRemoval)
            {
                string previousFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "PreviousWindowsInstallation");
                var previousHost = new WindowsPreviousWindowsInstallationHost();
                PreviousWindowsRemovalResult? removal = null;
                PreviousWindowsReconciliationResult? recovery = null;
                if (File.Exists(Path.Combine(previousFolder, PreviousWindowsInstallationRemoval.PendingFileName)))
                    recovery = await Task.Run(() => PreviousWindowsInstallationRemoval.ReconcileAsync(previousFolder,
                        previousHost, stop.Token));
                else
                    removal = await Task.Run(() => PreviousWindowsInstallationRemoval.RunAsync(
                        CleanupRemove.IsChecked == true, CleanupPreviousWindowsBackup.IsChecked == true,
                        previousFolder, previousHost, progress, stop.Token));
                string outcome = recovery?.Outcome.ToString() ?? removal!.Outcome.ToString();
                string detail = recovery?.Detail ?? removal!.Detail;
                string journal = recovery?.ArchivePath ?? recovery?.PendingPath ?? removal?.JournalPath ?? "No change journal created.";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("Previous Windows installation " + DateTime.Now.ToString("HHmmss"),
                    ("PREVIOUS WINDOWS INSTALLATION / " + outcome.ToUpperInvariant() + "\n\n" + detail +
                    "\n\nJournal: " + journal +
                    "\nKillerBench did not recursively delete Windows.old and did not restart Windows.").Split('\n'), false));
                Report(detail);
                SetScanLight(outcome is "Preview" or "Verified" or "NotChanged" or "NoPendingRecord" ? "ready" : "attention", detail);
                return;
            }
            if (kind == WorkflowKind.Cleanup && CleanupRemove.IsChecked == true)
            {
                var removal = await Task.Run(() => CleanupRemoval.RunAsync(cleanupOptions, folder, progress, stop.Token));
                var output = new TextEvidence("Cleanup " + DateTime.Now.ToString("HHmmss"), removal.Format().Split('\n'), false);
                workflowOutputActive = false;
                RegisterSource(output);
                Report(removal.Status + ". Journal: " + removal.JournalPath);
                SetScanLight(removal.Status == "Finished" ? "ready" : "incomplete", removal.Status + ". Review cleanup results.");
                return;
            }
            if (kind == WorkflowKind.Orca && autoRepair)
            {
                string orcaFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "Workflows");
                OrcaCacheResetResult? reset = null;
                OrcaCacheReconciliationResult? recovery = null;
                if (File.Exists(Path.Combine(orcaFolder, "orca-cache-reset.pending.json")))
                    recovery = await Task.Run(() => OrcaCacheReset.ReconcileAsync(orcaFolder,
                        new WindowsOrcaCacheResetHost(), stop.Token));
                else
                    reset = await Task.Run(() => OrcaCacheReset.RunAsync(orcaOptions, true, orcaFolder,
                        new WindowsOrcaCacheResetHost(), progress, stop.Token));
                string outcome = reset?.Outcome.ToString() ?? recovery!.Outcome.ToString();
                string detail = reset?.Detail ?? recovery!.Detail;
                string targets = reset?.Targets is { Count: > 0 }
                    ? "\nCaches: " + string.Join(", ", reset.Targets.Select(target => target.Label))
                    : "";
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("ORCA cache reset " + DateTime.Now.ToString("HHmmss"),
                    ("ORCA / OUTLOOK CACHE RESET\nOutcome: " + outcome + targets + "\n\n" + detail +
                    "\n\nRetained cache locations are recorded only in the local recovery journal.").Split('\n'), false));
                Report(detail);
                SetScanLight(outcome is "Verified" or "NotChanged" or "NoPendingRecord" ? "ready" : "attention", detail);
                return;
            }
            var host = new WindowsWorkflowHost { NetworkTimeout = networkTimeout, BitsAllUsers = bitsAllUsers, BitsSelectedJobId = BitsSelection.SelectedItem is Guid selectedBitsJob ? selectedBitsJob : null, TimeOptions = timeOptions, ApplicationScope = applicationScope, MaceOptions = maceOptions, OrcaOptions = orcaOptions, DepotOptions = depotOptions, BeamOptions = beamOptions, DebloatOptions = debloatOptions, IncludeProvisionedPackages = includeProvisioned, PackageScope = packageScope, NetworkEndpoint = networkEndpoint, NetworkLookupHost = networkLookupHost, MsiProductCode = msiProductCode, ProposedComputerName = proposedName, StorageVolume = storageVolume, CheckProfileDependencies = profileDependencies, ProfileMinimumDays = profileDays, CleanupOptions = cleanupOptions, SecurityOptions = securityOptions, AudioEndpointSelection = audioSelection, UpdateOptions = updateOptions, IncludeThirdPartyUpdates = UpdateThirdParty.IsChecked == true, PrivacyOptions = privacyOptions, BitLockerOptions = bitlockerOptions, BitLockerRecoveryProtector = string.Equals(protectorVolume, bitlockerOptions.Volume, StringComparison.OrdinalIgnoreCase) && BitLockerProtectorChoice.SelectedItem is Guid selectedProtector ? selectedProtector : null, EventChannel = selectedEventChannel, KnowledgeCatalogs = sessionKnowledgeCatalogs, Progress = progress };
            if (kind == WorkflowKind.AppPackages) SetAppPackageInventory(null);
            if (kind == WorkflowKind.InstalledApps) SetApplicationInventory(null);
            if (kind == WorkflowKind.Mace) SetMaceReview(null);
            if (kind == WorkflowKind.BackgroundTransfers) SetBitsInventory(null);
            var result = await Task.Run(() => Workflows.RunAsync(kind, autoRepair, folder, progress, stop.Token, host, selectedRecipes, diagnosticScopes));
            if (kind == WorkflowKind.BackgroundTransfers) SetBitsInventory(bitsAllUsers == (BitsScopeChoice.SelectedIndex == 1) ? host.LastBitsInventory : null);
            if (kind == WorkflowKind.AppPackages) SetAppPackageInventory(
                packageScope == (AppPackageScopeChoice.SelectedIndex == 1 ? AppPackageScope.AllUsers : AppPackageScope.CurrentAccount)
                    ? host.LastAppPackageInventory : null);
            if (kind == WorkflowKind.InstalledApps) SetApplicationInventory(
                applicationScope == (InstalledApplicationScopeChoice.SelectedIndex == 1 ? InstalledApplicationScope.CurrentAccount : InstalledApplicationScope.Machine)
                    ? host.LastApplicationInventory : null);
            if (kind == WorkflowKind.Mace) SetMaceReview(host.LastMaceReview?.Options == maceOptions ? host.LastMaceReview : null);
            if (kind == WorkflowKind.Profiles)
            {
                profileInventory = host.LastProfileInventory;
                profileDependenciesResult = host.LastProfileDependencies;
                ProfileSelection.ItemsSource = profileInventory?.Profiles;
            }
            RegisterWorkflowResult(result);
            if (kind == WorkflowKind.Installer && autoRepair)
            {
                string registrationFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "InstallerRegistrationRepair");
                string registrationPending = Path.Combine(registrationFolder,
                    "installer-registration-repair.pending.json");
                var registrationAction = InstallerRegistrationRepair.SelectAction(autoRepair,
                    File.Exists(registrationPending), result.Checks);
                if (registrationAction != InstallerRegistrationRepairAction.None)
                {
                    BeginWorkflowOutput(kind);
                    AppendWorkflowOutput(registrationAction == InstallerRegistrationRepairAction.Reconcile
                        ? "WINDOWS INSTALLER REGISTRATION / RECONCILING INTERRUPTED OPERATION"
                        : "WINDOWS INSTALLER REGISTRATION / CONFIRMED DAMAGE");
                    string title;
                    string detail;
                    string record;
                    bool verified;
                    if (registrationAction == InstallerRegistrationRepairAction.Reconcile)
                    {
                        var reconciliation = await Task.Run(() =>
                            InstallerRegistrationRepairReconciliation.RunAsync(registrationFolder,
                                new WindowsInstallerRegistrationRepairHost(), progress, stop.Token));
                        title = "WINDOWS INSTALLER REGISTRATION / RECONCILIATION " + reconciliation.Outcome;
                        detail = reconciliation.Detail;
                        record = reconciliation.ArchivePath ?? reconciliation.PendingPath;
                        verified = reconciliation.Outcome == InstallerRegistrationRepairReconciliationOutcome.Verified;
                    }
                    else
                    {
                        var registration = await Task.Run(() => InstallerRegistrationRepair.RunAsync(true,
                            registrationFolder, new WindowsInstallerRegistrationRepairHost(), progress, stop.Token));
                        title = "WINDOWS INSTALLER REGISTRATION / " + registration.Outcome;
                        detail = registration.Detail;
                        record = registration.JournalPath ?? "No registration journal created.";
                        verified = registration.Outcome == InstallerRegistrationRepairOutcome.Verified;
                    }
                    string registrationOutput = title + "\n" + detail + "\nRecord: " + record +
                        "\n\nThe Installer engine was never unregistered. Packages and user data were not selected.";
                    workflowOutputActive = false;
                    RegisterSource(new TextEvidence("Installer registration " +
                        DateTime.Now.ToString("HHmmss"), registrationOutput.Split('\n'), false));
                    Report(detail);
                    SetScanLight(verified ? "ready" : "attention", detail);
                    return;
                }
            }
            if (kind == WorkflowKind.Installer && autoRepair && host.LastMsiProduct is not null &&
                MsiProductDiagnostics.TryGetReportedState(msiProductCode!, host.LastMsiProduct, out int msiState) &&
                msiState is 0 or -6)
            {
                string msiFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KillerBench", "MsiRepair");
                if (File.Exists(Path.Combine(msiFolder, "msi-repair.pending.json")))
                {
                    var reconciliation = await Task.Run(() => MsiRepairReconciliation.RunAsync(msiFolder,
                        new WindowsMsiRepairHost(), progress, stop.Token));
                    string reviewOutput = "MSI PRODUCT REPAIR / INTERRUPTED OPERATION\nOutcome: " + reconciliation.Outcome +
                        "\n" + reconciliation.Detail + "\nRecord: " + (reconciliation.ArchivePath ?? reconciliation.PendingPath);
                    workflowOutputActive = false;
                    RegisterSource(new TextEvidence("MSI repair review " + DateTime.Now.ToString("HHmmss"),
                        reviewOutput.Split('\n'), false));
                    Report(reconciliation.Detail);
                    SetScanLight(reconciliation.Outcome == MsiRepairReconciliationOutcome.Verified ? "ready" : "attention",
                        reconciliation.Detail);
                    return;
                }
                var msiRepair = await Task.Run(() => MsiRepair.RunAsync(msiProductCode!, true, msiFolder,
                    new WindowsMsiRepairHost(), progress, stop.Token));
                string msiOutput = "MSI PRODUCT REPAIR\nProduct: " + msiRepair.ProductCode +
                    "\nOutcome: " + msiRepair.Outcome + "\n" + msiRepair.Detail +
                    "\nJournal: " + (msiRepair.JournalPath ?? "No repair journal created.");
                workflowOutputActive = false;
                RegisterSource(new TextEvidence("MSI repair " + DateTime.Now.ToString("HHmmss"), msiOutput.Split('\n'), false));
                Report(msiRepair.Detail);
                SetScanLight(msiRepair.Outcome == MsiRepairOutcome.Verified ? "ready" : "attention", msiRepair.Detail);
                return;
            }
            CaptureInfo.Text = result.Status + " | " + result.Checks.Count + " checks | " + result.Repairs.Count + " repairs";
            Report(result.Status + ". Journal: " + result.JournalPath);
            SetScanLight(result.GetExitCode() == 0 ? "ready" : result.Status == "Needs attention" ? "attention" : "incomplete", result.Status + ". Applies only to checked scopes.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or OperationCanceledException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            string detail = "Workflow not completed: " + error.Message;
            AppendWorkflowOutput(detail);
            string retained = EvidenceBox.Text;
            workflowOutputActive = false;
            string guidance = autoRepair || applyTime || applyPrivacyClipboardHistory || applyPrivacyBoolean || CleanupRemove.IsChecked == true
                ? "Repair completion is unverified. Review the retained output and operation journal before another attempt."
                : "Diagnostic coverage is incomplete. This result does not establish current system health.";
            RegisterSource(new TextEvidence(kind + " interruption " + DateTime.Now.ToString("HHmmss"),
                ("WORKFLOW / OUTCOME UNVERIFIED\n" + detail + "\n" + guidance + "\n\nRETAINED LIVE OUTPUT\n" + retained).Split('\n'), false));
            SetScanLight("incomplete", detail);
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            repairRunning = false;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            RepairConsent.IsChecked = false;
            CleanupRemove.IsChecked = false;
            PrivacyClipboardHistoryApply.IsChecked = false;
            PrivacyBooleanApply.IsChecked = false;
            ModeLabel.Text = "WORKFLOW ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private static uint PowerMinutes(System.Windows.Controls.ComboBox box) =>
        box.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is string value &&
        uint.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out uint minutes)
            ? minutes : throw new InvalidOperationException("Select a valid power timeout.");
}
