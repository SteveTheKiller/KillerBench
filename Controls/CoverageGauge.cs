using System.Windows;
using System.Windows.Media;

namespace KillerBench.Controls;

/// <summary>
/// Overview gauge. The outer arc is the share of checks that actually ran; the inner ring carries the
/// worst finding's color. The two never combine into a single health figure.
/// </summary>
public sealed class CoverageGauge : FrameworkElement
{
    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent), typeof(double), typeof(CoverageGauge),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(CoverageGauge),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CoverageBrushProperty = DependencyProperty.Register(
        nameof(CoverageBrush), typeof(Brush), typeof(CoverageGauge),
        new FrameworkPropertyMetadata(Brushes.Gold, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ToneBrushProperty = DependencyProperty.Register(
        nameof(ToneBrush), typeof(Brush), typeof(CoverageGauge),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Percent { get => (double)GetValue(PercentProperty); set => SetValue(PercentProperty, value); }
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public Brush CoverageBrush { get => (Brush)GetValue(CoverageBrushProperty); set => SetValue(CoverageBrushProperty, value); }
    public Brush ToneBrush { get => (Brush)GetValue(ToneBrushProperty); set => SetValue(ToneBrushProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (!(size > 0) || !double.IsFinite(size)) return;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double outerThickness = size * 0.075, innerThickness = size * 0.03;
        double outer = size / 2 - outerThickness / 2, inner = outer - outerThickness - innerThickness;
        dc.DrawEllipse(null, new Pen(TrackBrush, outerThickness), center, outer, outer);
        double fraction = Math.Clamp(Percent / 100, 0, 1);
        if (fraction >= 1)
            dc.DrawEllipse(null, new Pen(CoverageBrush, outerThickness), center, outer, outer);
        else if (fraction > 0)
            dc.DrawGeometry(null, new Pen(CoverageBrush, outerThickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat },
                Arc(center, outer, fraction));
        if (inner > 0)
            dc.DrawEllipse(null, new Pen(ToneBrush, innerThickness), center, inner, inner);
    }

    // Clockwise from twelve o'clock.
    private static StreamGeometry Arc(Point center, double radius, double fraction)
    {
        double angle = fraction * 2 * Math.PI;
        var start = new Point(center.X, center.Y - radius);
        var end = new Point(center.X + radius * Math.Sin(angle), center.Y - radius * Math.Cos(angle));
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, fraction > 0.5, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        return geometry;
    }
}
