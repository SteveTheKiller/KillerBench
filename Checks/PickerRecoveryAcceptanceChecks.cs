using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KillerBench.Controls;

internal static class PickerRecoveryAcceptanceChecks
{
    internal static void Run(KillerBench.Shell.MainWindow window)
    {
        IsolatedDesktop.Verify();
        string scratch = Environment.GetEnvironmentVariable("KILLERBENCH_PICKER_SCRATCH") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scratch", "KillerBench-picker-acceptance-" + Guid.NewGuid().ToString("N"));
        string retained = Environment.GetEnvironmentVariable("KILLERBENCH_PICKER_EVIDENCE") ?? Path.Combine(scratch, "retained");
        scratch = Path.GetFullPath(scratch);
        retained = Path.GetFullPath(retained);
        Directory.CreateDirectory(scratch);
        Directory.CreateDirectory(retained);
        string file = Path.Combine(scratch, "ready.txt");
        File.WriteAllText(file, "Synthetic picker fixture.");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int probes = 0, closed = 0;
        var picker = new FileDialog(FileDialogMode.Open)
        {
            InitialDirectory = scratch, Filter = "Text files|*.txt", FileName = "ready.txt",
            FileExistsProbe = _ =>
            {
                int attempt = Interlocked.Increment(ref probes);
                if (attempt != 1) return true;
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Delayed fixture probe was not released.");
                return false;
            }
        };
        picker.Closed += (_, _) => closed++;
        bool? accepted = Show(window, picker, dialog =>
        {
            var name = (TextBox)dialog.FindName("FileNameBox");
            var accept = (Button)dialog.FindName("AcceptButton");
            Wait(() => dialog.IsDirectoryListingAvailable && accept.IsEnabled, "initial picker readiness");
            Enter(name);
            Wait(() => entered.IsSet, "delayed filesystem probe");
            Require(!accept.IsEnabled, "Accept must be disabled while a filesystem answer is pending.");
            for (int index = 0; index < 20; index++) Enter(name);
            PumpFor(TimeSpan.FromMilliseconds(150));
            Require(Volatile.Read(ref probes) == 1 && dialog.IsVisible && closed == 0,
                "Repeated Enter must not duplicate an in-flight file probe or accept the dialog.");
            Capture(dialog, Path.Combine(retained, "delayed-enter-busy.png"));
            release.Set();
            Wait(() => accept.IsEnabled, "accept control recovery after unavailable file");
            Require(dialog.IsVisible && dialog.FileNames.Length == 0, "A negative filesystem answer must keep the picker usable.");
            Capture(dialog, Path.Combine(retained, "delayed-enter-recovered.png"));
            Enter(name);
        });
        Require(accepted == true && probes == 2 && closed == 1 && picker.FileNames.Length == 1 &&
            string.Equals(picker.FileName, file, StringComparison.OrdinalIgnoreCase),
            "A retry after recovery must accept exactly one chosen file.");
        string corrupt = Path.Combine(scratch, "corrupt-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(corrupt, [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0]);
        using (var held = new FileStream(corrupt, FileMode.Open, FileAccess.Read, FileShare.Read))
            Require(!CanOpenExclusive(corrupt), "The exclusive-access detector must reject a deliberately held read handle.");
        Require(CanOpenExclusive(corrupt), "The exclusive-access detector must recover after its positive handle control closes.");
        bool priorThumbnails = PickerEntry.ShowThumbnails;
        try
        {
            PickerEntry.ShowThumbnails = true;
            bool settled = false;
            var entry = new PickerEntry(Path.GetFileName(corrupt), corrupt, false, new FileInfo(corrupt).Length, File.GetLastWriteTime(corrupt));
            entry.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(PickerEntry.IconLarge)) settled = true; };
            _ = entry.IconLarge;
            Wait(() => settled, "asynchronous corrupt thumbnail answer");
            Require(CanOpenExclusive(corrupt), "A failed, settled thumbnail decode must not retain a source file handle.");
            string replaced = corrupt + ".replaced";
            File.Move(corrupt, replaced);
            File.WriteAllText(corrupt, "Replacement after failed thumbnail decode.");
            Require(File.ReadAllText(corrupt).StartsWith("Replacement") && File.Exists(replaced),
                "A corrupt image must remain movable and replaceable immediately after the thumbnail answer.");
        }
        finally { PickerEntry.ShowThumbnails = priorThumbnails; }
        string validImage = Path.Combine(scratch, "valid-preview.png");
        var pixels = Enumerable.Repeat((byte)255, 8 * 8 * 4).ToArray();
        var source = BitmapSource.Create(8, 8, 96, 96, PixelFormats.Bgra32, null, pixels, 8 * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (var output = File.Create(validImage)) encoder.Save(output);
        string invalidPreview = Path.Combine(scratch, "invalid-preview-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(invalidPreview, [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0]);
        var previewPicker = new FileDialog(FileDialogMode.Open)
        { InitialDirectory = scratch, Filter = "PNG images|*.png", ShowImagePreview = true };
        Show(window, previewPicker, dialog =>
        {
            Wait(() => dialog.IsDirectoryListingAvailable && dialog.Entries.Any(e => e.FullPath == invalidPreview), "preview picker listing");
            var list = (ListBox)dialog.FindName("FileList");
            var preview = (Image)dialog.FindName("ImagePreview");
            list.SelectedItem = dialog.Entries.Single(e => e.FullPath == validImage);
            Wait(() => preview.Source is not null, "valid image preview positive control");
            Require(CanOpenExclusive(validImage), "The valid OnLoad preview must release its source.");
            list.SelectedItem = dialog.Entries.Single(e => e.FullPath == invalidPreview);
            PumpFor(TimeSpan.FromMilliseconds(500));
            Require(preview.Source is null, "An invalid image must leave the neutral preview visible.");
            Capture(dialog, Path.Combine(retained, "invalid-image-preview.png"));
            list.SelectedItem = dialog.Entries.Single(e => e.FullPath == validImage);
            Wait(() => preview.Source is not null, "valid preview after failed decode");
            dialog.Close();
        });
        Require(CanOpenExclusive(invalidPreview), "A failed preview decode must not retain the image file after another preview loads and the picker closes.");
        Console.WriteLine("PASS: 2 completion-bounded picker recovery cases (delayed Enter and failed thumbnail), plus a valid/invalid/valid preview sequence and post-close exclusive-access observation. Failed-preview completion has no public signal. Captures: " + retained);
    }

    private static bool CanOpenExclusive(string path)
    {
        try { using var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return true; }
        catch (IOException) { return false; }
    }

    private static bool? Show(Window owner, FileDialog picker, Action<FileDialog> inspect)
    {
        Exception? failure = null;
        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        timeout.Tick += (_, _) => { failure ??= new TimeoutException("Picker acceptance exceeded 12 seconds."); picker.Close(); };
        picker.Loaded += (_, _) => picker.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
            try { inspect(picker); }
            catch (Exception error) { failure = error; picker.Close(); }
        }));
        timeout.Start();
        try
        {
            bool? result = picker.ShowDialog(owner);
            if (failure is not null) throw new InvalidOperationException("Picker recovery acceptance failed.", failure);
            return result;
        }
        finally { timeout.Stop(); }
    }

    private static void Enter(UIElement target)
    {
        var source = PresentationSource.FromVisual(target) ?? throw new InvalidOperationException("Picker has no presentation source.");
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter)
        { RoutedEvent = Keyboard.KeyDownEvent });
    }

    private static void Wait(Func<bool> complete, string stage)
    {
        var elapsed = Stopwatch.StartNew();
        while (!complete() && elapsed.Elapsed < TimeSpan.FromSeconds(5)) PumpFor(TimeSpan.FromMilliseconds(5));
        Require(complete(), "Timed out during " + stage + ".");
    }

    private static void PumpFor(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void Capture(FrameworkElement visual, string path)
    {
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth), (int)Math.Ceiling(visual.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(path);
        encoder.Save(output);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
