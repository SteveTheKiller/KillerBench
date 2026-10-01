using System.Text.RegularExpressions;

namespace KillerBench.Engine.Core.Recipes;

/// <summary>Validates caller-supplied option values against a recipe's declarations and substitutes ${name} references in probe and action parameters.</summary>
public static partial class RecipeOptions
{
    [GeneratedRegex(@"\$\{([a-z][a-z0-9-]*)\}", RegexOptions.CultureInvariant)]
    private static partial Regex Reference();

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial Regex NamePattern();

    public static bool IsValidName(string? name) => !string.IsNullOrEmpty(name) && name.Length <= 32 && NamePattern().IsMatch(name);

    /// <summary>Option names referenced anywhere in the recipe's parameters.</summary>
    public static IReadOnlySet<string> References(RecipeDefinition recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameters in AllParameters(recipe))
            foreach (string value in parameters.Values)
                foreach (Match match in Reference().Matches(value ?? "")) names.Add(match.Groups[1].Value);
        foreach (var probe in new[] { recipe.Detect, recipe.Verify }.Concat(recipe.Preconditions ?? []))
            foreach (var clause in probe?.Expect?.Clauses ?? [])
                foreach (Match match in Reference().Matches(clause?.Value ?? "")) names.Add(match.Groups[1].Value);
        return names;
    }

    /// <summary>Resolves the effective values: defaults first, then supplied values. Returns the problems found; an empty list means every referenced option is satisfied.</summary>
    public static IReadOnlyList<string> Resolve(RecipeDefinition recipe, IReadOnlyDictionary<string, string>? supplied, out IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var problems = new List<string>();
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        var declared = recipe.Options.ToDictionary(option => option.Name, StringComparer.Ordinal);
        foreach (var option in recipe.Options)
        {
            string? value = supplied is not null && supplied.TryGetValue(option.Name, out string? given) ? given : option.Default;
            if (string.IsNullOrEmpty(value))
            {
                if (option.Required) problems.Add("Option '" + option.Name + "' is required: " + option.Description);
                continue;
            }
            if (value.Length > 512 || value.Any(char.IsControl)) { problems.Add("Option '" + option.Name + "' is too long or contains control characters."); continue; }
            if (option.Pattern is not null && !Regex.IsMatch(value, "^(?:" + option.Pattern + ")$", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            { problems.Add("Option '" + option.Name + "' does not match its allowed pattern."); continue; }
            resolved[option.Name] = value;
        }
        if (supplied is not null)
            foreach (string name in supplied.Keys)
                if (!declared.ContainsKey(name)) problems.Add("Option '" + name + "' is not declared by this recipe.");
        foreach (string reference in References(recipe))
            if (!declared.ContainsKey(reference)) problems.Add("Parameter reference ${" + reference + "} has no matching option declaration.");
        values = resolved;
        return problems.AsReadOnly();
    }

    /// <summary>Returns a copy of the recipe with every ${name} in its parameters replaced by the resolved value. Unresolved references are left untouched and must have been reported by Resolve.</summary>
    public static RecipeDefinition Apply(RecipeDefinition recipe, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0 && References(recipe).Count == 0) return recipe;
        return recipe with
        {
            Detect = Substitute(recipe.Detect, values),
            Preconditions = [.. recipe.Preconditions.Select(probe => Substitute(probe, values))],
            Verify = recipe.Verify is null ? null : Substitute(recipe.Verify, values),
            Action = recipe.Action is null ? null : Substitute(recipe.Action, values),
            Rollback = recipe.Rollback is null ? null : Substitute(recipe.Rollback, values)
        };
    }

    private static ProbeSpec Substitute(ProbeSpec probe, IReadOnlyDictionary<string, string> values) =>
        probe with
        {
            Parameters = Substitute(probe.Parameters, values),
            Expect = new ExpectSpec([.. probe.Expect.Clauses.Select(clause => clause with { Value = Replace(clause.Value, values) })])
        };

    private static string Replace(string? text, IReadOnlyDictionary<string, string> values) =>
        Reference().Replace(text ?? "", match => values.TryGetValue(match.Groups[1].Value, out string? value) ? value : match.Value);

    private static ActionSpec Substitute(ActionSpec action, IReadOnlyDictionary<string, string> values) =>
        action with { Parameters = Substitute(action.Parameters, values) };

    private static Dictionary<string, string> Substitute(IReadOnlyDictionary<string, string> parameters, IReadOnlyDictionary<string, string> values) =>
        parameters.ToDictionary(pair => pair.Key, pair => Reference().Replace(pair.Value ?? "", match => values.TryGetValue(match.Groups[1].Value, out string? value) ? value : match.Value), StringComparer.Ordinal);

    private static IEnumerable<IReadOnlyDictionary<string, string>> AllParameters(RecipeDefinition recipe)
    {
        if (recipe.Detect?.Parameters is not null) yield return recipe.Detect.Parameters;
        foreach (var probe in recipe.Preconditions ?? []) if (probe?.Parameters is not null) yield return probe.Parameters;
        if (recipe.Verify?.Parameters is not null) yield return recipe.Verify.Parameters;
        if (recipe.Action?.Parameters is not null) yield return recipe.Action.Parameters;
        if (recipe.Rollback?.Parameters is not null) yield return recipe.Rollback.Parameters;
    }
}
