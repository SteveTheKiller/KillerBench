using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using KillerBench.Cli;

internal static class RepairCapabilityChecks
{
    internal static async Task RunAsync(string scratch)
    {
        await NativeScenarioProducerChecks.RunAsync(scratch);
        var packs = WindowsPacks.All();
        string binary = new('a', 64);
        var inventory = RepairCapabilityInventory.Build(packs, binary, WindowsRepairCapabilities.Explicit);
        foreach (var recipe in RecipeCatalog.All.Where(recipe => !recipe.DiagnosticOnly))
            Require(inventory.Single(capability => capability.Id == recipe.Id).Implementations.Any(item => item.Key == "legacy/" + recipe.Id && item.Version == recipe.Version),
                "Every legacy mutator has a versioned implementation");
        foreach (var pack in packs)
            foreach (var recipe in pack.Recipes.Where(recipe => !recipe.DiagnosticOnly))
                Require(inventory.Single(capability => capability.Id == recipe.Id).Implementations.Any(item => item.Key == "pack/" + pack.Id + "/" + recipe.Id && item.Version == recipe.Version),
                    "Every declarative mutator has a versioned implementation");
        Require(inventory.All(capability => capability.Status is NativeCapabilityStatus.NotYetVerified or NativeCapabilityStatus.DiagnosticOnly), "Missing evidence must never inherit a lab pass");
        Require(inventory.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == inventory.Count,
            "Overlapping recipe IDs are mapped without double-counting identities");
        foreach (var command in new[] { "task-register", "bitlocker-encrypt", "cleanup", "profiles", "update-plan", "privacy-setting" })
            Require(WindowsRepairCapabilities.ForCommand([command]).Any(item => item.Id == "command." + command), "Explicit mutators are mapped at their command seam");
        Require(WindowsRepairCapabilities.ForCommand(["repair", "dism"]).Any(item => item.Id == "command.repair-dism"), "Explicit servicing commands are mapped");
        Require(WindowsRepairCapabilities.ForCommand(["debloat-review", "--apply-high-performance"]).Any(item => item.Id == "command.debloat-high-performance"), "Declutter execution switches are mapped");

        string root = Path.Combine(scratch, "capability-evidence");
        Directory.CreateDirectory(root);
        try
        {
            var entry = inventory.Single(item => item.Id == "hardware.memory-diagnostic");
            var implementation = entry.Implementations.Single();
            var records = new List<NativeValidationRecord>();
            foreach (var scenario in implementation.Scenarios)
            {
                var artifacts = new List<NativeValidationArtifact>();
                foreach (string kind in scenario.RequiredArtifacts)
                {
                    string name = scenario.Kind + "-" + artifacts.Count + ".txt";
                    string path = Path.Combine(root, name);
                    File.WriteAllText(path, "Retained fixture evidence for " + kind);
                    artifacts.Add(new(kind, name, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))));
                }
                var record = new NativeValidationRecord(NativeValidationEvidence.SchemaVersion, scenario.Id, entry.Id, implementation.Version,
                    NativeValidationOutcome.Passed, "Windows 11 Pro", "24H2", "26100.1", "x64", "1.0.0", DateTimeOffset.UtcNow.AddMinutes(-1), artifacts, "Fixture only; no native acceptance claimed.")
                { ImplementationKey = implementation.Key, ImplementationSha256 = binary, DefinitionSha256 = implementation.DefinitionSha256 };
                records.Add(record);
                Write(record);
            }
            Require(Status() == NativeCapabilityStatus.LabVerified, "Complete matching evidence requires retained artifact hashes");
            var bound = NativeScenarioRunner.BindRetainedEvidence(records[0] with { ImplementationKey = null, ImplementationSha256 = null, DefinitionSha256 = null }, root);
            Require(bound.ImplementationKey == implementation.Key && bound.DefinitionSha256 == implementation.DefinitionSha256 &&
                bound.ImplementationSha256 == WindowsRepairCapabilities.BinaryFingerprint() && bound.ArtifactDirectory == Path.GetFileName(root),
                "Native runner emission binds the actual pack implementation, binary and artifact directory");
            bound = NativeScenarioRunner.BindRetainedEvidence(records[0] with { Artifacts = records[0].Artifacts.Skip(1).ToArray() }, root);
            Require(bound.Outcome == NativeValidationOutcome.Inconclusive && bound.Artifacts.Count == records[0].Artifacts.Count - 1,
                "Missing native artifacts downgrade a pass without generating replacement proof");
            string firstRecord = Path.Combine(root, records[0].ScenarioId + ".json");
            TextWriter previousOutput = Console.Out;
            using (var output = new StringWriter())
            {
                int exit;
                try { Console.SetOut(output); exit = await CommandEngine.RunAsync(["validation-record", firstRecord]); }
                finally { Console.SetOut(previousOutput); }
                Require(exit == 0 && output.ToString().Contains(entry.Id, StringComparison.Ordinal),
                    "The normal validation-record command accepts a bound declarative-only recipe record");
            }
            string[] paths = records.Select(record => Path.Combine(root, record.ScenarioId + ".json")).ToArray();
            foreach (string[] command in new[]
            {
                new[] { "validation-coverage", firstRecord },
                new[] { "validation-report", Path.Combine(root, "capability-report.txt") }.Concat(paths).ToArray(),
                new[] { "validation-report", Path.Combine(root, "capability-report.html") }.Concat(paths).ToArray()
            })
            {
                using var output = new StringWriter();
                int exit;
                try { Console.SetOut(output); exit = await CommandEngine.RunAsync(command); }
                finally { Console.SetOut(previousOutput); }
                string text = command[0] == "validation-report" ? File.ReadAllText(command[1]) : output.ToString();
                Require(exit == 0 && text.Contains(entry.Id, StringComparison.Ordinal) && text.Contains("command.task-register", StringComparison.Ordinal)
                    && text.Contains("legacy/", StringComparison.Ordinal) && text.Contains("stale", StringComparison.OrdinalIgnoreCase),
                    "Normal coverage and report commands include the complete shared inventory and preserve stale proof");
            }
            var failed = records[0] with { Outcome = NativeValidationOutcome.Failed, RecordedUtc = DateTimeOffset.UtcNow };
            Write(failed);
            Require(Status() == NativeCapabilityStatus.IncompleteEvidence, "A failed latest scenario clears the lab label");
            Write(records[0]);
            File.WriteAllText(Path.Combine(root, records[0].Artifacts[0].Name), "Changed fixture");
            Require(Status() == NativeCapabilityStatus.IncompleteEvidence, "Changed retained artifact bytes clear the lab label");
            Require(RepairCapabilityInventory.Build(packs, new string('b', 64), [], root).Single(item => item.Id == entry.Id).Status == NativeCapabilityStatus.StaleEvidence,
                "Changed binary implementation with unchanged recipe version cannot inherit a pass");

            NativeCapabilityStatus Status() => RepairCapabilityInventory.Build(packs, binary, [], root).Single(item => item.Id == entry.Id).Status;
            void Write(NativeValidationRecord record) => File.WriteAllText(Path.Combine(root, record.ScenarioId + ".json"), JsonSerializer.Serialize(record, Capture.JsonOptions));
        }
        finally { Directory.Delete(root, true); }

        foreach (string[] args in new[] { new[] { "validation-plan", "--recipe", "hardware.memory-diagnostic" }, new[] { "pack", "list" }, new[] { "recipes", "files" } })
        {
            TextWriter previous = Console.Out;
            using var output = new StringWriter();
            int exit;
            try { Console.SetOut(output); exit = await CommandEngine.RunAsync(args); }
            finally { Console.SetOut(previous); }
            Require(exit == 0 && output.ToString().Contains("Not yet verified", StringComparison.Ordinal), "Normal CLI catalogs expose native capability labels");
        }
        Console.WriteLine("PASS: shared repair capability inventory covers legacy, declarative and explicit mutation surfaces, validates retained native evidence, and labels CLI catalogs conservatively.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
