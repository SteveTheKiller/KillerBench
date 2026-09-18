using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WorkflowJournalChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string persistencePath = Path.Combine(scratch, "bounded-journal.json");
        await JournalPersistence.WriteAsync(persistencePath, "original");
        var stalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string? latePath = null;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await JournalPersistence.WriteAsync(persistencePath, "late", timeout: TimeSpan.FromMilliseconds(20),
                writeTemporary: (temporary, _, _) => { latePath = temporary; return stalled.Task; })
                .WaitAsync(TimeSpan.FromSeconds(3));
            throw new Exception("Stalled journal save completed.");
        }
        catch (TimeoutException error) when (error.Message.StartsWith("Journal save timed out")) { }
        if (watch.Elapsed > TimeSpan.FromSeconds(2) || await File.ReadAllTextAsync(persistencePath) != "original")
            throw new Exception("Journal timeout did not preserve the original within its deadline.");
        await JournalPersistence.WriteAsync(persistencePath, "newest");
        await File.WriteAllTextAsync(latePath!, "late");
        stalled.SetResult();
        if (await File.ReadAllTextAsync(persistencePath) != "newest")
            throw new Exception("Late temporary write replaced a newer journal.");
        using (var stop = new CancellationTokenSource())
        {
            try
            {
                await JournalPersistence.WriteAsync(persistencePath, "canceled", stop.Token,
                    writeTemporary: async (temporary, text, _) =>
                    { await File.WriteAllTextAsync(temporary, text); stop.Cancel(); });
                throw new Exception("Canceled write published its temporary file.");
            }
            catch (OperationCanceledException) { }
        }
        if (await File.ReadAllTextAsync(persistencePath) != "newest")
            throw new Exception("Cancellation replaced the last journal.");
        string path = Path.Combine(scratch, "workflow-review.json");
        var result = new WorkflowResult(WorkflowKind.Vitals, "Fixture", DateTimeOffset.UtcNow, "Running",
            [new("Fixture check", CheckState.Information, "Retained evidence")], [], "untrusted-path");
        string original = JsonSerializer.Serialize(result, Capture.JsonOptions);
        await File.WriteAllTextAsync(path, original);
        var review = await WorkflowJournalReview.ReadAsync(path);
        if (review.RecordedStatus != "Running" || review.CheckCount != 1 || review.RepairCount != 0 ||
            review.Source != Path.GetFullPath(path) || review.Record.GetProperty("journalPath").GetString() != "untrusted-path" ||
            await File.ReadAllTextAsync(path) != original)
            throw new Exception("Historical workflow review changed evidence or followed the recorded path.");
        string exported = Path.Combine(scratch, "exported-workflow-review.json");
        await File.WriteAllTextAsync(exported, JsonSerializer.Serialize(review, Capture.JsonOptions));
        var reopened = await WorkflowJournalReview.ReadAsync(exported);
        if (reopened.Source != Path.GetFullPath(exported) || JsonSerializer.Serialize(reopened.Record) != JsonSerializer.Serialize(review.Record) ||
            reopened.RecordedStatus != review.RecordedStatus || reopened.Machine != review.Machine)
            throw new Exception("Exported workflow review lost the recorded content or followed the old source path.");
        foreach (string invalid in new[] { "{}", "[]", "{", "{\"record\":null}", "{\"record\":{},\"record\":{}}", original.Replace("\"Vitals\"", "\"999\""),
            original.Replace("\"Running\"", "null"), original.Insert(1, "\"kind\":\"Storage\",") })
        {
            await File.WriteAllTextAsync(path, invalid);
            try { await WorkflowJournalReview.ReadAsync(path); throw new Exception("Malformed workflow accepted."); }
            catch (InvalidDataException) { }
        }
        await File.WriteAllTextAsync(path, original);
        try { await WorkflowJournalReview.ReadAsync(path, new CancellationToken(true)); throw new Exception("Cancellation ignored."); }
        catch (OperationCanceledException) { }
        try { await WorkflowJournalReview.ReadAsync(Path.Combine(scratch, "missing-review.json"), new CancellationToken(true)); throw new Exception("Canceled read opened a path."); }
        catch (OperationCanceledException) { }
        await using (var oversized = new FileStream(path, FileMode.Create, FileAccess.Write))
            oversized.SetLength(WorkflowJournalReview.MaximumBytes + 1L);
        try { await WorkflowJournalReview.ReadAsync(path); throw new Exception("Oversized journal accepted."); }
        catch (InvalidDataException) { }
        if (new FileInfo(path).Length != WorkflowJournalReview.MaximumBytes + 1L)
            throw new Exception("Oversized evidence was changed.");
        Console.WriteLine("PASS: historical workflow review retains source contents, rejects malformed roots and honors cancellation without dispatch.");
    }
}
