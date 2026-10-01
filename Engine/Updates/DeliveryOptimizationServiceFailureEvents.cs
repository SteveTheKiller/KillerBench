namespace KillerBench.Engine.Updates;

public static class DeliveryOptimizationServiceFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition Definition = new(
        "DoSvc",
        "Delivery Optimization",
        "Delivery Optimization failure history",
        " Records are historical observations tied to the exact local Delivery Optimization service identity. They do not establish a current outage, identify a failed transfer or authorize a service continue, start, transfer change, cache action, source change, policy change or repair.");

    public static Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null) =>
        ExactServiceFailureEvents.CheckAsync(Definition, token, execute);

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset nowUtc,
        CancellationToken token = default) =>
        ExactServiceFailureEvents.Interpret(Definition, evidence, nowUtc, token);
}
