using KillerBench.Engine;
using System.IO;

internal static class DefaultPrinterSelectionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var missing = State(["Office", "Warehouse"], [], mode: 1);
        var selected = State(["Office", "Warehouse"], ["Office"], mode: 1);
        var missingCheck = DefaultPrinterSelection.Assess(missing);
        Require(missingCheck is { State: CheckState.Repairable, Repair: RepairKind.DefaultPrinter } &&
            missingCheck.Detail.Contains("one exact installed queue", StringComparison.Ordinal) && missingCheck.Evidence is not null,
            "Missing default evidence exposes exact queue selection through the option-dependent recipe");
        var conflictingCheck = DefaultPrinterSelection.Assess(State(["Office", "Warehouse"], ["Office", "Warehouse"]));
        Require(conflictingCheck is { State: CheckState.Repairable, Repair: RepairKind.DefaultPrinter },
            "Conflicting defaults expose the guarded selection repair");
        Require(DefaultPrinterSelection.Assess(selected) is { State: CheckState.Information, Repair: null } &&
            DefaultPrinterSelection.Assess(State([], [])) is { State: CheckState.Information, Repair: null } &&
            DefaultPrinterSelection.Assess(new(false, ["Office"], [], false, null)) is { State: CheckState.Unknown, Repair: null },
            "Coherent, empty and incomplete printer inventories remain distinct without inventing a repair");

        string previewFolder = Folder(scratch, "default-printer-preview");
        var previewHost = new Host(missing, selected);
        var preview = await DefaultPrinterSelection.RunAsync("Office", false, previewFolder, previewHost);
        Require(preview.Outcome == DefaultPrinterSelectionOutcome.Preview && previewHost.Inspections == 0 &&
            previewHost.Commands == 0 && !Directory.Exists(previewFolder), "Preview is side-effect free");

        foreach (string invalid in new[] { "", " Office", "Office ", "Office\0Queue" })
            try
            {
                await DefaultPrinterSelection.RunAsync(invalid, false, previewFolder, previewHost);
                throw new Exception("Invalid default-printer target was accepted");
            }
            catch (ArgumentException) { }

        string verifiedFolder = Folder(scratch, "default-printer-verified");
        var verifiedHost = new Host(missing, selected);
        var verified = await DefaultPrinterSelection.RunAsync("Office", true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == DefaultPrinterSelectionOutcome.Verified && verifiedHost.Inspections == 4 &&
            verifiedHost.Commands == 1 && !File.Exists(Path.Combine(verifiedFolder, "default-printer.pending.json")) &&
            Directory.GetFiles(verifiedFolder, "*.prepared.json").Length == 1,
            "Verified selection uses two prechecks, one command, exact postcheck and archived preparation");
        Require(verified.Before?.LegacyDefaultPrinterMode == 1 && verified.After?.LegacyDefaultPrinterMode == 1,
            "Automatic-management evidence is preserved");

        foreach (var blockedState in new[]
        {
            State(["Warehouse"], []),
            State(["Office", "Warehouse"], ["Office"]),
            State(["Office", "Warehouse"], ["Warehouse"]),
            new DefaultPrinterState(false, ["Office"], [], false, null),
            State(["Office", "office"], [])
        })
        {
            string folder = Folder(scratch, "default-printer-blocked");
            var host = new Host(blockedState, selected);
            var result = await DefaultPrinterSelection.RunAsync("Office", true, folder, host);
            Require(result.Outcome == DefaultPrinterSelectionOutcome.Blocked && host.Commands == 0,
                "Ineligible or incomplete state cannot select a default");
        }

        string changedFolder = Folder(scratch, "default-printer-changed");
        var changedHost = new Host(missing, selected) { ChangeSecondInspection = true };
        var changed = await DefaultPrinterSelection.RunAsync("Office", true, changedFolder, changedHost);
        Require(changed.Outcome == DefaultPrinterSelectionOutcome.Blocked && changedHost.Commands == 0,
            "Changing preflight evidence blocks the command");

        string finalChangedFolder = Folder(scratch, "default-printer-final-changed");
        var finalChangedHost = new Host(missing, selected) { ChangeFinalInspection = true };
        var finalChanged = await DefaultPrinterSelection.RunAsync("Office", true, finalChangedFolder, finalChangedHost);
        Require(finalChanged.Outcome == DefaultPrinterSelectionOutcome.Blocked && finalChangedHost.Commands == 0 &&
            !File.Exists(Path.Combine(finalChangedFolder, "default-printer.pending.json")) &&
            Directory.GetFiles(finalChangedFolder, "*.prepared.json").Length == 1,
            "Changing final queue or management evidence closes the no-command guard");

        string finalFailureFolder = Folder(scratch, "default-printer-final-failure");
        var finalFailureHost = new Host(missing, selected) { FailFinalInspection = true };
        var finalFailure = await DefaultPrinterSelection.RunAsync("Office", true, finalFailureFolder, finalFailureHost);
        Require(finalFailure.Outcome == DefaultPrinterSelectionOutcome.Blocked && finalFailureHost.Commands == 0 &&
            !File.Exists(Path.Combine(finalFailureFolder, "default-printer.pending.json")) &&
            Directory.GetFiles(finalFailureFolder, "*.prepared.json").Length == 1,
            "Unavailable final default-printer evidence closes the no-command guard");

        string uncertainFolder = Folder(scratch, "default-printer-uncertain");
        var uncertainHost = new Host(missing, selected) { CommandResult = new(5, "", "denied", false) };
        var uncertain = await DefaultPrinterSelection.RunAsync("Office", true, uncertainFolder, uncertainHost);
        Require(uncertain.Outcome == DefaultPrinterSelectionOutcome.Uncertain && uncertainHost.Commands == 1 &&
            File.Exists(Path.Combine(uncertainFolder, "default-printer.pending.json")),
            "Unconfirmed command retains the no-repeat guard");
        var blockedRepeat = await DefaultPrinterSelection.RunAsync("Office", true, uncertainFolder, uncertainHost);
        Require(blockedRepeat.Outcome == DefaultPrinterSelectionOutcome.Blocked && uncertainHost.Commands == 1,
            "Pending selection cannot repeat the command");

        uncertainHost.Current = selected;
        var recovered = await DefaultPrinterSelection.ReconcileAsync(uncertainFolder, uncertainHost);
        Require(recovered.Outcome == DefaultPrinterSelectionOutcome.Verified && uncertainHost.Commands == 1 &&
            !File.Exists(Path.Combine(uncertainFolder, "default-printer.pending.json")),
            "Reconciliation verifies current state without repeating the command");

        string notAppliedFolder = Folder(scratch, "default-printer-not-applied");
        var notAppliedHost = new Host(missing, selected) { CommandResult = new(5, "", "failed", false) };
        await DefaultPrinterSelection.RunAsync("Office", true, notAppliedFolder, notAppliedHost);
        notAppliedHost.Current = missing;
        var notApplied = await DefaultPrinterSelection.ReconcileAsync(notAppliedFolder, notAppliedHost);
        Require(notApplied.Outcome == DefaultPrinterSelectionOutcome.NotApplied && notAppliedHost.Commands == 1 &&
            !File.Exists(Path.Combine(notAppliedFolder, "default-printer.pending.json")),
            "Exact prior state resolves as not applied without retrying");

        string retainedFolder = Folder(scratch, "default-printer-retained");
        var retainedHost = new Host(missing, selected) { CommandResult = new(5, "", "failed", false) };
        await DefaultPrinterSelection.RunAsync("Office", true, retainedFolder, retainedHost);
        retainedHost.Current = State(["Office", "Warehouse"], ["Office"], mode: 0);
        var retained = await DefaultPrinterSelection.ReconcileAsync(retainedFolder, retainedHost);
        Require(retained.Outcome == DefaultPrinterSelectionOutcome.Uncertain && retainedHost.Commands == 1 &&
            File.Exists(Path.Combine(retainedFolder, "default-printer.pending.json")),
            "Changed automatic-management evidence retains the guard");

        string foreignFolder = Folder(scratch, "default-printer-foreign");
        var foreignSeed = new Host(missing, selected) { CommandResult = new(5, "", "failed", false) };
        await DefaultPrinterSelection.RunAsync("Office", true, foreignFolder, foreignSeed);
        string foreignPath = Path.Combine(foreignFolder, "default-printer.pending.json");
        string json = await File.ReadAllTextAsync(foreignPath);
        await File.WriteAllTextAsync(foreignPath, json.Replace(Environment.MachineName, "OTHER-MACHINE", StringComparison.OrdinalIgnoreCase));
        var foreignHost = new Host(missing, selected);
        var foreign = await DefaultPrinterSelection.ReconcileAsync(foreignFolder, foreignHost);
        Require(foreign.Outcome == DefaultPrinterSelectionOutcome.Uncertain && foreignHost.Inspections == 0 &&
            foreignHost.Commands == 0 && File.Exists(foreignPath),
            "Foreign-machine recovery is rejected before provider access");

        Console.WriteLine("PASS: default-printer repair uses exact identity, repeated eligibility, preserved management state, durable preparation, exact verification and no-repeat recovery; simulated provider only.");
    }

    private static DefaultPrinterState State(string[] queues, string[] defaults, int? mode = null) =>
        new(true, queues, defaults, mode is not null, mode);

    private static string Folder(string scratch, string name) => Path.Combine(scratch, name + "-" + Guid.NewGuid().ToString("N"));
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Host(DefaultPrinterState before, DefaultPrinterState after) : IDefaultPrinterSelectionHost
    {
        internal int Inspections;
        internal int Commands;
        internal bool ChangeSecondInspection;
        internal bool ChangeFinalInspection;
        internal bool FailFinalInspection;
        internal ToolResult CommandResult = new(0, "accepted", "", false);
        internal DefaultPrinterState? Current;

        public Task<DefaultPrinterState> InspectAsync(CancellationToken token)
        {
            Inspections++;
            if (Current is not null) return Task.FromResult(Current);
            if (ChangeSecondInspection && Inspections == 2)
                return Task.FromResult(before with { LegacyDefaultPrinterMode = 0 });
            if (FailFinalInspection && Inspections == 3)
                return Task.FromException<DefaultPrinterState>(new InvalidDataException("fixture final inspection failure"));
            if (ChangeFinalInspection && Inspections == 3)
                return Task.FromResult(before with { LegacyDefaultPrinterMode = 0 });
            return Task.FromResult(Commands > 0 ? after : before);
        }

        public Task<ToolResult> SetDefaultAsync(string queue, CancellationToken token)
        {
            Commands++;
            if (!string.Equals(queue, "Office", StringComparison.Ordinal)) throw new Exception("Exact queue identity changed");
            return Task.FromResult(CommandResult);
        }
    }
}
