namespace KillerBench.Engine.Printing;

public static class PrinterDiagnostics
{
    public static async Task<PrinterOfflineSnapshot?> DiscoverAsync(CancellationToken token = default,
        IProgress<string>? progress = null, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        progress?.Report("Discovering local printer queues...");
        var result = await (execute ?? ((command, ct) => BoundedTool.RunAsync(command, TimeSpan.FromSeconds(40), progress, ct)))
            (PrinterOfflineRecovery.Step(true), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        deadline.Token.ThrowIfCancellationRequested();
        var snapshot = PrinterOfflineRecovery.Read(result);
        return snapshot is { Targets.Length: 0 } ? snapshot : null;
    }

    public static async Task<WorkflowCheck> CheckOfflineAsync(IReadOnlyList<string> selectedQueues,
        CancellationToken token = default, IProgress<string>? progress = null,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        ArgumentNullException.ThrowIfNull(selectedQueues);
        var selected = selectedQueues.ToArray();
        var step = PrinterOfflineRecovery.Step(true, selected);
        token.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        progress?.Report("Reading offline settings for selected local queues...");
        ToolResult result;
        try
        {
            result = await (execute ?? ((command, ct) => BoundedTool.RunAsync(command, TimeSpan.FromSeconds(40), progress, ct)))
                (step, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return new("Selected printer offline settings", CheckState.Unknown,
                "Selected queue inspection timed out. Offline settings remain unverified; no settings changed.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            return new("Selected printer offline settings", CheckState.Unknown,
                "Selected queue inspection failed. Offline settings remain unverified; no settings changed.",
                Evidence: new(-1, "", error.Message, false));
        }
        deadline.Token.ThrowIfCancellationRequested();
        if (result is null)
            return new("Selected printer offline settings", CheckState.Unknown,
                "Selected queue evidence is missing. No printer settings changed.");
        var snapshot = PrinterOfflineRecovery.Read(result);
        if (snapshot is null || snapshot.Targets.Length != 0 || snapshot.Queues.Count != selected.Length ||
            !selected.All(snapshot.Queues.ContainsKey))
            return new("Selected printer offline settings", CheckState.Unknown,
                "The selected queue set could not be verified. No printer settings changed.", Evidence: result);
        return PrinterOfflineRecovery.Interpret(result);
    }
}
