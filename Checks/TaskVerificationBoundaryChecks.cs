using KillerBench.Engine.Scheduling;
using System.Xml.Linq;

internal static class TaskVerificationBoundaryChecks
{
    internal static void Run()
    {
        var plan = new ScheduledTaskPlan(new("\\KillerBench\\", "Verification fixture"),
            new("C:\\Tools\\check.exe", "--check"), new(TaskScheduleKind.Daily, new(9, 30)),
            new(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", true), TimeSpan.FromMinutes(1)) { TimeZoneId = TimeZoneInfo.Local.Id };
        string requested = ScheduledTaskXml.Serialize(plan, new(2026, 11, 1));
        XNamespace ns = ScheduledTaskXml.Namespace;
        var observed = XElement.Parse(requested);
        observed.Descendants(ns + "CalendarTrigger").Single().Add(new XElement(ns + "Repetition", new XElement(ns + "Interval", "PT1M")));
        if (ScheduledTaskXml.MatchesRequested(requested, observed.ToString()))
            throw new Exception("An added one-minute repetition must fail requested-task verification.");
        Console.WriteLine("PASS: task verification rejects added repetition.");
        observed = XElement.Parse(requested);
        observed.Descendants(ns + "StartBoundary").Single().Value = "invalid date";
        if (ScheduledTaskXml.MatchesRequested(requested, observed.ToString()))
            throw new Exception("Malformed date values must fail verification without throwing.");
        foreach (var (parent, extra) in new[]
        {
            ("CalendarTrigger", new XElement(ns + "EndBoundary", "2027-01-01T00:00:00")),
            ("Exec", new XElement(ns + "WorkingDirectory", "C:\\Other")),
            ("Settings", new XElement(ns + "Hidden", "true")),
            ("Settings", new XElement(ns + "IdleSettings", new XElement(ns + "StopOnIdleEnd", "false"), new XElement(ns + "RestartOnIdle", "false"))),
            ("Command", new XElement(ns + "Unexpected", "behavior"))
        })
        {
            observed = XElement.Parse(requested);
            observed.Descendants(ns + parent).Single().Add(extra);
            if (ScheduledTaskXml.MatchesRequested(requested, observed.ToString()))
                throw new Exception("Added behavior must fail verification: " + extra.Name.LocalName);
        }
        Console.WriteLine("PASS: task verification rejects malformed dates, end boundaries, nested actions and nondefault settings.");
        observed = XElement.Parse(requested);
        var boundary = observed.Descendants(ns + "StartBoundary").Single();
        var localBoundary = DateTime.SpecifyKind(System.Xml.XmlConvert.ToDateTime(boundary.Value,
            System.Xml.XmlDateTimeSerializationMode.Unspecified), DateTimeKind.Local);
        boundary.Value = System.Xml.XmlConvert.ToString(localBoundary, System.Xml.XmlDateTimeSerializationMode.RoundtripKind);
        if (ScheduledTaskXml.MatchesRequested(requested, observed.ToString()))
            throw new Exception("A fixed-offset boundary must not verify as a floating local schedule.");
        Console.WriteLine("PASS: task verification preserves floating local-time semantics.");
        observed = XElement.Parse(requested);
        observed.Descendants(ns + "CalendarTrigger").Single().SetAttributeValue("unexpected", "behavior");
        if (ScheduledTaskXml.MatchesRequested(requested, observed.ToString()))
            throw new Exception("Unexpected task attributes must fail verification.");
        string noArguments = ScheduledTaskXml.Serialize(plan with { Action = new(plan.Action.Executable, "") }, new(2026, 11, 1));
        observed = XElement.Parse(noArguments);
        observed.Descendants(ns + "Arguments").Single().Remove();
        if (!ScheduledTaskXml.MatchesRequested(noArguments, observed.ToString()))
            throw new Exception("Windows may omit an empty action argument string during native registration.");
    }
}
