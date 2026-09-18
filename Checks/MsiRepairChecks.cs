using System.IO;
using KillerBench.Engine;

internal static class MsiRepairChecks
{
    private const string Product = "{11111111-2222-3333-4444-555555555555}";

    internal static async Task RunAsync(string scratch)
    {
        var previewHost = new Host();
        var preview = await MsiRepair.RunAsync(Product.ToLowerInvariant(), false, Folder(scratch, "preview"), previewHost);
        Require(preview.Outcome == MsiRepairOutcome.Preview && preview.GetExitCode() == 0 && previewHost.TotalCalls == 0 &&
            preview.ProductCode == Product && preview.Detail.Contains("vendor package"),
            "Preview normalizes the exact product and explains package effects without inspection");

        string command = string.Join(" ", MsiRepair.Step(Product, Path.Combine(scratch, "fixture.log")).Arguments);
        Require(command.Contains("/fa " + Product, StringComparison.Ordinal) && command.Contains("/qn", StringComparison.Ordinal) &&
            command.Contains("/norestart", StringComparison.Ordinal) && command.Contains("/L*V", StringComparison.Ordinal),
            "MSI repair plan uses exact full-file repair, quiet mode, restart suppression and verbose logging");
        foreach (int state in new[] { -6, 0 })
        {
            var eligible = MsiRepair.AssessEligibility(Product, MsiProductDiagnostics.Interpret(Product,
                new(0, "{\"productCode\":\"" + Product + "\",\"state\":" + state + "}", "", false)));
            Require(eligible.State == CheckState.Repairable && eligible.Repair == RepairKind.MsiProduct,
                "Broken and bad-configuration MSI product evidence exposes guarded exact-product repair");
        }
        var healthy = MsiProductDiagnostics.Interpret(Product,
            new(0, "{\"productCode\":\"" + Product + "\",\"state\":5}", "", false));
        Require(MsiRepair.AssessEligibility(Product, healthy) == healthy,
            "Healthy MSI product evidence remains informational and does not select repair");
        var eligibleReview = MsiRepairReviewDiagnostics.Check(Product,
            ProductEvidence(0), ActivityEvidence(IdleActivityEvidence()), RestartCheck(ClearRestartEvidence()), true);
        Require(eligibleReview is { State: CheckState.Attention, Repair: null } &&
            eligibleReview.Detail.Contains("separate guarded repair", StringComparison.Ordinal) &&
            eligibleReview.Detail.Contains("Vendor custom actions", StringComparison.Ordinal),
            "Exact eligible product review explains the guarded boundary without authorizing repair");
        var healthyReview = MsiRepairReviewDiagnostics.Check(Product,
            ProductEvidence(5), ActivityEvidence(IdleActivityEvidence()), RestartCheck(ClearRestartEvidence()), true);
        Require(healthyReview is { State: CheckState.Information, Repair: null } &&
            healthyReview.Detail.Contains("outside the guarded", StringComparison.Ordinal),
            "Healthy selected product review withholds repair");
        foreach (var review in new[]
        {
            MsiRepairReviewDiagnostics.Check(Product, ProductEvidence(0),
                ActivityEvidence(BusyActivityEvidence()), RestartCheck(ClearRestartEvidence()), true),
            MsiRepairReviewDiagnostics.Check(Product, ProductEvidence(-6),
                ActivityEvidence(IdleActivityEvidence()), RestartCheck(PendingRestartEvidence()), true),
            MsiRepairReviewDiagnostics.Check(Product, ProductEvidence(0),
                ActivityEvidence(IdleActivityEvidence()), RestartCheck(ClearRestartEvidence()), false)
        })
            Require(review is { State: CheckState.Attention, Repair: null } &&
                review.Detail.Contains("currently blocked", StringComparison.Ordinal),
                "Busy Installer, pending restart and standard-user evidence block selected-product review");
        var incompleteReview = MsiRepairReviewDiagnostics.Check(Product,
            ProductEvidence(0), new("Installer activity", CheckState.Unknown, "fixture",
                Evidence: new(0, "{}", "", false)), RestartCheck(ClearRestartEvidence()), true);
        Require(incompleteReview is { State: CheckState.Unknown, Repair: null },
            "Incomplete selected-product prerequisite evidence fails closed");
        try
        {
            MsiRepairReviewDiagnostics.Check(Product, ProductEvidence(0), ActivityEvidence(IdleActivityEvidence()),
                RestartCheck(ClearRestartEvidence()), true, new CancellationToken(true));
            throw new Exception("Canceled selected MSI repair review completed unexpectedly");
        }
        catch (OperationCanceledException) { }

        foreach (int state in new[] { -7, -5, -4, -3, -2, -1, 1, 2, 3, 4, 5 })
        {
            var host = new Host { BeforeState = state };
            var result = await MsiRepair.RunAsync(Product, true, Folder(scratch, "state-" + state), host);
            Require(result.Outcome == MsiRepairOutcome.Blocked && host.Repairs == 0 && host.ProductReads == 1,
                "Only broken and bad-configuration product states reach MSI repair");
        }

        foreach (var blocked in new[]
        {
            new Host { ActivityState = CheckState.Attention },
            new Host { ActivityState = CheckState.Unknown },
            new Host { RestartState = CheckState.Attention },
            new Host { RestartState = CheckState.Unknown },
            new Host { ActivityEvidence = new(0, "{}", "", false) { Truncated = true } },
            new Host { ActivityEvidence = new(0, "{}", "provider error", false) },
            new Host { RestartEvidence = new(0, "{}", "", true) },
            new Host { RestartEvidence = new(5, "{}", "", false) },
            new Host { ActivityEvidence = BusyActivityEvidence() },
            new Host { RestartEvidence = PendingRestartEvidence() },
            new Host { IsAdministrator = false }
        })
        {
            var result = await MsiRepair.RunAsync(Product, true, Folder(scratch, "blocked-" + Guid.NewGuid().ToString("N")), blocked);
            Require(result.Outcome == MsiRepairOutcome.Blocked && blocked.Repairs == 0,
                "Activity, restart and administrator prerequisites block mutation");
        }

        foreach (int eligible in new[] { -6, 0 })
        {
            var host = new Host { BeforeState = eligible, AfterState = 5 };
            var progress = new Observer();
            string folder = Folder(scratch, "verified-" + eligible);
            var result = await MsiRepair.RunAsync(Product, true, folder, host, progress);
            Require(result.Outcome == MsiRepairOutcome.Verified && result.GetExitCode() == 0 && host.Repairs == 1 &&
                host.ProductReads == 3 && host.ActivityReads == 3 && host.RestartReads == 2 && progress.Messages.Count == 7 &&
                !File.Exists(Path.Combine(folder, "msi-repair.pending.json")) && File.Exists(result.JournalPath + ".prepared.json") &&
                File.ReadAllText(result.JournalPath!).Contains("\"Stage\":\"Finished\"", StringComparison.Ordinal),
                "Eligible exact product is prepared, repaired, verified and durably completed");
        }

        string becameHealthyFolder = Folder(scratch, "became-healthy");
        var becameHealthyHost = new Host { ImmediateState = 5 };
        var becameHealthy = await MsiRepair.RunAsync(Product, true, becameHealthyFolder, becameHealthyHost);
        Require(becameHealthy.Outcome == MsiRepairOutcome.Blocked && becameHealthyHost.Repairs == 0 &&
            becameHealthyHost.ProductReads == 2 && becameHealthy.After is not null &&
            !File.Exists(Path.Combine(becameHealthyFolder, "msi-repair.pending.json")) &&
            File.Exists(becameHealthy.JournalPath + ".prepared.json") &&
            becameHealthy.Detail.Contains("No package repair", StringComparison.Ordinal),
            "An exact product that becomes installed after preparation records no command and closes the guard");

        foreach (var fixture in new[]
        {
            (Error: (Exception)new IOException("final activity unavailable"), Outcome: MsiRepairOutcome.Blocked),
            (Error: (Exception)new OperationCanceledException("final activity canceled"), Outcome: MsiRepairOutcome.Stopped)
        })
        {
            string folder = Folder(scratch, "final-unavailable-" + Guid.NewGuid().ToString("N"));
            var host = new Host { FinalActivityError = fixture.Error };
            var result = await MsiRepair.RunAsync(Product, true, folder, host);
            Require(result.Outcome == fixture.Outcome && host.Repairs == 0 &&
                !File.Exists(Path.Combine(folder, "msi-repair.pending.json")) &&
                File.Exists(result.JournalPath + ".prepared.json") &&
                result.Detail.Contains("No package repair was requested", StringComparison.Ordinal),
                "Unavailable or canceled final MSI evidence records an exact no-command outcome");
        }

        foreach (var pair in new[]
        {
            (new ToolResult(3010, "fixture", "", false), MsiRepairOutcome.RestartRequired, 4),
            (new ToolResult(1603, "fixture", "", false), MsiRepairOutcome.Failed, 3),
            (new ToolResult(0, "fixture", "", true), MsiRepairOutcome.Uncertain, 3)
        })
        {
            string folder = Folder(scratch, "command-" + pair.Item1.ExitCode + "-" + pair.Item1.Truncated);
            var host = new Host { Command = pair.Item1 };
            var result = await MsiRepair.RunAsync(Product, true, folder, host);
            Require(result.Outcome == pair.Item2 && result.GetExitCode() == pair.Item3 && host.ProductReads == 2 &&
                File.Exists(Path.Combine(folder, "msi-repair.pending.json")),
                "Restart, failed and incomplete command evidence retain pending reconciliation");
        }

        string stalledFolder = Folder(scratch, "stalled-provider");
        var stalledHost = new Host { RepairTask = new TaskCompletionSource<ToolResult>().Task };
        var stalled = await MsiRepair.RunAsync(Product, true, stalledFolder, stalledHost,
            executionTimeout: TimeSpan.FromMilliseconds(20));
        Require(stalled.Outcome == MsiRepairOutcome.Uncertain && stalledHost.Repairs == 1 &&
            File.Exists(Path.Combine(stalledFolder, "msi-repair.pending.json")),
            "Coordinator bounds a stalled repair provider and retains pending reconciliation");

        string canceledFolder = Folder(scratch, "command-canceled");
        var canceledHost = new Host { RepairTask = Task.FromCanceled<ToolResult>(new(true)) };
        var canceled = await MsiRepair.RunAsync(Product, true, canceledFolder, canceledHost);
        Require(canceled.Outcome == MsiRepairOutcome.Stopped && canceledHost.Repairs == 1 &&
            File.Exists(Path.Combine(canceledFolder, "msi-repair.pending.json")),
            "Canceled MSI repair after dispatch retains pending reconciliation");

        foreach (var host in new[]
        {
            new Host { AfterState = 0 },
            new Host { VerificationActivityState = CheckState.Attention },
            new Host { VerificationActivityEvidence = new(0, "{}", "", false) { Truncated = true } }
        })
        {
            string folder = Folder(scratch, "unverified-" + Guid.NewGuid().ToString("N"));
            var result = await MsiRepair.RunAsync(Product, true, folder, host);
            Require(result.Outcome == MsiRepairOutcome.Uncertain && result.GetExitCode() == 3 && host.Repairs == 1 &&
                File.Exists(Path.Combine(folder, "msi-repair.pending.json")),
                "Success exit alone cannot verify product state or idle Installer activity");
        }

        string pendingFolder = Folder(scratch, "pending");
        Directory.CreateDirectory(pendingFolder);
        await File.WriteAllTextAsync(Path.Combine(pendingFolder, "msi-repair.pending.json"), "fixture");
        var pendingHost = new Host();
        var pendingResult = await MsiRepair.RunAsync(Product, true, pendingFolder, pendingHost);
        Require(pendingResult.Outcome == MsiRepairOutcome.Blocked && pendingHost.TotalCalls == 0,
            "Existing pending operation blocks inspection and another repair");

        string recoveredFolder = Folder(scratch, "reconcile-verified");
        await MsiRepair.RunAsync(Product, true, recoveredFolder, new Host { Command = new(1603, "", "fixture", false) });
        var recoveredHost = new Host { BeforeState = 5 };
        var recovered = await MsiRepairReconciliation.RunAsync(recoveredFolder, recoveredHost);
        Require(recovered.Outcome == MsiRepairReconciliationOutcome.Verified && recovered.GetExitCode() == 0 &&
            recoveredHost.Repairs == 0 && !File.Exists(Path.Combine(recoveredFolder, "msi-repair.pending.json")) &&
            File.Exists(recovered.ArchivePath) && File.Exists(recovered.ArchivePath + ".prepared.json"),
            "Reconciliation verifies and archives an exact installed product without repeating repair");

        string notRepairedFolder = Folder(scratch, "reconcile-failed");
        await MsiRepair.RunAsync(Product, true, notRepairedFolder, new Host { Command = new(1603, "", "fixture", false) });
        var notRepairedHost = new Host { BeforeState = 0 };
        var notRepaired = await MsiRepairReconciliation.RunAsync(notRepairedFolder, notRepairedHost);
        Require(notRepaired.Outcome == MsiRepairReconciliationOutcome.NotRepaired && notRepaired.GetExitCode() == 3 &&
            notRepairedHost.Repairs == 0 && !File.Exists(Path.Combine(notRepairedFolder, "msi-repair.pending.json")),
            "Complete unrepaired state is archived without claiming success or repeating repair");

        string restartFolder = Folder(scratch, "reconcile-restart");
        await MsiRepair.RunAsync(Product, true, restartFolder, new Host { Command = new(3010, "", "", false) });
        var restartHost = new Host { RestartState = CheckState.Attention, RestartEvidence = PendingRestartEvidence() };
        var restartReview = await MsiRepairReconciliation.RunAsync(restartFolder, restartHost);
        Require(restartReview.Outcome == MsiRepairReconciliationOutcome.RestartRequired && restartReview.GetExitCode() == 4 &&
            restartHost.ProductReads == 0 && restartHost.Repairs == 0 && File.Exists(Path.Combine(restartFolder, "msi-repair.pending.json")),
            "Pending restart retains the guard and skips product or repair actions");

        string inconclusiveFolder = Folder(scratch, "reconcile-inconclusive");
        await MsiRepair.RunAsync(Product, true, inconclusiveFolder, new Host { Command = new(1603, "", "fixture", false) });
        var inconclusiveHost = new Host { ActivityState = CheckState.Unknown };
        var inconclusive = await MsiRepairReconciliation.RunAsync(inconclusiveFolder, inconclusiveHost);
        Require(inconclusive.Outcome == MsiRepairReconciliationOutcome.Inconclusive && inconclusiveHost.Repairs == 0 &&
            File.Exists(Path.Combine(inconclusiveFolder, "msi-repair.pending.json")),
            "Incomplete reconciliation retains the guard and never repeats repair");

        string busyEvidenceFolder = Folder(scratch, "reconcile-busy-raw-evidence");
        await MsiRepair.RunAsync(Product, true, busyEvidenceFolder, new Host { Command = new(1603, "", "fixture", false) });
        var busyEvidenceHost = new Host { ActivityEvidence = BusyActivityEvidence() };
        var busyEvidence = await MsiRepairReconciliation.RunAsync(busyEvidenceFolder, busyEvidenceHost);
        Require(busyEvidence.Outcome == MsiRepairReconciliationOutcome.Inconclusive &&
            busyEvidenceHost.ProductReads == 0 && busyEvidenceHost.Repairs == 0 &&
            File.Exists(Path.Combine(busyEvidenceFolder, "msi-repair.pending.json")),
            "Raw Installer activity prevents reconciliation despite an idle host label");

        string pendingEvidenceFolder = Folder(scratch, "reconcile-pending-raw-evidence");
        await MsiRepair.RunAsync(Product, true, pendingEvidenceFolder, new Host { Command = new(1603, "", "fixture", false) });
        var pendingEvidenceHost = new Host { RestartEvidence = PendingRestartEvidence() };
        var pendingEvidence = await MsiRepairReconciliation.RunAsync(pendingEvidenceFolder, pendingEvidenceHost);
        Require(pendingEvidence.Outcome == MsiRepairReconciliationOutcome.RestartRequired &&
            pendingEvidenceHost.ProductReads == 0 && pendingEvidenceHost.Repairs == 0 &&
            File.Exists(Path.Combine(pendingEvidenceFolder, "msi-repair.pending.json")),
            "Raw pending-restart evidence retains reconciliation despite a clear host label");

        foreach (var host in new[]
        {
            new Host { ActivityEvidence = new(0, "{}", "", false) { Truncated = true } },
            new Host { RestartEvidence = new(0, "{}", "provider error", false) },
            new Host { RestartState = CheckState.Attention, RestartEvidence = new(0, "{}", "", true) }
        })
        {
            string folder = Folder(scratch, "reconcile-incomplete-evidence-" + Guid.NewGuid().ToString("N"));
            await MsiRepair.RunAsync(Product, true, folder, new Host { Command = new(1603, "", "fixture", false) });
            var review = await MsiRepairReconciliation.RunAsync(folder, host);
            Require(review.Outcome == MsiRepairReconciliationOutcome.Inconclusive && host.Repairs == 0 &&
                File.Exists(Path.Combine(folder, "msi-repair.pending.json")),
                "Incomplete activity or restart evidence cannot clear an MSI repair guard");
        }

        string foreignFolder = Folder(scratch, "reconcile-foreign-machine");
        await MsiRepair.RunAsync(Product, true, foreignFolder, new Host { Command = new(1603, "", "fixture", false) });
        string foreignPath = Path.Combine(foreignFolder, "msi-repair.pending.json");
        string foreignJson = await File.ReadAllTextAsync(foreignPath);
        await File.WriteAllTextAsync(foreignPath, foreignJson.Replace(Environment.MachineName, "OTHER-MACHINE",
            StringComparison.OrdinalIgnoreCase));
        var foreignHost = new Host();
        var foreign = await MsiRepairReconciliation.RunAsync(foreignFolder, foreignHost);
        Require(foreign.Outcome == MsiRepairReconciliationOutcome.Inconclusive && foreignHost.TotalCalls == 0 &&
            File.Exists(foreignPath), "A foreign-machine MSI record remains guarded before Installer inspection");

        Console.WriteLine("PASS: exact-product MSI repair preview, eligibility, activity/restart/admin gates, durable preparation, bounded outcomes, post-state verification and no-repeat reconciliation; simulated provider only.");
    }

