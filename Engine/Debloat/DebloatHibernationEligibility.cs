namespace KillerBench.Engine;

public static class DebloatHibernationEligibility
{
    public static WorkflowCheck Assess(DebloatOptions options, bool pendingRecovery, bool? managedPolicy,
        DebloatPowerEvidence? evidence, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Areas.HasFlag(DebloatAreas.Hibernation))
            return new("Hibernation disable", CheckState.Information,
                "Select Hibernation to evaluate the guarded disable operation. No hibernation change was selected.");
        options.Validate();
        if (options.Areas != DebloatAreas.Hibernation)
            return new("Hibernation disable", CheckState.Attention,
                "Select Hibernation as the only declutter area before evaluating this power change.");
        if (pendingRecovery)
            return new("Hibernation disable", CheckState.Attention,
                "A previous hibernation change requires reconciliation. Recheck current state without repeating the command before another attempt.");
        if (managedPolicy == true)
            return new("Hibernation disable", CheckState.Attention,
                "Managed power policy is present. KillerBench will not override it.");
        if (managedPolicy is null)
            return new("Hibernation disable", CheckState.Unknown,
                "Managed power-policy ownership is unavailable. No hibernation change is eligible.");
        if (!Complete(evidence))
            return new("Hibernation disable", CheckState.Unknown,
                "Complete active-plan, timer, hibernation and hibernation-file evidence is required. No change is eligible.");
        if (evidence!.HibernationEnabled == false && evidence.HibernationFilePresent == false)
            return new("Hibernation disable", CheckState.Clear,
                "Hibernation is already disabled and the hibernation file is absent. No change is needed.");
        if (evidence.HibernationEnabled != true || evidence.HibernationFilePresent != true)
            return new("Hibernation disable", CheckState.Attention,
                "Hibernation state and hibernation-file evidence are contradictory. No change is eligible.");
        if (!isAdministrator)
            return new("Hibernation disable", CheckState.Attention,
                "Administrator access is required to disable hibernation.");
        return new("Hibernation disable", CheckState.Repairable,
            "Fresh unmanaged evidence confirms hibernation is enabled and hiberfil.sys is present. The guarded operation is eligible and still requires separate confirmation.",
            RepairKind.HibernationDisable);
    }

    public static async Task<WorkflowCheck> CheckAsync(DebloatOptions options, string folder,
        IDebloatHibernationHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        if (!options.Areas.HasFlag(DebloatAreas.Hibernation))
            return Assess(options, false, false, null, host.IsAdministrator);
        options.Validate();
        bool pendingRecovery;
        try { pendingRecovery = File.Exists(Path.Combine(Path.GetFullPath(folder), "debloat-hibernation.pending.json")); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or System.Security.SecurityException)
        {
            return new("Hibernation disable", CheckState.Unknown,
                "Pending hibernation state could not be inspected. No change is eligible.");
        }
        if (options.Areas != DebloatAreas.Hibernation || pendingRecovery)
            return Assess(options, pendingRecovery, false, null, host.IsAdministrator);
        try
        {
            bool? managed = await (host.HasManagedPowerPolicyAsync(token) ??
                throw new InvalidOperationException("Managed power-policy provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
            if (managed != false) return Assess(options, false, managed, null, host.IsAdministrator);
            var evidence = await (host.ReadAsync(token) ??
                throw new InvalidOperationException("Hibernation evidence provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
            return Assess(options, false, managed, evidence, host.IsAdministrator);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new("Hibernation disable", CheckState.Unknown,
                "Hibernation eligibility evidence could not be collected. No change is eligible.");
        }
    }

    private static bool Complete(DebloatPowerEvidence? value) => value is not null && value.Complete &&
        Guid.TryParseExact(value.ActiveSchemeId, "D", out _) && value.SleepAcSeconds.HasValue &&
        value.SleepDcSeconds.HasValue && value.DisplayAcSeconds.HasValue && value.DisplayDcSeconds.HasValue &&
        value.HibernationEnabled.HasValue && value.HibernationFilePresent.HasValue;
}
