using System.IO;
using KillerBench.Engine;

internal static class RecoveryChecks
{
    internal static async Task RunAsync(string scratch)
    {
        const string xml = "<Events><Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-Kernel-General'/><EventID>12</EventID><EventRecordID>100</EventRecordID><TimeCreated SystemTime='2026-09-12T01:00:00Z'/></System></Event></Events>";
        var boot = RepairRecoveryStore.ParseBoot(new(0, xml, "", false)) ?? throw new Exception("Startup event was not parsed");
        foreach (var failedBoot in new[] { new ToolResult(0, xml, "Partial query failure", false),
            new ToolResult(1, xml, "", false), new ToolResult(0, xml, "", false) { TimedOut = true },
            new ToolResult(0, xml, "", true) })
            Require(RepairRecoveryStore.ParseBoot(failedBoot) is null, "Incomplete startup collection cannot release recovery guard");
        foreach (string extra in new[] { "<Provider Name='other'/>", "<EventID>13</EventID>",
            "<EventRecordID>101</EventRecordID>", "<TimeCreated SystemTime='2026-09-13T01:00:00Z'/>" })
            Require(RepairRecoveryStore.ParseBoot(new(0, xml.Replace("</System>", extra + "</System>"), "", false)) is null,
                "Duplicate startup fields cannot establish a boot identity");
        Require(RepairRecoveryStore.ParseBoot(new(0, xml.Replace("</Event>", "<System/></Event>"), "", false)) is null,
            "Duplicate System sections cannot establish a boot identity");
        Require(RepairRecoveryStore.ParseBoot(new(0, xml.Replace("<EventID>12", "<EventID>13"), "", false)) is null, "Only startup events establish boot identity");
        Require(RepairRecoveryStore.ParseBoot(new(0, "<!DOCTYPE x [<!ENTITY x SYSTEM 'file:///missing'>]><x>&x;</x>", "", false)) is null, "Recovery XML rejects external entities");
        var linkedPending = new RepairRecoveryRecord(1, Environment.MachineName, "linked-journal.json", true, boot);
        var linkedJournal = new RepairResult(Environment.MachineName, RepairKind.SystemFiles, boot.StartedUtc,
            "Running", linkedPending.JournalPath, []);
        Require(RepairRecoveryStore.MatchesPendingJournal(linkedPending, linkedJournal, RepairKind.SystemFiles),
            "Exact machine, repair and journal identities link recovery evidence");
        Require(RepairRecoveryStore.IsRelatedJournal(linkedPending, linkedJournal, RepairKind.SystemFiles) &&
            RepairRecoveryStore.IsRelatedJournal(linkedPending with { Machine = "OTHER-MACHINE" }, linkedJournal,
                RepairKind.SystemFiles) &&
            !RepairRecoveryStore.IsRelatedJournal(linkedPending, linkedJournal, RepairKind.ComponentStore),
            "Recovery linkage distinguishes another repair from invalid machine ownership");
        foreach (var (pending, journal, kind) in new (RepairRecoveryRecord? Pending, RepairResult? Journal, RepairKind Kind)[]
        {
            (linkedPending with { Pending = false }, linkedJournal, RepairKind.SystemFiles),
            (linkedPending with { Machine = "OTHER-MACHINE" }, linkedJournal, RepairKind.SystemFiles),
            (linkedPending, linkedJournal with { Machine = "OTHER-MACHINE" }, RepairKind.SystemFiles),
            (linkedPending, linkedJournal with { JournalPath = "other-journal.json" }, RepairKind.SystemFiles),
            (linkedPending, linkedJournal, RepairKind.ComponentStore),
            (null, linkedJournal, RepairKind.SystemFiles),
            (linkedPending, null, RepairKind.SystemFiles)
        })
            Require(!RepairRecoveryStore.MatchesPendingJournal(pending, journal, kind),
                "Mismatched recovery identity cannot release a pending repair guard");
        string folder = Path.Combine(scratch, "recovery-state");
        RepairRecoveryStore Store() => new(folder, _ => Task.FromResult<BootStamp?>(boot));
        var first = Store();
        await first.BeginAsync("synthetic-first.json");
        await MustBlock(Store());
        Require((await Store().ReadAsync())?.JournalPath == "synthetic-first.json", "Blocked attempt preserves the previous recovery record");
        boot = boot with { RecordId = 101 };
        await MustBlock(Store());
        boot = boot with { StartedUtc = boot.StartedUtc.AddHours(1) };
        var afterRestart = Store();
        await afterRestart.BeginAsync("synthetic-after-restart.json");
        await afterRestart.CompleteAsync();
        Require((await Store().ReadAsync())?.Pending == false, "Completed command clears pending state without losing record");

        var timeoutHost = new Host { Timeout = true };
        var timed = await Repairs.RunAsync(RepairKind.SystemFiles, scratch, host: timeoutHost, recoveryStore: Store());
        Require(timed.Status.StartsWith("Timed out") && (await Store().ReadAsync())?.Pending == true, "Timeout leaves durable pending state");
        var blockedHost = new Host();
        try { await Repairs.RunAsync(RepairKind.SystemFiles, scratch, host: blockedHost, recoveryStore: Store()); throw new Exception("Unfinished operation was repeated"); }
        catch (InvalidOperationException) { Require(blockedHost.Calls == 0, "Recovery guard precedes command execution"); }
        boot = new(102, boot.StartedUtc.AddHours(1));
        var complete = await Repairs.RunAsync(RepairKind.SystemFiles, scratch, host: new Host(), recoveryStore: Store());
        Require(Repairs.IsVerified(complete) && (await Store().ReadAsync())?.Pending == false, "New boot permits fresh repair and normal verification");
        boot = new(103, boot.StartedUtc.AddHours(1));
        var failedMutationStore = Store();
        var failedMutation = await Repairs.RunAsync(RepairKind.SystemFiles, scratch, host: new Host { ExitCode = 5 }, recoveryStore: failedMutationStore);
        Require(failedMutation.Steps.Count == 1 && failedMutation.GetExitCode() == 3 && (await failedMutationStore.ReadAsync())?.Pending == true,
            "Failed mutation evidence retains the recovery guard and skips verification");
        boot = new(104, boot.StartedUtc.AddHours(1));
        var failedVerificationStore = Store();
        var failedVerification = await Repairs.RunAsync(RepairKind.SystemFiles, scratch, host: new Host { FailVerification = true }, recoveryStore: failedVerificationStore);
        Require(failedVerification.Steps.Count == 2 && failedVerification.GetExitCode() == 3 && (await failedVerificationStore.ReadAsync())?.Pending == true,
            "Failed verification retains the original mutation guard instead of starting a second recovery record");
        await File.WriteAllTextAsync(Path.Combine(folder, "recovery.json"), "{\"schema\":1,\"machine\":\"fixture\",\"journalPath\":\"fixture\"}");
        await MustBlock(Store());
        foreach (string conflictingRecord in new[] {
            "{\"schema\":1,\"machine\":\"fixture\",\"journalPath\":\"fixture\",\"pending\":true,\"pending\":false,\"boot\":null}",
            "{\"schema\":1,\"machine\":\"fixture\",\"journalPath\":\"fixture\",\"pending\":false,\"Pending\":true,\"boot\":null}",
            "{\"schema\":1,\"machine\":\"fixture\",\"journalPath\":\"fixture\",\"pending\":false,\"boot\":{\"recordId\":100,\"recordId\":101,\"startedUtc\":\"2026-09-12T01:00:00Z\"}}" })
        {
            string recoveryPath = Path.Combine(folder, "recovery.json");
            await File.WriteAllTextAsync(recoveryPath, conflictingRecord);
            await MustBlock(Store());
            Require(await File.ReadAllTextAsync(recoveryPath) == conflictingRecord,
                "Conflicting recovery fields block new execution without replacing retained evidence");
        }
        string boundedPath = Path.Combine(folder, "recovery.json");
        const string validRecord = "{\"schema\":1,\"machine\":\"fixture\",\"journalPath\":\"fixture\",\"pending\":false,\"boot\":null}";
        await File.WriteAllTextAsync(boundedPath, validRecord.PadRight(16_384));
        Require((await Store().ReadAsync())?.Pending == false, "Recovery record at byte limit remains readable");
        await File.WriteAllTextAsync(boundedPath, validRecord.PadRight(16_385));
        await MustBlock(Store());
        Require(new FileInfo(boundedPath).Length == 16_385, "Oversized recovery record is preserved");
        await File.WriteAllTextAsync(boundedPath, validRecord, new System.Text.UTF8Encoding(true));
        Require((await Store().ReadAsync())?.Pending == false, "UTF-8 BOM recovery record remains readable");
        try { await Store().ReadAsync(new CancellationToken(true)); throw new Exception("Canceled recovery read continued"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: durable interruption guard, restart evidence, command ordering, timeout persistence and corrupt-record rejection (simulated repairs).");
    }
    private static async Task MustBlock(RepairRecoveryStore store)
    {
        try { await store.BeginAsync("must-not-start.json"); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Recovery record should have blocked another repair");
    }
    private sealed class Host : IRepairHost
    {
        public bool Timeout;
        public bool FailVerification;
        public int ExitCode;
        public int Calls;
        public void CheckPrerequisites() { }
        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress)
        { Calls++; return Task.FromResult(new ToolResult(Timeout ? -1 : step.Verification && FailVerification ? 5 : ExitCode, "Windows Resource Protection did not find any integrity violations", "", false) { TimedOut = Timeout }); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
