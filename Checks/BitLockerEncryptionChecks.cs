using KillerBench.Engine;
using System.IO;

internal static class BitLockerEncryptionChecks
{
    private static readonly Guid Recovery = Guid.Parse("f2a718c9-bcc9-41c9-a2e9-2a7d9e294e9f");
    private static readonly Guid Tpm = Guid.Parse("64cf98b4-cc46-45fe-8783-8d93b53dba5a");
    private static readonly RestartSnapshot ClearRestart = new(true, false, false, false, false, false, "FIXTURE", "FIXTURE");

    internal static async Task RunAsync(string scratch)
    {
        const string backupPath = @"D:\bitlocker-encryption-recovery.txt";
        var options = new BitLockerOptions("C:", BitLockerOperation.Encrypt, BitLockerEncryption.XtsAes128,
            true, RecoveryDestination.File, backupPath);
        var eligible = State();
        Require(BitLockerEncryptionExecution.EligibilityBlocker(options, Recovery, eligible) is null,
            "Exact decrypted volume with TPM and selected recovery protector is eligible");
        foreach (var blocked in new[]
        {
            eligible with { Complete = false },
            eligible with { Identity = "" },
            eligible with { VolumeStatus = "EncryptionInProgress", EncryptionPercentage = 1, EncryptionMethod = "XtsAes128" },
            eligible with { LockStatus = "Locked" },
            eligible with { FileSystem = "ReFS" },
            eligible with { VolumeType = "Data" },
            eligible with { TpmReady = false },
            eligible with { PolicyPresent = true },
            eligible with { Protectors = [new(Recovery, "RecoveryPassword")] },
            eligible with { Protectors = [new(Tpm, "Tpm"), new(Recovery, "NumericalPassword")] },
            eligible with { EncryptionMethod = "Aes128" }
        })
            Require(BitLockerEncryptionExecution.EligibilityBlocker(options, Recovery, blocked) is not null,
                "Incomplete or changed encryption evidence fails closed");

        var parsed = WindowsBitLockerEncryptionHost.Interpret(new ToolResult(0,
            "{\"volume\":\"C:\",\"identity\":\"fixture-volume\",\"volumeStatus\":\"FullyDecrypted\",\"protectionStatus\":\"Off\",\"lockStatus\":\"Unlocked\",\"encryptionPercentage\":0,\"encryptionMethod\":\"None\",\"fileSystem\":\"NTFS\",\"volumeType\":\"OperatingSystem\",\"tpmReady\":true,\"policyPresent\":false,\"protectors\":[{\"id\":\"" + Tpm + "\",\"type\":\"Tpm\"},{\"id\":\"" + Recovery + "\",\"type\":\"RecoveryPassword\"}]}", "", false), "C:");
        Require(parsed.Complete && parsed.Protectors.Count == 2, "Strict native encryption inventory accepted");
        Require(!WindowsBitLockerEncryptionHost.Interpret(new ToolResult(0,
            "{\"volume\":\"C:\",\"identity\":\"fixture-volume\",\"volumeStatus\":\"FullyDecrypted\",\"protectionStatus\":\"Off\",\"lockStatus\":\"Unlocked\",\"encryptionPercentage\":0,\"encryptionMethod\":\"None\",\"fileSystem\":\"NTFS\",\"volumeType\":\"OperatingSystem\",\"tpmReady\":true,\"policyPresent\":false,\"protectors\":[],\"extra\":true}", "", false), "C:").Complete,
            "Unexpected provider fields fail closed");

        string successFolder = Path.Combine(scratch, "bitlocker-encryption-success-" + Guid.NewGuid().ToString("N"));
        var successHost = new Fixture();
        var success = await BitLockerEncryptionExecution.RunAsync(options, Recovery, true, successFolder, successHost);
        Require(success.Outcome == BitLockerEncryptionOutcome.Started && successHost.Backups == 1 && successHost.Starts == 1 &&
            success.After?.VolumeStatus == "EncryptionInProgress" && !File.Exists(Path.Combine(successFolder, "bitlocker-encryption.pending.json")),
            "Verified backup precedes one encryption request and exact conversion verification");

        string racedFolder = Path.Combine(scratch, "bitlocker-encryption-race-" + Guid.NewGuid().ToString("N"));
        var racedHost = new Fixture { RaceAtFinalCheck = true };
        var raced = await BitLockerEncryptionExecution.RunAsync(options, Recovery, true, racedFolder, racedHost);
        Require(raced.Outcome == BitLockerEncryptionOutcome.Blocked && racedHost.Backups == 1 && racedHost.Starts == 0 &&
            !File.Exists(Path.Combine(racedFolder, "bitlocker-encryption.pending.json")),
            "Final state change blocks dispatch and closes prepared guard");

        string interruptedFolder = Path.Combine(scratch, "bitlocker-encryption-interrupted-" + Guid.NewGuid().ToString("N"));
        var interruptedHost = new Fixture { InterruptVerification = true };
        var interrupted = await BitLockerEncryptionExecution.RunAsync(options, Recovery, true, interruptedFolder, interruptedHost);
        string pendingPath = Path.Combine(interruptedFolder, "bitlocker-encryption.pending.json");
        Require(interrupted.Outcome == BitLockerEncryptionOutcome.Uncertain && interruptedHost.Starts == 1 &&
            File.Exists(pendingPath) && !File.ReadAllText(pendingPath).Contains("bitlocker-encryption-recovery", StringComparison.OrdinalIgnoreCase),
            "Interrupted dispatch retains no-repeat recovery guard");
        interruptedHost.InterruptVerification = false;
        var recovered = await BitLockerEncryptionExecution.ReconcileAsync(interruptedFolder, interruptedHost);
        Require(recovered.Outcome == BitLockerEncryptionOutcome.Started && interruptedHost.Starts == 1 &&
            !File.Exists(Path.Combine(interruptedFolder, "bitlocker-encryption.pending.json")),
            "Recovery verifies current conversion without repeating encryption");

        Console.WriteLine("PASS: BitLocker encryption requires verified recovery backup, exact final eligibility and no-repeat reconciliation.");
    }

