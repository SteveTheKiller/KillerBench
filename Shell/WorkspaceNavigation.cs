using System.Windows;
using System.Windows.Controls;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private void OnWorkspaceToolbarClick(object sender, RoutedEventArgs e)
    {
        if (App.ScreenshotMode || !WorkspaceNavigation.IsEnabled ||
            AboutOverlay.Visibility == Visibility.Visible || ShortcutsOverlay.Visibility == Visibility.Visible) return;
        var item = WorkspaceNavigation.Items.OfType<ListBoxItem>().SingleOrDefault(candidate => ReferenceEquals(candidate.Content, sender));
        if (item is not null)
        {
            if (ReferenceEquals(WorkspaceNavigation.SelectedItem, item) && item.Tag is "Overview") ShowAreaEvidence();
            else WorkspaceNavigation.SelectedItem = item;
        }
    }

    private sealed record SessionEntry(KillerBench.Engine.Evidence.TextEvidence Evidence, string Label)
    {
        // What the row shows when it opens in place: the outcome line and each check with its state.
        public string Summary { get; init; } = "";
        public IReadOnlyList<HistoryCheck> Checks { get; init; } = [];
        public Visibility ChecksVisibility => Checks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public override string ToString() => Label;
    }

    private sealed record HistoryCheck(string Name, string State, System.Windows.Media.Brush Tone);

    private sealed record GuidedSymptom(string Id, string Art, string Area, WorkflowKind Workflow)
    {
        public string Title => Controls.LocalizedText.Get("Str_Guided_" + Id);
        public string Description => Controls.LocalizedText.Get("Str_Guided_" + Id + "Hint");
        public override string ToString() => Title;
    }

    private static readonly GuidedSymptom[] GuidedSymptoms =
    [
        new("Slow", "SlowPC", "Diagnostics", WorkflowKind.StartupPerformance),
        new("Updates", "Updates", "Updates", WorkflowKind.WindowsUpdate),
        new("Network", "Network", "Network", WorkflowKind.Network),
        new("Storage", "Storage", "Storage", WorkflowKind.Storage),
        new("Apps", "AppCrashes", "Diagnostics", WorkflowKind.ReliabilityHistory),
        new("Other", "Other", "Diagnostics", WorkflowKind.CommonProblems)
    ];

    private void OnWorkspaceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (WorkspaceNavigation.SelectedItem is not ListBoxItem { Tag: string page } || OverviewPage is null) return;
        RefreshWorkspaceToolbarSelection();
        RefreshPageVisibility();
        if (page == "Overview") ShowAreaEvidence();
        else if (page == "History")
        {
            if (SessionHistory.SelectedItem is SessionEntry) OnHistorySelected(sender, e);
            else if (SessionHistory.Items.Count > 0) SessionHistory.SelectedIndex = SessionHistory.Items.Count - 1;
            else SetOutputView();
        }
        else SetOutputView();
        // A page switch always lands on the page; details swap in only when something on it is chosen.
        SetOutputOpen(false);
    }

    // Only one thing fills the pane: the current page, or the output swapped in over it.
    private void RefreshPageVisibility()
    {
        if (OverviewPage is null || TerminalPanel is null) return;
        string page = WorkspaceNavigation.SelectedItem is ListBoxItem { Tag: string tag } ? tag : "Overview";
        bool covered = TerminalPanel.Visibility == Visibility.Visible;
        Visibility Show(string name) => page == name && !covered ? Visibility.Visible : Visibility.Collapsed;
        OverviewPage.Visibility = Show("Overview");
        HistoryPage.Visibility = Show("History");
        ConsoleBank.Visibility = Show("Tools");
        RefreshSidebarSelection();
    }

    private void InitializeWorkspace()
    {
        foreach (var area in ToolCategories) area.StateKey = "Str_Workspace_NotChecked";
        SystemAreas.ItemsSource = ToolCategories;
        SymptomChips.ItemsSource = GuidedSymptoms;
        SystemAreas.SelectedIndex = 0;
        OverviewScroll.LayoutUpdated += (_, _) => FitSystemMap();
    }

    private void FitSystemMap()
    {
        if (!OverviewPage.IsVisible || OverviewScroll.ViewportHeight <= 0) return;
        // The question block sits on top at its natural 100% size, and the machine takes whatever height is left.
        // Below 280px the page scrolls instead of shrinking the machine to nothing. Nothing on Home ever scales down.
        double top = OverviewHeroSpot.ActualHeight + 16;
        SetMargin(SystemAreas, new Thickness(0, top, 0, 0));
        double height = Math.Max(280, OverviewScroll.ViewportHeight - top - 2);
        if (double.IsNaN(SystemAreas.Height) || Math.Abs(SystemAreas.Height - height) > 1) SystemAreas.Height = height;
    }

    private static void SetMargin(FrameworkElement element, Thickness margin)
    {
        if (Math.Abs(element.Margin.Left - margin.Left) > 0.5 || Math.Abs(element.Margin.Top - margin.Top) > 0.5) element.Margin = margin;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            if (FindChild<T>(child) is { } found) return found;
        }
        return null;
    }

    private void FitInitialOverviewHeight()
    {
        // The page shares the width with the sidebar and its 8px gap.
        double width = Math.Max(220, (Width - 40) / appScale - LeftPanel.Width - 8 - OverviewScroll.Padding.Left - OverviewScroll.Padding.Right);
        // Measure the whole scroll content (hero, map hint and map) so nothing on the page is left out of the sum.
        var page = (FrameworkElement)OverviewScroll.Content;
        // A Height left over from a larger window would inflate the sum; FitSystemMap sets it again after layout.
        SystemAreas.ClearValue(HeightProperty);
        page.Measure(new Size(width, double.PositiveInfinity));
        CaptionBar.Measure(new Size(Width, double.PositiveInfinity));
        double content = page.DesiredSize.Height + OverviewScroll.Padding.Top + OverviewScroll.Padding.Bottom + 12;
        // Fit the Overview content, so the map isn't followed by a band of empty pane.
        Height = Math.Max(MinHeight, content * appScale + CaptionBar.DesiredSize.Height + 28);
    }

    private void OnSystemAreaSelected(object sender, SelectionChangedEventArgs e) => ShowAreaEvidence();

    private void RefreshSessionHistory()
    {
        var selected = (SessionHistory.SelectedItem as SessionEntry)?.Evidence;
        RefreshMapEvidence();
        SessionHistory.ItemsSource = sources.Select(source => workflowSummaries.TryGetValue(source, out var result)
            ? new SessionEntry(source, Controls.LocalizedText.Workflow(result.Kind) + " · " + result.StartedUtc.ToLocalTime().ToString("t"))
            {
                Summary = LocalizedStatus(result.Status),
                Checks = result.Checks.Where(check => check.Name != NativeCapabilityCheckName)
                    .Select(check => new HistoryCheck(DisplayReportCheckName(check.Name), Controls.LocalizedText.Choice(check.State),
                        // Information has no tone of its own in reports; on a dark row it needs the text color to show at all.
                        (System.Windows.Media.Brush)FindResource(check.State == CheckState.Information ? "TextBrush" : ToneBrush(check.State)))).ToArray()
            }
            : new SessionEntry(source, source.Source == "Synthetic demonstration" ? Controls.LocalizedText.Get("Str_Content_Load_sample")
                : System.IO.Path.GetFileName(source.Source)) { Summary = source.Source }).ToArray();
        SessionHistory.SelectedItem = SessionHistory.Items.Cast<SessionEntry>().FirstOrDefault(entry => ReferenceEquals(entry.Evidence, selected));
        HistoryEmpty.Visibility = sources.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnHistorySelected(object sender, SelectionChangedEventArgs e)
    {
        if (SessionHistory.SelectedItem is not SessionEntry selected) return;
        SourcesList.SelectedIndex = sources.IndexOf(selected.Evidence);
        OnSourceSelected(sender, e);
        AreaEvidencePanel.Visibility = Visibility.Collapsed;
        SetOutputView();
        // The row opens in place with its outcome and checks; the full report opens from the row's own buttons.
    }

    private void OnHistoryOpenReport(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SessionEntry entry }) return;
        SessionHistory.SelectedItem = entry;
        if (Equals((sender as FrameworkElement)?.Tag, "raw")) OnConsoleView(sender, e);
        else OnReportView(sender, e);
        SetOutputOpen(true);
    }

    private void ShowAreaEvidence()
    {
        if (readCancellation is not null || workflowOutputActive) return;
        if (SystemAreas.SelectedItem is not ToolCategory area) return;
        var previous = LatestAreaSource(area);
        if (previous is not null && workflowSummaries.TryGetValue(previous, out var result))
        {
            int sourceIndex = sources.IndexOf(previous);
            if (SourcesList.SelectedIndex == sourceIndex)
            {
                evidence = previous;
                ShowEvidence();
            }
            else SourcesList.SelectedIndex = sourceIndex;
            if (workflowReportTitles.TryGetValue(previous, out var title)) RenderNamedWorkflowReport(result, title);
            else RenderWorkflowReport(result);
            BuildAreaSummary(area, previous, result);
            return;
        }
        BuildAreaSummary(area, null, null);
    }

    private KillerBench.Engine.Evidence.TextEvidence? LatestAreaSource(ToolCategory area) =>
        sources.LastOrDefault(source => workflowSummaries.TryGetValue(source, out var result) && area.Tools.Contains(result.Kind));

    private void RefreshMapEvidence()
    {
        var latest = new List<WorkflowResult>();
        foreach (var area in ToolCategories)
        {
            var source = LatestAreaSource(area);
            WorkflowResult? result = null;
            if (source is not null) workflowSummaries.TryGetValue(source, out result);
            area.StateKey = source is null ? "Str_Workspace_NotChecked"
                : historicalWorkflows.TryGetValue(source, out _) ? "Str_Mode_Historical"
                : result?.Machine == DemoWorkflow.MachineName ? "Str_Mode_DemoDiagnosis" : "Str_Session_ReadableReport";
            area.Score = result is null ? null : OverviewScore.From([result]);
            if (result is not null && !latest.Contains(result)) latest.Add(result);
        }
        SystemAreas.Items.Refresh();
        RefreshOverviewHero(OverviewScore.From(latest));
    }

    private void RefreshOverviewHero(OverviewScore score)
    {
        OverviewGauge.Percent = score.CheckedPercent;
        OverviewPercent.Text = score.Total == 0 ? "0%" : score.CheckedPercent.ToString(System.Globalization.CultureInfo.CurrentCulture) + "%";
        OverviewGauge.SetResourceReference(Controls.CoverageGauge.ToneBrushProperty, score.Tone switch
        {
            OverviewTone.Clear => "OkBrush",
            OverviewTone.Review => "WarnBrush",
            OverviewTone.Fixable => "DangerRed",
            OverviewTone.Incomplete => "MutedTextBrush",
            _ => "PaneBorderBrush"
        });
        if (score.Total == 0)
        {
            OverviewHeadline.SetResourceReference(TextBlock.TextProperty, "Str_Workspace_SystemMap");
            // Hidden, not collapsed: the hero keeps its height so the map doesn't jump once a scan fills the line.
            // Before any check the card says what to do with the map beside it, in the space the coverage line will use.
            OverviewDetail.SetResourceReference(TextBlock.TextProperty, "Str_Workspace_MapHint");
            OverviewDetail.Visibility = Visibility.Visible;
            OverviewChips.Visibility = Visibility.Collapsed;
            return;
        }
        OverviewHeadline.Text = score.NeedsAttention > 0 ? Controls.LocalizedText.Format("Str_Overview_HeadlineReview", score.NeedsAttention)
            : Controls.LocalizedText.Get(score.Unknown > 0 ? "Str_Overview_HeadlineIncomplete" : "Str_Overview_HeadlineClear");
        OverviewDetail.Text = Controls.LocalizedText.Format("Str_Overview_Coverage", score.Ran, score.Total);
        OverviewDetail.Visibility = Visibility.Visible;
        OverviewChips.Children.Clear();
        AddOverviewChip("Str_Overview_Fixable", score.Fixable, "DangerRed");
        AddOverviewChip("Str_Overview_Review", score.NeedsAttention - score.Fixable, "WarnBrush");
        AddOverviewChip("Str_Overview_Unknown", score.Unknown, "MutedTextBrush");
        AddOverviewChip("Str_Overview_Fine", score.Ran - score.NeedsAttention, "OkBrush");
        OverviewChips.Visibility = Visibility.Visible;
    }

    private void AddOverviewChip(string key, int count, string brush)
    {
        if (count <= 0) return;
        var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        dot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, brush);
        var label = new TextBlock { Text = Controls.LocalizedText.Format(key, count), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        var chip = new Border
        {
            BorderThickness = new Thickness(1), Padding = new Thickness(8, 3, 10, 3), Margin = new Thickness(0, 0, 8, 6),
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { dot, label } }
        };
        chip.SetResourceReference(Border.BorderBrushProperty, "PaneBorderBrush");
        chip.SetResourceReference(Border.CornerRadiusProperty, "ControlCornerRadius");
        System.Windows.Automation.AutomationProperties.SetName(chip, label.Text);
        OverviewChips.Children.Add(chip);
    }

    private void OnReviewArea(object sender, RoutedEventArgs e)
    {
        if (SystemAreas.SelectedItem is not ToolCategory area) return;
        MainAutoRepair.IsChecked = false;
        CategoryList.SelectedItem = area;
        ShowToolsWorkspace();
    }

    private void ShowToolsWorkspace()
    {
        WorkspaceNavigation.SelectedIndex = 1;
        AreaEvidencePanel.Visibility = Visibility.Collapsed;
        SetOutputView();
        // Land on the tool controls; output swaps in when the tool runs.
        SetOutputOpen(false);
    }

    private void OnOverviewCheck(object sender, RoutedEventArgs e)
    {
        if (!MainGo.IsEnabled || !CanSearchWorkspace) return;
        MainAutoRepair.IsChecked = false;
        ShowToolsWorkspace();
        MainWorkflow.SelectedItem = KillerBench.Engine.Workflows.WorkflowCatalog.All.Single(tool => tool.Kind == WorkflowKind.CommonProblems);
        OnMainGo(sender, e);
    }

    // A Home chip opens the symptom's area and runs its matching check in one step.
    private void OnSymptomChip(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: GuidedSymptom symptom } || !MainGo.IsEnabled || !CanSearchWorkspace) return;
        CategoryList.SelectedItem = ToolCategories.Single(area => area.Id == symptom.Area);
        MainAutoRepair.IsChecked = false;
        ShowToolsWorkspace();
        MainWorkflow.SelectedItem = KillerBench.Engine.Workflows.WorkflowCatalog.All.Single(tool => tool.Kind == symptom.Workflow);
        OnMainGo(sender, e);
    }

    private void SetWorkspaceBusy(bool busy)
    {
        WorkspaceNavigation.IsEnabled = !busy;
        SymptomChips.IsEnabled = !busy;
        OverviewCheckButton.IsEnabled = !busy;
        OverviewOpenCaptureButton.IsEnabled = !busy;
        SystemAreas.IsEnabled = !busy;
        SessionHistory.IsEnabled = !busy;
    }

    private void SetAreaEvidenceMode(bool area)
    {
        AreaEvidencePanel.Visibility = AreaHeaderBlock.Visibility = ReviewAreaButton.Visibility = area ? Visibility.Visible : Visibility.Collapsed;
        // The header stays for its Back button; only the generic title and export hide on an area summary.
        EvidenceTitle.Visibility = ExportReportButton.Visibility = OutputViewButtons.Visibility = CoverageLabel.Visibility = SessionSourcesExpander.Visibility =
            area ? Visibility.Collapsed : Visibility.Visible;
        OutputViewButtons.IsEnabled = !area;
        ExportReportButton.IsEnabled = !area || areaSummarySource is not null;
        SearchBox.IsEnabled = !area && !workflowOutputActive;
    }
}
