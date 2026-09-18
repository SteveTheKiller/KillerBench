namespace KillerBench.Engine;

public static class AppPackageOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck registeredPackages, WorkflowCheck? provisionedPackages,
        WorkflowCheck deploymentHistory, WorkflowCheck storeHistory, WorkflowCheck restartState,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(registeredPackages);
        ArgumentNullException.ThrowIfNull(deploymentHistory);
        ArgumentNullException.ThrowIfNull(storeHistory);
        ArgumentNullException.ThrowIfNull(restartState);
        token.ThrowIfCancellationRequested();
        const string name = "App package operational conclusion";
        const string boundary = " This conclusion does not verify application launch, package payload integrity, licensing, dependencies, account access, download completion or managed ownership. No package, registration, repository, account, license, source, service, policy, file or restart state is changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (registeredPackages.Name != "Registered app package summary" ||
            provisionedPackages is not null && provisionedPackages.Name != "Provisioned app packages" ||
            deploymentHistory.Name != "Recent AppX deployment events" ||
            storeHistory.Name != "Recent Microsoft Store installation events" || restartState.Name != "Restart")
            return Unknown("App package evidence carried an unexpected identity.");

        var current = new[] { registeredPackages, provisionedPackages, restartState }.Where(check => check is not null).Cast<WorkflowCheck>().ToArray();
        var history = new[] { deploymentHistory, storeHistory };
        int currentUnknown = current.Count(check => check.EffectiveState == CheckState.Unknown);
        int currentAttention = current.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historyUnknown = history.Count(check => check.EffectiveState == CheckState.Unknown);
        int historyAttention = history.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed registered package state{(provisionedPackages is null ? "" : ", provisioned packages")}, restart state, AppX deployment history and Microsoft Store installation history. Current evidence: {currentAttention} need attention and {currentUnknown} incomplete. History: {historyAttention} need attention and {historyUnknown} incomplete. ";
        if (currentUnknown > 0 || historyUnknown > 0)
            return Unknown(detail + "Recollect incomplete package or history evidence before drawing a combined conclusion.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention, detail + "Review the exact current package or restart evidence before choosing a targeted deployment action." + boundary);
        return new(name, CheckState.Information, detail + (historyAttention > 0
            ? "Historical failures direct narrower package and transaction review but do not establish a current fault."
            : "No collected source requested current review. Package operation remains unverified.") + boundary);
    }
}
