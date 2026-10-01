namespace KillerBench.Engine.Evidence;

public static class EventCollectorOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Event Collector subscriptions",
        "Windows Event Collector service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Event Collector conclusion";
        const string boundary = " This bounded conclusion does not establish source reachability, authentication, event delivery, event freshness or subscription health. It does not start a service, create or change a subscription, open forwarded event contents, change credentials, channels or policy, or select a repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>(RequiredNames.Length);
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Windows Event Collector inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        var service = selected.Single(check => check.Name == "Focused service readiness");
        var history = selected.Single(check => check.Name == "Windows Event Collector service failure history");
        int subscriptionCount = ReadSubscriptionCount(selected.Single(check => check.Name == "Event Collector subscriptions").Detail);
        string detail = $"Reviewed bounded subscription inventory, exact service-failure history and current service readiness. Reported subscriptions: {(subscriptionCount >= 0 ? subscriptionCount.ToString() : "available in evidence")}; historical service findings: {(history.EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0)}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete subscription or service evidence before interpreting collector readiness.");
        if (service.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Attention,
                detail + " Review current service readiness. Subscription count and historical failures cannot authorize recovery or subscription changes." + boundary);
        return new(name, CheckState.Information,
            detail + " No current service concern was established within the collected evidence. An empty subscription inventory can be intentional, and retained failures remain historical context." + boundary);
    }

    private static int ReadSubscriptionCount(string detail)
    {
        if (detail.StartsWith("No Windows Event Collector subscriptions", StringComparison.Ordinal)) return 0;
        int separator = detail.IndexOf(' ');
        return separator > 0 && int.TryParse(detail.AsSpan(0, separator), out int count) ? count : -1;
    }
}
