using System.Windows;
using System.Windows.Controls;
using KillerBench.Controls;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private bool reportAvailable;
    // The last readable report, kept so a language change can render it again in the new language.
    private (WorkflowResult Result, string? Title)? lastReport;

    // The right sidebar holds all output: the readable report and the raw technical log.
    private void OnConsoleView(object sender, RoutedEventArgs e) => ShowRawOutput();

    private void OnReportView(object sender, RoutedEventArgs e)
    {
        rawView = false;
        SetOutputView();
    }

    private void SetOutputView()
    {
        ReportPane.Visibility = !rawView && reportAvailable ? Visibility.Visible : Visibility.Collapsed;
        ConsolePane.Visibility = rawView ? Visibility.Visible : Visibility.Collapsed;
        ConsoleSearchPanel.Visibility = ConsolePane.Visibility;
        RefreshOutputButtons();
    }

    private void ClearWorkflowReport()
    {
        reportAvailable = false;
        lastReport = null;
        ReportContent.Children.Clear();
        SetOutputView();
    }

    private void RenderWorkflowReport(WorkflowResult result) => RenderNamedWorkflowReport(result, null);

    private void RenderNamedWorkflowReport(WorkflowResult result, string? title)
    {
        rawView = false;
        BuildWorkflowReport(result, title);
    }

    // Rebuilds the visible report in the current language without changing the output view.
    private void RefreshWorkflowReportLanguage()
    {
        if (reportAvailable && lastReport is { } report) BuildWorkflowReport(report.Result, report.Title);
    }

    private void BuildWorkflowReport(WorkflowResult result, string? title)
    {
        bool demo = result.Machine == DemoWorkflow.MachineName;
        reportAvailable = true;
        lastReport = (result, title);
        ReportContent.Children.Clear();
        string reportTitle = title ?? LocalizedText.Workflow(result.Kind);
        ReportContent.Children.Add(Heading(LocalizedText.Format(demo ? "Str_Report_DemoTitle" : "Str_Report_Title", reportTitle), 15));
        ReportContent.Children.Add(Body(LocalizedStatus(result.Status) + " | " + result.StartedUtc.ToString("g"), 12, new Thickness(0, 0, 0, 14)));

        var summary = result.Summary;
        var metrics = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1, Columns = 4, Margin = new Thickness(0, 0, -8, 14) };
        AddMetric(metrics, LocalizedText.Get("Str_Report_NeedsReview"), summary.ReviewItems);
        AddMetric(metrics, LocalizedText.Get("Str_Report_VerifiedRepairs"), summary.VerifiedRepairs);
        AddMetric(metrics, LocalizedText.Get("Str_Report_Incomplete"), summary.Incomplete);
        AddMetric(metrics, LocalizedText.Get("Str_Report_Checked"), result.Checks.Count);
        ReportContent.Children.Add(metrics);

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_NextStep"), 13));
        ReportContent.Children.Add(Card(LocalizedText.Get("Str_Report_RecommendedNextStep"), "",
            demo ? LocalizedText.Get("Str_Report_DemoNextStep") : NextAction(result),
            demo ? LocalizedText.Get("Str_Report_DemoNextAction") : LocalizedText.Get("Str_Report_NoActionStarts")));
        if (!demo)
        {
            if (HasRepairAvailable(result)) ReportContent.Children.Add(RepairNavigationButton(result));
            if (HasMaintenanceOptions(result)) ReportContent.Children.Add(MaintenanceNavigationButton(result.Kind));
            if (HasSecurityOptions(result)) ReportContent.Children.Add(SecurityNavigationButton(result.Kind));
            if (HasNetworkOptions(result)) ReportContent.Children.Add(NetworkNavigationButton());
        }

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_NeedsAttention"), 13));
        var problems = result.Checks.Where(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable or CheckState.Unknown)
            .OrderBy(check => ReviewPriority(check.EffectiveState)).ThenBy(check => check.Name, StringComparer.Ordinal).ToArray();
        var reviewRecipes = result.Recipes.Where(recipe => recipe.EffectiveOutcome is not RecipeOutcome.Clear)
            .OrderBy(recipe => ReviewPriority(recipe.EffectiveOutcome)).ThenBy(recipe => recipe.RecipeId, StringComparer.Ordinal).ToArray();
        const int maximumVisibleReviewItems = 12;
        int visibleProblems = Math.Min(problems.Length, maximumVisibleReviewItems);
        if (problems.Length == 0 && reviewRecipes.Length == 0)
            ReportContent.Children.Add(Body(LocalizedText.Get("Str_Report_NoProblem"), 12, new Thickness(0, 0, 0, 12)));
        foreach (var check in problems.Take(visibleProblems))
            ReportContent.Children.Add(Card(check.Name, LocalizedText.Choice(check.EffectiveState), check.Detail,
                check.Repair is null ? LocalizedText.Get("Str_Report_ReviewEvidence") : Repairs.Plan(check.Repair.Value).Effects));

        int visibleRecipes = Math.Min(reviewRecipes.Length, maximumVisibleReviewItems - visibleProblems);
        foreach (var recipe in reviewRecipes.Take(visibleRecipes))
        {
            string recipeTitle = RecipeCatalog.All.FirstOrDefault(item => item.Id == recipe.RecipeId)?.Title ?? recipe.RecipeId;
            ReportContent.Children.Add(Card(recipeTitle, DescribeRecipeOutcome(recipe), recipe.Detail,
                recipe.Repair is null ? LocalizedText.Get("Str_Report_ReviewOutcome") : Repairs.Plan(recipe.Repair.Kind).Effects));
        }
        int additionalReviewItems = problems.Length + reviewRecipes.Length - visibleProblems - visibleRecipes;
        if (additionalReviewItems > 0)
            ReportContent.Children.Add(Body(LocalizedText.Format("Str_Report_MoreReviewItems", additionalReviewItems), 12, new Thickness(0, 0, 0, 12), "MutedTextBrush"));

        var information = result.Checks.Where(check => check.EffectiveState == CheckState.Information)
            .OrderBy(check => check.Name, StringComparer.Ordinal).ToArray();
        if (information.Length > 0)
        {
            ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_CollectedInformation"), 13));
            const int maximumVisibleInformationItems = 12;
            foreach (var check in information.Take(maximumVisibleInformationItems))
                ReportContent.Children.Add(Card(check.Name, LocalizedText.Choice(CheckState.Information), check.Detail,
                    LocalizedText.Get("Str_Report_EvidenceOnly")));
            if (information.Length > maximumVisibleInformationItems)
                ReportContent.Children.Add(Body(LocalizedText.Format("Str_Report_MoreInformation", information.Length - maximumVisibleInformationItems), 12, new Thickness(0, 0, 0, 12), "MutedTextBrush"));
        }

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_ActionsTaken"), 13));
        if (result.Actions.Count == 0 && result.Repairs.Count == 0)
            ReportContent.Children.Add(Body(LocalizedText.Get("Str_Report_NoRepair"), 12, new Thickness(0, 0, 0, 12)));
        foreach (var action in result.Actions)
            ReportContent.Children.Add(Card(action.Name, action.Status, action.Detail, action.NextAction));
        foreach (var repair in result.Repairs)
            ReportContent.Children.Add(Card(Repairs.Plan(repair.Kind).Title, repair.Status,
                Repairs.IsVerified(repair) ? LocalizedText.Get("Str_Report_RepairVerified") : LocalizedText.Get("Str_Report_RepairNotVerified"),
                repair.RequiresRestart ? LocalizedText.Get("Str_Report_RestartThenRerun") : LocalizedText.Get("Str_Report_TestFeature")));

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_ScopeAndLimits"), 13));
        ReportContent.Children.Add(Body(LocalizedText.Get(title is null ? "Str_Report_ScopeWorkflow" : "Str_Report_ScopeFocused"), 12, new Thickness(0, 0, 0, 4)));
    }

    private static Border Card(string title, string state, string detail, string action)
    {
        var content = new StackPanel();
        content.Children.Add(Heading(title, 13));
        if (!string.IsNullOrWhiteSpace(state)) content.Children.Add(Body(state, 11, new Thickness(0, 0, 0, 7), "PrimaryBrush"));
        content.Children.Add(Body(detail, 12, new Thickness(0, 0, 0, 9)));
        content.Children.Add(Body(LocalizedText.Format("Str_Report_NextPrefix", action), 12, new Thickness(0), "MutedTextBrush"));
        var card = new Border { Child = content, Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 10), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        card.SetResourceReference(Border.BackgroundProperty, "ReportCardBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "PaneBorderBrush");
        return card;
    }

    private Button RepairNavigationButton(WorkflowResult result)
    {
        WorkflowKind? target = RepairNavigationTarget(result);
        string targetTitle = target is { } kind ? LocalizedText.Workflow(kind) : "";
        var button = new Button
        {
            Content = target is null ? LocalizedText.Get("Str_Report_ReviewRepairOptions") : LocalizedText.Format("Str_Report_ReviewWorkflowRepair", targetTitle),
            ToolTip = target is null
                ? LocalizedText.Get("Str_Report_ReviewRepairOptionsTip")
                : LocalizedText.Format("Str_Report_ReviewWorkflowRepairTip", targetTitle),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        button.SetResourceReference(Button.StyleProperty, "SurfaceButton");
        button.Click += (_, _) => OnReviewRepairOptions(target);
        return button;
    }

    private void OnReviewRepairOptions(WorkflowKind? target)
    {
        MainAutoRepair.IsChecked = false;
        CategoryList.SelectedItem = target is { } kind ? CategoryOf(kind) : ToolCategories.Single(category => category.Id == "Repair");
        if (target is { } selected)
            MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(workflow => workflow.Kind == selected);
    }

    private Button MaintenanceNavigationButton(WorkflowKind workflow)
    {
        var button = new Button
        {
            Content = LocalizedText.Get("Str_Report_ReviewMaintenance"),
            ToolTip = LocalizedText.Get("Str_Report_ReviewMaintenanceTip"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        button.SetResourceReference(Button.StyleProperty, "SurfaceButton");
        button.Click += (_, _) =>
        {
            MainAutoRepair.IsChecked = false;
            WorkflowKind target = MaintenanceWorkflows.Contains(workflow) ? workflow : WorkflowKind.Cleanup;
            CategoryList.SelectedItem = CategoryOf(target);
            MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == target);
        };
        return button;
    }

    private Button SecurityNavigationButton(WorkflowKind workflow)
    {
        var button = new Button
        {
            Content = LocalizedText.Get("Str_Report_ReviewSecurity"),
            ToolTip = LocalizedText.Get("Str_Report_ReviewSecurityTip"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        button.SetResourceReference(Button.StyleProperty, "SurfaceButton");
        button.Click += (_, _) =>
        {
            MainAutoRepair.IsChecked = false;
            CategoryList.SelectedItem = CategoryOf(workflow);
            MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == workflow);
        };
        return button;
    }

    private Button NetworkNavigationButton()
    {
        var button = new Button
        {
            Content = LocalizedText.Get("Str_Report_ReviewNetwork"),
            ToolTip = LocalizedText.Get("Str_Report_ReviewNetworkTip"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        button.SetResourceReference(Button.StyleProperty, "SurfaceButton");
        button.Click += (_, _) =>
        {
            MainAutoRepair.IsChecked = false;
            CategoryList.SelectedItem = CategoryOf(WorkflowKind.Network);
            MainWorkflow.SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.Network);
        };
        return button;
    }

    private static bool HasRepairAvailable(WorkflowResult result) =>
        result.Checks.Any(check => check.EffectiveState == CheckState.Repairable && check.Repair is not null) ||
        result.Recipes.Any(recipe => recipe.EffectiveOutcome == RecipeOutcome.RepairAvailable &&
            (recipe.Repair is not null || recipe.Check?.Repair is not null ||
             RecipeCatalog.All.Any(item => item.Id == recipe.RecipeId)));

    private static WorkflowKind? RepairNavigationTarget(WorkflowResult result)
    {
        var targets = result.Checks
            .Where(check => check.EffectiveState == CheckState.Repairable && check.Repair is not null)
            .Select(check => check.Repair!.Value)
            .Concat(result.Recipes.Where(recipe => recipe.EffectiveOutcome == RecipeOutcome.RepairAvailable)
                .Select(recipe => recipe.Check?.Repair ?? RecipeCatalog.All.FirstOrDefault(item => item.Id == recipe.RecipeId)?.Action)
                .Where(repair => repair is not null)
                .Select(repair => repair!.Value))
            .Select(RepairWorkflow)
            .Where(kind => kind is not null)
            .Select(kind => kind!.Value)
            .Distinct()
            .ToArray();
        return targets.Length == 1 ? targets[0] : null;
    }

    private static WorkflowKind? RepairWorkflow(RepairKind repair)
    {
        foreach (WorkflowKind kind in RepairWorkflows)
        {
            if (Workflows.Scopes(kind).Contains(repair)) return kind;
        }
        return null;
    }

    private static bool HasMaintenanceOptions(WorkflowResult result) =>
        result.Summary.ReviewItems > 0 && (result.Kind == WorkflowKind.CommonProblems ||
            MaintenanceWorkflows.Contains(result.Kind));

    private static bool HasSecurityOptions(WorkflowResult result) =>
        result.Summary.ReviewItems > 0 && SecurityWorkflows.Contains(result.Kind);

    private static bool HasNetworkOptions(WorkflowResult result) =>
        result.Kind == WorkflowKind.Network && result.Summary.ReviewItems > 0;

    private static TextBlock Heading(string text, double size)
    {
        var block = new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 5) };
        block.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
        return block;
    }

    private static TextBlock Body(string text, double size, Thickness margin, string brush = "TextBrush")
    {
        var block = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = margin };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        return block;
    }

    private static void AddMetric(Panel host, string label, int value)
    {
        var metric = new StackPanel();
        metric.Children.Add(Heading(value.ToString(), 15));
        metric.Children.Add(Body(label, 11, new Thickness(0), "MutedTextBrush"));
        var card = new Border { Child = metric, Padding = new Thickness(10), Margin = new Thickness(0, 0, 8, 0), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        card.SetResourceReference(Border.BackgroundProperty, "ReportMetricBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "PaneBorderBrush");
        host.Children.Add(card);
    }

    private static string DescribeRecipeOutcome(RecipeResult recipe) => LocalizedText.Get(recipe.EffectiveOutcome switch
    {
        RecipeOutcome.Repaired when recipe.Repair is not null && Repairs.IsVerified(recipe.Repair) => "Str_Recipe_RepairedVerified",
        RecipeOutcome.Repaired => "Str_Recipe_VerificationIncomplete",
        RecipeOutcome.RepairAvailable => "Str_Recipe_RepairAvailable",
        RecipeOutcome.RestartRequired => "Str_Recipe_RestartRequired",
        RecipeOutcome.Blocked => "Str_Recipe_Deferred",
        RecipeOutcome.Canceled => "Str_Recipe_Stopped",
        RecipeOutcome.Unknown => "Str_Recipe_Unknown",
        _ => "Str_Status_NeedsAttention"
    });

    // Workflow outcomes that every workflow can report are shown in the selected language. A
    // workflow-specific outcome stays in the Engine's English wording.
    private static string LocalizedStatus(string status) => status switch
    {
        "Needs attention" => LocalizedText.Get("Str_Status_NeedsAttention"),
        "Running" => LocalizedText.Get("Str_Status_Running"),
        "No issues detected in checked scopes" => LocalizedText.Get("Str_Status_NoIssues"),
        "Repairs verified in checked scopes" => LocalizedText.Get("Str_Status_RepairsVerified"),
        "Restart required; run this workflow again after restarting to verify" => LocalizedText.Get("Str_Status_RestartRequired"),
        _ => status
    };

    private static int ReviewPriority(CheckState state) => state switch
    {
        CheckState.Repairable => 0,
        CheckState.Attention => 1,
        CheckState.Unknown => 2,
        _ => 3
    };

    private static int ReviewPriority(RecipeOutcome outcome) => outcome switch
    {
        RecipeOutcome.RepairAvailable => 0,
        RecipeOutcome.RestartRequired => 1,
        RecipeOutcome.Repaired => 2,
        RecipeOutcome.Unknown => 3,
        RecipeOutcome.Blocked => 4,
        RecipeOutcome.Canceled => 5,
        _ => 6
    };

    private static string NextAction(WorkflowResult result) => WorkflowReport.NextAction(result);
}
