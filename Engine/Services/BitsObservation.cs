namespace KillerBench.Engine.Services;

public sealed record BitsObservationResult(DateTimeOffset FirstCollected, DateTimeOffset SecondCollected,
    BitsJobComparison Comparison, ToolResult BeforeEvidence, ToolResult AfterEvidence);

public static class BitsObservation
{
    public static async Task<BitsObservationResult> ObserveAsync(Guid jobId,
        Func<CancellationToken, Task<BitsInventoryResult>> collect, TimeSpan interval, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(collect);
        if (jobId == Guid.Empty) throw new ArgumentException("An exact transfer ID is required.", nameof(jobId));
        if (interval <= TimeSpan.Zero || interval > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(interval));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(95));
        deadline.Token.ThrowIfCancellationRequested();
        var before = await collect(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        if (before?.Evidence is null) throw new InvalidDataException("First transfer snapshot evidence is missing.");
        var first = DateTimeOffset.UtcNow;
        await Task.Delay(interval, deadline.Token).ConfigureAwait(false);
        var after = await collect(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        if (after?.Evidence is null) throw new InvalidDataException("Second transfer snapshot evidence is missing.");
        var second = DateTimeOffset.UtcNow;
        deadline.Token.ThrowIfCancellationRequested();
        return new(first, second, BitsJobReview.Compare(before, after, jobId, deadline.Token), before.Evidence, after.Evidence);
    }
}
