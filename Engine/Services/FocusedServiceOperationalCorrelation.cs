namespace KillerBench.Engine.Services;

public static class FocusedServiceOperationalCorrelation
{
    public static bool Supports(WorkflowKind kind) => kind is
        WorkflowKind.FirewallServices or
        WorkflowKind.Bluetooth or WorkflowKind.Camera or WorkflowKind.SmartCard or WorkflowKind.Biometric or
        WorkflowKind.TaskSchedulerService or WorkflowKind.DiagnosticPolicyService or
        WorkflowKind.WindowsErrorReportingService or WorkflowKind.WindowsEventCollectorService or
        WorkflowKind.RemoteAccessConnectionManagerService or WorkflowKind.RemoteDesktopServices or
        WorkflowKind.WindowsRemoteManagement or WorkflowKind.SecurityCenterService or
        WorkflowKind.NetworkConnectionsService or WorkflowKind.WinHttpAutoProxyService or
        WorkflowKind.WindowsConnectionManagerService or WorkflowKind.NetworkSetupService or
        WorkflowKind.AppReadinessService or WorkflowKind.StateRepositoryService or
        WorkflowKind.UserProfileService or WorkflowKind.CredentialManagerService or
        WorkflowKind.ShellHardwareDetectionService or WorkflowKind.DeviceInstallService or
        WorkflowKind.AppxDeploymentService or WorkflowKind.ClientLicenseService or
        WorkflowKind.StoreInstallService or WorkflowKind.WindowsLicenseManagerService or
        WorkflowKind.PushToInstallService or WorkflowKind.WebAccountManagerService or
        WorkflowKind.MicrosoftAccountSignInService or WorkflowKind.StorageService or WorkflowKind.OptimizeDrivesService or
        WorkflowKind.WindowsImageAcquisitionService or
        WorkflowKind.FunctionDiscoveryProviderHostService or WorkflowKind.FunctionDiscoveryResourcePublicationService or
        WorkflowKind.HumanInterfaceDeviceService or WorkflowKind.PortableDeviceEnumeratorService or
        WorkflowKind.WindowsFontCacheService or WorkflowKind.DeviceAssociationService or
        WorkflowKind.TextInputManagementService or WorkflowKind.DisplayEnhancementService or
        WorkflowKind.SystemNotifications or WorkflowKind.CoreWindowsServices;

    public static WorkflowCheck Check(IReadOnlyList<RepairRecipe> recipes, IReadOnlyList<RecipeResult> results,
        IReadOnlyList<WorkflowCheck> history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(recipes);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();
        const string name = "Focused service readiness";
        const string boundary = " This conclusion covers exact service availability and retained Service Control Manager history only. It does not prove dependent feature behavior, identify root cause, authorize configuration changes, or select a repair. Startup settings, dependencies, service permissions, accounts, credentials, policy and user data remain unchanged.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (recipes.Count == 0 || recipes.Any(recipe => recipe is null || ServiceRecovery.ServiceName(recipe.Action) is null) ||
            recipes.Select(recipe => recipe.Id).Distinct(StringComparer.Ordinal).Count() != recipes.Count ||
            results.Count != recipes.Count || results.Any(result => result is null) ||
            results.Select(result => result.RecipeId).Distinct(StringComparer.Ordinal).Count() != results.Count ||
            !results.Select(result => result.RecipeId).OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(recipes.Select(recipe => recipe.Id).OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) ||
            history.Count != recipes.Count || history.Any(item => item is null ||
                !item.Name.EndsWith("failure history", StringComparison.Ordinal)) ||
            history.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != history.Count)
            return Unknown("Focused service correlation inputs were missing, duplicated or carried an unexpected identity.");

        foreach (var recipe in recipes)
        {
            var result = results.Single(item => item.RecipeId == recipe.Id);
            if (result.Check is not null && !string.Equals(result.Check.Name, recipe.Action.ToString(), StringComparison.Ordinal))
                return Unknown("A focused service result did not match its expected current-state identity.");
            if (result.Check is null && result.EffectiveOutcome is RecipeOutcome.Clear or RecipeOutcome.Repaired or
                RecipeOutcome.RepairAvailable or RecipeOutcome.NeedsAttention or RecipeOutcome.RestartRequired)
                return Unknown("A focused service result omitted the current-state evidence required by its outcome.");
        }

        int incompleteCurrent = results.Count(result => result.EffectiveOutcome is RecipeOutcome.Unknown or RecipeOutcome.Blocked or RecipeOutcome.Canceled);
        int currentAttention = results.Count(result => result.EffectiveOutcome is RecipeOutcome.RepairAvailable or RecipeOutcome.NeedsAttention or RecipeOutcome.RestartRequired);
        int repaired = results.Count(result => result.EffectiveOutcome == RecipeOutcome.Repaired);
        int incompleteHistory = history.Count(item => item.EffectiveState == CheckState.Unknown);
        int historicalFindings = history.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {recipes.Count} exact service {(recipes.Count == 1 ? "target" : "targets")} with current recipe evidence and matching failure history. Current results: {currentAttention} need attention, {repaired} repaired and {incompleteCurrent} incomplete. Historical findings: {historicalFindings}; incomplete histories: {incompleteHistory}. ";

        if (incompleteCurrent > 0 || incompleteHistory > 0)
            return Unknown(detail + "Recollect incomplete current and historical evidence before choosing another service action.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention, detail + "Review the current service results and their prerequisites. Historical failures remain context and do not establish a current outage." + boundary);
        return new(name, CheckState.Information, detail + (historicalFindings > 0
            ? "Current service evidence is clear or repaired. Retained failures remain historical context and do not establish a current outage."
            : "No current service availability issue was established within the checked scope." ) + boundary);
    }
}
