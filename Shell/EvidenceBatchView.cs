using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private async Task OpenEvidenceBatchAsync(string[] paths)
    {
        if (readCancellation is not null) return;
        if (paths.Length > 12 || paths.Any(path => System.IO.Path.GetExtension(path).Equals(".kbench", StringComparison.OrdinalIgnoreCase)))
        { Report("Choose up to 12 evidence files. Open saved captures separately."); return; }
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        readCancellation = stop;
        SetCaptureBusy(true);
        int loaded = 0, failed = 0;
        var imported = new List<InvestigationReport>();
        Report($"Importing {paths.Length} evidence files. Each file has a 15-second limit.");
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
                            Report("Imported: " + source.Source);
                        }
                        else { failed++; Report("Import failed: " + item.Path + ". " + item.Error); }
                    });
            }, stop.Token);
            if (loaded > 0)
            {
                var findings = imported.SelectMany(report => report.Findings).ToArray();
                var checks = new List<WorkflowCheck>
                {
                    new("Imported sources", failed == 0 ? CheckState.Information : CheckState.Unknown,
                        loaded + " source" + (loaded == 1 ? " was" : "s were") + " imported. " + failed + " failed.")
                };
                checks.AddRange(findings.Select(finding => new WorkflowCheck(finding.Title,
                    finding.Severity == "Information" ? CheckState.Information : CheckState.Attention,
                    finding.Explanation + " Next: " + finding.NextStep)));
                var summary = new WorkflowResult(WorkflowKind.WindowsUpdate, Environment.MachineName, DateTimeOffset.UtcNow,
                    findings.Length == 0 ? "Imported evidence reviewed" : "Imported evidence needs review",
                    checks, [],
                    "Imported evidence only. No repair journal was written.");
                var overview = new TextEvidence("Imported evidence summary", ["IMPORTED EVIDENCE", loaded + " loaded", failed + " failed"], false);
                workflowSummaries.Add(overview, summary);
                workflowReportTitles.Add(overview, "Imported evidence");
                RegisterSource(overview);
                RenderNamedWorkflowReport(summary, "Imported evidence");
                SetOutputView(report: true);
                OnFindingsTab(this, new RoutedEventArgs());
            }
            Report($"Batch import finished: {loaded} loaded, {failed} failed. Source files unchanged.");
        }
        catch (OperationCanceledException)
        { Report($"Batch import stopped: {loaded} loaded, {failed} failed. Successfully loaded evidence retained."); }
        finally
        {
            readCancellation = null;
            SetCaptureBusy(false);
            SetScanLight("incomplete", "Imported evidence only; no current-health assessment.");
        }
    }
}
