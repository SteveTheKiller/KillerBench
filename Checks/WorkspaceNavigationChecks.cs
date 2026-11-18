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
        Preferences.Set("ToolbarIconSize", "Large");
        Preferences.Set("ToolbarLabels", "Under");
        Preferences.Save();
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
            var caption = Find<FrameworkElement>(window, "CaptionBar");
            var navBounds = navigation.TransformToAncestor(window).TransformBounds(new Rect(navigation.RenderSize));
            var captionBounds = caption.TransformToAncestor(window).TransformBounds(new Rect(caption.RenderSize));
            Require(navBounds.Top >= captionBounds.Top && navBounds.Bottom <= captionBounds.Bottom + 1,
                "Workspace navigation fits inside the title bar instead of taking a separate content row.");
            VerifyToolbar(window);
            var titleSearch = Find<TextBox>(window, "TitleSearchBox");
            titleSearch.Text = "network";
            DateTime searchDeadline = DateTime.UtcNow.AddSeconds(4);
            while (window.FindName("TitleSearchResults") is not ListBox { Items.Count: > 0 } && DateTime.UtcNow < searchDeadline)
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            var searchResults = Find<ListBox>(window, "TitleSearchResults");
            Require(searchResults.Items.Count > 0, "Typing in title search finds diagnostic areas and tools.");
            titleSearch.Clear();
            var map = Find<ListBox>(window, "SystemAreas");
            var mapItem = (FrameworkElement)map.ItemContainerGenerator.ContainerFromIndex(0);
            Require(mapItem is { ActualWidth: >= 24, ActualHeight: >= 24 } && mapItem.ToolTip is not null,
                "Each hardware part on the map is a real click target with a name on hover. Size: " + mapItem.RenderSize);
            if (SystemParameters.WorkArea.Height >= 780)
                Require(Find<ScrollViewer>(window, "OverviewScroll").ScrollableHeight < 1,
                    "The initial window fits the entire map on a work area that supports its default height.");
            window.Width = 1280;
            window.Height = 1000;
            Settle(window);
            var mapPane = Find<FrameworkElement>(window, "OverviewPage");
            // The actions now sit in the hero above the map, so the map's own bottom edge is the measure.
            Require(map.TranslatePoint(new Point(0, map.ActualHeight), mapPane).Y >= mapPane.ActualHeight - 50,
                "The map grows with a taller window instead of leaving a large blank block beneath the cards.");
            window.Width = 1025;
            window.Height = 780;
            Settle(window);
            {
                Preferences.Set("AppScale", "1.5");
                var enlarged = new MainWindow { ShowActivated = false, ShowInTaskbar = false };
                try
                {
                    double preferredHeight = enlarged.Height;
                    Require(preferredHeight > 780, "Saved 150% scale requests a taller initial window before work-area fitting.");
                    enlarged.Show();
                    Settle(enlarged);
                    if (SystemParameters.WorkArea.Height >= preferredHeight)
                        Require(Find<ScrollViewer>(enlarged, "OverviewScroll").ScrollableHeight < 1,
                            "First launch fits the map at saved 150% scale when the work area permits it.");
                }
                finally { enlarged.Close(); Preferences.Set("AppScale", "1"); }
            }

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
            var categoryItems = Find<ListBox>(window, "CategoryList");
            var firstCategory = (FrameworkElement)categoryItems.ItemContainerGenerator.ContainerFromIndex(0);
            var secondCategory = (FrameworkElement)categoryItems.ItemContainerGenerator.ContainerFromIndex(1);
            Require(secondCategory.TranslatePoint(new Point(), window).X > firstCategory.TranslatePoint(new Point(), window).X &&
                    categories.TranslatePoint(new Point(0, categories.ActualHeight), window).Y <= options.TranslatePoint(new Point(), window).Y + 1,
                "Tools categories run horizontally above both content panes.");

            navigation.SelectedItem = destinations[0];
            Settle(window);
            Require(overview.IsVisible && evidence.IsVisible && !options.IsVisible && !categories.IsVisible,
                "Returning to Overview restores its page and evidence beside it.");

            var areas = Find<ListBox>(window, "SystemAreas");
            Require(areas.Items.Count == 12, "The system map exposes every existing tool category.");
            var machine = VisualElements(areas).OfType<KillerBench.Controls.MachineMapPanel>().SingleOrDefault();
            Require(machine is not null && machine.ScreenRect.Width > 0 && machine.TowerRect.Width > 0,
                "The Overview map draws the machine itself, a screen and a tower, instead of a plain grid of buttons.");
            Rect PartBounds(int index)
            {
                var part = (FrameworkElement)areas.ItemContainerGenerator.ContainerFromIndex(index);
                return part.TransformToAncestor(machine!).TransformBounds(new Rect(part.RenderSize));
            }
            bool Inside(Rect outer, Rect inner) => outer.Contains(inner.TopLeft) && outer.Contains(inner.BottomRight);
            Require(Inside(machine!.ScreenRect, PartBounds(0)) && Inside(machine.TowerRect, PartBounds(11)) &&
                    Inside(machine.TowerRect, PartBounds(9)) && Inside(machine.ScreenRect, PartBounds(5)),
                "Software areas light up on the screen and hardware areas sit in the tower bays. Diagnostics: " + PartBounds(0) +
                ", Storage: " + PartBounds(11) + ", screen: " + machine.ScreenRect + ", tower: " + machine.TowerRect);
            var partRects = Enumerable.Range(0, areas.Items.Count).Select(PartBounds).ToList();
            Require(partRects.All(rect => rect.Width >= 24 && rect.Height >= 24) &&
                    !partRects.SelectMany((a, i) => partRects.Skip(i + 1), (a, b) => Rect.Intersect(a, b)).Any(overlap => !overlap.IsEmpty && overlap.Width * overlap.Height > 4),
                "Every hardware part is a separate click target of at least 24 pixels, and no two parts overlap.");
            areas.SelectedIndex = 11;
            Settle(window);
            var detail = Find<TextBlock>(window, "AreaTitle");
            var state = Find<TextBlock>(window, "AreaState");
            Require(detail.Text == "Storage" && state.Text == "Not checked" && overview.IsVisible,
                "Choosing Storage updates adjacent evidence without leaving the map or inventing health.");
            Require(!Find<FrameworkElement>(window, "OutputViewButtons").IsVisible && !Find<TextBlock>(window, "EvidenceTitle").IsVisible,
                "The area summary replaces generic evidence chrome and disabled report tabs.");
            Find<Button>(window, "ReviewAreaButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(options.IsVisible && Find<TextBlock>(window, "CategoryTitle").Text == "Storage" &&
                    Find<CheckBox>(window, "MainAutoRepair").IsChecked == false,
                "Review opens the selected category with automatic repair off and starts no diagnostic.");

            navigation.SelectedItem = destinations[1];
            Settle(window);
            var investigation = Find<ListBox>(window, "InvestigationAreas");
            Require(investigation.Items.Count == 6,
                "Investigate offers the six illustrated symptom entry points from the approved concept.");
            investigation.SelectedIndex = 2;
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

            VerifyGroupedSearch(window);
            VerifyRecordedAreaSummary(window);

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
        Require(button.Content is StackPanel { Orientation: Orientation.Horizontal } initial &&
                initial.Children.OfType<TextBlock>().First().FontSize == 14 &&
                Find<FrameworkElement>(window, "CaptionBar").ActualHeight == (double)window.FindResource("TitleBarHeight"),
            "Title-bar navigation defaults to small icons beside text within one caption row.");
        var menu = (ContextMenu)window.FindName("ToolbarMenu");
        MenuItem Option(string name) => menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, name));
        void Select(string name)
        {
            Option(name).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Settle(window);
            Require(Find<FrameworkElement>(window, "CaptionBar").ActualHeight == (double)window.FindResource("TitleBarHeight"),
                "Every supported appearance mode preserves a single caption row.");
        }
        Select("Small icons");
        Require(button.Content is StackPanel small && small.Children.OfType<TextBlock>().First().FontSize == 14,
            "The toolbar changes icon size independently of label placement.");
        Select("Text beside icons");
        Require(button.Content is StackPanel { Orientation: Orientation.Horizontal }, "Toolbar labels can sit beside icons.");
        Require(!menu.Items.OfType<MenuItem>().Any(item => Equals(item.Header, "Large icons") || Equals(item.Header, "Text under icons")),
            "The title-bar menu excludes large icons and text underneath.");
        Select("Text only");
        Require(button.Content is TextBlock && !Option("Small icons").IsEnabled,
            "Text-only mode hides icons and disables icon sizing.");
        Select("No text");
        Require(button.Content is KillerFamily.Accessibility.Glyph { FontSize: 14 }, "Icon-only mode keeps the compact workspace glyph.");
        for (int index = 0; index < 6; index++)
        {
            var key = (Key)((int)Key.D1 + index);
            Require(window.InputBindings.OfType<KeyBinding>().Count(binding => binding.Key == key &&
                    binding.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) == (index is 1 or 4 ? 0 : 1),
                "Only supported single-row appearance options have keyboard bindings.");
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
        WorkbenchCommands.ToolbarTextBeside.Execute(null, window);
        Require(Equals(button.Tag, "selected"), "Toolbar restyling preserves the active workspace mark.");
    }

    private static void VerifyGroupedSearch(MainWindow window)
    {
        ImportFixture(window, "search-network.log", "Unrelated first line\nNetwork fixture connection dropped\nUnrelated last line\n");
        var input = Find<TextBox>(window, "TitleSearchBox");
        var results = Find<ListBox>(window, "TitleSearchResults");
        var navigation = Find<ListBox>(window, "WorkspaceNavigation");
        var popup = (System.Windows.Controls.Primitives.Popup)window.FindName("TitleSearchPopup");
        void Search(string query)
        {
            input.Text = query;
            DateTime deadline = DateTime.UtcNow.AddSeconds(4);
            while ((!popup.IsOpen || results.ItemsSource is null) && DateTime.UtcNow < deadline)
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Require(popup.IsOpen && results.ItemsSource is not null, "Search settles within its bounded deadline.");
        }
        void Enter(UIElement target) => target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(target), 0, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        object FromGroup(string name) => ((System.ComponentModel.ICollectionView)results.ItemsSource).Groups!
            .Cast<System.Windows.Data.CollectionViewGroup>().Single(group => Equals(group.Name, name)).Items[0];

        Search("network");
        Require(((System.ComponentModel.ICollectionView)results.ItemsSource).Groups!
            .Cast<System.Windows.Data.CollectionViewGroup>().Select(group => group.Name.ToString())
            .SequenceEqual(new[] { "Diagnostic areas", "Tools", "Evidence" }),
            "One query groups matching diagnostic areas, tools and loaded evidence.");
        results.SelectedItem = FromGroup("Diagnostic areas");
        Enter(results);
        Settle(window);
        Require(navigation.SelectedIndex == 0 && Find<TextBlock>(window, "AreaTitle").Text == "Network" &&
                !Find<Button>(window, "CancelButton").IsEnabled && !popup.IsOpen,
            "An area search result opens its evidence context without starting work.");
        Search("network ");
        results.SelectedItem = FromGroup("Tools");
        Enter(results);
        Settle(window);
        Require(navigation.SelectedIndex == 2 && Find<ComboBox>(window, "CategoryWorkflow").SelectedItem!.ToString()!
                .Contains("Network", StringComparison.OrdinalIgnoreCase) &&
                Find<CheckBox>(window, "MainAutoRepair").IsChecked == false && !Find<Button>(window, "CancelButton").IsEnabled,
            "A tool search result opens the workflow for review with repair consent off.");
        Search(" network");
        results.SelectedItem = FromGroup("Evidence");
        Enter(results);
        Settle(window);
        string visible = Find<TextBox>(window, "EvidenceBox").Text;
        Require(navigation.SelectedIndex == 3 && Find<TextBlock>(window, "EvidenceTitle").Text == "search-network.log" &&
                visible.Contains("2  Network fixture", StringComparison.Ordinal) && !visible.Contains("Unrelated", StringComparison.Ordinal) &&
                Find<FrameworkElement>(window, "ConsolePane").IsVisible,
            "An evidence result opens the matching source with original line numbers and the query applied.");
        Search("zz_no_matching_killerbench_fixture_zz");
        Require(results.Items.Count == 0 && Find<TextBlock>(window, "TitleSearchStatus").Text == "No matches",
            "An empty search reports no matches rather than stale results.");
        input.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(input), 0, Key.Escape)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Require(!popup.IsOpen, "Escape dismisses search without changing the selected evidence.");
        window.Activate();
        WorkbenchCommands.SearchWorkspace.Execute(null, window);
        Require(input.IsKeyboardFocusWithin, "Ctrl+K focuses the global search field.");
        input.Clear();
    }

    private static void VerifyRecordedAreaSummary(MainWindow window)
    {
        var navigation = Find<ListBox>(window, "WorkspaceNavigation");
        typeof(KillerBench.App).GetProperty("DemoMode")!.SetValue(null, true);
        try
        {
            navigation.SelectedIndex = 2;
            Find<ListBox>(window, "CategoryList").SelectedIndex = 11;
            Find<CheckBox>(window, "MainAutoRepair").IsChecked = false;
            typeof(KillerBench.App).GetProperty("ScreenshotMode")!.SetValue(null, true);
            Find<Button>(window, "MainGo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (Find<Button>(window, "CancelButton").IsEnabled && DateTime.UtcNow < deadline)
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Require(!Find<Button>(window, "CancelButton").IsEnabled, "The explicitly synthetic storage run finishes within its deadline.");
            typeof(KillerBench.App).GetProperty("ScreenshotMode")!.SetValue(null, false);
            navigation.SelectedIndex = 0;
            Find<ListBox>(window, "SystemAreas").SelectedIndex = 11;
            Settle(window);
            var chart = Find<Grid>(window, "AreaCoverageChart");
            var cards = Find<StackPanel>(window, "AreaReviewCards");
            string[] values = chart.Children.OfType<StackPanel>().Select(stack => stack.Children.OfType<TextBlock>().First().Text).ToArray();
            Require(chart.IsVisible && values.SequenceEqual(new[] { "5", "1", "1" }) && cards.Children.Count == 2 &&
                Find<TextBlock>(window, "AreaState").Text.Contains("Demo", StringComparison.OrdinalIgnoreCase),
                "The storage summary charts five checks that ran, one review item and one unknown check, never counting the unknown as review too. Values: " + string.Join(",", values));
            Require(Find<TextBlock>(window, "OverviewPercent").Text == "83%" &&
                Find<WrapPanel>(window, "OverviewChips").Children.Count == 3,
                "The Overview gauge shows the same storage result as 83% checked with review, unknown and fine chips. Gauge: " +
                Find<TextBlock>(window, "OverviewPercent").Text);
            Require(VisualElements(cards).OfType<TextBlock>().Any(text => text.Text.Contains("11.8 GB", StringComparison.Ordinal)) &&
                    VisualElements(cards).OfType<TextBlock>().Any(text => text.Text.Contains("no healthy verdict", StringComparison.Ordinal)),
                "Review cards retain the actual storage finding and unavailable-health boundary.");
            var stateChart = Find<Grid>(window, "AreaStateChart");
            double[] weights = stateChart.ColumnDefinitions.Select(column => column.Width.Value).ToArray();
            string stateName = System.Windows.Automation.AutomationProperties.GetName(stateChart);
            Require(stateChart.IsVisible && weights.SequenceEqual(new double[] { 1, 1, 4 }) &&
                    stateChart.Children.Count == 3 && stateName.Contains('4') && stateName.Contains('1'),
                "The storage state bar splits the same score into one review, one unknown and four fine segments and names those counts. Weights: " +
                string.Join(",", weights) + " Name: " + stateName);
            Find<Button>(window, "AreaFullReportButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Find<FrameworkElement>(window, "ReportPane").IsVisible && !Find<FrameworkElement>(window, "AreaEvidencePanel").IsVisible,
                "Full report opens the existing detailed report from the selected area.");
            WorkbenchCommands.Overview.Execute(null, window);
            Require(Find<FrameworkElement>(window, "AreaEvidencePanel").IsVisible,
                "Returning to the current Overview restores its summary even when its tab was already selected.");
            Find<Button>(window, "AreaRawEvidenceButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Find<FrameworkElement>(window, "ConsolePane").IsVisible && Find<TextBox>(window, "EvidenceBox").Text.Contains("11.8 GB", StringComparison.Ordinal),
                "Raw output opens evidence for this area rather than an earlier imported source.");
            WorkbenchCommands.Overview.Execute(null, window);
            Find<ListBox>(window, "SystemAreas").SelectedIndex = 4;
            Require(!chart.IsVisible && !stateChart.IsVisible && !Find<FrameworkElement>(window, "AreaDetailActions").IsVisible &&
                    Find<TextBlock>(window, "AreaState").Text == "Not checked" && !WorkbenchCommands.ExportReport.CanExecute(null, window),
                "An unrun area clears previous metrics, report actions and export eligibility.");
        }
        finally
        {
            typeof(KillerBench.App).GetProperty("ScreenshotMode")!.SetValue(null, false);
            typeof(KillerBench.App).GetProperty("DemoMode")!.SetValue(null, false);
        }
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

    private static IEnumerable<FrameworkElement> VisualElements(DependencyObject parent)
    {
        for (int index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is FrameworkElement element) yield return element;
            foreach (var descendant in VisualElements(child)) yield return descendant;
        }
    }
}
