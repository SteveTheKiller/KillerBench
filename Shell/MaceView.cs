using KillerBench.Engine;
using System.Windows;

namespace KillerBench;

public partial class MainWindow
{
    private sealed record MacePackageChoice(string Label, string Identity);
    private MaceRemovalReview? maceReview;

    private void OnMaceOptionChanged(object sender, RoutedEventArgs e)
    {
        if (MacePackageSelection is not null) SetMaceReview(null);
        if (MainAutoRepair is not null) MainAutoRepair.IsChecked = false;
        OnWorkflowOptionsChanged(sender, null);
    }

    private void OnMaceOptionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        OnMaceOptionChanged(sender, new RoutedEventArgs());

    private void SetMaceReview(MaceRemovalReview? review)
    {
        maceReview = review;
        var choices = review?.RemovalProviderEvidence?.Candidates
            .Where(provider => provider.ProviderIdentified &&
                provider.ProviderKind == MaceRemovalProviderKind.CurrentAccountAppPackage)
            .Select(provider => review.Candidates.Single(candidate =>
                candidate.Identity.Equals(provider.Identity, StringComparison.OrdinalIgnoreCase) &&
                candidate.RegistrationType == provider.RegistrationType && candidate.Scope == provider.Scope))
            .Select(candidate => new MacePackageChoice(candidate.Target + ": " + candidate.Name, candidate.Identity))
            .OrderBy(choice => choice.Label, StringComparer.Ordinal)
            .ToArray() ?? [];
        MacePackageSelection.ItemsSource = choices;
        MacePackageSelection.SelectedIndex = choices.Length == 1 ? 0 : -1;
        MaceReviewPackage.IsEnabled = choices.Length > 0;
        MacePackageHint.Text = choices.Length == 0
            ? "Run GO to find supported current-account package candidates."
            : choices.Length + " supported package candidate" + (choices.Length == 1 ? "" : "s") +
                " found. Select one to review the final safeguards.";
    }

    private void OnReviewMacePackage(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (maceReview is null || MacePackageSelection.SelectedItem is not MacePackageChoice selected)
        {
            Report("Run MACE, then select a supported package candidate.");
            return;
        }
        MacePackageRemovalEligibility eligibility = MacePackageRemoval.Evaluate(maceReview, selected.Identity);
        string text = "MACE PACKAGE PLAN / " + (eligibility.Eligible ? "ELIGIBLE" : "BLOCKED") +
            "\n" + selected.Label + "\nPackage: " + selected.Identity + "\n\n" + eligibility.Detail +
            "\n\nRun GO again to refresh evidence. Native package removal is not connected.";
        RegisterSource(new TextEvidence("MACE package plan " + DateTime.Now.ToString("HHmmss"),
            text.Split('\n'), false));
        Report(eligibility.Eligible
            ? "Exact package safeguards passed. Removal remains unavailable."
            : eligibility.Detail);
    }
}
