namespace KillerBench.Engine.Printing;

public static class PrintSpoolerFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition Definition = new(
        "Spooler",
        "Print Spooler",
        "Print Spooler failure history",
        " Records are historical observations tied to the local Print Spooler service identity. They do not establish a current outage, identify a failing driver or authorize a service restart, queue change, job action, driver replacement or print setting change.");

    public static Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        ExactServiceFailureEvents.CheckAsync(Definition, execute, token: token);

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset nowUtc, CancellationToken token = default) =>
        ExactServiceFailureEvents.Interpret(Definition, evidence, nowUtc, token);
}
