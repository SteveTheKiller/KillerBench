using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public sealed record DesktopWorkflowOptionBinding(string Key, string ControlName, bool Inverted = false, bool Aggregate = false);

public static class DesktopWorkflowOptionBindings
{
    public static IReadOnlyDictionary<WorkflowKind, IReadOnlyList<DesktopWorkflowOptionBinding>> All { get; } =
        new Dictionary<WorkflowKind, IReadOnlyList<DesktopWorkflowOptionBinding>>
        {
            [WorkflowKind.Network] = Bindings(
                ("lookupHost", "NetworkLookupHost"), ("tcpHost", "NetworkTcpHost"),
                ("tcpPort", "NetworkTcpPort"), ("timeoutSeconds", "NetworkTimeoutSeconds")),
            [WorkflowKind.Audio] = Bindings(
                ("direction", "AudioDirectionChoice"), ("role", "AudioRoleChoice")),
            [WorkflowKind.SystemRestore] = Bindings(
                ("prepare", "SystemRestoreCreate"), ("description", "SystemRestoreDescription"),
                ("reason", "SystemRestoreReasonChoice"), ("execute", "MainAutoRepair")),
            [WorkflowKind.Storage] = Bindings(
                ("volume", "StorageVolumeChoice"), ("repairReview", "StorageRepairReview"),
                ("repairMode", "StorageRepairModeChoice"), ("allowDismount", "StorageAllowDismount"),
                ("backupConfirmed", "StorageBackupConfirmed"), ("optimizationReview", "StorageOptimizationReview"),
                ("optimizationMode", "StorageOptimizationModeChoice"), ("execute", "MainAutoRepair")),
            [WorkflowKind.Printing] = [
                new("queue", "PrinterQueues"), new("clearOffline", "MainAutoRepair"),
                new("makeDefault", "PrinterMakeDefault"), new("jobQueue", "PrintJobChoice", Aggregate: true),
                new("jobId", "PrintJobChoice", Aggregate: true), new("cancelJob", "PrintJobCancelConsent")
            ],
            [WorkflowKind.Installer] = Bindings(
                ("scope", "InstallerProductScopeChoice"), ("productCode", "InstallerProductCode"),
                ("execute", "RepairConsent")),
            [WorkflowKind.TimeService] = Bindings(
                ("source", "TimeSourceChoice"), ("peers", "TimePeers"), ("execute", "TimeApply")),
            [WorkflowKind.EventLog] = Bindings(
                ("channel", "EventChannelChoice"), ("execute", "EventChannelEnableConsent")),
            [WorkflowKind.Profiles] = Bindings(
                ("minimumInactiveDays", "ProfileAge"), ("checkDependencies", "ProfileDependencyCheck")),
            [WorkflowKind.ComputerName] = Bindings(
                ("proposedName", "ProposedComputerName"), ("execute", "ComputerRenameConsent")),
            [WorkflowKind.BackgroundTransfers] = Bindings(("scope", "BitsScopeChoice")),
            [WorkflowKind.AppPackages] = Bindings(
                ("scope", "AppPackageScopeChoice"), ("includeProvisioned", "IncludeProvisionedPackages")),
            [WorkflowKind.InstalledApps] = Bindings(("scope", "InstalledApplicationScopeChoice")),
            [WorkflowKind.Search] = Bindings(("rebuildIndex", "SearchIndexRebuildConsent")),
            [WorkflowKind.ComponentCleanup] = Bindings(("execute", "MainAutoRepair")),
            [WorkflowKind.WindowsRecovery] = Bindings(("execute", "MainAutoRepair")),
            [WorkflowKind.FirewallServices] = Bindings(
                ("profile.domain", "FirewallDomainProfile"), ("profile.private", "FirewallPrivateProfile"),
                ("profile.public", "FirewallPublicProfile"), ("execute", "FirewallProfileApply")),
            [WorkflowKind.Cleanup] = Bindings(
                ("target.userTemp", "CleanupUserTemp"), ("target.windowsTemp", "CleanupWindowsTemp"),
                ("target.systemTemp", "CleanupSystemTemp"),
                ("target.shaderCaches", "CleanupShaderCaches"), ("target.browserCaches", "CleanupBrowserCaches"),
                ("target.errorReports", "CleanupErrorReports"), ("target.crashDumps", "CleanupCrashDumps"),
                ("target.windowsMinidumps", "CleanupWindowsMinidumps"), ("target.officeCaches", "CleanupOfficeCaches"),
                ("target.deliveryOptimizationCache", "CleanupDeliveryOptimization"), ("target.recycleBin", "CleanupRecycleBin"),
                ("target.thumbnailCache", "CleanupThumbnailCache"), ("target.iconCache", "CleanupIconCache"),
                ("minimumAgeDays", "CleanupAge"), ("maximumFilesPerTarget", "CleanupLimit"), ("execute", "CleanupRemove")),
            [WorkflowKind.AvailableUpdates] = Bindings(
                ("software", "UpdateSoftware"), ("drivers", "UpdateDrivers"),
                ("thirdPartyApplications", "UpdateThirdParty"), ("source", "UpdateSourceChoice")),
            [WorkflowKind.Privacy] = [.. WorkflowOptionCatalog.ForWorkflow(WorkflowKind.Privacy)!.Options
                .Select(option => new DesktopWorkflowOptionBinding(option.Key, "PrivacyAreas", Aggregate: true))],
            [WorkflowKind.BitLocker] = Bindings(
                ("volume", "BitLockerVolume"), ("operation", "BitLockerOperationChoice"),
                ("encryption", "BitLockerEncryptionChoice"), ("usedSpaceOnly", "BitLockerUsedSpace"),
                ("backup", "BitLockerDestination"), ("backupFile", "BitLockerBackupPath"),
                ("protectorId", "BitLockerProtectorChoice"), ("executeBackup", "BitLockerBackupConsent"),
                ("executeEncryption", "BitLockerEncryptConsent"), ("executeResume", "BitLockerResumeConsent")),
            [WorkflowKind.Defender] = Bindings(
                ("scanType", "DefenderScanChoice"), ("startScan", "DefenderScanConsent")),
            [WorkflowKind.Mace] = [
                new("target.oneDrive", "MaceOneDrive"), new("target.outlook", "MaceOutlook"),
                new("target.office", "MaceOffice"), new("target.project", "MaceProject"), new("target.teams", "MaceTeams"),
                new("scope", "MaceScopeChoice"), new("preserveApplicationData", "MaceRemoveData", Inverted: true),
                new("preserveCachedCredentials", "MaceRemoveCredentials", Inverted: true),
                new("restoreOneDriveFolderLocations", "MaceRestoreFolders")
            ],
            [WorkflowKind.Orca] = Bindings(
                ("mode", "OrcaModeChoice"), ("scope", "OrcaScopeChoice"),
                ("action.clearCaches", "OrcaClearCaches"), ("action.resetProfileSettings", "OrcaResetProfiles"),
                ("action.resetAuthentication", "OrcaResetAuthentication"), ("action.deleteOstFiles", "OrcaDeleteOst"),
                ("action.removeAddIns", "OrcaRemoveAddIns"), ("action.reinstallNewOutlook", "OrcaReinstallNew")),
            [WorkflowKind.Depot] = Bindings(
                ("application.microsoft365", "DepotMicrosoft365"), ("application.oneDrive", "DepotOneDrive"),
                ("application.teams", "DepotTeams"), ("application.chrome", "DepotChrome"),
                ("application.acrobatReader", "DepotAcrobat"), ("application.zoom", "DepotZoom"),
                ("application.sevenZip", "DepotSevenZip"), ("configuration.windowsUpdates", "DepotUpdates"),
                ("configuration.computerName", "DepotComputerName"), ("configuration.localAdministrator", "DepotLocalAdmin"),
                ("configuration.administratorReview", "DepotAdminReview"), ("configuration.browserExtensions", "DepotExtensions"),
                ("configuration.cleanup", "DepotCleanup"), ("configuration.edge", "DepotEdge"),
                ("configuration.privacy", "DepotPrivacy"), ("configuration.taskbar", "DepotTaskbar"),
                ("configuration.startMenu", "DepotStartMenu"), ("configuration.notifications", "DepotNotifications")),
            [WorkflowKind.Debloat] = Bindings(
                ("area.recallAndAi", "DebloatRecallAi"), ("area.telemetryAndReporting", "DebloatTelemetry"),
                ("area.searchAndSuggestions", "DebloatSearch"), ("area.consumerContent", "DebloatConsumer"),
                ("area.taskbarAndStart", "DebloatStart"), ("area.notifications", "DebloatNotifications"),
                ("area.edge", "DebloatEdge"), ("area.highPerformancePower", "DebloatPower"),
                ("area.sleepAndDisplayTimers", "DebloatTimers"), ("area.hibernation", "DebloatHibernation"),
                ("area.officeLanguages", "DebloatOfficeLanguages"), ("area.hpSoftware", "DebloatHp"),
                ("area.dellSoftware", "DebloatDell"), ("area.asusAcerSoftware", "DebloatAsusAcer"),
                ("area.lenovoSoftware", "DebloatLenovo"), ("area.thirdPartyApps", "DebloatThirdParty"),
                ("area.vendorData", "DebloatVendorData"), ("userScope", "DebloatScopeChoice"),
                ("retainedOfficeCulture", "DebloatOfficeCulture"), ("sleepAcMinutes", "DebloatSleepAc"),
                ("sleepDcMinutes", "DebloatSleepDc"), ("displayAcMinutes", "DebloatDisplayAc"),
                ("displayDcMinutes", "DebloatDisplayDc"), ("applyHighPerformance", "DebloatActionChoice"),
                ("applyPowerTimers", "DebloatActionChoice"), ("disableHibernation", "DebloatActionChoice")),
            [WorkflowKind.ServicingActivity] = Bindings(
                ("refreshSeconds", "ServicingMonitorRefresh"), ("samples", "ServicingMonitorSamples"),
                ("includeEvents", "ServicingMonitorEvents"), ("includeHistory", "ServicingMonitorHistory"),
                ("includeServices", "ServicingMonitorServices"), ("includeRestart", "ServicingMonitorRestart"),
                ("includeSetup", "ServicingMonitorSetup"), ("includeCompatibility", "ServicingMonitorCompatibility"),
                ("includeReadiness", "ServicingMonitorReadiness"), ("includeTransfers", "ServicingMonitorTransfers"),
                ("includeDeliveryOptimization", "ServicingMonitorDeliveryOptimization"),
                ("includeInstallerActivity", "ServicingMonitorInstaller"),
                ("includeScheduledTask", "ServicingMonitorScheduledTask"))
        };

