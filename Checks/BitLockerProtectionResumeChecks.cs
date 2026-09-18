using System.IO;
using KillerBench.Engine;

internal static class BitLockerProtectionResumeChecks
{
    private static readonly Guid ProtectorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProtectorB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    internal static async Task RunAsync(string scratch)
    {
        Require(BitLockerProtectionResume.NormalizeVolume("c:") == "C:", "BitLocker resume normalizes the selected volume");
        foreach (string invalid in new[] { "", "C", "C:\\", "1:" })
            try { BitLockerProtectionResume.NormalizeVolume(invalid); throw new Exception("Invalid volume accepted"); }
            catch (ArgumentException) { }

        string previewFolder = Path.Combine(scratch, "bitlocker-resume-preview");
        var previewHost = new Host();
        var preview = await BitLockerProtectionResume.RunAsync("c:", false, previewFolder, previewHost);
        Require(preview.Outcome == BitLockerProtectionResumeOutcome.Preview && previewHost.Calls == 0 && !Directory.Exists(previewFolder),
            "BitLocker resume preview performs no system read, mutation or journal write");

        var reviewHost = new Host { States = new([State()]) };
        var review = await BitLockerProtectionResume.ReviewAsync("C:", reviewHost);
        var reviewCheck = BitLockerProtectionResume.ToWorkflowCheck(review);
        Require(review.Outcome == BitLockerProtectionResumeOutcome.Preview && reviewHost.Inspections == 1 &&
            reviewHost.Resumes == 0 && reviewCheck.State == CheckState.Repairable &&
            reviewCheck.Repair == RepairKind.BitLockerProtectionResume,
            "Complete suspended-protection evidence exposes explicit resume without mutation");
        var protectedReview = await BitLockerProtectionResume.ReviewAsync("C:",
            new Host { States = new([State(protection: "On")]) });
        var protectedCheck = BitLockerProtectionResume.ToWorkflowCheck(protectedReview);
        Require(protectedReview.Outcome == BitLockerProtectionResumeOutcome.Blocked &&
            protectedCheck.State == CheckState.Attention && protectedCheck.Repair is null,
            "Already protected volume evidence cannot expose resume");

        var blockers = new[]
        {
            State(complete: false), State(identity: ""), State(status: "EncryptionInProgress", percent: 50),
            State(protection: "On"), State(locked: "Locked"), State(protectors: []),
            State(protectors: [ProtectorA, ProtectorA]), State(volume: "D:")
        };
        foreach (var state in blockers)
        {
            var host = new Host { States = new([state]) };
            var result = await BitLockerProtectionResume.RunAsync("C:", true,
                Path.Combine(scratch, "bitlocker-resume-blocked-" + Guid.NewGuid().ToString("N")), host);
            Require(result.Outcome == BitLockerProtectionResumeOutcome.Blocked && host.Resumes == 0,
                "Incomplete, mismatched or ineligible BitLocker evidence blocks resume");
        }
        foreach (var host in new[]
        {
            new Host { Administrator = false },
            new Host { Restart = ClearRestart() with { WindowsUpdate = true } },
            new Host { Restart = ClearRestart() with { Complete = false } }
        })
        {
            var result = await BitLockerProtectionResume.RunAsync("C:", true,
                Path.Combine(scratch, "bitlocker-resume-prerequisite-" + Guid.NewGuid().ToString("N")), host);
            Require(result.Outcome == BitLockerProtectionResumeOutcome.Blocked && host.Resumes == 0,
                "Administrator and clear restart evidence are required before BitLocker resume");
        }

        string finalChangedFolder = Path.Combine(scratch, "bitlocker-resume-final-changed");
        var finalChangedHost = new Host { States = new([State(), State(), State(protection: "On")]) };
        var finalChanged = await BitLockerProtectionResume.RunAsync("C:", true, finalChangedFolder, finalChangedHost);
        Require(finalChanged.Outcome == BitLockerProtectionResumeOutcome.Blocked && finalChangedHost.Inspections == 3 &&
            finalChangedHost.RestartReads == 2 && finalChangedHost.Resumes == 0 && finalChanged.After?.ProtectionStatus == "On" &&
            !File.Exists(Path.Combine(finalChangedFolder, "bitlocker-resume.pending.json")) &&
            Directory.GetFiles(finalChangedFolder, "*.prepared.json").Length == 1,
            "Protection activated after preparation records final evidence without another resume request");

        string finalRestartFolder = Path.Combine(scratch, "bitlocker-resume-final-restart");
        var finalRestartHost = new Host
        {
            States = new([State(), State(), State()]),
            Restarts = new([ClearRestart(), ClearRestart() with { WindowsUpdate = true }])
        };
        var finalRestart = await BitLockerProtectionResume.RunAsync("C:", true, finalRestartFolder, finalRestartHost);
        Require(finalRestart.Outcome == BitLockerProtectionResumeOutcome.Blocked && finalRestartHost.Resumes == 0 &&
            !File.Exists(Path.Combine(finalRestartFolder, "bitlocker-resume.pending.json")),
            "Restart evidence changed after preparation blocks the final resume request");

        string finalUnavailableFolder = Path.Combine(scratch, "bitlocker-resume-final-unavailable");
        var finalUnavailableHost = new Host { ThrowAfterInspection = 2 };
        var finalUnavailable = await BitLockerProtectionResume.RunAsync("C:", true,
            finalUnavailableFolder, finalUnavailableHost);
        Require(finalUnavailable.Outcome == BitLockerProtectionResumeOutcome.Blocked &&
            finalUnavailableHost.Inspections == 3 && finalUnavailableHost.Resumes == 0 &&
            !File.Exists(Path.Combine(finalUnavailableFolder, "bitlocker-resume.pending.json")) &&
            Directory.GetFiles(finalUnavailableFolder, "*.prepared.json").Length == 1 &&
            finalUnavailable.Detail.Contains("No resume request was sent", StringComparison.Ordinal),
            "Unavailable final BitLocker evidence archives a prepared no-request result");

        string verifiedFolder = Path.Combine(scratch, "bitlocker-resume-verified");
        var verifiedHost = new Host { States = new([State(), State(), State(), State(protection: "On")]) };
        var verified = await BitLockerProtectionResume.RunAsync("C:", true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == BitLockerProtectionResumeOutcome.Verified && verified.GetExitCode() == 0 &&
            verifiedHost.Inspections == 4 && verifiedHost.RestartReads == 2 && verifiedHost.Resumes == 1 &&
            !File.Exists(Path.Combine(verifiedFolder, "bitlocker-resume.pending.json")) &&
            Directory.GetFiles(verifiedFolder, "*.prepared.json").Length == 1,
            "BitLocker resume repeats eligibility, verifies the same volume and protector identities and archives its guard");

        string notAppliedFolder = Path.Combine(scratch, "bitlocker-resume-not-applied");
        var notApplied = await BitLockerProtectionResume.RunAsync("C:", true, notAppliedFolder,
            new Host { States = new([State(), State(), State(), State()]) });
        Require(notApplied.Outcome == BitLockerProtectionResumeOutcome.NotApplied &&
            !File.Exists(Path.Combine(notAppliedFolder, "bitlocker-resume.pending.json")),
            "A complete unchanged protected state records NotApplied without an indefinite guard");

        string changedFolder = Path.Combine(scratch, "bitlocker-resume-changed");
        var changedHost = new Host { States = new([State(), State(identity: "volume-b")]) };
        var changed = await BitLockerProtectionResume.RunAsync("C:", true, changedFolder, changedHost);
        Require(changed.Outcome == BitLockerProtectionResumeOutcome.Blocked && changedHost.Resumes == 0 &&
            !File.Exists(Path.Combine(changedFolder, "bitlocker-resume.pending.json")),
            "A changed volume identity or protector set blocks resume before durable preparation");

        string changedStateFolder = Path.Combine(scratch, "bitlocker-resume-changed-state");
        var changedStateHost = new Host { States = new([State(), State(protection: "On")]) };
        var changedState = await BitLockerProtectionResume.RunAsync("C:", true, changedStateFolder, changedStateHost);
        Require(changedState.Outcome == BitLockerProtectionResumeOutcome.Blocked && changedStateHost.Resumes == 0 &&
            !File.Exists(Path.Combine(changedStateFolder, "bitlocker-resume.pending.json")),
            "A volume that is no longer eligible is not resumed");

        string failedFolder = Path.Combine(scratch, "bitlocker-resume-failed");
        var failedHost = new Host { Command = new(1, "", "provider failed", false), States = new([State(), State(), State(), State()]) };
        var failed = await BitLockerProtectionResume.RunAsync("C:", true, failedFolder, failedHost);
        var failedRecovery = await BitLockerProtectionResume.ReconcileAsync(failedFolder, failedHost);
        Require(failed.Outcome == BitLockerProtectionResumeOutcome.Failed &&
            failedRecovery.Outcome == BitLockerProtectionResumeOutcome.Blocked &&
            failedHost.Resumes == 1 && failedHost.Inspections == 4 &&
            !File.Exists(Path.Combine(failedFolder, "bitlocker-resume.pending.json")),
            "An unsuccessful command with fresh exact unchanged state records failure without repeating or guessing");

        string recoverFolder = Path.Combine(scratch, "bitlocker-resume-recover");
        var recoverHost = new Host { States = new([State(), State(), State()]), ThrowAfterInspection = 3 };
        var uncertain = await BitLockerProtectionResume.RunAsync("C:", true, recoverFolder, recoverHost);
        Require(uncertain.Outcome == BitLockerProtectionResumeOutcome.Uncertain && recoverHost.Resumes == 1,
            "Unavailable post-state retains a durable BitLocker resume guard");
        recoverHost.ThrowAfterInspection = null;
        recoverHost.States.Enqueue(State(protection: "On"));
        int resumesBeforeRecovery = recoverHost.Resumes;
        var recovered = await BitLockerProtectionResume.ReconcileAsync(recoverFolder, recoverHost);
        Require(recovered.Outcome == BitLockerProtectionResumeOutcome.Verified &&
            recoverHost.Resumes == resumesBeforeRecovery && !File.Exists(Path.Combine(recoverFolder, "bitlocker-resume.pending.json")),
            "Recovery verifies fresh state without repeating BitLocker resume");

        string foreignFolder = Path.Combine(scratch, "bitlocker-resume-foreign");
        var foreignHost = new Host { States = new([State(), State(), State(), State(identity: "volume-b", protection: "On")]) };
        var foreignAttempt = await BitLockerProtectionResume.RunAsync("C:", true, foreignFolder, foreignHost);
        string foreignPending = Path.Combine(foreignFolder, "bitlocker-resume.pending.json");
        Require(foreignAttempt.Outcome == BitLockerProtectionResumeOutcome.Uncertain && File.Exists(foreignPending),
            "Changed identity creates a durable BitLocker resume guard");
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        int foreignInspections = foreignHost.Inspections;
        var foreignRecovery = await BitLockerProtectionResume.ReconcileAsync(foreignFolder, foreignHost);
        Require(foreignRecovery.Outcome == BitLockerProtectionResumeOutcome.Uncertain &&
            foreignHost.Inspections == foreignInspections && foreignHost.Resumes == 1 && File.Exists(foreignPending),
            "A foreign-machine BitLocker resume record retains its guard without inspecting or changing the host");

        string validJson = "{\"volume\":\"C:\",\"identity\":\"volume-a\",\"volumeStatus\":\"FullyEncrypted\",\"protectionStatus\":\"Off\",\"lockStatus\":\"Unlocked\",\"encryptionPercentage\":100,\"protectors\":[\"" + ProtectorA + "\",\"" + ProtectorB + "\"]}";
        Require(WindowsBitLockerProtectionResumeHost.Interpret(new(0, validJson, "", false), "C:").Complete,
            "Native BitLocker resume inspection accepts a complete exact payload");
        Require(!WindowsBitLockerProtectionResumeHost.Interpret(new(0, validJson.Replace("]}", "],\"extra\":true}"), "", false), "C:").Complete &&
            !WindowsBitLockerProtectionResumeHost.Interpret(new(0, validJson.Replace(ProtectorB.ToString(), ProtectorA.ToString()), "", false), "C:").Complete,
            "Native BitLocker resume inspection rejects added fields and duplicate protector identities");
    }

