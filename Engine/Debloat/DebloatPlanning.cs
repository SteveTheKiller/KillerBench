namespace KillerBench.Engine.Debloat;

[Flags]
public enum DebloatAreas
{
    None = 0,
    RecallAndAi = 1 << 0,
    TelemetryAndReporting = 1 << 1,
    SearchAndSuggestions = 1 << 2,
    ConsumerContent = 1 << 3,
    TaskbarAndStart = 1 << 4,
    Notifications = 1 << 5,
    Edge = 1 << 6,
    HighPerformancePower = 1 << 7,
    SleepAndDisplayTimers = 1 << 8,
    Hibernation = 1 << 9,
    OfficeLanguages = 1 << 10,
    HpSoftware = 1 << 11,
    DellSoftware = 1 << 12,
    AsusAcerSoftware = 1 << 13,
    LenovoSoftware = 1 << 14,
    ThirdPartyApps = 1 << 15,
    VendorData = 1 << 16
}

public enum DebloatUserScope { RunningAccount, AllProfilesAndDefault }

public sealed record DebloatEnvironmentEvidence(string? Manufacturer, bool ManagedPolicyPresent,
    bool MdmEnrollmentPresent, bool Complete, string Detail);

public interface IDebloatEnvironmentReader
{
    Task<DebloatEnvironmentEvidence> ReadAsync(CancellationToken token);
}

public sealed record DebloatOptions(DebloatAreas Areas, DebloatUserScope UserScope,
    bool PreserveManagedSettings = true, bool PreserveUserData = true, string? RetainedOfficeCulture = null,
    DebloatPowerRequest? PowerRequest = null)
{
    public void Validate()
    {
        const DebloatAreas all = DebloatAreas.RecallAndAi | DebloatAreas.TelemetryAndReporting |
            DebloatAreas.SearchAndSuggestions | DebloatAreas.ConsumerContent | DebloatAreas.TaskbarAndStart |
            DebloatAreas.Notifications | DebloatAreas.Edge | DebloatAreas.HighPerformancePower |
            DebloatAreas.SleepAndDisplayTimers | DebloatAreas.Hibernation | DebloatAreas.OfficeLanguages |
            DebloatAreas.HpSoftware | DebloatAreas.DellSoftware | DebloatAreas.AsusAcerSoftware |
            DebloatAreas.LenovoSoftware | DebloatAreas.ThirdPartyApps | DebloatAreas.VendorData;
        if (Areas == DebloatAreas.None) throw new ArgumentException("Select at least one declutter area.", nameof(Areas));
        if ((Areas & ~all) != 0) throw new ArgumentOutOfRangeException(nameof(Areas));
        if (!Enum.IsDefined(UserScope)) throw new ArgumentOutOfRangeException(nameof(UserScope));
        if (!PreserveManagedSettings)
            throw new ArgumentException("Managed policy preservation is required.", nameof(PreserveManagedSettings));
        if (!PreserveUserData)
            throw new ArgumentException("User data preservation is required.", nameof(PreserveUserData));
        if (RetainedOfficeCulture is not null)
        {
            if (!Areas.HasFlag(DebloatAreas.OfficeLanguages))
                throw new ArgumentException("A retained Office culture requires the Office languages area.", nameof(RetainedOfficeCulture));
            if (RetainedOfficeCulture.Length != 5 || RetainedOfficeCulture[2] != '-' ||
                !RetainedOfficeCulture[..2].All(char.IsAsciiLetter) || !RetainedOfficeCulture[3..].All(char.IsAsciiLetter))
                throw new ArgumentException("The retained Office culture must use a language-region value such as en-us.", nameof(RetainedOfficeCulture));
        }
        if (Areas.HasFlag(DebloatAreas.SleepAndDisplayTimers) != (PowerRequest is not null))
            throw new ArgumentException("Sleep and display timer review requires all four requested timeouts, and timeout values require that area.", nameof(PowerRequest));
        PowerRequest?.Validate();
    }
}

