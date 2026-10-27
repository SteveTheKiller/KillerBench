using System.IO;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private async void OnFindMsiProducts(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        readCancellation = cancellation;
        FindMsiProductsButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        InstallerProductChoice.ItemsSource = null;
        Report("Reading installed MSI application registrations...");
        try
        {
            var scope = InstallerProductScopeChoice.SelectedIndex == 1
                ? InstalledApplicationScope.CurrentAccount : InstalledApplicationScope.Machine;
            var inventory = await Task.Run(() => InstalledApplications.ReadAsync(scope, cancellation.Token));
            var candidates = MsiProductDiagnostics.SelectCandidates(inventory);
            InstallerProductChoice.ItemsSource = candidates;
            Report(candidates.Count == 0
                ? "No named MSI product-code registrations were found in the selected scope. You can still enter an exact product code."
                : candidates.Count + " installed MSI applications found. Choose one, then press GO to diagnose it.");
            SetScanLight(inventory.Complete ? "ready" : "incomplete",
                inventory.Complete ? "MSI application list ready. No package changed." : "The MSI application list may be incomplete. No package changed.");
        }
        catch (OperationCanceledException) { Report("MSI application discovery stopped or timed out."); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Report("Cannot read MSI applications: " + error.Message); }
        finally
        {
            readCancellation = null;
            FindMsiProductsButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }

    private void OnInstallerProductSelected(object sender, SelectionChangedEventArgs e)
    {
        if (InstallerProductChoice.SelectedItem is MsiProductCandidate candidate)
            InstallerProductCode.Text = candidate.ProductCode;
        RepairConsent?.IsChecked = false;
    }

    private void OnInstallerProductScopeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InstallerProductChoice is null) return;
        InstallerProductChoice.ItemsSource = null;
        InstallerProductCode.Clear();
        RepairConsent?.IsChecked = false;
    }
}
