namespace KillerBench.Engine.Security;

public static class SecurityCenterOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Registered antivirus products",
        "Security Center service failure history",
        "Focused service readiness"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Security Center readiness";
        const string boundary = " This bounded conclusion does not interpret raw provider state or establish antivirus presence, protection state, signature freshness, notification delivery, registration ownership or effective policy. It does not start a service, scan the system, update signatures, change a provider registration, protection setting, exclusion, notification or policy, or select a repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = new List<WorkflowCheck>(RequiredNames.Length);
        foreach (string required in RequiredNames)
        {
            var matches = checks.Where(check => check is not null && check.Name == required).ToArray();
            if (matches.Length != 1)
                return Unknown("Windows Security Center inputs were missing, duplicated or carried an unexpected identity.");
            selected.Add(matches[0]);
        }

        int incomplete = selected.Count(check => check.EffectiveState == CheckState.Unknown);
        var registration = selected.Single(check => check.Name == "Registered antivirus products");
        var history = selected.Single(check => check.Name == "Security Center service failure history");
        var service = selected.Single(check => check.Name == "Focused service readiness");
        int productCount = ReadProductCount(registration.Detail);
        string detail = $"Reviewed bounded antivirus registrations, exact service-failure history and current Security Center service readiness. Reported registrations: {(productCount >= 0 ? productCount.ToString() : "available in evidence")}; historical service findings: {(history.EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0)}; incomplete results: {incomplete}.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete registration or service evidence before interpreting Security Center readiness.");
        if (service.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Attention,
                detail + " Review current service readiness. Registration count, raw provider state and historical failures cannot authorize recovery or a security change." + boundary);
        return new(name, CheckState.Information,
            detail + " No current service concern was established within the collected evidence. An empty registration inventory is not a protection-health verdict, and retained failures remain historical context." + boundary);
    }

    private static int ReadProductCount(string detail)
    {
        int separator = detail.IndexOf(' ');
        return separator > 0 && int.TryParse(detail.AsSpan(0, separator), out int count) ? count : -1;
    }
}
