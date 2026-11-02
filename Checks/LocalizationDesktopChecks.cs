using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using KillerBench.Engine;
using KillerBench.Services;

internal static class LocalizationDesktopChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        var application = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        ThemeManager.GetSetting = _ => null;
        ThemeManager.SetSetting = (_, _) => { };
        ThemeManager.Initialize();
        var window = new KillerBench.Shell.MainWindow
        {
            ShowActivated = false, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = SystemParameters.VirtualScreenLeft - 2000, Top = SystemParameters.VirtualScreenTop
        };
        try
        {
            window.Show();
            var assembly = typeof(KillerBench.Shell.MainWindow).Assembly;
            var localeType = assembly.GetType("KillerBench.Services.Locale")!;
            var manager = assembly.GetType("KillerBench.Services.LocaleManager")!;
            var apply = manager.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            var refresh = typeof(KillerBench.Shell.MainWindow).GetMethod("RefreshLanguage", BindingFlags.Instance | BindingFlags.NonPublic)!;
            string englishInvalidCode = (string)window.FindResource("Str_Tkp_EnterWindowsCode");
            int checkedLocales = 0;
            foreach (var locale in Enum.GetValues(localeType))
            {
                apply.Invoke(null, [locale]);
                refresh.Invoke(window, []);
                var categories = (ListBox)window.FindName("CategoryList");
                categories.SelectedIndex = 0;
                window.UpdateLayout();
                Require(((TextBlock)window.FindName("CategoryTitle")).Text == (string)window.FindResource("Str_Category_Diagnostics"), "Category follows " + locale);
                Require(AutomationProperties.GetName((Button)window.FindName("ExportReportButton")) == (string)window.FindResource("Str_ExportReport"), "Export accessible label follows " + locale);
                ((Expander)window.FindName("KnowledgeTools")).IsExpanded = true;
                ((TextBox)window.FindName("KnowledgeCode")).Text = "not-a-code";
                ((MenuItem)window.FindName("WindowsCodeAction")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                string status = ((TextBlock)window.FindName("StatusLabel")).Text;
                Require(status == (string)window.FindResource("Str_Tkp_EnterWindowsCode"), "Windows code validation follows " + locale);
                Require(locale.ToString() == "EnUS" || status != englishInvalidCode, "Non-English validation does not silently fall back in " + locale);
                categories.SelectedIndex = 3;
                ((TextBox)window.FindName("KnowledgeCode")).Text = "-1";
                ((MenuItem)window.FindName("DeviceCodeAction")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Require(((TextBlock)window.FindName("StatusLabel")).Text == (string)window.FindResource("Str_Tkp_EnterDeviceCode"), "Device code validation follows " + locale);
                var report = new WorkflowResult(WorkflowKind.Privacy, "LANGUAGE-FIXTURE", DateTimeOffset.UtcNow,
                    "No issues detected in checked scopes", [new("Fixture", CheckState.Clear, "Read-only fixture")], [], "read-only");
                typeof(KillerBench.Shell.MainWindow).GetMethod("RenderNamedWorkflowReport", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [report, null]);
                window.UpdateLayout();
                Require(Descendants((FrameworkElement)window.FindName("ReportContent")).OfType<TextBlock>().Any(block => block.Text == (string)window.FindResource("Str_Guidance_Privacy")),
                    "Engine next-step guidance is localized in the real report in " + locale);
                Require(Descendants(window).OfType<Button>().Where(button => button.IsVisible).All(button => button.Content is not string text || !text.StartsWith("Str_", StringComparison.Ordinal)), "Visible buttons resolve keys in " + locale);
                checkedLocales++;
            }
            Require(checkedLocales == 20, "All twenty application locales were exercised");
            Console.WriteLine("PASS: twenty runtime languages, category navigation, accessible export labels and real read-only validation handlers. No provider or repair ran.");
            return 0;
        }
        finally { window.Close(); application.Shutdown(); }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
