using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>
/// Builds the Windows service recipe pack from the Engine's recipe catalog. Every catalog recipe whose plan is
/// "sc.exe start" followed by "sc.exe query" becomes one declarative recipe with the same id, version, title,
/// verification scope and dependencies, so the pack and the bespoke implementation stay in lockstep.
/// </summary>
public static class WindowsServicePack
{
    public const string PackId = "windows.services";

    /// <summary>Catalog recipes that are plain service starts and therefore expressible as pack recipes.</summary>
    public static IReadOnlyList<RepairRecipe> SourceRecipes { get; } = Array.AsReadOnly(RecipeCatalog.All
        .Where(recipe => !recipe.DiagnosticOnly && !recipe.RequiresExplicitOptions && IsServiceStart(recipe.Action))
        .ToArray());

    public static bool IsServiceStart(RepairKind kind)
    {
        if (ServiceRecovery.ServiceName(kind) is not { } service) return false;
        var steps = Repairs.Plan(kind).Steps;
        return steps.Count == 2 &&
            steps[0] is { Tool: "sc.exe", Verification: false, Arguments: ["start", var startName] } && startName == service &&
            steps[1] is { Tool: "sc.exe", Verification: true, Arguments: ["query", var queryName] } && queryName == service;
    }

    public static RecipePack Build()
    {
        var included = new HashSet<string>(SourceRecipes.Select(recipe => recipe.Id), StringComparer.Ordinal);
        var recipes = SourceRecipes.Select(recipe => Convert(recipe, included)).ToArray();
        return new RecipePack(RecipePack.CurrentSchemaVersion, PackId, 1, WindowsPrimitiveLibrary.Platform,
            "Windows service availability", recipes);
    }

    public static RecipeDefinition Convert(RepairRecipe recipe, IReadOnlySet<string> included)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(included);
        if (!IsServiceStart(recipe.Action)) throw new ArgumentException("Only plain service-start recipes convert to the service pack.", nameof(recipe));
        string service = ServiceRecovery.ServiceName(recipe.Action)!;
        var target = new Dictionary<string, string> { ["name"] = service };
        var expect = new ExpectSpec(
        [
            new ExpectClause("equals", nameof(CheckState.Clear), ProbeVerdict.Clear),
            new ExpectClause("equals", nameof(CheckState.Repairable), ProbeVerdict.Repairable),
            new ExpectClause("equals", nameof(CheckState.Attention), ProbeVerdict.Attention),
            new ExpectClause("equals", nameof(CheckState.Information), ProbeVerdict.Information),
            new ExpectClause("any", "", ProbeVerdict.Unknown)
        ]);
        var plan = Repairs.Plan(recipe.Action);
        return new RecipeDefinition(recipe.Id, recipe.Version, recipe.Title, recipe.Area, recipe.VerificationScope,
            new ProbeSpec("service.check", target, expect) { Name = "Service state and dependencies" })
        {
            Dependencies = recipe.Dependencies.Where(included.Contains).ToArray(),
            Action = new ActionSpec("service.start", target) { Timeout = TimeSpan.FromSeconds(20), Effects = plan.Effects },
            Verify = new ProbeSpec("service.running", target, ExpectSpec.ClearWhenEquals("Running", ProbeVerdict.Attention)) { Name = "Service stays running" },
            Risk = RecipeRisk.Reversible,
            RequiresElevation = true,
            InspectionTimeout = recipe.InspectionTimeout,
            ExecutionTimeout = recipe.ExecutionTimeout,
            Limitations = "Verification covers the service running state only; feature operation is not proven."
        };
    }
}
