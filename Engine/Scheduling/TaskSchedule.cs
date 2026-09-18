namespace KillerBench.Engine;

public enum TaskScheduleKind { Daily, Weekly, Monthly, Startup }
public enum TaskLocalTimeState { Unique, Ambiguous, Invalid }
public sealed record TaskLocalTime(DateTime LocalTime, string TimeZoneId, TaskLocalTimeState State, IReadOnlyList<DateTimeOffset> Candidates);

public sealed record TaskScheduleOptions(TaskScheduleKind Kind, TimeOnly? Time = null,
    IReadOnlyList<DayOfWeek>? Weekdays = null, IReadOnlyList<int>? MonthDays = null, bool LastDay = false);

public static class TaskSchedule
{
    public static TaskLocalTime ResolveLocalTime(DateOnly date, TimeOnly time, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) return new(local, zone.Id, TaskLocalTimeState.Invalid, Array.Empty<DateTimeOffset>());
        var offsets = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local) : [zone.GetUtcOffset(local)];
        var candidates = offsets.Select(offset => new DateTimeOffset(local, offset)).OrderBy(value => value.UtcDateTime).ToArray();
        return new(local, zone.Id, candidates.Length > 1 ? TaskLocalTimeState.Ambiguous : TaskLocalTimeState.Unique, Array.AsReadOnly(candidates));
    }

    public static IReadOnlyList<DateOnly> PreviewDates(TaskScheduleOptions options, DateOnly start, int count = 5)
    {
        var schedule = Validate(options);
        if (count is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(count), "Preview between 1 and 100 dates.");
        if (schedule.Kind == TaskScheduleKind.Startup) return Array.Empty<DateOnly>();
        var dates = new List<DateOnly>();
        var date = start;
        for (int inspected = 0; inspected < 3660 && dates.Count < count; inspected++)
        {
            if (MatchesDate(schedule, date)) dates.Add(date);
            if (date == DateOnly.MaxValue) break;
            date = date.AddDays(1);
        }
        return dates.AsReadOnly();
    }

    public static bool MatchesDate(TaskScheduleOptions options, DateOnly date)
    {
        var schedule = Validate(options);
        return schedule.Kind switch
        {
            TaskScheduleKind.Daily => true,
            TaskScheduleKind.Weekly => schedule.Weekdays!.Contains(date.DayOfWeek),
            TaskScheduleKind.Monthly => schedule.LastDay
                ? date.Day == DateTime.DaysInMonth(date.Year, date.Month)
                : schedule.MonthDays!.Contains(date.Day),
            TaskScheduleKind.Startup => false,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
    }

    public static TaskScheduleOptions Validate(TaskScheduleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.Kind)) throw new ArgumentException("Choose a supported schedule.", nameof(options));
        var weekdays = options.Weekdays?.ToArray() ?? [];
        var days = options.MonthDays?.ToArray() ?? [];
        if (weekdays.Length > 7 || weekdays.Any(day => !Enum.IsDefined(day)) || weekdays.Distinct().Count() != weekdays.Length)
            throw new ArgumentException("Choose distinct weekdays.", nameof(options));
        if (days.Length > 31 || days.Any(day => day is < 1 or > 31) || days.Distinct().Count() != days.Length)
            throw new ArgumentException("Choose distinct month days from 1 to 31.", nameof(options));
        if (options.Kind == TaskScheduleKind.Startup ? options.Time is not null : options.Time is null)
            throw new ArgumentException("Calendar schedules require a time; startup schedules do not use one.", nameof(options));
        if (options.Kind == TaskScheduleKind.Weekly ? weekdays.Length == 0 : weekdays.Length != 0)
            throw new ArgumentException("Weekdays apply only to a weekly schedule and at least one is required.", nameof(options));
        if (options.Kind == TaskScheduleKind.Monthly)
        {
            if (options.LastDay == (days.Length > 0))
                throw new ArgumentException("Choose month days or the last day of the month.", nameof(options));
        }
        else if (days.Length != 0 || options.LastDay)
            throw new ArgumentException("Month days apply only to a monthly schedule.", nameof(options));
        return options with { Weekdays = Array.AsReadOnly(weekdays.Order().ToArray()), MonthDays = Array.AsReadOnly(days.Order().ToArray()) };
    }
}
