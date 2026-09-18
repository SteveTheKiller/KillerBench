using System.IO;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private async void OnRecoverTime(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        TimeApply.IsChecked = false;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        ModeLabel.Text = "TIME / RECOVERY CHECK";
        BeginWorkflowOutput(WorkflowKind.TimeService);
        try
        {
            AppendWorkflowOutput("Checking an interrupted time change. No source change or synchronization will be retried.");
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Time");
            var recovery = await Task.Run(() => TimeConfigurationRecovery.ReconcileAsync(folder, stop.Token));
            string detail = recovery.Detail;
            workflowOutputActive = false;
            RegisterSource(new TextEvidence("Time recovery check",
                new[] { "TIME RECOVERY / " + recovery.Outcome.ToString().ToUpperInvariant(), detail,
                    "Pending record: " + recovery.PendingPath,
                    "Archived record: " + (recovery.ArchivedPath ?? "None"), "No clock or source settings changed." }, false));
            Report(detail);
            SetScanLight(recovery.Resolved ? "ready" : "incomplete", detail);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or OperationCanceledException)
        {
            string detail = "Recovery check did not complete: " + error.Message;
            workflowOutputActive = false;
            RegisterSource(new TextEvidence("Time recovery incomplete",
                new[] { "TIME RECOVERY / INCOMPLETE", detail, "No change retried. Pending evidence was not resolved by this check." }, false));
            Report(detail);
            SetScanLight("incomplete", "Recovery check incomplete. No change retried.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            repairRunning = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            ModeLabel.Text = "TIME RECOVERY ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
