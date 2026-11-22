using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KillerBench.Services;

namespace KillerBench.Shell;

// APP-OWNED: KillerBench's rows for the kit's F1 overlay (Shell/ShortcutsOverlay.cs), and the command
// bindings that read their chords from the same table, so a tooltip, a menu gesture and the overlay
// can never disagree about a key.
public partial class MainWindow
{
    private static partial KsBinding[] KsAppRows() =>
    [
        new("Ctrl+1", "Str_Workspace_Overview", KsCat.Nav, "D1"),
        new("Ctrl+2", "Str_Workspace_Tools", KsCat.Nav, "D2"),
        new("Ctrl+3", "Str_Workspace_History", KsCat.Nav, "D3"),
        new("Ctrl+Shift+1", "Str_Toolbar_SmallIcons", KsCat.View, "D1"),
        new("Ctrl+Shift+3", "Str_Toolbar_TextNone", KsCat.View, "D3"),
        new("Ctrl+Shift+4", "Str_Toolbar_TextBeside", KsCat.View, "D4"),
        new("Ctrl+Shift+6", "Str_Toolbar_TextOnly", KsCat.View, "D6"),
        new("F2", "Str_Shortcuts_ToggleSources", KsCat.View, "F2"),
        new("F3", "Str_Shortcuts_ToggleActivity", KsCat.View, "F3"),
        new("Ctrl+Shift+Plus / Minus / 0", "Str_Shortcuts_AppZoom", KsCat.View, "Equals", "Minus", "D0"),
        new("Ctrl+O", "Str_Shortcuts_OpenEvidence", KsCat.File, "O"),
        new("Ctrl+Shift+O", "Str_OpenCapture", KsCat.File, "O"),
        new("Ctrl+S", "Str_SaveCapture", KsCat.File, "S"),
        new("Ctrl+E", "Str_ExportReport", KsCat.File, "E"),
        new("Ctrl+Shift+E", "Str_ExportActivityHint", KsCat.File, "E"),
        new("Ctrl+F", "Str_AutomationProperties_Name_Filter_evidence", KsCat.Nav, "F"),
        new("Ctrl+K", "Str_Search_All", KsCat.Nav, "K"),
    ];

    // KillerBench's toolbar has four of the six looks, so it keeps its own Ctrl+Shift rows above
    // rather than the family 1-6 row, and it has no F9 sidebar toggle or Quit chord.
    private static partial FamilyKeys FamilyKeysInApp() => FamilyKeys.None;

    private static string ShortcutKeys(string labelKey) =>
        KsAll.FirstOrDefault(binding => binding.LabelKey == labelKey).Keys ?? "";

    private void InitializeShortcuts()
    {
        ShortcutButton.ToolTip = KillerFamily.Layout.ShortcutHints.WithHint(Resource("Str_KS_ThisList"), "F1");
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
        // The capture actions live on the Session sources card, whose tiles show these chords.
        BindShortcutCommand(WorkbenchCommands.OpenCapture, "Str_OpenCapture", () => OpenCaptureEnabled, OnOpenCapture);
        BindShortcutCommand(WorkbenchCommands.SaveCapture, "Str_SaveCapture", () => SaveCaptureEnabled, OnSaveCapture);
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
        if (control is MenuItem menu) menu.InputGestureText = ShortcutKeys(labelKey);
        SetShortcutHint(control, labelKey);
    }

    private void BindShortcutCommand(RoutedUICommand command, string labelKey, Func<bool> canExecute, RoutedEventHandler execute)
    {
        var gesture = (KeyGesture)new KeyGestureConverter().ConvertFromInvariantString(ShortcutKeys(labelKey))!;
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
        tip.Inlines.Add(new System.Windows.Documents.Run(" (" + ShortcutKeys(labelKey) + ")"));
        return tip;
    }

    // The rail's ? button and the demo captures open the overlay through the kit's toggle.
    private void OnShortcuts(object sender, RoutedEventArgs e) => ToggleShortcutsOverlay();

    private static string Resource(string key) => Application.Current.TryFindResource(key)?.ToString() ?? key;
}
