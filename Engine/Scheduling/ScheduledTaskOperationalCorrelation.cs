namespace KillerBench.Engine.Scheduling;

public static class ScheduledTaskOperationalCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> inventory,
        IReadOnlyList<WorkflowCheck> serviceHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();
        const string name = "Scheduled task operational conclusion";
        const string boundary = " This conclusion does not verify task actions, trigger conditions, credentials, application outcomes or future runs. No task is started, stopped, enabled, disabled, registered or changed, no service is started, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (inventory.Count is < 1 or > 2049 || inventory.Any(check => check is null))
            return Unknown("Scheduled task inventory evidence is missing or exceeds its bound.");
        var coverage = inventory.Where(check => check.Name == "Task inventory coverage").ToArray();
        var history = serviceHistory.Where(check => check.Name == "Task Scheduler service failure history").ToArray();
        if (coverage.Length != 1 || history.Length != 1)
            return Unknown("Scheduled task coverage or exact Task Scheduler service history was missing or duplicated.");

        var taskChecks = inventory.Where(check => check.Name != "Task inventory coverage").ToArray();
        int incomplete = taskChecks.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = taskChecks.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        bool incompleteSource = coverage[0].EffectiveState == CheckState.Unknown || history[0].EffectiveState == CheckState.Unknown;
        bool serviceAttention = history[0].EffectiveState is CheckState.Attention or CheckState.Repairable;
        string detail = $"Reviewed {taskChecks.Length} visible task results and exact Task Scheduler service failure history. Task results needing attention: {attention}; incomplete task results: {incomplete}; recent service failure history: {(serviceAttention ? "reported" : "not reported")}. ";
        if (incompleteSource || incomplete > 0)
            return Unknown(detail + "Recollect incomplete inventory or service evidence before drawing a combined conclusion.");
        if (attention > 0 || serviceAttention)
            return new(name, CheckState.Attention, detail + "Review the exact task results and service events before selecting a narrower diagnostic or recovery action." + boundary);
        return new(name, CheckState.Information, detail + "No collected source requested review. Successful future task execution remains unverified." + boundary);
    }
}
