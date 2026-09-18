using System.Text;

namespace KillerBench.Engine;

public static class WorkflowReport
{
    public static string NextAction(WorkflowResult result, bool commandLine = false)
        => BuildNextAction(result, commandLine, result.Summary);

    public static string Format(WorkflowResult result, bool commandLine = false)
    {
        var text = new StringBuilder();
        text.AppendLine((WorkflowCatalog.All.FirstOrDefault(w => w.Kind == result.Kind)?.Title ?? result.Kind.ToString()) + " / " + result.Status);
        text.AppendLine("THIS PC: " + result.Machine);
        if (result.SelectedDiagnosticScopes is not null)
            text.AppendLine("Selected diagnostic areas: " + string.Join(", ", result.SelectedDiagnosticScopes) + ". Other areas were not requested.");
        text.AppendLine();
        var summary = result.Summary;
        text.AppendLine($"Verified repairs: {summary.VerifiedRepairs} | Available repairs: {summary.AvailableRepairs}");
        text.AppendLine($"Needs attention: {summary.NeedsAttention} | Incomplete: {summary.Incomplete} | Deferred: {summary.Deferred}");
        text.AppendLine($"Clear checks: {summary.ClearChecks} | Informational: {summary.Informational}");
        var standalone = result.Checks.Where(c => !result.Recipes.Any(r => Equals(r.Check, c) || r.RecipeId == c.Name)).ToArray();
        static bool CheckNeedsReview(WorkflowCheck check) => check.EffectiveState is not (CheckState.Clear or CheckState.Information);
        static bool RecipeNeedsReview(RecipeResult recipe) => recipe.EffectiveOutcome != RecipeOutcome.Clear &&
            !(recipe.EffectiveOutcome == RecipeOutcome.Repaired && recipe.Repair is not null &&
              recipe.Check?.Repair == recipe.Repair.Kind && Repairs.IsVerified(recipe.Repair));
        text.AppendLine("Review items: " + (standalone.Count(CheckNeedsReview) + result.Recipes.Count(RecipeNeedsReview)) +
            " | Actions recorded: " + (result.Actions.Count + result.Repairs.Count));
        int nextPosition = text.Length;
        text.AppendLine();
        foreach (bool needsReview in new[] { true, false })
        {
        foreach (var check in standalone.Where(c => CheckNeedsReview(c) == needsReview))
        {
            text.AppendLine(check.Name + ": " + check.EffectiveState);
            if (check.EffectiveState != check.State)
                text.AppendLine("Reported clear state is contradicted by failed or incomplete command evidence. Review the retained evidence.");
            text.AppendLine(check.Detail);
            text.AppendLine();
        }
        foreach (var recipe in result.Recipes.Where(r => RecipeNeedsReview(r) == needsReview))
        {
            string title = RecipeCatalog.All.FirstOrDefault(r => r.Id == recipe.RecipeId)?.Title ?? recipe.RecipeId;
            bool verified = recipe.Repair is not null && recipe.Check?.Repair == recipe.Repair.Kind && Repairs.IsVerified(recipe.Repair);
            string outcome = recipe.EffectiveOutcome switch
            {
                RecipeOutcome.Repaired when verified => "Repaired and verified",
                RecipeOutcome.Repaired => "Verification incomplete",
                RecipeOutcome.Clear => "No issue detected in this check",
                RecipeOutcome.RepairAvailable => "Repair available; not run",
                RecipeOutcome.RestartRequired => "Restart required; verification pending",
                RecipeOutcome.Blocked => "Deferred",
                RecipeOutcome.Canceled => "Stopped",
                RecipeOutcome.Unknown => "Could not determine",
                _ => "Needs attention"
            };
            text.AppendLine(title + ": " + outcome);
            if (recipe.EffectiveOutcome != recipe.Outcome)
                text.AppendLine("The recorded outcome is not supported by complete diagnostic evidence. Review the retained result before proceeding.");
            text.AppendLine(recipe.Detail);
            if (recipe.Repair is { } repair)
            {
                if (repair.Steps is null || repair.Steps.Any(s => s?.Result is null))
                    text.AppendLine("Details: Command evidence is missing; repair verification is incomplete.");
                foreach (var step in (repair.Steps ?? []).Where(s => s?.Result is not null && (s.Result.ExitCode != 0 || s.Result.TimedOut || s.Result.Truncated || !string.IsNullOrWhiteSpace(s.Result.Error))).Take(3))
                {
                    string error = string.IsNullOrWhiteSpace(step.Result.Error) ? step.Result.Output ?? "" : step.Result.Error;
                    string state = step.Result.TimedOut ? "timed out" : "exit code " + step.Result.ExitCode;
                    if (step.Result.Truncated) state += "; captured output incomplete";
                    text.AppendLine("Details: " + (step.Step is null ? "Unidentified" : step.Step.Verification ? "Verification" : "Repair") + " command " + state + ". " +
                        (string.IsNullOrWhiteSpace(error) ? "No diagnostic text was returned." :
                        new string(error.Take(1000).Select(c => char.IsControl(c) ? ' ' : c).ToArray())) +
                        (error.Length > 1000 ? " [shortened; recorded output in journal]" : ""));
                    if (step.Result.TimedOut && !string.IsNullOrWhiteSpace(step.Result.Error) && !string.IsNullOrWhiteSpace(step.Result.Output))
                    {
                        string observed = step.Result.Output;
                        text.AppendLine("Last captured output (not current state): " +
                            new string(observed.Take(1000).Select(c => char.IsControl(c) ? ' ' : c).ToArray()) +
                            (observed.Length > 1000 ? " [shortened; recorded output in journal]" : ""));
                    }
                }
            }
            text.AppendLine();
        }
        }
        string next = BuildNextAction(result, commandLine, summary);
        text.Insert(nextPosition, "NEXT: " + next + Environment.NewLine);
        foreach (var action in result.Actions)
        {
            text.AppendLine("Action: " + action.Name + " - " + action.Status);
            text.AppendLine(action.Detail);
            text.AppendLine("Next: " + action.NextAction);
            text.AppendLine();
        }
        text.AppendLine("Actions recorded: " + (result.Actions.Count + result.Repairs.Count));
        text.AppendLine("Journal: " + result.JournalPath);
        text.AppendLine("Results apply only to checked scopes. Complete feature operation remains untested.");
        return text.ToString();
    }