public sealed record DebloatReview(DebloatOptions Options, DebloatEnvironmentEvidence Environment,
    IReadOnlyList<string> SelectedAreas, IReadOnlyList<PrivacyRegistryValue> CurrentValues,
    IReadOnlyList<DebloatRegistryValue> SpecificValues,
    IReadOnlyList<PrivacyServiceValue> CurrentServices,
    DebloatPowerEvidence? Power,
    DebloatPowerPlanEvidence? PowerPlans,
    DebloatRecallEvidence? Recall,
    DebloatTaskEvidence? Tasks,
    DebloatOemServiceEvidence? OemServices,
    DebloatVendorDataEvidence? VendorData,
    DebloatInventoryEvidence? Inventory,
    IReadOnlyList<WorkflowCheck> Checks, bool Ready, bool ExecutionSupported)
{
    public string Format() => "DECLUTTER / CHANGE REVIEW\n" +
        "User scope: " + Options.UserScope + "\nSelected areas: " + string.Join(", ", SelectedAreas) +
        "\nManufacturer: " + (Environment.Manufacturer ?? "Unavailable") +
        "\nManaged policy evidence: " + (Environment.ManagedPolicyPresent ? "Present" : Environment.Complete ? "Not found" : "Unknown") +
        "\nMDM enrollment evidence: " + (Environment.MdmEnrollmentPresent ? "Present" : Environment.Complete ? "Not found" : "Unknown") +
        "\nCurrent registry values inspected: " + CurrentValues.Count +
        "\nDEBLOAT-specific values inspected: " + SpecificValues.Count +
        "\nCurrent services inspected: " + CurrentServices.Count +
        "\nRetained Office culture: " + (Options.RetainedOfficeCulture ?? "Not selected") +
        (Options.PowerRequest is null ? "" : "\nRequested power timers (minutes): sleep AC/DC " + Options.PowerRequest.SleepAcMinutes + "/" + Options.PowerRequest.SleepDcMinutes +
            "; display AC/DC " + Options.PowerRequest.DisplayAcMinutes + "/" + Options.PowerRequest.DisplayDcMinutes) +
        (Power is null ? "" : "\nPower evidence complete: " + (Power.Complete ? "Yes" : "No")) +
        (PowerPlans is null ? "" : "\nHigh performance plan: " + (PowerPlans.HighPerformance?.Active == true ? "Active" : PowerPlans.HighPerformance is null ? "Not found" : "Available")) +
        (Recall is null ? "" : "\nRecall optional feature: " + Recall.State) +
        (Tasks is null ? "" : "\nExact scheduled tasks: " + Tasks.Matches.Count) +
        (OemServices is null ? "" : "\nExact OEM services: " + OemServices.Matches.Count) +
        (VendorData is null ? "" : "\nVendor paths inspected: " + VendorData.Paths.Count) +
        (Inventory is null ? "" : "\nInventory candidates: " + Inventory.Candidates.Count + "\nOffice products: " + Inventory.OfficeProducts.Count) +
        "\nPreserve managed settings: Yes\nPreserve user data: Yes\nExecution available: No\n\n" +
        string.Join("\n", Checks.Select(check => check.Name + ": " + check.State + "\n" + check.Detail)) +
        "\n\nNo optional feature, policy, profile, service, task, package, power setting, file or folder was changed.";
}

