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
            var recovery = await Task.Run(() => TimeConfigurationRecovery.ReconcileAsync(folder, token: stop.Token));
            string detail = recovery.Detail;
            AppendWorkflowOutput("TIME RECOVERY / " + recovery.Outcome.ToString().ToUpperInvariant() + "\n" + detail +
                "\nPending record: " + recovery.PendingPath + "\nArchived record: " + (recovery.ArchivedPath ?? "None") +
                "\nNo clock or source settings changed.");
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.TimeService, Environment.MachineName, DateTimeOffset.UtcNow,
                detail,
                [new WorkflowCheck("Recovery result", recovery.Resolved ? CheckState.Clear : CheckState.Unknown, detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "This recovery check did not synchronize the clock, change a time source, or retry a prior change.")], [],
                "Read-only time recovery check. No time-change journal was written."), "Time recovery check");
            Report(detail);
            SetScanLight(recovery.Resolved ? "ready" : "incomplete", detail);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or OperationCanceledException)
        {
            string detail = "Recovery check did not complete: " + error.Message;
            AppendWorkflowOutput("TIME RECOVERY / INCOMPLETE\n" + detail + "\nNo change retried. Pending evidence was not resolved by this check.");
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.TimeService, Environment.MachineName, DateTimeOffset.UtcNow,
                detail,
                [new WorkflowCheck("Recovery result", CheckState.Unknown, detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "No time source or clock setting was changed, and no pending change was retried.")], [],
                "Incomplete read-only time recovery check. No retry was requested."), "Time recovery check");
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
