using KillerBench.Engine;

internal static class UpdateOrchestratorOperationalCorrelationChecks
{
    public static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Update Orchestrator evidence remains informational");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention,
            "Current Update Orchestrator service concern requests review");
        Require(Check(task: CheckState.Attention).State == CheckState.Attention,
            "Windows Update task concern requests review");
        Require(Check(activity: CheckState.Attention).State == CheckState.Attention,
            "Windows Update activity concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Attention,
            "Historical Update Orchestrator failures request review");
        Require(Check(service: CheckState.Unknown, activity: CheckState.Attention).State == CheckState.Unknown,
            "Incomplete Update Orchestrator evidence takes precedence over attention");
        Require(UpdateOrchestratorOperationalCorrelation.Check(
            Info("Wrong service"), Task(), Activity(), History()).State == CheckState.Unknown,
            "Unexpected service identity fails closed");
        Require(UpdateOrchestratorOperationalCorrelation.Check(
            Service(), Info("Wrong task"), Activity(), History()).State == CheckState.Unknown,
            "Unexpected task identity fails closed");
        Require(UpdateOrchestratorOperationalCorrelation.Check(
            Service(), Task(), Info("Wrong activity"), History()).State == CheckState.Unknown,
            "Unexpected activity identity fails closed");
        Require(UpdateOrchestratorOperationalCorrelation.Check(
            Service(), Task(), Activity(), Info("Wrong history")).State == CheckState.Unknown,
            "Unexpected history identity fails closed");
        try
        {
            UpdateOrchestratorOperationalCorrelation.Check(Service(), Task(), Activity(), History(), new CancellationToken(true));
            throw new Exception("Canceled Update Orchestrator correlation completed");
        }
        catch (OperationCanceledException) { }
        var attention = Check(task: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize a service start or update operation", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish update applicability", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not start a task", StringComparison.Ordinal),
            "Update Orchestrator conclusion preserves service and update boundaries");
        Console.WriteLine("PASS: Update Orchestrator service, exact task, live activity and history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState service = CheckState.Information,
        CheckState task = CheckState.Information, CheckState activity = CheckState.Information,
        CheckState history = CheckState.Information) =>
        UpdateOrchestratorOperationalCorrelation.Check(Service(service), Task(task), Activity(activity), History(history));

    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new("Update Orchestrator Service state", state, "fixture");
    private static WorkflowCheck Task(CheckState state = CheckState.Information) =>
        new("Windows Update scheduled task", state, "fixture");
    private static WorkflowCheck Activity(CheckState state = CheckState.Information) =>
        new("Windows Update live activity", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("Update Orchestrator failure history", state, "fixture");
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
