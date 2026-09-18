namespace KillerBench.Engine;

public static class FocusedDefenderOperationalCorrelation
{
    private static readonly string[] RequiredNames =
    [
        "TPM readiness", "Secure Boot", "Virtualization-based security", "Memory integrity", "Credential Guard",
        "AppLocker Executable", "AppLocker Windows Installer", "AppLocker Script", "AppLocker DLL", "AppLocker Packaged app",
        "Defender active protection", "Behavior monitoring", "Downloaded-file scanning", "Network inspection system",
        "Tamper protection", "Potentially unwanted application protection", "Network protection", "Cloud protection",
        "Automatic sample submission", "Controlled folder access", "Microsoft Defender threat history", "Microsoft Defender scan history",
        "Recent Microsoft Defender events", "Administrator", "Restart"
    ];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Microsoft Defender focused conclusion";
        const string boundary = " This is a bounded configuration and history review, not a malware scan or proof of protection, compliance, threat removal, trustworthiness or future security. No scan, quarantine, exclusion, signature, application-control, policy, service, hardware or restart state changed, and no additional repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        var selected = RequiredNames.Select(required => evidence.Where(item => item.Name == required).ToArray()).ToArray();
        if (selected.Any(matches => matches.Length != 1))
        {
            string mismatches = string.Join("; ", RequiredNames.Select((required, index) => (required, selected[index].Length))
                .Where(item => item.Length != 1).Select(item => item.required + ": " + item.Length));
            return Unknown("Focused Defender inputs were missing or duplicated: " + mismatches + ".");
        }
        var checks = selected.Select(matches => matches[0]).ToArray();
        int incomplete = checks.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {checks.Length} exact hardware, application-control, Defender protection, threat-history, scan-history, event, administrator and restart results. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0) return Unknown(detail + "Recollect incomplete evidence before drawing a focused Defender conclusion.");
        if (attention > 0) return new(name, CheckState.Attention,
            detail + "Review the detailed current state, policy ownership and historical evidence before choosing a targeted security action." + boundary);
        return new(name, CheckState.Information,
            detail + "No collected source requested review. Effective protection and malware absence remain unverified." + boundary);
    }
}
