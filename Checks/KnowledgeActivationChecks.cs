using System.Security.Cryptography;
using System.Text.Json;
using System.IO;
using KillerBench.Engine;

internal static class KnowledgeActivationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "knowledge-activation");
        Directory.CreateDirectory(root);
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] publicKey = signer.ExportSubjectPublicKeyInfo();
        SignedKnowledgePack Sign(long revision, string title = "Fixture")
        {
            var pack = new KnowledgePack(1, "fixture-active", revision, new DateOnly(2026, 9, 14), "Synthetic",
                [new("fixture", "E1", title, "Synthetic fixture", "https://example.com/e1", new DateOnly(2026, 9, 14), "Fixture only")]);
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(pack);
            return new(payload, signer.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        var first = await KnowledgeActivation.ActivateAsync(root, Sign(1), publicKey, "fixture-active");
        var loaded = await KnowledgeActivation.LoadActiveAsync(root, publicKey, "fixture-active");
        Require(first.Changed && loaded.Catalog.Revision == 1 && File.Exists(first.DataPath) && File.Exists(first.ActivePath),
            "First verified pack is staged immutably and selected by an active pointer");
        var same = await KnowledgeActivation.ActivateAsync(root, Sign(1), publicKey, "fixture-active");
        Require(!same.Changed && same.DataPath == first.DataPath, "Identical active revision is idempotent");

        var second = await KnowledgeActivation.ActivateAsync(root, Sign(2, "New fixture"), publicKey, "fixture-active");
        loaded = await KnowledgeActivation.LoadActiveAsync(root, publicKey, "fixture-active", 2);
        Require(second.Changed && loaded.Catalog.Revision == 2 && File.Exists(first.DataPath) && File.Exists(second.DataPath) && first.DataPath != second.DataPath,
            "Newer verified revision activates while retaining the prior immutable version");
        await Reject(() => KnowledgeActivation.ActivateAsync(root, Sign(1), publicKey, "fixture-active"), "Rollback revision accepted");
        await Reject(() => KnowledgeActivation.ActivateAsync(root, Sign(2, "Conflicting fixture"), publicKey, "fixture-active"), "Conflicting same revision accepted");

        var tampered = Sign(3);
        tampered.Payload[^1] ^= 1;
        await Reject(() => KnowledgeActivation.ActivateAsync(root, tampered, publicKey, "fixture-active"), "Tampered candidate accepted");
        Require((await KnowledgeActivation.LoadActiveAsync(root, publicKey, "fixture-active")).Catalog.Revision == 2,
            "Rejected candidate leaves the active revision usable");

        string corruptRoot = Path.Combine(scratch, "knowledge-corrupt-pointer");
        await KnowledgeActivation.ActivateAsync(corruptRoot, Sign(1), publicKey, "fixture-active");
        await File.WriteAllTextAsync(Path.Combine(corruptRoot, "active.json"), "{\"PackId\":\"fixture-active\",\"PackId\":\"other\"}");
        await Reject(async () => { await KnowledgeActivation.LoadActiveAsync(corruptRoot, publicKey, "fixture-active"); }, "Ambiguous active pointer accepted");

        string corruptDataRoot = Path.Combine(scratch, "knowledge-corrupt-data");
        var corruptData = await KnowledgeActivation.ActivateAsync(corruptDataRoot, Sign(1), publicKey, "fixture-active");
        byte[] corruptBytes = await File.ReadAllBytesAsync(corruptData.DataPath);
        corruptBytes[12] ^= 1;
        await File.WriteAllBytesAsync(corruptData.DataPath, corruptBytes);
        await Reject(async () => { await KnowledgeActivation.LoadActiveAsync(corruptDataRoot, publicKey, "fixture-active"); }, "Tampered active data accepted");

        string recoveryRoot = Path.Combine(scratch, "knowledge-recovery");
        var recoveryFirst = await KnowledgeActivation.ActivateAsync(recoveryRoot, Sign(1), publicKey, "fixture-active");
        var recoverySecond = await KnowledgeActivation.ActivateAsync(recoveryRoot, Sign(2), publicKey, "fixture-active");
        byte[] damaged = await File.ReadAllBytesAsync(recoverySecond.DataPath);
        damaged[12] ^= 1;
        await File.WriteAllBytesAsync(recoverySecond.DataPath, damaged);
        var recovered = await KnowledgeActivation.RecoverAsync(recoveryRoot, publicKey, "fixture-active");
        var (recoveredCatalog, _, _) = await KnowledgeActivation.LoadActiveAsync(recoveryRoot, publicKey, "fixture-active");
        Require(recovered.Catalog.Revision == 1 && recovered.DataPath == recoveryFirst.DataPath && recovered.FilesInspected == 2 &&
            recovered.FilesRejected == 1 && recoveredCatalog.Revision == 1,
            "Recovery rejects damaged active data and atomically selects the newest retained verified version");

        string emptyRecoveryRoot = Path.Combine(scratch, "knowledge-no-recovery");
        Directory.CreateDirectory(emptyRecoveryRoot);
        await Reject(() => KnowledgeActivation.RecoverAsync(emptyRecoveryRoot, publicKey, "fixture-active"),
            "Recovery without retained versions succeeded");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try { await KnowledgeActivation.ActivateAsync(Path.Combine(scratch, "knowledge-canceled"), Sign(1), publicKey, "fixture-active", token: canceled.Token); throw new Exception("Canceled activation continued"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: signed knowledge activation, atomic pointer, revision guard, verified retained recovery and cancellation; scratch files only.");
    }

    private static async Task Reject(Func<Task> action, string message)
    {
        try { await action(); throw new Exception(message); }
        catch (Exception error) when (error is InvalidDataException or CryptographicException) { }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