public static class DebloatPlanning
{
    private static readonly (DebloatAreas Area, string Name, string Consequence)[] Catalog =
    [
        (DebloatAreas.RecallAndAi, "Recall and Windows AI", "Reviews Recall optional-feature removal and Windows AI policy. Feature availability and policy ownership require live evidence."),
        (DebloatAreas.TelemetryAndReporting, "Telemetry and reporting", "Reviews diagnostic services, telemetry, feedback, error reporting and customer-experience policies."),
        (DebloatAreas.SearchAndSuggestions, "Search and suggestions", "Reviews web search suggestions and related per-user search preferences."),
        (DebloatAreas.ConsumerContent, "Consumer content", "Reviews consumer experiences, Spotlight, soft-landing content and device-metadata retrieval."),
        (DebloatAreas.TaskbarAndStart, "Taskbar and Start", "Reviews chat, news, recommendations and Start suggestions for the selected user scope."),
        (DebloatAreas.Notifications, "Notifications", "Reviews notification preferences for the selected user scope."),
        (DebloatAreas.Edge, "Microsoft Edge", "Reviews Edge policy changes. Bookmarks, profiles, cookies, history and other browser data remain protected."),
        (DebloatAreas.HighPerformancePower, "High performance power", "Reviews selecting the High performance plan while on AC power."),
        (DebloatAreas.SleepAndDisplayTimers, "Sleep and display timers", "Reviews AC and battery sleep and display timeout changes."),
        (DebloatAreas.Hibernation, "Hibernation", "Reviews disabling hibernation and the resulting removal of the hibernation file."),
        (DebloatAreas.OfficeLanguages, "Office languages", "Reviews non-English Click-to-Run language products. Exact installed cultures and the retained language require inventory."),
        (DebloatAreas.HpSoftware, "HP software", "Reviews HP services, tasks, installed applications and provisioned packages only when manufacturer evidence matches HP."),
        (DebloatAreas.DellSoftware, "Dell software", "Reviews Dell services, tasks, installed applications and provisioned packages only when manufacturer evidence matches Dell."),
        (DebloatAreas.AsusAcerSoftware, "ASUS and Acer software", "Reviews ASUS or Acer services, tasks, installed applications and provisioned packages only when manufacturer evidence matches."),
        (DebloatAreas.LenovoSoftware, "Lenovo software", "Reviews Lenovo services, tasks, installed applications and provisioned packages only when manufacturer evidence matches Lenovo."),
        (DebloatAreas.ThirdPartyApps, "Third-party applications", "Reviews a bounded package catalog for exact installed matches. Name similarity alone never authorizes removal."),
        (DebloatAreas.VendorData, "Vendor data", "Reviews vendor folders and browser additions separately. User profiles and browser data remain protected.")
    ];

