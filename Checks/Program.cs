using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KillerBench.Engine;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // The family theme engine loads its palettes by application-relative pack URIs, which
        // resolve against the app assembly only when it is the resource assembly; and every
        // harness takes the app's theme hooks and first-run defaults, as App.OnStartup does.
        typeof(Application).GetField("_resourceAssembly", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(null, typeof(KillerBench.App).Assembly);
        KillerBench.Services.AppTheme.Configure();
        WindowsEngine.Register();
        try
        {
            if (args is ["--only", "desktop-state"]) return DesktopOperationStateChecks.Run(args);
            if (args is ["--only", "capability-desktop"]) return CapabilityDesktopChecks.Run();
            if (args is ["--only", "picker-no-window"]) return FilePickerListingChecks.Run();
            if (args is ["--only", "repair-capabilities"])
            {
                RepairCapabilityChecks.RunAsync(Path.GetTempPath()).GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "portable-publication"])
            {
                PortablePublicationChecks.Run(Path.GetTempPath());
                return 0;
            }
            if (args is ["--only", "memory-recovery"])
            {
                MemoryDiagnosticRecoveryChecks.RunAsync(Path.GetTempPath()).GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "restart-recovery"])
            {
                RestartRecoveryChecks.RunAsync(Path.GetTempPath()).GetAwaiter().GetResult();
                return 0;
            }
            if (RecipeOperationChecks.TryRunChildAsync(args).GetAwaiter().GetResult()) return 0;
            if (args is ["--isolated-import-reports"]) return ImportedReportUiChecks.Run();
            if (args is ["--only", "import-reports"]) return IsolatedDesktop.Run("--isolated-import-reports");
            if (args is ["--isolated-content-scale"]) return ImportedReportUiChecks.Run(verifyScaling: true);
            if (args is ["--only", "content-scale"]) return IsolatedDesktop.Run("--isolated-content-scale");
            if (args is ["--isolated-ui-controls"]) return RunChecks(includeUi: true, skipNonUi: true);
            if (args is ["--only", "ui-controls"]) return IsolatedDesktop.Run("--isolated-ui-controls");
            if (args is ["--only", "aggregate-cancellation"])
            {
                AggregateCancellationChecks.RunAsync(Path.GetTempPath()).GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "goal-options"])
            {
                GoalOptionValidationChecks.RunAsync().GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "scenario-restoration"])
            {
                NativeScenarioRestorationChecks.RunAsync().GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "malformed-input"])
            {
                string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-checks-" + Guid.NewGuid().ToString("N"));
                try { MalformedInputChecks.RunAsync(scratch).GetAwaiter().GetResult(); }
                finally { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }
                return 0;
            }
            if (args is ["--only", "truncated-evidence"])
            {
                WindowsTruncatedEvidenceChecks.Run();
                return 0;
            }
            if (args is ["--only", "powershell-binding"])
            {
                WindowsPowerShellBindingChecks.RunAsync().GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "command-path"])
            {
                CommandLinePathChecks.Run();
                return 0;
            }
            if (args is ["--only", "installer-runtime"])
            {
                InstallerRuntimeChecks.Run();
                return 0;
            }
            if (args is ["--only", "cli-entry"])
            {
                using var output = new StringWriter();
                var previous = Console.Out;
                try
                {
                    Console.SetOut(output);
                    int code = KillerBench.Cli.CommandEngine.RunAsync(["--help"]).GetAwaiter().GetResult();
                    if (code != 0 || !output.ToString().Contains("workflow"))
                        throw new InvalidOperationException("The shared command engine must return help without opening a window.");
                }
                finally { Console.SetOut(previous); }
                Console.WriteLine("PASS: the shared command engine lists commands without desktop startup.");
                return 0;
            }
            if (args.Length == 0) return RunChecks(includeUi: false);
            if (args is ["--isolated-ui"]) return RunChecks(includeUi: true);
            if (args is ["--isolated-ui-smoke"]) return RunUiSmoke();
            if (args is ["--isolated-category-ui"]) return CategoryNavigationChecks.Run();
            if (args is ["--isolated-dialog-smoke"]) return RunDialogSmoke();
            if (args is ["--isolated-demo-screenshots"]) return DemoScreenshotChecks.Run();
            if (args is ["--only", "menu-separators"]) return RunMenuSeparatorChecks();
            if (args is ["--only", "theme-dump"]) return ThemeDumpChecks.Run();
            if (args is ["--only", "workflow-report-export"])
            {
                WorkflowReportExportChecks.Run();
                return 0;
            }
            if (args is ["--only", "workflow-options"])
            {
                WorkflowOptionCatalogChecks.Run();
                return 0;
            }
            if (args is ["--only", "servicing-monitor"])
            {
                ServicingActivityMonitorChecks.RunAsync().GetAwaiter().GetResult();
                return 0;
            }
            if (args is ["--only", "recipes"])
            {
                string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-checks-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(scratch);
                try { RecipeChecks.RunAsync(scratch).GetAwaiter().GetResult(); }
                finally { Directory.Delete(scratch, true); }
                return 0;
            }
            if (args is ["--only", "native-validation"])
            {
                NativeWindowsProductMetadataChecks.Run();
                NativeValidationCatalogChecks.Run();
                NativeValidationEvidenceChecks.Run();
                NativeValidationReportChecks.Run();
                return 0;
            }
            if (args is ["--only", "ui-smoke"])
                return IsolatedDesktop.Run("--isolated-ui-smoke");
            if (args is ["--only", "ui-category"])
                return IsolatedDesktop.Run("--isolated-category-ui");
            if (args is ["--only", "dialog-smoke"])
                return IsolatedDesktop.Run("--isolated-dialog-smoke");
            if (args is ["--only", "demo-screenshots"])
                return IsolatedDesktop.Run("--isolated-demo-screenshots");
            if (args is ["--only", "ui-full"])
                return IsolatedDesktop.Run();
            if (args is ["--only", "workflow"])
            {
                string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-checks-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(scratch);
                try { WorkflowChecks.RunAsync(scratch).GetAwaiter().GetResult(); }
                finally { Directory.Delete(scratch, true); }
                return 0;
            }
            throw new ArgumentException("Use no arguments for all non-UI checks, or --only workflow-report-export, workflow-options, servicing-monitor, recipes, native-validation, workflow, dialog-smoke, demo-screenshots, ui-smoke, ui-category or ui-full. UI checks require an isolated desktop.");
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static int RunMenuSeparatorChecks()
    {
        var application = new Application();
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Black.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative) });
        foreach (var theme in Enum.GetValues<KillerBench.Services.Theme>())
        {
            KillerBench.Services.ThemeManager.Apply(theme);
            CheckMenuSeparators(theme);
        }
        Console.WriteLine("PASS: Menu separator brushes and unchanged general separators across all 13 themes.");
        return 0;
    }

    private static void CheckMenuSeparators(KillerBench.Services.Theme theme)
    {
        var application = Application.Current;
        var menuBrush = (SolidColorBrush)application.FindResource("MenuSeparatorBrush");
        var expected = theme switch
        {
            KillerBench.Services.Theme.Dark => Color.FromRgb(0x2E, 0x2E, 0x2E),
            KillerBench.Services.Theme.Light => Color.FromRgb(0xC0, 0xC0, 0xC0),
            KillerBench.Services.Theme.Black => Color.FromRgb(0x2A, 0x2A, 0x2A),
            KillerBench.Services.Theme.SE98 => Color.FromRgb(0x80, 0x80, 0x80),
            KillerBench.Services.Theme.Blood => Color.FromRgb(0x40, 0x1D, 0x1D),
            KillerBench.Services.Theme.Greed => Color.FromRgb(0x0F, 0x4A, 0x30),
            KillerBench.Services.Theme.Cyanotic => Color.FromRgb(0x18, 0x34, 0x50),
            KillerBench.Services.Theme.Ectoplasm => Color.FromRgb(0x10, 0x9F, 0xA3),
            KillerBench.Services.Theme.Decay => Color.FromRgb(0x6B, 0x70, 0x5C),
            KillerBench.Services.Theme.Malaise => Color.FromRgb(0x4A, 0x51, 0x54),
            KillerBench.Services.Theme.Sepulchre => Color.FromRgb(0x83, 0x5A, 0x30),
            KillerBench.Services.Theme.Delirium => Color.FromRgb(0x66, 0x66, 0x66),
            KillerBench.Services.Theme.Mourning => Color.FromRgb(0x75, 0x6B, 0x79),
            _ => throw new ArgumentOutOfRangeException(nameof(theme))
        };
        Require(menuBrush.Color == expected, "Menu separator color matches the KillerUI theme");
        var menuSeparator = new Separator { Style = (Style)application.FindResource(MenuItem.SeparatorStyleKey) };
        Require(ReferenceEquals(menuSeparator.Background, menuBrush), "Menu separator style uses its dedicated brush");
        var generalSeparator = new Separator { Style = (Style)application.FindResource(typeof(Separator)) };
        Require(ReferenceEquals(generalSeparator.Background, application.FindResource("MenuBorderBrush")),
            "Non-menu separators keep their existing border brush");
    }

    private static int RunDialogSmoke()
    {
        IsolatedDesktop.Verify();
        var application = new Application();
        SynchronizationContext.SetSynchronizationContext(
            new System.Windows.Threading.DispatcherSynchronizationContext(application.Dispatcher));
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Black.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Instrument.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Strings/en-US.xaml", UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var owner = new KillerBench.MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowActivated = false,
            ShowInTaskbar = false,
            Left = SystemParameters.VirtualScreenLeft - 2000,
            Top = SystemParameters.VirtualScreenTop
        };
        string captures = Path.Combine(Path.GetTempPath(), "KillerBench-dialogs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(captures);
        try
        {
            owner.Show();
            PickerDialogChecks.Run();
            foreach (var theme in Enum.GetValues<KillerBench.Services.Theme>())
            {
                KillerBench.Services.ThemeManager.Apply(theme);
                var dialog = new KillerBench.Controls.ConfirmDialog(
                    "Remove this exact current-account package?", "Fixture.Package_1\n\nSafeguards run again before removal.",
                    "Remove package") { Owner = owner };
                dialog.Show();
                Pump();
                Require(dialog.FindName("RootBorder") is KillerBench.Controls.PickerBorder { Child.Clip: not null },
                    "Dialog content is clipped to the card corners");
                Require(dialog.FindName("CancelButton") is Button { IsDefault: true, IsCancel: true },
                    "Cancel is the default keyboard action");
                Capture(dialog, Path.Combine(captures, theme + ".png"));
                ((Button)dialog.FindName("CloseButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(!dialog.Confirmed && !dialog.IsVisible, "Close never confirms removal");
            }
            var cancel = new KillerBench.Controls.ConfirmDialog("Confirm", "Fixture", "Remove") { Owner = owner };
            cancel.Show();
            ((Button)cancel.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(!cancel.Confirmed && !cancel.IsVisible, "Cancel never confirms removal");
            var remove = new KillerBench.Controls.ConfirmDialog("Confirm", "Fixture", "Remove") { Owner = owner };
            remove.Show();
            ((Button)remove.FindName("OkButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(remove.Confirmed && !remove.IsVisible, "Explicit Remove is the only confirmation path");
            Require(remove.FindName("Check1") is CheckBox { Visibility: Visibility.Collapsed },
                "A confirmation without a choice shows no checkbox");
            var choice = new KillerBench.Controls.ConfirmDialog("KillerBench v9.9.9 is available", "Fixture", "Update",
                "Not now", "Always check on startup", true) { Owner = owner };
            choice.Show();
            Pump();
            Require(choice.FindName("Check1") is CheckBox { Visibility: Visibility.Visible, IsChecked: true,
                    Content: TextBlock { Text: "Always check on startup" } } && choice.Check1Checked,
                "The update prompt's checkbox shows its label and the saved choice");
            ((CheckBox)choice.FindName("Check1")).IsChecked = false;
            ((Button)choice.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(!choice.Confirmed && !choice.Check1Checked && !choice.IsVisible,
                "Not now keeps a cleared checkbox and never starts the update");
            Console.WriteLine("PASS: KillerUI confirmation card and safe actions across all 13 themes. Captures: " + captures);
            return 0;
        }
        finally
        {
            owner.Close();
            application.Shutdown();
        }
    }

    private static int RunUiSmoke()
    {
        IsolatedDesktop.Verify();
        var application = new Application();
        SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(application.Dispatcher));
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Black.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Instrument.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Strings/en-US.xaml", UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var window = new KillerBench.MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowActivated = false,
            ShowInTaskbar = false,
            Left = SystemParameters.VirtualScreenLeft - 2000,
            Top = SystemParameters.VirtualScreenTop
        };
        try
        {
            window.Show();
            window.UpdateLayout();
            RenderedTypographyChecks.VerifySubtitle(window);
            Require(window.Width == 1025 && window.Height == 700 && window.MinWidth == 800 && window.MinHeight == 500 &&
                window.SizeToContent == SizeToContent.Manual, "Workbench window keeps its family window geometry");
            Require(window.FindName("ZoomViewport") is FrameworkElement { ClipToBounds: true },
                "Zoom content remains clipped to the workbench window");
            Require(window.FindName("ServicingMonitorSamples") is Slider { Minimum: 1, Maximum: 120, Value: 3 },
                "Servicing monitor desktop controls expose the shared one through 120 snapshot range");
            Require(window.FindName("ServicingMonitorEvents") is CheckBox { Content: "Recent Windows Update events" } &&
                window.FindName("ServicingMonitorScheduledTask") is CheckBox { Content: "Windows Update scheduled task" },
                "Servicing monitor desktop controls resolve their localizable English resources");
            Require(window.FindName("SearchIndexRebuildConsent") is CheckBox { Content: "Rebuild the Windows Search index" } &&
                window.FindName("ReviewBrowserExtensions") is Button { Content: "Review browser extensions" },
                "Search and application review controls resolve their localizable English resources");
            Require(window.FindName("MaceRemovePackage") is Button { Content: "REMOVE SELECTED PACKAGE" } &&
                window.FindName("OrcaClearCaches") is CheckBox { Content: "Outlook caches" },
                "Microsoft app removal and Outlook reset controls resolve their localizable English resources");
            Require(window.FindName("DepotUpdates") is CheckBox { Content: "Windows updates" } &&
                window.FindName("DepotPrivacy") is CheckBox { Content: "Privacy" },
                "Onboarding controls resolve their localizable English resources");
            Require(window.FindName("DebloatPower") is CheckBox { Content: "High performance power" } &&
                window.FindName("DebloatActionChoice") is ComboBox { Items.Count: 4 } debloatActions &&
                debloatActions.Items[3] is ComboBoxItem { Content: "Disable hibernation" } &&
                window.FindName("DebloatOfficeCulture") is ComboBox { Items.Count: 13 } officeCultures &&
                officeCultures.Items[0] is ComboBoxItem { Content: "Choose Office language to retain" } &&
                window.FindName("DebloatSleepAc") is ComboBox { Items.Count: 5 } sleepAc &&
                sleepAc.Items[0] is ComboBoxItem { Content: "Sleep AC: Never" } &&
                window.FindName("DebloatHp") is CheckBox { Content: "HP software" },
                "Declutter safety, culture and timer controls resolve their localizable English resources");
            Require(window.FindName("BitsRetryButton") is Button { Content: "Retry selected transfer" } &&
                window.FindName("PrinterMakeDefault") is CheckBox { Content: "Repair a missing or conflicting default with this queue" } &&
                window.FindName("FindMsiProductsButton") is Button { Content: "Find installed MSI apps" } &&
                window.FindName("InstallerProductCode") is TextBox { ToolTip: "Example: {00000000-0000-0000-0000-000000000000}" },
                "BITS, Printing and Installer controls resolve their localizable English resources");
            Require(window.FindName("TimeRecoverButton") is Button { Content: "Check interrupted time change" } &&
                window.FindName("CopyProfileBackupButton") is Button { Content: "Copy and verify" },
                "Time and Profiles controls resolve their localizable English resources");
            Require(window.FindName("PrivacyBooleanApply") is CheckBox { Content: "Apply when I select Run diagnosis" } &&
                window.FindName("BitLockerEncryptConsent") is CheckBox { Content: "Back up the selected recovery key, then start encryption when I select Run diagnosis" },
                "Privacy and BitLocker safety controls resolve their localizable English resources");
            Require(window.FindName("EventChannelEnableConsent") is CheckBox { Content: "Enable this channel after fresh checks" } &&
                window.FindName("AudioDirectionChoice") is ComboBox { Items.Count: 3 } audioDirections &&
                audioDirections.Items[0] is ComboBoxItem { Content: "Input and output" } &&
                window.FindName("SystemRestoreCreate") is CheckBox { Content: "Prepare one restore point" } &&
                window.FindName("StorageRepairReview") is CheckBox { Content: "Review filesystem repair" },
                "Event log, audio, System Restore and Storage controls resolve their localizable English resources");
            Require(window.FindName("ComponentCleanupSettings") is TextBlock { Text: "The Run button checks whether Windows recommends cleanup. Optional cleanup removes superseded components immediately; no automatic undo or restart. Reclaimed space is not measured." } &&
                window.FindName("ReviewDuplicatesButton") is Button { Content: "Find duplicates" } &&
                window.FindName("ComputerRenameConsent") is CheckBox { Content: "Rename this workgroup PC after fresh checks" } &&
                window.FindName("MainGo") is Button { Content: "Run diagnosis" },
                "Cleanup, file review, computer name and primary action controls resolve their localizable English resources");
            Require(window.FindName("TaskDraftExpander") is Expander { Header: "New task draft" } &&
                window.FindName("TaskRegistrationConsent") is CheckBox { Content: "Create this disabled Local System task after fresh checks" } &&
                window.FindName("TaskRegistrationRecoveryButton") is Button { Content: "Check interrupted registration" },
                "Scheduled task draft and registration controls resolve their localizable English resources");
            Require(window.FindName("CleanupSystemTemp") is CheckBox { Content: "Windows SystemTemp files" } &&
                window.FindName("CleanupPreviousWindows") is CheckBox { Content: "Remove Windows rollback capability and let Windows clean the previous installation" } &&
                window.FindName("CleanupRemove") is CheckBox { Content: "Perform the selected permanent cleanup after fresh checks" },
                "Cleanup targets and permanent-action controls resolve their localizable English resources");
            Require(window.FindName("UpdateSoftware") is CheckBox { Content: "Software updates" } &&
                window.FindName("UpdateStoreRecoveryConsent") is CheckBox { Content: "Retain the old database and rebuild Windows Update data" } &&
                window.FindName("UpdateInstallButton") is Button { Content: "Install selected" } &&
                window.FindName("ThirdPartyUpdateAgreements") is CheckBox { Content: "Accept the selected package and source agreements" },
                "Windows and third-party update controls resolve their localizable English resources");
            Require(window.FindName("SecurityVirtualization") is CheckBox { Content: "VBS, memory integrity and Credential Guard" } &&
                window.FindName("DefenderScanConsent") is CheckBox { Content: "Start the selected scan when I select Run diagnosis" } &&
                window.FindName("FirewallProfileApply") is CheckBox { Content: "Apply selected local profiles after fresh policy checks" },
                "Security scan and Firewall profile controls resolve their localizable English resources");
            Require(window.FindName("RecipeSelectionPanel") is Expander { Header: "Choose checks (advanced)" } &&
                window.FindName("AdvancedTools") is Expander { Header: "Advanced: logs and manual repairs" } &&
                window.FindName("KnowledgeSearchButton") is Button { Content: "Open pack and search text" } &&
                window.FindName("ConsoleViewButton") is Button { Content: "Raw output" } &&
                window.FindName("RepairConsent") is CheckBox { Content: "Run this repair on this PC" },
                "Advanced selection, knowledge, session and repair controls resolve their localizable English resources");
            Require(window.FindName("CategoryTitle") is TextBlock { Text: "Diagnostics" } &&
                window.FindName("ScanLight") is Border { ToolTip: "Ready. No scan performed." } &&
                window.FindName("AgentSafeRepairsCheck") is CheckBox && window.FindName("AgentDisruptiveRepairsCheck") is CheckBox &&
                window.FindName("AboutStartupUpdateCheck") is CheckBox { Content: TextBlock { Text: "Check for updates on startup" } } &&
                window.FindName("AboutUpdateButton") is Button { Visibility: Visibility.Collapsed },
                "Shell status and About controls resolve their localizable English resources");
            Require((string)application.FindResource("Str_Text_DIAGNOSTIC_WORKBENCH") == "Windows Maintenance Workbench",
                "Desktop subtitle identifies the full maintenance scope");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(KillerBench.MainWindow).GetMethod("SetCaptureBusy", flags)!.Invoke(window, [true]);
            Require(window.FindName("CategoryList") is ListBox { IsEnabled: true } &&
                window.FindName("CategoryWorkflow") is ComboBox { IsEnabled: true } &&
                window.FindName("ExportReportButton") is Button { IsEnabled: true } &&
                window.FindName("MainGo") is Button { IsEnabled: false },
                "Live work keeps category navigation and report export available while preventing another run");
            typeof(KillerBench.MainWindow).GetMethod("SetCaptureBusy", flags)!.Invoke(window, [false]);
            var actionable = new WorkflowResult(WorkflowKind.CommonProblems, "smoke", DateTimeOffset.UtcNow, "Needs attention",
                [new("Service repair", CheckState.Repairable, "Fixture repair option", RepairKind.SystemFiles),
                 new("Collected system detail", CheckState.Information, "Fixture informational evidence")], [], "smoke-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [actionable]);
            typeof(KillerBench.MainWindow).GetMethod("SetOutputView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, []);
            window.UpdateLayout();
            Require(FindTextBlocks(window).Any(block => block.Text == "Collected information") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("Fixture informational evidence", StringComparison.Ordinal)),
                "Readable reports retain collected evidence alongside repairable findings");
            var repairNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review System files repair"));
            var maintenanceNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review maintenance options"));
            repairNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Require(window.FindName("CategoryList") is ListBox { SelectedItem: not null } categories && categories.SelectedItem.ToString() == "Repair" &&
                window.FindName("MainWorkflow") is ComboBox { SelectedItem: WorkflowDefinition { Kind: WorkflowKind.SystemFiles } } &&
                window.FindName("MainAutoRepair") is CheckBox { IsChecked: not true },
                "Single-repair reports lead to their focused unapproved Repair option");
            maintenanceNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Require(window.FindName("CategoryList") is ListBox { SelectedItem: not null } maintenanceCategories && maintenanceCategories.SelectedItem.ToString() == "Maintenance" &&
                window.FindName("MainWorkflow") is ComboBox { SelectedItem: WorkflowDefinition { Kind: WorkflowKind.Cleanup } } &&
                window.FindName("MainAutoRepair") is CheckBox { IsChecked: not true, IsEnabled: false },
                "Full-scan reports lead to unapproved Maintenance options");
            var mixed = new WorkflowResult(WorkflowKind.CommonProblems, "smoke", DateTimeOffset.UtcNow, "Needs attention",
                [new("File repair", CheckState.Repairable, "Fixture repair option", RepairKind.SystemFiles),
                 new("Print repair", CheckState.Repairable, "Fixture repair option", RepairKind.PrintSpooler)], [], "smoke-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [mixed]);
            window.UpdateLayout();
            Require(FindButtons(window).Any(button => Equals(button.Content, "Review repair options")) &&
                !FindButtons(window).Any(button => Equals(button.Content, "Review System files repair")),
                "Mixed repair reports retain a broader Repair handoff instead of guessing");
            string systemFilesRecipe = RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems)
                .Single(recipe => recipe.Action == RepairKind.SystemFiles).Id;
            var recipeOnly = new WorkflowResult(WorkflowKind.CommonProblems, "smoke", DateTimeOffset.UtcNow, "Needs attention", [], [], "smoke-journal.json")
            {
                Recipes = [new RecipeResult(systemFilesRecipe, 1, RecipeOutcome.RepairAvailable, null, null, "Fixture repair option")]
            };
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [recipeOnly]);
            window.UpdateLayout();
            Require(FindButtons(window).Any(button => Equals(button.Content, "Review System files repair")),
                "Recipe-only repair reports identify their focused Repair workflow");
            ((ComboBox)window.FindName("MainWorkflow")).SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == WorkflowKind.BrowserExtensions);
            window.UpdateLayout();
            Require(window.FindName("CategoryList") is ListBox { SelectedItem: not null } browserCategories && browserCategories.SelectedItem.ToString() == "Apps" &&
                window.FindName("MainAutoRepair") is CheckBox { IsChecked: not true, IsEnabled: false },
                "Browser extension inventory appears in Apps without repair controls");
            ((Expander)window.FindName("AdvancedTools")).IsExpanded = true;
            window.UpdateLayout();
            FindButtons(window).Single(button => Equals(button.Content, "Workflows")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var workflowChoices = (ComboBox)window.FindName("RepairChoice");
            var workflowPreview = (TextBox)window.FindName("RepairPreview");
            var workflowConsent = (CheckBox)window.FindName("RepairConsent");
            workflowChoices.SelectedItem = workflowChoices.Items.Cast<ComboBoxItem>().Single(item => Equals(item.Content, "Browser extensions"));
            Require(workflowPreview.Text.Contains("Inventory bounded Chrome, Edge, Brave and Firefox extension identities", StringComparison.Ordinal) &&
                !workflowConsent.IsEnabled,
                "Advanced Browser extensions selection previews only browser inventory without repair consent");
            workflowChoices.SelectedItem = workflowChoices.Items.Cast<ComboBoxItem>().Single(item => Equals(item.Content, "Backup services"));
            Require(workflowPreview.Text.Contains("Check Volume Shadow Copy", StringComparison.Ordinal) && workflowConsent.IsEnabled,
                "Advanced Backup services selection previews its own repair-capable workflow");
            ((ComboBox)window.FindName("MainWorkflow")).SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == WorkflowKind.InstalledApps);
            window.UpdateLayout();
            ((Button)window.FindName("ReviewBrowserExtensions")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Require(window.FindName("MainWorkflow") is ComboBox { SelectedItem: WorkflowDefinition { Kind: WorkflowKind.BrowserExtensions } } &&
                window.FindName("MainAutoRepair") is CheckBox { IsChecked: not true, IsEnabled: false },
                "Installed-applications handoff routes browser review through Run diagnosis without repair consent");
            var browserReport = new WorkflowResult(WorkflowKind.BrowserExtensions, "smoke", DateTimeOffset.UtcNow,
                "Browser extension identities reviewed; no browser or extension changed",
                [new("Browser extension inventory", CheckState.Information, "1 extension identity collected."),
                 new("Fixture browser extension: fixture-extension", CheckState.Information, "Profile: Default. Identity only.")], [], "smoke-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [browserReport]);
            window.UpdateLayout();
            Require(FindTextBlocks(window).Any(block => block.Text == "Collected information") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("fixture-extension", StringComparison.Ordinal)),
                "Readable reports retain bounded informational browser evidence when no problem was found");
            ((ComboBox)window.FindName("MainWorkflow")).SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == WorkflowKind.InstalledApps);
            ((TextBox)window.FindName("ApplicationFolderName")).Text = "KillerBenchUiSmokeFolder";
            ((Button)window.FindName("ReviewApplicationFoldersButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int attempt = 0; attempt < 12 && !FindTextBlocks(window).Any(block => block.Text == "Application folder review report"); attempt++) Pump();
            Require(window.FindName("ReportPane") is Border { Visibility: Visibility.Visible } &&
                FindTextBlocks(window).Any(block => block.Text == "Application folder review report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("No folder contents were read or changed", StringComparison.Ordinal)),
                "Application-folder review opens a formatted read-only report without exposing folder contents");
            string emptyFolder = Path.Combine(Path.GetTempPath(), "KillerBench-ui-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(emptyFolder);
            try
            {
                ((ComboBox)window.FindName("MainWorkflow")).SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == WorkflowKind.Storage);
                ((TextBox)window.FindName("FileReviewFolder")).Text = emptyFolder;
                ((Button)window.FindName("ReviewDuplicatesButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                for (int attempt = 0; attempt < 12 && !FindTextBlocks(window).Any(block => block.Text == "Duplicate file review report"); attempt++) Pump();
                Require(window.FindName("ReportPane") is Border { Visibility: Visibility.Visible } &&
                    FindTextBlocks(window).Any(block => block.Text == "Duplicate file review report") &&
                    FindTextBlocks(window).Any(block => block.Text.Contains("No files changed", StringComparison.Ordinal)),
                    "Duplicate-file review opens a formatted read-only report");
            }
            finally { Directory.Delete(emptyFolder); }
            var comparisonReport = new WorkflowResult(WorkflowKind.InstalledApps, "smoke", DateTimeOffset.UtcNow,
                "Application inventory comparison complete. No applications changed.",
                [new("Comparison scope", CheckState.Information, "Both saved inventories use the Machine registration scope."),
                 new("Registration differences", CheckState.Attention, "1 registration difference was identified. Review the raw output before choosing any action.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [comparisonReport, "Application inventory comparison"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Application inventory comparison report") &&
                FindTextBlocks(window).Any(block => block.Text == "Registration differences"),
                "Saved inventory comparisons have a readable report with the retained change count");
            var packageReport = new WorkflowResult(WorkflowKind.AppPackages, "smoke", DateTimeOffset.UtcNow,
                "Package snapshot reviewed. No package removal is available or authorized.",
                [new("Package review item 1", CheckState.Information, "Windows reports this package as non-removable."),
                 new("Dependent packages", CheckState.Information, "No dependent packages were reported in this retained snapshot.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [packageReport, "Package snapshot review"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Package snapshot review report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("No package removal is available or authorized", StringComparison.Ordinal)),
                "Selected package reviews have a readable report without a removal action");
            var transferReport = new WorkflowResult(WorkflowKind.BackgroundTransfers, "smoke", DateTimeOffset.UtcNow,
                "Transfer snapshot reviewed. No job changes performed.",
                [new("Selected transfer", CheckState.Information, "Name: Fixture transfer. Owner: current account. State: Suspended."),
                 new("Snapshot review", CheckState.Information, "The job is suspended and may be intentional."),
                 new("Change boundary", CheckState.Information, "No transfer was resumed, completed, canceled, deleted, or reassigned.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [transferReport, "Background transfer review"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Background transfer review report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("No transfer was resumed", StringComparison.Ordinal)),
                "Background transfer reviews have a readable report that preserves the no-change boundary");
            var printJobReport = new WorkflowResult(WorkflowKind.Printing, "smoke", DateTimeOffset.UtcNow,
                "1 cancellation candidate found. Select one, confirm the exact cancellation, then select Run diagnosis.",
                [new("Print-job evidence", CheckState.Clear, "A complete bounded snapshot of current print jobs was collected."),
                 new("Cancellation candidates", CheckState.Attention, "1 exact Error or Blocked print job candidate was retained for review."),
                 new("Change boundary", CheckState.Information, "No print job was canceled, restarted, paused, resumed, or otherwise changed.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [printJobReport, "Blocked print job review"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Blocked print job review report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("No print job was canceled", StringComparison.Ordinal)),
                "Blocked print-job discovery has a readable report before any cancellation is considered");
            var updatePlanReport = new WorkflowResult(WorkflowKind.AvailableUpdates, "smoke", DateTimeOffset.UtcNow,
                "One or more selected updates need review before installation can be considered.",
                [new("Update: Fixture driver", CheckState.Attention, "Allow updates that may require a later restart to include this update. No automatic restart is planned."),
                 new("Installation boundary", CheckState.Information, "This is a preview only. No update was downloaded or installed, no license was accepted, and no restart was scheduled.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [updatePlanReport, "Windows Update plan"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Windows Update plan report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("No update was downloaded or installed", StringComparison.Ordinal)),
                "Update previews have a readable report that preserves the no-installation boundary");
            var applicationRecoveryReport = new WorkflowResult(WorkflowKind.AvailableUpdates, "smoke", DateTimeOffset.UtcNow,
                "No interrupted application update record was found.",
                [new("Recovery result", CheckState.Clear, "No interrupted application update record was found."),
                 new("Change boundary", CheckState.Information, "This recovery check did not install, retry, remove, or change an application update.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [applicationRecoveryReport, "Application update recovery"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Application update recovery report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("did not install, retry, remove", StringComparison.Ordinal)),
                "Application update recovery has a readable no-retry report");
            var timeRecoveryReport = new WorkflowResult(WorkflowKind.TimeService, "smoke", DateTimeOffset.UtcNow,
                "No interrupted time change record was found.",
                [new("Recovery result", CheckState.Clear, "No interrupted time change record was found."),
                 new("Change boundary", CheckState.Information, "This recovery check did not synchronize the clock, change a time source, or retry a prior change.")], [], "read-only");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [timeRecoveryReport, "Time recovery check"]);
            Require(FindTextBlocks(window).Any(block => block.Text == "Time recovery check report") &&
                FindTextBlocks(window).Any(block => block.Text.Contains("did not synchronize the clock", StringComparison.Ordinal)),
                "Time recovery checks have a readable no-change report");
            Console.WriteLine("PASS: WPF workbench geometry, zoom containment, servicing monitor bounds and safe report navigation.");
            return 0;
        }
        finally
        {
            window.Close();
            application.Shutdown();
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int RunChecks(bool includeUi, bool skipNonUi = false)
    {
        string scratch = Path.Combine(Path.GetTempPath(), "KillerBench-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        string captureFixture = "";
        try
        {
            if (!skipNonUi)
            {
            CheckEvidence(scratch).GetAwaiter().GetResult();
            AggregateCancellationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            RecipeOperationChecks.RunAsync().GetAwaiter().GetResult();
            RecipeOperationChecks.RunFileAsync(scratch).GetAwaiter().GetResult();
            RestartRecoveryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            MemoryDiagnosticRecoveryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            RepairCapabilityChecks.RunAsync(scratch).GetAwaiter().GetResult();
            GoalUncertaintyChecks.RunAsync().GetAwaiter().GetResult();
            WindowsMtuDispatchChecks.RunAsync().GetAwaiter().GetResult();
            MutationDispatchChecks.RunAsync().GetAwaiter().GetResult();
            DriveLetterTargetChecks.RunAsync().GetAwaiter().GetResult();
            WindowsProxyReadChecks.RunAsync().GetAwaiter().GetResult();
            WindowsEntraPrivacyChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WindowsGroupPolicyDateChecks.RunAsync().GetAwaiter().GetResult();
            WinGetExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            EvidenceBatchCoverageChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WindowsRetentionScopeChecks.RunAsync().GetAwaiter().GetResult();
            GoalOptionValidationChecks.RunAsync().GetAwaiter().GetResult();
            InstallerRuntimeChecks.Run();
            UpdateHelperChecks.Run(scratch);
            PortablePublicationChecks.Run(scratch);
            InstallationOwnershipChecks.Run(scratch);
            InstallationProtectionChecks.Run(scratch);
            NativeScenarioRestorationChecks.RunAsync().GetAwaiter().GetResult();
            RecipePackChecks.RunAsync().GetAwaiter().GetResult();
            MalformedInputChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ReleaseFeatureChecks.RunAsync().GetAwaiter().GetResult();
            WindowsTruncatedEvidenceChecks.Run();
            WindowsServicePackChecks.RunAsync().GetAwaiter().GetResult();
            WindowsPowerShellBindingChecks.RunAsync().GetAwaiter().GetResult();
            WindowsSettingsPackChecks.RunAsync().GetAwaiter().GetResult();
            WindowsSystemPackChecks.RunAsync().GetAwaiter().GetResult();
            WindowsMaintenancePackChecks.RunAsync().GetAwaiter().GetResult();
            SymptomTriageChecks.RunAsync().GetAwaiter().GetResult();
            SymptomMatrixChecks.Run();
            NativeScenarioRunnerChecks.RunAsync().GetAwaiter().GetResult();
            DemoWorkflowChecks.Run();
            ReleaseUpdateChecks.Run();
            InvestigationChecks.Run();
            ServicingLogChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ServicingActivityMonitorChecks.RunAsync().GetAwaiter().GetResult();
            ReliabilityHistoryChecks.Run();
            ReliabilityOperationalCorrelationChecks.Run();
            HardwareErrorEventChecks.Run();
            SetupApiDriverInstallChecks.Run();
            DriverHistoryOperationalCorrelationChecks.Run();
            SystemRestoreChecks.Run();
            SystemRestoreOperationalCorrelationChecks.Run();
            SystemRestoreCreationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WindowsRecoveryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WindowsRecoveryPartitionChecks.Run();
            WindowsRecoveryReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WmiRecoveryGateChecks.RunAsync().GetAwaiter().GetResult();
            TimeRecoveryGateChecks.RunAsync().GetAwaiter().GetResult();
            CertificateChecks.Run();
            TaskRuntimeChecks.Run();
            OptimizeDrivesDiagnosticChecks.Run();
            VolumeOptimizationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            SearchEventChecks.Run();
            AppDeploymentEventChecks.Run();
            AccountBrokerEventChecks.Run();
            WindowsUpdateEventChecks.Run();
            WindowsUpdateActivityChecks.Run();
            UserProfileEventChecks.Run();
            DeviceSetupEventChecks.Run();
            KernelPnpEventChecks.Run();
            WmiActivityEventChecks.Run();
            WmiHistoricalCorrelationChecks.Run();
            DefenderEventChecks.Run();
            DefenderThreatSummaryChecks.Run();
            DefenderScanHistoryChecks.Run();
            DefenderScanExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DefenderProtectionChecks.Run();
            UacPolicyChecks.Run();
            AppLockerPolicyChecks.Run();
            RemoteDesktopDiagnosticChecks.Run();
            RemoteDesktopEventChecks.Run();
            DiagnosticPolicyEventChecks.Run();
            WinRmEventChecks.Run();
            WinRmListenerChecks.Run();
            WirelessEventChecks.Run();
            WiredAuthenticationEventChecks.Run();
            DhcpClientEventChecks.RunAsync().GetAwaiter().GetResult();
            DhcpOperationalCorrelationChecks.Run();
            DnsClientEventChecks.RunAsync().GetAwaiter().GetResult();
            TcpSettingChecks.RunAsync().GetAwaiter().GetResult();
            NetworkOffloadChecks.RunAsync().GetAwaiter().GetResult();
            WinsockCatalogChecks.RunAsync().GetAwaiter().GetResult();
            WinsockNamespaceChecks.RunAsync().GetAwaiter().GetResult();
            WinsockProviderFileChecks.RunAsync().GetAwaiter().GetResult();
            WinsockReadinessCorrelationChecks.Run();
            StoreInstallEventChecks.Run();
            SearchCatalogChecks.Run();
            SearchOperationalCorrelationChecks.Run();
            SearchIndexRebuildChecks.RunAsync(scratch).GetAwaiter().GetResult();
            FileSignatureChecks.Run();
            TaskDefinitionChecks.Run();
            TaskPlanCommandChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WindowsTaskRegistrationHostChecks.Run();
            TaskRegistrationRecoveryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PrintJobChecks.RunAsync().GetAwaiter().GetResult();
            PrintJobCancellationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DefaultPrinterChecks.Run();
            DefaultPrinterSelectionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PrinterPortChecks.RunAsync().GetAwaiter().GetResult();
            PrintQueueEventCorrelationChecks.Run();
            PrintingEnvironmentCorrelationChecks.Run();
            PrintingOperationalCorrelationChecks.Run();
            PrintServiceOperationalEventChecks.RunAsync().GetAwaiter().GetResult();
            PrintPolicyChecks.Run();
            PrintRenderingPolicyChecks.Run();
            PrinterEndpointChecks.Run();
            PrinterEndpointReachabilityChecks.RunAsync().GetAwaiter().GetResult();
            PrinterDriverFileChecks.RunAsync().GetAwaiter().GetResult();
            PrintExtensionChecks.Run();
            PrintExtensionFileChecks.RunAsync().GetAwaiter().GetResult();
            PrintSpoolerFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            WindowsInstallerFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            WindowsUpdateServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            DeliveryOptimizationServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            ServicingServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            ServicingServiceStateChecks.RunAsync().GetAwaiter().GetResult();
            ServicingChainCorrelationChecks.Run();
            WindowsUpdateOperationalCorrelationChecks.Run();
            InstallerEngineFileChecks.RunAsync().GetAwaiter().GetResult();
            WindowsUpdateEngineFileChecks.RunAsync().GetAwaiter().GetResult();
            InstallerServiceRegistrationChecks.Run();
            PrintSpoolDirectoryChecks.Run();
            PrintSpoolDirectoryReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DeviceProblemCodeChecks.Run();
            WorkflowJournalChecks.RunAsync(scratch).GetAwaiter().GetResult();
            CrashDumpChecks.Run(scratch);
            MinidumpAnalyzerChecks.Run(scratch);
            CrashIncidentCorrelationChecks.Run();
            AppPackageChecks.Run();
            ProvisionedPackageChecks.Run();
            InstalledApplicationChecks.Run();
            ApplicationFolderReviewChecks.Run();
            BrowserExtensionReviewChecks.Run(scratch);
            FileReviewChecks.RunAsync(scratch).GetAwaiter().GetResult();
            MacePlanningChecks.Run();
            MacePackageRemovalChecks.RunAsync(scratch).GetAwaiter().GetResult();
            OrcaPlanningChecks.Run();
            OrcaCacheResetChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PreviousWindowsInstallationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DepotPlanningChecks.Run();
            DebloatPlanningChecks.Run();
            DebloatPowerPlanExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DebloatPowerTimerExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DebloatHibernationExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DebloatPowerReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ApplicationInventoryFileChecks.RunAsync(scratch).GetAwaiter().GetResult();
            BackupServiceChecks.RunAsync(scratch).GetAwaiter().GetResult();
            FirewallServiceChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WirelessChecks.RunAsync(scratch).GetAwaiter().GetResult();
            FirewallProfileChecks.Run();
            FirewallLoggingChecks.Run();
            FirewallRuleSummaryChecks.Run();
            FirewallEventChecks.RunAsync().GetAwaiter().GetResult();
            FirewallServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            NetworkServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            StorageServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            BackupServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            BackupOperationalCorrelationChecks.Run();
            SystemStabilityOperationalCorrelationChecks.Run();
            SecurityOperationalCorrelationChecks.Run();
            ApplicationDeploymentOperationalCorrelationChecks.Run();
            ApplicationStabilityCorrelationChecks.Run();
            ScheduledTaskOperationalCorrelationChecks.Run();
            AppPackageOperationalCorrelationChecks.Run();
            DeviceOperationalCorrelationChecks.Run();
            InstalledApplicationOperationalCorrelationChecks.Run();
            CertificateOperationalCorrelationChecks.Run();
            WindowsActivationOperationalCorrelationChecks.Run();
            StorageSpacesOperationalCorrelationChecks.Run();
            StartupPerformanceOperationalCorrelationChecks.Run();
            HardwareOperationalCorrelationChecks.Run();
            FeatureServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            PlatformServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            InfrastructureServiceFailureEventChecks.RunAsync().GetAwaiter().GetResult();
            FirewallProfileEnableChecks.RunAsync(scratch).GetAwaiter().GetResult();
            NetworkProfileChecks.Run();
            NcsiPolicyChecks.RunAsync().GetAwaiter().GetResult();
            NetworkFirewallCorrelationChecks.Run();
            SmbShareChecks.Run();
            SmbServerConfigurationChecks.Run();
            SmbClientConfigurationChecks.Run();
            FileSharingOperationalCorrelationChecks.Run();
            SmbConnectionChecks.Run();
            FontRegistrationChecks.Run();
            BackupEventChecks.Run();
            VssWriterChecks.Run();
            VssProviderChecks.RunAsync().GetAwaiter().GetResult();
            ShadowCopyChecks.RunAsync().GetAwaiter().GetResult();
            WindowsBackupSummaryChecks.RunAsync().GetAwaiter().GetResult();
            FileHistoryEventChecks.RunAsync().GetAwaiter().GetResult();
            FileHistoryStatusChecks.RunAsync().GetAwaiter().GetResult();
            BitsChecks.Run();
            BitsOperationalCorrelationChecks.Run();
            WindowsUpdateServiceOperationalCorrelationChecks.Run();
            CryptographicServiceOperationalCorrelationChecks.Run();
            ModulesInstallerOperationalCorrelationChecks.Run();
            WindowsUpdateMedicOperationalCorrelationChecks.Run();
            UpdateOrchestratorOperationalCorrelationChecks.Run();
            DiagnosticPolicyOperationalCorrelationChecks.Run();
            WindowsErrorReportingOperationalCorrelationChecks.Run();
            EventCollectorOperationalCorrelationChecks.Run();
            RemoteAccessOperationalCorrelationChecks.Run();
            RemoteDesktopOperationalCorrelationChecks.Run();
            WinRmOperationalCorrelationChecks.Run();
            SecurityCenterOperationalCorrelationChecks.Run();
            NetworkConnectionsOperationalCorrelationChecks.Run();
            FocusedNetworkManagementCorrelationChecks.Run();
            NetworkSetupOperationalCorrelationChecks.Run();
            FocusedStorageServiceOperationalCorrelationChecks.Run();
            BitsObservationChecks.RunAsync().GetAwaiter().GetResult();
            BitsRetryChecks.RunAsync().GetAwaiter().GetResult();
            WindowsBitsRetryChecks.RunAsync().GetAwaiter().GetResult();
            BitsRetryJournalChecks.RunAsync(scratch).GetAwaiter().GetResult();
            BitsRetryReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ActivityExportChecks.Run();
            WorkflowReportExportChecks.Run();
            ServiceInventoryChecks.Run();
            ServiceBinaryChecks.Run();
            ServicePathQuotingChecks.Run();
            ServiceLogonChecks.Run();
            ServiceStartupConfigurationChecks.Run();
            ServiceDependencyConfigurationChecks.Run();
            ServiceDllChecks.Run();
            ServiceTransitionChecks.Run();
            ServiceRecoveryPolicyChecks.Run();
            ServiceTriggerChecks.RunAsync().GetAwaiter().GetResult();
            ServiceEventChecks.Run();
            ServiceEventCorrelationChecks.Run();
            ServiceOperationalCorrelationChecks.Run();
            FocusedServiceOperationalCorrelationChecks.Run(scratch);
            PeripheralOperationalCorrelationChecks.Run(scratch);
            OperationalCorrelationExpansionChecks.Run();
            SystemFilesOperationalCorrelationChecks.Run(scratch);
            FocusedWindowsUpdateOperationalCorrelationChecks.Run(scratch);
            FocusedDefenderOperationalCorrelationChecks.Run();
            FocusedBackupOperationalCorrelationChecks.Run();
            DiagnosticReadinessSummaryChecks.Run();
            EventChannelChecks.Run();
            EventChannelEnableChecks.RunAsync(scratch).GetAwaiter().GetResult();
            EventLogCapacityChecks.RunAsync().GetAwaiter().GetResult();
            EventLogOperationalCorrelationChecks.Run();
            TimeOperationalCorrelationChecks.Run();
            EventCollectorSubscriptionChecks.Run();
            VpnProfileChecks.Run();
            SecurityProductChecks.Run();
            EventArchiveChecks.Run();
            EventXmlImportChecks.RunAsync(scratch).GetAwaiter().GetResult();
            EvidenceBatchChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ServiceDependencyChecks.Run();
            ServiceStartPrerequisiteChecks.Run();
            ProxyChecks.Run();
            DnsServerChecks.Run();
            DnsClientConfigurationChecks.Run();
            DnsNrptPolicyChecks.RunAsync().GetAwaiter().GetResult();
            NetworkPathCorrelationChecks.Run();
            DnsOperationalCorrelationChecks.Run();
            GatewayReachabilityChecks.RunAsync().GetAwaiter().GetResult();
            NetworkConnectivityCorrelationChecks.Run();
            NetworkOperationalCorrelationChecks.Run();
            IpAddressChecks.Run();
            InstallerSecurityPolicyChecks.Run();
            InstallerLoggingChecks.Run();
            WindowsCapabilityChecks.RunAsync().GetAwaiter().GetResult();
            MsiCacheChecks.Run();
            MsiSourceChecks.Run();
            ServiceObservationChecks.RunAsync().GetAwaiter().GetResult();
            DeliveryOptimizationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DeliveryOptimizationActivityChecks.RunAsync().GetAwaiter().GetResult();
            DeliveryOptimizationPolicyChecks.Run();
            DeliveryOptimizationOperationalCorrelationChecks.Run();
            UpdateCacheChecks.RunAsync().GetAwaiter().GetResult();
            UpdatePolicyChecks.Run();
            FeatureUpdatePolicyChecks.Run();
            UpdatePolicySurfaceChecks.Run();
            UpdateRestartPolicyChecks.Run();
            ComponentRepairSourceChecks.Run();
            UpdateServiceChecks.Run();
            WindowsUpdateScheduledTaskChecks.Run();
            WindowsUpdateErrorChecks.Run();
            WindowsSetupErrorChecks.Run();
            WindowsErrorReferenceChecks.Run();
            WindowsInstallerErrorChecks.Run();
            UpdateHistoryChecks.Run();
            InstalledHotfixChecks.Run();
            UpdateExecutionContextChecks.Run();
            UpdateApplicabilityChecks.Run();
            WindowsFeatureVersionChecks.Run();
            UpdateFailureFollowupChecks.RunAsync().GetAwaiter().GetResult();
            UpdateDataStoreResetChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ServicingPackageChecks.Run();
            OptionalFeatureChecks.Run();
            ServicingTransitionCorrelationChecks.Run();
            ServicingRepairExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            RestartDiagnosticChecks.Run();
            PendingFileOperationChecks.Run();
            ComputerRenameChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WindowsLicensingChecks.Run();
            SoftwareProtectionEventChecks.Run();
            InstallerActivityChecks.Run();
            InstallerPendingOperationCorrelationChecks.Run();
            InstallerOutcomeCorrelationChecks.Run();
            InstallationEnvironmentCorrelationChecks.Run();
            InstallerOperationalCorrelationChecks.Run();
            MsiProductChecks.Run();
            MsiRepairChecks.RunAsync(scratch).GetAwaiter().GetResult();
            InstallerRegistrationRepairChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DnsChecks.RunAsync().GetAwaiter().GetResult();
            DnsCacheRecoveryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DnsClientCacheChecks.RunAsync().GetAwaiter().GetResult();
            TcpChecks.RunAsync().GetAwaiter().GetResult();
            NetworkTargetDiagnosisChecks.Run();
            RouteChecks.Run();
            RoutePriorityChecks.RunAsync().GetAwaiter().GetResult();
            IpInterfaceConfigurationChecks.RunAsync().GetAwaiter().GetResult();
            DhcpLeaseDiagnosticChecks.RunAsync().GetAwaiter().GetResult();
            NeighborCacheChecks.RunAsync().GetAwaiter().GetResult();
            AdapterBindingChecks.RunAsync().GetAwaiter().GetResult();
            AdapterStatisticsChecks.RunAsync().GetAwaiter().GetResult();
            PrinterDriverModelChecks.RunAsync().GetAwaiter().GetResult();
            ReservedStorageChecks.RunAsync().GetAwaiter().GetResult();
            StorageSensePolicyChecks.Run();
            StorageSenseTaskChecks.Run();
            DiskCleanupTaskChecks.Run();
            DiskDiagnosticTaskChecks.Run();
            DiskDiagnosticEventChecks.Run();
            StorageJobChecks.RunAsync().GetAwaiter().GetResult();
            PhysicalDiskChecks.Run();
            PhysicalDiskCorrelationChecks.Run();
            StorageEnvironmentCorrelationChecks.Run();
            StorageOperationalCorrelationChecks.Run();
            HostsFileChecks.Run();
            StorageDispatchChecks.RunAsync().GetAwaiter().GetResult();
            PageFileChecks.RunAsync().GetAwaiter().GetResult();
            FileSystemRepairExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            KnowledgeChecks.Run();
            KnowledgeActivationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            KnowledgeUpdateChecks.RunAsync(scratch).GetAwaiter().GetResult();
            KnowledgeHttpSourceChecks.RunAsync().GetAwaiter().GetResult();
            captureFixture = CaptureChecks.RunAsync(scratch).GetAwaiter().GetResult();
            RepairChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WorkflowChecks.RunAsync(scratch).GetAwaiter().GetResult();
            FullSystemScanChecks.RunAsync(scratch).GetAwaiter().GetResult();
            CommonDiagnosticChecks.RunAsync(scratch).GetAwaiter().GetResult();
            RecipeChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WorkflowOptionCatalogChecks.Run();
            NativeValidationCatalogChecks.Run();
            NativeWindowsProductMetadataChecks.Run();
            NativeValidationEvidenceChecks.Run();
            NativeValidationReportChecks.Run();
            ComponentCleanupChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ComponentCleanupReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ServicingRepairReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ServiceStartReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            WmiRepositoryReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            TimeSynchronizationReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            TimeConfigurationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            NativeToolChecks.RunAsync().GetAwaiter().GetResult();
            DhcpChecks.RunAsync().GetAwaiter().GetResult();
            DhcpLeaseReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PrintResumeChecks.RunAsync().GetAwaiter().GetResult();
            PrintQueueReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            RecoveryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PrinterOfflineChecks.RunAsync().GetAwaiter().GetResult();
            PrinterOfflineReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PrinterConditionChecks.RunAsync().GetAwaiter().GetResult();
            PrinterQueueAttributeChecks.RunAsync().GetAwaiter().GetResult();
            VitalsChecks.Run();
            MemoryDiagnosticResultChecks.Run();
            StorageReliabilityChecks.Run();
            ShadowStorageChecks.RunAsync().GetAwaiter().GetResult();
            DiskQuotaChecks.RunAsync().GetAwaiter().GetResult();
            NtfsSelfHealingChecks.RunAsync().GetAwaiter().GetResult();
            DeleteNotificationChecks.RunAsync().GetAwaiter().GetResult();
            TcpPortPressureChecks.RunAsync().GetAwaiter().GetResult();
            StorageTopologyChecks.Run();
            StorageVolumeCapacityCorrelationChecks.Run();
            StorageEventCorrelationChecks.Run();
            StorageSpacesChecks.Run();
            BootPerformanceEventChecks.Run();
            StartupApplicationChecks.Run();
            StartupOperationalCorrelationChecks.Run();
            VolumeDirtyChecks.Run();
            FoxitChecks.Run();
            TaskScheduleChecks.Run();
            TaskXmlChecks.Run();
            CleanupChecks.Run(scratch);
            BitLockerChecks.RunAsync().GetAwaiter().GetResult();
            BitLockerProtectionResumeChecks.RunAsync(scratch).GetAwaiter().GetResult();
            BitLockerEscrowChecks.RunAsync(scratch).GetAwaiter().GetResult();
            BitLockerProtectorCreationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            BitLockerDecryptionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            BitLockerAutoUnlockChecks.RunAsync(scratch).GetAwaiter().GetResult();
            DepotInstallerTrustChecks.RunAsync().GetAwaiter().GetResult();
            BitLockerEncryptionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            AudioDeviceChecks.Run();
            DefaultAudioEndpointChecks.Run();
            AudioOperationalCorrelationChecks.Run();
            DefenderChecks.RunAsync().GetAwaiter().GetResult();
            DefenderRefreshReconciliationChecks.RunAsync(scratch).GetAwaiter().GetResult();
            SecurityChecks.Run();
            PrivacyChecks.Run();
            PrivacyClipboardHistoryChecks.RunAsync(scratch).GetAwaiter().GetResult();
            PrivacyBooleanSettingChecks.RunAsync(scratch).GetAwaiter().GetResult();
            ProfilePlanningChecks.Run();
            ProfileRegistrationChecks.Run();
            ProfileOperationalCorrelationChecks.Run();
            ProfileBackupCopyChecks.Run();
            ProfileRemovalChecks.RunAsync(scratch).GetAwaiter().GetResult();
            UpdateDiscoveryChecks.RunAsync().GetAwaiter().GetResult();
            ThirdPartyUpdateChecks.RunAsync().GetAwaiter().GetResult();
            ThirdPartyUpdateExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            UpdateExecutionChecks.RunAsync(scratch).GetAwaiter().GetResult();
            var fixture = new TextEvidence("synthetic.log", ["ordinary error text", "HRESULT 0x800f081f", "again 0X800F081F and 0x00000000", "ignore 0x123 and 0x123456789"], true);
            var analysis = LogAnalyzer.Analyze(fixture);
            Require(analysis.SourceTruncated && analysis.Observations.Count == 2, "Preserve coverage and reject malformed codes");
            var observed = analysis.Observations.Single(code => code.Code == "0x800F081F");
            Require(observed.Occurrences == 2 && observed.Evidence.Select(e => e.LineNumber).SequenceEqual([2, 3]), "Normalize codes and preserve evidence locations");
            Require(LogAnalyzer.Analyze(new("clean", ["error alone is not a code"], false)).Observations.Count == 0, "No invented findings from generic error text");
            var bounded = LogAnalyzer.Analyze(new("many", [.. Enumerable.Range(0, 300).Select(i => $"0x{i:X8}")], false));
            Require(bounded.Observations.Count == 256 && bounded.ObservationsTruncated, "Bound distinct observations");
            var repeats = LogAnalyzer.Analyze(new("repeat", [.. Enumerable.Repeat("0x80000001", 20)], false));
            Require(repeats.Observations[0].Occurrences == 20 && repeats.Observations[0].Evidence.Count == 8, "Count beyond retained reference limit");
            try { LogAnalyzer.Analyze(fixture, new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
            catch (OperationCanceledException) { }
            File.WriteAllLines(Path.Combine(scratch, "analysis.log"), fixture.Lines);
            var imported = LogAnalyzer.AnalyzeFileAsync(Path.Combine(scratch, "analysis.log")).GetAwaiter().GetResult();
            Require(imported.Observations.Single(code => code.Code == observed.Code).Occurrences == observed.Occurrences, "File and in-memory APIs agree");
            }
            if (!includeUi)
            {
                Console.WriteLine("Non-UI checks passed. Window creation and resizing were skipped.");
                return 0;
            }
            IsolatedDesktop.Verify();
            if (captureFixture.Length == 0) captureFixture = CaptureChecks.RunAsync(scratch).GetAwaiter().GetResult();
            var application = new Application();
            SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(application.Dispatcher));
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/Themes/Black.xaml", UriKind.Relative) });
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative) });
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/Themes/Instrument.xaml", UriKind.Relative) });
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/Strings/en-US.xaml", UriKind.Relative) });
            KillerBench.Services.ThemeManager.Initialize();
            Require(KillerBench.Services.ThemeManager.Current == KillerBench.Services.Theme.Dark && KillerBench.Services.ThemeManager.AccentChoiceFor(KillerBench.Services.Theme.Dark) == KillerBench.Services.Accent.Yellow, "Fresh default must be Dark using Yellow");
            var window = new KillerBench.MainWindow();
            var updateSnapshotField = typeof(KillerBench.MainWindow).GetField("availableUpdateResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var filteredUpdate = new AvailableUpdate("74ba239e-08d2-45bd-b285-b0d35610ca60", 1, "Fixture driver", "Driver", true, true);
            updateSnapshotField.SetValue(window, new UpdateSearchResult(new(true, true), [filteredUpdate, filteredUpdate with { Title = "Other", Downloaded = false }], false, "fixture"));
            var updateFilter = (TextBox)window.FindName("UpdateTitleFilter");
            var updateList = (ListBox)window.FindName("AvailableUpdateList");
            updateFilter.Text = "driver";
            Require(updateList.Items.Count == 1 && Equals(updateList.Items[0], filteredUpdate), "Update title filter changes visible choices");
            updateList.SelectedIndex = 0;
            Require(((Button)window.FindName("UpdatePreviewButton")).IsEnabled && !((Button)window.FindName("UpdateInstallButton")).IsEnabled,
                "Unknown installation metadata permits preview but blocks installation");
            updateFilter.Text = "";
            Require(updateList.Items.Count == 2 && updateList.SelectedItems.Count == 0, "Changing update filter clears hidden installation selections");
            ((CheckBox)window.FindName("UpdateDownloadedOnly")).IsChecked = true;
            Require(updateList.Items.Count == 1, "Downloaded-only checkbox filters update choices");
            var eligibleUpdate = filteredUpdate with { Behavior = new(false, 0, 1, false, [new("3689bdc8-b205-4af4-8d4a-a63924c5e9d5", "Fixture")]) };
            updateSnapshotField.SetValue(window, new UpdateSearchResult(new(true, true), [eligibleUpdate], false, "fixture"));
            updateFilter.Text = "Fixture";
            updateList.SelectedIndex = 0;
            var allowRestart = (CheckBox)window.FindName("UpdateAllowRestart");
            var allowFeatureUpgrade = (CheckBox)window.FindName("UpdateAllowFeatureUpgrade");
            Require(!((Button)window.FindName("UpdateInstallButton")).IsEnabled, "Restart-required update is blocked until included");
            allowRestart.IsChecked = true;
            Require(!((Button)window.FindName("UpdateInstallButton")).IsEnabled, "Feature upgrade remains blocked without its separate confirmation");
            allowFeatureUpgrade.IsChecked = true;
            Require(((Button)window.FindName("UpdateInstallButton")).IsEnabled, "One offered feature upgrade enables installation after both confirmations");
            allowFeatureUpgrade.IsChecked = false;
            allowRestart.IsChecked = false;
            Require(!((Button)window.FindName("UpdateInstallButton")).IsEnabled, "Removing restart consent immediately disables installation");
            var thirdPartyCandidate = new ThirdPartyUpdateCandidate("Vendor.Tool", "Vendor Tool", "1.0", "winget", ["2.0", "1.5"]);
            var setThirdPartyResult = typeof(KillerBench.MainWindow).GetMethod("SetThirdPartyUpdateResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            setThirdPartyResult.Invoke(window, [new ThirdPartyUpdateResult(true, "1.11.0", true, 1, [thirdPartyCandidate], "fixture")]);
            var thirdPartyList = (ListBox)window.FindName("ThirdPartyUpdateList");
            thirdPartyList.SelectedIndex = 0;
            Require(((ComboBox)window.FindName("ThirdPartyUpdateVersion")).SelectedItem as string == "2.0" &&
                ((Button)window.FindName("ThirdPartyUpdatePreview")).IsEnabled && !((Button)window.FindName("ThirdPartyUpdateInstall")).IsEnabled,
                "Third-party application selection defaults to an offered version and requires agreement acceptance");
            ((CheckBox)window.FindName("ThirdPartyUpdateAgreements")).IsChecked = true;
            Require(((Button)window.FindName("ThirdPartyUpdateInstall")).IsEnabled,
                "Exact eligible third-party selection enables installation only after agreement acceptance");
            setThirdPartyResult.Invoke(window, [null]);
            updateSnapshotField.SetValue(window, null);
            ((CheckBox)window.FindName("UpdateDownloadedOnly")).IsChecked = false;
            Require(window.Width / window.Height > 1.4, "Workbench opens in a landscape layout");
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
            window.Left = SystemParameters.VirtualScreenLeft - Math.Max(window.Width, window.MinWidth) - 200;
            window.Top = SystemParameters.VirtualScreenTop;
            IsolatedDesktop.Verify();
            window.Show();
            window.Left = SystemParameters.VirtualScreenLeft - Math.Max(window.Width, window.MinWidth) - 200;
            var text = (TextBox)window.FindName("EvidenceBox");
            window.UpdateLayout();
            Require(window.Left + window.ActualWidth < SystemParameters.VirtualScreenLeft && !window.ShowActivated && !window.ShowInTaskbar,
                "UI validation stays outside the desktop without activation or a taskbar entry. Left=" + window.Left +
                ", width=" + window.ActualWidth + ", screenLeft=" + SystemParameters.VirtualScreenLeft +
                ", ShowActivated=" + window.ShowActivated + ", ShowInTaskbar=" + window.ShowInTaskbar);
            Require(text.Text.Contains("START HERE") && text.Text.Contains("Full system scan is selected by default") &&
                ((ListBox)window.FindName("SourcesList")).Items.Count == 0,
                "Startup explains the diagnostic-first full scan without pretending that sample evidence is machine evidence");
            var controlBank = (FrameworkElement)window.FindName("LeftPanel");
            var consoleBank = (FrameworkElement)window.FindName("ConsoleBank");
            var controlOrigin = controlBank.TranslatePoint(new Point(), window);
            var consoleOrigin = consoleBank.TranslatePoint(new Point(), window);
            var rail = (FrameworkElement)window.FindName("SidebarRail");
            var railOrigin = rail.TranslatePoint(new Point(), window);
            Require(railOrigin.X >= controlOrigin.X + controlBank.ActualWidth - 1 &&
                railOrigin.X + rail.ActualWidth <= consoleOrigin.X + 1,
                "Icon rail remains entirely between the tool sidebar and main pane");
            Require(consoleOrigin.X >= controlOrigin.X + controlBank.ActualWidth && Math.Abs(consoleOrigin.Y - controlOrigin.Y) < 1,
                "Control bank sits beside the console, not above it");
            var categories = (ListBox)window.FindName("CategoryList");
            var categoryOrigin = categories.TranslatePoint(new Point(), window);
            Require(categoryOrigin.X >= controlOrigin.X - 1 && categoryOrigin.X + categories.ActualWidth <= controlOrigin.X + controlBank.ActualWidth + 1,
                "Section navigation lives in the left sidebar");
            Require(Grid.GetColumnSpan(categories) == 1 && ((FrameworkElement)window.FindName("DetailsPanel")).Visibility == Visibility.Collapsed &&
                ((Button)window.FindName("ExportReportButton")).Visibility == Visibility.Visible,
                "Category bank leads to the report workspace with a visible export action and no inspector sidebar");
            Require(window.FindName("ConsoleViewButton") is Button { Content: "Raw output" } &&
                window.FindName("ReportViewButton") is Button { Content: "Readable report" },
                "Output view controls distinguish technical evidence from the readable report");
            var shortcuts = (Border)window.FindName("ShortcutsOverlay");
            ((Button)window.FindName("ShortcutButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            ((Button)window.FindName("ShortcutListButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(shortcuts.Visibility == Visibility.Visible &&
                ((StackPanel)window.FindName("ShortcutListHost")).Children.OfType<Grid>().Count() == 8 &&
                ((Button)window.FindName("ShortcutButton")).ToolTip?.ToString() == "Keyboard shortcuts (F1)",
                "Shortcut list and rail tooltip come from the shared binding table");
            ((Button)window.FindName("ShortcutMapButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(((StackPanel)window.FindName("ShortcutMapHost")).Visibility == Visibility.Visible &&
                ((StackPanel)window.FindName("ShortcutKeyboardRows")).Children.Count == 5 &&
                ((Button)window.FindName("ShortcutMapButton")).Tag?.ToString() == "on",
                "Keyboard view renders the shared bindings and records the selected view");
            ((Button)window.FindName("ShortcutCtrlButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(((Button)window.FindName("ShortcutCtrlButton")).Tag?.ToString() == "on",
                "Keyboard map exposes the Ctrl layer for modified shortcuts");
            Capture(window, Path.Combine(scratch, "shortcut-keyboard.png"));
            ((Button)window.FindName("ShortcutButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(shortcuts.Visibility == Visibility.Collapsed,
                "Shortcut rail button closes the overlay without changing the workspace");
            var categoryTools = (ComboBox)window.FindName("CategoryWorkflow");
            var allTools = (ComboBox)window.FindName("MainWorkflow");
            Require(categories.Items.Cast<object>().Select(category => category.ToString()).SequenceEqual(["Diagnostics", "Repair", "Updates", "Devices", "Network", "Apps", "Maintenance", "Onboarding", "Security", "BitLocker", "Privacy", "Storage"]),
                "Twelve primary sections list in the left sidebar while preserving the existing category order");
            CategoryTypographyChecks.Run(window);
            var mainAction = (Button)window.FindName("MainGo");
            var automaticRepair = (CheckBox)window.FindName("MainAutoRepair");
            Require(categories.SelectedItem?.ToString() == "Diagnostics" && allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.CommonProblems } &&
                mainAction.Content is "Run diagnosis" && mainAction.ToolTip is string { Length: > 0 },
                "KillerBench opens on Diagnostics with the diagnosis-first Full system scan action");
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "Repair");
            Pump();
            Require(allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.CoreWindowsServices } && automaticRepair.IsEnabled,
                "Repair stays directly beside Diagnostics and requires a separate explicit approval");
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "Storage");
            Pump();
            Require(allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Storage } &&
                categoryTools.Items.Cast<WorkflowDefinition>().Select(tool => tool.Kind).SequenceEqual(
                    [WorkflowKind.Storage, WorkflowKind.StorageSpaces, WorkflowKind.StorageService, WorkflowKind.OptimizeDrivesService]),
                "Storage opens its four existing workflows without adding or duplicating a tool");
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "Repair");
            Pump();
            automaticRepair.IsChecked = true;
            Require(mainAction.Content is "Run repair", "Primary action changes only after explicit repair approval");
            automaticRepair.IsChecked = false;
            var reachableTools = new HashSet<WorkflowKind>();
            window.UpdateLayout();
            var stableWindowSize = new Size(window.ActualWidth, window.ActualHeight);
            var stableWorkspace = (FrameworkElement)window.FindName("ScaleHost");
            var stableWorkspaceSize = new Size(stableWorkspace.ActualWidth, stableWorkspace.ActualHeight);
            double stableConsoleHeight = consoleBank.ActualHeight;
            Require(window.FindName("ConsolePane") is System.Windows.Controls.Border { BorderThickness: var consoleBorderThickness, Effect: System.Windows.Media.Effects.DropShadowEffect } &&
                consoleBorderThickness == new Thickness(1) && window.FindName("Scanlines") is null && window.FindName("GlowSlider") is null,
                "Console uses one smoked-glass pane with a shadow and no CRT controls");
            for (int categoryIndex = 0; categoryIndex < categories.Items.Count; categoryIndex++)
            {
                categories.SelectedIndex = categoryIndex;
                foreach (WorkflowDefinition tool in categoryTools.Items.Cast<WorkflowDefinition>().ToArray())
                {
                    reachableTools.Add(tool.Kind);
                    categoryTools.SelectedItem = tool;
                    window.UpdateLayout();
                    if (tool.Kind == WorkflowKind.BackgroundTransfers)
                    {
                        Require(((StackPanel)window.FindName("BitsSettings")).IsVisible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "BITS review options appear without automatic transfer changes");
                        var bitsRaw = new ToolResult(0, "{\"scope\":\"CurrentAccount\",\"complete\":true,\"jobs\":[{\"id\":\"75a7703d-f0b2-441f-963c-600211b83772\",\"name\":\"Fixture transfer\",\"state\":\"Suspended\",\"error\":\"\"}]}", "", false);
                        window.GetType().GetMethod("SetBitsInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(window, [BitsDiagnostics.Parse(bitsRaw)]);
                        ((ComboBox)window.FindName("BitsSelection")).SelectedIndex = 0;
                        FindButtons(window).Single(button => Equals(button.Content, "Review selected transfer")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(text.Text.Contains("75a7703d-f0b2-441f-963c-600211b83772") && text.Text.Contains("may be intentional"), "BITS review retains exact target and suspension semantics");
                        Capture(window, Path.Combine(scratch, "bits-review.png"));
                        var retryConsent = (CheckBox)window.FindName("BitsRetryConsent");
                        Require(retryConsent.IsChecked != true, "BITS retry starts without consent");
                        Require(!retryConsent.IsEnabled && !((Button)window.FindName("BitsRetryButton")).IsEnabled,
                            "Suspended snapshot disables retry controls before execution");
                        ((Button)window.FindName("BitsRetryButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(((ComboBox)window.FindName("BitsSelection")).IsEnabled, "Unconfirmed retry does not start work");
                        retryConsent.IsChecked = true;
                        ((ComboBox)window.FindName("BitsScopeChoice")).SelectedIndex = 1;
                        Require(((ComboBox)window.FindName("BitsSelection")).Items.Count == 0, "BITS scope change clears stale selection");
                        ((ComboBox)window.FindName("BitsScopeChoice")).SelectedIndex = 0;
                        ((ListBox)window.FindName("SourcesList")).SelectedIndex = 0;
                    }
                    if (tool.Kind == WorkflowKind.InstalledApps)
                    {
                        Require(((StackPanel)window.FindName("InstalledApplicationSettings")).IsVisible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Application scope options visible without repair");
                        Require(window.FindName("ReviewBrowserExtensions") is Button { IsEnabled: true }, "Browser extension report handoff is available from installed applications");
                        ((ComboBox)window.FindName("InstalledApplicationScopeChoice")).SelectedIndex = 1;
                        window.UpdateLayout();
                        Capture(window, Path.Combine(scratch, "application-scope.png"));
                        ((ComboBox)window.FindName("InstalledApplicationScopeChoice")).SelectedIndex = 0;
                        var setApplications = window.GetType().GetMethod("SetApplicationInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                        setApplications.Invoke(window, [InstalledApplications.Interpret(new ToolResult(0, "{\"complete\":true,\"applications\":[]}", "", false))]);
                        Require(((Button)window.FindName("SaveApplicationInventory")).IsEnabled, "Collected inventory can be saved");
                        ((ComboBox)window.FindName("InstalledApplicationScopeChoice")).SelectedIndex = 1;
                        Require(!((Button)window.FindName("SaveApplicationInventory")).IsEnabled, "Changing scope clears stale inventory export");
                        ((ComboBox)window.FindName("InstalledApplicationScopeChoice")).SelectedIndex = 0;
                        string inventoryFile = Path.Combine(scratch, "application-inventory.json");
                        var inventoryEvidence = new ToolResult(0, "{\"scope\":\"Machine\",\"complete\":true,\"applications\":[]}", "", false);
                        File.WriteAllText(inventoryFile, System.Text.Json.JsonSerializer.Serialize(new { Evidence = inventoryEvidence }, KillerBench.Engine.Evidence.Capture.JsonOptions));
                        var compareMethod = window.GetType().GetMethod("CompareApplicationFilesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                        var comparisonTask = (Task)compareMethod.Invoke(window, [inventoryFile, inventoryFile])!;
                        var deadline = DateTime.UtcNow.AddSeconds(35);
                        while (!comparisonTask.IsCompleted && DateTime.UtcNow < deadline) Pump();
                        Require(comparisonTask.IsCompleted, "Desktop comparison completes within its bound");
                        comparisonTask.GetAwaiter().GetResult();
                        Require(text.Text.Contains("0 registration differences") && text.Text.Contains("not verified machine or account identity"), "Desktop comparison shows differences and provenance limits");
                        Capture(window, Path.Combine(scratch, "application-comparison.png"));
                        string comparisonText = text.Text;
                        comparisonTask = (Task)compareMethod.Invoke(window, [inventoryFile, inventoryFile + ".missing"])!;
                        deadline = DateTime.UtcNow.AddSeconds(35);
                        while (!comparisonTask.IsCompleted && DateTime.UtcNow < deadline) Pump();
                        Require(comparisonTask.IsCompleted, "Invalid comparison completes");
                        comparisonTask.GetAwaiter().GetResult();
                        Require(text.Text == comparisonText && ((Button)window.FindName("CompareApplicationInventories")).IsEnabled, "Unreadable comparison preserves evidence and restores controls");
                        ((ListBox)window.FindName("SourcesList")).SelectedIndex = 0;
                    }
                    if (tool.Kind == WorkflowKind.AppPackages)
                    {
                        Require(((StackPanel)window.FindName("AppPackageSettings")).IsVisible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled,
                            "Package selection appears without auto-repair controls");
                        const string packageJson = "{\"complete\":true,\"packages\":[{\"name\":\"Fixture\",\"fullName\":\"Fixture_1\",\"familyName\":\"Fixture\",\"framework\":false,\"nonRemovable\":true,\"developmentMode\":false,\"dependencies\":[]}]}";
                        var inventory = AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, new ToolResult(0, packageJson, "", false));
                        var setInventory = window.GetType().GetMethod("SetAppPackageInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                        setInventory.Invoke(window, [inventory]);
                        ((ComboBox)window.FindName("AppPackageSelection")).SelectedIndex = 0;
                        FindButtons(window).Single(button => Equals(button.Content, "Review selected package")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(text.Text.Contains("Fixture_1") && text.Text.Contains("non-removable") && text.Text.Contains("RETAINED SNAPSHOT"),
                            "Selected package review retains exact identity, protection and snapshot limitations");
                        Capture(window, Path.Combine(scratch, "package-review.png"));
                        ((ComboBox)window.FindName("AppPackageScopeChoice")).SelectedIndex = 1;
                        Require(((ComboBox)window.FindName("AppPackageSelection")).Items.Count == 0, "Changing account scope clears retained package selection");
                        ((ComboBox)window.FindName("AppPackageScopeChoice")).SelectedIndex = 0;
                        setInventory.Invoke(window, [null]);
                        Require(((ComboBox)window.FindName("AppPackageSelection")).Items.Count == 0, "Package refresh clears old selections");
                        ((ListBox)window.FindName("SourcesList")).SelectedIndex = 0;
                    }
                    if (tool.Kind == WorkflowKind.Mace)
                    {
                        Require(((StackPanel)window.FindName("MaceSettings")).IsVisible &&
                            !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled,
                            "Microsoft app removal choices remain review-only in the main workflow");
                        var setMaceReview = window.GetType().GetMethod("SetMaceReview",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                        setMaceReview.Invoke(window, [MacePackageRemovalChecks.CreateReview(packagePresent: true)]);
                        var selection = (ComboBox)window.FindName("MacePackageSelection");
                        var removalConsent = (CheckBox)window.FindName("MacePackageRemovalConsent");
                        var removePackage = (Button)window.FindName("MaceRemovePackage");
                        var reconcilePackage = (Button)window.FindName("MaceReconcilePackage");
                        Require(selection.Items.Count == 1 && selection.SelectedIndex == 0 &&
                            ((Button)window.FindName("MaceReviewPackage")).IsEnabled && !removePackage.IsEnabled && reconcilePackage.IsEnabled,
                            "One supported exact package is presented as one selected plan choice with separate removal recovery");
                        ((Button)window.FindName("MaceReviewPackage")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(text.Text.Contains("APP REMOVAL PACKAGE PLAN / ELIGIBLE") &&
                            text.Text.Contains("Run diagnosis stays review-only"),
                            "App removal package plan remains separate from the destructive action");
                        removalConsent.IsChecked = true;
                        Require(removePackage.IsEnabled, "The red app removal package action needs explicit consent after an eligible review");
                        ((CheckBox)window.FindName("MaceOutlook")).IsChecked = true;
                        Require(selection.Items.Count == 0 && !((Button)window.FindName("MaceReviewPackage")).IsEnabled &&
                            !removePackage.IsEnabled && removalConsent.IsChecked != true,
                            "Changing a Microsoft app removal selection clears stale package eligibility and destructive consent");
                        ((ListBox)window.FindName("SourcesList")).SelectedIndex = 0;
                    }
                    if (tool.Kind == WorkflowKind.Orca)
                    {
                        var orcaRepair = (CheckBox)window.FindName("MainAutoRepair");
                        var orcaScope = (ComboBox)window.FindName("OrcaScopeChoice");
                        var orcaProfiles = (CheckBox)window.FindName("OrcaResetProfiles");
                        Require(((StackPanel)window.FindName("OrcaSettings")).IsVisible && orcaRepair.IsEnabled &&
                            orcaRepair.IsChecked != true, "Outlook cache-only running-account choice exposes guarded retention without initial consent");
                        orcaRepair.IsChecked = true;
                        orcaScope.SelectedIndex = 1;
                        Require(!orcaRepair.IsEnabled && orcaRepair.IsChecked != true,
                            "Outlook reset all-user review disables cache retention and clears prior consent");
                        orcaScope.SelectedIndex = 0;
                        Require(orcaRepair.IsEnabled, "Outlook reset running-account cache-only choice restores guarded retention");
                        orcaRepair.IsChecked = true;
                        orcaProfiles.IsChecked = true;
                        Require(!orcaRepair.IsEnabled && orcaRepair.IsChecked != true,
                            "Outlook reset mixed reset areas remain review-only and clear prior consent");
                        orcaProfiles.IsChecked = false;
                        Require(orcaRepair.IsEnabled, "Returning to Outlook cache-only review restores guarded retention");
                    }
                    if (tool.Kind == WorkflowKind.CommonProblems)
                    {
                        var recipePanel = (Expander)window.FindName("RecipeSelectionPanel");
                        var toggle = (CheckBox)window.FindName("CustomizeRecipes");
                        var choices = (ListBox)window.FindName("RecipeSelectionList");
                        Require(choices.ItemContainerStyle is not null && ((ListBox)window.FindName("DiagnosticAreaList")).ItemContainerStyle is not null,
                            "Advanced check lists use the themed list-row template");
                        recipePanel.IsExpanded = true;
                        toggle.IsChecked = true;
                        choices.UnselectAll();
                        choices.SelectedItems.Add(choices.Items.Cast<RepairRecipe>().Single(recipe => recipe.Id == "windows.system-files"));
                        window.UpdateLayout();
                        Require(((TextBlock)window.FindName("RecipeSelectionSummary")).Text.StartsWith("1 selected; 2 checks") &&
                            ((CheckBox)window.FindName("MainAutoRepair")).IsChecked != true, "Advanced selection explains prerequisites and leaves repairs off");
                        var effects = (TextBlock)window.FindName("RecipeEffectsSummary");
                        Require(effects.Text.Contains(Repairs.Plan(RepairKind.SystemFiles).Effects) &&
                            effects.Text.Contains(Repairs.Plan(RepairKind.ComponentStore).Effects) && !effects.Text.Contains("waiting print jobs"),
                            "Selected repair review includes prerequisite effects without unrelated actions");
                        var effectsPanel = (Expander)((ScrollViewer)effects.Parent).Parent;
                        effectsPanel.IsExpanded = true;
                        window.UpdateLayout();
                        Capture(window, Path.Combine(scratch, "recipe-selection.png"));
                        choices.UnselectAll();
                        Require(effects.Text == "No recipe checks selected.", "Empty selection clears the repair-effects review");
                        toggle.IsChecked = false;
                        Require(effects.Text.Contains("waiting print jobs"), "Full recipe review restores all action effects");
                        effectsPanel.IsExpanded = false;
                        recipePanel.IsExpanded = false;
                        window.UpdateLayout();
                    }
                    if (tool.Kind == WorkflowKind.AvailableUpdates)
                    {
                        var updateExpander = (Expander)((FrameworkElement)updateList.Parent).Parent;
                        updateExpander.IsExpanded = true;
                        window.UpdateLayout();
                        Capture(window, Path.Combine(scratch, "update-selection.png"));
                        updateExpander.IsExpanded = false;
                        window.UpdateLayout();
                    }
                    Require(((FrameworkElement)window.FindName("TaskSettings")).IsVisible == (tool.Kind == WorkflowKind.ScheduledTasks),
                        "Saved task-plan review appears only for Scheduled tasks");
                    Require(((FrameworkElement)window.FindName("NetworkSettings")).IsVisible == (tool.Kind == WorkflowKind.Network),
                        "Hostname options appear only for Network");
                    Require(new Size(window.ActualWidth, window.ActualHeight) == stableWindowSize &&
                        new Size(stableWorkspace.ActualWidth, stableWorkspace.ActualHeight) == stableWorkspaceSize,
                        "Switching tools preserves window and workspace dimensions: " + tool.Kind);
                    Require(Math.Abs(consoleBank.ActualHeight - stableConsoleHeight) < 1,
                        "Tool content does not resize the main console row: " + tool.Kind);
                }
                Require(Equals(categoryTools.SelectedItem, allTools.SelectedItem), "Category selection preserves workflow identity");
            }
            Require(reachableTools.Count == WorkflowCatalog.All.Count, "Every workflow is reachable through a category");
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "Network");
            Require(categoryTools.Items.Cast<WorkflowDefinition>().First().Kind == WorkflowKind.Network &&
                categoryTools.Items.Cast<WorkflowDefinition>().Any(tool => tool.Kind == WorkflowKind.Wireless),
                "Network opens on the network diagnosis and holds the network services");
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "BitLocker");
            Require(categoryTools.Items.Cast<WorkflowDefinition>().Select(tool => tool.Kind).SequenceEqual([WorkflowKind.BitLocker]),
                "BitLocker is its own section");
            Require(((CheckBox)window.FindName("TaskRegistrationConsent")).IsChecked == false &&
                ((Button)window.FindName("TaskRegistrationButton")).Content is "Preview registration",
                "Desktop task registration defaults to preview and requires separate create consent");
            Require(((Button)window.FindName("TaskRegistrationRecoveryButton")).Content is "Check interrupted registration",
                "Desktop exposes exact read-only task registration recovery beside the draft");
            Require(((Button)window.FindName("MainGo")).IsEnabled && !((Button)window.FindName("CancelButton")).IsEnabled, "Navigation does not run a workflow");
            var setCaptureBusy = typeof(KillerBench.MainWindow).GetMethod("SetCaptureBusy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            setCaptureBusy.Invoke(window, [true]);
            Require(!((Button)window.FindName("MainGo")).IsEnabled && ((Button)window.FindName("CancelButton")).IsEnabled &&
                ((ListBox)window.FindName("CategoryList")).IsEnabled && ((ComboBox)window.FindName("CategoryWorkflow")).IsEnabled &&
                ((Button)window.FindName("ExportReportButton")).IsEnabled,
                "A running scan keeps Stop, navigation and report export available while preventing another run");
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "Maintenance");
            Pump();
            Require(((ComboBox)window.FindName("MainWorkflow")).SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Cleanup },
                "A running scan can open another category without starting another operation");
            var busyTools = (ComboBox)window.FindName("CategoryWorkflow");
            busyTools.SelectedItem = busyTools.Items.Cast<WorkflowDefinition>().Single(tool => tool.Kind == WorkflowKind.Profiles);
            Pump();
            Require(((ComboBox)window.FindName("MainWorkflow")).SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Profiles },
                "A running scan can browse another tool without enabling another run");
            setCaptureBusy.Invoke(window, [false]);
            categories.SelectedIndex = 0;
            Require(!((Expander)window.FindName("AdvancedTools")).IsExpanded, "Advanced tools start tucked away");
            Capture(window, Path.Combine(scratch, "simple-start.png"));
            var titleIcon = (Image)window.FindName("TitlebarIcon");
            Require(window.Icon is not null && titleIcon.Source is not null, "Window and titlebar use packaged icon resources");
            Require(Math.Abs(titleIcon.ActualWidth - 27) < .1 && Math.Abs(titleIcon.ActualHeight - 27) < .1 &&
                Equals(window.FindResource("TitleIconSize"), 27d), "Titlebar retains its approved 27-pixel family icon size");
            var stableTitleIconSize = new Size(titleIcon.ActualWidth, titleIcon.ActualHeight);
            var zoomHost = (FrameworkElement)window.FindName("ScaleHost");
            var zoomViewport = (Grid)window.FindName("ZoomViewport");
            var zoomMethod = typeof(KillerBench.MainWindow).GetMethod("ApplyAppScale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            foreach (double scale in new[] { 0.7, 1.0, 1.5, 2.5 })
            {
                zoomMethod.Invoke(window, [scale]);
                Pump();
                Require(Math.Abs(zoomHost.LayoutTransform.Value.M11 - scale) < .001 && Math.Abs(((TextBox)window.FindName("EvidenceBox")).FontSize - ((Slider)window.FindName("SizeSlider")).Value) < 0.001 &&
                    new Size(titleIcon.ActualWidth, titleIcon.ActualHeight) == stableTitleIconSize && Math.Abs(controlBank.Width * scale - 160) < 0.1 && Math.Abs(controlBank.ActualWidth - controlBank.Width) <= 1,
                    "Zoom scales content without squeezing the sidebar or moving titlebar controls. Scale=" + scale + ", transform=" + zoomHost.LayoutTransform.Value.M11 + ", sidebar=" + controlBank.ActualWidth + ", font=" + ((TextBox)window.FindName("EvidenceBox")).FontSize + ", slider=" + ((Slider)window.FindName("SizeSlider")).Value);
                Require(new Size(window.ActualWidth, window.ActualHeight) == stableWindowSize &&
                    zoomHost.ActualWidth * scale <= zoomViewport.ActualWidth + 1 && zoomHost.ActualHeight * scale <= zoomViewport.ActualHeight + 1,
                    "Every zoom level preserves the window and fits its workspace");
                if (scale == 2.5)
                {
                    Capture(window, Path.Combine(scratch, "zoom-250.png"));
                }
            }
            zoomMethod.Invoke(window, [1.0]);
            Pump();
            titleIcon.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, 120)
                { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent });
            Require(Math.Abs(zoomHost.LayoutTransform.Value.M11 - 1.02) < .001 && Math.Abs(((TextBox)window.FindName("EvidenceBox")).FontSize - ((Slider)window.FindName("SizeSlider")).Value) < 0.001 &&
                Equals(typeof(KillerBench.MainWindow).Assembly.GetType("KillerBench.Services.Preferences")!
                    .GetMethod("Get", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, ["AppScale"]), "1.02"),
                "Wheel over titlebar icon changes and persists zoom");
            titleIcon.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, -120)
                { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent });
            Pump();
            Require(Math.Abs(((TextBox)window.FindName("EvidenceBox")).FontSize - ((Slider)window.FindName("SizeSlider")).Value) < 0.001, "Reverse wheel restores zoom");
            Capture(window, Path.Combine(scratch, "icon-and-zoom.png"));
            var workflowPicker = (ComboBox)window.FindName("MainWorkflow");
            workflowPicker.SelectedIndex = (int)WorkflowKind.Printing;
            var printerQueues = (ComboBox)window.FindName("PrinterQueues");
            printerQueues.ItemsSource = new[] { "Synthetic local printer" };
            printerQueues.SelectedIndex = 0;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("PrinterSettings")).Visibility == Visibility.Visible &&
                ((CheckBox)window.FindName("MainAutoRepair")).IsEnabled &&
                ((CheckBox)window.FindName("MainAutoRepair")).IsChecked == false &&
                ((CheckBox)window.FindName("MainAutoRepair")).Content.ToString()!.Contains("waiting jobs"),
                "Selected printer exposes optional repair with explicit effects and no initial consent");
            Capture(window, Path.Combine(scratch, "printer-options.png"));
            ((CheckBox)window.FindName("MainAutoRepair")).IsChecked = true;
            FindButtons(window).Single(b => Equals(b.Content, "Use general printing checks")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(printerQueues.SelectedIndex == -1 && ((CheckBox)window.FindName("MainAutoRepair")).IsChecked == false,
                "Explicit general-printing action clears selected scope and repair consent");
            Require(((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Clearing printer selection restores general workflow controls");
            workflowPicker.SelectedIndex = (int)WorkflowKind.Cleanup;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("CleanupSettings")).Visibility == Visibility.Visible &&
                !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Cleanup exposes settings and disables mutation");
            Capture(window, Path.Combine(scratch, "cleanup-options.png"));
            Require(((CheckBox)window.FindName("CleanupShaderCaches")).IsChecked == false, "Shader cleanup remains optional");
            Require(((CheckBox)window.FindName("CleanupOfficeCaches")).IsChecked == false, "Office cache cleanup remains optional");
            Require(((CheckBox)window.FindName("CleanupThumbnailCache")).IsChecked == false, "Thumbnail cache cleanup remains optional");
            Require(((CheckBox)window.FindName("CleanupIconCache")).IsChecked == false, "Icon cache cleanup remains optional");
            Require(((CheckBox)window.FindName("CleanupErrorReports")).IsChecked == false, "Error-report cleanup remains optional");
            Require(((CheckBox)window.FindName("CleanupCrashDumps")).IsChecked == false, "Crash-dump cleanup remains optional");
            Require(((CheckBox)window.FindName("CleanupWindowsMinidumps")).IsChecked == false, "Windows minidump cleanup remains optional");
            workflowPicker.SelectedIndex = (int)WorkflowKind.Defender;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("SecuritySettings")).Visibility == Visibility.Visible &&
                ((StackPanel)window.FindName("CleanupSettings")).Visibility == Visibility.Collapsed, "Security choices replace cleanup choices");
            var defenderScanChoice = (ComboBox)window.FindName("DefenderScanChoice");
            var defenderScanConsent = (CheckBox)window.FindName("DefenderScanConsent");
            Require(FindButtons(window).Any(button => Equals(button.Content, "View Defender overview") && button.IsVisible),
                "Defender workflow exposes a read-only overview report action");
            Require(defenderScanChoice.SelectedIndex == 0 && defenderScanConsent.IsChecked == false && !defenderScanConsent.IsEnabled,
                "Defender scan controls default to diagnosis only without execution consent");
            defenderScanChoice.SelectedIndex = 1;
            Require(defenderScanConsent.IsEnabled && defenderScanConsent.IsChecked == false && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled,
                "Selecting a Defender scan enables separate consent and disables generic automatic repair");
            defenderScanConsent.IsChecked = true;
            defenderScanChoice.SelectedIndex = 2;
            Require(defenderScanConsent.IsChecked == false, "Changing Defender scan type clears execution consent");
            Capture(window, Path.Combine(scratch, "security-options.png"));
            workflowPicker.SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.FirewallServices);
            window.UpdateLayout();
            var firewallSettings = (StackPanel)window.FindName("FirewallProfileSettings");
            var firewallPrivate = (CheckBox)window.FindName("FirewallPrivateProfile");
            var firewallApply = (CheckBox)window.FindName("FirewallProfileApply");
            Require(firewallSettings.Visibility == Visibility.Visible && firewallApply.IsChecked == false,
                "Firewall workflow exposes separate unchecked profile consent");
            firewallPrivate.IsChecked = true;
            firewallApply.IsChecked = true;
            Require(((CheckBox)window.FindName("MainAutoRepair")).IsChecked == false,
                "Firewall profile consent remains separate from automatic service repair");
            firewallPrivate.IsChecked = false;
            Require(firewallApply.IsChecked == false, "Changing firewall profile targets clears consent");
            workflowPicker.SelectedIndex = (int)WorkflowKind.AvailableUpdates;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("UpdateSettings")).Visibility == Visibility.Visible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Update discovery exposes choices without enabling installation");
            var updateSoftware = (CheckBox)window.FindName("UpdateSoftware");
            var updateDrivers = (CheckBox)window.FindName("UpdateDrivers");
            var updateSource = (ComboBox)window.FindName("UpdateSourceChoice");
            updateSource.SelectedIndex = 2;
            updateSoftware.IsChecked = false;
            Require(!updateSource.IsEnabled && updateSource.SelectedIndex == 0, "Third-party-only discovery disables the unused Windows Update source");
            updateDrivers.IsChecked = true;
            Require(updateSource.IsEnabled, "Selecting drivers restores the Windows Update source");
            updateDrivers.IsChecked = false;
            updateSoftware.IsChecked = true;
            Capture(window, Path.Combine(scratch, "update-options.png"));
            workflowPicker.SelectedIndex = (int)WorkflowKind.Privacy;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("PrivacySettings")).Visibility == Visibility.Visible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Privacy exposes read-only selections");
            var privacyAreas = (ListBox)window.FindName("PrivacyAreas");
            Require(privacyAreas.Items.Count == PrivacyCatalog.All.Count && privacyAreas.SelectedItems.Count == 0, "Privacy areas start unselected");
            var cameraChoice = PrivacyCatalog.All.Single(c => c.Area == PrivacyArea.Camera);
            privacyAreas.SelectedItems.Add(cameraChoice);
            window.UpdateLayout();
            Require(privacyAreas.SelectedItems.Count == 1 && privacyAreas.SelectedItems[0] is PrivacyChoice { Area: PrivacyArea.Camera }, "Privacy selection remains isolated");
            Capture(window, Path.Combine(scratch, "privacy-options.png"));
            workflowPicker.SelectedIndex = (int)WorkflowKind.BitLocker;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("BitLockerSettings")).Visibility == Visibility.Visible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "BitLocker exposes preview options without automatic encryption");
            Require(((ComboBox)window.FindName("BitLockerEncryptionChoice")).SelectedItem is BitLockerEncryption.XtsAes128 && ((ComboBox)window.FindName("BitLockerDestination")).SelectedItem is RecoveryDestination.None, "BitLocker preview defaults");
            Capture(window, Path.Combine(scratch, "bitlocker-options.png"));
            var bitLockerActions = (ComboBox)window.FindName("BitLockerActionChoice");
            var bitLockerDecryptConsent = (CheckBox)window.FindName("BitLockerDecryptConsent");
            Require(bitLockerActions.Items.Count == 6 && ((ComboBoxItem)bitLockerActions.SelectedItem).Tag as string == "auto-unlock" &&
                window.FindName("BitLockerActionRun") is Button { IsEnabled: false } && window.FindName("BitLockerActionConsent") is CheckBox { IsChecked: false } &&
                bitLockerDecryptConsent.Visibility == Visibility.Collapsed,
                "More BitLocker actions start on auto-unlock with Run disabled until the change is confirmed");
            bitLockerActions.SelectedIndex = 5;
            window.UpdateLayout();
            Require(bitLockerDecryptConsent.Visibility == Visibility.Visible && bitLockerDecryptConsent.IsChecked == false,
                "Decryption shows its separate unprotected confirmation");
            ((Expander)window.FindName("BitLockerActionsExpander")).IsExpanded = true;
            window.UpdateLayout();
            Capture(window, Path.Combine(scratch, "bitlocker-actions.png"));
            ((Expander)window.FindName("BitLockerActionsExpander")).IsExpanded = false;
            bitLockerActions.SelectedIndex = 0;
            workflowPicker.SelectedIndex = (int)WorkflowKind.ComponentCleanup;
            window.UpdateLayout();
            Require(((TextBlock)window.FindName("ComponentCleanupSettings")).Visibility == Visibility.Visible && ((CheckBox)window.FindName("MainAutoRepair")).Content is "Run recommended cleanup", "Component cleanup exposes its maintenance choice");
            Capture(window, Path.Combine(scratch, "component-options.png"));
            workflowPicker.SelectedIndex = (int)WorkflowKind.Storage;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("StorageSettings")).Visibility == Visibility.Visible &&
                ((ComboBox)window.FindName("StorageVolumeChoice")).SelectedIndex == 0 &&
                !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Storage exposes volume selection without automatic repair");
            var storageReview = (CheckBox)window.FindName("StorageRepairReview");
            var storageMode = (ComboBox)window.FindName("StorageRepairModeChoice");
            var storageDismount = (CheckBox)window.FindName("StorageAllowDismount");
            Require(storageReview.IsChecked != true && storageMode.SelectedIndex == 0 && !storageMode.IsEnabled && !storageDismount.IsEnabled,
                "Filesystem repair review is explicit and defaults to spot repair");
            storageReview.IsChecked = true;
            window.UpdateLayout();
            var storageAutoRepair = (CheckBox)window.FindName("MainAutoRepair");
            Require(storageMode.IsEnabled && storageDismount.IsEnabled && storageAutoRepair.IsEnabled && storageAutoRepair.IsChecked != true,
                "Filesystem review options expose guarded repair without preselecting it");
            storageAutoRepair.IsChecked = true;
            storageDismount.IsChecked = true;
            window.UpdateLayout();
            Require(storageAutoRepair.IsChecked != true, "Changing filesystem repair consent clears execution consent");
            Capture(window, Path.Combine(scratch, "storage-options.png"));
            var storageChoice = (ComboBox)window.FindName("StorageVolumeChoice");
            storageChoice.ItemsSource = new[] { "Windows volume", "Z:" };
            storageChoice.SelectedIndex = 1;
            workflowPicker.SelectedIndex = (int)WorkflowKind.ComponentCleanup;
            workflowPicker.SelectedIndex = (int)WorkflowKind.Storage;
            window.UpdateLayout();
            Require(storageChoice.SelectedItem is "Z:", "Refreshing drive choices never silently changes the selected target");
            workflowPicker.SelectedIndex = (int)WorkflowKind.Profiles;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("ProfileSettings")).Visibility == Visibility.Visible && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Profile review exposes options without automatic deletion");
            Require(((Slider)window.FindName("ProfileAge")).Value == 90, "Profile age defaults to 90 days");
            Capture(window, Path.Combine(scratch, "profile-options.png"));
            workflowPicker.SelectedIndex = (int)WorkflowKind.ComputerName;
            window.UpdateLayout();
            Require(((StackPanel)window.FindName("ComputerNameSettings")).Visibility == Visibility.Visible &&
                !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled, "Computer name review exposes input without rename consent");
            var proposedName = (TextBox)window.FindName("ProposedComputerName");
            proposedName.Text = "invalid name";
            string evidenceBeforeName = ((TextBox)window.FindName("EvidenceBox")).Text;
            ((Button)window.FindName("MainGo")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            Require(((TextBox)window.FindName("EvidenceBox")).Text == evidenceBeforeName && ((Button)window.FindName("MainGo")).IsEnabled,
                "Invalid computer name does not begin collection or replace evidence");
            proposedName.Text = "OFFICE-PC";
            Capture(window, Path.Combine(scratch, "computer-name-options.png"));
            if (Environment.GetEnvironmentVariable("KILLERBENCH_CHECK_NAME_LIVE") == "1")
            {
                proposedName.Text = Environment.MachineName;
                ((Button)window.FindName("MainGo")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var deadline = DateTime.UtcNow.AddSeconds(40);
                while (!((Button)window.FindName("MainGo")).IsEnabled && DateTime.UtcNow < deadline) Pump();
                string nameOutput = ((TextBox)window.FindName("EvidenceBox")).Text;
                Require(((Button)window.FindName("MainGo")).IsEnabled && nameOutput.Contains("Name review completed; no rename performed") &&
                    nameOutput.Contains("Current name: " + Environment.MachineName), "Live desktop name review completes and restores Run diagnosis");
                Capture(window, Path.Combine(scratch, "computer-name-result.png"));
                Console.WriteLine("PASS: opt-in native read-only computer name review through desktop Run diagnosis.");
                typeof(KillerBench.MainWindow).GetMethod("OnSample", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, [window, new RoutedEventArgs()]);
            }
            var mainWorkflowForTime = (ComboBox)window.FindName("MainWorkflow");
            var previousWorkflowForTime = mainWorkflowForTime.SelectedItem;
            mainWorkflowForTime.SelectedItem = WorkflowCatalog.All.Single(w => w.Kind == WorkflowKind.TimeService);
            var timeSource = (ComboBox)window.FindName("TimeSourceChoice");
            timeSource.SelectedIndex = 2;
            ((TextBox)window.FindName("TimePeers")).Text = "time.example.test";
            Require(((StackPanel)window.FindName("TimeSettings")).Visibility == Visibility.Visible &&
                ((TextBox)window.FindName("TimePeers")).IsEnabled && !((CheckBox)window.FindName("MainAutoRepair")).IsEnabled,
                "Custom time source exposes peer input and blocks automatic mutation");
            Capture(window, Path.Combine(scratch, "time-options.png"));
            var timeApply = (CheckBox)window.FindName("TimeApply");
            Require(timeApply.IsEnabled && timeApply.IsChecked == false, "Custom time changes require separate unchecked consent");
            timeApply.IsChecked = true;
            ((TextBox)window.FindName("TimePeers")).Text = "other.example.test";
            Require(timeApply.IsChecked == false, "Changing time targets clears consent");
            timeApply.IsChecked = true;
            timeSource.SelectedIndex = 1;
            Require(timeApply.IsEnabled && timeApply.IsChecked == false && !((TextBox)window.FindName("TimePeers")).IsEnabled,
                "Domain hierarchy requires separate consent without peer input");
            timeSource.SelectedIndex = 0;
            Require(!((TextBox)window.FindName("TimePeers")).IsEnabled && ((CheckBox)window.FindName("MainAutoRepair")).IsEnabled,
                "Existing time source restores normal workflow");
            mainWorkflowForTime.SelectedItem = previousWorkflowForTime;
            var profileList = (ListBox)window.FindName("ProfileSelection");
            var profileFixture = new ProfileSnapshot("S-1-5-21-1-2-3-1001", @"C:\Users\Fixture", false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
            profileList.ItemsSource = new[] { profileFixture, profileFixture with { Sid = "S-1-5-21-1-2-3-1002", Path = @"C:\Users\SecondFixture" } };
            profileList.SelectedItems.Add(profileFixture);
            ((Expander)window.FindName("ProfileSelectionExpander")).IsExpanded = true;
            window.UpdateLayout();
            Require(profileList.SelectedItems.Count == 1 && ReferenceEquals(profileList.SelectedItem, profileFixture), "Profile selection preserves the exact record");
            Capture(window, Path.Combine(scratch, "profile-selection.png"));
            profileList.ItemsSource = null;
            workflowPicker.SelectedIndex = 0;
            ((Expander)window.FindName("AdvancedTools")).IsExpanded = true;
            ((Button)window.FindName("ActivityButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            typeof(KillerBench.MainWindow).GetMethod("LoadSample", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, null);
            Require(text.Text.Contains("SYNTHETIC SAMPLE"), "Theme changes start from explicitly loaded sample evidence");
            foreach (var theme in Enum.GetValues<KillerBench.Services.Theme>())
            {
                KillerBench.Services.ThemeManager.Apply(theme);
                window.UpdateLayout();
                CheckMenuSeparators(theme);
                double paneOpacity = (double)window.FindResource("PaneShadowOpacity");
                Require(paneOpacity == 0 ? window.TryFindResource("PaneShadowEffect") is null : window.TryFindResource("PaneShadowEffect") is System.Windows.Media.Effects.DropShadowEffect effect && effect.Opacity == paneOpacity, "Pane shadow follows theme, including flat themes");
                var caption = (Grid)window.FindName("CaptionBar");
                var captionPoint = caption.TranslatePoint(new Point(caption.ActualWidth / 2, caption.ActualHeight / 2), window);
                Require(ReferenceEquals(window.InputHitTest(captionPoint), caption), "Empty title bar receives drag input in " + theme);
                Require(text.Text.Contains("SYNTHETIC SAMPLE"), "Theme switch retains explicitly loaded sample evidence");
                Require(text.Foreground is SolidColorBrush brush && window.TryFindResource("ConsoleTextBrush") is SolidColorBrush consoleText && brush.Color == consoleText.Color,
                    "Console output follows the active theme");
                foreach (string key in new[] { "PrimaryBrush", "TextBrush", "PaneBrush", "ControlCornerRadius", "SliderTrack", "WindowEdgeThickness", "ConsoleSurfaceBrush", "ConsoleTextBrush", "ConsoleBorderBrush", "ConsoleShadowEffect" })
                    Require(window.TryFindResource(key) is not null, "Missing theme token: " + key);
                var snapshot = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                snapshot.Render(window);
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(snapshot));
                using var file = File.Create(Path.Combine(scratch, theme + ".png"));
                png.Save(file);
            }
            KillerBench.Services.ThemeManager.Apply(KillerBench.Services.Theme.Black);
            foreach (var family in new[] { KillerBench.Services.Theme.Dark, KillerBench.Services.Theme.Light, KillerBench.Services.Theme.Black, KillerBench.Services.Theme.SE98 })
                foreach (var accent in Enum.GetValues<KillerBench.Services.Accent>())
                {
                    KillerBench.Services.ThemeManager.Apply(family);
                    KillerBench.Services.ThemeManager.ApplyAccent(family, accent);
                    window.UpdateLayout();
                    Require(KillerBench.Services.ThemeManager.AccentChoiceFor(family) == accent, "Accent selection retained");
                }
            KillerBench.Services.ThemeManager.ApplyAccent(KillerBench.Services.Theme.Black, KillerBench.Services.Accent.Green);
            KillerBench.Services.ThemeManager.Apply(KillerBench.Services.Theme.Black);
            ((Button)window.FindName("SidebarButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((Button)window.FindName("SidebarButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((Button)window.FindName("ActivityButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(((Border)window.FindName("ActivityPanel")).Visibility == Visibility.Collapsed, "Activity hides independently");
            ((Button)window.FindName("ActivityButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((Button)window.FindName("ThemeButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(((Button)window.FindName("ThemeButton")).ContextMenu.IsOpen, "Theme menu opens");
            Pump();
            Capture(((Button)window.FindName("ThemeButton")).ContextMenu, Path.Combine(scratch, "theme-menu.png"));
            ((Button)window.FindName("ThemeButton")).ContextMenu.IsOpen = false;
            ((Button)window.FindName("LangButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            var languages = ((Button)window.FindName("LangButton")).ContextMenu;
            Capture(languages, Path.Combine(scratch, "language-menu.png"));
            var languageColumns = ((StackPanel)languages.Items[0]).Children.OfType<StackPanel>().ToArray();
            var languageButtons = languageColumns.SelectMany(column => column.Children.OfType<RadioButton>()).ToArray();
            var supportedLocales = Enum.GetValues<KillerBench.Services.Locale>();
            Require(languageColumns.Length == 2 &&
                    Math.Abs(languageColumns[0].Children.OfType<RadioButton>().Count() -
                             languageColumns[1].Children.OfType<RadioButton>().Count()) <= 1 &&
                    languageButtons.Length == supportedLocales.Length &&
                    supportedLocales.All(locale => languageButtons.Count(button => Equals(button.Tag, locale)) == 1),
                "Every supported language appears exactly once in two balanced columns");
            languages.IsOpen = false;
            foreach (var locale in Enum.GetValues<KillerBench.Services.Locale>())
            {
                KillerBench.Services.LocaleManager.Apply(locale);
                var openLog = (string)application.FindResource("Str_Content_Open_log");
                Require(locale == KillerBench.Services.Locale.EnUS
                        ? openLog == "Open log"
                        : openLog != "Open log",
                    "Every non-English locale overrides ordinary UI text");
                if (locale != KillerBench.Services.Locale.EnUS)
                {
                    var dictionaries = application.Resources.MergedDictionaries;
                    Require(dictionaries.Count > 4 && dictionaries[4].Count == dictionaries[3].Count,
                        "Every non-English locale has complete resource coverage");
                }
            }
            KillerBench.Services.LocaleManager.Apply(KillerBench.Services.Locale.EnUS);
            Require(((Border)window.FindName("LeftPanel")).Style != window.FindResource("Panel") && ((Border)window.FindName("LeftPanel")).Background == window.FindResource("SidebarPaneBrush"),
                "Tool sidebar sits on the window through SidebarPaneBrush like KillerScan");
            Require(((TextBox)window.FindName("PathBox")).Padding.Top <= 3, "Compact text input padding");
            window.UpdateLayout();
            var retainedSampleSources = ((ListBox)window.FindName("SourcesList")).Items.Cast<ListBoxItem>()
                .Count(item => item.Tag is TextEvidence { Source: "Synthetic demonstration" });
            Require(text.Text.Contains("SYNTHETIC SAMPLE") && retainedSampleSources == 1,
                "Sample evidence remains available after theme and localization changes");
            ((TextBox)window.FindName("SearchBox")).Text = "error";
            var find = FindButtons(window).Single(b => Equals(b.Content, "FIND"));
            find.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text.Contains("6  [10:42:06]") && !text.Text.Contains("Sample servicing"), "UI search retains source lines");
            ((TextBox)window.FindName("SearchBox")).Clear();
            find.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            string preview = Path.Combine(scratch, "KillerBench-preview.png");
            using (var output = File.Create(preview)) encoder.Save(output);
            var advanced = (Expander)window.FindName("AdvancedTools");
            advanced.IsExpanded = true;
            window.UpdateLayout();
            var windowsLookup = FindButtons(window).Single(b => Equals(b.Content, "Look up Windows system code"));
            var knowledgeCode = (TextBox)window.FindName("KnowledgeCode");
            string beforeLookup = text.Text;
            ((Button)window.FindName("KnowledgeSearchButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text == beforeLookup, "Empty reference search preserves evidence without opening a picker");
            var referenceDay = new DateOnly(2026, 9, 13);
            var searchEntry = new KnowledgeEntry("fixture", "5", "Fixture title", "Fixture explanation", "https://example.com/reference", referenceDay, "Fixture product");
            var searchCatalog = KnowledgeCatalog.Read(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                new KnowledgePack(1, "fixture", 1, referenceDay, "Synthetic", [searchEntry])));
            var formatSearch = typeof(KillerBench.MainWindow).GetMethod("FormatKnowledgeSearch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var searchEvidence = (TextEvidence)formatSearch.Invoke(null, [searchCatalog, "fixture", "Fixture", new KnowledgeSearchResult(2, [searchEntry])])!;
            Require(searchEvidence.Truncated && searchEvidence.Lines.Any(line => line.Contains("Showing 1 of 2")) &&
                searchEvidence.Lines.Any(line => line.Contains("https://example.com/reference")) &&
                searchEvidence.Lines.Any(line => line.Contains("Narrow the search")), "Desktop search retains provenance and omitted-result guidance");
            var emptySearch = (TextEvidence)formatSearch.Invoke(null, [searchCatalog, "fixture", "absent", new KnowledgeSearchResult(0, [])])!;
            Require(!emptySearch.Truncated && emptySearch.Lines.Any(line => line.Contains("No reference text matches")), "Empty reference search is explicit");
            knowledgeCode.Text = "not-a-code";
            windowsLookup.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text == beforeLookup, "Invalid Windows code preserves existing evidence");
            knowledgeCode.Text = "0x5";
            windowsLookup.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text.Contains("Code: 5 (0x5)") && text.Text.Contains("WINDOWS SYSTEM MESSAGE"),
                "Windows lookup button replaces sample with code reference");
            knowledgeCode.Text = "0x80240034";
            windowsLookup.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text.Contains("WINDOWS UPDATE RESULT CODE") && text.Text.Contains("WU_E_DOWNLOAD_FAILED") &&
                text.Text.Contains("BITS"), "Windows lookup adds the shared Windows Update reference and diagnostic route");
            knowledgeCode.Text = "0xFFFFFFFF";
            windowsLookup.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text.Contains("No system message found for this code."), "Unknown Windows code is explicitly unresolved");
            knowledgeCode.Clear();
            advanced.IsExpanded = false;
            ((Button)window.FindName("DemoButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int attempt = 0; attempt < 30 && ((TextBlock)window.FindName("EvidenceTitle")).Text != "CBS.log"; attempt++) Pump();
            Require(FindButtons((StackPanel)window.FindName("ReportContent")).Count(b => Equals(b.Content, "Show evidence")) == 2 &&
                ((Border)window.FindName("ReportPane")).IsVisible, "Demo import visibly populates two findings");
            Require(((StackPanel)window.FindName("FindingsPanel")).Visibility == Visibility.Visible, "Findings opens after import");
            window.UpdateLayout();
            var findingCards = (StackPanel)window.FindName("ReportContent");
            Require(FindButtons(findingCards).Count(b => Equals(b.Content, "Show evidence")) == 2, "One evidence action per finding");
            FindButtons(findingCards).Single(b => Equals(b.Tag, "KB-CBS-001")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.SelectedText.Contains("0x800f081f"), "Finding opens and selects original evidence line");
            find.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.Text.StartsWith("1  SYNTHETIC DEMO"), "Find restores complete evidence view after context");
            KillerBench.Services.ThemeManager.Apply(KillerBench.Services.Theme.Dark);
            Pump();
            Capture(window, Path.Combine(scratch, "investigation.png"));
            string installerFixture = Path.Combine(scratch, "installer-actions.log");
            File.WriteAllLines(installerFixture, ["MSI (s) (EC:BC) [12:34:56:123]: Starting action",
                "Synthetic package context", "Action ended 12:34:56: InstallFiles. Return value 3.",
                "MSI (s) (EC:BC) [12:34:56:124]: MainEngineThread is returning 1603"]);
            ((TextBox)window.FindName("PathBox")).Text = installerFixture;
            ((Button)window.FindName("OpenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int attempt = 0; attempt < 60 && ((TextBlock)window.FindName("EvidenceTitle")).Text != "installer-actions.log"; attempt++) Pump();
            Require(((TextBlock)window.FindName("EvidenceTitle")).Text == "installer-actions.log" &&
                FindButtons((StackPanel)window.FindName("ReportContent")).Count(b => Equals(b.Content, "Show evidence")) == 2, "MSI import populates action and main-engine findings");
            window.UpdateLayout();
            FindButtons(findingCards).Single(b => Equals(b.Tag, "KB-MSI-ACTION")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.SelectedText.Contains("InstallFiles. Return value 3.") && text.Text.Contains("Synthetic package context"),
                "MSI evidence action selects the failed action while displaying surrounding context");
            Capture(window, Path.Combine(scratch, "installer-action-evidence.png"));
            var activityPanel = (Border)window.FindName("ActivityPanel");
            activityPanel.Visibility = Visibility.Visible;
            window.UpdateLayout();
            var activityExport = (Button)window.FindName("ExportActivityButton");
            Require(activityExport.IsVisible && activityExport.IsEnabled && activityExport.ActualWidth > 0,
                "Session activity exposes an enabled export button");
            Capture(window, Path.Combine(scratch, "activity-export.png"));
            ((TextBox)window.FindName("PathBox")).Text = captureFixture;
            ((Button)window.FindName("OpenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int attempt = 0; attempt < 60 && ((TextBlock)window.FindName("EvidenceTitle")).Text != "Windows Update capture"; attempt++) Pump();
            Require(((TextBlock)window.FindName("EvidenceTitle")).Text == "Windows Update capture", "Saved capture opens through app workflow");
            Require(((Button)window.FindName("SaveCaptureButton")).IsEnabled, "Capture save available");
            var captureFindings = (StackPanel)window.FindName("FindingCards");
            Require(FindButtons(captureFindings).Count(b => Equals(b.Content, "Show evidence")) == 2, "Capture overview groups CBS and event findings");
            FindButtons(captureFindings).Single(b => Equals(b.Tag, "KB-CBS-001")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(text.SelectedText.Contains("0x800f081f"), "Capture finding selects the correct source line");
            ((ListBox)window.FindName("SourcesList")).SelectedIndex = ((ListBox)window.FindName("SourcesList")).Items.Count - 1;
            IsolatedDesktop.Verify();
            Require(window.Left + window.ActualWidth < SystemParameters.VirtualScreenLeft,
                "Screenshot fixtures remain outside the desktop");
            Capture(window, Path.Combine(scratch, "capture.png"));
            var grain = (Border)window.FindName("WorkspaceGrain");
            Require(grain.Margin.Top == 0 && Grid.GetRow(grain) == 1 && grain.ActualHeight >= ((Grid)window.FindName("ScaleHost")).ActualHeight, "Workspace grain covers margin below title bar");
            Require(window.TryFindResource("PaneShadowEffect") is System.Windows.Media.Effects.DropShadowEffect { BlurRadius: 16, ShadowDepth: 5 }, "Family pane shadow geometry");
            var panel = new Border { Style = (Style)window.FindResource("Panel") };
            window.UpdateLayout();
            Require(ReferenceEquals(panel.BorderBrush, window.FindResource("PaneBorderBrush")), "Pane uses family border resource");
            FindButtons(window).Single(b => Equals(b.Content, "Repairs")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(!((Button)window.FindName("RepairRun")).IsEnabled, "Repair requires explicit consent");
            Require(((TextBox)window.FindName("RepairPreview")).Text.Contains("TARGET: THIS PC") && ((TextBox)window.FindName("RepairPreview")).Text.Contains("/RestoreHealth"), "Repair preview names target and action");
            ((CheckBox)window.FindName("RepairConsent")).IsChecked = true;
            ((ComboBox)window.FindName("RepairChoice")).SelectedIndex = 1;
            Require(((CheckBox)window.FindName("RepairConsent")).IsChecked == false, "Changing action clears consent");
            Capture(window, Path.Combine(scratch, "repairs.png"));
            FindButtons(window).Single(b => Equals(b.Content, "Workflows")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(((Button)window.FindName("RepairRun")).IsEnabled && ((ComboBox)window.FindName("RepairChoice")).Items.Count == Enum.GetValues<WorkflowKind>().Length, "All workflows support diagnosis without repair consent");
            var mainChoices = (ComboBox)window.FindName("MainWorkflow");
            var advancedChoices = (ComboBox)window.FindName("RepairChoice");
            Require(WorkflowCatalog.All.Single(definition => definition.Kind == WorkflowKind.WindowsUpdate).Title == "Windows Update" &&
                WorkflowCatalog.All.Select(x => x.Kind).Order().SequenceEqual(Enum.GetValues<WorkflowKind>().Order()), "Workflow display text and complete enum coverage remain stable");
            Require(mainChoices.Items.Count == advancedChoices.Items.Count && mainChoices.Items.Cast<WorkflowDefinition>().Select(c => c.Title).SequenceEqual(advancedChoices.Items.Cast<ComboBoxItem>().Select(c => (string)c.Content)), "Main and advanced workflows have identical selection order");
            mainChoices.SelectedItem = WorkflowCatalog.All.Single(definition => definition.Kind == WorkflowKind.EventLog);
            Pump();
            Require(((StackPanel)window.FindName("EventLogSettings")).Visibility == Visibility.Visible &&
                ((ComboBox)window.FindName("EventChannelChoice")).Items.Count == EventLogCapacityDiagnostics.Channels.Count &&
                ((CheckBox)window.FindName("EventChannelEnableConsent")).IsChecked == false,
                "Event Log exposes one exact-channel selector with explicit enablement confirmation");
            mainChoices.SelectedItem = WorkflowCatalog.All.Single(definition => definition.Kind == WorkflowKind.Mace);
            Pump();
            var maceRemoval = (Button)window.FindName("MaceRemovePackage");
            Require(((StackPanel)window.FindName("MaceSettings")).Visibility == Visibility.Visible &&
                !maceRemoval.IsEnabled && Equals(maceRemoval.Content, "REMOVE SELECTED PACKAGE") &&
                ((CheckBox)window.FindName("MacePackageRemovalConsent")).IsChecked != true &&
                (maceRemoval.ToolTip?.ToString()?.Contains("second confirmation") ?? false),
                "Microsoft app removal keeps package removal as a separate disabled control until review, consent, and final confirmation");
            mainChoices.SelectedItem = WorkflowCatalog.All.Single(definition => definition.Kind == WorkflowKind.WindowsUpdate);
            Pump();
            ((CheckBox)window.FindName("RepairConsent")).IsChecked = true;
            ((ComboBox)window.FindName("RepairChoice")).SelectedIndex = 2;
            Pump();
            Require(((Button)window.FindName("RepairRun")).IsEnabled && ((CheckBox)window.FindName("RepairConsent")).IsChecked == false, "Workflow selection clears auto-repair but retains diagnose action");
            Capture(window, Path.Combine(scratch, "workflows.png"));
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            TaskDraftUiChecks.Run(window);
            ShellHandoffUiChecks.Run(window);
            typeof(KillerBench.MainWindow).GetMethod("BeginWorkflowOutput", flags)!.Invoke(window, [WorkflowKind.SystemFiles, false]);
            typeof(KillerBench.MainWindow).GetMethod("AppendWorkflowOutput", flags)!.Invoke(window, ["Verification 42% complete"]);
            Require(text.Text.Contains("Verification 42% complete") && !text.Text.Contains("SYNTHETIC SAMPLE") &&
                ((TextBlock)window.FindName("CoverageLabel")).Text.Contains("browse tools and export earlier reports", StringComparison.Ordinal),
                "Live output replaces sample evidence while retaining navigation and earlier report export guidance");
            Require(!((TextBox)window.FindName("SearchBox")).IsEnabled, "Search cannot replace live output");
            Require(window.FindName("ConsolePane") is Border { Visibility: Visibility.Visible } &&
                window.FindName("ReportPane") is Border { Visibility: Visibility.Collapsed } &&
                Equals(((Button)window.FindName("ConsoleViewButton")).Tag, "selected"),
                "A running workflow shows Raw output in the output pane even when an earlier report exists");
            Capture(window, Path.Combine(scratch, "live-workflow.png"));
            var completedWorkflow = new WorkflowResult(WorkflowKind.Services, "fixture", DateTimeOffset.UtcNow, "Needs attention",
                [new("Example service", CheckState.Attention, "Service needs review")], [], "fixture-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RegisterWorkflowResult", flags)!.Invoke(window, [completedWorkflow, "Focused service review", false]);
            var outcomeLabel = (TextBlock)window.FindName("CoverageLabel");
            Require(outcomeLabel.Text.Contains("Needs attention | 1 checks | 0 repairs run") && !outcomeLabel.Text.Contains("0 findings"),
                "Workflow result summary uses recorded checks instead of log-rule findings");
            Require(window.FindName("ReportPane") is Border { Visibility: Visibility.Visible } &&
                window.FindName("StartGuide") is TextBlock { Visibility: Visibility.Collapsed } &&
                ((StackPanel)window.FindName("ReportContent")).Children.OfType<Border>().Any(card => card.Child is StackPanel),
                "Completed workflows open a formatted report in the center pane, separate from the raw-output terminal");
            typeof(KillerBench.MainWindow).GetMethod("OnSourceSelected", flags)!.Invoke(window, [window, null]);
            Require(((StackPanel)window.FindName("ReportContent")).Children.OfType<TextBlock>().Any(block => block.Text == "Focused service review report"),
                "Reopening a workflow source retains its focused readable report title");
            ((Button)window.FindName("ConsoleViewButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(window.FindName("ConsolePane") is Border { Visibility: Visibility.Visible } &&
                window.FindName("TerminalPanel") is Border { Visibility: Visibility.Visible } && text.Text.Contains("Verification 42% complete"),
                "Console retains the raw live output after a report is created");
            ((TextBox)window.FindName("SearchBox")).Text = "no matching report text";
            typeof(KillerBench.MainWindow).GetMethod("ShowEvidence", flags)!.Invoke(window, null);
            Require(outcomeLabel.Text.Contains("Needs attention") && outcomeLabel.Text.Contains("0 matching report lines"),
                "Filtering does not replace the recorded workflow outcome");
            ((TextBox)window.FindName("SearchBox")).Clear();
            typeof(KillerBench.MainWindow).GetMethod("ShowEvidence", flags)!.Invoke(window, null);
            ((Button)window.FindName("ReportViewButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(window.FindName("ReportPane") is Border { Visibility: Visibility.Visible } &&
                ((StackPanel)window.FindName("ReportContent")).Children.OfType<TextBlock>().Any(block => block.Text == "Next step"),
                "Report view leads with a distinct next-action section");
            using var historicalRecord = JsonDocument.Parse(JsonSerializer.Serialize(completedWorkflow, KillerBench.Engine.Evidence.Capture.JsonOptions));
            var historicalSnapshot = new WorkflowJournalSnapshot("fixture-history.json", "fixture", "Services", "Needs attention",
                DateTimeOffset.UtcNow, 1, 0, historicalRecord.RootElement.Clone());
            var historicalReport = (WorkflowResult)typeof(KillerBench.MainWindow).GetMethod("BuildHistoricalWorkflowReport",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, [historicalSnapshot])!;
            Require(historicalReport.Status == "Historical evidence - current verification required" &&
                historicalReport.JournalPath == "fixture-history.json" && historicalReport.Actions.Count == 0 &&
                historicalReport.Checks.Any(check => check.Name == "Current health" && check.State == CheckState.Unknown),
                "Historical workflow review is a readable current-health-unknown report without an action replay");
            typeof(KillerBench.MainWindow).GetMethod("RenderNamedWorkflowReport", flags)!.Invoke(window, [historicalReport, "Saved workflow"]);
            Require(((StackPanel)window.FindName("ReportContent")).Children.OfType<TextBlock>().Any(block => block.Text == "Saved workflow report") &&
                ((StackPanel)window.FindName("ReportContent")).Children.OfType<Border>().Any(card => card.Child is StackPanel panel &&
                    panel.Children.OfType<TextBlock>().Any(block => block.Text == "Current health")),
                "Historical workflow evidence opens the readable report instead of raw JSON only");
            var largeWorkflow = completedWorkflow with
            {
                Checks = [.. Enumerable.Range(1, 16).Select(index => new WorkflowCheck("Review item " + index,
                    CheckState.Attention, "Fixture attention item"))]
            };
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", flags)!.Invoke(window, [largeWorkflow]);
            var reportContent = (StackPanel)window.FindName("ReportContent");
            Require(reportContent.Children.OfType<Border>().Count() == 13 &&
                reportContent.Children.OfType<TextBlock>().Any(block => block.Text.Contains("4 additional review items")),
                "Report caps the visible triage list while retaining the remaining review items");
            var prioritizedWorkflow = completedWorkflow with
            {
                Checks =
                [
                    new("Unknown item", CheckState.Unknown, "Fixture incomplete item"),
                    new("Attention item", CheckState.Attention, "Fixture attention item"),
                    new("Repairable item", CheckState.Repairable, "Fixture repairable item", RepairKind.SystemFiles)
                ]
            };
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", flags)!.Invoke(window, [prioritizedWorkflow]);
            string[] cardTitles = [.. ((StackPanel)window.FindName("ReportContent")).Children.OfType<Border>()
                .Select(card => ((StackPanel)card.Child).Children.OfType<TextBlock>().First().Text)];
            Require(cardTitles.Take(4).SequenceEqual(["Recommended next step", "Repairable item", "Attention item", "Unknown item"]),
                "Report orders actionable findings before attention and incomplete evidence");
            var repairNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review System files repair"));
            categories.SelectedItem = categories.Items.Cast<object>().Single(category => category.ToString() == "Diagnostics");
            Pump();
            repairNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(categories.SelectedItem?.ToString() == "Repair" && allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.SystemFiles } &&
                automaticRepair.IsChecked != true && window.FindName("ReportPane") is Border { Visibility: Visibility.Visible },
                "Actionable reports open the focused Repair option without approving or starting a repair");
            var defenderWorkflow = new WorkflowResult(WorkflowKind.Defender, "fixture", DateTimeOffset.UtcNow, "Needs attention",
                [new("Defender fixture", CheckState.Attention, "Security review required.")], [], "fixture-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", flags)!.Invoke(window, [defenderWorkflow]);
            var securityNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review security options"));
            securityNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(categories.SelectedItem?.ToString() == "Security" && allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Defender } &&
                automaticRepair.IsChecked != true,
                "Defender reports open Security options without selecting a scan or security action");
            var bitLockerWorkflow = new WorkflowResult(WorkflowKind.BitLocker, "fixture", DateTimeOffset.UtcNow, "Needs attention",
                [new("BitLocker fixture", CheckState.Attention, "Encryption review required.")], [], "fixture-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", flags)!.Invoke(window, [bitLockerWorkflow]);
            securityNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review security options"));
            securityNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(categories.SelectedItem?.ToString() == "BitLocker" && allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.BitLocker } &&
                automaticRepair.IsChecked != true,
                "BitLocker reports open Security options without selecting encryption or recovery-key actions");
            var networkWorkflow = new WorkflowResult(WorkflowKind.Network, "fixture", DateTimeOffset.UtcNow, "Needs attention",
                [new("Network fixture", CheckState.Attention, "Network review required.")], [], "fixture-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", flags)!.Invoke(window, [networkWorkflow]);
            var networkNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review network options"));
            networkNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(categories.SelectedItem?.ToString() == "Network" && allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.Network } &&
                automaticRepair.IsChecked != true,
                "Network reports open Network options without selecting a repair");
            var updateWorkflow = new WorkflowResult(WorkflowKind.AvailableUpdates, "fixture", DateTimeOffset.UtcNow, "Needs attention",
                [new("Windows Update fixture", CheckState.Attention, "Update review required.")], [], "fixture-journal.json");
            typeof(KillerBench.MainWindow).GetMethod("RenderWorkflowReport", flags)!.Invoke(window, [updateWorkflow]);
            var maintenanceNavigation = FindButtons(window).Single(button => Equals(button.Content, "Review maintenance options"));
            maintenanceNavigation.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Require(categories.SelectedItem?.ToString() == "Updates" && allTools.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.AvailableUpdates } &&
                automaticRepair.IsChecked != true,
                "Update reports open the exact Maintenance tool without selecting installation");
            Capture(window, Path.Combine(scratch, "workflow-outcome.png"));
            window.Close();
            Console.WriteLine("PASS: UI construction, sample labeling, console and report output, all 13 themes, panels, and theme menu.");
            Console.WriteLine("Preview: " + preview);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static async Task CheckEvidence(string scratch)
    {
        string path = Path.Combine(scratch, "sample.log");
        await File.WriteAllTextAsync(path, "first\r\nERROR example\rthird\nfourth");
        var evidence = await TextEvidence.ReadAsync(path, CancellationToken.None);
        Require(evidence.Lines.Length == 4 && !evidence.Truncated, "Mixed newline handling");
        string filtered = evidence.Excerpt("error", out int matches);
        Require(matches == 1 && filtered.StartsWith("2  ERROR"), "Case-insensitive search and original line number");
        await File.WriteAllTextAsync(path, new string('a', TextEvidence.MaximumCharacters + 20));
        evidence = await TextEvidence.ReadAsync(path, CancellationToken.None);
        Require(evidence.Truncated && evidence.Lines[0].Length == TextEvidence.MaximumCharacters, "Oversized source bounded and marked");
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        try { await TextEvidence.ReadAsync(path, cancel.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        await File.WriteAllBytesAsync(path, [65, 0, 66, 0, 67]);
        try { await TextEvidence.ReadAsync(path, CancellationToken.None); throw new Exception("Binary input accepted"); }
        catch (InvalidDataException) { }
        var many = new TextEvidence("synthetic", [.. Enumerable.Repeat("line", 3000)], false);
        Require(many.Excerpt("", out matches).Contains("Display limited") && matches == 3000, "Display bound is explicit");
        Console.WriteLine("PASS: newlines, search references, source bound, cancellation, binary rejection, display bound.");
    }

    private static IEnumerable<Button> FindButtons(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Button button) yield return button;
            foreach (var nested in FindButtons(child)) yield return nested;
        }
    }

    private static IEnumerable<TextBlock> FindTextBlocks(DependencyObject parent)
    {
        if (parent is TextBlock block) yield return block;
        int children = VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < children; index++)
            foreach (TextBlock nested in FindTextBlocks(VisualTreeHelper.GetChild(parent, index))) yield return nested;
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private static void Pump()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }
    private static void Capture(FrameworkElement visual, string path)
    {
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth), (int)Math.Ceiling(visual.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
