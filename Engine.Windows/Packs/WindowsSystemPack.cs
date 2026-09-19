using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Identity, performance and hardware triage recipes. Most are diagnostic; the two actions are explicit options.</summary>
public static class WindowsSystemPack
{
    public const string PackId = "windows.system";

    private static readonly IReadOnlyDictionary<string, string> None = new Dictionary<string, string>();

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
        new RecipeDefinition("shell.explorer-restart", 1, "Restart the Windows shell", "Windows",
            "An explorer.exe process exists after the restart; open File Explorer windows are closed",
            new ProbeSpec("process.running", new Dictionary<string, string> { ["name"] = "explorer" }, new ExpectSpec(
            [
                new ExpectClause("equals", "Running", ProbeVerdict.Repairable) { Detail = "The shell is running; restarting it rebuilds the taskbar and desktop." },
                new ExpectClause("equals", "NotRunning", ProbeVerdict.Attention) { Detail = "No shell is running; use the shell start recipe instead." },
                new ExpectClause("any", "", ProbeVerdict.Unknown)
            ])) { Name = "Explorer process" })
        {
            Symptoms = ["shell.taskbar-unresponsive"],
            Action = new ActionSpec("explorer.restart", None) { Timeout = TimeSpan.FromSeconds(20), Effects = "Ends explorer.exe for the current session and starts it again. Open File Explorer windows close; unsaved work in other apps is unaffected." },
            Verify = new ProbeSpec("process.running", new Dictionary<string, string> { ["name"] = "explorer" }, ExpectSpec.ClearWhenEquals("Running", ProbeVerdict.Attention)),
            Risk = RecipeRisk.Reversible,
            RequiresExplicitOptions = true,
            Limitations = "A hang is not detected; the recipe assumes the person asked for the restart."
        }
    ]);
}
