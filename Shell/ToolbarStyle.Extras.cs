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
        BindShortcutCommand(WorkbenchCommands.ToolbarLargeIcons, "Str_Toolbar_LargeIcons", CanChangeIconSize,
            (_, _) => SetIconSize(IconSize.Large));
        BindShortcutCommand(WorkbenchCommands.ToolbarTextNone, "Str_Toolbar_TextNone", CanChangeAppearance,
            (_, _) => SetLabelMode(LabelMode.None));
        BindShortcutCommand(WorkbenchCommands.ToolbarTextBeside, "Str_Toolbar_TextBeside", CanChangeAppearance,
            (_, _) => SetLabelMode(LabelMode.Beside));
        BindShortcutCommand(WorkbenchCommands.ToolbarTextUnder, "Str_Toolbar_TextUnder", CanChangeAppearance,
            (_, _) => SetLabelMode(LabelMode.Under));
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
        gesture = ShortcutBindings.Single(binding => binding.LabelKey == key).Keys;
    }

    partial void OnToolbarStyleApplied()
    {
        foreach (var (button, _, key) in ToolbarItems())
            AutomationProperties.SetName(button, Loc(key));
        RefreshWorkspaceToolbarSelection();
        ReflowCaption();
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
