using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class NativeValidationEvidenceChecks
{
    internal static void Run()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var scenario = NativeValidationCatalog.Find("windows.system-files")!.Scenarios
            .Single(item => item.Kind == NativeValidationScenarioKind.Healthy);
        var record = Record(scenario, "windows.system-files", 1, NativeValidationOutcome.Passed, now);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(record, Capture.JsonOptions);
        var parsed = NativeValidationEvidence.Read(json, now);
        Require(parsed.SchemaVersion == record.SchemaVersion && parsed.ScenarioId == record.ScenarioId &&
            parsed.RecipeId == record.RecipeId && parsed.RecipeVersion == record.RecipeVersion &&
            parsed.Outcome == record.Outcome && parsed.RecordedUtc == record.RecordedUtc &&
            parsed.Artifacts.SequenceEqual(record.Artifacts),
            "Native validation records round trip through strict JSON parsing");

        var older = record with { Outcome = NativeValidationOutcome.Failed, RecordedUtc = now.AddMinutes(-5) };
        var coverage = NativeValidationEvidence.Summarize([older, record]);
        Require(coverage.InstalledRecipes == RecipeCatalog.All.Count &&
            coverage.InstalledScenarios == NativeValidationCatalog.All.Sum(plan => plan.Scenarios.Count) &&
            coverage.RecordedScenarios == 1 && coverage.PassedScenarios == 1 && coverage.FailedScenarios == 0 &&
            coverage.MissingScenarioIds.Count == coverage.InstalledScenarios - 1,
            "Coverage uses the latest current-version record for each stable scenario");

        Reject(record with { RecipeVersion = 99 }, now, "stale recipe version");
        Reject(record with { ScenarioId = "windows.system-files.unknown" }, now, "unknown scenario");
        Reject(record with { RecordedUtc = now.AddMinutes(6) }, now, "future timestamp");
        Reject(record with { Artifacts = record.Artifacts.Skip(1).ToArray() }, now, "missing required artifact");
        Reject(record with { Artifacts = record.Artifacts.Select(artifact => artifact with { Name = "C:\\secret\\artifact.json" }).ToArray() }, now,
            "artifact path disclosure");
        Reject(record with { Artifacts = record.Artifacts.Select(artifact => artifact with { Sha256 = "bad" }).ToArray() }, now,
            "invalid artifact digest");
        Console.WriteLine("PASS: native VM validation records require current scenario identity, bounded metadata and complete hashed artifacts.");
    }

    private static NativeValidationRecord Record(NativeValidationScenario scenario, string recipeId, int recipeVersion,
        NativeValidationOutcome outcome, DateTimeOffset recordedUtc) =>
        new(NativeValidationEvidence.SchemaVersion, scenario.Id, recipeId, recipeVersion, outcome,
            "Windows 11 Pro", "24H2", "26100.1", "x64", "0.1.0", recordedUtc,
            scenario.RequiredArtifacts.Select((kind, index) => new NativeValidationArtifact(kind, "artifact-" + index + ".json",
                new string(index % 2 == 0 ? 'a' : 'b', 64))).ToArray(), "Disposable VM fixture.");

    private static void Reject(NativeValidationRecord record, DateTimeOffset now, string label)
    {
        try
        {
            NativeValidationEvidence.Read(JsonSerializer.SerializeToUtf8Bytes(record, Capture.JsonOptions), now);
            throw new Exception("Native validation accepted " + label);
        }
        catch (InvalidDataException) { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
