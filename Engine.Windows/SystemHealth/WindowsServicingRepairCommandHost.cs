namespace KillerBench.Engine.Windows.SystemHealth;

public sealed class WindowsServicingRepairCommandHost : IServicingRepairCommandHost
{
    public Task<WorkflowCheck> CheckAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
        new WindowsWorkflowHost().CheckIntegrityAsync(kind, progress, token);

    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token) =>
        BoundedTool.RunAsync(step, TimeSpan.FromHours(1), progress, token);
}
