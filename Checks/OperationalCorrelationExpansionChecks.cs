using KillerBench.Engine;

internal static class OperationalCorrelationExpansionChecks
{
    public static void Run()
    {
        var associationClear = DeviceAssociationOperationalCorrelation.Check([
            new("Recent device setup events", CheckState.Information, "fixture"),
            new("Recent Plug and Play configuration events", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(associationClear is { Name: "Device association readiness", State: CheckState.Information, Repair: null } &&
            associationClear.Detail.Contains("No current service-availability issue") && associationClear.Detail.Contains("selects no repair"),
            "Complete device association evidence produces one bounded informational conclusion");

        var associationHistory = DeviceAssociationOperationalCorrelation.Check([
            new("Recent device setup events", CheckState.Attention, "fixture"),
            new("Recent Plug and Play configuration events", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(associationHistory.State == CheckState.Information && associationHistory.Detail.Contains("Historical results needing review: 1") &&
            associationHistory.Detail.Contains("exact affected device"),
            "Historical device events remain review context when the exact service is currently available");

        var associationService = DeviceAssociationOperationalCorrelation.Check([
            new("Recent device setup events", CheckState.Information, "fixture"),
            new("Recent Plug and Play configuration events", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]);
        Require(associationService.State == CheckState.Attention && associationService.Detail.Contains("current exact service conclusion"),
            "A current Device Association service concern remains attention");
        Require(DeviceAssociationOperationalCorrelation.Check([
            new("Recent device setup events", CheckState.Unknown, "fixture"),
            new("Recent Plug and Play configuration events", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]).State == CheckState.Unknown && DeviceAssociationOperationalCorrelation.Check([
            new("Focused service readiness", CheckState.Information, "fixture")
        ]).State == CheckState.Unknown, "Incomplete or missing device association evidence fails closed");

        foreach (var kind in new[] { WorkflowKind.FunctionDiscoveryProviderHostService,
            WorkflowKind.FunctionDiscoveryResourcePublicationService })
            Require(NetworkDiscoveryOperationalCorrelation.Supports(kind), "Each Function Discovery workflow is supported");
        Require(!NetworkDiscoveryOperationalCorrelation.Supports(WorkflowKind.Network),
            "The broad network workflow is not treated as a focused discovery service");

        var discoveryClear = NetworkDiscoveryOperationalCorrelation.Check(WorkflowKind.FunctionDiscoveryProviderHostService, [
            new("Network profile: Ethernet", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(discoveryClear is { Name: "Function Discovery provider readiness", State: CheckState.Information, Repair: null } &&
            discoveryClear.Detail.Contains("1 network profile result") && discoveryClear.Detail.Contains("provider health"),
            "Private-profile provider evidence produces a feature-specific bounded conclusion");

        var discoveryAttention = NetworkDiscoveryOperationalCorrelation.Check(WorkflowKind.FunctionDiscoveryResourcePublicationService, [
            new("Network profile: Wi-Fi", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(discoveryAttention is { Name: "Function Discovery publication readiness", State: CheckState.Attention, Repair: null } &&
            discoveryAttention.Detail.Contains("Public-profile trust") && discoveryAttention.Detail.Contains("this PC's visibility"),
            "A Public profile remains attention without selecting a network change");
        Require(NetworkDiscoveryOperationalCorrelation.Check(WorkflowKind.FunctionDiscoveryProviderHostService, [
            new("Network profiles", CheckState.Unknown, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]).State == CheckState.Unknown && NetworkDiscoveryOperationalCorrelation.Check(WorkflowKind.FunctionDiscoveryProviderHostService, [
            new("Network profile: Ethernet", CheckState.Information, "fixture")
        ]).State == CheckState.Unknown, "Incomplete or missing discovery evidence fails closed");

        var fontClear = FontCacheOperationalCorrelation.Check([
            new("Font registrations", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(fontClear is { Name: "Font cache readiness", State: CheckState.Information, Repair: null } &&
            fontClear.Detail.Contains("No missing registered file") && fontClear.Detail.Contains("does not parse font files"),
            "Complete registration and service evidence produces one bounded font-cache conclusion");
        var fontAttention = FontCacheOperationalCorrelation.Check([
            new("Font registrations", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(fontAttention is { State: CheckState.Attention, Repair: null } &&
            fontAttention.Detail.Contains("Neither finding authorizes a cache rebuild"),
            "A missing registered font file remains attention without selecting repair");
        Require(FontCacheOperationalCorrelation.Check([
            new("Font registrations", CheckState.Unknown, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]).State == CheckState.Unknown && FontCacheOperationalCorrelation.Check([
            new("Font registrations", CheckState.Information, "fixture")
        ]).State == CheckState.Unknown, "Incomplete or missing font-cache evidence fails closed");

        foreach (var kind in new[] { WorkflowKind.AppReadinessService, WorkflowKind.StateRepositoryService,
            WorkflowKind.AppxDeploymentService, WorkflowKind.ClientLicenseService, WorkflowKind.StoreInstallService,
            WorkflowKind.WindowsLicenseManagerService, WorkflowKind.PushToInstallService,
            WorkflowKind.UserProfileService, WorkflowKind.DeviceInstallService, WorkflowKind.OptimizeDrivesService })
            Require(FeatureServiceOperationalCorrelation.Supports(kind), "Each evidence-backed feature service is supported");
        Require(!FeatureServiceOperationalCorrelation.Supports(WorkflowKind.CredentialManagerService),
            "A service without separate feature evidence remains outside the feature correlation");

        var deploymentHistory = FeatureServiceOperationalCorrelation.Check(WorkflowKind.AppxDeploymentService, [
            new("Recent AppX deployment events", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(deploymentHistory is { Name: "AppX deployment readiness", State: CheckState.Information, Repair: null } &&
            deploymentHistory.Detail.Contains("historical context") && deploymentHistory.Detail.Contains("selects no repair"),
            "Historical AppX findings remain scoped context when the exact service is available");

        var repositoryHistory = FeatureServiceOperationalCorrelation.Check(WorkflowKind.StateRepositoryService, [
            new("Recent AppX deployment events", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(repositoryHistory is { Name: "State Repository readiness", State: CheckState.Information, Repair: null } &&
            repositoryHistory.Detail.Contains("historical context") && repositoryHistory.Detail.Contains("repository consistency"),
            "Historical AppX findings do not become a current State Repository fault");

        var storeService = FeatureServiceOperationalCorrelation.Check(WorkflowKind.StoreInstallService, [
            new("Recent Microsoft Store installation events", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]);
        Require(storeService is { Name: "Microsoft Store installation readiness", State: CheckState.Attention, Repair: null } &&
            storeService.Detail.Contains("current feature and service findings") && storeService.Detail.Contains("account access"),
            "A current Store service concern remains attention with feature-specific boundaries");

        foreach (var (kind, name, boundary) in new[]
        {
            (WorkflowKind.ClientLicenseService, "Client License Service readiness", "license validation"),
            (WorkflowKind.WindowsLicenseManagerService, "Windows License Manager readiness", "token issuance"),
            (WorkflowKind.PushToInstallService, "Push-to-install readiness", "request approval")
        })
        {
            var historical = FeatureServiceOperationalCorrelation.Check(kind, [
                new("Recent Microsoft Store installation events", CheckState.Attention, "fixture"),
                new("Focused service readiness", CheckState.Information, "fixture")
            ]);
            Require(historical.Name == name && historical.State == CheckState.Information && historical.Repair is null &&
                historical.Detail.Contains("historical context") && historical.Detail.Contains(boundary),
                "Historical Store events remain scoped context for " + name);
        }

        foreach (var (kind, name, boundary) in new[]
        {
            (WorkflowKind.WebAccountManagerService, "Web Account Manager readiness", "token availability"),
            (WorkflowKind.MicrosoftAccountSignInService, "Microsoft Account Sign-in readiness", "account connectivity")
        })
        {
            var historical = FeatureServiceOperationalCorrelation.Check(kind, [
                new("Recent account broker events", CheckState.Attention, "fixture"),
                new("Focused service readiness", CheckState.Information, "fixture")
            ]);
            Require(historical.Name == name && historical.State == CheckState.Information && historical.Repair is null &&
                historical.Detail.Contains("historical context") && historical.Detail.Contains(boundary) &&
                historical.Detail.Contains("payload-sanitized"),
                "Historical account broker events remain private scoped context for " + name);
        }

        var deviceInstall = FeatureServiceOperationalCorrelation.Check(WorkflowKind.DeviceInstallService, [
            new("Recent device setup events", CheckState.Information, "fixture"),
            new("Recent Plug and Play configuration events", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(deviceInstall is { Name: "Device installation readiness", State: CheckState.Information, Repair: null } &&
            deviceInstall.Detail.Contains("2 feature results") && deviceInstall.Detail.Contains("driver suitability"),
            "Device installation retains historical review without claiming a current device problem");

        var optimization = FeatureServiceOperationalCorrelation.Check(WorkflowKind.OptimizeDrivesService, [
            new("Scheduled drive optimization", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(optimization is { Name: "Scheduled drive optimization readiness", State: CheckState.Attention, Repair: null } &&
            optimization.Detail.Contains("volume analysis") && optimization.Detail.Contains("changes no task"),
            "Current ScheduledDefrag concerns remain attention without authorizing execution");
        Require(FeatureServiceOperationalCorrelation.Check(WorkflowKind.UserProfileService, [
            new("Recent user profile events", CheckState.Unknown, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]).State == CheckState.Unknown && FeatureServiceOperationalCorrelation.Check(WorkflowKind.DeviceInstallService, [
            new("Recent device setup events", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]).State == CheckState.Unknown, "Incomplete or missing feature-service evidence fails closed");

        try
        {
            DeviceAssociationOperationalCorrelation.Check([], new CancellationToken(true));
            throw new Exception("Device association correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }
        try
        {
            NetworkDiscoveryOperationalCorrelation.Check(WorkflowKind.Network, []);
            throw new Exception("Unsupported discovery workflow accepted");
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            FontCacheOperationalCorrelation.Check([], new CancellationToken(true));
            throw new Exception("Font cache correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }
        try
        {
            FeatureServiceOperationalCorrelation.Check(WorkflowKind.AppReadinessService, [], new CancellationToken(true));
            throw new Exception("Feature service correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: device association, Function Discovery, font-cache and feature-service evidence produce conservative combined readiness conclusions.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
