namespace KillerBench.Engine.Workflows;

/// <summary>Worst state across a session, used to color the Overview gauge.</summary>
public enum OverviewTone { NotChecked, Clear, Incomplete, Review, Fixable }

/// <summary>
/// The two Overview numbers: how much of the machine was actually read, and how many findings need a look.
/// They stay separate on purpose. Unread evidence lowers coverage and is never folded into a health figure.
/// </summary>
public sealed record OverviewScore(int Total, int Unknown, int NeedsAttention, int Fixable)
{
    public int Ran => Total - Unknown;

    // Rounded down so a single unread check can never show as 100%.
    public int CheckedPercent => Total == 0 ? 0 : Ran * 100 / Total;

    public OverviewTone Tone =>
        Total == 0 ? OverviewTone.NotChecked
        : Fixable > 0 ? OverviewTone.Fixable
        : NeedsAttention > 0 ? OverviewTone.Review
        : Unknown > 0 ? OverviewTone.Incomplete
        : OverviewTone.Clear;

    public static OverviewScore From(IEnumerable<WorkflowResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        int total = 0, unknown = 0, attention = 0, fixable = 0;
        foreach (var result in results)
        {
            var summary = result.Summary;
            total += summary.ClearChecks + summary.VerifiedRepairs + summary.AvailableRepairs + summary.NeedsAttention +
                summary.Incomplete + summary.Deferred + summary.Informational;
            unknown += summary.Incomplete;
            attention += summary.AvailableRepairs + summary.NeedsAttention + summary.Deferred;
            // The summary files repairable checks under attention; count them as fixable as well.
            fixable += summary.AvailableRepairs + result.Checks.Count(check =>
                check.EffectiveState == CheckState.Repairable &&
                !result.Recipes.Any(recipe => Equals(recipe.Check, check) || recipe.RecipeId == check.Name));
        }
        return new(total, unknown, attention, fixable);
    }
}
