using System.Diagnostics;

namespace KillerBench.Engine.Automation;

[Flags]
public enum MaceTargets
{
    None = 0,
    OneDrive = 1,
    Outlook = 2,
    Office = 4,
    Project = 8,
    Teams = 16
}

public enum MaceScope { RunningAccount, AllUsers }

public sealed record MaceReviewOptions(MaceTargets Targets, MaceScope Scope = MaceScope.RunningAccount,
    bool PreserveApplicationData = true, bool PreserveCachedCredentials = true,
    bool RestoreOneDriveFolderLocations = false)
{
    public void Validate()
    {
        const MaceTargets all = MaceTargets.OneDrive | MaceTargets.Outlook | MaceTargets.Office |
            MaceTargets.Project | MaceTargets.Teams;
        if (Targets == MaceTargets.None || (Targets & ~all) != 0)
            throw new ArgumentException("Select at least one supported Microsoft application target.", nameof(Targets));
        if (!Enum.IsDefined(Scope)) throw new ArgumentOutOfRangeException(nameof(Scope));
        if (RestoreOneDriveFolderLocations && !Targets.HasFlag(MaceTargets.OneDrive))
            throw new ArgumentException("OneDrive folder-location review requires selecting OneDrive.", nameof(RestoreOneDriveFolderLocations));
    }
}

public sealed record MaceCandidate(MaceTargets Target, string RegistrationType, string Scope,
    string Identity, string Name, string? Version, bool Protected)
{
    public bool DevelopmentMode { get; init; }
}

public sealed record MaceProcessEvidence(int OneDriveProcesses, int OutlookProcesses, int OfficeProcesses,
    int ProjectProcesses, int TeamsProcesses, bool Complete, string Limitation)
{
    public int RelevantCount(MaceTargets targets) =>
        (targets.HasFlag(MaceTargets.OneDrive) ? OneDriveProcesses : 0) +
        (targets.HasFlag(MaceTargets.Outlook) ? OutlookProcesses : 0) +
        (targets.HasFlag(MaceTargets.Office) ? OfficeProcesses : 0) +
        (targets.HasFlag(MaceTargets.Project) ? ProjectProcesses : 0) +
        (targets.HasFlag(MaceTargets.Teams) ? TeamsProcesses : 0);
}

public sealed record MaceRemovalReview(MaceReviewOptions Options, IReadOnlyList<MaceCandidate> Candidates,
    bool Complete, bool ExecutionSupported, IReadOnlyList<WorkflowCheck> Checks)
{
    public MaceProcessEvidence? ProcessEvidence { get; init; }
    public MaceOneDriveFolderEvidence? OneDriveFolderEvidence { get; init; }
    public MaceCredentialEvidence? CredentialEvidence { get; init; }
    public MaceApplicationDataEvidence? ApplicationDataEvidence { get; init; }
    public MaceManagedPolicyEvidence? ManagedPolicyEvidence { get; init; }
    public MaceRemovalProviderEvidence? RemovalProviderEvidence { get; init; }

    public string Format()
    {
        string selected = string.Join(", ", Enum.GetValues<MaceTargets>()
            .Where(value => value != MaceTargets.None && Options.Targets.HasFlag(value)));
        return "MICROSOFT APP REMOVAL REVIEW\nSelected: " + selected + "\nScope: " + Options.Scope +
            "\nApplication data: " + (Options.PreserveApplicationData ? "Preserve" : "Remove requested") +
            "\nCached credentials: " + (Options.PreserveCachedCredentials ? "Preserve" : "Remove requested") +
            "\nOneDrive folder locations: " + (Options.RestoreOneDriveFolderLocations ? "Review requested" : "Leave unchanged") +
            "\nCandidates: " + Candidates.Count + "\nCoverage: " + (Complete ? "Complete for collected sources" : "Incomplete") +
            (ProcessEvidence is null ? "" : "\nSelected applications active: " + ProcessEvidence.RelevantCount(Options.Targets)) +
            (OneDriveFolderEvidence is null ? "" : "\nKnown folders under OneDrive: " + OneDriveFolderEvidence.RedirectedCount) +
            (CredentialEvidence is null ? "" : "\nSelected credential references: " + CredentialEvidence.RelevantCount(Options.Targets)) +
            (ApplicationDataEvidence is null ? "" : "\nSelected application-data locations present: " + ApplicationDataEvidence.PresentCount(Options.Targets)) +
            (ManagedPolicyEvidence is null ? "" : "\nSelected managed policy areas configured: " + ManagedPolicyEvidence.ConfiguredCount(Options.Targets)) +
            (RemovalProviderEvidence is null ? "" : "\nCandidates without an identified provider: " + RemovalProviderEvidence.UnclassifiedCount) +
            "\nExecution available: No\n\nNo process, application, package, file, credential, folder location, registry value or user data changed.";
    }
}

