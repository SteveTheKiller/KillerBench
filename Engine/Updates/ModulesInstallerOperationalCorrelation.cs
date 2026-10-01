namespace KillerBench.Engine.Updates;

public static class ModulesInstallerOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck packages,
        WorkflowCheck history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();

        const string name = "Windows Modules Installer conclusion";
        const string boundary = " This bounded conclusion does not establish component-store health, package applicability, servicing completion or restart readiness. It does not install, remove, clean up or roll back a package, change a source or policy, request a restart, start a service, or select a repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (service.Name != "Windows Modules Installer state" ||
            packages.Name != "Servicing packages" ||
            history.Name != "Windows Modules Installer failure history")
            return Unknown("Windows Modules Installer inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] checks = [service, packages, history];
        int incomplete = checks.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed exact current service state, bounded servicing package states and exact service-failure history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete service, package or history evidence before interpreting Windows Modules Installer readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review current package transitions and the exact service evidence before selecting a focused service action. Historical failures and package states cannot authorize a service start or package operation." + boundary);
        return new(name, CheckState.Information,
            detail + " No current concern was established within the collected evidence." + boundary);
    }
}
