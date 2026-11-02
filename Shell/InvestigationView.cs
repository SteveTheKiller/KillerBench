using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private readonly ConditionalWeakTable<TextEvidence, InvestigationReport> reports = [];
    private InvestigationReport? shownReport;

    private void OnFindingsTab(object sender, RoutedEventArgs e)
    {
        RepairPanel.Visibility = Visibility.Collapsed;
        SourceDetails.Visibility = Visibility.Collapsed;
        DisplaySettings.Visibility = Visibility.Collapsed;
        FindingsPanel.Visibility = Visibility.Visible;
        SetDetailsOpen(true);
    }

    private void RefreshFindings(InvestigationReport report)
    {
        if (ReferenceEquals(shownReport, report) && lastReport is null && reportAvailable) return;
        shownReport = report;
        FindingCards.Children.Clear();
        FindingsSummary.Text = report.Format + "\n" +
            Controls.LocalizedText.Format("Str_Ui_EvidenceCounts", report.ParsedLines, report.LinesAnalyzed) + "\n" +
            (report.Partial ? Controls.LocalizedText.Get("Str_Ui_PartialEvidence") + "\n" : "") +
            (report.Findings.Count == 0 ? Controls.LocalizedText.Get("Str_Ui_NoRuleMatch")
                : Controls.LocalizedText.Format("Str_Ui_FindingsCount", report.Findings.Count));
        bool importedReport = evidence is not null && !workflowSummaries.TryGetValue(evidence, out _) &&
            !historicalWorkflows.TryGetValue(evidence, out _);
        if (importedReport)
        {
            reportAvailable = true;
            lastReport = null;
            rawView = false;
            ReportContent.Children.Clear();
            ReportContent.Children.Add(Heading(Controls.LocalizedText.Get("Str_Ui_ImportedReport"), 15));
            ReportContent.Children.Add(Body(FindingsSummary.Text + "\n" + report.Limitation, 12, new Thickness(0, 0, 0, 12)));
        }
        foreach (var finding in report.Findings)
        {
            var card = new StackPanel { Margin = new Thickness(0, 12, 0, 6) };
            var title = new TextBlock { Text = finding.Title, TextWrapping = TextWrapping.Wrap, FontSize = 16 };
            title.SetResourceReference(TextBlock.FontFamilyProperty, "BrandSansFont");
            title.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
            card.Children.Add(title);
            AddText($"{finding.RuleId} v{finding.RuleVersion} | {finding.Severity}\n{finding.Certainty} | {finding.Occurrences} occurrences");
            AddText(finding.Explanation);
            if (report.Source == "Windows Update capture" && finding.Evidence.Count > 0) AddText("Source: " + finding.Evidence[0].Source);
            AddText(Controls.LocalizedText.Format("Str_Report_NextPrefix", finding.NextStep));
            if ((finding.Evidence.Count > 0 ? finding.Evidence[0] : null) is { } reference)
            {
                var button = new Button { Content = Controls.LocalizedText.Get("Str_Ui_ShowEvidence"), Tag = finding.RuleId, Margin = new Thickness(0, 4, 0, 0), ToolTip = reference.Excerpt };
                System.Windows.Automation.AutomationProperties.SetName(button, Controls.LocalizedText.Get("Str_Ui_ShowEvidence") + ": " + finding.Title);
                button.ToolTip = reference.Source + "\n" + reference.Excerpt;
                button.Click += (_, _) =>
                {
                    int index = sources.FindIndex(s => s.Source == reference.Source);
                    if (index >= 0) SourcesList.SelectedIndex = index;
                    else if (capture?.Sources.FirstOrDefault(s => s.Evidence?.Source == reference.Source)?.Evidence is { } retained) RegisterSource(retained);
                    else { Report(Controls.LocalizedText.Get("Str_Ui_SourceMissing")); return; }
                    ShowReference(reference.LineNumber);
                    CoverageLabel.Text += " " + Controls.LocalizedText.Format("Str_Ui_RelatedLines",
                        string.Join(", ", finding.Evidence.Select(r => r.LineNumber)));
                };
                card.Children.Add(button);
            }
            var source = new TextBox { Text = finding.ReferenceUrl, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
            card.Children.Add(source);
            if (importedReport) ReportContent.Children.Add(card);
            else FindingCards.Children.Add(card);
            void AddText(string value) => card.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0), FontSize = 11 });
        }
        if (importedReport) SetOutputView();
    }

    private void ShowReference(int lineNumber)
    {
        if (evidence is null || lineNumber < 1 || lineNumber > evidence.Lines.Length) return;
        // The raw evidence lives in the slide-out terminal; open it so the referenced lines are visible.
        ShowRawOutput();
        EvidenceBox.UpdateLayout();
        SearchBox.Clear();
        int start = Math.Max(0, lineNumber - 16), end = Math.Min(evidence.Lines.Length, lineNumber + 15);
        var displayedLines = Enumerable.Range(start, end - start).Select(i => $"{i + 1}  {evidence.Lines[i]}").ToArray();
        EvidenceBox.Text = string.Join(Environment.NewLine, displayedLines);
        int localLine = lineNumber - 1 - start;
        int offset = displayedLines.Take(localLine).Sum(line => line.Length + Environment.NewLine.Length);
        EvidenceBox.Select(offset, displayedLines[localLine].Length);
        int lineIndex = EvidenceBox.GetLineIndexFromCharacterIndex(offset);
        if (lineIndex >= 0) EvidenceBox.ScrollToLine(lineIndex);
        EvidenceBox.Focus();
        CoverageLabel.Text = Controls.LocalizedText.Format("Str_Ui_EvidenceContext", start + 1, end);
    }

    private async void OnDemo(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        PathBox.Text = Path.Combine(AppContext.BaseDirectory, "Samples", "CBS.log");
        await OpenEvidenceAsync();
        if (evidence?.Source == PathBox.Text)
        {
            ModeLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "Str_Mode_DemoDiagnosis");
            Report(Controls.LocalizedText.Get("Str_Ui_SampleLoaded"));
        }
    }

    private WorkflowResult? ExportWorkflow(out ScheduledTaskInspectionReport? taskInspection, out WorkflowJournalSnapshot? historical)
    {
        taskInspection = null;
        historical = null;
        WorkflowResult? workflow = null;
        if (evidence is not null) taskInspectionReports.TryGetValue(evidence, out taskInspection);
        if (evidence is not null) historicalWorkflows.TryGetValue(evidence, out historical);
        if (evidence is not null) workflowSummaries.TryGetValue(evidence, out workflow);
        if (workflow is null && historical is not null)
        {
            try { workflow = historical.ReadWorkflow(); }
            catch (InvalidDataException) { }
        }
        return workflow;
    }

    private Controls.FileDialog CreateExportPicker()
    {
        var workflow = ExportWorkflow(out _, out _);
        var picker = workflow is null
            ? new Controls.FileDialog(Controls.FileDialogMode.Save) { Filter = "JSON (*.json)|*.json", FileName = "KillerBench-report.json", Title = Controls.LocalizedText.Get("Str_ExportReport") }
            : new Controls.FileDialog(Controls.FileDialogMode.Save)
            {
                Filter = Controls.LocalizedText.Get("Str_Ui_DiagnosticReport") + " (HTML)|*.html|" +
                    Controls.LocalizedText.Get("Str_Ui_DiagnosticRepairReport") + " (HTML)|*.html|" +
                    Controls.LocalizedText.Get("Str_Ui_DiagnosticReport") + " (TXT)|*.txt|" +
                    Controls.LocalizedText.Get("Str_Ui_DiagnosticRepairReport") + " (TXT)|*.txt",
                FileName = "KillerBench-report",
                AddExtension = true,
                Title = Controls.LocalizedText.Get("Str_ExportReport")
            };
        // While a new run streams, the retained selection is an earlier report; say which one.
        if (workflowOutputActive && evidence is not null)
            picker.Title = Controls.LocalizedText.Format("Str_Ui_ExportEarlierReport", evidence.Source);
        return picker;
    }

    private async void OnExportReport(object sender, RoutedEventArgs e)
    {
        var workflow = ExportWorkflow(out var taskInspection, out var historical);
        if (shownReport is null && taskInspection is null && historical is null && workflow is null) return;
        var report = shownReport;
        var picker = CreateExportPicker();
        if (picker.ShowDialog(this) != true) return;
        if (sources.Any(source => string.Equals(source.Source, Path.GetFullPath(picker.FileName), StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(capturePath, Path.GetFullPath(picker.FileName), StringComparison.OrdinalIgnoreCase))
        { Report(Controls.LocalizedText.Get("Str_Ui_ExportSeparateDest")); return; }
        try
        {
            if (workflow is not null)
            {
                bool includeRepairs = picker.FilterIndex is 2 or 4;
                bool html = picker.FilterIndex is 1 or 2;
                var scope = includeRepairs ? WorkflowReportScope.DiagnosticsAndRepairs : WorkflowReportScope.Diagnostics;
                string contents = html
                    ? WorkflowReportExport.BuildHtml(workflow, scope, Services.ThemeManager.Current.ToString(),
                        Services.ThemeManager.AccentChoiceFor(Services.ThemeManager.Current).ToString())
                    : WorkflowReportExport.BuildText(workflow, scope);
                await File.WriteAllTextAsync(picker.FileName, contents);
                Report(Controls.LocalizedText.Format("Str_Ui_Saved", Controls.LocalizedText.Get(includeRepairs ? "Str_Ui_DiagnosticRepairReport" : "Str_Ui_DiagnosticReport"), picker.FileName));
            }
            else
            {
                string json = taskInspection is not null ? System.Text.Json.JsonSerializer.Serialize(taskInspection, Capture.JsonOptions) :
                    capture is not null && report!.Source == "Windows Update capture" ? await Task.Run(() => Capture.ReportJson(capture)) : Investigation.ToJson(report!);
                await File.WriteAllTextAsync(picker.FileName, json);
                Report(Controls.LocalizedText.Format("Str_Ui_Saved", Controls.LocalizedText.Get("Str_ExportReport"), picker.FileName) + " " + Controls.LocalizedText.Get("Str_ExportContents"));
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Get("Str_ExportReport"), error.Message)); }
    }
}
