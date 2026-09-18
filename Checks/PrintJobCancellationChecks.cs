using System.IO;
using KillerBench.Engine;

internal static class PrintJobCancellationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var target = new PrintJobSnapshot("Office", "Office, 42", 42, 512);
        var retained = new PrintJobSnapshot("Office", "Office, 43", 43, 8);
        var before = new PrintJobInventory(true, [target, retained]);
        var after = new PrintJobInventory(true, [retained]);
        var repairable = PrintJobCancellation.Assess(before);
        var clear = PrintJobCancellation.Assess(after);
        var unknown = PrintJobCancellation.Assess(new(false, [target]));
        Require(repairable.State == CheckState.Repairable && repairable.Repair == RepairKind.PrintJobCancellation &&
            clear.State == CheckState.Information && clear.Repair is null &&
            unknown.State == CheckState.Unknown && unknown.Repair is null,
            "Assessment exposes exact stuck-job eligibility only from complete inventory");
        Require(PrintJobCancellation.IsCandidate(target) &&
            !PrintJobCancellation.IsCandidate(target with { StatusMask = 2 | 16 }) &&
            !PrintJobCancellation.IsCandidate(target with { Name = "Other, 42" }),
            "Candidate projection uses the same exact identity and inactive-state boundary");
        var targetReview = PrintingTargetReview.Check(new("Office", "Office", 42),
            [new("Printer condition: Office", CheckState.Attention, "Queue reports an offline condition.")],
            [new("Printer queue attributes: Office", CheckState.Information, "Queue attributes are coherent.")], before);
        Require(targetReview is [
                { Name: "Selected printer queue", State: CheckState.Attention, Repair: null },
                { Name: "Selected print job", State: CheckState.Attention, Repair: null } jobReview
            ] && jobReview.Detail.Contains("separate guarded cancellation", StringComparison.Ordinal),
            "Exact queue and blocked-job selections produce read-only target reviews without repair authority");
        var staleReview = PrintingTargetReview.Check(new(null, "Office", 99), [], [], before);
        Require(staleReview is [{ State: CheckState.Attention, Repair: null }] &&
            staleReview[0].Detail.Contains("no longer visible", StringComparison.Ordinal),
            "A stale selected print job requests refreshed evidence without changing the queue");
        var incompleteReview = PrintingTargetReview.Check(new("Office", "Office", 42),
            [new("Local printer conditions", CheckState.Unknown, "Fixture incomplete")],
            [new("Local printer queue attributes", CheckState.Unknown, "Fixture incomplete")], new(false, [target]));
        Require(incompleteReview.Count == 2 && incompleteReview.All(item => item.State == CheckState.Unknown && item.Repair is null),
            "Incomplete queue and job evidence fails the exact-target review closed");
        var duplicateQueueReview = PrintingTargetReview.Check(new("Office", null, null),
            [
                new("Printer condition: Office", CheckState.Information, "First fixture row."),
                new("Printer condition: Office", CheckState.Attention, "Duplicate fixture row.")
            ],
            [new("Printer queue attributes: Office", CheckState.Information, "Queue attributes are coherent.")], null);
        Require(duplicateQueueReview is [{ State: CheckState.Unknown, Repair: null }] &&
            duplicateQueueReview[0].Detail.Contains("duplicate rows", StringComparison.Ordinal),
            "Duplicate exact queue evidence fails closed without throwing or exposing repair authority");

        string previewFolder = Folder(scratch, "print-job-preview");
        var previewHost = new Host(before, after);
        var preview = await PrintJobCancellation.RunAsync("Office", 42, false, previewFolder, previewHost);
        Require(preview.Outcome == PrintJobCancellationOutcome.Preview && previewHost.Inspections == 0 &&
            previewHost.Commands == 0 && !Directory.Exists(previewFolder), "Preview remains side-effect free");

        string verifiedFolder = Folder(scratch, "print-job-verified");
        var verifiedHost = new Host(before, after);
        var verified = await PrintJobCancellation.RunAsync("Office", 42, true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == PrintJobCancellationOutcome.Verified && verifiedHost.Inspections == 4 &&
            verifiedHost.Commands == 1 && !File.Exists(Path.Combine(verifiedFolder, "print-job-cancel.pending.json")) &&
            Directory.GetFiles(verifiedFolder, "*.prepared.json").Length == 1,
            "Verified cancellation uses three matching prechecks, one exact command and one exact postcheck");

        foreach (var blocked in new[]
        {
            new PrintJobInventory(false, [target]),
            new PrintJobInventory(true, [target with { StatusMask = 1 }]),
            new PrintJobInventory(true, [target with { StatusMask = 2 | 16 }]),
            new PrintJobInventory(true, [retained])
        })
        {
            var host = new Host(blocked, after);
            var result = await PrintJobCancellation.RunAsync("Office", 42, true,
                Folder(scratch, "print-job-blocked"), host);
            Require(result.Outcome == PrintJobCancellationOutcome.Blocked && host.Commands == 0,
                "Incomplete, ordinary, active or missing job evidence blocks cancellation");
        }

        string changedFolder = Folder(scratch, "print-job-changed");
        var changedHost = new Host(before, after) { ChangeSecondInspection = true };
        var changed = await PrintJobCancellation.RunAsync("Office", 42, true, changedFolder, changedHost);
        Require(changed.Outcome == PrintJobCancellationOutcome.Blocked && changedHost.Commands == 0,
            "Changed repeated evidence blocks cancellation");

        string failedFolder = Folder(scratch, "print-job-failed");
        var failedHost = new Host(before, after) { CommandResult = new(5, "", "denied", false) };
        var failed = await PrintJobCancellation.RunAsync("Office", 42, true, failedFolder, failedHost);
        Require(failed.Outcome == PrintJobCancellationOutcome.Uncertain && failedHost.Commands == 1 &&
            File.Exists(Path.Combine(failedFolder, "print-job-cancel.pending.json")),
            "Unconfirmed command retains the no-repeat guard");
        var blockedRepeat = await PrintJobCancellation.RunAsync("Office", 42, true, failedFolder, failedHost);
        Require(blockedRepeat.Outcome == PrintJobCancellationOutcome.Blocked && failedHost.Commands == 1,
            "Pending cancellation cannot repeat the command");
        failedHost.Current = after;
        var recovered = await PrintJobCancellation.ReconcileAsync(failedFolder, failedHost);
        Require(recovered.Outcome == PrintJobCancellationOutcome.Verified && failedHost.Commands == 1 &&
            !File.Exists(Path.Combine(failedFolder, "print-job-cancel.pending.json")),
            "Reconciliation verifies current inventory without repeating cancellation");

        string preservedFolder = Folder(scratch, "print-job-preservation");
        var preservationHost = new Host(before, new(true, []));
        var preservation = await PrintJobCancellation.RunAsync("Office", 42, true, preservedFolder, preservationHost);
        Require(preservation.Outcome == PrintJobCancellationOutcome.Uncertain &&
            File.Exists(Path.Combine(preservedFolder, "print-job-cancel.pending.json")),
            "Missing non-target job prevents a false preservation claim");

        string notAppliedFolder = Folder(scratch, "print-job-not-applied");
        var notAppliedHost = new Host(before, before);
        var notApplied = await PrintJobCancellation.RunAsync("Office", 42, true, notAppliedFolder, notAppliedHost);
        Require(notApplied.Outcome == PrintJobCancellationOutcome.NotApplied && notAppliedHost.Commands == 1 &&
            !File.Exists(Path.Combine(notAppliedFolder, "print-job-cancel.pending.json")),
            "Unchanged exact inventory resolves without a retry");

        Console.WriteLine("PASS: selected print-job cancellation requires repeated exact evidence, preserves other observed jobs, verifies removal and never repeats during reconciliation; simulated provider only.");
    }

    private static string Folder(string scratch, string name) =>
        Path.Combine(scratch, name + "-" + Guid.NewGuid().ToString("N"));

    private static void Require(bool value, string message)
    { if (!value) throw new Exception(message); }

    private sealed class Host(PrintJobInventory before, PrintJobInventory after) : IPrintJobCancellationHost
    {
        internal int Inspections;
        internal int Commands;
        internal bool ChangeSecondInspection;
        internal ToolResult CommandResult = new(0, "accepted", "", false);
        internal PrintJobInventory? Current;

        public Task<PrintJobInventory> InspectAsync(CancellationToken token)
        {
            Inspections++;
            if (Current is not null) return Task.FromResult(Current);
            if (ChangeSecondInspection && Inspections == 2)
                return Task.FromResult(new PrintJobInventory(true,
                    [before.Jobs[0] with { StatusMask = 2 }, before.Jobs[1]]));
            return Task.FromResult(Commands > 0 ? after : before);
        }

        public Task<ToolResult> CancelAsync(PrintJobSnapshot expected, CancellationToken token)
        {
            Commands++;
            if (expected.Name != "Office, 42" || expected.JobId != 42 || expected.StatusMask != 512)
                throw new Exception("Exact print-job identity changed");
            return Task.FromResult(CommandResult);
        }
    }
}