    public static async Task<DebloatReview> ReviewAsync(DebloatOptions options,
        IProgress<string>? progress = null, IDebloatEnvironmentReader? environmentReader = null,
        Func<PrivacyRegistryTarget, PrivacyRegistryValue>? registryReader = null,
        Func<PrivacyServiceTarget, CancellationToken, Task<ToolResult>>? serviceReader = null,
        IDebloatPowerReader? powerReader = null,
        Func<DebloatRegistryTarget, DebloatRegistryValue>? specificRegistryReader = null,
        Func<CancellationToken, Task<DebloatInventoryEvidence>>? inventoryReader = null,
        IDebloatRecallReader? recallReader = null,
        Func<CancellationToken, Task<DebloatTaskEvidence>>? taskReader = null,
        Func<CancellationToken, Task<DebloatOemServiceEvidence>>? oemServiceReader = null,
        Func<CancellationToken, DebloatVendorDataEvidence>? vendorDataReader = null,
        IDebloatPowerPlanReader? powerPlanReader = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        token.ThrowIfCancellationRequested();
        progress?.Report("Building the selected declutter change review...");
        environmentReader ??= new WindowsDebloatEnvironmentReader();
        DebloatEnvironmentEvidence environment = await environmentReader.ReadAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        PrivacyArea[] privacyAreas = PrivacyAreas(options.Areas);
        IReadOnlyList<PrivacyRegistryValue> currentValues = privacyAreas.Length == 0
            ? [] : PrivacyInspection.Inspect(new PrivacyOptions(privacyAreas), registryReader, token);
        IReadOnlyList<DebloatRegistryValue> specificValues = DebloatRegistryInspection.Inspect(
            options, specificRegistryReader, token);
        progress?.Report("Reading services related to the selected declutter areas...");
        IReadOnlyList<PrivacyServiceValue> currentServices = privacyAreas.Length == 0
            ? [] : await PrivacyInspection.InspectServicesAsync(new PrivacyOptions(privacyAreas), serviceReader, token)
                .ConfigureAwait(false);
        DebloatPowerEvidence? power = null;
        if ((options.Areas & (DebloatAreas.HighPerformancePower | DebloatAreas.SleepAndDisplayTimers |
            DebloatAreas.Hibernation)) != 0)
        {
            progress?.Report("Reading active power and hibernation state...");
            powerReader ??= new WindowsDebloatPowerReader();
            power = await powerReader.ReadAsync(token).ConfigureAwait(false);
        }
        DebloatPowerPlanEvidence? powerPlans = null;
        if (options.Areas.HasFlag(DebloatAreas.HighPerformancePower))
        {
            progress?.Report("Reading available power plans...");
            powerPlans = await (powerPlanReader ?? new WindowsDebloatPowerPlanReader()).ReadAsync(token).ConfigureAwait(false);
        }
        DebloatRecallEvidence? recall = null;
        if (options.Areas.HasFlag(DebloatAreas.RecallAndAi))
        {
            progress?.Report("Reading the exact Recall optional-feature state...");
            recall = await (recallReader ?? new WindowsDebloatRecallReader()).ReadAsync(token).ConfigureAwait(false);
        }
        DebloatTaskEvidence? tasks = null;
        if ((options.Areas & (DebloatAreas.TelemetryAndReporting | DebloatAreas.HpSoftware |
            DebloatAreas.DellSoftware | DebloatAreas.AsusAcerSoftware)) != 0)
        {
            progress?.Report("Reading exact scheduled tasks for selected declutter areas...");
            tasks = taskReader is not null ? await taskReader(token).ConfigureAwait(false) :
                await DebloatTaskReview.ReadAsync(options, environment, token).ConfigureAwait(false);
        }
        DebloatOemServiceEvidence? oemServices = null;
        if ((options.Areas & (DebloatAreas.HpSoftware | DebloatAreas.DellSoftware | DebloatAreas.AsusAcerSoftware)) != 0)
        {
            progress?.Report("Reading exact OEM service registrations...");
            oemServices = oemServiceReader is not null ? await oemServiceReader(token).ConfigureAwait(false) :
                await DebloatOemServiceReview.ReadAsync(options, environment, token).ConfigureAwait(false);
        }
        DebloatVendorDataEvidence? vendorData = null;
        if (options.Areas.HasFlag(DebloatAreas.VendorData))
        {
            progress?.Report("Inspecting exact vendor-data paths without reading their contents...");
            vendorData = vendorDataReader is not null ? vendorDataReader(token) : DebloatVendorDataReview.Review(token: token);
        }
        DebloatInventoryEvidence? inventory = null;
        if ((options.Areas & (DebloatAreas.OfficeLanguages | DebloatAreas.HpSoftware | DebloatAreas.DellSoftware |
            DebloatAreas.AsusAcerSoftware | DebloatAreas.LenovoSoftware | DebloatAreas.ThirdPartyApps)) != 0)
        {
            progress?.Report("Reading exact application, package and Office registrations...");
            inventory = inventoryReader is not null ? await inventoryReader(token).ConfigureAwait(false) :
                DebloatInventoryReview.Review(options, environment,
                    await InstalledApplications.ReadAsync(InstalledApplicationScope.Machine, token).ConfigureAwait(false),
                    await AppPackageInventory.ReadAsync(options.UserScope == DebloatUserScope.AllProfilesAndDefault ? AppPackageScope.AllUsers : AppPackageScope.CurrentAccount, token).ConfigureAwait(false),
                    await ProvisionedPackages.ReadAsync(token).ConfigureAwait(false), OfficeClickToRunInventory.Read(token));
        }
        var selected = Catalog.Where(entry => options.Areas.HasFlag(entry.Area)).ToArray();
        var checks = selected.Select(entry => AreaCheck(entry, environment)).ToList();
        checks.Add(new("Management evidence", environment.Complete ?
            environment.ManagedPolicyPresent || environment.MdmEnrollmentPresent ? CheckState.Attention : CheckState.Clear : CheckState.Unknown,
            environment.Detail + " Existing managed values remain protected. Registry presence is evidence for review, not proof of effective policy ownership."));
        if (currentValues.Count > 0)
        {
            int unavailable = currentValues.Count(value => value.State is "Unavailable" or "Unsupported value");
            int policies = currentValues.Count(value => value.Target.Policy && value.State != "Not configured");
            checks.Add(new("Current registry state", unavailable == 0 ? CheckState.Information : CheckState.Unknown,
                currentValues.Count + " relevant machine or running-account values inspected; " + policies +
                " policy value(s) are present and " + unavailable + " value(s) are unavailable or unsupported. " +
                (options.UserScope == DebloatUserScope.AllProfilesAndDefault
                    ? "Current-account values do not represent other profiles or the default profile. No offline hive was loaded."
                    : "User values apply only to the account running KillerBench.") +
                " Recorded values are evidence only and do not authorize a change."));
        }
        if (specificValues.Count > 0)
        {
            int unavailable = specificValues.Count(value => value.State is "Unavailable" or "Unsupported value");
            int already = specificValues.Count(value => value.State == "Already proposed");
            int different = specificValues.Count(value => value.State == "Different");
            checks.Add(new("Declutter-specific registry state", unavailable == 0 ? CheckState.Information : CheckState.Unknown,
                specificValues.Count + " selected value(s) inspected; " + already + " already match the source proposal, " +
                different + " differ and " + unavailable + " are unavailable or unsupported. " +
                (options.UserScope == DebloatUserScope.AllProfilesAndDefault && specificValues.Any(value => !value.Target.Machine)
                    ? "Current-account values do not represent other profiles or the default profile. No offline hive was loaded. " : "") +
                "Proposed values are reference data only. Existing policy and application requirements remain authoritative."));
        }
        if (currentServices.Count > 0)
        {
            int unavailable = currentServices.Count(service => service.State == "Unavailable");
            int running = currentServices.Count(service => service.State == "Running");
            checks.Add(new("Current service state", unavailable == 0 ? CheckState.Information : CheckState.Unknown,
                currentServices.Count + " selected service(s) inspected; " + running + " running and " + unavailable +
                " unavailable. A stopped demand-start service can be normal. Service state does not prove diagnostic collection, upload, policy ownership or application dependency. No service change is authorized."));
        }
        if (power is not null)
        {
            checks.Add(new("Current power state", power.Complete ? CheckState.Information : CheckState.Unknown,
                "Active scheme: " + (power.ActiveSchemeId ?? "Unavailable") +
                "; sleep AC/DC seconds: " + Value(power.SleepAcSeconds) + "/" + Value(power.SleepDcSeconds) +
                "; display AC/DC seconds: " + Value(power.DisplayAcSeconds) + "/" + Value(power.DisplayDcSeconds) +
                "; hibernation enabled: " + Value(power.HibernationEnabled) +
                "; hibernation file present: " + Value(power.HibernationFilePresent) + ". " + power.Detail +
                " Zero timeout means Never. These values do not establish battery suitability or authorize a power change."));
            if (options.PowerRequest is not null)
                checks.Add(new("Requested power timers", CheckState.Information,
                    "Sleep AC/DC: " + options.PowerRequest.SleepAcMinutes + "/" + options.PowerRequest.SleepDcMinutes +
                    " minutes; display AC/DC: " + options.PowerRequest.DisplayAcMinutes + "/" + options.PowerRequest.DisplayDcMinutes +
                    " minutes. Zero means Never. These requested values are a review plan only and were not applied."));
        }
        if (powerPlans is not null)
            checks.Add(new("High performance power plan", powerPlans.Complete ? CheckState.Information : CheckState.Unknown,
                powerPlans.Detail + " Plan inventory is evidence only and no active plan was changed.", Evidence: powerPlans.Evidence));
        if (recall is not null)
            checks.Add(new("Recall optional feature", recall.Complete ?
                recall.State is "EnablePending" or "DisablePending" ? CheckState.Attention : CheckState.Information : CheckState.Unknown,
                "State: " + recall.State + ". " + recall.Detail +
                " Feature state is evidence only and does not authorize enablement, disablement, removal or payload changes.", Evidence: recall.Evidence));
        if (tasks is not null)
            checks.Add(new("Selected scheduled tasks", tasks.Complete ? CheckState.Information : CheckState.Unknown,
                tasks.Matches.Count + " exact selected task(s) found. " + tasks.Detail, Evidence: tasks.Evidence));
        if (oemServices is not null)
            checks.Add(new("Selected OEM services", oemServices.Complete ? CheckState.Information : CheckState.Unknown,
                oemServices.Matches.Count + " exact selected OEM service(s) found. " + oemServices.Detail, Evidence: oemServices.Evidence));
        if (vendorData is not null)
            checks.Add(new("Vendor data paths", vendorData.Complete ? CheckState.Information : CheckState.Unknown,
                vendorData.Paths.Count(value => value.State == "Present") + " of " + vendorData.Paths.Count +
                " exact source path(s) are present. " + vendorData.Detail));
        if (inventory is not null)
            checks.Add(new("Application and Office inventory", inventory.Complete ? CheckState.Information : CheckState.Unknown,
                inventory.Candidates.Count + " exact OEM or third-party candidate(s) and " + inventory.OfficeProducts.Count +
                " Office Click-to-Run product(s) recorded. " + inventory.Detail));
        checks.Add(new("Execution boundary", CheckState.Information,
            "This increment reviews selected consequences only. It does not load offline user hives or alter optional features, policies, services, tasks, packages, power settings, files or folders."));
        checks.Add(new("Eligibility", CheckState.Unknown,
            "A later execution stage must collect current state, identify policy ownership, match the manufacturer and exact package registrations, create a bounded plan, obtain confirmation, journal every mutation and verify the result."));
        return new DebloatReview(options, environment,
            Array.AsReadOnly(selected.Select(entry => entry.Name).ToArray()), currentValues, specificValues,
            currentServices, power, powerPlans, recall, tasks, oemServices, vendorData, inventory,
            checks.AsReadOnly(), true, false);
    }

