using System.IO;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

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
        if (!busy) CompleteWorkflowOutput();
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
        if (busy) SetScanLight("running", Controls.LocalizedText.Get("Str_Ui_Working"));
        else if (ScanLight.Tag as string == "running") SetScanLight("incomplete", Controls.LocalizedText.Get("Str_Ui_EndedReview"));
        ScanButton.IsEnabled = !busy;
        OpenCaptureButton.IsEnabled = !busy;
        SaveCaptureButton.IsEnabled = !busy && capture is not null;
        OpenButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        ExportReportButton.IsEnabled = true;
        // Raw output slides out on its own while work runs and slides back when it ends.
        // Raw output streams in the right sidebar while work runs; the report takes over when it lands.
        if (busy) ShowRawOutput();
        else if (reportAvailable) { rawView = false; SetOutputView(); }
    }

    private async void OnScan(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        readCancellation = stop;
        SetCaptureBusy(true);
        ModeLabel.Text = Controls.LocalizedText.Get("Str_Mode_Collecting");
        Report(Controls.LocalizedText.Get("Str_CaptureScope"));
        try
        {
            var progress = new Progress<string>(Report);
            var collected = await Task.Run(() => Capture.CollectAsync(WindowsUpdateCollectors.Create(), progress, stop.Token));
            // Keep completed collection even after Stop; analysis of bounded snapshots needs no live reads.
            var analyzed = await Task.Run(() => collected.Sources.Where(s => s.Evidence is not null).ToDictionary(s => s.Evidence!, s => Capture.Analyze(s)));
            ApplyCapture(collected, analyzed, null);
            SetScanLight(analyzed.Values.Any(r => r.Findings.Count > 0) ? "attention" : "incomplete", Controls.LocalizedText.Get("Str_RecordedEvidence"));
            Report(Controls.LocalizedText.Get(collected.Partial ? "Str_Ui_CapturePartial" : "Str_Ui_CaptureReady"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Get("Str_OpenCapture"), error.Message)); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnOpenCapture(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Filter = "KillerBench (*.kbench)|*.kbench|JSON (*.json)|*.json", Title = Controls.LocalizedText.Get("Str_OpenCapture") };
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
            Report(Controls.LocalizedText.Get("Str_Ui_CaptureLoaded"));
        }
        catch (OperationCanceledException) { Report(Controls.LocalizedText.Get("Str_Ui_ReadStopped")); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Get("Str_OpenCapture"), error.Message)); }
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
        SetOutputView();
        SaveCaptureButton.IsEnabled = true;
        CaptureInfo.Text = $"{session.Machine} | {session.Elevation} | {session.StartedUtc.ToLocalTime():g} | {session.Sources.Count(s => s.Evidence is not null)}/{session.Sources.Count} sources | {findings.Length} findings";
        OnFindingsTab(this, new RoutedEventArgs());
    }

    private async void OnSaveCapture(object sender, RoutedEventArgs e)
    {
        if (capture is null || readCancellation is not null) return;
        var saved = capture;
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Save) { Filter = "KillerBench (*.kbench)|*.kbench", FileName = "KillerBench-capture.kbench", Title = Controls.LocalizedText.Get("Str_Ui_CaptureSaveTitle") };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            await Capture.SaveAsync(saved, picker.FileName);
            capturePath = Path.GetFullPath(picker.FileName);
            Report(Controls.LocalizedText.Format("Str_Ui_Saved", Controls.LocalizedText.Get("Str_Ui_CaptureSaveTitle"), picker.FileName));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Get("Str_OpenCapture"), error.Message)); }
    }
}
