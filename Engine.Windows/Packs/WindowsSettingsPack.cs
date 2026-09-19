using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Declarative recipes over registry, BCD and shell primitives. Each recipe states exactly what it observes, changes and proves.</summary>
public static class WindowsSettingsPack
{
    public const string PackId = "windows.settings";

    private static Dictionary<string, string> Registry(string hive, string key, string name) => new() { ["hive"] = hive, ["key"] = key, ["name"] = name };

    public static RecipePack Build() => new(RecipePack.CurrentSchemaVersion, PackId, 1, WindowsPrimitiveLibrary.Platform, "Windows settings and boot state",
    [
        new RecipeDefinition("power.fast-startup-disable", 1, "Disable Fast Startup", "Power",
            "HiberbootEnabled reads 0 after the change; Fast Startup takes effect at the next shutdown and boot",
            new ProbeSpec("registry.value", Registry("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled"), new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear) { Detail = "Fast Startup is disabled." },
                new ExpectClause("equals", "1", ProbeVerdict.Repairable) { Detail = "Fast Startup is enabled. Stale kernel sessions across shutdown can explain devices or network state that only a full restart fixes." },
                new ExpectClause("absent", "", ProbeVerdict.Information) { Detail = "HiberbootEnabled is not set; Windows uses its default, which enables Fast Startup when hibernation is available." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Fast Startup setting" })
        {
            Symptoms = ["boot.fast-startup-corruption"],
            Action = new ActionSpec("registry.set-dword", new Dictionary<string, string>(Registry("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled")) { ["value"] = "0" })
                { Timeout = TimeSpan.FromSeconds(10), Effects = "Sets HiberbootEnabled to 0. Shutdown becomes a full shutdown; hibernation and sleep settings are unchanged. The prior value is retained in the journal." },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "Boot behavior is verified only through the registry value, not through an observed shutdown.",
            Rollback = new ActionSpec("registry.set-dword", new Dictionary<string, string>(Registry("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled")) { ["value"] = "1" })
                { Timeout = TimeSpan.FromSeconds(10), Effects = "Restores HiberbootEnabled to 1." }
        },
        new RecipeDefinition("boot.safeboot-clear", 1, "Clear a stuck Safe Mode boot flag", "Windows",
            "The current boot entry has no safeboot value after the change; the next boot starts normally",
            new ProbeSpec("bcd.safeboot", new Dictionary<string, string>(), new ExpectSpec(
            [
                new ExpectClause("absent", "", ProbeVerdict.Clear) { Detail = "The current boot entry boots normally." },
                new ExpectClause("matches", "^(Minimal|Network|DsRepair)$", ProbeVerdict.Repairable) { Detail = "The current boot entry is set to boot into Safe Mode every time." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Safe Mode boot flag" })
        {
            Symptoms = ["boot.safe-mode-stuck"],
            Action = new ActionSpec("bcd.safeboot-clear", new Dictionary<string, string>()) { Timeout = TimeSpan.FromSeconds(15), Effects = "Runs bcdedit /deletevalue {current} safeboot. Other boot settings are unchanged." },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            MayRequireRestart = true,
            Limitations = "The next boot is not observed; a msconfig Safe boot setting will re-add the flag if it is still selected there."
        },
        new RecipeDefinition("profile.temporary-detect", 1, "Detect a temporary-profile registry backup entry", "Applications",
            "Presence of a .bak entry under ProfileList is reported; no change is made",
            new ProbeSpec("registry.subkeys", new Dictionary<string, string> { ["hive"] = "HKLM", ["key"] = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList" }, new ExpectSpec(
            [
                new ExpectClause("contains", ".bak", ProbeVerdict.Attention) { Detail = "A ProfileList entry ends in .bak. Windows created a temporary profile for that account; the original entry was renamed. Renaming it back while the account is signed out restores the original profile." },
                new ExpectClause("present", "", ProbeVerdict.Clear) { Detail = "No ProfileList backup entry exists." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "ProfileList backup entries" })
        {
            Symptoms = ["profile.temporary"],
            Limitations = "Only the registry symptom is checked; folder permissions and NTUSER.DAT damage are not inspected. The rename is a manual step."
        },
        new RecipeDefinition("shell.explorer-start", 1, "Start the Windows shell when it is not running", "Windows",
            "An explorer.exe process exists after the change; taskbar and desktop rendering are not verified",
            new ProbeSpec("process.running", new Dictionary<string, string> { ["name"] = "explorer" }, new ExpectSpec(
            [
                new ExpectClause("equals", "Running", ProbeVerdict.Clear) { Detail = "The Windows shell is running." },
                new ExpectClause("equals", "NotRunning", ProbeVerdict.Repairable) { Detail = "No explorer.exe process is running; the desktop and taskbar are absent." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Explorer process" })
        {
            Symptoms = ["display.black-screen-after-login"],
            Action = new ActionSpec("explorer.start", new Dictionary<string, string>()) { Timeout = TimeSpan.FromSeconds(15), Effects = "Starts explorer.exe for the current session. No process is terminated." },
            Risk = RecipeRisk.Reversible,
            Limitations = "A shell that is running but hung is not detected; only absence is."
        }
    ]);
}
