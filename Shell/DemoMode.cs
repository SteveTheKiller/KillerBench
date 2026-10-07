using KillerBench.Engine;

namespace KillerBench;

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
        ModeLabel.Text = simulateRepair ? "DEMO / SIMULATED REPAIR" : "DEMO / SYNTHETIC DIAGNOSIS";
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
            Report(result.Status + ". No machine data was read and no journal was written.");
            SetScanLight(simulateRepair ? "ready" : "attention", result.Status + ". Synthetic demonstration only.");
        }
        catch (OperationCanceledException)
        {
            AppendWorkflowOutput("Demo stopped. No machine state was changed.");
            SetScanLight("incomplete", "Synthetic demonstration stopped.");
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
            ModeLabel.Text = screenshotPreset ? "DEMO SCREENSHOT / SYNTHETIC MACHINE" : "DEMO GO / OTHER TOOLS LIVE";
            if (closeAfterOperation) Close();
        }
    }
}
