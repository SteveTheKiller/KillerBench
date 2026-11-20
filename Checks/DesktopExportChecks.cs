using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KillerBench.Engine;

internal static class DesktopExportChecks
{
    internal static async Task RunPublicationAsync(string scratch)
    {
        Directory.CreateDirectory(scratch);
        string path = Path.Combine(scratch, "replacement.txt");
        foreach (string scenario in new[] { "success", "fault", "canceled", "competitor" })
        {
            await File.WriteAllTextAsync(path, "prior report");
            string? backup = null;
            Exception? failure = null;
            using var cancellation = new CancellationTokenSource();
            using (var prepared = await PreparedReportFile.CreateAsync(path, "complete replacement"))
            {
                try
                {
                    prepared.PublishReplacing(original => { using var reader = new StreamReader(original, leaveOpen: true); return reader.ReadToEnd() == "prior report"; },
                        cancellation.Token, originalArchived: archived =>
                        {
                            backup = archived;
                            if (scenario == "fault") throw new IOException("Fixture publication interruption.");
                            if (scenario == "canceled") cancellation.Cancel();
                            if (scenario == "competitor") File.WriteAllText(path, "competing report");
                        });
                }
                catch (Exception error) when (error is IOException or OperationCanceledException) { failure = error; }
            }
            if (scenario == "success")
                Require(failure is null && File.ReadAllText(path) == "complete replacement" && !File.Exists(backup),
                    "Successful replacement must publish the complete output and remove only the owned prior object.");
            else if (scenario == "competitor")
                Require(failure is IOException && File.ReadAllText(path) == "competing report" && File.ReadAllText(backup!) == "prior report" &&
                    failure is ReportPublicationException { Reason: ReportPublicationFailure.NotPublishedPriorRetained, Saved: false } publication && publication.RecoveryPath == backup,
                    "A concurrent file must survive and failure must identify the preserved prior-report backup without parsing prose.");
            else
                Require(failure is not null && File.ReadAllText(path) == "prior report" && !File.Exists(backup),
                    "A canceled or failed handoff must restore the original without overwriting a competitor.");
        }
        string failed = Path.Combine(scratch, "encoding.txt");
        await File.WriteAllTextAsync(failed, "prior bytes");
        try
        {
            using var ignored = await PreparedReportFile.CreateAsync(failed, "\ud800", encoding: new System.Text.UTF8Encoding(false, true));
            throw new Exception("Invalid UTF-16 unexpectedly encoded.");
        }
        catch (System.Text.EncoderFallbackException) { }
        Require(File.ReadAllText(failed) == "prior bytes" && !Directory.GetFiles(scratch, ".killerbench-report-*.tmp").Any(p => !p.Contains("-backup-")),
            "Failure after staged output starts must preserve original bytes and remove owned staging output.");
        var workflow = new WorkflowResult(WorkflowKind.Vitals, "Synthetic", DateTimeOffset.Parse("2026-11-15T12:00:00Z"),
            "Recorded", [new WorkflowCheck("Fixture", CheckState.Information, "Fixture detail")], [], "fixture.json");
        Require(WorkflowReportExport.BuildHtml(workflow, WorkflowReportScope.Diagnostics, accentKey: "magenta").Contains("#FF6CD1"),
            "Magenta must be a supported exported report accent.");
        try { WorkflowReportExport.BuildHtml(workflow, WorkflowReportScope.Diagnostics, accentKey: "unsupported-token"); throw new Exception("Unknown accent accepted."); }
        catch (ArgumentException) { }
        await ReportFilePublicationChecks.RunAsync(scratch);
        Console.WriteLine("PASS: 7 publication/format cases cover safe replacement, interrupted/canceled restoration, concurrent backup recovery, staged encoding failure and accent validation.");
    }

