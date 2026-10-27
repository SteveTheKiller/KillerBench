using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using KillerBench.Engine;

internal static class DesktopOperationStateChecks
{
    internal static int Run(string[] args)
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var window = new KillerBench.Shell.MainWindow();
        try
        {
            T Get<T>(string name) => (T)window.FindName(name);
            Get<ComboBox>("MainWorkflow").SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.ScheduledTasks);
            Get<Expander>("TaskDraftExpander").RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Get<TextBox>("TaskDraftName").Text = "First fixture";
            Get<TextBox>("TaskDraftExecutable").Text = "C:\\Fixture\\tool.exe";
            Get<TextBox>("TaskDraftUser").Text = "FixtureUser";
            Button review = Descendants(window).OfType<Button>().Single(button => Equals(button.Content, "Review draft"));
            review.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Get<ListBox>("SourcesList").IsEnabled && Get<TextBox>("SearchBox").IsEnabled,
                "S005: source navigation and search must be enabled after synchronous task review.");
            Require(Get<TextBox>("EvidenceBox").Text.Contains("First fixture"), "The actual draft action must produce the requested report.");
            Get<TextBox>("TaskDraftMinutes").Text = "0";
            review.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Get<ListBox>("SourcesList").IsEnabled && Get<TextBox>("SearchBox").IsEnabled,
                "S005: an invalid draft after a valid review must leave navigation and search usable.");
            Get<TextBox>("TaskDraftMinutes").Text = "60";
            application.Resources["Str_Output_WorkflowSource"] = "Fixed display source";
            review.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            int firstCount = Get<ListBox>("SourcesList").Items.Count;
            Get<TextBox>("TaskDraftName").Text = "Second fixture";
            review.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var sources = Get<ListBox>("SourcesList");
            Require(sources.Items.Count == firstCount + 1,
                "S006: distinct completed runs must remain selectable even with identical display labels and clock values.");
            sources.SelectedIndex = firstCount - 1;
            Require(Get<TextBox>("EvidenceBox").Text.Contains("First fixture") && !Get<TextBox>("EvidenceBox").Text.Contains("Second fixture"),
                "S006: selecting the earlier run must preserve its own transcript.");
            sources.SelectedIndex = sources.Items.Count - 1;
            Require(Get<TextBox>("EvidenceBox").Text.Contains("Second fixture"), "S006: the later run must retain its own transcript.");
            Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
            Get<TextBox>("TaskDraftName").Text = "Changed after confirmation";
            Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false &&
                Equals(Get<Button>("TaskRegistrationButton").Content, "Preview registration"),
                "S009: a draft edit must clear registration consent and restore the preview action.");
            foreach (string name in new[] { "TaskDraftFolder", "TaskDraftName", "TaskDraftExecutable", "TaskDraftArguments",
                "TaskDraftDirectory", "TaskDraftTime", "TaskDraftUser", "TaskDraftMinutes", "TaskDraftStartDate" })
            {
                TextBox field = Get<TextBox>(name);
                string previous = field.Text;
                Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
                field.Text += "x";
                Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false, "S009: text edit retained consent: " + name);
                field.Text = previous;
            }
            foreach (string name in new[] { "TaskDraftSchedule", "TaskDraftZone", "TaskDraftAccountMode", "TaskDraftOverlap" })
            {
                ComboBox field = Get<ComboBox>(name);
                int previous = field.SelectedIndex;
                Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
                field.SelectedIndex = (previous + 1) % field.Items.Count;
                Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false, "S009: selection edit retained consent: " + name);
                field.SelectedIndex = previous;
            }
            foreach (string name in new[] { "TaskDraftWeekdays", "TaskDraftMonthDays" })
            {
                Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
                Get<ListBox>(name).SelectedIndex = 0;
                Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false, "S009: schedule-day edit retained consent: " + name);
                Get<ListBox>(name).SelectedIndex = -1;
            }
            foreach (string name in new[] { "TaskDraftHighest", "TaskDraftLastDay" })
            {
                Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
                Get<CheckBox>(name).IsChecked = true;
                Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false, "S009: checked option retained consent: " + name);
                Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
                Get<CheckBox>(name).IsChecked = false;
                Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false, "S009: unchecked option retained consent: " + name);
            }
            Get<TextBox>("TaskDraftMinutes").Text = "invalid";
            Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
            Get<Button>("TaskRegistrationButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Get<CheckBox>("TaskRegistrationConsent").IsChecked == false,
                "S009: rejected form validation must clear consent before another attempt. No host registration is reached.");
            Get<TextBox>("TaskDraftMinutes").Text = "60";
            Get<Button>("TaskRegistrationButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            PumpUntil(() => Get<StackPanel>("TaskSettings").IsEnabled);
            Require(Get<TextBox>("EvidenceBox").Text.Contains("Task registration preview") &&
                Get<ListBox>("SourcesList").IsEnabled && Get<TextBox>("SearchBox").IsEnabled &&
                Get<CheckBox>("TaskRegistrationConsent").IsChecked == false,
                "S005/S009: registration preview must restore controls and consume confirmation without probing, journaling or registering.");
            Get<TextBox>("TaskDraftName").Text = "No-pending-fixture-" + Guid.NewGuid().ToString("N");
            Get<CheckBox>("TaskRegistrationConsent").IsChecked = true;
            Get<Button>("TaskRegistrationRecoveryButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            PumpUntil(() => Get<StackPanel>("TaskSettings").IsEnabled);
            Require(Get<TextBox>("EvidenceBox").Text.Contains("NOPENDINGRECORD") &&
                Get<ListBox>("SourcesList").IsEnabled && Get<TextBox>("SearchBox").IsEnabled &&
                Get<CheckBox>("TaskRegistrationConsent").IsChecked == false,
                "S005/S009: read-only recovery for an absent random pending record must restore controls and clear confirmation.");
            string folder = Path.Combine(Path.GetTempPath(), "KillerBench-desktop-state-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                Get<TextBox>("FileReviewFolder").Text = folder;
                Get<Button>("ReviewLargeFilesButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                PumpUntil(() => Get<Button>("ReviewLargeFilesButton").IsEnabled);
                Require(Get<TextBox>("RepairPreview").Text.Contains("Large file review complete"), "The real large-file action must establish the prior successful report.");
                var successfulSource = (ListBoxItem)sources.SelectedItem;
                Get<TextBox>("FileReviewFolder").Text = Path.Combine(folder, "missing");
                Get<Button>("ReviewLargeFilesButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                PumpUntil(() => Get<Button>("ReviewLargeFilesButton").IsEnabled);
                Require(!ReferenceEquals(sources.SelectedItem, successfulSource) && Get<TextBox>("RepairPreview").Text.Contains("Review coverage: Unknown"),
                    "S010: failed file review must publish its own current incomplete report instead of retaining a successful report.");
                Require(Get<Border>("ScanLight").ToolTip is string unavailable && unavailable.Contains("unavailable"),
                    "S010: an unavailable review must replace the previous ready indicator.");
                foreach (string button in new[] { "ReviewLargeFilesButton", "ReviewDuplicatesButton" })
                {
                    Get<TextBox>("FileReviewFolder").Text = folder;
                    object prior = sources.SelectedItem;
                    Get<Button>(button).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Require(Get<Button>("CancelButton").IsEnabled, "A pending file review must accept stop.");
                    Get<Button>("CancelButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    PumpUntil(() => Get<Button>(button).IsEnabled);
                    Require(!ReferenceEquals(sources.SelectedItem, prior) && Get<TextBox>("RepairPreview").Text.Contains("canceled") &&
                        Get<TextBox>("RepairPreview").Text.Contains("Review coverage: Unknown") &&
                        Get<Border>("ScanLight").ToolTip is string canceled && canceled.Contains("canceled"),
                        "S010: stopping either file review must select its own incomplete report and indicator.");
                    var canceledSource = (ListBoxItem)sources.SelectedItem;
                    sources.SelectedItem = successfulSource;
                    Require(Get<TextBox>("EvidenceBox").Text.Contains("LARGE FILE REVIEW") && !Get<TextBox>("EvidenceBox").Text.Contains("canceled"),
                        "S010: prior successful evidence must remain separately selectable.");
                    sources.SelectedItem = canceledSource;
                    Require(Get<TextBox>("EvidenceBox").Text.Contains("canceled"), "S010: canceled raw evidence must remain tied to its own selected source.");
                }
            }
            finally { Directory.Delete(folder, true); }
            Require(!window.IsVisible && new WindowInteropHelper(window).Handle == IntPtr.Zero,
                "Desktop state checks must never show a window or create its native handle.");
            Console.WriteLine("PASS S005/S006/S009/S010: real never-shown actions restore navigation, retain identical-label runs, reset all draft consent inputs, and select failed/canceled file-review evidence. No native window, task registration or screen capture.");
            return 0;
        }
        finally { window.Close(); application.Shutdown(); }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(parent))
            if (child is DependencyObject item)
            {
                yield return item;
                foreach (DependencyObject descendant in Descendants(item)) yield return descendant;
            }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void PumpUntil(Func<bool> completed)
    {
        var frame = new DispatcherFrame();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) => { if (completed() || elapsed.Elapsed > TimeSpan.FromSeconds(10)) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Require(completed(), "The never-shown operation did not reach its terminal state in ten seconds.");
    }
}
