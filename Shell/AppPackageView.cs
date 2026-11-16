using KillerBench.Engine;
using KillerBench.Controls;
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
        { Report(Controls.LocalizedText.Format("Str_Ui_RunFirst", Controls.LocalizedText.Workflow(WorkflowKind.AppPackages)) + " " + Controls.LocalizedText.Get("Str_Packages_ReviewSelected")); return; }
        var review = AppPackageReview.Review(appPackageInventory, selected.FullName);
        string text = LocalizedText.Get("Str_PackageReview_Title") + "\n" + review.SelectedFullName +
            "\n" + LocalizedText.Format("Str_PackageReview_Scope", LocalizedText.Get(review.Scope == AppPackageScope.AllUsers ? "Str_Scope_AllUsers" : "Str_Scope_RunningAccount")) + "\n\n" + string.Join("\n", review.ReviewItems) +
            "\n\n" + LocalizedText.Get("Str_PackageReview_Dependents") + ":\n" +
            (review.DependentPackages.Count == 0 ? LocalizedText.Get("Str_PackageReview_None") : string.Join("\n", review.DependentPackages)) +
            "\n\n" + LocalizedText.Format("Str_PackageReview_Refresh", LocalizedText.Get("Str_Main_RunDiagnosis"));
        BeginWorkflowOutput(WorkflowKind.AppPackages);
        AppendWorkflowOutput(text);
        var checks = review.ReviewItems.Select((item, index) => new WorkflowCheck("Package review item " + (index + 1),
            item.StartsWith("Package or dependency coverage is incomplete", StringComparison.Ordinal) || item.StartsWith("The exact selected package was not found", StringComparison.Ordinal)
                ? CheckState.Unknown : CheckState.Information, item))
            .Append(new WorkflowCheck("Dependent packages", CheckState.Information,
                "Dependent packages reported: " + review.DependentPackages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + ". Review Raw output before any action."))
            .ToArray();
        string status = review.Selected is null || review.ReviewItems.Any(item => item.StartsWith("Package or dependency coverage is incomplete", StringComparison.Ordinal))
            ? "Package snapshot review is incomplete. No package was changed."
            : "Package snapshot reviewed. No package removal is available or authorized.";
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.AppPackages, Environment.MachineName, DateTimeOffset.UtcNow,
            status, checks, [], "Read-only package snapshot review. No journal was written."), "Package snapshot review");
        Report(LocalizePackageReviewText(status));
    }

    private static string LocalizePackageReviewText(string text)
    {
        string? key = text switch
        {
            "Package snapshot review" => "Str_PackageReview_Title",
            "Dependent packages" => "Str_PackageReview_Dependents",
            "No dependent packages were reported in this retained snapshot." => "Str_PackageReview_None",
            "Package snapshot review is incomplete. No package was changed." => "Str_PackageReview_Incomplete",
            "Package snapshot reviewed. No package removal is available or authorized." => "Str_PackageReview_Reviewed",
            "Read-only package snapshot review. No journal was written." => "Str_PackageReview_Journal",
            _ => null
        };
        if (key is not null) return LocalizedText.Get(key);
        const string itemPrefix = "Package review item ";
        if (text.StartsWith(itemPrefix, StringComparison.Ordinal) &&
            int.TryParse(text.AsSpan(itemPrefix.Length), out int index))
            return LocalizedText.Format("Str_PackageReview_Item", index);
        const string countPrefix = "Dependent packages reported: ";
        const string countSuffix = ". Review Raw output before any action.";
        if (text.StartsWith(countPrefix, StringComparison.Ordinal) && text.EndsWith(countSuffix, StringComparison.Ordinal) &&
            int.TryParse(text.AsSpan(countPrefix.Length, text.Length - countPrefix.Length - countSuffix.Length), out int count))
            return LocalizedText.Format("Str_PackageReview_DependentCount", count);
        return text;
    }
}
