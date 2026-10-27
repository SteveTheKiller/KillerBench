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

        string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bench-landing", "screenshots"));
        Require(Directory.Exists(output), "Landing screenshot folder exists: " + output);
        var dark = KillerBench.Services.Theme.Dark;
        var light = KillerBench.Services.Theme.Light;
        var black = KillerBench.Services.Theme.Black;
        var A = (KillerBench.Services.Accent a) => (KillerBench.Services.Accent?)a;
        var shots = new Shot[]
        {
            new("01.png", WorkflowKind.CommonProblems, dark, A(KillerBench.Services.Accent.Yellow)),
            new("02.png", WorkflowKind.WindowsUpdate, KillerBench.Services.Theme.Delirium, null),
            new("03.png", WorkflowKind.Network, light, A(KillerBench.Services.Accent.Orange), Grain: false),
            new("04.png", WorkflowKind.Printing, KillerBench.Services.Theme.SE98, null),
            new("05.png", WorkflowKind.CoreWindowsServices, KillerBench.Services.Theme.Malaise, null, Scale: 1.1, Repair: true),
            new("06.png", WorkflowKind.CommonProblems, black, A(KillerBench.Services.Accent.Teal), Raw: true),
            new("07.png", WorkflowKind.Defender, KillerBench.Services.Theme.Sepulchre, null, Collapse: true),
            new("08.png", WorkflowKind.Depot, KillerBench.Services.Theme.Mourning, null),
            new("09.png", WorkflowKind.Storage, KillerBench.Services.Theme.Ectoplasm, null, Scale: 0.9),
            new("10.png", WorkflowKind.BitLocker, light, A(KillerBench.Services.Accent.Green), Grain: false),
            new("11.png", WorkflowKind.CommonProblems, dark, A(KillerBench.Services.Accent.Blue), Shortcuts: true),
            new("12.png", WorkflowKind.Certificates, KillerBench.Services.Theme.Greed, null, Scale: 1.2)
        };
        foreach (var shot in shots) RenderShot(shot, output);
        KillerBench.Services.ThemeManager.Apply(dark);
        Console.WriteLine("PASS: twelve synthetic screenshots rendered on an isolated desktop: " + output);
        application.Shutdown();
        return 0;
    }

    private sealed record Shot(string File, WorkflowKind Kind, KillerBench.Services.Theme Theme, KillerBench.Services.Accent? Accent,
        double Scale = 1, bool Grain = true, bool Raw = false, bool Collapse = false, bool Shortcuts = false, bool Repair = false);

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
            Width = 1440,
            Height = 900,
            MaxWidth = 1440,
            MaxHeight = 900
        };
        bool grainToggled = false;
        try
        {
            window.Show();
            window.Width = 1440;
            window.Height = 900;
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
            if (shot.Collapse)
            {
                Call(window, "CollapseSidebar");
                var left = (FrameworkElement)window.FindName("LeftPanel");
                left.BeginAnimation(FrameworkElement.WidthProperty, null);
                left.Width = 0;
            }
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
            if (shot.Shortcuts) Call(window, "OnShortcuts", window, new RoutedEventArgs());
            Settle(window);
            Require(Math.Abs(window.ActualWidth - 1440) < 0.5 && Math.Abs(window.ActualHeight - 900) < 0.5,
                "Screenshot window is exactly 1440 by 900, was " + window.ActualWidth + "x" + window.ActualHeight);
            Capture(window, Path.Combine(output, shot.File));
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

    private static void Capture(FrameworkElement visual, string path)
    {
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth),
            (int)Math.Ceiling(visual.ActualHeight), 96, 96, PixelFormats.Pbgra32);
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
