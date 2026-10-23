using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Packs;

/// <summary>Identity, performance and hardware triage recipes. Most are diagnostic; every action is an explicit option.</summary>
public static class WindowsSystemPack
{
    public const string PackId = "windows.system";

    private static readonly IReadOnlyDictionary<string, string> None = new Dictionary<string, string>();
    private static readonly IReadOnlyDictionary<string, string> FoxitUpdater = new Dictionary<string, string> { ["hive"] = "HKCU", ["key"] = @"Software\Foxit Software\${product}\plugins\Updater", ["name"] = "UpdateMode" };

    public static RecipePack Build() => new(RecipePack.CurrentSchemaVersion, PackId, 1, WindowsPrimitiveLibrary.Platform, "Windows identity, performance and hardware triage",
    [
        new RecipeDefinition("identity.secure-channel", 1, "Domain secure channel", "Security",
            "Test-ComputerSecureChannel result only; the repair needs domain credentials and is a separate explicit step",
            new ProbeSpec("domain.secure-channel", None, new ExpectSpec(
            [
                new ExpectClause("equals", "True", ProbeVerdict.Clear),
                new ExpectClause("equals", "NotDomainJoined", ProbeVerdict.Information),
                new ExpectClause("equals", "False", ProbeVerdict.Attention) { Detail = "The secure channel to the domain is broken. Reset it with Test-ComputerSecureChannel -Repair -Credential using a domain account, or rejoin the domain." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Secure channel" })
        {
            Symptoms = ["signin.domain-trust-broken"],
            Limitations = "Cached-credential sign-in can still succeed with a broken secure channel; a passing test does not prove Group Policy or Kerberos health."
        },
        new RecipeDefinition("identity.entra-token", 1, "Microsoft Entra join and token state", "Security",
            "dsregcmd join and primary refresh token flags only",
            new ProbeSpec("entra.status", None, new ExpectSpec(
            [
                new ExpectClause("equals", "YES/YES", ProbeVerdict.Clear),
                new ExpectClause("equals", "YES/NO", ProbeVerdict.Attention) { Detail = "The device is joined to Microsoft Entra but the current user has no primary refresh token. Lock and unlock with the work account, check network access to login.microsoftonline.com, then review dsregcmd /status for the error." },
                new ExpectClause("matches", "^NO/", ProbeVerdict.Information) { Detail = "The device is not joined to Microsoft Entra." },
                new ExpectClause("matches", "^YES/NONE$", ProbeVerdict.Information) { Detail = "The device is joined to Microsoft Entra; run this as the signed-in user to see the token state." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Entra state" })
        {
            Symptoms = ["signin.entra-join-broken"],
            Limitations = "Registration health beyond the join and token flags is not evaluated."
        },
        new RecipeDefinition("identity.group-policy-age", 1, "Computer Group Policy freshness", "Security",
            "Hours since the last recorded computer policy application; content of the policy is not evaluated",
            new ProbeSpec("gpresult.hours-since-applied", None, new ExpectSpec(
            [
                new ExpectClause("equals", "NotDomainJoined", ProbeVerdict.Information),
                new ExpectClause("equals", "NeverApplied", ProbeVerdict.Attention) { Detail = "Computer Group Policy has never been recorded as applied. Check domain controller reachability and SYSVOL access, then run gpupdate /force." },
                new ExpectClause("less-than", "24", ProbeVerdict.Clear),
                new ExpectClause("greater-than", "23.99", ProbeVerdict.Attention) { Detail = "Computer Group Policy has not applied in over a day. Check domain controller reachability, DNS and the Group Policy Client service, then run gpupdate /force." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Group Policy age" })
        {
            Symptoms = ["signin.group-policy-not-applying"],
            RequiresElevation = true,
            Limitations = "User policy and policy content are not inspected."
        },
        new RecipeDefinition("performance.high-cpu", 1, "Sustained high CPU by one process", "Performance",
            "A three-second sample only; the busiest process is named, not judged",
            new ProbeSpec("process.top-cpu", new Dictionary<string, string> { ["seconds"] = "3" }, new ExpectSpec(
            [
                new ExpectClause("greater-than", "60", ProbeVerdict.Attention) { Detail = "One process is consuming most of the CPU. Identify it from the sample before ending it; antivirus scans and indexing are expected to finish on their own." },
                new ExpectClause("any", "", ProbeVerdict.Clear)
            ])) { Name = "CPU sample" })
        {
            Symptoms = ["perf.high-cpu"],
            Limitations = "A short sample can miss intermittent spikes and cannot tell a legitimate workload from a runaway one."
        },
        new RecipeDefinition("hardware.memory-diagnostic", 1, "Schedule Windows Memory Diagnostic", "Hardware",
            "The BCD boot sequence names {memdiag} after the change; the test itself runs at the next restart",
            new ProbeSpec("bcd.bootsequence", None, new ExpectSpec(
            [
                new ExpectClause("contains", "{memdiag}", ProbeVerdict.Clear) { Detail = "Windows Memory Diagnostic is already scheduled for the next restart." },
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "Windows Memory Diagnostic is not scheduled." }
            ])) { Name = "Boot sequence" })
        {
            Symptoms = ["hardware.memory-suspect"],
            Action = new ActionSpec("memory-diagnostic.schedule", None) { Timeout = TimeSpan.FromSeconds(15), Effects = "Sets a one-time boot sequence to {memdiag}. The next restart runs the memory test before Windows starts; results appear in the System event log after the following boot." },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            MayRequireRestart = true,
            Limitations = "The test result is not read back by this recipe."
        },
        new RecipeDefinition("performance.high-memory", 1, "One process holds most of the memory", "Performance",
            "Largest working set as a share of physical memory; the process is named, not judged",
            new ProbeSpec("process.top-memory", None, new ExpectSpec(
            [
                new ExpectClause("greater-than", "40", ProbeVerdict.Attention) { Detail = "One process holds more than 40% of physical memory. Browsers and databases can be legitimate; a process that keeps growing is a leak and needs a restart or its vendor." },
                new ExpectClause("any", "", ProbeVerdict.Clear)
            ])) { Name = "Memory sample" })
        {
            Symptoms = ["perf.high-memory"],
            Limitations = "Working set is a snapshot; committed memory, paging and total pressure are reported by the Vitals workflow."
        },
        new RecipeDefinition("performance.disk-busy", 1, "Physical disks are saturated", "Performance",
            "Average physical disk busy time over three one-second samples",
            new ProbeSpec("disk.busy-percent", new Dictionary<string, string> { ["samples"] = "3" }, new ExpectSpec(
            [
                new ExpectClause("greater-than", "90", ProbeVerdict.Attention) { Detail = "Physical disks are busy more than 90% of the time. Check the Storage workflow for disk health, then Windows Search indexing, antivirus scans and Windows Update activity." },
                new ExpectClause("any", "", ProbeVerdict.Clear)
            ])) { Name = "Disk sample" })
        {
            Symptoms = ["perf.disk-100-percent"],
            Limitations = "A short sample can miss bursts and does not identify the process responsible."
        },
        new RecipeDefinition("backup.vss-writers", 1, "VSS writers in a failed state", "Backup",
            "vssadmin writer states only; backup success is not tested",
            new ProbeSpec("vss.unstable-writers", None, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear),
                new ExpectClause("equals", "InProgress", ProbeVerdict.Information) { Detail = "VSS writers are participating in backup activity. Allow the backup to finish and check again; do not interrupt its services." },
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "One or more VSS writers report a documented failure. Review backup status and writer event logs before recovery; wait for active backups to finish before changing services." }
            ])) { Name = "VSS writers" })
        {
            Symptoms = ["backup.vss-writer-failed"],
            RequiresElevation = true,
            Limitations = "The owning service is not restarted automatically because several writers share services that host other features."
        },
        new RecipeDefinition("backup.shadow-storage", 1, "Shadow storage near its limit", "Backup",
            "Used versus maximum shadow storage per association",
            new ProbeSpec("vss.shadowstorage-percent", None, new ExpectSpec(
            [
                new ExpectClause("less-than", "90", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "Shadow storage is at or above 90% of its maximum, so new snapshots will delete the oldest ones or fail. Raise the limit with vssadmin resize shadowstorage after confirming free space." }
            ])) { Name = "Shadow storage" })
        {
            Symptoms = ["backup.shadow-storage-full"],
            RequiresElevation = true,
            Limitations = "Resizing is not automated; it depends on free space the person must confirm."
        },
        new RecipeDefinition("apps.pdf-association", 1, "PDF file association", "Applications",
            "The current account's chosen PDF handler is reported",
            new ProbeSpec("registry.value", new Dictionary<string, string> { ["hive"] = "HKCU", ["key"] = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.pdf\UserChoice", ["name"] = "ProgId" }, new ExpectSpec(
            [
                new ExpectClause("present", "", ProbeVerdict.Information),
                new ExpectClause("absent", "", ProbeVerdict.Information) { Detail = "No PDF handler has been chosen for this account, so the Windows default applies. Choose one in Settings, Apps, Default apps; Windows protects the choice from programs that try to set it silently." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "PDF handler" })
        {
            Symptoms = ["apps.file-association-wrong"],
            Limitations = "Windows signs the user choice; changing it programmatically is not supported, so the recipe only reports it."
        },
        new RecipeDefinition("apps.foxit-auto-update-off", 1, "Turn off Foxit automatic updates", "Applications",
            "The running account's Foxit UpdateMode reads back as string 0; machine policy, other accounts and installed version are untested",
            new ProbeSpec("registry.value", FoxitUpdater, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear) { Detail = "Foxit will not download or install updates automatically for this account." },
                new ExpectClause("equals", "1", ProbeVerdict.Repairable) { Detail = "Foxit downloads updates automatically and prompts to install them. Turning this off also stops security updates until someone updates Foxit by hand." },
                new ExpectClause("equals", "2", ProbeVerdict.Repairable) { Detail = "Foxit downloads and installs updates automatically. Turning this off also stops security updates until someone updates Foxit by hand." },
                new ExpectClause("absent", "", ProbeVerdict.Repairable) { Detail = "No UpdateMode is stored for this account, so Foxit's default automatic updates apply. The Updater key must already exist; KillerBench does not create it." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Foxit UpdateMode" })
        {
            Symptoms = ["apps.foxit-auto-updates"],
            Options = [new RecipeOption("product", "Exact Foxit product key name, such as Foxit PDF Editor 12.0") { Pattern = @"Foxit (PDF Editor|PDF Reader|PhantomPDF|Reader)( [0-9]{1,2}\.[0-9])?" }],
            Preconditions =
            [
                new ProbeSpec("registry.value", new Dictionary<string, string> { ["hive"] = "HKLM", ["key"] = @"SOFTWARE\Policies\Foxit\PDFEditor\Plugins", ["name"] = "Updater" }, new ExpectSpec(
                [
                    new ExpectClause("absent", "", ProbeVerdict.Clear),
                    new ExpectClause("present", "", ProbeVerdict.Attention) { Detail = "Machine policy already manages the Foxit PDF Editor updater. Change it through that policy instead." },
                    new ExpectClause("any", "", ProbeVerdict.Unknown)
                ])) { Name = "Foxit updater policy" }
            ],
            Action = new ActionSpec("registry.set-string", new Dictionary<string, string>(FoxitUpdater) { ["value"] = "0" })
                { Timeout = TimeSpan.FromSeconds(15), Effects = "Sets UpdateMode to string 0 under the running account's Foxit Updater key, the documented per-user setting for no automatic download or install. Other accounts, services and tasks are left alone." },
            Verify = new ProbeSpec("registry.value", FoxitUpdater, ExpectSpec.ClearWhenEquals("0", ProbeVerdict.Attention)) { Name = "Foxit UpdateMode" },
            Risk = RecipeRisk.Persistent,
            RequiresExplicitOptions = true,
            Limitations = "Only the running account changes. The prior value is journaled; there is no automatic rollback. Only the documented PDF Editor policy location is checked, and the updater service and scheduled tasks are not changed."
        },
        new RecipeDefinition("apps.http-association", 1, "Default browser association", "Applications",
            "The current account's chosen http handler is reported",
            new ProbeSpec("registry.value", new Dictionary<string, string> { ["hive"] = "HKCU", ["key"] = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice", ["name"] = "ProgId" }, new ExpectSpec(
            [
                new ExpectClause("present", "", ProbeVerdict.Information),
                new ExpectClause("absent", "", ProbeVerdict.Information) { Detail = "No default browser has been chosen for this account, so the Windows default applies. Choose one in Settings, Apps, Default apps." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "http handler" })
        {
            Symptoms = ["apps.default-apps-reset"],
            Limitations = "Windows signs the user choice; changing it programmatically is not supported, so the recipe only reports it."
        },
        new RecipeDefinition("shell.explorer-restart", 1, "Restart the Windows shell", "Windows",
            "An explorer.exe process exists after the restart; open File Explorer windows are closed",
            new ProbeSpec("process.running", new Dictionary<string, string> { ["name"] = "explorer", ["scope"] = "current-session" }, new ExpectSpec(
            [
                new ExpectClause("equals", "Running", ProbeVerdict.Repairable) { Detail = "The shell is running; restarting it rebuilds the taskbar and desktop." },
                new ExpectClause("equals", "NotRunning", ProbeVerdict.Attention) { Detail = "No shell is running; use the shell start recipe instead." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Explorer process" })
        {
            Symptoms = ["shell.taskbar-unresponsive"],
            Action = new ActionSpec("explorer.restart", None) { Timeout = TimeSpan.FromSeconds(20), Effects = "Ends explorer.exe for the current session and starts it again. Open File Explorer windows close; unsaved work in other apps is unaffected." },
            Verify = new ProbeSpec("process.running", new Dictionary<string, string> { ["name"] = "explorer", ["scope"] = "current-session" }, ExpectSpec.ClearWhenEquals("Running", ProbeVerdict.Attention)),
            Risk = RecipeRisk.Reversible,
            RequiresExplicitOptions = true,
            Limitations = "A hang is not detected; the recipe assumes the person asked for the restart."
        }
    ]);
}
