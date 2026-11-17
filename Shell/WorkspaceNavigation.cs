using System.Windows;
using System.Windows.Controls;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private sealed record SessionEntry(KillerBench.Engine.Evidence.TextEvidence Evidence, string Label)
    {
        public override string ToString() => Label;
    }

    private void OnWorkspaceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (WorkspaceNavigation.SelectedItem is not ListBoxItem { Tag: string page } || OverviewPage is null) return;
        OverviewPage.Visibility = page == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        InvestigatePage.Visibility = page == "Investigate" ? Visibility.Visible : Visibility.Collapsed;
        HistoryPage.Visibility = page == "History" ? Visibility.Visible : Visibility.Collapsed;
        ConsoleBank.Visibility = page == "Tools" ? Visibility.Visible : Visibility.Collapsed;
        LeftPanel.Visibility = ConsoleBank.Visibility;
        if (page == "Investigate") InvestigationAreas.SelectedItem = SystemAreas.SelectedItem;
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
        InvestigationAreas.ItemsSource = ToolCategories;
        SystemAreas.SelectedIndex = 0;
        InvestigationAreas.SelectedIndex = 0;
    }

    private void OnSystemAreaSelected(object sender, SelectionChangedEventArgs e) => ShowAreaEvidence();

    private void OnInvestigationAreaSelected(object sender, SelectionChangedEventArgs e)
    {
        if (InvestigationAreas.SelectedItem is ToolCategory area) SystemAreas.SelectedItem = area;
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
            AreaEvidencePanel.Visibility = Visibility.Collapsed;
            SetOutputView();
            return;
        }
        AreaTitle.Text = area.Title;
        AreaDescription.Text = area.Description;
        AreaState.SetResourceReference(TextBlock.TextProperty, "Str_Workspace_NotChecked");
        CoverageLabel.SetResourceReference(TextBlock.TextProperty, "Str_Workspace_NotChecked");
        EvidenceTitle.SetResourceReference(TextBlock.TextProperty, "Str_Workspace_Evidence");
        SetAreaEvidenceMode(true);
        ReportPane.Visibility = ConsolePane.Visibility = StartGuide.Visibility = ConsoleSearchPanel.Visibility = Visibility.Collapsed;
        SetOutputOpen(true);
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
        ShowToolsWorkspace();
    }

    private void ShowToolsWorkspace()
    {
        WorkspaceNavigation.SelectedIndex = 2;
        AreaEvidencePanel.Visibility = Visibility.Collapsed;
        SetOutputOpen(true);
    }

    private void SetWorkspaceBusy(bool busy)
    {
        WorkspaceNavigation.IsEnabled = !busy;
        SystemAreas.IsEnabled = !busy;
        InvestigationAreas.IsEnabled = !busy;
        SessionHistory.IsEnabled = !busy;
    }

    private void SetAreaEvidenceMode(bool area)
    {
        AreaEvidencePanel.Visibility = area ? Visibility.Visible : Visibility.Collapsed;
        OutputViewButtons.IsEnabled = !area;
        ExportReportButton.IsEnabled = !area;
        SearchBox.IsEnabled = !area && !workflowOutputActive;
    }
}
