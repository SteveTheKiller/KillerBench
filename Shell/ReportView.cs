using System.Windows;
using System.Windows.Controls;
using KillerBench.Controls;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private bool reportAvailable;
    // The last readable report, kept so a language change can render it again in the new language.
    private (WorkflowResult Result, string? Title)? lastReport;

    // The right sidebar holds all output: the readable report and the raw technical log.
    private void OnConsoleView(object sender, RoutedEventArgs e)
    {
        if (AreaEvidencePanel.Visibility != Visibility.Visible) ShowRawOutput();
    }

    private void OnReportView(object sender, RoutedEventArgs e)
    {
        if (AreaEvidencePanel.Visibility == Visibility.Visible) return;
        rawView = false;
        SetOutputView();
    }

    private void SetOutputView()
    {
        if (AreaEvidencePanel is not null) SetAreaEvidenceMode(false);
        if (rawView || reportAvailable) SetOutputOpen(true);
        ReportPane.Visibility = !rawView && reportAvailable ? Visibility.Visible : Visibility.Collapsed;
        ConsolePane.Visibility = rawView ? Visibility.Visible : Visibility.Collapsed;
        ConsoleSearchPanel.Visibility = ConsolePane.Visibility;
        RefreshOutputButtons();
        RefreshOutputEmpty();
    }

    private void RefreshOutputEmpty()
    {
        if (OutputEmpty is null) return;
        bool empty = AreaEvidencePanel.Visibility != Visibility.Visible && ReportPane.Visibility != Visibility.Visible &&
            ConsolePane.Visibility != Visibility.Visible && StartGuide.Visibility != Visibility.Visible;
        OutputEmpty.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        if (empty) OutputEmpty.Id = ToolArt.Id;
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
        SetOutputOpen(true);
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
        string reportTitle = title is null ? LocalizedText.Workflow(result.Kind) : LocalizePackageReviewText(LocalizeBitLockerResultText(LocalizeProfileResultText(title)));
        ReportContent.Children.Add(Heading(LocalizedText.Format("Str_Report_Title", reportTitle), 15));
        ReportContent.Children.Add(Body(LocalizedStatus(result.Status) + " | " + result.StartedUtc.ToString("g"), 12, new Thickness(0, 0, 0, 14)));

        // The same state bar and drive bars as the Overview, so a result reads the same everywhere.
        var charts = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        var stateChart = new Grid { Height = 14, Margin = new Thickness(0, 0, 0, 8) };
        var stateLegend = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        var metricChart = new StackPanel();
        FillStateChart(stateChart, stateLegend, OverviewScore.From([result]));
        FillMetricChart(metricChart, result.Checks);
        charts.Children.Add(stateChart);
        charts.Children.Add(stateLegend);
        charts.Children.Add(metricChart);
        ReportContent.Children.Add(charts);

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_NextStep"), 13));
        ReportContent.Children.Add(Card(LocalizedText.Get("Str_Report_RecommendedNextStep"), "",
            NextAction(result),
            LocalizedText.Get("Str_Report_NoActionStarts"), "PrimaryBrush"));
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
            ReportContent.Children.Add(Card(DisplayReportCheckName(check.Name), LocalizedText.Choice(check.EffectiveState), result.Kind is WorkflowKind.Profiles or WorkflowKind.BitLocker ? LocalizeBitLockerResultText(LocalizeProfileResultText(check.Detail)) : result.Kind == WorkflowKind.AppPackages ? LocalizePackageReviewText(check.Detail) : check.Detail,
                check.Repair is null ? null : Repairs.Plan(check.Repair.Value).Effects, ToneBrush(check.EffectiveState)));

        int visibleRecipes = Math.Min(reviewRecipes.Length, maximumVisibleReviewItems - visibleProblems);
        foreach (var recipe in reviewRecipes.Take(visibleRecipes))
        {
            string recipeTitle = RecipeCatalog.All.FirstOrDefault(item => item.Id == recipe.RecipeId)?.Title ?? recipe.RecipeId;
            ReportContent.Children.Add(Card(recipeTitle, DescribeRecipeOutcome(recipe), recipe.Detail,
                recipe.Repair is null ? null : Repairs.Plan(recipe.Repair.Kind).Effects,
                recipe.EffectiveOutcome is RecipeOutcome.RepairAvailable ? "DangerRed" : recipe.EffectiveOutcome is RecipeOutcome.Unknown or RecipeOutcome.Blocked or RecipeOutcome.Canceled ? "MutedTextBrush" : "WarnBrush"));
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
                ReportContent.Children.Add(Card(DisplayReportCheckName(check.Name), LocalizedText.Choice(CheckState.Information), result.Kind is WorkflowKind.Profiles or WorkflowKind.BitLocker ? LocalizeBitLockerResultText(LocalizeProfileResultText(check.Detail)) : result.Kind == WorkflowKind.AppPackages ? LocalizePackageReviewText(check.Detail) : check.Detail,
                    null, "PaneBorderBrush"));
            if (information.Length > maximumVisibleInformationItems)
                ReportContent.Children.Add(Body(LocalizedText.Format("Str_Report_MoreInformation", information.Length - maximumVisibleInformationItems), 12, new Thickness(0, 0, 0, 12), "MutedTextBrush"));
        }

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_ActionsTaken"), 13));
        if (result.Actions.Count == 0 && result.Repairs.Count == 0)
            ReportContent.Children.Add(Body(LocalizedText.Get("Str_Report_NoRepair"), 12, new Thickness(0, 0, 0, 12)));
        foreach (var action in result.Actions)
            ReportContent.Children.Add(Card(LocalizeBitLockerResultText(action.Name), LocalizedStatus(action.Status), action.Detail, LocalizeBitLockerResultText(LocalizeProfileResultText(LocalizeReportGuidance(action.NextAction)))));
        foreach (var repair in result.Repairs)
            ReportContent.Children.Add(Card(Repairs.Plan(repair.Kind).Title, LocalizedStatus(repair.Status),
                Repairs.IsVerified(repair) ? LocalizedText.Get("Str_Report_RepairVerified") : LocalizedText.Get("Str_Report_RepairNotVerified"),
                repair.RequiresRestart ? LocalizedText.Get("Str_Report_RestartThenRerun") : LocalizedText.Get("Str_Report_TestFeature"),
                Repairs.IsVerified(repair) ? "OkBrush" : "WarnBrush"));

        ReportContent.Children.Add(Heading(LocalizedText.Get("Str_Report_ScopeAndLimits"), 13));
        ReportContent.Children.Add(Body(LocalizedText.Get(title is null ? "Str_Report_ScopeWorkflow" : "Str_Report_ScopeFocused"), 12, new Thickness(0, 0, 0, 4)));
    }

    // A finding card: the left stripe and state word carry the finding's tone, and the next line appears
    // only when it says something specific, never the same stock sentence on every card.
    private static Border Card(string title, string state, string detail, string? action, string tone = "PaneBorderBrush")
    {
        var content = new StackPanel();
        content.Children.Add(Heading(title, 13));
        if (!string.IsNullOrWhiteSpace(state))
            content.Children.Add(Body(state, 11, new Thickness(0, 0, 0, 7), tone == "PaneBorderBrush" ? "MutedTextBrush" : tone));
        content.Children.Add(Body(detail, 12, new Thickness(0, 0, 0, string.IsNullOrWhiteSpace(action) ? 0 : 9)));
        if (!string.IsNullOrWhiteSpace(action))
            content.Children.Add(Body(LocalizedText.Format("Str_Report_NextPrefix", action), 12, new Thickness(0), "MutedTextBrush"));
        var card = new Border { Child = content, Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 8), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(4, 0, 0, 0) };
        card.SetResourceReference(Border.BackgroundProperty, "ReportCardBrush");
        card.SetResourceReference(Border.BorderBrushProperty, tone);
        return card;
    }

    private static string ToneBrush(CheckState state) => state switch
    {
        CheckState.Repairable => "DangerRed",
        CheckState.Attention => "WarnBrush",
        CheckState.Unknown => "MutedTextBrush",
        CheckState.Clear => "OkBrush",
        _ => "PaneBorderBrush"
    };

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
        ShowToolsWorkspace();
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
            ShowToolsWorkspace();
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
            ShowToolsWorkspace();
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
            ShowToolsWorkspace();
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
        _ => LocalizePackageReviewText(LocalizeBitLockerResultText(LocalizeProfileResultText(status)))
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

    private static readonly (string Source, string Key)[] ReportGuidance =
    [
        ("Restart when convenient, then run this workflow again to verify and continue.", "Str_Guidance_Restart"),
        ("Review the saved journal before starting another repair; the previous operation may be incomplete.", "Str_Guidance_Interrupted"),
        ("Open an administrator terminal and rerun this workflow to complete its checks.", "Str_Guidance_AdminCli"),
        ("Run KillerBench as administrator, then select this check and select Run diagnosis again.", "Str_Guidance_Admin"),
        ("Review the new results before enabling repairs.", "Str_Guidance_ReviewBeforeRepairs"),
        ("This workflow performs checks only; no repair or deletion is selected.", "Str_Guidance_ChecksOnly"),
        ("Choose the exact repair target and required options, review its effects, then confirm that specific repair. The generic automatic-repair switch will not run it.", "Str_Guidance_ExactRepair"),
        ("Rerun this workflow with --auto-repair to run eligible repairs.", "Str_Guidance_AutoCli"),
        ("Select Run recommended cleanup, then select Run repair to perform the recommended maintenance.", "Str_Guidance_Cleanup"),
        ("Select Auto-repair confirmed issues, then select Run repair to run eligible repairs.", "Str_Guidance_Auto"),
        ("Review the reported device ID and configuration code in Device Manager. Use its hardware IDs to locate an approved vendor driver; a code alone does not prove hardware failure.", "Str_Guidance_DeviceAttention"),
        ("Device evidence is incomplete. Resolve the reported collection problem and repeat this check before deciding whether a driver or device needs attention.", "Str_Guidance_DeviceIncomplete"),
        ("No active fault requiring attention was identified in the collected device evidence. Review any absent devices and test the affected hardware directly; undetected devices remain outside this snapshot.", "Str_Guidance_DeviceClear"),
        ("No device was enabled, reset or given a different driver.", "Str_Guidance_DeviceNoChange"),
        ("Use the reported device name, ID and code to review its connection and approved driver in Device Manager. Missing driver inventory is not proof that a driver is absent.", "Str_Guidance_InputAttention"),
        ("No configuration fault was reported in the collected scopes. Test the affected keyboard, mouse or HID function directly.", "Str_Guidance_InputClear"),
        ("No input was captured and no device reset or driver change was performed.", "Str_Guidance_InputNoChange"),
        ("Review each flagged Defender control in Windows Security and compare it with the intended endpoint-security policy. Confirm the active security product and policy owner before changing anything.", "Str_Guidance_DefenderAttention"),
        ("No Defender control in this overview needs immediate review. Compare the retained settings with the intended endpoint-security policy.", "Str_Guidance_DefenderClear"),
        ("No scan, remediation, exclusion, policy or security setting was changed.", "Str_Guidance_DefenderNoChange"),
        ("Review the unresolved findings below. No broader repair has been selected automatically.", "Str_Guidance_Findings"),
        ("Review the returned update list. Installation is not enabled by this discovery workflow.", "Str_Guidance_Updates"),
        ("Review the estimate before selecting permanent removal. Preview did not remove files.", "Str_Guidance_CleanupPreview"),
        ("Review the selected privacy snapshots. Recorded values do not prove effective permissions. No privacy settings were changed.", "Str_Guidance_Privacy"),
        ("Review the selected volume and backup options. This inspection did not back up keys or change encryption.", "Str_Guidance_BitLocker"),
        ("Review the profile ages and exclusions. Age alone does not authorize deletion. Verify backups and account dependencies before any future removal.", "Str_Guidance_Profiles"),
        ("Review the proposed name and environment notes. Confirm organizational naming policy, name availability and application dependencies before planning a rename. This review made no changes.", "Str_Guidance_ComputerName"),
        ("Review the component-store recommendation below. Reclaimed space was not measured.", "Str_Guidance_Component"),
        ("Review the matching machine registry entries. This inventory does not verify installed executables, user installations or update policy. No Foxit settings were changed.", "Str_Guidance_Foxit"),
        ("Review full task paths and reported principals. Inventory does not verify actions, triggers, permissions or execution success. No tasks were started or changed.", "Str_Guidance_Tasks"),
        ("The selected checks finished. Test the affected feature to confirm normal operation.", "Str_Guidance_Finished"),
        ("This workflow is still running. Wait for its final result before planning another repair.", "Str_Guidance_Wait"),
        ("A command timed out. Confirm that the previous work has ended, then repeat the checks before selecting another repair.", "Str_Guidance_Timeout"),
        ("Some checks could not determine a result. Review the incomplete evidence below and repeat those checks before selecting a repair.", "Str_Guidance_Incomplete"),
    ];

    private static string LocalizeReportGuidance(string text)
    {
        foreach (var (source, key) in ReportGuidance)
        {
            if (text.Contains(source, StringComparison.Ordinal))
                text = text.Replace(source, LocalizedText.Get(key), StringComparison.Ordinal);
        }
        return text;
    }

    private static string DisplayReportCheckName(string name) =>
        name == NativeCapabilityCheckName ? LocalizedText.Get("Str_NativeLab_Name") : LocalizePackageReviewText(LocalizeBitLockerResultText(LocalizeProfileResultText(name)));

    private static string NextAction(WorkflowResult result) => LocalizeBitLockerResultText(LocalizeReportGuidance(WorkflowReport.NextAction(result)));
}
