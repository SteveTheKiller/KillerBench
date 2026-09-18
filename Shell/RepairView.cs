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
        var workflowKind = kind == RepairKind.SystemFiles ? WorkflowKind.SystemFiles : WorkflowKind.ComponentCleanup;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        RepairChoice.IsEnabled = false;
        ModeLabel.Text = "REPAIR / THIS PC";
        BeginWorkflowOutput(workflowKind);
        AppendWorkflowOutput("Repair requested for this PC. Stop waits for the current command; it does not kill servicing.");
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs");
            var progress = new Progress<string>(message => { if (!string.IsNullOrWhiteSpace(message)) AppendWorkflowOutput(message); });
            var execution = await Task.Run(() => ServicingRepairExecution.RunAsync(kind, folder,
                new WindowsWorkflowHost(), progress, stop.Token));
            if (execution.Repair is null)
            {
                RepairPreview.Text = Repairs.Preview(kind) + "\n\nFRESH EVIDENCE: " + execution.Eligibility.Detail +
                    "\n\nRESULT: No repair was run.";
                AppendWorkflowOutput(RepairPreview.Text);
                RegisterWorkflowResult(new WorkflowResult(workflowKind, Environment.MachineName, DateTimeOffset.UtcNow,
                    "Repair not authorized",
                    [new WorkflowCheck("Fresh servicing evidence", execution.Eligibility.State, execution.Eligibility.Detail),
                     new WorkflowCheck("Change boundary", CheckState.Information,
                        "No servicing command was run because fresh evidence did not authorize this repair.")], [],
                    "No Windows servicing repair was requested."), "Windows servicing repair");
                SetScanLight("attention", execution.Eligibility.Detail);
                return;
            }
            var result = execution.Repair;
            RepairPreview.Text = Repairs.Preview(kind) + "\n\nFRESH EVIDENCE: " + execution.Eligibility.Detail +
                "\n\nRESULT: " + result.Status + "\nJournal: " + result.JournalPath;
            string output = RepairPreview.Text + "\n\n" + string.Join("\n", result.Steps.Select(s => s.Step.Tool + "\n" + s.Result.Output + "\n" + s.Result.Error));
            bool verified = Repairs.IsVerified(result);
            RegisterGuardedActionReport(workflowKind, "Windows servicing repair", Repairs.Plan(kind).Title,
                output, result.Status, result.JournalPath, verified,
                "Review Windows health again before selecting another servicing action.", result.RequiresRestart,
                "Restart Windows, then rerun the selected servicing check.");
            SetScanLight(verified ? "ready" : "attention", result.Status);
        }
        catch (OperationCanceledException)
        {
            const string detail = "Repair stopped before completion. No additional servicing command was requested.";
            AppendWorkflowOutput(detail);
            RegisterWorkflowResult(new WorkflowResult(workflowKind, Environment.MachineName, DateTimeOffset.UtcNow,
                "Repair requires review", [new WorkflowCheck("Windows servicing repair", CheckState.Unknown, detail)], [],
                "Servicing repair did not reach a verified outcome."), "Windows servicing repair");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.Security.SecurityException)
        {
            string detail = "Repair not completed: " + error.Message;
            RepairPreview.Text += "\n\n" + detail;
            AppendWorkflowOutput(detail);
            RegisterWorkflowResult(new WorkflowResult(workflowKind, Environment.MachineName, DateTimeOffset.UtcNow,
                "Repair requires review", [new WorkflowCheck("Windows servicing repair", CheckState.Unknown, detail)], [],
                "Servicing repair did not reach a verified outcome."), "Windows servicing repair");
        }
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
