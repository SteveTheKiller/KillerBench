namespace KillerBench.Engine;

internal static class BoundedTool
{
    internal static async Task<ToolResult> RunAsync(RepairStep step, TimeSpan limit,
        IProgress<string>? progress = null, CancellationToken token = default) =>
        await RunCoreAsync(step, limit, progress, token, null).ConfigureAwait(false);

    internal static async Task<ToolResult> RunWithInputAsync(RepairStep step, TimeSpan limit, string standardInput,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(standardInput);
        return await RunCoreAsync(step, limit, progress, token, standardInput).ConfigureAwait(false);
    }

    private static async Task<ToolResult> RunCoreAsync(RepairStep step, TimeSpan limit,
        IProgress<string>? progress, CancellationToken token, string? standardInput)
    {
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromHours(2)) throw new ArgumentOutOfRangeException(nameof(limit));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(limit);
        try
        {
            return standardInput is null
                ? await NativeTool.RunAsync(step.Tool, step.Arguments, unicode: step.Tool == "sfc.exe",
                    progress: progress, token: timeout.Token).ConfigureAwait(false)
                : await NativeTool.RunWithInputAsync(step.Tool, step.Arguments, standardInput,
                    unicode: step.Tool == "sfc.exe", progress: progress, token: timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(-1, "", "Command exceeded its time limit. Windows services may still be completing work. State is unverified; reassess before another repair.", false) { TimedOut = true };
        }
    }
}
