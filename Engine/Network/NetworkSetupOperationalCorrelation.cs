namespace KillerBench.Engine.Network;

public static class NetworkSetupOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Network adapter bindings",
        "Network Setup service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Network Setup readiness";
        const string boundary = " This bounded conclusion does not establish adapter availability, driver suitability, pending setup completion, address assignment, route selection, name resolution, firewall access or connectivity. It does not install, remove, roll back or select a driver, device, adapter, binding, address, route, DNS server, firewall rule, service or policy change, and it selects no repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>();
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Network Setup inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        bool bindingAttention = selected.Single(check => check.Name == "Network adapter bindings").EffectiveState is CheckState.Attention or CheckState.Repairable;
        bool serviceAttention = selected.Single(check => check.Name == "Focused service readiness").EffectiveState is CheckState.Attention or CheckState.Repairable;
        int historicalFindings = selected.Count(check => check.Name == "Network Setup service failure history" &&
            check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed current adapter protocol bindings, exact service-failure history and current service readiness. Binding results needing review: {(bindingAttention ? 1 : 0)}; current service results needing attention: {(serviceAttention ? 1 : 0)}; historical findings: {historicalFindings}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete binding, history or service evidence before interpreting Network Setup readiness.");
        if (bindingAttention || serviceAttention)
            return new(name, CheckState.Attention, detail + " Review the exact active adapter binding or service concern. Binding state and historical failures cannot authorize service recovery or a setup change." + boundary);
        return new(name, CheckState.Information, detail + " No current adapter-binding or exact-service concern was established. Retained service failures remain context for narrower setup review." + boundary);
    }
}
