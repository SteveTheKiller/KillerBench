using KillerBench.Engine;
using System.Windows;

namespace KillerBench;

public partial class MainWindow
{
    private AppPackageInventoryResult? appPackageInventory;

    private void OnAppPackageScopeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (AppPackageSelection is not null) SetAppPackageInventory(null);
    }

    private void SetAppPackageInventory(AppPackageInventoryResult? inventory)
    {
        appPackageInventory = inventory;
        AppPackageSelection.ItemsSource = inventory?.Packages;
        AppPackageSelection.SelectedIndex = -1;
    }

    private void OnReviewAppPackage(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (appPackageInventory is null || AppPackageSelection.SelectedItem is not AppPackage selected)
        { Report("Run App packages, then select a package to review."); return; }
        var review = AppPackageReview.Review(appPackageInventory, selected.FullName);
        string text = "PACKAGE REVIEW / RETAINED SNAPSHOT\n" + review.SelectedFullName +
            "\nScope: " + review.Scope + "\n\n" + string.Join("\n", review.ReviewItems) +
            "\n\nDependent packages:\n" + (review.DependentPackages.Count == 0 ? "None reported in this snapshot." : string.Join("\n", review.DependentPackages)) +
            "\n\nRun GO again to refresh inventory. No packages changed.";
        RegisterSource(new TextEvidence("Package review " + DateTime.Now.ToString("HHmmss"), text.Split('\n'), false));
        Report("Package snapshot reviewed. No removal performed or authorized.");
    }
}