public static class MacePlanning
{
    public static async Task<MaceRemovalReview> ReviewCurrentAsync(MaceReviewOptions options,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        progress?.Report("Reading machine application registrations...");
        var machine = await InstalledApplications.ReadAsync(InstalledApplicationScope.Machine, token).ConfigureAwait(false);
        progress?.Report("Reading running-account application registrations...");
        var account = await InstalledApplications.ReadAsync(InstalledApplicationScope.CurrentAccount, token).ConfigureAwait(false);
        var packageScope = options.Scope == MaceScope.AllUsers ? AppPackageScope.AllUsers : AppPackageScope.CurrentAccount;
        progress?.Report("Reading " + packageScope + " app package registrations...");
        var packages = await AppPackageInventory.ReadAsync(packageScope, token).ConfigureAwait(false);
        ProvisionedPackageInventory? provisioned = null;
        if (options.Scope == MaceScope.AllUsers)
        {
            progress?.Report("Reading packages provisioned for new users...");
            provisioned = await ProvisionedPackages.ReadAsync(token).ConfigureAwait(false);
        }
        progress?.Report("Reading Office Click-to-Run product composition...");
        var clickToRun = OfficeClickToRunInventory.Read(token);
        progress?.Report("Checking whether selected Microsoft applications are running...");
        var processes = MaceProcessInventory.Read(options.Targets, token: token);
        MaceOneDriveFolderEvidence? folders = null;
        if (options.Targets.HasFlag(MaceTargets.OneDrive))
        {
            progress?.Report("Reviewing OneDrive known-folder locations without retaining paths...");
            folders = MaceOneDriveFolderReview.Read(options.Scope, token);
        }
        MaceCredentialEvidence? credentials = null;
        if (!options.PreserveCachedCredentials)
        {
            progress?.Report("Counting selected product credential references without retaining identities...");
            credentials = MaceCredentialReview.Read(options.Targets, options.Scope, token);
        }
        MaceApplicationDataEvidence? applicationData = null;
        if (!options.PreserveApplicationData)
        {
            progress?.Report("Reviewing selected application-data locations without traversing contents...");
            applicationData = MaceApplicationDataReview.Read(options.Targets, options.Scope, token);
        }
        progress?.Report("Reviewing selected managed configuration without retaining policy values...");
        var managedPolicy = MaceManagedPolicyReview.Read(options.Targets, options.Scope, token);
        return Review(options, machine, account, packages, provisioned, clickToRun, processes, folders,
            credentials, applicationData, managedPolicy);
    }

