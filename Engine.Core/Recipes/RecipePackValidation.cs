using System.Text.RegularExpressions;

namespace KillerBench.Engine.Core.Recipes;

public sealed record RecipePackIssue(string RecipeId, string Message);

public sealed record RecipePackValidationResult(IReadOnlyList<RecipePackIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

/// <summary>Validates a pack against structural rules and, when a library is supplied, against the platform's whitelisted primitives.</summary>
public static class RecipePackValidation
{
    private static readonly string[] Operators = ["equals", "not-equals", "contains", "matches", "any", "absent", "present", "less-than", "greater-than"];
    private static readonly HashSet<string> OperatorSet = new(Operators, StringComparer.Ordinal);

    public static bool IsValidRecipeId(string? id) =>
        !string.IsNullOrEmpty(id) && id.Length <= 96 && id.Split('.').Length >= 2 &&
        id.Split('.').All(part => part.Length > 0 && part.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'));

    public static RecipePackValidationResult Validate(RecipePack pack, PrimitiveLibrary? library = null)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var issues = new List<RecipePackIssue>();
        void Issue(string recipe, string message) => issues.Add(new(recipe, message));

        if (pack.SchemaVersion != RecipePack.CurrentSchemaVersion) Issue("", "Unsupported schema version " + pack.SchemaVersion + ".");
        if (!IsValidRecipeId(pack.Id) && !PrimitiveLibrary.IsValidName(pack.Id)) Issue("", "Pack id must be a lower-case dotted identifier.");
        if (pack.Revision < 1) Issue("", "Pack revision must be at least 1.");
        if (string.IsNullOrWhiteSpace(pack.Platform)) Issue("", "Pack platform is required.");
        if (library is not null && !string.Equals(pack.Platform, library.Platform, StringComparison.Ordinal))
            Issue("", "Pack platform " + pack.Platform + " does not match the library platform " + library.Platform + ".");
        if (string.IsNullOrWhiteSpace(pack.Title)) Issue("", "Pack title is required.");
        if (pack.Recipes is null) { Issue("", "Pack recipes are missing."); return new(issues); }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var recipe in pack.Recipes)
        {
            if (recipe is null) { Issue("", "A recipe entry is null."); continue; }
            string id = recipe.Id ?? "";
            if (!IsValidRecipeId(id)) Issue(id, "Recipe id must be a lower-case dotted identifier with at least two parts.");
            else if (!ids.Add(id)) Issue(id, "Duplicate recipe id.");
            if (recipe.Version < 1) Issue(id, "Recipe version must be at least 1.");
            if (string.IsNullOrWhiteSpace(recipe.Title)) Issue(id, "Recipe title is required.");
            if (string.IsNullOrWhiteSpace(recipe.Area)) Issue(id, "Recipe area is required.");
            if (string.IsNullOrWhiteSpace(recipe.VerificationScope)) Issue(id, "Recipe verification scope is required.");
            if (recipe.InspectionTimeout <= TimeSpan.Zero || recipe.InspectionTimeout > TimeSpan.FromHours(2)) Issue(id, "Inspection timeout must be between 1 second and 2 hours.");
            if (recipe.ExecutionTimeout <= TimeSpan.Zero || recipe.ExecutionTimeout > TimeSpan.FromHours(6)) Issue(id, "Execution timeout must be between 1 second and 6 hours.");
            if (recipe.Detect is null) Issue(id, "Detect probe is required.");
            else ValidateProbe(id, "detect", recipe.Detect, library, Issue);
            for (int i = 0; i < (recipe.Preconditions?.Count ?? 0); i++)
                if (recipe.Preconditions![i] is null) Issue(id, "Precondition " + i + " is null.");
                else ValidateProbe(id, "precondition " + i, recipe.Preconditions[i], library, Issue);
            if (recipe.Verify is not null) ValidateProbe(id, "verify", recipe.Verify, library, Issue);
            if (recipe.Action is not null) ValidateAction(id, "action", recipe.Action, recipe, library, Issue);
            if (recipe.Rollback is not null)
            {
                if (recipe.Action is null) Issue(id, "Rollback requires an action.");
                ValidateAction(id, "rollback", recipe.Rollback, null, library, Issue);
            }
            if (recipe.Action is null && recipe.Risk != RecipeRisk.ReadOnly) Issue(id, "Diagnostic-only recipes must declare ReadOnly risk.");
            if (recipe.Action is not null && recipe.Risk == RecipeRisk.ReadOnly) Issue(id, "A recipe with an action cannot declare ReadOnly risk.");
            var optionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var option in recipe.Options ?? [])
            {
                if (option is null) { Issue(id, "An option entry is null."); continue; }
                if (!RecipeOptions.IsValidName(option.Name)) Issue(id, "Option name '" + option.Name + "' must be a short lower-case identifier.");
                else if (!optionNames.Add(option.Name)) Issue(id, "Duplicate option '" + option.Name + "'.");
                if (string.IsNullOrWhiteSpace(option.Description)) Issue(id, "Option '" + option.Name + "' needs a description.");
                if (option.Pattern is not null)
                {
                    try
                    {
                        _ = new Regex(option.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                        if (option.Default is not null && !Regex.IsMatch(option.Default, "^(?:" + option.Pattern + ")$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                            Issue(id, "Option '" + option.Name + "' has a default that does not match its pattern.");
                    }
                    catch (ArgumentException) { Issue(id, "Option '" + option.Name + "' has an invalid pattern."); }
                }
            }
            foreach (string reference in RecipeOptions.References(recipe))
                if (!optionNames.Contains(reference)) Issue(id, "Parameter reference ${" + reference + "} has no matching option declaration.");
            if ((recipe.Options?.Count ?? 0) > 0 && recipe.Options!.Any(option => option is { Required: true, Default: null }) && !recipe.RequiresExplicitOptions)
                Issue(id, "A recipe with required options must declare RequiresExplicitOptions.");
            foreach (string symptom in recipe.Symptoms ?? [])
                if (!IsValidRecipeId(symptom)) Issue(id, "Symptom reference '" + symptom + "' must be a lower-case dotted identifier.");
            foreach (string dependency in recipe.Dependencies ?? [])
            {
                if (!IsValidRecipeId(dependency)) Issue(id, "Dependency '" + dependency + "' must be a lower-case dotted identifier.");
                if (string.Equals(dependency, id, StringComparison.Ordinal)) Issue(id, "A recipe cannot depend on itself.");
            }
        }
        foreach (var recipe in pack.Recipes.Where(recipe => recipe is not null))
            foreach (string dependency in recipe.Dependencies ?? [])
                if (IsValidRecipeId(dependency) && !ids.Contains(dependency)) Issue(recipe.Id, "Dependency '" + dependency + "' is not in this pack.");
        foreach (string cycle in FindCycles([.. pack.Recipes.Where(recipe => recipe is not null && ids.Contains(recipe.Id)).DistinctBy(recipe => recipe.Id, StringComparer.Ordinal)]))
            Issue(cycle.Split(" -> ")[0], "Dependency cycle: " + cycle + ".");
        return new(issues.AsReadOnly());
    }

    private static void ValidateProbe(string id, string role, ProbeSpec probe, PrimitiveLibrary? library, Action<string, string> issue)
    {
        if (string.IsNullOrWhiteSpace(probe.Primitive)) { issue(id, role + ": primitive name is required."); return; }
        if (!PrimitiveLibrary.IsValidName(probe.Primitive)) issue(id, role + ": primitive name '" + probe.Primitive + "' is not a lower-case dotted identifier.");
        if (probe.Parameters is null) issue(id, role + ": parameters are missing.");
        if (probe.Expect is null || probe.Expect.Clauses is null || probe.Expect.Clauses.Count == 0) issue(id, role + ": at least one expect clause is required.");
        else
        {
            foreach (var clause in probe.Expect.Clauses)
            {
                if (clause is null) { issue(id, role + ": an expect clause is null."); continue; }
                if (!OperatorSet.Contains(clause.Operator ?? "")) issue(id, role + ": unknown operator '" + clause.Operator + "'.");
                if (clause.Operator == "matches")
                {
                    try { _ = new Regex(clause.Value ?? "", RegexOptions.None, TimeSpan.FromMilliseconds(100)); }
                    catch (ArgumentException) { issue(id, role + ": invalid regular expression."); }
                }
                if (clause.Operator is "less-than" or "greater-than" && !double.TryParse(clause.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                    issue(id, role + ": numeric comparison requires a numeric value.");
            }
        }
        if (library is null) return;
        var primitive = library.FindProbe(probe.Primitive);
        if (primitive is null) { issue(id, role + ": probe primitive '" + probe.Primitive + "' is not available on " + library.Platform + "."); return; }
        ValidateParameters(id, role, primitive.Parameters, probe.Parameters ?? new Dictionary<string, string>(), issue);
    }

    private static void ValidateAction(string id, string role, ActionSpec action, RecipeDefinition? recipe, PrimitiveLibrary? library, Action<string, string> issue)
    {
        if (string.IsNullOrWhiteSpace(action.Primitive)) { issue(id, role + ": primitive name is required."); return; }
        if (!PrimitiveLibrary.IsValidName(action.Primitive)) issue(id, role + ": primitive name '" + action.Primitive + "' is not a lower-case dotted identifier.");
        if (action.Parameters is null) issue(id, role + ": parameters are missing.");
        if (action.Timeout <= TimeSpan.Zero || action.Timeout > TimeSpan.FromHours(6)) issue(id, role + ": timeout must be between 1 second and 6 hours.");
        if (library is null) return;
        var primitive = library.FindAction(action.Primitive);
        if (primitive is null) { issue(id, role + ": action primitive '" + action.Primitive + "' is not available on " + library.Platform + "."); return; }
        ValidateParameters(id, role, primitive.Parameters, action.Parameters ?? new Dictionary<string, string>(), issue);
        if (recipe is not null)
        {
            if (primitive.Risk > recipe.Risk) issue(id, role + ": primitive risk " + primitive.Risk + " exceeds the declared recipe risk " + recipe.Risk + ".");
            if (primitive.RequiresElevation && !recipe.RequiresElevation) issue(id, role + ": primitive requires elevation but the recipe does not declare it.");
        }
    }

    private static void ValidateParameters(string id, string role, IReadOnlyList<PrimitiveParameter> declared, IReadOnlyDictionary<string, string> supplied, Action<string, string> issue)
    {
        foreach (var parameter in declared)
        {
            supplied.TryGetValue(parameter.Name, out string? value);
            if (parameter.Required && string.IsNullOrEmpty(value)) { issue(id, role + ": parameter '" + parameter.Name + "' is required."); continue; }
            if (value is not null && parameter.Pattern is not null && !value.Contains("${", StringComparison.Ordinal) &&
                !Regex.IsMatch(value, "^(?:" + parameter.Pattern + ")$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                issue(id, role + ": parameter '" + parameter.Name + "' does not match its allowed pattern.");
        }
        foreach (string name in supplied.Keys)
            if (!declared.Any(parameter => parameter.Name == name)) issue(id, role + ": parameter '" + name + "' is not accepted by this primitive.");
    }

    private static List<string> FindCycles(IReadOnlyList<RecipeDefinition> recipes)
    {
        var graph = recipes.ToDictionary(recipe => recipe.Id, recipe => recipe.Dependencies ?? [], StringComparer.Ordinal);
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new List<string>();
        var found = new List<string>();
        foreach (string id in graph.Keys.OrderBy(key => key, StringComparer.Ordinal)) Visit(id);
        return found;

        void Visit(string id)
        {
            if (state.TryGetValue(id, out int mark))
            {
                if (mark == 1)
                {
                    int start = stack.IndexOf(id);
                    found.Add(string.Join(" -> ", stack.Skip(start).Append(id)));
                }
                return;
            }
            state[id] = 1;
            stack.Add(id);
            if (graph.TryGetValue(id, out var dependencies))
                foreach (string dependency in dependencies) if (graph.ContainsKey(dependency)) Visit(dependency);
            stack.RemoveAt(stack.Count - 1);
            state[id] = 2;
        }
    }
}