    private static string BuildNextAction(WorkflowResult result, bool commandLine, WorkflowSummary summary)
    {
        static bool CheckNeedsReview(WorkflowCheck check) => check.EffectiveState is not (CheckState.Clear or CheckState.Information);
        string next = result.RequiresRestart ? "Restart when convenient, then run this workflow again to verify and continue." :
            result.Actions.Count > 0 ? result.Actions[^1].NextAction :
            result.Status.StartsWith("Stopped", StringComparison.Ordinal) ? "Review the saved journal before starting another repair; the previous operation may be incomplete." :
            Enum.IsDefined(result.Kind) && Workflows.Scopes(result.Kind).Length > 0 && result.Checks.Any(c => c.Name == "Administrator" && c.State == CheckState.Attention) ?
                (commandLine ? "Open an administrator terminal and rerun this workflow to complete its checks. " : "Run KillerBench as administrator, then select this check and press GO again. ") +
                (Workflows.SupportsRepair(result.Kind) ? "Review the new results before enabling repairs." : "This workflow performs checks only; no repair or deletion is selected.") :
            result.RecoveryPlan.Action == RecoveryNextAction.ConfigureRepairOptions ? "Choose the exact repair target and required options, review its effects, then confirm that specific repair. The generic automatic-repair switch will not run it." :
            result.RecoveryPlan.Action == RecoveryNextAction.ReviewAvailableRepairs ? (commandLine ? "Rerun this workflow with --auto-repair to run eligible repairs." : result.Kind == WorkflowKind.ComponentCleanup ? "Select Run recommended cleanup and press GO to perform the recommended maintenance." : "Select Auto-repair confirmed issues and press GO to run eligible repairs.") :
            result.Kind == WorkflowKind.DeviceProblems ?
                (result.Checks.Any(c => c.State == CheckState.Attention) ? "Review the reported device ID and configuration code in Device Manager. Use its hardware IDs to locate an approved vendor driver; a code alone does not prove hardware failure. " :
                 result.Checks.Any(CheckNeedsReview) || result.Checks.Count == 0 ? "Device evidence is incomplete. Resolve the reported collection problem and repeat this check before deciding whether a driver or device needs attention. " :
                 "No active fault requiring attention was identified in the collected device evidence. Review any absent devices and test the affected hardware directly; undetected devices remain outside this snapshot. ") +
                "No device was enabled, reset or given a different driver." :
            result.Kind == WorkflowKind.Input ?
                (result.Checks.Any(CheckNeedsReview) ? "Use the reported device name, ID and code to review its connection and approved driver in Device Manager. Missing driver inventory is not proof that a driver is absent. " : "No configuration fault was reported in the collected scopes. Test the affected keyboard, mouse or HID function directly. ") +
                "No input was captured and no device reset or driver change was performed." :
            result.Status == "Needs attention" ? "Review the unresolved findings below. No broader repair has been selected automatically." :
            result.Kind == WorkflowKind.AvailableUpdates ? "Review the returned update list. Installation is not enabled by this discovery workflow." :
            result.Kind == WorkflowKind.Cleanup ? "Review the estimate before selecting permanent removal. Preview did not remove files." :
            result.Kind == WorkflowKind.Privacy ? "Review the selected privacy snapshots. Recorded values do not prove effective permissions. No privacy settings were changed." :
            result.Kind == WorkflowKind.BitLocker ? "Review the selected volume and backup options. This inspection did not back up keys or change encryption." :
            result.Kind == WorkflowKind.Profiles ? "Review the profile ages and exclusions. Age alone does not authorize deletion. Verify backups and account dependencies before any future removal." :
            result.Kind == WorkflowKind.ComputerName ? "Review the proposed name and environment notes. Confirm organizational naming policy, name availability and application dependencies before planning a rename. This review made no changes." :
            result.Kind == WorkflowKind.ComponentCleanup ? "Review the component-store recommendation below. Reclaimed space was not measured." :
            result.Kind == WorkflowKind.Foxit ? "Review the matching machine registry entries. This inventory does not verify installed executables, user installations or update policy. No Foxit settings were changed." :
            result.Kind == WorkflowKind.ScheduledTasks ? "Review full task paths and reported principals. Inventory does not verify actions, triggers, permissions or execution success. No tasks were started or changed." :
            "The selected checks finished. Test the affected feature to confirm normal operation.";
        next = result.RecoveryPlan.Action switch
        {
            RecoveryNextAction.Wait => "This workflow is still running. Wait for its final result before planning another repair.",
            RecoveryNextAction.ReviewInterruptedRun => "Review the saved journal before starting another repair; the previous operation may be incomplete.",
            RecoveryNextAction.RecheckTimedOutWork => "A command timed out. Confirm that the previous work has ended, then repeat the checks before selecting another repair.",
            RecoveryNextAction.RestartThenRecheck => "Restart when convenient, then run this workflow again to verify and continue.",
            RecoveryNextAction.ConfigureRepairOptions => "Choose the exact repair target and required options, review its effects, then confirm that specific repair. The generic automatic-repair switch will not run it.",
            RecoveryNextAction.ReviewFindings when summary.Incomplete > 0 &&
                next == "The selected checks finished. Test the affected feature to confirm normal operation." =>
                "Some checks could not determine a result. Review the incomplete evidence below and repeat those checks before selecting a repair.",
            _ => next
        };
        return next;
    }
}
