using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using KillerBench.Engine;

internal static class TaskDraftUiChecks
{
    internal static void Run(KillerBench.MainWindow window)
    {
        T Get<T>(string name) => (T)window.FindName(name);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Invoke(string method) => typeof(KillerBench.MainWindow).GetMethod(method, flags)!.Invoke(window, [window, new RoutedEventArgs()]);
        Get<ComboBox>("MainWorkflow").SelectedItem = WorkflowCatalog.All.Single(x => x.Kind == WorkflowKind.ScheduledTasks);
        window.UpdateLayout();
        var dimensions = new Size(window.ActualWidth, window.ActualHeight);
        Get<Expander>("TaskDraftExpander").IsExpanded = true;
        Invoke("OnTaskDraftLoaded");
        Get<TextBox>("TaskDraftName").Text = "Fixture";
        Get<TextBox>("TaskDraftExecutable").Text = "C:\\Fixture\\tool.exe";
        Get<TextBox>("TaskDraftUser").Text = "FixtureUser";
        var schedule = Get<ComboBox>("TaskDraftSchedule");
        string Output() => Get<TextBox>("EvidenceBox").Text;
        foreach (int kind in Enumerable.Range(0, 4))
        {
            schedule.SelectedIndex = kind;
            if (kind == 1)
            {
                Get<ListBox>("TaskDraftWeekdays").SelectedItems.Add(DayOfWeek.Monday);
                Get<ListBox>("TaskDraftWeekdays").SelectedItems.Add(DayOfWeek.Friday);
            }
            if (kind == 2) Get<ListBox>("TaskDraftMonthDays").SelectedItems.Add(31);
            Invoke("OnReviewTaskDraft");
            string expected = new[] { "Every day", "Monday, Friday", "Month days 31", "At machine startup" }[kind];
            if (!Output().Contains(expected) || !Output().Contains("No task was created or run.")) throw new Exception("Task form lost schedule: " + kind);
            window.UpdateLayout();
            if (new Size(window.ActualWidth, window.ActualHeight) != dimensions) throw new Exception("Task form resized the window");
        }
        schedule.SelectedIndex = 2;
        Get<CheckBox>("TaskDraftLastDay").IsChecked = true;
        Invoke("OnReviewTaskDraft");
        if (!Output().Contains("Last day of each month")) throw new Exception("Last-day form option ignored");
        Get<ComboBox>("TaskDraftAccountMode").SelectedIndex = 1;
        string prior = Output();
        Invoke("OnReviewTaskDraft");
        if (Output() != prior) throw new Exception("System draft bypassed explicit privilege choice");
        Get<CheckBox>("TaskDraftHighest").IsChecked = true;
        Invoke("OnReviewTaskDraft");
        if (!Output().Contains("S-1-5-18") || !Output().Contains("Highest privileges: Selected")) throw new Exception("System draft lost identity/privilege");
        prior = Output();
        Get<TextBox>("TaskDraftMinutes").Text = "0";
        Invoke("OnReviewTaskDraft");
        if (Output() != prior) throw new Exception("Invalid duration produced a draft");
        Get<Expander>("TaskDraftExpander").IsExpanded = false;
        Console.WriteLine("PASS: task form schedule selections, last day, explicit System privileges, invalid duration and stable dimensions; no tasks run.");
    }
}
