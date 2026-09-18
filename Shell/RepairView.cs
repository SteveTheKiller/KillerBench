using System.IO;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private bool repairRunning;
    private bool workflowMode;
    private void OnWorkflows(object sender, RoutedEventArgs e)
    {
        if (repairRunning) return;
        OnRepairs(sender, e);
        workflowMode = true;
        RepairChoice.Items.Clear();
        foreach (var workflow in WorkflowCatalog.All)
            RepairChoice.Items.Add(new ComboBoxItem { Content = workflow.Title });
        RepairChoice.SelectedIndex = 0;
        RepairConsent.Content = "Auto-repair confirmed issues";
        System.Windows.Data.BindingOperations.ClearBinding(RepairRun, IsEnabledProperty);
        RepairRun.IsEnabled = true;
        RepairRun.ToolTip = "Run live checks on this PC; optional automatic repair of confirmed issues";
    }
    private void OnRepairs(object sender, RoutedEventArgs e)
    {
        if (repairRunning) return;
        workflowMode = false;
        RepairChoice.Items.Clear();
        RepairChoice.Items.Add(new ComboBoxItem { Content = "Component store (DISM)" });
        RepairChoice.Items.Add(new ComboBoxItem { Content = "System files (SFC)" });
        RepairChoice.SelectedIndex = 0;
        RepairConsent.Content = "Run this repair on this PC";
        RepairConsent.IsEnabled = true;
        RepairRun.ToolTip = "Run the previewed repair on this PC; administrator rights required";
        RepairConsent.IsChecked = false;
        RepairRun.SetBinding(IsEnabledProperty, new System.Windows.Data.Binding("IsChecked") { Source = RepairConsent });
        SourceDetails.Visibility = Visibility.Collapsed;
        DisplaySettings.Visibility = Visibility.Collapsed;
        FindingsPanel.Visibility = Visibility.Collapsed;
        RepairPanel.Visibility = Visibility.Visible;
        RepairPreview.Text = Repairs.Preview(RepairChoice.SelectedIndex == 1 ? RepairKind.SystemFiles : RepairKind.ComponentStore);
        SetDetailsOpen(true);
    }
    private void OnRepairChoice(object sender, SelectionChangedEventArgs e)
    {
        if (RepairPreview is not null)
        {
            if (workflowMode && RepairChoice.SelectedIndex == (int)WorkflowKind.Cleanup)
                MainWorkflow.SelectedIndex = (int)WorkflowKind.Cleanup;
            if (workflowMode && RepairChoice.SelectedIndex == (int)WorkflowKind.Defender)
                MainWorkflow.SelectedIndex = (int)WorkflowKind.Defender;
            if (workflowMode && RepairChoice.SelectedIndex == (int)WorkflowKind.AvailableUpdates)
                MainWorkflow.SelectedIndex = (int)WorkflowKind.AvailableUpdates;
            RepairPreview.Text = workflowMode ? Workflows.Preview((WorkflowKind)Math.Max(0, RepairChoice.SelectedIndex))
                : Repairs.Preview(RepairChoice.SelectedIndex == 1 ? RepairKind.SystemFiles : RepairKind.ComponentStore);
            RepairConsent.IsChecked = false;
            RepairConsent.IsEnabled = !workflowMode || Workflows.SupportsRepair((WorkflowKind)Math.Max(0, RepairChoice.SelectedIndex));
        }
    }
    private async void OnRunRepair(object sender, RoutedEventArgs e)
    {
        if (workflowMode) { await RunWorkflowAsync(); return; }
        if (readCancellation is not null || RepairConsent.IsChecked != true) return;
        var kind = RepairChoice.SelectedIndex == 1 ? RepairKind.SystemFiles : RepairKind.ComponentStore;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        RepairChoice.IsEnabled = false;
        ModeLabel.Text = "REPAIR / THIS PC";
        Report("Repair requested for this PC. Stop waits for the current command; it does not kill servicing.");
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs");
            var progress = new Progress<string>(message => { if (!string.IsNullOrWhiteSpace(message)) Report(message); });
            var execution = await Task.Run(() => ServicingRepairExecution.RunAsync(kind, folder,
                new WindowsWorkflowHost(), progress, stop.Token));
            if (execution.Repair is null)
            {
                RepairPreview.Text = Repairs.Preview(kind) + "\n\nFRESH EVIDENCE: " + execution.Eligibility.Detail +
                    "\n\nRESULT: No repair was run.";
                Report("Fresh evidence did not authorize the selected repair. No servicing command was run.");
                SetScanLight("attention", execution.Eligibility.Detail);
                return;
            }
            var result = execution.Repair;
            RepairPreview.Text = Repairs.Preview(kind) + "\n\nFRESH EVIDENCE: " + execution.Eligibility.Detail +
                "\n\nRESULT: " + result.Status + "\nJournal: " + result.JournalPath;
            Report(result.Status + ". Journal: " + result.JournalPath);
            SetScanLight(result.Status.StartsWith("Verification reported") ? "ready" : "attention", result.Status);
            var text = new TextEvidence("Repair result " + DateTime.Now.ToString("HHmmss"),
                (RepairPreview.Text + "\n\n" + string.Join("\n", result.Steps.Select(s => s.Step.Tool + "\n" + s.Result.Output + "\n" + s.Result.Error)))
                .Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'), false);
            reports.Add(text, Investigation.Analyze(text) with { Limitation = "Recorded repair execution. Refer to the journal for commands, outputs and verification status." });
            RegisterSource(text);
            OnRepairs(this, new RoutedEventArgs());
            RepairPreview.Text += "\n\nRESULT: " + result.Status + "\nJournal: " + result.JournalPath;
        }
        catch (OperationCanceledException) { Report("Repair stopped before execution."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        { RepairPreview.Text += "\n\nNot completed: " + error.Message; Report(error.Message); }
        finally
        {
            readCancellation = null;
            repairRunning = false;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            RepairChoice.IsEnabled = true;
            RepairConsent.IsChecked = false;
            ModeLabel.Text = "REPAIR SESSION ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
