using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;

namespace KillerBench.Engine;

public enum OrcaOutlookMode { New, Classic, Both }
public enum OrcaScope { RunningAccount, AllUsers }

[Flags]
public enum OrcaActions
{
    None = 0,
    ClearCaches = 1,
    ResetProfileSettings = 2,
    ResetAuthentication = 4,
    DeleteOstFiles = 8,
    RemoveAddIns = 16,
    ReinstallNewOutlook = 32
}

public sealed record OrcaReviewOptions(OrcaOutlookMode Mode, OrcaScope Scope,
    OrcaActions Actions = OrcaActions.ClearCaches)
{
    public void Validate()
    {
        const OrcaActions all = OrcaActions.ClearCaches | OrcaActions.ResetProfileSettings |
            OrcaActions.ResetAuthentication | OrcaActions.DeleteOstFiles | OrcaActions.RemoveAddIns |
            OrcaActions.ReinstallNewOutlook;
        if (!Enum.IsDefined(Mode)) throw new ArgumentOutOfRangeException(nameof(Mode));
        if (!Enum.IsDefined(Scope)) throw new ArgumentOutOfRangeException(nameof(Scope));
        if (Actions == OrcaActions.None || (Actions & ~all) != 0)
            throw new ArgumentException("Select at least one supported Outlook review action.", nameof(Actions));
        if (Mode == OrcaOutlookMode.New && (Actions & (OrcaActions.DeleteOstFiles | OrcaActions.RemoveAddIns)) != 0)
            throw new ArgumentException("OST and add-in actions require Classic Outlook.", nameof(Actions));
        if (Mode == OrcaOutlookMode.Classic && Actions.HasFlag(OrcaActions.ReinstallNewOutlook))
            throw new ArgumentException("New Outlook reinstall requires New Outlook or Both.", nameof(Actions));
    }
}

public sealed record OrcaEvidence(string Area, string Scope, bool? Present, int ItemCount,
    string Detail, bool Complete);

public sealed record OrcaEvidenceSnapshot(IReadOnlyList<OrcaEvidence> Items, bool Complete,
    string Limitation);

