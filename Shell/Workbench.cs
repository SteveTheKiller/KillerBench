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

namespace KillerBench;

public partial class MainWindow
{
    private readonly List<TextEvidence> sources = [];
    private readonly Queue<SessionActivityEntry> activity = new();
    private bool shellReady;
    private bool sourcesOpen = true;
    private double appScale = 1;
    private readonly Dictionary<Control, double> zoomFonts = [];
    private void InitializeShell()
    {
        GrainTexture.Apply(this);
        Shell.SidebarChrome.WireEdgeFades(SourcesList, SourceFadeTop, SourceFadeBottom);
        InitializeShortcuts();
        SourceInitialized += (_, _) => { DwmChrome.SetRoundedCorners(this, true); DwmChrome.SetThemeBorder(this); };
        StateChanged += (_, _) =>
        {
            DwmChrome.SetRoundedCorners(this, WindowState != WindowState.Maximized);
            MaximizeButton.Content = ((char)(WindowState == WindowState.Maximized ? 0xE923 : 0xE922)).ToString();
        };
        ThemeManager.ThemeChanged += RefreshTheme;
        Closed += (_, _) => { ThemeManager.ThemeChanged -= RefreshTheme; knowledgeUpdateCancellation?.Cancel(); };
        SizeSlider.Value = ReadNumber("ConsoleSize", 14, 12, 24);
        appScale = ReadNumber("AppScale", 1, 0.7, 2.5);
        ApplyAppScale(appScale);
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
        ThemeStatus.Text = "v0.1.0";
        DwmChrome.SetThemeBorder(this);
    }
    private static string ThemeLabel(Theme theme) => theme == Theme.Retro98 ? "98SE" : theme.ToString();

    private void RegisterSource(TextEvidence item)
    {
        int old = sources.FindIndex(s => s.Source == item.Source);
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
            ToolTip = item.Source, Tag = item
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
        SearchBox.Clear();
        ShowEvidence();
        if (workflowSummaries.TryGetValue(selected, out var workflow))
        {
            if (workflowReportTitles.TryGetValue(selected, out var reportTitle)) RenderNamedWorkflowReport(workflow, reportTitle);
            else RenderWorkflowReport(workflow);
        }
        else ClearWorkflowReport();
        if (SourceDetails.Visibility == Visibility.Visible) SetDetailsOpen(true);
    }

    private void Report(string message)
    {
        StatusLabel.Text = message;
        activity.Enqueue(new(DateTimeOffset.Now, message));
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
        sourcesOpen = !sourcesOpen;
        LeftPanel.IsEnabled = sourcesOpen;
        AnimateWidth(LeftPanel, sourcesOpen ? 370 : 0);
        SidebarButton.Content = ((char)(sourcesOpen ? 0xE76B : 0xE76C)).ToString();
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
        ApplyAppScale(appScale + Math.Sign(e.Delta) * 0.02);
        Preferences.Set("AppScale", appScale.ToString(CultureInfo.InvariantCulture));
        StatusLabel.Text = $"Interface zoom: {appScale:P0}";
        e.Handled = true;
    }
    private void ApplyAppScale(double scale)
    {
        appScale = double.IsFinite(scale) ? Math.Clamp(Math.Round(scale, 3), 0.7, 2.5) : 1;
        ScaleHost.LayoutTransform = Transform.Identity;
        var controls = FindZoomControls(ScaleHost).ToArray();
        foreach (var control in controls)
            if (!zoomFonts.ContainsKey(control)) zoomFonts.Add(control, control.FontSize);
        foreach (var control in controls)
        {
            control.SetCurrentValue(Control.FontSizeProperty, zoomFonts[control] * appScale);
            if (control is ComboBox)
            {
                control.SetCurrentValue(HeightProperty, double.NaN);
                control.SetCurrentValue(MinHeightProperty, Math.Max(24, control.FontSize * 1.5 + 8));
            }
        }
        EvidenceBox.FontSize = SizeSlider.Value * appScale;
        EvidenceBox.TextWrapping = TextWrapping.Wrap;
        EvidenceBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        ActivityLog.TextWrapping = TextWrapping.Wrap;
        TextOptions.SetTextFormattingMode(ScaleHost, appScale == 1 ? TextFormattingMode.Display : TextFormattingMode.Ideal);
        TextOptions.SetTextRenderingMode(ScaleHost, appScale == 1 ? TextRenderingMode.Auto : TextRenderingMode.Grayscale);
    }
    private static IEnumerable<Control> FindZoomControls(DependencyObject parent)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is not DependencyObject element) continue;
            if (element is TextBox or ComboBox) yield return (Control)element;
            foreach (var nested in FindZoomControls(element)) yield return nested;
        }
    }
    private bool HandleShellKey(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && AboutOverlay.Visibility == Visibility.Visible)
        { CloseAbout(); e.Handled = true; return true; }
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
