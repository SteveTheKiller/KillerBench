namespace KillerBench.Engine;

public static class JournalPersistence
{
    public static async Task WriteAsync(string path, string contents, CancellationToken token = default,
        TimeSpan? timeout = null,
        Func<string, string, CancellationToken, Task>? writeTemporary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(10);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        token.ThrowIfCancellationRequested();
        string destination = Path.GetFullPath(path);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        try
        {
            var pending = (writeTemporary ?? File.WriteAllTextAsync)(temporary, contents, deadline.Token)
                ?? throw new InvalidOperationException("Journal storage returned no write task.");
            await pending.WaitAsync(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            File.Move(temporary, destination, true);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            throw new TimeoutException("Journal save timed out. The previous journal is preserved; an incomplete temporary file may remain. No further repair should run.");
        }
    }
}
