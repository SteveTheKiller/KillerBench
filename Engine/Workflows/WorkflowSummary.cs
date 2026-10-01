namespace KillerBench.Engine.Workflows;

public sealed record WorkflowSummary(int ClearChecks, int VerifiedRepairs, int AvailableRepairs,
    int NeedsAttention, int Incomplete, int Deferred, int Informational)
{
    public int ReviewItems => AvailableRepairs + NeedsAttention + Incomplete + Deferred;

    public static WorkflowSummary From(WorkflowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        int clear = 0, verified = 0, available = 0, attention = 0, incomplete = 0, deferred = 0, information = 0;
        foreach (var check in result.Checks.Where(check => !result.Recipes.Any(recipe => Equals(recipe.Check, check) || recipe.RecipeId == check.Name)))
        {
            switch (check.EffectiveState)
            {
                case CheckState.Clear: clear++; break;
                case CheckState.Information: information++; break;
                case CheckState.Unknown: incomplete++; break;
                default: attention++; break;
            }
        }
        foreach (var recipe in result.Recipes)
        {
            if (recipe.ExecutionUncertain) { incomplete++; continue; }
            switch (recipe.EffectiveOutcome)
            {
                case RecipeOutcome.Clear: clear++; break;
                case RecipeOutcome.Repaired when recipe.Repair is not null && recipe.Check?.Repair == recipe.Repair.Kind && Repairs.IsVerified(recipe.Repair): verified++; break;
                case RecipeOutcome.RepairAvailable: available++; break;
                case RecipeOutcome.Blocked: deferred++; break;
                case RecipeOutcome.Unknown:
                case RecipeOutcome.Canceled:
                case RecipeOutcome.RestartRequired:
                case RecipeOutcome.Repaired: incomplete++; break;
                default: attention++; break;
            }
        }
        return new(clear, verified, available, attention, incomplete, deferred, information);
    }
}
