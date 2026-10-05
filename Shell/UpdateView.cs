using KillerBench.Engine;
using System.Windows;
using System.IO;

namespace KillerBench;

public partial class MainWindow
{
    private UpdateSearchResult? availableUpdateResult;
    private ThirdPartyUpdateResult? thirdPartyUpdateResult;
    private UpdateSearchResult? classificationSource;
    private bool updatingUpdateFilters;
    private sealed record UpdateClassificationChoice(Guid? Id, string Name);

    private static string UpdateStoreFolder() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "UpdateDataStoreReset");

    private static string ThirdPartyUpdateFolder() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "ThirdPartyUpdates");

    private void SetThirdPartyUpdateResult(ThirdPartyUpdateResult? result)
    {
        thirdPartyUpdateResult = result;
        ThirdPartyUpdateList.UnselectAll();
        ThirdPartyUpdateList.ItemsSource = result?.Candidates;
        ThirdPartyUpdateVersion.ItemsSource = null;
        ThirdPartyUpdateAgreements.IsChecked = false;
        ThirdPartyUpdateCount.Text = result is null ? "Select Third-party apps, then press GO." :
            result.ProviderAvailable && result.Complete
                ? $"{result.Candidates.Count} exact update candidates from {result.Scanned} installed packages."
                : "Third-party discovery is incomplete. Review its evidence and run GO again before installation.";
        RefreshThirdPartyUpdateActions();
    }

    private void OnThirdPartyUpdateSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, ThirdPartyUpdateList))
        {
            ThirdPartyUpdateVersion.ItemsSource = (ThirdPartyUpdateList.SelectedItem as ThirdPartyUpdateCandidate)?.AvailableVersions;
            ThirdPartyUpdateVersion.SelectedIndex = ThirdPartyUpdateVersion.Items.Count > 0 ? 0 : -1;
            ThirdPartyUpdateAgreements.IsChecked = false;
        }
        RefreshThirdPartyUpdateActions();
    }

    private bool TrySelectedThirdPartyUpdate(out ThirdPartyUpdateSelection? selection)
    {
        selection = null;
        if (ThirdPartyUpdateList.SelectedItem is not ThirdPartyUpdateCandidate candidate ||
            ThirdPartyUpdateVersion.SelectedItem is not string version) return false;
        selection = new(candidate.Id, candidate.Source, version);
        return true;
    }

    private void RefreshThirdPartyUpdateActions()
    {
        if (ThirdPartyUpdatePreview is null || ThirdPartyUpdateInstall is null || ThirdPartyUpdateStatus is null) return;
        ThirdPartyUpdatePreview.IsEnabled = false;
        ThirdPartyUpdateInstall.IsEnabled = false;
        ThirdPartyUpdateStatus.Text = "One exact package, source and version per operation. No automatic restart.";
        if (thirdPartyUpdateResult is null || !TrySelectedThirdPartyUpdate(out var selection) || selection is null) return;
        try
        {
            var plan = ThirdPartyUpdatePlanning.Create(thirdPartyUpdateResult, selection);
            ThirdPartyUpdatePreview.IsEnabled = true;
            ThirdPartyUpdateInstall.IsEnabled = plan.Eligible && ThirdPartyUpdateAgreements.IsChecked == true;
            ThirdPartyUpdateStatus.Text = plan.Eligible
                ? "Ready to preview. Installation requires explicit agreement acceptance and repeats discovery before changing the app."
                : string.Join(" ", plan.Blockers.Take(2));
        }
        catch (ArgumentException error) { ThirdPartyUpdateStatus.Text = error.Message; }
    }

    private void RefreshUpdateStoreRecovery()
    {
        if (UpdateStoreRecoveryPanel is null || UpdateStoreRecoveryStatus is null || UpdateStoreRecoveryConsent is null ||
            UpdateStoreResetButton is null || UpdateStoreReconcileButton is null) return;
        bool pending = File.Exists(Path.Combine(UpdateStoreFolder(), "update-store-reset.pending.json"));
        bool eligible = availableUpdateResult is not null && UpdateDataStoreReset.IsEligible(availableUpdateResult, out _);
        UpdateStoreRecoveryPanel.Visibility = eligible || pending ? Visibility.Visible : Visibility.Collapsed;
        UpdateStoreRecoveryConsent.IsEnabled = eligible && !pending;
        UpdateStoreResetButton.IsEnabled = eligible && !pending;
        UpdateStoreReconcileButton.IsEnabled = pending;
        UpdateStoreResetButton.Content = UpdateStoreRecoveryConsent.IsChecked == true ? "Run recovery" : "Preview recovery";
        UpdateStoreRecoveryStatus.Text = pending
            ? "A prepared recovery needs verification. Check it before another database recovery."
            : eligible
                ? "The current search found " + WindowsUpdateErrorCatalog.Find(availableUpdateResult!.FailureHResult!.Value)?.Symbol +
                    ". Preview the retained-store recovery, then select the confirmation only if you want to run it."
                : "Run Available updates to establish current recovery eligibility.";
    }

    private void OnUpdateStoreRecoveryConsentChanged(object sender, RoutedEventArgs e) => RefreshUpdateStoreRecovery();

    private void OnUpdateFilterChanged(object sender, RoutedEventArgs e) => ApplyUpdateFilter();

    private void OnUpdateSelectionChanged(object sender, RoutedEventArgs e) => RefreshUpdateActions();

    private void RefreshUpdateActions()
    {
        if (UpdatePreviewButton is null || UpdateInstallButton is null || UpdateSelectionStatus is null || UpdateAllowRestart is null || UpdateAllowFeatureUpgrade is null) return;
        UpdatePreviewButton.IsEnabled = availableUpdateResult is not null && AvailableUpdateList.SelectedItems.Count > 0;
        UpdateInstallButton.IsEnabled = false;
        UpdateSelectionStatus.Text = "Select updates to review their installation requirements.";
        if (!UpdatePreviewButton.IsEnabled) return;
        try
        {
            var selections = AvailableUpdateList.SelectedItems.Cast<AvailableUpdate>().Select(update => new UpdateSelection(Guid.Parse(update.Id), update.Revision));
            var plan = UpdatePlanning.Create(availableUpdateResult!, selections,
                new(UpdateAllowRestart.IsChecked == true, UpdateAllowFeatureUpgrade.IsChecked == true));
            UpdateInstallButton.IsEnabled = plan.Eligible;
            UpdateSelectionStatus.Text = plan.Eligible ? $"{plan.Updates.Count} selected. Planning passed; installation rechecks prerequisites." :
                string.Join(" ", plan.Updates.SelectMany(update => update.Blockers).Distinct().Take(2));
        }
        catch (Exception error) when (error is ArgumentException or FormatException)
        { UpdateSelectionStatus.Text = error.Message; }
    }

    private void ApplyUpdateFilter()
    {
        if (updatingUpdateFilters || AvailableUpdateList is null || UpdateTitleFilter is null || UpdateDownloadedOnly is null || UpdateFilterCount is null || UpdateClassificationFilter is null || UpdateProductFilter is null) return;
        if (!ReferenceEquals(classificationSource, availableUpdateResult) || UpdateClassificationFilter.ItemsSource is null || UpdateProductFilter.ItemsSource is null)
        {
            updatingUpdateFilters = true;
            try
            {
                classificationSource = availableUpdateResult;
                UpdateClassificationFilter.ItemsSource = new[] { new UpdateClassificationChoice(null, "All classifications") }
                    .Concat((availableUpdateResult?.GetClassifications() ?? []).Select(category => new UpdateClassificationChoice(Guid.Parse(category.Id), category.Name))).ToArray();
                UpdateClassificationFilter.SelectedIndex = 0;
                UpdateProductFilter.ItemsSource = new[] { new UpdateClassificationChoice(null, "All products") }
                    .Concat((availableUpdateResult?.GetProducts() ?? []).Select(product => new UpdateClassificationChoice(Guid.Parse(product.Id), product.Name))).ToArray();
                UpdateProductFilter.SelectedIndex = 0;
            }
            finally { updatingUpdateFilters = false; }
        }
        AvailableUpdateList.UnselectAll();
        try
        {
            var visible = availableUpdateResult?.Filter(UpdateTitleFilter.Text, UpdateDownloadedOnly.IsChecked == true,
                (UpdateClassificationFilter.SelectedItem as UpdateClassificationChoice)?.Id,
                (UpdateProductFilter.SelectedItem as UpdateClassificationChoice)?.Id) ?? [];
            AvailableUpdateList.ItemsSource = visible;
            UpdateFilterCount.Text = availableUpdateResult is null ? "Run discovery to see available updates." :
                $"Showing {visible.Count} of {availableUpdateResult.Updates.Count}. " +
                (availableUpdateResult.Partial ? "Discovery is incomplete. Search again before planning installation." :
                    visible.Count == 0 ? "No updates match these filters. This does not verify that Windows is fully updated." :
                    "Select updates after filtering.");
        }
        catch (ArgumentException error)
        {
            AvailableUpdateList.ItemsSource = null;
            UpdateFilterCount.Text = error.Message;
        }
        RefreshUpdateActions();
        RefreshUpdateStoreRecovery();
    }

    private async void OnUpdateStoreReset(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (availableUpdateResult is null)
        { Report("Run Available updates again before recovery."); return; }
        if (!UpdateDataStoreReset.IsEligible(availableUpdateResult, out string reason))
        { Report(reason); return; }
        bool execute = UpdateStoreRecoveryConsent.IsChecked == true;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = execute;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
        ModeLabel.Text = execute ? "WINDOWS UPDATE / DATABASE RECOVERY" : "WINDOWS UPDATE / RECOVERY PREVIEW";
        try
        {
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => UpdateDataStoreReset.RunAsync(availableUpdateResult, execute,
                UpdateStoreFolder(), new WindowsUpdateDataStoreResetHost(), progress, stop.Token));
            string output = "WINDOWS UPDATE DATABASE RECOVERY / " + result.Outcome + "\n" + result.Detail +
                "\nRetained store: " + (result.RetainedStorePath ?? "No store retained in preview or blocked result.") +
                "\nJournal: " + (result.JournalPath ?? "No execution journal created.") +
                "\nNo update was installed and Windows was not restarted.";
            if (execute)
                RegisterGuardedActionReport(WorkflowKind.AvailableUpdates, "Windows Update database recovery", "Windows Update database recovery",
                    output, result.Detail, result.JournalPath ?? "No execution journal created.", result.Outcome == UpdateDataStoreResetOutcome.Verified,
                    "Review the completed recovery and run Available updates again before considering any installation.");
            else
            {
                AppendWorkflowOutput(output);
                CheckState state = result.Outcome == UpdateDataStoreResetOutcome.Preview ? CheckState.Information :
                    result.Outcome == UpdateDataStoreResetOutcome.Blocked ? CheckState.Attention : CheckState.Unknown;
                RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                    result.Detail,
                    [new WorkflowCheck("Database recovery preview", state, result.Detail),
                     new WorkflowCheck("Change boundary", CheckState.Information,
                        "Preview did not stop services, retain the update store, install updates or restart Windows.")], [],
                    "Read-only Windows Update database recovery preview. No execution journal was written."), "Windows Update database recovery");
            }
            Report(result.Detail);
            SetScanLight(result.Outcome is UpdateDataStoreResetOutcome.Verified or UpdateDataStoreResetOutcome.Preview
                ? "ready" : result.Outcome == UpdateDataStoreResetOutcome.Blocked ? "attention" : "incomplete", result.Detail);
            if (execute) availableUpdateResult = result.After ?? result.Before;
        }
        catch (OperationCanceledException)
        {
            AppendWorkflowOutput("Recovery stopped. Check the pending operation before another attempt.");
            SetScanLight("incomplete", "Windows Update database recovery was not verified.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            AppendWorkflowOutput("Recovery unavailable: " + error.Message);
            SetScanLight("incomplete", "Windows Update database recovery was not verified.");
        }
        finally
        {
            readCancellation = null;
            repairRunning = false;
            workflowOutputActive = false;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            UpdateStoreRecoveryConsent.IsChecked = false;
            ApplyUpdateFilter();
            ModeLabel.Text = "UPDATE DATABASE RECOVERY ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private async void OnRecoverUpdateStore(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
        ModeLabel.Text = "WINDOWS UPDATE / CHECKING RECOVERY";
        try
        {
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => UpdateDataStoreResetReconciliation.RunAsync(UpdateStoreFolder(),
                new WindowsUpdateDataStoreResetHost(), progress, stop.Token));
            string output = "WINDOWS UPDATE DATABASE RECONCILIATION / " + result.Outcome + "\n" + result.Detail +
                "\nNo service or directory change was repeated.";
            AppendWorkflowOutput(output);
            CheckState state = result.Outcome is UpdateDataStoreReconciliationOutcome.Verified or
                UpdateDataStoreReconciliationOutcome.NotChanged or UpdateDataStoreReconciliationOutcome.NoPendingRecord
                ? CheckState.Clear : CheckState.Unknown;
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                result.Detail,
                [new WorkflowCheck("Database recovery reconciliation", state, result.Detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "This reconciliation did not stop services, retain a directory, install updates or restart Windows.")], [],
                "Read-only Windows Update database recovery reconciliation. No service or directory change was repeated."),
                "Windows Update database reconciliation");
            Report(result.Detail);
            SetScanLight(result.Outcome is UpdateDataStoreReconciliationOutcome.Verified or
                UpdateDataStoreReconciliationOutcome.NotChanged or UpdateDataStoreReconciliationOutcome.NoPendingRecord
                ? "ready" : "incomplete", result.Detail);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or OperationCanceledException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            AppendWorkflowOutput("Recovery check unavailable: " + error.Message);
            SetScanLight("incomplete", "Windows Update database recovery remains unverified.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            RefreshUpdateStoreRecovery();
            ModeLabel.Text = "UPDATE DATABASE RECOVERY CHECK ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private async void OnRecoverUpdates(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
        ModeLabel.Text = "CHECKING INTERRUPTED UPDATE";
        AppendWorkflowOutput("Checking the recorded update and restart state. No installation or retry will run.");
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Updates");
        string outcome = "Recovery outcome unavailable. Review retained records before another installation attempt.";
        bool reported = false;
        try
        {
            var recovery = await Task.Run(() => UpdateReconciliation.ResolveAsync(folder, new WindowsUpdateExecutionHost(), token: stop.Token));
            outcome = recovery.Detail;
            AppendWorkflowOutput(recovery.Detail);
            AppendWorkflowOutput("Recovery records: " + folder);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                recovery.Detail,
                [new WorkflowCheck("Interrupted update recovery", recovery.Resolved ? CheckState.Clear : CheckState.Unknown, recovery.Detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "This recovery check did not install, retry, remove, or change a Windows Update.")], [],
                "Read-only Windows Update recovery check. No installation or retry was requested."), "Windows Update recovery");
            reported = true;
            Report(recovery.Detail);
            SetScanLight(recovery.Resolved ? "ready" : "incomplete", recovery.Detail);
        }
        catch (OperationCanceledException)
        {
            outcome = "Update recovery check stopped. Verification is incomplete; review the pending record before another attempt.";
            AppendWorkflowOutput(outcome);
            SetScanLight("incomplete", "Recovery check stopped.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            outcome = "Recovery check unavailable: " + error.Message;
            AppendWorkflowOutput(outcome);
            SetScanLight("incomplete", "Recovery was not verified.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            if (!reported)
                RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                    "Windows Update recovery incomplete",
                    [new WorkflowCheck("Interrupted update recovery", CheckState.Unknown, outcome),
                     new WorkflowCheck("Change boundary", CheckState.Information,
                        "This recovery check did not install, retry, remove, or change a Windows Update.")], [],
                    "Review retained update records in " + folder + " before another installation attempt."), "Windows Update recovery");
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            ModeLabel.Text = "RECOVERY CHECK ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private async void OnInstallUpdates(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (availableUpdateResult is null) { Report("Run Available updates first."); return; }
        var selections = AvailableUpdateList.SelectedItems.Cast<AvailableUpdate>().Select(u => new UpdateSelection(Guid.Parse(u.Id), u.Revision)).ToArray();
        var options = new UpdateInstallOptions(UpdateAllowRestart.IsChecked == true, UpdateAllowFeatureUpgrade.IsChecked == true);
        var search = availableUpdateResult.Options;
        try
        {
            var plan = UpdatePlanning.Create(availableUpdateResult, selections, options);
            if (!plan.Eligible) { OnPreviewUpdates(sender, e); return; }
        }
        catch (ArgumentException error) { Report(error.Message); return; }
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
        ModeLabel.Text = "INSTALLING SELECTED UPDATES";
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Updates");
        try
        {
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => UpdateExecution.RunAsync(search, selections, options, folder, new WindowsUpdateExecutionHost(), progress, stop.Token));
            string summary = result.Format();
            AppendWorkflowOutput(summary);
            bool restartRequired = result.GetExitCode() == 4;
            bool verified = result.GetExitCode() == 0;
            var action = new WorkflowAction("Windows Update installation", restartRequired ? "Restart required" : verified ? "Verified" : "Review required",
                result.Status + ". " + result.Outcomes.Count(outcome => outcome.Installed) + " of " + result.SelectedCount + " selected update(s) verified installed.",
                restartRequired ? "Restart Windows when convenient, then check the interrupted update result before another installation." :
                    verified ? "Review the installed updates and test the affected computer before considering another installation." :
                    "Review Windows Update and the saved journal before another installation attempt.", verified, restartRequired);
            WorkflowCheck[] checks = [.. result.Outcomes.Select(outcome => new WorkflowCheck(
                "Update " + outcome.Selection.Id + " revision " + outcome.Selection.Revision,
                outcome.Installed ? CheckState.Information : CheckState.Unknown, outcome.Detail)),
                new WorkflowCheck("Installation boundary", CheckState.Information,
                    "Only the explicitly selected updates were requested. No automatic restart was requested.")];
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                restartRequired ? "Restart required" : result.Status, checks, [], result.JournalPath)
            { Actions = [action] }, "Windows Update installation");
            Report(result.Status + ". Journal: " + result.JournalPath);
            SetScanLight(result.GetExitCode() == 0 ? "ready" : "incomplete", result.Status + ". Review the update report.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or OperationCanceledException or ArgumentException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            string detail = "Update operation not completed: " + error.Message;
            AppendWorkflowOutput(detail);
            var action = new WorkflowAction("Windows Update installation", "Review required", detail,
                "Check the interrupted update result and saved journal before another installation attempt.", false);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                "Windows Update outcome unverified",
                [new WorkflowCheck("Interrupted update installation", CheckState.Unknown, detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "The exact installation outcome could not be established. No automatic retry was requested.")], [], folder)
            { Actions = [action] }, "Windows Update installation");
            SetScanLight("incomplete", "Update outcome unverified. Review the retained session result.");
        }
        finally
        {
            availableUpdateResult = null;
            ApplyUpdateFilter();
            readCancellation = null;
            workflowOutputActive = false;
            repairRunning = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            ModeLabel.Text = "UPDATE OPERATION ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private void OnPreviewUpdates(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (availableUpdateResult is null) { Report("Run Available updates first."); return; }
        try
        {
            var selected = AvailableUpdateList.SelectedItems.Cast<AvailableUpdate>().Select(u => new UpdateSelection(Guid.Parse(u.Id), u.Revision)).ToArray();
            var plan = UpdatePlanning.Create(availableUpdateResult, selected,
                new(UpdateAllowRestart.IsChecked == true, UpdateAllowFeatureUpgrade.IsChecked == true));
            BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
            AppendWorkflowOutput(plan.Format());
            var checks = plan.Updates.Select(item => new WorkflowCheck("Update: " + item.Update.Title,
                item.Eligible ? CheckState.Information : CheckState.Attention,
                item.Eligible ? "Planning checks passed for this exact update identity and revision. Execution will search again before any installation." : string.Join(" ", item.Blockers)))
                .Append(new WorkflowCheck("Installation boundary", CheckState.Information,
                    "This is a preview only. No update was downloaded or installed, no license was accepted, and no restart was scheduled."))
                .ToArray();
            string status = plan.Eligible ? "Selected updates passed planning checks. No installation started." : "One or more selected updates need review before installation can be considered.";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                status, checks, [], "Read-only Windows Update plan. No execution journal was written."), "Windows Update plan");
            Report(status);
        }
        catch (ArgumentException error) { Report(error.Message); }
    }

    private void OnPreviewThirdPartyUpdate(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (thirdPartyUpdateResult is null || !TrySelectedThirdPartyUpdate(out var selection) || selection is null)
        { Report("Run third-party discovery and select one application version first."); return; }
        try
        {
            var plan = ThirdPartyUpdatePlanning.Create(thirdPartyUpdateResult, selection);
            BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
            AppendWorkflowOutput(plan.Format());
            var checks = new[]
            {
                new WorkflowCheck("Selected application", plan.Eligible ? CheckState.Information : CheckState.Attention,
                    "Package: " + plan.Selection.Id + ". Source: " + plan.Selection.Source + ". Target version: " + plan.Selection.TargetVersion + "."),
                new WorkflowCheck("Planning result", plan.Eligible ? CheckState.Information : CheckState.Attention,
                    plan.Eligible ? "Fresh discovery found the exact package and target version." : string.Join(" ", plan.Blockers)),
                new WorkflowCheck("Installation boundary", CheckState.Information,
                    "This is a preview only. No application update was downloaded or installed, and no restart was scheduled.")
            };
            string status = plan.Eligible ? "The exact application update passed preview checks. No update started." : "The selected application update needs review before installation can be considered.";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                status, checks, [], "Read-only third-party application update plan. No execution journal was written."), "Application update plan");
            Report(status);
        }
        catch (ArgumentException error) { Report(error.Message); }
    }

    private async void OnInstallThirdPartyUpdate(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (thirdPartyUpdateResult is null || !TrySelectedThirdPartyUpdate(out var selection) || selection is null)
        { Report("Run third-party discovery and select one application version first."); return; }
        if (ThirdPartyUpdateAgreements.IsChecked != true)
        { Report("Accept the selected package and source agreements before installation."); return; }
        try
        {
            if (!ThirdPartyUpdatePlanning.Create(thirdPartyUpdateResult, selection).Eligible)
            { OnPreviewThirdPartyUpdate(sender, e); return; }
        }
        catch (ArgumentException error) { Report(error.Message); return; }
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        repairRunning = true;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
        ModeLabel.Text = "UPDATING SELECTED APPLICATION";
        try
        {
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => ThirdPartyUpdateExecution.RunAsync(selection, true, ThirdPartyUpdateFolder(),
                new WindowsThirdPartyUpdateExecutionHost(), progress, stop.Token));
            AppendWorkflowOutput(result.Format());
            bool verified = result.GetExitCode() == 0;
            string detail = result.Outcome?.Detail ?? result.Status;
            var action = new WorkflowAction("Application update", verified ? "Verified" : "Review required", detail,
                verified ? "Test the updated application and restart Windows only if the vendor requires it." :
                    "Review the application state and saved journal before another update attempt.", verified);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                result.Status,
                [new WorkflowCheck("Selected application", verified ? CheckState.Information : CheckState.Unknown,
                    "Package: " + selection.Id + ". Source: " + selection.Source + ". Target version: " + selection.TargetVersion + "."),
                 new WorkflowCheck("Update result", verified ? CheckState.Information : CheckState.Unknown, detail),
                 new WorkflowCheck("Restart boundary", CheckState.Information,
                    "KillerBench did not request an automatic reboot.")], [], result.JournalPath)
            { Actions = [action] }, "Application update");
            Report(result.Status + ". Journal: " + result.JournalPath);
            SetScanLight(result.GetExitCode() == 0 ? "ready" : "incomplete", result.Status + ". Review the application update result.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or OperationCanceledException or ArgumentException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            string detail = "Application update not completed: " + error.Message;
            AppendWorkflowOutput(detail);
            var action = new WorkflowAction("Application update", "Review required", detail,
                "Check the interrupted application update and saved journal before another update attempt.", false);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                "Application update outcome unverified",
                [new WorkflowCheck("Interrupted application update", CheckState.Unknown, detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "The exact update outcome could not be established. No automatic retry was requested.")], [], ThirdPartyUpdateFolder())
            { Actions = [action] }, "Application update");
            SetScanLight("incomplete", "Application update outcome is unverified. Check the interrupted operation before another attempt.");
        }
        finally
        {
            SetThirdPartyUpdateResult(null);
            readCancellation = null;
            workflowOutputActive = false;
            repairRunning = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            ModeLabel.Text = "APPLICATION UPDATE ENDED";
            if (closeAfterOperation) Close();
        }
    }

    private async void OnRecoverThirdPartyUpdate(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        RepairPanel.IsEnabled = false;
        BeginWorkflowOutput(WorkflowKind.AvailableUpdates);
        ModeLabel.Text = "CHECKING APPLICATION UPDATE";
        AppendWorkflowOutput("Checking the exact installed version. No application update or retry will run.");
        try
        {
            var result = await Task.Run(() => ThirdPartyUpdateExecution.ReconcileAsync(ThirdPartyUpdateFolder(),
                new WindowsThirdPartyUpdateExecutionHost(), stop.Token));
            string output = "THIRD-PARTY APPLICATION UPDATE RECOVERY / " + result.Outcome + "\n" + result.Detail +
                "\nPending record: " + result.PendingPath + "\nNo update or retry was requested.";
            AppendWorkflowOutput(output);
            var state = result.Outcome is ThirdPartyUpdateRecoveryOutcome.Verified or ThirdPartyUpdateRecoveryOutcome.NoPendingRecord
                ? CheckState.Clear : CheckState.Unknown;
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AvailableUpdates, Environment.MachineName, DateTimeOffset.UtcNow,
                result.Detail,
                [new WorkflowCheck("Recovery result", state, result.Detail),
                 new WorkflowCheck("Change boundary", CheckState.Information,
                    "This recovery check did not install, retry, remove, or change an application update.")], [],
                "Read-only third-party update recovery check. No retry journal was written."), "Application update recovery");
            Report(result.Detail);
            SetScanLight(result.Outcome is ThirdPartyUpdateRecoveryOutcome.Verified or ThirdPartyUpdateRecoveryOutcome.NoPendingRecord
                ? "ready" : "incomplete", result.Detail);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or OperationCanceledException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            AppendWorkflowOutput("Application update recovery unavailable: " + error.Message);
            SetScanLight("incomplete", "Application update recovery remains unverified.");
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            SetCaptureBusy(false);
            RepairPanel.IsEnabled = true;
            ModeLabel.Text = "APPLICATION UPDATE CHECK ENDED";
            if (closeAfterOperation) Close();
        }
    }
}
