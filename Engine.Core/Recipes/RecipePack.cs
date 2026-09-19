namespace KillerBench.Engine.Core;

/// <summary>Outcome of one recipe run. Mirrors the Engine's RecipeOutcome so results can be mapped without loss.</summary>
public enum RecipeRunOutcome { Clear, RepairAvailable, Repaired, NeedsAttention, Unknown, Blocked, Canceled, RestartRequired }

/// <summary>Result of one probe evaluation.</summary>
public enum ProbeVerdict { Clear, Repairable, Attention, Unknown, Information }

/// <summary>How much a recipe changes the machine. Drives consent and journaling requirements.</summary>
public enum RecipeRisk { ReadOnly, Reversible, Persistent, Destructive }

/// <summary>A versioned catalog of declarative recipes for one platform.</summary>
public sealed record RecipePack(
    int SchemaVersion,
    string Id,
    int Revision,
    string Platform,
    string Title,
    IReadOnlyList<RecipeDefinition> Recipes)
{
    public const int CurrentSchemaVersion = 1;
}

/// <summary>One declarative diagnose, repair and verify recipe.</summary>
public sealed record RecipeDefinition(
    string Id,
    int Version,
    string Title,
    string Area,
    string VerificationScope,
    ProbeSpec Detect)
{
    /// <summary>Symptom identifiers this recipe can explain or resolve.</summary>
    public IReadOnlyList<string> Symptoms { get; init; } = [];
    /// <summary>Recipe identifiers that must be Clear or Repaired before this recipe runs.</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    /// <summary>Probes that must all be Clear before the action may run.</summary>
    public IReadOnlyList<ProbeSpec> Preconditions { get; init; } = [];
    /// <summary>The repair action. Null means the recipe is diagnostic only.</summary>
    public ActionSpec? Action { get; init; }
    /// <summary>Probe evaluated after the action. Null falls back to Detect.</summary>
    public ProbeSpec? Verify { get; init; }
    /// <summary>Action that undoes the repair when a real undo exists. Null means no rollback is claimed.</summary>
    public ActionSpec? Rollback { get; init; }
    public RecipeRisk Risk { get; init; } = RecipeRisk.ReadOnly;
    public bool RequiresElevation { get; init; }
    public bool RequiresExplicitOptions { get; init; }
    public bool MayRequireRestart { get; init; }
    public TimeSpan InspectionTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan ExecutionTimeout { get; init; } = TimeSpan.FromMinutes(30);
    /// <summary>What the recipe explicitly does not prove, shown with every result.</summary>
    public string Limitations { get; init; } = "";

    public bool DiagnosticOnly => Action is null;
}

/// <summary>A read-only observation made through a named primitive with parameters, and how its result maps to a verdict.</summary>
public sealed record ProbeSpec(string Primitive, IReadOnlyDictionary<string, string> Parameters, ExpectSpec Expect)
{
    public string? Name { get; init; }
}

/// <summary>Maps a probe's observed value to a verdict. Exactly one clause matches; unmatched values are Unknown.</summary>
public sealed record ExpectSpec(IReadOnlyList<ExpectClause> Clauses)
{
    public static ExpectSpec ClearWhenEquals(string clearValue, ProbeVerdict otherwise = ProbeVerdict.Repairable) =>
        new([new ExpectClause("equals", clearValue, ProbeVerdict.Clear), new ExpectClause("any", "", otherwise)]);
}

/// <summary>One matching clause: Operator is equals, not-equals, contains, matches (regex), any, absent, present, less-than or greater-than.</summary>
public sealed record ExpectClause(string Operator, string Value, ProbeVerdict Verdict)
{
    public string? Detail { get; init; }
}

/// <summary>A machine-changing step performed through a named primitive with parameters.</summary>
public sealed record ActionSpec(string Primitive, IReadOnlyDictionary<string, string> Parameters)
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public string Effects { get; init; } = "";
}
