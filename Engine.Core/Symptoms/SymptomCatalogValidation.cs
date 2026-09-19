namespace KillerBench.Engine.Core;

public sealed record SymptomCatalogIssue(string Subject, string Message);

public sealed record SymptomCatalogValidationResult(IReadOnlyList<SymptomCatalogIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

/// <summary>Validates a symptom catalog on its own and, when packs and a library are supplied, against the recipes and primitives it references.</summary>
public static class SymptomCatalogValidation
{
    public static readonly IReadOnlySet<string> RecipeBranchKeys = new HashSet<string>(Enum.GetNames<RecipeRunOutcome>().Append("default"), StringComparer.Ordinal);
    public static readonly IReadOnlySet<string> ProbeBranchKeys = new HashSet<string>(Enum.GetNames<ProbeVerdict>().Append("default"), StringComparer.Ordinal);

    public static SymptomCatalogValidationResult Validate(SymptomCatalog catalog, IReadOnlyList<RecipePack>? packs = null, PrimitiveLibrary? library = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var issues = new List<SymptomCatalogIssue>();
        void Issue(string subject, string message) => issues.Add(new(subject, message));

        if (catalog.SchemaVersion != SymptomCatalog.CurrentSchemaVersion) Issue("", "Unsupported schema version " + catalog.SchemaVersion + ".");
        if (!PrimitiveLibrary.IsValidName(catalog.Id)) Issue("", "Catalog id must be a lower-case dotted identifier.");
        if (catalog.Revision < 1) Issue("", "Catalog revision must be at least 1.");
        if (string.IsNullOrWhiteSpace(catalog.Platform)) Issue("", "Catalog platform is required.");
        if (library is not null && !string.Equals(catalog.Platform, library.Platform, StringComparison.Ordinal))
            Issue("", "Catalog platform " + catalog.Platform + " does not match the library platform " + library.Platform + ".");
        if (catalog.Symptoms is null || catalog.Trees is null) { Issue("", "Symptoms and trees are required."); return new(issues); }

        var recipeIds = new HashSet<string>(StringComparer.Ordinal);
        if (packs is not null)
            foreach (var pack in packs)
            {
                if (library is not null && !string.Equals(pack.Platform, library.Platform, StringComparison.Ordinal)) Issue("", "Pack " + pack.Id + " targets " + pack.Platform + ".");
                foreach (var recipe in pack.Recipes) recipeIds.Add(recipe.Id);
            }

        var treeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tree in catalog.Trees)
        {
            if (tree is null) { Issue("", "A tree entry is null."); continue; }
            string treeId = tree.Id ?? "";
            if (!RecipePackValidation.IsValidRecipeId(treeId)) Issue(treeId, "Tree id must be a lower-case dotted identifier with at least two parts.");
            else if (!treeIds.Add(treeId)) Issue(treeId, "Duplicate tree id.");
            ValidateTree(tree, recipeIds, packs is not null, library, Issue);
        }

        var symptomIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var symptom in catalog.Symptoms)
        {
            if (symptom is null) { Issue("", "A symptom entry is null."); continue; }
            string id = symptom.Id ?? "";
            if (!RecipePackValidation.IsValidRecipeId(id)) Issue(id, "Symptom id must be a lower-case dotted identifier with at least two parts.");
            else if (!symptomIds.Add(id)) Issue(id, "Duplicate symptom id.");
            if (string.IsNullOrWhiteSpace(symptom.Title)) Issue(id, "Symptom title is required.");
            if (string.IsNullOrWhiteSpace(symptom.Area)) Issue(id, "Symptom area is required.");
            if (string.IsNullOrWhiteSpace(symptom.Description)) Issue(id, "Symptom description is required.");
            if (symptom.TreeId is not null && !treeIds.Contains(symptom.TreeId)) Issue(id, "Tree '" + symptom.TreeId + "' does not exist.");
            if (symptom.TreeId is null && string.IsNullOrWhiteSpace(symptom.Guidance)) Issue(id, "A symptom without a tree needs guidance text.");
            if (symptom.Confirm is not null) ValidateProbe(id, "confirm", symptom.Confirm, library, Issue);
            foreach (string alias in symptom.Aliases ?? [])
                if (string.IsNullOrWhiteSpace(alias) || alias.Length > 120) Issue(id, "Aliases must be short non-empty phrases.");
        }
        return new(issues.AsReadOnly());
    }

    private static void ValidateTree(TriageTree tree, IReadOnlySet<string> recipeIds, bool checkRecipes, PrimitiveLibrary? library, Action<string, string> issue)
    {
        string subject = tree.Id ?? "";
        if (tree.Nodes is null || tree.Nodes.Count == 0) { issue(subject, "Tree has no nodes."); return; }
        var nodes = new Dictionary<string, TriageNode>(StringComparer.Ordinal);
        foreach (var node in tree.Nodes)
        {
            if (node is null) { issue(subject, "A node entry is null."); continue; }
            if (string.IsNullOrWhiteSpace(node.Id) || node.Id.Length > 64 || node.Id.Any(char.IsWhiteSpace)) { issue(subject, "Node ids must be short and contain no whitespace."); continue; }
            if (!nodes.TryAdd(node.Id, node)) { issue(subject, "Duplicate node id '" + node.Id + "'."); continue; }
            string nodeSubject = subject + "/" + node.Id;
            switch (node.Kind)
            {
                case TriageNodeKind.Recipe:
                    if (string.IsNullOrWhiteSpace(node.RecipeId)) issue(nodeSubject, "Recipe nodes need a recipe id.");
                    else if (!RecipePackValidation.IsValidRecipeId(node.RecipeId)) issue(nodeSubject, "Recipe id '" + node.RecipeId + "' is malformed.");
                    else if (checkRecipes && !recipeIds.Contains(node.RecipeId)) issue(nodeSubject, "Recipe '" + node.RecipeId + "' is not in any supplied pack.");
                    foreach (string key in node.Next.Keys) if (!RecipeBranchKeys.Contains(key)) issue(nodeSubject, "Branch key '" + key + "' is not a recipe outcome.");
                    if (node.Probe is not null || node.Question is not null || node.Terminal is not null) issue(nodeSubject, "Recipe nodes carry only a recipe id.");
                    break;
                case TriageNodeKind.Probe:
                    if (node.Probe is null) issue(nodeSubject, "Probe nodes need a probe.");
                    else ValidateProbe(nodeSubject, "probe", node.Probe, library, issue);
                    foreach (string key in node.Next.Keys) if (!ProbeBranchKeys.Contains(key)) issue(nodeSubject, "Branch key '" + key + "' is not a probe verdict.");
                    if (node.RecipeId is not null || node.Question is not null || node.Terminal is not null) issue(nodeSubject, "Probe nodes carry only a probe.");
                    break;
                case TriageNodeKind.Ask:
                    if (node.Question is null || string.IsNullOrWhiteSpace(node.Question.Text) || node.Question.Options is null || node.Question.Options.Count < 2)
                        issue(nodeSubject, "Ask nodes need a question with at least two options.");
                    else
                    {
                        var keys = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var option in node.Question.Options)
                        {
                            if (option is null || string.IsNullOrWhiteSpace(option.Key) || string.IsNullOrWhiteSpace(option.Label)) { issue(nodeSubject, "Options need a key and a label."); continue; }
                            if (!keys.Add(option.Key)) issue(nodeSubject, "Duplicate option key '" + option.Key + "'.");
                            if (!node.Next.ContainsKey(option.Key) && !node.Next.ContainsKey("default")) issue(nodeSubject, "Option '" + option.Key + "' has no branch.");
                        }
                        foreach (string key in node.Next.Keys) if (key != "default" && !keys.Contains(key)) issue(nodeSubject, "Branch key '" + key + "' is not an option.");
                    }
                    if (node.RecipeId is not null || node.Probe is not null || node.Terminal is not null) issue(nodeSubject, "Ask nodes carry only a question.");
                    break;
                case TriageNodeKind.Terminal:
                    if (node.Terminal is null) issue(nodeSubject, "Terminal nodes need a terminal outcome.");
                    if (node.Next.Count > 0) issue(nodeSubject, "Terminal nodes have no branches.");
                    if (node.RecipeId is not null || node.Probe is not null || node.Question is not null) issue(nodeSubject, "Terminal nodes carry only an outcome.");
                    break;
                default:
                    issue(nodeSubject, "Unknown node kind.");
                    break;
            }
            if (node.Kind != TriageNodeKind.Terminal && node.Next.Count == 0) issue(nodeSubject, "Non-terminal nodes need at least one branch.");
        }
        if (string.IsNullOrWhiteSpace(tree.RootNodeId) || !nodes.ContainsKey(tree.RootNodeId)) { issue(subject, "Root node '" + tree.RootNodeId + "' does not exist."); return; }
        foreach (var node in nodes.Values)
            foreach (var branch in node.Next)
                if (!nodes.ContainsKey(branch.Value)) issue(subject + "/" + node.Id, "Branch '" + branch.Key + "' points to missing node '" + branch.Value + "'.");

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        stack.Push(tree.RootNodeId);
        while (stack.Count > 0)
        {
            string current = stack.Pop();
            if (!reachable.Add(current) || !nodes.TryGetValue(current, out var node)) continue;
            foreach (string next in node.Next.Values) stack.Push(next);
        }
        foreach (string id in nodes.Keys) if (!reachable.Contains(id)) issue(subject + "/" + id, "Node is unreachable from the root.");

        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string id in nodes.Keys) if (Cycle(id, nodes, state, out string path)) { issue(subject, "Cycle: " + path + "."); break; }
    }

    private static bool Cycle(string id, Dictionary<string, TriageNode> nodes, Dictionary<string, int> state, out string path)
    {
        var trail = new List<string>();
        bool found = Visit(id);
        path = found ? string.Join(" -> ", trail) : "";
        return found;

        bool Visit(string current)
        {
            if (state.TryGetValue(current, out int mark))
            {
                if (mark == 1) { trail.Add(current); return true; }
                return false;
            }
            state[current] = 1;
            trail.Add(current);
            if (nodes.TryGetValue(current, out var node))
                foreach (string next in node.Next.Values)
                    if (nodes.ContainsKey(next) && Visit(next)) return true;
            trail.RemoveAt(trail.Count - 1);
            state[current] = 2;
            return false;
        }
    }

    private static void ValidateProbe(string subject, string role, ProbeSpec probe, PrimitiveLibrary? library, Action<string, string> issue)
    {
        var pack = new RecipePack(RecipePack.CurrentSchemaVersion, "validation.probe", 1, library?.Platform ?? "any", "probe",
            [new RecipeDefinition("validation.probe", 1, "probe", "validation", "probe", probe)]);
        foreach (var packIssue in RecipePackValidation.Validate(pack, library).Issues)
            if (packIssue.Message.StartsWith("detect", StringComparison.Ordinal)) issue(subject, role + packIssue.Message["detect".Length..]);
    }
}
