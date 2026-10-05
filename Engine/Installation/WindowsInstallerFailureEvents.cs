namespace KillerBench.Engine.Installation;

public static class WindowsInstallerFailureEvents
{
    private static readonly ExactServiceFailureEventDefinition Definition = new(
        "msiserver",
        "Windows Installer service",
        "Windows Installer service failure history",
        " Records are historical observations tied to the exact local Windows Installer service identity. They do not establish a current outage, identify a failing package or authorize a service start, package repair, registration change, cache change, policy change or installation retry.");

    public static Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        ExactServiceFailureEvents.CheckAsync(Definition, execute, token: token);

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset nowUtc, CancellationToken token = default) =>
        ExactServiceFailureEvents.Interpret(Definition, evidence, nowUtc, token);
}
