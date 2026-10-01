using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

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
        if (ReferenceEquals(shownReport, report)) return;
        shownReport = report;
        FindingCards.Children.Clear();
        FindingsSummary.Text = $"{report.Format}\n{report.ParsedLines:N0} parsed entries / {report.LinesAnalyzed:N0} inspected lines\n" +
            (report.Partial ? "Partial evidence or analysis.\n" : "") +
            (report.Findings.Count == 0 ? "No supported rule matched. This is not a health verdict." : $"{report.Findings.Count} findings. Recorded events only.");
        foreach (var finding in report.Findings)
        {
            var card = new StackPanel { Margin = new Thickness(0, 12, 0, 6) };
            var title = new TextBlock { Text = finding.Title, TextWrapping = TextWrapping.Wrap, FontSize = 16 };
            title.SetResourceReference(TextBlock.FontFamilyProperty, "WordmarkFont");
            title.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
            card.Children.Add(title);
            AddText($"{finding.RuleId} v{finding.RuleVersion} | {finding.Severity}\n{finding.Certainty} | {finding.Occurrences} occurrences");
            AddText(finding.Explanation);
            if (report.Source == "Windows Update capture" && finding.Evidence.Count > 0) AddText("Source: " + finding.Evidence[0].Source);
            AddText("Next: " + finding.NextStep);
            if ((finding.Evidence.Count > 0 ? finding.Evidence[0] : null) is { } reference)
            {
                var button = new Button { Content = "Show evidence", Tag = finding.RuleId, Margin = new Thickness(0, 4, 0, 0), ToolTip = reference.Excerpt };
                System.Windows.Automation.AutomationProperties.SetName(button, "Show evidence: " + finding.Title);
                button.ToolTip = reference.Source + "\n" + reference.Excerpt;
                button.Click += (_, _) =>
                {
                    int index = sources.FindIndex(s => s.Source == reference.Source);
                    if (index >= 0) SourcesList.SelectedIndex = index;
                    else if (capture?.Sources.FirstOrDefault(s => s.Evidence?.Source == reference.Source)?.Evidence is { } retained) RegisterSource(retained);
                    else { Report("The referenced evidence is no longer in the session."); return; }
                    ShowReference(reference.LineNumber);
                    CoverageLabel.Text += $" Related lines: {string.Join(", ", finding.Evidence.Select(r => r.LineNumber))}.";
                };
                card.Children.Add(button);
            }
            var source = new TextBox { Text = finding.ReferenceUrl, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
            card.Children.Add(source);
            FindingCards.Children.Add(card);
            void AddText(string value) => card.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0), FontSize = 11 });
        }
    }

    private void ShowReference(int lineNumber)
    {
        if (evidence is null || lineNumber < 1 || lineNumber > evidence.Lines.Length) return;
        SearchBox.Clear();
        int start = Math.Max(0, lineNumber - 16), end = Math.Min(evidence.Lines.Length, lineNumber + 15);
        var displayedLines = Enumerable.Range(start, end - start).Select(i => $"{i + 1}  {evidence.Lines[i]}").ToArray();
        EvidenceBox.Text = string.Join(Environment.NewLine, displayedLines);
        int localLine = lineNumber - 1 - start;
        int offset = displayedLines.Take(localLine).Sum(line => line.Length + Environment.NewLine.Length);
        EvidenceBox.Select(offset, displayedLines[localLine].Length);
        EvidenceBox.ScrollToLine(EvidenceBox.GetLineIndexFromCharacterIndex(offset));
        EvidenceBox.Focus();
        CoverageLabel.Text = $"Evidence context: lines {start + 1} through {end}. FIND returns to the full loaded evidence.";
    }

    private async void OnDemo(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        PathBox.Text = Path.Combine(AppContext.BaseDirectory, "Samples", "CBS.log");
        await OpenEvidenceAsync();
        if (evidence?.Source == PathBox.Text)
        {
            ModeLabel.Text = "SYNTHETIC DEMO / RULES EVALUATED";
            Report("Synthetic CBS demo loaded. No system scan performed.");
        }
    }

    private async void OnExportReport(object sender, RoutedEventArgs e)
    {
        ScheduledTaskInspectionReport? taskInspection = null;
        WorkflowJournalSnapshot? historical = null;
        WorkflowResult? workflow = null;
        if (evidence is not null) taskInspectionReports.TryGetValue(evidence, out taskInspection);
        if (evidence is not null) historicalWorkflows.TryGetValue(evidence, out historical);
        if (evidence is not null) workflowSummaries.TryGetValue(evidence, out workflow);
        if (workflow is null && historical is not null)
        {
            try { workflow = System.Text.Json.JsonSerializer.Deserialize<WorkflowResult>(historical.Record.GetRawText(), Capture.JsonOptions); }
            catch (System.Text.Json.JsonException) { }
        }
        if (shownReport is null && taskInspection is null && historical is null && workflow is null) return;
        var report = shownReport;
        var picker = workflow is null
            ? new SaveFileDialog { Filter = "JSON report|*.json", FileName = "KillerBench-report.json", Title = "Export evidence report" }
            : new SaveFileDialog
            {
                Filter = "Diagnostic HTML report|*.html|Diagnostic and repair HTML report|*.html|Diagnostic text report|*.txt|Diagnostic and repair text report|*.txt",
                FileName = "KillerBench-report",
                AddExtension = true,
                Title = "Export a structured workflow report"
            };
        if (picker.ShowDialog(this) != true) return;
        if (sources.Any(source => string.Equals(source.Source, Path.GetFullPath(picker.FileName), StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(capturePath, Path.GetFullPath(picker.FileName), StringComparison.OrdinalIgnoreCase))
        { Report("Choose a report destination separate from your source evidence."); return; }
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
                Report((includeRepairs ? "Diagnostic and repair" : "Diagnostic") + " report exported: " + picker.FileName);
            }
            else
            {
                string json = taskInspection is not null ? System.Text.Json.JsonSerializer.Serialize(taskInspection, Capture.JsonOptions) :
                    capture is not null && report!.Source == "Windows Update capture" ? await Task.Run(() => Capture.ReportJson(capture)) : Investigation.ToJson(report!);
                await File.WriteAllTextAsync(picker.FileName, json);
                Report("Evidence report exported with source paths and excerpts: " + picker.FileName);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Report("Cannot export report: " + error.Message); }
    }
}
