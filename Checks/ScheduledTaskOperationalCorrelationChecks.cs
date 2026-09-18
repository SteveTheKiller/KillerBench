using KillerBench.Engine;

internal static class ScheduledTaskOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Invoke();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("2 visible task results"),
            "Complete task evidence returns one bounded informational conclusion");
        Require(Invoke(tasks: [Info("Task inventory coverage"), Attention("\\Vendor\\Task")]).State == CheckState.Attention,
            "A task result needing review produces attention");
        Require(Invoke(history: [Attention("Task Scheduler service failure history")]).State == CheckState.Attention,
            "Exact service history produces attention");
        Require(Invoke(tasks: [new("Task inventory coverage", CheckState.Unknown, "fixture")]).State == CheckState.Unknown,
            "Incomplete inventory takes precedence");
        Require(Invoke(history: []).State == CheckState.Unknown &&
            Invoke(history: [Info("Task Scheduler service failure history"), Info("Task Scheduler service failure history")]).State == CheckState.Unknown,
            "Missing and duplicate service identities fail closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Task correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: scheduled task operational correlation combines bounded runtime results and exact service history without selecting changes.");
    }

    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? tasks = null,
        IReadOnlyList<WorkflowCheck>? history = null, CancellationToken token = default) =>
        ScheduledTaskOperationalCorrelation.Check(tasks ?? [Info("Task inventory coverage"), Info("\\One"), Info("\\Two")],
            history ?? [Info("Task Scheduler service failure history")], token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static WorkflowCheck Attention(string name) => new(name, CheckState.Attention, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