public sealed record OrcaProcessEvidence(int NewOutlookProcesses, int ClassicOutlookProcesses,
    bool Complete, string Limitation)
{
    public int RelevantCount(OrcaOutlookMode mode) => mode switch
    {
        OrcaOutlookMode.New => NewOutlookProcesses,
        OrcaOutlookMode.Classic => ClassicOutlookProcesses,
        OrcaOutlookMode.Both => NewOutlookProcesses + ClassicOutlookProcesses,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}

public sealed record OrcaResetReadiness(bool ApplicationsClosed, bool BackupRequired,
    bool Complete, string Detail);

public sealed record OrcaReview(OrcaReviewOptions Options, OrcaEvidenceSnapshot Evidence,
    bool Applicable, bool Complete, bool ExecutionSupported, IReadOnlyList<WorkflowCheck> Checks)
{
    public OrcaResetReadiness? ResetReadiness { get; init; }

    public string Format()
    {
        string actions = string.Join(", ", Enum.GetValues<OrcaActions>()
            .Where(value => value != OrcaActions.None && Options.Actions.HasFlag(value)));
        return "OUTLOOK RESET REVIEW\nMode: " + Options.Mode + "\nScope: " + Options.Scope +
            "\nRequested actions: " + actions + "\nApplicability: " + (Applicable ? "Evidence found" : "Not established") +
            "\nCoverage: " + (Complete ? "Complete for collected sources" : "Incomplete") +
            (ResetReadiness is null ? "" : "\nApplications closed: " + (ResetReadiness.ApplicationsClosed ? "Yes" : "No or unverified") +
                "\nBackup required: " + (ResetReadiness.BackupRequired ? "Yes" : "No")) +
            "\nExecution available: " + (ExecutionSupported ? "Cache retention only" : "No") +
            "\n\nNo process, package, file, credential, registry value, add-in or user data changed.";
    }
}

public static class OrcaPlanning
{
    public static OrcaReview Review(OrcaReviewOptions options, OrcaEvidenceSnapshot evidence,
        OrcaProcessEvidence? processes = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(evidence);
        options.Validate();
        bool applicable = evidence.Items.Any(item => item.Present == true && Relevant(options.Mode, item.Area));
        bool complete = evidence.Complete && evidence.Items.All(item => item.Complete) && processes?.Complete != false;
        int activeProcesses = processes?.RelevantCount(options.Mode) ?? 0;
        bool backupRequired = (options.Actions & ~OrcaActions.ClearCaches) != 0;
        bool cacheOnly = options.Scope == OrcaScope.RunningAccount && options.Actions == OrcaActions.ClearCaches;
        bool executionSupported = cacheOnly && applicable && complete && processes is { Complete: true } && activeProcesses == 0;
        var readiness = processes is null ? null : new OrcaResetReadiness(
            processes.Complete && activeProcesses == 0, backupRequired, processes.Complete,
            !processes.Complete ? "Process evidence is incomplete. A future reset must remain blocked." :
            activeProcesses > 0 ? $"{activeProcesses} selected Outlook process{(activeProcesses == 1 ? " is" : "es are")} active. Close Outlook and collect fresh evidence before any reset." :
            backupRequired ? "Selected Outlook applications are closed. A verified backup and fresh data-ownership review are still required before any reset." :
            "Selected Outlook applications are closed. Cache-only retention is available through the guarded reset action." +
            (string.IsNullOrWhiteSpace(processes.Limitation) ? "" : " " + processes.Limitation));
        var checks = new List<WorkflowCheck>
        {
            new("Outlook reset review coverage", complete ? CheckState.Information : CheckState.Unknown,
                (complete ? "Requested evidence sources completed. " : "At least one requested evidence source was incomplete. Absence is not established. ") +
                evidence.Limitation),
            new("Outlook applicability", applicable ? CheckState.Information : complete ? CheckState.Attention : CheckState.Unknown,
                applicable ? "At least one selected Outlook edition has local registration or data evidence. This does not prove that a reset is needed."
                    : "No selected Outlook edition was established from the collected evidence. No repair should be planned from absence alone."),
            new("Requested Outlook reset actions", CheckState.Information,
                DescribeActions(options) + (cacheOnly
                    ? " Cache-only execution uses the separate guarded reset action and retains each selected cache under a unique recovery name."
                    : " These selections are a review request only. Execution remains unavailable."))
        };
        if (readiness is not null)
        {
            checks.Add(new("Outlook process readiness", !readiness.Complete ? CheckState.Unknown :
                readiness.ApplicationsClosed ? CheckState.Information : CheckState.Attention, readiness.Detail));
            checks.Add(new("Outlook reset backup prerequisite", backupRequired ? CheckState.Attention : CheckState.Information,
                backupRequired ? "The selected actions can change or remove Outlook state. A verified backup destination, exact retained items and restore procedure are required before execution can be implemented." :
                "The selected cache review does not require a separate backup plan because execution retains each exact cache under a unique recovery name."));
        }
        checks.AddRange(evidence.Items.Where(item => Relevant(options.Mode, item.Area)).Select(item =>
            new WorkflowCheck(item.Area, !item.Complete ? CheckState.Unknown : item.Present == true ? CheckState.Information : CheckState.Clear,
                item.Detail + " Scope: " + item.Scope + ". Items: " + item.ItemCount + ".")));
        return new(options, evidence, applicable, complete, executionSupported, checks.AsReadOnly()) { ResetReadiness = readiness };
    }

    public static OrcaReview ReviewCurrent(OrcaReviewOptions options, CancellationToken token = default)
    {
        options.Validate();
        OrcaEvidenceSnapshot evidence = OrcaInventory.Read(options, token);
        OrcaProcessEvidence processes = OrcaProcessInventory.Read(options.Mode, token: token);
        return Review(options, evidence, processes);
    }

    private static bool Relevant(OrcaOutlookMode mode, string area) => mode == OrcaOutlookMode.Both ||
        (mode == OrcaOutlookMode.New && area.StartsWith("New Outlook", StringComparison.Ordinal)) ||
        (mode == OrcaOutlookMode.Classic && area.StartsWith("Classic Outlook", StringComparison.Ordinal));

    private static string DescribeActions(OrcaReviewOptions options)
    {
        var values = Enum.GetValues<OrcaActions>().Where(value => value != OrcaActions.None && options.Actions.HasFlag(value));
        return "Selected: " + string.Join(", ", values) + ".";
    }
}

public static class OrcaProcessInventory
{
    public static OrcaProcessEvidence Read(OrcaOutlookMode mode, Func<string, int>? count = null,
        CancellationToken token = default)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        token.ThrowIfCancellationRequested();
        count ??= CountNative;
        try
        {
            int modern = mode is OrcaOutlookMode.New or OrcaOutlookMode.Both ? count("olk") : 0;
            token.ThrowIfCancellationRequested();
            int classic = mode is OrcaOutlookMode.Classic or OrcaOutlookMode.Both ? count("outlook") : 0;
            token.ThrowIfCancellationRequested();
            if (modern is < 0 or > 128 || classic is < 0 or > 128)
                return new(Math.Clamp(modern, 0, 128), Math.Clamp(classic, 0, 128), false,
                    "Process count exceeded the bounded Outlook reset inventory.");
            return new(modern, classic, true,
                "Only bounded process counts were retained. Process IDs, paths, command lines, users and window titles were excluded.");
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return new(0, 0, false, "Outlook process inventory failed: " + error.GetType().Name + ".");
        }
    }

    private static int CountNative(string name)
    {
        Process[] processes = Process.GetProcessesByName(name);
        try { return Math.Min(processes.Length, 129); }
        finally { foreach (Process process in processes) process.Dispose(); }
    }
}

