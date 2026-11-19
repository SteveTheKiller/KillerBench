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

        KillerBench.Services.ThemeManager.GetSetting = _ => null;
        KillerBench.Services.ThemeManager.SetSetting = (_, _) => { };

        string output = Environment.GetEnvironmentVariable("KILLERBENCH_SCREENSHOT_OUTPUT") ??
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bench-landing", "screenshots"));
        Directory.CreateDirectory(output);
        RenderWorkspaceShots(output);

        typeof(KillerBench.App).GetProperty("DemoMode")!.SetValue(null, true);
        typeof(KillerBench.App).GetProperty("ScreenshotMode")!.SetValue(null, true);
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
            new("overview-storage.png", WorkflowKind.Storage, dark, A(KillerBench.Services.Accent.Yellow), Width: 1600, Height: 1000, Area: 11),
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
        Console.WriteLine("PASS: four workspace views, grouped search, three toolbar modes, twelve gallery images and five size, zoom and tool variants rendered without a cursor: " + output);
        application.Shutdown();
        return 0;
    }

    private sealed record Shot(string File, WorkflowKind Kind, KillerBench.Services.Theme Theme, KillerBench.Services.Accent? Accent,
        double Scale = 1, bool Grain = true, bool Raw = false, bool OutputHidden = false, bool Shortcuts = false, bool Repair = false,
        double Width = 1440, double Height = 900, double PixelScale = 1, int? Area = null);

    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void Call(object target, string name, params object?[] args) =>
        (target.GetType().GetMethod(name, Private) ?? throw new InvalidOperationException("Missing " + name)).Invoke(target, args);

    private static void RenderWorkspaceShots(string output)
    {
        var window = new KillerBench.Shell.MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowActivated = false,
            ShowInTaskbar = false,
            Left = SystemParameters.VirtualScreenLeft - 2000,
            Top = SystemParameters.VirtualScreenTop,
            Width = 800,
            Height = 500
        };
        try
        {
            window.Show();
            // The map fits its viewport now, so a short window is what makes the score column scroll.
            window.MinHeight = 0;
            window.Height = 320;
            Call(window, "ApplyAppScale", 2.5);
            var navigation = (ListBox)window.FindName("WorkspaceNavigation");
            var destinations = navigation.Items.OfType<ListBoxItem>().ToDictionary(item => (string)item.Tag);
            Call(window, "ApplyAppScale", 1d);
            navigation.SelectedItem = destinations["Overview"];
            Settle(window);
            var overview = (Border)window.FindName("OverviewPage");
            var viewer = window.FindName("OverviewScroll") as ScrollViewer;
            Require(viewer is not null, "Overview uses the shared pane-edge shadow viewer.");
            viewer!.ScrollToTop();
            Settle(window);
            var shadowRoot = (Grid)VisualTreeHelper.GetChild(viewer, 0);
            var bottomCue = shadowRoot.Children.OfType<Border>().Single(border =>
                ReferenceEquals(border.Style, window.FindResource("ScrollBottomCue")));
            Rect cue = bottomCue.TransformToAncestor(overview).TransformBounds(new Rect(bottomCue.RenderSize));
            Require(viewer.ScrollableHeight > 0 && bottomCue.IsVisible &&
                Math.Abs(cue.Left - overview.BorderThickness.Left) < 1 &&
                Math.Abs(cue.Right - (overview.ActualWidth - overview.BorderThickness.Right)) < 1 &&
                Math.Abs(cue.Bottom - (overview.ActualHeight - overview.BorderThickness.Bottom)) < 1,
                "The scrolling shadow reaches the pane edges without a padding gap.");
            viewer.ScrollToBottom();
            Settle(window);
            Require(!bottomCue.IsVisible, "The bottom cue disappears when no content remains below it.");
            viewer.ScrollToTop();
            Settle(window);
            Call(window, "ApplyAppScale", 2.5);
            navigation.SelectedItem = destinations["History"];
            Settle(window);
            var open = (Button)window.FindName("OpenSavedWorkflowButton");
            var viewport = (FrameworkElement)window.FindName("ZoomViewport");
            open.BringIntoView();
            Settle(window);
            var bounds = open.TransformToAncestor(viewport).TransformBounds(new Rect(open.RenderSize));
            Require(open.IsVisible && open.IsEnabled && bounds.Width > 0 && bounds.Height > 0 &&
                bounds.Left >= -1 && bounds.Top >= -1 &&
                bounds.Right <= viewport.ActualWidth + 1 && bounds.Bottom <= viewport.ActualHeight + 1,
                $"History's saved-workflow action fits inside the visible viewport at 250% and 800x500. Action {bounds}, viewport {viewport.RenderSize}.");

            window.Width = 1025;
            Call(window, "ApplyAppScale", 1d);
            Call(window, "FitInitialOverviewHeight");
            Settle(window);
            var grain = (CheckBox)window.FindName("FilmGrainCheck");
            var evidence = (FrameworkElement)window.FindName("TerminalPanel");
            navigation.SelectedItem = destinations["Overview"];
            Settle(window);
            Require(!evidence.IsVisible, "Overview opens on the map alone; details swap in only when an area is chosen.");
            Call(window, "ShowAreaEvidence");
            Settle(window);
            Require(evidence.IsVisible && !((FrameworkElement)window.FindName("OverviewPage")).IsVisible,
                "Choosing an area swaps its details in over the map instead of beside it.");
            // Overview map parts are transparent hotspots over the drawn machine, not cards, so only review cards carry grain.
            var areaCard = ((StackPanel)window.FindName("AreaReviewCards")).Children.OfType<Border>().First();
            foreach (FrameworkElement card in new FrameworkElement[] { areaCard })
            {
                grain.IsChecked = true;
                grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                Settle(window);
                byte[] on = CardPaddingPixels(card);
                grain.IsChecked = false;
                grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                Settle(window);
                byte[] off = CardPaddingPixels(card);
                Require(ChangedPixels(on, off) > 8, "The card texture covers its padding without an inset grain rectangle.");
                Require(ChangedPixels(off, CardPaddingPixels(card)) == 0, "Unchanged grain-off card padding is the absent-texture control.");
            }
            grain.IsChecked = true;
            grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Settle(window);
            byte[] evidenceOn = GrainPixels(evidence);
            grain.IsChecked = false;
            grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Settle(window);
            byte[] evidenceOff = GrainPixels(evidence);
            Require(ChangedPixels(evidenceOn, evidenceOff) > 32, "The established evidence pane calibrates visible film grain.");
            Require(ChangedPixels(evidenceOff, GrainPixels(evidence)) == 0, "The unchanged grain-off pane is the absent texture control.");
            grain.IsChecked = true;
            grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Settle(window);
            foreach (string destination in new[] { "Overview", "Investigate", "History" })
            {
                navigation.SelectedItem = destinations[destination];
                Settle(window);
                var pane = (FrameworkElement)window.FindName(destination + "Page");
                byte[] on = GrainPixels(pane);
                grain.IsChecked = false;
                grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                Settle(window);
                Require(ChangedPixels(on, GrainPixels(pane)) > 32, destination + " uses the same visible KillerUI grain and shared toggle as the evidence pane.");
                grain.IsChecked = true;
                grain.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                Settle(window);
            }
            navigation.SelectedItem = destinations["History"];
            Settle(window);
            VerifySharedPicker(window);
            Capture(window, Path.Combine(output, "workspace-history.png"), 1);
            navigation.SelectedItem = destinations["Investigate"];
            Settle(window);
            Capture(window, Path.Combine(output, "workspace-investigate.png"), 1);
            navigation.SelectedItem = destinations["Overview"];
            Settle(window);
            Capture(window, Path.Combine(output, "workspace-overview.png"), 1);
            Call(window, "ShowAreaEvidence");
            Settle(window);
            Capture(window, Path.Combine(output, "workspace-area.png"), 1);
            ((Button)window.FindName("OutputBackButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle(window);
            Require(((FrameworkElement)window.FindName("OverviewPage")).IsVisible && !evidence.IsVisible, "Back swaps the map in again.");
            navigation.SelectedItem = destinations["Tools"];
            Settle(window);
            Capture(window, Path.Combine(output, "workspace-tools.png"), 1);
            navigation.SelectedItem = destinations["Overview"];
            var titleSearch = (TextBox)window.FindName("TitleSearchBox");
            titleSearch.Text = "network";
            var searchPopup = (System.Windows.Controls.Primitives.Popup)window.FindName("TitleSearchPopup");
            DateTime deadline = DateTime.UtcNow.AddSeconds(4);
            while (!searchPopup.IsOpen && DateTime.UtcNow < deadline)
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            Require(searchPopup.IsOpen && ((ListBox)window.FindName("TitleSearchResults")).Items.Count > 0,
                "The actual grouped search flyout renders matching areas and tools.");
            Settle(window);
            Capture((FrameworkElement)VisualTreeHelper.GetParent(searchPopup.Child), Path.Combine(output, "search-results.png"), 1);
            searchPopup.IsOpen = false;
            titleSearch.Clear();
            var toolbar = (ContextMenu)window.FindName("ToolbarMenu");
            foreach (var (label, file) in new[] { ("Text beside icons", "toolbar-beside.png"), ("No text", "toolbar-icons.png"), ("Text only", "toolbar-text.png") })
            {
                toolbar.Items.OfType<MenuItem>().Single(item => Equals(item.Header, label))
                    .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Settle(window);
                Capture(window, Path.Combine(output, file), 1);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void VerifySharedPicker(Window owner)
    {
        string root = Path.Combine(Path.GetTempPath(), "kit-picker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var picker = new KillerBench.Controls.FileDialog(KillerBench.Controls.FileDialogMode.Open)
            { Owner = owner, InitialDirectory = root, ShowInTaskbar = false };
        Exception? failure = null;
        int stage = 0;
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        timer.Tick += (_, _) =>
        {
            try
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Shared picker readiness timed out at stage " + stage);
                if (!picker.IsVisible) return;
                if (stage == 0 && ((TextBox)picker.FindName("PathBox")).Text.TrimEnd('\\') != root.TrimEnd('\\')) return;
                if (stage == 1 && PresentationSource.FromVisual((TextBox)picker.FindName("NewFolderBox")) is null) return;
                if (stage == 0)
                {
                    var open = (Button)picker.FindName("NewFolderButton");
                    Require(open.IsVisible && open.ToolTip?.ToString()?.Contains("Ctrl+Shift+N", StringComparison.Ordinal) == true,
                        "The current KillerUI picker exposes its New folder action and shortcut.");
                    open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    stage = 1;
                    return;
                }
                timer.Stop();
                CheckNames();
                picker.Close();
            }
            catch (Exception error) { failure = error; timer.Stop(); picker.Close(); }
        };
        try
        {
            timer.Start();
            picker.ShowDialog(owner);
            if (failure is not null) throw new InvalidOperationException("Shared picker behavior check failed.", failure);
            Require(stage == 1, "The shared picker exercised its actual New folder controls.");
        }
        finally { timer.Stop(); if (picker.IsVisible) picker.Close(); }

        void CheckNames()
        {
            var input = (TextBox)picker.FindName("NewFolderBox");
            var error = (TextBlock)picker.FindName("NewFolderError");
            input.Text = "NUL";
            input.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(input), 0, System.Windows.Input.Key.Enter) { RoutedEvent = System.Windows.Input.Keyboard.KeyDownEvent });
            Require(error.IsVisible && Directory.GetDirectories(root).Length == 0,
                "The shared picker rejects a Windows reserved name before creating anything.");
            input.Text = "Fixture folder";
            input.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(input), 0, System.Windows.Input.Key.Enter) { RoutedEvent = System.Windows.Input.Keyboard.KeyDownEvent });
            Require(Directory.Exists(Path.Combine(root, "Fixture folder")), "The shared picker creates the valid fixture folder.");
        }
    }

    private static byte[] GrainPixels(FrameworkElement pane)
    {
        var owner = Window.GetWindow(pane);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(owner.ActualWidth),
            (int)Math.Ceiling(owner.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(owner);
        // A blank strip inside the pane padding avoids labels, controls, edges and shadows.
        var pixels = new byte[8 * 64 * 4];
        Point origin = pane.TranslatePoint(new Point(8, 80), owner);
        bitmap.CopyPixels(new Int32Rect((int)origin.X, (int)origin.Y, 8, 64), pixels, 8 * 4, 0);
        return pixels;
    }

    private static int ChangedPixels(byte[] before, byte[] after)
    {
        int changed = 0;
        for (int index = 0; index < before.Length; index += 4)
            if (BitConverter.ToInt32(before, index) != BitConverter.ToInt32(after, index)) changed++;
        return changed;
    }

    private static byte[] CardPaddingPixels(FrameworkElement card)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(card.ActualWidth), (int)Math.Ceiling(card.ActualHeight),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(card);
        var pixels = new byte[4 * 24 * 4];
        bitmap.CopyPixels(new Int32Rect(4, 12, 4, 24), pixels, 16, 0);
        return pixels;
    }

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
                initialHeading.Text == "Full system scan report" &&
                window.FindName("ZoomViewport") is Grid { IsHitTestVisible: false } && !window.AllowDrop,
                "Screenshot launch opens a finished, read-only synthetic report");

            if (!shot.Grain) { Call(window, "ToggleFilmGrain"); grainToggled = true; }
            if (shot.Scale != 1) Call(window, "ApplyAppScale", shot.Scale);
            bool overviewShot = shot.Kind == WorkflowKind.CommonProblems;
            var navigation = (ListBox)window.FindName("WorkspaceNavigation");
            string destination = overviewShot || shot.Area is not null ? "Overview" : "Tools";
            navigation.SelectedItem = navigation.Items.OfType<ListBoxItem>().Single(item => Equals(item.Tag, destination));
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
            Require(report.Children[0] is TextBlock heading && heading.Text.EndsWith(" report", StringComparison.Ordinal) &&
                !System.Text.RegularExpressions.Regex.IsMatch(raw.Text,
                    "demo|fabricated|synthetic|simulat", System.Text.RegularExpressions.RegexOptions.IgnoreCase) &&
                ((TextBlock)window.FindName("ModeLabel")).Text == "OPERATION ENDED",
                "Screenshot reports and status use normal product wording");
            if (overviewShot)
            {
                navigation.SelectedItem = navigation.Items.OfType<ListBoxItem>().Single(item => Equals(item.Tag, "Tools"));
                navigation.SelectedItem = navigation.Items.OfType<ListBoxItem>().Single(item => Equals(item.Tag, "Overview"));
                Settle(window);
                // One pane: Overview lands on the map, and the selected area's details swap in over it.
                Require(window.FindName("SystemAreas") is FrameworkElement { IsVisible: true } &&
                    window.FindName("AreaEvidencePanel") is FrameworkElement { IsVisible: false },
                    "Returning to Overview lands on the scored map.");
                Call(window, "ShowAreaEvidence");
                Settle(window);
                Require(window.FindName("AreaCoverageChart") is FrameworkElement { IsVisible: true } &&
                    window.FindName("AreaEvidencePanel") is FrameworkElement { IsVisible: true } &&
                    window.FindName("AreaFullReportButton") is FrameworkElement { IsVisible: true } &&
                    report.Children[0] is TextBlock { Text: "Full system scan report" },
                    "Returning to Overview shows scoped coverage and review cards with the full report available.");
                var areas = (ListBox)window.FindName("SystemAreas");
                var selectedArea = areas.ItemContainerGenerator.ContainerFromItem(areas.SelectedItem);
                areas.SelectedIndex = 11;
                Settle(window);
                areas.SelectedIndex = 0;
                Settle(window);
                var coverage = (Grid)window.FindName("AreaCoverageChart");
                // Five of the six demo checks ran; the unknown one is not counted as checked.
                Require(coverage.IsVisible && ((StackPanel)coverage.Children[0]).Children.OfType<TextBlock>().First().Text == "5" &&
                        ((TextBlock)window.FindName("AreaTitle")).Text == "Diagnostics",
                    "Returning from an unchecked area restores its scoped summary and five checks that ran.");
                Call(window, "OnOutputBack", window, new RoutedEventArgs());
                Settle(window);
                Require(window.FindName("SystemAreas") is FrameworkElement { IsVisible: true } && selectedArea is ListBoxItem { IsVisible: true } && HasDemoStatus(selectedArea),
                    "Back returns to the map, where the selected area visibly identifies the completed result as a demo.");
            }
            if (shot.Area is int area)
            {
                navigation.SelectedItem = navigation.Items.OfType<ListBoxItem>().Single(item => Equals(item.Tag, "Overview"));
                ((ListBox)window.FindName("SystemAreas")).SelectedIndex = area;
                Settle(window);
                Require(window.FindName("AreaMetricChart") is StackPanel { IsVisible: true, Children.Count: > 0 },
                    "The area shot shows measured metric bars.");
            }
            if (shot.Raw) Call(window, "ShowRawOutput");
            if (shot.OutputHidden) Call(window, "SetOutputOpen", false);
            if (shot.Shortcuts) Call(window, "OnShortcuts", window, new RoutedEventArgs());
            Thread.Sleep(180);
            Settle(window);
            Require(Math.Abs(window.ActualWidth - shot.Width) < 0.5 && Math.Abs(window.ActualHeight - shot.Height) < 0.5,
                "Screenshot window matches its requested size, was " + window.ActualWidth + "x" + window.ActualHeight);
            var categories = (FrameworkElement)window.FindName("LeftPanel");
            var overview = (FrameworkElement)window.FindName("OverviewPage");
            var tools = (FrameworkElement)window.FindName("ConsoleBank");
            var outputPane = (FrameworkElement)window.FindName("TerminalPanel");
            var details = (FrameworkElement)window.FindName("AreaEvidencePanel");
            // One pane: the map, an area's swapped-in details, or a tool's controls or output, never two side by side.
            Require(shot.Area is not null ? details.IsVisible && !overview.IsVisible && !categories.IsVisible && !tools.IsVisible
                  : overviewShot && shot.Raw ? outputPane.IsVisible && !overview.IsVisible && !categories.IsVisible && !tools.IsVisible
                  : overviewShot ? overview.IsVisible && !outputPane.IsVisible && !categories.IsVisible && !tools.IsVisible
                  : categories.IsVisible && categories.ActualWidth > 0 && !overview.IsVisible && (tools.IsVisible != outputPane.IsVisible),
                "Screenshot shows the selected " + destination + " workspace.");
            Capture(window, Path.Combine(output, shot.File), shot.PixelScale);
            Console.WriteLine(shot.File + ": " + shot.Width + "x" + shot.Height + ", zoom " + shot.Scale + ", pixel scale " + shot.PixelScale);
        }
        finally
        {
            if (grainToggled) Call(window, "ToggleFilmGrain");
            window.Close();
        }
    }

    private static bool HasDemoStatus(DependencyObject element)
    {
        if (element is TextBlock { IsVisible: true } text &&
            (text.Text.Contains("synthetic", StringComparison.OrdinalIgnoreCase) ||
             text.Text.Contains("demo", StringComparison.OrdinalIgnoreCase))) return true;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            if (HasDemoStatus(VisualTreeHelper.GetChild(element, i))) return true;
        return false;
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