    private static string Folder(string scratch, string name) => Path.Combine(scratch, "msi-repair-" + name);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Observer : IProgress<string>
    {
        public readonly List<string> Messages = [];
        public void Report(string value) => Messages.Add(value);
    }

    private sealed class Host : IMsiRepairHost
    {
        public bool IsAdministrator { get; set; } = true;
        public CheckState ActivityState { get; set; } = CheckState.Information;
        public CheckState VerificationActivityState { get; set; } = CheckState.Information;
        public CheckState RestartState { get; set; } = CheckState.Clear;
        public int BeforeState { get; set; } = 0;
        public int ImmediateState { get; set; } = 0;
        public int AfterState { get; set; } = 5;
        public ToolResult Command { get; set; } = new(0, "fixture success", "", false);
        public ToolResult ActivityEvidence { get; set; } = IdleActivityEvidence();
        public ToolResult VerificationActivityEvidence { get; set; } = IdleActivityEvidence();
        public ToolResult RestartEvidence { get; set; } = ClearRestartEvidence();
        public Exception? FinalActivityError { get; set; }
        public Task<ToolResult>? RepairTask { get; set; }
        public int ActivityReads, ProductReads, RestartReads, Repairs;
        public int TotalCalls => ActivityReads + ProductReads + RestartReads + Repairs;

