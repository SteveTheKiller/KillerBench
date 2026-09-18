using System.IO;
using KillerBench.Engine;

internal static class PrivacyClipboardHistoryChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "privacy-clipboard-history");
        var absent = Evidence(false, null);
        var enabled = Evidence(true, 1);
        var disabled = Evidence(true, 0);

        var previewHost = new Host([absent]);
        var preview = await PrivacyClipboardHistoryExecution.RunAsync(true, false,
            Path.Combine(root, "preview"), previewHost);
        Require(preview.Outcome == PrivacyChangeOutcome.Preview && previewHost.Reads == 0 && previewHost.Writes.Count == 0,
            "Clipboard history defaults to preview without reading or writing state");

        string enableFolder = Path.Combine(root, "enable");
        var enableHost = new Host([absent, enabled]);
        var enabledResult = await PrivacyClipboardHistoryExecution.RunAsync(true, true, enableFolder, enableHost);
        Require(enabledResult.Outcome == PrivacyChangeOutcome.Verified && enabledResult.GetExitCode() == 0 &&
            enableHost.Writes.SequenceEqual(new bool?[] { true }) && enabledResult.JournalPath is not null &&
            File.Exists(enabledResult.JournalPath) && !File.Exists(Path.Combine(enableFolder, "privacy-clipboard-history.pending.json")),
            "Absent current-user value is enabled once with durable preparation and exact verification");

        var undoHost = new Host([enabled, absent]);
        var undone = await PrivacyClipboardHistoryExecution.UndoAsync(enabledResult.JournalPath!,
            Path.Combine(root, "undo"), undoHost);
        Require(undone.Outcome == PrivacyChangeOutcome.Verified && undoHost.Writes.SequenceEqual(new bool?[] { null }),
            "Undo restores an originally absent value and verifies exact absence");

        string interruptedUndoFolder = Path.Combine(root, "interrupted-undo");
        var interruptedUndoHost = new Host([enabled]) { Result = new(5, "", "denied", false) };
        var interruptedUndo = await PrivacyClipboardHistoryExecution.UndoAsync(enabledResult.JournalPath!,
            interruptedUndoFolder, interruptedUndoHost);
        Require(interruptedUndo.Outcome == PrivacyChangeOutcome.Uncertain &&
            File.Exists(Path.Combine(interruptedUndoFolder, "privacy-clipboard-history.pending.json")),
            "Interrupted undo retains the exact absent-value restoration target");
        var undoRecoveryHost = new Host([absent]);
        var recoveredUndo = await PrivacyClipboardHistoryExecution.ReconcileAsync(interruptedUndoFolder, undoRecoveryHost);
        Require(recoveredUndo.Outcome == PrivacyChangeReconciliationOutcome.Verified && undoRecoveryHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(interruptedUndoFolder, "privacy-clipboard-history.pending.json")),
            "Interrupted undo reconciliation verifies restored absence without repeating the write");

        var disableHost = new Host([enabled, disabled]);
        var disabledResult = await PrivacyClipboardHistoryExecution.RunAsync(false, true,
            Path.Combine(root, "disable"), disableHost);
        Require(disabledResult.Outcome == PrivacyChangeOutcome.Verified && disableHost.Writes.SequenceEqual(new bool?[] { false }),
            "Enabled current-user value is disabled once and verified");

        var already = new Host([enabled]);
        Require((await PrivacyClipboardHistoryExecution.RunAsync(true, true, Path.Combine(root, "already"), already)).Outcome == PrivacyChangeOutcome.Blocked && already.Writes.Count == 0,
            "Matching current-user state is not rewritten");
        var managed = new Host([Evidence(false, null, true, 1)]);
        Require((await PrivacyClipboardHistoryExecution.RunAsync(false, true, Path.Combine(root, "managed"), managed)).Outcome == PrivacyChangeOutcome.Blocked && managed.Writes.Count == 0,
            "Machine policy blocks current-user changes");
        var unsupported = new Host([new(false, true, null, false, null, "unsupported")]);
        Require((await PrivacyClipboardHistoryExecution.RunAsync(false, true, Path.Combine(root, "unsupported"), unsupported)).Outcome == PrivacyChangeOutcome.Blocked && unsupported.Writes.Count == 0,
            "Unsupported value evidence blocks changes");

        var changedAfter = new Host([absent, disabled]);
        var uncertain = await PrivacyClipboardHistoryExecution.RunAsync(true, true,
            Path.Combine(root, "changed-after"), changedAfter);
        Require(uncertain.Outcome == PrivacyChangeOutcome.Uncertain && File.Exists(Path.Combine(root, "changed-after", "privacy-clipboard-history.pending.json")),
            "Failed verification retains the pending guard");
        var failed = new Host([absent]) { Result = new(5, "", "denied", false) };
        Require((await PrivacyClipboardHistoryExecution.RunAsync(true, true, Path.Combine(root, "failed"), failed)).Outcome == PrivacyChangeOutcome.Uncertain &&
            File.Exists(Path.Combine(root, "failed", "privacy-clipboard-history.pending.json")),
            "Incomplete write outcome remains uncertain and guarded");
        var recoveredAppliedHost = new Host([enabled]);
        var recoveredApplied = await PrivacyClipboardHistoryExecution.ReconcileAsync(
            Path.Combine(root, "changed-after"), recoveredAppliedHost);
        Require(recoveredApplied.Outcome == PrivacyChangeReconciliationOutcome.Verified && recoveredAppliedHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(root, "changed-after", "privacy-clipboard-history.pending.json")),
            "Later evidence verifies an interrupted applied value without repeating the write");
        var recoveredOriginalHost = new Host([absent]);
        var recoveredOriginal = await PrivacyClipboardHistoryExecution.ReconcileAsync(
            Path.Combine(root, "failed"), recoveredOriginalHost);
        Require(recoveredOriginal.Outcome == PrivacyChangeReconciliationOutcome.CurrentStateDiffers && recoveredOriginalHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(root, "failed", "privacy-clipboard-history.pending.json")),
            "Later evidence archives a confirmed not-applied attempt without repeating the write");

        string ambiguousFolder = Path.Combine(root, "ambiguous-recovery");
        var ambiguous = new Host([absent, disabled]);
        await PrivacyClipboardHistoryExecution.RunAsync(true, true, ambiguousFolder, ambiguous);
        var ambiguousRecoveryHost = new Host([disabled]);
        Require((await PrivacyClipboardHistoryExecution.ReconcileAsync(ambiguousFolder, ambiguousRecoveryHost)).Outcome == PrivacyChangeReconciliationOutcome.Inconclusive &&
            ambiguousRecoveryHost.Writes.Count == 0 && File.Exists(Path.Combine(ambiguousFolder, "privacy-clipboard-history.pending.json")),
            "Later value unlike the requested or original state keeps the guard without repeating a write");

        var laterChange = new Host([disabled]);
        Require((await PrivacyClipboardHistoryExecution.UndoAsync(enabledResult.JournalPath!,
            Path.Combine(root, "later-change"), laterChange)).Outcome == PrivacyChangeOutcome.Blocked && laterChange.Writes.Count == 0,
            "Undo does not overwrite a later value change");
        var managedUndo = new Host([Evidence(true, 1, true, 1)]);
        Require((await PrivacyClipboardHistoryExecution.UndoAsync(enabledResult.JournalPath!,
            Path.Combine(root, "managed-undo"), managedUndo)).Outcome == PrivacyChangeOutcome.Blocked && managedUndo.Writes.Count == 0,
            "Undo preserves policy that appeared after the original change");

        string finalPolicyFolder = Path.Combine(root, "final-policy");
        var finalPolicyHost = new Host([enabled]) { FinalEligibility = Evidence(true, 1, true, 1) };
        var finalPolicy = await PrivacyClipboardHistoryExecution.RunAsync(false, true, finalPolicyFolder, finalPolicyHost);
        Require(finalPolicy.Outcome == PrivacyChangeOutcome.Blocked && finalPolicyHost.Writes.Count == 0 &&
            finalPolicy.JournalPath is not null && File.Exists(finalPolicy.JournalPath) &&
            File.Exists(finalPolicy.JournalPath + ".prepared.json") &&
            !File.Exists(Path.Combine(finalPolicyFolder, "privacy-clipboard-history.pending.json")),
            "Policy appearing after preparation prevents the registry write and closes the prepared guard");

        string unavailableFinalFolder = Path.Combine(root, "unavailable-final");
        var unavailableFinalHost = new Host([absent]) { ReadErrorAt = 2, ReadError = new IOException("fixture") };
        var unavailableFinal = await PrivacyClipboardHistoryExecution.RunAsync(true, true,
            unavailableFinalFolder, unavailableFinalHost);
        Require(unavailableFinal.Outcome == PrivacyChangeOutcome.Blocked && unavailableFinalHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(unavailableFinalFolder, "privacy-clipboard-history.pending.json")) &&
            File.Exists(unavailableFinal.JournalPath + ".prepared.json") &&
            unavailableFinal.Detail.Contains("No registry write was dispatched", StringComparison.Ordinal),
            "Unavailable final Clipboard history evidence archives a prepared no-write result");

        var finalUndoChange = new Host([enabled]) { FinalEligibility = disabled };
        var finalUndoBlocked = await PrivacyClipboardHistoryExecution.UndoAsync(enabledResult.JournalPath!,
            Path.Combine(root, "final-undo-change"), finalUndoChange);
        Require(finalUndoBlocked.Outcome == PrivacyChangeOutcome.Blocked && finalUndoChange.Writes.Count == 0,
            "A Clipboard history value changed after undo preparation is preserved");

        string unavailableUndoFolder = Path.Combine(root, "unavailable-final-undo");
        var unavailableUndoHost = new Host([enabled]) { ReadErrorAt = 2, ReadError = new IOException("fixture") };
        var unavailableUndo = await PrivacyClipboardHistoryExecution.UndoAsync(enabledResult.JournalPath!,
            unavailableUndoFolder, unavailableUndoHost);
        Require(unavailableUndo.Outcome == PrivacyChangeOutcome.Blocked && unavailableUndoHost.Writes.Count == 0 &&
            !File.Exists(Path.Combine(unavailableUndoFolder, "privacy-clipboard-history.pending.json")) &&
            File.Exists(unavailableUndo.JournalPath + ".prepared.json") &&
            unavailableUndo.Detail.Contains("No registry write was dispatched", StringComparison.Ordinal),
            "Unavailable final Clipboard history undo evidence archives a prepared no-write result");

        string canceledFolder = Path.Combine(root, "canceled");
        var canceledHost = new Host([absent]) { CancelDuringWrite = true };
        var stopped = await PrivacyClipboardHistoryExecution.RunAsync(true, true, canceledFolder, canceledHost);
        Require(stopped.Outcome == PrivacyChangeOutcome.Stopped && File.Exists(Path.Combine(canceledFolder, "privacy-clipboard-history.pending.json")),
            "Cancellation after preparation retains a no-repeat guard");

        Console.WriteLine("PASS: Clipboard history change uses exact current-user evidence, policy preservation, durable preparation, final eligibility, no-repeat reconciliation and guarded undo; simulated provider only.");
    }

    private static PrivacyClipboardHistoryEvidence Evidence(bool present, int? value,
        bool policyPresent = false, int? policyValue = null) =>
        new(true, present, value, policyPresent, policyValue, "fixture");

    private sealed class Host(IEnumerable<PrivacyClipboardHistoryEvidence> evidence) : IPrivacyClipboardHistoryHost
    {
        private readonly Queue<PrivacyClipboardHistoryEvidence> values = new(evidence);
        public int Reads;
        public List<bool?> Writes { get; } = [];
        public ToolResult Result = new(0, "written", "", false);
        public bool CancelDuringWrite;
        public PrivacyClipboardHistoryEvidence? FinalEligibility;
        public int ReadErrorAt;
        public Exception? ReadError;
        public Task<PrivacyClipboardHistoryEvidence> ReadAsync(CancellationToken token)
        {
            Reads++;
            if (Reads == ReadErrorAt && ReadError is not null)
                return Task.FromException<PrivacyClipboardHistoryEvidence>(ReadError);
            if (Writes.Count == 0)
                return Task.FromResult(Reads > 1 && FinalEligibility is not null ? FinalEligibility : values.Peek());
            return Task.FromResult(values.Peek());
        }
        public Task<ToolResult> WriteUserValueAsync(bool? enabled, CancellationToken token)
        {
            Writes.Add(enabled);
            if (values.Count > 1) values.Dequeue();
            if (CancelDuringWrite) throw new OperationCanceledException(token);
            return Task.FromResult(Result);
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
