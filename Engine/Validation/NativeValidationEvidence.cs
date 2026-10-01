using System.Text.Json;
using System.Text.Json.Serialization;

namespace KillerBench.Engine.Validation;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NativeValidationOutcome
{
    Passed,
    Failed,
    Blocked,
    Inconclusive
}

public sealed record NativeValidationArtifact(string Kind, string Name, string Sha256);

public sealed record NativeValidationRecord(
    int SchemaVersion,
    string ScenarioId,
    string RecipeId,
    int RecipeVersion,
    NativeValidationOutcome Outcome,
    string WindowsProduct,
    string WindowsVersion,
    string WindowsBuild,
    string Architecture,
    string KillerBenchVersion,
    DateTimeOffset RecordedUtc,
    IReadOnlyList<NativeValidationArtifact> Artifacts,
    string Notes);

public sealed record NativeValidationCoverage(
    int InstalledRecipes,
    int InstalledScenarios,
    int RecordedScenarios,
    int PassedScenarios,
    int FailedScenarios,
    int BlockedScenarios,
    int InconclusiveScenarios,
    IReadOnlyList<string> MissingScenarioIds,
    IReadOnlyList<NativeValidationRecord> LatestRecords);

public static class NativeValidationEvidence
{
    public const int SchemaVersion = 1;
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumRecords = 4096;

    public static NativeValidationRecord Read(ReadOnlySpan<byte> json, DateTimeOffset? now = null)
    {
        if (json.Length is 0 or > MaximumBytes)
            throw new InvalidDataException("A native validation record must contain 1 byte through 1 MiB.");
        NativeValidationRecord record;
        try
        {
            record = JsonSerializer.Deserialize<NativeValidationRecord>(json, Capture.JsonOptions)
                ?? throw new InvalidDataException("The native validation record is empty.");
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("The native validation record is not valid JSON.", error);
        }
        Validate(record, now ?? DateTimeOffset.UtcNow);
        return record with
        {
            Artifacts = Array.AsReadOnly(record.Artifacts.ToArray()),
            Notes = record.Notes.Trim()
        };
    }

    public static NativeValidationCoverage Summarize(IEnumerable<NativeValidationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var materialized = records.Take(MaximumRecords + 1).ToArray();
        if (materialized.Length > MaximumRecords)
            throw new ArgumentException("At most 4,096 native validation records can be summarized.", nameof(records));
        foreach (var record in materialized) Validate(record, DateTimeOffset.UtcNow);

        var known = NativeValidationCatalog.All.SelectMany(plan => plan.Scenarios)
            .ToDictionary(scenario => scenario.Id, StringComparer.Ordinal);
        var latest = materialized.GroupBy(record => record.ScenarioId, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(record => record.RecordedUtc).ThenByDescending(record => record.KillerBenchVersion, StringComparer.Ordinal).First())
            .OrderBy(record => record.ScenarioId, StringComparer.Ordinal)
            .ToArray();
        var recorded = latest.Select(record => record.ScenarioId).ToHashSet(StringComparer.Ordinal);
        var missing = known.Keys.Where(id => !recorded.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        return new NativeValidationCoverage(NativeValidationCatalog.All.Count, known.Count, latest.Length,
            latest.Count(record => record.Outcome == NativeValidationOutcome.Passed),
            latest.Count(record => record.Outcome == NativeValidationOutcome.Failed),
            latest.Count(record => record.Outcome == NativeValidationOutcome.Blocked),
            latest.Count(record => record.Outcome == NativeValidationOutcome.Inconclusive),
            Array.AsReadOnly(missing), Array.AsReadOnly(latest));
    }

    private static void Validate(NativeValidationRecord record, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.SchemaVersion != SchemaVersion) throw new InvalidDataException("Unsupported native validation record schema.");
        if (!Enum.IsDefined(record.Outcome)) throw new InvalidDataException("The native validation outcome is invalid.");
        var plan = NativeValidationCatalog.Find(record.RecipeId)
            ?? throw new InvalidDataException("The native validation record names an unknown recipe.");
        if (record.RecipeVersion != plan.RecipeVersion)
            throw new InvalidDataException("The native validation record does not match the installed recipe version.");
        var scenario = plan.Scenarios.FirstOrDefault(candidate => string.Equals(candidate.Id, record.ScenarioId, StringComparison.Ordinal))
            ?? throw new InvalidDataException("The native validation record names an unknown scenario for its recipe.");
        RequireText(record.WindowsProduct, 100, "Windows product");
        RequireText(record.WindowsVersion, 100, "Windows version");
        RequireText(record.WindowsBuild, 50, "Windows build");
        RequireText(record.Architecture, 30, "Architecture");
        RequireText(record.KillerBenchVersion, 50, "KillerBench version");
        if (record.RecordedUtc == default || record.RecordedUtc > now.AddMinutes(5))
            throw new InvalidDataException("The native validation time is missing or in the future.");
        if (record.Notes is null || record.Notes.Length > 2000)
            throw new InvalidDataException("Native validation notes must be at most 2,000 characters.");
        if (record.Artifacts is null || record.Artifacts.Count is 0 or > 32)
            throw new InvalidDataException("A native validation record requires 1 through 32 artifacts.");

        var artifacts = new Dictionary<string, NativeValidationArtifact>(StringComparer.Ordinal);
        foreach (var artifact in record.Artifacts)
        {
            if (artifact is null) throw new InvalidDataException("A native validation artifact is missing.");
            RequireText(artifact.Kind, 100, "Artifact kind");
            RequireText(artifact.Name, 180, "Artifact name");
            if (Path.IsPathRooted(artifact.Name) || Path.GetFileName(artifact.Name) != artifact.Name)
                throw new InvalidDataException("Artifact names must be filenames without a path.");
            if (artifact.Sha256 is null || artifact.Sha256.Length != 64 || artifact.Sha256.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidDataException("Artifact SHA-256 values must contain exactly 64 hexadecimal characters.");
            if (!artifacts.TryAdd(artifact.Kind, artifact))
                throw new InvalidDataException("Artifact kinds must be unique within one native validation record.");
        }
        foreach (string required in scenario.RequiredArtifacts)
            if (!artifacts.ContainsKey(required))
                throw new InvalidDataException("The native validation record is missing required artifact kind: " + required);
    }

    private static void RequireText(string? value, int maximum, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(char.IsControl))
            throw new InvalidDataException(label + " is missing or invalid.");
    }
}
