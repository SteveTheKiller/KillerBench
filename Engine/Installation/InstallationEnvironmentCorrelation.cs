namespace KillerBench.Engine.Installation;

public static class InstallationEnvironmentCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck readiness, WorkflowCheck securityPolicy, WorkflowCheck loggingPolicy,
        IReadOnlyList<WorkflowCheck> cache, IReadOnlyList<WorkflowCheck> sources, WorkflowCheck outcome,
        WorkflowCheck appDeploymentHistory, WorkflowCheck storeHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        ArgumentNullException.ThrowIfNull(securityPolicy);
        ArgumentNullException.ThrowIfNull(loggingPolicy);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(appDeploymentHistory);
        ArgumentNullException.ThrowIfNull(storeHistory);
        token.ThrowIfCancellationRequested();

        if (readiness.Name != "Installer readiness" || securityPolicy.Name != "Installer security and patch policy" ||
            loggingPolicy.Name != "Installer logging policy" || outcome.Name != "Installer attempt correlation" ||
            appDeploymentHistory.Name != "Recent AppX deployment events" ||
            storeHistory.Name != "Recent Microsoft Store installation events" ||
            !ValidGroup(cache, 1024, item => item.Name == "MSI cached packages" || item.Name.StartsWith("MSI cache: ", StringComparison.Ordinal)) ||
            !ValidGroup(sources, 1, item => item.Name == "MSI source metadata"))
            return Unknown("Installation environment inputs were missing, exceeded their bounds or carried an unexpected identity.");

        var current = new[] { readiness, securityPolicy, loggingPolicy }.Concat(cache).Concat(sources).ToArray();
        var history = new[] { outcome, appDeploymentHistory, storeHistory };
        int currentUnknown = current.Count(item => item.EffectiveState == CheckState.Unknown);
        int currentAttention = current.Count(item => NeedsAttention(item.EffectiveState));
        int historyUnknown = history.Count(item => item.EffectiveState == CheckState.Unknown);
        int historyAttention = history.Count(item => NeedsAttention(item.EffectiveState));
        string detail = $"Reviewed Installer readiness, security and patch policy, logging policy, {cache.Count} cache result{(cache.Count == 1 ? "" : "s")}, source metadata, correlated MSI outcomes, AppX deployment history and Store installation history. Current evidence: {currentAttention} need attention and {currentUnknown} incomplete. History: {historyAttention} need attention and {historyUnknown} incomplete. ";
        const string boundary = "This is bounded configuration, registration, cache and retained-history evidence. It does not prove package applicability, source reachability, account licensing, dependency resolution, vendor custom actions, application launch or installation success. No process, service, package, cache, source, account, license, registry value, file, policy or restart state changed, and no repair is selected.";
        if (currentUnknown > 0 || historyUnknown > 0)
            return Unknown(detail + "Recollect incomplete evidence before choosing an installation action. " + boundary);
        if (currentAttention > 0)
            return new("Installation environment correlation", CheckState.Attention,
                detail + "Review the exact current findings before choosing a product, source, policy or service action. " + boundary);
        if (historyAttention > 0)
            return new("Installation environment correlation", CheckState.Information,
                detail + "Current environment evidence did not establish an installation problem. Review retained history only when it matches the product, time and symptom. " + boundary);
        return new("Installation environment correlation", CheckState.Information,
            detail + "No environment contradiction was established within the collected scope. " + boundary);
    }

    private static bool ValidGroup(IReadOnlyList<WorkflowCheck> checks, int maximum, Func<WorkflowCheck, bool> valid) =>
        checks.Count is > 0 && checks.Count <= maximum && checks.All(item => item is not null && valid(item));

    private static bool NeedsAttention(CheckState state) => state is CheckState.Attention or CheckState.Repairable;

    private static WorkflowCheck Unknown(string detail) => new("Installation environment correlation", CheckState.Unknown,
        detail + " No repair is selected and no installation state changed.");
}
