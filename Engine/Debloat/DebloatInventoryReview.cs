namespace KillerBench.Engine;

public sealed record DebloatInventoryCandidate(DebloatAreas Area, string Kind, string Identity,
    string Name, string? Publisher, string? Version, bool Protected, string SourcePattern);

public sealed record DebloatInventoryEvidence(IReadOnlyList<DebloatInventoryCandidate> Candidates,
    IReadOnlyList<OfficeClickToRunProduct> OfficeProducts,
    IReadOnlyList<OfficeClickToRunProduct> OfficeLanguageCandidates, bool Complete, string Detail);

public static class DebloatInventoryReview
{
    public static DebloatInventoryEvidence Review(DebloatOptions options, DebloatEnvironmentEvidence environment,
        InstalledApplicationInventory applications, AppPackageInventoryResult packages,
        ProvisionedPackageInventory provisioned, OfficeClickToRunSnapshot office)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(applications);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(provisioned);
        ArgumentNullException.ThrowIfNull(office);
        options.Validate();

        var candidates = new List<DebloatInventoryCandidate>();
        foreach (var app in applications.Applications)
        {
            var (area, pattern) = MatchApplication(app, environment);
            if (area != DebloatAreas.None && options.Areas.HasFlag(area))
                candidates.Add(new(area, "Uninstall registration", app.View + "/" + app.Key,
                    app.Name, app.Publisher, app.Version, false, pattern!));
        }
        foreach (var package in packages.Packages)
        {
            var (area, pattern) = MatchPackage(package.Name, environment);
            if (area != DebloatAreas.None && options.Areas.HasFlag(area))
                candidates.Add(new(area, "Registered app package", package.FullName,
                    package.Name, null, null, package.NonRemovable, pattern!));
        }
        foreach (var package in provisioned.Packages)
        {
            var (area, pattern) = MatchPackage(package.Name, environment);
            if (area != DebloatAreas.None && options.Areas.HasFlag(area))
                candidates.Add(new(area, "Provisioned app package", package.PackageName,
                    package.Name, null, package.Version, false, pattern!));
        }
        var selectedOffice = options.Areas.HasFlag(DebloatAreas.OfficeLanguages)
            ? office.Products.ToArray() : [];
        var officeCandidates = options.RetainedOfficeCulture is null ? [] : selectedOffice
            .Where(product => product.Culture is not null &&
                !product.Culture.Equals(options.RetainedOfficeCulture, StringComparison.OrdinalIgnoreCase)).ToArray();
        bool complete = applications.Complete && packages.Complete && provisioned.Complete && office.Complete;
        return new(Array.AsReadOnly(candidates.DistinctBy(candidate => candidate.Kind + "\0" + candidate.Identity,
                StringComparer.OrdinalIgnoreCase).OrderBy(candidate => candidate.Area).ThenBy(candidate => candidate.Identity, StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(selectedOffice), Array.AsReadOnly(officeCandidates), complete,
            "Exact installed identities are retained with the source pattern that selected them. Wildcard catalog matches and package presence do not establish removal eligibility. Office candidates require an explicit retained culture, but culture difference still does not authorize removal.");
    }

    private static (DebloatAreas Area, string? Pattern) MatchApplication(InstalledApplication app, DebloatEnvironmentEvidence environment)
    {
        if (MatchesManufacturer(environment, "hp", "hewlett-packard", "hewlett packard") &&
            Match(app.Name, ["HP Wolf Security", "HP Wolf Security - Console", "HP Security Update Service"], out string? hp)) return (DebloatAreas.HpSoftware, hp);
        if (MatchesManufacturer(environment, "dell") && Match(app.Name,
            ["Dell SupportAssist", "Dell Optimizer", "Dell Digital Delivery", "Dell Update", "Dell Customer Connect", "Dell Help and Support"], out string? dell)) return (DebloatAreas.DellSoftware, dell + "*");
        if (MatchesManufacturer(environment, "asus", "asustek", "acer") && Match(app.Name,
            ["Armoury Crate", "MyASUS", "ASUS System Control Interface", "Acer Care Center", "Acer Configuration Manager", "Acer Portal", "Quick Access", "AbFiles", "AOP Framework"], out string? vendor)) return (DebloatAreas.AsusAcerSoftware, vendor + "*");
        return (DebloatAreas.None, null);
    }

    private static (DebloatAreas Area, string? Pattern) MatchPackage(string name, DebloatEnvironmentEvidence environment)
    {
        if (MatchesManufacturer(environment, "hp", "hewlett-packard", "hewlett packard") && Match(name,
            ["HPJumpStarts", "HPPrivacySettings", "HPSupportAssistant", "HPQuickDrop", "myHP", "HPEasyClean", "HPSmart"], out string? hp)) return (DebloatAreas.HpSoftware, "*" + hp + "*");
        if (MatchesManufacturer(environment, "dell") && Match(name,
            ["DellInc.DellDigitalDelivery", "DellInc.DellSupportAssist", "DellInc.DellOptimizer", "DellInc.DellPowerManager", "DellInc.MyDell", "DellInc.DellCommandUpdate", "DellInc.DellRegistration", "WavesAudio.WavesMaxxAudio"], out string? dell)) return (DebloatAreas.DellSoftware, "*" + dell + "*");
        if (MatchesManufacturer(environment, "asus", "asustek", "acer") && Match(name,
            ["AsusSystemAnalysis", "MyASUS", "ArmouryCrate", "ASUSGlideX", "AcerCareCenter", "AcerConfigurationManager", "AcerQuickAccess", "AcerUserExperience", "AcerProductRegistration"], out string? vendor)) return (DebloatAreas.AsusAcerSoftware, "*" + vendor + "*");
        if (Match(name, ["McAfee", "Norton", "Avast", "AVG", "ExpressVPN", "TikTok", "Instagram", "Facebook", "LinkedIn", "Netflix", "PrimeVideo", "Disney", "CandyCrush", "Roblox", "Spotify", "SolitaireCollection", "WildTangent", "ByteDance", "Amazon", "eBay", "Pinterest", "Todoist", "Clipchamp", "MicrosoftNews"], out string? thirdParty))
            return (DebloatAreas.ThirdPartyApps, "*" + thirdParty + "*");
        return (DebloatAreas.None, null);
    }

    private static bool Match(string value, IReadOnlyList<string> patterns, out string? pattern)
    {
        pattern = patterns.FirstOrDefault(item => value.Contains(item, StringComparison.OrdinalIgnoreCase));
        return pattern is not null;
    }

    private static bool MatchesManufacturer(DebloatEnvironmentEvidence evidence, params string[] values) =>
        evidence.Complete && evidence.Manufacturer is not null &&
        values.Any(value => evidence.Manufacturer.Contains(value, StringComparison.OrdinalIgnoreCase));
}
