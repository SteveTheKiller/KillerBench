namespace KillerBench.Engine;

public static class UpdateOrchestratorOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck task,
        WorkflowCheck activity, WorkflowCheck history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();

        const string name = "Update Orchestrator conclusion";
        const string boundary = " This bounded conclusion does not establish update applicability, download progress, installation progress, completion or restart readiness. It does not start a task, select, approve, download or install an update, request a restart, change policy or scheduling, start a service, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (service.Name != "Update Orchestrator Service state" ||
            task.Name != "Windows Update scheduled task" ||
            activity.Name != "Windows Update live activity" ||
            history.Name != "Update Orchestrator failure history")
            return Unknown("Update Orchestrator inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] checks = [service, task, activity, history];
        int incomplete = checks.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed exact current service state, the exact built-in Windows Update task, bounded current activity and exact service-failure history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete service, task, activity or history evidence before interpreting orchestration readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the detailed evidence before selecting a focused service action. Task results, update activity and historical failures cannot authorize a service start or update operation." + boundary);
        return new(name, CheckState.Information,
            detail + " No current concern was established within the collected evidence." + boundary);
    }
}
