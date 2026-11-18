using KillerBench.Engine.Workflows;

internal static class OverviewScoreChecks
{
    private static readonly DateTimeOffset Started = DateTimeOffset.Parse("2026-11-18T12:00:00Z");

    internal static void Run()
    {
        var none = OverviewScore.From([]);
        Require(none.Tone == OverviewTone.NotChecked && none.CheckedPercent == 0 && none.Total == 0,
            "An empty session must read as not checked, never as clear.");

        var clean = OverviewScore.From([Result(Check(CheckState.Clear), Check(CheckState.Information))]);
        Require(clean.Tone == OverviewTone.Clear && clean.CheckedPercent == 100 && clean.NeedsAttention == 0,
            "Fully read evidence with no findings must report every check ran and nothing to review.");

        var blocked = OverviewScore.From([Result(Check(CheckState.Clear), Check(CheckState.Unknown), Check(CheckState.Unknown))]);
        Require(blocked.Tone == OverviewTone.Incomplete && blocked.CheckedPercent == 33 && blocked.Unknown == 2,
            "Unknown checks must lower coverage and keep the gauge out of the clear state.");

        var mixed = OverviewScore.From([
            Result(Check(CheckState.Attention), Check(CheckState.Clear)),
            Result(Check(CheckState.Repairable), Check(CheckState.Unknown))]);
        Require(mixed.Tone == OverviewTone.Fixable && mixed.Fixable == 1 && mixed.NeedsAttention == 2 && mixed.CheckedPercent == 75,
            "A repairable finding must set the fixable tone and still count toward needs attention.");

        var nearly = OverviewScore.From([Result([.. Enumerable.Repeat(Check(CheckState.Clear), 199), Check(CheckState.Unknown)])]);
        Require(nearly.CheckedPercent == 99, "Coverage must round down so one unread check never shows 100%.");

        var recipes = OverviewScore.From([Result([]) with
        {
            Recipes = [Recipe(RecipeOutcome.RepairAvailable), Recipe(RecipeOutcome.Blocked), Recipe(RecipeOutcome.Clear)]
        }]);
        Require(recipes.Fixable == 1 && recipes.NeedsAttention == 2 && recipes.Tone == OverviewTone.Fixable,
            "Recipe outcomes must feed the same two numbers as plain checks.");

        Console.WriteLine("PASS: overview score keeps coverage and findings separate.");
    }

    private static WorkflowCheck Check(CheckState state) => new("Fixture check", state, "Fixture detail");

    private static WorkflowResult Result(params WorkflowCheck[] checks) =>
        new(WorkflowKind.Vitals, "FIXTURE-PC", Started, "Completed", checks, [], "");

    private static RecipeResult Recipe(RecipeOutcome outcome) =>
        new("fixture-" + outcome, 1, outcome, null, null, "Fixture recipe");

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
