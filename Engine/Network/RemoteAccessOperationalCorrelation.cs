namespace KillerBench.Engine;

public static class RemoteAccessOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Current-account VPN profiles",
        "Remote Access Connection Manager failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Remote Access readiness";
        const string boundary = " This bounded conclusion does not establish profile applicability, server reachability, authentication, tunnel negotiation, route installation, name resolution or connectivity. It does not initiate a connection, start a service, expose credentials, change a profile, adapter, route, firewall or policy setting, or select a repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>(RequiredNames.Length);
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Remote Access inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        var service = selected.Single(check => check.Name == "Focused service readiness");
        var history = selected.Single(check => check.Name == "Remote Access Connection Manager failure history");
        int profileCount = ReadProfileCount(selected.Single(check => check.Name == "Current-account VPN profiles").Detail);
        string detail = $"Reviewed current-account VPN profile inventory, exact service-failure history and current service readiness. Reported profiles: {(profileCount >= 0 ? profileCount.ToString() : "available in evidence")}; historical service findings: {(history.EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0)}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete profile or service evidence before interpreting Remote Access readiness.");
        if (service.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Attention,
                detail + " Review current service readiness. Profile presence, connection labels and historical failures cannot authorize recovery or a connection attempt." + boundary);
        return new(name, CheckState.Information,
            detail + " No current service concern was established within the collected evidence. An empty profile inventory can be intentional, connection labels are snapshots, and retained failures remain historical context." + boundary);
    }

    private static int ReadProfileCount(string detail)
    {
        int separator = detail.IndexOf(' ');
        return separator > 0 && int.TryParse(detail.AsSpan(0, separator), out int count) ? count : -1;
    }
}
