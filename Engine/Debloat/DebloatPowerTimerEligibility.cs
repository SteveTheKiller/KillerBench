namespace KillerBench.Engine;

public static class DebloatPowerTimerEligibility
{
    public static WorkflowCheck Assess(DebloatOptions options, bool pendingRecovery, bool? managedPolicy,
        DebloatPowerEvidence? evidence, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Areas.HasFlag(DebloatAreas.SleepAndDisplayTimers))
            return new("Power timers", CheckState.Information,
                "Select SleepAndDisplayTimers and all four timeout values to evaluate the guarded power change. No timer change was selected.");
        options.Validate();
        if (options.Areas != DebloatAreas.SleepAndDisplayTimers || options.PowerRequest is null)
            return new("Power timers", CheckState.Attention,
                "Select SleepAndDisplayTimers as the only DEBLOAT area and provide all four timeout values.");
        if (pendingRecovery)
            return new("Power timers", CheckState.Attention,
                "A previous power-timer change requires reconciliation. Recheck all four timers without repeating commands before another attempt.");
        if (managedPolicy == true)
            return new("Power timers", CheckState.Attention,
                "Managed power policy is present. KillerBench will not override it.");
        if (managedPolicy is null)
            return new("Power timers", CheckState.Unknown,
                "Managed power-policy ownership is unavailable. No timer change is eligible.");
        if (!Complete(evidence))
            return new("Power timers", CheckState.Unknown,
                "Complete active-plan, timer and hibernation evidence is required. No timer change is eligible.");
        if (Matches(evidence!, options.PowerRequest))
            return new("Power timers", CheckState.Clear,
                "All four power timers already match the requested values on the current active plan. No change is needed.");
        if (!isAdministrator)
            return new("Power timers", CheckState.Attention,
                "Administrator access is required to change power timers.");
        return new("Power timers", CheckState.Repairable,
            "Fresh unmanaged evidence confirms one or more selected timers differ. The guarded four-value operation is eligible and still requires separate confirmation.",
            RepairKind.PowerTimers);
    }

    public static async Task<WorkflowCheck> CheckAsync(DebloatOptions options, string folder,
        IDebloatPowerTimerHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        if (!options.Areas.HasFlag(DebloatAreas.SleepAndDisplayTimers))
            return Assess(options, false, false, null, host.IsAdministrator);
        options.Validate();
        bool pendingRecovery;
        try { pendingRecovery = File.Exists(Path.Combine(Path.GetFullPath(folder), "debloat-power-timers.pending.json")); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or System.Security.SecurityException)
        {
            return new("Power timers", CheckState.Unknown,
                "Pending power-timer state could not be inspected. No change is eligible.");
        }
        if (options.Areas != DebloatAreas.SleepAndDisplayTimers || options.PowerRequest is null || pendingRecovery)
            return Assess(options, pendingRecovery, false, null, host.IsAdministrator);
        try
        {
            bool? managed = await (host.HasManagedPowerPolicyAsync(token) ??
                throw new InvalidOperationException("Managed power-policy provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
            if (managed != false) return Assess(options, false, managed, null, host.IsAdministrator);
            var evidence = await (host.ReadAsync(token) ??
                throw new InvalidOperationException("Power-timer evidence provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
            return Assess(options, false, managed, evidence, host.IsAdministrator);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new("Power timers", CheckState.Unknown,
                "Power-timer eligibility evidence could not be collected. No change is eligible.");
        }
    }

    private static bool Complete(DebloatPowerEvidence? value) => value is not null && value.Complete &&
        Guid.TryParseExact(value.ActiveSchemeId, "D", out _) && value.SleepAcSeconds.HasValue &&
        value.SleepDcSeconds.HasValue && value.DisplayAcSeconds.HasValue && value.DisplayDcSeconds.HasValue &&
        value.HibernationEnabled.HasValue && value.HibernationFilePresent.HasValue;

    private static bool Matches(DebloatPowerEvidence value, DebloatPowerRequest request) =>
        value.SleepAcSeconds == checked(request.SleepAcMinutes * 60) &&
        value.SleepDcSeconds == checked(request.SleepDcMinutes * 60) &&
        value.DisplayAcSeconds == checked(request.DisplayAcMinutes * 60) &&
        value.DisplayDcSeconds == checked(request.DisplayDcMinutes * 60);
}
