using KillerBench.Engine;

internal static class NativeValidationCatalogChecks
{
    internal static void Run()
    {
        var plans = NativeValidationCatalog.All;
        Require(plans.Count == RecipeCatalog.All.Count, "Every installed recipe has one native validation plan");
        Require(plans.Select(plan => plan.RecipeId).SequenceEqual(RecipeCatalog.All.Select(recipe => recipe.Id)),
            "Native validation plans preserve deterministic recipe order");
        Require(plans.SelectMany(plan => plan.Scenarios).Select(scenario => scenario.Id).Distinct(StringComparer.Ordinal).Count() ==
            plans.Sum(plan => plan.Scenarios.Count), "Native validation scenario IDs are globally unique");

        foreach (var recipe in RecipeCatalog.All)
        {
            var plan = NativeValidationCatalog.Find(recipe.Id) ?? throw new Exception("Missing validation plan: " + recipe.Id);
            Require(plan.RecipeVersion == recipe.Version && plan.Action == recipe.Action && plan.RecoveryContract == recipe.RecoveryContract,
                "Validation plan identity matches recipe " + recipe.Id);
            Require(plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.Healthy) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.IncompleteEvidence) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.ProviderFailure) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.Cancellation),
                "Every recipe covers healthy, incomplete, provider-failure and cancellation states: " + recipe.Id);

            if (recipe.DiagnosticOnly)
            {
                Require(plan.Mode == NativeValidationMode.DiagnosticOnly && plan.Scenarios.All(scenario => !scenario.AllowsNativeMutation),
                    "Diagnostic-only validation never permits mutation: " + recipe.Id);
                continue;
            }

            Require(recipe.RecoveryContract != RepairRecoveryContract.None,
                "Every executable recipe declares a recovery contract: " + recipe.Id);
            Require(plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.Preview) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.VerifiedExecution) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.VerificationFailure) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.InterruptionAfterDispatch) &&
                plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.Reconciliation),
                "Every repair covers preview, execution, uncertainty, interruption and reconciliation: " + recipe.Id);
            Require(plan.Scenarios.Single(scenario => scenario.Kind == NativeValidationScenarioKind.Reconciliation).AllowsNativeMutation == false,
                "Reconciliation is read-only: " + recipe.Id);

            if (recipe.RequiresExplicitOptions)
                Require(plan.Mode == NativeValidationMode.ExplicitOptions &&
                    plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.InvalidOptions) &&
                    plan.Scenarios.Any(scenario => scenario.Kind == NativeValidationScenarioKind.ChangedOptions),
                    "Option-driven repairs cover missing and changed choices: " + recipe.Id);
        }

        foreach (var workflow in WorkflowCatalog.All)
        {
            var expected = RecipeCatalog.ForWorkflow(workflow.Kind).Select(recipe => recipe.Id);
            var actual = NativeValidationCatalog.ForWorkflow(workflow.Kind).Select(plan => plan.RecipeId);
            Require(actual.SequenceEqual(expected), "Workflow validation plan matches recipe scope: " + workflow.Key);
        }

        Console.WriteLine("PASS: every recipe has deterministic read-only VM validation requirements with repair recovery coverage.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
