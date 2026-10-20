using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class ImportedReportUiChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        var app = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(app.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var window = new KillerBench.MainWindow { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
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
}