    private static BitLockerEncryptionState State() => new("C:", "fixture-volume", "FullyDecrypted", "Off",
        "Unlocked", 0, "None", "NTFS", "OperatingSystem", true, false,
        [new(Tpm, "Tpm"), new(Recovery, "RecoveryPassword")], true);

    private static BitLockerEncryptionState StartedState() => State() with
    {
        VolumeStatus = "EncryptionInProgress",
        ProtectionStatus = "On",
        EncryptionPercentage = 1,
        EncryptionMethod = "XtsAes128"
    };

    private sealed class Fixture : IBitLockerEncryptionHost
    {
        internal int Backups;
        internal int Starts;
        internal bool RaceAtFinalCheck;
        internal bool InterruptVerification;
        internal BitLockerEncryptionState State = BitLockerEncryptionChecks.State();
        private int inspections;
        public bool IsAdministrator => true;
        public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token) => Task.FromResult(ClearRestart);
        public Task<BitLockerEncryptionState> InspectAsync(string volume, CancellationToken token)
        {
            inspections++;
            if (RaceAtFinalCheck && inspections >= 3) State = StartedState();
            if (InterruptVerification && Starts > 0) throw new IOException("Fixture verification interruption");
            return Task.FromResult(State);
        }
        public Task<RecoveryBackupReceipt> BackupAsync(BitLockerOptions options, Guid protector,
            IProgress<string>? progress, CancellationToken token)
        {
            Backups++;
            return Task.FromResult(new RecoveryBackupReceipt(State.Identity, protector, options.Backup, true));
        }
        public Task<ToolResult> StartAsync(BitLockerOptions options, IProgress<string>? progress, CancellationToken token)
        {
            Starts++;
            State = StartedState();
            return Task.FromResult(new ToolResult(0, "Conversion requested", "", false));
        }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
