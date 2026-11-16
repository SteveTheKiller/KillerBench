using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using KillerBench.Services;

internal static class SharedKitChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        var app = new Application();
        var styles = new ResourceDictionary
        {
            Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative)
        };
        app.Resources.MergedDictionaries.Add(styles);
        var host = new ContentControl();
        var window = new Window { Content = host, Width = 400, Height = 200, ShowInTaskbar = false };
        int count = 0;
        try
        {
            window.Show();
            int detectorControls = CalibrateDetector(host, window);
            foreach (var theme in Enum.GetValues<Theme>())
            {
                ThemeManager.Apply(theme);
                foreach (DictionaryEntry entry in styles)
                {
                    if (entry.Value is not Style style || !typeof(ButtonBase).IsAssignableFrom(style.TargetType)
                        || style.TargetType.IsAbstract) continue;
                    var button = (ButtonBase)Activator.CreateInstance(style.TargetType)!;
                    button.Style = style;
                    button.Content = "Test";
                    host.Content = button;
                    window.UpdateLayout();
                    button.ApplyTemplate();
                    RejectEffects(button, theme + "/" + entry.Key);
                    button.IsEnabled = false;
                    window.UpdateLayout();
                    RejectEffects(button, theme + "/" + entry.Key + "/disabled");
                    count++;
                }
            }
            if (count == 0) throw new Exception("No button styles were checked.");
            Console.WriteLine($"PASS: {count} rendered button style/theme combinations have no shadow or blur in normal and disabled states; {detectorControls} injected-effect controls were detected and passed after removal.");
            return 0;
        }
        finally { window.Close(); app.Shutdown(); }
    }

    private static int CalibrateDetector(ContentControl host, Window window)
    {
        string retained = Environment.GetEnvironmentVariable("KILLERBENCH_PICKER_EVIDENCE") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scratch", "KillerBench-shadow-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(retained);
        var caption = new TextBlock { Text = "Detector calibration", Foreground = Brushes.Black };
        var button = new Button { Content = caption, Background = Brushes.White, Width = 220, Height = 64 };
        host.Content = button;
        button.ApplyTemplate();
        window.UpdateLayout();
        RejectEffects(button, "calibration/absent");
        SaveCalibration("shadow-control-absent.png");
        int detected = 0;
        foreach (bool nested in new[] { false, true })
        foreach (bool blur in new[] { false, true })
        {
            UIElement target = nested ? caption : button;
            target.Effect = blur ? new BlurEffect { Radius = 4 } : new DropShadowEffect { BlurRadius = 8, ShadowDepth = 4 };
            window.UpdateLayout();
            bool rejected = false;
            try { RejectEffects(button, "calibration/present"); }
            catch (Exception error) when (error.Message.Contains("contains a button shadow or blur")) { rejected = true; }
            if (!rejected) throw new Exception("The shadow detector missed a rendered injected effect.");
            SaveCalibration($"shadow-control-{(nested ? "caption" : "button")}-{(blur ? "blur" : "shadow")}.png");
            target.Effect = null;
            window.UpdateLayout();
            RejectEffects(button, "calibration/removed");
            detected++;
        }
        return detected;

        void SaveCalibration(string name)
        {
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(retained, name));
            encoder.Save(output);
        }
    }

    private static void RejectEffects(DependencyObject node, string context)
    {
        if (node is UIElement { Effect: DropShadowEffect or BlurEffect })
            throw new Exception(context + " contains a button shadow or blur.");
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            RejectEffects(VisualTreeHelper.GetChild(node, i), context);
    }
}
