namespace KillerBench.Engine.Updates;

public static class DeliveryOptimizationOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck activity, WorkflowCheck policy,
        WorkflowCheck history,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();
        const string name = "Delivery Optimization conclusion";
        const string boundary = " This conclusion does not prove transfer progress, cache health, peer reachability, update applicability or update completion. It does not start or stop the service, continue a paused service, change a transfer, clear the cache, or change peer, bandwidth, source, policy or startup settings. No repair is selected.";

        if (service.Name != "Delivery Optimization" || activity.Name != "Delivery Optimization activity" ||
            policy.Name != "Delivery Optimization policy" || history.Name != "Delivery Optimization failure history")
            return new(name, CheckState.Unknown,
                "Delivery Optimization inputs were missing or did not match the required service, activity, policy and exact history evidence." + boundary);

        var checks = new[] { service, activity, policy, history };
        int incomplete = checks.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed exact service state, privacy-bounded current transfer aggregates, recognized policy surfaces and exact failure history. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return new(name, CheckState.Unknown,
                detail + "Recollect incomplete evidence before interpreting Delivery Optimization behavior or choosing the focused recovery." + boundary);
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + "Review the detailed current, policy and historical results. Historical failures cannot authorize continuation. Only exact Paused current service evidence can make the separate continuation recipe eligible after its remaining prerequisites pass." + boundary);
        return new(name, CheckState.Information,
            detail + "No current concern was established within the collected Delivery Optimization evidence." + boundary);
    }
}
