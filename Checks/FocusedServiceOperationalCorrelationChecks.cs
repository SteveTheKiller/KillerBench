using System.IO;
using KillerBench.Engine;

internal static class FocusedServiceOperationalCorrelationChecks
{
    public static void Run(string scratch)
    {
        var recipes = RecipeCatalog.ForWorkflow(WorkflowKind.SystemNotifications);
        var clear = FocusedServiceOperationalCorrelation.Check(recipes,
            Results(recipes, RecipeOutcome.Clear), Histories(CheckState.Information));
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("2 exact service targets") &&
            clear.Detail.Contains("0 need attention") && clear.Detail.Contains("No current service availability issue"),
            "Complete focused service evidence produces one bounded informational conclusion");

        var historical = FocusedServiceOperationalCorrelation.Check(recipes,
            Results(recipes, RecipeOutcome.Clear), Histories(CheckState.Attention));
        Require(historical is { State: CheckState.Information, Repair: null } && historical.Detail.Contains("Historical findings: 2") &&
            historical.Detail.Contains("remain historical context"), "Historical findings alone do not become a current service outage");

        var attentionResults = Results(recipes, RecipeOutcome.Clear);
        attentionResults[0] = Result(recipes[0], RecipeOutcome.RepairAvailable);
        var attention = FocusedServiceOperationalCorrelation.Check(recipes, attentionResults, Histories(CheckState.Information));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("1 need attention"),
            "A current eligible service issue remains attention without selecting another repair");

        var repairedResults = Results(recipes, RecipeOutcome.Clear);
        repairedResults[0] = Result(recipes[0], RecipeOutcome.Repaired);
        var repaired = FocusedServiceOperationalCorrelation.Check(recipes, repairedResults, Histories(CheckState.Attention));
        Require(repaired is { State: CheckState.Information, Repair: null } && repaired.Detail.Contains("1 repaired"),
            "Verified recipe recovery is reflected without turning retained history into failure");

        var incompleteResults = Results(recipes, RecipeOutcome.Clear);
        incompleteResults[0] = new(recipes[0].Id, recipes[0].Version, RecipeOutcome.Blocked, null, null, "fixture");
        Require(FocusedServiceOperationalCorrelation.Check(recipes, incompleteResults, Histories(CheckState.Attention)).State == CheckState.Unknown &&
            FocusedServiceOperationalCorrelation.Check(recipes, Results(recipes, RecipeOutcome.Clear), Histories(CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete current or historical evidence takes precedence");

        var wrongResult = Results(recipes, RecipeOutcome.Clear);
        wrongResult[0] = wrongResult[0] with { RecipeId = "other.recipe" };
        var wrongHistory = Histories(CheckState.Information);
        wrongHistory[0] = wrongHistory[0] with { Name = "Other history" };
        Require(FocusedServiceOperationalCorrelation.Check(recipes, wrongResult, Histories(CheckState.Information)).State == CheckState.Unknown &&
            FocusedServiceOperationalCorrelation.Check(recipes, Results(recipes, RecipeOutcome.Clear), wrongHistory).State == CheckState.Unknown,
            "Unexpected recipe and history identities fail closed");

        try
        {
            FocusedServiceOperationalCorrelation.Check(recipes, Results(recipes, RecipeOutcome.Clear), Histories(CheckState.Information), new CancellationToken(true));
            throw new Exception("Focused service correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Require(FocusedServiceOperationalCorrelation.Supports(WorkflowKind.FirewallServices) &&
            FocusedServiceOperationalCorrelation.Supports(WorkflowKind.CoreWindowsServices) &&
            !FocusedServiceOperationalCorrelation.Supports(WorkflowKind.Search),
            "Generic focused service summary includes applicable workflows and excludes specialized correlations");

        string workflowFolder = Path.Combine(scratch, "focused-service-correlation");
        var workflow = Workflows.RunAsync(WorkflowKind.SystemNotifications, false, workflowFolder,
            host: new WorkflowHost()).GetAwaiter().GetResult();
        var workflowSummary = workflow.Checks.Single(check => check.Name == "Focused service readiness");
        Require(workflowSummary is { State: CheckState.Information, Repair: null } && workflow.Repairs.Count == 0 &&
            workflow.Recipes.Count == recipes.Count && workflow.Checks.Count(check => check.Name == "Focused service readiness") == 1,
            "The workflow appends exactly one focused service conclusion and executes no repair during diagnosis");

        var blockedWorkflow = Workflows.RunAsync(WorkflowKind.SystemNotifications, false, workflowFolder,
            host: new WorkflowHost(blocked: true)).GetAwaiter().GetResult();
        Require(blockedWorkflow.Checks.Single(check => check.Name == "Focused service readiness") is
            { State: CheckState.Unknown, Repair: null } && blockedWorkflow.Recipes.Count == 0 && blockedWorkflow.Repairs.Count == 0,
            "A blocked recipe pass still emits one explicit incomplete focused service conclusion without executing repair");

        Console.WriteLine("PASS: focused service recipe outcomes and exact failure histories correlate in the workflow without widening repair authority.");
    }

    private static RecipeResult[] Results(IReadOnlyList<RepairRecipe> recipes, RecipeOutcome outcome) =>
        [.. recipes.Select(recipe => Result(recipe, outcome))];

    private static RecipeResult Result(RepairRecipe recipe, RecipeOutcome outcome) =>
        new(recipe.Id, recipe.Version, outcome,
            new(recipe.Action.ToString(), outcome == RecipeOutcome.RepairAvailable ? CheckState.Repairable : CheckState.Clear,
                "fixture", outcome == RecipeOutcome.RepairAvailable ? recipe.Action : null),
            null, "fixture");

    private static WorkflowCheck[] Histories(CheckState state) =>
    [
        new("COM+ Event System failure history", state, "fixture"),
        new("System Event Notification service failure history", state, "fixture")
    ];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class WorkflowHost(bool blocked = false) : IWorkflowHost
    {
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>(blocked
                ? [.. Histories(CheckState.Information), new("Restart", CheckState.Attention, "fixture")]
                : Histories(CheckState.Information));

        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Clear, "fixture"));

        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Read-only focused service workflow dispatched a repair");
    }
}
