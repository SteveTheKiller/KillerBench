namespace KillerBench.Engine;

public static class FeatureServiceOperationalCorrelation
{
    public static bool Supports(WorkflowKind kind) => kind is WorkflowKind.AppReadinessService or
        WorkflowKind.StateRepositoryService or WorkflowKind.AppxDeploymentService or
        WorkflowKind.ClientLicenseService or WorkflowKind.StoreInstallService or
        WorkflowKind.WindowsLicenseManagerService or WorkflowKind.PushToInstallService or
        WorkflowKind.UserProfileService or WorkflowKind.DeviceInstallService or
        WorkflowKind.OptimizeDrivesService or WorkflowKind.WebAccountManagerService or
        WorkflowKind.MicrosoftAccountSignInService;

    public static WorkflowCheck Check(WorkflowKind kind, IReadOnlyList<WorkflowCheck> checks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        var definition = Definition(kind);
        const string serviceName = "Focused service readiness";
        string boundary = " This conclusion combines " + definition.Scope +
            " with the exact service conclusion and retained Service Control Manager history. It does not prove " +
            definition.ExcludedBehavior + ". It identifies no root cause, selects no repair and changes no " +
            definition.PreservedState + ".";
        WorkflowCheck Unknown(string detail) => new(definition.ResultName, CheckState.Unknown, detail + boundary);

        if (checks.Count == 0 || checks.Any(check => check is null))
            return Unknown("Feature correlation evidence is missing or invalid.");

        var service = checks.Where(check => check.Name == serviceName).ToArray();
        var feature = definition.EvidenceNames.SelectMany(name => checks.Where(check => check.Name == name)).ToArray();
        if (service.Length != 1 || feature.Length != definition.EvidenceNames.Length ||
            feature.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() != definition.EvidenceNames.Length)
            return Unknown("The expected feature evidence or focused-service conclusion was missing or duplicated.");

        int incomplete = feature.Count(check => check.EffectiveState == CheckState.Unknown) +
            (service[0].EffectiveState == CheckState.Unknown ? 1 : 0);
        int featureAttention = feature.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        bool serviceAttention = service[0].EffectiveState is CheckState.Attention or CheckState.Repairable;
        int attention = featureAttention + (serviceAttention ? 1 : 0);
        string counts = $"Reviewed {feature.Length} feature result{(feature.Length == 1 ? "" : "s")} plus the exact service conclusion. Results needing review: {attention}; incomplete results: {incomplete}. ";

        if (incomplete > 0)
            return Unknown(counts + "Recollect the incomplete feature or service evidence before drawing a readiness conclusion.");
        if (serviceAttention || definition.CurrentEvidence && featureAttention > 0)
            return new(definition.ResultName, CheckState.Attention,
                counts + "Review the current feature and service findings by exact identity before choosing any action." + boundary);
        return new(definition.ResultName, CheckState.Information,
            counts + (featureAttention > 0
                ? "The exact service is currently available within the checked scope. Retained event findings remain historical context for narrower identity-based review."
                : "No current service-availability concern was established within the checked scope, and the retained feature evidence reported no warning or error.") + boundary);
    }

    private static DefinitionRecord Definition(WorkflowKind kind) => kind switch
    {
        WorkflowKind.AppReadinessService => new("App readiness", ["Recent AppX deployment events"], false,
            "bounded AppX deployment history", "package readiness, dependency resolution, application launch or deployment success",
            "package, registration, repository, application data, license, service configuration, permission, policy, credential or user data"),
        WorkflowKind.StateRepositoryService => new("State Repository readiness", ["Recent AppX deployment events"], false,
            "bounded AppX deployment history", "repository consistency, current package registration, application-model state, application launch or deployment success",
            "repository content, package, registration, profile, browser or application data, service configuration, permission, policy, credential or user data"),
        WorkflowKind.AppxDeploymentService => new("AppX deployment readiness", ["Recent AppX deployment events"], false,
            "bounded AppX deployment history", "current package registration, dependency resolution, application launch or deployment success",
            "package, registration, repository, application data, license, service configuration, permission, policy, credential or user data"),
        WorkflowKind.ClientLicenseService => new("Client License Service readiness", ["Recent Microsoft Store installation events"], false,
            "bounded Microsoft Store installation history", "current license validation, entitlement, account access or application launch",
            "package, registration, license, token, account, profile, application data, service configuration, permission, policy, credential or user data"),
        WorkflowKind.StoreInstallService => new("Microsoft Store installation readiness", ["Recent Microsoft Store installation events"], false,
            "bounded Microsoft Store installation history", "current download, license, package installation, account access or application launch",
            "package, download, license, account, source, application data, service configuration, permission, policy, credential or user data"),
        WorkflowKind.WindowsLicenseManagerService => new("Windows License Manager readiness", ["Recent Microsoft Store installation events"], false,
            "bounded Microsoft Store installation history", "current license validation, entitlement, token issuance or application launch",
            "package, registration, license, token, account, application data, service configuration, permission, policy, credential or user data"),
        WorkflowKind.PushToInstallService => new("Push-to-install readiness", ["Recent Microsoft Store installation events"], false,
            "bounded Microsoft Store installation history", "current request approval, download, installation, account access or application outcome",
            "package, registration, license, account, application data, queued request, source, service configuration, permission, policy, credential or user data"),
        WorkflowKind.WebAccountManagerService => new("Web Account Manager readiness", ["Recent account broker events"], false,
            "payload-sanitized account broker history", "current account validity, token availability, authentication or application access",
            "account, credential, token, cookie, application registration, permission, sign-in state, service configuration, policy or user data"),
        WorkflowKind.MicrosoftAccountSignInService => new("Microsoft Account Sign-in readiness", ["Recent account broker events"], false,
            "payload-sanitized account broker history", "current account validity, token availability, authentication, account connectivity or application access",
            "account, credential, token, device registration, application data, permission, sign-in state, service configuration, policy or user data"),
        WorkflowKind.UserProfileService => new("User profile service readiness", ["Recent user profile events"], false,
            "bounded user profile history", "profile load, sign-in, data integrity, permissions or application behavior",
            "profile, account, registry data, file, permission, service configuration, policy, credential or user data"),
        WorkflowKind.DeviceInstallService => new("Device installation readiness", ["Recent device setup events", "Recent Plug and Play configuration events"], false,
            "bounded Device Setup Manager and Plug and Play configuration history", "current device state, driver suitability, installation completion or physical device operation",
            "device, driver, package, service configuration, permission, policy, credential or user data"),
        WorkflowKind.OptimizeDrivesService => new("Scheduled drive optimization readiness", ["Scheduled drive optimization"], true,
            "the exact ScheduledDefrag task result", "task execution, volume analysis, trim, defragmentation, optimization or storage health",
            "task, schedule, volume, filesystem, file, service configuration, permission, policy or user data"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed record DefinitionRecord(string ResultName, string[] EvidenceNames, bool CurrentEvidence,
        string Scope, string ExcludedBehavior, string PreservedState);
}
