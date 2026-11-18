using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KillerBench.Controls;

public sealed class SystemMapPanel : Panel
{
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush),
        typeof(SystemMapPanel), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ConnectionBrushProperty = DependencyProperty.Register(nameof(ConnectionBrush), typeof(Brush),
        typeof(SystemMapPanel), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Brush ConnectionBrush { get => (Brush)GetValue(ConnectionBrushProperty); set => SetValue(ConnectionBrushProperty, value); }

    private static double CenterWidth(double width) => width >= 460 ? 128 : 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 600;
        double nodeWidth = Math.Max(0, (width - CenterWidth(width)) / 2);
        double rowHeight = 84;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(nodeWidth, double.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
        }
        return new Size(width, rowHeight * Math.Ceiling(InternalChildren.Count / 2d));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int rows = Math.Max(1, (InternalChildren.Count + 1) / 2);
        double nodeWidth = Math.Max(0, (finalSize.Width - CenterWidth(finalSize.Width)) / 2);
        double rowHeight = finalSize.Height / rows;
        for (int index = 0; index < InternalChildren.Count; index++)
        {
            int column = index < rows ? 0 : 1;
            int row = index % rows;
            InternalChildren[index].Arrange(new Rect(column == 0 ? 0 : finalSize.Width - nodeWidth,
                row * rowHeight, nodeWidth, rowHeight));
        }
        return finalSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double centerWidth = CenterWidth(ActualWidth);
        if (centerWidth == 0 || ActualHeight <= 0) return;
        double middle = ActualWidth / 2;
        double nodeWidth = (ActualWidth - centerWidth) / 2;
        var pen = new Pen(ConnectionBrush, 1);
        int rows = Math.Max(1, (InternalChildren.Count + 1) / 2);
        for (int row = 0; row < rows; row++)
        {
            double y = (row + .5) * ActualHeight / rows;
            foreach (int direction in new[] { -1, 1 })
            {
                double outer = direction < 0 ? nodeWidth - 4 : ActualWidth - nodeWidth + 4;
                double trunk = middle + direction * 52;
                dc.DrawLine(pen, new Point(outer, y), new Point(trunk, y));
                dc.DrawLine(pen, new Point(trunk, y), new Point(trunk, ActualHeight / 2));
                dc.DrawEllipse(ConnectionBrush, null, new Point(outer, y), 2.5, 2.5);
            }
        }
        dc.DrawLine(pen, new Point(middle - 52, ActualHeight / 2), new Point(middle + 52, ActualHeight / 2));
        AreaIllustration.Draw(dc, "Workstation", new Rect(middle - 58, ActualHeight / 2 - 58, 116, 116), Stroke);
    }
}
