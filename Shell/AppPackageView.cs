using KillerBench.Engine;
using System.Windows;

namespace KillerBench.Shell;

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
        BeginWorkflowOutput(WorkflowKind.AppPackages);
        AppendWorkflowOutput(text);
        var checks = review.ReviewItems.Select((item, index) => new WorkflowCheck("Package review item " + (index + 1),
            item.StartsWith("Package or dependency coverage is incomplete", StringComparison.Ordinal) || item.StartsWith("The exact selected package was not found", StringComparison.Ordinal)
                ? CheckState.Unknown : CheckState.Information, item))
            .Append(new WorkflowCheck("Dependent packages", CheckState.Information,
                review.DependentPackages.Count == 0 ? "No dependent packages were reported in this retained snapshot." : review.DependentPackages.Count + " dependent package" + (review.DependentPackages.Count == 1 ? " was" : "s were") + " reported. Review Raw output before any action."))
            .ToArray();
        string status = review.Selected is null || review.ReviewItems.Any(item => item.StartsWith("Package or dependency coverage is incomplete", StringComparison.Ordinal))
            ? "Package snapshot review is incomplete. No package was changed."
            : "Package snapshot reviewed. No package removal is available or authorized.";
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AppPackages, Environment.MachineName, DateTimeOffset.UtcNow,
            status, checks, [], "Read-only package snapshot review. No journal was written."), "Package snapshot review");
        Report(status);
    }
}
