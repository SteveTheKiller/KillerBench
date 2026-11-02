using System.Windows;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private async Task OpenEvidenceBatchAsync(string[] paths)
    {
        if (readCancellation is not null) return;
        if (paths.Length > 12 || paths.Any(path => System.IO.Path.GetExtension(path).Equals(".kbench", StringComparison.OrdinalIgnoreCase)))
        { Report(Controls.LocalizedText.Get("Str_Ui_BatchLimit")); return; }
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        readCancellation = stop;
        SetCaptureBusy(true);
        int loaded = 0, failed = 0;
        var imported = new List<InvestigationReport>();
        Report(Controls.LocalizedText.Format("Str_Ui_BatchStarting", paths.Length));
        try
        {
            await Task.Run(async () =>
            {
                await foreach (var item in EvidenceBatch.ReadAsync(paths, stop.Token))
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (item.Evidence is { } source && item.Report is { } report)
                        {
                            reports.Add(source, report);
                            imported.Add(report);
                            RegisterSource(source);
                            loaded++;
                            Report(Controls.LocalizedText.Format("Str_Ui_ImportedSource", source.Source));
                        }
                        else { failed++; Report(Controls.LocalizedText.Format("Str_Ui_ImportFailed", item.Path, item.Error)); }
                    });
            }, stop.Token);
            if (loaded > 0)
            {
                var (overview, summary) = EvidenceBatch.Summarize(imported, failed);
                workflowSummaries.Add(overview, summary);
                workflowReportTitles.Add(overview, Controls.LocalizedText.Get("Str_Ui_ImportedReport"));
                RegisterSource(overview);
                RenderNamedWorkflowReport(summary, Controls.LocalizedText.Get("Str_Ui_ImportedReport"));
                SetOutputView();
                OnFindingsTab(this, new RoutedEventArgs());
            }
            Report(Controls.LocalizedText.Format("Str_Ui_BatchFinished", loaded, failed));
        }
        catch (OperationCanceledException)
        { Report(Controls.LocalizedText.Format("Str_Ui_BatchStopped", loaded, failed)); }
        finally
        {
            readCancellation = null;
            SetCaptureBusy(false);
            SetScanLight("incomplete", Controls.LocalizedText.Get("Str_RecordedEvidence"));
        }
    }
}
