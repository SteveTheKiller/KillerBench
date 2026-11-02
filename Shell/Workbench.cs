using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using KillerBench.Controls;
using KillerBench.Engine;
using KillerBench.Services;
using KillerFamily.Windowing;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private readonly List<TextEvidence> sources = [];
    private sealed record WorkflowSourceIdentity(Guid Id);
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextEvidence, WorkflowSourceIdentity> workflowSourceIdentities = [];
    private readonly Queue<SessionActivityEntry> activity = new();
    private bool shellReady;
    private double appScale = 1;
    private void InitializeShell()
    {
        GrainTexture.Apply(this);
        Shell.SidebarChrome.WireEdgeFades(SourcesList, SourceFadeTop, SourceFadeBottom);
        InitializeShortcuts();
        SourceInitialized += (_, _) => { RefreshWindowEdge(); DwmChrome.SetThemeBorder(this); };
        // Windows 11 snap layouts flyout on the maximize button. Attached after WindowChrome's
        // hook, so it answers WM_NCHITTEST first.
        SourceInitialized += (_, _) => SnapLayouts.Attach(this, MaximizeButton);
        // Before the first show: a default size larger than this monitor's work area is shrunk
        // to fit and the window moved onto it.
        SourceInitialized += (_, _) => WorkAreaFit.Fit(this);
        StateChanged += (_, _) =>
        {
            RefreshWindowEdge();
            MaximizeButton.Content = ((char)(WindowState == WindowState.Maximized ? 0xE923 : 0xE922)).ToString();
        };
        ThemeManager.ThemeChanged += RefreshTheme;
        Closed += (_, _) => ThemeManager.ThemeChanged -= RefreshTheme;
        SizeSlider.Value = ReadNumber("ConsoleSize", 14, 12, 24);
        appScale = ReadNumber("AppScale", 1, 0.7, 2.5);
        ApplyAppScale(appScale);
        InitializePanels();
        OutputScroll.SizeChanged += (_, _) => UpdateScaledWorkspaceWidths();
        ToolScroll.SizeChanged += (_, _) => UpdateScaledWorkspaceWidths();
        shellReady = true;
        RefreshTheme();
    }

    private static double ReadNumber(string key, double fallback, double min, double max) =>
        double.TryParse(Preferences.Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value)
            ? Math.Clamp(value, min, max) : fallback;

    private void SaveDisplayPreferences()
    {
        if (!shellReady) return;
        Preferences.Set("ConsoleSize", SizeSlider.Value.ToString(CultureInfo.InvariantCulture));
    }

    private void RefreshTheme()
    {
        ThemeStatus.Text = "v1.0.0";
        RefreshWindowEdge();
        DwmChrome.SetThemeBorder(this);
    }
    private void RefreshWindowEdge()
    {
        bool nativeCorners = DwmChrome.SetRoundedCorners(this,
            WindowState != WindowState.Maximized && ThemeManager.Current != Theme.SE98);
        if (nativeCorners) WindowEdgeBorder.BorderBrush = Brushes.Transparent;
        else WindowEdgeBorder.SetResourceReference(Border.BorderBrushProperty, "AppBorderBrush");
    }
    private static string ThemeLabel(Theme theme) => theme == Theme.SE98 ? "98SE" : theme.ToString();

    private void RegisterSource(TextEvidence item)
    {
        WorkflowSourceIdentity? identity = workflowSummaries.TryGetValue(item, out _)
            ? workflowSourceIdentities.GetValue(item, _ => new(Guid.NewGuid())) : null;
        int old = sources.FindIndex(source => identity is not null
            ? workflowSourceIdentities.TryGetValue(source, out var previous) && previous.Id == identity.Id
            : source.Source == item.Source && !workflowSourceIdentities.TryGetValue(source, out _));
        if (old >= 0) { sources.RemoveAt(old); SourcesList.Items.RemoveAt(old); }
        if (sources.Count >= (capture is null ? 12 : Capture.MaximumSources + 1))
        {
            int remove = sources.FindIndex(source => source.Source != "Synthetic demonstration");
            if (remove < 0) remove = 0;
            sources.RemoveAt(remove);
            SourcesList.Items.RemoveAt(remove);
            Report("Oldest source removed from the display limit. Original file and saved capture unchanged.");
        }
        sources.Add(item);
        SourcesList.Items.Add(new ListBoxItem
        {
            Content = new TextBlock { Text = item.Source == "Synthetic demonstration" ? "Sample evidence" : Path.GetFileName(item.Source), TextTrimming = TextTrimming.CharacterEllipsis },
            ToolTip = identity is null ? item.Source : item.Source + "\nResult ID: " + identity.Id, Tag = item
        });
        SourcesList.SelectedIndex = sources.Count - 1;
        SessionLabel.Text = $"{sources.Count} source{(sources.Count == 1 ? "" : "s")}";
    }

    private void OnSourceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (SourcesList.SelectedItem is not ListBoxItem { Tag: TextEvidence selected }) return;
        evidence = selected;
        ModeLabel.Text = selected.Source == "Synthetic demonstration" ? "SAMPLE MODE" :
            Path.GetExtension(selected.Source).ToLowerInvariant() is ".evtx" or ".xml" ? "IMPORTED EVENTS / RULES EVALUATED" : "IMPORTED TEXT / RULES EVALUATED";
        if (capture is not null && (selected.Source == "Windows Update capture" || capture.Sources.Any(s => ReferenceEquals(s.Evidence, selected))))
            ModeLabel.Text = "CAPTURED EVIDENCE / RULES EVALUATED";
        if (workflowSummaries.TryGetValue(selected, out var demoWorkflow) && demoWorkflow.Machine == DemoWorkflow.MachineName)
            ModeLabel.Text = Controls.LocalizedText.Get(App.ScreenshotMode ? "Str_Mode_DemoScreenshot" : "Str_Mode_DemoLive");
        SearchBox.Clear();
        if (!workflowSummaries.TryGetValue(selected, out _)) ClearWorkflowReport();
        ShowEvidence();
        if (workflowSummaries.TryGetValue(selected, out var workflow))
        {
            if (workflowReportTitles.TryGetValue(selected, out var reportTitle)) RenderNamedWorkflowReport(workflow, reportTitle);
            else RenderWorkflowReport(workflow);
        }
        if (SourceDetails.Visibility == Visibility.Visible) SetDetailsOpen(true);
    }

    private void Report(string message)
    {
        StatusLabel.Text = message;
        activity.Enqueue(new(App.ScreenshotMode ? DemoWorkflow.ScreenshotStartedUtc : DateTimeOffset.Now, message));
        while (activity.Count > 150) activity.Dequeue();
        ActivityLog.Text = string.Join("\n", activity.Select(entry => $"{entry.Time:HH:mm:ss}  {entry.Message}"));
        ActivityLog.ScrollToEnd();
    }

    private static void AnimateWidth(FrameworkElement panel, double target)
    {
        double start = panel.ActualWidth;
        panel.BeginAnimation(WidthProperty, null);
        panel.Width = target;
        if (!SystemParameters.ClientAreaAnimation) return;
        panel.BeginAnimation(WidthProperty, new DoubleAnimation(start, target, TimeSpan.FromMilliseconds(150))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop });
    }
    private void OnToggleSources(object sender, RoutedEventArgs e)
    {
        SetOutputOpen(TerminalPanel.Visibility != Visibility.Visible);
    }
    private void SetDetailsOpen(bool _) => DetailsPanel.Visibility = Visibility.Collapsed;
    private void OnToggleActivity(object sender, RoutedEventArgs e)
    {
        ActivityPanel.Visibility = ActivityPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        ActivityButton.Tag = ActivityPanel.Visibility == Visibility.Visible ? "on" : null;
    }
    private void OnSourceTab(object sender, RoutedEventArgs e)
    { RepairPanel.Visibility = Visibility.Collapsed; SourceDetails.Visibility = Visibility.Visible; DisplaySettings.Visibility = Visibility.Collapsed; FindingsPanel.Visibility = Visibility.Collapsed; }
    private void OnDisplayTab(object sender, RoutedEventArgs e)
    { RepairPanel.Visibility = Visibility.Collapsed; SourceDetails.Visibility = Visibility.Collapsed; DisplaySettings.Visibility = Visibility.Visible; FindingsPanel.Visibility = Visibility.Collapsed; }
    private void OnSearchKey(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) { ShowEvidence(); e.Handled = true; } }
    private void OnCaptionDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindButtonAncestor(source)) return;
        if (e.ClickCount == 2) OnMaximize(sender, e);
        else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private static bool FindButtonAncestor(DependencyObject item)
    {
        while (item is not null)
        {
            if (item is ButtonBase) return true;
            item = item is Visual ? VisualTreeHelper.GetParent(item) : LogicalTreeHelper.GetParent(item);
        }
        return false;
    }
    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnMaximize(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private void OnAppScale(object sender, MouseWheelEventArgs e)
    {
        SetAppScale(appScale + Math.Sign(e.Delta) * 0.02);
        e.Handled = true;
    }

    /// <summary>The family's whole-app zoom chord: Ctrl+Shift+Plus and Minus step by 5 percent, Ctrl+Shift+0 resets.</summary>
    private bool HandleZoomKey(Key key, ModifierKeys modifiers)
    {
        if (modifiers != (ModifierKeys.Control | ModifierKeys.Shift)) return false;
        double? target = key switch
        {
            Key.OemPlus or Key.Add => appScale + 0.05,
            Key.OemMinus or Key.Subtract => appScale - 0.05,
            Key.D0 or Key.NumPad0 => 1,
            _ => null
        };
        if (target is null) return false;
        SetAppScale(target.Value);
        return true;
    }

    private void SetAppScale(double scale)
    {
        ApplyAppScale(scale);
        Preferences.Set("AppScale", appScale.ToString(CultureInfo.InvariantCulture));
        StatusLabel.Text = $"Interface zoom: {appScale:P0}";
    }
    private void ApplyAppScale(double scale)
    {
        if (shellReady) sidebarWidth = (double.IsNaN(LeftPanel.Width) ? LeftPanel.ActualWidth : LeftPanel.Width) * appScale;
        appScale = double.IsFinite(scale) ? Math.Clamp(Math.Round(scale, 3), 0.7, 2.5) : 1;
        ScaleHost.LayoutTransform = new ScaleTransform(appScale, appScale);
        SidebarRail.LayoutTransform = new ScaleTransform(1 / appScale, 1 / appScale);
        OutputDivider.LayoutTransform = new ScaleTransform(1 / appScale, 1 / appScale);
        LeftPanel.Width = sidebarWidth / appScale;
        ScaleHost.ColumnDefinitions[1].Width = new GridLength(24 / appScale);
        ContentPane.ColumnDefinitions[1].Width = new GridLength(8 / appScale);
        ContentPane.ColumnDefinitions[3].Width = new GridLength(24 / appScale);
        ContentPane.ColumnDefinitions[2].MinWidth = (appScale > 1 ? 200 : 300) / appScale;
        ContentPane.ColumnDefinitions[4].MinWidth = TerminalPanel.Visibility == Visibility.Visible ? 200 / appScale : 0;
        // Panes never scroll sideways at any zoom: content takes the viewport width and text wraps inside it.
        // A forced minimum content width wider than the viewport is what clipped text mid-word above 100%.
        ToolScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        OutputScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        UpdateScaledWorkspaceWidths();
        EvidenceBox.FontSize = SizeSlider.Value;
        EvidenceBox.TextWrapping = TextWrapping.Wrap;
        EvidenceBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        ActivityLog.TextWrapping = TextWrapping.Wrap;
        TextOptions.SetTextFormattingMode(ScaleHost, appScale == 1 ? TextFormattingMode.Display : TextFormattingMode.Ideal);
        TextOptions.SetTextRenderingMode(ScaleHost, appScale == 1 ? TextRenderingMode.Auto : TextRenderingMode.Grayscale);
    }
    private void UpdateScaledWorkspaceWidths()
    {
        OutputWorkspace.Width = double.NaN;
        if (ToolScroll.Content is FrameworkElement content) content.Width = double.NaN;
    }
    private bool HandleShellKey(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && AboutOverlay.Visibility == Visibility.Visible)
        { FadeOverlayOut(AboutOverlay); e.Handled = true; return true; }
        if (e.Key == Key.Escape && ShortcutsOverlay.Visibility == Visibility.Visible)
        { ShortcutsOverlay.Visibility = Visibility.Collapsed; e.Handled = true; return true; }
        if (Keyboard.Modifiers != ModifierKeys.None) return false;
        switch (e.Key)
        {
            case Key.F1: OnShortcuts(this, e); break;
            case Key.F2: OnToggleSources(this, e); break;
            case Key.F3: OnToggleActivity(this, e); break;
            case Key.F12: _ = ShowAboutAsync(); break;
            default: return false;
        }
        e.Handled = true;
        return true;
    }

}
