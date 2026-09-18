namespace KillerBench.Engine;

[Flags]
public enum DepotApplications
{
    None = 0, Microsoft365 = 1, OneDrive = 2, Teams = 4, Chrome = 8,
    AcrobatReader = 16, Zoom = 32, SevenZip = 64
}

[Flags]
public enum DepotConfiguration
{
    None = 0, WindowsUpdates = 1, ComputerName = 2, LocalAdministrator = 4,
    AdministratorReview = 8, BrowserExtensions = 16, Cleanup = 32,
    Edge = 64, Privacy = 128, Taskbar = 256, StartMenu = 512, Notifications = 1024
}

public sealed record DepotOptions(DepotApplications Applications, DepotConfiguration Configuration)
{
    public void Validate()
    {
        const DepotApplications allApps = DepotApplications.Microsoft365 | DepotApplications.OneDrive |
            DepotApplications.Teams | DepotApplications.Chrome | DepotApplications.AcrobatReader |
            DepotApplications.Zoom | DepotApplications.SevenZip;
        const DepotConfiguration allConfig = DepotConfiguration.WindowsUpdates | DepotConfiguration.ComputerName |
            DepotConfiguration.LocalAdministrator | DepotConfiguration.AdministratorReview |
            DepotConfiguration.BrowserExtensions | DepotConfiguration.Cleanup | DepotConfiguration.Edge |
            DepotConfiguration.Privacy | DepotConfiguration.Taskbar | DepotConfiguration.StartMenu |
            DepotConfiguration.Notifications;
        if ((Applications & ~allApps) != 0) throw new ArgumentOutOfRangeException(nameof(Applications));
        if ((Configuration & ~allConfig) != 0) throw new ArgumentOutOfRangeException(nameof(Configuration));
        if (Applications == DepotApplications.None && Configuration == DepotConfiguration.None)
            throw new ArgumentException("Select at least one application or configuration group.");
    }
}

public enum DepotDetection { Installed, NotDetected, Unknown }
public sealed record DepotApplicationReview(DepotApplications Application, DepotDetection Detection,
    IReadOnlyList<string> Registrations, string Limitation);
public sealed record DepotReview(DepotOptions Options, IReadOnlyList<DepotApplicationReview> Applications,
    bool Complete, bool ExecutionSupported, IReadOnlyList<WorkflowCheck> Checks)
{
    public string Format()
    {
        string apps = Options.Applications == DepotApplications.None ? "None" : string.Join(", ",
            Enum.GetValues<DepotApplications>().Where(value => value != DepotApplications.None && Options.Applications.HasFlag(value)));
        string config = Options.Configuration == DepotConfiguration.None ? "None" : string.Join(", ",
            Enum.GetValues<DepotConfiguration>().Where(value => value != DepotConfiguration.None && Options.Configuration.HasFlag(value)));
        return "DEPOT / PROVISIONING REVIEW\nApplications: " + apps + "\nConfiguration: " + config +
            "\nCoverage: " + (Complete ? "Complete for collected registrations" : "Incomplete") +
            "\nExecution available: No\n\nNo download, installation, account, update, cleanup, policy, registry, service, profile or restart action occurred.";
    }
}

public static class DepotPlanning
{
    public static async Task<DepotReview> ReviewCurrentAsync(DepotOptions options,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        progress?.Report("Reading machine application registrations...");
        var installed = await InstalledApplications.ReadAsync(InstalledApplicationScope.Machine, token).ConfigureAwait(false);
        progress?.Report("Reading running-account app package registrations...");
        var packages = await AppPackageInventory.ReadAsync(AppPackageScope.CurrentAccount, token).ConfigureAwait(false);
        progress?.Report("Reading Office Click-to-Run composition...");
        var office = OfficeClickToRunInventory.Read(token);
        return Review(options, installed, packages, office);
    }

