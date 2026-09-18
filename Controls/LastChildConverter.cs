using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KillerBench.Controls;

public sealed class LastChildConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DependencyObject item) return false;
        var parent = ItemsControl.ItemsControlFromItemContainer(item);
        if (parent is null) return false;
        int index = parent.ItemContainerGenerator.IndexFromContainer(item);
        return index >= 0 && index == parent.Items.Count - 1;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
