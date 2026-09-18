using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;

namespace KillerBench;

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
        Report("Browser extensions selected. Press GO to create a read-only report.");
    }

    private async void OnReviewApplicationFolders(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        string folderName = ApplicationFolderName.Text.Trim();
        if (folderName.Length == 0) { Report("Enter one exact application folder name to review."); return; }
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
        var picker = new Microsoft.Win32.SaveFileDialog { Filter = "Application inventory JSON|*.json", DefaultExt = ".json", FileName = "applications-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json", OverwritePrompt = true };
        if (picker.ShowDialog(this) != true) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = cancellation;
        SaveApplicationInventory.IsEnabled = false;
        CancelButton.IsEnabled = true;
        try
        {
            await Task.Run(() => ApplicationInventoryFiles.SaveAsync(inventory, picker.FileName, cancellation.Token, overwrite: true));
            Report("Application inventory saved: " + picker.FileName + ". Collection coverage is retained.");
        }
        catch (OperationCanceledException) { Report("Inventory save stopped or timed out."); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report("Cannot save inventory: " + error.Message); }
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
        var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Application inventory JSON|*.json", Title = "Choose BEFORE inventory" };
        if (picker.ShowDialog(this) != true) return;
        string before = picker.FileName;
        picker.Title = "Choose AFTER inventory";
        picker.FileName = "";
        if (picker.ShowDialog(this) != true) return;
        await CompareApplicationFilesAsync(before, picker.FileName);
    }

    private async Task CompareApplicationFilesAsync(string before, string after)
    {
        if (readCancellation is not null) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = cancellation;
        CompareApplicationInventories.IsEnabled = false;
        CancelButton.IsEnabled = true;
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
        catch (OperationCanceledException) { Report("Inventory comparison stopped or timed out."); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or NotSupportedException or FormatException)
        { Report("Cannot compare inventories: " + error.Message); }
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
