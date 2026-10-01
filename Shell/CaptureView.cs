using System.IO;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

public partial class MainWindow
{
    private CaptureSession? capture;
    private string? capturePath;

    private static WorkflowResult BuildCaptureWorkflowReport(CaptureSession session, DiagnosticFinding[] findings,
        bool partial)
    {
        var checks = new List<WorkflowCheck>
        {
            new("Capture coverage", partial ? CheckState.Unknown : CheckState.Information,
                partial ? "Some requested evidence could not be collected or analyzed completely. Current update health is unknown." :
                    "The selected log snapshots were collected and analyzed. They do not establish current update health.")
        };
        checks.AddRange(findings.Select(finding => new WorkflowCheck(finding.Title,
            finding.Severity == "Information" ? CheckState.Information : CheckState.Attention,
            finding.Explanation + " Next: " + finding.NextStep)));
        return new WorkflowResult(WorkflowKind.WindowsUpdate, session.Machine, session.StartedUtc,
            findings.Length == 0 ? "Windows Update capture reviewed" : "Windows Update capture needs review", checks, [],
            "Read-only Windows Update capture. No repair journal was written.");
    }

    private void SetCaptureBusy(bool busy)
    {
        MainGo.IsEnabled = !busy;
        MainWorkflow.IsEnabled = !busy;
        PrinterSettings.IsEnabled = !busy;
        TimeSettings.IsEnabled = !busy;
        TaskSettings.IsEnabled = !busy;
        SystemRestoreSettings.IsEnabled = !busy;
        CategoryList.IsEnabled = true;
        CategoryWorkflow.IsEnabled = true;
        MainAutoRepair.IsEnabled = !busy;
        CleanupSettings.IsEnabled = !busy;
        SecuritySettings.IsEnabled = !busy;
        UpdateSettings.IsEnabled = !busy;
        DebloatSettings.IsEnabled = !busy;
        if (!busy) OnWorkflowOptionsChanged(this, null);
        if (busy) SetScanLight("running", "Working. Stop waits for the current servicing command.");
        else if (ScanLight.ToolTip is string tip && tip.StartsWith("Working.")) SetScanLight("incomplete", "Operation ended. Review the result.");
        ScanButton.IsEnabled = !busy;
        OpenCaptureButton.IsEnabled = !busy;
        SaveCaptureButton.IsEnabled = !busy && capture is not null;
        OpenButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        ExportReportButton.IsEnabled = true;
    }

    private async void OnScan(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        readCancellation = stop;
        SetCaptureBusy(true);
        ModeLabel.Text = "COLLECTING / READ ONLY";
        Report("Windows Update scan: CBS, Panther and recent events. No repairs. Stop keeps completed sources.");
        try
        {
            var progress = new Progress<string>(Report);
            var collected = await Task.Run(() => Capture.CollectAsync(WindowsUpdateCollectors.Create(), progress, stop.Token));
            // Keep completed collection even after Stop; analysis of bounded snapshots needs no live reads.
            var analyzed = await Task.Run(() => collected.Sources.Where(s => s.Evidence is not null).ToDictionary(s => s.Evidence!, s => Capture.Analyze(s)));
            ApplyCapture(collected, analyzed, null);
            SetScanLight(analyzed.Values.Any(r => r.Findings.Count > 0) ? "attention" : "incomplete", "Capture finished. Historical logs do not establish current health.");
            Report((collected.Partial ? "Partial capture ready. Review source coverage." : "Capture ready.") + " Save capture to keep its evidence.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Report("Cannot collect: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnOpenCapture(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new OpenFileDialog { Filter = "KillerBench capture|*.kbench|JSON capture|*.json", Title = "Open saved capture" };
        if (picker.ShowDialog(this) != true) return;
        await OpenCaptureAsync(picker.FileName);
    }

    private async Task OpenCaptureAsync(string path)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            var loaded = await Task.Run(async () =>
            {
                var session = await Capture.OpenAsync(path, stop.Token);
                var analyzed = session.Sources.Where(s => s.Evidence is not null).ToDictionary(s => s.Evidence!, s => Capture.Analyze(s, stop.Token));
                return (session, analyzed);
            });
            ApplyCapture(loaded.session, loaded.analyzed, Path.GetFullPath(path));
            Report("Opened saved evidence. Findings evaluated with installed rules; no machine scan performed.");
        }
        catch (OperationCanceledException) { Report("Open stopped. Previous session retained."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report("Cannot open capture: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private void ApplyCapture(CaptureSession session, Dictionary<TextEvidence, InvestigationReport> analyzed, string? path)
    {
        capture = session;
        capturePath = path;
        sources.Clear();
        SourcesList.Items.Clear();
        foreach (var source in session.Sources.Where(s => s.Evidence is not null))
        {
            reports.Add(source.Evidence!, analyzed[source.Evidence!]);
            RegisterSource(source.Evidence!);
        }
        string[] lines = ["WINDOWS UPDATE CAPTURE / READ ONLY", $"Machine: {session.Machine}",
            $"OS: {session.OperatingSystem} ({session.Architecture}) | {session.Elevation}",
            $"Started: {session.StartedUtc:O}", $"Finished: {session.FinishedUtc:O}",
            "", "COVERAGE / snapshots are not an atomic view of the machine", ..session.Sources.Select(s => $"{s.Status}: {s.Name} | {s.Detail}"),
            "", "Select a source or a finding. No findings does not mean healthy. No repairs executed."];
        var overview = new TextEvidence("Windows Update capture", lines, false);
        var findings = analyzed.Values.SelectMany(r => r.Findings).OrderBy(f => f.RuleId, StringComparer.Ordinal).ToArray();
        reports.Add(overview, new("1", overview.Source, "Windows Update capture", analyzed.Values.Sum(r => r.LinesAnalyzed),
            analyzed.Values.Sum(r => r.ParsedLines), session.Partial || analyzed.Values.Any(r => r.Partial), [], findings,
            LogAnalyzer.Analyze(overview), "Grouped findings retain their original source references. No current-health verdict."));
        var workflowReport = BuildCaptureWorkflowReport(session, findings,
            session.Partial || analyzed.Values.Any(report => report.Partial));
        workflowSummaries.Add(overview, workflowReport);
        workflowReportTitles.Add(overview, "Windows Update capture");
        RegisterSource(overview);
        RenderNamedWorkflowReport(workflowReport, "Windows Update capture");
        SetOutputView(report: true);
        SaveCaptureButton.IsEnabled = true;
        CaptureInfo.Text = $"{session.Machine} | {session.Elevation} | {session.StartedUtc.ToLocalTime():g} | {session.Sources.Count(s => s.Evidence is not null)}/{session.Sources.Count} sources | {findings.Length} findings";
        OnFindingsTab(this, new RoutedEventArgs());
    }

    private async void OnSaveCapture(object sender, RoutedEventArgs e)
    {
        if (capture is null || readCancellation is not null) return;
        var saved = capture;
        var picker = new SaveFileDialog { Filter = "KillerBench capture|*.kbench", FileName = "KillerBench-capture.kbench", Title = "Save capture including machine identity, source paths and raw evidence" };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            await Capture.SaveAsync(saved, picker.FileName);
            capturePath = Path.GetFullPath(picker.FileName);
            Report("Capture saved locally with raw evidence and machine metadata: " + picker.FileName);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report("Cannot save capture: " + error.Message); }
    }
}
