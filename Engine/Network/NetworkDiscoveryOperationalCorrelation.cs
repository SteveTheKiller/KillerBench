namespace KillerBench.Engine.Network;

public static class NetworkDiscoveryOperationalCorrelation
{
    public static bool Supports(WorkflowKind kind) => kind is WorkflowKind.FunctionDiscoveryProviderHostService or
        WorkflowKind.FunctionDiscoveryResourcePublicationService;

    public static WorkflowCheck Check(WorkflowKind kind, IReadOnlyList<WorkflowCheck> checks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        var (title, excludedBehavior) = Definition(kind);
        const string serviceName = "Focused service readiness";
        string boundary = " This conclusion combines current network profile assignments with the exact Function Discovery service conclusion and its retained Service Control Manager history. It does not prove " +
            excludedBehavior + ". It identifies no root cause, selects no repair and changes no network profile, interface, firewall rule, share, resource, provider, service configuration, permission, policy, credential or user data.";
        WorkflowCheck Unknown(string detail) => new(title + " readiness", CheckState.Unknown, detail + boundary);

        if (checks.Count == 0 || checks.Any(check => check is null))
            return Unknown("Network discovery correlation evidence is missing or invalid.");

        var profiles = checks.Where(check => check.Name == "Network profiles" ||
            check.Name.StartsWith("Network profile: ", StringComparison.Ordinal)).ToArray();
        var service = checks.Where(check => check.Name == serviceName).ToArray();
        if (profiles.Length == 0 || service.Length != 1 || profiles.Select(check => check.Name).Any(string.IsNullOrWhiteSpace))
            return Unknown("The expected network profile or focused-service conclusion was missing or duplicated.");

        int incomplete = profiles.Count(check => check.EffectiveState == CheckState.Unknown) +
            (service[0].EffectiveState == CheckState.Unknown ? 1 : 0);
        int attention = profiles.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable) +
            (service[0].EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0);
        string counts = $"Reviewed {profiles.Length} network profile result{(profiles.Length == 1 ? "" : "s")} plus the exact service conclusion. Results needing attention: {attention}; incomplete results: {incomplete}. ";

        if (incomplete > 0)
            return Unknown(counts + "Recollect the incomplete profile or service evidence before drawing a discovery-readiness conclusion.");
        if (attention > 0)
            return new(title + " readiness", CheckState.Attention,
                counts + "Review Public-profile trust and the current service finding before changing discovery behavior." + boundary);
        return new(title + " readiness", CheckState.Information,
            counts + "No current profile-assignment or service-availability concern was established within the checked scope." + boundary);
    }

    private static (string Title, string ExcludedBehavior) Definition(WorkflowKind kind) => kind switch
    {
        WorkflowKind.FunctionDiscoveryProviderHostService => ("Function Discovery provider", "provider health, device discovery, neighboring-device visibility, name resolution, firewall effectiveness or connectivity"),
        WorkflowKind.FunctionDiscoveryResourcePublicationService => ("Function Discovery publication", "resource publication, this PC's visibility, share access, name resolution, firewall effectiveness or connectivity"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
