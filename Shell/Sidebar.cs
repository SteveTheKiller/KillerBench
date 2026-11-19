using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KillerBench.Shell;

// One left column drives every page: Home, one row per area, History. WorkspaceNavigation stays the page model.
public partial class MainWindow
{
    private const double SidebarFullWidth = 188;
    private const double SidebarIconWidth = 44;
    private bool sidebarCompact;

    private void InitializeSidebar()
    {
        // The label-mode menu that used to sit on the tab bar now belongs to the sidebar it styles.
        WorkspaceNavigation.ContextMenu = null;
        LeftPanel.ContextMenu = ToolbarMenu;
        SizeChanged += (_, _) => RefreshSidebarWidth();
        RefreshSidebarWidth();
        RefreshSidebarSelection();
    }

    private void OnSidebarHome(object sender, RoutedEventArgs e) => SelectPage("Overview");

    private void OnSidebarHistory(object sender, RoutedEventArgs e) => SelectPage("History");

    private void SelectPage(string tag)
    {
        if (!WorkspaceNavigation.IsEnabled) return;
        var item = WorkspaceNavigation.Items.OfType<ListBoxItem>().Single(candidate => Equals(candidate.Tag, tag));
        if (ReferenceEquals(WorkspaceNavigation.SelectedItem, item)) SetOutputOpen(false);
        else WorkspaceNavigation.SelectedItem = item;
    }

    // A click on an area row always opens that area, including the one that is already selected.
    private void OnSidebarAreaClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(CategoryList, source) is not ListBoxItem) return;
        if (!CategoryList.IsEnabled) return;
        ShowToolsWorkspace();
    }

    // Keyboard selection in the area list opens the area too; programmatic syncs do not move the page.
    private void OpenSelectedAreaFromSidebar()
    {
        if (IsLoaded && CategoryList.IsKeyboardFocusWithin && !synchronizingNavigation) ShowToolsWorkspace();
    }

    // Exactly one sidebar row looks selected: Home, the current area, or History.
    private void RefreshSidebarSelection()
    {
        if (SidebarHome is null || CategoryList is null) return;
        string page = WorkspaceNavigation.SelectedItem is ListBoxItem { Tag: string tag } ? tag : "Overview";
        MarkViewButton(SidebarHome, page is "Overview" or "Investigate");
        MarkViewButton(SidebarHistory, page == "History");
        CategoryList.Tag = page == "Tools" ? "active" : null;
    }

    private void RefreshSidebarWidth()
    {
        if (LeftPanel is null) return;
        // Effective width, so 250% zoom on a small window collapses the same way a small window at 100% does.
        // "No text" in the appearance menu keeps it icons only at any width.
        bool compact = _labelMode == LabelMode.None || ActualWidth > 0 && ActualWidth / appScale < 900;
        sidebarCompact = compact;
        LeftPanel.Width = compact ? SidebarIconWidth : SidebarFullWidth;
        var labels = compact ? Visibility.Collapsed : Visibility.Visible;
        SidebarHomeText.Visibility = labels;
        SidebarHistoryText.Visibility = labels;
        // Icons only: no label gap, and the 18px glyph centered in the 44px column (1px border each side, 12px padding).
        LeftPanel.Resources["SidebarLabelVisibility"] = labels;
        LeftPanel.Resources["SidebarIconGap"] = compact ? new Thickness(0) : new Thickness(0, 0, 10, 0);
        LeftPanel.Resources["SidebarRowPadding"] = compact ? new Thickness(12, 0, 12, 0) : new Thickness(10, 0, 10, 0);
        LeftPanel.Resources["SidebarDotAlignment"] = compact ? VerticalAlignment.Top : VerticalAlignment.Center;
        // A 16px scrollbar would eat the narrow column, so it hides there; the wheel and arrow keys still scroll.
        ScrollViewer.SetVerticalScrollBarVisibility(CategoryList, compact ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto);
        SidebarHome.Padding = SidebarHistory.Padding = compact ? new Thickness(12, 0, 12, 0) : new Thickness(11, 0, 11, 0);
        SidebarRail.Visibility = Visibility.Visible;
        // Icons only: the four app buttons stack two by two so they fit the narrow column.
        if (SidebarRail.Children.OfType<StackPanel>().FirstOrDefault() is { } rail)
            rail.Orientation = compact ? Orientation.Vertical : Orientation.Horizontal;
        foreach (var button in SidebarRail.Children.OfType<StackPanel>().SelectMany(panel => panel.Children.OfType<Button>()))
            button.Margin = compact ? new Thickness(0, 0, 0, 12) : new Thickness(0, 0, 16, 0);
        SidebarRail.Margin = compact ? new Thickness(12, 0, 0, 4) : new Thickness(10, 0, 0, 10);
        if (LeftPanel.Child is DockPanel dock && dock.Children[0] is Border spacer)
            spacer.Height = compact ? 4 * 32 : 34;
    }
}
