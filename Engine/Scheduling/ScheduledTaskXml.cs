using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Scheduling;

/// <summary>Creates a disabled review definition. Does not register or execute tasks.</summary>
public static class ScheduledTaskXml
{
    public const string Namespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";
    public const int MaximumCharacters = 1024 * 1024;

    public static string Serialize(ScheduledTaskPlan draft, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var plan = draft.Validate();
        XNamespace ns = Namespace;
        XElement E(string name, object? content = null) => new(ns + name, content);
        XElement trigger;
        if (plan.Schedule.Kind == TaskScheduleKind.Startup) trigger = E("BootTrigger", E("Enabled", true));
        else
        {
            // Offset-free calendar boundaries follow the scheduler machine's local wall clock.
            if (!string.Equals(plan.TimeZoneId, TimeZoneInfo.Local.Id, StringComparison.Ordinal))
                throw new NotSupportedException("XML calendar export requires this machine's local time zone. Other zones cannot be represented without changing recurring wall-clock semantics.");
            var local = TaskSchedule.ResolveLocalTime(startDate, plan.Schedule.Time!.Value, TimeZoneInfo.Local);
            if (local.State != TaskLocalTimeState.Unique)
                throw new ArgumentException("Choose a start boundary outside a daylight-saving gap or repeated hour.", nameof(startDate));
            trigger = E("CalendarTrigger", new object[]
            {
                E("StartBoundary", local.LocalTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture)),
                E("Enabled", true)
            });
            trigger.Add(plan.Schedule.Kind switch
            {
                TaskScheduleKind.Daily => E("ScheduleByDay", E("DaysInterval", 1)),
                TaskScheduleKind.Weekly => E("ScheduleByWeek", new object[] { E("WeeksInterval", 1),
                    E("DaysOfWeek", plan.Schedule.Weekdays!.Select(day => E(day.ToString()))) }),
                TaskScheduleKind.Monthly => E("ScheduleByMonth", new object[] {
                    E("DaysOfMonth", plan.Schedule.LastDay ? [E("Day", "Last")] :
                        plan.Schedule.MonthDays!.Select(day => E("Day", day))),
                    E("Months", CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12).Select(month => E(month))) }),
                _ => throw new ArgumentOutOfRangeException(nameof(draft))
            });
        }
        var principal = E("Principal");
        principal.SetAttributeValue("id", "Operator");
        principal.Add(E("UserId", plan.Principal.UserId));
        if (plan.Principal.Kind == ScheduledTaskAccountKind.InteractiveUser)
            principal.Add(E("LogonType", "InteractiveToken"));
        principal.Add(E("RunLevel", plan.Principal.HighestPrivileges ? "HighestAvailable" : "LeastPrivilege"));
        var action = E("Exec", new object[] { E("Command", plan.Action.Executable), E("Arguments", plan.Action.Arguments) });
        if (plan.Action.WorkingDirectory is not null) action.Add(E("WorkingDirectory", plan.Action.WorkingDirectory));
        var actions = E("Actions", action);
        actions.SetAttributeValue("Context", "Operator");
        var task = E("Task", new object[] {
            E("RegistrationInfo", new object[] { E("Description", "Disabled review definition. Verify account, executable, schedule and permissions before enabling."), E("URI", plan.Identity.FullName) }),
            E("Triggers", trigger), E("Principals", principal),
            E("Settings", new object[] {
                E("MultipleInstancesPolicy", plan.Overlap == ScheduledTaskOverlap.SkipNew ? "IgnoreNew" : plan.Overlap.ToString()),
                E("DisallowStartIfOnBatteries", true), E("StopIfGoingOnBatteries", true),
                E("StartWhenAvailable", false), E("AllowStartOnDemand", false), E("Enabled", false),
                E("ExecutionTimeLimit", XmlConvert.ToString(plan.ExecutionLimit)) }), actions });
        task.SetAttributeValue("version", "1.2");
        return task.ToString();
    }

    public static bool MatchesRequested(string requestedXml, string observedXml)
    {
        if (string.IsNullOrWhiteSpace(requestedXml) || string.IsNullOrWhiteSpace(observedXml) ||
            requestedXml.Length > MaximumCharacters || observedXml.Length > MaximumCharacters) return false;
        try
        {
            var requested = Read(requestedXml);
            var observed = Read(observedXml);
            return requested.Root is not null && observed.Root is not null &&
                requested.Root.Name == XName.Get("Task", Namespace) && observed.Root.Name == requested.Root.Name &&
                MatchesElement(requested.Root, observed.Root);
        }
        catch (Exception error) when (error is XmlException or FormatException or ArgumentException) { return false; }
    }

    private static XDocument Read(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumCharacters,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static bool MatchesElement(XElement requested, XElement observed)
    {
        if (requested.Name != observed.Name) return false;
        if (observed.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration && requested.Attribute(attribute.Name) is null)) return false;
        foreach (var attribute in requested.Attributes())
            if (!string.Equals(attribute.Value, observed.Attribute(attribute.Name)?.Value, StringComparison.Ordinal)) return false;
        var expectedChildren = requested.Elements().ToArray();
        var actualChildren = observed.Elements().ToArray();
        foreach (var extra in actualChildren.Where(actual => !expectedChildren.Any(expected => expected.Name == actual.Name)))
            if (!IsAddedDefault(requested, extra) || actualChildren.Count(actual => actual.Name == extra.Name) != 1) return false;
        foreach (var group in expectedChildren.GroupBy(element => element.Name))
        {
            var expected = group.ToArray();
            var actual = actualChildren.Where(element => element.Name == group.Key).ToArray();
            if (actual.Length == 0 && expected.Length == 1 && IsOmittedDefault(requested, expected[0])) continue;
            if (actual.Length != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (!MatchesElement(expected[i], actual[i])) return false;
        }
        if (expectedChildren.Length != 0) return true;
        if (requested.Name.LocalName == "StartBoundary")
        {
            var expectedTime = XmlConvert.ToDateTime(requested.Value, XmlDateTimeSerializationMode.RoundtripKind);
            var actualTime = XmlConvert.ToDateTime(observed.Value, XmlDateTimeSerializationMode.RoundtripKind);
            // Floating local schedules follow Windows time-zone rules; fixed offsets do not.
            if (expectedTime.Kind == DateTimeKind.Unspecified || actualTime.Kind == DateTimeKind.Unspecified)
                return expectedTime.Kind == actualTime.Kind && expectedTime == actualTime;
            return XmlConvert.ToDateTimeOffset(requested.Value).EqualsExact(XmlConvert.ToDateTimeOffset(observed.Value));
        }
        return string.Equals(requested.Value, observed.Value, StringComparison.Ordinal);
    }

    private static bool IsOmittedDefault(XElement parent, XElement child)
    {
        // Only these emitted schema defaults may disappear. Disabled task Enabled remains required.
        XNamespace ns = Namespace;
        if (child.HasAttributes || child.HasElements) return false;
        if (parent.Name == ns + "Exec" && parent.Parent?.Name == ns + "Actions" && parent.Parent?.Parent?.Name == ns + "Task")
            return child.Name == ns + "Arguments" && child.Value.Length == 0;
        if (parent.Name == ns + "Settings" && parent.Parent?.Name == ns + "Task")
            return child.Name == ns + "StartWhenAvailable" && child.Value == "false";
        return (parent.Name == ns + "BootTrigger" || parent.Name == ns + "CalendarTrigger") &&
            parent.Parent?.Name == ns + "Triggers" && parent.Parent?.Parent?.Name == ns + "Task" &&
            child.Name == ns + "Enabled" && child.Value == "true";
    }

    private static bool IsAddedDefault(XElement parent, XElement child)
    {
        XNamespace ns = Namespace;
        if (parent.Name != ns + "Settings" || parent.Parent?.Name != ns + "Task" || child.HasAttributes) return false;
        // Windows materializes these schema defaults during a disabled definition round-trip.
        // No additional triggers, actions, principal fields or nondefault settings are accepted.
        if (child.Name == ns + "IdleSettings")
            return child.Elements().Count() == 2 && child.Nodes().All(node => node is XElement || node is XText text && string.IsNullOrWhiteSpace(text.Value)) &&
                child.Elements().All(element => !element.HasAttributes && !element.HasElements) &&
                child.Elements(ns + "StopOnIdleEnd").Count() == 1 && child.Element(ns + "StopOnIdleEnd")!.Value == "true" &&
                child.Elements(ns + "RestartOnIdle").Count() == 1 && child.Element(ns + "RestartOnIdle")!.Value == "false";
        if (child.HasElements) return false;
        return child.Name.LocalName switch
        {
            "AllowHardTerminate" => child.Name.Namespace == ns && child.Value == "true",
            "RunOnlyIfNetworkAvailable" or "Hidden" or "RunOnlyIfIdle" or "WakeToRun" => child.Name.Namespace == ns && child.Value == "false",
            "Priority" => child.Name.Namespace == ns && child.Value == "7",
            _ => false
        };
    }
}
