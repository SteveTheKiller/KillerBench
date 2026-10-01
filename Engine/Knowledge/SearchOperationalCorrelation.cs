namespace KillerBench.Engine.Knowledge;

public static class SearchOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck catalog, WorkflowCheck events,
        WorkflowCheck serviceHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "Windows Search operational correlation";
        const string boundary = " This bounded correlation does not issue a query or prove indexed-location coverage, result completeness, ranking, freshness, application integration or file accessibility. Historical failures do not establish a current outage. No index, service, file, location, policy, database, query or Search setting changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (catalog.Name != "Windows Search catalog" || events.Name != "Recent Windows Search events" ||
            serviceHistory.Name != "Windows Search service failure history")
            return Unknown("Windows Search correlation inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] all = [catalog, events, serviceHistory];
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed current SystemIndex activity, recent Search application events and exact Windows Search service history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete catalog or event evidence before drawing a combined Search-readiness conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact current and historical results before selecting a focused service or index action." + boundary);
        return new(name, CheckState.Information,
            detail + " No correlated observation requested review. Search result completeness and freshness remain unverified." + boundary);
    }
}
