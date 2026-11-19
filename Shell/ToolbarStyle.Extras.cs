using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using KillerBench.Controls;
using KillerBench.Services;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private void InitializeWorkspaceToolbar()
    {
        InitToolbarStyle();

        bool CanChangeAppearance() => WorkspaceNavigation.IsEnabled;
        bool CanChangeIconSize() => CanChangeAppearance() && _labelMode != LabelMode.Only;
        BindShortcutCommand(WorkbenchCommands.ToolbarSmallIcons, "Str_Toolbar_SmallIcons", CanChangeIconSize,
            (_, _) => SetIconSize(IconSize.Small));
        BindShortcutCommand(WorkbenchCommands.ToolbarTextNone, "Str_Toolbar_TextNone", CanChangeAppearance,
            (_, _) => SetLabelMode(LabelMode.None));
        BindShortcutCommand(WorkbenchCommands.ToolbarTextBeside, "Str_Toolbar_TextBeside", CanChangeAppearance,
            (_, _) => SetLabelMode(LabelMode.Beside));
        BindShortcutCommand(WorkbenchCommands.ToolbarTextOnly, "Str_Toolbar_TextOnly", CanChangeAppearance,
            (_, _) => SetLabelMode(LabelMode.Only));
    }

    private IEnumerable<(Button Btn, string Glyph, string Key)> ToolbarItems()
    {
        yield return (OverviewButton, "\uE80F", "Str_Workspace_Overview");
        yield return (InvestigateButton, "\uE721", "Str_Workspace_Investigate");
        yield return (ToolsButton, "\uE90F", "Str_Workspace_Tools");
        yield return (HistoryButton, "\uE81C", "Str_Workspace_History");
    }

    partial void OnToolbarStyleRestored()
    {
        _iconSize = IconSize.Small;
        if (_labelMode == LabelMode.Under) _labelMode = LabelMode.Beside;
        Settings.Set("ToolbarIconSize", _iconSize.ToString());
        Settings.Set("ToolbarLabels", _labelMode.ToString());
    }

    partial void AddToolbarMenuRows(ContextMenu menu)
    {
        foreach (var item in menu.Items.OfType<MenuItem>().Where(item => item.Tag is IconSize.Large or LabelMode.Under).ToArray())
            menu.Items.Remove(item);
    }

    private void CompactWorkspaceToolbarButtons()
    {
        double height = (double)FindResource("TitleBarHeight");
        foreach (var (button, _, _) in ToolbarItems())
        {
            button.Height = height;
            button.MinHeight = 0;
            button.Padding = new Thickness(6, Math.Min(3, Math.Max(0, (height - 24) / 2)), 6, Math.Min(3, Math.Max(0, (height - 24) / 2)));
            button.MaxWidth = _labelMode == LabelMode.Only && double.IsFinite(WorkspaceNavigation.MaxWidth)
                ? Math.Max(24, WorkspaceNavigation.MaxWidth / 4 - 4) : double.PositiveInfinity;
            if (button.Content is TextBlock text)
            {
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                text.MaxWidth = double.IsFinite(button.MaxWidth) ? Math.Max(8, button.MaxWidth - 12) : double.PositiveInfinity;
            }
        }
        TitleSearchBox.Height = Math.Max(18, height - 4);
        double searchPadding = Math.Min(3, Math.Max(0, (height - 22) / 2));
        TitleSearchBox.Padding = new Thickness(26, searchPadding, 8, searchPadding);
    }

    private void RefreshWorkspaceToolbarSelection()
    {
        int index = 0;
        foreach (var (button, _, _) in ToolbarItems())
        {
            MarkViewButton(button, WorkspaceNavigation.SelectedIndex == index);
            index++;
        }
    }

    private void RefreshWorkspaceToolbarLanguage() => RelocalizeToolbar();

    private static string Loc(string key) => LocalizedText.Get(key);

    partial void ToolbarTooltip(Button btn, string label, ref object tip)
    {
        string key = ToolbarItems().Single(item => item.Btn == btn).Key;
        tip = CreateShortcutHint(key);
    }

    partial void ToolbarMenuGesture(string id, ref string gesture)
    {
        string key = IconSizes.FirstOrDefault(option => option.GestureId == id).LabelKey
            ?? LabelModes.Single(option => option.GestureId == id).LabelKey;
        gesture = ShortcutBindings.FirstOrDefault(binding => binding.LabelKey == key).Keys ?? "";
    }

    partial void OnToolbarStyleApplied()
    {
        foreach (var (button, _, key) in ToolbarItems())
            AutomationProperties.SetName(button, Loc(key));
        RefreshWorkspaceToolbarSelection();
        ReflowCaption();
        RefreshSidebarWidth();
    }

    private static class Settings
    {
        internal static string? Get(string key) => Preferences.Get(key);

        internal static void Set(string key, string value)
        {
            if (!Preferences.Loaded) return;
            Preferences.Set(key, value);
            Preferences.Save();
        }
    }
}
