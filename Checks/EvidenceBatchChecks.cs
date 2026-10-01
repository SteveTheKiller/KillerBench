using System.IO;
using KillerBench.Engine;

internal static class EvidenceBatchChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string first = Path.Combine(scratch, "batch-first.log"), last = Path.Combine(scratch, "batch-last.log");
        await File.WriteAllTextAsync(first, "Synthetic first evidence");
        await File.WriteAllTextAsync(last, "Synthetic last evidence");
        var results = new List<EvidenceImportResult>();
        await foreach (var item in EvidenceBatch.ReadAsync([first, Path.Combine(scratch, "missing.xml"), last])) results.Add(item);
        if (results.Count != 3 || results[0].Evidence?.Source != first || results[1].Error is null ||
            results[1].Evidence is not null || results[2].Evidence?.Source != last ||
            await File.ReadAllTextAsync(first) != "Synthetic first evidence")
            throw new Exception("Batch import lost ordering, partial success or source preservation.");
        using var stop = new CancellationTokenSource();
        int received = 0;
        try
        {
            await foreach (var item in EvidenceBatch.ReadAsync([first, last], stop.Token)) { received++; stop.Cancel(); }
            throw new Exception("Canceled import continued.");
        }
        catch (OperationCanceledException) { }
        if (received != 1) throw new Exception("Canceled batch lost its completed result boundary.");
        string[][] invalidBatches = [[], [.. Enumerable.Repeat(first, 13)], [first, "capture.kbench"]];
        foreach (var invalid in invalidBatches)
        {
            try { await foreach (var item in EvidenceBatch.ReadAsync(invalid)) throw new Exception("Invalid batch dispatched."); }
            catch (ArgumentException) { continue; }
            throw new Exception("Invalid batch accepted.");
        }
        Console.WriteLine("PASS: batch import partial success, file order, cancellation, bounds and source preservation.");
    }
}
