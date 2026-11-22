using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

// The top of every report: a plain verdict, one Fix button that repairs and checks again,
// and the problems in plain words. The full technical report sits under a toggle below it.
public partial class MainWindow
{
    private void BuildResultsVerdict(StackPanel host, WorkflowResult result, string reportTitle, bool demo)
    {
        var problems = result.Checks.Where(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable)
            .OrderBy(check => ReviewPriority(check.EffectiveState)).ThenBy(check => check.Name, StringComparer.Ordinal).ToArray();
        var unknown = result.Checks.Where(check => check.EffectiveState == CheckState.Unknown)
            .OrderBy(check => check.Name, StringComparer.Ordinal).ToArray();
        int passed = result.Checks.Count(check => check.EffectiveState == CheckState.Clear);
        var fixes = FixableRepairs(result);
        bool canFix = FixWorkflows(result, fixes).Length > 0;
        bool repaired = result.Repairs.Count > 0 && result.Repairs.All(Repairs.IsVerified) ||
            result.Checks.Any(check => check.Name == "Repair verification" && check.EffectiveState == CheckState.Clear);
        int fixableCount = problems.Count(check => check.EffectiveState == CheckState.Repairable) +
            result.Recipes.Count(recipe => recipe.EffectiveOutcome == RecipeOutcome.RepairAvailable);

        string headline, explain, tone;
        if (problems.Length > 0)
        {
            headline = problems.Length == 1 ? LocalizedText.Get("Str_Results_OneProblem") : LocalizedText.Format("Str_Results_Problems", problems.Length);
            explain = canFix
                ? fixableCount >= problems.Length ? LocalizedText.Get(problems.Length == 1 ? "Str_Results_CanFixAll" : "Str_Results_CanFixAllMany") : LocalizedText.Format("Str_Results_CanFixSome", fixableCount)
                : LocalizedText.Get("Str_Results_CantFix");
            tone = fixableCount > 0 ? "DangerRed" : "WarnBrush";
        }
        else if (repaired)
        {
            headline = LocalizedText.Get("Str_Results_Fixed");
            explain = LocalizedText.Get("Str_Results_FixedDetail");
            tone = "OkBrush";
        }
        else if (unknown.Length > 0)
        {
            headline = LocalizedText.Get("Str_Results_Incomplete");
            explain = LocalizedText.Format("Str_Results_IncompleteDetail", unknown.Length);
            tone = "MutedTextBrush";
        }
        else
        {
            headline = LocalizedText.Get("Str_Results_AllGood");
            explain = LocalizedText.Format("Str_Results_AllGoodDetail", passed);
            tone = "OkBrush";
        }

        var verdict = new StackPanel();
        var big = Body(headline, 22, new Thickness(0, 0, 0, 2));
        big.FontWeight = FontWeights.SemiBold;
        big.SetResourceReference(TextBlock.ForegroundProperty, tone == "MutedTextBrush" ? "TextBrush" : tone);
        verdict.Children.Add(big);
        verdict.Children.Add(Body(LocalizedText.Format("Str_Results_Checked", reportTitle,
            result.StartedUtc.ToLocalTime().ToString("t", CultureInfo.CurrentCulture)), 12, new Thickness(0, 0, 0, 8), "MutedTextBrush"));
        verdict.Children.Add(Body(explain, 13, new Thickness(0)));
        host.Children.Add(Card(verdict, tone));

        // One row of actions. Fix is the loud one; everything else is a quiet way to the right tool.
        var actions = new WrapPanel { Margin = new Thickness(0, 4, 0, 12) };
        var confirm = new StackPanel { Visibility = Visibility.Collapsed };
        bool live = !demo || App.DemoMode;
        if (canFix && live)
        {
            var fix = ResultsButton(fixableCount > 1 ? LocalizedText.Format("Str_Results_FixCount", fixableCount) : LocalizedText.Get("Str_Results_Fix"), "PrimaryButton");
            fix.Click += (_, _) =>
            {
                confirm.Visibility = Visibility.Visible;
                actions.Visibility = Visibility.Collapsed;
            };
            actions.Children.Add(fix);
            BuildFixConfirm(confirm, actions, result, fixes);
        }
        else if (!demo && fixes.Length > 0) actions.Children.Add(RepairNavigationButton(result));
        if (!demo)
        {
            if (HasMaintenanceOptions(result)) actions.Children.Add(MaintenanceNavigationButton(result.Kind));
            if (HasSecurityOptions(result)) actions.Children.Add(SecurityNavigationButton(result.Kind));
            if (HasNetworkOptions(result)) actions.Children.Add(NetworkNavigationButton());
        }
        if (live)
        {
            var again = ResultsButton(LocalizedText.Get("Str_Results_CheckAgain"), "OutlineButton");
            again.Click += async (_, _) => await RunFromReportAsync(result, autoRepair: false);
            actions.Children.Add(again);
        }
        foreach (var button in actions.Children.OfType<Button>()) button.Margin = new Thickness(0, 0, 8, 6);
        if (actions.Children.Count > 0) host.Children.Add(actions);
        host.Children.Add(confirm);

        foreach (var check in problems.Concat(unknown).Take(12))
            host.Children.Add(ProblemCard(result, check));
        if (problems.Length + unknown.Length > 12)
            host.Children.Add(Body(LocalizedText.Format("Str_Report_MoreReviewItems", problems.Length + unknown.Length - 12), 12, new Thickness(0, 0, 0, 8), "MutedTextBrush"));
        if (passed > 0 && problems.Length + unknown.Length > 0)
            host.Children.Add(Body(LocalizedText.Format("Str_Results_Passed", passed), 12, new Thickness(0, 2, 0, 4), "MutedTextBrush"));
    }

