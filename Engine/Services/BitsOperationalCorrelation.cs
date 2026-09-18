namespace KillerBench.Engine;

public static class BitsOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck jobs, WorkflowCheck service,
        WorkflowCheck serviceHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "Background transfer operational correlation";
        const string boundary = " This bounded correlation does not prove transfer progress, a stalled job, application ownership, application acknowledgment or application completion. No job is resumed, completed, canceled, deleted or reassigned; no service, proxy, priority, destination, cache, policy or network setting changes; and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (jobs.Name != "BITS transfer jobs" ||
            service.Name != "Background Intelligent Transfer Service state" ||
            serviceHistory.Name != "BITS service failure history")
            return Unknown("Background transfer correlation inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] all = [jobs, service, serviceHistory];
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed the selected BITS job scope, exact current service state and bounded exact service-failure history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete job, service or history evidence before interpreting background-transfer readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact job, current service and historical results before selecting a focused retry or service action." + boundary);
        return new(name, CheckState.Information,
            detail + " No correlated observation requested review. Actual transfer progress and owning-application completion remain unverified." + boundary);
    }
}
