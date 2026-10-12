using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KillerBench.Controls;

/// <summary>Shares the scroll viewport between categories without shrinking readable rows.</summary>
public sealed class CategoryPanel : Panel
{
    // Includes the existing two-pixel gap below each 40-pixel category button.
    private const double MinimumSlotHeight = 42;
    public static readonly DependencyProperty ViewportHeightProperty = DependencyProperty.Register(
        nameof(ViewportHeight), typeof(double), typeof(CategoryPanel),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ViewportHeight
    {
        get => (double)GetValue(ViewportHeightProperty);
        set => SetValue(ViewportHeightProperty, value);
    }

    private double SlotHeight => InternalChildren.Count == 0 ? 0 :
        Math.Max(MinimumSlotHeight, ViewportHeight / InternalChildren.Count);

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = SlotHeight;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(availableSize.Width, height));
            width = Math.Max(width, child.DesiredSize.Width);
        }
        return new Size(width, height * InternalChildren.Count);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double height = SlotHeight, top = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Arrange(new Rect(0, top, finalSize.Width, height));
            top += height;
        }
        return finalSize;
    }
}

/// <summary>Uses the allocated button height, independent of its content's desired size.</summary>
public sealed class TallCategoryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double height && height >= 64;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
