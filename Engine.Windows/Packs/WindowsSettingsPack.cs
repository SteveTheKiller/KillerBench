using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Packs;

/// <summary>Declarative recipes over registry, BCD and shell primitives. Each recipe states exactly what it observes, changes and proves.</summary>
public static class WindowsSettingsPack
{
    public const string PackId = "windows.settings";

    private const string Fips = @"SYSTEM\CurrentControlSet\Control\Lsa\FIPSAlgorithmPolicy";

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
            new ProbeSpec("process.running", new Dictionary<string, string> { ["name"] = "explorer", ["scope"] = "current-session" }, new ExpectSpec(
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
        },
        new RecipeDefinition("security.fips-policy-enable", 1, "Enable the FIPS algorithm policy", "Security",
            "FIPSAlgorithmPolicy Enabled reads 1 afterwards; applications that need non-FIPS algorithms are not tested",
            new ProbeSpec("registry.value", Registry("HKLM", Fips, "Enabled"), new ExpectSpec(
            [
                new ExpectClause("equals", "1", ProbeVerdict.Clear) { Detail = "The FIPS algorithm policy is enabled." },
                new ExpectClause("equals", "0", ProbeVerdict.Repairable) { Detail = "The FIPS algorithm policy is off. Since the April 2024 updates Windows runs its FIPS self-tests without it; it is only still needed when BitLocker PIN or password protectors must be FIPS-approved. Enabling it can break applications that use non-FIPS algorithms." },
                new ExpectClause("absent", "", ProbeVerdict.Repairable) { Detail = "The FIPS algorithm policy is not set, so it is off. It is only still needed when BitLocker PIN or password protectors must be FIPS-approved. Enabling it can break applications that use non-FIPS algorithms." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "FIPS algorithm policy" })
        {
            Symptoms = ["security.fips-required"],
            Preconditions =
            [
                new ProbeSpec("registry.value", Registry("HKLM", Fips, "MDMEnabled"), new ExpectSpec(
                [
                    new ExpectClause("absent", "", ProbeVerdict.Clear),
                    new ExpectClause("equals", "0", ProbeVerdict.Clear),
                    new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "Device management controls the FIPS policy. Change it there instead." }
                ])) { Name = "FIPS policy management" }
            ],
            Action = new ActionSpec("registry.set-dword", new Dictionary<string, string>(Registry("HKLM", Fips, "Enabled")) { ["value"] = "1" })
                { Timeout = TimeSpan.FromSeconds(10), Effects = "Sets FIPSAlgorithmPolicy Enabled to 1, the same value as the System cryptography FIPS security option. Create BitLocker PIN protectors after this so they use the FIPS-approved derivation. Some applications and older protocols may stop working." },
            Verify = new ProbeSpec("registry.value", Registry("HKLM", Fips, "Enabled"), ExpectSpec.ClearWhenEquals("1", ProbeVerdict.Attention)) { Name = "FIPS algorithm policy" },
            Rollback = new ActionSpec("registry.set-dword", new Dictionary<string, string>(Registry("HKLM", Fips, "Enabled")) { ["value"] = "0" })
                { Timeout = TimeSpan.FromSeconds(10), Effects = "Turns the FIPS algorithm policy back off." },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            MayRequireRestart = true,
            Limitations = "Only the registry value is verified. A domain security template can set it back, and applications only pick it up after they restart."
        },
        new RecipeDefinition("security.tpm-initialize", 1, "Prepare the TPM", "Security",
            "Get-Tpm reports the TPM ready afterwards, or after the restart Windows asks for; BitLocker and Windows Hello use are not tested",
            new ProbeSpec("tpm.status", new Dictionary<string, string>(), new ExpectSpec(
            [
                new ExpectClause("equals", "Ready", ProbeVerdict.Clear),
                new ExpectClause("equals", "NotReady", ProbeVerdict.Repairable) { Detail = "The TPM is present but not ready. Windows can usually provision it without clearing it; BitLocker with a TPM protector needs a ready TPM." },
                new ExpectClause("equals", "Absent", ProbeVerdict.Information) { Detail = "No TPM is visible to Windows. Turn it on in the firmware settings if the computer has one." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "TPM readiness" })
        {
            Symptoms = ["security.tpm-not-ready"],
            Action = new ActionSpec("tpm.initialize", new Dictionary<string, string>())
                { Timeout = TimeSpan.FromMinutes(2), Effects = "Runs Initialize-Tpm without clearing the TPM and without queuing a firmware prompt. A TPM that needs clearing or a physical-presence confirmation is reported and left alone." },
            Verify = new ProbeSpec("tpm.status", new Dictionary<string, string>(), ExpectSpec.ClearWhenEquals("Ready", ProbeVerdict.Attention)) { Name = "TPM readiness" },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            MayRequireRestart = true,
            Limitations = "KillerBench never clears a TPM. When Windows asks for a restart the TPM is only checked again after it."
        }
    ]);
}
