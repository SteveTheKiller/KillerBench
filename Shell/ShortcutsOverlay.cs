using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KillerBench.Services;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private enum ShortcutCategory { View, Nav, File, Help }
    private enum ShortcutLayer { Base, Ctrl, CtrlShift, AltShift }

    private readonly record struct ShortcutBinding(
        string Keys, string LabelKey, ShortcutCategory Category, bool Ctrl, params string[] Caps)
    {
        public ShortcutLayer Layer => Keys.StartsWith("Alt+Shift+", StringComparison.Ordinal) ? ShortcutLayer.AltShift
            : Keys.StartsWith("Ctrl+Shift+", StringComparison.Ordinal) ? ShortcutLayer.CtrlShift
            : Ctrl ? ShortcutLayer.Ctrl : ShortcutLayer.Base;
    }

    private static readonly ShortcutBinding[] ShortcutBindings =
    [
        new("Ctrl+1", "Str_Workspace_Overview", ShortcutCategory.Nav, true, "D1"),
        new("Ctrl+2", "Str_Workspace_Tools", ShortcutCategory.Nav, true, "D2"),
        new("Ctrl+3", "Str_Workspace_History", ShortcutCategory.Nav, true, "D3"),
        new("Ctrl+Shift+1", "Str_Toolbar_SmallIcons", ShortcutCategory.View, true, "D1"),
        new("Ctrl+Shift+3", "Str_Toolbar_TextNone", ShortcutCategory.View, true, "D3"),
        new("Ctrl+Shift+4", "Str_Toolbar_TextBeside", ShortcutCategory.View, true, "D4"),
        new("Ctrl+Shift+6", "Str_Toolbar_TextOnly", ShortcutCategory.View, true, "D6"),
        new("F2", "Str_Shortcuts_ToggleSources", ShortcutCategory.View, false, "F2"),
        new("F3", "Str_Shortcuts_ToggleActivity", ShortcutCategory.View, false, "F3"),
        new("Alt+Shift+G", "Str_Theme_FilmGrain", ShortcutCategory.View, false, "G"),
        new("Ctrl+Shift+Plus / Minus / 0", "Str_Shortcuts_AppZoom", ShortcutCategory.View, true, "Plus", "Minus", "D0"),
        new("Ctrl+O", "Str_Shortcuts_OpenEvidence", ShortcutCategory.File, true, "O"),
        new("Ctrl+Shift+O", "Str_OpenCapture", ShortcutCategory.File, true, "O"),
        new("Ctrl+S", "Str_SaveCapture", ShortcutCategory.File, true, "S"),
        new("Ctrl+E", "Str_ExportReport", ShortcutCategory.File, true, "E"),
        new("Ctrl+Shift+E", "Str_ExportActivityHint", ShortcutCategory.File, true, "E"),
        new("Ctrl+F", "Str_AutomationProperties_Name_Filter_evidence", ShortcutCategory.Nav, true, "F"),
        new("Ctrl+K", "Str_Search_All", ShortcutCategory.Nav, true, "K"),
        new("F1", "Str_Shortcuts_Show", ShortcutCategory.Help, false, "F1"),
        new("F12", "Str_Shortcuts_About", ShortcutCategory.Help, false, "F12"),
        new("Esc", "Str_Shortcuts_CloseOrStop", ShortcutCategory.Nav, false, "Esc")
    ];

    private static readonly (string Id, string Cap, double Width)[][] KeyboardRows =
    [
        [("Esc", "Esc", 1.0), ("", "", 0.7), ("F1", "F1", 1.0), ("F2", "F2", 1.0), ("F3", "F3", 1.0), ("F4", "F4", 1.0), ("", "", 0.5), ("F5", "F5", 1.0), ("F6", "F6", 1.0), ("F7", "F7", 1.0), ("F8", "F8", 1.0), ("", "", 0.5), ("F9", "F9", 1.0), ("F10", "F10", 1.0), ("F11", "F11", 1.0), ("F12", "F12", 1.0)],
        [("D1", "1", 1.0), ("D2", "2", 1.0), ("D3", "3", 1.0), ("D4", "4", 1.0), ("D5", "5", 1.0), ("D6", "6", 1.0), ("", "", 1.8), ("D0", "0", 1.0), ("Minus", "-", 1.0), ("Plus", "+", 1.0)],
        [("Tab", "Tab", 1.5), ("Q", "Q", 1.0), ("W", "W", 1.0), ("E", "E", 1.0), ("R", "R", 1.0), ("T", "T", 1.0), ("Y", "Y", 1.0), ("U", "U", 1.0), ("I", "I", 1.0), ("O", "O", 1.0), ("P", "P", 1.0)],
        [("Caps", "Caps", 1.8), ("A", "A", 1.0), ("S", "S", 1.0), ("D", "D", 1.0), ("F", "F", 1.0), ("G", "G", 1.0), ("H", "H", 1.0), ("J", "J", 1.0), ("K", "K", 1.0), ("L", "L", 1.0), ("Enter", "Enter", 2.2)],
        [("Shift", "Shift", 2.3), ("Z", "Z", 1.0), ("X", "X", 1.0), ("C", "C", 1.0), ("V", "V", 1.0), ("B", "B", 1.0), ("N", "N", 1.0), ("M", "M", 1.0), ("Comma", ",", 1.0), ("Period", ".", 1.0), ("RShift", "Shift", 2.7)],
        [("Ctrl", "Ctrl", 1.5), ("Win", "Win", 1.2), ("Alt", "Alt", 1.5), ("Space", "", 6.8), ("RAlt", "Alt", 1.5), ("RCtrl", "Ctrl", 1.5)]
    ];

    private const double KeyboardUnit = 42;
    private ShortcutLayer shortcutLayer;

    private void InitializeShortcuts()
    {
        ShortcutButton.ToolTip = KillerFamily.Layout.ShortcutHints.WithHint(Label(ShortcutBindings.Single(binding => binding.Keys == "F1")), "F1");
        ApplyShortcutView(Preferences.Get("ShortcutsMapView") == "1");
        RoutedUICommand[] destinations = [WorkbenchCommands.Overview, WorkbenchCommands.Tools, WorkbenchCommands.History];
        for (int index = 0; index < destinations.Length; index++)
        {
            int target = index;
            var item = (ListBoxItem)WorkspaceNavigation.Items[index];
            BindAction(destinations[index], "Str_Workspace_" + item.Tag, (Button)item.Content,
                (_, _) =>
                {
                    if (WorkspaceNavigation.SelectedIndex == target && target == 0) ShowAreaEvidence();
                    else WorkspaceNavigation.SelectedIndex = target;
                });
        }
        BindAction(WorkbenchCommands.OpenCapture, "Str_OpenCapture", OpenCaptureButton, OnOpenCapture);
        BindAction(WorkbenchCommands.SaveCapture, "Str_SaveCapture", SaveCaptureButton, OnSaveCapture);
        BindAction(WorkbenchCommands.ExportReport, "Str_ExportReport", ExportReportButton, OnExportReport);
        BindAction(WorkbenchCommands.ExportActivity, "Str_ExportActivityHint", ExportActivityButton, OnExportActivity);
        BindAction(WorkbenchCommands.FindEvidence, "Str_AutomationProperties_Name_Filter_evidence", SearchBox,
            (_, _) => { ShowRawOutput(); SearchBox.Focus(); SearchBox.SelectAll(); });
        SetShortcutHint(HistoryOpenCaptureButton, "Str_OpenCapture");
        SetShortcutHint(OverviewOpenCaptureButton, "Str_OpenCapture");
    }

    private void BindAction(RoutedUICommand command, string labelKey, UIElement control, RoutedEventHandler execute)
    {
        BindShortcutCommand(command, labelKey, () => control.IsEnabled, execute);
        if (control is MenuItem menu) menu.InputGestureText = ShortcutBindings.Single(item => item.LabelKey == labelKey).Keys;
        SetShortcutHint(control, labelKey);
    }

    private void BindShortcutCommand(RoutedUICommand command, string labelKey, Func<bool> canExecute, RoutedEventHandler execute)
    {
        ShortcutBinding binding = ShortcutBindings.Single(item => item.LabelKey == labelKey);
        var gesture = (KeyGesture)new KeyGestureConverter().ConvertFromInvariantString(binding.Keys)!;
        InputBindings.Add(new KeyBinding(command, gesture));
        bool Available() => !App.ScreenshotMode && canExecute() &&
            AboutOverlay.Visibility != Visibility.Visible && ShortcutsOverlay.Visibility != Visibility.Visible;
        CommandBindings.Add(new CommandBinding(command, (_, e) => { if (Available()) execute(this, e); e.Handled = true; },
            (_, e) => { e.CanExecute = Available(); e.Handled = true; }));
    }

    private static void SetShortcutHint(UIElement control, string labelKey) =>
        ToolTipService.SetToolTip(control, CreateShortcutHint(labelKey));

    private static TextBlock CreateShortcutHint(string labelKey)
    {
        ShortcutBinding binding = ShortcutBindings.Single(item => item.LabelKey == labelKey);
        string tipKey = labelKey switch
        {
            "Str_ExportReport" => "Str_ExportContents",
            "Str_AutomationProperties_Name_Filter_evidence" => "Str_ToolTip_Filter_loaded_lines_Clear_to_show_all_lines",
            _ => labelKey
        };
        var tip = new TextBlock();
        var label = new System.Windows.Documents.Run();
        label.SetResourceReference(System.Windows.Documents.Run.TextProperty, tipKey);
        tip.Inlines.Add(label);
        tip.Inlines.Add(new System.Windows.Documents.Run(" (" + binding.Keys + ")"));
        return tip;
    }

    private void OnShortcuts(object sender, RoutedEventArgs e)
    {
        if (ShortcutsOverlay.Visibility == Visibility.Visible)
        {
            ShortcutsOverlay.Visibility = Visibility.Collapsed;
            return;
        }
        AboutOverlay.Visibility = Visibility.Collapsed;
        ApplyShortcutView(Preferences.Get("ShortcutsMapView") == "1");
        ShortcutsOverlay.Visibility = Visibility.Visible;
    }

    private void OnShortcutsBackdrop(object sender, MouseButtonEventArgs e) => ShortcutsOverlay.Visibility = Visibility.Collapsed;
    private void OnShortcutsCard(object sender, MouseButtonEventArgs e) => e.Handled = true;
    private void OnShortcutList(object sender, RoutedEventArgs e) => ApplyShortcutView(false, true);
    private void OnShortcutMap(object sender, RoutedEventArgs e) => ApplyShortcutView(true, true);
    private void OnShortcutBase(object sender, RoutedEventArgs e) { shortcutLayer = ShortcutLayer.Base; BuildKeyboard(); }
    private void OnShortcutCtrl(object sender, RoutedEventArgs e) { shortcutLayer = ShortcutLayer.Ctrl; BuildKeyboard(); }
    private void OnShortcutCtrlShift(object sender, RoutedEventArgs e) { shortcutLayer = ShortcutLayer.CtrlShift; BuildKeyboard(); }
    private void OnShortcutAltShift(object sender, RoutedEventArgs e) { shortcutLayer = ShortcutLayer.AltShift; BuildKeyboard(); }

    private void ApplyShortcutView(bool keyboard, bool persist = false)
    {
        if (persist)
        {
            Preferences.Set("ShortcutsMapView", keyboard ? "1" : "0");
            Preferences.Save();
        }
        ShortcutListScroll.Visibility = keyboard ? Visibility.Collapsed : Visibility.Visible;
        ShortcutMapHost.Visibility = keyboard ? Visibility.Visible : Visibility.Collapsed;
        ShortcutListButton.Tag = keyboard ? null : "on";
        ShortcutMapButton.Tag = keyboard ? "on" : null;
        ShortcutCard.MaxWidth = keyboard ? 940 : 640;
        if (keyboard) BuildKeyboard(); else BuildShortcutList();
    }

    private void BuildShortcutList()
    {
        ShortcutListHost.Children.Clear();
        // Key column sizes to the widest key text in the list so long chords never run into their labels.
        Grid.SetIsSharedSizeScope(ShortcutListHost, true);
        foreach (ShortcutCategory category in Enum.GetValues<ShortcutCategory>())
        {
            var group = ShortcutBindings.Where(binding => binding.Category == category).ToArray();
            if (group.Length == 0) continue;
            var heading = new TextBlock { Text = Resource("Str_Shortcuts_Group" + category), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, ShortcutListHost.Children.Count == 0 ? 0 : 12, 0, 6) };
            heading.SetResourceReference(TextBlock.ForegroundProperty, "KsCat" + category);
            ShortcutListHost.Children.Add(heading);
            foreach (ShortcutBinding binding in group)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 100, SharedSizeGroup = "ShortcutKeys" });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var keys = new TextBlock { Text = binding.Keys, FontSize = 12, Margin = new Thickness(0, 0, 16, 0) };
                keys.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
                var label = new TextBlock { Text = Label(binding), FontSize = 12, TextWrapping = TextWrapping.Wrap };
                label.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
                Grid.SetColumn(label, 1);
                row.Children.Add(keys);
                row.Children.Add(label);
                ShortcutListHost.Children.Add(row);
            }
        }
    }

    private void BuildKeyboard()
    {
        ShortcutKeyboardRows.Children.Clear();
        var lit = ShortcutBindings.Where(binding => binding.Layer == shortcutLayer)
            .SelectMany(binding => binding.Caps.Select(cap => (cap, binding)))
            .ToDictionary(item => item.cap, item => item.binding, StringComparer.Ordinal);
        foreach (var row in KeyboardRows)
        {
            var host = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            foreach (var key in row)
            {
                if (key.Id.Length == 0) { host.Children.Add(new Border { Width = KeyboardUnit * key.Width }); continue; }
                lit.TryGetValue(key.Id, out ShortcutBinding binding);
                bool bound = binding.Keys is not null;
                var text = new TextBlock { Text = bound ? key.Cap + "\n" + Label(binding) : key.Cap, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = bound ? 8.5 : 10.5, VerticalAlignment = VerticalAlignment.Center };
                text.SetResourceReference(TextBlock.ForegroundProperty, bound ? "KsCat" + binding.Category : "DimTextBrush");
                var cap = new Border { Width = KeyboardUnit * key.Width - 4, Height = 46, Margin = new Thickness(0, 0, 4, 0), CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), ToolTip = bound ? binding.Keys + ": " + Label(binding) : null };
                cap.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
                cap.SetResourceReference(Border.BorderBrushProperty, bound ? "KsCat" + binding.Category : "CardBorderBrush");
                if (bound)
                {
                    var marker = new Rectangle { Height = 3, VerticalAlignment = VerticalAlignment.Bottom, RadiusX = 1.5, RadiusY = 1.5 };
                    marker.SetResourceReference(Shape.FillProperty, "KsCat" + binding.Category);
                    var grid = new Grid(); grid.Children.Add(text); grid.Children.Add(marker); cap.Child = grid;
                }
                else cap.Child = text;
                host.Children.Add(cap);
            }
            ShortcutKeyboardRows.Children.Add(host);
        }
        ShortcutBaseButton.Tag = shortcutLayer == ShortcutLayer.Base ? "on" : null;
        ShortcutCtrlButton.Tag = shortcutLayer == ShortcutLayer.Ctrl ? "on" : null;
        ShortcutCtrlShiftButton.Tag = shortcutLayer == ShortcutLayer.CtrlShift ? "on" : null;
        ShortcutAltShiftButton.Tag = shortcutLayer == ShortcutLayer.AltShift ? "on" : null;
    }

    private static string Resource(string key) => Application.Current.TryFindResource(key)?.ToString() ?? key;
    private static string Label(ShortcutBinding binding) => Resource(binding.LabelKey);
}
