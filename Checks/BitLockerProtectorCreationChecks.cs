using System.IO;
using KillerBench.Engine;

internal static class BitLockerProtectorCreationChecks
{
    private static readonly Guid Recovery = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid Tpm = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");

    public static async Task RunAsync(string scratch)
    {
        var bare = State([]);
        var withRecovery = State([new(Recovery, "RecoveryPassword")]);
        var both = State([new(Recovery, "RecoveryPassword"), new(Tpm, "Tpm")]);

        Require(BitLockerProtectorCreation.Preview("c:", BitLockerNewProtector.RecoveryPassword).Outcome == BitLockerProtectorCreationOutcome.Preview,
            "Protector preview inspects nothing");
        Require(BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.Tpm, bare with { OperatingSystemVolume = false }) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.Tpm, bare with { TpmReady = false }) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.Tpm, both) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.Tpm, State([new(Tpm, "TpmPin")])) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.RecoveryPassword, withRecovery) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.RecoveryPassword, bare with { LockStatus = "Locked" }) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.RecoveryPassword, bare with { VolumeStatus = "DecryptionInProgress" }) is not null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.Tpm, withRecovery) is null &&
            BitLockerProtectorCreation.EligibilityBlocker("C:", BitLockerNewProtector.RecoveryPassword, bare) is null,
            "Protector eligibility requires the OS volume and a ready TPM for TPM, one TPM-based protector at most, one recovery password at most, and an unlocked volume");

        string root = Path.Combine(scratch, "bitlocker-protector");
        var added = State([new(Recovery, "RecoveryPassword"), new(Tpm, "Tpm")]);
        var host = new Host([withRecovery, withRecovery, added]);
        var verified = await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.Tpm, true, root, host);
        Require(verified.Outcome == BitLockerProtectorCreationOutcome.Verified && verified.Created == Tpm && host.Adds == 1 &&
            !File.Exists(Path.Combine(root, "bitlocker-protector.pending.json")), "A single new TPM protector beside the prior set is verified and the guard closes");

        var noChange = new Host([bare, bare, bare]);
        Require((await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.RecoveryPassword, true, Path.Combine(scratch, "bitlocker-protector-none"), noChange)).Outcome == BitLockerProtectorCreationOutcome.NotApplied,
            "A completed request with no new protector is not applied and not repeated");
        var lost = new Host([withRecovery, withRecovery, State([new(Tpm, "Tpm")])]);
        Require((await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.Tpm, true, Path.Combine(scratch, "bitlocker-protector-lost"), lost)).Outcome == BitLockerProtectorCreationOutcome.Uncertain,
            "A lost prior protector makes the result uncertain");
        var race = new Host([bare, withRecovery]);
        string raceRoot = Path.Combine(scratch, "bitlocker-protector-race");
        Require((await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.RecoveryPassword, true, raceRoot, race)).Outcome == BitLockerProtectorCreationOutcome.Blocked &&
            race.Adds == 0 && !File.Exists(Path.Combine(raceRoot, "bitlocker-protector.pending.json")), "A protector added by something else before dispatch blocks the request and closes the guard");
        var restart = new Host([bare, bare]) { RestartPending = true };
        Require((await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.RecoveryPassword, true, Path.Combine(scratch, "bitlocker-protector-restart"), restart)).Outcome == BitLockerProtectorCreationOutcome.Blocked &&
            restart.Adds == 0, "Pending restart blocks protector creation");
        var standard = new Host([bare]) { Administrator = false };
        Require((await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.RecoveryPassword, true, Path.Combine(scratch, "bitlocker-protector-user"), standard)).Outcome == BitLockerProtectorCreationOutcome.Blocked &&
            standard.Adds == 0, "Protector creation requires administrator access");

        string interruptedRoot = Path.Combine(scratch, "bitlocker-protector-interrupted");
        var interrupted = new Host([bare, bare]) { Throw = true };
        var stopped = await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.RecoveryPassword, true, interruptedRoot, interrupted);
        Require(stopped.Outcome == BitLockerProtectorCreationOutcome.Uncertain && File.Exists(Path.Combine(interruptedRoot, "bitlocker-protector.pending.json")),
            "An interrupted request keeps its pending guard");
        var second = new Host([bare, bare]);
        Require((await BitLockerProtectorCreation.RunAsync("C:", BitLockerNewProtector.RecoveryPassword, true, interruptedRoot, second)).Outcome == BitLockerProtectorCreationOutcome.Blocked && second.Adds == 0,
            "A pending guard blocks a second protector request");
        var reconcile = new Host([withRecovery]);
        var reconciled = await BitLockerProtectorCreation.ReconcileAsync(interruptedRoot, reconcile);
        Require(reconciled.Outcome == BitLockerProtectorCreationOutcome.Verified && reconcile.Adds == 0 && !File.Exists(Path.Combine(interruptedRoot, "bitlocker-protector.pending.json")),
            "Reconciliation verifies the interrupted request without repeating it");

        const string json = "{\"volume\":\"C:\",\"identity\":\"\\\\\\\\?\\\\Volume{1}\\\\\",\"volumeStatus\":\"FullyDecrypted\",\"lockStatus\":\"Unlocked\",\"operatingSystem\":true,\"tpmPresent\":true,\"tpmReady\":true,\"protectors\":[]}";
        var parsed = WindowsBitLockerProtectorCreationHost.Interpret(new ToolResult(0, json, "", false), "C:");
        Require(parsed.Complete && parsed.OperatingSystemVolume && parsed.TpmReady && parsed.Protectors.Count == 0, "Protector inspection parses exact evidence");
        Require(!WindowsBitLockerProtectorCreationHost.Interpret(new ToolResult(0, json.Replace("\"tpmReady\":true", "\"tpmReady\":\"True\""), "", false), "C:").Complete &&
            !WindowsBitLockerProtectorCreationHost.Interpret(new ToolResult(0, json.Replace("Unlocked", "Open"), "", false), "C:").Complete,
            "Mistyped protector evidence stays incomplete");
        Console.WriteLine("PASS: BitLocker TPM and recovery-password protector creation, single-protector rules, final eligibility, durable no-repeat guard and reconciliation; recovery passwords never returned; substituted providers only.");
    }

    private static BitLockerProtectorCreationState State(IReadOnlyList<BitLockerProtector> protectors) =>
        new("C:", @"\\?\Volume{1}\", "FullyDecrypted", "Unlocked", true, true, true, protectors, true);

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host(IReadOnlyList<BitLockerProtectorCreationState> states) : IBitLockerProtectorCreationHost
    {
        private int index;
        public bool Administrator { get; init; } = true;
        public bool RestartPending { get; init; }
        public bool Throw { get; init; }
        public bool IsAdministrator => Administrator;
        public int Adds { get; private set; }
        public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token) => Task.FromResult(RestartPending ? PendingRestart() : ClearRestart());
        public Task<BitLockerProtectorCreationState> InspectAsync(string volume, CancellationToken token) => Task.FromResult(states[Math.Min(index++, states.Count - 1)]);
        public Task<ToolResult> AddAsync(string volume, BitLockerNewProtector kind, CancellationToken token)
        {
            Adds++;
            if (Throw) throw new IOException("The connection to the provider was lost.");
            return Task.FromResult(new ToolResult(0, "", "", false));
        }
    }

    private static RestartSnapshot ClearRestart() => new(true, false, false, false, false, false, "TESTPC", "TESTPC");
    private static RestartSnapshot PendingRestart() => new(true, true, false, false, false, false, "TESTPC", "TESTPC");
}
