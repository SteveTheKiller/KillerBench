using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KillerBench.Services;

internal static class RenderedTypographyChecks
{
    private static readonly Dictionary<string, string> FontHashes = [];

    internal static void VerifyFallbackRejected()
    {
        var fallback = new TextBlock { Text = "Windows Maintenance Workbench", FontFamily = new FontFamily("Segoe UI"),
            FontSize = 18, Foreground = Brushes.White };
        fallback.Measure(new Size(500, 40));
        fallback.Arrange(new Rect(new Point(), fallback.DesiredSize));
        bool rejected = false;
        try { VerifyFont(fallback, "Killer Sans", "KillerSans-Regular.ttf"); }
        catch (InvalidOperationException error) when (error.Message.Contains("not fallback Segoe UI", StringComparison.Ordinal))
        { rejected = true; }
        Require(rejected, "Rendered font verification must reject the previous Segoe UI fallback");
    }

    internal static void VerifySubtitle(KillerBench.MainWindow window)
    {
        string subtitle = (string)window.FindResource("Str_Text_DIAGNOSTIC_WORKBENCH");
        var label = ((StackPanel)window.FindName("LogoBar")).Children.OfType<TextBlock>().Single(block => block.Text == subtitle);
        bool classic = ThemeManager.Current == Theme.SE98;
        Require(label.FontSize == (classic ? 11 : 18), "Subtitle retains its production filename font size");
        VerifyFont(label, classic ? "Microsoft Sans Serif" : "Killer Sans", classic ? null : "KillerSans-Regular.ttf");
    }

    internal static void VerifyFont(TextBlock label, string expectedFamily, string? productionFile)
    {
        // Inspect the real TextBlock drawing, including WPF fallback and font simulation.
        // Loading a second Typeface with a qualified URI cannot prove what this label rendered.
        label.UpdateLayout();
        var bitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(label.ActualWidth)),
            Math.Max(1, (int)Math.Ceiling(label.ActualHeight)), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(label);
        var runs = Glyphs(label).ToArray();
        Require(runs.Length > 0, "Rendered label contains inspectable glyph runs: " + label.Text);
        foreach (var run in runs)
        {
            var face = run.GlyphTypeface;
            Require(face.FamilyNames.Values.Contains(expectedFamily),
                $"Rendered {label.Text} uses {expectedFamily}, not fallback {string.Join(", ", face.FamilyNames.Values)} ({face.FontUri})");
            if (productionFile is null) continue;
            Require(face.FontUri.Scheme == "pack", "Rendered custom font comes from embedded resources");
            var productionUri = new Uri("pack://application:,,,/KillerBench;component/Fonts/" + productionFile);
            Require(Hash(face.FontUri) == Hash(productionUri), "Rendered font bytes match the production app font: " + productionFile);
        }
    }

    private static IEnumerable<GlyphRun> Glyphs(Visual visual)
    {
        foreach (var run in Glyphs(VisualTreeHelper.GetDrawing(visual))) yield return run;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(visual); index++)
            if (VisualTreeHelper.GetChild(visual, index) is Visual child)
                foreach (var run in Glyphs(child)) yield return run;
    }

    private static IEnumerable<GlyphRun> Glyphs(Drawing? drawing)
    {
        if (drawing is GlyphRunDrawing glyph) yield return glyph.GlyphRun;
        if (drawing is DrawingGroup group)
            foreach (Drawing child in group.Children)
                foreach (var run in Glyphs(child)) yield return run;
    }

    private static string Hash(Uri uri)
    {
        if (FontHashes.TryGetValue(uri.AbsoluteUri, out string? hash)) return hash;
        using Stream stream = Application.GetResourceStream(uri)?.Stream
            ?? throw new InvalidOperationException("Rendered font resource is unavailable: " + uri);
        hash = Convert.ToHexString(SHA256.HashData(stream));
        FontHashes.Add(uri.AbsoluteUri, hash);
        return hash;
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
