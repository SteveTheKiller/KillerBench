using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KillerBench.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private TextEvidence? areaSummarySource;

    private void BuildAreaSummary(ToolCategory area, TextEvidence? source, WorkflowResult? result)
    {
        areaSummarySource = source;
        AreaTitle.Text = area.Title;
        SelectedAreaArt.Id = area.Id;
        AreaDescription.Text = area.Description;
        AreaReviewCards.Children.Clear();
        AreaCoverageChart.Children.Clear();
        AreaCoverageChart.ColumnDefinitions.Clear();
        bool recorded = source is not null && result is not null;
        AreaCoverageChart.Visibility = AreaDetailActions.Visibility = recorded ? Visibility.Visible : Visibility.Collapsed;
        AreaBeforeCheck.Visibility = recorded ? Visibility.Collapsed : Visibility.Visible;
        AreaSectionTitle.SetResourceReference(TextBlock.TextProperty, recorded ? "Str_Area_ReviewNext" : "Str_Area_ChooseChecks");
        if (recorded)
        {
            string provenance = historicalWorkflows.TryGetValue(source!, out _) ? "Str_Mode_Historical"
                : result!.Machine == DemoWorkflow.MachineName ? "Str_Mode_DemoDiagnosis" : "Str_Session_ReadableReport";
            AreaState.Text = LocalizedText.Get(provenance) + ", " + result!.StartedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
            // Same numbers as the Overview gauge, so the two panes never disagree about one result.
            var score = OverviewScore.From([result]);
            AddAreaCoverage("Str_Report_Checked", score.Ran, score.Total, 0);
            AddAreaCoverage("Str_Report_NeedsReview", score.NeedsAttention, score.Total, 1);
            AddAreaCoverage("Str_Report_Incomplete", score.Unknown, score.Total, 2);
            var problems = result.Checks.Where(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable or CheckState.Unknown)
                .OrderBy(check => ReviewPriority(check.EffectiveState)).ToArray();
            foreach (var check in problems.Take(2))
            {
                var content = new StackPanel();
                var title = Body(DisplayReportCheckName(check.Name), 15, new Thickness(0, 0, 0, 6));
                title.FontWeight = FontWeights.SemiBold;
                content.Children.Add(title);
                content.Children.Add(Body(LocalizedText.Choice(check.EffectiveState), 11, new Thickness(0, 0, 0, 8), "PrimaryBrush"));
                string detail = result.Kind is WorkflowKind.Profiles or WorkflowKind.BitLocker
                    ? LocalizeBitLockerResultText(LocalizeProfileResultText(check.Detail))
                    : result.Kind == WorkflowKind.AppPackages ? LocalizePackageReviewText(check.Detail) : check.Detail;
                var text = Body(detail, 12, new Thickness(0));
                text.MaxHeight = 64;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                text.ToolTip = detail;
                content.Children.Add(text);
                AreaReviewCards.Children.Add(AreaCard(content));
            }
            if (problems.Length == 0)
                AreaReviewCards.Children.Add(AreaCard(Body(NextAction(result), 13, new Thickness(0))));
            if (problems.Length > 2)
                AreaReviewCards.Children.Add(Body(LocalizedText.Format("Str_Report_MoreReviewItems", problems.Length - 2), 12, new Thickness(0, 0, 0, 8), "MutedTextBrush"));
        }
        else
        {
            AreaState.SetResourceReference(TextBlock.TextProperty, "Str_Workspace_NotChecked");
            IEnumerable<WorkflowKind> choices = InvestigatePage.IsVisible && InvestigationAreas.SelectedItem is GuidedSymptom symptom && symptom.Area == area.Id
                ? new[] { symptom.Workflow }.Concat(area.Tools.Where(kind => kind != symptom.Workflow)).Take(3)
                : area.Tools.Take(3);
            foreach (var kind in choices)
            {
                var button = new Button
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(2, 4, 2, 4),
                    ToolTip = LocalizedText.Get("Str_Area_ChooseChecks")
                };
                var row = new DockPanel();
                var icon = new AreaIllustration { Id = area.Id, Width = 34, Height = 34, Margin = new Thickness(0, 0, 12, 0), IsHitTestVisible = false };
                icon.SetResourceReference(AreaIllustration.StrokeProperty, "PrimaryBrush");
                DockPanel.SetDock(icon, Dock.Left);
                row.Children.Add(icon);
                row.Children.Add(Body(LocalizedText.Workflow(kind), 14, new Thickness(0)));
                button.Content = row;
                button.SetResourceReference(StyleProperty, "AreaChoiceButton");
                button.SetResourceReference(FontFamilyProperty, "UiFont");
                System.Windows.Automation.AutomationProperties.SetName(button, LocalizedText.Workflow(kind));
                button.Click += (_, _) => OpenAreaTool(kind);
                AreaReviewCards.Children.Add(AreaCard(button));
            }
        }
        SetAreaEvidenceMode(true);
        ReportPane.Visibility = ConsolePane.Visibility = StartGuide.Visibility = ConsoleSearchPanel.Visibility = Visibility.Collapsed;
        SetOutputOpen(true);
    }

    private static Border AreaCard(UIElement content)
    {
        var grid = new Grid();
        grid.Children.Add(new Border { Child = content, Padding = new Thickness(14) });
        var grain = new Border();
        grain.SetResourceReference(StyleProperty, "FlyoutGrain");
        grid.Children.Add(grain);
        var card = new Border
        {
            Child = grid, Margin = new Thickness(0, 0, 0, 10), BorderThickness = new Thickness(1)
        };
        card.SetResourceReference(Border.BackgroundProperty, "ReportCardBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "PaneBorderBrush");
        card.SetResourceReference(Border.CornerRadiusProperty, "ControlCornerRadius");
        return card;
    }

    private void AddAreaCoverage(string label, int count, int total, int column)
    {
        AreaCoverageChart.ColumnDefinitions.Add(new ColumnDefinition());
        var stack = new StackPanel { Margin = new Thickness(0, 0, column == 2 ? 0 : 12, 0) };
        var value = Body(count.ToString("N0", CultureInfo.CurrentCulture), 27, new Thickness(0, 0, 0, 2), column == 1 ? "PrimaryBrush" : "TextBrush");
        stack.Children.Add(value);
        stack.Children.Add(Body(LocalizedText.Get(label), 11, new Thickness(0, 0, 0, 8), "MutedTextBrush"));
        double fraction = total > 0 ? Math.Clamp((double)count / total, 0, 1) : 0;
        var bar = new Grid { Height = 5 };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fraction, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - fraction, GridUnitType.Star) });
        var track = new Border { Opacity = .18, CornerRadius = new CornerRadius(2) };
        track.SetResourceReference(Border.BackgroundProperty, "MutedTextBrush");
        Grid.SetColumnSpan(track, 2);
        bar.Children.Add(track);
        var fill = new Border { CornerRadius = new CornerRadius(2) };
        fill.SetResourceReference(Border.BackgroundProperty, column == 1 ? "PrimaryBrush" : "MutedTextBrush");
        bar.Children.Add(fill);
        stack.Children.Add(bar);
        Grid.SetColumn(stack, column);
        AreaCoverageChart.Children.Add(stack);
    }

    private void OpenAreaTool(WorkflowKind kind)
    {
        if (!CanSearchWorkspace) return;
        MainAutoRepair.IsChecked = false;
        ShowToolsWorkspace();
        MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(tool => tool.Kind == kind);
        SynchronizeNavigation();
        CategoryWorkflow.Focus();
    }

    private void OnAreaOpenReport(object sender, RoutedEventArgs e)
    {
        if (!CanSearchWorkspace || areaSummarySource is not { } source || !sources.Contains(source) ||
            !workflowSummaries.TryGetValue(source, out var result)) return;
        evidence = source;
        ShowEvidence();
        if (workflowReportTitles.TryGetValue(source, out var title)) RenderNamedWorkflowReport(result, title);
        else RenderWorkflowReport(result);
        SetAreaEvidenceMode(false);
        if (sender is Button { Tag: "raw" }) ShowRawOutput();
        else { rawView = false; SetOutputView(); }
    }
}
