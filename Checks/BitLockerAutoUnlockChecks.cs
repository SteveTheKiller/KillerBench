using System.IO;
using KillerBench.Engine;

internal static class BitLockerAutoUnlockChecks
{
    public static async Task RunAsync(string scratch)
    {
        var off = State(false);
        var on = State(true);
        Require(BitLockerAutoUnlock.Preview("e:").Outcome == BitLockerAutoUnlockOutcome.Preview, "Auto-unlock preview inspects nothing");
        Require(BitLockerAutoUnlock.EligibilityBlocker("E:", off) is null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { VolumeStatus = "EncryptionInProgress" }) is null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", on) is not null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { VolumeType = "OperatingSystem" }) is not null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { LockStatus = "Locked" }) is not null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { VolumeStatus = "FullyDecrypted" }) is not null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { SystemVolumeStatus = "EncryptionInProgress" }) is not null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { SystemProtectionStatus = "Off" }) is not null &&
            BitLockerAutoUnlock.EligibilityBlocker("E:", off with { Complete = false }) is not null,
            "Auto-unlock needs an unlocked encrypting or encrypted data volume with it off and a protected fully encrypted operating-system volume");

        var enabled = new Host([off, off, on]);
        string enabledRoot = Path.Combine(scratch, "autounlock-enabled");
        Require((await BitLockerAutoUnlock.RunAsync("E:", true, enabledRoot, enabled)).Outcome == BitLockerAutoUnlockOutcome.Enabled && enabled.Requests == 1 &&
            !File.Exists(Path.Combine(enabledRoot, "bitlocker-autounlock.pending.json")), "A verified request is reported as enabled and closes the guard");
        var ignored = new Host([off, off, off]);
        Require((await BitLockerAutoUnlock.RunAsync("E:", true, Path.Combine(scratch, "autounlock-ignored"), ignored)).Outcome == BitLockerAutoUnlockOutcome.NotApplied,
            "A completed request that leaves auto-unlock off is not applied");
        var race = new Host([off, off with { SystemProtectionStatus = "Off" }]);
        Require((await BitLockerAutoUnlock.RunAsync("E:", true, Path.Combine(scratch, "autounlock-race"), race)).Outcome == BitLockerAutoUnlockOutcome.Blocked && race.Requests == 0,
            "Operating-system protection turning off before dispatch blocks the request");
        var standard = new Host([off]) { Administrator = false };
        Require((await BitLockerAutoUnlock.RunAsync("E:", true, Path.Combine(scratch, "autounlock-standard"), standard)).Outcome == BitLockerAutoUnlockOutcome.Blocked && standard.Requests == 0,
            "A standard user cannot request auto-unlock");

        string interruptedRoot = Path.Combine(scratch, "autounlock-interrupted");
        var interrupted = new Host([off, off]) { Throw = true };
        Require((await BitLockerAutoUnlock.RunAsync("E:", true, interruptedRoot, interrupted)).Outcome == BitLockerAutoUnlockOutcome.Uncertain &&
            File.Exists(Path.Combine(interruptedRoot, "bitlocker-autounlock.pending.json")), "An interrupted request keeps its pending guard");
        var second = new Host([off, off]);
        Require((await BitLockerAutoUnlock.RunAsync("E:", true, interruptedRoot, second)).Outcome == BitLockerAutoUnlockOutcome.Blocked && second.Requests == 0,
            "A pending guard blocks a second auto-unlock request");
        var unproven = new Host([off]);
        Require((await BitLockerAutoUnlock.ReconcileAsync(interruptedRoot, unproven)).Outcome == BitLockerAutoUnlockOutcome.Uncertain &&
            File.Exists(Path.Combine(interruptedRoot, "bitlocker-autounlock.pending.json")), "Reconciliation that cannot prove the request keeps the guard");
        var reconcile = new Host([on]);
        Require((await BitLockerAutoUnlock.ReconcileAsync(interruptedRoot, reconcile)).Outcome == BitLockerAutoUnlockOutcome.Enabled && reconcile.Requests == 0 &&
            !File.Exists(Path.Combine(interruptedRoot, "bitlocker-autounlock.pending.json")), "Reconciliation confirms the interrupted request without repeating it");

        const string json = "{\"volume\":\"E:\",\"identity\":\"id-e\",\"volumeType\":\"Data\",\"volumeStatus\":\"FullyEncrypted\",\"lockStatus\":\"Unlocked\",\"autoUnlock\":false,\"systemStatus\":\"FullyEncrypted\",\"systemProtection\":\"On\"}";
        var parsed = WindowsBitLockerAutoUnlockHost.Interpret(new ToolResult(0, json, "", false), "E:");
        Require(parsed.Complete && parsed.VolumeType == "Data" && !parsed.AutoUnlockEnabled, "Auto-unlock inspection parses exact evidence");
        Require(!WindowsBitLockerAutoUnlockHost.Interpret(new ToolResult(0, json.Replace("false", "\"no\""), "", false), "E:").Complete &&
            !WindowsBitLockerAutoUnlockHost.Interpret(new ToolResult(0, json.Replace("\"Data\"", "\"Removable\""), "", false), "E:").Complete &&
            !WindowsBitLockerAutoUnlockHost.Interpret(new ToolResult(0, json.Replace("}", ",\"extra\":1}"), "", false), "E:").Complete,
            "Malformed auto-unlock evidence stays incomplete");
        Console.WriteLine("PASS: BitLocker auto-unlock requires an encrypted data volume, a protected operating-system volume, administrator access, final eligibility and a durable no-repeat guard; substituted providers only.");
    }

    private static BitLockerAutoUnlockState State(bool autoUnlock) => new("E:", "id-e", "Data", "FullyEncrypted", "Unlocked", autoUnlock, "FullyEncrypted", "On", true);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host(IReadOnlyList<BitLockerAutoUnlockState> states) : IBitLockerAutoUnlockHost
    {
        private int index;
        public bool Administrator { get; init; } = true;
        public bool Throw { get; init; }
        public bool IsAdministrator => Administrator;
        public int Requests { get; private set; }
        public Task<BitLockerAutoUnlockState> InspectAsync(string volume, CancellationToken token) => Task.FromResult(states[Math.Min(index++, states.Count - 1)]);
        public Task<ToolResult> EnableAsync(string volume, CancellationToken token)
        {
            Requests++;
            if (Throw) throw new IOException("The provider connection was lost.");
            return Task.FromResult(new ToolResult(0, "", "", false));
        }
    }
}
