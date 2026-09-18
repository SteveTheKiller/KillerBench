using KillerBench.Engine;

internal static class BitsObservationChecks
{
    internal static async Task RunAsync()
    {
        var id = Guid.NewGuid();
        int calls = 0;
        Task<BitsInventoryResult> Collect(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            calls++;
            var output = System.Text.Json.JsonSerializer.Serialize(new { scope = "CurrentAccount", complete = true,
                jobs = new[] { new { id, name = "Fixture", state = "Transferring", error = "", owner = "Fixture",
                    errorCode = 0, bytesTotal = "100", bytesTransferred = calls == 1 ? "10" : "20" } } });
            return Task.FromResult(BitsDiagnostics.Parse(new(0, output, "", false)));
        }
        var result = await BitsObservation.ObserveAsync(id, Collect, TimeSpan.FromMilliseconds(1));
        if (calls != 2 || result.Comparison.BytesAdvanced != 10 || !result.Comparison.Comparable ||
            result.BeforeEvidence.Output == result.AfterEvidence.Output)
            throw new Exception("BITS observation lost ordered snapshots or raw evidence.");
        using var stop = new CancellationTokenSource();
        calls = 0;
        async Task<BitsInventoryResult> CancelAfterFirst(CancellationToken token)
        {
            var inventory = await Collect(token);
            stop.Cancel();
            return inventory;
        }
        try { await BitsObservation.ObserveAsync(id, CancelAfterFirst, TimeSpan.FromMilliseconds(1), stop.Token); throw new Exception("Canceled observation continued."); }
        catch (OperationCanceledException) { }
        if (calls != 1) throw new Exception("Canceled observation collected another snapshot.");
        foreach (var interval in new[] { TimeSpan.Zero, TimeSpan.FromSeconds(31) })
        {
            try { await BitsObservation.ObserveAsync(id, Collect, interval); throw new Exception("Invalid observation interval accepted."); }
            catch (ArgumentOutOfRangeException) { }
        }
        if (calls != 1) throw new Exception("Invalid observation options dispatched collection.");
        for (int stage = 1; stage <= 2; stage++)
        {
            using var interrupted = new CancellationTokenSource();
            var pending = new TaskCompletionSource<BitsInventoryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            int reads = 0;
            Task<BitsInventoryResult> Stalled(CancellationToken token)
            {
                reads++;
                if (reads != stage) return Collect(token);
                interrupted.Cancel();
                return pending.Task;
            }
            try
            {
                await BitsObservation.ObserveAsync(id, Stalled, TimeSpan.FromMilliseconds(1), interrupted.Token).WaitAsync(TimeSpan.FromSeconds(2));
                throw new Exception("Stalled collector prevented Stop.");
            }
            catch (OperationCanceledException) { }
            if (reads != stage) throw new Exception("Stop dispatched a later snapshot.");
            pending.SetResult(await Collect(CancellationToken.None));
        }
        for (int stage = 1; stage <= 2; stage++)
        foreach (bool missingEvidence in new[] { false, true })
        {
            int reads = 0;
            async Task<BitsInventoryResult> Missing(CancellationToken token)
            {
                reads++;
                if (reads == stage) return missingEvidence ? new("CurrentAccount", true, [], null!) : null!;
                return await Collect(token);
            }
            try { await BitsObservation.ObserveAsync(id, Missing, TimeSpan.FromMilliseconds(1)); throw new Exception("Missing snapshot accepted."); }
            catch (System.IO.InvalidDataException) { }
            if (reads != stage) throw new Exception("Missing evidence did not stop collection.");
        }
        Console.WriteLine("PASS: BITS observation sequence, evidence retention, cancellation and interval guards; fixtures only.");
    }
}
