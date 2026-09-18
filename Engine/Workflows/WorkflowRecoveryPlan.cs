namespace KillerBench.Engine;

public enum RecoveryNextAction { Wait, ReviewInterruptedRun, RecheckTimedOutWork, RestartThenRecheck, ElevateAndRecheck, ConfigureRepairOptions, ReviewAvailableRepairs, ReviewFindings, TestAffectedFeature }

public sealed record WorkflowRecoveryPlan(RecoveryNextAction Action, IReadOnlyList<string> AvailableRecipeIds)
{
    public static WorkflowRecoveryPlan From(WorkflowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowRecoveryPlan Gate(RecoveryNextAction action) => new(action, Array.Empty<string>());
        if (result.Status == "Running") return Gate(RecoveryNextAction.Wait);
        if (result.Status.StartsWith("Stopped", StringComparison.Ordinal) || result.Recipes.Any(recipe => recipe.ExecutionUncertain ||
            recipe.Repair is not null && recipe.Repair.Kind != recipe.Check?.Repair))
            return Gate(RecoveryNextAction.ReviewInterruptedRun);
        if (result.RequiresRestart || result.Recipes.Any(recipe => recipe.Outcome == RecipeOutcome.RestartRequired) ||
            result.Checks.Any(check => check.Name == "Restart" && check.State == CheckState.Attention))
            return Gate(RecoveryNextAction.RestartThenRecheck);
        if (result.Checks.Any(check => check.Evidence?.TimedOut == true) || result.Recipes.Any(recipe => recipe.Check?.Evidence?.TimedOut == true) ||
            result.Repairs.Any(repair => repair.Steps?.Any(step => step?.Result?.TimedOut == true) == true))
            return Gate(RecoveryNextAction.RecheckTimedOutWork);
        if (Enum.IsDefined(result.Kind) && Workflows.Scopes(result.Kind).Length > 0 &&
            result.Checks.Any(check => check.Name == "Administrator" && check.State == CheckState.Attention))
            return Gate(RecoveryNextAction.ElevateAndRecheck);
        if (Workflows.HasBlockingPrerequisite(result.Kind, result.Checks)) return Gate(RecoveryNextAction.ReviewFindings);
        if (result.Recipes.GroupBy(recipe => recipe.RecipeId, StringComparer.Ordinal).Any(group => group.Count() != 1))
            return Gate(RecoveryNextAction.ReviewFindings);
        var definitions = Enum.IsDefined(result.Kind) ? RecipeCatalog.ForWorkflow(result.Kind) : [];
        bool Eligible(RecipeResult recipe, Func<RepairRecipe, bool> selector) => recipe.Outcome == RecipeOutcome.RepairAvailable &&
            recipe.Check is { State: CheckState.Repairable, Repair: not null } check &&
            check.Evidence is not ({ TimedOut: true } or { Truncated: true }) && string.IsNullOrWhiteSpace(check.Evidence?.Error) &&
            definitions.Any(definition => definition.Id == recipe.RecipeId && definition.Version == recipe.RecipeVersion &&
                selector(definition) && definition.Action == check.Repair && definition.Dependencies.All(DependencySatisfied));
        var options = result.Recipes.Where(recipe => Eligible(recipe, definition => definition.RequiresExplicitOptions))
            .Select(recipe => recipe.RecipeId).Distinct(StringComparer.Ordinal).ToArray();
        if (options.Length > 0) return new(RecoveryNextAction.ConfigureRepairOptions, Array.AsReadOnly(options));
        var available = result.Recipes.Where(recipe => Eligible(recipe, definition => definition.CanAutoExecute))
            .Select(recipe => recipe.RecipeId).Distinct(StringComparer.Ordinal).ToArray();
        if (available.Length > 0) return new(RecoveryNextAction.ReviewAvailableRepairs, Array.AsReadOnly(available));
        return Gate(result.Summary.ReviewItems > 0 || result.Checks.Count + result.Recipes.Count == 0
            ? RecoveryNextAction.ReviewFindings : RecoveryNextAction.TestAffectedFeature);

        bool DependencySatisfied(string id)
        {
            var matches = result.Recipes.Where(recipe => recipe.RecipeId == id).ToArray();
            if (matches.Length != 1) return false;
            var dependency = matches[0];
            var definition = definitions.SingleOrDefault(recipe => recipe.Id == id);
            if (definition is null || dependency.RecipeVersion != definition.Version) return false;
            return dependency.EffectiveOutcome == RecipeOutcome.Clear || dependency.EffectiveOutcome == RecipeOutcome.Repaired &&
                dependency.Repair is not null && dependency.Repair.Kind == definition.Action &&
                dependency.Check?.Repair == dependency.Repair.Kind && Repairs.IsVerified(dependency.Repair);
        }
    }
}
