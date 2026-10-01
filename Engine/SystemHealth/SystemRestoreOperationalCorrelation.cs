namespace KillerBench.Engine.SystemHealth;

public static class SystemRestoreOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck inventory, WorkflowCheck creation, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(creation);
        token.ThrowIfCancellationRequested();
        const string name = "System Restore readiness conclusion";
        const string boundary = " This conclusion covers the bounded restore-point inventory and creation eligibility only. It does not prove protection coverage for every volume, restore-point contents, rollback safety or future restore success. No restore point was created, deleted or applied, no protection setting changed, and this conclusion selects no repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (inventory.Name != "System Restore points" || creation.Name != "System Restore point creation")
            return Unknown("System Restore inputs were missing or carried an unexpected identity.");
        if (inventory.EffectiveState == CheckState.Unknown || creation.EffectiveState == CheckState.Unknown)
            return Unknown("System Restore inventory or creation eligibility was incomplete. Review the individual result before relying on recovery readiness.");
        if (creation.EffectiveState == CheckState.Repairable && creation.Repair == RepairKind.SystemRestorePoint)
            return new(name, CheckState.Information,
                "The restore-point inventory is available and this Windows client is eligible for one explicitly requested restore point. Creation still requires a description, reason and separate confirmation." + boundary);
        if (creation.EffectiveState == CheckState.Attention)
            return new(name, CheckState.Attention,
                "Restore-point creation is currently unavailable or blocked by the reported eligibility condition. Existing inventory remains historical recovery context." + boundary);
        return new(name, CheckState.Information,
            "System Restore inventory and creation status were reviewed. No creation action is currently offered by this result." + boundary);
    }
}