    private static string Value(uint? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unavailable";
    private static string Value(bool? value) => value.HasValue ? value.Value ? "Yes" : "No" : "Unavailable";

    private static PrivacyArea[] PrivacyAreas(DebloatAreas areas)
    {
        var selected = new HashSet<PrivacyArea>();
        if (areas.HasFlag(DebloatAreas.TelemetryAndReporting))
        { selected.Add(PrivacyArea.DiagnosticData); selected.Add(PrivacyArea.Feedback); }
        if (areas.HasFlag(DebloatAreas.SearchAndSuggestions)) selected.Add(PrivacyArea.WebSearch);
        if (areas.HasFlag(DebloatAreas.ConsumerContent))
        { selected.Add(PrivacyArea.Advertising); selected.Add(PrivacyArea.Spotlight); }
        if (areas.HasFlag(DebloatAreas.TaskbarAndStart)) selected.Add(PrivacyArea.Advertising);
        return [.. selected.OrderBy(area => area)];
    }

    private static WorkflowCheck AreaCheck((DebloatAreas Area, string Name, string Consequence) entry,
        DebloatEnvironmentEvidence environment)
    {
        string[]? expected = entry.Area switch
        {
            DebloatAreas.HpSoftware => ["hp", "hewlett-packard", "hewlett packard"],
            DebloatAreas.DellSoftware => ["dell"],
            DebloatAreas.AsusAcerSoftware => ["asus", "asustek", "acer"],
            DebloatAreas.LenovoSoftware => ["lenovo"],
            _ => null
        };
        if (expected is null) return new(entry.Name, CheckState.Information, entry.Consequence);
        if (!environment.Complete || string.IsNullOrWhiteSpace(environment.Manufacturer))
            return new(entry.Name, CheckState.Unknown, entry.Consequence + " Manufacturer evidence is unavailable, so this OEM area is not eligible.");
        bool matches = expected.Any(value => environment.Manufacturer.Contains(value, StringComparison.OrdinalIgnoreCase));
        return new(entry.Name, matches ? CheckState.Information : CheckState.Attention,
            entry.Consequence + (matches
                ? " Current manufacturer evidence matches this OEM family. Exact installed identities still require collection."
                : " Current manufacturer evidence does not match this OEM family, so this area is not applicable."));
    }
}

public sealed class WindowsDebloatEnvironmentReader : IDebloatEnvironmentReader
{
    public Task<DebloatEnvironmentEvidence> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(new DebloatEnvironmentEvidence(null, false, false, false,
                "Windows manufacturer and management evidence is unavailable on this platform."));
        return Task.FromResult(ReadWindows());
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static DebloatEnvironmentEvidence ReadWindows()
    {
        try
        {
            using var bios = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            string? manufacturer = bios?.GetValue("SystemManufacturer") as string;
            if (manufacturer is { Length: > 256 } || manufacturer?.Any(char.IsControl) == true) manufacturer = null;
            manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim();
            using var policy = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PolicyManager\current\device");
            using var enrollments = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Enrollments");
            bool managed = policy?.SubKeyCount > 0;
            bool enrolled = enrollments?.GetSubKeyNames().Any(name => Guid.TryParse(name, out _)) == true;
            return new DebloatEnvironmentEvidence(manufacturer, managed, enrolled, true,
                "Read local manufacturer and Windows policy or enrollment registry presence.");
        }
        catch (Exception error) when (error is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return new DebloatEnvironmentEvidence(null, false, false, false,
                "Manufacturer or management evidence could not be read: " + error.Message);
        }
    }
}
