namespace KillerBench.Engine;

public static class ApplicationDeploymentOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck registeredPackages, WorkflowCheck provisionedPackages,
        WorkflowCheck machineApplications, WorkflowCheck accountApplications,
        IReadOnlyList<WorkflowCheck> installationReadiness, IReadOnlyList<WorkflowCheck> applicationHistory,
        IReadOnlyList<WorkflowCheck> restartState, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(registeredPackages); ArgumentNullException.ThrowIfNull(provisionedPackages);
        ArgumentNullException.ThrowIfNull(machineApplications); ArgumentNullException.ThrowIfNull(accountApplications);
        ArgumentNullException.ThrowIfNull(installationReadiness); ArgumentNullException.ThrowIfNull(applicationHistory);
        ArgumentNullException.ThrowIfNull(restartState); token.ThrowIfCancellationRequested();
        const string name = "Application deployment operational correlation";
        const string boundary = " This is a bounded registration, installation and history review, not proof that an application launches, updates, licenses, repairs or uninstalls successfully. No package, application, registration, cache, source, service, policy, file or restart state changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        var fixedInputs = new[]
        {
            (Check: registeredPackages, Name: "Registered app package summary"),
            (Check: provisionedPackages, Name: "Provisioned app packages"),
            (Check: machineApplications, Name: "Application registrations"),
            (Check: accountApplications, Name: "Application registrations")
        };
        var installer = installationReadiness.Where(item => item.Name == "Windows Installer operational correlation").ToArray();
        var history = applicationHistory.Where(item => item.Name == "Recent application errors").ToArray();
        var restart = restartState.Where(item => item.Name == "Restart").ToArray();
        if (fixedInputs.Any(item => item.Check.Name != item.Name) || installer.Length != 1 || history.Length != 1 || restart.Length != 1)
            return Unknown("Application deployment correlation inputs were missing, duplicated or carried an unexpected identity.");
        var all = fixedInputs.Select(item => item.Check).Concat(installer).Concat(history).Concat(restart).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed package and application registrations, Windows Installer readiness, application failure history and restart state. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0) return Unknown(detail + "Recollect incomplete evidence before drawing a combined deployment conclusion.");
        if (attention > 0) return new(name, CheckState.Attention, detail + "Review the exact package, application and Installer evidence before choosing a targeted deployment action." + boundary);
        return new(name, CheckState.Information, detail + "No collected source requested review. Application operation and deployment success remain unverified." + boundary);
    }
}