    internal static int Run(string scratch)
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
        Exception? escapedException = null;
        string activeStage = "initializing desktop export window";
        string? escapedStage = null;
        bool cancelingForEscape = false;
        DispatcherUnhandledExceptionEventHandler escapedHandler = (_, e) =>
        {
            e.Handled = true;
            if (escapedException is null)
            {
                escapedException = e.Exception;
                escapedStage = activeStage;
                Console.Error.WriteLine("Escaped dispatcher exception at " + escapedStage + ": " + e.Exception);
                Console.Error.Flush();
            }
            if (cancelingForEscape) return;
            cancelingForEscape = true;
            try { CancelOwnedModals(); }
            finally { cancelingForEscape = false; }
        };
        application.DispatcherUnhandledException += escapedHandler;
        try
        {
            window.Show();
            CheckSynchronousModalTimers(window);
            string path = Path.Combine(scratch, "evidence.kbench");
            var now = DateTimeOffset.Parse("2026-11-15T12:00:00Z");
            var session = new CaptureSession(1, now, now, "Synthetic", "Synthetic OS", "x64", "Standard",
                [new CaptureSource("Synthetic log", "Text", CollectionStatus.Collected, "Fixture evidence",
                    new TextEvidence("Synthetic log", ["2026-11-15 12:00:00, Error CBS source missing 0x800f081f"], false))]);
            Capture.SaveAsync(session, path).GetAwaiter().GetResult();
            byte[] before = File.ReadAllBytes(path);
            Pick("OpenCaptureButton", path, menu: true);
            Wait(() => window.SaveCaptureEnabled);
            string activity = ((TextBox)window.FindName("ActivityLog")).Text;
            Pick("ExportActivityButton", path);
            Wait(() => ((Button)window.FindName("ExportActivityButton")).IsEnabled);
            Require(before.SequenceEqual(File.ReadAllBytes(path)), "Activity export must not overwrite the active capture.");
            Require(((TextBox)window.FindName("ActivityLog")).Text[activity.Length..].Contains(
                KillerBench.Controls.LocalizedText.Get("Str_Ui_ExportSeparateDest")),
                "Evidence overwrite rejection must request a separate destination.");
            string concurrent = Path.Combine(scratch, "concurrent-" + Guid.NewGuid().ToString("N") + ".txt");
            string beforeConcurrent = ((TextBox)window.FindName("ActivityLog")).Text;
            Pick("ExportActivityButton", concurrent, afterAccepted: () => File.WriteAllText(concurrent, "competing report"));
            Wait(() => ((Button)window.FindName("ExportActivityButton")).IsEnabled);
            Require(File.ReadAllText(concurrent) == "competing report", "Activity export must preserve a destination created after picker consent.");
            Require(!((TextBox)window.FindName("ActivityLog")).Text[beforeConcurrent.Length..].Contains("Exported "),
                "A failed publication must not report export success.");
            string alias = Path.Combine(scratch, "alias-" + Guid.NewGuid().ToString("N") + ".txt");
            Require(CreateHardLink(alias, path, 0), "Cannot create the evidence hardlink fixture.");
            Pick("ExportActivityButton", alias);
            Wait(() => ((Button)window.FindName("ExportActivityButton")).IsEnabled);
            Require(before.SequenceEqual(File.ReadAllBytes(alias)), "Activity export must refuse a hardlink alias of active evidence.");
            string normalized = Path.Combine(scratch, ".", "EVIDENCE.KBENCH");
            Pick("ExportActivityButton", normalized);
            Wait(() => ((Button)window.FindName("ExportActivityButton")).IsEnabled);
            Require(before.SequenceEqual(File.ReadAllBytes(path)), "Normalized and case evidence aliases must be refused.");
            for (int index = 1; index <= 3; index++)
            {
                string output = Path.Combine(scratch, "activity-" + Guid.NewGuid().ToString("N") + (index == 1 ? ".txt" : index == 2 ? ".csv" : ".html"));
                Pick("ExportActivityButton", output, filterIndex: index);
                Wait(() => ((Button)window.FindName("ExportActivityButton")).IsEnabled && File.Exists(output));
                Require(File.ReadAllBytes(output).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "Activity export must retain UTF-8 BOM in each format.");
            }
            for (int index = 1; index <= 4; index++)
            {
                string output = Path.Combine(scratch, "report-" + Guid.NewGuid().ToString("N") + (index <= 2 ? ".html" : ".txt"));
                File.WriteAllText(output, "prior report");
                Pick("ExportReportButton", output, filterIndex: index);
                Wait(() => File.ReadAllText(output) != "prior report");
                Require(!File.ReadAllBytes(output).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "Diagnostic reports must retain UTF-8 without BOM.");
            }
            string changed = Path.Combine(scratch, "changed-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(changed, "confirmed report");
            string beforeChanged = ((TextBox)window.FindName("ActivityLog")).Text;
            Pick("ExportReportButton", changed, filterIndex: 3, afterAccepted: () => File.WriteAllText(changed, "newer report"));
            Wait(() => ((TextBox)window.FindName("ActivityLog")).Text.Length > beforeChanged.Length);
            Require(File.ReadAllText(changed) == "newer report" && !((TextBox)window.FindName("ActivityLog")).Text[beforeChanged.Length..].Contains("Saved "),
                "A changed existing report must survive with a failure status.");
            string locked = Path.Combine(scratch, "locked-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(locked, "locked original report");
            FileStream? held = null;
            try
            {
                string beforeLocked = ((TextBox)window.FindName("ActivityLog")).Text;
                Pick("ExportReportButton", locked, filterIndex: 3,
                    afterAccepted: () => held = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.Read));
                Wait(() => ((TextBox)window.FindName("ActivityLog")).Text.Length > beforeLocked.Length);
                Require(File.ReadAllText(locked) == "locked original report" && !((TextBox)window.FindName("ActivityLog")).Text[beforeLocked.Length..].Contains("Saved "),
                    "A failed locked-destination rename must preserve original bytes and report failure.");
            }
            finally { held?.Dispose(); }
            string journal = Path.Combine(scratch, "historical.json");
            var historical = new WorkflowResult(WorkflowKind.SystemFiles, "Synthetic", now, "Recorded fixture status",
                [new WorkflowCheck("Retained fixture check", CheckState.Attention, "Retained fixture check detail")],
                [new RepairResult("Synthetic", RepairKind.SystemFiles, now, "Recorded fixture repair detail", "recorded-only.json", [])],
                "recorded-only.json");
            File.WriteAllText(journal, JsonSerializer.Serialize(historical, Capture.JsonOptions));
            Pick("OpenWorkflowJournal", journal, menu: true);
            Wait(() => ((TextBox)window.FindName("EvidenceBox")).Text.Contains("SAVED WORKFLOW"));
            string historyOutput = Path.Combine(scratch, "history-" + Guid.NewGuid().ToString("N") + ".txt");
            Pick("ExportReportButton", historyOutput, filterIndex: 4);
            Wait(() => File.Exists(historyOutput));
            string historyText = File.ReadAllText(historyOutput);
            Require(historyText.Contains("Retained fixture check detail") && historyText.Contains("Recorded fixture repair detail") &&
                historyText.Contains("RECORDED HISTORY") && !historyText.Contains("REPAIRED AND VERIFIED"),
                "Historical report export must retain detailed checks and repairs with recorded-history labels.");
            string historyHtml = Path.Combine(scratch, "history-" + Guid.NewGuid().ToString("N") + ".html");
            Pick("ExportReportButton", historyHtml, filterIndex: 2);
            Wait(() => File.Exists(historyHtml));
            Require(File.ReadAllText(historyHtml).Contains("Recorded history") && File.ReadAllText(historyHtml).Contains("Retained fixture check detail"),
                "Historical HTML must retain the same history label and detailed evidence.");
            string damaged = Path.Combine(scratch, "damaged-history.json");
            var damagedRecord = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(historical, Capture.JsonOptions))!;
            damagedRecord["checks"] = new System.Text.Json.Nodes.JsonArray((System.Text.Json.Nodes.JsonNode?)null);
            File.WriteAllText(damaged, damagedRecord.ToJsonString());
            Pick("OpenWorkflowJournal", damaged, menu: true);
            Wait(() => ((TextBox)window.FindName("EvidenceBox")).Text.Contains(Path.GetFullPath(damaged)) && window.OpenCaptureEnabled);
            string damagedOutput = Path.Combine(scratch, "damaged-history-" + Guid.NewGuid().ToString("N") + ".txt");
            Pick("ExportReportButton", damagedOutput, filterIndex: 4);
            Wait(() => File.Exists(damagedOutput));
            Require(File.ReadAllText(damagedOutput).Contains("RECORDED HISTORY") && File.ReadAllText(damagedOutput).Contains("Recorded detail unavailable"),
                "Invalid nested history must remain explicitly historical and incomplete.");
            string log = Path.Combine(scratch, "opened-fixture.log");
            File.WriteAllText(log, "2026-11-15 12:00:00, Error CBS source missing 0x800f081f JSON fixture sentinel");
            byte[] logBefore = File.ReadAllBytes(log);
            ((TextBox)window.FindName("PathBox")).Text = log;
            ((Button)window.FindName("OpenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Wait(() => ((TextBox)window.FindName("EvidenceBox")).Text.Contains("JSON fixture sentinel") && ((Button)window.FindName("OpenButton")).IsEnabled);
            Pick("ExportActivityButton", log);
            Wait(() => ((Button)window.FindName("ExportActivityButton")).IsEnabled);
            Require(logBefore.SequenceEqual(File.ReadAllBytes(log)), "Activity export must preserve an opened log source.");
            string json = Path.Combine(scratch, "json-report-" + Guid.NewGuid().ToString("N") + ".json");
            Pick("ExportReportButton", json);
            Wait(() => File.Exists(json));
            using var parsed = JsonDocument.Parse(File.ReadAllText(json));
            Require(!File.ReadAllBytes(json).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }) && parsed.RootElement.ValueKind == JsonValueKind.Object,
                "Imported-log JSON reports must publish valid JSON without BOM.");
            Require(!Directory.GetFiles(scratch, ".killerbench-report-*.tmp").Any(), "Completed desktop exports must leave no staging or backup output.");
            ThrowIfEscaped();
            Console.WriteLine("PASS: 18 desktop export cases cover capture/log aliases, concurrent/changed/locked destinations, BOM/report formats and detailed/incomplete history.");
            return 0;
        }
        finally
        {
            try { window.Close(); application.Shutdown(); }
            finally { application.DispatcherUnhandledException -= escapedHandler; }
        }

