using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private void LockScreenshotPresentation()
    {
        ZoomViewport.IsHitTestVisible = false;
        WorkspaceNavigation.IsHitTestVisible = false;
        TitleSearchBox.IsEnabled = false;
        AllowDrop = false;
    }

    private async Task RunDemoWorkflowAsync(WorkflowKind kind, bool simulateRepair, bool screenshotPreset = false)
    {
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        ModeLabel.Text = Controls.LocalizedText.Get("Str_Mode_InProgress");
        BeginWorkflowOutput(kind, demo: true);
        try
        {
            foreach (string message in DemoWorkflow.Progress(kind, simulateRepair))
            {
                stop.Token.ThrowIfCancellationRequested();
                AppendWorkflowOutput(message);
                if (!screenshotPreset) await Task.Delay(180, stop.Token);
            }
            var result = DemoWorkflow.Create(kind, simulateRepair, screenshotPreset: screenshotPreset);
            AppendWorkflowOutput(WorkflowReport.Format(result));
            RegisterWorkflowResult(result, demo: true);
            CaptureInfo.Text = result.Status + " | " + result.Checks.Count + " checks | " + result.Repairs.Count + " repairs";
            Report(LocalizedStatus(result.Status));
            SetScanLight(simulateRepair ? "ready" : "attention", NextAction(result));
        }
        catch (OperationCanceledException)
        {
            AppendWorkflowOutput("Operation stopped.");
            SetScanLight("incomplete", Controls.LocalizedText.Format("Str_Ui_Stopped", Controls.LocalizedText.Workflow(kind)));
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            repairRunning = false;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            RepairConsent.IsChecked = false;
            ModeLabel.Text = Controls.LocalizedText.Get("Str_Mode_Ended");
            if (closeAfterOperation) Close();
        }
    }
}
