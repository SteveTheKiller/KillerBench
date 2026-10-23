using System.Text.Json;
using KillerBench.Engine;

internal static class ValidationPlanCommands
{
    private static readonly string[] SupportedTargets = ["Windows 11; the native acceptance matrix is evidence coverage, not a product support limit"];


    internal static int Run(string[] args)
    {
        IReadOnlyList<RepairCapability> plans;
        if (args.Length == 1)
        {
            plans = WindowsRepairCapabilities.All;
        }
        else if (args is ["validation-plan", "--recipe", var recipeId])
        {
            var plan = WindowsRepairCapabilities.Find(recipeId);
            if (plan is null)
            {
                Console.Error.WriteLine("Unknown recipe ID. Use 'recipes' to list installed definitions.");
                return 2;
            }
            plans = [plan];
        }
        else if (args is ["validation-plan", "--workflow", var workflowKey] && WorkflowCatalog.Find(workflowKey) is { } workflow)
        {
            var ids = RecipeCatalog.ForWorkflow(workflow.Kind).Select(recipe => recipe.Id).ToHashSet(StringComparer.Ordinal);
            plans = WindowsRepairCapabilities.All.Where(capability => ids.Contains(capability.Id)).ToArray();
        }
        else
        {
            Console.Error.WriteLine("Usage: validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW] (read-only VM test requirements; no diagnostics or repairs)");
            return 2;
        }

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = 2,
            generatedFromInstalledCatalog = true,
            executionPerformed = false,
            supportedTargets = SupportedTargets,
            limitations = "This plan defines required scenarios. It is not evidence that native VM validation has been completed.",
            recipes = plans
        }, Capture.JsonOptions));
        return 0;
    }
}
