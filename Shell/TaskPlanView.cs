using System.IO;
using System.Text.Json;
using System.Windows;
using KillerBench.Engine;
using L = KillerBench.Controls.LocalizedText;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextEvidence, ScheduledTaskInspectionReport> taskInspectionReports = [];

    private static WorkflowResult BuildTaskInspectionWorkflowReport(ScheduledTaskInspectionReport inspection)
    {
        var checks = new List<WorkflowCheck>
        {
            new(L.Get("Str_Task_Name"), CheckState.Information, L.Format("Str_Tkp_TaskTarget", inspection.Plan.Identity.FullName)),
            new(L.Get("Str_Task_Executable"), inspection.Executable is null || inspection.Executable.RequiresReview ? CheckState.Unknown : CheckState.Information,
                inspection.Executable?.ImageReview ?? L.Format("Str_Tkp_ObservationMissing", L.Get("Str_Task_Executable"))),
            new(L.Get("Str_Tkp_Signature"), inspection.Signature?.Status == "Valid" ? CheckState.Information : inspection.Signature is null ? CheckState.Unknown : CheckState.Attention,
                inspection.Signature is null ? L.Format("Str_Tkp_ObservationMissing", L.Get("Str_Tkp_Signature")) : L.Format("Str_Tkp_RecordedStatus", inspection.Signature.Status, inspection.Signature.Detail)),
            new(L.Get("Str_Task_UserAccount"), inspection.Account?.Sid is null ? CheckState.Unknown : CheckState.Information,
                inspection.Account?.Detail ?? L.Format("Str_Tkp_ObservationMissing", L.Get("Str_Task_UserAccount"))),
            new(L.Get("Str_Tkp_TaskConflict"), inspection.Conflict?.State switch
                {
                    ScheduledTaskConflictState.ExistingTask => CheckState.Attention,
                    ScheduledTaskConflictState.NotObserved => CheckState.Information,
                    _ => CheckState.Unknown
                }, inspection.Conflict?.Detail ?? L.Format("Str_Tkp_ObservationMissing", L.Get("Str_Tkp_TaskConflict")))
        };
        if (inspection.Incomplete) checks.Add(new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Unknown,
            inspection.Failure ?? L.Get("Str_Report_Incomplete")));
        return new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, inspection.InspectedUtc,
            L.Format("Str_Ui_Error", L.Get("Str_Task_InspectDraft"), L.Get(inspection.Incomplete ? "Str_Report_Incomplete" : "Str_Report_Checked")), checks, [],
            L.Get("Str_Tkp_TaskNoJournal"));
    }
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
        TaskDraftZone?.IsEnabled = TaskDraftSchedule.SelectedIndex != 3;
    }

    private void OnTaskRegistrationConsentChanged(object sender, RoutedEventArgs e)
    {
        TaskRegistrationButton?.SetResourceReference(System.Windows.Controls.ContentControl.ContentProperty,
            TaskRegistrationConsent.IsChecked == true ? "Str_Ui_CreateDisabledTask" : "Str_Task_PreviewRegistration");
    }

    private void InitializeTaskDraftConsent()
    {
        RefreshTaskDraftWeekdayLabels();
        Services.LocaleManager.LocaleChanged += RefreshTaskDraftWeekdayLabels;
        Services.LocaleManager.ExternalReloaded += RefreshTaskDraftWeekdayLabels;
        Closed += (_, _) =>
        {
            Services.LocaleManager.LocaleChanged -= RefreshTaskDraftWeekdayLabels;
            Services.LocaleManager.ExternalReloaded -= RefreshTaskDraftWeekdayLabels;
        };
        TaskDraftExpander.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new System.Windows.Controls.TextChangedEventHandler(OnTaskDraftInputChanged), true);
        TaskDraftExpander.AddHandler(System.Windows.Controls.Primitives.Selector.SelectionChangedEvent,
            new System.Windows.Controls.SelectionChangedEventHandler(OnTaskDraftInputChanged), true);
        TaskDraftExpander.AddHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent,
            new RoutedEventHandler(OnTaskDraftInputChanged), true);
        TaskDraftExpander.AddHandler(System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent,
            new RoutedEventHandler(OnTaskDraftInputChanged), true);
    }

    private void RefreshTaskDraftWeekdayLabels()
    {
        var label = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
        label.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
            new System.Windows.Data.Binding { Converter = new TaskDraftWeekdayConverter() });
        TaskDraftWeekdays.ItemTemplate = new DataTemplate { VisualTree = label };
    }

    private sealed class TaskDraftWeekdayConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is DayOfWeek day
                ? Services.LocaleManager.CultureFor(Services.LocaleManager.Current).DateTimeFormat.GetDayName(day)
                : value?.ToString() ?? "";

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

    private void OnTaskDraftInputChanged(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, TaskRegistrationConsent))
            TaskRegistrationConsent.IsChecked = false;
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
            string status = L.Get("Str_Tkp_DraftReviewBoundary");
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, DateTimeOffset.UtcNow,
                status,
                [new WorkflowCheck(L.Get("Str_Task_Name"), CheckState.Information, L.Format("Str_Tkp_TaskTarget", plan.Identity.FullName)),
                 new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Information,
                    L.Get("Str_Tkp_DraftReviewBoundary"))], [],
                L.Get("Str_Tkp_TaskNoJournal")), L.Get("Str_Task_ReviewDraft"));
            Report(status);
            SetScanLight("incomplete", L.Get("Str_Tkp_DraftOnly"));
        }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_ReviewDraft"), error.Message)); }
        finally { CompleteWorkflowOutput(); }
    }

    private async void OnSaveTaskDraft(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        try
        {
            byte[] data = ScheduledTaskPlanJson.Serialize(ReadTaskDraftForm());
            var picker = new Controls.FileDialog(Controls.FileDialogMode.Save) { Title = Controls.LocalizedText.Get("Str_Task_SaveDraft"), Filter = "JSON (*.json)|*.json", DefaultExt = ".json", FileName = "task-plan.json" };
            if (picker.ShowDialog(this) != true) return;
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            readCancellation = stop;
            SetCaptureBusy(true);
            try
            {
                await using var file = new FileStream(picker.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                await file.WriteAsync(data, stop.Token);
                await file.FlushAsync(stop.Token);
                Report(L.Get("Str_Tkp_DraftSaved"));
            }
            finally { readCancellation = null; SetCaptureBusy(false); }
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_FileSaveStopped", L.Get("Str_Task_SaveDraft"))); }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_SaveDraft"), L.Format("Str_Tkp_WriteError", error.Message))); }
    }

    private async void OnExportTaskXml(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        try
        {
            var startDate = DateOnly.ParseExact(TaskDraftStartDate.Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(ScheduledTaskXml.Serialize(ReadTaskDraftForm(), startDate));
            var picker = new Controls.FileDialog(Controls.FileDialogMode.Save) { Title = Controls.LocalizedText.Get("Str_Task_ExportXml"), Filter = "XML (*.xml)|*.xml", DefaultExt = ".xml", FileName = "task-review.xml" };
            if (picker.ShowDialog(this) != true) return;
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            readCancellation = stop;
            SetCaptureBusy(true);
            try
            {
                await using var file = new FileStream(picker.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                await file.WriteAsync(data, stop.Token);
                await file.FlushAsync(stop.Token);
                Report(L.Get("Str_Tkp_XmlSaved"));
            }
            finally { readCancellation = null; SetCaptureBusy(false); }
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_FileSaveStopped", L.Get("Str_Task_ExportXml"))); }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException or NotSupportedException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_ExportXml"), L.Format("Str_Tkp_WriteError", error.Message))); }
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
            repairRunning = execute;
            SetCaptureBusy(true);
            BeginWorkflowOutput(WorkflowKind.ScheduledTasks);
            AppendWorkflowOutput(execute
                ? "TASK REGISTRATION / GUARDED CREATE"
                : "TASK REGISTRATION / PREVIEW");
            var progress = new Progress<string>(AppendWorkflowOutput);
            var result = await Task.Run(() => TaskRegistration.RunAsync(plan,
                new WindowsTaskRegistrationHost(startDate), execute, new FileTaskRegistrationJournal(journalFolder, startDate),
                progress: progress, token: stop.Token));
            string output = "TASK REGISTRATION / " + result.Status.ToUpperInvariant() +
                "\nTarget: " + plan.Identity.FullName +
                "\nCreate attempted: " + (result.CreateAttempted ? "Yes" : "No") +
                "\nDefinition enabled: No\nTask executed: No\n\n" + result.Detail +
                "\n\nRecovery folder: " + journalFolder;
            AppendWorkflowOutput(output);
            bool registered = result.Status == "Registered";
            bool preview = result.Status == "Preview";
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, DateTimeOffset.UtcNow,
                L.Format("Str_Ui_Error", L.Get("Str_Tkp_Registration"), L.Get(registered ? "Str_Tkp_Verified" : preview ? "Str_Task_PreviewRegistration" : "Str_Report_NeedsReview")),
                [new WorkflowCheck(L.Get("Str_Ui_CreateDisabledTask"), registered || preview ? CheckState.Information : CheckState.Unknown, result.Detail),
                 new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Information,
                    L.Get("Str_Tkp_DisabledBoundary"))], [], journalFolder), L.Get("Str_Tkp_Registration"));
            Report(L.Format("Str_Ui_Error", L.Get("Str_Tkp_Registration"), result.Detail));
            SetScanLight(result.Status == "Registered" ? "ready" :
                result.Status is "Blocked" or "Unverified" ? "attention" : "incomplete", L.Format("Str_Ui_Error", L.Get("Str_Tkp_Registration"), result.Detail));
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Error", L.Get("Str_Tkp_Registration"), L.Get("Str_Tkp_GuardStopped"))); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            FormatException or OverflowException or NotSupportedException or InvalidOperationException or
            System.ComponentModel.Win32Exception)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Tkp_Registration"), error.Message)); }
        finally
        {
            TaskRegistrationConsent.IsChecked = false;
            workflowOutputActive = false;
            readCancellation = null;
            repairRunning = false;
            SetCaptureBusy(false);
            if (closeAfterOperation) Close();
        }
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
                new WindowsTaskRegistrationRecoveryHost(), progress: progress, token: stop.Token));
            string output = "TASK REGISTRATION RECOVERY / " + result.Outcome.ToString().ToUpperInvariant() +
                "\nTarget: " + plan.Identity.FullName +
                "\nRetry attempted: No\nTask changed: No\nTask executed: No\n\n" + result.Detail +
                "\n\nPending or archived record: " + (result.PendingRecord ?? "None") +
                "\nRecovery record: " + (result.RecoveryRecord ?? "None");
            AppendWorkflowOutput(output);
            bool verified = result.Outcome == TaskRegistrationRecoveryOutcome.Verified;
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, DateTimeOffset.UtcNow,
                L.Format("Str_Ui_Error", L.Get("Str_Task_CheckInterrupted"), L.Get(verified ? "Str_Tkp_Verified" : "Str_Report_NeedsReview")),
                [new WorkflowCheck(L.Get("Str_Task_CheckInterrupted"), verified ? CheckState.Information : CheckState.Unknown, result.Detail),
                 new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Information,
                    L.Get("Str_Tkp_RecoveryBoundary"))], [],
                result.RecoveryRecord ?? result.PendingRecord ?? L.Get("Str_Tkp_NoRecoveryRecord")), L.Get("Str_Task_CheckInterrupted"));
            Report(L.Format("Str_Ui_Error", L.Get("Str_Task_CheckInterrupted"), result.Detail));
            SetScanLight(result.Outcome == TaskRegistrationRecoveryOutcome.Verified ? "ready" :
                result.Outcome is TaskRegistrationRecoveryOutcome.Conflict or TaskRegistrationRecoveryOutcome.Inconclusive
                    ? "attention" : "incomplete", L.Format("Str_Ui_Error", L.Get("Str_Task_CheckInterrupted"), result.Detail));
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_CheckInterrupted"), L.Get("Str_Tkp_GuardStopped"))); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            FormatException or OverflowException or NotSupportedException or InvalidOperationException or
            System.ComponentModel.Win32Exception or System.Security.SecurityException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_CheckInterrupted"), error.Message)); }
        finally
        {
            TaskRegistrationConsent.IsChecked = false;
            readCancellation = null;
            SetCaptureBusy(false);
        }
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
        Report(L.Format("Str_Tkp_ExecutableReading", L.Get("Str_Task_Executable")));
        executable = await Task.Run(() => ScheduledTaskExecutable.InspectAsync(plan.Action, token));
        Report(L.Format("Str_Tkp_ExecutableReading", L.Get("Str_Tkp_Signature")));
        signature = await FileSignature.InspectAsync(plan.Action, token);
        Report(L.Format("Str_Ui_Reading", L.Get("Str_Task_UserAccount")));
        account = await ScheduledTaskAccount.ResolveAsync(plan.Principal, token);
        Report(L.Format("Str_Ui_Reading", L.Get("Str_Tkp_TaskConflict")));
        inventory = await ScheduledTaskInventory.ReadAsync(token);
        conflict = ScheduledTaskConflict.Review(plan.Identity, inventory);
        if (conflict.State == ScheduledTaskConflictState.ExistingTask)
        {
            Report(L.Format("Str_Ui_Reading", L.Get("Str_Task_InspectDraft")));
            definition = await ScheduledTaskDefinition.ReadAsync(plan.Identity, token);
        }
        completed = true;
        }
        catch (Exception error)
        {
            failure = error is OperationCanceledException ? L.Get("Str_Tkp_ObservationsRetained") : error.Message;
            throw;
        }
        finally
        {
        var inspection = new ScheduledTaskInspectionReport(DateTimeOffset.UtcNow, plan, executable, account, conflict, inventory?.Evidence)
        { Signature = signature, ExistingDefinition = definition, Incomplete = !completed, Failure = failure };
        var inspectionSource = new TextEvidence(L.Format("Str_Ui_Error", L.Get("Str_Task_NewDraft"), plan.Identity.FullName),
            inspection.Format().Split('\n'), false);
        var report = BuildTaskInspectionWorkflowReport(inspection);
        taskInspectionReports.Add(inspectionSource, inspection);
        workflowSummaries.Add(inspectionSource, report);
        workflowReportTitles.Add(inspectionSource, L.Get("Str_Task_InspectDraft"));
        RegisterSource(inspectionSource);
        ShowEvidence();
        RenderNamedWorkflowReport(report, L.Get("Str_Task_InspectDraft"));
        SetOutputView();
        Report(L.Format("Str_Ui_Error", L.Get(completed ? "Str_Report_Checked" : "Str_Report_Incomplete"), L.Get("Str_Tkp_InspectionRetained")));
        SetScanLight("incomplete", L.Get("Str_Tkp_DraftOnly"));
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
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Stopped", L.Get("Str_Task_InspectDraft"))); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or FormatException or OverflowException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_InspectDraft"), error.Message)); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnReviewTaskJournal(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Title = Controls.LocalizedText.Get("Str_Task_ReviewRecovery"), Filter = "JSON (*.json)|*.json", CheckFileExists = true, Multiselect = true };
        if (picker.ShowDialog(this) != true) return;
        if (picker.FileNames.Length is < 1 or > 2) { Report(L.Get("Str_Tkp_SelectRecords")); return; }
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            Report(L.Format("Str_Ui_Reading", L.Get("Str_Task_ReviewRecovery")));
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
            BeginWorkflowOutput(WorkflowKind.ScheduledTasks);
            AppendWorkflowOutput(output);
            RegisterWorkflowResult(new WorkflowResult(WorkflowKind.ScheduledTasks, Environment.MachineName, DateTimeOffset.UtcNow,
                L.Get("Str_Task_ReviewRecovery"),
                [new WorkflowCheck(L.Get("Str_Task_ReviewRecovery"), CheckState.Unknown, review.Detail),
                 new WorkflowCheck(L.Get("Str_Report_ScopeAndLimits"), CheckState.Information,
                    L.Get("Str_Tkp_HistoricalBoundary"))], [],
                L.Get("Str_Tkp_HistoricalBoundary")), L.Get("Str_Task_ReviewRecovery"));
            SetScanLight("incomplete", L.Get("Str_Tkp_HistoricalBoundary"));
            Report(L.Format("Str_Ui_Error", review.State, L.Get("Str_Tkp_HistoricalBoundary")));
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Stopped", L.Get("Str_Task_ReviewRecovery"))); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_ReviewRecovery"), error.Message)); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }

    private async void OnReviewTaskPlan(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        var picker = new Controls.FileDialog(Controls.FileDialogMode.Open) { Title = Controls.LocalizedText.Get("Str_Task_ReviewSavedPlan"), Filter = "JSON (*.json)|*.json", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        using var stop = new CancellationTokenSource();
        readCancellation = stop;
        SetCaptureBusy(true);
        try
        {
            Report(L.Format("Str_Ui_Reading", L.Get("Str_Task_ReviewSavedPlan")));
            var plan = await Task.Run(() => ScheduledTaskPlanJson.ReadFileAsync(picker.FileName, stop.Token));
            await InspectTaskDraftAsync(plan, stop.Token);
        }
        catch (OperationCanceledException) { Report(L.Format("Str_Ui_Stopped", L.Get("Str_Task_ReviewSavedPlan"))); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Report(L.Format("Str_Ui_Error", L.Get("Str_Task_ReviewSavedPlan"), error.Message)); }
        finally { readCancellation = null; SetCaptureBusy(false); }
    }
}
