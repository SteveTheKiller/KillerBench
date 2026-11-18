using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KillerBench.Controls;

// Draws a monitor and an open tower, then places each Overview area over the real part it stands for:
// Storage on the drives, Network on the Ethernet port and Wi-Fi card, BitLocker on the TPM chip, and the
// software areas as windows and icons on the screen. Each part stays a real ListBoxItem, so selection,
// keyboard focus and screen readers keep working. Below MinDrawnWidth the items stack as a plain list.
public sealed class MachineMapPanel : Panel
{
    public static readonly DependencyProperty PartProperty = DependencyProperty.RegisterAttached("Part", typeof(string),
        typeof(MachineMapPanel), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsParentArrange));
    public static string GetPart(DependencyObject element) => (string)element.GetValue(PartProperty);
    public static void SetPart(DependencyObject element, string value) => element.SetValue(PartProperty, value);

    // True while the panel draws the machine, so item templates can hide their list text and show only the glow.
    public static readonly DependencyProperty IsDrawnProperty = DependencyProperty.RegisterAttached("IsDrawn", typeof(bool),
        typeof(MachineMapPanel), new FrameworkPropertyMetadata(false));
    public static bool GetIsDrawn(DependencyObject element) => (bool)element.GetValue(IsDrawnProperty);
    public static void SetIsDrawn(DependencyObject element, bool value) => element.SetValue(IsDrawnProperty, value);

    private const double DesignWidth = 760, DesignHeight = 460, MinDrawnWidth = 460;

    // Part hotspots in design coordinates.
    private static readonly Dictionary<string, Rect> Parts = new()
    {
        ["Privacy"] = new(192, 16, 56, 36),
        ["Updates"] = new(40, 58, 160, 88),
        ["Apps"] = new(226, 60, 86, 56),
        ["Diagnostics"] = new(40, 160, 160, 80),
        ["Onboarding"] = new(226, 160, 120, 74),
        ["Security"] = new(376, 240, 40, 36),
        ["Maintenance"] = new(668, 58, 66, 66),
        ["Storage"] = new(494, 162, 108, 70),
        ["BitLocker"] = new(616, 178, 50, 38),
        ["Network"] = new(478, 238, 66, 62),
        ["Devices"] = new(556, 238, 62, 50),
        ["Repair"] = new(488, 348, 154, 74),
    };

    private static readonly Rect MonitorScreen = new(24, 40, 392, 236);
    private static readonly Rect TowerBoard = new(474, 24, 262, 300);

    public Rect ScreenRect { get; private set; } = Rect.Empty;
    public Rect TowerRect { get; private set; } = Rect.Empty;

    private double scale = 1;
    private Vector offset;
    private bool drawn;

    private Rect ToView(Rect design) => new(offset.X + design.X * scale, offset.Y + design.Y * scale, design.Width * scale, design.Height * scale);

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 640;
        if (width < MinDrawnWidth)
        {
            double height = 0;
            foreach (UIElement child in InternalChildren) { SetIsDrawn(child, false); child.Measure(new Size(width, double.PositiveInfinity)); height += child.DesiredSize.Height; }
            return new Size(width, height);
        }
        foreach (UIElement child in InternalChildren) { SetIsDrawn(child, true); child.Measure(new Size(200, 200)); }
        double fit = width / DesignWidth;
        if (double.IsFinite(availableSize.Height)) fit = Math.Min(fit, availableSize.Height / DesignHeight);
        return new Size(width, DesignHeight * Math.Max(fit, MinDrawnWidth / DesignWidth));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        drawn = finalSize.Width >= MinDrawnWidth;
        if (!drawn)
        {
            double y = 0;
            foreach (UIElement child in InternalChildren) { child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height)); y += child.DesiredSize.Height; }
            ScreenRect = TowerRect = Rect.Empty;
            InvalidateVisual();
            return finalSize;
        }
        scale = Math.Min(finalSize.Width / DesignWidth, finalSize.Height / DesignHeight);
        offset = new Vector((finalSize.Width - DesignWidth * scale) / 2, 0);
        ScreenRect = ToView(MonitorScreen);
        TowerRect = ToView(new Rect(TowerBoard.X, TowerBoard.Y, TowerBoard.Width, 400));
        foreach (UIElement child in InternalChildren)
            child.Arrange(Parts.TryGetValue(GetPart(child), out var part) ? ToView(part) : new Rect(-9999, -9999, 0, 0));
        InvalidateVisual();
        return finalSize;
    }

    // Hardware colors are fixed on purpose: the machine is an object, and it reads the same in every theme.
    private static Brush Solid(uint argb) { var brush = new SolidColorBrush(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb)); brush.Freeze(); return brush; }
    private static readonly Brush Case = Solid(0xFF1A1A19), Edge = Solid(0xFF45433E), Pcb = Solid(0xFF1F3A2C), PcbTrace = Solid(0xFF2A4D3A),
        Chip = Solid(0xFF151514), Gold = Solid(0xFFB89A3C), Metal = Solid(0xFF3E3D38), MetalEdge = Solid(0xFF5C5A53), Silver = Solid(0xFFBDBDB6),
        CpuLid = Solid(0xFFC9C8C1), CpuBase = Solid(0xFF9A9A94), Paper = Solid(0xFFF1F1EE), TitleBar = Solid(0xFFDCDCD6), Ink = Solid(0xFF222222),
        Blue = Solid(0xFF2A6FD6), Orange = Solid(0xFFD65A2A), UsbBlue = Solid(0xFF2B5FBF), Lens = Solid(0xFF24344A), Taskbar = Solid(0xFF0B0F15),
        Label = Solid(0xFFE9E6DA), Dark = Solid(0xFF111111), Ram = Solid(0xFF20201E), RamChip = Solid(0xFF2E2D2A);
    private static readonly Brush ScreenFill = Freeze(new LinearGradientBrush(Color.FromRgb(0x1D, 0x2A, 0x3A), Color.FromRgb(0x0F, 0x16, 0x20), 45));
    private static Brush Freeze(Brush brush) { brush.Freeze(); return brush; }
    private static Pen Line(Brush brush, double thickness) { var pen = new Pen(brush, thickness); pen.Freeze(); return pen; }

    private void Text(DrawingContext dc, string text, double x, double y, double size, Brush brush, bool bold = false, bool center = false)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
            size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point(center ? x - formatted.Width / 2 : x, y - formatted.Baseline));
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!drawn || ActualWidth <= 0) return;
        dc.PushTransform(new TranslateTransform(offset.X, offset.Y));
        dc.PushTransform(new ScaleTransform(scale, scale));
        DrawMonitor(dc);
        DrawTower(dc);
        dc.Pop();
        dc.Pop();
    }

    private void Window(DrawingContext dc, Rect r, string title)
    {
        dc.DrawRoundedRectangle(Paper, null, r, 4, 4);
        dc.DrawRoundedRectangle(TitleBar, null, new Rect(r.X, r.Y, r.Width, 14), 4, 4);
        Text(dc, title, r.X + 8, r.Y + 30, 9, Ink, bold: true);
    }

    private void DrawMonitor(DrawingContext dc)
    {
        var edge = Line(Edge, 2);
        dc.DrawRoundedRectangle(Case, edge, new Rect(10, 20, 420, 284), 12, 12);
        dc.DrawRectangle(ScreenFill, null, MonitorScreen);
        dc.DrawRectangle(Taskbar, null, new Rect(MonitorScreen.X, MonitorScreen.Bottom - 20, MonitorScreen.Width, 20));
        dc.DrawRectangle(Metal, null, new Rect(190, 304, 60, 38));
        dc.DrawRoundedRectangle(Metal, null, new Rect(140, 342, 160, 12), 5, 5);

        // Privacy: the webcam on the top bezel.
        dc.DrawRoundedRectangle(Dark, null, new Rect(203, 30, 34, 10), 5, 5);
        dc.DrawEllipse(Lens, Line(MetalEdge, 1), new Point(220, 35), 3.2, 3.2);

        // Updates: a Windows Update window with a progress bar.
        var updates = Parts["Updates"];
        Window(dc, updates, "Windows Update");
        dc.DrawEllipse(null, Line(Blue, 3), new Point(updates.X + 22, updates.Y + 56), 12, 12);
        dc.DrawRoundedRectangle(TitleBar, null, new Rect(updates.X + 44, updates.Y + 54, updates.Width - 56, 5), 2, 2);
        dc.DrawRoundedRectangle(Blue, null, new Rect(updates.X + 44, updates.Y + 54, (updates.Width - 56) * .3, 5), 2, 2);

        // Apps: a block of app icons.
        var apps = Parts["Apps"];
        Brush[] tiles = [Blue, Orange, Solid(0xFF3AA35A), Solid(0xFF8A4FD6), Solid(0xFFD6B52A), Solid(0xFF2AB5D6)];
        for (int index = 0; index < 6; index++)
            dc.DrawRoundedRectangle(tiles[index], null, new Rect(apps.X + 4 + index % 3 * 28, apps.Y + 4 + index / 3 * 28, 22, 22), 5, 5);

        // Diagnostics: a reliability chart window.
        var diagnostics = Parts["Diagnostics"];
        Window(dc, diagnostics, "Reliability");
        var trend = new StreamGeometry();
        using (var g = trend.Open())
        {
            double[] ys = [.8, .65, .72, .35, .58, .45, .66, .5];
            g.BeginFigure(new Point(diagnostics.X + 10, diagnostics.Y + 36 + ys[0] * 36), false, false);
            for (int index = 1; index < ys.Length; index++)
                g.LineTo(new Point(diagnostics.X + 10 + index * (diagnostics.Width - 20) / (ys.Length - 1), diagnostics.Y + 36 + ys[index] * 36), true, false);
        }
        trend.Freeze();
        dc.DrawGeometry(null, Line(Orange, 2), trend);

        // Onboarding: a setup checklist window.
        var onboarding = Parts["Onboarding"];
        Window(dc, onboarding, "Setup");
        for (int row = 0; row < 3; row++)
        {
            dc.DrawRectangle(null, Line(Blue, 1.5), new Rect(onboarding.X + 10, onboarding.Y + 38 + row * 11, 7, 7));
            dc.DrawRectangle(TitleBar, null, new Rect(onboarding.X + 22, onboarding.Y + 40 + row * 11, onboarding.Width - 34, 4));
        }

        // Security: the shield in the tray.
        var security = Parts["Security"];
        var shield = Geometry.Parse($"M{security.X + 20},{security.Y + 5} l10,4 v7 c0,6 -5,10 -10,12 c-5,-2 -10,-6 -10,-12 v-7 z");
        dc.DrawGeometry(Blue, null, shield);
    }

    private void DrawTower(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(Case, Line(Edge, 2), new Rect(460, 10, 290, 440), 10, 10);
        dc.DrawRoundedRectangle(Pcb, null, TowerBoard, 4, 4);
        var trace = Line(PcbTrace, 1.4);
        for (int index = 0; index < 12; index++)
            dc.DrawLine(trace, new Point(TowerBoard.X + 10, TowerBoard.Y + 14 + index * 24), new Point(TowerBoard.X + 60 + index * 8, TowerBoard.Y + 14 + index * 24));

        // CPU and RAM are scenery, the parts people recognize first.
        dc.DrawRoundedRectangle(CpuBase, null, new Rect(560, 64, 70, 70), 4, 4);
        dc.DrawRoundedRectangle(CpuLid, null, new Rect(568, 72, 54, 54), 3, 3);
        Text(dc, "CPU", 595, 103, 10, Ink, bold: true, center: true);
        for (int stick = 0; stick < 2; stick++)
        {
            dc.DrawRoundedRectangle(Ram, Line(MetalEdge, 1), new Rect(500 + stick * 13, 40, 9, 110), 1.5, 1.5);
            dc.DrawRectangle(RamChip, null, new Rect(501.5 + stick * 13, 50, 6, 90));
        }

        // Maintenance: the case fan.
        var fan = Parts["Maintenance"];
        var hub = new Point(fan.X + fan.Width / 2, fan.Y + fan.Height / 2);
        double radius = fan.Width / 2 - 2;
        dc.DrawEllipse(Dark, Line(MetalEdge, 1.5), hub, radius, radius);
        var blade = Line(Solid(0xFF666666), 5);
        for (int index = 0; index < 5; index++)
        {
            double angle = index * Math.PI * 2 / 5;
            dc.DrawLine(blade, hub, new Point(hub.X + Math.Cos(angle) * radius * .8, hub.Y + Math.Sin(angle) * radius * .8));
        }
        dc.DrawEllipse(Metal, null, hub, 7, 7);

        // Storage: two NVMe sticks.
        var storage = Parts["Storage"];
        for (int stick = 0; stick < 2; stick++)
        {
            double y = storage.Y + 8 + stick * 32;
            dc.DrawRoundedRectangle(Solid(0xFF173A2A), null, new Rect(storage.X + 4, y, storage.Width - 8, 22), 2, 2);
            for (int chip = 0; chip < 3; chip++) dc.DrawRectangle(Chip, null, new Rect(storage.X + 12 + chip * 28, y + 4, 22, 14));
            dc.DrawRectangle(Gold, null, new Rect(storage.Right - 10, y + 3, 6, 16));
        }

        // BitLocker: the TPM chip.
        var tpm = Parts["BitLocker"];
        dc.DrawRoundedRectangle(Chip, Line(MetalEdge, 1), new Rect(tpm.X + 6, tpm.Y + 7, tpm.Width - 12, tpm.Height - 14), 2, 2);
        Text(dc, "TPM", tpm.X + tpm.Width / 2, tpm.Y + tpm.Height / 2 + 4, 9, Silver, center: true);

        // Network: the Ethernet port with its link light, and the Wi-Fi card.
        var network = Parts["Network"];
        dc.DrawRoundedRectangle(Silver, null, new Rect(network.X + 6, network.Y + 4, 30, 26), 2, 2);
        dc.DrawRectangle(Dark, null, new Rect(network.X + 11, network.Y + 9, 20, 14));
        dc.DrawRectangle(Dark, null, new Rect(network.X + 17, network.Y + 23, 8, 4));
        dc.DrawEllipse(Solid(0xFF4FC36B), null, new Point(network.X + 10, network.Y + 7), 1.8, 1.8);
        dc.DrawRoundedRectangle(Solid(0xFF173A2A), null, new Rect(network.X + 6, network.Y + 38, 54, 16), 2, 2);
        dc.DrawRectangle(Chip, null, new Rect(network.X + 12, network.Y + 41, 16, 10));
        dc.DrawEllipse(Gold, null, new Point(network.X + 40, network.Y + 46), 3, 3);
        dc.DrawEllipse(Gold, null, new Point(network.X + 50, network.Y + 46), 3, 3);

        // Devices: USB ports and the audio jacks.
        var devices = Parts["Devices"];
        for (int port = 0; port < 2; port++)
        {
            dc.DrawRoundedRectangle(Silver, null, new Rect(devices.X + 4, devices.Y + 8 + port * 16, 26, 10), 1, 1);
            dc.DrawRectangle(UsbBlue, null, new Rect(devices.X + 7, devices.Y + 11 + port * 16, 20, 4));
        }
        Brush[] jacks = [Solid(0xFF6DB04F), Solid(0xFFE0C040), Solid(0xFFE46BA0)];
        for (int jack = 0; jack < 3; jack++) dc.DrawEllipse(jacks[jack], null, new Point(devices.X + 46, devices.Y + 11 + jack * 12), 4, 4);

        // Repair: the power supply.
        var psu = Parts["Repair"];
        dc.DrawRoundedRectangle(Metal, Line(MetalEdge, 1), new Rect(psu.X + 2, psu.Y + 2, psu.Width - 4, psu.Height - 4), 4, 4);
        dc.DrawEllipse(Dark, Line(Solid(0xFF666666), 1), new Point(psu.X + psu.Width / 2, psu.Y + psu.Height / 2), psu.Height / 2 - 9, psu.Height / 2 - 9);
        Text(dc, "PSU", psu.X + 10, psu.Bottom - 10, 9, Silver);

        // SATA SSD as scenery beside the drives, and the power button and vents along the bottom.
        dc.DrawRoundedRectangle(Metal, Line(MetalEdge, 1), new Rect(640, 238, 84, 54), 4, 4);
        Text(dc, "SSD", 682, 270, 10, Silver, center: true);
        var vent = Line(Edge, 1.5);
        for (int slot = 0; slot < 5; slot++) dc.DrawLine(vent, new Point(660, 380 + slot * 6), new Point(724, 380 + slot * 6));
        dc.DrawEllipse(null, Line(Edge, 2), new Point(706, 428), 7, 7);
    }
}
