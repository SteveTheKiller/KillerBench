using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;

internal static class RailMenuGeometryChecks
{
    internal static void Run(KillerBench.Shell.MainWindow window)
    {
        var scale = typeof(KillerBench.Shell.MainWindow).GetMethod("ApplyAppScale",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        double originalLeft = window.Left, originalTop = window.Top;
        int selected = 0;
        try
        {
            foreach (double factor in new[] { .7, 1d, 1.5, 2.5 })
            {
                scale.Invoke(window, [factor]);
                foreach (bool screenEdge in new[] { false, true })
                {
                window.Left = screenEdge ? SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - window.ActualWidth - 8
                    : SystemParameters.VirtualScreenLeft + 8;
                window.UpdateLayout();
                foreach (string name in new[] { "ThemeButton", "LangButton" })
                {
                    var button = (Button)window.FindName(name);
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    var menu = button.ContextMenu!;
                    try
                    {
                        menu.UpdateLayout();
                        var card = (FrameworkElement)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(menu, 0), 0);
                        var pane = (FrameworkElement)window.FindName("ContentPane");
                        double pixel = VisualTreeHelper.GetDpi(window).DpiScaleX;
                        var visible = new Rect(card.PointToScreen(new Point()), card.PointToScreen(new Point(card.ActualWidth, card.ActualHeight)));
                        var paneBounds = new Rect(pane.PointToScreen(new Point()), pane.PointToScreen(new Point(pane.ActualWidth, pane.ActualHeight)));
                        double expectedLeft = paneBounds.Left + 8 * pixel;
                        var info = new MonitorInformation { Size = Marshal.SizeOf<MonitorInformation>() };
                        var handle = ((HwndSource)PresentationSource.FromVisual(menu)).Handle;
                        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) throw new Exception("Cannot measure the flyout work area.");
                        var work = new Rect(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
                        double menuScale = visible.Width / card.ActualWidth;
                        expectedLeft = Math.Clamp(expectedLeft, work.Left + card.Margin.Left * menuScale,
                            Math.Max(work.Left + card.Margin.Left * menuScale, work.Right - visible.Width - card.Margin.Right * menuScale));
                        double expectedTop = visible.Height > paneBounds.Height ? paneBounds.Top : paneBounds.Bottom - 8 * pixel - visible.Height;
                        expectedTop = Math.Clamp(expectedTop, work.Top + card.Margin.Top * menuScale,
                            Math.Max(work.Top + card.Margin.Top * menuScale, work.Bottom - visible.Height - card.Margin.Bottom * menuScale));
                        // Compare rendered coordinates independently of the placement callback.
                        bool AtPaneEdge(Rect bounds) => Math.Abs(bounds.Left - expectedLeft) <= 2 && Math.Abs(bounds.Top - expectedTop) <= 2;
                        if (!menu.IsOpen || !AtPaneEdge(visible))
                            throw new Exception($"{name} at {factor:P0} missed the pane edge: {visible}; pane {paneBounds}.");
                        var misplaced = visible;
                        misplaced.Offset(20 * pixel, 0);
                        if (AtPaneEdge(misplaced)) throw new Exception("Misplaced rail menu passed the geometry detector.");
                        if (visible.Left < work.Left - 2 || visible.Right > work.Right + 2 || visible.Top < work.Top - 2 || visible.Bottom > work.Bottom + 2)
                            throw new Exception($"{name} at {factor:P0} escaped its screen work area: {visible}; screen {work}.");
                        selected++;
                    }
                    finally { menu.IsOpen = false; }
                }
                }
            }
            Console.WriteLine($"PASS: {selected} rendered bottom-rail menu scale cases and independent misplaced-menu controls.");
        }
        finally
        {
            scale.Invoke(window, [1d]);
            window.Left = originalLeft;
            window.Top = originalTop;
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInformation { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInformation information);
}
