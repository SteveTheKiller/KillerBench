using KillerBench.Engine;
using System.Xml.Linq;

internal static class TaskXmlChecks
{
    internal static void Run()
    {
        var draft = new ScheduledTaskPlan(new("\\KillerBench\\", "Review & inspect"),
            new("C:\\Tools\\check.exe", "--name \"A&B\"", "C:\\Tools"),
            new(TaskScheduleKind.Startup), new(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", true), TimeSpan.FromMinutes(15));
        XNamespace ns = ScheduledTaskXml.Namespace;
        foreach (var schedule in new[] { new TaskScheduleOptions(TaskScheduleKind.Startup), new(TaskScheduleKind.Daily, new(9, 30)),
            new(TaskScheduleKind.Weekly, new(9, 30), [DayOfWeek.Friday, DayOfWeek.Monday]),
            new(TaskScheduleKind.Monthly, new(9, 30), MonthDays: [1, 15]), new(TaskScheduleKind.Monthly, new(9, 30), LastDay: true) })
        foreach (var overlap in Enum.GetValues<ScheduledTaskOverlap>())
        {
            var plan = draft with { Schedule = schedule, Overlap = overlap,
                TimeZoneId = schedule.Kind == TaskScheduleKind.Startup ? null : TimeZoneInfo.Local.Id };
            string xml = ScheduledTaskXml.Serialize(plan, new(2026, 9, 14));
            var root = XElement.Parse(xml);
            ValidateNativeDefinition(xml);
            Require(root.Name == ns + "Task" && root.Element(ns + "Settings")!.Element(ns + "Enabled")!.Value == "false", "Review definition remains disabled");
            Require(root.Descendants(ns + "Arguments").Single().Value == draft.Action.Arguments &&
                root.Descendants(ns + "URI").Single().Value == draft.Identity.FullName, "XML escapes literal action and identity values");
            Require(root.Descendants(ns + "ExecutionTimeLimit").Single().Value == "PT15M", "Execution limit retained");
            if (schedule.LastDay) Require(root.Descendants(ns + "Day").Single().Value == "Last", "Last-day schedule retained");
            if (schedule.Kind == TaskScheduleKind.Monthly) Require(root.Descendants(ns + "Months").Single().Elements().Count() == 12, "All months retained");
            Require(xml == ScheduledTaskXml.Serialize(plan, new(2026, 9, 14)), "Deterministic task definition");
        }
        var interactive = draft with { Principal = new(ScheduledTaskAccountKind.InteractiveUser, "DOMAIN\\user", false) };
        var interactiveXml = XElement.Parse(ScheduledTaskXml.Serialize(interactive, new(2026, 9, 14)));
        ValidateNativeDefinition(interactiveXml.ToString());
        Require(interactiveXml.Descendants(ns + "LogonType").Single().Value == "InteractiveToken" &&
            interactiveXml.Descendants(ns + "RunLevel").Single().Value == "LeastPrivilege", "Interactive account and privilege retained");
        var otherZone = TimeZoneInfo.GetSystemTimeZones().First(zone => zone.Id != TimeZoneInfo.Local.Id);
        try
        {
            ScheduledTaskXml.Serialize(draft with { Schedule = new(TaskScheduleKind.Daily, new(9, 30)), TimeZoneId = otherZone.Id }, new(2026, 9, 14));
            throw new Exception("Foreign calendar time zone was silently converted");
        }
        catch (NotSupportedException) { }
        var transitions = new HashSet<TaskLocalTimeState>();
        for (int day = 0; day < 365 && transitions.Count < 2; day++)
        for (int hour = 0; hour < 24; hour++)
        {
            var date = new DateOnly(2026, 1, 1).AddDays(day);
            var time = new TimeOnly(hour, 30);
            var state = TaskSchedule.ResolveLocalTime(date, time, TimeZoneInfo.Local).State;
            if (state == TaskLocalTimeState.Unique || !transitions.Add(state)) continue;
            try
            {
                ScheduledTaskXml.Serialize(draft with { Schedule = new(TaskScheduleKind.Daily, time), TimeZoneId = TimeZoneInfo.Local.Id }, date);
                throw new Exception("Ambiguous or invalid calendar boundary was exported");
            }
            catch (ArgumentException) { }
        }
        string baseline = ScheduledTaskXml.Serialize(draft, new(2026, 9, 14));
        Require(ScheduledTaskXml.MatchesRequested(baseline, baseline), "Exact requested task XML verifies");
        var changedEnabled = XElement.Parse(baseline);
        changedEnabled.Descendants(ns + "Enabled").Last().Value = "true";
        Require(!ScheduledTaskXml.MatchesRequested(baseline, changedEnabled.ToString()), "Changed enabled state is rejected");
        var changedPrincipal = XElement.Parse(baseline);
        changedPrincipal.Descendants(ns + "UserId").Single().Value = "S-1-5-19";
        Require(!ScheduledTaskXml.MatchesRequested(baseline, changedPrincipal.ToString()), "Changed principal is rejected");
        var addedAction = XElement.Parse(baseline);
        addedAction.Element(ns + "Actions")!.Add(new XElement(ns + "ComHandler", new XElement(ns + "ClassId", Guid.Empty)));
        Require(!ScheduledTaskXml.MatchesRequested(baseline, addedAction.ToString()), "Additional action is rejected");
        Require(!ScheduledTaskXml.MatchesRequested(baseline, "<!DOCTYPE Task [<!ENTITY x 'y'>]>" + baseline), "DTD input is rejected");
        Console.WriteLine($"Task XML rejected {transitions.Count} local transition types and a foreign time zone.");
        Console.WriteLine("PASS: disabled task XML schedules, strict semantic read-back, literal values, accounts and execution limits; no registration.");
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
    private static void ValidateNativeDefinition(string xml)
    {
        object? service = null;
        object? definition = null;
        try
        {
            service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true)!);
            definition = ((dynamic)service!).NewTask(0);
            ((dynamic)definition).XmlText = xml;
            var parsed = XElement.Parse((string)((dynamic)definition).XmlText);
            XNamespace ns = ScheduledTaskXml.Namespace;
            Require(parsed.Element(ns + "Settings")!.Element(ns + "Enabled")!.Value == "false",
                "Windows accepts the in-memory definition and preserves disabled state");
            Require(ScheduledTaskXml.MatchesRequested(xml, parsed.ToString()),
                "Windows-normalized task XML retains every requested semantic field");
            CompareRequested(XElement.Parse(xml), parsed);
        }
        finally
        {
            if (definition is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(definition);
            if (service is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(service);
        }
    }
    private static void CompareRequested(XElement requested, XElement observed)
    {
        foreach (var group in requested.Elements().GroupBy(element => element.Name))
        {
            var expected = group.ToArray();
            var actual = observed.Elements(group.Key).ToArray();
            Require(actual.Length == expected.Length, "Windows retained requested element count: " + group.Key.LocalName);
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i].HasElements) CompareRequested(expected[i], actual[i]);
                else if (group.Key.LocalName == "StartBoundary")
                    Require(System.Xml.XmlConvert.ToDateTime(expected[i].Value, System.Xml.XmlDateTimeSerializationMode.Unspecified) ==
                        System.Xml.XmlConvert.ToDateTime(actual[i].Value, System.Xml.XmlDateTimeSerializationMode.Unspecified), "Windows retained local start boundary");
                else Require(expected[i].Value == actual[i].Value, "Windows retained requested value: " + group.Key.LocalName);
            }
        }
    }
}
