namespace KillerBench.Engine.Updates;

public static class WindowsUpdateMedicOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck registration,
        WorkflowCheck activity, WorkflowCheck history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();

        const string name = "Windows Update Medic conclusion";
        const string boundary = " This bounded conclusion does not establish remediation success, component health, update applicability, download progress, installation progress or restart readiness. It does not start the service, replace a component, select, approve, download or install an update, request a restart, change policy or select a repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (service.Name != "Windows Update Medic Service state" ||
            registration.Name != "Windows Update Medic registration" ||
            activity.Name != "Windows Update live activity" ||
            history.Name != "Windows Update Medic failure history")
            return Unknown("Windows Update Medic inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] checks = [service, registration, activity, history];
        int incomplete = checks.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed exact current service state, registered service DLL evidence, bounded current update activity and exact service-failure history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete service, registration, activity or history evidence before interpreting Medic readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the detailed evidence before selecting the separately guarded service action. DLL observations, update activity and historical failures cannot authorize a service start or component action." + boundary);
        return new(name, CheckState.Information,
            detail + " No current concern was established within the collected evidence." + boundary);
    }
}
