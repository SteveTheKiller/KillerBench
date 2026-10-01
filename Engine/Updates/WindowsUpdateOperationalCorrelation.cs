namespace KillerBench.Engine.Updates;

public static class WindowsUpdateOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Windows Update servicing-chain correlation",
        "Restart",
        "Servicing transition readiness",
        "Volume capacity",
        "Windows reserved storage",
        "BITS transfer jobs",
        "Network operational correlation"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Update operational correlation";
        const string boundary = " This is a bounded prerequisite review. It does not search for updates, establish applicability, download or install content, validate every policy, prove component-store health, clear state, restart Windows or authorize repair. No service, DLL, update, transfer, cache, source, policy, package, capability, feature, file, network or restart state changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = RequiredNames.Select(required => evidence.Where(item => item.Name == required).ToArray()).ToArray();
        if (selected.Any(matches => matches.Length != 1))
        {
            string mismatches = string.Join("; ", RequiredNames.Select((required, index) => (required, selected[index].Length))
                .Where(item => item.Length != 1).Select(item => item.required + ": " + item.Length));
            return Unknown("Windows Update correlation inputs were missing or duplicated: " + mismatches + ".");
        }

        var checks = selected.Select(matches => matches[0]).ToArray();
        int incomplete = checks.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed the exact servicing chain, restart and servicing-transition state, system-volume capacity, reserved storage, BITS jobs and network operational result. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before selecting an update or servicing action.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the detailed prerequisite results together before searching for updates or choosing a focused repair." + boundary);
        return new(name, CheckState.Information, detail + "No prerequisite issue was established within the collected scope." + boundary);
    }
}
