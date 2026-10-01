namespace KillerBench.Engine.Debloat;

public static class DebloatPowerPlanEligibility
{
    public static WorkflowCheck Assess(DebloatOptions options, bool pendingRecovery, bool? managedPolicy,
        DebloatPowerPlanEvidence? evidence, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Areas.HasFlag(DebloatAreas.HighPerformancePower))
            return new("High performance power plan", CheckState.Information,
                "Select HighPerformancePower as the only declutter area to evaluate the guarded plan change. No plan change was selected.");
        options.Validate();
        if (options.Areas != DebloatAreas.HighPerformancePower)
            return new("High performance power plan", CheckState.Attention,
                "Select HighPerformancePower as the only declutter area before changing the active plan.");
        if (pendingRecovery)
            return new("High performance power plan", CheckState.Attention,
                "A previous power-plan change requires reconciliation. Recheck the active plan without repeating activation before another attempt.");
        if (managedPolicy == true)
            return new("High performance power plan", CheckState.Attention,
                "Managed power policy is present. KillerBench will not override it.");
        if (managedPolicy is null)
            return new("High performance power plan", CheckState.Unknown,
                "Managed power-policy ownership is unavailable. No plan change is eligible.");
        if (!Complete(evidence))
            return new("High performance power plan", CheckState.Unknown,
                "Complete power-plan inventory is required. No plan change is eligible.");
        if (evidence!.HighPerformance!.Active)
            return new("High performance power plan", CheckState.Clear,
                "The exact standard High performance plan is already active. No change is needed.");
        if (!isAdministrator)
            return new("High performance power plan", CheckState.Attention,
                "Administrator access is required to activate the High performance plan.");
        return new("High performance power plan", CheckState.Repairable,
            "Fresh unmanaged evidence confirms the exact standard High performance plan is registered but inactive. Guarded activation is eligible and still requires separate confirmation.",
            RepairKind.HighPerformancePowerPlan);
    }

    public static async Task<WorkflowCheck> CheckAsync(DebloatOptions options, string folder,
        IDebloatPowerPlanChangeHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        if (!options.Areas.HasFlag(DebloatAreas.HighPerformancePower))
            return Assess(options, false, false, null, host.IsAdministrator);
        options.Validate();
        bool pendingRecovery;
        try { pendingRecovery = File.Exists(Path.Combine(Path.GetFullPath(folder), "debloat-power-plan.pending.json")); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or System.Security.SecurityException)
        {
            return new("High performance power plan", CheckState.Unknown,
                "Pending power-plan state could not be inspected. No plan change is eligible.");
        }
        if (options.Areas != DebloatAreas.HighPerformancePower || pendingRecovery)
            return Assess(options, pendingRecovery, false, null, host.IsAdministrator);
        try
        {
            bool? managed = await (host.HasManagedPowerPolicyAsync(token) ??
                throw new InvalidOperationException("Managed power-policy provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
            if (managed != false) return Assess(options, false, managed, null, host.IsAdministrator);
            var evidence = await (host.ReadAsync(token) ??
                throw new InvalidOperationException("Power-plan evidence provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
            return Assess(options, false, managed, evidence, host.IsAdministrator);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new("High performance power plan", CheckState.Unknown,
                "Power-plan eligibility evidence could not be collected. No plan change is eligible.");
        }
    }

    private static bool Complete(DebloatPowerPlanEvidence? evidence) => evidence is
    {
        Complete: true,
        Evidence: { ExitCode: 0, TimedOut: false, Truncated: false },
        HighPerformance: not null
    } && string.IsNullOrWhiteSpace(evidence.Evidence.Error) &&
        evidence.HighPerformance.Id.Equals(WindowsDebloatPowerPlanReader.HighPerformanceId, StringComparison.OrdinalIgnoreCase) &&
        evidence.Plans.Count(plan => plan.Active) == 1;
}
