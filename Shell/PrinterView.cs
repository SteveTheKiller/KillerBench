using System.IO;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private void OnGeneralPrinting(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        PrinterQueues.SelectedIndex = -1;
        PrinterMakeDefault.IsChecked = false;
        PrintJobChoice.SelectedIndex = -1;
        PrintJobCancelConsent.IsChecked = false;
        MainAutoRepair.IsChecked = false;
        Report("General printing checks selected. Automatic repair is off.");
    }

    private void OnPrinterSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        PrinterMakeDefault?.IsChecked = false;
        OnWorkflowOptionsChanged(sender, e);
    }

    private void OnPrinterDefaultChanged(object sender, RoutedEventArgs e)
    {
        if (PrinterMakeDefault?.IsChecked == true && MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
    }

    private void OnPrintJobSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PrintJobCancelConsent is not null)
        {
            PrintJobCancelConsent.IsChecked = false;
            PrintJobCancelConsent.IsEnabled = PrintJobChoice.SelectedItem is System.Windows.Controls.ComboBoxItem
                { Tag: PrintJobSnapshot };
        }
        OnWorkflowOptionsChanged(sender, e);
    }

    private void OnPrintJobCancelChanged(object sender, RoutedEventArgs e)
    {
        if (PrintJobCancelConsent?.IsChecked == true && MainAutoRepair is not null)
            MainAutoRepair.IsChecked = false;
    }

    private async void OnDiscoverPrinters(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        string? selectedQueue = PrinterQueues.SelectedItem as string;
        try
        {
            Report("Discovering local printer queues...");
            var snapshot = await Task.Run(() => PrinterDiagnostics.DiscoverAsync(stop.Token));
            if (snapshot is not null)
            {
                if (selectedQueue is not null && !snapshot.Queues.ContainsKey(selectedQueue))
                {
                    Report("The selected printer is no longer available. Selection retained to prevent switching scope; GO will recheck it.");
                    return;
                }
                PrinterQueues.ItemsSource = snapshot.Queues.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
                if (selectedQueue is not null)
                    PrinterQueues.SelectedItem = snapshot.Queues.Keys.Single(name => string.Equals(name, selectedQueue, StringComparison.OrdinalIgnoreCase));
            }
            Report(snapshot is null ? "Printer discovery unavailable. No settings changed." : $"{snapshot.Queues.Count} local queues found. Select one for an offline-setting check.");
        }
        catch (OperationCanceledException) { Report("Printer discovery stopped. No settings changed."); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Report("Printer discovery unavailable: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnDiscoverPrintJobs(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            Report("Finding print jobs currently reported as Error or Blocked...");
            BeginWorkflowOutput(WorkflowKind.Printing);
            ToolResult evidence = await Task.Run(() => PrintJobDiagnostics.CaptureAsync(stop.Token));
            PrintJobInventory? inventory = PrintJobDiagnostics.ReadInventory(evidence);
            if (inventory is not { Complete: true })
            {
                const string incompleteStatus = "Complete print-job evidence is unavailable. No cancellation is available.";
                RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Printing, Environment.MachineName, DateTimeOffset.UtcNow,
                    incompleteStatus,
                    [new WorkflowCheck("Print-job evidence", CheckState.Unknown,
                        "The print-job list was incomplete or could not be read. No cancellation candidate was made available.", Evidence: evidence),
                     new WorkflowCheck("Change boundary", CheckState.Information,
                        "No print job was canceled, restarted, paused, resumed, or otherwise changed.")], [],
                    "Read-only print-job discovery. No cancellation journal was written."), "Blocked print job review");
                Report(incompleteStatus);
                return;
            }
            var items = inventory.Jobs.Where(PrintJobCancellation.IsCandidate)
                .OrderBy(job => job.QueueName, StringComparer.OrdinalIgnoreCase).ThenBy(job => job.JobId)
                .Select(job => new System.Windows.Controls.ComboBoxItem
                {
                    Content = job.QueueName + " | Job " + job.JobId + " | Status " + job.StatusMask,
                    Tag = job
                }).ToArray();
            PrintJobChoice.ItemsSource = items;
            PrintJobChoice.SelectedIndex = -1;
            PrintJobCancelConsent.IsChecked = false;
            PrintJobCancelConsent.IsEnabled = false;
            string status = items.Length == 0
                ? "No visible print job is currently reported as Error or Blocked. No job changed."
                : items.Length + " cancellation candidate(s) found. Select one, confirm the exact cancellation, then press GO.";
            AppendWorkflowOutput("PRINT JOB REVIEW\n" + status + "\nCandidates: " +
                (items.Length == 0 ? "None" : string.Join("; ", items.Select(item => item.Content?.ToString()))));
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Printing, Environment.MachineName, DateTimeOffset.UtcNow,
                status,
                [new WorkflowCheck("Print-job evidence", CheckState.Clear,
                    "A complete bounded snapshot of current print jobs was collected.", Evidence: evidence),
                 new WorkflowCheck("Cancellation candidates", items.Length == 0 ? CheckState.Clear : CheckState.Attention,
                    items.Length == 0 ? "No visible Error or Blocked print job was reported in this snapshot." : items.Length + " exact Error or Blocked print job candidate" + (items.Length == 1 ? " was" : "s were") + " retained for review. Select one in the Printing controls before any cancellation can be considered."),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "No print job was canceled, restarted, paused, resumed, or otherwise changed.")], [],
                "Read-only print-job discovery. No cancellation journal was written."), "Blocked print job review");
            Report(status);
        }
        catch (OperationCanceledException) { Report("Print-job discovery stopped. No job changed."); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException)
        { Report("Print-job discovery unavailable: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnRecoverPrintJobCancellation(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "PrintJobs");
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            BeginWorkflowOutput(WorkflowKind.Printing);
            var result = await Task.Run(() => PrintJobCancellation.ReconcileAsync(folder,
                new WindowsPrintJobCancellationHost(), new Progress<string>(AppendWorkflowOutput), stop.Token));
            string output = "PRINT JOB RECOVERY / " + result.Outcome.ToString().ToUpperInvariant() +
                "\n" + result.Detail + "\nRecord: " + (result.RecordPath ?? "No operation record created.") +
                "\n\nCancellation was not repeated.";
            bool verified = result.Outcome is PrintJobCancellationOutcome.Verified or
                PrintJobCancellationOutcome.NotApplied or PrintJobCancellationOutcome.NoPending;
            RegisterGuardedActionReport(WorkflowKind.Printing, "Print job cancellation",
                "Print job cancellation recovery / " + result.Outcome, output, result.Detail,
                result.RecordPath ?? "No operation record created.", verified,
                "Confirm the queue is clear or resubmit the document before making another print change.");
            SetScanLight(verified ? "ready" : "incomplete", result.Detail);
        }
        catch (OperationCanceledException) { Report("Print-job recovery stopped. Cancellation was not repeated."); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException)
        { Report("Print-job recovery unavailable: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }
}