    private static BitLockerProtectionState State(string volume = "C:", string identity = "volume-a",
        string status = "FullyEncrypted", string protection = "Off", string locked = "Unlocked",
        int percent = 100, IReadOnlyList<Guid>? protectors = null, bool complete = true) =>
        new(volume, identity, status, protection, locked, percent, protectors ?? [ProtectorA, ProtectorB], complete);

    private static RestartSnapshot ClearRestart() => new(true, false, false, false, false, false, "TESTPC", "TESTPC");

    private sealed class Host : IBitLockerProtectionResumeHost
    {
        public bool Administrator { get; set; } = true;
        public bool IsAdministrator => Administrator;
        public RestartSnapshot Restart { get; set; } = ClearRestart();
        public Queue<RestartSnapshot> Restarts { get; set; } = new();
        public Queue<BitLockerProtectionState> States { get; set; } = new([State(), State(), State(), State(protection: "On")]);
        public ToolResult Command { get; set; } = new(0, "", "", false);
        public int? ThrowAfterInspection { get; set; }
        public int Inspections { get; private set; }
        public int RestartReads { get; private set; }
        public int Resumes { get; private set; }
        public int Calls => Inspections + RestartReads + Resumes;

        public Task<RestartSnapshot> ReadRestartAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            RestartReads++;
            return Task.FromResult(Restarts.Count > 0 ? Restarts.Dequeue() : Restart);
        }

        public Task<BitLockerProtectionState> InspectAsync(string volume, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Inspections++;
            if (ThrowAfterInspection is int count && Inspections > count) throw new IOException("verification unavailable");
            return Task.FromResult(States.Count == 0 ? State() : States.Dequeue());
        }

        public Task<ToolResult> ResumeAsync(string volume, IProgress<string>? progress, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Resumes++; progress?.Report("resume dispatched"); return Task.FromResult(Command); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("CHECK FAILED: " + message);
    }
}
