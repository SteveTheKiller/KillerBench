using System.Windows;
using System.Windows.Media;

namespace KillerBench.Controls;

/// <summary>Draws the technical illustrations used in the workstation map.</summary>
public sealed class AreaIllustration : FrameworkElement
{
    private const double CanvasSize = 100;

    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
        nameof(Id), typeof(string), typeof(AreaIllustration),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(AreaIllustration),
        new FrameworkPropertyMetadata(Brushes.Gold, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        Draw(drawingContext, Id, new Rect(RenderSize), Stroke);
    }

    public static void Draw(DrawingContext dc, string id, Rect bounds, Brush stroke)
    {
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0 ||
            !double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) ||
            !double.IsFinite(bounds.Right) || !double.IsFinite(bounds.Bottom) ||
            stroke is null || id is null || !Illustrations.TryGetValue(id, out Geometry? illustration))
            return;

        double scale = Math.Min(bounds.Width, bounds.Height) / CanvasSize;
        if (scale <= 0)
            return;

        double size = CanvasSize * scale;
        double left = bounds.X + (bounds.Width - size) / 2;
        double top = bounds.Y + (bounds.Height - size) / 2;
        bool isWorkstation = id == "Workstation";
        var pen = new Pen(stroke, isWorkstation ? 2.2 : 3.4)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        dc.PushTransform(new MatrixTransform(scale, 0, 0, scale, left, top));
        dc.DrawGeometry(null, pen, illustration);
        if (isWorkstation)
            dc.DrawGeometry(stroke, null, WorkstationScreen);
        dc.Pop();
    }

    private static Geometry Figure(string path)
    {
        Geometry geometry = Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }

    private static readonly Geometry WorkstationScreen = Figure("""
        M 27,35 L 34,34 V 40 H 27 Z
        M 36,33.6 L 46,31.5 V 40 H 36 Z
        M 27,42 H 34 V 48 L 27,47 Z
        M 36,42 H 46 V 50 L 36,48.4 Z
        """);

    // Each illustration shares a 100-unit view box. Coordinates are grouped by visible part.
    private static readonly Dictionary<string, Geometry> Illustrations = new(StringComparer.Ordinal)
    {
        ["Storage"] = Figure("""
            M 20,8 H 80 Q 85,8 85,13 V 87 Q 85,92 80,92 H 20 Q 15,92 15,87 V 13 Q 15,8 20,8 Z
            M 24,17 H 25 M 75,17 H 76 M 24,83 H 25 M 75,83 H 76
            M 37,64 A 24,24 0 1 1 49,68
            M 45,44 A 5,5 0 1 0 55,44 A 5,5 0 1 0 45,44 Z
            M 30,73 L 41,58 Q 44,55 47,58 Q 50,61 47,64 L 36,78 Q 33,81 30,78 Q 28,76 30,73 Z
            """),
        ["Network"] = Figure("""
            M 19,56 H 81 Q 89,56 89,64 V 73 Q 89,81 81,81 H 19 Q 11,81 11,73 V 64 Q 11,56 19,56 Z
            M 50,56 V 44 M 55,68.5 H 56 M 66,68.5 H 67 M 77,68.5 H 78
            M 29,28 Q 50,8 71,28 M 36,35 Q 50,22 64,35 M 44,41 Q 50,35 56,41
            """),
        ["Updates"] = Figure("""
            M 13,47 V 22 Q 13,16 19,16 H 81 Q 87,16 87,22 V 77 Q 87,83 81,83 H 19 Q 13,83 13,77 V 47
            M 13,31 H 87 M 22,24 H 23 M 31,24 H 32
            M 31,55 A 20,20 0 0 1 68,51 M 67,43 L 68,51 H 60
            M 69,60 A 20,20 0 0 1 32,64 M 33,72 L 32,64 H 40
            """),
        ["Apps"] = Figure("""
            M 18,14 H 82 Q 87,14 87,19 V 81 Q 87,86 82,86 H 18 Q 13,86 13,81 V 19 Q 13,14 18,14 Z
            M 13,33 H 87 M 23,23.5 H 24 M 32,23.5 H 33 M 41,23.5 H 42
            M 32,44 H 45 V 56 H 32 Z M 55,44 H 68 V 56 H 55 Z
            M 32,63 H 45 V 75 H 32 Z M 55,63 H 68 V 75 H 55 Z
            """),
        ["Security"] = Figure("""
            M 50,9 Q 66,20 85,23 V 46 Q 85,75 50,90 Q 15,75 15,46 V 23 Q 34,20 50,9 Z
            M 39,45 H 61 Q 63,45 63,47 V 62 Q 63,64 61,64 H 39 Q 37,64 37,62 V 47 Q 37,45 39,45 Z
            M 43,45 V 38 A 7,7 0 0 1 57,38 V 45 M 50,53 V 56
            """),
        ["Devices"] = Figure("""
            M 15,35 H 39 Q 43,35 43,39 V 72 A 16,16 0 0 1 11,72 V 39 Q 11,35 15,35 Z
            M 17,35 V 15 H 37 V 35 M 23,25 H 24 M 30,25 H 31
            M 89,66 V 58 A 17,17 0 0 0 55,58 V 66 A 17,17 0 0 0 89,66 Z
            M 72,41 V 50 M 72,50 A 3,3 0 0 1 75,53 V 57 A 3,3 0 0 1 69,57 V 53 A 3,3 0 0 1 72,50 Z
            """),
        ["Diagnostics"] = Figure("""
            M 33,17 H 18 Q 13,17 13,22 V 82 Q 13,87 18,87 H 54
            M 59,17 H 73 Q 78,17 78,22 V 43
            M 33,13 H 40 Q 40,7 46,7 Q 52,7 52,13 H 59 V 24 H 33 Z
            M 23,48 H 33 L 39,36 L 45,58 L 52,43 H 62
            M 23,70 H 45
            M 54,65 A 16,16 0 1 0 86,65 A 16,16 0 1 0 54,65 Z M 81,77 L 93,89
            """),
        ["Repair"] = Figure("""
            M 77,15 L 65,27 L 67,36 L 76,38 L 88,26
            Q 92,40 81,48 Q 74,53 64,50 L 29,85
            Q 23,91 17,85 Q 11,79 17,73 L 52,38
            Q 49,28 54,21 Q 62,10 77,15 Z
            M 23,78 L 24,79
            M 14,22 L 20,16 H 30 L 36,22 V 32 L 30,38 H 20 L 14,32 Z
            M 22,27 A 3,3 0 1 0 28,27 A 3,3 0 1 0 22,27 Z
            """),
        ["Maintenance"] = Figure("""
            M 44,51 L 61,12 Q 63,8 67,10 Q 71,12 69,16 L 56,55
            M 34,47 L 63,57 L 60,66 L 31,56 Z
            M 32,57 Q 27,69 13,78 L 60,92 Q 55,77 59,66
            M 40,63 Q 36,74 29,82 M 49,66 Q 46,78 45,87
            M 80,43 V 57 M 73,50 H 87 M 80,73 V 83 M 75,78 H 85
            """),
        ["Onboarding"] = Figure("""
            M 23,10 H 62 L 77,25 V 59 M 63,11 V 26 H 76
            M 23,10 Q 18,10 18,15 V 84 Q 18,89 23,89 H 57
            M 27,36 L 31,40 L 39,32 M 46,37 H 66
            M 27,51 L 31,55 L 39,47 M 46,52 H 66
            M 27,66 L 31,70 L 39,62 M 46,67 H 54
            M 58,79 A 16,16 0 1 0 90,79 A 16,16 0 1 0 58,79 Z
            M 66,79 H 81 M 75,73 L 81,79 L 75,85
            """),
        ["BitLocker"] = Figure("""
            M 20,46 L 30,27 H 70 L 80,46
            M 85,57 V 50 Q 85,46 81,46 H 19 Q 15,46 15,50 V 71 Q 15,75 19,75 H 51
            M 24,63 H 25 M 34,63 H 35 M 44,63 H 45
            M 63,66 H 86 Q 90,66 90,70 V 85 Q 90,89 86,89 H 63 Q 59,89 59,85 V 70 Q 59,66 63,66 Z
            M 66,66 V 59 A 8.5,8.5 0 0 1 83,59 V 66 M 74.5,76 V 80
            """),
        ["Privacy"] = Figure("""
            M 16,20 H 84 Q 89,20 89,25 V 75 Q 89,80 84,80 H 16 Q 11,80 11,75 V 25 Q 11,20 16,20 Z
            M 11,35 H 89 M 20,28 H 21 M 29,28 H 30
            M 25,58 Q 34,46 44,45 M 56,45 Q 66,46 75,58 Q 65,71 54,72 M 44,72 Q 33,70 25,58
            M 42,55 A 8.5,8.5 0 0 1 51,49 M 58,60 A 8.5,8.5 0 0 1 49,67
            M 32,77 L 68,40
            """),
        ["Workstation"] = Figure("""
            M 8,14 H 65 Q 69,14 69,18 V 60 Q 69,64 65,64 H 8 Q 4,64 4,60 V 18 Q 4,14 8,14 Z
            M 9,18 H 64 V 60 H 9 Z
            M 30,64 V 71 L 24,73 H 49 L 43,71 V 64
            M 78,20 H 92 Q 96,20 96,24 V 69 Q 96,73 92,73 H 78 Q 74,73 74,69 V 24 Q 74,20 78,20 Z
            M 79,25 H 91 M 79,33 H 91 M 79,41 H 91
            M 82,60 A 3,3 0 1 0 88,60 A 3,3 0 1 0 82,60 Z
            M 12,77 H 61 Q 63,77 64,79 L 69,86 Q 70,88 67,88 H 8 Q 5,88 6,86 L 10,79 Q 11,77 12,77 Z
            M 14,80.5 H 18 M 22,80.5 H 25 M 29,80.5 H 32 M 36,80.5 H 39
            M 43,80.5 H 46 M 50,80.5 H 53 M 57,80.5 H 60
            M 12,84.5 H 16 M 20,84.5 H 24 M 28,84.5 H 46 M 50,84.5 H 54 M 58,84.5 H 63
            M 73,84 Q 71,80 75,79 Q 83,75 88,83 Q 92,89 83,89 Q 75,90 73,84 Z M 78,78 L 81,82
            """),
        ["SlowPC"] = Figure("""
            M 16,16 H 84 Q 89,16 89,21 V 64 Q 89,69 84,69 H 16 Q 11,69 11,64 V 21 Q 11,16 16,16 Z
            M 11,60 H 89 M 43,69 V 80 H 33 V 84 H 67 V 80 H 57 V 69
            M 22,46 H 34 L 42,27 L 52,51 L 60,38 L 68,46 H 78
            """),
        ["AppCrashes"] = Figure("""
            M 17,17 H 83 Q 89,17 89,23 V 77 Q 89,83 83,83 H 17 Q 11,83 11,77 V 23 Q 11,17 17,17 Z
            M 11,33 H 89 M 20,25 H 21 M 29,25 H 30 M 38,25 H 39
            M 20,60 H 31 L 39,46 L 44,56 M 56,65 L 61,73 L 69,59 H 80
            M 48,61 L 53,51 M 49,67 L 49.5,66
            """),
        ["Other"] = Figure("""
            M 63,43 V 23 Q 63,19 59,19 H 13 Q 9,19 9,23 V 57 Q 9,61 13,61 H 47
            M 9,53 H 49 M 31,61 V 72 H 23 V 76 H 49 V 72 H 41 V 61
            M 69,39 V 25 Q 69,22 72,22 H 85 Q 88,22 88,25 V 49 M 74,29 H 83 M 74,35 H 83
            M 51,61 A 17,17 0 1 0 85,61 A 17,17 0 1 0 51,61 Z M 80,73 L 92,86
            """)
    };
}