        public Task<WorkflowCheck> ReadActivityAsync(CancellationToken token)
        {
            ActivityReads++;
            if (ActivityReads == 2 && FinalActivityError is not null)
                return Task.FromException<WorkflowCheck>(FinalActivityError);
            var state = ActivityReads < 3 ? ActivityState : VerificationActivityState;
            var evidence = ActivityReads < 3 ? ActivityEvidence : VerificationActivityEvidence;
            return Task.FromResult(new WorkflowCheck("Installer activity", state, "fixture", Evidence: evidence));
        }

        public Task<WorkflowCheck> ReadProductAsync(string productCode, CancellationToken token)
        {
            ProductReads++;
            int state = ProductReads switch { 1 => BeforeState, 2 => ImmediateState, _ => AfterState };
            return Task.FromResult(MsiProductDiagnostics.Interpret(productCode,
                new(0, "{\"productCode\":\"" + Product + "\",\"state\":" + state + "}", "", false)));
        }

        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
        {
            RestartReads++;
            return Task.FromResult(new WorkflowCheck("Restart", RestartState, "fixture", Evidence: RestartEvidence));
        }

        public Task<ToolResult> RepairAsync(string productCode, string logPath, CancellationToken token)
        {
            if (productCode != Product || !Path.GetFileName(logPath).StartsWith("msi-repair-", StringComparison.Ordinal))
                throw new Exception("MSI repair received the wrong target or log path.");
            Repairs++;
            return RepairTask ?? Task.FromResult(Command);
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

    private static WorkflowCheck ProductEvidence(int state) => MsiProductDiagnostics.Interpret(Product,
        new(0, "{\"productCode\":\"" + Product + "\",\"state\":" + state + "}", "", false));

    private static WorkflowCheck ActivityEvidence(ToolResult evidence) =>
        InstallerActivityDiagnostics.Interpret(evidence);

    private static WorkflowCheck RestartCheck(ToolResult evidence) =>
        RestartDiagnostics.Interpret(evidence);
}
