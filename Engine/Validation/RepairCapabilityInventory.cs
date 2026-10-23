using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using KillerBench.Engine.Core.Recipes;

namespace KillerBench.Engine.Validation;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NativeCapabilityStatus { NotYetVerified, StaleEvidence, IncompleteEvidence, LabVerified, DiagnosticOnly }

public sealed record ExplicitRepairCapability(string Id, int Version, string Title, string VerificationScope,
    IReadOnlyList<string> EntryPoints);

public sealed record RepairCapabilityImplementation(string Key, int Version, string DefinitionSha256,
    string ImplementationSha256, string VerificationScope, string Prerequisites, string RecoveryContract,
    IReadOnlyList<string> EntryPoints, IReadOnlyList<NativeValidationScenario> Scenarios)
{
    public NativeCapabilityStatus Status { get; init; } = NativeCapabilityStatus.NotYetVerified;
    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];
    public string Label => RepairCapabilityInventory.Label(Status);
}

public sealed record RepairCapability(string Id, string Title, IReadOnlyList<RepairCapabilityImplementation> Implementations)
{
    public NativeCapabilityStatus Status => Implementations.All(item => item.Status == NativeCapabilityStatus.DiagnosticOnly)
        ? NativeCapabilityStatus.DiagnosticOnly : Implementations.All(item => item.Status is NativeCapabilityStatus.LabVerified or NativeCapabilityStatus.DiagnosticOnly)
        ? NativeCapabilityStatus.LabVerified : Implementations.Any(item => item.Status == NativeCapabilityStatus.IncompleteEvidence)
        ? NativeCapabilityStatus.IncompleteEvidence : Implementations.Any(item => item.Status == NativeCapabilityStatus.StaleEvidence)
        ? NativeCapabilityStatus.StaleEvidence : NativeCapabilityStatus.NotYetVerified;
    public string Label => RepairCapabilityInventory.Label(Status);
}

