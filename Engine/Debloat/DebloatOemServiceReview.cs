namespace KillerBench.Engine.Debloat;

public sealed record DebloatOemServiceEvidence(IReadOnlyList<ServiceInventoryEntry> Matches,
    bool Complete, ToolResult Evidence, string Detail);

public static class DebloatOemServiceReview
{
    private static readonly IReadOnlyDictionary<DebloatAreas, string[]> Targets =
        new Dictionary<DebloatAreas, string[]>
        {
            [DebloatAreas.HpSoftware] = ["HPTouchpointAnalyticsService", "HPAppHelperCap", "HPDiagsCap", "HPSysInfoCap", "HPNetworkCap", "HPSupportAssistant"],
            [DebloatAreas.DellSoftware] = ["SupportAssistAgent", "DellHardwareSupport", "DellDigitalDeliveryService", "DellOptimizer", "DellClientManagementService", "DellUpdate", "KNDBWM", "Killer Network Service", "Killer Selection Service"],
            [DebloatAreas.AsusAcerSoftware] = ["AsusAppService", "ASUSSystemAnalysis", "ASUSSystemDiagnosis", "ArmouryCrateService", "AsusROGLSLService", "ASUSLinkRemote", "AcerAgentService", "AcerConfigurationManager", "AcerSvc", "AOP_UtilityService", "LiveUpdateSvc"]
        };

    public static DebloatOemServiceEvidence Review(DebloatOptions options,
        DebloatEnvironmentEvidence environment, ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(evidence);
        options.Validate();
        var interpreted = ServiceInventoryDiagnostics.Interpret(evidence);
        if (interpreted.State == CheckState.Unknown)
            return new([], false, evidence, "Installed-service inventory is unavailable or incomplete. No OEM service absence was inferred.");
        ServiceInventorySnapshot inventory;
        try { inventory = ServiceInventoryDiagnostics.Parse(evidence.Output); }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidDataException or InvalidOperationException)
        { return new([], false, evidence, "Installed-service inventory could not be represented safely. No OEM service absence was inferred."); }
        var selectedNames = Targets.Where(entry => options.Areas.HasFlag(entry.Key) && ManufacturerMatches(entry.Key, environment))
            .SelectMany(entry => entry.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matches = inventory.Services.Where(service => selectedNames.Contains(service.Name))
            .OrderBy(service => service.Name, StringComparer.Ordinal).ToArray();
        return new(Array.AsReadOnly(matches), inventory.Complete, evidence,
            "Only exact source service names for a matching manufacturer were correlated. Stopped or disabled state does not establish safe removal. No service or startup setting changed.");
    }

    public static async Task<DebloatOemServiceEvidence> ReadAsync(DebloatOptions options,
        DebloatEnvironmentEvidence environment, CancellationToken token)
    {
        var check = await ServiceInventoryDiagnostics.CheckAsync(token).ConfigureAwait(false);
        return Review(options, environment, check.Evidence!);
    }

    private static bool ManufacturerMatches(DebloatAreas area, DebloatEnvironmentEvidence evidence)
    {
        if (!evidence.Complete || evidence.Manufacturer is null) return false;
        string[] names = area switch
        {
            DebloatAreas.HpSoftware => ["hp", "hewlett-packard", "hewlett packard"],
            DebloatAreas.DellSoftware => ["dell"],
            DebloatAreas.AsusAcerSoftware => ["asus", "asustek", "acer"],
            _ => []
        };
        return names.Any(name => evidence.Manufacturer.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}
