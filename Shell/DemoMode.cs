using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private void LockScreenshotPresentation()
    {
        ZoomViewport.IsHitTestVisible = false;
        AllowDrop = false;
    }

    private async Task RunDemoWorkflowAsync(WorkflowKind kind, bool simulateRepair, bool screenshotPreset = false)
    {
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        ModeLabel.Text = Controls.LocalizedText.Get(simulateRepair ? "Str_Mode_DemoRepair" : "Str_Mode_DemoDiagnosis");
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
            RegisterWorkflowResult(result, demo: true);
            CaptureInfo.Text = result.Status + " | " + result.Checks.Count + " synthetic checks | 0 real repairs";
            Report(Controls.LocalizedText.Format("Str_Demo_Finished", LocalizedStatus(result.Status)));
            SetScanLight(simulateRepair ? "ready" : "attention", Controls.LocalizedText.Get(simulateRepair ? "Str_Report_DemoRepairNextStep" : "Str_Report_DemoNextAction"));
        }
        catch (OperationCanceledException)
        {
            AppendWorkflowOutput("Demo stopped. No machine state was changed.");
            SetScanLight("incomplete", Controls.LocalizedText.Format("Str_Ui_Stopped", Controls.LocalizedText.Get("Str_LoadDemo")));
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
            ModeLabel.Text = Controls.LocalizedText.Get(screenshotPreset ? "Str_Mode_DemoScreenshot" : "Str_Mode_DemoLive");
            if (closeAfterOperation) Close();
        }
    }
}
