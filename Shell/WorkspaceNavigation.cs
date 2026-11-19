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
            if (ReferenceEquals(WorkspaceNavigation.SelectedItem, item) && item.Tag is "Overview" or "Investigate") ShowAreaEvidence();
            else WorkspaceNavigation.SelectedItem = item;
        }
    }

    private sealed record SessionEntry(KillerBench.Engine.Evidence.TextEvidence Evidence, string Label)
    {
        public override string ToString() => Label;
    }

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
        if (page == "Investigate" && SystemAreas.SelectedItem is ToolCategory current &&
            (InvestigationAreas.SelectedItem is not GuidedSymptom selected || selected.Area != current.Id))
            InvestigationAreas.SelectedItem = GuidedSymptoms.FirstOrDefault(symptom => symptom.Area == current.Id);
        if (page is "Overview" or "Investigate") ShowAreaEvidence();
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
        InvestigatePage.Visibility = Show("Investigate");
        HistoryPage.Visibility = Show("History");
        ConsoleBank.Visibility = Show("Tools");
        LeftPanel.Visibility = page == "Tools" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void InitializeWorkspace()
    {
        foreach (var area in ToolCategories) area.StateKey = "Str_Workspace_NotChecked";
        SystemAreas.ItemsSource = ToolCategories;
        InvestigationAreas.ItemsSource = GuidedSymptoms;
        SystemAreas.SelectedIndex = 0;
        InvestigationAreas.SelectedIndex = 0;
        OverviewScroll.LayoutUpdated += (_, _) => FitSystemMap();
    }

    private void FitSystemMap()
    {
        if (!OverviewPage.IsVisible || OverviewScroll.ViewportHeight <= 0) return;
        // The map sits beside the score column and shares its height only with the hint underneath it.
        double hint = OverviewMapHint.ActualHeight + OverviewMapHint.Margin.Top + OverviewMapHint.Margin.Bottom;
        double height = Math.Max(0, OverviewScroll.ViewportHeight - 8 - hint);
        if (Math.Abs(SystemAreas.MinHeight - height) > 1) SystemAreas.MinHeight = height;
    }

    private void FitInitialOverviewHeight()
    {
        double width = Math.Max(220, (Width - 40) / appScale - OverviewScroll.Padding.Left - OverviewScroll.Padding.Right);
        // Measure the whole scroll content (hero, map hint and map) so nothing on the page is left out of the sum.
        var page = (FrameworkElement)OverviewScroll.Content;
        // A MinHeight left over from a larger window would inflate the sum; FitSystemMap sets it again after layout.
        SystemAreas.ClearValue(MinHeightProperty);
        page.Measure(new Size(width, double.PositiveInfinity));
        CaptionBar.Measure(new Size(Width, double.PositiveInfinity));
        double content = page.DesiredSize.Height + OverviewScroll.Padding.Top + OverviewScroll.Padding.Bottom + 12;
        // Fit the Overview content, so the map isn't followed by a band of empty pane.
        Height = Math.Max(MinHeight, content * appScale + CaptionBar.DesiredSize.Height + 28);
    }

    private void OnSystemAreaSelected(object sender, SelectionChangedEventArgs e) => ShowAreaEvidence();

    private void OnInvestigationAreaSelected(object sender, SelectionChangedEventArgs e)
    {
        if (InvestigationAreas.SelectedItem is GuidedSymptom symptom)
            SystemAreas.SelectedItem = ToolCategories.Single(area => area.Id == symptom.Area);
        ShowAreaEvidence();
    }

    private void RefreshSessionHistory()
    {
        var selected = (SessionHistory.SelectedItem as SessionEntry)?.Evidence;
        RefreshMapEvidence();
        SessionHistory.ItemsSource = sources.Select(source => new SessionEntry(source,
            workflowSummaries.TryGetValue(source, out var result)
                ? Controls.LocalizedText.Workflow(result.Kind) + " · " + result.StartedUtc.ToLocalTime().ToString("t")
                : source.Source == "Synthetic demonstration" ? Controls.LocalizedText.Get("Str_Content_Load_sample")
                : System.IO.Path.GetFileName(source.Source))).ToArray();
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
            OverviewDetail.Text = " ";
            OverviewDetail.Visibility = Visibility.Hidden;
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
        if (WorkspaceNavigation.SelectedItem is ListBoxItem { Tag: "Investigate" } && InvestigationAreas.SelectedItem is GuidedSymptom symptom && symptom.Area == area.Id)
            MainWorkflow.SelectedItem = KillerBench.Engine.Workflows.WorkflowCatalog.All.Single(tool => tool.Kind == symptom.Workflow);
        ShowToolsWorkspace();
    }

    private void ShowToolsWorkspace()
    {
        WorkspaceNavigation.SelectedIndex = 2;
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

    private void SetWorkspaceBusy(bool busy)
    {
        WorkspaceNavigation.IsEnabled = !busy;
        OverviewCheckButton.IsEnabled = !busy;
        OverviewOpenCaptureButton.IsEnabled = !busy;
        SystemAreas.IsEnabled = !busy;
        InvestigationAreas.IsEnabled = !busy;
        SessionHistory.IsEnabled = !busy;
    }

    private void SetAreaEvidenceMode(bool area)
    {
        AreaEvidencePanel.Visibility = area ? Visibility.Visible : Visibility.Collapsed;
        // The header stays for its Back button; only the generic title and export hide on an area summary.
        EvidenceTitle.Visibility = ExportReportButton.Visibility = OutputViewButtons.Visibility = CoverageLabel.Visibility = SessionSourcesExpander.Visibility =
            area ? Visibility.Collapsed : Visibility.Visible;
        OutputViewButtons.IsEnabled = !area;
        ExportReportButton.IsEnabled = !area || areaSummarySource is not null;
        SearchBox.IsEnabled = !area && !workflowOutputActive;
    }
}
