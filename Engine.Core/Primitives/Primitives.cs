namespace KillerBench.Engine.Core;

/// <summary>Raw evidence returned by a primitive. Value is the observation the expectation is matched against; Raw is retained verbatim.</summary>
public sealed record PrimitiveResult(bool Complete, string? Value, string Raw, string? Error = null)
{
    public bool TimedOut { get; init; }
    public bool Truncated { get; init; }
    public static PrimitiveResult Unavailable(string error) => new(false, null, "", error);
}

/// <summary>Result of a machine-changing primitive.</summary>
public sealed record ActionResult(bool Dispatched, bool Succeeded, string Raw, string? Error = null)
{
    public bool RestartRequired { get; init; }
    public bool TimedOut { get; init; }
}

/// <summary>Describes one parameter a primitive accepts, for validation before any execution.</summary>
public sealed record PrimitiveParameter(string Name, bool Required, string Description)
{
    /// <summary>Regular expression the value must match. Null accepts any non-empty value.</summary>
    public string? Pattern { get; init; }
}

/// <summary>A read-only observation primitive such as service state, registry value, file presence or a bounded command.</summary>
public interface IProbePrimitive
{
    string Name { get; }
    IReadOnlyList<PrimitiveParameter> Parameters { get; }
    Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token);
}

/// <summary>A machine-changing primitive. Implementations must be bounded, must not retry on their own and must report whether the change was dispatched.</summary>
public interface IActionPrimitive
{
    string Name { get; }
    IReadOnlyList<PrimitiveParameter> Parameters { get; }
    RecipeRisk Risk { get; }
    bool RequiresElevation { get; }
    Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token);
}

/// <summary>The whitelisted primitives one platform provides. Packs may only reference primitives present here.</summary>
public sealed class PrimitiveLibrary
{
    private readonly Dictionary<string, IProbePrimitive> probes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IActionPrimitive> actions = new(StringComparer.Ordinal);

    public PrimitiveLibrary(string platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        Platform = platform;
    }

    public string Platform { get; }
    public IReadOnlyCollection<IProbePrimitive> Probes => probes.Values;
    public IReadOnlyCollection<IActionPrimitive> Actions => actions.Values;

    public PrimitiveLibrary AddProbe(IProbePrimitive probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ValidateName(probe.Name);
        if (!probes.TryAdd(probe.Name, probe)) throw new ArgumentException("A probe primitive named " + probe.Name + " is already registered.", nameof(probe));
        return this;
    }

    public PrimitiveLibrary AddAction(IActionPrimitive action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ValidateName(action.Name);
        if (!actions.TryAdd(action.Name, action)) throw new ArgumentException("An action primitive named " + action.Name + " is already registered.", nameof(action));
        return this;
    }

    public IProbePrimitive? FindProbe(string name) => probes.GetValueOrDefault(name);
    public IActionPrimitive? FindAction(string name) => actions.GetValueOrDefault(name);

    /// <summary>Primitive names are lower-case dotted identifiers such as service.state.</summary>
    public static bool IsValidName(string name) =>
        !string.IsNullOrEmpty(name) && name.Length <= 64 &&
        name.Split('.').All(part => part.Length > 0 && part.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'));

    private static void ValidateName(string name)
    {
        if (!IsValidName(name)) throw new ArgumentException("Primitive names are lower-case dotted identifiers.", nameof(name));
    }
}
