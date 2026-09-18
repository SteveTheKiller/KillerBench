using KillerBench.Engine;
using System.IO;
using System.Text.Json;

internal static class WorkflowChecks
{
    internal static async Task RunAsync(string folder)
    {
        foreach (bool otherFault in new[] { false, true })
        {
            var elevationHost = new Host { InspectionOverride = [new("Administrator", CheckState.Attention, "Elevation required"),
                new("Search history", otherFault ? CheckState.Unknown : CheckState.Information, "Fixture history")] };
            var elevation = await Workflows.RunAsync(WorkflowKind.Search, true, folder, host: elevationHost);
            Require(elevation.Status == (otherFault ? "Needs attention" : "Administrator access required; workflow incomplete") &&
                elevation.GetExitCode() == 3 && elevationHost.Scanned.Count == 0 && elevationHost.Repaired.Count == 0 &&
                elevation.RecoveryPlan.Action == RecoveryNextAction.ElevateAndRecheck,
                "Elevation-only outcome remains incomplete and does not hide other findings or dispatch work");
            using var elevationJournal = JsonDocument.Parse(await File.ReadAllTextAsync(elevation.JournalPath));
            Require(elevationJournal.RootElement.GetProperty("status").GetString() == elevation.Status,
                "Elevation outcome is retained in the workflow journal");
        }
        var focusedHost = new Host();
        var focusedWorkflow = await Workflows.RunAsync(WorkflowKind.CommonProblems, false, folder, host: focusedHost,
            selectedDiagnosticScopes: [CommonDiagnosticScope.PrinterPorts, CommonDiagnosticScope.Vitals, CommonDiagnosticScope.PrinterPorts]);
        Require(focusedWorkflow.SelectedDiagnosticScopes!.SequenceEqual(new[] { CommonDiagnosticScope.Vitals, CommonDiagnosticScope.PrinterPorts }) &&
            focusedWorkflow.PlannedRecipeIds.Count == 0 && focusedHost.Scanned.Count == 0 && focusedHost.Repaired.Count == 0,
            "Focused workflow retains selected areas without adding integrity scans or repair recipes");
        using (var focusedJournal = JsonDocument.Parse(await File.ReadAllTextAsync(focusedWorkflow.JournalPath)))
            Require(focusedJournal.RootElement.GetProperty("selectedDiagnosticScopes").GetArrayLength() == 2 &&
                focusedJournal.RootElement.GetProperty("plannedRecipeIds").GetArrayLength() == 0, "Journal retains focused scope and empty recipe plan");
        var bitsReadOnlyHost = new Host { InspectionOverride = [new("Background transfer operational correlation", CheckState.Information, "Fixture readiness")] };
        var bitsReadOnly = await Workflows.RunAsync(WorkflowKind.BackgroundTransfers, false, folder, host: bitsReadOnlyHost);
        Require(bitsReadOnly.Status == "Transfer inventory completed; no jobs changed" && bitsReadOnly.GetExitCode() == 0 &&
            bitsReadOnly.PlannedRecipeIds.Count == 0 && bitsReadOnly.Recipes.Count == 0 &&
            bitsReadOnlyHost.Scanned.Count == 0 && bitsReadOnlyHost.Repaired.Count == 0,
            "Default background-transfer workflow remains read only and does not evaluate repair recipes");
        var computerNameHost = new Host { InspectionOverride = [new("Computer name change", CheckState.Attention,
            "Fixture read-only readiness", Evidence: new(0, "{}", "", false))] };
        var computerNameReview = await Workflows.RunAsync(WorkflowKind.ComputerName, false, folder, host: computerNameHost);
        Require(computerNameReview.PlannedRecipeIds.Count == 0 && computerNameReview.Recipes.Count == 0 &&
            computerNameReview.Checks.Count == 1 && computerNameHost.Scanned.Count == 0,
            "Default computer-name workflow keeps exact readiness review separate from guarded rename eligibility");
        var selectedComputerNameHost = new Host { State = CheckState.Repairable, InspectionOverride =
            [new("Computer name change", CheckState.Attention, "Fixture read-only readiness")] };
        var selectedComputerName = await Workflows.RunAsync(WorkflowKind.ComputerName, false, folder,
            host: selectedComputerNameHost, selectedRecipes: ["windows.computer-name"]);
        Require(selectedComputerName.PlannedRecipeIds.SequenceEqual(["windows.computer-name"]) &&
            selectedComputerNameHost.Scanned.SequenceEqual([RepairKind.ComputerRename]) && selectedComputerNameHost.Repaired.Count == 0,
            "Explicit computer-name recipe selection retains guarded eligibility without executing rename");
        var standardUserServiceHost = new Host { State = CheckState.Clear, InspectionOverride =
            [new("Administrator", CheckState.Information, "Diagnosis available; execution requires elevation"),
             new("Restart", CheckState.Clear, "No supported restart source")] };
        var standardUserService = await Workflows.RunAsync(WorkflowKind.BitsService, false, folder, host: standardUserServiceHost);
        Require(standardUserService.GetExitCode() == 0 && standardUserServiceHost.Scanned.SequenceEqual([RepairKind.BackgroundTransfer]) &&
            standardUserServiceHost.Repaired.Count == 0 && standardUserService.Recipes.Single().Outcome == RecipeOutcome.Clear,
            "Informational standard-user context permits exact service diagnosis while repair execution remains unrequested");
        try
        {
            await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: focusedHost,
                selectedDiagnosticScopes: [CommonDiagnosticScope.Vitals]);
            throw new Exception("Focused scan accepted automatic repair");
        }
        catch (ArgumentException) { }
        using (var focusedStop = new CancellationTokenSource())
        {
            var partialFocused = await Workflows.RunAsync(WorkflowKind.CommonProblems, false, folder,
                token: focusedStop.Token, host: new FocusedStopHost(focusedStop.Cancel),
                selectedDiagnosticScopes: [CommonDiagnosticScope.Vitals, CommonDiagnosticScope.PrinterPorts])
                .WaitAsync(TimeSpan.FromSeconds(3));
            Require(partialFocused.GetExitCode() == 130 && partialFocused.Checks.Count == 1 &&
                partialFocused.Checks[0].Name == "Retained focused finding" && partialFocused.Repairs.Count == 0 &&
                partialFocused.SelectedDiagnosticScopes!.Count == 2,
                "Stop preserves focused selection and saved partial findings when the host does not finish");
            using var stoppedFocusedJournal = JsonDocument.Parse(await File.ReadAllTextAsync(partialFocused.JournalPath));
            Require(stoppedFocusedJournal.RootElement.GetProperty("status").GetString()!.StartsWith("Stopped") &&
                stoppedFocusedJournal.RootElement.GetProperty("checks").GetArrayLength() == 1 &&
                stoppedFocusedJournal.RootElement.GetProperty("selectedDiagnosticScopes").GetArrayLength() == 2,
                "Stopped focused journal retains partial coverage and requested areas");
        }
        var outcome = new WorkflowResult(WorkflowKind.Vitals, "Fixture", DateTimeOffset.UtcNow, "Inspection completed",
            [new("Fixture", CheckState.Information, "Observed")], [], "fixture");
        Require(outcome.GetExitCode() == 0 && (outcome with { Checks = [] }).GetExitCode() == 3 &&
            (outcome with { Checks = [new("Fixture", CheckState.Unknown, "Unavailable")] }).GetExitCode() == 3 &&
            (outcome with { Status = "Stopped" }).GetExitCode() == 130 &&
            (outcome with { Status = "Running" }).GetExitCode() == 3,
            "Workflow exit codes require evidence and preserve incomplete and stopped outcomes");
        var subsetHost = new Host { State = CheckState.Clear };
        var observerHost = new Host { State = CheckState.Clear, InspectionOverride =
        [
            new("Administrator", CheckState.Clear, "fixture"),
            new("Restart", CheckState.Clear, "fixture"),
            new("System disk", CheckState.Clear, "fixture"),
            new("Servicing transition readiness", CheckState.Information, "fixture"),
            new("Windows reserved storage", CheckState.Information, "fixture"),
            new("CBS servicing log", CheckState.Information, "fixture"),
            new("DISM servicing log", CheckState.Information, "fixture"),
            new("Servicing log coverage", CheckState.Information, "fixture")
        ] };
        var observerResult = await Workflows.RunAsync(WorkflowKind.SystemFiles, false, folder,
            progress: new BrokenProgress(), host: observerHost);
        Require(observerResult.GetExitCode() == 0 && observerHost.Scanned.Count > 0 && observerHost.Repaired.Count == 0 &&
            File.Exists(observerResult.JournalPath), "Workflow progress failures do not prevent checks or final journal persistence");
        foreach (var failedEvidence in new[] { new ToolResult(5, "", "", false), new ToolResult(0, "", "provider error", false),
            new ToolResult(0, "partial", "", true), new ToolResult(0, "", "", false) { TimedOut = true } })
        {
            foreach (string prerequisite in new[] { "Administrator", "Restart", "System disk", "Installer readiness" })
            {
                var prerequisiteHost = new Host { State = CheckState.Repairable,
                    InspectionOverride = [new(prerequisite, CheckState.Clear, "Reported clear", Evidence: failedEvidence)] };
                var gated = await Workflows.RunAsync(WorkflowKind.Installer, true, folder, host: prerequisiteHost);
                Require(gated.Status == "Needs attention" && gated.GetExitCode() == 3 &&
                    prerequisiteHost.Scanned.Count == 0 && prerequisiteHost.Repaired.Count == 0,
                    "Contradictory initial prerequisite evidence blocks diagnostic dispatch and automatic repair: " + prerequisite);
                using var gatedJournal = JsonDocument.Parse(await File.ReadAllTextAsync(gated.JournalPath));
                Require(gatedJournal.RootElement.GetProperty("status").GetString() == "Needs attention" &&
                    gatedJournal.RootElement.GetProperty("repairs").GetArrayLength() == 0,
                    "Final journal records unresolved prerequisite evidence without repair claims");
            }
            var contradicted = outcome with { Checks = [new("Contradicted check", CheckState.Clear, "Fixture", Evidence: failedEvidence)] };
            Require(contradicted.GetExitCode() == 3 && contradicted.Summary.ClearChecks == 0 && contradicted.Summary.Incomplete == 1 &&
                WorkflowReport.Format(contradicted).Contains("Contradicted check: Unknown"),
                "Contradictory command evidence cannot produce successful workflow exit, summary or report");
            Require(contradicted.RecoveryPlan.Action != RecoveryNextAction.TestAffectedFeature &&
                !WorkflowReport.Format(contradicted).Contains("The selected checks finished."),
                "Incomplete evidence cannot receive clean completion guidance");
            var recipeCheck = contradicted.Checks[0];
            var recipeContradiction = outcome with { Checks = [], Recipes =
                [new("fixture.clear", 1, RecipeOutcome.Clear, recipeCheck, null, "Recorded clear")] };
            Require(recipeContradiction.GetExitCode() == 3 && recipeContradiction.Summary is { ClearChecks: 0, Incomplete: 1 } &&
                WorkflowReport.Format(recipeContradiction).Contains("fixture.clear: Could not determine") &&
                !WorkflowReport.Format(recipeContradiction).Contains("No issue detected in this check"),
                "Recipe-only contradictory evidence remains incomplete in exit codes and reports");
        }
        var subset = await Workflows.RunAsync(WorkflowKind.CommonProblems, false, folder, host: subsetHost, selectedRecipes: ["windows.system-files"]);
        Require(subset.Recipes.Count == 2 && subsetHost.Scanned.SequenceEqual(new[] { RepairKind.ComponentStore, RepairKind.SystemFiles }) && subsetHost.Repaired.Count == 0,
            "Journaled selected workflow includes servicing prerequisite without running unrelated recipes");
        using (var subsetJournal = JsonDocument.Parse(await File.ReadAllTextAsync(subset.JournalPath)))
            Require(subsetJournal.RootElement.GetProperty("requestedRecipeIds").GetArrayLength() == 1 && subsetJournal.RootElement.GetProperty("plannedRecipeIds").GetArrayLength() == 2,
                "Journal distinguishes requested recipes from expanded prerequisites");
        var emptyHost = new Host { State = CheckState.Repairable };
        var emptySelection = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: emptyHost, selectedRecipes: []);
        Require(emptySelection.Status == "Inspection completed; no recipes selected" && emptyHost.Scanned.Count == 0 && emptyHost.Repaired.Count == 0,
            "Explicit empty selection never expands into automatic repairs");
        bool invalidSelection = false;
        try { await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: emptyHost, selectedRecipes: ["unknown"]); }
        catch (ArgumentException) { invalidSelection = true; }
        Require(invalidSelection && emptyHost.Inspections == 1, "Invalid recipe selection is rejected before inspecting the machine");
        var repairedFixture = RecipeChecks.Success(RepairKind.SystemFiles);
        var repairCheck = new WorkflowCheck("SystemFiles", CheckState.Repairable, "Fixture corruption", RepairKind.SystemFiles);
        var verifiedWorkflow = outcome with
        {
            Status = "Repairs verified in checked scopes", Checks = [repairCheck], Repairs = [repairedFixture],
            Recipes = [new("windows.system-files", 1, RecipeOutcome.Repaired, repairCheck, repairedFixture, "Verified fixture")]
        };
        Require(verifiedWorkflow.GetExitCode() == 0, "Verified recipe recovery is successful despite the retained original finding");
        var restartRepair = repairedFixture with
        {
            Status = "Restart required",
            Steps = [repairedFixture.Steps[0] with { Result = new ToolResult(3010, "Restart required", "", false) }]
        };
        var restartRepairWorkflow = verifiedWorkflow with { Status = "Repairs verified in checked scopes", Repairs = [restartRepair] };
        Require(restartRepairWorkflow.RequiresRestart && restartRepairWorkflow.GetExitCode() == 4 &&
            restartRepairWorkflow.RecoveryPlan.Action == RecoveryNextAction.RestartThenRecheck,
            "Recorded repair restart evidence overrides a stale completion outcome");
        Require((verifiedWorkflow with { Recipes = [verifiedWorkflow.Recipes[0] with { ExecutionUncertain = true }] }).GetExitCode() == 3,
            "Explicit execution uncertainty cannot claim success");
        foreach (var state in Enum.GetValues<RecipeOutcome>())
        {
            var uncertain = verifiedWorkflow with { Recipes = [verifiedWorkflow.Recipes[0] with { Outcome = state, ExecutionUncertain = true }] };
            Require(uncertain.Summary is { Incomplete: 1, VerifiedRepairs: 0, ClearChecks: 0, AvailableRepairs: 0 },
                "Execution uncertainty overrides summary labels: " + state);
        }
        foreach (var recipeOutcome in new[] { RecipeOutcome.Unknown, RecipeOutcome.Blocked, RecipeOutcome.NeedsAttention,
            RecipeOutcome.RepairAvailable, RecipeOutcome.Canceled, RecipeOutcome.RestartRequired })
        {
            var reviewed = verifiedWorkflow with { Recipes = [verifiedWorkflow.Recipes[0] with { Outcome = recipeOutcome }] };
            Require(reviewed.GetExitCode() == (recipeOutcome == RecipeOutcome.RestartRequired ? 4 : 3),
                "Recipe outcome overrides a stale success label: " + recipeOutcome);
        }
        var unverifiedRepair = repairedFixture with { Steps = [] };
        Require((verifiedWorkflow with { Repairs = [unverifiedRepair] }).GetExitCode() == 3 &&
            (verifiedWorkflow with { Recipes = [verifiedWorkflow.Recipes[0] with { Repair = unverifiedRepair }] }).GetExitCode() == 3,
            "Both top-level and recipe repair evidence must verify success");
        foreach (var failure in new ToolResult[] {
            new(5, "Fixture stdout failure", "", false), new(5, "", "", false),
            new(0, "Fixture truncated output", "", true), new(-1, "", "", false) { TimedOut = true },
            new(-1, "STATE : 2\nSTART_PENDING", "Fixture observation timeout", false) { TimedOut = true },
            new(-1, new string('x', 1200), "Fixture observation timeout", false) { TimedOut = true },
            new(5, new string('x', 1200), "", false) })
        {
            var failedRepair = repairedFixture with { Steps = [new(Repairs.Plan(RepairKind.SystemFiles).Steps[0], failure)] };
            var failureWorkflow = new WorkflowResult(WorkflowKind.SystemFiles, "fixture", DateTimeOffset.UtcNow, "Needs attention", [], [failedRepair], "fixture")
            { Recipes = [new("windows.system-files", 1, RecipeOutcome.Unknown,
                new("SystemFiles", CheckState.Repairable, "fixture", RepairKind.SystemFiles), failedRepair, "fixture")] };
            string report = WorkflowReport.Format(failureWorkflow);
            Require(report.Contains("Details: Repair command"), "Every failed or incomplete command has a visible explanation");
            Require(!failure.Truncated || report.Contains("captured output incomplete"), "Truncated output remains explicit");
            Require(!failure.TimedOut || report.Contains("timed out"), "Timeout is visible even with no returned text");
            if (failure.TimedOut && failure.Output.Length > 0 && failure.Error.Length > 0)
                Require(report.Contains("Last captured output (not current state): ") && report.Contains(failure.Error) &&
                    report.Contains(failure.Output[..Math.Min(1000, failure.Output.Length)].Replace('\n', ' ')),
                    "Timeout report retains both failure explanation and bounded historical output");
            Require(failure.Output != "Fixture stdout failure" || report.Contains(failure.Output), "Failure text on stdout is retained in the report");
            Require(failure.Output.Length <= 1000 || report.Contains("shortened; recorded output in journal") && !report.Contains(new string('x', 1001)),
                "Report bounds long failure output without claiming complete capture");
        }
        var originalCheck = new WorkflowCheck("SystemFiles", CheckState.Repairable, "fixture", RepairKind.SystemFiles);
        var recoveryFixture = new WorkflowResult(WorkflowKind.SystemFiles, "fixture", DateTimeOffset.UtcNow, "Needs attention", [], [], "fixture")
        { Recipes = [new("windows.system-files", 1, RecipeOutcome.RepairAvailable, originalCheck, null, "fixture")] };
        Require(recoveryFixture.RecoveryPlan.Action == RecoveryNextAction.ReviewAvailableRepairs &&
            recoveryFixture.RecoveryPlan.AvailableRecipeIds.SequenceEqual(new[] { "windows.system-files" }),
            "Recovery plan exposes matching eligible recipe identities");
        foreach (var duplicateOutcome in new[] { RecipeOutcome.RepairAvailable, RecipeOutcome.Clear, RecipeOutcome.Unknown })
        {
            var duplicateRecipe = recoveryFixture.Recipes[0] with { Outcome = duplicateOutcome };
            foreach (bool reverse in new[] { false, true })
            {
                var duplicateRecovery = recoveryFixture with { Recipes = reverse
                    ? [duplicateRecipe, recoveryFixture.Recipes[0]] : [recoveryFixture.Recipes[0], duplicateRecipe] };
                Require(duplicateRecovery.RecoveryPlan.Action == RecoveryNextAction.ReviewFindings &&
                    duplicateRecovery.RecoveryPlan.AvailableRecipeIds.Count == 0,
                    "Duplicate recipe outcomes require review instead of selecting an apparently eligible record");
            }
        }
        var uncertainRecovery = recoveryFixture with { Recipes = [.. recoveryFixture.Recipes,
            new("interrupted", 1, RecipeOutcome.Unknown, null, null, "fixture") { ExecutionUncertain = true }] };
        Require(uncertainRecovery.RecoveryPlan.Action == RecoveryNextAction.ReviewInterruptedRun && uncertainRecovery.RecoveryPlan.AvailableRecipeIds.Count == 0 &&
            WorkflowReport.Format(uncertainRecovery).Contains("NEXT: Review the saved journal"),
            "Uncertain execution takes priority over an available repair in JSON and text guidance");
        var timedRecovery = recoveryFixture with { Checks = [new("Timed fixture", CheckState.Unknown, "fixture", Evidence: new(-1, "", "", false) { TimedOut = true })] };
        var restartRecovery = recoveryFixture with { Recipes = [.. recoveryFixture.Recipes,
            new("pending-restart", 1, RecipeOutcome.RestartRequired, null, null, "Restart pending")] };
        Require(restartRecovery.RequiresRestart && restartRecovery.GetExitCode() == 4 &&
            restartRecovery.RecoveryPlan.Action == RecoveryNextAction.RestartThenRecheck && restartRecovery.RecoveryPlan.AvailableRecipeIds.Count == 0 &&
            WorkflowReport.Format(restartRecovery).Contains("NEXT: Restart when convenient"),
            "Recipe restart requirement reaches workflow status, exit code and follow-up guidance");
        Require(timedRecovery.RecoveryPlan.Action == RecoveryNextAction.RecheckTimedOutWork && WorkflowReport.Format(timedRecovery).Contains("NEXT: A command timed out"),
            "Timed-out work requires rechecking before offering another repair");
        Require((recoveryFixture with { Checks = [new("Restart", CheckState.Attention, "fixture")] }).RecoveryPlan.Action == RecoveryNextAction.RestartThenRecheck &&
            (recoveryFixture with { Status = "Running" }).RecoveryPlan.Action == RecoveryNextAction.Wait,
            "Pending restart and running workflows cannot offer repair execution");
        Require((recoveryFixture with { Recipes = [recoveryFixture.Recipes[0] with { RecipeId = "foreign" }] }).RecoveryPlan.AvailableRecipeIds.Count == 0,
            "Unknown recipe identity does not appear as an available repair");
        foreach (var prerequisite in new[] { "Administrator", "Restart", "System disk" })
            Require((recoveryFixture with { Checks = [new(prerequisite, CheckState.Unknown, "fixture")] }).RecoveryPlan.AvailableRecipeIds.Count == 0,
                "Incomplete prerequisite cannot offer repair: " + prerequisite);
        var dependentRecovery = recoveryFixture with { Kind = WorkflowKind.WindowsUpdate };
        Require(dependentRecovery.RecoveryPlan.AvailableRecipeIds.Count == 0,
            "System files in the servicing workflow requires component-store evidence");
        var storeCheck = new WorkflowCheck("ComponentStore", CheckState.Clear, "fixture");
        var storeResult = new RecipeResult("windows.component-store", 1, RecipeOutcome.Clear, storeCheck, null, "fixture");
        Require((dependentRecovery with { Recipes = [storeResult, .. dependentRecovery.Recipes] }).RecoveryPlan.AvailableRecipeIds.Count == 1,
            "A clear servicing prerequisite allows the matching follow-up repair suggestion");
        foreach (var prerequisiteCheck in new WorkflowCheck?[] { null,
            storeCheck with { Evidence = new(5, "failed", "", false) },
            storeCheck with { State = CheckState.Unknown } })
        {
            var invalidPrerequisite = dependentRecovery with
                { Recipes = [storeResult with { Check = prerequisiteCheck }, .. dependentRecovery.Recipes] };
            Require(invalidPrerequisite.RecoveryPlan.AvailableRecipeIds.Count == 0 &&
                invalidPrerequisite.Summary.Incomplete == 1,
                "Missing or contradictory prerequisite evidence cannot enable a dependent repair suggestion");
        }
        foreach (var mismatchedPrerequisite in new[] { storeResult with { RecipeVersion = 2 },
            storeResult with { Outcome = RecipeOutcome.Repaired, Check = storeCheck with { Repair = RepairKind.PrintSpooler },
                Repair = RecipeChecks.Success(RepairKind.PrintSpooler) } })
            Require((dependentRecovery with { Recipes = [mismatchedPrerequisite, .. dependentRecovery.Recipes] })
                .RecoveryPlan.AvailableRecipeIds.Count == 0,
                "Prerequisite version or verified action mismatch cannot enable a dependent repair suggestion");
        var unverifiedStore = storeResult with { Outcome = RecipeOutcome.Repaired, Repair = RecipeChecks.Success(RepairKind.ComponentStore) with { Steps = [] } };
        Require((dependentRecovery with { Recipes = [unverifiedStore, .. dependentRecovery.Recipes] }).RecoveryPlan.AvailableRecipeIds.Count == 0,
            "An unverified prerequisite repair cannot enable its dependent suggestion");
        var summaryFixture = new WorkflowResult(WorkflowKind.CommonProblems, "fixture", DateTimeOffset.UtcNow, "Needs attention",
            [originalCheck, new("device", CheckState.Attention, "fixture"), new("missing", CheckState.Unknown, "fixture")], [repairedFixture], "fixture")
        { Recipes = [new("windows.system-files", 1, RecipeOutcome.Repaired, originalCheck, repairedFixture, "fixture"),
            new("available", 1, RecipeOutcome.RepairAvailable, null, null, "fixture"), new("blocked", 1, RecipeOutcome.Blocked, null, null, "fixture")] };
        Require(summaryFixture.Summary is { VerifiedRepairs: 1, AvailableRepairs: 1, NeedsAttention: 1, Incomplete: 1, Deferred: 1, ReviewItems: 4 },
            "Summary separates verified recovery, incomplete evidence and deferred work without counting original findings twice");
        var invalidVerification = repairedFixture with { Steps = repairedFixture.Steps.Select(step => step with { Result = step.Result with { Error = "fixture error" } }).ToArray() };
        var invalidSummary = summaryFixture with { Recipes = [summaryFixture.Recipes[0] with { Repair = invalidVerification }] };
        Require(invalidSummary.Summary is { VerifiedRepairs: 0, Incomplete: 2 } && WorkflowReport.Format(invalidSummary).Contains("fixture error"),
            "Summary and report never call error-bearing repair evidence verified and expose command errors");
        foreach (var (kind, cleanOutput, damagedOutput) in new[]
        {
            (RepairKind.ComponentStore, "No component store corruption detected", "The component store is repairable"),
            (RepairKind.SystemFiles, "Windows Resource Protection did not find any integrity violations", "Windows Resource Protection found integrity violations")
        })
        {
            foreach (string output in new[] { cleanOutput, damagedOutput, cleanOutput + "\n" + damagedOutput, "Unrecognized output" })
            {
                var evidence = new ToolResult(0, output, "", false);
                Require(ReferenceEquals(Workflows.Interpret(kind, evidence).Evidence, evidence), "Integrity classifications retain original command evidence");
                var mixed = evidence with { Error = "Provider reported an error" };
                Require(Workflows.Interpret(kind, mixed) is { State: CheckState.Unknown, Repair: null } rejected && ReferenceEquals(rejected.Evidence, mixed),
                    "Integrity output accompanied by errors cannot authorize repair or report clear");
            }
        }
        foreach (string recommendation in new[] { "Yes", "No" })
        {
            var mixed = new ToolResult(0, "Component Store Cleanup Recommended : " + recommendation + "\nThe operation completed successfully.", "Provider reported an error", false);
            Require(ComponentCleanup.Interpret(mixed) is { State: CheckState.Unknown, Repair: null }, "Cleanup errors prevent eligibility and clean verification");
        }
        foreach (var kind in new[] { RepairKind.ComponentStore, RepairKind.SystemFiles, RepairKind.ComponentCleanup })
        {
            var success = RecipeChecks.Success(kind);
            if (kind == RepairKind.ComponentCleanup)
                success = success with { Steps = success.Steps.Select(step => step with { Result = new ToolResult(0,
                    "Component Store Cleanup Recommended : No\nThe operation completed successfully.", "", false) }).ToArray() };
            Require(Repairs.IsVerified(success), "Clean servicing fixture verifies");
            for (int index = 0; index < success.Steps.Count; index++)
            {
                var steps = success.Steps.ToArray();
                steps[index] = steps[index] with { Result = steps[index].Result with { Error = "Provider reported an error" } };
                Require(!Repairs.IsVerified(success with { Steps = steps }), "An error in any servicing step prevents verified recovery");
            }
        }
        using (var inspectionStop = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
        {
            var ignoringHost = new Host { IgnoreInspectionCancellation = true };
            var stoppedInspection = await Workflows.RunAsync(WorkflowKind.SystemFiles, true, folder, token: inspectionStop.Token, host: ignoringHost).WaitAsync(TimeSpan.FromSeconds(5));
            Require(stoppedInspection.Status.StartsWith("Stopped") && ignoringHost.Scanned.Count == 0 && ignoringHost.Repaired.Count == 0,
                "Stop releases workflow inspection even when a supplied read-only host ignores cancellation");
        }
        using (var refreshStop = new CancellationTokenSource())
        {
            var refreshingHost = new Host { State = CheckState.Repairable, StopRefresh = refreshStop.Cancel };
            var interruptedRefresh = await Workflows.RunAsync(WorkflowKind.Audio, true, folder, token: refreshStop.Token, host: refreshingHost).WaitAsync(TimeSpan.FromSeconds(5));
            Require(interruptedRefresh.Status.StartsWith("Stopped") && interruptedRefresh.Repairs.Count == 2 &&
                interruptedRefresh.Repairs.All(Repairs.IsVerified), "Canceled follow-up retains both verified audio repair records without claiming refreshed health");
            using var retainedJournal = JsonDocument.Parse(await File.ReadAllTextAsync(interruptedRefresh.JournalPath));
            Require(retainedJournal.RootElement.GetProperty("repairs").GetArrayLength() == 2 &&
                retainedJournal.RootElement.GetProperty("status").GetString()!.StartsWith("Stopped"), "Canceled follow-up journal preserves completed repair evidence");
        }
        foreach (var invalid in new[] { (WorkflowKind)(-1), (WorkflowKind)int.MaxValue })
        {
            Require(!Workflows.SupportsRepair(invalid), "Undefined workflows never advertise repair support");
            bool rejected = false;
            try { RecipeCatalog.ForWorkflow(invalid); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "Undefined workflows cannot become empty recipe plans");
            var unused = new Host();
            rejected = false;
            try { await Workflows.RunAsync(invalid, true, folder, host: unused); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected && unused.Inspections == 0 && unused.Repaired.Count == 0, "Undefined execution fails before host access");
        }
        foreach (Exception failure in new Exception[] { new UnauthorizedAccessException("fixture denied"), new IOException("fixture unavailable"),
            new System.Security.SecurityException("fixture blocked"), new InvalidOperationException("fixture changed") })
        {
            var unavailable = InstallerDiagnostics.Check(() => throw failure);
            Require(unavailable is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 } &&
                unavailable.Evidence.Error.Contains(failure.Message), "Installer collection failure retains evidence without aborting the caller");
        }
        Require(InstallerDiagnostics.Check(() => null!) is { State: CheckState.Unknown, Repair: null }, "Missing installer snapshot remains unknown");
        Require(InstallerDiagnostics.Check(() => new(null, false, true)).State == CheckState.Clear, "Supplied installer snapshot is assessed normally");
        InstallerDiagnostics.RequireReady(() => new(null, false, true));
        foreach (var environment in new[] { new InstallerEnvironment(2, false, true), new(null, true, true), new(null, false, false), new("invalid", false, true) })
        {
            bool deferred = false;
            try { InstallerDiagnostics.RequireReady(() => environment); }
            catch (InvalidOperationException) { deferred = true; }
            Require(deferred, "Installer recovery gate preserves restrictive, busy, missing-engine and unknown environments");
        }
        bool missingInstallerDeferred = false;
        try { InstallerDiagnostics.RequireReady(() => throw new UnauthorizedAccessException("fixture")); }
        catch (InvalidOperationException) { missingInstallerDeferred = true; }
        Require(missingInstallerDeferred, "Installer recovery gate rejects unavailable policy evidence");
        foreach (object? userValue in new object?[] { null, 0, 1 })
        {
            var elevation = InstallerDiagnostics.Assess(new(0, false, true) { InspectedAccount = "fixture", ElevationPolicyCollected = true,
                MachineAlwaysInstallElevated = 1, UserAlwaysInstallElevated = userValue });
            Require(elevation is { State: CheckState.Attention, Repair: null } && elevation.Detail.Contains("security exposure") && elevation.Evidence is not null,
                "Machine installer elevation policy requires review even when user setting is absent");
        }
        Require(InstallerDiagnostics.Assess(new(0, false, true) { ElevationPolicyCollected = true, MachineAlwaysInstallElevated = "1" }).State == CheckState.Unknown,
            "String elevation policy cannot be treated as a DWORD");
        Require(InstallerDiagnostics.Assess(new(0, false, true) { ElevationPolicyCollected = true, MachineAlwaysInstallElevated = 0, UserAlwaysInstallElevated = 1 }).State == CheckState.Clear,
            "User-only elevation setting does not imply enabled policy pair");
        var sourcePolicy = InstallerDiagnostics.Assess(new(0, false, true) { SourcePolicyCollected = true,
            MachineSearchOrder = "nmu", UserSearchOrder = "un", MachineDisableMedia = 1, UserDisableBrowse = 1 });
        Require(sourcePolicy.State == CheckState.Attention && sourcePolicy.Detail.Contains("network, removable media, URL") &&
            sourcePolicy.Detail.Contains("source-resolution failure") && sourcePolicy.Repair is null,
            "Installer source order and media or browsing restrictions remain visible without policy changes");
        foreach (object invalidSourcePolicy in new object[] { "nn", "x", 7 })
            Require(InstallerDiagnostics.Assess(new(0, false, true) { SourcePolicyCollected = true,
                MachineSearchOrder = invalidSourcePolicy }).State == CheckState.Unknown,
                "Malformed Installer source order cannot pass readiness");
        foreach (object invalidRestriction in new object[] { "1", 2, -1 })
            Require(InstallerDiagnostics.Assess(new(0, false, true) { SourcePolicyCollected = true,
                MachineDisableMedia = invalidRestriction }).State == CheckState.Unknown,
                "Malformed Installer source restriction cannot pass readiness");
        foreach (Exception error in new Exception[] { new System.Net.NetworkInformation.NetworkInformationException(5),
            new InvalidOperationException("Interface disappeared"), new UnauthorizedAccessException(), new System.Security.SecurityException() })
        {
            var unreadable = WindowsWorkflowHost.ReadNetworkConfiguration("unavailable", () => throw error);
            var readable = WindowsWorkflowHost.ReadNetworkConfiguration("available", () => (["192.0.2.2"], ["192.0.2.1"], ["192.0.2.1"]));
            Require(unreadable is { State: CheckState.Unknown, Repair: null } && readable.State == CheckState.Clear,
                "Unreadable adapter does not prevent inspecting another interface");
        }
        Require(WindowsWorkflowHost.ReadNetworkConfiguration("incomplete", () => (null!, [], [])) is { State: CheckState.Unknown, Repair: null },
            "Incomplete adapter collection cannot be interpreted as no bindings");
        foreach (var kind in Enum.GetValues<WorkflowKind>())
        {
            var empty = new Host { EmptyInspection = true };
            var missing = await Workflows.RunAsync(kind, true, folder, host: empty);
            Require(missing.Status == "Needs attention" && missing.Checks.Any(c => c.State == CheckState.Unknown) &&
                empty.Scanned.Count == 0 && empty.Repaired.Count == 0, "Missing inspection evidence blocks execution and cannot claim health");
        }
        foreach (var evidence in new[] { "No component store corruption detected", "The component store is repairable" })
        {
            var timedOut = new ToolResult(0, evidence, "", false) { TimedOut = true };
            var check = Workflows.Interpret(RepairKind.ComponentStore, timedOut);
            Require(check.State == CheckState.Unknown && check.Repair is null && check.Evidence == timedOut,
                "Timed-out servicing output cannot establish health or authorize repair and retains its evidence");
        }
        var discovery = await Workflows.RunAsync(WorkflowKind.AvailableUpdates, false, folder, host: new Host { State = CheckState.Clear });
        Require(discovery.Status == "Discovery completed" && discovery.Repairs.Count == 0, "Update discovery does not claim no issues or run repairs");
        var thirdPartyOnly = await Workflows.RunAsync(WorkflowKind.AvailableUpdates, false, folder, host: new WindowsWorkflowHost
        {
            UpdateOptions = new(false, false),
            IncludeThirdPartyUpdates = true,
            ThirdPartyUpdates = _ => Task.FromResult(new ThirdPartyUpdateResult(true, "fixture", true, 0, [], "No updates offered."))
        });
        Require(thirdPartyOnly.Checks.Select(check => check.Name).SequenceEqual(new[] { "Windows Update discovery", "Third-party application updates" }) &&
            thirdPartyOnly.Checks[0].Detail.Contains("not selected", StringComparison.OrdinalIgnoreCase),
            "Third-party-only discovery does not collect or search Windows Update");
        var maintenance = await Workflows.RunAsync(WorkflowKind.ComponentCleanup, false, folder, host: new Host { State = CheckState.Repairable });
        Require(WorkflowReport.Format(maintenance).Contains("Select Run recommended cleanup") && WorkflowReport.Format(maintenance).StartsWith("Component-store cleanup /"), "Maintenance guidance matches the actual app control and title");
        var ordered = new WorkflowResult(WorkflowKind.Network, "fixture", DateTimeOffset.UtcNow, "Needs attention",
            [new("Routine", CheckState.Clear, "Routine detail"), new("Missing evidence", CheckState.Unknown, "Unavailable detail"),
             new("Proxy information", CheckState.Information, "Proxy detail")], [], "fixture.json");
        string orderedReport = WorkflowReport.Format(ordered);
        foreach (var state in new[] { CheckState.Attention, CheckState.Unknown, CheckState.Information })
        {
            var devices = ordered with { Kind = WorkflowKind.DeviceProblems, Checks = [new("Device evidence", state, "fixture")] };
            string report = WorkflowReport.Format(devices);
            string expected = state == CheckState.Attention ? "Review the reported device ID" :
                state == CheckState.Unknown ? "Device evidence is incomplete" : "test the affected hardware directly";
            Require(report.Contains("NEXT: " + (state == CheckState.Information ? "No active fault" : expected)) &&
                report.Contains(expected) && report.Contains("No device was enabled") && !report.Contains("Select Auto-repair"),
                "Device problem guidance distinguishes faults, missing evidence and informational observations");
            Require(WorkflowReport.Format(devices with { Status = "Stopped" }).Contains("NEXT: Review the saved journal"),
                "Stopped device workflow preserves interruption guidance");
        }
        Require(WorkflowReport.Format(ordered with { Kind = WorkflowKind.DeviceProblems, Checks = [] }).Contains("NEXT: Device evidence is incomplete"),
            "Empty device report cannot imply no active fault");
        foreach (bool needsReview in new[] { false, true })
        {
            var input = ordered with { Kind = WorkflowKind.Input, Status = needsReview ? "Needs attention" : "No issues detected in checked scopes",
                Checks = [new("Input device", needsReview ? CheckState.Unknown : CheckState.Clear, "fixture")] };
            string inputReport = WorkflowReport.Format(input);
            Require(inputReport.Contains(needsReview ? "Missing driver inventory is not proof" : "Test the affected keyboard") &&
                inputReport.Contains("No input was captured") && !inputReport.Contains("Select Auto-repair"),
                "Input next steps distinguish incomplete device evidence from a clear scoped snapshot");
        }
        foreach (IReadOnlyList<RepairStepResult> missingSteps in new IReadOnlyList<RepairStepResult>[] { null!, [null!], [new(new("fixture", [], false), null!)] })
        {
            var damagedRepair = RecipeChecks.Success(RepairKind.WindowsEventLog) with { Steps = missingSteps };
            var damagedWorkflow = ordered with { Repairs = [damagedRepair], Recipes = [new("eventlog.service", 1, RecipeOutcome.Repaired,
                new("EventLog", CheckState.Repairable, "fixture", RepairKind.WindowsEventLog), damagedRepair, "fixture")] };
            string damagedReport = WorkflowReport.Format(damagedWorkflow);
            Require(damagedReport.Contains("Verification incomplete") && damagedReport.Contains("Command evidence is missing") &&
                !damagedReport.Contains("Repaired and verified"), "Missing command records remain readable and unverified in workflow reports");
        }
        var needsElevation = ordered with { Kind = WorkflowKind.EventLog, Checks = [new("Administrator", CheckState.Attention, "Elevation required")] };
        Require(WorkflowReport.Format(needsElevation).Contains("NEXT: Run KillerBench as administrator") &&
            WorkflowReport.Format(needsElevation, true).Contains("NEXT: Open an administrator terminal") &&
            !WorkflowReport.Format(needsElevation with { Kind = WorkflowKind.Services }).Contains("NEXT: Run KillerBench as administrator") &&
            WorkflowReport.Format(needsElevation with { Status = "Stopped" }).Contains("NEXT: Review the saved journal"),
            "Elevation guidance matches the client and repair scope while preserving interruption review priority");
        string storageElevation = WorkflowReport.Format(needsElevation with { Kind = WorkflowKind.Storage });
        Require(storageElevation.Contains("NEXT: Run KillerBench as administrator") && storageElevation.Contains("checks only") &&
            !storageElevation.Contains("before enabling repairs"), "Generic Storage elevation guidance does not advertise automatic repair before option-dependent eligibility exists");
        Require(orderedReport.Contains("Review items: 1") && orderedReport.IndexOf("NEXT:", StringComparison.Ordinal) < orderedReport.IndexOf("Missing evidence:", StringComparison.Ordinal) &&
            orderedReport.IndexOf("Missing evidence:", StringComparison.Ordinal) < orderedReport.IndexOf("Routine:", StringComparison.Ordinal) && ordered.Checks[0].Name == "Routine",
            "Workflow summary leads with next action and review items without changing retained evidence order");
        foreach (var snapshotKind in new[] { WorkflowKind.Privacy, WorkflowKind.BitLocker })
        {
            var snapshot = await Workflows.RunAsync(snapshotKind, false, folder, host: new Host { State = CheckState.Clear });
            string report = WorkflowReport.Format(snapshot);
            Require(snapshotKind == WorkflowKind.Privacy ? report.Contains("No privacy settings were changed") : report.Contains("did not back up keys or change encryption"), "Snapshot guidance does not imply a repair");
        }
        foreach (var kind in new[] { WorkflowKind.Printing, WorkflowKind.TimeService, WorkflowKind.Installer, WorkflowKind.Wmi, WorkflowKind.Audio, WorkflowKind.Search, WorkflowKind.EventLog })
        {
            var host = new Host { State = CheckState.Repairable };
            var recovered = await Workflows.RunAsync(kind, true, folder, host: host);
            bool leavesReview = RecipeCatalog.ForWorkflow(kind).Any(recipe => !recipe.CanAutoExecute);
            Require(host.Scanned.SequenceEqual(Workflows.Scopes(kind)) && host.Repaired.SequenceEqual(RecipeCatalog.ForWorkflow(kind).Where(r => r.CanAutoExecute).Select(r => r.Action)) &&
                (leavesReview ? recovered.Status == "Needs attention" : recovered.Status.StartsWith("Repairs verified")),
                "Selected flow runs only its own recovery; diagnostic-only findings cannot trigger repairs: " + kind +
                "; scanned=" + string.Join(',', host.Scanned) + "; repaired=" + string.Join(',', host.Repaired));
            var inspect = new Host { State = CheckState.Repairable };
            await Workflows.RunAsync(kind, false, folder, host: inspect);
            Require(inspect.Repaired.Count == 0, "Diagnosis does not execute recovery");
        }
        foreach (var kind in Enum.GetValues<RepairKind>().Where(k => ServiceRecovery.ServiceName(k) is not null))
        {
            string servicePrefix = "SERVICE_NAME: " + ServiceRecovery.ServiceName(kind) + "\n";
            var stoppedService = new ToolResult(0, servicePrefix + "STATE : 1 STOPPED", "", false);
            Require(ServiceRecovery.Interpret(kind, stoppedService, 2).State == CheckState.Repairable, "Stopped automatic service is eligible in selected recovery");
            Require(ServiceRecovery.Interpret(kind, stoppedService, 3).State == CheckState.Information &&
                ServiceRecovery.Interpret(kind, stoppedService, 3).Repair is null,
                "Stopped demand-start service is not automatically started from idle state alone");
            Require(ServiceRecovery.Interpret(kind, stoppedService, 4).Repair is null, "Disabled service is never enabled");
            foreach (int? invalidStartup in new int?[] { null, -1, 0, 1, 5 })
                Require(ServiceRecovery.Interpret(kind, stoppedService, invalidStartup).Repair is null, "Unrecognized startup configuration cannot authorize service recovery");
            foreach (string ambiguous in new[] { "STATE : 4 RUNNING\nSTATE : 1 STOPPED", "STATE : 4 RUNNING\nSTATE : 9 INVALID", "STATE : 4 RUNNING\nSTATE : 4 RUNNING" })
                Require(ServiceRecovery.State(new(0, ambiguous, "", false)) is null, "Multiple service-state fields cannot verify a single target");
            Require(ServiceRecovery.Interpret(kind, stoppedService with { TimedOut = true }, 3).Repair is null, "Timed-out service evidence cannot authorize recovery");
            Require(ServiceRecovery.Interpret(kind, new(0, servicePrefix + "STATE : 4 RUNNING", "", false), 2).State == CheckState.Clear, "Running service is left alone");
            Require(ServiceRecovery.Interpret(kind, new(0, servicePrefix + "STATE : 2 START_PENDING", "", false), 2).Repair is null, "Transitioning service is left alone");
            Require(ServiceRecovery.State(new(5, "STATE : 4 RUNNING", "", false)) is null && ServiceRecovery.State(new(0, "localized", "", false)) is null, "Failed and unknown queries cannot verify");
        }
        var failedAudio = new Host { State = CheckState.Repairable, Verified = false };
        var audioResult = await Workflows.RunAsync(WorkflowKind.Audio, true, folder, host: failedAudio);
        Require(failedAudio.Scanned.SequenceEqual(new[] { RepairKind.AudioEndpoints }) && audioResult.Recipes[1].Outcome == RecipeOutcome.Blocked, "Audio service waits for verified endpoint builder");
        Require(ServiceRecovery.ServiceName(RepairKind.AudioEndpoints) == "AudioEndpointBuilder" && ServiceRecovery.ServiceName(RepairKind.WindowsAudio) == "Audiosrv", "Audio recipes use exact service names");
        var ipHelperPlan = Repairs.Plan(RepairKind.IpHelper);
        Require(ipHelperPlan.Steps.Count == 2 && ipHelperPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "iphlpsvc" }) &&
            ipHelperPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "iphlpsvc" }) && ipHelperPlan.Steps[1].Verification &&
            ipHelperPlan.Effects.Contains("Preserves transition configuration", StringComparison.Ordinal),
            "IP Helper recovery starts and verifies only the exact service while declaring preserved settings");
        var bluetoothPlan = Repairs.Plan(RepairKind.BluetoothSupport);
        Require(WorkflowCatalog.Find("bluetooth")?.Kind == WorkflowKind.Bluetooth &&
            Workflows.Scopes(WorkflowKind.Bluetooth).SequenceEqual(new[] { RepairKind.BluetoothSupport }) &&
            bluetoothPlan.Steps.Count == 2 && bluetoothPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "bthserv" }) &&
            bluetoothPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "bthserv" }) && bluetoothPlan.Steps[1].Verification &&
            bluetoothPlan.Effects.Contains("Preserves pairings", StringComparison.Ordinal),
            "Bluetooth recovery is an isolated exact-service workflow with preserved pairing settings");
        var cameraPlan = Repairs.Plan(RepairKind.CameraFrameServer);
        Require(WorkflowCatalog.Find("camera")?.Kind == WorkflowKind.Camera &&
            Workflows.Scopes(WorkflowKind.Camera).SequenceEqual(new[] { RepairKind.CameraFrameServer }) &&
            cameraPlan.Steps.Count == 2 && cameraPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "FrameServer" }) &&
            cameraPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "FrameServer" }) && cameraPlan.Steps[1].Verification &&
            cameraPlan.Effects.Contains("Preserves camera privacy settings", StringComparison.Ordinal),
            "Camera recovery is an isolated exact-service workflow with preserved privacy settings");
        var smartCardPlan = Repairs.Plan(RepairKind.SmartCardService);
        Require(WorkflowCatalog.Find("smart-card")?.Kind == WorkflowKind.SmartCard &&
            Workflows.Scopes(WorkflowKind.SmartCard).SequenceEqual(new[] { RepairKind.SmartCardService }) &&
            smartCardPlan.Steps.Count == 2 && smartCardPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "SCardSvr" }) &&
            smartCardPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "SCardSvr" }) && smartCardPlan.Steps[1].Verification &&
            smartCardPlan.Effects.Contains("Preserves certificates, PINs, credentials", StringComparison.Ordinal),
            "Smart-card recovery is an isolated exact-service workflow with preserved credential material");
        var biometricPlan = Repairs.Plan(RepairKind.BiometricService);
        Require(WorkflowCatalog.Find("biometric")?.Kind == WorkflowKind.Biometric &&
            Workflows.Scopes(WorkflowKind.Biometric).SequenceEqual(new[] { RepairKind.BiometricService }) &&
            biometricPlan.Steps.Count == 2 && biometricPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "WbioSrvc" }) &&
            biometricPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "WbioSrvc" }) && biometricPlan.Steps[1].Verification &&
            biometricPlan.Effects.Contains("Preserves biometric templates, Windows Hello data", StringComparison.Ordinal),
            "Biometric recovery is an isolated exact-service workflow that preserves enrolled identity data");
        var fileSharingPlan = Repairs.Plan(RepairKind.ServerService);
        Require(WorkflowCatalog.Find("file-sharing")?.Kind == WorkflowKind.FileSharing &&
            Workflows.Scopes(WorkflowKind.FileSharing).SequenceEqual(new[] { RepairKind.ServerService }) &&
            ServiceRecovery.ServiceName(RepairKind.ServerService) == "LanmanServer" &&
            fileSharingPlan.Steps.Count == 2 && fileSharingPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "LanmanServer" }) &&
            fileSharingPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "LanmanServer" }) && fileSharingPlan.Steps[1].Verification &&
            fileSharingPlan.Effects.Contains("Preserves shares, permissions, SMB signing and encryption settings", StringComparison.Ordinal),
            "File-sharing recovery is an isolated exact-service workflow with preserved shares and security settings");
        var taskSchedulerPlan = Repairs.Plan(RepairKind.TaskSchedulerService);
        Require(WorkflowCatalog.Find("task-scheduler-service")?.Kind == WorkflowKind.TaskSchedulerService &&
            Workflows.Scopes(WorkflowKind.TaskSchedulerService).SequenceEqual(new[] { RepairKind.TaskSchedulerService }) &&
            ServiceRecovery.ServiceName(RepairKind.TaskSchedulerService) == "Schedule" &&
            taskSchedulerPlan.Steps.Count == 2 && taskSchedulerPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "Schedule" }) &&
            taskSchedulerPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "Schedule" }) && taskSchedulerPlan.Steps[1].Verification &&
            taskSchedulerPlan.Effects.Contains("Existing enabled tasks can begin running", StringComparison.Ordinal) &&
            taskSchedulerPlan.Effects.Contains("Preserves every task definition", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.TaskSchedulerService).Contains("runtime results and missed-run counts", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.TaskSchedulerService).Contains("cannot authorize a service start", StringComparison.Ordinal),
            "Task Scheduler recovery is an isolated evidence-backed service workflow with explicit missed-task effects and preserved definitions");
        foreach (var focused in new[]
        {
            (Key: "windows-update-service", Kind: WorkflowKind.WindowsUpdateService, Repair: RepairKind.UpdateService, Service: "wuauserv", Preserved: "Update policy, source enrollment, schedules, restart settings"),
            (Key: "bits-service", Kind: WorkflowKind.BitsService, Repair: RepairKind.BackgroundTransfer, Service: "BITS", Preserved: "Jobs, priorities, owners, destinations"),
            (Key: "cryptographic-service", Kind: WorkflowKind.CryptographicService, Repair: RepairKind.CryptographicServices, Service: "CryptSvc", Preserved: "Certificates, keys, catalogs, trust settings"),
            (Key: "modules-installer-service", Kind: WorkflowKind.ModulesInstallerService, Repair: RepairKind.ModulesInstaller, Service: "TrustedInstaller", Preserved: "Packages, component-store content"),
            (Key: "update-medic-service", Kind: WorkflowKind.WindowsUpdateMedicService, Repair: RepairKind.WindowsUpdateMedicService, Service: "WaaSMedicSvc", Preserved: "Update policy, source enrollment, schedules, packages"),
            (Key: "update-orchestrator-service", Kind: WorkflowKind.UpdateOrchestratorService, Repair: RepairKind.UpdateOrchestrator, Service: "UsoSvc", Preserved: "Update policy, source enrollment, schedules, restart settings"),
            (Key: "diagnostic-policy-service", Kind: WorkflowKind.DiagnosticPolicyService, Repair: RepairKind.DiagnosticPolicyService, Service: "DPS", Preserved: "Policy, troubleshooting data"),
            (Key: "error-reporting-service", Kind: WorkflowKind.WindowsErrorReportingService, Repair: RepairKind.WindowsErrorReportingService, Service: "WerSvc", Preserved: "Report archives, dumps"),
            (Key: "event-collector-service", Kind: WorkflowKind.WindowsEventCollectorService, Repair: RepairKind.WindowsEventCollectorService, Service: "Wecsvc", Preserved: "Subscriptions, forwarded events, credentials"),
            (Key: "remote-access-service", Kind: WorkflowKind.RemoteAccessConnectionManagerService, Repair: RepairKind.RemoteAccessConnectionManagerService, Service: "RasMan", Preserved: "Connection profiles, credentials, routes"),
            (Key: "remote-desktop-services", Kind: WorkflowKind.RemoteDesktopServices, Repair: RepairKind.RemoteDesktopServices, Service: "TermService", Preserved: "Remote Desktop enablement, listeners, certificates, ports"),
            (Key: "windows-remote-management", Kind: WorkflowKind.WindowsRemoteManagement, Repair: RepairKind.WindowsRemoteManagement, Service: "WinRM", Preserved: "Listeners, addresses, ports, certificates"),
            (Key: "security-center-service", Kind: WorkflowKind.SecurityCenterService, Repair: RepairKind.SecurityCenterService, Service: "wscsvc", Preserved: "Antivirus configuration, provider registrations"),
            (Key: "network-connections-service", Kind: WorkflowKind.NetworkConnectionsService, Repair: RepairKind.NetworkConnectionsService, Service: "Netman", Preserved: "Adapters, addresses, routes, DNS settings"),
            (Key: "winhttp-autoproxy-service", Kind: WorkflowKind.WinHttpAutoProxyService, Repair: RepairKind.WinHttpAutoProxyService, Service: "WinHttpAutoProxySvc", Preserved: "Proxy definitions, PAC URLs, credentials, network settings"),
            (Key: "connection-manager-service", Kind: WorkflowKind.WindowsConnectionManagerService, Repair: RepairKind.WindowsConnectionManagerService, Service: "Wcmsvc", Preserved: "Adapters, profiles, credentials, metering"),
            (Key: "network-setup-service", Kind: WorkflowKind.NetworkSetupService, Repair: RepairKind.NetworkSetupService, Service: "NetSetupSvc", Preserved: "Adapters, drivers, driver packages, bindings"),
            (Key: "app-readiness-service", Kind: WorkflowKind.AppReadinessService, Repair: RepairKind.AppReadinessService, Service: "AppReadiness", Preserved: "Packages, registrations, licenses, user profiles"),
            (Key: "state-repository-service", Kind: WorkflowKind.StateRepositoryService, Repair: RepairKind.StateRepositoryService, Service: "StateRepository", Preserved: "Registrations, packages, licenses, profiles"),
            (Key: "appx-deployment-service", Kind: WorkflowKind.AppxDeploymentService, Repair: RepairKind.AppxDeploymentService, Service: "AppXSvc", Preserved: "Packages, registrations, licenses, profiles"),
            (Key: "client-license-service", Kind: WorkflowKind.ClientLicenseService, Repair: RepairKind.ClientLicenseService, Service: "ClipSVC", Preserved: "Packages, registrations, licenses, tokens, accounts"),
            (Key: "store-install-service", Kind: WorkflowKind.StoreInstallService, Repair: RepairKind.StoreInstallService, Service: "InstallService", Preserved: "Packages, registrations, licenses, accounts"),
            (Key: "license-manager-service", Kind: WorkflowKind.WindowsLicenseManagerService, Repair: RepairKind.WindowsLicenseManagerService, Service: "LicenseManager", Preserved: "Packages, registrations, licenses, tokens, accounts"),
            (Key: "push-to-install-service", Kind: WorkflowKind.PushToInstallService, Repair: RepairKind.PushToInstallService, Service: "PushToInstall", Preserved: "Packages, registrations, licenses, accounts"),
            (Key: "web-account-manager-service", Kind: WorkflowKind.WebAccountManagerService, Repair: RepairKind.WebAccountManagerService, Service: "TokenBroker", Preserved: "Accounts, credentials, tokens, cookies"),
            (Key: "microsoft-account-sign-in-service", Kind: WorkflowKind.MicrosoftAccountSignInService, Repair: RepairKind.MicrosoftAccountSignInService, Service: "wlidsvc", Preserved: "Accounts, credentials, tokens, device registration"),
            (Key: "storage-service", Kind: WorkflowKind.StorageService, Repair: RepairKind.StorageService, Service: "StorSvc", Preserved: "Volumes, partitions, filesystems, mount points"),
            (Key: "optimize-drives-service", Kind: WorkflowKind.OptimizeDrivesService, Repair: RepairKind.OptimizeDrivesService, Service: "defragsvc", Preserved: "Volumes, filesystems, data, optimization schedules"),
            (Key: "image-acquisition-service", Kind: WorkflowKind.WindowsImageAcquisitionService, Repair: RepairKind.WindowsImageAcquisitionService, Service: "stisvc", Preserved: "Devices, drivers, image data, application permissions"),
            (Key: "function-discovery-provider", Kind: WorkflowKind.FunctionDiscoveryProviderHostService, Repair: RepairKind.FunctionDiscoveryProviderHostService, Service: "fdPHost", Preserved: "Network profiles, shares, devices, providers"),
            (Key: "function-discovery-publication", Kind: WorkflowKind.FunctionDiscoveryResourcePublicationService, Repair: RepairKind.FunctionDiscoveryResourcePublicationService, Service: "FDResPub", Preserved: "Network profiles, shares, published resources, firewall rules"),
            (Key: "human-interface-service", Kind: WorkflowKind.HumanInterfaceDeviceService, Repair: RepairKind.HumanInterfaceDeviceService, Service: "hidserv", Preserved: "Devices, drivers, button mappings, accessibility settings"),
            (Key: "portable-device-service", Kind: WorkflowKind.PortableDeviceEnumeratorService, Repair: RepairKind.PortableDeviceEnumeratorService, Service: "WPDBusEnum", Preserved: "Devices, drivers, files, libraries"),
            (Key: "font-cache-service", Kind: WorkflowKind.WindowsFontCacheService, Repair: RepairKind.WindowsFontCacheService, Service: "FontCache", Preserved: "Installed fonts, font files, application data, cache files"),
            (Key: "device-association-service", Kind: WorkflowKind.DeviceAssociationService, Repair: RepairKind.DeviceAssociationService, Service: "DeviceAssociationService", Preserved: "Paired and associated devices, drivers, credentials, metadata"),
            (Key: "text-input-service", Kind: WorkflowKind.TextInputManagementService, Repair: RepairKind.TextInputManagementService, Service: "TextInputManagementService", Preserved: "Dictionaries, handwriting data, language and keyboard settings"),
            (Key: "display-enhancement-service", Kind: WorkflowKind.DisplayEnhancementService, Repair: RepairKind.DisplayEnhancementService, Service: "DisplayEnhancementService", Preserved: "Display configuration, brightness, color profiles, HDR settings"),
            (Key: "user-profile-service", Kind: WorkflowKind.UserProfileService, Repair: RepairKind.UserProfileService, Service: "ProfSvc", Preserved: "Profile folders, registry hives, permissions"),
            (Key: "credential-manager-service", Kind: WorkflowKind.CredentialManagerService, Repair: RepairKind.CredentialManagerService, Service: "VaultSvc", Preserved: "Vault contents, passwords, passkeys"),
            (Key: "hardware-detection-service", Kind: WorkflowKind.ShellHardwareDetectionService, Repair: RepairKind.ShellHardwareDetectionService, Service: "ShellHWDetection", Preserved: "Devices, drivers, volumes, mount points"),
            (Key: "device-install-service", Kind: WorkflowKind.DeviceInstallService, Repair: RepairKind.DeviceInstallService, Service: "DeviceInstall", Preserved: "Devices, drivers, packages, configuration")
        })
        {
            Require(WorkflowCatalog.Find(focused.Key)?.Kind == focused.Kind &&
                Workflows.Scopes(focused.Kind).SequenceEqual(new[] { focused.Repair }) &&
                ServiceRecovery.ServiceName(focused.Repair) == focused.Service &&
                Workflows.Preview(focused.Kind).Contains(focused.Preserved, StringComparison.Ordinal) &&
                RecipeCatalog.ForWorkflow(focused.Kind).Single().Action == focused.Repair,
                "Focused service workflow exposes the exact existing recipe and declares preserved state: " + focused.Key);
        }
        var focusedBitsPreview = Workflows.Preview(WorkflowKind.BitsService);
        var focusedUpdateServicePreview = Workflows.Preview(WorkflowKind.WindowsUpdateService);
        Require(focusedUpdateServicePreview.Contains("registered service DLL", StringComparison.Ordinal) &&
            focusedUpdateServicePreview.Contains("bounded current update activity", StringComparison.Ordinal) &&
            focusedUpdateServicePreview.Contains("cannot authorize a service start or update operation", StringComparison.Ordinal) &&
            focusedUpdateServicePreview.Contains("not source reachability, applicability, download, installation, completion or restart readiness", StringComparison.Ordinal),
            "Focused Windows Update Service workflow declares its registration, activity, history and verification boundaries");
        Require(focusedBitsPreview.Contains("current-account transfer jobs", StringComparison.Ordinal) &&
            focusedBitsPreview.Contains("bounded exact service-failure history", StringComparison.Ordinal) &&
            focusedBitsPreview.Contains("cannot authorize a service start", StringComparison.Ordinal) &&
            focusedBitsPreview.Contains("not transfer progress, application ownership or completion", StringComparison.Ordinal),
            "Focused BITS workflow declares its job, service, history and verification boundaries");
        var focusedCryptographicPreview = Workflows.Preview(WorkflowKind.CryptographicService);
        Require(focusedCryptographicPreview.Contains("registered service DLL", StringComparison.Ordinal) &&
            focusedCryptographicPreview.Contains("bounded exact service-failure history", StringComparison.Ordinal) &&
            focusedCryptographicPreview.Contains("cannot authorize a service start or component replacement", StringComparison.Ordinal) &&
            focusedCryptographicPreview.Contains("not certificate, signature, catalog or trust validation", StringComparison.Ordinal),
            "Focused Cryptographic Services workflow declares its registration, history and verification boundaries");
        var focusedModulesInstallerPreview = Workflows.Preview(WorkflowKind.ModulesInstallerService);
        Require(focusedModulesInstallerPreview.Contains("bounded servicing package transitions", StringComparison.Ordinal) &&
            focusedModulesInstallerPreview.Contains("exact service-failure history", StringComparison.Ordinal) &&
            focusedModulesInstallerPreview.Contains("cannot authorize a service start or package operation", StringComparison.Ordinal) &&
            focusedModulesInstallerPreview.Contains("not component-store health, package applicability, servicing completion or restart readiness", StringComparison.Ordinal),
            "Focused Windows Modules Installer workflow declares its package, history and verification boundaries");
        var focusedMedicPreview = Workflows.Preview(WorkflowKind.WindowsUpdateMedicService);
        Require(focusedMedicPreview.Contains("registered service DLL", StringComparison.Ordinal) &&
            focusedMedicPreview.Contains("bounded current update activity", StringComparison.Ordinal) &&
            focusedMedicPreview.Contains("cannot authorize a service start or component action", StringComparison.Ordinal) &&
            focusedMedicPreview.Contains("not remediation, component health, applicability, download, installation or restart readiness", StringComparison.Ordinal),
            "Focused Windows Update Medic workflow declares its registration, activity, history and verification boundaries");
        var focusedOrchestratorPreview = Workflows.Preview(WorkflowKind.UpdateOrchestratorService);
        Require(focusedOrchestratorPreview.Contains("exact built-in Windows Update task", StringComparison.Ordinal) &&
            focusedOrchestratorPreview.Contains("bounded current activity", StringComparison.Ordinal) &&
            focusedOrchestratorPreview.Contains("cannot authorize a service start or update operation", StringComparison.Ordinal) &&
            focusedOrchestratorPreview.Contains("not applicability, download, installation, completion or restart readiness", StringComparison.Ordinal),
            "Focused Update Orchestrator workflow declares its task, activity, history and verification boundaries");
        var errorReportingPreview = Workflows.Preview(WorkflowKind.WindowsErrorReportingService);
        Require(errorReportingPreview.Contains("crash-dump configuration", StringComparison.Ordinal) &&
            errorReportingPreview.Contains("bounded dump-file metadata", StringComparison.Ordinal) &&
            errorReportingPreview.Contains("cannot authorize recovery", StringComparison.Ordinal) &&
            errorReportingPreview.Contains("not report creation, upload, delivery, problem resolution or crash diagnosis", StringComparison.Ordinal),
            "Windows Error Reporting workflow declares its crash evidence, history and verification boundaries");
        var eventCollectorPreview = Workflows.Preview(WorkflowKind.WindowsEventCollectorService);
        Require(eventCollectorPreview.Contains("bounded Windows Event Collector subscription inventory", StringComparison.Ordinal) &&
            eventCollectorPreview.Contains("empty inventory can be intentional", StringComparison.Ordinal) &&
            eventCollectorPreview.Contains("cannot authorize recovery", StringComparison.Ordinal) &&
            eventCollectorPreview.Contains("not source reachability, authentication, remote delivery, event freshness or subscription health", StringComparison.Ordinal),
            "Windows Event Collector workflow declares its subscription, history and verification boundaries");
        var remoteAccessPreview = Workflows.Preview(WorkflowKind.RemoteAccessConnectionManagerService);
        Require(remoteAccessPreview.Contains("bounded current-account VPN profile configuration", StringComparison.Ordinal) &&
            remoteAccessPreview.Contains("empty inventory can be intentional", StringComparison.Ordinal) &&
            remoteAccessPreview.Contains("cannot authorize recovery or a connection attempt", StringComparison.Ordinal) &&
            remoteAccessPreview.Contains("not profile applicability, server reachability, authentication, tunnel negotiation, route installation, name resolution or connectivity", StringComparison.Ordinal),
            "Remote Access workflow declares its profile, history and verification boundaries");
        var remoteDesktopPreview = Workflows.Preview(WorkflowKind.RemoteDesktopServices);
        Require(remoteDesktopPreview.Contains("local Remote Desktop enablement, Network Level Authentication and listener port", StringComparison.Ordinal) &&
            remoteDesktopPreview.Contains("bounded connection and session history", StringComparison.Ordinal) &&
            remoteDesktopPreview.Contains("Historical records cannot authorize service recovery or configuration changes", StringComparison.Ordinal) &&
            remoteDesktopPreview.Contains("not effective policy, listener binding, firewall access, certificate identity, permissions, authentication, network reachability, connection success or session creation", StringComparison.Ordinal),
            "Remote Desktop workflow declares its configuration, history and verification boundaries");
        var winRmPreview = Workflows.Preview(WorkflowKind.WindowsRemoteManagement);
        Require(winRmPreview.Contains("bounded WinRM listener configuration", StringComparison.Ordinal) &&
            winRmPreview.Contains("Disabled listeners or current service concerns request review", StringComparison.Ordinal) &&
            winRmPreview.Contains("historical records cannot authorize recovery or configuration changes", StringComparison.Ordinal) &&
            winRmPreview.Contains("not effective policy, listener binding, firewall access, certificate validity, authentication, permissions, client reachability or remote command execution", StringComparison.Ordinal),
            "WinRM workflow declares its listener, history and verification boundaries");
        var securityCenterPreview = Workflows.Preview(WorkflowKind.SecurityCenterService);
        Require(securityCenterPreview.Contains("bounded antivirus registrations with raw provider state", StringComparison.Ordinal) &&
            securityCenterPreview.Contains("empty inventory is not a protection-health verdict", StringComparison.Ordinal) &&
            securityCenterPreview.Contains("cannot authorize recovery or a security change", StringComparison.Ordinal) &&
            securityCenterPreview.Contains("not antivirus presence, protection state, signature freshness, notification delivery, registration ownership or effective policy", StringComparison.Ordinal),
            "Security Center workflow declares its registration, history and verification boundaries");
        foreach (var repair in new[] { RepairKind.WinHttpAutoProxyService, RepairKind.WindowsImageAcquisitionService, RepairKind.FunctionDiscoveryProviderHostService, RepairKind.FunctionDiscoveryResourcePublicationService,
            RepairKind.HumanInterfaceDeviceService, RepairKind.PortableDeviceEnumeratorService, RepairKind.WindowsFontCacheService,
            RepairKind.DeviceAssociationService, RepairKind.TextInputManagementService, RepairKind.DisplayEnhancementService })
        {
            var plan = Repairs.Plan(repair);
            string service = ServiceRecovery.ServiceName(repair)!;
            Require(plan.Steps.Count == 2 && plan.Steps[0].Arguments.SequenceEqual(new[] { "start", service }) &&
                plan.Steps[1].Arguments.SequenceEqual(new[] { "query", service }) && plan.Steps[1].Verification &&
                plan.Effects.Contains("Preserves", StringComparison.Ordinal) && plan.Effects.Contains("Verification covers stable service running state only", StringComparison.Ordinal),
                "Focused recovery plans start and verify only their exact service with explicit preservation limits: " + repair);
        }
        Require(WorkflowCatalog.Find("storage-spaces")?.Kind == WorkflowKind.StorageSpaces &&
            Workflows.Scopes(WorkflowKind.StorageSpaces).Length == 0 && !Workflows.SupportsRepair(WorkflowKind.StorageSpaces) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.StorageSpaces).Count == 0 &&
            Workflows.Preview(WorkflowKind.StorageSpaces).Contains("Windows may demand-start its Storage Spaces management provider", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.StorageSpaces).Contains("No pool, disk, volume, allocation, attachment, repair, optimization or setting operation is requested", StringComparison.Ordinal),
            "Storage Spaces is a diagnostic-only workflow with explicit provider activation and mutation limits");
        Require(WorkflowCatalog.Find("windows-activation")?.Kind == WorkflowKind.WindowsActivation &&
            Workflows.Scopes(WorkflowKind.WindowsActivation).Length == 0 && !Workflows.SupportsRepair(WorkflowKind.WindowsActivation) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.WindowsActivation).Count == 0 &&
            Workflows.Preview(WorkflowKind.WindowsActivation).Contains("without collecting full or partial product keys", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.WindowsActivation).Contains("Event payload is removed before evidence is saved", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.WindowsActivation).Contains("No activation, product-key, token, license, account, service or policy action is selected", StringComparison.Ordinal),
            "Windows activation is a diagnostic-only workflow with key and mutation limits");
        Require(WorkflowCatalog.Find("startup-performance")?.Kind == WorkflowKind.StartupPerformance &&
            Workflows.Scopes(WorkflowKind.StartupPerformance).Length == 0 && !Workflows.SupportsRepair(WorkflowKind.StartupPerformance) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.StartupPerformance).Count == 0 &&
            Workflows.Preview(WorkflowKind.StartupPerformance).Contains("bounded numeric duration fields", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.StartupPerformance).Contains("do not identify a current bottleneck", StringComparison.Ordinal),
            "Startup performance is a sanitized diagnostic-only workflow");
        Require(WorkflowCatalog.Find("startup-applications")?.Kind == WorkflowKind.StartupApplications &&
            Workflows.Scopes(WorkflowKind.StartupApplications).Length == 0 && !Workflows.SupportsRepair(WorkflowKind.StartupApplications) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.StartupApplications).Count == 0 &&
            Workflows.Preview(WorkflowKind.StartupApplications).Contains("Command lines, target paths", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.StartupApplications).Contains("accounts and credentials are not collected", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.StartupApplications).Contains("does not prove an item ran", StringComparison.Ordinal),
            "Startup application inventory is private and diagnostic-only");
        var notificationRecipes = RecipeCatalog.ForWorkflow(WorkflowKind.SystemNotifications);
        var eventSystemPlan = Repairs.Plan(RepairKind.ComPlusEventSystem);
        var sensPlan = Repairs.Plan(RepairKind.SystemEventNotificationService);
        Require(WorkflowCatalog.Find("system-notifications")?.Kind == WorkflowKind.SystemNotifications &&
            Workflows.Scopes(WorkflowKind.SystemNotifications).SequenceEqual(new[] { RepairKind.ComPlusEventSystem, RepairKind.SystemEventNotificationService }) &&
            notificationRecipes.Select(recipe => recipe.Action).SequenceEqual(new[] { RepairKind.ComPlusEventSystem, RepairKind.SystemEventNotificationService }) &&
            notificationRecipes[1].Dependencies.SequenceEqual(new[] { notificationRecipes[0].Id }) &&
            ServiceRecovery.ServiceName(RepairKind.ComPlusEventSystem) == "EventSystem" &&
            ServiceRecovery.ServiceName(RepairKind.SystemEventNotificationService) == "SENS" &&
            Workflows.Preview(WorkflowKind.SystemNotifications).Contains("COM+ catalogs, subscriptions, notification registrations, consumers", StringComparison.Ordinal) &&
            Workflows.Preview(WorkflowKind.SystemNotifications).Contains("not end-to-end notification delivery", StringComparison.Ordinal),
            "System notifications exposes an ordered two-service workflow with explicit preservation and verification limits");
        Require(eventSystemPlan.Steps.Count == 2 && eventSystemPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "EventSystem" }) &&
            eventSystemPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "EventSystem" }) && eventSystemPlan.Steps[1].Verification &&
            sensPlan.Steps.Count == 2 && sensPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "SENS" }) &&
            sensPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "SENS" }) && sensPlan.Steps[1].Verification &&
            eventSystemPlan.Effects.Contains("Preserves COM+ catalogs, subscriptions", StringComparison.Ordinal) &&
            sensPlan.Effects.Contains("after the COM+ Event System prerequisite is verified", StringComparison.Ordinal),
            "System notification recovery plans start and verify only their exact services");
        var coreServiceKinds = new[] { RepairKind.PowerService, RepairKind.PlugAndPlayService, RepairKind.UserManagerService,
            RepairKind.GroupPolicyClientService, RepairKind.NetworkStoreInterfaceService, RepairKind.RpcEndpointMapperService,
            RepairKind.RemoteProcedureCallService, RepairKind.DcomServerProcessLauncherService,
            RepairKind.BackgroundTasksInfrastructureService, RepairKind.CoreMessagingService };
        Require(WorkflowCatalog.Find("core-services")?.Kind == WorkflowKind.CoreWindowsServices &&
            Workflows.Scopes(WorkflowKind.CoreWindowsServices).SequenceEqual(coreServiceKinds) &&
            RecipeCatalog.ForWorkflow(WorkflowKind.CoreWindowsServices).Select(recipe => recipe.Action).SequenceEqual(coreServiceKinds) &&
            ServiceRecovery.ServiceName(RepairKind.PowerService) == "Power" &&
            ServiceRecovery.ServiceName(RepairKind.PlugAndPlayService) == "PlugPlay" &&
            ServiceRecovery.ServiceName(RepairKind.UserManagerService) == "UserManager" &&
            ServiceRecovery.ServiceName(RepairKind.GroupPolicyClientService) == "gpsvc" &&
            ServiceRecovery.ServiceName(RepairKind.NetworkStoreInterfaceService) == "nsi" &&
            ServiceRecovery.ServiceName(RepairKind.RpcEndpointMapperService) == "RpcEptMapper" &&
            ServiceRecovery.ServiceName(RepairKind.RemoteProcedureCallService) == "RpcSs" &&
            ServiceRecovery.ServiceName(RepairKind.DcomServerProcessLauncherService) == "DcomLaunch" &&
            ServiceRecovery.ServiceName(RepairKind.BackgroundTasksInfrastructureService) == "BrokerInfrastructure" &&
            ServiceRecovery.ServiceName(RepairKind.CoreMessagingService) == "CoreMessagingRegistrar" &&
            Workflows.Preview(WorkflowKind.CoreWindowsServices).Contains("No policy refresh, device scan, account action, network change, power-setting change, registration change or task launch", StringComparison.Ordinal),
            "Core Windows services exposes ten exact recoveries with explicit preservation limits");
        foreach (var repair in coreServiceKinds)
        {
            var plan = Repairs.Plan(repair);
            string service = ServiceRecovery.ServiceName(repair)!;
            Require(plan.Steps.Count == 2 && plan.Steps[0].Arguments.SequenceEqual(new[] { "start", service }) &&
                plan.Steps[1].Arguments.SequenceEqual(new[] { "query", service }) && plan.Steps[1].Verification &&
                plan.Effects.Contains("Preserves", StringComparison.Ordinal) && plan.Effects.Contains("Verification covers stable service running state only", StringComparison.Ordinal),
                "Core service recovery starts and verifies only its exact service: " + repair);
        }
        var notificationHost = new Host { State = CheckState.Repairable };
        var notificationResult = await Workflows.RunAsync(WorkflowKind.SystemNotifications, true, folder, host: notificationHost);
        Require(notificationHost.Scanned.SequenceEqual(new[] { RepairKind.ComPlusEventSystem, RepairKind.SystemEventNotificationService }) &&
            notificationHost.Repaired.SequenceEqual(new[] { RepairKind.ComPlusEventSystem, RepairKind.SystemEventNotificationService }) &&
            notificationResult.Repairs.Count == 2,
            "System notifications verifies Event System before dispatching SENS recovery");
        notificationHost = new Host { State = CheckState.Repairable, Verified = false };
        notificationResult = await Workflows.RunAsync(WorkflowKind.SystemNotifications, true, folder, host: notificationHost);
        Require(notificationHost.Scanned.SequenceEqual(new[] { RepairKind.ComPlusEventSystem }) &&
            notificationHost.Repaired.SequenceEqual(new[] { RepairKind.ComPlusEventSystem }) &&
            notificationResult.Recipes[1].Outcome == RecipeOutcome.Blocked,
            "Failed Event System verification blocks SENS inspection and repair");
        var eventLogPlan = Repairs.Plan(RepairKind.WindowsEventLog);
        var eventChannelPlan = Repairs.Plan(RepairKind.EventChannelEnable);
        Require(WorkflowCatalog.Find("eventlog")?.Kind == WorkflowKind.EventLog &&
            Workflows.Scopes(WorkflowKind.EventLog).SequenceEqual(new[] { RepairKind.WindowsEventLog, RepairKind.EventChannelEnable }) &&
            eventLogPlan.Steps.Count == 2 && eventLogPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "EventLog" }) &&
            eventLogPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "EventLog" }) && eventLogPlan.Steps[1].Verification &&
            eventChannelPlan.Steps.Count == 0 && eventChannelPlan.Effects.Contains("one exact supported channel", StringComparison.Ordinal) &&
            RepairRecoveryCatalog.For(RepairKind.EventChannelEnable) == RepairRecoveryContract.EventChannelState,
            "Event Log workflow keeps service recovery separate from exact-channel guarded enablement");
        var clean = new Host();
        var commonScopes = Workflows.Scopes(WorkflowKind.CommonProblems);
        Require(WorkflowCatalog.Find("common-problems") is { Kind: WorkflowKind.CommonProblems, Title: "Full system scan" },
            "The broad workflow keeps its command key and exposes a clear title");
        var allServiceScopes = RecipeCatalog.All.Where(recipe => ServiceRecovery.ServiceName(recipe.Action) is not null &&
                recipe.Action != RepairKind.WindowsUpdateMedicService)
            .Select(recipe => recipe.Action).Distinct().ToArray();
        var commonRecipeScopes = RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems)
            .Select(recipe => recipe.Action).Distinct().ToArray();
        Require(commonScopes.Length == commonRecipeScopes.Length && commonRecipeScopes.All(commonScopes.Contains) &&
            commonScopes.Distinct().Count() == commonScopes.Length && allServiceScopes.All(commonScopes.Contains) && commonScopes.Contains(RepairKind.Workstation) &&
            commonScopes.Contains(RepairKind.PrintSpoolDirectory) && commonScopes.Contains(RepairKind.PrintJobCancellation) &&
            commonScopes.Contains(RepairKind.NetworkLocationAwareness) && commonScopes.Contains(RepairKind.NetworkListService) &&
            commonScopes.Contains(RepairKind.IpHelper) && ServiceRecovery.ServiceName(RepairKind.IpHelper) == "iphlpsvc" &&
            !commonScopes.Contains(RepairKind.ComponentCleanup) && commonScopes.Contains(RepairKind.BluetoothSupport) && commonScopes.Contains(RepairKind.CameraFrameServer) && commonScopes.Contains(RepairKind.SmartCardService) && commonScopes.Contains(RepairKind.BiometricService) &&
            commonScopes.Contains(RepairKind.WindowsInstaller) && commonScopes.Contains(RepairKind.InstallerRegistration) && commonScopes.Contains(RepairKind.MsiProduct) && commonScopes.Contains(RepairKind.UpdateOrchestrator) &&
            commonScopes.Contains(RepairKind.BaseFilteringEngine) && commonScopes.Contains(RepairKind.WindowsFirewall) &&
            commonScopes.Contains(RepairKind.TaskSchedulerService) && commonScopes.Contains(RepairKind.DiagnosticPolicyService) &&
            commonScopes.Contains(RepairKind.WindowsErrorReportingService) && commonScopes.Contains(RepairKind.WindowsEventCollectorService) &&
            !commonScopes.Contains(RepairKind.EventChannelEnable) && !commonScopes.Contains(RepairKind.WindowsUpdateMedicService) &&
            ServiceRecovery.ServiceName(RepairKind.UpdateOrchestrator) == "UsoSvc",
            "Common problems has every broad-scan service recipe while excluding focused demand-start authorization and component cleanup");
        var commonHost = new Host();
        var common = await Workflows.RunAsync(WorkflowKind.CommonProblems, false, folder, host: commonHost);
        Require(commonHost.Scanned.SequenceEqual(commonScopes) && common.Repairs.Count == 0 && common.Recipes.Count == commonScopes.Length,
            "Common problems diagnoses every selected recipe in one journal without implicit repair");
        commonHost = new Host { State = CheckState.Repairable };
        common = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: commonHost);
        int executableCommonRecipes = RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Count(recipe => recipe.CanAutoExecute);
        Require(common.Repairs.Count == executableCommonRecipes && common.Repairs.All(Repairs.IsVerified) &&
            !commonHost.Repaired.Contains(RepairKind.WmiProvider), "Combined automatic recovery preserves diagnostic-only scope and verification");
        Require(RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Single(r => r.Action == RepairKind.SystemFiles).Dependencies.Contains("windows.component-store"),
            "Combined recovery preserves component-store prerequisite for system files");
        foreach (string blocker in new[] { "Administrator", "Restart", "System disk" })
        {
            var blockedCommon = new Host { State = CheckState.Repairable, Blocker = blocker };
            var blockedResult = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: blockedCommon);
            Require(blockedResult.Status == (blocker == "Administrator" ? "Administrator access required; workflow incomplete" : "Needs attention") && blockedCommon.Scanned.Count == 0 && blockedCommon.Repaired.Count == 0,
                "Combined workflow obeys machine-wide prerequisite: " + blocker);
        }
        var failedCommon = new Host { State = CheckState.Repairable, Verified = false };
        var timedInspectionHost = new Host { State = CheckState.Repairable, TimedInspection = true };
        var timedInspection = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: timedInspectionHost);
        Require(timedInspectionHost.Scanned.Count == 0 && timedInspectionHost.Repaired.Count == 0 &&
            timedInspection.RecoveryPlan.Action == RecoveryNextAction.RecheckTimedOutWork && timedInspection.Repairs.Count == 0,
            "Timed-out preliminary collection defers recipe execution and directs fresh verification");
        var failedCommonResult = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, folder, host: failedCommon);
        Require(failedCommonResult.Status == "Needs attention" && !failedCommon.Scanned.Contains(RepairKind.SystemFiles) &&
            !failedCommon.Scanned.Contains(RepairKind.PrintQueues) && !failedCommon.Scanned.Contains(RepairKind.DhcpLease) &&
            !failedCommon.Scanned.Contains(RepairKind.WindowsAudio) && failedCommon.Scanned.Contains(RepairKind.WindowsSearch) &&
            failedCommonResult.Repairs.All(repair => !Repairs.IsVerified(repair)),
            "Unverified prerequisites block dependent repairs while independent checks continue without success claims");
        using (var failedCommonJournal = JsonDocument.Parse(await File.ReadAllTextAsync(failedCommonResult.JournalPath)))
            Require(failedCommonJournal.RootElement.GetProperty("recipes").GetArrayLength() == commonScopes.Length &&
                failedCommonJournal.RootElement.GetProperty("repairs").GetArrayLength() == failedCommonResult.Repairs.Count,
                "Combined failure journal retains every recipe and attempted repair");
        var result = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, host: clean);
        Require(clean.Scanned.SequenceEqual(Workflows.Scopes(WorkflowKind.WindowsUpdate)) && clean.Repaired.Count == 0 &&
            result.Status.StartsWith("No issues"), "Clean scans do not repair");
        var damaged = new Host { State = CheckState.Repairable };
        result = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, host: damaged);
        Require(damaged.Repaired.SequenceEqual(new[] { RepairKind.UpdateService, RepairKind.BackgroundTransfer, RepairKind.CryptographicServices, RepairKind.ModulesInstaller, RepairKind.UpdateOrchestrator, RepairKind.DeliveryOptimization, RepairKind.ComponentStore, RepairKind.SystemFiles }) &&
            result.Status == "Needs attention" && result.Recipes.Any(recipe => recipe.RecipeId == "update.data-store-reset" && recipe.Outcome == RecipeOutcome.RepairAvailable),
            "Update recovery verifies automatic repairs while preserving the focused data-store option for review");
        using var journal = JsonDocument.Parse(await File.ReadAllTextAsync(result.JournalPath));
        Require(journal.RootElement.GetProperty("repairs").GetArrayLength() == 8, "Workflow journal retains repair outcomes");
        string repairedReport = WorkflowReport.Format(result);
        Require(repairedReport.Contains("Repaired and verified") && !repairedReport.Contains(": Repairable") && repairedReport.Contains("NEXT:"), "Final report distinguishes resolved repairs from original diagnosis and gives next action");
        foreach (var state in new[] { CheckState.Unknown, CheckState.Attention, CheckState.Repairable })
        {
            var readOnly = new Host { State = state };
            result = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, false, folder, host: readOnly);
            Require(readOnly.Repaired.Count == 0 && result.Status == "Needs attention", "Diagnosis never repairs");
            if (state == CheckState.Repairable) Require(WorkflowReport.Format(result).Contains("Repair available; not run") && WorkflowReport.Format(result).Contains("press GO") && WorkflowReport.Format(result, true).Contains("--auto-repair"), "Diagnosis report explains how to run eligible repairs on the current surface");
        }
        var unknown = new Host { State = CheckState.Unknown };
        Require(InstallerDiagnostics.Assess(new(null, false, true)).State == CheckState.Clear, "Unconfigured MSI policy is not treated as explicit restriction");
        foreach (var policy in new object[] { 1, 2 })
            Require(InstallerDiagnostics.Assess(new(policy, false, true)).State == CheckState.Attention, "Installer policy restrictions are preserved");
        Require(InstallerDiagnostics.Assess(new("0", false, true)).State == CheckState.Unknown, "Invalid policy type cannot pass readiness");
        Require(InstallerDiagnostics.Assess(new(0, false, true, 1)).State == CheckState.Attention, "Machine rollback restriction blocks readiness");
        Require(InstallerDiagnostics.Assess(new(0, false, true, 0)).State == CheckState.Clear, "Explicit rollback allowance does not block readiness");
        Require(InstallerDiagnostics.Assess(new(0, false, true) { InspectedAccount = "fixture", UserDisableRollback = 1 }).State == CheckState.Attention,
            "Per-user rollback restriction blocks readiness independently of machine policy");
        Require(InstallerDiagnostics.Assess(new(0, false, true) { InspectedAccount = "fixture", UserDisableRollback = "1" }).State == CheckState.Unknown,
            "Malformed per-user rollback policy cannot pass readiness");
        Require(InstallerDiagnostics.Assess(new(0, false, true) { InspectedAccount = "fixture", UserDisableRollback = 0 }).State == CheckState.Clear,
            "Explicit per-user rollback allowance does not block readiness");
        foreach (object rollback in new object[] { "1", 2, -1 })
            Require(InstallerDiagnostics.Assess(new(0, false, true, rollback)).State == CheckState.Unknown, "Malformed rollback policy cannot pass readiness");
        Require(InstallerDiagnostics.Assess(new(0, true, true)).State == CheckState.Attention, "In-progress marker defers recovery without deleting state");
        Require(InstallerDiagnostics.Assess(new(0, false, false)).State == CheckState.Attention, "Unavailable installer binaries need protected-file investigation");
        var installerBlocked = new Host { Blocker = "Installer readiness", State = CheckState.Repairable };
        var installerResult = await Workflows.RunAsync(WorkflowKind.Installer, true, folder, host: installerBlocked);
        Require(installerBlocked.Scanned.Count == 0 && installerBlocked.Repaired.Count == 0 && installerResult.Status == "Needs attention", "Installer readiness blocks automatic service recovery");
        await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, host: unknown);
        Require(unknown.Repaired.Count == 0, "Unknown checks cannot select repair");
        foreach (string blocker in new[] { "Restart", "System disk", "Administrator" })
        {
            var blocked = new Host { Blocker = blocker };
            result = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, host: blocked);
            Require(blocked.Scanned.Count == 0 && result.Status == (blocker == "Administrator" ? "Administrator access required; workflow incomplete" : "Needs attention"), "Prerequisite blocks servicing");
        }
        var failed = new Host { State = CheckState.Repairable, Verified = false };
        result = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, host: failed);
        Require(failed.Repaired.Count == 7 && failed.Repaired.Contains(RepairKind.DeliveryOptimization) &&
            !failed.Scanned.Contains(RepairKind.SystemFiles) && result.Status == "Needs attention",
            "Failed component verification blocks dependent SFC while independent recoveries continue");
        var vitals = new Host();
        await Workflows.RunAsync(WorkflowKind.Vitals, true, folder, host: vitals);
        Require(vitals.Scanned.Count == 0 && vitals.Repaired.Count == 0, "Vitals never runs servicing");
        var network = new Host();
        await Workflows.RunAsync(WorkflowKind.Network, true, folder, host: network);
        Require(network.Scanned.SequenceEqual(Workflows.Scopes(WorkflowKind.Network)) && network.Repaired.Count == 0, "Healthy network checks do not repair");
        var missingLease = new Host { State = CheckState.Repairable };
        var renewed = await Workflows.RunAsync(WorkflowKind.Network, true, folder, host: missingLease);
        Require(missingLease.Repaired.SequenceEqual(RecipeCatalog.ForWorkflow(WorkflowKind.Network).Where(recipe => recipe.CanAutoExecute).Select(recipe => recipe.Action)) &&
            renewed.Status == "Needs attention" && renewed.Recipes.Any(recipe => recipe.RecipeId == "network.firewall-profiles" && recipe.Outcome == RecipeOutcome.RepairAvailable),
            "Network recovery verifies automatic repairs while preserving the focused firewall option for review");
        foreach (var refreshKind in new[] { WorkflowKind.Network, WorkflowKind.Audio, WorkflowKind.Search })
        {
            var emptyRefresh = await Workflows.RunAsync(refreshKind, true, folder,
                host: new Host { State = CheckState.Repairable, EmptyRefresh = true });
            Require(emptyRefresh.Status == "Needs attention" && emptyRefresh.Repairs.Count == RecipeCatalog.ForWorkflow(refreshKind).Count(recipe => recipe.CanAutoExecute) &&
                emptyRefresh.Checks.Any(c => c.Name == "Follow-up inspection" && c.State == CheckState.Unknown),
                "Empty follow-up retains repairs but cannot establish refreshed health");
            var refreshHost = new Host { State = CheckState.Repairable, RefreshFailure = true };
            var partial = await Workflows.RunAsync(refreshKind, true, folder, host: refreshHost);
            int automaticRepairs = RecipeCatalog.ForWorkflow(refreshKind).Count(recipe => recipe.CanAutoExecute);
            Require(refreshHost.Inspections == 2 && partial.Status == "Needs attention" && partial.Repairs.Count == automaticRepairs &&
                partial.Repairs.All(Repairs.IsVerified), "Follow-up failure retains completed verified repairs without claiming refreshed health");
            using var savedPartial = JsonDocument.Parse(await File.ReadAllTextAsync(partial.JournalPath));
            Require(savedPartial.RootElement.GetProperty("repairs").GetArrayLength() == automaticRepairs &&
                WorkflowReport.Format(partial).Contains("Synthetic refresh failure"), "Journal and report retain repairs and explain refresh failure");
        }
        Require(WindowsWorkflowHost.AssessNetwork("fixture", ["169.254.1.2"], [], []).State == CheckState.Attention, "Link-local configuration needs attention");
        Require(WindowsWorkflowHost.AssessNetwork("fixture", ["192.0.2.2"], ["192.0.2.1"], ["192.0.2.1"]).State == CheckState.Clear, "Configured adapter is not a reachability verdict");
        var gatewayOnly = WindowsWorkflowHost.AssessNetwork("fixture", ["192.0.2.2"], [], ["192.0.2.1"]);
        Require(gatewayOnly.State == CheckState.Attention && gatewayOnly.Repair is null && gatewayOnly.Detail.Contains("No default gateway") &&
            !gatewayOnly.Detail.Contains("No DNS server") && !gatewayOnly.Detail.Contains("No usable host"), "Missing gateway is identified independently");
        var dnsOnly = WindowsWorkflowHost.AssessNetwork("fixture", ["192.0.2.2"], ["192.0.2.1"], []);
        Require(dnsOnly.State == CheckState.Attention && dnsOnly.Repair is null && dnsOnly.Detail.Contains("No DNS server") &&
            !dnsOnly.Detail.Contains("No default gateway") && !dnsOnly.Detail.Contains("No usable host"), "Missing DNS is identified independently");
        var addressOnly = WindowsWorkflowHost.AssessNetwork("fixture", ["169.254.1.2"], ["192.0.2.1"], ["192.0.2.1"]);
        Require(addressOnly.State == CheckState.Attention && addressOnly.Repair is null && addressOnly.Detail.Contains("No usable host") &&
            !addressOnly.Detail.Contains("No default gateway") && !addressOnly.Detail.Contains("No DNS server"), "Unusable host address is identified independently");
        foreach (string unusable in new[] { "0.0.0.0", "127.0.0.2", "::", "::1", "fe80::1", "ff02::1", "224.0.0.1", "255.255.255.255", "::ffff:127.0.0.2", "::ffff:169.254.1.2" })
            Require(WindowsWorkflowHost.AssessNetwork("fixture", [unusable], ["192.0.2.1"], ["192.0.2.1"]).State == CheckState.Attention,
                "Unspecified, loopback, link-local and multicast addresses cannot establish usable host configuration");
        Require(WindowsWorkflowHost.AssessNetwork("fixture", ["2001:db8::2", "fe80::2"], ["fe80::1"], ["::1"]).State == CheckState.Clear,
            "IPv6 host configuration permits link-local gateways and local DNS resolvers");
        Require(WindowsWorkflowHost.AssessNetwork("fixture", ["not-an-address"], ["192.0.2.1"], ["192.0.2.1"]).State == CheckState.Unknown,
            "Malformed host evidence remains unknown");
        Require(WindowsWorkflowHost.AssessNetwork("fixture", ["192.0.2.2"], ["bad"], ["192.0.2.1"]).State == CheckState.Unknown,
            "Malformed gateway evidence remains unknown");
        var files = new Host();
        await Workflows.RunAsync(WorkflowKind.SystemFiles, false, folder, host: files);
        Require(files.Scanned.SequenceEqual(new[] { RepairKind.SystemFiles }), "System files selects SFC only");
        using var stop = new CancellationTokenSource();
        var canceled = new Host { State = CheckState.Repairable, AfterScan = stop.Cancel };
        result = await Workflows.RunAsync(WorkflowKind.WindowsUpdate, true, folder, token: stop.Token, host: canceled);
        Require(canceled.Repaired.Count == 0 && result.Status.StartsWith("Stopped"), "Stop between diagnosis and repair");
        foreach (string text in new[] { "historical 0x80073712", "localized output", "" })
            Require(Workflows.Interpret(RepairKind.ComponentStore, new(0, text, "", false)).State == CheckState.Unknown, "Historical codes are not current diagnoses");
        Require(Workflows.Interpret(RepairKind.ComponentStore, new(0, "The component store is repairable.", "", false)).State == CheckState.Repairable, "Confirmed DISM corruption");
        var unrecoverableStore = Workflows.Interpret(RepairKind.ComponentStore,
            new(0, "The component store cannot be repaired.", "", false));
        Require(unrecoverableStore.State == CheckState.Attention && unrecoverableStore.Repair is null &&
            unrecoverableStore.Detail.Contains("in-place repair or recovery plan"),
            "Unrecoverable DISM result requests review without selecting RestoreHealth");
        Require(Workflows.Interpret(RepairKind.ComponentStore,
            new(0, "No component store corruption detected. The component store cannot be repaired.", "", false)).State == CheckState.Unknown,
            "Contradictory DISM outcomes fail closed");
        Require(Workflows.Interpret(RepairKind.SystemFiles, new(0, "Windows Resource Protection found integrity violations.", "", false)).State == CheckState.Repairable, "Confirmed SFC corruption");
        Require(Workflows.Interpret(RepairKind.ComponentStore, new(5, "The component store is repairable.", "", false)).State == CheckState.Unknown, "Failed check never repairs");
        Require(Workflows.Interpret(RepairKind.ComponentStore, new(0, "The component store is repairable.", "", true)).State == CheckState.Unknown, "Truncated check never repairs");
        using var during = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var waiting = new Host { WaitForStop = true };
        var stopped = await Workflows.RunAsync(WorkflowKind.SystemFiles, false, folder, token: during.Token, host: waiting).WaitAsync(TimeSpan.FromSeconds(5));
        Require(stopped.Status.StartsWith("Stopped") && waiting.Repaired.Count == 0, "Cancellation reaches an active diagnostic command");
        Console.WriteLine("PASS: workflow selection, blockers, automatic decisions, verification, journaling and cancellation (simulated).");
    }
    private sealed class Host : IWorkflowHost
    {
        public CheckState State = CheckState.Clear;
        public string? Blocker;
        public bool Verified = true;
        public bool WaitForStop;
        public bool RefreshFailure;
        public bool EmptyRefresh;
        public bool EmptyInspection;
        public IReadOnlyList<WorkflowCheck>? InspectionOverride;
        public bool IgnoreInspectionCancellation;
        public Action? StopRefresh;
        public int Inspections;
        public bool TimedInspection;
        public Action? AfterScan;
        public List<RepairKind> Scanned = [], Repaired = [];
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token,
            Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint, IReadOnlyList<CommonDiagnosticScope>? selectedScopes) =>
            selectedScopes is null ? InspectAsync(kind, token) : Task.FromResult<IReadOnlyList<WorkflowCheck>>(
                selectedScopes.Select(scope => new WorkflowCheck(scope.ToString(), CheckState.Information, "Fixture selected scope")).ToArray());
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token)
        {
            Inspections++;
            if (InspectionOverride is not null) return Task.FromResult(InspectionOverride);
            if (TimedInspection) return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new("Supplemental collector", CheckState.Unknown,
                "Fixture timed out", Evidence: new ToolResult(-1, "", "", false) { TimedOut = true })]);
            if (Inspections > 1 && StopRefresh is not null)
            {
                StopRefresh();
                return new TaskCompletionSource<IReadOnlyList<WorkflowCheck>>().Task;
            }
            if (IgnoreInspectionCancellation) return new TaskCompletionSource<IReadOnlyList<WorkflowCheck>>().Task;
            if (EmptyRefresh && Inspections > 1) return Task.FromResult<IReadOnlyList<WorkflowCheck>>([]);
            if (EmptyInspection) return Task.FromResult<IReadOnlyList<WorkflowCheck>>([]);
            if (RefreshFailure && Inspections > 1) throw new IOException("Synthetic refresh failure");
            if (kind == WorkflowKind.WindowsUpdate)
                return Task.FromResult<IReadOnlyList<WorkflowCheck>>(WindowsUpdateEvidence(Blocker));
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(Blocker ?? "Restart", Blocker is null ? CheckState.Clear : CheckState.Attention, "Synthetic prerequisite")]);
        }
        public async Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token)
        { Scanned.Add(kind); if (WaitForStop) await Task.Delay(Timeout.Infinite, token); AfterScan?.Invoke(); return new WorkflowCheck(kind.ToString(), State, "Synthetic check", State == CheckState.Repairable ? kind : null); }
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token)
        { Repaired.Add(kind); return Task.FromResult(RecipeChecks.Success(kind) with { Status = Verified ? "Verification reported no corruption" : "Command failed", Steps = Verified ? RecipeChecks.Success(kind).Steps : [new(Repairs.Plan(kind).Steps[0], new(5, "Synthetic known failure", "", false))] }); }
    }
    private sealed class BrokenProgress : IProgress<string>
    {
        public void Report(string value) => throw new InvalidOperationException("Fixture observer failure");
    }
    private sealed class FocusedStopHost(Action stop) : IWorkflowHost
    {
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            throw new Exception("Focused selection was lost");
        public async Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token,
            Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint, IReadOnlyList<CommonDiagnosticScope>? selectedScopes)
        {
            Require(selectedScopes?.Count == 2, "Focused host receives requested selection");
            await checkpoint([new("Retained focused finding", CheckState.Information, "Completed before Stop")]);
            stop();
            return await new TaskCompletionSource<IReadOnlyList<WorkflowCheck>>().Task;
        }
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Focused scan dispatched an integrity recipe");
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Focused scan dispatched a repair");
    }
    private static WorkflowCheck[] WindowsUpdateEvidence(string? blocker)
    {
        var evidence = new[]
        {
            new WorkflowCheck("Windows Update servicing-chain correlation", CheckState.Clear, "fixture"),
            new WorkflowCheck("Windows Update engine files", CheckState.Information, "fixture"),
            new WorkflowCheck("Restart", CheckState.Clear, "fixture"),
            new WorkflowCheck("Servicing transition readiness", CheckState.Information, "fixture"),
            new WorkflowCheck("System disk", CheckState.Clear, "fixture"),
            new WorkflowCheck("Windows reserved storage", CheckState.Information, "fixture"),
            new WorkflowCheck("BITS transfer jobs", CheckState.Information, "fixture"),
            new WorkflowCheck("Update policy", CheckState.Information, "fixture"),
            new WorkflowCheck("Component repair source", CheckState.Information, "fixture"),
            new WorkflowCheck("Administrator", CheckState.Clear, "fixture")
        };
        if (blocker is not null)
        {
            int index = Array.FindIndex(evidence, check => check.Name == blocker);
            if (index >= 0) evidence[index] = new(blocker, CheckState.Attention, "Synthetic prerequisite");
        }
        return evidence;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
