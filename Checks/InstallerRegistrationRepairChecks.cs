using System.IO;
using KillerBench.Engine;

internal static class InstallerRegistrationRepairChecks
{
    internal static async Task RunAsync(string scratch)
    {
        var previewHost = new Host();
        var preview = await InstallerRegistrationRepair.RunAsync(false,
            Folder(scratch, "preview"), previewHost);
        Require(preview.Outcome == InstallerRegistrationRepairOutcome.Preview &&
            preview.GetExitCode() == 0 && previewHost.TotalCalls == 0 &&
            preview.Detail.Contains("without unregistering", StringComparison.Ordinal),
            "Installer registration preview performs no inspection and excludes unregister");

        var steps = InstallerRegistrationRepair.Steps();
        Require(steps.Count is 1 or 2 && steps.All(step => !step.Verification &&
            step.Arguments.Length == 1 && step.Arguments[0] == "/regserver") &&
            steps.All(step => !string.Join(" ", step.Arguments).Contains("unregister", StringComparison.OrdinalIgnoreCase)),
            "Installer registration uses bounded direct regserver commands only");
        Require(InstallerRegistrationRepair.IsEligible(Registration(CheckState.Attention)) &&
            !InstallerRegistrationRepair.IsEligible(Registration(CheckState.Information)) &&
            !InstallerRegistrationRepair.IsEligible(new("Unrelated attention", CheckState.Attention,
                "fixture", Evidence: CompleteEvidence())),
            "Desktop routing accepts only complete exact Installer registration damage");
        Require(InstallerRegistrationRepair.SelectAction(false, true,
                [Registration(CheckState.Attention)]) == InstallerRegistrationRepairAction.None &&
            InstallerRegistrationRepair.SelectAction(true, true,
                [Registration(CheckState.Attention)]) == InstallerRegistrationRepairAction.Reconcile &&
            InstallerRegistrationRepair.SelectAction(true, false,
                [Registration(CheckState.Attention)]) == InstallerRegistrationRepairAction.Repair &&
            InstallerRegistrationRepair.SelectAction(true, false,
                [Registration(CheckState.Information)]) == InstallerRegistrationRepairAction.None,
            "Desktop selection requires consent, prioritizes reconciliation and routes only exact damage");

        foreach (var host in new[]
        {
            new Host { ActivityState = CheckState.Attention },
            new Host { ActivityState = CheckState.Unknown },
            new Host { ActivityEvidence = CompleteEvidence() with { Truncated = true } },
            new Host { RestartState = CheckState.Attention },
            new Host { RestartState = CheckState.Unknown },
            new Host { RestartEvidence = CompleteEvidence() with { TimedOut = true } },
            new Host { ActivityEvidence = BusyActivityEvidence() },
            new Host { RestartEvidence = PendingRestartEvidence() },
            new Host { Files = EngineFiles(CheckState.Attention) },
            new Host { Files = EngineFiles(CheckState.Unknown) },
            new Host { Before = Registration(CheckState.Information) },
            new Host { Before = Registration(CheckState.Unknown) },
            new Host { Before = new("Unrelated attention", CheckState.Attention, "fixture", Evidence: CompleteEvidence()) },
            new Host { IsAdministrator = false }
        })
        {
            var result = await InstallerRegistrationRepair.RunAsync(true,
                Folder(scratch, "blocked-" + Guid.NewGuid().ToString("N")), host);
            Require(result.Outcome == InstallerRegistrationRepairOutcome.Blocked &&
                host.Registrations == 0,
                "Installer activity, restart, trusted files, exact defect and administrator prerequisites block registration");
        }

        string verifiedFolder = Folder(scratch, "verified");
        var verifiedHost = new Host
        {
            PendingPath = Path.Combine(verifiedFolder, "installer-registration-repair.pending.json")
        };
        var verified = await InstallerRegistrationRepair.RunAsync(true, verifiedFolder, verifiedHost);
        Require(verified.Outcome == InstallerRegistrationRepairOutcome.Verified &&
            verified.GetExitCode() == 0 && verifiedHost.Registrations == 1 &&
            verifiedHost.PendingObserved && verifiedHost.ActivityReads == 3 &&
            verifiedHost.RestartReads == 2 && verifiedHost.FileReads == 2 &&
            verifiedHost.RegistrationReads == 3 &&
            !File.Exists(verifiedHost.PendingPath) &&
            File.Exists(verified.JournalPath + ".prepared.json") &&
            File.ReadAllText(verified.JournalPath!).Contains("\"Stage\":\"Finished\"", StringComparison.Ordinal),
            "Eligible Installer registration is durably prepared before commands and exactly verified afterward");

        string becameHealthyFolder = Folder(scratch, "became-healthy");
        var becameHealthyHost = new Host { Immediate = Registration(CheckState.Information) };
        var becameHealthy = await InstallerRegistrationRepair.RunAsync(true, becameHealthyFolder, becameHealthyHost);
        Require(becameHealthy.Outcome == InstallerRegistrationRepairOutcome.Blocked &&
            becameHealthyHost.Registrations == 0 && becameHealthyHost.RegistrationReads == 2 &&
            becameHealthy.After?.State == CheckState.Information &&
            !File.Exists(Path.Combine(becameHealthyFolder, "installer-registration-repair.pending.json")) &&
            File.Exists(becameHealthy.JournalPath + ".prepared.json") &&
            becameHealthy.Detail.Contains("No registration command", StringComparison.Ordinal),
            "Registration that becomes healthy after preparation records a no-command outcome and closes the guard");

        foreach (var fixture in new[]
        {
            (Error: (Exception)new IOException("final activity unavailable"), Outcome: InstallerRegistrationRepairOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final activity canceled"), Outcome: InstallerRegistrationRepairOutcome.Stopped)
        })
        {
            string folder = Folder(scratch, "final-unavailable-" + Guid.NewGuid().ToString("N"));
            var host = new Host { FinalActivityError = fixture.Error };
            var result = await InstallerRegistrationRepair.RunAsync(true, folder, host);
            Require(result.Outcome == fixture.Outcome && host.Registrations == 0 &&
                !File.Exists(Path.Combine(folder, "installer-registration-repair.pending.json")) &&
                File.Exists(result.JournalPath + ".prepared.json") &&
                result.Detail.Contains("No registration command was requested", StringComparison.Ordinal),
                "Unavailable or canceled final Installer evidence records an exact no-command outcome");
        }

        foreach (var fixture in new[]
        {
            (Commands: new Func<IReadOnlyList<RepairStep>, IReadOnlyList<ToolResult>>(s =>
                Results(s, new(3010, "restart", "", false))), Outcome: InstallerRegistrationRepairOutcome.RestartRequired, Exit: 4),
            (Commands: new Func<IReadOnlyList<RepairStep>, IReadOnlyList<ToolResult>>(s =>
                Results(s, new(1603, "failed", "", false))), Outcome: InstallerRegistrationRepairOutcome.Failed, Exit: 3),
            (Commands: new Func<IReadOnlyList<RepairStep>, IReadOnlyList<ToolResult>>(s =>
                Results(s, new(0, "partial", "", true))), Outcome: InstallerRegistrationRepairOutcome.Uncertain, Exit: 3),
            (Commands: new Func<IReadOnlyList<RepairStep>, IReadOnlyList<ToolResult>>(_ => []),
                Outcome: InstallerRegistrationRepairOutcome.Uncertain, Exit: 3)
        })
        {
            string folder = Folder(scratch, "command-" + Guid.NewGuid().ToString("N"));
            var host = new Host { CommandFactory = fixture.Commands };
            var result = await InstallerRegistrationRepair.RunAsync(true, folder, host);
            Require(result.Outcome == fixture.Outcome && result.GetExitCode() == fixture.Exit &&
                File.Exists(Path.Combine(folder, "installer-registration-repair.pending.json")),
                "Restart, failure and incomplete command evidence retain no-repeat registration state");
        }

        string canceledFolder = Folder(scratch, "command-canceled");
        var canceledHost = new Host
        {
            RegistrationTask = Task.FromCanceled<IReadOnlyList<ToolResult>>(new(true))
        };
        var canceled = await InstallerRegistrationRepair.RunAsync(true, canceledFolder, canceledHost);
        Require(canceled.Outcome == InstallerRegistrationRepairOutcome.Stopped && canceledHost.Registrations == 1 &&
            File.Exists(Path.Combine(canceledFolder, "installer-registration-repair.pending.json")),
            "Canceled Installer registration after dispatch retains pending reconciliation");

        foreach (var host in new[]
        {
            new Host { After = Registration(CheckState.Attention) },
            new Host { VerificationActivityState = CheckState.Attention },
            new Host { VerificationActivityEvidence = CompleteEvidence() with { Truncated = true } }
        })
        {
            string folder = Folder(scratch, "unverified-" + Guid.NewGuid().ToString("N"));
            var result = await InstallerRegistrationRepair.RunAsync(true, folder, host);
            Require(result.Outcome == InstallerRegistrationRepairOutcome.Uncertain &&
                host.Registrations == 1 &&
                File.Exists(Path.Combine(folder, "installer-registration-repair.pending.json")),
                "Command success alone cannot verify registration or idle Installer activity");
        }

        string pendingFolder = Folder(scratch, "existing-pending");
        Directory.CreateDirectory(pendingFolder);
        await File.WriteAllTextAsync(Path.Combine(pendingFolder,
            "installer-registration-repair.pending.json"), "fixture");
        var pendingHost = new Host();
        var pending = await InstallerRegistrationRepair.RunAsync(true, pendingFolder, pendingHost);
        Require(pending.Outcome == InstallerRegistrationRepairOutcome.Blocked && pendingHost.TotalCalls == 0,
            "Existing registration repair state blocks inspection and another command");

        string recoveredFolder = Folder(scratch, "reconcile-verified");
        await InstallerRegistrationRepair.RunAsync(true, recoveredFolder,
            new Host { CommandFactory = s => Results(s, new(1603, "failed", "", false)) });
        var recoveredHost = new Host { Before = Registration(CheckState.Information) };
        var recovered = await InstallerRegistrationRepairReconciliation.RunAsync(recoveredFolder, recoveredHost);
        Require(recovered.Outcome == InstallerRegistrationRepairReconciliationOutcome.Verified &&
            recovered.GetExitCode() == 0 && recoveredHost.Registrations == 0 &&
            !File.Exists(Path.Combine(recoveredFolder, "installer-registration-repair.pending.json")) &&
            File.Exists(recovered.ArchivePath) && File.Exists(recovered.ArchivePath + ".prepared.json"),
            "Reconciliation verifies and archives exact registration without repeating commands");

        string notRepairedFolder = Folder(scratch, "reconcile-not-repaired");
        await InstallerRegistrationRepair.RunAsync(true, notRepairedFolder,
            new Host { CommandFactory = s => Results(s, new(1603, "failed", "", false)) });
        var notRepairedHost = new Host();
        var notRepaired = await InstallerRegistrationRepairReconciliation.RunAsync(notRepairedFolder, notRepairedHost);
        Require(notRepaired.Outcome == InstallerRegistrationRepairReconciliationOutcome.NotRepaired &&
            notRepairedHost.Registrations == 0 &&
            !File.Exists(Path.Combine(notRepairedFolder, "installer-registration-repair.pending.json")),
            "Complete unrepaired registration is archived without repeating commands or claiming success");

        string restartFolder = Folder(scratch, "reconcile-restart");
        await InstallerRegistrationRepair.RunAsync(true, restartFolder,
            new Host { CommandFactory = s => Results(s, new(3010, "restart", "", false)) });
        var restartHost = new Host { RestartState = CheckState.Attention, RestartEvidence = PendingRestartEvidence() };
        var restart = await InstallerRegistrationRepairReconciliation.RunAsync(restartFolder, restartHost);
        Require(restart.Outcome == InstallerRegistrationRepairReconciliationOutcome.RestartRequired &&
            restartHost.RegistrationReads == 0 && restartHost.Registrations == 0 &&
            File.Exists(Path.Combine(restartFolder, "installer-registration-repair.pending.json")),
            "Pending restart retains registration guard without inspection or command repetition");

        string busyEvidenceFolder = Folder(scratch, "reconcile-busy-raw-evidence");
        await InstallerRegistrationRepair.RunAsync(true, busyEvidenceFolder,
            new Host { CommandFactory = s => Results(s, new(1603, "failed", "", false)) });
        var busyEvidenceHost = new Host { ActivityEvidence = BusyActivityEvidence() };
        var busyEvidence = await InstallerRegistrationRepairReconciliation.RunAsync(busyEvidenceFolder, busyEvidenceHost);
        Require(busyEvidence.Outcome == InstallerRegistrationRepairReconciliationOutcome.Inconclusive &&
            busyEvidenceHost.RegistrationReads == 0 && busyEvidenceHost.Registrations == 0 &&
            File.Exists(Path.Combine(busyEvidenceFolder, "installer-registration-repair.pending.json")),
            "Raw Installer activity prevents registration reconciliation despite an idle host label");

        string pendingEvidenceFolder = Folder(scratch, "reconcile-pending-raw-evidence");
        await InstallerRegistrationRepair.RunAsync(true, pendingEvidenceFolder,
            new Host { CommandFactory = s => Results(s, new(1603, "failed", "", false)) });
        var pendingEvidenceHost = new Host { RestartEvidence = PendingRestartEvidence() };
        var pendingEvidence = await InstallerRegistrationRepairReconciliation.RunAsync(pendingEvidenceFolder, pendingEvidenceHost);
        Require(pendingEvidence.Outcome == InstallerRegistrationRepairReconciliationOutcome.RestartRequired &&
            pendingEvidenceHost.RegistrationReads == 0 && pendingEvidenceHost.Registrations == 0 &&
            File.Exists(Path.Combine(pendingEvidenceFolder, "installer-registration-repair.pending.json")),
            "Raw pending-restart evidence retains registration reconciliation despite a clear host label");

        string malformedFolder = Folder(scratch, "reconcile-malformed");
        Directory.CreateDirectory(malformedFolder);
        string malformedPath = Path.Combine(malformedFolder, "installer-registration-repair.pending.json");
        await File.WriteAllTextAsync(malformedPath, "{\"schema\":1,\"schema\":2}");
        var malformedHost = new Host();
        var malformed = await InstallerRegistrationRepairReconciliation.RunAsync(malformedFolder, malformedHost);
        Require(malformed.Outcome == InstallerRegistrationRepairReconciliationOutcome.Inconclusive &&
            malformedHost.TotalCalls == 0 && File.Exists(malformedPath),
            "Malformed registration repair state remains guarded before provider inspection");

        Console.WriteLine("PASS: Installer registration preview, exact eligibility, durable preparation, bounded outcomes, exact verification and no-repeat reconciliation; simulated provider only.");
    }

    private static WorkflowCheck Registration(CheckState state) => state switch
    {
        CheckState.Information => InstallerServiceRegistrationDiagnostics.Assess(
            new(true, true, true, true, true, true, true, 3, 16, 1)),
        CheckState.Attention => InstallerServiceRegistrationDiagnostics.Assess(
            new(true, true, true, false, true, true, true, 3, 16, 1)),
        _ => InstallerServiceRegistrationDiagnostics.Assess(
            new(false, true, true, true, true, true, true, 3, 16, 1))
    };

    private static WorkflowCheck EngineFiles(CheckState state)
    {
        string signature = state == CheckState.Attention ? "HashMismatch" : "Valid";
        bool complete = state != CheckState.Unknown;
        string json = "{\"complete\":" + complete.ToString().ToLowerInvariant() +
            ",\"files\":[{\"architecture\":\"Native\",\"file\":\"msiexec.exe\",\"exists\":true,\"signature\":\"" + signature +
            "\"},{\"architecture\":\"Native\",\"file\":\"msi.dll\",\"exists\":true,\"signature\":\"Valid\"},{\"architecture\":\"Native\",\"file\":\"msihnd.dll\",\"exists\":true,\"signature\":\"Valid\"}]}";
        return InstallerEngineFileDiagnostics.Interpret(new(0, json, "", false));
    }

    private static ToolResult CompleteEvidence() => new(0, "{}", "", false);
    private static IReadOnlyList<ToolResult> Results(IReadOnlyList<RepairStep> steps, ToolResult result) =>
        Enumerable.Repeat(result, steps.Count).ToArray();
    private static string Folder(string scratch, string name) =>
        Path.Combine(scratch, "installer-registration-repair-" + name);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class Host : IInstallerRegistrationRepairHost
    {
        public bool IsAdministrator { get; set; } = true;
        public CheckState ActivityState { get; set; } = CheckState.Information;
        public CheckState VerificationActivityState { get; set; } = CheckState.Information;
        public CheckState RestartState { get; set; } = CheckState.Clear;
        public ToolResult ActivityEvidence { get; set; } = IdleActivityEvidence();
        public ToolResult VerificationActivityEvidence { get; set; } = IdleActivityEvidence();
        public ToolResult RestartEvidence { get; set; } = ClearRestartEvidence();
        public WorkflowCheck Files { get; set; } = EngineFiles(CheckState.Information);
        public WorkflowCheck Before { get; set; } = Registration(CheckState.Attention);
        public WorkflowCheck Immediate { get; set; } = Registration(CheckState.Attention);
        public WorkflowCheck After { get; set; } = Registration(CheckState.Information);
        public Func<IReadOnlyList<RepairStep>, IReadOnlyList<ToolResult>> CommandFactory { get; set; } =
            steps => Results(steps, new(0, "success", "", false));
        public Exception? FinalActivityError { get; set; }
        public Task<IReadOnlyList<ToolResult>>? RegistrationTask { get; set; }
        public string? PendingPath { get; set; }
        public bool PendingObserved { get; private set; }
        public int ActivityReads, RestartReads, FileReads, RegistrationReads, Registrations;
        public int TotalCalls => ActivityReads + RestartReads + FileReads + RegistrationReads + Registrations;

        public Task<WorkflowCheck> ReadActivityAsync(CancellationToken token)
        {
            ActivityReads++;
            if (ActivityReads == 2 && FinalActivityError is not null)
                return Task.FromException<WorkflowCheck>(FinalActivityError);
            return Task.FromResult(new WorkflowCheck("Installer activity",
                ActivityReads < 3 ? ActivityState : VerificationActivityState, "fixture",
                Evidence: ActivityReads < 3 ? ActivityEvidence : VerificationActivityEvidence));
        }

        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
        {
            RestartReads++;
            return Task.FromResult(new WorkflowCheck("Restart", RestartState, "fixture",
                Evidence: RestartEvidence));
        }

        public Task<WorkflowCheck> ReadEngineFilesAsync(CancellationToken token)
        {
            FileReads++;
            return Task.FromResult(Files);
        }

        public Task<WorkflowCheck> ReadRegistrationAsync(CancellationToken token)
        {
            RegistrationReads++;
            return Task.FromResult(RegistrationReads switch { 1 => Before, 2 => Immediate, _ => After });
        }

        public Task<IReadOnlyList<ToolResult>> RegisterAsync(IReadOnlyList<RepairStep> steps,
            CancellationToken token)
        {
            Registrations++;
            PendingObserved = PendingPath is null || File.Exists(PendingPath);
            return RegistrationTask ?? Task.FromResult(CommandFactory(steps));
        }
    }

    private static ToolResult IdleActivityEvidence() =>
        new(0, "{\"complete\":true,\"marker\":false,\"processes\":[]}", "", false);

    private static ToolResult BusyActivityEvidence() =>
        new(0, "{\"complete\":true,\"marker\":true,\"processes\":[]}", "", false);

    private static ToolResult ClearRestartEvidence() => RestartEvidence(false);
    private static ToolResult PendingRestartEvidence() => RestartEvidence(true);

    private static ToolResult RestartEvidence(bool pending) => new(0,
        System.Text.Json.JsonSerializer.Serialize(new RestartSnapshot(true, pending, false, false, false, false,
            "TEST-PC", "TEST-PC"), Capture.JsonOptions), "", false);
}
