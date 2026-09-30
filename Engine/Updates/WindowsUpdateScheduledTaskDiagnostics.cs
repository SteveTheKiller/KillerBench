namespace KillerBench.Engine;

public static class WindowsUpdateScheduledTaskDiagnostics
{
    public const string TaskPath = @"\Microsoft\Windows\WindowsUpdate\";
    public const string TaskName = "Scheduled Start";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        var read = await ExactScheduledTaskDiagnostics.ReadAsync(TaskPath, TaskName, TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
        return InterpretRead(read.Read, read.Evidence);
    }

    public static RepairStep CreateReadStep() => ExactScheduledTaskDiagnostics.CreateReadStep(TaskPath, TaskName);

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default) =>
        InterpretRead(ExactScheduledTaskDiagnostics.Parse(result, TaskPath, TaskName, token), result);

    public static WorkflowCheck Interpret(ToolResult result, Func<uint, WindowsErrorMessage?> messageLookup,
        CancellationToken token = default) =>
        InterpretRead(ExactScheduledTaskDiagnostics.Parse(result, TaskPath, TaskName, token), result, messageLookup);

    private static WorkflowCheck InterpretRead(ExactScheduledTaskRead read, ToolResult result,
        Func<uint, WindowsErrorMessage?>? messageLookup = null)
    {
        const string name = "Windows Update scheduled task";
        if (read.State == ExactScheduledTaskReadState.Unavailable)
            return new(name, CheckState.Unknown, "The exact built-in Windows Update task could not be inspected completely. No task, update, service or setting changed.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Missing)
            return new(name, CheckState.Information, "The exact built-in Windows Update Scheduled Start task was not found. Windows version, edition and managed policy can affect availability. This does not authorize task creation or an update operation.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Ambiguous)
            return new(name, CheckState.Unknown, "The exact Windows Update task identity returned ambiguous evidence. No task, update, service or setting changed.", Evidence: result);
        if (read.State != ExactScheduledTaskReadState.Found || read.Task is null)
            return new(name, CheckState.Unknown, "Windows Update task evidence was malformed or inconsistent. No task, update, service or setting changed.", Evidence: result);

        var assessed = ScheduledTaskDiagnostics.Assess(read.Task, messageLookup);
        if (read.Task.State != "Disabled" && read.Task.LastTaskResult is >= 0x80000000 && assessed.State == CheckState.Information)
            assessed = assessed with
            {
                State = CheckState.Attention,
                Detail = assessed.Detail + "\nThe enabled Windows Update task retained a failure-class result. Correlate it with servicing activity, update history and Task Scheduler history. This value alone does not establish why the task returned it."
            };
        return assessed with
        {
            Name = name,
            Evidence = result,
            Detail = assessed.Detail + "\nThis exact task evidence complements live servicing activity but does not prove an update search, download or installation is required. It never authorizes task execution, task recreation, schedule changes or an update operation."
        };
    }
}
