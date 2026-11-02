using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using KillerBench.Engine.Security;

namespace KillerBench.Controls
{
    public sealed class PrivacyTextConverter : IValueConverter
    {
        public static PrivacyTextConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            PrivacyChoice choice => LocalizedText.Get("Str_PrivacyArea_" + choice.Area +
                (parameter as string == "Impact" ? "_Impact" : "_Title")),
            PrivacyBooleanSettingDefinition setting => LocalizedText.Get("Str_PrivacySetting_" +
                setting.Key.Replace('-', '_') + "_Title"),
            _ => ""
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}

namespace KillerBench.Shell
{
    public partial class MainWindow
    {
        private void RefreshPrivacyLanguage()
        {
            Refresh(PrivacyAreas);
            Refresh(PrivacyBooleanSettingChoice);

            static void Refresh(DependencyObject element)
            {
                if (element is TextBlock text) text.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
                if (element is CheckBox check)
                {
                    check.GetBindingExpression(ContentControl.ContentProperty)?.UpdateTarget();
                    check.GetBindingExpression(FrameworkElement.ToolTipProperty)?.UpdateTarget();
                }
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                    Refresh(VisualTreeHelper.GetChild(element, i));
            }
        }
    }
}
