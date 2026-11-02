using System.IO;
using System.Windows;
using KillerBench.Engine;
using L = KillerBench.Controls.LocalizedText;

namespace KillerBench.Shell;

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
        Report(L.Get("Str_Tkp_GeneralPrinting"));
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
            Report(L.Format("Str_Ui_Reading", L.Get("Str_Print_FindPrinters")));
            var snapshot = await Task.Run(() => PrinterDiagnostics.DiscoverAsync(token: stop.Token));
            if (snapshot is not null)
            {
                if (selectedQueue is not null && !snapshot.Queues.ContainsKey(selectedQueue))
                {
                    Report(L.Get("Str_Tkp_PrinterMissing"));
                    return;
                }
                PrinterQueues.ItemsSource = snapshot.Queues.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
                if (selectedQueue is not null)
                    PrinterQueues.SelectedItem = snapshot.Queues.Keys.Single(name => string.Equals(name, selectedQueue, StringComparison.OrdinalIgnoreCase));
            }
            Report(snapshot is null ? L.Get("Str_Tkp_PrinterUnavailable") : L.Format("Str_Tkp_PrintersFound", snapshot.Queues.Count));
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Error", L.Format("Str_Ui_Stopped", L.Get("Str_Print_FindPrinters")), L.Get("Str_Tkp_PrinterUnavailable"))); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Print_FindPrinters"), error.Message)); }
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
            Report(L.Format("Str_Ui_Reading", L.Get("Str_Print_FindBlockedJobs")));
            BeginWorkflowOutput(WorkflowKind.Printing);
            ToolResult evidence = await Task.Run(() => PrintJobDiagnostics.CaptureAsync(token: stop.Token));
            PrintJobInventory? inventory = PrintJobDiagnostics.ReadInventory(evidence);
            if (inventory is not { Complete: true })
            {
                string incompleteStatus = L.Get("Str_Tkp_JobsIncomplete");
                RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Printing, Environment.MachineName, DateTimeOffset.UtcNow,
                    incompleteStatus,
                    [new WorkflowCheck(L.Get("Str_Print_FindBlockedJobs"), CheckState.Unknown,
                        L.Get("Str_Tkp_JobsIncomplete"), Evidence: evidence),
                     new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Information,
                        L.Get("Str_Tkp_PrintNoChange"))], [],
                    L.Get("Str_Tkp_PrintNoJournal")), L.Get("Str_Print_FindBlockedJobs"));
                Report(incompleteStatus);
                return;
            }
            var items = inventory.Jobs.Where(PrintJobCancellation.IsCandidate)
                .OrderBy(job => job.QueueName, StringComparer.OrdinalIgnoreCase).ThenBy(job => job.JobId)
                .Select(job => new System.Windows.Controls.ComboBoxItem
                {
                    Content = L.Format("Str_Tkp_JobItem", job.QueueName, job.JobId, job.StatusMask),
                    Tag = job
                }).ToArray();
            PrintJobChoice.ItemsSource = items;
            PrintJobChoice.SelectedIndex = -1;
            PrintJobCancelConsent.IsChecked = false;
            PrintJobCancelConsent.IsEnabled = false;
            string rawStatus = items.Length == 0
                ? "No visible print job is currently reported as Error or Blocked. No job changed."
                : items.Length + " cancellation candidate(s) found. Select one, confirm the exact cancellation, then select Run diagnosis.";
            AppendWorkflowOutput("PRINT JOB REVIEW\n" + rawStatus + "\nCandidates: " +
                (items.Length == 0 ? "None" : string.Join("; ", items.Select(item =>
                {
                    var job = (PrintJobSnapshot)item.Tag;
                    return job.QueueName + " | Job " + job.JobId + " | Status " + job.StatusMask;
                }))));
            string status = items.Length == 0 ? L.Get("Str_Tkp_JobsNone") : L.Format("Str_Tkp_JobsCandidates", items.Length);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Printing, Environment.MachineName, DateTimeOffset.UtcNow,
                status,
                [new WorkflowCheck(L.Get("Str_Print_FindBlockedJobs"), CheckState.Clear,
                    L.Get("Str_Tkp_JobsSnapshot"), Evidence: evidence),
                 new WorkflowCheck(L.Get("Str_Print_CancelSelected"), items.Length == 0 ? CheckState.Clear : CheckState.Attention,
                    status),
                 new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Information,
                    L.Get("Str_Tkp_PrintNoChange"))], [],
                L.Get("Str_Tkp_PrintNoJournal")), L.Get("Str_Print_FindBlockedJobs"));
            Report(status);
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Error", L.Format("Str_Ui_Stopped", L.Get("Str_Print_FindBlockedJobs")), L.Get("Str_Tkp_PrintNoChange"))); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Print_FindBlockedJobs"), error.Message)); }
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
            RegisterGuardedActionReport(WorkflowKind.Printing, L.Get("Str_Print_CancelSelected"),
                L.Format("Str_Ui_Error", L.Get("Str_Print_CheckInterrupted"), result.Outcome), output, result.Detail,
                result.RecordPath ?? "No operation record created.", verified,
                L.Get("Str_Tkp_PrintNext"));
            SetScanLight(verified ? "ready" : "incomplete", L.Format("Str_Ui_Error", L.Get("Str_Print_CheckInterrupted"), result.Detail));
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Error", L.Format("Str_Ui_Stopped", L.Get("Str_Print_CheckInterrupted")), L.Get("Str_Tkp_CancellationNotRepeated"))); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Print_CheckInterrupted"), error.Message)); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }
}