    public static MaceRemovalReview Review(MaceReviewOptions options, InstalledApplicationInventory machine,
        InstalledApplicationInventory account, AppPackageInventoryResult packages,
        ProvisionedPackageInventory? provisioned = null, OfficeClickToRunSnapshot? clickToRun = null,
        MaceProcessEvidence? processes = null, MaceOneDriveFolderEvidence? oneDriveFolders = null,
        MaceCredentialEvidence? credentials = null, MaceApplicationDataEvidence? applicationData = null,
        MaceManagedPolicyEvidence? managedPolicy = null, MaceRemovalProviderEvidence? removalProviders = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(packages);
        options.Validate();
        if (machine.Scope != InstalledApplicationScope.Machine || account.Scope != InstalledApplicationScope.CurrentAccount)
            throw new ArgumentException("Microsoft app removal review requires separate machine and running-account application inventories.");
        var candidates = new List<MaceCandidate>();
        AddApplications(candidates, options, machine);
        AddApplications(candidates, options, account);
        AddPackages(candidates, options, packages);
        if (provisioned is not null) AddProvisioned(candidates, options, provisioned);
        if (clickToRun is not null) AddClickToRun(candidates, options, clickToRun);
        candidates = [.. candidates.GroupBy(item => item.RegistrationType + "\0" + item.Scope + "\0" + item.Identity,
                StringComparer.OrdinalIgnoreCase).Select(group => group.First())
            .OrderBy(item => item.Target).ThenBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Identity, StringComparer.Ordinal)];
        removalProviders ??= MaceRemovalProviderReview.Classify(candidates);
        bool complete = machine.Complete && account.Complete && packages.Complete && clickToRun?.Complete == true &&
            processes?.Complete == true && (options.Scope != MaceScope.AllUsers || provisioned?.Complete == true) &&
            (!options.Targets.HasFlag(MaceTargets.OneDrive) || oneDriveFolders?.Complete == true) &&
            (options.PreserveCachedCredentials || credentials?.Complete == true) &&
            (options.PreserveApplicationData || applicationData?.Complete == true) && managedPolicy?.Complete == true &&
            removalProviders.Complete;
        int activeProcesses = processes?.RelevantCount(options.Targets) ?? 0;
        List<WorkflowCheck> checks =
        [
            new("Microsoft app removal review coverage", complete ? CheckState.Information : CheckState.Unknown,
                $"{candidates.Count} exact registrations matched the selected products. " +
                (complete ? "Every requested inventory source completed. " : "At least one requested inventory source was incomplete. Absence is not established. ") +
                "Portable applications, individual Click-to-Run app inclusion, application use, managed ownership and user data are not verified. No removal is authorized."),
            new("Retention choices", CheckState.Information,
                "Application data: " + (options.PreserveApplicationData ? "preserve" : "removal requested for future review") +
                "; cached credentials: " + (options.PreserveCachedCredentials ? "preserve" : "removal requested for future review") +
                "; OneDrive folder locations: " + (options.RestoreOneDriveFolderLocations ? "review requested" : "leave unchanged") +
                ". These choices describe a future plan and do not change the computer."),
            new("Microsoft app removal process readiness", processes is null || !processes.Complete ? CheckState.Unknown :
            activeProcesses > 0 ? CheckState.Attention : CheckState.Information,
            processes is null ? "Selected-application process evidence was not supplied. Future removal eligibility must remain blocked." :
            !processes.Complete ? "Selected-application process evidence is incomplete. Future removal eligibility must remain blocked. " + processes.Limitation :
            activeProcesses > 0 ? $"{activeProcesses} selected Microsoft application process{(activeProcesses == 1 ? " is" : "es are")} active. Close the selected applications and collect fresh evidence before any removal. " + processes.Limitation :
            "No selected Microsoft application process was found in the bounded inventory. Registration, ownership, data, credential and removal eligibility checks are still required. " + processes.Limitation)
        ];
        if (options.Targets.HasFlag(MaceTargets.OneDrive))
        {
            checks.Add(new("OneDrive known-folder review", oneDriveFolders is null || !oneDriveFolders.Complete ? CheckState.Unknown :
                oneDriveFolders.RedirectedCount > 0 ? CheckState.Attention : CheckState.Information,
                oneDriveFolders is null ? "OneDrive folder-location evidence was not supplied. Future removal eligibility must remain blocked." :
                !oneDriveFolders.Complete ? "OneDrive folder-location evidence is incomplete. Future removal eligibility must remain blocked. " + oneDriveFolders.Limitation :
                oneDriveFolders.RedirectedCount > 0 ? $"{oneDriveFolders.RedirectedCount} of {oneDriveFolders.Folders.Count} reviewed known folders are under a registered OneDrive root: " +
                    string.Join(", ", oneDriveFolders.Folders.Where(item => item.UnderRegisteredOneDriveRoot).Select(item => item.Folder)) +
                    ". Record an explicit restoration plan before any future OneDrive removal. " + oneDriveFolders.Limitation :
                $"None of the {oneDriveFolders.Folders.Count} reviewed known folders is under a registered OneDrive root. This does not establish file ownership, sync completion or removal eligibility. " + oneDriveFolders.Limitation));
        }
        if (!options.PreserveCachedCredentials)
        {
            int references = credentials?.RelevantCount(options.Targets) ?? 0;
            checks.Add(new("Cached credential review", credentials is null || !credentials.Complete ? CheckState.Unknown :
                references > 0 ? CheckState.Attention : CheckState.Information,
                credentials is null ? "Credential reference evidence was not supplied. Future credential removal eligibility must remain blocked." :
                !credentials.Complete ? "Credential reference evidence is incomplete. Future credential removal eligibility must remain blocked. " + credentials.Limitation :
                references > 0 ? $"{references} cached credential references matched the selected products. Exact ownership and sign-in impact require review before any future removal. " + credentials.Limitation :
                "No cached credential reference matched the selected products. This does not establish account ownership, sign-in state or removal eligibility. " + credentials.Limitation));
        }
        if (!options.PreserveApplicationData)
        {
            int present = applicationData?.PresentCount(options.Targets) ?? 0;
            int reparse = applicationData?.Locations.Count(item => item.Present && item.ReparsePoint) ?? 0;
            checks.Add(new("Application-data review", applicationData is null || !applicationData.Complete ? CheckState.Unknown :
                present > 0 ? CheckState.Attention : CheckState.Information,
                applicationData is null ? "Application-data evidence was not supplied. Future data removal eligibility must remain blocked." :
                !applicationData.Complete ? "Application-data evidence is incomplete. Future data removal eligibility must remain blocked. " + applicationData.Limitation :
                present > 0 ? $"{present} selected application-data location{(present == 1 ? " is" : "s are")} present" +
                    (reparse > 0 ? $", including {reparse} reparse-point location{(reparse == 1 ? "" : "s")}" : "") +
                    ": " + string.Join(", ", applicationData.Locations.Where(item => item.Present).Select(item => item.Location)) +
                    ". Establish ownership and a verified backup or explicit preservation plan before any future removal. " + applicationData.Limitation :
                "No fixed selected application-data location was present. This does not establish that no user data exists or authorize removal. " + applicationData.Limitation));
        }
        int managedAreas = managedPolicy?.ConfiguredCount(options.Targets) ?? 0;
        checks.Add(new("Managed configuration review", managedPolicy is null || !managedPolicy.Complete ? CheckState.Unknown :
            managedAreas > 0 ? CheckState.Attention : CheckState.Information,
            managedPolicy is null ? "Managed-configuration evidence was not supplied. Future removal eligibility must remain blocked." :
            !managedPolicy.Complete ? "Managed-configuration evidence is incomplete. Future removal eligibility must remain blocked. " + managedPolicy.Limitation :
            managedAreas > 0 ? $"{managedAreas} selected policy area{(managedAreas == 1 ? " is" : "s are")} configured: " +
                string.Join(", ", managedPolicy.Areas.Where(item => item.MachineConfigured || item.CurrentAccountConfigured).Select(item => item.Area)) +
                ". Preserve policy and establish deployment ownership before any future removal. " + managedPolicy.Limitation :
            "No setting was found in the fixed selected policy areas. This does not prove that the applications are unmanaged or authorize removal. " + managedPolicy.Limitation));
        checks.Add(new("Removal provider readiness", !removalProviders.Complete ? CheckState.Unknown :
            removalProviders.UnclassifiedCount > 0 ? CheckState.Attention : CheckState.Information,
            !removalProviders.Complete ? "Removal-provider classification is incomplete. Future removal eligibility must remain blocked. " + removalProviders.Limitation :
            removalProviders.UnclassifiedCount > 0 ? $"{removalProviders.UnclassifiedCount} of {removalProviders.Candidates.Count} exact candidates lack a supported automatic removal provider. Provider classification does not authorize removal or prove user-data safety. " + removalProviders.Limitation :
            removalProviders.Candidates.Count == 0 ? "No exact candidate required provider classification. This does not establish application absence or removal eligibility. " + removalProviders.Limitation :
            $"All {removalProviders.Candidates.Count} exact candidates have an identified provider boundary. Provider classification does not authorize removal or prove user-data safety. " + removalProviders.Limitation));
        checks.AddRange(candidates.Select(item => new WorkflowCheck(item.Target + ": " + item.Name,
            item.Protected ? CheckState.Attention : CheckState.Information,
            item.RegistrationType + " identity: " + item.Identity + "; scope: " + item.Scope +
            "; version: " + (item.Version ?? "not reported") +
            (item.Protected ? ". Windows reports this package as non-removable. It must not be selected for removal." :
                ". Registration is inventory evidence only. Removal eligibility and data ownership are unverified."))));
        return new(options, candidates.AsReadOnly(), complete, false, checks.AsReadOnly())
        {
            ProcessEvidence = processes,
            OneDriveFolderEvidence = oneDriveFolders,
            CredentialEvidence = credentials,
            ApplicationDataEvidence = applicationData,
            ManagedPolicyEvidence = managedPolicy,
            RemovalProviderEvidence = removalProviders
        };
    }

    private static void AddApplications(List<MaceCandidate> output, MaceReviewOptions options, InstalledApplicationInventory inventory)
    {
        foreach (var app in inventory.Applications)
        {
            var target = MatchApplication(app);
            if (target == MaceTargets.None || !options.Targets.HasFlag(target)) continue;
            output.Add(new(target, "Uninstall registration", inventory.Scope.ToString(), app.View + "/" + app.Key,
                app.Name, app.Version, false));
        }
    }

    private static void AddPackages(List<MaceCandidate> output, MaceReviewOptions options, AppPackageInventoryResult inventory)
    {
        foreach (var package in inventory.Packages)
        {
            var target = MatchPackage(package.Name);
            if (target == MaceTargets.None || !options.Targets.HasFlag(target)) continue;
            output.Add(new(target, "Registered app package", inventory.Scope.ToString(), package.FullName,
                package.Name, null, package.NonRemovable) { DevelopmentMode = package.DevelopmentMode });
        }
    }

    private static void AddProvisioned(List<MaceCandidate> output, MaceReviewOptions options, ProvisionedPackageInventory inventory)
    {
        foreach (var package in inventory.Packages)
        {
            var target = MatchPackage(package.Name);
            if (target == MaceTargets.None || !options.Targets.HasFlag(target)) continue;
            output.Add(new(target, "Provisioned app package", "NewUsers", package.PackageName,
                package.Name, package.Version, false));
        }
    }

    private static void AddClickToRun(List<MaceCandidate> output, MaceReviewOptions options, OfficeClickToRunSnapshot inventory)
    {
        foreach (var product in inventory.Products)
        {
            MaceTargets target = product.ProductId.StartsWith("Project", StringComparison.OrdinalIgnoreCase)
                ? MaceTargets.Project : MaceTargets.Office;
            if (!options.Targets.HasFlag(target)) continue;
            output.Add(new(target, "Office Click-to-Run product", "Machine", product.View + "/" + product.ProductId,
                product.ProductId, product.Version, false));
        }
    }

    private static MaceTargets MatchApplication(InstalledApplication app)
    {
        if (!IsMicrosoft(app.Publisher)) return MaceTargets.None;
        string name = app.Name.Trim();
        if (name.Equals("Microsoft OneDrive", StringComparison.OrdinalIgnoreCase) || name.Equals("OneDrive", StringComparison.OrdinalIgnoreCase)) return MaceTargets.OneDrive;
        if (name.StartsWith("Microsoft Project", StringComparison.OrdinalIgnoreCase)) return MaceTargets.Project;
        if (name.Equals("Microsoft Outlook", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Microsoft Outlook ", StringComparison.OrdinalIgnoreCase)) return MaceTargets.Outlook;
        if (name.Equals("Microsoft Teams", StringComparison.OrdinalIgnoreCase) || name.Equals("Teams Machine-Wide Installer", StringComparison.OrdinalIgnoreCase)) return MaceTargets.Teams;
        if (name.StartsWith("Microsoft 365 ", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Microsoft Office ", StringComparison.OrdinalIgnoreCase)) return MaceTargets.Office;
        return MaceTargets.None;
    }

    private static MaceTargets MatchPackage(string name) => name switch
    {
        "Microsoft.OutlookForWindows" => MaceTargets.Outlook,
        "MSTeams" or "MicrosoftTeams" => MaceTargets.Teams,
        _ => MaceTargets.None
    };

    private static bool IsMicrosoft(string? publisher) => publisher is not null &&
        (publisher.Equals("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) ||
         publisher.Equals("Microsoft", StringComparison.OrdinalIgnoreCase));
}

public static class MaceProcessInventory
{
    private static readonly IReadOnlyDictionary<MaceTargets, string[]> Names = new Dictionary<MaceTargets, string[]>
    {
        [MaceTargets.OneDrive] = ["OneDrive"],
        [MaceTargets.Outlook] = ["olk", "OUTLOOK"],
        [MaceTargets.Office] = ["WINWORD", "EXCEL", "POWERPNT", "ONENOTE", "MSACCESS", "MSPUB"],
        [MaceTargets.Project] = ["WINPROJ"],
        [MaceTargets.Teams] = ["ms-teams", "Teams"]
    };

    public static MaceProcessEvidence Read(MaceTargets targets, Func<string, int>? count = null,
        CancellationToken token = default)
    {
        new MaceReviewOptions(targets).Validate();
        token.ThrowIfCancellationRequested();
        count ??= CountNative;
        try
        {
            var totals = new Dictionary<MaceTargets, int>();
            foreach (var pair in Names)
            {
                int total = 0;
                if (targets.HasFlag(pair.Key))
                {
                    foreach (string name in pair.Value)
                    {
                        token.ThrowIfCancellationRequested();
                        int observed = count(name);
                        if (observed is < 0 or > 128 || total > 128 - observed)
                            return Evidence(totals, false, "A selected process count exceeded the bounded Microsoft app removal inventory.");
                        total += observed;
                    }
                }
                totals[pair.Key] = total;
            }
            return Evidence(totals, true,
                "Only bounded counts for fixed executable names were retained. Process IDs, paths, command lines, users and window titles were excluded.");
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return new(0, 0, 0, 0, 0, false, "Selected-application process inventory failed: " + error.GetType().Name + ".");
        }
    }

    private static MaceProcessEvidence Evidence(Dictionary<MaceTargets, int> values, bool complete, string limitation)
    {
        int Value(MaceTargets target) => values.TryGetValue(target, out int value) ? value : 0;
        return new(Value(MaceTargets.OneDrive), Value(MaceTargets.Outlook), Value(MaceTargets.Office),
            Value(MaceTargets.Project), Value(MaceTargets.Teams), complete, limitation);
    }

    private static int CountNative(string name)
    {
        Process[] processes = Process.GetProcessesByName(name);
        try { return Math.Min(processes.Length, 129); }
        finally { foreach (Process process in processes) process.Dispose(); }
    }

}
