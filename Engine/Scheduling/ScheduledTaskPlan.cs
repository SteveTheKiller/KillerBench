namespace KillerBench.Engine;

public enum ScheduledTaskAccountKind { InteractiveUser, LocalSystem }
public enum ScheduledTaskOverlap { SkipNew, Queue, Parallel, StopExisting }

public sealed record ScheduledTaskPrincipal(ScheduledTaskAccountKind Kind, string UserId, bool HighestPrivileges)
{
    public ScheduledTaskPrincipal Validate()
    {
        if (!Enum.IsDefined(Kind) || string.IsNullOrWhiteSpace(UserId) || UserId.Length > 512 ||
            UserId != UserId.Trim() || UserId.Any(char.IsControl))
            throw new ArgumentException("Choose an explicit task account.");
        bool system = string.Equals(UserId, "S-1-5-18", StringComparison.OrdinalIgnoreCase);
        if (Kind == ScheduledTaskAccountKind.LocalSystem && (!system || !HighestPrivileges))
            throw new ArgumentException("Local System requires its exact SID and an explicit highest-privilege selection.");
        if (Kind == ScheduledTaskAccountKind.InteractiveUser &&
            (system || UserId.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) || UserId.Equals("NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Choose Local System explicitly for a System task.");
        return this;
    }
}

/// <summary>A review draft only. Validation does not establish registration eligibility or executable trust.</summary>
public sealed record ScheduledTaskPlan(ScheduledTaskIdentity Identity, ScheduledTaskAction Action,
    TaskScheduleOptions Schedule, ScheduledTaskPrincipal Principal, TimeSpan ExecutionLimit)
{
    public string? TimeZoneId { get; init; }
    public ScheduledTaskOverlap Overlap { get; init; } = ScheduledTaskOverlap.SkipNew;

    public ScheduledTaskPlan Validate()
    {
        ArgumentNullException.ThrowIfNull(Identity);
        ArgumentNullException.ThrowIfNull(Action);
        ArgumentNullException.ThrowIfNull(Schedule);
        ArgumentNullException.ThrowIfNull(Principal);
        if (!Enum.IsDefined(Overlap)) throw new ArgumentException("Choose a supported duplicate-start policy.");
        if (Schedule.Kind == TaskScheduleKind.Startup)
        {
            if (TimeZoneId is not null) throw new ArgumentException("Startup schedules do not use a time zone.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(TimeZoneId) || TimeZoneId.Length > 256 || TimeZoneId != TimeZoneId.Trim())
                throw new ArgumentException("Choose an explicit calendar time zone.");
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
            catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException)
            { throw new ArgumentException("The calendar time zone is unavailable on this machine.", nameof(TimeZoneId), error); }
        }
        if (ExecutionLimit < TimeSpan.FromMinutes(1) || ExecutionLimit > TimeSpan.FromHours(72))
            throw new ArgumentOutOfRangeException(nameof(ExecutionLimit), "Choose an execution limit from one minute through 72 hours.");
        return this with { Action = Action.Validate(), Schedule = TaskSchedule.Validate(Schedule), Principal = Principal.Validate() };
    }
}
