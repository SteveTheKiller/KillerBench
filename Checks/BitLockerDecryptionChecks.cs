using System.IO;
using KillerBench.Engine;

internal static class BitLockerDecryptionChecks
{
    public static async Task RunAsync(string scratch)
    {
        var encrypted = State("FullyEncrypted", autoUnlock: 0);
        Require(BitLockerDecryption.Preview("c:").Outcome == BitLockerDecryptionOutcome.Preview, "Decryption preview inspects nothing");
        Require(BitLockerDecryption.EligibilityBlocker("C:", encrypted with { AutoUnlockVolumes = 2 }) is not null &&
            BitLockerDecryption.EligibilityBlocker("C:", encrypted with { AutoUnlockVolumes = 2, OperatingSystemVolume = false }) is null &&
            BitLockerDecryption.EligibilityBlocker("C:", encrypted with { LockStatus = "Locked" }) is not null &&
            BitLockerDecryption.EligibilityBlocker("C:", State("EncryptionInProgress", 0)) is not null &&
            BitLockerDecryption.EligibilityBlocker("C:", State("FullyDecrypted", 0)) is not null &&
            BitLockerDecryption.EligibilityBlocker("C:", encrypted) is null,
            "Decryption needs a fully encrypted unlocked volume and no auto-unlock drives tied to an operating-system volume");

        var unconfirmed = new Host([encrypted]);
        Require((await BitLockerDecryption.RunAsync("C:", true, false, Path.Combine(scratch, "decrypt-unconfirmed"), unconfirmed)).Outcome == BitLockerDecryptionOutcome.Blocked &&
            unconfirmed.Requests == 0, "Decryption without the separate unprotected confirmation is refused");
        var started = new Host([encrypted, encrypted, State("DecryptionInProgress", 0)]);
        string startedRoot = Path.Combine(scratch, "decrypt-started");
        Require((await BitLockerDecryption.RunAsync("C:", true, true, startedRoot, started)).Outcome == BitLockerDecryptionOutcome.Started && started.Requests == 1 &&
            !File.Exists(Path.Combine(startedRoot, "bitlocker-decrypt.pending.json")), "A confirmed request that begins decryption is reported as started and closes the guard");
        var ignored = new Host([encrypted, encrypted, encrypted]);
        Require((await BitLockerDecryption.RunAsync("C:", true, true, Path.Combine(scratch, "decrypt-ignored"), ignored)).Outcome == BitLockerDecryptionOutcome.NotApplied,
            "A completed request that leaves the volume encrypted is not applied");
        var race = new Host([encrypted, encrypted with { AutoUnlockVolumes = 1 }]);
        Require((await BitLockerDecryption.RunAsync("C:", true, true, Path.Combine(scratch, "decrypt-race"), race)).Outcome == BitLockerDecryptionOutcome.Blocked && race.Requests == 0,
            "An auto-unlock drive added before dispatch blocks the request");
        var restart = new Host([encrypted]) { RestartPending = true };
        Require((await BitLockerDecryption.RunAsync("C:", true, true, Path.Combine(scratch, "decrypt-restart"), restart)).Outcome == BitLockerDecryptionOutcome.Blocked && restart.Requests == 0,
            "Pending restart blocks decryption");

        string interruptedRoot = Path.Combine(scratch, "decrypt-interrupted");
        var interrupted = new Host([encrypted, encrypted]) { Throw = true };
        Require((await BitLockerDecryption.RunAsync("C:", true, true, interruptedRoot, interrupted)).Outcome == BitLockerDecryptionOutcome.Uncertain &&
            File.Exists(Path.Combine(interruptedRoot, "bitlocker-decrypt.pending.json")), "An interrupted request keeps its pending guard");
        var second = new Host([encrypted, encrypted]);
        Require((await BitLockerDecryption.RunAsync("C:", true, true, interruptedRoot, second)).Outcome == BitLockerDecryptionOutcome.Blocked && second.Requests == 0,
            "A pending guard blocks a second decryption request");
        var reconcile = new Host([State("DecryptionInProgress", 0)]);
        Require((await BitLockerDecryption.ReconcileAsync(interruptedRoot, reconcile)).Outcome == BitLockerDecryptionOutcome.Started && reconcile.Requests == 0 &&
            !File.Exists(Path.Combine(interruptedRoot, "bitlocker-decrypt.pending.json")), "Reconciliation confirms the interrupted request without repeating it");

        const string json = "{\"volume\":\"C:\",\"identity\":\"id-1\",\"volumeStatus\":\"FullyEncrypted\",\"lockStatus\":\"Unlocked\",\"encryptionMethod\":\"XtsAes128\",\"operatingSystem\":true,\"autoUnlock\":0}";
        var parsed = WindowsBitLockerDecryptionHost.Interpret(new ToolResult(0, json, "", false), "C:");
        Require(parsed.Complete && parsed.EncryptionMethod == "XtsAes128" && parsed.OperatingSystemVolume, "Decryption inspection parses exact evidence");
        Require(!WindowsBitLockerDecryptionHost.Interpret(new ToolResult(0, json.Replace("\"autoUnlock\":0", "\"autoUnlock\":-1"), "", false), "C:").Complete &&
            !WindowsBitLockerDecryptionHost.Interpret(new ToolResult(0, json.Replace("true", "\"yes\""), "", false), "C:").Complete,
            "Malformed decryption evidence stays incomplete");
        Console.WriteLine("PASS: BitLocker decryption requires a separate unprotected confirmation, a fully encrypted unlocked volume, no dependent auto-unlock drives, final eligibility and a durable no-repeat guard; substituted providers only.");
    }

    private static BitLockerDecryptionState State(string status, int autoUnlock) => new("C:", "id-1", status, "Unlocked", "XtsAes128", true, autoUnlock, true);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host(IReadOnlyList<BitLockerDecryptionState> states) : IBitLockerDecryptionHost
    {
        private int index;
        public bool RestartPending { get; init; }
        public bool Throw { get; init; }
        public bool IsAdministrator => true;
        public int Requests { get; private set; }
        public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token) =>
            Task.FromResult(new RestartSnapshot(true, RestartPending, false, false, false, false, "TESTPC", "TESTPC"));
        public Task<BitLockerDecryptionState> InspectAsync(string volume, CancellationToken token) => Task.FromResult(states[Math.Min(index++, states.Count - 1)]);
        public Task<ToolResult> DecryptAsync(string volume, CancellationToken token)
        {
            Requests++;
            if (Throw) throw new IOException("The provider connection was lost.");
            return Task.FromResult(new ToolResult(0, "", "", false));
        }
    }
}
