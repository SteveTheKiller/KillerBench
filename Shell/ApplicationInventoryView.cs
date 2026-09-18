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

    private async void OnReviewBrowserExtensions(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        readCancellation = cancellation;
        ReviewBrowserExtensions.IsEnabled = false;
        CancelButton.IsEnabled = true;
        Report("Reviewing browser extension identities...");
        try
        {
            BrowserExtensionReviewResult result = await Task.Run(() => BrowserExtensionReview.Read(cancellation.Token));
            var text = new StringBuilder("BROWSER EXTENSION REVIEW\n");
            text.AppendLine("Coverage: " + (result.Complete ? "Complete" : "Incomplete"));
            text.AppendLine("Extension identities: " + result.Extensions.Count).AppendLine();
            foreach (BrowserExtensionIdentity extension in result.Extensions)
                text.AppendLine(extension.Browser + " | " + extension.Profile + " | " + extension.ExtensionId);
            text.AppendLine().AppendLine(result.Detail);
            RegisterSource(new TextEvidence("Browser extensions " + DateTime.Now.ToString("HHmmss"), text.ToString().Replace("\r\n", "\n").Split('\n'), !result.Complete));
            ModeLabel.Text = "BROWSER EXTENSION REVIEW";
            Report(result.Complete ? "Browser extension review complete. No browser or extension changed." : "Browser extension review is incomplete. No browser or extension changed.");
        }
        catch (OperationCanceledException) { Report("Browser extension review stopped or timed out."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or NotSupportedException)
        { Report("Browser extension review unavailable: " + error.Message); }
        finally
        {
            readCancellation = null;
            ReviewBrowserExtensions.IsEnabled = true;
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
        Report("Comparing saved application inventories...");
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
            RegisterSource(new TextEvidence("Application comparison " + DateTime.Now.ToString("HHmmss"), text.ToString().Replace("\r\n", "\n").Split('\n'), false));
            ModeLabel.Text = "SAVED INVENTORY COMPARISON";
            Report(result.Comparable ? "Inventory comparison ready. No applications changed." : "Inventories cannot establish differences: incomplete or mismatched coverage.");
        }
        catch (OperationCanceledException) { Report("Inventory comparison stopped or timed out."); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or NotSupportedException or FormatException)
        { Report("Cannot compare inventories: " + error.Message); }
        finally
        {
            readCancellation = null;
            CompareApplicationInventories.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }
}
