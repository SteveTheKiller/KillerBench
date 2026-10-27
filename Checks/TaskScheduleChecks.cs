using KillerBench.Engine;
using System.Text.Json;

internal static class TaskScheduleChecks
{
    private static readonly int[] SortedMonthDays = [1, 20];
    internal static void Run()
    {
        TaskRegistrationChecks.Run();
        ExactScheduledTaskReadChecks.Run();
        var accountPrincipal = new ScheduledTaskPrincipal(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", true);
        var accountEvidence = new ToolResult(0, "{\"account\":\"S-1-5-18\",\"sid\":\"S-1-5-18\"}", "", false);
        if (ScheduledTaskAccount.Interpret(accountPrincipal, accountEvidence).Sid != "S-1-5-18") throw new Exception("Resolved account SID lost");
        var mismatchedSid = accountEvidence with { Output = "{\"account\":\"S-1-5-18\",\"sid\":\"S-1-5-19\"}" };
        if (ScheduledTaskAccount.Interpret(accountPrincipal, mismatchedSid).Sid is not null) throw new Exception("Provider changed requested SID");
        var aliasPrincipal = new ScheduledTaskPrincipal(ScheduledTaskAccountKind.InteractiveUser, "FixtureAlias", false);
        var aliasEvidence = accountEvidence with { Output = "{\"account\":\"FixtureAlias\",\"sid\":\"S-1-5-18\"}" };
        if (ScheduledTaskAccount.Interpret(aliasPrincipal, aliasEvidence).Sid is not null) throw new Exception("System alias bypassed principal selection");
        foreach (var invalidAccount in new[] { accountEvidence with { Truncated = true }, accountEvidence with { TimedOut = true },
            accountEvidence with { Error = "Fixture account lookup failed after output" }, accountEvidence with { ExitCode = 1 },
            accountEvidence with { Output = "{\"account\":\"other\",\"sid\":\"S-1-5-18\"}" }, accountEvidence with { Output = "{}" } })
        {
            var unresolved = ScheduledTaskAccount.Interpret(accountPrincipal, invalidAccount);
            if (unresolved.Sid is not null || unresolved.Evidence != invalidAccount)
                throw new Exception("Unresolved account gained an identity or lost its evidence");
        }
        string executableFixture = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KillerBench-task-file-" + Guid.NewGuid().ToString("N") + ".exe");
        System.IO.File.WriteAllBytes(executableFixture, [1, 2, 3]);
        var fileReview = ScheduledTaskExecutable.InspectAsync(new(executableFixture, "")).GetAwaiter().GetResult();
        if (!fileReview.HasContentIdentity || fileReview.Bytes != 3 || fileReview.Sha256 != Convert.ToHexString(System.Security.Cryptography.SHA256.HashData([1, 2, 3])))
            throw new Exception("Executable content snapshot lost identity");
        var missingFile = ScheduledTaskExecutable.InspectAsync(new(executableFixture + ".missing", "")).GetAwaiter().GetResult();
        if (fileReview.HasPeHeader != false || fileReview.IsDll is not null || fileReview.Machine is not null)
            throw new Exception("Arbitrary task bytes were classified as a PE image");
        var managedImage = ScheduledTaskExecutable.InspectAsync(new(typeof(ScheduledTaskPlan).Assembly.Location, "")).GetAwaiter().GetResult();
        if (!managedImage.HasContentIdentity || managedImage.HasPeHeader != true || managedImage.IsDll != true || string.IsNullOrEmpty(managedImage.Machine))
            throw new Exception("Task inspection lost managed DLL header information");
        var imageRoundTrip = JsonSerializer.Deserialize<ScheduledTaskExecutableResult>(JsonSerializer.Serialize(managedImage));
        if (imageRoundTrip != managedImage) throw new Exception("Executable header details did not survive report serialization");
        if (missingFile.HasContentIdentity) throw new Exception("Missing task executable acquired an identity");
        foreach (var uncertain in new[] { missingFile, fileReview, managedImage,
            managedImage with { IsDll = null }, managedImage with { HasPeHeader = null } })
            if (!uncertain.RequiresReview) throw new Exception("Incomplete image or DLL inspection did not request review");
        var nativeImage = ScheduledTaskExecutable.InspectAsync(new(System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe"), "")).GetAwaiter().GetResult();
        if (nativeImage.RequiresReview || nativeImage.IsDll != false || !nativeImage.ImageReview.Contains("remain unverified"))
            throw new Exception("Read-only Windows executable inspection failed or implied launch verification");
        using (var stopped = new CancellationTokenSource())
        {
            stopped.Cancel();
            try { ScheduledTaskExecutable.InspectAsync(new(executableFixture, ""), stopped.Token).GetAwaiter().GetResult(); throw new Exception("Canceled inspection continued"); }
            catch (OperationCanceledException) { }
        }
        const string draft = """
            {"identity":{"path":"\\Fixture\\","name":"Review"},"action":{"executable":"C:\\Fixture\\tool.exe","arguments":"--flag"},"schedule":{"kind":"Startup"},"principal":{"kind":"InteractiveUser","userId":"fixture","highestPrivileges":false},"executionLimit":"01:00:00"}
            """;
        var parsedDraft = ScheduledTaskPlanJson.Parse(System.Text.Encoding.UTF8.GetBytes(draft));
        byte[] bomDraft = [0xef, 0xbb, 0xbf, .. System.Text.Encoding.UTF8.GetBytes(draft)];
        var parsedBom = ScheduledTaskPlanJson.Parse(bomDraft);
        if (!ScheduledTaskPlanJson.Serialize(parsedBom).SequenceEqual(ScheduledTaskPlanJson.Serialize(parsedDraft)))
            throw new Exception("UTF-8 BOM changed the imported task plan.");
        bool oversizedBomRejected = false;
        try
        {
            byte[] oversizedBom = [0xef, 0xbb, 0xbf, .. new byte[ScheduledTaskPlanJson.MaximumBytes - 2]];
            ScheduledTaskPlanJson.Parse(oversizedBom);
        }
        catch (ArgumentException) { oversizedBomRejected = true; }
        if (!oversizedBomRejected) throw new Exception("BOM bypassed task-plan byte limit.");
        foreach (var overlap in Enum.GetValues<ScheduledTaskOverlap>())
        {
            var overlapPlan = ScheduledTaskPlanJson.Parse(ScheduledTaskPlanJson.Serialize(parsedDraft with { Overlap = overlap }));
            if (overlapPlan.Overlap != overlap || !ScheduledTaskReview.Format(overlapPlan).Contains("If already running:"))
                throw new Exception("Duplicate-start selection lost in draft");
        }
        try { (parsedDraft with { Overlap = (ScheduledTaskOverlap)99 }).Validate(); throw new Exception("Unknown overlap policy accepted"); }
        catch (ArgumentException) { }
        var partialReport = new ScheduledTaskInspectionReport(DateTimeOffset.UnixEpoch, parsedDraft, fileReview, null, null, null)
        { Incomplete = true, Failure = "Canceled fixture" };
        var partialRoundTrip = JsonSerializer.Deserialize<ScheduledTaskInspectionReport>(JsonSerializer.Serialize(partialReport, Capture.JsonOptions), Capture.JsonOptions)!;
        if (partialRoundTrip.Executable != fileReview || !partialRoundTrip.Incomplete || partialRoundTrip.TaskRegistered ||
            partialRoundTrip.Account is not null || !partialRoundTrip.Format().Contains("INSPECTION INCOMPLETE") ||
            !partialRoundTrip.Format().Contains(fileReview.Sha256!) || !partialRoundTrip.Format().Contains("Absence of a conflict is not established"))
            throw new Exception("Partial inspection lost evidence or implied completed checks");
        var emptyReport = partialReport with { Executable = null };
        if (!emptyReport.Format().Contains("Not collected.")) throw new Exception("Canceled initial inspection omitted missing evidence");
        var inspectionReport = new ScheduledTaskInspectionReport(DateTimeOffset.UnixEpoch, parsedDraft, fileReview,
            ScheduledTaskAccount.Interpret(accountPrincipal, accountEvidence), new(ScheduledTaskConflictState.Unknown, "Fixture incomplete"), accountEvidence)
        { ExistingDefinition = new(parsedDraft.Identity, "<Task/>", "Fixture snapshot", accountEvidence) };
        using (var exported = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(inspectionReport, Capture.JsonOptions)))
        {
            var root = exported.RootElement;
            if (root.GetProperty("existingDefinition").GetProperty("xml").GetString() != "<Task/>")
                throw new Exception("Task inspection export lost existing definition evidence");
            if (root.GetProperty("taskRegistered").GetBoolean() || root.GetProperty("schemaVersion").GetInt32() != 1 ||
                root.GetProperty("account").GetProperty("evidence").GetProperty("output").GetString() != accountEvidence.Output ||
                root.GetProperty("inventoryEvidence").GetProperty("output").GetString() != accountEvidence.Output)
                throw new Exception("Task inspection export lost evidence or claimed registration");
        }
        var calendarDraft = parsedDraft with { TimeZoneId = "Pacific Standard Time", Schedule = new(TaskScheduleKind.Daily, new TimeOnly(2, 30)) };
        string gapReview = ScheduledTaskReview.Format(calendarDraft, new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero));
        string repeatReview = ScheduledTaskReview.Format(calendarDraft with { Schedule = new(TaskScheduleKind.Daily, new TimeOnly(1, 30)) }, new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero));
        if (!gapReview.Contains("INVALID CLOCK TIME") || !repeatReview.Contains("REPEATED CLOCK TIME") ||
            !repeatReview.Contains("-07:00") || !repeatReview.Contains("-08:00")) throw new Exception("Task review concealed DST ambiguity");
        var savedDraft = ScheduledTaskPlanJson.Parse(ScheduledTaskPlanJson.Serialize(parsedDraft));
        if (savedDraft.Identity != parsedDraft.Identity || savedDraft.Action != parsedDraft.Action ||
            savedDraft.Principal != parsedDraft.Principal || savedDraft.ExecutionLimit != parsedDraft.ExecutionLimit ||
            savedDraft.Schedule.Kind != parsedDraft.Schedule.Kind) throw new Exception("Saved task draft changed options");
        var conflictEvidence = new ToolResult(0, "", "", false);
        var nestedConflict = new ScheduledTaskSnapshot(new("\\fixture\\", "review"), "Ready");
        if (ScheduledTaskConflict.Review(parsedDraft.Identity, new([nestedConflict], false, conflictEvidence)).State != ScheduledTaskConflictState.ExistingTask)
            throw new Exception("Partial inventory lost an observed exact task conflict");
        var otherFolder = new ScheduledTaskSnapshot(new("\\Other\\", "Review"), "Ready");
        if (ScheduledTaskConflict.Review(parsedDraft.Identity, new([otherFolder], true, conflictEvidence)).State != ScheduledTaskConflictState.NotObserved)
            throw new Exception("Same task name in another folder caused a false conflict");
        foreach (var incomplete in new[] { new ScheduledTaskInventoryResult([], false, conflictEvidence),
            new([], true, conflictEvidence with { Truncated = true }), new([], true, conflictEvidence with { TimedOut = true }),
            new([], true, conflictEvidence with { ExitCode = 1 }) })
            if (ScheduledTaskConflict.Review(parsedDraft.Identity, incomplete).State != ScheduledTaskConflictState.Unknown)
                throw new Exception("Incomplete inventory implied task availability");
        string review = ScheduledTaskReview.Format(parsedDraft);
        if (!review.Contains("At machine startup") || !review.Contains("Highest privileges: Not selected") ||
            !review.Contains("No task was created or run.")) throw new Exception("Task review obscured operation or privilege state");
        var monthReview = ScheduledTaskReview.Format(parsedDraft with { TimeZoneId = "UTC", Schedule = new(TaskScheduleKind.Monthly, new TimeOnly(8, 30), MonthDays: [31]) });
        if (!monthReview.Contains("Month days 31 at 08:30:00") || !monthReview.Contains("skipped"))
            throw new Exception("Monthly review omitted missing-date behavior");
        if (parsedDraft.Identity.FullName != "\\Fixture\\Review" || parsedDraft.Action.Arguments != "--flag")
            throw new Exception("Task JSON lost selected identity or arguments");
        foreach (string invalidJson in new[] { "null", "{}", draft.Replace("\"arguments\":", "\"unknown\":0,\"arguments\":"),
            draft.Replace("\"highestPrivileges\":false", "\"highestPrivileges\":true,\"highestPrivileges\":false"),
            draft.Replace("\"InteractiveUser\"", "0"), draft.Replace("\"executionLimit\":\"01:00:00\"", "\"executionLimit\":\"00:00:00\"") })
        {
            try { ScheduledTaskPlanJson.Parse(System.Text.Encoding.UTF8.GetBytes(invalidJson)); }
            catch (Exception e) when (e is ArgumentException or System.Text.Json.JsonException) { continue; }
            throw new Exception("Invalid task JSON accepted");
        }
        var action = new ScheduledTaskAction("C:\\Program Files\\Fixture\\tool.exe", "--name \"two words\"", "C:\\Program Files\\Fixture").Validate();
        if (action.Arguments != "--name \"two words\"") throw new Exception("Task arguments rewritten");
        var selectedDays = new[] { DayOfWeek.Monday };
        var taskPlan = new ScheduledTaskPlan(new("\\Fixture\\", "Review"), action,
            new(TaskScheduleKind.Weekly, new TimeOnly(9, 0), selectedDays),
            new(ScheduledTaskAccountKind.InteractiveUser, "S-1-5-21-1-2-3-1001", false), TimeSpan.FromHours(1)) { TimeZoneId = "UTC" }.Validate();
        selectedDays[0] = DayOfWeek.Friday;
        if (taskPlan.Schedule.Weekdays!.Single() != DayOfWeek.Monday || taskPlan.Principal.HighestPrivileges)
            throw new Exception("Task plan changed caller choices or retained mutable schedule");
        foreach (var invalid in new[] {
            taskPlan with { TimeZoneId = null },
            taskPlan with { TimeZoneId = "KillerBench-Unknown-Zone" },
            taskPlan with { ExecutionLimit = TimeSpan.Zero },
            taskPlan with { ExecutionLimit = TimeSpan.FromHours(73) },
            taskPlan with { Principal = new(ScheduledTaskAccountKind.InteractiveUser, "SYSTEM", false) },
            taskPlan with { Principal = new(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", false) },
            taskPlan with { Principal = new(ScheduledTaskAccountKind.LocalSystem, "someone", true) },
            taskPlan with { Principal = new((ScheduledTaskAccountKind)99, "someone", false) } })
        {
            try { invalid.Validate(); }
            catch (ArgumentException) { continue; }
            throw new Exception("Invalid task registration draft accepted");
        }
        (taskPlan with { Principal = new(ScheduledTaskAccountKind.LocalSystem, "S-1-5-18", true) }).Validate();
        foreach (string executable in new[] { "tool.exe", "C:tool.exe", "\"C:\\tool.exe\"", "C:\\..\\tool.exe", "C:\\tool.exe:stream", "\\\\server\\tool.exe", "C:\\", "C:\\Folder\\", "C:\\Folder.\\tool.exe", "C:\\Folder \\tool.exe", "C:\\Folder\\\\tool.exe" })
        {
            try { new ScheduledTaskAction(executable, "").Validate(); }
            catch (ArgumentException) { continue; }
            throw new Exception("Ambiguous task executable accepted");
        }
        const string taskRow = "{\"path\":\"\\\\\",\"name\":\"Fixture\",\"state\":\"Ready\"}";
        var taskEvidence = new ToolResult(0, "{\"tasks\":[" + taskRow + "]}", "", false);
        var inventory = ScheduledTaskInventory.Interpret(taskEvidence);
        foreach (string conflictingRow in new[] { taskRow.Replace("Ready", "Disabled"), taskRow.Replace("Ready", "InvalidState") })
        foreach (string orderedRows in new[] { taskRow + "," + conflictingRow, conflictingRow + "," + taskRow })
        {
            var evidence = taskEvidence with { Output = "{\"tasks\":[" + orderedRows + "]}" };
            var ambiguous = ScheduledTaskInventory.Interpret(evidence);
            if (ambiguous.Complete || ambiguous.Tasks.Count != 0 || ambiguous.Evidence != evidence)
                throw new Exception("Conflicting task identities retained a preferred record");
        }
        foreach (string ambiguousJson in new[] { "{\"tasks\":[],\"tasks\":[" + taskRow + "]}",
            "{\"tasks\":[" + taskRow.Replace("\"state\":", "\"state\":\"Disabled\",\"state\":") + "]}" })
            if (ScheduledTaskInventory.Interpret(taskEvidence with { Output = ambiguousJson }) is not { Complete: false, Tasks.Count: 0 })
                throw new Exception("Duplicate task inventory fields established a usable record");
        var failedInventoryEvidence = taskEvidence with { Error = "Fixture enumeration failed after output" };
        var failedInventory = ScheduledTaskInventory.Interpret(failedInventoryEvidence);
        if (failedInventory.Complete || failedInventory.Tasks.Count != 0 || failedInventory.Evidence != failedInventoryEvidence)
            throw new Exception("Task inventory accepted conflicting error output.");
        var absentTarget = new ScheduledTaskIdentity("\\", "AbsentFixture");
        if (ScheduledTaskConflict.Review(absentTarget, inventory with { Evidence = failedInventoryEvidence }).State != ScheduledTaskConflictState.Unknown)
            throw new Exception("Task error output established target availability.");
        var observed = inventory.Tasks.Single();
        foreach (var invalid in new[] { observed with { State = "Other" }, observed with { ActionCount = -1 },
            observed with { ActionCount = 33 }, observed with { ActionCount = 0, ActionTypes = ["MSFT_TaskExecAction"] },
            observed with { ActionCount = 1, ActionTypes = [""] } })
            if (ScheduledTaskDiagnostics.Assess(invalid).State != CheckState.Unknown)
                throw new Exception("Invalid embedded task snapshot was accepted");
        foreach (string state in new[] { "Ready", "Running", "Queued", "Disabled", "Unknown" })
        {
            var finding = ScheduledTaskDiagnostics.Assess(observed with { State = state, ActionCount = 0 });
            var expected = state == "Disabled" ? CheckState.Information : state == "Unknown" ? CheckState.Unknown : CheckState.Attention;
            if (finding.State != expected || finding.Repair is not null) throw new Exception("Task action diagnosis selected an unsafe result");
        }
        if (ScheduledTaskDiagnostics.Assess(observed with { ActionCount = null, TriggerTypes = [] }).State != CheckState.Unknown ||
            ScheduledTaskDiagnostics.Assess(observed with { ActionCount = 1, TriggerTypes = [] }).State != CheckState.Information)
            throw new Exception("Missing action evidence was not separated from an intentional manual task");
        foreach (string incompleteState in new[] { "Disabled", "Ready", "Running", "Queued" })
        {
            var incompleteTask = ScheduledTaskDiagnostics.Assess(observed with { State = incompleteState, ActionCount = null, ActionTypes = null });
            if (incompleteTask.State != CheckState.Unknown || incompleteTask.Repair is not null || !incompleteTask.Detail.Contains("action count is unavailable"))
                throw new Exception("Missing task action coverage lost uncertainty or explicit follow-up");
        }
        if (!inventory.Complete || inventory.Tasks.Count != 1 || inventory.Evidence != taskEvidence) throw new Exception("Task inventory lost identity/evidence");
        var noTriggers = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"triggerTypes\":[],\"state\":") }).Tasks.Single();
        if (noTriggers.TriggerTypes is null || noTriggers.TriggerTypes.Count != 0) throw new Exception("Empty trigger collection lost");
        foreach (string trigger in new[] { "MSFT_TaskDailyTrigger", "MSFT_TaskBootTrigger", "MSFT_TaskEventTrigger" })
        {
            var triggered = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"triggerTypes\":[\"" + trigger + "\"],\"state\":") }).Tasks.Single();
            if (triggered.TriggerTypes?.Single() != trigger) throw new Exception("Trigger class lost");
        }
        var malformedTrigger = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"triggerTypes\":[null],\"state\":") }).Tasks.Single();
        if (malformedTrigger.TriggerTypes is not null) throw new Exception("Malformed trigger accepted");
        foreach (string type in new[] { "MSFT_TaskExecAction", "MSFT_TaskComHandlerAction", "FutureProviderAction" })
        {
            var typed = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"actionCount\":1,\"actionTypes\":[\"" + type + "\"],\"state\":") }).Tasks.Single();
            if (typed.ActionTypes?.Single() != type) throw new Exception("Reported action class was lost");
        }
        foreach (int count in new[] { -1, 0, 1, 32, 33 })
        {
            var actionTask = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"actionCount\":" + count + ",\"state\":") }).Tasks.Single();
            if (actionTask.ActionCount != (count is >= 0 and <= 32 ? (int?)count : null)) throw new Exception("Task action count validation failed");
        }
        var principal = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"userId\":\"S-1-5-18\",\"runLevel\":\"Highest\",\"state\":") }).Tasks.Single();
        if (principal.UserId != "S-1-5-18" || principal.RunLevel != "Highest" || inventory.Tasks[0].UserId is not null)
            throw new Exception("Task principal evidence was inferred or lost");
        var partialTasks = ScheduledTaskInventory.Interpret(taskEvidence with { Output = "{\"tasks\":[" + taskRow + ",null]}" });
        var groupTask = ScheduledTaskInventory.Interpret(taskEvidence with { Output = taskEvidence.Output.Replace("\"state\":", "\"groupId\":\"S-1-5-32-545\",\"state\":") }).Tasks.Single();
        if (groupTask.GroupId != "S-1-5-32-545" || groupTask.UserId is not null) throw new Exception("Group principal conflated with user identity");
        if (partialTasks.Complete || partialTasks.Tasks.Count != 1) throw new Exception("Invalid task suppressed independent inventory");
        if (ScheduledTaskInventory.Interpret(taskEvidence with { Truncated = true }).Complete) throw new Exception("Truncated task inventory accepted");
        var rootTask = new ScheduledTaskIdentity("\\", "Fixture");
        var nestedTask = new ScheduledTaskIdentity("\\Vendor\\", "Fixture");
        if (rootTask.Matches(nestedTask) || !nestedTask.Matches(new("\\vendor\\", "fixture")))
            throw new Exception("Scheduled task folder identity was lost");
        foreach (string path in new[] { "", "Vendor", "\\Vendor", "\\..\\", "\\Vendor\\\\", "\\*\\" })
        {
            try { _ = new ScheduledTaskIdentity(path, "Fixture"); }
            catch (ArgumentException) { continue; }
            throw new Exception("Invalid task folder accepted");
        }
        foreach (string name in new[] { "", "*", "Folder\\Task", " Task", "Task\n", ".." })
        {
            try { _ = new ScheduledTaskIdentity("\\", name); }
            catch (ArgumentException) { continue; }
            throw new Exception("Invalid task name accepted");
        }
        var time = new TimeOnly(8, 30);
        foreach (var option in new[] { new TaskScheduleOptions(TaskScheduleKind.Startup),
            new(TaskScheduleKind.Daily, time), new(TaskScheduleKind.Weekly, time, [DayOfWeek.Monday]),
            new(TaskScheduleKind.Monthly, time, MonthDays: [31]), new(TaskScheduleKind.Monthly, time, LastDay: true) })
            TaskSchedule.Validate(option);
        foreach (var option in new[] { new TaskScheduleOptions(TaskScheduleKind.Startup, time),
            new(TaskScheduleKind.Daily), new(TaskScheduleKind.Weekly, time),
            new(TaskScheduleKind.Monthly, time), new(TaskScheduleKind.Monthly, time, MonthDays: [1], LastDay: true),
            new(TaskScheduleKind.Monthly, time, MonthDays: [0]), new(TaskScheduleKind.Monthly, time, MonthDays: [32]),
            new(TaskScheduleKind.Daily, time, [DayOfWeek.Monday]), new(TaskScheduleKind.Weekly, time, [DayOfWeek.Monday, DayOfWeek.Monday]),
            new((TaskScheduleKind)99, time) })
        {
            try { TaskSchedule.Validate(option); }
            catch (ArgumentException) { continue; }
            throw new Exception("Invalid task schedule accepted");
        }
        var days = new[] { 20, 1 };
        var plan = TaskSchedule.Validate(new(TaskScheduleKind.Monthly, time, MonthDays: days));
        days[0] = 99;
        if (!plan.MonthDays!.SequenceEqual(SortedMonthDays)) throw new Exception("Validated schedule changed through caller collection");
        var last = new TaskScheduleOptions(TaskScheduleKind.Monthly, time, LastDay: true);
        foreach (int year in new[] { 2024, 2025, 2100, 2000 })
        foreach (int month in Enumerable.Range(1, 12))
        {
            int end = DateTime.DaysInMonth(year, month);
            if (!TaskSchedule.MatchesDate(last, new(year, month, end)) || TaskSchedule.MatchesDate(last, new(year, month, end - 1)))
                throw new Exception("Last-day schedule mismatch");
            var day31 = new TaskScheduleOptions(TaskScheduleKind.Monthly, time, MonthDays: [31]);
            if (TaskSchedule.MatchesDate(day31, new(year, month, end)) != (end == 31))
                throw new Exception("Missing month day was shifted to another date");
        }
        var monday = new TaskScheduleOptions(TaskScheduleKind.Weekly, time, [DayOfWeek.Monday]);
        if (!TaskSchedule.MatchesDate(monday, new(2026, 9, 14)) || TaskSchedule.MatchesDate(monday, new(2026, 9, 15)) ||
            TaskSchedule.MatchesDate(new(TaskScheduleKind.Startup), new(2026, 9, 14)))
            throw new Exception("Weekly/startup date classification failed");
        Console.WriteLine("PASS: task schedule options, conflicting selections and immutable normalized days.");
        var preview = TaskSchedule.PreviewDates(new(TaskScheduleKind.Monthly, time, MonthDays: [31]), new(2026, 2, 1), 3);
        if (!preview.SequenceEqual([new DateOnly(2026, 3, 31), new(2026, 5, 31), new(2026, 7, 31)]))
            throw new Exception("Monthly preview shifted missing dates");
        if (TaskSchedule.PreviewDates(new(TaskScheduleKind.Startup), new(2026, 1, 1)).Count != 0 ||
            TaskSchedule.PreviewDates(new(TaskScheduleKind.Daily, time), DateOnly.MaxValue, 5).Count != 1)
            throw new Exception("Preview boundary handling failed");
        foreach (int count in new[] { 0, -1, 101 })
        {
            try { TaskSchedule.PreviewDates(last, new(2026, 1, 1), count); }
            catch (ArgumentOutOfRangeException) { continue; }
            throw new Exception("Unbounded schedule preview accepted");
        }
        var zone = TimeZoneInfo.CreateCustomTimeZone("Fixture DST", TimeSpan.FromHours(-8), "Fixture DST", "Standard", "Daylight",
            [TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 8),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1))]);
        var gap = TaskSchedule.ResolveLocalTime(new(2026, 3, 8), new(2, 30), zone);
        var repeated = TaskSchedule.ResolveLocalTime(new(2026, 11, 1), new(1, 30), zone);
        var ordinary = TaskSchedule.ResolveLocalTime(new(2026, 7, 1), new(8, 30), zone);
        if (gap.State != TaskLocalTimeState.Invalid || gap.Candidates.Count != 0 ||
            repeated.State != TaskLocalTimeState.Ambiguous || repeated.Candidates.Count != 2 ||
            repeated.Candidates[1] - repeated.Candidates[0] != TimeSpan.FromHours(1) ||
            ordinary.State != TaskLocalTimeState.Unique || ordinary.Candidates.Single().Offset != TimeSpan.FromHours(-7))
            throw new Exception("Schedule timezone resolution concealed DST ambiguity");
    }
}
