using System.Text.Json;
using KillerBench.Engine;

internal static class ValidationPlanCommands
{
    private static readonly string[] SupportedTargets = ["Windows 10", "Windows 11", "Supported Windows Server when the recipe applies"];


    internal static int Run(string[] args)
    {
        IReadOnlyList<NativeRecipeValidationPlan> plans;
        if (args.Length == 1)
        {
            plans = NativeValidationCatalog.All;
        }
        else if (args is ["validation-plan", "--recipe", var recipeId])
        {
            var plan = NativeValidationCatalog.Find(recipeId);
            if (plan is null)
            {
                Console.Error.WriteLine("Unknown recipe ID. Use 'recipes' to list installed definitions.");
                return 2;
            }
            plans = [plan];
        }
        else if (args is ["validation-plan", "--workflow", var workflowKey] && WorkflowCatalog.Find(workflowKey) is { } workflow)
        {
            plans = NativeValidationCatalog.ForWorkflow(workflow.Kind);
        }
        else
        {
            Console.Error.WriteLine("Usage: validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW] (read-only VM test requirements; no diagnostics or repairs)");
            return 2;
        }

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            generatedFromInstalledCatalog = true,
            executionPerformed = false,
            supportedTargets = SupportedTargets,
            limitations = "This plan defines required scenarios. It is not evidence that native VM validation has been completed.",
            recipes = plans
        }, Capture.JsonOptions));
        return 0;
    }
}
