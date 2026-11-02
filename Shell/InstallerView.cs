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
        Report(Controls.LocalizedText.Format("Str_Ui_Reading", Controls.LocalizedText.Workflow(WorkflowKind.Installer)));
        try
        {
            var scope = InstallerProductScopeChoice.SelectedIndex == 1
                ? InstalledApplicationScope.CurrentAccount : InstalledApplicationScope.Machine;
            var inventory = await Task.Run(() => InstalledApplications.ReadAsync(scope, cancellation.Token));
            var candidates = MsiProductDiagnostics.SelectCandidates(inventory);
            InstallerProductChoice.ItemsSource = candidates;
            Report(candidates.Count == 0
                ? Controls.LocalizedText.Get("Str_Ui_MsiNone")
                : Controls.LocalizedText.Format("Str_Ui_MsiFound", candidates.Count));
            SetScanLight(inventory.Complete ? "ready" : "incomplete",
                Controls.LocalizedText.Get(inventory.Complete ? "Str_Ui_ReadOnlyResult" : "Str_Ui_DiscoveryIncomplete"));
        }
        catch (OperationCanceledException) { Report(Controls.LocalizedText.Format("Str_Ui_Stopped", Controls.LocalizedText.Workflow(WorkflowKind.Installer))); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Report(Controls.LocalizedText.Format("Str_Ui_Error", Controls.LocalizedText.Workflow(WorkflowKind.Installer), error.Message)); }
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
