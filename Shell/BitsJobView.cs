using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private BitsInventoryResult? bitsInventory;
    private void OnBitsSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (BitsRetryConsent is not null) BitsRetryConsent.IsChecked = false;
        RefreshBitsRetryAvailability();
    }
    private void RefreshBitsRetryAvailability()
    {
        if (BitsRetryButton is null || BitsRetryConsent is null || BitsRetryAvailability is null) return;
        bool eligible = false;
        string detail = "Collect and select a running-account job to review retry availability.";
        if (bitsInventory is { } inventory && BitsSelection.SelectedItem is Guid id)
        {
            eligible = BitsRetry.IsEligible(inventory, id, new WindowsBitsRetryHost().OwnerAccount);
            detail = eligible ? "Snapshot permits a retry request. Live checks and prior-attempt records may still block it." :
                "Retry unavailable: requires complete running-account evidence and an errored job owned by this account. Suspended jobs are preserved.";
        }
        BitsRetryConsent.IsEnabled = eligible;
        BitsRetryButton.IsEnabled = eligible;
        BitsRetryAvailability.Text = detail;
    }
    private void SetBitsInventory(BitsInventoryResult? inventory)
    {
        bitsInventory = inventory;
        BitsSelection.ItemsSource = inventory?.Jobs.Select(job => job.Id).ToArray();
        BitsSelection.SelectedIndex = -1;
        RefreshBitsRetryAvailability();
    }
    private void OnBitsScopeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (BitsSelection is not null) SetBitsInventory(null);
    }
    private void OnReviewBitsJob(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (bitsInventory is null || BitsSelection.SelectedItem is not Guid id)
        { Report("Run Background transfers, then select an exact job to review."); return; }
        var review = BitsJobReview.Review(bitsInventory, id);
        var selected = review.Selected;
        string text = "BITS JOB REVIEW / RETAINED SNAPSHOT\n" + id + "\nScope: " + review.Scope +
            "\nName: " + selected?.Name + "\nOwner: " + selected?.Owner + "\nState: " + selected?.State +
            "\nError: " + selected?.Error + "\n\n" + string.Join("\n", review.Notes);
        BeginWorkflowOutput(WorkflowKind.BackgroundTransfers);
        AppendWorkflowOutput(text);
        var checks = new[]
        {
            new WorkflowCheck("Selected transfer", CheckState.Information,
                "Name: " + (selected?.Name ?? "Not reported") + ". Owner: " + (selected?.Owner ?? "Not reported") + ". State: " + (selected?.State ?? "Not reported") + "."),
            new WorkflowCheck("Snapshot review", selected is null ? CheckState.Unknown : CheckState.Information,
                selected is null ? "The selected transfer was not found in the retained snapshot. Refresh the transfer list before relying on it." : string.Join(" ", review.Notes)),
            new WorkflowCheck("Change boundary", CheckState.Information,
                "This is a retained snapshot review only. No transfer was resumed, completed, canceled, deleted, or reassigned.")
        };
        const string status = "Transfer snapshot reviewed. No job changes performed.";
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.BackgroundTransfers, Environment.MachineName, DateTimeOffset.UtcNow,
            status, checks, [], "Read-only transfer snapshot review. No journal was written."), "Background transfer review");
        Report(status);
    }

    private async void OnObserveBitsJob(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (BitsSelection.SelectedItem is not Guid id)
        { Report("Run Background transfers, then select an exact job to observe."); return; }
        bool allUsers = BitsScopeChoice.SelectedIndex == 1;
        BitsRetryConsent.IsChecked = false;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(95));
        readCancellation = stop;
        SetCaptureBusy(true);
        BitsSettings.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.BackgroundTransfers);
        ModeLabel.Text = "SELECTED TRANSFER / OBSERVATION";
        try
        {
            int collected = 0;
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => BitsObservation.ObserveAsync(id, async token =>
            {
                ((IProgress<string>)progress).Report($"Reading transfer snapshot {++collected} of 2. No job changes.");
                var inventory = await BitsDiagnostics.ReadAsync(allUsers, token);
                if (collected == 1) ((IProgress<string>)progress).Report("First snapshot captured. Waiting five seconds before the second observation.");
                return inventory;
            }, TimeSpan.FromSeconds(5), stop.Token));
            stop.Token.ThrowIfCancellationRequested();
            workflowOutputActive = false;
            var comparison = result.Comparison;
            RegisterSource(new TextEvidence("Transfer observation " + DateTime.Now.ToString("HHmmss"), new[]
            {
                "BITS / TWO-SNAPSHOT OBSERVATION", "Selected transfer: " + id,
                "First collected: " + result.FirstCollected.ToString("O"), "Second collected: " + result.SecondCollected.ToString("O"),
                "Bytes advanced: " + (comparison.BytesAdvanced?.ToString() ?? "Unknown"), comparison.Detail,
                "Before state: " + comparison.Before.Selected?.State, "After state: " + comparison.After.Selected?.State,
                "No job changed. Existing retry guards remain in place.", "",
                "FIRST EVIDENCE", result.BeforeEvidence.Output, result.BeforeEvidence.Error,
                "SECOND EVIDENCE", result.AfterEvidence.Output, result.AfterEvidence.Error
            }, result.BeforeEvidence.Truncated || result.AfterEvidence.Truncated));
            bool complete = !result.BeforeEvidence.Truncated && !result.AfterEvidence.Truncated &&
                string.IsNullOrWhiteSpace(result.BeforeEvidence.Error) && string.IsNullOrWhiteSpace(result.AfterEvidence.Error);
            var checks = new[]
            {
                new WorkflowCheck("Selected transfer", CheckState.Information, "The same transfer was observed twice: " + id + "."),
                new WorkflowCheck("Observation result", CheckState.Information, comparison.Detail),
                new WorkflowCheck("Observation coverage", complete ? CheckState.Clear : CheckState.Unknown,
                    complete ? "Both retained snapshots completed without reported truncation or collection errors." : "One or both retained snapshots were incomplete. Review Console evidence before drawing a conclusion."),
                new WorkflowCheck("Change boundary", CheckState.Information,
                    "No transfer was changed. Retry remains a separate guarded action that requires fresh eligibility and explicit consent.")
            };
            const string status = "Transfer observed twice. No job changed. Review the result and detailed evidence before considering a retry.";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.BackgroundTransfers, Environment.MachineName, DateTimeOffset.UtcNow,
                status, checks, [], "Read-only two-snapshot transfer observation. No retry journal was written."), "Background transfer observation");
            Report(status);
            SetScanLight("incomplete", "Observation only. Transfer completion and machine health remain unverified.");
        }
        catch (Exception error) when (error is OperationCanceledException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException)
        {
            string detail = error is OperationCanceledException ? "Transfer observation stopped or timed out." : "Transfer observation unavailable: " + error.Message;
            AppendWorkflowOutput(detail);
            Report(detail);
            SetScanLight("incomplete", detail);
        }
        finally
        {
            workflowOutputActive = false;
            readCancellation = null;
            BitsSettings.IsEnabled = true;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            ModeLabel.Text = "TRANSFER OBSERVATION ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private async void OnRetryBitsJob(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (!BitsRetryButton.IsEnabled || BitsRetryConsent.IsChecked != true || bitsInventory is not { } inventory || BitsSelection.SelectedItem is not Guid id)
        { Report("Select a transfer, review it, then check the retry option."); return; }
        BitsRetryConsent.IsChecked = false;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        BitsSettings.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.BackgroundTransfers);
        ModeLabel.Text = "SELECTED TRANSFER / RETRY";
        string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "BitsRetry");
        try
        {
            var progress = new Progress<string>(AppendWorkflowOutput);
            string pending = System.IO.Path.Combine(folder, id.ToString("N") + ".pending.json");
            if (System.IO.File.Exists(pending))
            {
                ModeLabel.Text = "SELECTED TRANSFER / RECOVERY";
                var recovery = await Task.Run(() => BitsRetryReconciliation.RunAsync(folder, id,
                    new WindowsBitsRetryHost(), progress: progress, token: stop.Token));
                RegisterGuardedActionReport(WorkflowKind.BackgroundTransfers, "Background transfer retry",
                    "Background transfer retry recovery / " + recovery.Outcome,
                    string.Join("\n", new[] { "BITS RETRY RECOVERY / " + recovery.Outcome, id.ToString(), recovery.Detail,
                        "Record: " + (recovery.ArchivePath ?? recovery.PendingPath), "No retry was requested during recovery." }),
                    recovery.Detail, recovery.ArchivePath ?? recovery.PendingPath,
                    recovery.Outcome == BitsRetryReconciliationOutcome.TransferReported,
                    "Review the transfer's current state and owning application before another retry.");
                SetBitsInventory(null);
                SetScanLight(recovery.Outcome == BitsRetryReconciliationOutcome.TransferReported ? "ready" : "incomplete", recovery.Detail);
                return;
            }
            var result = await Task.Run(() => BitsRetry.RunAsync(inventory, id, new WindowsBitsRetryHost(), new BitsRetryJournal(folder), stop.Token, progress));
            RegisterGuardedActionReport(WorkflowKind.BackgroundTransfers, "Background transfer retry",
                "Background transfer retry / " + result.Outcome,
                string.Join("\n", new[] { "BITS RETRY / " + result.Outcome, id.ToString(), result.Detail,
                    "Journal: " + folder, "Application completion is unverified. Existing attempts remain guarded." }),
                result.Detail, folder, result.Outcome == BitsRetryOutcome.TransferReported,
                "Review the transfer's current state and owning application before another retry.");
            SetBitsInventory(null);
            SetScanLight(result.Outcome == BitsRetryOutcome.TransferReported ? "ready" : "incomplete", result.Detail);
        }
        catch (Exception error) when (error is OperationCanceledException or System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            string detail = "Retry outcome unavailable: " + error.Message;
            RegisterGuardedActionReport(WorkflowKind.BackgroundTransfers, "Background transfer retry",
                "Background transfer retry / outcome unverified",
                string.Join("\n", new[] { "BITS RETRY / OUTCOME UNVERIFIED", "Selected transfer: " + id, detail,
                    "Journal: " + folder, "A request may still be active. Review the saved attempt and owning application before another retry. Application completion remains unverified." }),
                detail, folder, false, "Review the saved attempt and owning application before another retry.");
            SetScanLight("incomplete", "Retry outcome unverified. Review retained evidence.");
        }
        finally
        {
            workflowOutputActive = false;
            readCancellation = null;
            SetBitsInventory(null);
            repairRunning = false;
            BitsSettings.IsEnabled = true;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            ModeLabel.Text = "TRANSFER RETRY ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
