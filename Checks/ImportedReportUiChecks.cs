using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class ImportedReportUiChecks
{
    internal static int Run(bool verifyScaling = false)
    {
        IsolatedDesktop.Verify();
        var app = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(app.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var window = new KillerBench.Shell.MainWindow { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        string scratch = Path.Combine(Path.GetTempPath(), "import-report-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            window.Show();
            window.Left = SystemParameters.VirtualScreenLeft - window.Width - 200;
            window.UpdateLayout();
            string log = Path.Combine(scratch, "CBS.log");
            File.WriteAllText(log, "2026-09-01 10:00:00, Error CBS Failed to resolve package 0x800f081f\n");
            Open(log);
            var pane = (Border)window.FindName("ReportPane");
            if (!pane.IsVisible || !Text(pane).Any(text => text.Contains("0x800F081F", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Imported findings did not populate the visible readable report.");
            var evidenceButton = Buttons(pane).FirstOrDefault(button => Equals(button.Content, "Show evidence"));
            if (evidenceButton is null) throw new Exception("Imported readable finding has no evidence navigation.");
            if (verifyScaling) VerifyScale(window, pane, evidenceButton);
            evidenceButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!((Border)window.FindName("ConsolePane")).IsVisible || !((TextBox)window.FindName("EvidenceBox")).Text.Contains("1  "))
                throw new Exception("Imported finding did not open original numbered evidence.");
            string empty = Path.Combine(scratch, "empty.log");
            File.WriteAllText(empty, "Harmless fixture with no supported finding.");
            Open(empty);
            if (!pane.IsVisible || !Text(pane).Any(text => text.Contains("No supported rule matched")))
                throw new Exception("No-findings import left a blank or stale readable report.");
            Console.WriteLine("PASS: imported findings, evidence navigation and no-findings reports on isolated desktop.");
            return 0;

            void Open(string path)
            {
                ((TextBox)window.FindName("PathBox")).Text = path;
                var open = (Button)window.FindName("OpenButton");
                open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (!open.IsEnabled && watch.Elapsed < TimeSpan.FromSeconds(20))
                {
                    var frame = new DispatcherFrame();
                    window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                    Dispatcher.PushFrame(frame);
                    Thread.Sleep(5);
                }
                if (!open.IsEnabled) throw new Exception("Evidence import did not finish within its fixture deadline.");
                window.UpdateLayout();
            }
        }
        finally { window.Close(); app.Shutdown(); Directory.Delete(scratch, true); }
    }

    private static IEnumerable<string> Text(DependencyObject root)
    {
        if (root is TextBlock block) yield return block.Text;
        foreach (var child in Children(root)) foreach (string value in Text(child)) yield return value;
    }
    private static IEnumerable<Button> Buttons(DependencyObject root)
    {
        if (root is Button button) yield return button;
        foreach (var child in Children(root)) foreach (var value in Buttons(child)) yield return value;
    }
    private static IEnumerable<DependencyObject> Children(DependencyObject root) => LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>();

    private static void VerifyScale(KillerBench.Shell.MainWindow window, Border pane, Button evidenceButton)
    {
        var icon = (FrameworkElement)window.FindName("TitlebarIcon");
        var heading = ((StackPanel)window.FindName("ReportContent")).Children.OfType<TextBlock>().First();
        var run = (Button)window.FindName("MainGo");
        double headingFont = heading.FontSize, buttonFont = run.FontSize;
        double titleHeight = ((FrameworkElement)window.FindName("CaptionBar")).ActualHeight;
        void Wheel(int direction) => icon.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(
            System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, direction * 120)
            { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent });
        for (int i = 0; i < 200; i++) Wheel(-1);
        int current = 70;
        foreach (double width in new[] { 1025d, 800d })
        {
            window.Width = width;
            foreach (int percent in new[] { 100, 150, 250 })
            {
                while (current != percent) { int direction = Math.Sign(percent - current); Wheel(direction); current += direction * 2; }
                window.UpdateLayout();
                double scale = percent / 100d;
                var headingTransform = heading.TransformToAncestor(window);
                var actionTransform = run.TransformToAncestor(window);
                double headingScale = headingTransform.Transform(new Point(1, 0)).X - headingTransform.Transform(new Point()).X;
                double actionScale = actionTransform.Transform(new Point(1, 0)).X - actionTransform.Transform(new Point()).X;
                if (Math.Abs(heading.FontSize * headingScale - headingFont * scale) > .1 ||
                    Math.Abs(run.FontSize * actionScale - buttonFont * scale) > .1)
                    throw new Exception("Interface zoom did not scale report headings and action controls at " + percent + "%.");
                if (!pane.IsVisible || Math.Abs(((FrameworkElement)window.FindName("CaptionBar")).ActualHeight - titleHeight) > .1 ||
                    Math.Abs(icon.ActualHeight - 27) > .1)
                    throw new Exception("Content zoom changed fixed titlebar geometry or hid the report.");
                var output = (ScrollViewer)window.FindName("OutputScroll");
                var sidebar = (FrameworkElement)window.FindName("LeftPanel");
                var grip = (System.Windows.Controls.Primitives.Thumb)window.FindName("SidebarGrip");
                double before = sidebar.Width * scale;
                grip.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(10, 0)
                    { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragDeltaEvent });
                if (Math.Abs(sidebar.Width * scale - before) > .1 || grip.Visibility != Visibility.Collapsed)
                    throw new Exception("Fixed navigation changed width through its inactive grip at " + percent + "%.");
                grip.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(-10, 0)
                    { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragDeltaEvent });
                var export = (Button)window.FindName("ExportReportButton");
                export.BringIntoView();
                var frame = new DispatcherFrame();
                window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                window.UpdateLayout();
                var exportBounds = export.TransformToAncestor(output).TransformBounds(new Rect(export.RenderSize));
                if (exportBounds.Right > output.ActualWidth + 1 || exportBounds.Left < -1)
                    throw new Exception("Export action cannot be reached through the scaled output viewport: " + percent + "% width " + width + ", bounds " + exportBounds + ", viewport " + output.ActualWidth + ", offset " + output.HorizontalOffset + ", extent " + output.ExtentWidth);
                output.ScrollToLeftEnd();
                window.UpdateLayout();
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),
                    (int)Math.Ceiling(window.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using var capture = File.Create(Path.Combine(Path.GetTempPath(), "killerbench-scale-" + width + "-" + percent + ".png"));
                encoder.Save(capture);
            }
        }
        while (current != 100) { Wheel(-1); current -= 2; }
        window.Width = 1025;
        window.UpdateLayout();
        Console.WriteLine("PASS: report and action text scales at 100%, 150%, and 250% at normal and narrow widths.");
    }
}
