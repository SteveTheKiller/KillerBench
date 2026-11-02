using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KillerBench.Engine;

internal static class DemoScreenshotChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(
            new System.Windows.Threading.DispatcherSynchronizationContext(application.Dispatcher));
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Dark.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Themes/Instrument.xaml", UriKind.Relative) });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/KillerBench;component/Strings/en-US.xaml", UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        KillerBench.Services.ThemeManager.Apply(KillerBench.Services.Theme.Dark);
        KillerBench.Services.ThemeManager.ApplyAccent(KillerBench.Services.Theme.Dark, KillerBench.Services.Accent.Yellow);
        typeof(KillerBench.App).GetProperty("DemoMode")!.SetValue(null, true);
        typeof(KillerBench.App).GetProperty("ScreenshotMode")!.SetValue(null, true);

        KillerBench.Services.ThemeManager.GetSetting = _ => null;
        KillerBench.Services.ThemeManager.SetSetting = (_, _) => { };

        string output = Environment.GetEnvironmentVariable("KILLERBENCH_SCREENSHOT_OUTPUT") ??
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bench-landing", "screenshots"));
        Directory.CreateDirectory(output);
        var dark = KillerBench.Services.Theme.Dark;
        var light = KillerBench.Services.Theme.Light;
        var black = KillerBench.Services.Theme.Black;
        var A = (KillerBench.Services.Accent a) => (KillerBench.Services.Accent?)a;
        var shots = new Shot[]
        {
            new("01.png", WorkflowKind.CommonProblems, dark, A(KillerBench.Services.Accent.Yellow), Width: 1600, Height: 1000),
            new("02.png", WorkflowKind.WindowsUpdate, KillerBench.Services.Theme.Delirium, null),
            new("03.png", WorkflowKind.Network, light, A(KillerBench.Services.Accent.Orange), Grain: false, Width: 1280, Height: 800, PixelScale: 2),
            new("04.png", WorkflowKind.Printing, KillerBench.Services.Theme.SE98, null, Scale: 1.25, Width: 1280, Height: 850, PixelScale: 2),
            new("05.png", WorkflowKind.CoreWindowsServices, KillerBench.Services.Theme.Malaise, null, Scale: 1.1, Repair: true, Width: 1600, Height: 1000),
            new("06.png", WorkflowKind.CommonProblems, black, A(KillerBench.Services.Accent.Teal), Raw: true, Width: 1920, Height: 1080, PixelScale: 2),
            new("07.png", WorkflowKind.Defender, KillerBench.Services.Theme.Sepulchre, null, OutputHidden: true, Height: 1000),
            new("08.png", WorkflowKind.Depot, KillerBench.Services.Theme.Mourning, null, Height: 1000),
            new("09.png", WorkflowKind.Storage, KillerBench.Services.Theme.Ectoplasm, null, Scale: 0.9, Width: 1280, Height: 850, PixelScale: 2),
            new("10.png", WorkflowKind.BitLocker, light, A(KillerBench.Services.Accent.Green), Grain: false, Height: 960),
            new("11.png", WorkflowKind.CommonProblems, dark, A(KillerBench.Services.Accent.Blue), Shortcuts: true, Width: 1280),
            new("12.png", WorkflowKind.Certificates, KillerBench.Services.Theme.Greed, null, Scale: 1.2, Width: 1920, Height: 1080),
            new("matrix-privacy.png", WorkflowKind.Privacy, KillerBench.Services.Theme.Cyanotic, null, Scale: 1.25, OutputHidden: true, Height: 1000),
            new("matrix-maintenance.png", WorkflowKind.Cleanup, KillerBench.Services.Theme.Blood, null, Scale: 1.5, Width: 1600, Height: 1000),
            new("matrix-devices.png", WorkflowKind.DeviceProblems, KillerBench.Services.Theme.Decay, null, Scale: 2, Width: 1920, Height: 1080),
            new("matrix-small.png", WorkflowKind.CommonProblems, dark, A(KillerBench.Services.Accent.Blue), Scale: .7, Width: 800, Height: 500),
            new("matrix-zoom250.png", WorkflowKind.InstalledApps, light, A(KillerBench.Services.Accent.Purple), Scale: 2.5, Width: 1920, Height: 1080)
        };
        foreach (var shot in shots) RenderShot(shot, output);
        KillerBench.Services.ThemeManager.Apply(dark);
        File.WriteAllText(Path.Combine(output, "capture-manifest.json"), System.Text.Json.JsonSerializer.Serialize(shots,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: twelve gallery images plus five size, zoom and tool variants rendered without a cursor: " + output);
        application.Shutdown();
        return 0;
    }

    private sealed record Shot(string File, WorkflowKind Kind, KillerBench.Services.Theme Theme, KillerBench.Services.Accent? Accent,
        double Scale = 1, bool Grain = true, bool Raw = false, bool OutputHidden = false, bool Shortcuts = false, bool Repair = false,
        double Width = 1440, double Height = 900, double PixelScale = 1);

    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void Call(object target, string name, params object?[] args) =>
        (target.GetType().GetMethod(name, Private) ?? throw new InvalidOperationException("Missing " + name)).Invoke(target, args);

    private static void RenderShot(Shot shot, string output)
    {
        if (shot.Accent is { } accent) KillerBench.Services.ThemeManager.ApplyAccent(shot.Theme, accent);
        KillerBench.Services.ThemeManager.Apply(shot.Theme);
        var window = new KillerBench.Shell.MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowActivated = false,
            ShowInTaskbar = false,
            Left = SystemParameters.VirtualScreenLeft - 2000,
            Top = SystemParameters.VirtualScreenTop,
            Width = shot.Width,
            Height = shot.Height,
            MaxWidth = shot.Width,
            MaxHeight = shot.Height
        };
        bool grainToggled = false;
        try
        {
            window.Show();
            window.Width = shot.Width;
            window.Height = shot.Height;
            Settle(window);
            var workflowPicker = (ComboBox)window.FindName("MainWorkflow");
            var raw = (TextBox)window.FindName("EvidenceBox");
            var report = (StackPanel)window.FindName("ReportContent");
            Require(report.Children[0] is TextBlock initialHeading &&
                initialHeading.Text == "Full system scan demo report" &&
                window.FindName("ZoomViewport") is Grid { IsHitTestVisible: false } && !window.AllowDrop,
                "Screenshot launch opens a finished, read-only synthetic report");

            if (!shot.Grain) { Call(window, "ToggleFilmGrain"); grainToggled = true; }
            if (shot.Scale != 1) Call(window, "ApplyAppScale", shot.Scale);
            Settle(window);

            workflowPicker.SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == shot.Kind);
            var run = typeof(KillerBench.Shell.MainWindow).GetMethod("RunDemoWorkflowAsync", Private)
                ?? throw new InvalidOperationException("Missing demo workflow entry point.");
            var task = (Task?)run.Invoke(window, [shot.Kind, shot.Repair, true])
                ?? throw new InvalidOperationException("Demo workflow did not start.");
            Require(task.IsCompleted, "Screenshot fixture completes without live progress or providers");
            task.GetAwaiter().GetResult();
            Settle(window);
            RenderedTypographyChecks.VerifySubtitle(window);
            Require(raw.Text.Contains(DemoWorkflow.MachineName, StringComparison.Ordinal) &&
                !raw.Text.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                !raw.Text.Contains("Starting live checks", StringComparison.Ordinal),
                "Raw demo output contains only the fictional machine");
            Require(report.Children[0] is TextBlock heading && heading.Text.Contains("demo report", StringComparison.Ordinal),
                "Readable report identifies the synthetic demonstration");
            if (shot.Raw) Call(window, "ShowRawOutput");
            if (shot.OutputHidden) Call(window, "SetOutputOpen", false);
            if (shot.Shortcuts) Call(window, "OnShortcuts", window, new RoutedEventArgs());
            Thread.Sleep(180);
            Settle(window);
            Require(Math.Abs(window.ActualWidth - shot.Width) < 0.5 && Math.Abs(window.ActualHeight - shot.Height) < 0.5,
                "Screenshot window matches its requested size, was " + window.ActualWidth + "x" + window.ActualHeight);
            Require(((FrameworkElement)window.FindName("LeftPanel")).ActualWidth > 0,
                "Every screenshot retains the fixed category navigation");
            Capture(window, Path.Combine(output, shot.File), shot.PixelScale);
            Console.WriteLine(shot.File + ": " + shot.Width + "x" + shot.Height + ", zoom " + shot.Scale + ", pixel scale " + shot.PixelScale);
        }
        finally
        {
            if (grainToggled) Call(window, "ToggleFilmGrain");
            window.Close();
        }
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
    }

    private static void Capture(FrameworkElement visual, string path, double pixelScale)
    {
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth * pixelScale),
            (int)Math.Ceiling(visual.ActualHeight * pixelScale), 96 * pixelScale, 96 * pixelScale, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
