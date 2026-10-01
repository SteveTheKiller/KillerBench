namespace KillerBench.Engine.Scheduling;

public static class ScheduledTaskDiagnostics
{
    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        var inventory = await ScheduledTaskInventory.ReadAsync(token).ConfigureAwait(false);
        return Array.AsReadOnly(new WorkflowCheck[]
        {
            new("Scheduled task inventory", inventory.Complete ? CheckState.Information : CheckState.Unknown,
                $"{inventory.Tasks.Count} visible tasks. Execution success and action contents are not tested. No tasks changed.",
                Evidence: inventory.Evidence)
        }.Concat(inventory.Tasks.Select(task => Assess(task))).ToArray());
    }

    public static WorkflowCheck Assess(ScheduledTaskSnapshot task, Func<uint, WindowsErrorMessage?>? messageLookup = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.Identity is null || task.State is not ("Unknown" or "Disabled" or "Queued" or "Ready" or "Running") ||
            task.ActionCount is < 0 or > 32 ||
            (task.ActionTypes is not null && (task.ActionCount != task.ActionTypes.Count || task.ActionTypes.Any(string.IsNullOrWhiteSpace))))
            return new(task.Identity?.FullName ?? "Scheduled task", CheckState.Unknown,
                "Task identity, state or action evidence is invalid or inconsistent. No repair selected.");
        bool missingActions = task.State is "Ready" or "Queued" or "Running" && task.ActionCount == 0;
        bool disabled = task.State == "Disabled";
        var state = task.State == "Unknown" || task.ActionCount is null ? CheckState.Unknown : missingActions ? CheckState.Attention : CheckState.Information;
        string runtime = "Run-time evidence was not collected.";
        if (!string.IsNullOrWhiteSpace(task.RuntimeError) || task.RuntimeAvailable == false || (task.RuntimeAvailable == true && (task.LastTaskResult is null || task.MissedRuns is null)))
        {
            if (state != CheckState.Attention && !disabled) state = CheckState.Unknown;
            runtime = disabled
                ? "Run-time evidence is unavailable or incomplete. The task is currently disabled, so no current execution problem is inferred."
                : "Run-time evidence is unavailable or incomplete. Recollect with access to this task; no execution outcome inferred.";
            if (!string.IsNullOrWhiteSpace(task.RuntimeError)) runtime += "\nProvider: " + task.RuntimeError;
        }
        else if (task.RuntimeAvailable == true)
        {
            uint code = task.LastTaskResult!.Value;
            string meaning = code switch
            {
                0 => "Last result reports zero; the action's intended effect is not verified.",
                0x41300 => "Task reports ready for its next scheduled run.",
                0x41301 => "Task reports a run in progress; this is not a completed outcome.",
                0x41302 => "Task reports disabled; this can be intentional.",
                0x41303 => "Task has not yet run.",
                0x41304 => "No further scheduled runs are reported.",
                0x41306 => "Last run was terminated by a user; review whether interruption was intended.",
                0x41308 => "Event triggers have no fixed run time.",
                0x41325 => "Run requested; completion remains unverified.",
                0x41305 or 0x8004130A => "Review missing scheduling properties with the task owner.",
                0x41307 or 0x80041309 => "Review absent or disabled triggers; preserve intentional settings.",
                0x4131B => "Some triggers failed. Review Scheduler events.",
                0x4131C => "Review batch-logon rights with the policy owner.",
                0x8004130F => "Review missing task account information; credentials remain unchanged.",
                0x80041310 => "Resolve the configured account with its owner.",
                0x80041314 => "Review incompatible account and execution options.",
                0x80041316 or 0x80041317 or 0x80041318 or 0x80041319 or 0x8004131A or 0x8004131D => "Validate the task XML against its schema; preserve the original definition.",
                0x8004131E => "Review the trigger's expired end boundary.",
                0x8004131F => "An instance already runs. Review overlap settings before retrying.",
                0x80041320 => "Required user session is absent. Review the intended logon mode.",
                0x80041321 => "Task integrity check failed. Preserve evidence and verify provenance before replacement.",
                0x80041324 => "Task conditions prevented execution. Review conditions before changing them.",
                0x80041328 => "On-demand starts are disabled by this definition.",
                _ => "The nonzero result is retained for correlation with the task's action and Scheduler history. Application exit codes can have component-specific meanings, so this value alone does not establish a current problem."
            };
            if (disabled)
                meaning = "The task is currently disabled. Its retained result and missed-run count are historical and do not establish a current problem.";
            if (!disabled && (RequiresDefinitionReview(code) || task.MissedRuns > 0))
                state = CheckState.Attention;
            runtime = $"Last result: 0x{code:X8}; missed runs: {task.MissedRuns}. {meaning} Reported history is not a current root-cause diagnosis. No retry selected.";
            if (!disabled && code >= 0x80000000 && TryExplain(code, messageLookup, out string explanation))
                runtime += "\nInstalled Windows message: " + explanation +
                    " This is a system message-table correlation only. The task action and Task Scheduler history still determine what the result means here.";
            if (code != 0x41303)
                runtime += "\nReported last-run local time: " + (task.LastRunLocalTime?.ToString("yyyy-MM-dd HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture) ?? "unavailable") +
                    ". Source-machine wall clock; UTC offset and clock accuracy are unverified.";
        }
        string detail = $"State: {task.State}; user: {task.UserId ?? "unreported"}; group: {task.GroupId ?? "unreported"}; run level: {task.RunLevel ?? "unreported"}; actions: {task.ActionCount?.ToString() ?? "unreported"}; " +
            $"action classes: {(task.ActionTypes is null ? "unreported" : string.Join(", ", task.ActionTypes))}; trigger classes: {(task.TriggerTypes is null ? "unreported" : task.TriggerTypes.Count == 0 ? "none reported" : string.Join(", ", task.TriggerTypes))}.\n" +
            (missingActions ? "This enabled task reports no actions to execute. Review its definition and owner before changing it.\n" : "") +
            (task.State == "Unknown" ? "The provider could not determine this task's state.\n" : "") +
            (task.ActionCount is null ? "The action count is unavailable. Recollect the task definition before assessing whether it has actions.\n" : "") +
            "Disabled tasks and tasks without triggers can be intentional. Action contents, trigger settings and execution success are not verified. No task changed.";
        return new(task.Identity.FullName, state, detail + "\n" + runtime);
    }

    private static bool RequiresDefinitionReview(uint code) => code is
        0x41305 or 0x41307 or 0x4131B or 0x4131C or
        0x80041309 or 0x8004130A or 0x8004130F or 0x80041310 or 0x80041314 or
        0x80041316 or 0x80041317 or 0x80041318 or 0x80041319 or 0x8004131A or
        0x8004131D or 0x8004131E or 0x8004131F or 0x80041320 or 0x80041321 or
        0x80041324 or 0x80041328;

    private static bool TryExplain(uint code, Func<uint, WindowsErrorMessage?>? messageLookup, out string explanation)
    {
        explanation = string.Empty;
        try
        {
            var reference = (messageLookup ?? WindowsErrorReference.Lookup)(code);
            if (reference is null || reference.Code != code) return false;
            explanation = WindowsErrorReference.Interpret(code, reference.Message).Message ?? string.Empty;
            return explanation.Length > 0;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or PlatformNotSupportedException or System.Runtime.InteropServices.ExternalException)
        {
            return false;
        }
    }
}
