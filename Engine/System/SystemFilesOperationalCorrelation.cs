namespace KillerBench.Engine;

public static class SystemFilesOperationalCorrelation
{
    private static readonly string[] RequiredEvidenceNames =
    [
        "Administrator",
        "Restart",
        "System disk",
        "Servicing transition readiness",
        "Windows reserved storage",
        "CBS servicing log",
        "DISM servicing log",
        "Servicing log coverage"
    ];

    public static WorkflowCheck Check(RecipeResult result, IReadOnlyList<WorkflowCheck> evidence,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "System files operational conclusion";
        const string boundary = " This conclusion covers the current protected-file recipe result, servicing prerequisites and bounded CBS/DISM history only. It does not prove component-store health, install or remove packages, enable or disable features or capabilities, change a repair source or policy, restart Windows, authorize another repair, or establish that unrelated Windows components are healthy. No additional repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (!string.Equals(result.RecipeId, "windows.system-files", StringComparison.Ordinal) || result.RecipeVersion != 1)
            return Unknown("The protected-file result carried an unexpected recipe identity or version.");
        if (result.Check is not null && !string.Equals(result.Check.Name, RepairKind.SystemFiles.ToString(), StringComparison.Ordinal))
            return Unknown("The protected-file result did not match its expected current-state identity.");
        if (result.Check is null && result.EffectiveOutcome is RecipeOutcome.Clear or RecipeOutcome.Repaired or
            RecipeOutcome.RepairAvailable or RecipeOutcome.NeedsAttention or RecipeOutcome.RestartRequired)
            return Unknown("The protected-file result omitted the current-state evidence required by its outcome.");

        var selected = RequiredEvidenceNames
            .Select(required => evidence.Where(item => item.Name == required).ToArray())
            .ToArray();
        if (selected.Any(matches => matches.Length != 1))
        {
            string mismatches = string.Join("; ", RequiredEvidenceNames.Select((required, index) => (required, selected[index].Length))
                .Where(item => item.Length != 1).Select(item => item.required + ": " + item.Length));
            return Unknown("System-file correlation inputs were missing or duplicated: " + mismatches + ".");
        }

        var checks = selected.Select(matches => matches[0]).ToArray();
        var current = checks.Where(item => item.Name is not ("CBS servicing log" or "DISM servicing log" or "Servicing log coverage")).ToArray();
        var history = checks.Where(item => item.Name is "CBS servicing log" or "DISM servicing log").ToArray();
        int incompleteCurrent = current.Count(item => item.EffectiveState == CheckState.Unknown);
        int currentAttention = current.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int incompleteHistory = history.Count(item => item.EffectiveState == CheckState.Unknown);
        int historicalFindings = history.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed the current protected-file result, administrator and restart readiness, system-disk capacity, servicing transitions, reserved storage, and bounded CBS/DISM history. Current prerequisites: {currentAttention} need attention and {incompleteCurrent} incomplete. Historical findings: {historicalFindings}; incomplete histories: {incompleteHistory}. ";

        if (result.ExecutionUncertain || result.EffectiveOutcome is RecipeOutcome.Unknown or RecipeOutcome.Blocked or RecipeOutcome.Canceled ||
            incompleteCurrent > 0 || incompleteHistory > 0)
            return Unknown(detail + "The protected-file conclusion is incomplete. Recollect the missing or uncertain evidence before choosing another servicing action.");
        if (result.EffectiveOutcome is RecipeOutcome.RepairAvailable or RecipeOutcome.NeedsAttention or RecipeOutcome.RestartRequired || currentAttention > 0)
            return new(name, CheckState.Attention, detail + "Review the current protected-file result and prerequisite details before proceeding. Historical log findings remain context and do not establish current corruption." + boundary);
        if (result.EffectiveOutcome == RecipeOutcome.Repaired)
            return new(name, CheckState.Information, detail + "The selected protected-file repair reports verified completion within its declared scope. Historical log findings remain context." + boundary);
        return new(name, CheckState.Information, detail + (historicalFindings > 0
            ? "The current protected-file result and prerequisites are clear. Retained log findings remain historical context and do not establish current corruption."
            : "No protected-file issue was established within the checked scope.") + boundary);
    }
}
