using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private InstalledApplicationInventory? applicationInventory;

    private void SetApplicationInventory(InstalledApplicationInventory? inventory)
    {
        applicationInventory = inventory;
        SaveApplicationInventory.IsEnabled = inventory is not null;
    }

    private void OnApplicationScopeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SaveApplicationInventory is not null) SetApplicationInventory(null);
    }

    private void OnReviewBrowserExtensions(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        MainAutoRepair.IsChecked = false;
        MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == WorkflowKind.BrowserExtensions);
        Report(Controls.LocalizedText.Workflow(WorkflowKind.BrowserExtensions) + ". " + Controls.LocalizedText.Get("Str_Apps_ReviewExtensionsTip"));
    }

    private async void OnReviewApplicationFolders(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string folderName = ApplicationFolderName.Text.Trim();
        if (folderName.Length == 0) { Report(Controls.LocalizedText.Get("Str_Apps_FolderName") + ". " + Controls.LocalizedText.Get("Str_Apps_FolderNameTip")); return; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = stop;
        ReviewApplicationFoldersButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        BeginWorkflowOutput(WorkflowKind.InstalledApps);
        AppendWorkflowOutput("Reviewing five exact application-folder locations. No folder contents will be read or changed.");
        try
        {
            ApplicationFolderReviewResult review = await Task.Run(() => ApplicationFolderReview.Review(folderName, token: stop.Token), stop.Token);
            var checks = review.Locations.Select(location => new WorkflowCheck(location.Area,
                location.State == "Unavailable" ? CheckState.Unknown : location.State == "Absent" ? CheckState.Clear : CheckState.Information,
                location.Detail)).ToArray();
            string status = review.Complete
                ? "Application folder review complete. No folder contents were read or changed."
                : "Application folder review incomplete. No folder contents were read or changed.";
            AppendWorkflowOutput(review.Detail);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.InstalledApps, Environment.MachineName, DateTimeOffset.UtcNow,
                status, checks, [], "Read-only application folder review. No journal was written."), "Application folder review");
            Report(status);
            SetScanLight(review.Complete ? "ready" : "incomplete", status);
        }
        catch (OperationCanceledException) { AppendWorkflowOutput("Application folder review stopped. Nothing was changed."); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException)
        { AppendWorkflowOutput("Application folder review unavailable: " + error.Message); }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            ReviewApplicationFoldersButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }

    private async void OnSaveApplicationInventory(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null || applicationInventory is not { } inventory) return;
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Save) { Filter = Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps) + " (JSON)|*.json", DefaultExt = ".json", FileName = "applications-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json", OverwritePrompt = true };
        if (picker.ShowDialog(this) != true) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = cancellation;
        SaveApplicationInventory.IsEnabled = false;
        CancelButton.IsEnabled = true;
        try
        {
            await Task.Run(() => ApplicationInventoryFiles.SaveAsync(inventory, picker.FileName, overwrite: true, token: cancellation.Token));
            Report(Controls.LocalizedText.Format("Str_Ui_Saved", Controls.LocalizedText.Get("Str_Apps_SaveInventory"), picker.FileName));
        }
        catch (OperationCanceledException) { Report(Controls.LocalizedText.Format("Str_Ui_Stopped", Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps))); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps), error.Message)); }
        finally
        {
            readCancellation = null;
            SaveApplicationInventory.IsEnabled = applicationInventory is not null;
            CancelButton.IsEnabled = false;
        }
    }

    private async void OnCompareApplicationInventories(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Filter = Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps) + " (JSON)|*.json", Title = Controls.LocalizedText.Get("Str_Ui_BeforeInventory") };
        if (picker.ShowDialog(this) != true) return;
        string before = picker.FileName;
        var afterPicker = new Controls.FileDialog(Controls.FileDialogMode.Open)
        {
            Filter = Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps) + " (JSON)|*.json",
            Title = Controls.LocalizedText.Get("Str_Ui_AfterInventory"),
            InitialDirectory = Path.GetDirectoryName(before) ?? ""
        };
        if (afterPicker.ShowDialog(this) != true) return;
        await CompareApplicationFilesAsync(before, afterPicker.FileName);
    }

    private async Task CompareApplicationFilesAsync(string before, string after)
    {
        if (readCancellation is not null) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = cancellation;
        CompareApplicationInventories.IsEnabled = false;
        CancelButton.IsEnabled = true;
        string previousTitle = EvidenceTitle.Text;
        string previousEvidence = EvidenceBox.Text;
        string previousCoverage = CoverageLabel.Text;
        BeginWorkflowOutput(WorkflowKind.InstalledApps);
        AppendWorkflowOutput("Comparing saved application inventories. No application will be changed.");
        try
        {
            var result = await Task.Run(() => ApplicationInventoryFiles.CompareAsync(before, after, cancellation.Token));
            var text = new StringBuilder("APPLICATION REGISTRATION COMPARISON\n");
            text.AppendLine("Before: " + before).AppendLine("After: " + after).AppendLine("Scope: " + result.Scope);
            text.AppendLine().AppendLine(result.Detail).AppendLine();
            if (result.Comparable) text.AppendLine(result.Differences.Count + " registration differences.");
            foreach (var change in result.Differences)
            {
                var app = change.After ?? change.Before!;
                text.AppendLine(change.Change + ": " + app.View + "\\" + app.Key);
                if (change.Before is { } old) text.AppendLine("  Before: " + old.Name + " | " + old.Version + " | " + old.Publisher);
                if (change.After is { } current) text.AppendLine("  After: " + current.Name + " | " + current.Version + " | " + current.Publisher);
            }
            AppendWorkflowOutput(text.ToString());
            var checks = new[]
            {
                new WorkflowCheck("Comparison scope", CheckState.Information, "Both saved inventories use the " + result.Scope + " registration scope."),
                new WorkflowCheck("Registration differences", !result.Comparable ? CheckState.Unknown : result.Differences.Count == 0 ? CheckState.Clear : CheckState.Attention,
                    !result.Comparable ? "The saved inventories have incomplete or mismatched coverage, so differences cannot be established." : result.Differences.Count == 0 ? "No registration differences were identified." : result.Differences.Count + " registration difference" + (result.Differences.Count == 1 ? " was" : "s were") + " identified. Review the raw output before choosing any action.")
            };
            string status = result.Comparable ? "Application inventory comparison complete. No applications changed." : "Application inventory comparison incomplete. No applications changed.";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.InstalledApps, Environment.MachineName, DateTimeOffset.UtcNow,
                status, checks, [], "Read-only application inventory comparison. No journal was written."), "Application inventory comparison");
            Report(status);
            SetScanLight(result.Comparable ? "ready" : "incomplete", status);
        }
        catch (OperationCanceledException)
        {
            EvidenceTitle.Text = previousTitle;
            EvidenceBox.Text = previousEvidence;
            CoverageLabel.Text = previousCoverage;
            Report(Controls.LocalizedText.Format("Str_Ui_Stopped", Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps)));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or NotSupportedException or FormatException)
        {
            EvidenceTitle.Text = previousTitle;
            EvidenceBox.Text = previousEvidence;
            CoverageLabel.Text = previousCoverage;
            Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Workflow(WorkflowKind.InstalledApps), error.Message));
        }
        finally
        {
            readCancellation = null;
            workflowOutputActive = false;
            SourcesList.IsEnabled = true;
            SearchBox.IsEnabled = true;
            CompareApplicationInventories.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }
}
