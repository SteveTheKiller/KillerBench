namespace KillerBench.Engine;

public static class FileSharingOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck serverConfiguration, IReadOnlyList<WorkflowCheck> shares,
        WorkflowCheck firewallAlignment, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(serverConfiguration);
        ArgumentNullException.ThrowIfNull(shares);
        ArgumentNullException.ThrowIfNull(firewallAlignment);
        token.ThrowIfCancellationRequested();

        const string name = "File sharing operational correlation";
        const string boundary = " This bounded correlation does not test share permissions, authentication, name resolution, client negotiation, transport reachability, file access, data transfer, third-party filtering or policy ownership. No share, permission, session, file, SMB setting, firewall profile, rule, service or network setting changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (serverConfiguration.Name != "SMB server configuration" || shares.Count == 0 ||
            shares.Any(item => !item.Name.StartsWith("SMB share", StringComparison.Ordinal)) ||
            firewallAlignment.Name != "Active network firewall alignment")
            return Unknown("File sharing correlation inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] all = [serverConfiguration, .. shares, firewallAlignment];
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed SMB server configuration, {shares.Count} bounded share result(s), and active network/firewall alignment. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete SMB, share or profile evidence before interpreting file-sharing readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact configuration, share and profile results before selecting a focused service or firewall action." + boundary);
        return new(name, CheckState.Information,
            detail + " No correlated observation requested review. Actual client access and data transfer remain unverified." + boundary);
    }
}
