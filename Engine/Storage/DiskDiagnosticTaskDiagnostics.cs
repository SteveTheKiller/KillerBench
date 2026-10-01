namespace KillerBench.Engine.Storage;

public static class DiskDiagnosticTaskDiagnostics
{
    public const string TaskPath = @"\Microsoft\Windows\DiskDiagnostic\";
    public const string CollectorTaskName = "Microsoft-Windows-DiskDiagnosticDataCollector";
    public const string ResolverTaskName = "Microsoft-Windows-DiskDiagnosticResolver";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        var (collectorRead, collectorEvidence) = await ExactScheduledTaskDiagnostics.ReadAsync(TaskPath, CollectorTaskName, TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
        var (resolverRead, resolverEvidence) = await ExactScheduledTaskDiagnostics.ReadAsync(TaskPath, ResolverTaskName, TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
        return Array.AsReadOnly<WorkflowCheck>(
        [
            InterpretRead(collectorRead, collectorEvidence, "Disk diagnostic data collector task"),
            InterpretRead(resolverRead, resolverEvidence, "Disk diagnostic resolver task")
        ]);
    }

    public static RepairStep CreateCollectorReadStep() => ExactScheduledTaskDiagnostics.CreateReadStep(TaskPath, CollectorTaskName);
    public static RepairStep CreateResolverReadStep() => ExactScheduledTaskDiagnostics.CreateReadStep(TaskPath, ResolverTaskName);

    public static WorkflowCheck InterpretCollector(ToolResult result, Func<uint, WindowsErrorMessage?>? messageLookup = null, CancellationToken token = default) =>
        InterpretRead(ExactScheduledTaskDiagnostics.Parse(result, TaskPath, CollectorTaskName, token), result, "Disk diagnostic data collector task", messageLookup);

    public static WorkflowCheck InterpretResolver(ToolResult result, Func<uint, WindowsErrorMessage?>? messageLookup = null, CancellationToken token = default) =>
        InterpretRead(ExactScheduledTaskDiagnostics.Parse(result, TaskPath, ResolverTaskName, token), result, "Disk diagnostic resolver task", messageLookup);

    private static WorkflowCheck InterpretRead(ExactScheduledTaskRead read, ToolResult result, string name,
        Func<uint, WindowsErrorMessage?>? messageLookup = null)
    {
        if (read.State == ExactScheduledTaskReadState.Unavailable)
            return new(name, CheckState.Unknown, "The exact built-in disk diagnostic task could not be inspected completely. No task, disk, file or setting changed.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Missing)
            return new(name, CheckState.Information, "The exact built-in disk diagnostic task was not found. Windows version, edition and policy can affect availability. This does not authorize task creation or disk activity.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Ambiguous)
            return new(name, CheckState.Unknown, "The exact disk diagnostic task identity returned ambiguous evidence. No task, disk, file or setting changed.", Evidence: result);
        if (read.State != ExactScheduledTaskReadState.Found || read.Task is null)
            return new(name, CheckState.Unknown, "Disk diagnostic task evidence was malformed or inconsistent. No task, disk, file or setting changed.", Evidence: result);

        var assessed = ScheduledTaskDiagnostics.Assess(read.Task, messageLookup);
        if (read.Task.State != "Disabled" && read.Task.LastTaskResult is >= 0x80000000 && assessed.State == CheckState.Information)
            assessed = assessed with
            {
                State = CheckState.Attention,
                Detail = assessed.Detail + "\nThe enabled Windows disk diagnostic task retained a failure-class result. Review its Task Scheduler and disk diagnostic history. This value alone does not establish a disk fault or why the task returned it."
            };
        return assessed with
        {
            Name = name,
            Evidence = result,
            Detail = assessed.Detail + "\nThis task evidence does not prove a disk is healthy, failing or repaired. It never authorizes task execution, schedule changes, disk tests, storage repair, formatting, cleanup, file removal or a storage setting change."
        };
    }
}