        void ThrowIfEscaped()
        {
            if (escapedException is not null)
                throw new InvalidOperationException("Desktop export received an escaped dispatcher exception at " + escapedStage + ".", escapedException);
        }

        void CancelOwnedModals()
        {
            foreach (var picker in application.Windows.OfType<KillerBench.Controls.FileDialog>()
                .Where(candidate => candidate.Owner == window).ToArray())
            {
                foreach (var confirm in application.Windows.OfType<KillerBench.Controls.ConfirmDialog>()
                    .Where(candidate => candidate.Owner == picker && candidate.IsVisible).ToArray())
                {
                    try { ((Button)confirm.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
                    catch (Exception error) { Console.Error.WriteLine("Escaped-exception owned confirmation cancellation failed: " + error); }
                }
                try { if (picker.IsVisible) picker.Close(); }
                catch (Exception error) { Console.Error.WriteLine("Escaped-exception owned picker cancellation failed: " + error); }
            }
        }

        void Pick(string controlName, string path, bool menu = false, Action? afterAccepted = null, int filterIndex = 1)
        {
            ThrowIfEscaped();
            bool configured = false, selected = false, confirmed = false;
            bool acceptCallActive = false, synchronousModalObserved = false;
            Exception? pickerError = null;
            KillerBench.Controls.FileDialog? ownedPicker = null;
            string stage = "starting modal action";
            var elapsed = Stopwatch.StartNew();
            TraceStage(stage);
            var timer = new DispatcherTimer(DispatcherPriority.Background, application.Dispatcher) { Interval = TimeSpan.FromMilliseconds(20) };
            timer.Tick += (_, _) =>
            {
                if (escapedException is not null) { Fail(escapedException); return; }
                if (elapsed.Elapsed > TimeSpan.FromSeconds(10))
                {
                    Fail(new TimeoutException("The picker watchdog expired at " + stage + "."));
                    return;
                }
                if (pickerError is not null) { CloseOwnedDialogs(); return; }
                try
                {
                    ownedPicker ??= application.Windows.OfType<KillerBench.Controls.FileDialog>()
                        .SingleOrDefault(candidate => candidate.Owner == window && candidate.IsVisible && candidate.IsLoaded);
                    if (ownedPicker is null) { TraceStage("waiting for visible owned picker"); return; }
                    var confirmations = application.Windows.OfType<KillerBench.Controls.ConfirmDialog>()
                        .Where(candidate => candidate.Owner == ownedPicker && candidate.IsVisible && candidate.IsLoaded).ToArray();
                    if (confirmations.Length > 1) throw new InvalidOperationException("More than one confirmation belongs to the picker.");
                    if (confirmations.SingleOrDefault() is { } confirm)
                    {
                        string expected = KillerBench.Controls.LocalizedText.Format("Str_Dlg_OverwriteMsg", Path.GetFileName(path));
                        var heading = (TextBlock)confirm.FindName("HeadingText");
                        var replace = (Button)confirm.FindName("OkButton");
                        if (!selected || controlName is not ("ExportActivityButton" or "ExportReportButton") || heading.Text != expected ||
                            !Equals(replace.Content, KillerBench.Controls.LocalizedText.Get("Str_Btn_Replace")))
                            throw new InvalidOperationException("The owned modal is not the expected overwrite confirmation.");
                        TraceStage("waiting for enabled overwrite confirmation");
                        if (confirmed || !confirm.IsEnabled || !replace.IsEnabled || !replace.IsVisible) return;
                        confirmed = true;
                        TraceStage("confirming owned overwrite once");
                        replace.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        return;
                    }
                    if (selected) return;
                    if (!configured)
                    {
                        TraceStage("configuring owned picker");
                        ((ComboBox)ownedPicker.FindName("FilterCombo")).SelectedIndex = filterIndex - 1;
                        ((TextBox)ownedPicker.FindName("FileNameBox")).Text = path;
                        configured = true;
                    }
                    var accept = (Button)ownedPicker.FindName("AcceptButton");
                    if (!ownedPicker.IsEnabled || !ownedPicker.IsDirectoryListingAvailable || ownedPicker.IsDirectoryListingBusy ||
                        !accept.IsVisible || !accept.IsEnabled)
                    { TraceStage("waiting for enabled picker acceptance"); return; }
                    selected = true;
                    if (afterAccepted is not null) ownedPicker.Closed += (_, _) =>
                    {
                        if (escapedException is not null || pickerError is not null || ownedPicker.DialogResult != true) return;
                        try { TraceStage("running accepted-picker filesystem boundary"); afterAccepted(); }
                        catch (Exception error) { Fail(error); }
                    };
                    TraceStage("queueing owned picker acceptance once");
                    application.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (escapedException is not null || pickerError is not null) return;
                        TraceStage("dispatching queued owned picker acceptance");
                        acceptCallActive = true;
                        try { accept.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
                        catch (Exception error) { Fail(error); }
                        finally { acceptCallActive = false; }
                    }));
                }
                catch (Exception error) { Fail(error); }
            };
            var modalObserver = new DispatcherTimer(DispatcherPriority.Background, application.Dispatcher) { Interval = TimeSpan.FromMilliseconds(50) };
            modalObserver.Tick += (_, _) =>
            {
                if (elapsed.Elapsed <= TimeSpan.FromSeconds(10)) return;
                if (ownedPicker is not null && acceptCallActive && !synchronousModalObserved &&
                    application.Windows.OfType<KillerBench.Controls.ConfirmDialog>().Any(candidate => candidate.Owner == ownedPicker && candidate.IsVisible))
                {
                    synchronousModalObserved = true;
                    Console.Error.WriteLine("MODAL-PROBE: owned confirmation is visible while acceptCallActive=true at " + activeStage);
                    Console.Error.Flush();
                }
                Fail(new TimeoutException("The independent modal observer expired at " + stage + "; acceptCallActive=" + acceptCallActive + "."));
            };
            modalObserver.Start();
            timer.Start();
            try
            {
                if (menu)
                {
                    var tile = KillerBench.Shell.MainWindow.FindCardTile(window.BuildSessionSourcesCard(),
                        controlName == "OpenWorkflowJournal" ? "Str_Advanced_OpenWorkflow" : "Str_OpenCapture");
                    tile.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else ((Button)window.FindName(controlName)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            finally { timer.Stop(); modalObserver.Stop(); }
            ThrowIfEscaped();
            if (pickerError is not null) throw new InvalidOperationException("Export picker failed for " + controlName + " at " + stage + ".", pickerError);
            Require(selected, "Export picker returned without dispatching an enabled acceptance for " + controlName + ".");
            TraceStage("modal action returned");

            void TraceStage(string next)
            {
                if (next == stage && next != "starting modal action") return;
                stage = next;
                activeStage = controlName + " / " + Path.GetFileName(path) + " / " + stage;
                Console.WriteLine("STAGE: desktop-export / " + controlName + " / " + Path.GetFileName(path) + " / " + stage);
                Console.Out.Flush();
            }

            void Fail(Exception error)
            {
                if (pickerError is null)
                {
                    pickerError = error;
                    Console.Error.WriteLine("Picker failure at " + stage + ": " + error);
                }
                CloseOwnedDialogs();
            }

            void CloseOwnedDialogs()
            {
                ownedPicker ??= application.Windows.OfType<KillerBench.Controls.FileDialog>()
                    .FirstOrDefault(candidate => candidate.Owner == window);
                if (ownedPicker is null) return;
                foreach (var confirm in application.Windows.OfType<KillerBench.Controls.ConfirmDialog>()
                    .Where(candidate => candidate.Owner == ownedPicker && candidate.IsVisible).ToArray())
                {
                    try { ((Button)confirm.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
                    catch (Exception error) { Console.Error.WriteLine("Owned confirmation cancellation failed: " + error); }
                }
                try { if (ownedPicker.IsVisible) ownedPicker.Close(); }
                catch (Exception error) { Console.Error.WriteLine("Owned picker cancellation failed: " + error); }
            }
        }

        void Wait(Func<bool> complete)
        {
            ThrowIfEscaped();
            activeStage = "waiting for observable result after " + activeStage;
            var elapsed = Stopwatch.StartNew();
            while (escapedException is null && !complete() && elapsed.Elapsed < TimeSpan.FromSeconds(10))
            {
                var frame = new DispatcherFrame();
                application.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                Thread.Sleep(1);
            }
            ThrowIfEscaped();
            string log = ((TextBox)window.FindName("ActivityLog")).Text;
            Require(complete(), "Desktop export did not finish within 10 seconds. Activity: " + log[Math.Max(0, log.Length - 2500)..] +
                " Evidence: " + ((TextBox)window.FindName("EvidenceBox")).Text[..Math.Min(800, ((TextBox)window.FindName("EvidenceBox")).Text.Length)]);
        }
    }

    private static void CheckSynchronousModalTimers(Window owner)
    {
        foreach (bool queued in new[] { false, true })
        {
            var modal = new Window { Owner = owner, Width = 260, Height = 120, ShowInTaskbar = false,
                Content = new TextBlock { Text = "Synchronous modal timer control", Margin = new Thickness(12) } };
            bool dispatched = false, tickActive = false, callActive = false, observed = false;
            int driverTicks = 0;
            Exception? failure = null;
            var frame = new DispatcherFrame();
            var elapsed = Stopwatch.StartNew();
            var driver = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(20) };
            var observer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(50) };
            modal.Closed += (_, _) => frame.Continue = false;
            driver.Tick += (_, _) =>
            {
                driverTicks++;
                if (dispatched) return;
                dispatched = true;
                tickActive = true;
                try
                {
                    if (queued) owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ShowModal));
                    else ShowModal();
                }
                finally { tickActive = false; }
            };
            observer.Tick += (_, _) =>
            {
                if (modal.IsVisible && (!queued || driverTicks > 1))
                {
                    observed = callActive && (queued ? !tickActive && driverTicks > 1 : tickActive && driverTicks == 1);
                    modal.Close();
                }
                else if (elapsed.Elapsed > TimeSpan.FromSeconds(3))
                {
                    failure = new TimeoutException("Synchronous modal timer control did not complete.");
                    if (modal.IsVisible) modal.Close();
                    frame.Continue = false;
                }
            };
            driver.Start();
            observer.Start();
            try { Dispatcher.PushFrame(frame); }
            finally { driver.Stop(); observer.Stop(); if (modal.IsVisible) modal.Close(); }
            if (failure is not null) throw failure;
            Require(observed, queued ? "Queued modal dispatch must leave its driver timer live during ShowDialog." :
                "The inline modal control must demonstrate that its driver timer cannot tick again until Tick returns.");

            void ShowModal()
            {
                callActive = true;
                try { modal.ShowDialog(); }
                finally { callActive = false; }
            }
        }
        Console.WriteLine("PASS: 2 synchronous modal timer controls distinguish blocked inline dispatch from live queued dispatch.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string path, string existing, nint security);
}
