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
            var navigation = (ListBox)window.FindName("WorkspaceNavigation");
            var destinations = navigation.Items.OfType<ListBoxItem>().ToArray();
            var investigate = destinations.Single(item => Equals(item.Tag, "Investigate"));
            var tools = destinations.Single(item => Equals(item.Tag, "Tools"));
            var historyPage = destinations.Single(item => Equals(item.Tag, "History"));
            var investigationAreas = (ListBox)window.FindName("InvestigationAreas");
            var history = (ListBox)window.FindName("SessionHistory");
            var languageButton = (Button)window.FindName("LangButton");
            var languages = OpenLanguageMenu(languageButton);
            var localeNames = LanguageChoices(languages).Select(item => item.Tag.ToString()).ToArray();
            languages.IsOpen = false;
            Require(localeNames.Length == 20 && localeNames.Distinct().Count() == 20, "The language menu offers twenty distinct locales");

            navigation.SelectedItem = tools;
            var sourceArea = (Grid)((ListBox)window.FindName("SourcesList")).Parent;
            var sourceMenu = sourceArea.Children.OfType<DockPanel>().Single().Children.OfType<Button>().Single().ContextMenu;
            var loadSample = sourceMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, window.FindResource("Str_Session_LoadSample")));
            loadSample.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(history.Items.Count == 1, "Loading the built-in sample creates one history entry");

            string englishInvalidCode = (string)window.FindResource("Str_Tkp_EnterWindowsCode");
            int checkedLocales = 0;
            foreach (var locale in localeNames)
            {
                navigation.SelectedItem = investigate;
                investigationAreas.SelectedIndex = 0;
                investigationAreas.ScrollIntoView(investigationAreas.SelectedItem);
                window.UpdateLayout();

                languages = OpenLanguageMenu(languageButton);
                LanguageChoices(languages).Single(item => item.Tag.ToString() == locale).IsChecked = true;
                languages.IsOpen = false;
                window.UpdateLayout();

                foreach (var destination in destinations)
                    Require(destination.Content is Button button && Equals(AutomationProperties.GetName(button), window.FindResource("Str_Workspace_" + destination.Tag)),
                        $"The {destination.Tag} workspace tab follows {locale}");

                var title = (TextBlock)window.FindName("AreaTitle");
                var description = (TextBlock)window.FindName("AreaDescription");
                Require(title.IsVisible && title.Text == (string)window.FindResource("Str_Category_Diagnostics"),
                    "The Investigate evidence title follows " + locale);
                Require(description.IsVisible && description.Text == (string)window.FindResource("Str_Category_Diagnostics_Description"),
                    "The Investigate evidence description follows " + locale);
                Require(((TextBlock)window.FindName("AreaState")).Text == (string)window.FindResource("Str_Workspace_NotChecked"),
                    "Changing language leaves the unrun area unchecked in " + locale);
                var areaRow = (ListBoxItem?)investigationAreas.ItemContainerGenerator.ContainerFromIndex(0);
                Require(areaRow is not null && Equals(AutomationProperties.GetName(areaRow), title.Text) &&
                    Descendants(areaRow).OfType<TextBlock>().Any(text => text.Text == title.Text) &&
                    Descendants(areaRow).OfType<TextBlock>().Any(text => text.Text == description.Text),
                    "The Investigate row text and accessible name follow " + locale);

                navigation.SelectedItem = historyPage;
                window.UpdateLayout();
                Require(history.IsVisible && history.Items.Count == 1 &&
                    history.Items[0].ToString() == (string)window.FindResource("Str_Content_Load_sample"),
                    "The existing synthetic History entry follows " + locale);

                navigation.SelectedItem = tools;
                var categories = (ListBox)window.FindName("CategoryList");
                categories.SelectedIndex = 0;
                window.UpdateLayout();
                Require(((TextBlock)window.FindName("CategoryTitle")).Text == (string)window.FindResource("Str_Category_Diagnostics"), "Category follows " + locale);
                Require(AutomationProperties.GetName((Button)window.FindName("ExportReportButton")) == (string)window.FindResource("Str_ExportReport"), "Export accessible label follows " + locale);
                ((Expander)window.FindName("KnowledgeTools")).IsExpanded = true;
                ((TextBox)window.FindName("KnowledgeCode")).Text = "not-a-code";
                ((Button)window.FindName("KnowledgeRunButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                string status = ((TextBlock)window.FindName("StatusLabel")).Text;
                Require(status == (string)window.FindResource("Str_Tkp_EnterWindowsCode"), "Windows code validation follows " + locale);
                Require(locale == "EnUS" || status != englishInvalidCode, "Non-English validation does not silently fall back in " + locale);
                categories.SelectedIndex = 3;
                ((TextBox)window.FindName("KnowledgeCode")).Text = "-1";
                ((Button)window.FindName("KnowledgeRunButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
            Console.WriteLine("PASS: twenty locales through the language menu, localized workspace tabs, Investigate evidence, session history and Tools validation. No provider or repair ran.");
            return 0;
        }
        finally { window.Close(); application.Shutdown(); }
    }

    private static ContextMenu OpenLanguageMenu(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var menu = button.ContextMenu;
        Require(menu.IsOpen, "The language button opens its menu");
        return menu;
    }

    private static IEnumerable<RadioButton> LanguageChoices(ContextMenu menu) =>
        menu.Items.OfType<StackPanel>().Single().Children.OfType<StackPanel>()
            .SelectMany(column => column.Children.OfType<RadioButton>());

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
