namespace KillerBench.Engine.Updates;

public static class WindowsUpdateServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition Definition = new(
        "wuauserv",
        "Windows Update",
        "Windows Update service failure history",
        " Records are historical observations tied to the exact local Windows Update service identity. They do not establish a current outage, identify a failed update or authorize a service start, update retry, source change, cache reset, policy change or repair.");

    public static Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null) =>
        ExactServiceFailureEvents.CheckAsync(Definition, token, execute);

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset nowUtc, CancellationToken token = default) =>
        ExactServiceFailureEvents.Interpret(Definition, evidence, nowUtc, token);
}