/// <summary>Unifies public repair identities while retaining independent implementations and native evidence requirements.</summary>
public static class RepairCapabilityInventory
{
    private static readonly JsonSerializerOptions RecordJsonOptions = new(Capture.JsonOptions)
    { AllowDuplicateProperties = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    public static string Label(NativeCapabilityStatus status) => status switch
    {
        NativeCapabilityStatus.LabVerified => "Lab verified for the recorded scenarios",
        NativeCapabilityStatus.StaleEvidence => "Not yet verified: native evidence is stale",
        NativeCapabilityStatus.IncompleteEvidence => "Not yet verified: native evidence is incomplete or failed",
        NativeCapabilityStatus.DiagnosticOnly => "Diagnostic only; no repair is dispatched",
        _ => "Not yet verified in the native lab"
    };

    public static string Fingerprint<T>(T definition) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(definition, Capture.JsonOptions)));

    public static IReadOnlyList<RepairCapability> Build(IReadOnlyList<RecipePack> packs, string implementationSha256,
        IReadOnlyList<ExplicitRepairCapability>? explicitRepairs = null, string? evidenceDirectory = null,
        IReadOnlyList<string>? evidenceFiles = null)
    {
        ArgumentNullException.ThrowIfNull(packs);
        if (!IsSha256(implementationSha256)) throw new ArgumentException("An exact implementation SHA-256 is required.", nameof(implementationSha256));
        var evidence = LoadEvidence(evidenceDirectory, evidenceFiles);
        var entries = new List<(string Id, string Title, RepairCapabilityImplementation Implementation)>();
        foreach (var recipe in RecipeCatalog.All)
        {
            var plan = NativeValidationCatalog.Find(recipe.Id)!;
            Add(recipe.Id, recipe.Title, new("legacy/" + recipe.Id, recipe.Version, Fingerprint(recipe), implementationSha256,
                recipe.VerificationScope, "Fresh recipe eligibility, dependency and explicit-option checks before dispatch.", recipe.RecoveryContract.ToString(),
                ["workflow", "desktop/workflow", "repair-kind/" + recipe.Action], plan.Scenarios)
                { Status = recipe.DiagnosticOnly ? NativeCapabilityStatus.DiagnosticOnly : NativeCapabilityStatus.NotYetVerified });
        }
        foreach (var pack in packs)
            foreach (var recipe in pack.Recipes)
                Add(recipe.Id, recipe.Title, new("pack/" + pack.Id + "/" + recipe.Id, recipe.Version,
                    Fingerprint(new { pack.Id, pack.Revision, Recipe = recipe }), implementationSha256, recipe.VerificationScope,
                    string.Join("; ", recipe.Preconditions.Select(probe => probe.Name ?? probe.Primitive)),
                    "Durable pending operation; read-only verification without redispatch" + (recipe.MayRequireRestart ? "; operation-specific postboot evidence required" : ""),
                    ["symptom", "desktop/symptom", "pack/" + pack.Id], Scenarios(recipe.Id, recipe.RequiresExplicitOptions, recipe.MayRequireRestart, recipe.DiagnosticOnly))
                    { Status = recipe.DiagnosticOnly ? NativeCapabilityStatus.DiagnosticOnly : NativeCapabilityStatus.NotYetVerified });
        foreach (var repair in explicitRepairs ?? [])
            Add(repair.Id, repair.Title, new("explicit/" + repair.Id, repair.Version, Fingerprint(repair), implementationSha256,
                repair.VerificationScope, "Exact selected target, fresh provider eligibility and command-specific confirmation.",
                "Guarded execution journal and command-specific reconciliation; no implicit retry.", repair.EntryPoints, Scenarios(repair.Id, true, true)));
        return Array.AsReadOnly(entries.GroupBy(item => item.Id, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new RepairCapability(group.Key, group.First().Title, Array.AsReadOnly(group.Select(item => item.Implementation).ToArray()))).ToArray());

        void Add(string id, string title, RepairCapabilityImplementation implementation) =>
            entries.Add((id, title, Evaluate(id, implementation, evidence)));
    }

    private static IReadOnlyList<NativeValidationScenario> Scenarios(string id, bool explicitOptions, bool restart, bool diagnosticOnly = false) =>
        Array.AsReadOnly(Enum.GetValues<NativeValidationScenarioKind>()
            .Where(kind => (!diagnosticOnly || kind is NativeValidationScenarioKind.Healthy or NativeValidationScenarioKind.IncompleteEvidence or NativeValidationScenarioKind.ProviderFailure or NativeValidationScenarioKind.Cancellation) &&
                (explicitOptions || kind is not (NativeValidationScenarioKind.InvalidOptions or NativeValidationScenarioKind.ChangedOptions)) &&
                (restart || kind != NativeValidationScenarioKind.RestartRequired))
            .Select(kind => new NativeValidationScenario(id + "." + NativeValidationCatalog.ScenarioSuffix(kind), kind, kind.ToString(),
                "Verify exact target and unselected-state preservation, truthful outcome, durable recovery and successful lab restoration.",
                kind is NativeValidationScenarioKind.VerifiedExecution or NativeValidationScenarioKind.VerificationFailure or NativeValidationScenarioKind.InterruptionAfterDispatch or NativeValidationScenarioKind.RestartRequired,
                ["before snapshot", "native result", "after snapshot", "workflow journal", "restoration evidence"])).ToArray());

    public static NativeValidationRecord ReadRecord(string path, IReadOnlyList<RepairCapability> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var info = new FileInfo(path);
        if (!info.Exists || info.Length is 0 or > NativeValidationEvidence.MaximumBytes || (info.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("A native record must be an existing non-redirected file from 1 byte through 1 MiB.");
        NativeValidationRecord record;
        try
        {
            record = JsonSerializer.Deserialize<NativeValidationRecord>(File.ReadAllBytes(info.FullName), RecordJsonOptions)
                ?? throw new InvalidDataException("Native validation record is empty.");
        }
        catch (JsonException error) { throw new InvalidDataException("Native validation record is invalid JSON.", error); }
        var capability = catalog.FirstOrDefault(item => item.Id == record.RecipeId)
            ?? throw new InvalidDataException("The native record names an unknown capability.");
        var implementations = capability.Implementations.Where(item => record.ImplementationKey is null || item.Key == record.ImplementationKey).ToArray();
        if (record.SchemaVersion != NativeValidationEvidence.SchemaVersion || !Enum.IsDefined(record.Outcome) ||
            !implementations.Any(item => item.Scenarios.Any(scenario => scenario.Id == record.ScenarioId)) || record.RecipeVersion < 1 ||
            record.RecordedUtc == default || record.RecordedUtc > DateTimeOffset.UtcNow.AddMinutes(5) ||
            string.IsNullOrWhiteSpace(record.WindowsProduct) || string.IsNullOrWhiteSpace(record.WindowsVersion) ||
            string.IsNullOrWhiteSpace(record.WindowsBuild) || string.IsNullOrWhiteSpace(record.Architecture) ||
            string.IsNullOrWhiteSpace(record.KillerBenchVersion) || record.Notes is null || record.Notes.Length > 2000 ||
            record.Artifacts is null || record.Artifacts.Count is 0 or > 32 ||
            record.Artifacts.Any(artifact => artifact is null || string.IsNullOrWhiteSpace(artifact.Kind) || !SafeFilename(artifact.Name) || !IsSha256(artifact.Sha256)) ||
            record.Artifacts.Select(artifact => artifact.Kind).Distinct(StringComparer.Ordinal).Count() != record.Artifacts.Count ||
            (record.ArtifactDirectory is not null && !SafeFilename(record.ArtifactDirectory)))
            throw new InvalidDataException("The native record has invalid identity, scenario, metadata or artifact references.");
        return record;
    }

    private sealed record EvidenceSnapshot(IReadOnlyList<(NativeValidationRecord Record, string Reference, string Directory)> Records, bool Incomplete);

    private static EvidenceSnapshot LoadEvidence(string? evidenceDirectory, IReadOnlyList<string>? evidenceFiles)
    {
        if (evidenceFiles is null && (evidenceDirectory is null || !Directory.Exists(evidenceDirectory))) return new([], false);
        bool incomplete = false;
        var records = new List<(NativeValidationRecord Record, string Reference, string Directory)>();
        try
        {
            if (evidenceDirectory is not null && (File.GetAttributes(evidenceDirectory) & FileAttributes.ReparsePoint) != 0)
                return new([], true);
            int count = 0;
            long bytes = 0;
            foreach (string file in evidenceFiles ?? (IEnumerable<string>)Directory.EnumerateFiles(evidenceDirectory!, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (++count > NativeValidationEvidence.MaximumRecords) { incomplete = true; break; }
                var info = new FileInfo(file);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length is 0 or > NativeValidationEvidence.MaximumBytes) { incomplete = true; continue; }
                bytes += info.Length;
                if (bytes > 32 * 1024 * 1024) { incomplete = true; break; }
                NativeValidationRecord? record;
                try { record = JsonSerializer.Deserialize<NativeValidationRecord>(File.ReadAllBytes(file), RecordJsonOptions); }
                catch (JsonException) { incomplete = true; continue; }
                if (record is not null) records.Add((record, Path.GetFileName(file), Path.GetDirectoryName(info.FullName)!));
                else incomplete = true;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { incomplete = true; }
        return new(records.AsReadOnly(), incomplete);
    }

    private static RepairCapabilityImplementation Evaluate(string recipeId, RepairCapabilityImplementation implementation, EvidenceSnapshot evidence)
    {
        if (implementation.Status == NativeCapabilityStatus.DiagnosticOnly) return implementation;
        bool incomplete = evidence.Incomplete, stale = false;
        var records = new List<(NativeValidationRecord Record, string Reference)>();
        long artifactBytes = 0;
        try
        {
            foreach (var source in evidence.Records.Where(item => item.Record.RecipeId == recipeId))
            {
                var record = source.Record;
                if (record.ImplementationKey != implementation.Key || record.RecipeVersion != implementation.Version ||
                    record.ImplementationSha256 != implementation.ImplementationSha256 || record.DefinitionSha256 != implementation.DefinitionSha256)
                { stale = true; continue; }
                var scenario = implementation.Scenarios.FirstOrDefault(item => item.Id == record.ScenarioId);
                if (record.SchemaVersion != NativeValidationEvidence.SchemaVersion || scenario is null || !Enum.IsDefined(record.Outcome) ||
                    record.RecordedUtc == default || record.RecordedUtc > DateTimeOffset.UtcNow.AddMinutes(5) ||
                    string.IsNullOrWhiteSpace(record.WindowsProduct) || string.IsNullOrWhiteSpace(record.WindowsVersion) ||
                    string.IsNullOrWhiteSpace(record.WindowsBuild) || string.IsNullOrWhiteSpace(record.Architecture) ||
                    string.IsNullOrWhiteSpace(record.KillerBenchVersion) || record.Notes is null || record.Notes.Length > 2000 ||
                    record.Artifacts is null || record.Artifacts.Count is 0 or > 32 ||
                    record.Artifacts.Any(artifact => artifact is null) ||
                    record.Artifacts.Select(artifact => artifact.Kind).Distinct(StringComparer.Ordinal).Count() != record.Artifacts.Count ||
                    !scenario.RequiredArtifacts.All(required => record.Artifacts.Count(artifact => artifact is not null && artifact.Kind == required) == 1))
                { incomplete = true; continue; }
                string artifactDirectory = source.Directory;
                if (record.ArtifactDirectory is not null)
                {
                    if (!SafeFilename(record.ArtifactDirectory)) { incomplete = true; continue; }
                    artifactDirectory = Path.Combine(source.Directory, record.ArtifactDirectory);
                    if (!Directory.Exists(artifactDirectory) || (File.GetAttributes(artifactDirectory) & FileAttributes.ReparsePoint) != 0)
                    { incomplete = true; continue; }
                }
                bool retained = true;
                foreach (var artifact in record.Artifacts)
                {
                    if (artifact is null || !SafeFilename(artifact.Name) || !IsSha256(artifact.Sha256))
                    { retained = false; break; }
                    string artifactPath = Path.Combine(artifactDirectory, artifact.Name);
                    if (!File.Exists(artifactPath) || (File.GetAttributes(artifactPath) & FileAttributes.ReparsePoint) != 0)
                    { retained = false; break; }
                    using var stream = File.OpenRead(artifactPath);
                    artifactBytes += stream.Length;
                    if (artifactBytes > 128 * 1024 * 1024) { retained = false; break; }
                    if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(artifact.Sha256, StringComparison.OrdinalIgnoreCase))
                    { retained = false; break; }
                }
                if (!retained) { incomplete = true; continue; }
                records.Add((record, source.Reference));
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { incomplete = true; }
        var latest = records.GroupBy(item => item.Record.ScenarioId, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(item => item.Record.RecordedUtc).ThenBy(item => item.Record.Outcome == NativeValidationOutcome.Passed).First()).ToArray();
        bool passed = implementation.Scenarios.All(scenario => latest.Any(item => item.Record.ScenarioId == scenario.Id && item.Record.Outcome == NativeValidationOutcome.Passed));
        return implementation with
        {
            Status = incomplete ? NativeCapabilityStatus.IncompleteEvidence : passed ? NativeCapabilityStatus.LabVerified :
                records.Count > 0 ? NativeCapabilityStatus.IncompleteEvidence : stale ? NativeCapabilityStatus.StaleEvidence : NativeCapabilityStatus.NotYetVerified,
            EvidenceReferences = Array.AsReadOnly(latest.Select(item => item.Reference).Order(StringComparer.Ordinal).ToArray())
        };
    }

    private static bool IsSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
    private static bool SafeFilename(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 180 &&
        value is not ("." or "..") && Path.GetFileName(value) == value && value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
}
