using KillerBench.Engine;
using KillerBench.Controls;
using System.Windows;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private sealed record MacePackageChoice(string Label, string Identity);
    private MaceRemovalReview? maceReview;
    private bool macePackageRemovalRequested;
    private bool macePackageReconciliationRequested;

    private void OnMaceOptionChanged(object sender, RoutedEventArgs e)
    {
        if (MacePackageSelection is not null) SetMaceReview(null);
        MainAutoRepair?.IsChecked = false;
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
        MacePackageRemovalConsent.IsChecked = false;
        MacePackageHint.Text = choices.Length == 0
            ? Controls.LocalizedText.Get("Str_Mace_PackageHint")
            : choices.Length + " supported package candidate" + (choices.Length == 1 ? "" : "s") +
                " found. Select one to review the final safeguards.";
        RefreshMacePackageRemoval();
    }

    private void OnMacePackageSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        MacePackageRemovalConsent.IsChecked = false;
        RefreshMacePackageRemoval();
    }

    private void OnMacePackageRemovalConsentChanged(object sender, RoutedEventArgs e) => RefreshMacePackageRemoval();

    private void RefreshMacePackageRemoval()
    {
        bool eligible = maceReview is not null && MacePackageSelection.SelectedItem is MacePackageChoice selected &&
            MacePackageRemoval.Evaluate(maceReview, selected.Identity).Eligible;
        MaceRemovePackage.IsEnabled = eligible && MacePackageRemovalConsent.IsChecked == true && readCancellation is null;
    }

    private void OnReviewMacePackage(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        if (maceReview is null || MacePackageSelection.SelectedItem is not MacePackageChoice selected)
        {
            Report("Run Microsoft app removal, then select a supported package candidate.");
            return;
        }
        MacePackageRemovalEligibility eligibility = MacePackageRemoval.Evaluate(maceReview, selected.Identity);
        string text = "APP REMOVAL PACKAGE PLAN / " + (eligibility.Eligible ? "ELIGIBLE" : "BLOCKED") +
            "\n" + selected.Label + "\nPackage: " + selected.Identity + "\n\n" + eligibility.Detail +
            "\n\nRun diagnosis stays review-only. The separate red removal control requires its own confirmation and repeats fresh exact evidence before one request.";
        BeginWorkflowOutput(WorkflowKind.Mace);
        AppendWorkflowOutput(text);
        string status = eligibility.Eligible
            ? "Exact package safeguards passed. The separate red control is available after its confirmation is selected."
            : eligibility.Detail;
        var checks = new[]
        {
            new WorkflowCheck("Selected package safeguards", eligibility.Eligible ? CheckState.Information : CheckState.Unknown, eligibility.Detail),
            new WorkflowCheck("Removal boundary", CheckState.Information,
                "Run diagnosis remains review-only. Any removal requires the separate red control, its own consent and a second confirmation after fresh safeguards pass.")
        };
        RegisterWorkflowResult(new WorkflowResult(WorkflowKind.Mace, Environment.MachineName, DateTimeOffset.UtcNow,
            status, checks, [], "Read-only app removal package plan. No journal was written."), "App removal package plan");
        Report(status);
        RefreshMacePackageRemoval();
    }

    private async void OnRemoveMacePackage(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null || MacePackageSelection.SelectedItem is not MacePackageChoice selected ||
            maceReview is null || !MacePackageRemoval.Evaluate(maceReview, selected.Identity).Eligible ||
            MacePackageRemovalConsent.IsChecked != true)
        {
            RefreshMacePackageRemoval();
            return;
        }
        var confirmation = new ConfirmDialog(
            "Remove this exact current-account package?",
            selected.Identity + "\n\nKillerBench will repeat the safeguard review, record the request, send one removal request, and verify the package inventory. It will not retry automatically.",
            "Remove package") { Owner = this };
        confirmation.ShowDialog();
        if (!confirmation.Confirmed) return;
        macePackageRemovalRequested = true;
        try { await RunWorkflowAsync(); }
        finally { macePackageRemovalRequested = false; }
    }

    private async void OnReconcileMacePackage(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        macePackageReconciliationRequested = true;
        try { await RunWorkflowAsync(); }
        finally { macePackageReconciliationRequested = false; }
    }
}