public static class OrcaInventory
{
    public static OrcaEvidenceSnapshot Read(OrcaReviewOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Outlook reset inventory requires Windows.");
        var items = new List<OrcaEvidence>();
        bool complete = true;
        if (options.Scope == OrcaScope.AllUsers)
        {
            items.Add(new("New Outlook registration", "AllUsers", null, 0,
                "All-user package registration is not yet correlated with individual profile data", false));
            items.Add(new("Classic Outlook registration", "AllUsers", null, 0,
                "All-user profile hives are not loaded or opened by this review", false));
            return new(items.AsReadOnly(), false,
                "All-user Outlook reset review intentionally leaves per-user files, registry hives and credentials uninspected.");
        }

        if (options.Mode is OrcaOutlookMode.New or OrcaOutlookMode.Both)
        {
            var packages = AppPackageInventory.ReadAsync(AppPackageScope.CurrentAccount, token).GetAwaiter().GetResult();
            int registrations = packages.Packages.Count(package => package.Name == "Microsoft.OutlookForWindows");
            items.Add(new("New Outlook registration", "RunningAccount", registrations > 0, registrations,
                registrations > 0 ? "Exact Microsoft.OutlookForWindows package registration found" : "Exact package registration was not found",
                packages.Complete));
            complete &= packages.Complete;
            AddDirectory(items, "New Outlook cache", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Olk"));
            AddDirectory(items, "New Outlook package data", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Microsoft.OutlookForWindows_8wekyb3d8bbwe"));
        }
        if (options.Mode is OrcaOutlookMode.Classic or OrcaOutlookMode.Both)
        {
            AddClassicRegistration(items);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AddDirectory(items, "Classic Outlook cache", Path.Combine(local, "Microsoft", "Outlook", "RoamCache"));
            AddDirectory(items, "Classic Outlook address books", Path.Combine(local, "Microsoft", "Outlook", "Offline Address Books"));
            AddFiles(items, "Classic Outlook OST files", Path.Combine(local, "Microsoft", "Outlook"), "*.ost");
            AddRegistryCount(items, "Classic Outlook user add-ins", Registry.CurrentUser,
                @"Software\Microsoft\Office\Outlook\Addins");
            AddRegistryCount(items, "Classic Outlook machine add-ins", Registry.LocalMachine,
                @"Software\Microsoft\Office\Outlook\Addins");
            AddRegistryCount(items, "Classic Outlook profile settings", Registry.CurrentUser,
                @"Software\Microsoft\Office\16.0\Outlook\Profiles");
        }
        if (options.Actions.HasFlag(OrcaActions.ResetAuthentication))
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AddDirectory(items, options.Mode == OrcaOutlookMode.New ? "New Outlook authentication cache" : "Classic Outlook authentication cache",
                Path.Combine(local, "Microsoft", "IdentityCache"));
            items.Add(new(options.Mode == OrcaOutlookMode.New ? "New Outlook stored credentials" : "Classic Outlook stored credentials",
                "RunningAccount", null, 0, "Credential contents and target names were not enumerated", false));
            complete = false;
        }
        return new(items.AsReadOnly(), complete,
            "This review checks exact registrations and bounded known locations. It does not inspect mailbox contents, credential contents, policy ownership, add-in compatibility, sync health or repair necessity.");
    }

    private static void AddDirectory(List<OrcaEvidence> items, string area, string path)
    {
        try
        {
            bool present = Directory.Exists(path);
            items.Add(new(area, "RunningAccount", present, present ? 1 : 0,
                present ? "Known data directory exists" : "Known data directory was not found", true));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        { items.Add(new(area, "RunningAccount", null, 0, "Directory inspection failed: " + error.GetType().Name, false)); }
    }

    private static void AddFiles(List<OrcaEvidence> items, string area, string directory, string pattern)
    {
        try
        {
            int count = Directory.Exists(directory) ? Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Take(1001).Count() : 0;
            bool complete = count <= 1000;
            items.Add(new(area, "RunningAccount", count > 0, Math.Min(count, 1000),
                count > 0 ? "Local cache files were found" : "No matching local cache files were found", complete));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        { items.Add(new(area, "RunningAccount", null, 0, "File inspection failed: " + error.GetType().Name, false)); }
    }

    [SupportedOSPlatform("windows")]
    private static void AddClassicRegistration(List<OrcaEvidence> items)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE");
            bool found = key?.GetValue(null) is string value && !string.IsNullOrWhiteSpace(value);
            items.Add(new("Classic Outlook registration", "Machine", found, found ? 1 : 0,
                found ? "OUTLOOK.EXE application registration found" : "OUTLOOK.EXE application registration was not found", true));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        { items.Add(new("Classic Outlook registration", "Machine", null, 0, "Registry inspection failed: " + error.GetType().Name, false)); }
    }

    [SupportedOSPlatform("windows")]
    private static void AddRegistryCount(List<OrcaEvidence> items, string area, RegistryKey root, string path)
    {
        try
        {
            using var key = root.OpenSubKey(path);
            int count = key?.GetSubKeyNames().Take(1001).Count() ?? 0;
            bool complete = count <= 1000;
            items.Add(new(area, root == Registry.CurrentUser ? "RunningAccount" : "Machine", count > 0,
                Math.Min(count, 1000), count > 0 ? "Registry entries were found" : "No registry entries were found", complete));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        { items.Add(new(area, "Unknown", null, 0, "Registry inspection failed: " + error.GetType().Name, false)); }
    }
}
