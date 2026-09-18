using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

namespace KillerBench;

public partial class MainWindow
{
    private bool reportAvailable;

    private void OnConsoleView(object sender, RoutedEventArgs e) => SetOutputView(report: false);

    private void OnReportView(object sender, RoutedEventArgs e)
    {
        if (reportAvailable) SetOutputView(report: true);
    }

    private void SetOutputView(bool report)
    {
        bool showReport = report && reportAvailable;
        ConsolePane.Visibility = showReport ? Visibility.Collapsed : Visibility.Visible;
        ConsoleSearchPanel.Visibility = showReport ? Visibility.Collapsed : Visibility.Visible;
        ReportPane.Visibility = showReport ? Visibility.Visible : Visibility.Collapsed;
        ConsoleViewButton.IsEnabled = showReport;
        ReportViewButton.IsEnabled = reportAvailable && !showReport;
    }

    private void ClearWorkflowReport()
    {
        reportAvailable = false;
        ReportContent.Children.Clear();
        SetOutputView(report: false);
    }

    private void RenderWorkflowReport(WorkflowResult result)
    {
        reportAvailable = true;
        ReportContent.Children.Clear();
        ReportContent.Children.Add(Heading(WorkflowCatalog.All.Single(workflow => workflow.Kind == result.Kind).Title + " report", 22));
        ReportContent.Children.Add(Body(result.Status + " | " + result.StartedUtc.ToString("g"), 12, new Thickness(0, 0, 0, 14)));

        var summary = result.Summary;
        var metrics = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
        AddMetric(metrics, "Needs review", summary.ReviewItems);
        AddMetric(metrics, "Verified repairs", summary.VerifiedRepairs);
        AddMetric(metrics, "Incomplete", summary.Incomplete);
        AddMetric(metrics, "Checked", result.Checks.Count);
        ReportContent.Children.Add(metrics);

        ReportContent.Children.Add(Heading("Start here", 18));
        ReportContent.Children.Add(Card("Recommended next step", "", NextAction(result),
            "No action starts from this report. Review the selected workflow controls before confirming a repair."));
        if (HasRepairAvailable(result)) ReportContent.Children.Add(RepairNavigationButton());
        if (HasMaintenanceOptions(result)) ReportContent.Children.Add(MaintenanceNavigationButton());

        ReportContent.Children.Add(Heading("What needs attention", 18));
        var problems = result.Checks.Where(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable or CheckState.Unknown)
            .OrderBy(check => ReviewPriority(check.EffectiveState)).ThenBy(check => check.Name, StringComparer.Ordinal).ToArray();
        var reviewRecipes = result.Recipes.Where(recipe => recipe.EffectiveOutcome is not RecipeOutcome.Clear)
            .OrderBy(recipe => ReviewPriority(recipe.EffectiveOutcome)).ThenBy(recipe => recipe.RecipeId, StringComparer.Ordinal).ToArray();
        const int maximumVisibleReviewItems = 12;
        int visibleProblems = Math.Min(problems.Length, maximumVisibleReviewItems);
        if (problems.Length == 0 && reviewRecipes.Length == 0)
            ReportContent.Children.Add(Body("No problem was established in the completed checks. Review incomplete coverage before treating the computer as healthy.", 13, new Thickness(0, 0, 0, 12)));
        foreach (var check in problems.Take(visibleProblems))
            ReportContent.Children.Add(Card(check.Name, check.EffectiveState.ToString(), check.Detail,
                check.Repair is null ? "Review the retained evidence and stated limits." : Repairs.Plan(check.Repair.Value).Effects));

        int visibleRecipes = Math.Min(reviewRecipes.Length, maximumVisibleReviewItems - visibleProblems);
        foreach (var recipe in reviewRecipes.Take(visibleRecipes))
        {
            string title = RecipeCatalog.All.FirstOrDefault(item => item.Id == recipe.RecipeId)?.Title ?? recipe.RecipeId;
            ReportContent.Children.Add(Card(title, DescribeRecipeOutcome(recipe), recipe.Detail,
                recipe.Repair is null ? "Review the recorded outcome before selecting another action." : Repairs.Plan(recipe.Repair.Kind).Effects));
        }
        int additionalReviewItems = problems.Length + reviewRecipes.Length - visibleProblems - visibleRecipes;
        if (additionalReviewItems > 0)
            ReportContent.Children.Add(Body($"{additionalReviewItems} additional review items are retained in Console and the exported report.", 12, new Thickness(0, 0, 0, 12), "MutedTextBrush"));

        ReportContent.Children.Add(Heading("Actions taken", 18));
        if (result.Repairs.Count == 0)
            ReportContent.Children.Add(Body("No repair was executed in this workflow.", 13, new Thickness(0, 0, 0, 12)));
        foreach (var repair in result.Repairs)
            ReportContent.Children.Add(Card(Repairs.Plan(repair.Kind).Title, repair.Status,
                Repairs.IsVerified(repair) ? "The recorded follow-up step verified this repair." : "This action is not verified. Review its recorded outcome before taking another action.",
                repair.RequiresRestart ? "Restart Windows, then rerun the affected workflow to verify." : "Test the affected feature and review the report before any further change."));

        ReportContent.Children.Add(Heading("Scope and limits", 18));
        ReportContent.Children.Add(Body("This report covers only the checks selected for this workflow. Historical evidence does not by itself prove a current fault or authorize a repair.", 12, new Thickness(0, 0, 0, 4)));
    }

