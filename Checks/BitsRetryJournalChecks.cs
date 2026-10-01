using System.IO;
using KillerBench.Engine;

internal static class BitsRetryJournalChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "bits-retry-journal");
        Guid id = Guid.NewGuid();
        var evidence = BitsDiagnostics.Parse(new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(new
        {
            scope = "CurrentAccount", complete = true,
            jobs = new[] { new { id, name = "Fixture", state = "Error", error = "Fixture error", owner = "Fixture", errorCode = -1, bytesTotal = "100", bytesTransferred = "0" } }
        }), "", false));
        var invalid = BitsDiagnostics.Parse(new ToolResult(0, "{}", "", false));
        string rejectedRoot = Path.Combine(scratch, "bits-rejected");
        try { await new BitsRetryJournal(rejectedRoot).BeginAsync(id, invalid, CancellationToken.None); throw new Exception("Incomplete evidence accepted."); }
        catch (InvalidDataException) { }
        if (Directory.Exists(rejectedRoot)) throw new Exception("Rejected evidence created a journal.");
        var first = new BitsRetryJournal(root);
        await first.BeginAsync(id, evidence, CancellationToken.None);
        string pending = Path.Combine(root, id.ToString("N") + ".pending.json");
        byte[] original = File.ReadAllBytes(pending);
        try { await new BitsRetryJournal(root).BeginAsync(id, evidence, CancellationToken.None); throw new Exception("Competing attempt accepted."); }
        catch (IOException) { }
        try { await first.CompleteAsync(new(id, BitsRetryOutcome.Uncertain, "Wrong evidence", invalid), CancellationToken.None); throw new Exception("Mismatched evidence accepted."); }
        catch (InvalidDataException) { }
        if (File.Exists(Path.Combine(root, id.ToString("N") + ".result.json"))) throw new Exception("Rejected result persisted.");
        await first.CompleteAsync(new(id, BitsRetryOutcome.Uncertain, "Interrupted fixture", evidence), CancellationToken.None);
        try { await new BitsRetryJournal(root).BeginAsync(id, evidence, CancellationToken.None); throw new Exception("Uncertain attempt silently repeated."); }
        catch (IOException) { }
        if (!original.SequenceEqual(File.ReadAllBytes(pending)) || !File.Exists(Path.Combine(root, id.ToString("N") + ".result.json")))
            throw new Exception("Pending evidence or result lost.");
        Guid abandonedId = Guid.NewGuid();
        var abandonedEvidence = BitsDiagnostics.Parse(new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(new
        {
            scope = "CurrentAccount", complete = true,
            jobs = new[] { new { id = abandonedId, name = "Fixture", state = "Error", error = "Fixture error", owner = "Fixture", errorCode = -1, bytesTotal = "100", bytesTransferred = "0" } }
        }), "", false));
        string abandonedRoot = Path.Combine(scratch, "bits-no-request");
        var abandoned = new BitsRetryJournal(abandonedRoot);
        await abandoned.BeginAsync(abandonedId, abandonedEvidence, CancellationToken.None);
        await abandoned.AbandonAsync(new(abandonedId, BitsRetryOutcome.Blocked, "No request fixture", abandonedEvidence), CancellationToken.None);
        string[] noRequestRecords = [.. Directory.GetFiles(abandonedRoot, "bits-retry-no-request-*.json")
            .Where(path => !path.EndsWith(".prepared.json", StringComparison.OrdinalIgnoreCase))];
        if (File.Exists(Path.Combine(abandonedRoot, abandonedId.ToString("N") + ".pending.json")) ||
            noRequestRecords.Length != 1 ||
            Directory.GetFiles(abandonedRoot, "*.prepared.json").Length != 1)
            throw new Exception("No-request preparation was not archived exactly once.");
        var repeat = new BitsRetryJournal(abandonedRoot);
        await repeat.BeginAsync(abandonedId, abandonedEvidence, CancellationToken.None);
        if (!File.Exists(Path.Combine(abandonedRoot, abandonedId.ToString("N") + ".pending.json")))
            throw new Exception("Archived no-request preparation incorrectly blocked a new explicit attempt.");
        Guid canceled = Guid.NewGuid();
        string conflictRoot = Path.Combine(scratch, "bits-result-conflict");
        var conflictJournal = new BitsRetryJournal(conflictRoot);
        await conflictJournal.BeginAsync(id, evidence, CancellationToken.None);
        string conflictPending = Path.Combine(conflictRoot, id.ToString("N") + ".pending.json");
        byte[] conflictOriginal = File.ReadAllBytes(conflictPending);
        string conflictResult = Path.Combine(conflictRoot, id.ToString("N") + ".result.json");
        const string existing = "Existing result must survive, even if it is not valid JSON.";
        File.WriteAllText(conflictResult, existing);
        try
        {
            await conflictJournal.CompleteAsync(new(id, BitsRetryOutcome.RetryRequested, "Fixture", evidence), CancellationToken.None);
            throw new Exception("Conflicting result was overwritten.");
        }
        catch (IOException) { }
        if (File.ReadAllText(conflictResult) != existing || !conflictOriginal.SequenceEqual(File.ReadAllBytes(conflictPending)))
            throw new Exception("Failed result persistence altered retained evidence.");
        try { await new BitsRetryJournal(conflictRoot).BeginAsync(id, evidence, CancellationToken.None); throw new Exception("Failed persistence released the retry guard."); }
        catch (IOException) { }
        try { await new BitsRetryJournal(root).BeginAsync(canceled, evidence, new CancellationToken(true)); throw new Exception("Cancellation ignored."); }
        catch (OperationCanceledException) { }
        if (File.Exists(Path.Combine(root, canceled.ToString("N") + ".pending.json"))) throw new Exception("Canceled attempt created a record.");
        Console.WriteLine("PASS: BITS retry journal persists preparation and outcomes, blocks competing/restarted attempts and preserves cancellation; scratch files only.");
    }
}
