using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KillerBench.Controls;

// Lays the Overview areas out as parts of one drawn machine: software areas fill a 2 by N grid on the
// monitor screen, hardware and system areas stack as bays in the tower. Each part stays a real ListBoxItem,
// so selection, keyboard focus and screen readers behave exactly like the list they replace.
public sealed class MachineMapPanel : Panel
{
    public static readonly DependencyProperty InTowerProperty = DependencyProperty.RegisterAttached("InTower", typeof(bool),
        typeof(MachineMapPanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    public static bool GetInTower(DependencyObject element) => (bool)element.GetValue(InTowerProperty);
    public static void SetInTower(DependencyObject element, bool value) => element.SetValue(InTowerProperty, value);

    public static readonly DependencyProperty FrameBrushProperty = DependencyProperty.Register(nameof(FrameBrush), typeof(Brush),
        typeof(MachineMapPanel), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty BezelBrushProperty = DependencyProperty.Register(nameof(BezelBrush), typeof(Brush),
        typeof(MachineMapPanel), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush FrameBrush { get => (Brush)GetValue(FrameBrushProperty); set => SetValue(FrameBrushProperty, value); }
    public Brush BezelBrush { get => (Brush)GetValue(BezelBrushProperty); set => SetValue(BezelBrushProperty, value); }
    public static readonly DependencyProperty ScreenBrushProperty = DependencyProperty.Register(nameof(ScreenBrush), typeof(Brush),
        typeof(MachineMapPanel), new FrameworkPropertyMetadata(Brushes.DimGray, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush ScreenBrush { get => (Brush)GetValue(ScreenBrushProperty); set => SetValue(ScreenBrushProperty, value); }

    private const double Bezel = 14, StandHeight = 44, Gap = 22, MinMachineWidth = 460, ScreenRowMin = 76, BayMin = 50;

    public Rect ScreenRect { get; private set; }
    public Rect TowerRect { get; private set; }
    private Rect monitorRect;
    private bool drawn;

    private (List<UIElement> screen, List<UIElement> tower) Split()
    {
        var screen = new List<UIElement>();
        var tower = new List<UIElement>();
        foreach (UIElement child in InternalChildren) (GetInTower(child) ? tower : screen).Add(child);
        return (screen, tower);
    }

    private static (double monitor, double tower) Widths(double width)
    {
        double tower = Math.Round(width * .34);
        return (width - tower - Gap, tower);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 640;
        var (screen, tower) = Split();
        if (width < MinMachineWidth)
        {
            double height = 0;
            foreach (UIElement child in InternalChildren) { child.Measure(new Size(width, double.PositiveInfinity)); height += child.DesiredSize.Height; }
            return new Size(width, height);
        }
        var (monitorWidth, towerWidth) = Widths(width);
        double cellWidth = (monitorWidth - Bezel * 2) / 2;
        double rowHeight = ScreenRowMin, bayHeight = BayMin;
        foreach (var child in screen) { child.Measure(new Size(cellWidth, double.PositiveInfinity)); rowHeight = Math.Max(rowHeight, child.DesiredSize.Height); }
        foreach (var child in tower) { child.Measure(new Size(towerWidth - Bezel * 2, double.PositiveInfinity)); bayHeight = Math.Max(bayHeight, child.DesiredSize.Height); }
        int rows = Math.Max(1, (screen.Count + 1) / 2);
        double screenHeight = rows * rowHeight + Bezel * 2 + StandHeight;
        double towerHeight = Math.Max(1, tower.Count) * bayHeight + Bezel * 2 + 34;
        return new Size(width, Math.Max(screenHeight, towerHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (screen, tower) = Split();
        drawn = finalSize.Width >= MinMachineWidth;
        if (!drawn)
        {
            double y = 0;
            foreach (UIElement child in InternalChildren) { child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height)); y += child.DesiredSize.Height; }
            ScreenRect = TowerRect = Rect.Empty;
            return finalSize;
        }
        var (monitorWidth, towerWidth) = Widths(finalSize.Width);
        monitorRect = new Rect(0, 0, monitorWidth, Math.Max(0, finalSize.Height - StandHeight));
        ScreenRect = new Rect(Bezel, Bezel, monitorWidth - Bezel * 2, Math.Max(0, monitorRect.Height - Bezel * 2));
        int rows = Math.Max(1, (screen.Count + 1) / 2);
        double cellWidth = ScreenRect.Width / 2, cellHeight = ScreenRect.Height / rows;
        for (int index = 0; index < screen.Count; index++)
            screen[index].Arrange(new Rect(ScreenRect.X + index % 2 * cellWidth, ScreenRect.Y + index / 2 * cellHeight, cellWidth, cellHeight));

        TowerRect = new Rect(finalSize.Width - towerWidth + Bezel, Bezel, towerWidth - Bezel * 2, Math.Max(0, finalSize.Height - Bezel * 2 - 34));
        double bay = TowerRect.Height / Math.Max(1, tower.Count);
        for (int index = 0; index < tower.Count; index++)
            tower[index].Arrange(new Rect(TowerRect.X, TowerRect.Y + index * bay, TowerRect.Width, bay));
        InvalidateVisual();
        return finalSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!drawn || ActualWidth <= 0) return;
        var frame = new Pen(FrameBrush, 2);
        // Monitor: bezel, neck and foot.
        dc.DrawRoundedRectangle(BezelBrush, frame, monitorRect, 10, 10);
        dc.DrawRectangle(ScreenBrush, null, ScreenRect);
        var seam = new Pen(FrameBrush, 1);
        int rows = Math.Max(1, (Split().screen.Count + 1) / 2);
        for (int row = 1; row < rows; row++)
            dc.DrawLine(seam, new Point(ScreenRect.X + 8, ScreenRect.Y + row * ScreenRect.Height / rows), new Point(ScreenRect.Right - 8, ScreenRect.Y + row * ScreenRect.Height / rows));
        dc.DrawLine(seam, new Point(ScreenRect.X + ScreenRect.Width / 2, ScreenRect.Y + 8), new Point(ScreenRect.X + ScreenRect.Width / 2, ScreenRect.Bottom - 8));
        double neckX = monitorRect.Width / 2;
        dc.DrawRectangle(BezelBrush, frame, new Rect(neckX - 22, monitorRect.Bottom, 44, StandHeight - 12));
        dc.DrawRoundedRectangle(BezelBrush, frame, new Rect(neckX - 90, ActualHeight - 12, 180, 10), 4, 4);
        // Tower: case, front bays sit inside, power button and vents along the bottom.
        var tower = new Rect(TowerRect.X - Bezel, 0, TowerRect.Width + Bezel * 2, ActualHeight - 2);
        dc.DrawRoundedRectangle(BezelBrush, frame, tower, 8, 8);
        // Each bay gets a faceplate edge and a drive slot, so the parts read as hardware.
        int bays = Math.Max(1, Split().tower.Count);
        double bayHeight = TowerRect.Height / bays;
        for (int bayIndex = 0; bayIndex < bays; bayIndex++)
        {
            var plate = new Rect(TowerRect.X, TowerRect.Y + bayIndex * bayHeight + 2, TowerRect.Width, bayHeight - 4);
            dc.DrawRoundedRectangle(ScreenBrush, seam, plate, 3, 3);
            dc.DrawLine(seam, new Point(plate.Right - 46, plate.Bottom - 8), new Point(plate.Right - 10, plate.Bottom - 8));
        }
        double ventY = TowerRect.Bottom + 14;
        for (int vent = 0; vent < 5; vent++)
            dc.DrawLine(new Pen(FrameBrush, 1.5), new Point(tower.X + 18, ventY + vent * 3.5), new Point(tower.Right - 46, ventY + vent * 3.5));
        dc.DrawEllipse(null, frame, new Point(tower.Right - 24, ventY + 7), 7, 7);
        dc.DrawLine(frame, new Point(tower.Right - 24, ventY + 1), new Point(tower.Right - 24, ventY + 7));
    }
}
