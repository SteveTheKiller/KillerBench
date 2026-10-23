using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KillerBench.Controls;
using KillerBench.Services;

internal static class FilePickerListingChecks
{
    public static int Run()
    {
        var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/Themes/Dark.xaml", UriKind.Relative) });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/Controls/Controls.xaml", UriKind.Relative) });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/Strings/en-US.xaml", UriKind.Relative) });
        ThemeManager.GetSetting = _ => null;
        ThemeManager.SetSetting = (_, _) => { };
        ThemeManager.Apply(Theme.Dark);
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        string root = Path.Combine(Path.GetTempPath(), "KillerBenchPickerNoWindow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var picker = new FileDialog();
        int pulses = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => pulses++;
        try
        {
            NoPresentation(picker);
            picker.DirectoryListingReader = (_, _, token) =>
            {
                entered.Set(); release.Wait(TimeSpan.FromSeconds(5));
                return new PickerListingResult([new PickerEntry("old.txt", Path.Combine(root, "old.txt"), false, 1, DateTime.MinValue)], true, true);
            };
            timer.Start();
            var old = picker.NavigateDirectoryAsync(root);
            Pump(() => entered.IsSet && pulses > 0);
            if (old.IsCompleted || !picker.IsDirectoryListingBusy || ((Button)picker.FindName("AcceptButton")).IsEnabled)
                throw new Exception("S008 slow listing blocked the dispatcher or allowed premature acceptance.");
            picker.DirectoryListingReader = (_, _, _) => new PickerListingResult(
                Enumerable.Range(0, 513).Select(index => new PickerEntry(index + ".txt", Path.Combine(root, index + ".txt"), false, index, DateTime.MinValue)).ToArray(), true, true);
            var current = picker.NavigateDirectoryAsync(root);
            Pump(() => current.IsCompleted);
            current.GetAwaiter().GetResult();
            release.Set(); Pump(() => old.IsCompleted);
            if (!picker.IsDirectoryListingAvailable || !picker.IsDirectoryListingComplete || picker.Entries.Count != 513 || picker.Entries.Any(row => row.Name == "old.txt"))
                throw new Exception("S008 current navigation did not replace a canceled generation.");
            NoPresentation(picker);
            using var closeEntered = new ManualResetEventSlim();
            using var closeRelease = new ManualResetEventSlim();
            picker.DirectoryListingReader = (_, _, _) => { closeEntered.Set(); closeRelease.Wait(TimeSpan.FromSeconds(5)); return new PickerListingResult([], true, true); };
            var closing = picker.NavigateDirectoryAsync(root);
            Pump(() => closeEntered.IsSet);
            picker.Close(); closeRelease.Set(); Pump(() => closing.IsCompleted);
            if (picker.IsDirectoryListingBusy || picker.Entries.Count != 0) throw new Exception("S008 closed dialog received stale publication.");
            NoPresentation(picker);
            Console.WriteLine("PASS S008: real picker public navigation stays responsive, rejects stale and closed publications, and completes513 rows without showing a window.");
        }
        finally { timer.Stop(); release.Set(); if (!picker.IsDirectoryListingBusy) picker.Close(); Directory.Delete(root, true); }
        return 0;
    }

    private static void NoPresentation(Window window)
    {
        if (window.IsVisible || PresentationSource.FromVisual(window) is not null)
            throw new Exception("No-window picker check attached a presentation source.");
    }

    private static void Pump(Func<bool> complete)
    {
        var timeout = Stopwatch.StartNew();
        while (!complete())
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(8)) throw new TimeoutException("Picker fixture did not finish.");
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            Thread.Sleep(1);
        }
    }
}
