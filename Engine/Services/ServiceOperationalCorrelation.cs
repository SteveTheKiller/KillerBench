namespace KillerBench.Engine;

public static class ServiceOperationalCorrelation
{
    private static readonly string[] StructuralNames =
    [
        "Installed service inventory",
        "Service startup configuration",
        "Service binary registrations",
        "Service executable path quoting",
        "Service logon configuration",
        "Registered service DLLs",
        "Service recovery policy",
        "Service dependency configuration",
        "Service triggers",
        "Persistent service transitions"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> structural, WorkflowCheck eventCorrelation,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(structural);
        ArgumentNullException.ThrowIfNull(eventCorrelation);
        token.ThrowIfCancellationRequested();
        const string name = "Service operational correlation";
        const string boundary = " This is a bounded registration, configuration, current-state and history review. It does not start, stop or exercise a service, validate feature behavior, test credentials, inspect protected payloads, or authorize recovery. No service, executable, DLL, driver, startup setting, dependency, trigger, account, credential, permission, registry value, recovery action or file changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (eventCorrelation.Name != "Service failure correlation" || structural.Count != StructuralNames.Length ||
            structural.Select(item => item.Name).OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(StructuralNames.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) is false)
            return Unknown("Service correlation inputs were missing, duplicated or carried an unexpected identity.");

        var all = structural.Append(eventCorrelation).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed installed-service inventory, startup, binary, path, logon, DLL, recovery, dependency, trigger and persistent-transition evidence with exact current-state and failure-history correlation. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before selecting a service action.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the detailed registration, current-state and historical results together before choosing a service action. Historical failures do not establish a current outage." + boundary);
        return new(name, CheckState.Information, detail + "No issue was established within the collected scope, but feature behavior remains untested." + boundary);
    }
}
