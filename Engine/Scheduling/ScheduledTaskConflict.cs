namespace KillerBench.Engine;

public enum ScheduledTaskConflictState { ExistingTask, NotObserved, Unknown }
public sealed record ScheduledTaskConflictResult(ScheduledTaskConflictState State, string Detail);

public static class ScheduledTaskConflict
{
    public static ScheduledTaskConflictResult Review(ScheduledTaskIdentity target, ScheduledTaskInventoryResult inventory)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(inventory);
        if (inventory.Tasks.Any(task => task is not null && target.Matches(task.Identity)))
            return new(ScheduledTaskConflictState.ExistingTask,
                "A task already exists at " + target.FullName + ". Creating this draft must not overwrite it. Replacement requires a separate reviewed operation and recovery copy.");
        if (!inventory.Complete || inventory.Evidence.ExitCode != 0 || inventory.Evidence.TimedOut || inventory.Evidence.Truncated ||
            !string.IsNullOrWhiteSpace(inventory.Evidence.Error))
            return new(ScheduledTaskConflictState.Unknown,
                "Task inventory is incomplete. The target's availability is unknown; do not register from this result.");
        return new(ScheduledTaskConflictState.NotObserved,
            "No matching task was visible in this inventory. This is not proof of absence or registration eligibility; permissions and a fresh exact-target check are still required.");
    }
}
