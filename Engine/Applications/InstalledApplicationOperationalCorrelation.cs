namespace KillerBench.Engine;

public static class InstalledApplicationOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck registrations, WorkflowCheck stabilityHistory,
        WorkflowCheck restartState, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(stabilityHistory);
        ArgumentNullException.ThrowIfNull(restartState);
        token.ThrowIfCancellationRequested();
        const string name = "Installed application operational conclusion";
        const string boundary = " Registration is not proof that an application is present, managed, supported, licensed, launchable or removable. Stability history is not correlated to a registration identity in this workflow. No installer, uninstaller, application, process, service, file, registration, account, policy or restart action is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (registrations.Name != "Application registrations" ||
            stabilityHistory.Name != "Application stability history conclusion" || restartState.Name != "Restart")
            return Unknown("Installed application evidence carried an unexpected identity.");
        var evidence = new[] { registrations, stabilityHistory, restartState };
        int incomplete = evidence.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = evidence.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed the selected registration scope, bounded application stability history and restart state. Current evidence needing attention: {attention}; incomplete results: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete registration, history or restart evidence before drawing a combined conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Resolve or account for the reported current restart state before installation or repair planning." + boundary);
        return new(name, CheckState.Information, detail + "Use the retained history to choose a narrower application or installation diagnostic. Current application operation remains unverified." + boundary);
    }
}
