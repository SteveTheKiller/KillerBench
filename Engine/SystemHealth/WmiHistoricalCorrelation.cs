namespace KillerBench.Engine.SystemHealth;

public static class WmiHistoricalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck activity, WorkflowCheck serviceHistory,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "WMI historical correlation";
        const string boundary = " These bounded histories do not establish current repository consistency, namespace availability, provider behavior, client behavior or service state. Raw WMI event evidence can contain private client context and remains separate from the ordinary summary. No repository salvage, reset, service start, provider registration, client change or repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (activity.Name != "Recent WMI activity events" || serviceHistory.Name != "WMI service failure history")
            return Unknown("WMI history inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] all = [activity, serviceHistory];
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed recent WMI Activity records and exact Windows Management Instrumentation service history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete activity or service-history evidence before interpreting retained failures together.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review exact retained events with fresh repository, provider and service evidence before selecting a focused action." + boundary);
        return new(name, CheckState.Information,
            detail + " No retained historical observation requested review. Current WMI operation remains independently verified by focused checks." + boundary);
    }
}
