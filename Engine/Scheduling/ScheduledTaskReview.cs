using System.Globalization;

namespace KillerBench.Engine.Scheduling;

public static class ScheduledTaskReview
{
    public static string Format(ScheduledTaskPlan draft, DateTimeOffset? asOf = null)
    {
        var plan = draft.Validate();
        var schedule = plan.Schedule;
        string clock = schedule.Time?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
        string trigger = schedule.Kind switch
        {
            TaskScheduleKind.Startup => "At machine startup (no clock time)",
            TaskScheduleKind.Daily => "Every day at " + clock,
            TaskScheduleKind.Weekly => string.Join(", ", schedule.Weekdays!) + " at " + clock,
            TaskScheduleKind.Monthly when schedule.LastDay => "Last day of each month at " + clock,
            TaskScheduleKind.Monthly => "Month days " + string.Join(", ", schedule.MonthDays!) + " at " + clock + "; months without a selected day are skipped",
            _ => throw new ArgumentException("Unsupported schedule.")
        };
        string text = string.Join('\n', new[] {
            "TASK DRAFT / REVIEW ONLY", "No task was created or run.", "",
            "Task: " + plan.Identity.FullName,
            "Executable: " + plan.Action.Executable,
            "Arguments: " + (plan.Action.Arguments.Length == 0 ? "(none)" : plan.Action.Arguments),
            "Working directory: " + (plan.Action.WorkingDirectory ?? "Not specified"), "",
            "Account: " + plan.Principal.UserId,
            "Account mode: " + (plan.Principal.Kind == ScheduledTaskAccountKind.LocalSystem ? "Local System" : "Interactive user; requires that user's logged-on session"),
            "Highest privileges: " + (plan.Principal.HighestPrivileges ? "Selected" : "Not selected"),
            "Schedule: " + trigger,
            "Time zone: " + (plan.TimeZoneId ?? "Not used for startup"),
            "Execution limit: " + plan.ExecutionLimit.ToString("c", CultureInfo.InvariantCulture), "",
            "If already running: " + (plan.Overlap switch {
                ScheduledTaskOverlap.SkipNew => "Skip the new run",
                ScheduledTaskOverlap.Queue => "Queue the new run until the previous run finishes",
                ScheduledTaskOverlap.Parallel => "Run both at once; concurrent access may conflict",
                ScheduledTaskOverlap.StopExisting => "Stop the previous run; unfinished work may be interrupted",
                _ => throw new ArgumentException("Unsupported duplicate-start policy.") }),
            "Before registration: verify the executable and account, bind the schedule's time zone, and check for an existing task at this exact path.",
            "Registration eligibility and successful execution are not verified."
        });
        if (asOf is null || schedule.Kind == TaskScheduleKind.Startup) return text;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(plan.TimeZoneId!);
        var start = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(asOf.Value, zone).DateTime);
        var previews = TaskSchedule.PreviewDates(schedule, start).Select(date => TaskSchedule.ResolveLocalTime(date, schedule.Time!.Value, zone));
        var lines = previews.Select(item => item.LocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " / " +
            (item.State == TaskLocalTimeState.Invalid ? "INVALID CLOCK TIME: no corresponding instant" :
            item.State == TaskLocalTimeState.Ambiguous ? "REPEATED CLOCK TIME: " + string.Join(" or ", item.Candidates.Select(FormatInstant)) :
            FormatInstant(item.Candidates.Single())));
        return text + "\n\nCALENDAR PREVIEW FROM " + start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "\n" +
            string.Join('\n', lines) + "\nDate preview only; today's listed time may already have passed. These are not registered next-run times. Repeated or invalid times require a scheduling policy.";
    }

    private static string FormatInstant(DateTimeOffset value) => value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
}