    private static DesktopWorkflowOptionBinding[] Bindings(params (string Key, string Control)[] values) =>
        [.. values.Select(value => new DesktopWorkflowOptionBinding(value.Key, value.Control))];
}

public partial class MainWindow
{
    private void ApplyWorkflowOptionCatalog()
    {
        foreach ((WorkflowKind kind, IReadOnlyList<DesktopWorkflowOptionBinding> bindings) in DesktopWorkflowOptionBindings.All)
        {
            WorkflowOptionSet options = WorkflowOptionCatalog.ForWorkflow(kind)
                ?? throw new InvalidOperationException("Missing workflow options for " + kind + ".");
            foreach (DesktopWorkflowOptionBinding binding in bindings)
            {
                WorkflowOptionDefinition option = options.Options.Single(item => item.Key == binding.Key);
                FrameworkElement control = FindName(binding.ControlName) as FrameworkElement
                    ?? throw new InvalidOperationException("Missing workflow option control " + binding.ControlName + ".");
                ApplyWorkflowOption(control, option, binding);
            }
        }
    }

    private static void ApplyWorkflowOption(FrameworkElement control, WorkflowOptionDefinition option,
        DesktopWorkflowOptionBinding binding)
    {
        if (!binding.Aggregate)
        {
            switch (control)
            {
                case CheckBox checkBox when bool.TryParse(option.DefaultValue, out bool value):
                    checkBox.IsChecked = binding.Inverted ? !value : value;
                    break;
                case Slider slider when double.TryParse(option.DefaultValue, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out double value):
                    if (option.Minimum is int minimum) slider.Minimum = minimum;
                    if (option.Maximum is int maximum) slider.Maximum = maximum;
                    if (option.Step is int step) slider.TickFrequency = step;
                    slider.Value = value;
                    break;
                case TextBox textBox:
                    textBox.Text = option.DefaultValue;
                    break;
                case ComboBox comboBox when option.Choices is not null:
                    int index = option.Choices.ToList().FindIndex(choice => choice.Value == option.DefaultValue);
                    if (index >= 0 && index < comboBox.Items.Count) comboBox.SelectedIndex = index;
                    break;
            }
        }

        control.ToolTip ??= option.Description;
        System.Windows.Data.BindingOperations.SetBinding(control, AutomationProperties.HelpTextProperty,
            new System.Windows.Data.Binding(nameof(FrameworkElement.ToolTip)) { Source = control });
    }
}
