namespace KillerBench.Engine;

public static class FocusedWindowsUpdateOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "Windows Update servicing-chain correlation",
        "Windows Update engine files",
        "Restart",
        "Servicing transition readiness",
        "System disk",
        "Windows reserved storage",
        "BITS transfer jobs",
        "Update policy",
        "Component repair source"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Update focused conclusion";
        const string boundary = " This focused conclusion does not test external update-source or Internet reachability, search for updates, establish applicability, download or install content, prove component-store health, restart Windows, or authorize another repair. No service, DLL, transfer, cache, source, policy, package, capability, feature, file, network or restart state changed, and no additional repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = RequiredNames.Select(required => evidence.Where(item => item.Name == required).ToArray()).ToArray();
        if (selected.Any(matches => matches.Length != 1))
        {
            string mismatches = string.Join("; ", RequiredNames.Select((required, index) => (required, selected[index].Length))
                .Where(item => item.Length != 1).Select(item => item.required + ": " + item.Length));
            return Unknown("Focused Windows Update inputs were missing or duplicated: " + mismatches + ".");
        }

        var checks = selected.Select(matches => matches[0]).ToArray();
        int incomplete = checks.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed the exact servicing chain, update engine files, restart and servicing-transition state, system-disk capacity, reserved storage, BITS jobs, managed update policy and component repair source. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before searching for updates or choosing a focused servicing action.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the detailed current and historical results together before searching for updates or choosing a focused action." + boundary);
        return new(name, CheckState.Information, detail + "No issue was established within the focused local Windows Update evidence." + boundary);
    }
}
