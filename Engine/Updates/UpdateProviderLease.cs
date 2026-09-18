namespace KillerBench.Engine;

internal static class UpdateProviderLease
{
    internal static FileStream Acquire(string folder)
    {
        try { return new FileStream(Path.Combine(folder, "provider-active.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33)
        {
            throw new IOException("An update provider is still active. Wait for it to finish before checking recovery. The pending operation has not been cleared.", error);
        }
    }

    internal static Task<UpdateExecutionOutcome> Start(string folder, Func<Task<UpdateExecutionOutcome>> start)
    {
        var lease = Acquire(folder);
        Task<UpdateExecutionOutcome> operation;
        try { operation = start(); }
        catch { lease.Dispose(); throw; }
        _ = operation.ContinueWith(completed =>
        {
            _ = completed.Exception;
            lease.Dispose();
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return operation;
    }
}
