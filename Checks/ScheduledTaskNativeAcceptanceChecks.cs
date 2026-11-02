using System.IO;
using System.Security.Principal;
using System.Xml.Linq;
using KillerBench.Engine.Scheduling;
using KillerBench.Engine.Windows.Scheduling;

internal static class ScheduledTaskNativeAcceptanceChecks
{
    internal static async Task RunAsync(string scratch)
    {
        if (!OperatingSystem.IsWindows() || Environment.MachineName != "KILLERLAB-W11" ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Native task acceptance requires the explicitly admitted disposable KillerLab guest.");
        string name = "KillerBench-Acceptance-" + Guid.NewGuid().ToString("N");
        DateOnly date = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var plan = new ScheduledTaskPlan(new("\\", name),
            new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "whoami.exe"), ""),
            new(TaskScheduleKind.Daily, new(9, 30)), new(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", true),
            TimeSpan.FromMinutes(1)) { TimeZoneId = TimeZoneInfo.Local.Id };
        var host = new WindowsTaskRegistrationHost(date);
        var preflight = await host.CheckAsync(plan, CancellationToken.None);
        if (!preflight.Eligible) throw new Exception("Native task preflight blocked: " + preflight.Detail);
        string journalRoot = Path.Combine(scratch, name);
        var journal = new FileTaskRegistrationJournal(journalRoot, date);
        await journal.PrepareAsync(plan, preflight, CancellationToken.None);
        dynamic scheduler = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true)!)!;
        scheduler.Connect();
        dynamic folder = scheduler.GetFolder("\\");
        bool created = false;
        try
        {
            var result = await host.CreateNewAsync(plan, CancellationToken.None);
            if (result.ExitCode != 0) throw new Exception("Native task creation failed: " + result.Error);
            created = true;
            var exact = await host.ReadBackAsync(plan.Identity, CancellationToken.None);
            if (exact.Observed is null) throw new Exception("Native disabled task round-trip did not verify: " + exact.Evidence.Output);
            string requested = ScheduledTaskXml.Serialize(plan, date);
            XNamespace ns = ScheduledTaskXml.Namespace;
            foreach (var extra in new[]
            {
                new XElement(ns + "Repetition", new XElement(ns + "Interval", "PT1M")),
                new XElement(ns + "EndBoundary", "2030-01-01T00:00:00")
            })
            {
                var changed = XElement.Parse(requested);
                changed.Descendants(ns + "CalendarTrigger").Single().Add(extra);
                dynamic definition = scheduler.NewTask(0);
                definition.XmlText = changed.ToString();
                dynamic altered = folder.RegisterTaskDefinition(name, definition, 6, "SYSTEM", null, 5, null);
                if ((bool)altered.Enabled) throw new Exception("The acceptance task unexpectedly became enabled.");
                var read = await host.ReadBackAsync(plan.Identity, CancellationToken.None);
                if (read.Observed is not null) throw new Exception("Native altered task behavior was accepted.");
                byte[] pending = await File.ReadAllBytesAsync(FileTaskRegistrationJournal.GetPendingPath(journalRoot, plan.Identity));
                var recovery = await TaskRegistrationRecovery.RecoverAsync(plan, journalRoot, new WindowsTaskRegistrationRecoveryHost());
                byte[] retained = await File.ReadAllBytesAsync(FileTaskRegistrationJournal.GetPendingPath(journalRoot, plan.Identity));
                if (recovery.Outcome != TaskRegistrationRecoveryOutcome.Conflict ||
                    !pending.SequenceEqual(retained))
                    throw new Exception("Native task mismatch did not preserve pending recovery evidence.");
            }
            dynamic restored = scheduler.NewTask(0);
            restored.XmlText = requested;
            dynamic final = folder.RegisterTaskDefinition(name, restored, 6, "SYSTEM", null, 5, null);
            if ((bool)final.Enabled || (int)final.NumberOfMissedRuns != 0)
                throw new Exception("Task acceptance must leave the definition disabled and never execute it.");
            var resolved = await TaskRegistrationRecovery.RecoverAsync(plan, journalRoot, new WindowsTaskRegistrationRecoveryHost());
            if (resolved.Outcome != TaskRegistrationRecoveryOutcome.Verified) throw new Exception("Exact native recovery did not verify: " + resolved.Detail);
            Console.WriteLine("PASS: native disabled task creation, Windows normalization, repetition/end-boundary mismatch and no-repeat pending recovery.");
        }
        finally
        {
            if (created) folder.DeleteTask(name, 0);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(folder);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(scheduler);
        }
    }
}
