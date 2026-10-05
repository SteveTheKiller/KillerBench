namespace KillerBench.Engine.Storage;

public static class DiskDiagnosticCorrelation
{
    private static readonly string[] TaskNames = ["Disk diagnostic data collector task", "Disk diagnostic resolver task"];
    private static readonly string[] HistoryNames = ["Disk diagnostic operational history", "Disk diagnostic collector history", "Disk diagnostic resolver history"];

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var taskRead = DiskDiagnosticTaskDiagnostics.CheckAsync(token);
        var historyRead = DiskDiagnosticEvents.CheckAsync(token: token);
        await Task.WhenAll(taskRead, historyRead).ConfigureAwait(false);
        var tasks = await taskRead.ConfigureAwait(false);
        var histories = await historyRead.ConfigureAwait(false);
        return Array.AsReadOnly(tasks.Concat(histories).Append(Check(tasks, histories, token)).ToArray());
    }

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> tasks, IReadOnlyList<WorkflowCheck> histories,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(histories);
        bool exact = tasks.Count == TaskNames.Length && histories.Count == HistoryNames.Length &&
            TaskNames.All(name => tasks.Count(item => item.Name == name) == 1) &&
            HistoryNames.All(name => histories.Count(item => item.Name == name) == 1);
        if (!exact)
            return Result(CheckState.Unknown, "Exact task or event-channel evidence is missing, duplicated or inconsistent. No combined disk diagnostic conclusion was made.");
        var all = tasks.Concat(histories).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState == CheckState.Attention);
        CheckState state = incomplete > 0 ? CheckState.Unknown : attention > 0 ? CheckState.Attention : CheckState.Information;
        string detail = $"Correlated 2 exact task states with 3 exact event channels. {attention} source{(attention == 1 ? "" : "s")} request review; {incomplete} source{(incomplete == 1 ? " is" : "s are")} incomplete. ";
        detail += state == CheckState.Attention
            ? "Review the retained task result and matching event history together with current device, storage-provider and backup evidence. "
            : state == CheckState.Unknown
                ? "Recollect the incomplete source before drawing a disk diagnostic conclusion. "
                : "No retained warning, error or critical event was reported in the collected window, but absence of history is not a disk-health verdict. ";
        return Result(state, detail + "This correlation does not identify a failing disk, prove health or authorize task execution, disk testing, repair, formatting, cleanup, file removal or a storage setting change.");
    }

    private static WorkflowCheck Result(CheckState state, string detail) =>
        new("Windows disk diagnostic correlation", state, detail);
}
