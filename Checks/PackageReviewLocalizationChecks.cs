using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

internal static class PackageReviewLocalizationChecks
{
    internal static void Run(KillerBench.Shell.MainWindow window)
    {
        var localeType = window.GetType().Assembly.GetType("KillerBench.Services.Locale")!;
        const string fixture = "{\"complete\":true,\"packages\":[{\"name\":\"Fixture\",\"fullName\":\"Fixture_1\",\"familyName\":\"Fixture\",\"framework\":false,\"nonRemovable\":true,\"developmentMode\":false,\"status\":\"OK\",\"dependencies\":[]}]}";
        var inventory = AppPackageInventory.Interpret(AppPackageScope.CurrentAccount, new ToolResult(0, fixture, "", false));
        var categories = (ListBox)window.FindName("CategoryList");
        categories.SelectedIndex = 5;
        ((ComboBox)window.FindName("MainWorkflow")).SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == WorkflowKind.AppPackages);
        var setup = window.GetType().GetMethod("SetAppPackageInventory", BindingFlags.Instance | BindingFlags.NonPublic)!;
        setup.Invoke(window, [inventory]);
        ((ComboBox)window.FindName("AppPackageSelection")).SelectedIndex = 0;
        int checkedLocales = 0;
        foreach (object locale in Enum.GetValues(localeType))
        {
            ((Button)window.FindName("LangButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var menu = ((Button)window.FindName("LangButton")).ContextMenu;
            var panel = (StackPanel)menu.Items[0];
            var radio = panel.Children.OfType<StackPanel>().SelectMany(column => column.Children.OfType<RadioButton>())
                .Single(item => Equals(item.Tag, locale));
            radio.IsChecked = true;
            menu.IsOpen = false;
            if (checkedLocales == 0)
            {
                window.UpdateLayout();
                Descendants(window).OfType<Button>().Single(button => Equals(button.Content, window.FindResource("Str_Packages_ReviewSelected")))
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            window.UpdateLayout();
            string expectedTitle = string.Format((string)window.FindResource("Str_Report_Title"), window.FindResource("Str_PackageReview_Title"));
            Verify(window, expectedTitle);
            if (locale.ToString() == "De")
            {
                var heading = Descendants((DependencyObject)window.FindName("ReportContent")).OfType<TextBlock>().First();
                string previous = heading.Text;
                try
                {
                    heading.Text = "Package snapshot review";
                    bool rejected = false;
                    try { Verify(window, expectedTitle); }
                    catch (InvalidOperationException) { rejected = true; }
                    Require(rejected, "Rendered English-heading negative control must fail German acceptance");
                }
                finally { heading.Text = previous; }
            }
            checkedLocales++;
        }
        Require(checkedLocales == 20, "All twenty package-review locales were selected");
        setup.Invoke(window, [null]);
        Console.WriteLine("PASS: package action and retained report rerender through 20 real language-menu choices; English-heading negative control rejected; package identifiers and raw evidence retained.");
    }

    private static void Verify(KillerBench.Shell.MainWindow window, string title)
    {
        var texts = Descendants((DependencyObject)window.FindName("ReportContent")).OfType<TextBlock>().Select(text => text.Text).ToArray();
        Require(texts.Contains(title), "Package report title follows the selected language");
        Require(texts.Any(text => text.StartsWith((string)window.FindResource("Str_PackageReview_Reviewed"), StringComparison.Ordinal)), "Package report status follows the selected language");
        Require(texts.Contains((string)window.FindResource("Str_PackageReview_Dependents")), "Dependent-packages heading follows the selected language");
        Require(texts.Contains(string.Format((string)window.FindResource("Str_PackageReview_Item"), 1)), "Package check heading follows the selected language");
        Require(((TextBox)window.FindName("EvidenceBox")).Text.Contains("Fixture_1", StringComparison.Ordinal), "Raw package identifier remains verbatim");
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
