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
            // The sidebar replaced the title-bar tabs; the hidden list stays as the page model it drives.
            Require(!navigation.IsVisible && navigation.IsEnabled, "The title-bar tabs are gone and the page model stays enabled.");
            Require(navigation.Items.Count == 4 && destinations.Select(item => item.Tag as string)
                .SequenceEqual(new[] { "Overview", "Investigate", "Tools", "History" }),
                "The page model keeps Overview, Investigate, Tools and History in order.");
            var sidebarPanel = Find<FrameworkElement>(window, "LeftPanel");
            var homeRow = Find<Button>(window, "SidebarHome");
            var historyRow = Find<Button>(window, "SidebarHistory");
            var areaRows = Find<ListBox>(window, "CategoryList");
            Require(sidebarPanel.IsVisible && homeRow.IsVisible && historyRow.IsVisible && areaRows.IsVisible &&
                    homeRow.TranslatePoint(new Point(), window).Y < areaRows.TranslatePoint(new Point(), window).Y &&
                    areaRows.TranslatePoint(new Point(0, areaRows.ActualHeight), window).Y <= historyRow.TranslatePoint(new Point(), window).Y + 1,
                "One sidebar lists Home, then every area, then History.");
            Require(Find<ItemsControl>(window, "SymptomChips").Items.Count == 6 && Find<ItemsControl>(window, "SymptomChips").IsVisible,
                "Home offers the six symptom chips under the score and map.");
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
                    "The initial window fits the entire map on a work area that supports its default height. Overflow: " + Find<ScrollViewer>(window, "OverviewScroll").ScrollableHeight);
            window.Width = 1280;
            window.Height = 1000;
            Settle(window);
            var mapPane = Find<FrameworkElement>(window, "OverviewPage");
            // The chips sit right under the map, the map grows until the chips meet the pane's bottom edge,
            // and the score card runs the map's full height instead of leaving a blank block under it.
            var chips = Find<FrameworkElement>(window, "SymptomChips");
            var hero = Find<FrameworkElement>(window, "OverviewHero");
            double mapTop = map.TranslatePoint(new Point(), mapPane).Y;
            double mapBottom = map.TranslatePoint(new Point(0, map.ActualHeight), mapPane).Y;
            double chipsTop = chips.TranslatePoint(new Point(0, 0), mapPane).Y;
            double heroTop = hero.TranslatePoint(new Point(), mapPane).Y;
            double heroBottom = heroTop + hero.ActualHeight;
            Require(chipsTop - mapBottom < 24 && chipsTop + chips.ActualHeight >= mapPane.ActualHeight - 50 &&
                    Math.Abs(heroTop - mapTop) <= 1 && Math.Abs(heroBottom - mapBottom) <= 1,
                "The map and score card fill the pane down to the chips, with no blank block. Map " + mapTop + "-" + mapBottom +
                ", card " + heroTop + "-" + heroBottom + ", chips " + chipsTop + "+" + chips.ActualHeight + ", pane " + mapPane.ActualHeight);
            Require(Find<TextBlock>(window, "OverviewDetail") is { IsVisible: true } detailLine && detailLine.Text.Length > 1,
                "Before any check, the score card carries the map instruction instead of an empty line.");
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
            var categoryItems = Find<ListBox>(window, "CategoryList");
            Require(navigation.SelectedItem == destinations[0] && overview.IsVisible && categories.IsVisible &&
                    !evidence.IsVisible && !options.IsVisible && Equals(homeRow.Tag, "selected") && categoryItems.Tag is null,
                "Startup opens Home beside the sidebar, with Home the only marked row.");

            var secondCategory = (FrameworkElement)categoryItems.ItemContainerGenerator.ContainerFromIndex(1);
            categoryItems.SelectedIndex = 1;
            categoryItems.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent, Source = secondCategory });
            Settle(window);
            Require(navigation.SelectedItem == destinations[2] && options.IsVisible && !overview.IsVisible &&
                    Equals(categoryItems.Tag, "active") && homeRow.Tag is null && historyRow.Tag is null,
                "Clicking an area in the sidebar opens its page and marks only that area.");
            var firstCategory = (FrameworkElement)categoryItems.ItemContainerGenerator.ContainerFromIndex(0);
            Require(secondCategory.TranslatePoint(new Point(), window).Y > firstCategory.TranslatePoint(new Point(), window).Y &&
                    categories.TranslatePoint(new Point(categories.ActualWidth, 0), window).X <= options.TranslatePoint(new Point(), window).X + 1,
                "Areas run down the sidebar to the left of the area page.");

            homeRow.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(navigation.SelectedItem == destinations[0] && overview.IsVisible && !evidence.IsVisible && !options.IsVisible &&
                    Equals(homeRow.Tag, "selected") && categoryItems.Tag is null,
                "Home in the sidebar restores the Home page alone, with no evidence beside it.");

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
            Require(detail.Text == "Storage" && state.Text == "Not checked" && evidence.IsVisible && !overview.IsVisible &&
                    Find<Button>(window, "OutputBackButton").IsVisible,
                "Choosing Storage swaps its details in over the map with a way back, without inventing health.");
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
            Require(evidence.IsVisible && !Find<FrameworkElement>(window, "InvestigatePage").IsVisible && detail.Text == "Network",
                "Guided investigation swaps in the matching area without dispatching an action.");
            Find<Button>(window, "ReviewAreaButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(options.IsVisible && Find<TextBlock>(window, "CategoryTitle").Text == "Network" &&
                    Find<CheckBox>(window, "MainAutoRepair").IsChecked == false,
                "Guided handoff opens the matching workflow for review without repair consent.");
            historyRow.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(navigation.SelectedItem == destinations[3] && Equals(historyRow.Tag, "selected") && homeRow.Tag is null,
                "History in the sidebar opens History and marks only that row.");
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
            // One pane: no divider, and the page or the swapped-in output takes the whole content width.
            var content = Find<FrameworkElement>(window, "ContentPane");
            double pageWidth = content.ActualWidth - categories.ActualWidth - 8;
            Require(!Find<GridSplitter>(window, "OutputDivider").IsVisible && overview.ActualWidth > pageWidth - 40,
                "The Overview fills the area beside the sidebar alone, with no side-by-side divider.");
            areas.SelectedIndex = 0;
            Settle(window);
            Require(!Find<GridSplitter>(window, "OutputDivider").IsVisible && evidence.ActualWidth > pageWidth - 40 && !overview.IsVisible,
                "Area details take the whole content width when they swap in.");
            Find<Button>(window, "OutputBackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(overview.IsVisible && !evidence.IsVisible, "Back returns to the map.");
            areas.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent, Source = areas.ItemContainerGenerator.ContainerFromIndex(0) });
            Settle(window);
            Require(evidence.IsVisible && detail.Text == "Diagnostics", "Clicking the area that is still selected opens its details again.");

            navigation.SelectedIndex = 2;
            ImportFixture(window, "workspace-evidence.log", "Synthetic fixture. No machine evidence was collected.\n");
            ImportFixture(window, "workspace-other.log", "Second synthetic fixture. No machine evidence was collected.\n");

            navigation.SelectedIndex = 0;
            areas.SelectedIndex = 0;
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
                    Find<FrameworkElement>(window, "HistoryPage").IsVisible && !evidence.IsVisible,
                "Returning to History lands on the list with its row and report still loaded.");
            history.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent, Source = history.ItemContainerGenerator.ContainerFromIndex(0) });
            Settle(window);
            var openRow = (ListBoxItem)history.ItemContainerGenerator.ContainerFromIndex(0);
            var rowDetail = (FrameworkElement)openRow.Template.FindName("HistoryDetail", openRow);
            var closedRow = history.Items.Count > 1 ? (ListBoxItem)history.ItemContainerGenerator.ContainerFromIndex(1) : null;
            Require(!evidence.IsVisible && rowDetail.IsVisible &&
                    (closedRow is null || !((FrameworkElement)closedRow.Template.FindName("HistoryDetail", closedRow)).IsVisible),
                "Clicking the selected History row opens it in place and leaves the other rows closed.");
            ((Button)openRow.Template.FindName("HistoryFullReport", openRow)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(evidence.IsVisible && Find<FrameworkElement>(window, "ReportPane").IsVisible && evidenceTitle.Text == "workspace-evidence.log",
                "The open History row's Full report button swaps its report in.");

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

    // The appearance menu now styles the sidebar: "No text" is icons only, the other modes keep names beside icons.
    private static void VerifyToolbar(MainWindow window)
    {
        var sidebar = Find<FrameworkElement>(window, "LeftPanel");
        var homeText = Find<FrameworkElement>(window, "SidebarHomeText");
        Require(sidebar.ActualWidth >= 180 && homeText.IsVisible &&
                Find<FrameworkElement>(window, "CaptionBar").ActualHeight == (double)window.FindResource("TitleBarHeight"),
            "The sidebar defaults to icons beside names, under a single caption row.");
        var menu = (ContextMenu)window.FindName("ToolbarMenu");
        Require(ReferenceEquals(sidebar.ContextMenu, menu), "Right-clicking the sidebar opens its appearance menu.");
        MenuItem Option(string name) => menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, name));
        void Select(string name)
        {
            Option(name).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Settle(window);
            Require(Find<FrameworkElement>(window, "CaptionBar").ActualHeight == (double)window.FindResource("TitleBarHeight"),
                "Every supported appearance mode preserves a single caption row.");
        }
        Select("Small icons");
        Select("Text beside icons");
        Require(sidebar.ActualWidth >= 180 && homeText.IsVisible, "Text beside icons shows the full sidebar.");
        Require(!menu.Items.OfType<MenuItem>().Any(item => Equals(item.Header, "Large icons") || Equals(item.Header, "Text under icons")),
            "The appearance menu excludes large icons and text underneath.");
        Select("Text only");
        Require(!Option("Small icons").IsEnabled && sidebar.ActualWidth >= 180, "Text-only mode keeps names and disables icon sizing.");
        Select("No text");
        Require(sidebar.ActualWidth <= 48 && !homeText.IsVisible, "No text collapses the sidebar to icons only.");
        Select("Text beside icons");
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
            Require(Find<FrameworkElement>(reopened, "LeftPanel").ActualWidth >= 180 && Find<FrameworkElement>(reopened, "SidebarHomeText").IsVisible,
                "Reloaded settings restore the sidebar appearance in a new window.");
        }
        finally { reopened.Close(); }
        WorkbenchCommands.ToolbarTextBeside.Execute(null, window);
        Require(Equals(Find<Button>(window, "SidebarHome").Tag, "selected"), "Sidebar restyling preserves the active page mark.");
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
            var metricChart = Find<StackPanel>(window, "AreaMetricChart");
            string[] metricNames = metricChart.Children.OfType<FrameworkElement>()
                .Select(row => System.Windows.Automation.AutomationProperties.GetName(row)).ToArray();
            Require(metricChart.IsVisible && metricNames.Length == 2 &&
                    metricNames[0].StartsWith("C:", StringComparison.Ordinal) && !metricNames[0].Contains("::", StringComparison.Ordinal) &&
                    metricNames[0].Contains("476", StringComparison.Ordinal) &&
                    metricNames[1].StartsWith("D:", StringComparison.Ordinal) && metricNames[1].Contains("931", StringComparison.Ordinal),
                "The storage summary draws one used-space bar per measured volume from typed metrics. Rows: " + string.Join(" | ", metricNames));
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
            Require(!chart.IsVisible && !stateChart.IsVisible && !metricChart.IsVisible && !Find<FrameworkElement>(window, "AreaDetailActions").IsVisible &&
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