    public static DepotReview Review(DepotOptions options, InstalledApplicationInventory installed,
        AppPackageInventoryResult packages, OfficeClickToRunSnapshot office)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(installed);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(office);
        options.Validate();
        bool complete = installed.Complete && packages.Complete && office.Complete;
        var appReviews = Enum.GetValues<DepotApplications>()
            .Where(value => value != DepotApplications.None && options.Applications.HasFlag(value))
            .Select(value => ReviewApplication(value, installed, packages, office, complete)).ToArray();
        var checks = new List<WorkflowCheck>
        {
            new("DEPOT review coverage", complete ? CheckState.Information : CheckState.Unknown,
                (complete ? "Application registration sources completed. " : "At least one registration source was incomplete. Absence is not established. ") +
                "Portable applications, per-user Win32 installs, installer source trust, licenses, compatibility, disk space, restart needs and effective policy are not verified."),
            new("Provisioning execution", CheckState.Information,
                "Execution is unavailable. No installer URL, package, command, account change, cleanup, policy write, service change or restart is authorized by this review.")
        };
        checks.AddRange(appReviews.Select(item => new WorkflowCheck("Application: " + item.Application,
            item.Detection == DepotDetection.Unknown ? CheckState.Unknown : item.Detection == DepotDetection.Installed ? CheckState.Clear : CheckState.Attention,
            item.Detection + ". " + item.Limitation + (item.Registrations.Count == 0 ? "" : " Registrations: " + string.Join(", ", item.Registrations)) + ".")));
        checks.AddRange(Enum.GetValues<DepotConfiguration>()
            .Where(value => value != DepotConfiguration.None && options.Configuration.HasFlag(value))
            .Select(value => new WorkflowCheck("Configuration: " + value, CheckState.Information,
                ConfigurationConsequence(value) + " Review only; no change is available.")));
        return new(options, Array.AsReadOnly(appReviews), complete, false, checks.AsReadOnly());
    }

    private static DepotApplicationReview ReviewApplication(DepotApplications target,
        InstalledApplicationInventory installed, AppPackageInventoryResult packages,
        OfficeClickToRunSnapshot office, bool complete)
    {
        var registrations = new List<string>();
        foreach (var app in installed.Applications)
            if (Matches(target, app.Name, app.Publisher)) registrations.Add("Uninstall/" + app.View + "/" + app.Key);
        foreach (var package in packages.Packages)
            if (MatchesPackage(target, package.Name)) registrations.Add("Package/" + package.FullName);
        if (target == DepotApplications.Microsoft365)
            registrations.AddRange(office.Products.Where(product => !product.ProductId.StartsWith("Project", StringComparison.OrdinalIgnoreCase))
                .Select(product => "ClickToRun/" + product.View + "/" + product.ProductId));
        registrations = registrations.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToList();
        var detection = registrations.Count > 0 ? DepotDetection.Installed : complete ? DepotDetection.NotDetected : DepotDetection.Unknown;
        return new(target, detection, registrations.AsReadOnly(),
            "Registration evidence does not establish application health, edition, entitlement or provisioning eligibility");
    }

    private static bool Matches(DepotApplications target, string name, string? publisher)
    {
        string value = name.Trim();
        return target switch
        {
            DepotApplications.Microsoft365 => IsPublisher(publisher, "Microsoft") &&
                (value.StartsWith("Microsoft 365 ", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Microsoft Office ", StringComparison.OrdinalIgnoreCase)),
            DepotApplications.OneDrive => IsPublisher(publisher, "Microsoft") && value.Equals("Microsoft OneDrive", StringComparison.OrdinalIgnoreCase),
            DepotApplications.Teams => IsPublisher(publisher, "Microsoft") && value.Equals("Microsoft Teams", StringComparison.OrdinalIgnoreCase),
            DepotApplications.Chrome => IsPublisher(publisher, "Google") && value.StartsWith("Google Chrome", StringComparison.OrdinalIgnoreCase),
            DepotApplications.AcrobatReader => IsPublisher(publisher, "Adobe") &&
                (value.StartsWith("Adobe Acrobat Reader", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Adobe Acrobat", StringComparison.OrdinalIgnoreCase)),
            DepotApplications.Zoom => IsPublisher(publisher, "Zoom") && value.StartsWith("Zoom", StringComparison.OrdinalIgnoreCase),
            DepotApplications.SevenZip => value.StartsWith("7-Zip", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool MatchesPackage(DepotApplications target, string name) => target switch
    {
        DepotApplications.Teams => name is "MSTeams" or "MicrosoftTeams",
        _ => false
    };

    private static bool IsPublisher(string? publisher, string value) => publisher?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

    private static string ConfigurationConsequence(DepotConfiguration value) => value switch
    {
        DepotConfiguration.WindowsUpdates => "Would discover and install selected Windows or Microsoft updates through the existing update workflow.",
        DepotConfiguration.ComputerName => "Would require a separately validated computer name and may require restart.",
        DepotConfiguration.LocalAdministrator => "Would create a separately named local account and handle its secret outside reports and journals.",
        DepotConfiguration.AdministratorReview => "Would review exact local administrator identities before any disablement or removal.",
        DepotConfiguration.BrowserExtensions => "Would review exact browser extension identifiers, source stores and managed policy ownership.",
        DepotConfiguration.Cleanup => "Would use the existing selected cleanup targets, age limits and permanent-removal consent.",
        DepotConfiguration.Edge => "Would review individual Edge policies and current ownership before proposing changes.",
        DepotConfiguration.Privacy => "Would use individual SHADE selections and preserve managed settings.",
        DepotConfiguration.Taskbar => "Would review individual taskbar settings for the selected account scope.",
        DepotConfiguration.StartMenu => "Would review individual Start menu settings for the selected account scope.",
        DepotConfiguration.Notifications => "Would review individual notification settings for the selected account scope.",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}
