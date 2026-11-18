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
        OverviewPage.Visibility = page == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        InvestigatePage.Visibility = page == "Investigate" ? Visibility.Visible : Visibility.Collapsed;
        HistoryPage.Visibility = page == "History" ? Visibility.Visible : Visibility.Collapsed;
        ConsoleBank.Visibility = page == "Tools" ? Visibility.Visible : Visibility.Collapsed;
        LeftPanel.Visibility = ConsoleBank.Visibility;
        if (page == "Investigate" && SystemAreas.SelectedItem is ToolCategory current &&
            (InvestigationAreas.SelectedItem is not GuidedSymptom selected || selected.Area != current.Id))
            InvestigationAreas.SelectedItem = GuidedSymptoms.FirstOrDefault(symptom => symptom.Area == current.Id);
        SetOutputOpen(true);
        if (page is "Overview" or "Investigate") ShowAreaEvidence();
        else if (page == "History")
        {
            if (SessionHistory.SelectedItem is SessionEntry) OnHistorySelected(sender, e);
            else if (SessionHistory.Items.Count > 0) SessionHistory.SelectedIndex = SessionHistory.Items.Count - 1;
            else SetOutputView();
        }
        else SetOutputView();
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
        double height = Math.Max(0, OverviewScroll.ViewportHeight - OverviewMapActions.ActualHeight - OverviewMapActions.Margin.Top - 8);
        if (Math.Abs(SystemAreas.MinHeight - height) > 1) SystemAreas.MinHeight = height;
    }

    private void FitInitialOverviewHeight()
    {
        double width = Math.Max(220, (Width - 40) * .6 / appScale - OverviewScroll.Padding.Left - OverviewScroll.Padding.Right);
        SystemAreas.Measure(new Size(width, double.PositiveInfinity));
        OverviewMapActions.Measure(new Size(width, double.PositiveInfinity));
        CaptionBar.Measure(new Size(Width, double.PositiveInfinity));
        double content = SystemAreas.DesiredSize.Height + OverviewMapActions.DesiredSize.Height +
            OverviewMapActions.Margin.Top + OverviewScroll.Padding.Top + OverviewScroll.Padding.Bottom + 12;
        Height = Math.Max(780, content * appScale + CaptionBar.DesiredSize.Height + 28);
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
        foreach (var area in ToolCategories)
        {
            var source = LatestAreaSource(area);
            area.StateKey = source is null ? "Str_Workspace_NotChecked"
                : historicalWorkflows.TryGetValue(source, out _) ? "Str_Mode_Historical"
                : workflowSummaries.TryGetValue(source, out var result) && result.Machine == DemoWorkflow.MachineName
                    ? "Str_Mode_DemoDiagnosis" : "Str_Session_ReadableReport";
        }
        SystemAreas.Items.Refresh();
    }

    private void OnReviewArea(object sender, RoutedEventArgs e)
    {
        if (SystemAreas.SelectedItem is not ToolCategory area) return;
        MainAutoRepair.IsChecked = false;
        CategoryList.SelectedItem = area;
        if (InvestigatePage.IsVisible && InvestigationAreas.SelectedItem is GuidedSymptom symptom && symptom.Area == area.Id)
            MainWorkflow.SelectedItem = KillerBench.Engine.Workflows.WorkflowCatalog.All.Single(tool => tool.Kind == symptom.Workflow);
        ShowToolsWorkspace();
    }

    private void ShowToolsWorkspace()
    {
        WorkspaceNavigation.SelectedIndex = 2;
        AreaEvidencePanel.Visibility = Visibility.Collapsed;
        SetOutputOpen(true);
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
        EvidenceHeader.Visibility = OutputViewButtons.Visibility = CoverageLabel.Visibility = SessionSourcesExpander.Visibility =
            area ? Visibility.Collapsed : Visibility.Visible;
        OutputViewButtons.IsEnabled = !area;
        ExportReportButton.IsEnabled = !area || areaSummarySource is not null;
        SearchBox.IsEnabled = !area && !workflowOutputActive;
    }
}
