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

    private const double DesignWidth = 740, DesignHeight = 430, MinDrawnWidth = 460;

    // Part hotspots in design coordinates. The parts fill the screen and the case so the machine reads at a glance.
    private static readonly Dictionary<string, Rect> Parts = new()
    {
        ["Privacy"] = new(182, 0, 72, 37),
        ["Updates"] = new(24, 40, 206, 112),
        ["Apps"] = new(240, 40, 178, 112),
        ["Diagnostics"] = new(24, 162, 206, 104),
        ["Onboarding"] = new(240, 162, 132, 104),
        ["Security"] = new(380, 236, 38, 40),
        ["Maintenance"] = new(626, 22, 102, 102),
        ["Storage"] = new(452, 134, 150, 92),
        ["BitLocker"] = new(612, 140, 116, 74),
        ["Network"] = new(452, 236, 110, 92),
        ["Devices"] = new(572, 236, 72, 92),
        ["Repair"] = new(448, 340, 284, 82),
    };

    // The monitor sits flush on the tower's floor line; the strip it frees above it is GaugeRect, where the
    // Overview puts its score gauge. Monitor parts are drawn in the old coordinates and shifted down by MonitorDrop.
    private const double MonitorDrop = 124;
    private static readonly Rect Monitor = new(4, 4, 424, 300);
    private static readonly Rect MonitorScreen = new(16, 32, 410, 260);
    private static readonly Rect Tower = new(438, 4, 300, 424);

    public Rect ScreenRect { get; private set; } = Rect.Empty;
    public Rect TowerRect { get; private set; } = Rect.Empty;
    public Rect GaugeRect { get; private set; } = Rect.Empty;

    private double scale = 1;
    private Vector offset;
    private bool drawn;

    private static Rect Placed(Rect design) => design.X < Tower.X ? Rect.Offset(design, 0, MonitorDrop) : design;

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
            ScreenRect = TowerRect = GaugeRect = Rect.Empty;
            InvalidateVisual();
            return finalSize;
        }
        scale = Math.Min(finalSize.Width / DesignWidth, finalSize.Height / DesignHeight);
        // Centered across, sitting on the floor: spare height goes above the machine, into the space over the monitor.
        offset = new Vector((finalSize.Width - DesignWidth * scale) / 2, Math.Max(0, finalSize.Height - DesignHeight * scale));
        ScreenRect = ToView(Placed(MonitorScreen));
        // Everything above the monitor, from the panel's top edge down to just over the webcam.
        var monitorView = ToView(Placed(Monitor));
        GaugeRect = new Rect(monitorView.X, 0, monitorView.Width, Math.Max(0, ToView(Placed(Parts["Privacy"])).Y - 6 * scale));
        TowerRect = ToView(Tower);
        foreach (UIElement child in InternalChildren)
            child.Arrange(Parts.TryGetValue(GetPart(child), out var part) ? ToView(Placed(part)) : new Rect(-9999, -9999, 0, 0));
        InvalidateVisual();
        return finalSize;
    }

    // Hardware colors are fixed on purpose: the machine is an object, and it reads the same in every theme.
    private static Brush Solid(uint argb) { var brush = new SolidColorBrush(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb)); brush.Freeze(); return brush; }
    private static readonly Brush Case = Solid(0xFF1A1A19), Edge = Solid(0xFF45433E), Pcb = Solid(0xFF1F3A2C), PcbTrace = Solid(0xFF2A4D3A),
        Chip = Solid(0xFF151514), Gold = Solid(0xFFB89A3C), Metal = Solid(0xFF3E3D38), MetalEdge = Solid(0xFF5C5A53), Silver = Solid(0xFFBDBDB6),
        CpuLid = Solid(0xFFC9C8C1), CpuBase = Solid(0xFF9A9A94), Paper = Solid(0xFFF1F1EE), TitleBar = Solid(0xFFDCDCD6), Ink = Solid(0xFF222222),
        Blue = Solid(0xFF2A6FD6), Orange = Solid(0xFFD65A2A), UsbBlue = Solid(0xFF2B5FBF), Lens = Solid(0xFF24344A), Taskbar = Solid(0xFF0B0F15),
        Dark = Solid(0xFF111111), Ram = Solid(0xFF20201E), RamChip = Solid(0xFF2E2D2A), NvmeBoard = Solid(0xFF173A2A), Blade = Solid(0xFF666666);
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
        dc.PushTransform(new TranslateTransform(0, MonitorDrop));
        DrawMonitor(dc);
        dc.Pop();
        DrawTower(dc);
        dc.Pop();
        dc.Pop();
    }

    private void Window(DrawingContext dc, Rect r, string title)
    {
        dc.DrawRoundedRectangle(Paper, null, r, 5, 5);
        dc.DrawRoundedRectangle(TitleBar, null, new Rect(r.X, r.Y, r.Width, 18), 5, 5);
        dc.DrawRectangle(TitleBar, null, new Rect(r.X, r.Y + 10, r.Width, 8));
        Text(dc, title, r.X + 10, r.Y + 38, 13, Ink, bold: true);
    }

    private void DrawMonitor(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(Case, Line(Edge, 2), Monitor, 12, 12);
        dc.DrawRectangle(ScreenFill, null, new Rect(MonitorScreen.X, MonitorScreen.Y, MonitorScreen.Width - 12, MonitorScreen.Height - 4));
        dc.DrawRectangle(Taskbar, null, new Rect(MonitorScreen.X, 270, MonitorScreen.Width - 12, 18));

        // Privacy: the webcam on the top bezel.
        var cam = Parts["Privacy"];
        dc.DrawRoundedRectangle(Dark, null, new Rect(cam.X + 14, cam.Y + 12, cam.Width - 28, 14), 7, 7);
        dc.DrawEllipse(Lens, Line(MetalEdge, 1), new Point(cam.X + cam.Width / 2, cam.Y + 19), 4.5, 4.5);

        // Updates: a Windows Update window with a refresh arrow and progress bar.
        var updates = Parts["Updates"];
        Window(dc, updates, "Windows Update");
        dc.DrawEllipse(null, Line(Blue, 4), new Point(updates.X + 34, updates.Y + 76), 18, 18);
        dc.DrawGeometry(Blue, null, Geometry.Parse($"M{updates.X + 46},{updates.Y + 54} l8,0 l-2,10 z"));
        dc.DrawRoundedRectangle(TitleBar, null, new Rect(updates.X + 64, updates.Y + 72, updates.Width - 80, 8), 4, 4);
        dc.DrawRoundedRectangle(Blue, null, new Rect(updates.X + 64, updates.Y + 72, (updates.Width - 80) * .3, 8), 4, 4);

        // Apps: a grid of app icons.
        var apps = Parts["Apps"];
        Brush[] tiles = [Blue, Orange, Solid(0xFF3AA35A), Solid(0xFF8A4FD6), Solid(0xFFD6B52A), Solid(0xFF2AB5D6), Solid(0xFFD62A6F), Solid(0xFF7A7A72)];
        double tile = (apps.Height - 24) / 2;
        double gap = (apps.Width - tile * 4) / 5;
        for (int index = 0; index < 8; index++)
            dc.DrawRoundedRectangle(tiles[index], null, new Rect(apps.X + gap + index % 4 * (tile + gap), apps.Y + 8 + index / 4 * (tile + 8), tile, tile), 8, 8);

        // Diagnostics: a reliability chart window.
        var diagnostics = Parts["Diagnostics"];
        Window(dc, diagnostics, "Reliability");
        var trend = new StreamGeometry();
        using (var g = trend.Open())
        {
            double[] ys = [.8, .6, .72, .25, .55, .4, .68, .45, .6];
            double top = diagnostics.Y + 50, height = diagnostics.Height - 62;
            g.BeginFigure(new Point(diagnostics.X + 12, top + ys[0] * height), false, false);
            for (int index = 1; index < ys.Length; index++)
                g.LineTo(new Point(diagnostics.X + 12 + index * (diagnostics.Width - 24) / (ys.Length - 1), top + ys[index] * height), true, false);
        }
        trend.Freeze();
        dc.DrawGeometry(null, Line(Orange, 3), trend);

        // Onboarding: a setup checklist window.
        var onboarding = Parts["Onboarding"];
        Window(dc, onboarding, "Setup");
        for (int row = 0; row < 3; row++)
        {
            dc.DrawRectangle(null, Line(Blue, 2), new Rect(onboarding.X + 12, onboarding.Y + 52 + row * 16, 10, 10));
            dc.DrawRectangle(TitleBar, null, new Rect(onboarding.X + 30, onboarding.Y + 55 + row * 16, onboarding.Width - 44, 5));
        }

        // Security: the shield in the tray.
        var security = Parts["Security"];
        var shield = Geometry.Parse($"M{security.X + 19},{security.Y + 4} l14,6 v10 c0,9 -7,14 -14,17 c-7,-3 -14,-8 -14,-17 v-10 z");
        dc.DrawGeometry(Blue, null, shield);
        dc.DrawGeometry(null, Line(Brushes.White, 2.5), Geometry.Parse($"M{security.X + 12},{security.Y + 20} l5,5 l9,-9"));
    }

    private void DrawTower(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(Case, Line(Edge, 2), Tower, 10, 10);
        var board = new Rect(Tower.X + 8, Tower.Y + 10, Tower.Width - 16, 322);
        dc.DrawRoundedRectangle(Pcb, null, board, 4, 4);
        var trace = Line(PcbTrace, 1.4);
        for (int index = 0; index < 13; index++)
            dc.DrawLine(trace, new Point(board.X + 6, board.Y + 8 + index * 24), new Point(board.Right - 6, board.Y + 8 + index * 24));

        // CPU and RAM are scenery, the parts people recognize first.
        dc.DrawRoundedRectangle(CpuBase, null, new Rect(454, 22, 102, 102), 5, 5);
        dc.DrawRoundedRectangle(CpuLid, null, new Rect(464, 32, 82, 82), 4, 4);
        Text(dc, "CPU", 505, 78, 15, Ink, bold: true, center: true);
        for (int stick = 0; stick < 4; stick++)
        {
            dc.DrawRoundedRectangle(Ram, Line(MetalEdge, 1), new Rect(566 + stick * 14, 22, 10, 102), 1.5, 1.5);
            dc.DrawRectangle(RamChip, null, new Rect(568 + stick * 14, 32, 6, 82));
        }

        // Maintenance: the case fan.
        var fan = Parts["Maintenance"];
        var hub = new Point(fan.X + fan.Width / 2, fan.Y + fan.Height / 2);
        double radius = fan.Width / 2 - 3;
        dc.DrawEllipse(Dark, Line(MetalEdge, 2), hub, radius, radius);
        var blade = Line(Blade, 9);
        for (int index = 0; index < 5; index++)
        {
            double angle = index * Math.PI * 2 / 5;
            dc.DrawLine(blade, hub, new Point(hub.X + Math.Cos(angle) * radius * .78, hub.Y + Math.Sin(angle) * radius * .78));
        }
        dc.DrawEllipse(Metal, null, hub, 11, 11);

        // Storage: two NVMe sticks.
        var storage = Parts["Storage"];
        for (int stick = 0; stick < 2; stick++)
        {
            double y = storage.Y + 8 + stick * 42;
            dc.DrawRoundedRectangle(NvmeBoard, Line(PcbTrace, 1), new Rect(storage.X + 4, y, storage.Width - 8, 34), 3, 3);
            for (int chip = 0; chip < 3; chip++) dc.DrawRectangle(Chip, null, new Rect(storage.X + 12 + chip * 40, y + 6, 32, 22));
            dc.DrawRectangle(Gold, null, new Rect(storage.Right - 14, y + 4, 8, 26));
        }

        // BitLocker: the TPM chip with its pins.
        var tpm = Parts["BitLocker"];
        var body = new Rect(tpm.X + 22, tpm.Y + 12, tpm.Width - 44, tpm.Height - 24);
        var pin = Line(Silver, 2);
        for (int index = 0; index < 6; index++)
        {
            double x = body.X + 6 + index * (body.Width - 12) / 5;
            dc.DrawLine(pin, new Point(x, body.Y - 6), new Point(x, body.Y));
            dc.DrawLine(pin, new Point(x, body.Bottom), new Point(x, body.Bottom + 6));
        }
        dc.DrawRoundedRectangle(Chip, Line(MetalEdge, 1), body, 3, 3);
        Text(dc, "TPM", body.X + body.Width / 2, body.Y + body.Height / 2 + 6, 15, Silver, bold: true, center: true);

        // Network: the Ethernet port with its link light, and the Wi-Fi card.
        var network = Parts["Network"];
        var port = new Rect(network.X + 8, network.Y + 6, 52, 44);
        dc.DrawRoundedRectangle(Silver, null, port, 3, 3);
        dc.DrawRectangle(Dark, null, new Rect(port.X + 8, port.Y + 8, port.Width - 16, 24));
        dc.DrawRectangle(Dark, null, new Rect(port.X + 18, port.Y + 32, port.Width - 36, 6));
        dc.DrawEllipse(Solid(0xFF4FC36B), null, new Point(port.X + 6, port.Y + 5), 2.5, 2.5);
        var wifi = new Rect(network.X + 8, network.Y + 58, network.Width - 16, 26);
        dc.DrawRoundedRectangle(NvmeBoard, Line(PcbTrace, 1), wifi, 3, 3);
        dc.DrawRectangle(Chip, null, new Rect(wifi.X + 8, wifi.Y + 5, 30, 16));
        dc.DrawEllipse(Gold, null, new Point(wifi.Right - 30, wifi.Y + 13), 5, 5);
        dc.DrawEllipse(Gold, null, new Point(wifi.Right - 14, wifi.Y + 13), 5, 5);

        // Devices: USB ports and the audio jacks.
        var devices = Parts["Devices"];
        for (int index = 0; index < 2; index++)
        {
            var usb = new Rect(devices.X + 8, devices.Y + 8 + index * 22, devices.Width - 16, 16);
            dc.DrawRoundedRectangle(Silver, null, usb, 2, 2);
            dc.DrawRectangle(UsbBlue, null, new Rect(usb.X + 5, usb.Y + 5, usb.Width - 10, 6));
        }
        Brush[] jacks = [Solid(0xFF6DB04F), Solid(0xFFE0C040), Solid(0xFFE46BA0)];
        for (int jack = 0; jack < 3; jack++)
        {
            var center = new Point(devices.X + 16 + jack * (devices.Width - 32) / 2, devices.Y + 72);
            dc.DrawEllipse(jacks[jack], null, center, 7, 7);
            dc.DrawEllipse(Dark, null, center, 2.5, 2.5);
        }

        // Scenery: a SATA SSD in the free corner of the board.
        dc.DrawRoundedRectangle(Metal, Line(MetalEdge, 1), new Rect(654, 236, 74, 92), 4, 4);
        Text(dc, "SSD", 691, 286, 14, Silver, bold: true, center: true);

        // Repair: the power supply across the bottom of the case.
        var psu = Parts["Repair"];
        dc.DrawRoundedRectangle(Metal, Line(MetalEdge, 1.5), new Rect(psu.X + 2, psu.Y + 2, psu.Width - 4, psu.Height - 4), 5, 5);
        var grill = new Point(psu.X + 60, psu.Y + psu.Height / 2);
        double grillRadius = psu.Height / 2 - 10;
        dc.DrawEllipse(Dark, Line(Blade, 1.5), grill, grillRadius, grillRadius);
        for (int ring = 1; ring < 3; ring++) dc.DrawEllipse(null, Line(Blade, 1), grill, grillRadius * ring / 3, grillRadius * ring / 3);
        var vent = Line(Edge, 2.5);
        for (int slot = 0; slot < 5; slot++) dc.DrawLine(vent, new Point(psu.X + 120, psu.Y + 18 + slot * 9), new Point(psu.Right - 20, psu.Y + 18 + slot * 9));
        Text(dc, "PSU", psu.X + 120, psu.Bottom - 10, 12, Silver, bold: true);
    }
}
