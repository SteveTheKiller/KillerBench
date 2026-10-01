using System.Globalization;
using System.Text.Json;
using KillerBench.Engine;

internal static class ScheduleCommands
{
    internal static int Run(string[] args)
    {
        try
        {
            if (args.Length is < 5 or > 6 || !DateOnly.TryParseExact(args[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !TimeOnly.TryParseExact(args[3], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                throw new ArgumentException("Usage: schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]");
            TaskScheduleOptions options = args[1] switch
            {
                "daily" when args.Length == 5 => new(TaskScheduleKind.Daily, time),
                "weekly" when args.Length == 6 => new(TaskScheduleKind.Weekly, time, [.. args[5].Split(',').Select(ParseDay)]),
                "monthly" when args.Length == 6 && args[5] == "last" => new(TaskScheduleKind.Monthly, time, LastDay: true),
                "monthly" when args.Length == 6 => new(TaskScheduleKind.Monthly, time, MonthDays: [.. args[5].Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture))]),
                _ => throw new ArgumentException("Choose daily, weekly with weekdays, or monthly with month days or last.")
            };
            var zone = TimeZoneInfo.FindSystemTimeZoneById(args[4]);
            var dates = TaskSchedule.PreviewDates(options, start);
            var preview = dates.Select(date => TaskSchedule.ResolveLocalTime(date, time, zone)).ToArray();
            Console.WriteLine(JsonSerializer.Serialize(new { previewOnly = true, taskRegistered = false, dates = preview }, Capture.JsonOptions));
            return preview.Any(value => value.State != TaskLocalTimeState.Unique) ? 3 : 0;
        }
        catch (Exception e) when (e is ArgumentException or FormatException or OverflowException or TimeZoneNotFoundException or InvalidTimeZoneException)
        { Console.Error.WriteLine(e.Message); return 2; }
    }

    private static DayOfWeek ParseDay(string value) => value switch
    {
        "Sun" => DayOfWeek.Sunday, "Mon" => DayOfWeek.Monday, "Tue" => DayOfWeek.Tuesday,
        "Wed" => DayOfWeek.Wednesday, "Thu" => DayOfWeek.Thursday, "Fri" => DayOfWeek.Friday,
        "Sat" => DayOfWeek.Saturday, _ => throw new ArgumentException("Use weekday names Sun,Mon,Tue,Wed,Thu,Fri,Sat.")
    };
}
