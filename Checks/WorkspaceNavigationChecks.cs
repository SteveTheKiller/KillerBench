using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;
using KillerBench.Services;
using KillerBench.Shell;

internal static class WorkspaceNavigationChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        Preferences.FilePath = Path.Combine(Path.GetTempPath(), "toolbar-settings-" + Guid.NewGuid().ToString("N") + ".json");
        Preferences.Load();
        var application = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        ThemeManager.Initialize();

        var window = new MainWindow { ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Settle(window);

            Require(window.FindName("SidebarButton") is null,
                "The utility rail has no obsolete sidebar chevron.");
            var navigation = Find<ListBox>(window, "WorkspaceNavigation");
            var destinations = navigation.Items.OfType<ListBoxItem>().ToArray();
            Require(window.FindName("OverviewButton") is Button,
                "The workspace toolbar exposes real icon-and-label buttons.");
            Require(navigation.IsVisible && navigation.IsEnabled, "Workspace navigation is visible and enabled.");
            Require(navigation.Items.Count == 4 && destinations.Select(item => item.Tag as string)
                .SequenceEqual(new[] { "Overview", "Investigate", "Tools", "History" }),
                "Workspace navigation offers Overview, Investigate, Tools and History in order.");
            VerifyToolbar(window);

            var overview = Find<FrameworkElement>(window, "OverviewPage");
            var options = Find<FrameworkElement>(window, "ConsoleBank");
            var categories = Find<FrameworkElement>(window, "LeftPanel");
            var evidence = Find<FrameworkElement>(window, "TerminalPanel");
            Require(navigation.SelectedItem == destinations[0] && overview.IsVisible &&
                    evidence.IsVisible && !options.IsVisible && !categories.IsVisible,
                "Startup opens Overview with evidence and keeps Tools controls out of view.");

            navigation.SelectedItem = destinations[2];
            Settle(window);
            Require(options.IsVisible && categories.IsVisible && !overview.IsVisible,
                "Selecting Tools opens the existing options and category controls.");

            navigation.SelectedItem = destinations[0];
            Settle(window);
            Require(overview.IsVisible && evidence.IsVisible && !options.IsVisible && !categories.IsVisible,
                "Returning to Overview restores its page and evidence beside it.");

            var areas = Find<ListBox>(window, "SystemAreas");
            Require(areas.Items.Count == 12, "The system map exposes every existing tool category.");
            areas.SelectedIndex = 11;
            Settle(window);
            var detail = Find<TextBlock>(window, "AreaTitle");
            var state = Find<TextBlock>(window, "AreaState");
            Require(detail.Text == "Storage" && state.Text == "Not checked" && overview.IsVisible,
                "Choosing Storage updates adjacent evidence without leaving the map or inventing health.");
            Find<Button>(window, "ReviewAreaButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(options.IsVisible && Find<TextBlock>(window, "CategoryTitle").Text == "Storage" &&
                    Find<CheckBox>(window, "MainAutoRepair").IsChecked == false,
                "Review opens the selected category with automatic repair off and starts no diagnostic.");

            navigation.SelectedItem = destinations[1];
            Settle(window);
            var investigation = Find<ListBox>(window, "InvestigationAreas");
            Require(Equals(investigation.SelectedItem, areas.SelectedItem),
                "Investigate retains the system area selected on Overview.");
            investigation.SelectedIndex = 4;
            Settle(window);
            Require(Find<FrameworkElement>(window, "InvestigatePage").IsVisible && detail.Text == "Network",
                "Guided investigation selects the matching area without dispatching an action.");
            Find<Button>(window, "ReviewAreaButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(options.IsVisible && Find<TextBlock>(window, "CategoryTitle").Text == "Network" &&
                    Find<CheckBox>(window, "MainAutoRepair").IsChecked == false,
                "Guided handoff opens the matching workflow for review without repair consent.");
            navigation.SelectedItem = destinations[3];
            Settle(window);
            Require(Find<FrameworkElement>(window, "HistoryPage").IsVisible &&
                    Find<Button>(window, "OpenSavedWorkflowButton").IsVisible &&
                    Find<ListBox>(window, "SessionHistory").Items.Count == 0,
                "History exposes the existing saved workflow action and an honest empty session.");
            Require(Find<Button>(window, "HistoryOpenCaptureButton").ToolTip is TextBlock captureHint &&
                    captureHint.Inlines.OfType<System.Windows.Documents.Run>().Any(run => run.Text.Contains("Ctrl+Shift+O", StringComparison.Ordinal)),
                "History's Open capture action advertises its existing keyboard shortcut.");

            for (int index = 0; index < 4; index++)
            {
                Key key = (Key)((int)Key.D1 + index);
                var binding = window.InputBindings.OfType<KeyBinding>().SingleOrDefault(item =>
                    item.Key == key && item.Modifiers == ModifierKeys.Control);
                Require(binding?.Command is RoutedCommand, $"Ctrl+{index + 1} has an app navigation command.");
                ((RoutedCommand)binding!.Command).Execute(null, window);
                Settle(window);
                Require(navigation.SelectedIndex == index, $"Ctrl+{index + 1} selects its advertised page.");
            }
            navigation.SelectedIndex = 0;
            Settle(window);
            var divider = Find<GridSplitter>(window, "OutputDivider");
            double before = overview.ActualWidth;
            divider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Right)
                { RoutedEvent = Keyboard.KeyDownEvent });
            Settle(window);
            Require(overview.ActualWidth > before, "The evidence divider resizes the graphical workspace by keyboard.");

            navigation.SelectedIndex = 2;
            ImportFixture(window, "workspace-evidence.log", "Synthetic fixture. No machine evidence was collected.\n");
            ImportFixture(window, "workspace-other.log", "Second synthetic fixture. No machine evidence was collected.\n");

            navigation.SelectedIndex = 0;
            areas.SelectedIndex = 11;
            Settle(window);
            Require(detail.Text == "Storage" && state.Text == "Not checked" &&
                    Find<FrameworkElement>(window, "AreaEvidencePanel").IsVisible,
                "An unchecked area keeps its own scope after other evidence is imported.");
            var export = Find<Button>(window, "ExportReportButton");
            Require(!Find<FrameworkElement>(window, "OutputViewButtons").IsEnabled && !export.IsEnabled &&
                    !Find<TextBox>(window, "SearchBox").IsEnabled &&
                    !WorkbenchCommands.ExportReport.CanExecute(null, window),
                "An unchecked area cannot reveal, search or export previously selected evidence.");

            navigation.SelectedIndex = 3;
            var history = Find<ListBox>(window, "SessionHistory");
            Settle(window);
            Require(history.Items.Count == 2 && history.Items[0].ToString() == "workspace-evidence.log" &&
                    history.Items[1].ToString() == "workspace-other.log",
                "Session history identifies imported evidence by readable filenames.");
            history.SelectedIndex = 0;
            Settle(window);
            var evidenceTitle = Find<TextBlock>(window, "EvidenceTitle");
            Require(Find<FrameworkElement>(window, "ReportPane").IsVisible &&
                    !Find<FrameworkElement>(window, "AreaEvidencePanel").IsVisible &&
                    evidenceTitle.Text == "workspace-evidence.log" && export.IsEnabled,
                "Selecting a session entry restores the actual imported report.");

            navigation.SelectedIndex = 2;
            Find<ListBox>(window, "SourcesList").SelectedIndex = 1;
            Settle(window);
            Require(evidenceTitle.Text == "workspace-other.log", "Tools can show the other retained report.");
            navigation.SelectedIndex = 3;
            Settle(window);
            Require(history.SelectedIndex == 0 && evidenceTitle.Text == "workspace-evidence.log" &&
                    Find<FrameworkElement>(window, "ReportPane").IsVisible,
                "Returning to History restores the report for its selected row.");

            navigation.SelectedIndex = 2;
            ImportFixture(window, "workspace-new.log", "Third synthetic fixture. No machine evidence was collected.\n");
            Settle(window);
            Require(evidenceTitle.Text == "workspace-new.log",
                "A fresh import in Tools remains selected when another entry is selected in History.");

            Console.WriteLine("PASS: workspace navigation and system-area review preserve explicit unchecked evidence and repair consent.");
            return 0;
        }
        finally
        {
            window.Close();
            application.Shutdown();
        }
    }

    private static void VerifyToolbar(MainWindow window)
    {
        var button = Find<Button>(window, "OverviewButton");
        Require(button.Content is StackPanel { Orientation: Orientation.Vertical } initial &&
                initial.Children.OfType<TextBlock>().First().FontSize == 20,
            "The family default shows large icons with labels underneath.");
        var menu = (ContextMenu)window.FindName("ToolbarMenu");
        MenuItem Option(string name) => menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, name));
        void Select(string name) { Option(name).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Settle(window); }
        Select("Small icons");
        Require(button.Content is StackPanel small && small.Children.OfType<TextBlock>().First().FontSize == 14,
            "The toolbar changes icon size independently of label placement.");
        Select("Text beside icons");
        Require(button.Content is StackPanel { Orientation: Orientation.Horizontal }, "Toolbar labels can sit beside icons.");
        Select("Large icons");
        Require(button.Content is StackPanel large && large.Children.OfType<TextBlock>().First().FontSize == 20,
            "Large icons retain the chosen beside-label mode.");
        Select("Text only");
        Require(button.Content is TextBlock && !Option("Small icons").IsEnabled && !Option("Large icons").IsEnabled,
            "Text-only mode hides icons and disables both size choices.");
        Require(!WorkbenchCommands.ToolbarLargeIcons.CanExecute(null, window), "Text-only mode also guards the icon-size shortcut.");
        Select("No text");
        Require(button.Content is KillerFamily.Accessibility.Glyph { FontSize: 20 }, "Icon-only mode keeps the chosen large workspace glyph.");
        for (int index = 0; index < 6; index++)
        {
            var key = (Key)((int)Key.D1 + index);
            Require(window.InputBindings.OfType<KeyBinding>().Count(binding => binding.Key == key &&
                    binding.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) == 1,
                "Each toolbar appearance shortcut has one binding.");
        }
        WorkbenchCommands.ToolbarSmallIcons.Execute(null, window);
        WorkbenchCommands.ToolbarTextBeside.Execute(null, window);
        string saved = Preferences.FilePath;
        string empty = Path.Combine(Path.GetTempPath(), "empty-toolbar-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(empty, "{}");
        Preferences.FilePath = empty;
        Preferences.Load();
        Preferences.FilePath = saved;
        Preferences.Load();
        var reopened = new MainWindow { ShowActivated = false, ShowInTaskbar = false };
        try
        {
            reopened.Show();
            Settle(reopened);
            Require(Find<Button>(reopened, "OverviewButton").Content is StackPanel { Orientation: Orientation.Horizontal } restored &&
                    restored.Children.OfType<TextBlock>().First().FontSize == 14,
                "Reloaded settings restore both toolbar appearance axes in a new window.");
        }
        finally { reopened.Close(); }
        WorkbenchCommands.ToolbarLargeIcons.Execute(null, window);
        WorkbenchCommands.ToolbarTextUnder.Execute(null, window);
        Require(Equals(button.Tag, "selected"), "Toolbar restyling preserves the active workspace mark.");
    }

    private static T Find<T>(Window window, string name) where T : FrameworkElement =>
        window.FindName(name) as T ?? throw new InvalidOperationException($"The window must expose {name} as a {typeof(T).Name}.");

    private static void ImportFixture(Window window, string fileName, string contents)
    {
        string path = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(path, contents);
        Find<TextBox>(window, "PathBox").Text = path;
        var open = Find<Button>(window, "OpenButton");
        open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (!open.IsEnabled && DateTime.UtcNow < deadline)
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Require(open.IsEnabled, "The evidence import finishes within its deadline.");
    }

    private static void Settle(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
