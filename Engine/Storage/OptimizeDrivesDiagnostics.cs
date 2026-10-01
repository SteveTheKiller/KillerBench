namespace KillerBench.Engine.Storage;

public static class OptimizeDrivesDiagnostics
{
    public const string TaskPath = @"\Microsoft\Windows\Defrag\";
    public const string TaskName = "ScheduledDefrag";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        var (read, evidence) = await ExactScheduledTaskDiagnostics.ReadAsync(TaskPath, TaskName, TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
        return InterpretRead(read, evidence);
    }

    public static RepairStep CreateReadStep() => ExactScheduledTaskDiagnostics.CreateReadStep(TaskPath, TaskName);

    public static WorkflowCheck Interpret(ToolResult result, CancellationToken token = default)
    {
        var read = ExactScheduledTaskDiagnostics.Parse(result, TaskPath, TaskName, token);
        return InterpretRead(read, result);
    }

    private static WorkflowCheck InterpretRead(ExactScheduledTaskRead read, ToolResult result)
    {
        const string name = "Scheduled drive optimization";
        if (read.State == ExactScheduledTaskReadState.Unavailable)
            return new(name, CheckState.Unknown, "The exact ScheduledDefrag task could not be inspected completely. No task, schedule, service or drive changed.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Missing)
            return new(name, CheckState.Attention, "The exact built-in ScheduledDefrag task was not found. This does not authorize task creation, service recovery or drive optimization.", Evidence: result);
        if (read.State == ExactScheduledTaskReadState.Ambiguous)
            return new(name, CheckState.Unknown, "The exact ScheduledDefrag identity returned ambiguous evidence. No task, schedule, service or drive changed.", Evidence: result);
        if (read.State != ExactScheduledTaskReadState.Found || read.Task is null)
            return Invalid(result);
        var assessed = ScheduledTaskDiagnostics.Assess(read.Task);
        return assessed with
        {
            Name = name,
            Evidence = result,
            Detail = assessed.Detail + "\nThis exact task evidence never authorizes task execution, schedule changes, service recovery, volume selection, analysis, trim, defragmentation or optimization."
        };
    }

    private static WorkflowCheck Invalid(ToolResult result) => new("Scheduled drive optimization", CheckState.Unknown,
        "ScheduledDefrag task evidence was malformed or inconsistent. No task, schedule, service or drive changed.", Evidence: result);

}
