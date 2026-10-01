namespace KillerBench.Engine.Storage;

public static class StorageSenseTaskDiagnostics
{
    public const string TaskPath = @"\Microsoft\Windows\DiskFootprint\";
    public const string TaskName = "StorageSense";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        var (read, evidence) = await ExactScheduledTaskDiagnostics.ReadAsync(TaskPath, TaskName, TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
        return InterpretRead(read, evidence);
    }

    public static RepairStep CreateReadStep() => ExactScheduledTaskDiagnostics.CreateReadStep(TaskPath, TaskName);

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default)
    {
        var read = ExactScheduledTaskDiagnostics.Parse(result, TaskPath, TaskName, token);
        return InterpretRead(read, result, null);
    }

    public static WorkflowCheck Interpret(ToolResult result, Func<uint, WindowsErrorMessage?> messageLookup, CancellationToken token = default) =>
        InterpretRead(ExactScheduledTaskDiagnostics.Parse(result, TaskPath, TaskName, token), result, messageLookup);

    private static WorkflowCheck InterpretRead(ExactScheduledTaskRead read, ToolResult result, Func<uint, WindowsErrorMessage?>? messageLookup = null)
    {
        const string name = "Storage Sense scheduled task";
        if (read.State == ExactScheduledTaskReadState.Unavailable)
            return new(name, CheckState.Unknown, "The exact StorageSense task could not be inspected completely. No task, cleanup operation, file or setting changed.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Missing)
            return new(name, CheckState.Information, "The exact built-in StorageSense task was not found. Windows version, edition and current policy can affect availability. This does not authorize task creation, cleanup or a setting change.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Ambiguous)
            return new(name, CheckState.Unknown, "The exact StorageSense task identity returned ambiguous evidence. No task, cleanup operation, file or setting changed.", Evidence: result);
        if (read.State != ExactScheduledTaskReadState.Found || read.Task is null)
            return Invalid(result);
        var assessed = ScheduledTaskDiagnostics.Assess(read.Task, messageLookup);
        if (read.Task.State != "Disabled" && read.Task.LastTaskResult is >= 0x80000000 && assessed.State == CheckState.Information)
            assessed = assessed with
            {
                State = CheckState.Attention,
                Detail = assessed.Detail + "\nThe enabled Windows task retained a failure-class result. Review its Task Scheduler history and Storage Sense configuration. This value alone does not establish why the task returned it or whether cleanup was needed."
            };
        return assessed with
        {
            Name = name,
            Evidence = result,
            Detail = assessed.Detail + "\nThis exact task evidence does not prove cleanup ran or that any file was eligible. It never authorizes task execution, schedule changes, cleanup, file removal or a Storage Sense setting change."
        };
    }

    private static WorkflowCheck Invalid(ToolResult result) => new("Storage Sense scheduled task", CheckState.Unknown,
        "StorageSense task evidence was malformed or inconsistent. No task, cleanup operation, file or setting changed.", Evidence: result);
}