    // Fix asks once, in place: what will change, then Fix now or Cancel. Nothing runs until Fix now.
    private void BuildFixConfirm(StackPanel confirm, WrapPanel actions, WorkflowResult result, RepairKind[] fixes)
    {
        var body = new StackPanel();
        var title = Body(LocalizedText.Get("Str_Results_ConfirmTitle"), 15, new Thickness(0, 0, 0, 8));
        title.FontWeight = FontWeights.SemiBold;
        body.Children.Add(title);
        foreach (var kind in fixes)
        {
            // Name the problem the way its card does; the engine's step title stays in the tooltip.
            var plan = Repairs.Plan(kind);
            var owner = result.Checks.FirstOrDefault(check => check.EffectiveState == CheckState.Repairable && check.Repair == kind);
            var line = Body("•  " + LocalizedText.Format("Str_Results_FixLine", owner is null ? plan.Title : DisplayReportCheckName(owner.Name)), 13, new Thickness(0, 0, 0, 4));
            line.ToolTip = plan.Title + "\n\n" + plan.Effects;
            body.Children.Add(line);
        }
        body.Children.Add(Body(LocalizedText.Get("Str_Results_ConfirmNote"), 12, new Thickness(0, 6, 0, 10), "MutedTextBrush"));
        var row = new WrapPanel();
        var now = ResultsButton(LocalizedText.Get("Str_Results_FixNow"), "PrimaryButton");
        now.Margin = new Thickness(0, 0, 8, 0);
        now.Click += async (_, _) => await RunFromReportAsync(result, autoRepair: true);
        var cancel = ResultsButton(LocalizedText.Get("Str_Results_Cancel"), "OutlineButton");
        cancel.Click += (_, _) =>
        {
            confirm.Visibility = Visibility.Collapsed;
            actions.Visibility = Visibility.Visible;
        };
        row.Children.Add(now);
        row.Children.Add(cancel);
        body.Children.Add(row);
        confirm.Children.Add(Card(body, "PrimaryBrush"));
    }

