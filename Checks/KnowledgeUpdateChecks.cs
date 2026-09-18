using System.Security.Cryptography;
using System.Text.Json;
using System.IO;
using KillerBench.Engine;

internal static class KnowledgeUpdateChecks
{
    internal static async Task RunAsync(string scratch)
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] key = signer.ExportSubjectPublicKeyInfo();
        SignedKnowledgePack Sign(string id, long revision)
        {
            var pack = new KnowledgePack(1, id, revision, new DateOnly(2026, 9, 14), "Synthetic",
                [new("fixture", "E1", "Fixture", "Synthetic fixture", "https://example.com/e1", new DateOnly(2026, 9, 14), "Fixture only")]);
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(pack);
            return new(payload, signer.SignData(payload, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        string root = Path.Combine(scratch, "knowledge-updates");
        var source = new FixtureSource(new Dictionary<string, SignedKnowledgePack>
        {
            ["errors"] = Sign("errors", 1),
            ["vendors"] = Sign("vendors", 2)
        });
        var progress = new List<KnowledgeUpdateProgress>();
        var request = new KnowledgeUpdateRequest("errors", 1, Path.Combine(root, "errors"), key);
        string configurationJson = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            databases = new[]
            {
                new { packId = "errors", minimumRevision = 1, payloadUrl = "https://updates.example/errors.json",
                    signatureUrl = "https://updates.example/errors.sig", trustedPublicKey = Convert.ToBase64String(key) },
                new { packId = "vendors", minimumRevision = 2, payloadUrl = "https://updates.example/vendors.json",
                    signatureUrl = "https://updates.example/vendors.sig", trustedPublicKey = Convert.ToBase64String(key) }
            }
        });
        var configuration = KnowledgeUpdateConfiguration.Read(JsonSerializer.SerializeToUtf8Bytes(JsonSerializer.Deserialize<JsonElement>(configurationJson)));
        var plan = configuration.CreatePlan(Path.Combine(root, "configured"));
        Require(plan.Requests.Count == 2 && plan.Endpoints.Count == 2 && plan.Requests[0].PackId == "errors" &&
            plan.Requests[0].StorageDirectory == Path.Combine(Path.GetFullPath(Path.Combine(root, "configured")), "errors") &&
            plan.Endpoints[1].SignatureUri.AbsoluteUri == "https://updates.example/vendors.sig",
            "Strict publisher configuration creates deterministic update requests and endpoints");
        var missingStatuses = await configuration.InspectAsync(Path.Combine(root, "configured"));
        Require(missingStatuses.Count == 2 && missingStatuses.All(status => !status.Installed && !status.Verified),
            "Status inspection reports configured databases as uninstalled without changing storage");
        await KnowledgeActivation.ActivateAsync(plan.Requests[0].StorageDirectory, Sign("errors", 1), key, "errors");
        var installedStatuses = await configuration.InspectAsync(Path.Combine(root, "configured"));
        Require(installedStatuses[0] is { Installed: true, Verified: true, Revision: 1, EntryCount: 1 } &&
            installedStatuses[1] is { Installed: false, Verified: false },
            "Status inspection verifies each active database independently");
        RejectConfiguration("{\"schemaVersion\":1,\"schemaVersion\":1,\"databases\":[]}");
        RejectConfiguration("{\"schemaVersion\":1,\"databases\":[{\"packId\":\"../escape\",\"minimumRevision\":1,\"payloadUrl\":\"https://updates.example/a\",\"signatureUrl\":\"https://updates.example/b\",\"trustedPublicKey\":\"AA==\"}]}");
        RejectConfiguration(configurationJson.Replace("https://updates.example/errors.json", "http://updates.example/errors.json", StringComparison.Ordinal));
        RejectConfiguration(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            databases = new[] { new { packId = "errors", minimumRevision = 1,
                payloadUrl = "https://updates.example/errors.json", signatureUrl = "https://updates.example/errors.sig",
                trustedPublicKey = Convert.ToBase64String(new byte[] { 1, 2, 3 }) } }
        }));
        var activated = await KnowledgeUpdates.UpdateAsync(request, source, new ProgressRecorder(progress));
        Require(activated.State == KnowledgeUpdateState.Activated && activated.InstalledRevision == 1 && activated.EntryCount == 1,
            "A verified candidate reports activated metadata");
        var current = await KnowledgeUpdates.UpdateAsync(request, source);
        Require(current.State == KnowledgeUpdateState.Current && current.ActivatedUtc == activated.ActivatedUtc,
            "An identical candidate reports current without changing activation time");
        Require(progress.Select(item => item.Stage).SequenceEqual(["Inspecting", "Fetching", "Verifying", "Activated"]),
            "Update progress is ordered and complete");

        var bad = Sign("errors", 2);
        bad.Signature[0] ^= 1;
        source.Packs["errors"] = bad;
        var failed = await KnowledgeUpdates.UpdateAsync(request, source);
        Require(failed.State == KnowledgeUpdateState.Failed && failed.InstalledRevision == 1 && failed.EntryCount == 1 &&
            (await KnowledgeActivation.LoadActiveAsync(request.StorageDirectory, key, "errors")).Catalog.Revision == 1,
            "A rejected candidate reports retained installed metadata and leaves active data usable");

        var batchSource = new FixtureSource(new Dictionary<string, SignedKnowledgePack>
        {
            ["errors"] = bad,
            ["vendors"] = Sign("vendors", 2)
        });
        var batch = await KnowledgeUpdates.UpdateAllAsync([
            request,
            new("vendors", 1, Path.Combine(root, "vendors"), key)
        ], batchSource);
        Require(batch.Count == 2 && batch[0].State == KnowledgeUpdateState.Failed &&
            batch[1].State == KnowledgeUpdateState.Activated && batch[1].InstalledRevision == 2,
            "One failed database does not prevent a different database from activating");

        string recoveryRoot = Path.Combine(root, "automatic-recovery");
        await KnowledgeActivation.ActivateAsync(recoveryRoot, Sign("errors", 1), key, "errors");
        var damagedActive = await KnowledgeActivation.ActivateAsync(recoveryRoot, Sign("errors", 2), key, "errors");
        byte[] damagedBytes = await File.ReadAllBytesAsync(damagedActive.DataPath);
        damagedBytes[12] ^= 1;
        await File.WriteAllBytesAsync(damagedActive.DataPath, damagedBytes);
        var recoveryProgress = new List<KnowledgeUpdateProgress>();
        var recoverySource = new FixtureSource(new() { ["errors"] = Sign("errors", 3) });
        var recoveredUpdate = await KnowledgeUpdates.UpdateAsync(new("errors", 1, recoveryRoot, key), recoverySource,
            new ProgressRecorder(recoveryProgress));
        Require(recoveredUpdate.State == KnowledgeUpdateState.Activated && recoveredUpdate.InstalledRevision == 3 &&
            recoveryProgress.Any(item => item.Stage == "Recovering"),
            "A damaged active database recovers from a retained version before activating a newer verified candidate");

        var canceled = new CancellationToken(true);
        try { await KnowledgeUpdates.UpdateAsync(request, source, token: canceled); throw new Exception("Canceled update continued"); }
        catch (OperationCanceledException) { }
        await Reject(() => KnowledgeUpdates.UpdateAllAsync([request, request], source), "Duplicate batch identities accepted");
        Console.WriteLine("PASS: strict database publisher configuration, verified status, update orchestration, progress, retained recovery, batch isolation and cancellation; substituted source only.");
    }

    private sealed class FixtureSource(Dictionary<string, SignedKnowledgePack> packs) : IKnowledgeCandidateSource
    {
        internal Dictionary<string, SignedKnowledgePack> Packs { get; } = packs;
        public Task<SignedKnowledgePack> FetchAsync(string packId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Packs[packId]);
        }
    }

    private sealed class ProgressRecorder(List<KnowledgeUpdateProgress> values) : IProgress<KnowledgeUpdateProgress>
    {
        public void Report(KnowledgeUpdateProgress value) => values.Add(value);
    }

    private static async Task Reject(Func<Task> action, string message)
    {
        try { await action(); throw new Exception(message); }
        catch (ArgumentException) { }
    }

    private static void RejectConfiguration(string json)
    {
        try { KnowledgeUpdateConfiguration.Read(System.Text.Encoding.UTF8.GetBytes(json)); throw new Exception("Invalid database update configuration accepted"); }
        catch (InvalidDataException) { }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
