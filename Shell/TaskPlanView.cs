using System.IO;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;
using Microsoft.Win32;

namespace KillerBench;

public partial class MainWindow
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextEvidence, ScheduledTaskInspectionReport> taskInspectionReports = new();
    private void OnTaskDraftLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TaskDraftStartDate.Text))
            TaskDraftStartDate.Text = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        TaskDraftWeekdays.ItemsSource ??= Enum.GetValues<DayOfWeek>();
        TaskDraftMonthDays.ItemsSource ??= Enumerable.Range(1, 31).ToArray();
        if (TaskDraftZone.ItemsSource is null)
        {
            TaskDraftZone.ItemsSource = TimeZoneInfo.GetSystemTimeZones();
            TaskDraftZone.SelectedValue = TimeZoneInfo.Local.Id;
        }
    }

    private void OnTaskDraftScheduleChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TaskDraftWeekly is null || TaskDraftMonthly is null || TaskDraftTime is null) return;
        TaskDraftWeekly.Visibility = TaskDraftSchedule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        TaskDraftMonthly.Visibility = TaskDraftSchedule.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        TaskDraftTime.IsEnabled = TaskDraftSchedule.SelectedIndex != 3;
        if (TaskDraftZone is not null) TaskDraftZone.IsEnabled = TaskDraftSchedule.SelectedIndex != 3;
    }

    private void OnTaskRegistrationConsentChanged(object sender, RoutedEventArgs e)
    {
        if (TaskRegistrationButton is not null)
            TaskRegistrationButton.Content = TaskRegistrationConsent.IsChecked == true
                ? "Create disabled task"
                : "Preview registration";
    }

    private ScheduledTaskPlan ReadTaskDraftForm()
    {
            var kind = (TaskScheduleKind)TaskDraftSchedule.SelectedIndex;
            TimeOnly? time = kind == TaskScheduleKind.Startup ? null :
                TimeOnly.ParseExact(TaskDraftTime.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            bool last = kind == TaskScheduleKind.Monthly && TaskDraftLastDay.IsChecked == true;
            var schedule = new TaskScheduleOptions(kind, time,
                kind == TaskScheduleKind.Weekly ? TaskDraftWeekdays.SelectedItems.Cast<DayOfWeek>().ToArray() : null,
                kind == TaskScheduleKind.Monthly && !last ? TaskDraftMonthDays.SelectedItems.Cast<int>().ToArray() : null, last);
            bool system = TaskDraftAccountMode.SelectedIndex == 1;
            int minutes = int.Parse(TaskDraftMinutes.Text, System.Globalization.CultureInfo.InvariantCulture);
            return new ScheduledTaskPlan(new(TaskDraftFolder.Text, TaskDraftName.Text),
                new(TaskDraftExecutable.Text, TaskDraftArguments.Text, string.IsNullOrWhiteSpace(TaskDraftDirectory.Text) ? null : TaskDraftDirectory.Text),
                schedule, new(system ? ScheduledTaskAccountKind.LocalSystem : ScheduledTaskAccountKind.InteractiveUser,
                    system ? "S-1-5-18" : TaskDraftUser.Text, TaskDraftHighest.IsChecked == true), TimeSpan.FromMinutes(minutes))
                { TimeZoneId = kind == TaskScheduleKind.Startup ? null : TaskDraftZone.SelectedValue as string,
                  Overlap = (ScheduledTaskOverlap)TaskDraftOverlap.SelectedIndex }.Validate();
    }

    private void OnReviewTaskDraft(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        try
        {
            var plan = ReadTaskDraftForm();
            string output = ScheduledTaskReview.Format(plan, DateTimeOffset.UtcNow);
            BeginWorkflowOutput(WorkflowKind.ScheduledTasks);
            AppendWorkflowOutput(output);
            const string status = "Task draft reviewed. No conflict check, registration or execution performed.";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, DateTimeOffset.UtcNow,
                status,
                [new WorkflowCheck("Task identity", CheckState.Information, "Draft: " + plan.Identity.FullName + "."),
                 new WorkflowCheck("Registration boundary", CheckState.Information,
                    "This draft review did not check for conflicts, register, enable, execute, or change a Windows task.")], [],
                "Read-only scheduled-task draft review. No task journal was written."), "Scheduled task draft");
            Report(status);
            SetScanLight("incomplete", "Draft review only; eligibility is unverified.");
        }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException)
        { Report("Check task options: " + error.Message); }
    }

    private async void OnSaveTaskDraft(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        try
        {
            byte[] data = ScheduledTaskPlanJson.Serialize(ReadTaskDraftForm());
            var picker = new SaveFileDialog { Title = "Save new task draft", Filter = "Task plan (*.json)|*.json", DefaultExt = ".json", FileName = "task-plan.json" };
            if (picker.ShowDialog(this) != true) return;
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            readCancellation = stop;
            SetCaptureBusy(true);
            try
            {
                await using var file = new FileStream(picker.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                await file.WriteAsync(data, stop.Token);
                await file.FlushAsync(stop.Token);
                Report("Task draft saved. No Windows task was created.");
            }
            finally { readCancellation = null; SetCaptureBusy(false); }
        }
        catch (OperationCanceledException) { Report("Draft save canceled or timed out. The destination may contain incomplete data."); }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Report("Cannot save draft. Choose a new filename and check the options: " + error.Message); }
    }

    private async void OnExportTaskXml(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        try
        {
            var startDate = DateOnly.ParseExact(TaskDraftStartDate.Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(ScheduledTaskXml.Serialize(ReadTaskDraftForm(), startDate));
            var picker = new SaveFileDialog { Title = "Export disabled task definition", Filter = "Task XML (*.xml)|*.xml", DefaultExt = ".xml", FileName = "task-review.xml" };
            if (picker.ShowDialog(this) != true) return;
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            readCancellation = stop;
            SetCaptureBusy(true);
            try
            {
                await using var file = new FileStream(picker.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                await file.WriteAsync(data, stop.Token);
                await file.FlushAsync(stop.Token);
                Report("Disabled task XML exported. No Windows task was registered or run.");
            }
            finally { readCancellation = null; SetCaptureBusy(false); }
        }
        catch (OperationCanceledException) { Report("XML export canceled or timed out. The destination may contain incomplete data."); }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Report("Cannot export task XML. Choose a new filename and check the options: " + error.Message); }
    }

    private async void OnRegisterTaskDraft(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        try
        {
            var plan = ReadTaskDraftForm();
            var startDate = DateOnly.ParseExact(TaskDraftStartDate.Text, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);
            bool execute = TaskRegistrationConsent.IsChecked == true;
            string journalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "TaskRegistration");
            readCancellation = stop;
            SetCaptureBusy(true);
            BeginWorkflowOutput(WorkflowKind.ScheduledTasks);
            AppendWorkflowOutput(execute
                ? "TASK REGISTRATION / GUARDED CREATE"
                : "TASK REGISTRATION / PREVIEW");
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => TaskRegistration.RunAsync(plan,
                new WindowsTaskRegistrationHost(startDate), execute, stop.Token,
                new FileTaskRegistrationJournal(journalFolder, startDate), progress: progress));
            string output = "TASK REGISTRATION / " + result.Status.ToUpperInvariant() +
                "\nTarget: " + plan.Identity.FullName +
                "\nCreate attempted: " + (result.CreateAttempted ? "Yes" : "No") +
                "\nDefinition enabled: No\nTask executed: No\n\n" + result.Detail +
                "\n\nRecovery folder: " + journalFolder;
            workflowOutputActive = false;
            RegisterSource(new TextEvidence("Task registration: " + plan.Identity.FullName, output.Split('\n'), false));
            Report(result.Detail);
            SetScanLight(result.Status == "Registered" ? "ready" :
                result.Status is "Blocked" or "Unverified" ? "attention" : "incomplete", result.Detail);
        }
        catch (OperationCanceledException) { Report("Task registration canceled or timed out."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            FormatException or OverflowException or NotSupportedException or InvalidOperationException or
            System.ComponentModel.Win32Exception)
        { Report("Cannot register task draft: " + error.Message); }
        finally { workflowOutputActive = false; readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnRecoverTaskRegistration(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        try
        {
            var plan = ReadTaskDraftForm();
            string journalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KillerBench", "TaskRegistration");
            readCancellation = stop;
            SetCaptureBusy(true);
            BeginWorkflowOutput(WorkflowKind.ScheduledTasks);
            AppendWorkflowOutput("TASK REGISTRATION / READ-ONLY RECOVERY");
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => TaskRegistrationRecovery.RecoverAsync(plan, journalFolder,
                new WindowsTaskRegistrationRecoveryHost(), stop.Token, progress: progress));
            string output = "TASK REGISTRATION RECOVERY / " + result.Outcome.ToString().ToUpperInvariant() +
                "\nTarget: " + plan.Identity.FullName +
                "\nRetry attempted: No\nTask changed: No\nTask executed: No\n\n" + result.Detail +
                "\n\nPending or archived record: " + (result.PendingRecord ?? "None") +
                "\nRecovery record: " + (result.RecoveryRecord ?? "None");
            workflowOutputActive = false;
            RegisterSource(new TextEvidence("Task registration recovery: " + plan.Identity.FullName, output.Split('\n'), false));
            Report(result.Detail);
            SetScanLight(result.Outcome == TaskRegistrationRecoveryOutcome.Verified ? "ready" :
                result.Outcome is TaskRegistrationRecoveryOutcome.Conflict or TaskRegistrationRecoveryOutcome.Inconclusive
                    ? "attention" : "incomplete", result.Detail);
        }
        catch (OperationCanceledException) { Report("Task registration recovery canceled or timed out. The pending guard remains in place."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            FormatException or OverflowException or NotSupportedException or InvalidOperationException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Report("Cannot recover task registration: " + error.Message); }
        finally { workflowOutputActive = false; readCancellation = null; SetCaptureBusy(false); }
    }

    private async Task InspectTaskDraftAsync(ScheduledTaskPlan plan, CancellationToken token)
    {
        ScheduledTaskExecutableResult? executable = null;
        FileSignatureResult? signature = null;
        ScheduledTaskAccountResult? account = null;
        ScheduledTaskInventoryResult? inventory = null;
        ScheduledTaskConflictResult? conflict = null;
        ScheduledTaskDefinitionResult? definition = null;
        bool completed = false;
        string? failure = null;
        try
        {
        Report("Inspecting executable file without running it...");
        executable = await Task.Run(() => ScheduledTaskExecutable.InspectAsync(plan.Action, token));
        Report("Reading executable signature without running it...");
        signature = await FileSignature.InspectAsync(plan.Action, token);
        Report("Resolving task account identity...");
        account = await ScheduledTaskAccount.ResolveAsync(plan.Principal, token);
        Report("Checking visible tasks for a conflicting full path...");
        inventory = await ScheduledTaskInventory.ReadAsync(token);
        conflict = ScheduledTaskConflict.Review(plan.Identity, inventory);
        if (conflict.State == ScheduledTaskConflictState.ExistingTask)
        {
            Report("Reading the existing task definition without changing it...");
            definition = await ScheduledTaskDefinition.ReadAsync(plan.Identity, token);
        }
        completed = true;
        }
        catch (Exception error)
        {
            failure = error is OperationCanceledException ? "Canceled or timed out. Completed observations are retained." : error.Message;
            throw;
        }
        finally
        {
        var inspection = new ScheduledTaskInspectionReport(DateTimeOffset.UtcNow, plan, executable, account, conflict, inventory?.Evidence)
        { Signature = signature, ExistingDefinition = definition, Incomplete = !completed, Failure = failure };
        var inspectionSource = new TextEvidence("Task draft: " + plan.Identity.FullName,
            inspection.Format().Split('\n'), false);
        taskInspectionReports.Add(inspectionSource, inspection);
        RegisterSource(inspectionSource);
        Report(completed ? "Task draft inspected. No task was created or run." : "Partial task inspection retained. No task was created or run.");
        SetScanLight("incomplete", "Draft inspection only; task execution is not verified.");
        }
    }

    private async void OnInspectTaskDraft(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(85));
        try
        {
            var plan = ReadTaskDraftForm();
            readCancellation = stop;
            SetCaptureBusy(true);
            await InspectTaskDraftAsync(plan, stop.Token);
        }
        catch (OperationCanceledException) { Report("Task draft inspection canceled or timed out."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or FormatException or OverflowException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Report("Cannot inspect task draft: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnReviewTaskJournal(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new OpenFileDialog { Title = "Select one recovery record or an intent/outcome pair", Filter = "Task journal (*.json)|*.json", CheckFileExists = true, Multiselect = true };
        if (picker.ShowDialog(this) != true) return;
        if (picker.FileNames.Length is < 1 or > 2) { Report("Select one recovery record or two linked records."); return; }
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            Report("Reading task recovery record...");
            var review = await Task.Run(() => TaskJournalReview.ReadAsync(picker.FileName, stop.Token));
            if (picker.FileNames.Length == 2)
            {
                string first = picker.FileNames[0], second = picker.FileNames[1];
                review = await Task.Run(() => review.State is "DefinitionVerified" or "NotDispatched"
                    ? TaskJournalReview.ReadPairAsync(second, first, stop.Token)
                    : TaskJournalReview.ReadPairAsync(first, second, stop.Token));
            }
            string output = "TASK RECOVERY / " + review.State + "\n" + review.Detail +
                "\nNo task changed. Retry is not authorized by this record.\n\n" +
                (review.RecordedEvidence is { } recorded ? JsonSerializer.Serialize(recorded, Capture.JsonOptions) : "No validated record available.");
            RegisterSource(new TextEvidence("Task recovery: " + string.Join(" + ", picker.FileNames.Select(System.IO.Path.GetFileName)), output.Split('\n'), false));
            SetScanLight("incomplete", "Historical recovery evidence; current task state is unverified.");
            Report("Task recovery review: " + review.State + ". No task changed.");
        }
        catch (OperationCanceledException) { Report("Task recovery review canceled or timed out."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report("Cannot review task recovery record: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnReviewTaskPlan(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new OpenFileDialog { Title = "Review saved task plan", Filter = "Task plan (*.json)|*.json", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            Report("Reading task draft...");
            var plan = await Task.Run(() => ScheduledTaskPlanJson.ReadFileAsync(picker.FileName, stop.Token));
            await InspectTaskDraftAsync(plan, stop.Token);
        }
        catch (OperationCanceledException) { Report("Task draft read canceled or timed out."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Report("Cannot review task draft: " + error.Message); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }
}
