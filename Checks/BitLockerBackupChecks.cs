using KillerBench.Engine;
using System.IO;

internal static class BitLockerBackupChecks
{
    internal static async Task RunAsync()
    {
        var identityEvidence = new ToolResult(0, "fixture-volume\r\n", "", false);
        if (WindowsBitLockerBackupHost.InterpretVolumeIdentity(identityEvidence) != "fixture-volume")
            throw new Exception("Complete volume identity rejected.");
        foreach (var invalid in new[]
        {
            identityEvidence with { Error = "Provider failed after output" },
            identityEvidence with { ExitCode = 1 },
            identityEvidence with { TimedOut = true },
            identityEvidence with { Truncated = true },
            identityEvidence with { Output = " " },
            identityEvidence with { Output = new string('x', 261) },
            identityEvidence with { Output = "first\nsecond" },
            identityEvidence with { Output = "first\rsecond" },
            identityEvidence with { Output = "first\tsecond" },
            identityEvidence with { Output = "first\0second" }
        })
            if (WindowsBitLockerBackupHost.InterpretVolumeIdentity(invalid) is not null)
                throw new Exception("Incomplete or malformed volume identity accepted.");
        var options = new BitLockerOptions("C:", BitLockerOperation.BackupRecovery, Backup: RecoveryDestination.File, BackupFile: @"D:\recovery.txt");
        var completeInventory = new BitLockerProtectorInventory("C:", [new(Fixture.Protector, "RecoveryPassword")], true);
        var eligible = BitLockerBackupEligibility.Assess(options, Fixture.Protector, true,
            BitLockerBackupDestinationState.Available, "volume", completeInventory, false);
        if (eligible is not { State: CheckState.Repairable, Repair: RepairKind.BitLockerRecoveryBackup })
            throw new Exception("Exact recovery backup selection was not exposed as a guarded recipe.");
        string selectedDestination = Path.Combine(Path.GetTempPath(), "KillerBench-eligibility-" + Guid.NewGuid().ToString("N") + ".txt");
        var selectedOptions = new BitLockerOptions("Z:", BitLockerOperation.BackupRecovery,
            Backup: RecoveryDestination.File, BackupFile: selectedDestination);
        var collectedEligibility = await BitLockerBackupEligibility.CheckAsync(selectedOptions, Fixture.Protector,
            Path.Combine(Path.GetTempPath(), "KillerBench-eligibility-journal-" + Guid.NewGuid().ToString("N")),
            new Fixture("success"), true);
        if (collectedEligibility is not { State: CheckState.Repairable, Repair: RepairKind.BitLockerRecoveryBackup } ||
            BitLockerBackupEligibility.InspectDestination(selectedOptions) != BitLockerBackupDestinationState.Available)
            throw new Exception("Read-only recovery backup eligibility did not retain exact selection evidence.");
        foreach (var blocked in new[]
        {
            BitLockerBackupEligibility.Assess(options, null, true, BitLockerBackupDestinationState.Available, "volume", completeInventory, false),
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, false, BitLockerBackupDestinationState.Available, "volume", completeInventory, false),
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, true, BitLockerBackupDestinationState.Existing, "volume", completeInventory, false),
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, true, BitLockerBackupDestinationState.Available, "volume", completeInventory, true)
        })
            if (blocked.State == CheckState.Repairable || blocked.Repair is not null)
                throw new Exception("Blocked recovery backup selection remained eligible.");
        foreach (var incomplete in new[]
        {
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, true, BitLockerBackupDestinationState.Unavailable, "volume", completeInventory, false),
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, true, BitLockerBackupDestinationState.Available, null, completeInventory, false),
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, true, BitLockerBackupDestinationState.Available, "volume", completeInventory with { Complete = false }, false),
            BitLockerBackupEligibility.Assess(options, Fixture.Protector, true, BitLockerBackupDestinationState.Available, "volume", completeInventory with { Volume = "D:" }, false)
        })
            if (incomplete.State != CheckState.Unknown || incomplete.Repair is not null)
                throw new Exception("Incomplete recovery backup evidence did not fail closed.");
        var progress = new BackupProgress();
        var observed = await BitLockerBackupWorkflow.BackupExistingAsync(options, Fixture.Protector, new Fixture("success"), progress: progress);
        if (!observed.Verified || progress.Messages.Count != 5 || progress.Messages.Any(s => s.Contains("recovery.txt") || s.Contains("SECRET")))
            throw new Exception("Backup stage progress lost or leaked sensitive data.");
        var brokenDisplay = await BitLockerBackupWorkflow.BackupExistingAsync(options, Fixture.Protector, new Fixture("success"), progress: new BackupProgress { Throw = true });
        if (!brokenDisplay.Verified) throw new Exception("Display failure interrupted backup verification.");
        foreach (string scenario in new[] { "success", "changed", "missing", "failed", "removed", "incomplete", "final-removed" })
        {
            var host = new Fixture(scenario);
            var receipt = await BitLockerBackupWorkflow.BackupExistingAsync(options, Fixture.Protector, host);
            bool prewriteBlocked = scenario is "changed" or "missing" or "final-removed";
            if (receipt.Verified != (scenario == "success") || host.Calls != (prewriteBlocked ? 0 : 1))
                throw new Exception("Backup identity verification failed: " + scenario);
        }
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        var cancelled = new Fixture("success");
        try
        {
            await BitLockerBackupWorkflow.BackupExistingAsync(options, Fixture.Protector, cancelled, stop.Token);
            throw new Exception("Cancelled backup was accepted.");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        if (cancelled.Calls != 0) throw new Exception("Cancelled backup wrote a file.");
        for (int stage = 1; stage <= 7; stage++)
        {
            var stalled = new StalledHost(stage);
            try
            {
                await BitLockerBackupWorkflow.BackupExistingAsync(options, Fixture.Protector, stalled,
                    timeout: TimeSpan.FromMilliseconds(30));
                throw new Exception("Stalled backup was accepted.");
            }
            catch (TimeoutException) { }
            if (stalled.Calls != stage || !stalled.Token.IsCancellationRequested)
                throw new Exception("Backup deadline did not stop provider sequencing.");
            stalled.Release.SetResult(true);
            if (stalled.Calls != stage) throw new Exception("Late provider completion resumed backup.");
        }
        foreach (string scenario in new[] { "success", "failed", "throws", "invalid-data", "security", "timeout", "changed", "final-removed" })
        {
            string folder = Path.Combine(Path.GetTempPath(), "KillerBench-backup-checks-" + scenario + "-" + Guid.NewGuid().ToString("N"));
            var result = await BitLockerBackupExecution.RunAsync(options, Fixture.Protector, folder, new Fixture(scenario));
            string journal = File.ReadAllText(result.JournalPath);
            bool prewriteBlocked = scenario is "changed" or "final-removed";
            if ((result.Status == "Backup verified") != (scenario == "success") ||
                (result.Status == "Backup blocked before write") != prewriteBlocked ||
                result.Status.Contains("SECRET") || journal.Contains("SECRET") || journal.Contains("recovery.txt") || journal.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length != 2)
                throw new Exception("Backup journal outcome or secret exclusion failed.");
            string pending = Path.Combine(folder, "backup.pending.json");
            bool uncertain = scenario is not ("success" or "changed" or "final-removed");
            if (uncertain != File.Exists(pending) || File.Exists(pending) && File.ReadAllText(pending).Contains("recovery.txt") ||
                prewriteBlocked && Directory.GetFiles(folder, "*.prepared.json").Length != 1)
                throw new Exception("Backup pending guard or destination privacy failed: " + scenario);
        }
        string guardedFolder = Path.Combine(Path.GetTempPath(), "KillerBench-backup-guard-" + Guid.NewGuid().ToString("N"));
        await BitLockerBackupExecution.RunAsync(options, Fixture.Protector, guardedFolder, new Fixture("failed"));
        var guardedHost = new Fixture("success");
        var guarded = await BitLockerBackupExecution.RunAsync(options, Fixture.Protector, guardedFolder, guardedHost);
        if (guardedHost.Calls != 0 || guarded.Status != "A previous backup requires recovery before another write")
            throw new Exception("A pending BitLocker backup did not prevent a repeated write.");
        foreach (var pair in new[]
        {
            (BitLockerBackupFileState.Verified, BitLockerBackupReconciliationOutcome.Verified),
            (BitLockerBackupFileState.Missing, BitLockerBackupReconciliationOutcome.Missing),
            (BitLockerBackupFileState.Invalid, BitLockerBackupReconciliationOutcome.Invalid),
            (BitLockerBackupFileState.Unavailable, BitLockerBackupReconciliationOutcome.Inconclusive)
        })
        {
            string folder = Path.Combine(Path.GetTempPath(), "KillerBench-backup-recovery-" + pair.Item1 + "-" + Guid.NewGuid().ToString("N"));
            await BitLockerBackupExecution.RunAsync(options, Fixture.Protector, folder, new Fixture("failed"));
            var recoveryHost = new Fixture("success") { FileState = pair.Item1 };
            var recovery = await BitLockerBackupReconciliation.RunAsync(options, Fixture.Protector, folder, recoveryHost);
            if (recovery.Outcome != pair.Item2 || recoveryHost.Inspections != 1 ||
                (pair.Item1 != BitLockerBackupFileState.Unavailable) == File.Exists(Path.Combine(folder, "backup.pending.json")))
                throw new Exception("Backup reconciliation outcome or guard handling failed: " + pair.Item1);
        }
        string mismatchFolder = Path.Combine(Path.GetTempPath(), "KillerBench-backup-recovery-mismatch-" + Guid.NewGuid().ToString("N"));
        await BitLockerBackupExecution.RunAsync(options, Fixture.Protector, mismatchFolder, new Fixture("failed"));
        var mismatchOptions = options with { BackupFile = @"D:\different.txt" };
        var mismatchHost = new Fixture("success") { FileState = BitLockerBackupFileState.Verified };
        var mismatch = await BitLockerBackupReconciliation.RunAsync(mismatchOptions, Fixture.Protector, mismatchFolder, mismatchHost);
        if (mismatch.Outcome != BitLockerBackupReconciliationOutcome.Inconclusive || mismatchHost.Inspections != 0 ||
            !File.Exists(Path.Combine(mismatchFolder, "backup.pending.json")))
            throw new Exception("A different recovery destination consumed pending backup evidence.");

        string foreignFolder = Path.Combine(Path.GetTempPath(), "KillerBench-backup-recovery-foreign-" + Guid.NewGuid().ToString("N"));
        await BitLockerBackupExecution.RunAsync(options, Fixture.Protector, foreignFolder, new Fixture("failed"));
        string foreignPending = Path.Combine(foreignFolder, "backup.pending.json");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        var foreignHost = new Fixture("success") { FileState = BitLockerBackupFileState.Verified };
        var foreign = await BitLockerBackupReconciliation.RunAsync(options, Fixture.Protector, foreignFolder, foreignHost);
        if (foreign.Outcome != BitLockerBackupReconciliationOutcome.Inconclusive || foreignHost.Inspections != 0 ||
            !File.Exists(foreignPending))
            throw new Exception("A foreign-machine backup record consumed evidence or inspected the selected destination.");
    }
    private sealed class BackupProgress : IProgress<string>
    {
        internal readonly List<string> Messages = [];
        internal bool Throw;
        public void Report(string value)
        {
            if (Throw) throw new InvalidOperationException("Display unavailable");
            Messages.Add(value);
        }
    }
    private sealed class StalledHost(int stage) : IBitLockerBackupHost
    {
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Calls;
        internal CancellationToken Token;
        private async Task<T> NextAsync<T>(T value, CancellationToken token)
        {
            Token = token;
            if (++Calls == stage) await Release.Task;
            return value;
        }
        public Task<string?> VolumeIdentityAsync(string volume, CancellationToken token) => NextAsync<string?>("volume", token);
        public Task<BitLockerProtectorInventory> ProtectorsAsync(string volume, CancellationToken token) =>
            NextAsync(new BitLockerProtectorInventory(volume, [new(Fixture.Protector, "RecoveryPassword")], true), token);
        public Task<bool> BackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token) => NextAsync(true, token);
    }
    private sealed class Fixture(string scenario) : IBitLockerBackupHost
    {
        internal static readonly Guid Protector = Guid.Parse("b19a63d0-55d6-44f1-a89b-82e2f784b982");
        internal int Calls;
        internal int Inspections;
        internal BitLockerBackupFileState FileState = BitLockerBackupFileState.Unavailable;
        private int reads;
        private int protectorReads;
        public Task<string?> VolumeIdentityAsync(string volume, CancellationToken token) => Task.FromResult<string?>(++reads > 1 && scenario == "changed" ? "changed" : "volume");
        public Task<BitLockerProtectorInventory> ProtectorsAsync(string volume, CancellationToken token)
        {
            protectorReads++;
            bool removed = scenario == "missing" || scenario == "final-removed" && protectorReads > 1 || Calls > 0 && scenario == "removed";
            return Task.FromResult(new BitLockerProtectorInventory(volume,
                removed ? [] : [new(Protector, "RecoveryPassword")], !(Calls > 0 && scenario == "incomplete")));
        }
        public Task<bool> BackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token)
        {
            Calls++;
            if (scenario == "throws") throw new IOException("SECRET provider text");
            if (scenario == "invalid-data") throw new InvalidDataException("SECRET invalid provider data");
            if (scenario == "security") throw new System.Security.SecurityException("SECRET provider security context");
            if (scenario == "timeout") throw new TimeoutException("SECRET provider timeout");
            return Task.FromResult(scenario != "failed");
        }
        public Task<BitLockerBackupFileState> InspectBackupFileAsync(BitLockerOptions options, Guid protector, CancellationToken token)
        { Inspections++; return Task.FromResult(FileState); }
    }
}