    private Border ProblemCard(WorkflowResult result, WorkflowCheck check)
    {
        (string key, string brush) = check.EffectiveState switch
        {
            CheckState.Repairable => ("Str_Results_StateFix", "DangerRed"),
            CheckState.Attention => ("Str_Results_StateLook", "WarnBrush"),
            _ => ("Str_Results_StateUnknown", "MutedTextBrush")
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var text = new StackPanel();
        var name = Body(DisplayReportCheckName(check.Name), 14, new Thickness(0, 0, 0, 3));
        name.FontWeight = FontWeights.SemiBold;
        text.Children.Add(name);
        string detail = result.Kind is WorkflowKind.Profiles or WorkflowKind.BitLocker
            ? LocalizeBitLockerResultText(LocalizeProfileResultText(check.Detail))
            : result.Kind == WorkflowKind.AppPackages ? LocalizePackageReviewText(check.Detail) : check.Detail;
        text.Children.Add(Body(detail, 12, new Thickness(0)));
        grid.Children.Add(text);
        var chip = Body(LocalizedText.Get(key), 11, new Thickness(12, 2, 0, 0), brush);
        chip.FontWeight = FontWeights.SemiBold;
        chip.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(chip, 1);
        grid.Children.Add(chip);
        return Card(grid, brush);
    }

    private static Border Card(UIElement content, string tone)
    {
        var face = new Grid();
        face.Children.Add(new Border { Child = content, Padding = new Thickness(14, 12, 14, 12) });
        var grain = new Border();
        grain.SetResourceReference(StyleProperty, "FlyoutGrain");
        face.Children.Add(grain);
        var card = new Border { Child = face, Margin = new Thickness(0, 0, 0, 8), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(4, 0, 0, 0) };
        card.SetResourceReference(Border.BackgroundProperty, "ReportCardBrush");
        card.SetResourceReference(Border.BorderBrushProperty, tone);
        return card;
    }

    private static Button ResultsButton(string text, string style)
    {
        var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Left };
        button.SetResourceReference(StyleProperty, style);
        return button;
    }

    // The exact repairs this result offers, from repairable checks and recipes that found a repair.
    private static RepairKind[] FixableRepairs(WorkflowResult result) =>
        [.. result.Checks.Where(check => check.EffectiveState == CheckState.Repairable && check.Repair is not null)
            .Select(check => check.Repair!.Value)
            .Concat(result.Recipes.Where(recipe => recipe.EffectiveOutcome == RecipeOutcome.RepairAvailable)
                .Select(recipe => recipe.Repair?.Kind ?? recipe.Check?.Repair ?? RecipeCatalog.All.FirstOrDefault(item => item.Id == recipe.RecipeId)?.Action)
                .Where(repair => repair is not null)
                .Select(repair => repair!.Value))
            .Distinct()];

    // The checks whose guarded auto-repair covers these repairs. A check that repairs runs itself; a full
    // scan, which only diagnoses, hands each repair to the focused check that owns it.
    private static WorkflowKind[] FixWorkflows(WorkflowResult result, RepairKind[] fixes) =>
        fixes.Length == 0 ? [] : Workflows.SupportsRepair(result.Kind) ? [result.Kind]
            : [.. fixes.Select(RepairWorkflow).Where(kind => kind is { } found && Workflows.SupportsRepair(found))
                .Select(kind => kind!.Value).Distinct()];

    // Fix runs each owning check with auto-repair on (each repairs, then verifies), and then runs the
    // original check again so the page ends on a fresh verdict. Check again is that last step alone.
    // A Home part that owned the old result takes the new one, so its color follows the fix.
    private async Task RunFromReportAsync(WorkflowResult result, bool autoRepair)
    {
        if (readCancellation is not null || workflowOutputActive) return;
        string? part = partSources.FirstOrDefault(pair => workflowSummaries.TryGetValue(pair.Value, out var owned) && ReferenceEquals(owned, result)).Key;
        var runs = new List<(WorkflowKind Kind, bool Repair)>();
        if (autoRepair) runs.AddRange(FixWorkflows(result, FixableRepairs(result)).Select(kind => (kind, true)));
        if (!runs.Any(run => run.Kind == result.Kind)) runs.Add((result.Kind, false));
        try
        {
            foreach (var (kind, repair) in runs)
            {
                int before = sources.Count;
                MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == kind);
                MainAutoRepair.IsChecked = repair;
                await StartMainWorkflowAsync(this, new RoutedEventArgs());
                // A stopped or refused run leaves no new result; the rest of the plan waits for the person.
                if (sources.Count == before) break;
                if (kind == result.Kind && part is not null) partSources[part] = sources[^1];
            }
        }
        finally
        {
            MainAutoRepair.IsChecked = false;
            RefreshMapEvidence();
        }
    }
}