    private Border Card(string title, string state, string detail, string action)
    {
        var content = new StackPanel();
        content.Children.Add(Heading(title, 15));
        if (!string.IsNullOrWhiteSpace(state)) content.Children.Add(Body(state, 11, new Thickness(0, 0, 0, 7), "PrimaryBrush"));
        content.Children.Add(Body(detail, 13, new Thickness(0, 0, 0, 9)));
        content.Children.Add(Body("Next: " + action, 12, new Thickness(0), "MutedTextBrush"));
        var card = new Border { Child = content, Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 10), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        card.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "PaneBorderBrush");
        return card;
    }

    private Button RepairNavigationButton()
    {
        var button = new Button
        {
            Content = "Review repair options",
            ToolTip = "Open the Repair workspace. No repair is selected, approved or started.",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        button.SetResourceReference(Button.StyleProperty, "SurfaceButton");
        button.Click += OnReviewRepairOptions;
        return button;
    }

    private void OnReviewRepairOptions(object sender, RoutedEventArgs e)
    {
        MainAutoRepair.IsChecked = false;
        CategoryList.SelectedItem = ToolCategories.Single(category => category.Title == "Repair");
    }

    private Button MaintenanceNavigationButton()
    {
        var button = new Button
        {
            Content = "Review maintenance options",
            ToolTip = "Open the Maintenance workspace. No cleanup, update, profile or task action is selected or started.",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        button.SetResourceReference(Button.StyleProperty, "SurfaceButton");
        button.Click += OnReviewMaintenanceOptions;
        return button;
    }

    private void OnReviewMaintenanceOptions(object sender, RoutedEventArgs e)
    {
        MainAutoRepair.IsChecked = false;
        CategoryList.SelectedItem = ToolCategories.Single(category => category.Title == "Maintenance");
    }

    private static bool HasRepairAvailable(WorkflowResult result) =>
        result.Checks.Any(check => check.EffectiveState == CheckState.Repairable && check.Repair is not null) ||
        result.Recipes.Any(recipe => recipe.EffectiveOutcome == RecipeOutcome.RepairAvailable && recipe.Repair is not null);

    private static bool HasMaintenanceOptions(WorkflowResult result) =>
        result.Kind == WorkflowKind.CommonProblems && result.Summary.ReviewItems > 0;

    private TextBlock Heading(string text, double size)
    {
        var block = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 7) };
        block.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
        return block;
    }

    private TextBlock Body(string text, double size, Thickness margin, string brush = "TextBrush")
    {
        var block = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = margin };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        return block;
    }

    private void AddMetric(WrapPanel host, string label, int value)
    {
        var metric = new StackPanel { Width = 132, Margin = new Thickness(0, 0, 8, 8) };
        metric.Children.Add(Heading(value.ToString(), 22));
        metric.Children.Add(Body(label, 11, new Thickness(0), "MutedTextBrush"));
        var card = new Border { Child = metric, Padding = new Thickness(10), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1) };
        card.SetResourceReference(Border.BackgroundProperty, "PaneBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "PaneBorderBrush");
        host.Children.Add(card);
    }

    private static string DescribeRecipeOutcome(RecipeResult recipe) => recipe.EffectiveOutcome switch
    {
        RecipeOutcome.Repaired when recipe.Repair is not null && Repairs.IsVerified(recipe.Repair) => "Repaired and verified",
        RecipeOutcome.Repaired => "Verification incomplete",
        RecipeOutcome.RepairAvailable => "Repair available",
        RecipeOutcome.RestartRequired => "Restart required",
        RecipeOutcome.Blocked => "Deferred",
        RecipeOutcome.Canceled => "Stopped",
        RecipeOutcome.Unknown => "Could not determine",
        _ => "Needs attention"
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
