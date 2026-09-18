namespace KillerBench.Engine;

public static class TimeOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck configuration, WorkflowCheck events,
        WorkflowCheck serviceHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "Windows Time operational correlation";
        const string boundary = " This bounded correlation does not measure clock offset, absolute accuracy, source trust, source reachability, network delay, domain convergence or future synchronization. Historical failures do not establish a current outage. No clock, source, service, policy, registry or synchronization setting changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (configuration.Name != "Time source configuration" || events.Name != "Recent time-service events" ||
            serviceHistory.Name != "Windows Time service failure history")
            return Unknown("Windows Time correlation inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] all = [configuration, events, serviceHistory];
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed source configuration, recent Windows Time operational history and exact service history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete configuration or history before interpreting time readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact source and historical results with a fresh synchronization-status check before selecting a focused action." + boundary);
        return new(name, CheckState.Information,
            detail + " No correlated observation requested review. Current synchronization and clock accuracy remain independently unverified." + boundary);
    }
}
