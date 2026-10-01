using KillerBench.Engine;

internal static class MacePlanningChecks
{
    private static readonly string[] ProcessInventoryNames = ["olk", "OneDrive", "OUTLOOK", "WINPROJ"];

    internal static void Run()
    {
        var ok = new ToolResult(0, "{}", "", false);
        var machine = new InstalledApplicationInventory([
            new("Registry64", "OneDrive", "Microsoft OneDrive", "25.1", "Microsoft Corporation"),
            new("Registry64", "Project", "Microsoft Project Professional 2024", "1", "Microsoft Corporation"),
            new("Registry64", "Impostor", "Microsoft Teams", "1", "Unrelated Vendor")], true, ok);
        var account = new InstalledApplicationInventory([
            new("Registry64", "Office", "Microsoft 365 Apps for enterprise - en-us", "16", "Microsoft Corporation")], true, ok)
            { Scope = InstalledApplicationScope.CurrentAccount };
        var packages = new AppPackageInventoryResult(AppPackageScope.CurrentAccount, [
            new AppPackage("Microsoft.OutlookForWindows", "Microsoft.OutlookForWindows_1_x64__8wekyb3d8bbwe", "Microsoft.OutlookForWindows_8wekyb3d8bbwe", false, false) { DevelopmentMode = true },
            new("MSTeams", "MSTeams_1_x64__8wekyb3d8bbwe", "MSTeams_8wekyb3d8bbwe", false, true)], true, ok);
        var options = new MaceReviewOptions(MaceTargets.OneDrive | MaceTargets.Outlook | MaceTargets.Office |
            MaceTargets.Project | MaceTargets.Teams, RestoreOneDriveFolderLocations: true);
        var clickToRun = new OfficeClickToRunSnapshot([], true);
        var closed = new MaceProcessEvidence(0, 0, 0, 0, 0, true, "fixture");
        var folders = new MaceOneDriveFolderEvidence(1, [
            new("Desktop", false), new("Documents", false), new("Pictures", false)], true, "fixture");
        var policy = MaceManagedPolicyReview.Analyze(MaceScope.RunningAccount, options.Targets,
        [
            new(MaceTargets.OneDrive, "OneDrive policy", false, false),
            new(MaceTargets.Outlook, "Outlook policy", false, false),
            new(MaceTargets.Office | MaceTargets.Project, "Office and Project shared policy", false, false),
            new(MaceTargets.Project, "Project policy", false, false),
            new(MaceTargets.Teams, "Teams policy", false, false)
        ]);
        var review = MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: folders, managedPolicy: policy);
        Require(review.Complete && !review.ExecutionSupported && review.Candidates.Count == 5,
            "Selected exact Microsoft registrations produce a complete review without execution");
        Require(review.Candidates.All(item => item.Identity.Length > 0) &&
            review.Candidates.Single(item => item.Name == "MSTeams").Protected,
            "Review retains exact identities and package protection");
        Require(review.Candidates.All(item => item.Name != "Microsoft Teams"),
            "A matching product name from another publisher is excluded");
        Require(review.Format().Contains("Execution available: No") && review.Checks.All(item => item.Repair is null),
            "Review output never offers a repair");
        Require(review.ProcessEvidence == closed && review.Checks.Single(item => item.Name == "Microsoft app removal process readiness").State == CheckState.Information &&
            review.Format().Contains("Selected applications active: 0"),
            "Complete closed-process evidence is retained without exposing process identities");
        Require(!MacePlanning.Review(options, machine with { Complete = false }, account, packages,
            clickToRun: clickToRun, processes: closed, oneDriveFolders: folders).Complete,
            "Incomplete source evidence cannot establish complete coverage");
        var composed = MacePlanning.Review(options, machine, account, packages, clickToRun: new([
            new("Registry64", "O365ProPlusRetail", "16.0.1", "x64", "en-us"),
            new("Registry64", "ProjectPro2024Retail", "16.0.1", "x64", "en-us")], true),
            processes: closed, oneDriveFolders: folders, managedPolicy: policy);
        Require(composed.Candidates.Any(item => item.Target == MaceTargets.Office && item.Identity.EndsWith("/O365ProPlusRetail")) &&
            composed.Candidates.Any(item => item.Target == MaceTargets.Project && item.Identity.EndsWith("/ProjectPro2024Retail")),
            "Click-to-Run product identities distinguish suite and Project composition");
        Require(!MacePlanning.Review(options, machine, account, packages,
            clickToRun: new([], false, "Registry64 unavailable"), processes: closed, oneDriveFolders: folders).Complete,
            "Incomplete Click-to-Run evidence prevents complete review coverage");
        var active = MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed with { OneDriveProcesses = 1, OfficeProcesses = 2 }, oneDriveFolders: folders,
            managedPolicy: policy);
        Require(active.Complete && active.Checks.Single(item => item.Name == "Microsoft app removal process readiness") is
            { State: CheckState.Attention, Repair: null } && active.Format().Contains("Selected applications active: 3"),
            "Selected active applications block future removal readiness without exposing process details");
        Require(!MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed with { Complete = false }, oneDriveFolders: folders).Complete,
            "Incomplete process evidence prevents complete review coverage");

        var redirected = folders with
        {
            Folders = [new("Desktop", true), new("Documents", true), new("Pictures", false)]
        };
        var folderReview = MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: redirected, managedPolicy: policy);
        Require(folderReview.Checks.Single(item => item.Name == "OneDrive known-folder review") is
            { State: CheckState.Attention, Repair: null } && folderReview.Format().Contains("Known folders under OneDrive: 2") &&
            folderReview.Checks.Single(item => item.Name == "OneDrive known-folder review").Detail.Contains("Desktop, Documents") &&
            !folderReview.Checks.Single(item => item.Name == "OneDrive known-folder review").Detail.Contains("C:\\"),
            "Known-folder containment requests a restoration plan without retaining paths");
        Require(!MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: folders with { Complete = false }).Complete,
            "Incomplete OneDrive folder evidence prevents complete review coverage");

        var analyzed = MaceOneDriveFolderReview.Analyze(MaceScope.RunningAccount,
            [@"C:\Users\Fixture\OneDrive", @"C:\Users\Fixture\OneDrive - Work"],
            new Dictionary<string, string?>
            {
                ["Desktop"] = @"C:\Users\Fixture\OneDrive\Desktop",
                ["Documents"] = @"C:\Users\Fixture\Documents",
                ["Pictures"] = @"C:\Users\Fixture\OneDrive - Work\Pictures"
            });
        Require(analyzed is { Complete: true, RegisteredRootCount: 2, RedirectedCount: 2 } &&
            analyzed.Folders.Single(item => item.Folder == "Documents").UnderRegisteredOneDriveRoot == false &&
            analyzed.Limitation.Contains("Account names, paths and file contents were excluded"),
            "Exact path-boundary analysis retains only folder names and containment results");
        Require(!MaceOneDriveFolderReview.Analyze(MaceScope.RunningAccount,
            [@"C:\Users\Fixture\OneDrive"], new Dictionary<string, string?> { ["Desktop"] = "relative" }).Complete &&
            !MaceOneDriveFolderReview.Analyze(MaceScope.AllUsers, [], new Dictionary<string, string?>()).Complete,
            "Malformed and all-user folder evidence fails closed without loading profile hives");
        try
        {
            MaceOneDriveFolderReview.Analyze(MaceScope.RunningAccount, [], new Dictionary<string, string?>(),
                token: new CancellationToken(true));
            throw new Exception("Canceled OneDrive folder review completed");
        }
        catch (OperationCanceledException) { }

        var credentialOptions = options with { PreserveCachedCredentials = false };
        var credentials = MaceCredentialReview.Analyze(options.Targets,
            ["MicrosoftOffice", "OneDrive Cached", "Teams", "Unrelated"]);
        var credentialReview = MacePlanning.Review(credentialOptions, machine, account, packages,
            clickToRun: clickToRun, processes: closed, oneDriveFolders: folders, credentials: credentials,
            managedPolicy: policy);
        var credentialCheck = credentialReview.Checks.Single(item => item.Name == "Cached credential review");
        Require(credentialReview.Complete && credentialReview.CredentialEvidence == credentials &&
            credentialCheck is { State: CheckState.Attention, Repair: null } &&
            credentialReview.Format().Contains("Selected credential references: 3") &&
            !credentialCheck.Detail.Contains("MicrosoftOffice") && !credentialCheck.Detail.Contains("OneDrive Cached"),
            "Credential review retains only bounded product counts and blocks future removal without identities");
        Require(!MacePlanning.Review(credentialOptions, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: folders).Complete &&
            !MaceCredentialReview.Read(MaceTargets.OneDrive, MaceScope.AllUsers).Complete &&
            !MaceCredentialReview.Analyze(MaceTargets.Teams, Enumerable.Repeat("Teams", 129)).Complete,
            "Missing, all-user and oversized credential evidence fails closed");
        try
        {
            MaceCredentialReview.Analyze(MaceTargets.OneDrive, [], token: new CancellationToken(true));
            throw new Exception("Canceled credential review completed");
        }
        catch (OperationCanceledException) { }

        var dataOptions = options with { PreserveApplicationData = false };
        var data = MaceApplicationDataReview.Analyze(MaceScope.RunningAccount, options.Targets,
        [
            new(MaceTargets.OneDrive, "OneDrive settings", true, false),
            new(MaceTargets.Outlook, "Outlook local data", true, true),
            new(MaceTargets.Office | MaceTargets.Project, "Office and Project shared local data", true, false),
            new(MaceTargets.Teams, "Teams local data", false, false)
        ]);
        var dataReview = MacePlanning.Review(dataOptions, machine, account, packages,
            clickToRun: clickToRun, processes: closed, oneDriveFolders: folders, applicationData: data,
            managedPolicy: policy);
        var dataCheck = dataReview.Checks.Single(item => item.Name == "Application-data review");
        Require(dataReview.Complete && dataReview.ApplicationDataEvidence == data &&
            dataCheck is { State: CheckState.Attention, Repair: null } &&
            dataReview.Format().Contains("Selected application-data locations present: 3") &&
            dataCheck.Detail.Contains("OneDrive settings, Outlook local data, Office and Project shared local data") &&
            !dataCheck.Detail.Contains("C:\\"),
            "Application-data review retains fixed labels and blocks future removal without paths or contents");
        Require(!MacePlanning.Review(dataOptions, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: folders).Complete &&
            !MaceApplicationDataReview.Analyze(MaceScope.AllUsers, MaceTargets.OneDrive, []).Complete &&
            !MaceApplicationDataReview.Analyze(MaceScope.RunningAccount, MaceTargets.OneDrive,
                [new(MaceTargets.OneDrive, "Unknown path", true, false)]).Complete,
            "Missing, all-user and unknown application-data evidence fails closed");
        try
        {
            MaceApplicationDataReview.Analyze(MaceScope.RunningAccount, MaceTargets.OneDrive, [],
                token: new CancellationToken(true));
            throw new Exception("Canceled application-data review completed");
        }
        catch (OperationCanceledException) { }

        var configuredPolicy = policy with
        {
            Areas =
            [
                new(MaceTargets.OneDrive, "OneDrive policy", true, false),
                new(MaceTargets.Office | MaceTargets.Project, "Office and Project shared policy", false, true)
            ]
        };
        var policyReview = MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: folders, managedPolicy: configuredPolicy);
        var policyCheck = policyReview.Checks.Single(item => item.Name == "Managed configuration review");
        Require(policyReview.Complete && policyReview.ManagedPolicyEvidence == configuredPolicy &&
            policyCheck is { State: CheckState.Attention, Repair: null } &&
            policyReview.Format().Contains("Selected managed policy areas configured: 2") &&
            !policyCheck.Detail.Contains("Software\\Policies"),
            "Managed policy review retains fixed area presence without policy names or values");
        Require(!MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
            processes: closed, oneDriveFolders: folders).Complete &&
            !MaceManagedPolicyReview.Analyze(MaceScope.AllUsers, MaceTargets.OneDrive, []).Complete &&
            !MaceManagedPolicyReview.Analyze(MaceScope.RunningAccount, MaceTargets.OneDrive,
                [new(MaceTargets.OneDrive, "Unknown policy", true, false)]).Complete,
            "Missing, all-user and unknown policy evidence fails closed");
        try
        {
            MaceManagedPolicyReview.Analyze(MaceScope.RunningAccount, MaceTargets.OneDrive, [],
                token: new CancellationToken(true));
            throw new Exception("Canceled managed policy review completed");
        }
        catch (OperationCanceledException) { }

        var providerEvidence = review.RemovalProviderEvidence!;
        Require(providerEvidence is { Complete: true, Candidates.Count: 5, UnclassifiedCount: 4 } &&
            providerEvidence.Candidates.Single(item => item.Identity.StartsWith("Microsoft.OutlookForWindows_")) is
                { ProviderKind: MaceRemovalProviderKind.CurrentAccountAppPackage, ProviderIdentified: true } &&
            providerEvidence.Candidates.Single(item => item.Identity.StartsWith("MSTeams_")) is
                { ProviderKind: MaceRemovalProviderKind.None, ProviderIdentified: false } &&
            review.Checks.Single(item => item.Name == "Removal provider readiness") is
                { State: CheckState.Attention, Repair: null } &&
            review.Format().Contains("Candidates without an identified provider: 4"),
            "Exact provider boundaries identify only the supported current-account package path");
        Require(composed.RemovalProviderEvidence!.Candidates.Where(item =>
                item.RegistrationType == "Office Click-to-Run product").All(item =>
                item.ProviderKind == MaceRemovalProviderKind.OfficeClickToRunProduct && !item.ProviderIdentified) &&
            providerEvidence.Candidates.Where(item => item.RegistrationType == "Uninstall registration").All(item =>
                item.ProviderKind == MaceRemovalProviderKind.UninstallRegistration && !item.ProviderIdentified),
            "Click-to-Run and classic uninstall registrations remain explicitly unsupported");
        var installedPackageProvider = MaceRemovalProviderReview.Classify([
            new(MaceTargets.Outlook, "Registered app package", "CurrentAccount",
                "Microsoft.OutlookForWindows_fixture", "Microsoft.OutlookForWindows", "1", false)
        ]);
        Require(installedPackageProvider is { Complete: true, UnclassifiedCount: 1 } &&
            installedPackageProvider.Candidates[0] is
                { ProviderKind: MaceRemovalProviderKind.CurrentAccountAppPackage, ProviderIdentified: false } &&
            installedPackageProvider.Candidates[0].Reason.Contains("development package", StringComparison.Ordinal),
            "Ordinary installed app packages cannot claim Windows application-data preservation support");
        var provisionedProvider = MaceRemovalProviderReview.Classify([
            new(MaceTargets.Teams, "Provisioned app package", "NewUsers", "MSTeams_fixture", "MSTeams", "1", false)
        ]);
        Require(provisionedProvider is { Complete: true, UnclassifiedCount: 1 } &&
            provisionedProvider.Candidates[0].ProviderKind == MaceRemovalProviderKind.ProvisionedAppPackage &&
            provisionedProvider.Candidates[0].Identity == "MSTeams_fixture",
            "Provisioned-package classification preserves exact identity and blocks automatic execution");
        Require(!MaceRemovalProviderReview.Classify([.. Enumerable.Range(0, 513).Select(index =>
                new MaceCandidate(MaceTargets.Teams, "Registered app package", "CurrentAccount",
                    "MSTeams_" + index, "MSTeams", null, false))]).Complete &&
            !MacePlanning.Review(options, machine, account, packages, clickToRun: clickToRun,
                processes: closed, oneDriveFolders: folders, managedPolicy: policy,
                removalProviders: new([], false, "fixture")).Complete,
            "Oversized and incomplete removal-provider evidence fails closed");
        Require(typeof(MaceRemovalProviderCandidate).GetProperties().All(property =>
                !property.Name.Contains("Command", StringComparison.OrdinalIgnoreCase)),
            "Removal-provider evidence cannot retain or expose command lines");
        try
        {
            MaceRemovalProviderReview.Classify([], new CancellationToken(true));
            throw new Exception("Canceled removal-provider review completed");
        }
        catch (OperationCanceledException) { }

        var queried = new List<string>();
        var inventory = MaceProcessInventory.Read(MaceTargets.OneDrive | MaceTargets.Outlook | MaceTargets.Project,
            name => { queried.Add(name); return name.Equals("OUTLOOK", StringComparison.OrdinalIgnoreCase) ? 2 : 0; });
        Require(inventory is { Complete: true, OutlookProcesses: 2, OneDriveProcesses: 0, ProjectProcesses: 0 } &&
            queried.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).SequenceEqual(
                ProcessInventoryNames.OrderBy(value => value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase) && !queried.Contains("WINWORD", StringComparer.OrdinalIgnoreCase) &&
            inventory.Limitation.Contains("Process IDs, paths, command lines, users and window titles were excluded"),
            "Process inventory queries only fixed names for selected products and retains bounded counts");
        Require(!MaceProcessInventory.Read(MaceTargets.Teams, _ => 129).Complete &&
            !MaceProcessInventory.Read(MaceTargets.Teams, _ => throw new InvalidOperationException()).Complete,
            "Oversized and unavailable process inventories fail closed");
        try
        {
            MaceProcessInventory.Read(MaceTargets.OneDrive, _ => 0, new CancellationToken(true));
            throw new Exception("Canceled Microsoft app removal process inventory completed");
        }
        catch (OperationCanceledException) { }
        ExpectInvalid(new(MaceTargets.None));
        ExpectInvalid(new(MaceTargets.Teams, RestoreOneDriveFolderLocations: true));
        Console.WriteLine("PASS: Microsoft app removal target, scope, retention, exact-registration, process, known-folder, credential, application-data, managed-policy and removal-provider readiness; no removal available.");
    }

    private static void ExpectInvalid(MaceReviewOptions options)
    {
        try { options.Validate(); throw new Exception("Invalid Microsoft app removal options were accepted."); }
        catch (ArgumentException) { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
