namespace KillerBench.Engine;

public static class WindowsRecoveryOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck status, WorkflowCheck partition, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(partition);
        token.ThrowIfCancellationRequested();
        const string name = "Windows Recovery readiness conclusion";
        const string boundary = " Enabled status and partition presence do not prove recovery-image integrity, BCD correctness, bootability or repair success. No recovery boot or restart was initiated, no image was downloaded, relocated or replaced, and no partition, boot or recovery setting changed. This conclusion selects no repair.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (status.Name != "Windows Recovery Environment" || partition.Name != "Windows Recovery partition correlation")
            return Unknown("Windows Recovery inputs were missing or carried an unexpected identity.");
        if (status.EffectiveState == CheckState.Unknown || partition.EffectiveState == CheckState.Unknown)
            return Unknown("Windows Recovery status or configured partition coverage was incomplete.");
        if (status.EffectiveState == CheckState.Repairable)
            return new(name, CheckState.Attention,
                "Windows Recovery is disabled. The existing configuration can be considered for guarded enablement after fresh eligibility checks." + boundary);
        if (status.EffectiveState != CheckState.Clear)
            return Unknown("Windows Recovery status had an unexpected state.");
        if (partition.EffectiveState == CheckState.Attention)
            return new(name, CheckState.Attention,
                "Windows Recovery is enabled, but its configured disk and partition were not confirmed in the complete current inventory." + boundary);
        if (partition.EffectiveState != CheckState.Information)
            return Unknown("Windows Recovery partition correlation had an unexpected state.");
        return new(name, CheckState.Information,
            "Windows Recovery is enabled and its exact configured disk and partition are present in the current inventory." + boundary);
    }
}
