using KillerBench.Engine;
using System.IO;

internal static class RecipeChecks
{
    private static void CheckComputerNames()
    {
        const string snapshot = "{\"Name\":\"PC-1\",\"DomainRole\":1,\"PartOfDomain\":true}";
        Require(ComputerNameCollector.Interpret(new(0, snapshot, "", false)) == new ComputerNameContext("PC-1", 1, null), "Name collection preserves identity without inferring restart state");
        foreach (string pending in new[] { "true", "false", "null", "123", "\"true\"", "[]", "{}" })
        {
            string payload = snapshot[..^1] + ",\"RenamePending\":" + pending + "}";
            var context = ComputerNameCollector.Interpret(new(0, payload, "", false));
            bool? expected = pending == "true" ? true : pending == "false" ? false : null;
            Require(context.CurrentName == "PC-1" && context.RestartPending == expected,
                "Explicit pending and clear rename evidence remain distinct; malformed states remain unknown");
            if (pending == "true")
                Require(ComputerNameReview.Review("NEW-PC", context).ReviewItems.Any(item => item.Contains("restart is pending")),
                    "Collected pending rename reaches the review guidance");
        }
        foreach (var invalid in new[] { new ToolResult(0, snapshot.Replace("true", "false"), "", false),
            new ToolResult(0, snapshot.Replace("\"Name\":", "\"Name\":\"other\",\"Name\":"), "", false),
            new ToolResult(0, snapshot, "", true), new ToolResult(0, snapshot, "", false) { TimedOut = true },
            new ToolResult(5, snapshot, "", false) })
            Require(ComputerNameCollector.Interpret(invalid).CurrentName is null, "Incomplete or inconsistent computer identity cannot support name planning");
        var same = ComputerNameReview.Review("PC-1", new("pc-1", 1, false));
        for (int role = 0; role <= 5; role++)
        {
            bool member = role is 1 or 3 or 4 or 5;
            string identity = System.Text.Json.JsonSerializer.Serialize(new { Name = "PC-1", DomainRole = role, PartOfDomain = member });
            Require(ComputerNameCollector.Interpret(new(0, identity, "", false)).DomainRole == role,
                "All supported workstation, server and controller roles retain their identity");
            identity = System.Text.Json.JsonSerializer.Serialize(new { Name = "PC-1", DomainRole = role, PartOfDomain = !member });
            Require(ComputerNameCollector.Interpret(new(0, identity, "", false)).CurrentName is null,
                "Contradictory domain membership cannot support rename planning");
        }
        foreach (string role in new[] { "-1", "6", "2147483648", "1.5", "null", "\"1\"", "{}" })
        {
            string identity = snapshot.Replace("\"DomainRole\":1", "\"DomainRole\":" + role);
            Require(ComputerNameCollector.Interpret(new(0, identity, "", false)).CurrentName is null,
                "Unsupported and malformed domain roles remain unknown without throwing");
        }
        Require(same.ChangeNeeded == false && same.ReviewItems.Any(i => i.Contains("Domain member")), "Name review retains domain context without inventing a change");
        foreach (string? missingName in new string?[] { null, "", " ", "bad\nname", " padded" })
            Require(ComputerNameReview.Review("NEW-PC", new(missingName, null, null)).ChangeNeeded is null,
                "Missing or malformed identity cannot claim a rename is needed");
        var controller = ComputerNameReview.Review("NEW-PC", new("OLD-PC", 5, true));
        Require(controller.ReviewItems.Any(i => i.Contains("Domain controller")) && controller.ReviewItems.Any(i => i.Contains("restart is pending")), "Controller and restart constraints remain explicit");
        foreach (string invalid in new[] { "", "123", "-pc", "pc-", "pc name", "pc.example", "pc;cmd", new string('a', 64) })
        {
            bool rejected = false;
            try { ComputerNameReview.Review(invalid, new(null, null, null)); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "Invalid proposed computer name rejected");
        }
    }
    public static async Task RunAsync(string folder)
    {
        string[] requested = ["windows.system-files", "printing.paused-queues", "windows.system-files"];
        var selected = RecipeCatalog.SelectForWorkflow(WorkflowKind.CommonProblems, requested);
        Require(selected.Select(recipe => recipe.Id).SequenceEqual(["windows.component-store", "windows.system-files", "printing.spooler", "printing.paused-queues"]),
            "Selected recipes include only required prerequisites in workflow order and deduplicate requests");
        requested[0] = "changed";
        Require(selected[0].Id == "windows.component-store" && RecipeCatalog.SelectForWorkflow(WorkflowKind.CommonProblems, []).Count == 0,
            "Selection is a stable snapshot and empty selection does not imply all repairs");
        foreach (string id in new[] { "missing", "windows.component-cleanup", "" })
        {
            bool rejected = false;
            try { RecipeCatalog.SelectForWorkflow(WorkflowKind.CommonProblems, [id]); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "Unknown and out-of-scope selections fail before execution");
        }
        Require(RecipeCatalog.All.Where(recipe => !recipe.DiagnosticOnly)
            .All(recipe => recipe.RecoveryContract != RepairRecoveryContract.None),
            "Every executable catalog recipe declares a recovery contract");
        var workflowActions = Enum.GetValues<WorkflowKind>().SelectMany(Workflows.Scopes).Distinct().ToArray();
        Require(workflowActions.All(action => RecipeCatalog.All.Count(recipe => recipe.Action == action) == 1),
            "Every workflow action has exactly one catalog recipe");
        Require(!workflowActions.Contains(RepairKind.WindowsTime) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.TimeService).Select(recipe => recipe.Action)
                .SequenceEqual([RepairKind.TimeSynchronization, RepairKind.TimeSourceConfiguration]),
            "Time workflow uses synchronization and source recipes instead of the legacy service action");
        var storageRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "storage.filesystem");
        Require(storageRecipe.Version == 2 && storageRecipe.RequiresExplicitOptions && !storageRecipe.CanAutoExecute &&
            !storageRecipe.DiagnosticOnly && storageRecipe.RecoveryContract == RepairRecoveryContract.FileSystemState &&
            storageRecipe.Effects.Contains("Explicit repair options") && storageRecipe.Effects.Contains("generic recipe runner never"),
            "Filesystem repair is a first-class option-dependent recipe that generic automatic recovery cannot execute");
        var optionHost = new Host { State = CheckState.Repairable };
        var optionResults = await RecipeEngine.RunAsync([storageRecipe], true, folder, optionHost);
        Require(optionResults.Single().Outcome == RecipeOutcome.RepairAvailable && optionHost.Repairs == 0,
            "Generic automatic recovery reports option-dependent filesystem eligibility without dispatching repair");
        var optionWorkflow = new WorkflowResult(WorkflowKind.Storage, "fixture", DateTimeOffset.UtcNow,
            "Needs attention", [], [], "fixture") { Recipes = optionResults };
        Require(optionWorkflow.RecoveryPlan.Action == RecoveryNextAction.ConfigureRepairOptions &&
            optionWorkflow.RecoveryPlan.AvailableRecipeIds.SequenceEqual(["storage.filesystem"]) &&
            WorkflowReport.Format(optionWorkflow).Contains("generic automatic-repair switch will not run it"),
            "Workflow recovery directs option-dependent filesystem evidence to explicit configuration");
        var optimizationRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "storage.optimization");
        Require(optimizationRecipe.RequiresExplicitOptions && !optimizationRecipe.CanAutoExecute &&
            !optimizationRecipe.DiagnosticOnly && optimizationRecipe.RecoveryContract == RepairRecoveryContract.VolumeOptimizationState &&
            Repairs.Plan(RepairKind.VolumeOptimization).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.VolumeOptimization).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Storage).Any(recipe => recipe.Id == optimizationRecipe.Id),
            "Volume optimization is an option-dependent Storage recipe with a separate guarded coordinator");
        var optimizationHost = new Host { State = CheckState.Repairable };
        var optimizationResults = await RecipeEngine.RunAsync([optimizationRecipe], true, folder, optimizationHost);
        Require(optimizationResults.Single().Outcome == RecipeOutcome.RepairAvailable && optimizationHost.Repairs == 0,
            "Generic automatic recovery exposes eligible volume optimization without selecting a drive or method");
        var restorePointRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "windows.restore-point");
        Require(restorePointRecipe.RequiresExplicitOptions && !restorePointRecipe.CanAutoExecute &&
            !restorePointRecipe.DiagnosticOnly && restorePointRecipe.RecoveryContract == RepairRecoveryContract.SystemRestorePointState &&
            Repairs.Plan(RepairKind.SystemRestorePoint).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.SystemRestorePoint).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.SystemRestore).Any(recipe => recipe.Id == restorePointRecipe.Id),
            "Restore-point creation is an option-dependent System Restore recipe with a separate guarded coordinator");
        var restorePointHost = new Host { State = CheckState.Repairable };
        var restorePointResults = await RecipeEngine.RunAsync([restorePointRecipe], true, folder, restorePointHost);
        Require(restorePointResults.Single().Outcome == RecipeOutcome.RepairAvailable && restorePointHost.Repairs == 0,
            "Generic automatic recovery exposes restore-point creation without selecting a description or reason");
        var computerNameRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "windows.computer-name");
        Require(computerNameRecipe.RequiresExplicitOptions && !computerNameRecipe.CanAutoExecute &&
            !computerNameRecipe.DiagnosticOnly && computerNameRecipe.RecoveryContract == RepairRecoveryContract.ComputerNameState &&
            Repairs.Plan(RepairKind.ComputerRename).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.ComputerRename).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.ComputerName).Single().Id == computerNameRecipe.Id &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != computerNameRecipe.Id),
            "Standalone computer rename is a focused option-dependent recipe with a separate guarded coordinator");
        var computerNameHost = new Host { State = CheckState.Repairable };
        var computerNameResults = await RecipeEngine.RunAsync([computerNameRecipe], true, folder, computerNameHost);
        Require(computerNameResults.Single().Outcome == RecipeOutcome.RepairAvailable && computerNameHost.Repairs == 0,
            "Generic automatic recovery exposes eligible standalone rename without selecting or changing a name");
        var searchIndexRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "search.index-rebuild");
        Require(searchIndexRecipe.RequiresExplicitOptions && !searchIndexRecipe.CanAutoExecute &&
            !searchIndexRecipe.DiagnosticOnly && searchIndexRecipe.RecoveryContract == RepairRecoveryContract.SearchIndexState &&
            Repairs.Plan(RepairKind.SearchIndexRebuild).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.SearchIndexRebuild).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Search).Any(recipe => recipe.Id == searchIndexRecipe.Id),
            "Search index rebuild is an option-dependent Search recipe with a separate guarded coordinator");
        var searchChecks = new Queue<CheckState>([CheckState.Clear, CheckState.Repairable]);
        var searchIndexHost = new Host
        {
            InspectionOverride = () => Task.FromResult(new WorkflowCheck("Synthetic", searchChecks.Dequeue(), "Synthetic",
                searchChecks.Count == 0 ? RepairKind.SearchIndexRebuild : RepairKind.WindowsSearch))
        };
        var searchIndexResults = await RecipeEngine.RunAsync(
            RecipeCatalog.SelectForWorkflow(WorkflowKind.Search, ["search.index-rebuild"]), true, folder, searchIndexHost);
        Require(searchIndexResults.Single(result => result.RecipeId == "search.index-rebuild").Outcome == RecipeOutcome.RepairAvailable &&
            searchIndexHost.Repairs == 0,
            "Generic automatic recovery exposes eligible Search rebuild without stopping a service or moving index data");
        var bitLockerResumeRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "security.bitlocker-resume");
        Require(bitLockerResumeRecipe.RequiresExplicitOptions && !bitLockerResumeRecipe.CanAutoExecute &&
            !bitLockerResumeRecipe.DiagnosticOnly && bitLockerResumeRecipe.RecoveryContract == RepairRecoveryContract.BitLockerProtectionState &&
            Repairs.Plan(RepairKind.BitLockerProtectionResume).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.BitLockerProtectionResume).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.BitLocker).Any(recipe => recipe.Id == bitLockerResumeRecipe.Id),
            "BitLocker protection resume is an option-dependent security recipe with a separate guarded coordinator");
        var bitLockerResumeHost = new Host { State = CheckState.Repairable };
        var bitLockerResumeResults = await RecipeEngine.RunAsync([bitLockerResumeRecipe], true, folder, bitLockerResumeHost);
        Require(bitLockerResumeResults.Single().Outcome == RecipeOutcome.RepairAvailable && bitLockerResumeHost.Repairs == 0,
            "Generic automatic recovery exposes suspended BitLocker protection without selecting or changing a volume");
        var bitLockerBackupRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "security.bitlocker-recovery-backup");
        Require(bitLockerBackupRecipe.RequiresExplicitOptions && !bitLockerBackupRecipe.CanAutoExecute &&
            !bitLockerBackupRecipe.DiagnosticOnly && bitLockerBackupRecipe.RecoveryContract == RepairRecoveryContract.BitLockerRecoveryBackupState &&
            Repairs.Plan(RepairKind.BitLockerRecoveryBackup).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.BitLockerRecoveryBackup).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.BitLocker).Any(recipe => recipe.Id == bitLockerBackupRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != bitLockerBackupRecipe.Id),
            "BitLocker recovery backup is an option-dependent security recipe with a separate guarded coordinator");
        var bitLockerBackupHost = new Host { State = CheckState.Repairable };
        var bitLockerBackupResults = await RecipeEngine.RunAsync([bitLockerBackupRecipe], true, folder, bitLockerBackupHost);
        Require(bitLockerBackupResults.Single().Outcome == RecipeOutcome.RepairAvailable && bitLockerBackupHost.Repairs == 0,
            "Generic automatic recovery exposes eligible BitLocker recovery backup without selecting a protector or destination");
        var hibernationRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "power.hibernation-disable");
        Require(hibernationRecipe.RequiresExplicitOptions && !hibernationRecipe.CanAutoExecute &&
            !hibernationRecipe.DiagnosticOnly && hibernationRecipe.Area == "Power" &&
            hibernationRecipe.RecoveryContract == RepairRecoveryContract.HibernationState &&
            Repairs.Plan(RepairKind.HibernationDisable).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.HibernationDisable).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Debloat).Any(recipe => recipe.Id == hibernationRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != hibernationRecipe.Id),
            "Hibernation disablement is a focused option-dependent power recipe with a separate guarded coordinator");
        var hibernationHost = new Host { State = CheckState.Repairable };
        var hibernationResults = await RecipeEngine.RunAsync([hibernationRecipe], true, folder, hibernationHost);
        Require(hibernationResults.Single().Outcome == RecipeOutcome.RepairAvailable && hibernationHost.Repairs == 0,
            "Generic automatic recovery exposes eligible hibernation disablement without dispatching a power change");
        var powerPlanRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "power.high-performance-plan");
        Require(powerPlanRecipe.RequiresExplicitOptions && !powerPlanRecipe.CanAutoExecute &&
            !powerPlanRecipe.DiagnosticOnly && powerPlanRecipe.Area == "Power" &&
            powerPlanRecipe.RecoveryContract == RepairRecoveryContract.PowerPlanState &&
            Repairs.Plan(RepairKind.HighPerformancePowerPlan).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.HighPerformancePowerPlan).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Debloat).Any(recipe => recipe.Id == powerPlanRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != powerPlanRecipe.Id),
            "High performance activation is a focused option-dependent recipe with a separate guarded coordinator");
        var powerPlanHost = new Host { State = CheckState.Repairable };
        var powerPlanResults = await RecipeEngine.RunAsync([powerPlanRecipe], true, folder, powerPlanHost);
        Require(powerPlanResults.Single().Outcome == RecipeOutcome.RepairAvailable && powerPlanHost.Repairs == 0,
            "Generic automatic recovery exposes eligible plan activation without dispatching a power change");
        var powerTimerRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "power.sleep-display-timers");
        Require(powerTimerRecipe.RequiresExplicitOptions && !powerTimerRecipe.CanAutoExecute &&
            !powerTimerRecipe.DiagnosticOnly && powerTimerRecipe.Area == "Power" &&
            powerTimerRecipe.RecoveryContract == RepairRecoveryContract.PowerTimerState &&
            Repairs.Plan(RepairKind.PowerTimers).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.PowerTimers).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Debloat).Any(recipe => recipe.Id == powerTimerRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != powerTimerRecipe.Id),
            "Power timers are a focused option-dependent recipe with a separate guarded coordinator");
        var powerTimerHost = new Host { State = CheckState.Repairable };
        var powerTimerResults = await RecipeEngine.RunAsync([powerTimerRecipe], true, folder, powerTimerHost);
        Require(powerTimerResults.Single().Outcome == RecipeOutcome.RepairAvailable && powerTimerHost.Repairs == 0,
            "Generic automatic recovery exposes eligible timer changes without dispatching power commands");
        var updateStoreRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "update.data-store-reset");
        Require(updateStoreRecipe.RequiresExplicitOptions && !updateStoreRecipe.CanAutoExecute &&
            !updateStoreRecipe.DiagnosticOnly && updateStoreRecipe.RecoveryContract == RepairRecoveryContract.UpdateDataStoreState &&
            Repairs.Plan(RepairKind.UpdateDataStoreReset).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.UpdateDataStoreReset).Effects.Contains("generic recipe runner never") &&
            updateStoreRecipe.Dependencies.SequenceEqual(["update.service", "update.transfers", "update.cryptography"]) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.WindowsUpdate).Any(recipe => recipe.Id == updateStoreRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != updateStoreRecipe.Id),
            "Windows Update data-store recovery is a focused option-dependent recipe with service prerequisites and a separate guarded coordinator");
        var updateStoreHost = new Host { State = CheckState.Repairable };
        var updateStoreResults = await RecipeEngine.RunAsync([updateStoreRecipe with { Dependencies = [] }], true, folder, updateStoreHost);
        Require(updateStoreResults.Single().Outcome == RecipeOutcome.RepairAvailable && updateStoreHost.Repairs == 0,
            "Generic automatic recovery exposes structural update-store eligibility without dispatching reset operations");
        var bitsRetryRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "update.bits-job-retry");
        Require(bitsRetryRecipe.RequiresExplicitOptions && !bitsRetryRecipe.CanAutoExecute &&
            !bitsRetryRecipe.DiagnosticOnly && bitsRetryRecipe.RecoveryContract == RepairRecoveryContract.BitsJobState &&
            Repairs.Plan(RepairKind.BitsJobRetry).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.BitsJobRetry).Effects.Contains("generic recipe runner never") &&
            bitsRetryRecipe.Dependencies.SequenceEqual(["update.transfers"]) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.BackgroundTransfers).Any(recipe => recipe.Id == bitsRetryRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != bitsRetryRecipe.Id),
            "Exact BITS job retry is a focused option-dependent recipe with a service prerequisite and a separate guarded coordinator");
        var bitsRetryHost = new Host { State = CheckState.Repairable };
        var bitsRetryResults = await RecipeEngine.RunAsync([bitsRetryRecipe with { Dependencies = [] }], true, folder, bitsRetryHost);
        Require(bitsRetryResults.Single().Outcome == RecipeOutcome.RepairAvailable && bitsRetryHost.Repairs == 0,
            "Generic automatic recovery exposes exact BITS retry eligibility without dispatching a transfer request");
        var timeSourceRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "time.source-configuration");
        Require(timeSourceRecipe.RequiresExplicitOptions && !timeSourceRecipe.CanAutoExecute &&
            !timeSourceRecipe.DiagnosticOnly && timeSourceRecipe.RecoveryContract == RepairRecoveryContract.TimeSourceState &&
            Repairs.Plan(RepairKind.TimeSourceConfiguration).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.TimeSourceConfiguration).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.TimeService).Any(recipe => recipe.Id == timeSourceRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != timeSourceRecipe.Id),
            "Windows Time source configuration is a focused option-dependent recipe with a separate guarded coordinator");
        var timeSourceHost = new Host { State = CheckState.Repairable };
        var timeSourceResults = await RecipeEngine.RunAsync([timeSourceRecipe], true, folder, timeSourceHost);
        Require(timeSourceResults.Single().Outcome == RecipeOutcome.RepairAvailable && timeSourceHost.Repairs == 0,
            "Generic automatic recovery exposes eligible time-source configuration without selecting or changing a source");
        var firewallRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "network.firewall-profiles");
        Require(firewallRecipe.RequiresExplicitOptions && !firewallRecipe.CanAutoExecute && !firewallRecipe.DiagnosticOnly &&
            firewallRecipe.RecoveryContract == RepairRecoveryContract.FirewallProfileState &&
            Repairs.Plan(RepairKind.FirewallProfiles).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.FirewallProfiles).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Network).Any(recipe => recipe.Id == firewallRecipe.Id),
            "Firewall profile enablement is an option-dependent Network recipe with a separate guarded coordinator");
        var firewallHost = new Host { State = CheckState.Repairable };
        var firewallResults = await RecipeEngine.RunAsync([firewallRecipe], true, folder, firewallHost);
        var firewallWorkflow = new WorkflowResult(WorkflowKind.Network, "fixture", DateTimeOffset.UtcNow,
            "Needs attention", [], [], "fixture") { Recipes = firewallResults };
        Require(firewallResults.Single().Outcome == RecipeOutcome.RepairAvailable && firewallHost.Repairs == 0 &&
            firewallWorkflow.RecoveryPlan.Action == RecoveryNextAction.ConfigureRepairOptions &&
            firewallWorkflow.RecoveryPlan.AvailableRecipeIds.SequenceEqual(["network.firewall-profiles"]),
            "Generic automatic recovery exposes eligible firewall profiles without selecting or changing one");
        var defaultPrinterRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "printing.default-printer");
        Require(defaultPrinterRecipe.RequiresExplicitOptions && !defaultPrinterRecipe.CanAutoExecute && !defaultPrinterRecipe.DiagnosticOnly &&
            defaultPrinterRecipe.RecoveryContract == RepairRecoveryContract.DefaultPrinterState &&
            Repairs.Plan(RepairKind.DefaultPrinter).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.DefaultPrinter).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Printing).Any(recipe => recipe.Id == defaultPrinterRecipe.Id),
            "Default-printer selection is an option-dependent Printing recipe with a separate guarded coordinator");
        var printerHost = new Host { State = CheckState.Repairable };
        var printerResults = await RecipeEngine.RunAsync([defaultPrinterRecipe], true, folder, printerHost);
        Require(printerResults.Single().Outcome == RecipeOutcome.RepairAvailable && printerHost.Repairs == 0,
            "Generic automatic recovery exposes missing default-printer eligibility without selecting or changing a queue");
        var printJobRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "printing.stuck-job");
        Require(printJobRecipe.RequiresExplicitOptions && !printJobRecipe.CanAutoExecute && !printJobRecipe.DiagnosticOnly &&
            printJobRecipe.Area == "Printing" && printJobRecipe.Dependencies.SequenceEqual(["printing.spooler"]) &&
            printJobRecipe.RecoveryContract == RepairRecoveryContract.PrintJobState &&
            Repairs.Plan(RepairKind.PrintJobCancellation).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.PrintJobCancellation).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Printing).Any(recipe => recipe.Id == printJobRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Any(recipe => recipe.Id == printJobRecipe.Id),
            "Exact stuck-job cancellation is an option-dependent Printing recipe with a separate guarded coordinator");
        var printJobHost = new Host { State = CheckState.Repairable };
        var printJobResults = await RecipeEngine.RunAsync([printJobRecipe with { Dependencies = [] }], true, folder, printJobHost);
        Require(printJobResults.Single().Outcome == RecipeOutcome.RepairAvailable && printJobHost.Repairs == 0,
            "Generic automatic recovery exposes stuck-job eligibility without selecting or canceling a job");
        var installerRegistrationRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "installer.registration");
        Require(installerRegistrationRecipe.RequiresExplicitOptions && !installerRegistrationRecipe.CanAutoExecute &&
            !installerRegistrationRecipe.DiagnosticOnly &&
            installerRegistrationRecipe.RecoveryContract == RepairRecoveryContract.InstallerRegistrationState &&
            Repairs.Plan(RepairKind.InstallerRegistration).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.InstallerRegistration).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Installer).Any(recipe => recipe.Id == installerRegistrationRecipe.Id),
            "Installer registration is an option-dependent Installation recipe with a separate guarded coordinator");
        var registrationHost = new Host { State = CheckState.Repairable };
        var registrationResults = await RecipeEngine.RunAsync([installerRegistrationRecipe], true, folder, registrationHost);
        Require(registrationResults.Single().Outcome == RecipeOutcome.RepairAvailable && registrationHost.Repairs == 0,
            "Generic automatic recovery exposes exact Installer registration damage without dispatching registration commands");
        var msiProductRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "installer.msi-product");
        Require(msiProductRecipe.RequiresExplicitOptions && !msiProductRecipe.CanAutoExecute && !msiProductRecipe.DiagnosticOnly &&
            msiProductRecipe.RecoveryContract == RepairRecoveryContract.MsiProductState &&
            Repairs.Plan(RepairKind.MsiProduct).Steps.Count == 0 &&
            Repairs.Plan(RepairKind.MsiProduct).Effects.Contains("generic recipe runner never") &&
            RecipeCatalog.ForWorkflow(WorkflowKind.Installer).Any(recipe => recipe.Id == msiProductRecipe.Id),
            "Exact MSI product repair is an option-dependent Installation recipe with a separate guarded coordinator");
        var msiHost = new Host { State = CheckState.Repairable };
        var msiResults = await RecipeEngine.RunAsync([msiProductRecipe], true, folder, msiHost);
        Require(msiResults.Single().Outcome == RecipeOutcome.RepairAvailable && msiHost.Repairs == 0,
            "Generic automatic recovery exposes selected MSI product damage without dispatching a package repair");
        var eventChannelRecipe = RecipeCatalog.All.Single(recipe => recipe.Id == "eventlog.channel-enable");
        Require(eventChannelRecipe.Action == RepairKind.EventChannelEnable && eventChannelRecipe.RequiresExplicitOptions &&
            !eventChannelRecipe.CanAutoExecute && eventChannelRecipe.Dependencies.SequenceEqual(["eventlog.service"]) &&
            eventChannelRecipe.RecoveryContract == RepairRecoveryContract.EventChannelState &&
            Repairs.Plan(eventChannelRecipe.Action).Steps.Count == 0 &&
            RecipeCatalog.ForWorkflow(WorkflowKind.EventLog).Any(recipe => recipe.Id == eventChannelRecipe.Id) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).All(recipe => recipe.Id != eventChannelRecipe.Id),
            "Exact event channel enablement is a focused option-dependent recipe with a separate guarded coordinator");
        var eventChannelHost = new Host { State = CheckState.Repairable };
        var eventChannelResults = await RecipeEngine.RunAsync([eventChannelRecipe with { Dependencies = [] }], true, folder, eventChannelHost);
        Require(eventChannelResults.Single().Outcome == RecipeOutcome.RepairAvailable && eventChannelHost.Repairs == 0,
            "Generic automatic recovery exposes event channel eligibility without selecting or changing a channel");
        Require(RecipeCatalog.All.All(recipe => !string.IsNullOrWhiteSpace(recipe.Area) && recipe.Area != "Other"),
            "Every installed recipe declares a known functional area");
        Require(RecipeCatalog.All.Single(recipe => recipe.Id == "windows.component-store").Area == "Windows" &&
            RecipeCatalog.All.Single(recipe => recipe.Id == "network.dhcp-client").Area == "Network" &&
            RecipeCatalog.All.Single(recipe => recipe.Id == "devices.image-acquisition-service").Area == "Devices" &&
            RecipeCatalog.All.Single(recipe => recipe.Id == "applications.appx-deployment-service").Area == "Applications" &&
            RecipeCatalog.All.Single(recipe => recipe.Id == "remote.winrm-service").Area == "Remote access",
            "Recipe areas remain stable for desktop and CLI grouping");
        Require(new RepairRecipe("fixture", 1, "Fixture", RepairKind.WmiProvider, [], "Fixture") { DiagnosticOnly = true }.Area == "Other",
            "External recipes with an unknown prefix remain usable without inventing an area");
        Require(RepairRecoveryCatalog.For(RepairKind.PrintSpooler) == RepairRecoveryContract.ExactServiceState &&
            RepairRecoveryCatalog.For(RepairKind.ComponentStore) == RepairRecoveryContract.ServicingHealth &&
            RepairRecoveryCatalog.For(RepairKind.DhcpLease) == RepairRecoveryContract.DhcpAdapterState &&
            RepairRecoveryCatalog.For(RepairKind.PrinterOffline) == RepairRecoveryContract.PrinterOfflineState &&
            RepairRecoveryCatalog.For(RepairKind.StorageFileSystem) == RepairRecoveryContract.FileSystemState &&
            RepairRecoveryCatalog.For(RepairKind.FirewallProfiles) == RepairRecoveryContract.FirewallProfileState &&
            RepairRecoveryCatalog.For(RepairKind.DefaultPrinter) == RepairRecoveryContract.DefaultPrinterState &&
            RepairRecoveryCatalog.For(RepairKind.InstallerRegistration) == RepairRecoveryContract.InstallerRegistrationState &&
            RepairRecoveryCatalog.For(RepairKind.MsiProduct) == RepairRecoveryContract.MsiProductState &&
            RepairRecoveryCatalog.For(RepairKind.VolumeOptimization) == RepairRecoveryContract.VolumeOptimizationState &&
            RepairRecoveryCatalog.For(RepairKind.SystemRestorePoint) == RepairRecoveryContract.SystemRestorePointState &&
            RepairRecoveryCatalog.For(RepairKind.ComputerRename) == RepairRecoveryContract.ComputerNameState &&
            RepairRecoveryCatalog.For(RepairKind.SearchIndexRebuild) == RepairRecoveryContract.SearchIndexState &&
            RepairRecoveryCatalog.For(RepairKind.BitLockerProtectionResume) == RepairRecoveryContract.BitLockerProtectionState &&
            RepairRecoveryCatalog.For(RepairKind.BitLockerRecoveryBackup) == RepairRecoveryContract.BitLockerRecoveryBackupState &&
            RepairRecoveryCatalog.For(RepairKind.HighPerformancePowerPlan) == RepairRecoveryContract.PowerPlanState &&
            RepairRecoveryCatalog.For(RepairKind.HibernationDisable) == RepairRecoveryContract.HibernationState &&
            RepairRecoveryCatalog.For(RepairKind.PowerTimers) == RepairRecoveryContract.PowerTimerState &&
            RepairRecoveryCatalog.For(RepairKind.UpdateDataStoreReset) == RepairRecoveryContract.UpdateDataStoreState &&
            RepairRecoveryCatalog.For(RepairKind.BitsJobRetry) == RepairRecoveryContract.BitsJobState &&
            RepairRecoveryCatalog.For(RepairKind.TimeSourceConfiguration) == RepairRecoveryContract.TimeSourceState,
            "Recovery contracts distinguish service, servicing and exact-target evidence");
        bool missingRecoveryRejected = false;
        try
        {
            await RecipeEngine.RunAsync([new("fixture.uncovered", 1, "Uncovered mutation", RepairKind.WmiProvider,
                [], "Fixture")], false, folder, new Host { State = CheckState.Clear });
        }
        catch (ArgumentException) { missingRecoveryRejected = true; }
        Require(missingRecoveryRejected, "An executable recipe without a recovery contract is rejected before inspection");
        var selectionHost = new Host { State = CheckState.Clear };
        var selectionResults = await RecipeEngine.RunAsync(selected, false, folder, selectionHost);
        Require(selectionResults.Count == 4 && selectionResults.All(result => result.Outcome == RecipeOutcome.Clear) && selectionHost.Repairs == 0,
            "Selected dependency-complete plan executes through the existing engine without broadening scope");
        foreach (var action in new[] { RepairKind.DhcpLease, RepairKind.PrintQueues, RepairKind.WindowsTime, RepairKind.TimeSynchronization, RepairKind.WmiRepository })
        {
            var evidence = Success(action).Steps[^1].Result with { Error = "Fixture command error" };
            bool rejected = action switch
            {
                RepairKind.DhcpLease => DhcpRecovery.Read(evidence) is null,
                RepairKind.PrintQueues => PrintRecovery.Read(evidence) is null,
                RepairKind.WindowsTime => ServiceRecovery.State(evidence) is null && !ServiceRecovery.MatchesService(evidence, "W32Time"),
                RepairKind.TimeSynchronization => TimeRecovery.Interpret(evidence) is { State: CheckState.Unknown, Repair: null },
                _ => WmiRecovery.Interpret(evidence) is { State: CheckState.Unknown, Repair: null }
            };
            Require(rejected, "Command errors invalidate otherwise plausible automatic-repair evidence: " + action);
        }
        Require(WmiRecovery.Interpret(new(1358, "WMI repository is inconsistent", "Fixture command error", false)).Repair is null,
            "Mixed error output cannot authorize WMI salvage even with the expected corruption code");
        CheckComputerNames();
        await TimeConfigurationChecks.RunAsync(folder);
        var standalone = new TimeEnvironment(false, false, false, true) { LocalMode = "NTP" };
        foreach (string? mode in new string?[] { null, "NoSync", "Unexpected" })
            Require(!TimePlanning.Create(new(TimeSourceMode.CustomPeers, ["time.example.test"]), standalone with { LocalMode = mode }).ReviewPassed,
                "Disabled or unavailable local synchronization blocks source-change planning");
        var localTime = TimeEnvironmentCollector.Interpret(new(0, "{\"joined\":false,\"role\":0,\"policy\":null,\"elevated\":false,\"localMode\":\"NTP\",\"localPeers\":\"time.example.test,0x9\"}", "", false));
        Require(localTime.LocalMode == "NTP" && localTime.LocalPeers == "time.example.test,0x9" && localTime.PolicyManaged is null,
            "Local time values retained without claiming policy ownership");
        var localConfig = new ToolResult(0, "Type: NTP (Local)\nNtpServer: time.example.test,0x9 (Local)", "", false);
        Require(TimeEnvironmentCollector.WithConfigurationOrigin(localTime, localConfig).PolicyManaged == false, "Matching local origins recognized");
        var serviceStopped = new ToolResult(unchecked((int)0x80070426), "The service has not been started.", "", false);
        var stoppedEnvironment = TimeEnvironmentCollector.WithConfigurationOrigin(localTime, serviceStopped);
        var stoppedFinding = TimeConfigurationDiagnostics.Assess(stoppedEnvironment);
        Require(ReferenceEquals(stoppedEnvironment.ConfigurationEvidence, serviceStopped) && stoppedEnvironment.PolicyManaged == false &&
            stoppedFinding.State == CheckState.Information && stoppedFinding.Detail.Contains("service has not been started"),
            "Exact stopped-service evidence retains the command cause and the registry-confirmed unmanaged configuration");
        Require(TimeEnvironmentCollector.WithConfigurationOrigin(localTime with { PolicyManaged = true }, serviceStopped).PolicyManaged == true,
            "Stopped-service evidence cannot override a managed time-policy snapshot");
        foreach (var incompleteStopped in new[] { serviceStopped with { ExitCode = 5 }, serviceStopped with { TimedOut = true }, serviceStopped with { Truncated = true } })
            Require(TimeEnvironmentCollector.WithConfigurationOrigin(localTime, incompleteStopped).PolicyManaged is null,
                "Inexact or incomplete stopped-service evidence cannot establish time-policy ownership");
        Require(TimeEnvironmentCollector.WithConfigurationOrigin(localTime, localConfig with { Error = "Provider failure" }).PolicyManaged is null,
            "Error-bearing configuration output cannot establish unmanaged ownership");
        Require(TimeEnvironmentCollector.WithConfigurationOrigin(localTime, localConfig with { Output = localConfig.Output.Replace("(Local)", "(Policy)") }).PolicyManaged == true, "Effective time policy preserved");
        foreach (var uncertain in new[] { localConfig with { TimedOut = true }, localConfig with { Output = localConfig.Output + "\nType: NTP (Local)" },
            localConfig with { Output = localConfig.Output.Replace("time.example.test", "changed.example.test") } })
            Require(TimeEnvironmentCollector.WithConfigurationOrigin(localTime, uncertain).PolicyManaged is null, "Uncertain configuration origin remains unknown");
        var retainedPeers = new List<string> { "time.example.test" };
        foreach (var configurationCase in new[] {
            (Environment: standalone with { LocalMode = "NT5DS" }, State: CheckState.Attention),
            (Environment: standalone with { LocalMode = "NTP", LocalPeers = "" }, State: CheckState.Attention),
            (Environment: standalone with { LocalMode = "NTP", LocalPeers = null }, State: CheckState.Unknown),
            (Environment: standalone with { LocalMode = "NoSync" }, State: CheckState.Information),
            (Environment: standalone with { LocalMode = "NT5DS", PolicyManaged = true }, State: CheckState.Information),
            (Environment: standalone with { LocalMode = "Other" }, State: CheckState.Unknown),
            (Environment: standalone with { PolicyManaged = null }, State: CheckState.Unknown) })
        {
            var sourceCheck = TimeConfigurationDiagnostics.Assess(configurationCase.Environment);
            Require(sourceCheck.State == configurationCase.State && sourceCheck.Repair is null && sourceCheck.Evidence is not null,
                "Time configuration findings preserve managed and disabled settings without selecting changes");
        }
        foreach (Exception failure in new Exception[] { new IOException(), new UnauthorizedAccessException(), new System.Security.SecurityException(), new InvalidOperationException(), new TimeoutException() })
        {
            var unavailable = await TimePlanning.PreviewAsync(new(TimeSourceMode.CustomPeers, ["time.example.test"]), collector: _ => throw failure);
            Require(!unavailable.Plan.ReviewPassed && unavailable.Environment.DomainJoined is null && unavailable.Environment.PolicyManaged is null,
                "Unavailable time collection cannot authorize a source change");
        }
        foreach (var provider in new Func<CancellationToken, Task<TimeEnvironment>>[] { _ => null!, _ => Task.FromResult<TimeEnvironment>(null!) })
            Require(!(await TimePlanning.PreviewAsync(new(TimeSourceMode.DomainHierarchy), collector: provider)).Plan.ReviewPassed,
                "Missing time collection results block source changes");
        using (var interrupted = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)))
        {
            try
            {
                await TimePlanning.PreviewAsync(new(), interrupted.Token, _ => new TaskCompletionSource<TimeEnvironment>().Task);
                throw new Exception("Uncooperative time provider ignored cancellation");
            }
            catch (OperationCanceledException) { }
        }
        Require(TimeEnvironmentCollector.Interpret(new(0, "{\"joined\":false,\"role\":4294967296,\"policy\":false,\"elevated\":true}", "", false)).DomainJoined is null,
            "Overflowing domain role remains unknown");
        var freshPreview = await TimePlanning.PreviewAsync(new(TimeSourceMode.CustomPeers, retainedPeers), collector: _ =>
        {
            retainedPeers[0] = "changed.example.test";
            return Task.FromResult(standalone);
        });
        Require(freshPreview.Plan.Peers.Single() == "time.example.test", "Time preview retains the selected peers across collection");
        using (var canceledPreview = new CancellationTokenSource())
        {
            try
            {
                await TimePlanning.PreviewAsync(new(), canceledPreview.Token, _ => { canceledPreview.Cancel(); return Task.FromResult(standalone); });
                throw new Exception("Canceled time preview returned a plan");
            }
            catch (OperationCanceledException) { }
        }
        string timePreview = TimePlanning.Create(new(TimeSourceMode.CustomPeers, ["time.example.test"]), standalone with { PolicyManaged = null }).Format();
        Require(timePreview.Contains("time.example.test") && timePreview.Contains("Source change blocked:") &&
            timePreview.Contains("ownership is unknown") && timePreview.Contains("No clock, service or source settings changed."), "Time preview preserves targets, blockers and non-execution outcome");
        var timeSnapshot = new ToolResult(0, "{\"joined\":true,\"role\":3,\"policy\":null,\"elevated\":true}", "", false);
        Require(TimeEnvironmentCollector.Interpret(timeSnapshot) == new TimeEnvironment(true, false, null, true), "Time domain role parsed without inventing policy ownership");
        foreach (var bad in new[] { timeSnapshot with { TimedOut = true }, timeSnapshot with { Truncated = true },
            timeSnapshot with { Output = timeSnapshot.Output.Replace("\"role\":3", "\"role\":0") },
            timeSnapshot with { Output = timeSnapshot.Output.Replace("\"role\":3", "\"role\":3,\"role\":0") } })
            Require(TimeEnvironmentCollector.Interpret(bad).DomainJoined is null, "Incomplete time environment remains unknown");
        Require(TimePlanning.Create(new(TimeSourceMode.CustomPeers, ["time.example.test"]), standalone).ReviewPassed, "Standalone custom peer planning");
        Require(!TimePlanning.Create(new(TimeSourceMode.CustomPeers, ["time.example.test"]), standalone with { PolicyManaged = true }).ReviewPassed, "Managed time source preserved");
        Require(!TimePlanning.Create(new(TimeSourceMode.DomainHierarchy), standalone).ReviewPassed, "Standalone domain hierarchy blocked");
        Require(TimePlanning.Create(new(TimeSourceMode.DomainHierarchy), standalone with { DomainJoined = true }).ReviewPassed, "Domain member hierarchy planning");
        Require(!TimePlanning.Create(new(TimeSourceMode.DomainHierarchy), standalone with { DomainJoined = true, DomainController = true }).ReviewPassed, "DC topology requires review");
        foreach (string peer in new[] { "", "time.example.test,0x8", "host /update", "https://time.example.test", "host:123" })
        {
            try { TimePlanning.Create(new(TimeSourceMode.CustomPeers, [peer]), standalone); throw new Exception("Invalid NTP peer accepted"); }
            catch (ArgumentException) { }
        }
        const string synced = "Leap Indicator: 0(no warning)\nStratum: 3 (secondary reference)\nSource: dc.example.test\nLast Sync Error: 0 (success)";
        foreach (string field in new[] { "Leap Indicator", "Stratum", "Last Sync Error" })
        {
            foreach (string badValue in new[] { "0.5", "0 garbage", "", "\n0", "0 (unfinished", "0x0", "+0", "-0" })
            {
                string malformed = System.Text.RegularExpressions.Regex.Replace(synced, "(?m)^" + field + ":[^\\r\\n]*", field + ": " + badValue);
                var rejected = TimeRecovery.Interpret(new(0, malformed, "", false));
                if (rejected.State != CheckState.Unknown || rejected.Repair is not null)
                    throw new Exception("Malformed time status authorized synchronization or claimed success");
            }
            if (TimeRecovery.Interpret(new(0, synced + "\n" + field + ": invalid", "", false)).State != CheckState.Unknown)
                throw new Exception("Duplicate invalid time status field was ignored");
        }
        Require(TimeRecovery.Interpret(new(0, synced, "", false)).State == CheckState.Clear, "Time source status recognized");
        Require(TimeRecovery.Interpret(new(0, synced.Replace("0(no warning)", "3(not synchronized)"), "", false)).Repair == RepairKind.TimeSynchronization, "Unsynchronized time selects resync");
        Require(TimeRecovery.Interpret(new(0, synced.Replace("dc.example.test", "Local CMOS Clock"), "", false)).State == CheckState.Attention, "Local oscillator is not external synchronization proof");
        Require(TimeRecovery.Interpret(new(0, synced + "\nLeap Indicator: 3", "", false)).State == CheckState.Unknown, "Contradictory time fields cannot verify");
        Require(TimeRecovery.Interpret(new(5, synced, "", false)).State == CheckState.Unknown, "Time query failure cannot verify");
        foreach (string output in new[] { synced, synced.Replace("0(no warning)", "3(not synchronized)") })
        {
            var timedOut = TimeRecovery.Interpret(new ToolResult(0, output, "", false) { TimedOut = true });
            Require(timedOut.State == CheckState.Unknown && timedOut.Repair is null, "Timed-out evidence cannot verify synchronization or select resync");
        }
        Require(!TimeRecovery.CanSynchronize("NoSync") && !TimeRecovery.CanSynchronize(null) && TimeRecovery.CanSynchronize("NT5DS"), "Disabled and missing time configuration preserved");
        var serviceNotStarted = new ToolResult(unchecked((int)0x80070426), "", "The service has not been started.", false);
        Require(TimeRecovery.IsServiceNotStarted(serviceNotStarted) &&
            !TimeRecovery.IsServiceNotStarted(serviceNotStarted with { ExitCode = 5 }) &&
            !TimeRecovery.IsServiceNotStarted(serviceNotStarted with { TimedOut = true }) &&
            !TimeRecovery.IsServiceNotStarted(serviceNotStarted with { Truncated = true }),
            "Only complete exact Windows service-not-started evidence enters focused time recovery");
        var timePlan = Repairs.Plan(RepairKind.TimeSynchronization);
        Require(timePlan.Steps.Count == 3 && timePlan.Steps[0].Tool == "sc.exe" && timePlan.Steps[0].Arguments.SequenceEqual(["start", "W32Time"]) && !timePlan.Steps[0].Verification &&
            timePlan.Steps[1].Tool == TimeRecovery.Tool && timePlan.Steps[1].Arguments.SequenceEqual(["/resync"]) && !timePlan.Steps[1].Verification &&
            timePlan.Steps[2].Tool == TimeRecovery.Tool && timePlan.Steps[2].Arguments.SequenceEqual(["/query", "/status", "/verbose"]) && timePlan.Steps[2].Verification &&
            RecipeCatalog.ForWorkflow(WorkflowKind.TimeService)[0].Action == RepairKind.TimeSynchronization &&
            RecipeCatalog.ForWorkflow(WorkflowKind.TimeService)[^1].Action == RepairKind.TimeSourceConfiguration,
            "Time recovery owns service start, resync and fresh status verification in one recipe");
        Require(Repairs.IsVerified(Success(RepairKind.TimeSynchronization)), "Resync verifies reported synchronization");
        Require(WmiRecovery.Interpret(new(0, "WMI repository is consistent", "", false)).State == CheckState.Clear, "WMI clean result recognized");
        foreach (int code in new[] { 1358, unchecked((int)0x8007054E) })
            Require(WmiRecovery.Interpret(new(code, "WMI repository is inconsistent", "", false)).Repair == RepairKind.WmiRepository, "Confirmed WMI corruption selects salvage");
        foreach (var uncertain in new[] {
            new ToolResult(5, "WMI repository is inconsistent", "", false),
            new ToolResult(1358, "Access denied", "", false),
            new ToolResult(0, "WMI repository is consistent", "", true),
            new ToolResult(1358, "WMI repository is inconsistent\nWMI repository is consistent", "", false) })
            Require(WmiRecovery.Interpret(uncertain).State == CheckState.Unknown, "Ambiguous WMI evidence cannot trigger salvage");
        Require(Repairs.IsVerified(Success(RepairKind.WmiRepository)), "WMI repair requires matching salvage and consistency check");
        foreach (string verdict in new[] { "WMI repository is consistent", "WMI repository is inconsistent" })
        {
            int code = verdict.EndsWith("inconsistent", StringComparison.Ordinal) ? 1358 : 0;
            foreach (var uncertain in new[] { new ToolResult(code, verdict, "", false) { TimedOut = true },
                new ToolResult(code, verdict, "", true), new ToolResult(code, "Example: " + verdict, "", false),
                new ToolResult(code, verdict + "\n" + verdict, "", false) })
                Require(WmiRecovery.Interpret(uncertain).State == CheckState.Unknown && WmiRecovery.Interpret(uncertain).Repair is null,
                    "Timed-out, truncated, quoted and repeated WMI verdicts cannot establish consistency or authorize salvage");
        }
        Require(PrintRecovery.Interpret(new(0, "{\"count\":2,\"paused\":1}", "", false)) is { State: CheckState.Attention, Repair: null },
            "Paused queues alone require waiting-job evidence and do not select resume");
        Require(PrintRecovery.Interpret(new(0, "{\"count\":0,\"paused\":0}", "", false)).State == CheckState.Clear, "No installed queues do not trigger repair");
        Require(PrintRecovery.Read(new(0, "{\"count\":1,\"paused\":2}", "", false)) is null, "Inconsistent queue results cannot verify");
        Require(PrintRecovery.Read(new(5, "{\"count\":1,\"paused\":0}", "", false)) is null, "Failed query cannot verify");
        Require(PrintRecovery.Read(new(0, "not json", "", false)) is null, "Unrecognized queue output cannot select repair");
        var resumed = Success(RepairKind.PrintQueues);
        var renewedLease = Success(RepairKind.DhcpLease);
        foreach (string operation in new[] { "{\"targets\":[1],\"adapters\":[]}",
            "{\"targets\":[1],\"adapters\":[{\"id\":2,\"addresses\":[\"192.0.2.10\"]}]}" })
        {
            var inconsistentLease = renewedLease.Steps.ToArray();
            inconsistentLease[0] = inconsistentLease[0] with { Result = new(0, operation, "", false) };
            Require(!Repairs.IsVerified(renewedLease with { Steps = inconsistentLease }), "DHCP verification requires target in operation snapshot");
        }
        Require(Repairs.IsVerified(renewedLease), "Matching DHCP operation and final adapter retain verified result");
        Require(Repairs.IsVerified(resumed), "Resumed queues remain present and unpaused");
        foreach (string snapshot in new[] { "{\"count\":0,\"paused\":0,\"names\":[],\"targets\":[\"A\"]}",
            "{\"count\":1,\"paused\":0,\"names\":[\"B\"],\"targets\":[\"A\"]}",
            "{\"count\":2,\"paused\":0,\"names\":[\"A\",\"a\"],\"targets\":[\"A\"]}",
            "{\"count\":1,\"paused\":0,\"names\":[null],\"targets\":[\"A\"]}",
            "{\"count\":2,\"paused\":0,\"names\":[\"A\"],\"targets\":[\"A\"]}" })
        {
            var inconsistentSteps = resumed.Steps.ToArray();
            inconsistentSteps[0] = inconsistentSteps[0] with { Result = new(0, snapshot, "", false) };
            Require(!Repairs.IsVerified(resumed with { Steps = inconsistentSteps }), "Resume verification rejects inconsistent operation identities");
        }
        foreach (string malformed in new[] { "null", "[]", "{\"count\":2147483648,\"paused\":1}",
            "{\"count\":1,\"paused\":0,\"paused\":1}", "{\"count\":1,\"count\":2,\"paused\":1}",
            "{\"count\":1,\"paused\":1.5}", "{\"count\":1,\"paused\":null}" })
        {
            var malformedResult = new ToolResult(0, malformed, "", false);
            Require(PrintRecovery.Interpret(malformedResult) is { State: CheckState.Unknown, Repair: null },
                "Ambiguous or invalid print counts cannot authorize resume");
            var invalidSteps = resumed.Steps.ToArray();
            invalidSteps[0] = invalidSteps[0] with { Result = malformedResult };
            Require(!Repairs.IsVerified(resumed with { Steps = invalidSteps }), "Invalid print evidence cannot verify repair");
        }
        foreach (string targets in new[] { "[\"A\",\"a\"]", "[\"A\\n\"]", "[null]", "[1]" })
        {
            var invalidSteps = resumed.Steps.ToArray();
            invalidSteps[0] = invalidSteps[0] with { Result = new(0,
                "{\"count\":1,\"paused\":0,\"names\":[\"A\"],\"targets\":" + targets + "}", "", false) };
            Require(!Repairs.IsVerified(resumed with { Steps = invalidSteps }), "Invalid or duplicate resume targets cannot verify repair");
        }
        foreach (string names in new[] { "[]", "[\"Other\"]", "[null]" })
        {
            var missing = resumed.Steps.ToArray();
            missing[1] = missing[1] with { Result = new(0, "{\"count\":1,\"paused\":0,\"names\":" + names + "}", "", false) };
            Require(!Repairs.IsVerified(resumed with { Steps = missing }), "Missing or unrelated queues cannot verify resume");
        }
        Require(PrintRecovery.Read(new(0, "{\"count\":1,\"paused\":0}", "", false) { TimedOut = true }) is null, "Timed-out print snapshots cannot verify");
        Require(StorageDiagnostics.Interpret(new(0, "Windows has scanned the file system and found no problems", "", false)).State == CheckState.Clear, "Recognized clean filesystem result");
        Require(StorageDiagnostics.Interpret(new(0, "Windows has scanned the file system and found no problems.", "", false)).State == CheckState.Clear, "Punctuated clean filesystem result recognized");
        foreach (string ambiguous in new[] {
            "Example: Windows has scanned the file system and found no problems",
            "Windows has scanned the file system and found no problems later",
            "Windows has scanned the file system and found no problems\nWindows has scanned the file system and found no problems",
            "Windows has scanned the file system and found no problems\nWindows has checked the file system and found no problems" })
            Require(StorageDiagnostics.Interpret(new(0, ambiguous, "", false)) is { State: CheckState.Unknown, Repair: null },
                "Embedded or duplicate filesystem verdict cannot establish a clean result");
        Require(StorageDiagnostics.Interpret(new(0, "unrecognized", "", false)).State == CheckState.Unknown, "Exit zero alone is not a filesystem verdict");
        Require(StorageDiagnostics.Interpret(new(3, "cannot check", "", false)).State == CheckState.Attention, "Incomplete filesystem check requires attention");
        Require(StorageDiagnostics.NormalizeVolume("d:\\") == "D:" && StorageDiagnostics.NormalizeVolume("C:") == "C:", "Selected volume normalization preserves the drive identity");
        Require(new WindowsWorkflowHost { StorageVolume = "d:\\" }.StorageVolume == "D:" &&
            new WindowsWorkflowHost().StorageVolume is null, "Workflow host retains selected volume and defaults to the system volume");
        var selectedVolumePlan = StorageDiagnostics.PlanCheck("d:\\");
        Require(selectedVolumePlan.Tool == "chkdsk.exe" && selectedVolumePlan.Arguments.SequenceEqual(["D:"]),
            "Selected-drive preview contains only the intended target and no repair or dismount switches");
        foreach (string invalidVolume in new[] { "", "C", "C:/", "C:\\folder", "\\\\server\\share", "C: /f", "/r", "1:", " C:" })
        {
            bool rejected = false;
            try { _ = new WindowsWorkflowHost { StorageVolume = invalidVolume }; } catch (ArgumentException) { rejected = true; }
            Require(rejected, "Volume selection rejects paths and injected repair switches");
        }
        var storageHost = new Host { State = CheckState.Repairable };
        var storage = await RecipeEngine.RunAsync(RecipeCatalog.ForWorkflow(WorkflowKind.Storage), true, folder, storageHost);
        Require(storageHost.Repairs == 0 && storage.Count == 2 && storage.All(result => result.Outcome == RecipeOutcome.RepairAvailable),
            "Option-dependent recipes retain eligibility without mutating when generic automatic repair is enabled");
        var verified = Success(RepairKind.ComponentStore);
        Require(Repairs.IsVerified(verified), "Complete matching evidence verifies repair");
        Require(!Repairs.IsVerified(verified with { Steps = [] }), "Success text alone cannot verify repair");
        Require(!Repairs.IsVerified(verified with { Steps = [.. verified.Steps.Select(s => s with { Result = s.Result with { ExitCode = 5 } })] }), "Failed commands cannot verify repair");
        var first = RecipeCatalog.All[0];
        var wmiRecipes = RecipeCatalog.ForWorkflow(WorkflowKind.Wmi);
        var wmiSuccess = Success(RepairKind.WindowsManagement);
        Require(Repairs.IsVerified(wmiSuccess), "WMI service repair verifies exact running identity");
        foreach (string wrongIdentity in new[] { "STATE : 4 RUNNING", "SERVICE_NAME: Spooler\nSTATE : 4 RUNNING", "SERVICE_NAME: winmgmt\nSERVICE_NAME: winmgmt\nSTATE : 4 RUNNING" })
            Require(!Repairs.IsVerified(wmiSuccess with { Steps = [.. wmiSuccess.Steps.Select(s => s.Step.Verification ? s with { Result = s.Result with { Output = wrongIdentity } } : s)] }),
                "Missing, wrong or duplicate service identity cannot verify WMI recovery");
        var recoveringWmi = new Host { State = CheckState.Repairable };
        var recoveredWmi = await RecipeEngine.RunAsync(wmiRecipes, true, folder, recoveringWmi);
        Require(recoveredWmi.Take(2).All(r => r.Outcome == RecipeOutcome.Repaired) && recoveredWmi[2].Outcome == RecipeOutcome.NeedsAttention && recoveringWmi.Checked.SequenceEqual([RepairKind.WindowsManagement, RepairKind.WmiRepository, RepairKind.WmiProvider]) &&
            recoveringWmi.Repairs == 2, "Verified WMI service recovery permits repository recovery in order");
        var healthyWmi = await RecipeEngine.RunAsync(wmiRecipes, false, folder, new Host { State = CheckState.Clear });
        Require(healthyWmi.All(r => r.Outcome == RecipeOutcome.Clear && r.Repair is null), "Healthy WMI workflow requires no repairs");
        Require(wmiRecipes.Select(r => r.Action).SequenceEqual([RepairKind.WindowsManagement, RepairKind.WmiRepository, RepairKind.WmiProvider]) &&
            wmiRecipes[1].Dependencies.SequenceEqual([wmiRecipes[0].Id]), "WMI service readiness precedes repository checks");
        var unavailableWmi = new Host { State = CheckState.Attention };
        var blockedWmi = await RecipeEngine.RunAsync(wmiRecipes, true, folder, unavailableWmi);
        Require(unavailableWmi.Checked.SequenceEqual([RepairKind.WindowsManagement]) && unavailableWmi.Repairs == 0 &&
            blockedWmi[1].Outcome == RecipeOutcome.Blocked, "Unavailable WMI service prevents repository execution");
        var dependent = RecipeCatalog.All[1] with { Dependencies = [first.Id] };
        var failedClearHost = new Host { State = CheckState.Clear, DiagnosticEvidence = new(5, "Failed provider", "", false) };
        var failedClear = await RecipeEngine.RunAsync([first, dependent], true, folder, failedClearHost);
        Require(failedClear[0].Outcome == RecipeOutcome.Unknown && failedClear[0].Check?.Evidence?.ExitCode == 5 &&
            failedClear[1].Outcome == RecipeOutcome.Blocked && failedClearHost.Checked.Count == 1 && failedClearHost.Repairs == 0,
            "Failed clear evidence cannot satisfy a dependent recipe or dispatch repair");
        var unrelated = RecipeCatalog.All[2];
        var host = new Host();
        var missingDiagnostic = new Host { MissingDiagnostic = true };
        var missingResults = await RecipeEngine.RunAsync([first, dependent, unrelated], true, folder, missingDiagnostic);
        Require(missingResults[0].Outcome == RecipeOutcome.Unknown && !missingResults[0].ExecutionUncertain &&
            missingResults[1].Outcome == RecipeOutcome.Blocked && missingResults[2].Outcome == RecipeOutcome.Unknown &&
            missingDiagnostic.Checked.Count == 2 && missingDiagnostic.Repairs == 0,
            "Missing diagnostic results block dependent work while preserving independent checks and cannot authorize repairs");
        foreach (Func<RepairResult, RepairResult> damage in new Func<RepairResult, RepairResult>[] {
            r => r with { Steps = null! }, r => r with { Steps = [] }, r => r with { Steps = [null!] },
            r => r with { Steps = [new(null!, new(0, "", "", false))] },
            r => r with { Steps = [new(r.Steps[0].Step, null!)] } })
        {
            var damagedHost = new Host { State = CheckState.Repairable, TransformRepair = damage };
            var damagedResults = await RecipeEngine.RunAsync([first, dependent, unrelated], true, folder, damagedHost);
            Require(damagedResults[0].Outcome == RecipeOutcome.Unknown && damagedResults[0].ExecutionUncertain &&
                damagedResults[0].Repair is not null && damagedResults.Skip(1).All(r => r.Outcome == RecipeOutcome.Blocked) &&
                damagedHost.Repairs == 1 && damagedHost.Checked.Count == 1,
                "Missing command evidence retains the provider result and blocks dependent and independent work");
            var damagedWorkflow = await Workflows.RunAsync(WorkflowKind.SystemFiles, true, folder,
                host: new Host { State = CheckState.Repairable, TransformRepair = damage });
            var retainedWorkflow = System.Text.Json.JsonSerializer.Deserialize<WorkflowResult>(File.ReadAllText(damagedWorkflow.JournalPath), Capture.JsonOptions)!;
            Require(retainedWorkflow.Recipes.Single().ExecutionUncertain && retainedWorkflow.Repairs.Count == 1 &&
                retainedWorkflow.RecoveryPlan.Action == RecoveryNextAction.ReviewInterruptedRun &&
                WorkflowReport.Format(retainedWorkflow).Contains("NEXT: Review the saved journal"),
                "Incomplete provider results survive workflow serialization and lead to review guidance");
        }
        foreach (string failure in new[] { "io", "cancel", "timeout", "null" })
        {
            var failedHost = new Host { State = CheckState.Repairable, RepairFailure = failure };
            var failedResults = await RecipeEngine.RunAsync([first, unrelated], true, folder, failedHost);
            Require(failedResults[0].ExecutionUncertain && failedResults[0].Outcome == RecipeOutcome.Unknown &&
                failedResults[1].Outcome == RecipeOutcome.Blocked && failedHost.Repairs == 1 && failedHost.Checked.Count == 1,
                "Repair provider failure without a result blocks independent subsequent work");
            var savedFailure = System.Text.Json.JsonSerializer.Deserialize<RecipeResult>(System.Text.Json.JsonSerializer.Serialize(failedResults[0]))!;
            Require(savedFailure.ExecutionUncertain, "Execution uncertainty survives workflow journaling");
            var failedWorkflow = await Workflows.RunAsync(WorkflowKind.SystemFiles, true, folder,
                host: new Host { State = CheckState.Repairable, RepairFailure = failure });
            var savedWorkflow = System.Text.Json.JsonSerializer.Deserialize<WorkflowResult>(File.ReadAllText(failedWorkflow.JournalPath), Capture.JsonOptions)!;
            Require(savedWorkflow.Status == "Needs attention" && savedWorkflow.Recipes.Single().ExecutionUncertain && savedWorkflow.Repairs.Count == 0,
                "Missing repair results remain uncertain in the saved workflow rather than reporting verified success");
        }
        var live = new LiveProgress();
        var results = await RecipeEngine.RunAsync([first, dependent, unrelated], true, folder, host, live);
        Require(results[1].Outcome == RecipeOutcome.Blocked && host.Checked.SequenceEqual([first.Action, unrelated.Action]), "Blocked dependency does not prevent independent recipes");
        Require(results[1].BlockingRecipes.SequenceEqual([first.Id]) && results[1].Detail.Contains(first.Title) &&
            results[1].Detail.Contains(results[0].Outcome.ToString()), "Blocked recipe identifies the prerequisite and its outcome");
        Require(live.Messages.First() == "Repair check 1 of 3: " + first.Title + "..." &&
            live.Messages.Any(message => message == "Repair check 3 of 3: " + unrelated.Title + "...") &&
            live.Messages.Any(message => message.Contains(dependent.Title + ": Blocked.") && message.Contains(first.Title)),
            "Live progress shows overall position and identifies blocked prerequisites without requiring report inspection");
        var restoredBlock = System.Text.Json.JsonSerializer.Deserialize<RecipeResult>(System.Text.Json.JsonSerializer.Serialize(results[1]))!;
        Require(restoredBlock.BlockingRecipes.SequenceEqual(results[1].BlockingRecipes), "Workflow JSON retains prerequisite references");
        host = new Host { State = CheckState.Repairable };
        var checkpoints = new List<IReadOnlyList<RecipeResult>>();
        var displayFailureHost = new Host { State = CheckState.Repairable };
        var displayFailureCheckpoints = new List<IReadOnlyList<RecipeResult>>();
        var displayFailureResults = await RecipeEngine.RunAsync([first], true, folder, displayFailureHost,
            new ThrowingProgress(), checkpoint: rows => { displayFailureCheckpoints.Add(rows); return Task.CompletedTask; });
        Require(displayFailureHost.Repairs == 1 && displayFailureResults[0].Outcome == RecipeOutcome.Repaired &&
            displayFailureCheckpoints[0][0].Outcome == RecipeOutcome.RepairAvailable &&
            displayFailureCheckpoints[^1][0].Outcome == RecipeOutcome.Repaired,
            "Display callback failure cannot bypass preparation or interrupt outcome persistence");
        results = await RecipeEngine.RunAsync([first], true, folder, host, checkpoint: r => { checkpoints.Add(r); return Task.CompletedTask; });
        Require(checkpoints[0][0].Outcome == RecipeOutcome.RepairAvailable && results[0].Outcome == RecipeOutcome.Repaired, "Diagnosis checkpoint precedes repair outcome");
        foreach (Exception saveFailure in new Exception[] { new IOException("Fixture journal failure"),
            new TimeoutException("Fixture journal timeout"), new OperationCanceledException("Fixture journal cancellation") })
        {
            foreach (bool afterRepair in new[] { false, true })
            {
                var persistenceHost = new Host { State = CheckState.Repairable, WrongResult = afterRepair };
                int saves = 0;
                try
                {
                    await RecipeEngine.RunAsync([first, RecipeCatalog.All[2]], true, folder, persistenceHost,
                        checkpoint: _ => ++saves == (afterRepair ? 2 : 1)
                            ? Task.FromException(saveFailure) : Task.CompletedTask);
                    throw new Exception("Journal failure was ignored");
                }
                catch (Exception error) when (ReferenceEquals(error, saveFailure)) { }
                Require(persistenceHost.Checked.Count == 1 && persistenceHost.Repairs == (afterRepair ? 1 : 0) &&
                    saves == (afterRepair ? 2 : 1),
                    "Checkpoint failure propagates unchanged without another inspection, mutation or checkpoint");
            }
        }
        foreach (bool afterRepair in new[] { false, true })
        foreach (bool missingSaveTask in new[] { false, true })
        {
            var persistenceHost = new Host { State = CheckState.Repairable };
            int saves = 0;
            var stalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var saveWatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await RecipeEngine.RunAsync([first, unrelated], true, folder, persistenceHost,
                    checkpoint: _ => ++saves == (afterRepair ? 2 : 1)
                        ? missingSaveTask ? null! : stalled.Task : Task.CompletedTask,
                    checkpointTimeout: TimeSpan.FromMilliseconds(20)).WaitAsync(TimeSpan.FromSeconds(3));
                throw new Exception("Incomplete checkpoint was accepted");
            }
            catch (TimeoutException) when (!missingSaveTask) { }
            catch (InvalidOperationException error) when (missingSaveTask && error.Message.Contains("checkpoint provider")) { }
            finally { stalled.TrySetResult(); }
            Require(saveWatch.Elapsed < TimeSpan.FromSeconds(2) && persistenceHost.Checked.Count == 1 && persistenceHost.Repairs == (afterRepair ? 1 : 0) &&
                saves == (afterRepair ? 2 : 1), "Incomplete checkpoint stops the batch before another repair or inspection");
        }
        Require(results[0].RecipeVersion == first.Version && results[0].RecipeId == first.Id, "Execution retains recipe identity and version");
        host = new Host { State = CheckState.Repairable, TimeoutRepair = true };
        results = await RecipeEngine.RunAsync([first, unrelated], true, folder, host);
        Require(host.Repairs == 1 && host.Checked.Count == 1 && results[1].Outcome == RecipeOutcome.Blocked, "Timeout prevents even independent recipes from overlapping unfinished Windows work");
        host = new Host { State = CheckState.Repairable, RestartRepair = true };
        results = await RecipeEngine.RunAsync([first, unrelated], true, folder, host);
        Require(host.Repairs == 1 && host.Checked.Count == 1 && results[0].Outcome == RecipeOutcome.RestartRequired && results[1].Outcome == RecipeOutcome.Blocked, "Restart return defers independent recipes and remains distinct from verified repair");
        var restartWorkflow = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, host: new Host { State = CheckState.Repairable, RestartRepair = true });
        Require(restartWorkflow.RequiresRestart && restartWorkflow.Status.StartsWith("Restart required") && restartWorkflow.Repairs.Count == 1, "Workflow exposes restart requirement without running later operations");
        Require(!Success(RepairKind.ComponentStore).RequiresRestart && !(Success(RepairKind.ComponentStore) with { Status = "Restart required" }).RequiresRestart, "Restart state uses recorded exit evidence, not status text");
        foreach (var invalid in new IReadOnlyList<RepairRecipe>[] { [first, first], [dependent], [first with { Dependencies = [first.Id] }] })
        {
            host = new Host();
            try { await RecipeEngine.RunAsync(invalid, true, folder, host); throw new Exception("Invalid recipe graph accepted"); }
            catch (ArgumentException) { Require(host.Checked.Count == 0, "Invalid graph rejected before checks"); }
        }
        foreach (var invalidDeadline in new[] { TimeSpan.Zero, TimeSpan.FromTicks(-1), TimeSpan.FromHours(4) + TimeSpan.FromTicks(1) })
        {
            var invalidHost = new Host { State = CheckState.Repairable };
            try
            {
                await RecipeEngine.RunAsync([first with { ExecutionTimeout = invalidDeadline }], true, folder, invalidHost);
                throw new Exception("Invalid execution deadline accepted");
            }
            catch (ArgumentException)
            { Require(invalidHost.Checked.Count == 0 && invalidHost.Repairs == 0, "Invalid execution deadline rejected before dispatch"); }
        }
        host = new Host { State = CheckState.Repairable, WrongAction = true };
        results = await RecipeEngine.RunAsync([first], true, folder, host);
        Require(results[0].Outcome == RecipeOutcome.Unknown && host.Repairs == 0, "Check cannot substitute an unrelated repair action");
        host = new Host { State = CheckState.Repairable, WrongResult = true };
        results = await RecipeEngine.RunAsync([first, unrelated], true, folder, host);
        Require(results[0].Outcome == RecipeOutcome.Unknown && results[0].Repair is not null &&
            results[1].Outcome == RecipeOutcome.Blocked && host.Repairs == 1 && host.Checked.Count == 1,
            "Mismatched repair result is retained as unverified and stops independent repairs");
        var mismatchedWorkflow = await Workflows.RunAsync(WorkflowKind.SystemFiles, true, folder,
            host: new Host { State = CheckState.Repairable, WrongResult = true });
        Require(mismatchedWorkflow.Status == "Needs attention" &&
            WorkflowReport.Format(mismatchedWorkflow).Contains("different repair identity") &&
            !WorkflowReport.Format(mismatchedWorkflow).Contains("Repaired and verified"),
            "Mismatched provider evidence reaches the report without a verified claim");
        var forged = mismatchedWorkflow with { Recipes = [.. mismatchedWorkflow.Recipes.Select(r => r with { Outcome = RecipeOutcome.Repaired })] };
        var failedRecipe = mismatchedWorkflow.Recipes.First(r => r.Repair is not null);
        string failureText = "Service start deferred: disabled dependency\n" + new string('x', 1200);
        var failedReport = mismatchedWorkflow with { Recipes = [failedRecipe with
        {
            Repair = failedRecipe.Repair! with { Steps = [new(new("sc.exe", ["start", "Audiosrv"], false), new(-1, "", failureText, false))] }
        }] };
        string formattedFailure = WorkflowReport.Format(failedReport);
        Require(formattedFailure.Contains("Details: Repair command exit code -1. Service start deferred: disabled dependency ") &&
            formattedFailure.Contains("[shortened; recorded output in journal]") && !formattedFailure.Contains(new string('x', 1001)),
            "Workflow output shows bounded provider failure details without losing full journal evidence");
        Require(WorkflowReport.Format(forged).Contains("Verification incomplete") && !WorkflowReport.Format(forged).Contains("Repaired and verified"),
            "Report checks action identity even when supplied a repaired outcome");
        host = new Host();
        results = await RecipeEngine.RunAsync([first, dependent], true, folder, host, token: new CancellationToken(true));
        Require(results.All(r => r.Outcome == RecipeOutcome.Canceled) && host.Checked.Count == 0, "Pre-canceled run records skipped recipes without execution");
        host = new Host { InternalCancellation = true };
        using (var stalledStop = new CancellationTokenSource())
        {
            var late = new TaskCompletionSource<WorkflowCheck>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stalledHost = new Host { InspectionOverride = () => { stalledStop.Cancel(); return late.Task; } };
            var stopped = await RecipeEngine.RunAsync([first, dependent], true, folder, stalledHost, token: stalledStop.Token).WaitAsync(TimeSpan.FromSeconds(3));
            Require(stopped.All(r => r.Outcome == RecipeOutcome.Canceled) && stalledHost.Repairs == 0 && stalledHost.Checked.Count == 1,
                "Stop releases a stalled diagnostic provider without reaching repair or dependent checks");
            late.SetResult(new("Late fault", CheckState.Repairable, "fixture", first.Action));
            Require(stalledHost.Repairs == 0 && stopped.All(r => r.Repair is null), "Late diagnostic evidence cannot authorize repair after Stop");
        }
        foreach (bool cancelRepair in new[] { false, true })
        {
            using var repairStop = new CancellationTokenSource();
            var lateRepair = new TaskCompletionSource<RepairResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stalledRepairHost = new Host { State = CheckState.Repairable, RepairOverride = () =>
                { if (cancelRepair) repairStop.Cancel(); return lateRepair.Task; } };
            var repairSnapshots = new List<IReadOnlyList<RecipeResult>>();
            var repairResults = await RecipeEngine.RunAsync([first with { ExecutionTimeout = TimeSpan.FromMilliseconds(50) }, dependent],
                true, folder, stalledRepairHost, token: repairStop.Token,
                checkpoint: snapshot => { repairSnapshots.Add(snapshot); return Task.CompletedTask; }).WaitAsync(TimeSpan.FromSeconds(3));
            Require(repairResults[0].ExecutionUncertain && repairResults[0].Repair is null && stalledRepairHost.Repairs == 1 &&
                stalledRepairHost.Checked.Count == 1 && repairResults[0].Outcome == (cancelRepair ? RecipeOutcome.Canceled : RecipeOutcome.Unknown) &&
                repairResults[1].Outcome == (cancelRepair ? RecipeOutcome.Canceled : RecipeOutcome.Blocked),
                "Repair Stop and deadline release stalled providers while preventing subsequent repairs");
            Require(repairSnapshots.Count == 3 && repairSnapshots[0][0].Outcome == RecipeOutcome.RepairAvailable &&
                repairSnapshots[1][0].ExecutionUncertain && repairSnapshots[2].SequenceEqual(repairResults),
                "Checkpoints retain pre-dispatch diagnosis, uncertain execution and final blocked or canceled recipes");
            lateRepair.SetResult(Success(first.Action));
            Require(repairResults[0].ExecutionUncertain && repairResults[0].Repair is null,
                "Late repair completion cannot replace an uncertain published result");
        }
        var lateDeadline = new TaskCompletionSource<WorkflowCheck>();
        var deadlineHost = new Host { InspectionOverride = () => lateDeadline.Task };
        var expired = await RecipeEngine.RunAsync([first with { InspectionTimeout = TimeSpan.FromMilliseconds(50) }, dependent],
            true, folder, deadlineHost).WaitAsync(TimeSpan.FromSeconds(3));
        Require(expired[0] is { Outcome: RecipeOutcome.Unknown, Check.Evidence.TimedOut: true } && expired[1].Outcome == RecipeOutcome.Blocked && deadlineHost.Repairs == 0,
            "Recipe deadline retains timeout evidence and prevents subsequent recovery while provider may still run");
        lateDeadline.SetResult(new("Late fixture", CheckState.Repairable, "Late", first.Action));
        Require(deadlineHost.Repairs == 0, "Expired diagnostic cannot authorize a late repair");
        var missingTask = await RecipeEngine.RunAsync([first], true, folder, new Host { InspectionOverride = () => null! });
        Require(missingTask.Single().Outcome == RecipeOutcome.Unknown && missingTask[0].Repair is null,
            "Missing diagnostic task becomes an unknown result without mutation");
        results = await RecipeEngine.RunAsync([first], true, folder, host);
        Require(results.Single().Outcome == RecipeOutcome.Unknown && host.Repairs == 0, "Internal cancellation is an incomplete result, not a user stop or a clean check");
        foreach (var evidence in new[] { new ToolResult(0, "synthetic", "", true), new ToolResult(0, "synthetic", "", false) { TimedOut = true },
            new ToolResult(0, "synthetic", "Fixture command failure", false) })
        {
            host = new Host { State = CheckState.Repairable, DiagnosticEvidence = evidence };
            results = await RecipeEngine.RunAsync([first], true, folder, host);
            Require(host.Repairs == 0 && results.Single().Outcome == RecipeOutcome.Unknown && results[0].Check?.Repair is null, "Incomplete diagnostic evidence cannot authorize mutation even if a host proposes repair");
            host = new Host { State = CheckState.Clear, DiagnosticEvidence = evidence };
            results = await RecipeEngine.RunAsync([first, dependent], false, folder, host);
            Require(results[0].Outcome == RecipeOutcome.Unknown && results[1].Outcome == RecipeOutcome.Blocked && ReferenceEquals(results[0].Check?.Evidence, evidence),
                "Incomplete evidence cannot establish a clear prerequisite and remains available for review");
        }
        string[] mutableDependencies = [first.Id];
        RepairRecipe[] mutableRecipes = [first, dependent with { Dependencies = mutableDependencies }];
        host = new Host { State = CheckState.Clear };
        results = await RecipeEngine.RunAsync(mutableRecipes, false, folder, host, checkpoint: _ =>
        {
            mutableDependencies[0] = "changed-after-start";
            mutableRecipes[1] = unrelated;
            return Task.CompletedTask;
        });
        Require(results[1].RecipeId == dependent.Id && results[1].Outcome == RecipeOutcome.Clear && host.Checked[1] == dependent.Action,
            "Caller changes cannot replace an active recipe or its dependency graph");
        Console.WriteLine("PASS: recipe graph validation, independent branches, action matching, versioning and checkpoints.");
    }
    private sealed class ThrowingProgress : IProgress<string>
    {
        public void Report(string value) => throw new InvalidOperationException("Fixture display failure");
    }
    private sealed class LiveProgress : IProgress<string>
    {
        internal List<string> Messages { get; } = [];
        public void Report(string value) => Messages.Add(value);
    }
    private sealed class Host : IWorkflowHost
    {
        public CheckState State = CheckState.Unknown;
        public bool WrongAction;
        public bool WrongResult;
        public bool TimeoutRepair;
        public bool RestartRepair;
        public bool InternalCancellation;
        public bool MissingDiagnostic;
        public Func<Task<WorkflowCheck>>? InspectionOverride;
        public Func<Task<RepairResult>>? RepairOverride;
        public string? RepairFailure;
        public Func<RepairResult, RepairResult>? TransformRepair;
        public ToolResult? DiagnosticEvidence;
        public int Repairs;
        public List<RepairKind> Checked = [];
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([new("Administrator", CheckState.Clear, "Synthetic prerequisite"), new("Restart", CheckState.Clear, "Synthetic prerequisite")]);
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
        { Checked.Add(kind); if (InspectionOverride is not null) return InspectionOverride(); if (InternalCancellation) throw new OperationCanceledException(); if (MissingDiagnostic) return Task.FromResult<WorkflowCheck>(null!); return Task.FromResult(new WorkflowCheck("Synthetic", State, "Synthetic", WrongAction ? RepairKind.PrintSpooler : kind, DiagnosticEvidence)); }
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        {
            Repairs++;
            if (RepairOverride is not null) return RepairOverride();
            if (RepairFailure is not null)
            {
                if (RepairFailure == "cancel") throw new OperationCanceledException();
                if (RepairFailure == "timeout") throw new TimeoutException("Synthetic provider timeout");
                if (RepairFailure == "null") return Task.FromResult<RepairResult>(null!);
                throw new IOException("Synthetic provider failed after dispatch");
            }
            var result = Success(WrongResult ? RepairKind.PrintSpooler : kind);
            if (TransformRepair is not null) result = TransformRepair(result);
            if (RestartRepair) result = result with { Status = "Restart required", Steps = [result.Steps[0] with { Result = new(3010, "Restart required", "", false) }] };
            if (TimeoutRepair) result = result with { Steps = [.. result.Steps.Select(s => s with { Result = s.Result with { TimedOut = true } })] };
            return Task.FromResult(result);
        }
    }
    internal static RepairResult Success(RepairKind kind)
    {
        if (kind == RepairKind.DnsCache)
        {
            var selected = new DnsLookupResult("fixture.example", DnsLookupState.Failed, [], "fixture") { ErrorCode = System.Net.Sockets.SocketError.HostNotFound };
            var flush = new ToolResult(0, "Successfully flushed the DNS Resolver Cache.", "", false);
            var recovery = new DnsCacheRecoveryResult("fixture.example", DnsCacheRecoveryOutcome.Verified,
                "The selected hostname now resolves.", selected, selected, flush,
                new("fixture.example", DnsLookupState.Resolved, ["192.0.2.30"], "fixture"), "synthetic", true);
            return new("Synthetic", kind, DateTimeOffset.UtcNow, recovery.Detail, "synthetic",
                [new(DnsCacheRecovery.FlushStep(), flush)]) { DnsCacheRecovery = recovery };
        }
        if (kind == RepairKind.DefenderSignatures) return new("Synthetic", kind, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
            [.. Repairs.Plan(kind).Steps.Select(step => new RepairStepResult(step, new(0, DefenderChecks.Fresh, "", false)))]);
        if (kind == RepairKind.PrintQueues)
        {
            var plan = PrintRecovery.Plan(["A"]);
            return new("Synthetic", kind, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
                [new(plan.Steps[0], new(0, "{\"count\":1,\"paused\":0,\"targets\":[\"A\"],\"names\":[\"A\"],\"pausedNames\":[],\"waitingNames\":[\"A\"]}", "", false)),
                 new(plan.Steps[1], new(0, "{\"count\":1,\"paused\":0,\"targets\":[],\"names\":[\"A\"],\"pausedNames\":[]}", "", false))])
            { SelectedPrinterQueues = ["A"] };
        }
        if (kind == RepairKind.PrinterOffline) return new("Synthetic", kind, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
            [.. Repairs.Plan(kind).Steps.Select(step => new RepairStepResult(step, new(0, "{\"targets\":[\"A\"],\"queues\":[{\"id\":\"A\",\"offline\":false}]}", "", false)))]);
        if (kind == RepairKind.DhcpLease) return new("Synthetic", kind, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
            [.. Repairs.Plan(kind).Steps.Select(step => new RepairStepResult(step, new(0, "{\"targets\":[1],\"adapters\":[{\"id\":1,\"settingId\":\"11111111-1111-1111-1111-111111111111\",\"addresses\":[\"192.0.2.10\"]}]}", "", false)))]);
        if (kind == RepairKind.PrintSpoolDirectory)
        {
            string path = Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS");
            string snapshotJson = System.Text.Json.JsonSerializer.Serialize(new PrintSpoolDirectorySnapshot(true, path, path,
                true, false, DriveType.Fixed, 10L * 1024 * 1024 * 1024, true), Capture.JsonOptions);
            return new("Synthetic", kind, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
                [.. Repairs.Plan(kind).Steps.Select(step => new RepairStepResult(step, new(0, snapshotJson, "", false)))]);
        }
        string output = kind == RepairKind.ComponentStore ? "No component store corruption detected" : kind == RepairKind.SystemFiles ? "Windows Resource Protection did not find any integrity violations" : kind == RepairKind.PrintQueues ? "{\"count\":1,\"paused\":0}" : kind == RepairKind.WmiRepository ? "WMI repository is consistent" : kind == RepairKind.TimeSynchronization ? "Leap Indicator: 0(no warning)\nStratum: 3\nSource: dc.example.test\nLast Sync Error: 0" : kind == RepairKind.DeliveryOptimization ? "SERVICE_NAME: DoSvc\nSTATE : 4 RUNNING" : "STATE : 4 RUNNING";
        if (ServiceRecovery.ServiceName(kind) is { } service) output = "SERVICE_NAME: " + service + "\n" + output;
        return new("Synthetic", kind, DateTimeOffset.UtcNow, "Verification reported", "synthetic",
            [.. Repairs.Plan(kind).Steps.Select(step => new RepairStepResult(step, new(0, output, "", false)))]);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
