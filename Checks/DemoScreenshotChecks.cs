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
        var application = new Application();
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

        string output = Path.Combine(Path.GetTempPath(), "KillerBench-demo-screenshots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var window = new KillerBench.MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowActivated = false,
            ShowInTaskbar = false,
            Left = SystemParameters.VirtualScreenLeft - 2000,
            Top = SystemParameters.VirtualScreenTop
        };
        try
        {
            window.Show();
            var run = typeof(KillerBench.MainWindow).GetMethod("RunDemoWorkflowAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Missing demo workflow entry point.");
            var showRawOutput = typeof(KillerBench.MainWindow).GetMethod("ShowRawOutput", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Missing raw output view.");
            var workflowPicker = (ComboBox)window.FindName("MainWorkflow");
            var raw = (TextBox)window.FindName("EvidenceBox");
            var report = (StackPanel)window.FindName("ReportContent");
            Require(report.Children[0] is TextBlock initialHeading &&
                initialHeading.Text == "Full system scan demo report" &&
                window.FindName("ZoomViewport") is Grid { IsHitTestVisible: false } && !window.AllowDrop,
                "Screenshot launch opens a finished, read-only synthetic report");

            foreach ((WorkflowKind kind, string fileName) in new[]
            {
                (WorkflowKind.CommonProblems, "full-system-scan.png"),
                (WorkflowKind.WindowsUpdate, "windows-update.png"),
                (WorkflowKind.Network, "network.png"),
                (WorkflowKind.Printing, "printing.png")
            })
            {
                workflowPicker.SelectedItem = WorkflowCatalog.All.Single(item => item.Kind == kind);
                var task = (Task?)run.Invoke(window, [kind, false, true])
                    ?? throw new InvalidOperationException("Demo workflow did not start.");
                Require(task.IsCompleted, "Screenshot fixture completes without live progress or providers");
                task.GetAwaiter().GetResult();
                window.UpdateLayout();
                RenderedTypographyChecks.VerifySubtitle(window);
                Require(raw.Text.Contains(DemoWorkflow.MachineName, StringComparison.Ordinal) &&
                    !raw.Text.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                    !raw.Text.Contains("Starting live checks", StringComparison.Ordinal),
                    "Raw demo output contains only the fictional machine");
                Require(report.Children[0] is TextBlock heading && heading.Text.Contains("demo report", StringComparison.Ordinal),
                    "Readable report identifies the synthetic demonstration");
                Capture(window, Path.Combine(output, fileName));
                if (kind != WorkflowKind.CommonProblems) continue;
                showRawOutput.Invoke(window, []);
                window.UpdateLayout();
                Capture(window, Path.Combine(output, "raw-output.png"));
                ((Button)window.FindName("ReportViewButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            Console.WriteLine("PASS: five synthetic screenshots rendered on an isolated desktop: " + output);
            return 0;
        }
        finally
        {
            window.Close();
            application.Shutdown();
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
