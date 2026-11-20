using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KillerBench.Engine;

internal static class CaptureBoundaryChecks
{
    internal static async Task RunCliAsync(string scratch)
    {
        Directory.CreateDirectory(scratch);
        string path = Path.Combine(scratch, "malformed.kbench");
        await File.WriteAllTextAsync(path, "{\"schemaVer");
        var result = await OpenCliAsync(path);
        Require(result.Exit == 1 && result.Error.Trim() == "Invalid KillerBench capture." && result.Output.Length == 0,
            $"Malformed capture must give one controlled CLI error. Actual exit {result.Exit}: {result.Error}");
        string schema = Path.Combine(scratch, "schema.kbench");
        await File.WriteAllTextAsync(schema, JsonSerializer.Serialize(Session() with { SchemaVersion = 999 }, Capture.JsonOptions));
        await Reject(schema, "Unsupported or invalid capture metadata.");
        string metadata = Path.Combine(scratch, "metadata.kbench");
        await File.WriteAllTextAsync(metadata, JsonSerializer.Serialize(Session() with { Machine = null! }, Capture.JsonOptions));
        await Reject(metadata, "Unsupported or invalid capture metadata.");
        string huge = Path.Combine(scratch, "huge.kbench");
        using (var file = File.Create(huge)) file.SetLength(64L * 1024 * 1024 + 1);
        await Reject(huge, "Capture exceeds the 64 MB limit.");
        string locked = Path.Combine(scratch, "locked.kbench");
        await File.WriteAllTextAsync(locked, "{}");
        using (var file = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            await Reject(locked, null);
        string valid = Path.Combine(scratch, "valid.kbench");
        await Capture.SaveAsync(Session(), valid);
        var accepted = await OpenCliAsync(valid);
        Require(accepted.Exit == 0 && accepted.Error.Length == 0, "Valid CLI capture must remain usable.");
        using var report = JsonDocument.Parse(accepted.Output);
        Require(report.RootElement.GetProperty("machine").GetString() == "Synthetic" &&
            report.RootElement.GetProperty("sources").GetArrayLength() == 1,
            "Valid capture CLI round trip must preserve metadata and evidence.");
        await CaptureChecks.RunAsync(scratch);
        Console.WriteLine("PASS: 6 CLI capture boundary cases (malformed JSON, unsupported schema, invalid metadata, oversized, inaccessible, valid round trip), plus existing CaptureChecks.");

        async Task Reject(string rejected, string? message)
        {
            var actual = await OpenCliAsync(rejected);
            Require(actual.Exit == 1 && actual.Output.Length == 0 && actual.Error.Trim().Length > 0 &&
                !actual.Error.Contains("Exception") && !actual.Error.Contains("   at ") &&
                (message is null || actual.Error.Trim() == message),
                $"Rejected capture must give a controlled CLI error. Actual exit {actual.Exit}: {actual.Error}");
        }
    }

    internal static int RunDesktop(string scratch)
    {
        IsolatedDesktop.Verify();
        Directory.CreateDirectory(scratch);
        var application = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        KillerBench.Services.ThemeManager.Initialize();
        var window = new KillerBench.Shell.MainWindow();
        Exception? escaped = null;
        application.DispatcherUnhandledException += (_, e) => { escaped = e.Exception; e.Handled = true; };
        try
        {
            window.Show();
            var session = Session();
            string valid = Path.Combine(scratch, "valid.kbench");
            Capture.SaveAsync(session, valid).GetAwaiter().GetResult();
            Open(valid);
            string prior = ((TextBox)window.FindName("EvidenceBox")).Text;
            string priorInfo = ((TextBlock)window.FindName("CaptureInfo")).Text;
            int priorSources = ((ListBox)window.FindName("SourcesList")).Items.Count;
            Require(priorSources > 0 && window.SaveCaptureEnabled,
                "Valid capture must be loaded before malformed input.");
            string malformed = Path.Combine(scratch, "malformed.kbench");
            File.WriteAllText(malformed, "{\"schemaVer");
            Reject(malformed, "Invalid KillerBench capture.");
            string schema = Path.Combine(scratch, "schema.kbench");
            File.WriteAllText(schema, JsonSerializer.Serialize(session with { SchemaVersion = 999 }, Capture.JsonOptions));
            Reject(schema, "Unsupported or invalid capture metadata.");
            string metadata = Path.Combine(scratch, "metadata.kbench");
            File.WriteAllText(metadata, JsonSerializer.Serialize(session with { Machine = null! }, Capture.JsonOptions));
            Reject(metadata, "Unsupported or invalid capture metadata.");
            string huge = Path.Combine(scratch, "huge.kbench");
            using (var file = File.Create(huge)) file.SetLength(64L * 1024 * 1024 + 1);
            Reject(huge, "Capture exceeds the 64 MB limit.");
            string locked = Path.Combine(scratch, "locked.kbench");
            File.WriteAllText(locked, "{}");
            using (var file = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Reject(locked, null);
            string cancel = Path.Combine(scratch, "cancel.kbench");
            File.WriteAllText(cancel, new string(' ', 32 * 1024 * 1024) + JsonSerializer.Serialize(session, Capture.JsonOptions));
            Reject(cancel, KillerBench.Controls.LocalizedText.Get("Str_Ui_ReadStopped"), cancelRead: true);
            Open(valid);
            Require(escaped is null && ((ListBox)window.FindName("SourcesList")).Items.Count == priorSources,
                "A valid capture must still open after failure.");
            Console.WriteLine("PASS: 6 desktop rejected/canceled capture cases, prior evidence preservation and valid open recovery.");
            return 0;

            void Reject(string path, string? message, bool cancelRead = false)
            {
                string activity = ((TextBox)window.FindName("ActivityLog")).Text;
                Open(path, cancelRead);
                string after = ((TextBox)window.FindName("ActivityLog")).Text;
                Require(escaped is null, "Open capture leaked an exception: " + escaped);
                Require(after.Length > activity.Length && (message is null || after[activity.Length..].Contains(message)),
                    "Open capture must explain the rejected or canceled input.");
                Require(window.IsVisible && ((Button)window.FindName("OpenButton")).IsEnabled &&
                    window.SaveCaptureEnabled &&
                    ((TextBox)window.FindName("EvidenceBox")).Text == prior &&
                    ((TextBlock)window.FindName("CaptureInfo")).Text == priorInfo &&
                    ((ListBox)window.FindName("SourcesList")).Items.Count == priorSources,
                    "Failed open must preserve the live desktop and prior evidence.");
            }
        }
        finally { window.Close(); application.Shutdown(); }

        void Open(string path, bool cancelRead = false)
        {
            Exception? pickerError = null;
            bool selected = false;
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, (_, _) =>
            {
                var picker = application.Windows.OfType<KillerBench.Controls.FileDialog>().FirstOrDefault();
                if (picker is null || selected) return;
                selected = true;
                try
                {
                    ((TextBox)picker.FindName("FileNameBox")).Text = path;
                    ((Button)picker.FindName("AcceptButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                catch (Exception error) { pickerError = error; picker.Close(); }
            }, application.Dispatcher);
            try { KillerBench.Shell.MainWindow.FindCardTile(window.BuildSessionSourcesCard(), "Str_OpenCapture").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
            finally { timer.Stop(); }
            Require(selected && pickerError is null, "Capture picker failed: " + pickerError);
            if (cancelRead)
                ((Button)window.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var until = Stopwatch.StartNew();
            while (!window.OpenCaptureEnabled && escaped is null && until.Elapsed < TimeSpan.FromSeconds(10))
            {
                var frame = new DispatcherFrame();
                application.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                Thread.Sleep(1);
            }
            Require(window.OpenCaptureEnabled, "Capture controls did not recover within 10 seconds.");
        }
    }

    private static CaptureSession Session()
    {
        var now = DateTimeOffset.Parse("2026-11-15T12:00:00Z");
        return new CaptureSession(1, now, now, "Synthetic", "Synthetic OS", "x64", "Standard",
            [new CaptureSource("Synthetic log", "Text", CollectionStatus.Collected, "Fixture evidence",
                new TextEvidence("Synthetic log", ["2026-11-15 12:00:00, Error CBS source missing 0x800f081f"], false))]);
    }

    private static async Task<(int Exit, string Output, string Error)> OpenCliAsync(string path)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(KillerBench.Cli.CommandEngine).Assembly.Location);
        start.ArgumentList.Add("open");
        start.ArgumentList.Add(path);
        start.ArgumentList.Add("--json");
        using var process = Process.Start(start) ?? throw new Exception("Cannot launch standalone CLI.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new Exception("Capture CLI exceeded 15 seconds."); }
        return (process.ExitCode, await output, await error);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
