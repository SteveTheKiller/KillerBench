namespace KillerBench.Engine.Core.Symptoms;

/// <summary>A user-facing problem statement, the entry point for triage. Ids are lower-case dotted identifiers such as printing.nothing-prints.</summary>
public sealed record Symptom(string Id, string Title, string Area, string Description)
{
    /// <summary>Phrases a person might type; used for search only.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];
    /// <summary>Optional probe that confirms the symptom is present before triage and absent afterwards. Null means resolution is inferred from the tree.</summary>
    public ProbeSpec? Confirm { get; init; }
    /// <summary>Id of the triage tree for this symptom. Null means the catalog lists the symptom but has no runnable triage yet.</summary>
    public string? TreeId { get; init; }
    /// <summary>Plain-language guidance when no tree exists or triage ends without resolution.</summary>
    public string Guidance { get; init; } = "";
}

public enum TriageNodeKind { Recipe, Probe, Ask, Terminal }

public enum TriageTerminal { Resolved, RuledOut, NeedsHuman, OutOfScope }

/// <summary>One node in a triage tree. Next maps a branch key to the next node id; keys are recipe outcomes, probe verdicts or answer keys.</summary>
public sealed record TriageNode(string Id, TriageNodeKind Kind)
{
    public string? RecipeId { get; init; }
    public ProbeSpec? Probe { get; init; }
    public TriageQuestion? Question { get; init; }
    public TriageTerminal? Terminal { get; init; }
    /// <summary>Branches by key. A missing key falls back to the "default" key when present; otherwise the walk ends as NeedsHuman.</summary>
    public IReadOnlyDictionary<string, string> Next { get; init; } = new Dictionary<string, string>();
    /// <summary>Shown to the person when this node is reached, for example why a step is being tried or what to do next.</summary>
    public string Guidance { get; init; } = "";
}

public sealed record TriageQuestion(string Text, IReadOnlyList<TriageOption> Options);

public sealed record TriageOption(string Key, string Label);

/// <summary>An acyclic decision graph from a root node to terminals. The runner walks it, running recipes and probes, and asking questions when an answer source is available.</summary>
public sealed record TriageTree(string Id, string RootNodeId, IReadOnlyList<TriageNode> Nodes)
{
    public string Title { get; init; } = "";
}

/// <summary>A versioned catalog of symptoms and triage trees for one platform.</summary>
public sealed record SymptomCatalog(int SchemaVersion, string Id, int Revision, string Platform, string Title,
    IReadOnlyList<Symptom> Symptoms, IReadOnlyList<TriageTree> Trees)
{
    public const int CurrentSchemaVersion = 1;

    public Symptom? FindSymptom(string id) => Symptoms.FirstOrDefault(symptom => string.Equals(symptom.Id, id, StringComparison.Ordinal));
    public TriageTree? FindTree(string id) => Trees.FirstOrDefault(tree => string.Equals(tree.Id, id, StringComparison.Ordinal));

    /// <summary>Case-insensitive search over ids, titles, descriptions and aliases. Whole-word phrase matches rank above partial matches.</summary>
    public IReadOnlyList<Symptom> Search(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        string needle = query.Trim();
        return [.. Symptoms
            .Select(symptom => (symptom, score: Score(symptom, needle)))
            .Where(pair => pair.score > 0)
            .OrderByDescending(pair => pair.score).ThenBy(pair => pair.symptom.Id, StringComparer.Ordinal)
            .Take(Math.Max(1, limit)).Select(pair => pair.symptom)];
    }

    private static int Score(Symptom symptom, string needle)
    {
        int score = 0;
        if (symptom.Id.Equals(needle, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (symptom.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)) score += 40;
        if (symptom.Aliases.Any(alias => alias.Equals(needle, StringComparison.OrdinalIgnoreCase))) score += 60;
        if (symptom.Aliases.Any(alias => alias.Contains(needle, StringComparison.OrdinalIgnoreCase))) score += 20;
        if (symptom.Description.Contains(needle, StringComparison.OrdinalIgnoreCase)) score += 10;
        foreach (string word in needle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (word.Length >= 3 && (symptom.Title.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                symptom.Aliases.Any(alias => alias.Contains(word, StringComparison.OrdinalIgnoreCase)))) score += 3;
        return score;
    }
}
