using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

internal static class CapabilityDesktopChecks
{
    internal static int Run()
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var window = new KillerBench.Shell.MainWindow();
        try
        {
            T Get<T>(string name) => (T)window.FindName(name);
            Get<ComboBox>("MainWorkflow").SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.CommonProblems);
            Get<CheckBox>("CustomizeRecipes").IsChecked = true;
            Require(Get<TextBlock>("RecipeEffectsSummary").Text.Contains("Native lab:", StringComparison.Ordinal) &&
                Get<TextBlock>("RecipeEffectsSummary").Text.Contains("Not yet verified", StringComparison.Ordinal),
                "Recipe effect summaries must display the shared native lab label before selection or execution");
            Button repairs = Descendants(window).OfType<Button>().Single(button => Equals(button.Content, application.FindResource("Str_Advanced_Repairs")));
            repairs.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Get<TextBox>("RepairPreview").Text.Contains("Native lab:", StringComparison.Ordinal), "Focused repair previews show the native lab label");
            Get<ComboBox>("RepairChoice").SelectedIndex = 1;
            Require(Get<TextBox>("RepairPreview").Text.Contains("Not yet verified", StringComparison.Ordinal), "Changing the focused repair preserves its current label");
            Get<ComboBox>("MainWorkflow").SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.ScheduledTasks);
            Require(Get<TextBlock>("NativeCapabilityStatus").Text.Contains("Not yet verified", StringComparison.Ordinal) &&
                Get<TextBlock>("NativeCapabilityStatus").ToolTip is string status && status.Contains("command.task-register", StringComparison.Ordinal),
                "Main workflow status maps dedicated task registration to the shared native capability");
            Get<Expander>("TaskDraftExpander").RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Get<TextBox>("TaskDraftName").Text = "Capability fixture";
            Get<TextBox>("TaskDraftExecutable").Text = "C:\\Fixture\\tool.exe";
            Get<TextBox>("TaskDraftUser").Text = "FixtureUser";
            Descendants(window).OfType<Button>().Single(button => Equals(button.Content, "Review draft")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Get<TextBox>("EvidenceBox").Text.Contains("Native lab: command.task-register", StringComparison.Ordinal) &&
                Get<TextBox>("RepairPreview").Text.Contains("Native lab capability status", StringComparison.Ordinal),
                "Real draft review retains native capability labels in both raw output and the readable result without registration");
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/Strings/de-DE.xaml", UriKind.Relative) });
            Get<ComboBox>("MainWorkflow").SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.CommonProblems);
            Get<ComboBox>("MainWorkflow").SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.ScheduledTasks);
            Require(Get<TextBlock>("NativeCapabilityStatus").Text.StartsWith("Natives Testlabor:", StringComparison.Ordinal) &&
                Get<TextBlock>("NativeCapabilityStatus").ToolTip is string localized && localized.Contains("Noch nicht", StringComparison.Ordinal),
                "Changing language must localize the visible native status and its tooltip together");
            Require(!window.IsVisible && new WindowInteropHelper(window).Handle == IntPtr.Zero,
                "Capability desktop checks must never show a window or create a native handle");
            Console.WriteLine("PASS: native capability labels appear in real never-shown desktop selection handlers. No native action or window handle.");
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
}
