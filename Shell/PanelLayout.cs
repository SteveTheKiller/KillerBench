using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using KillerBench.Services;

namespace KillerBench;

// Three-pane shell: section navigation on the left, the selected tool's options in the center, and its
// output (readable report or raw log) in the wider pane on the right. The left sidebar resizes with the
// family grip (KillerPDF's sidebar behavior): drag past the minimum and release to snap it closed.
public partial class MainWindow
{
    private const double SidebarDefaultWidth = 160;
    private const double SidebarMinOpen = 140;
    private const double SidebarMaxWidth = 360;
    private const double PanelCloseBuffer = 36;

    private double sidebarWidth = SidebarDefaultWidth;
    private bool sidebarDragOpening;
    private bool sidebarWantClose;
    private bool rawView;

    private void InitializePanels()
    {
        sidebarWidth = ReadNumber("SidebarWidth", SidebarDefaultWidth, SidebarMinOpen, SidebarMaxWidth);
        LeftPanel.Width = sidebarWidth;
        SetOutputView();
    }

    private void RefreshOutputButtons()
    {
        ConsoleViewButton.IsEnabled = true;
        ReportViewButton.IsEnabled = true;
        MarkViewButton(ReportViewButton, !rawView);
        MarkViewButton(ConsoleViewButton, rawView);
    }

    // Family active-view mark: the theme's selection pair, set locally so hover cannot clear it.
    private static void MarkViewButton(System.Windows.Controls.Button button, bool active)
    {
        if (active)
        {
            button.SetResourceReference(BackgroundProperty, "SelectionBg");
            button.SetResourceReference(ForegroundProperty, "SelectionFg");
            button.Tag = "selected";
            return;
        }
        button.ClearValue(BackgroundProperty);
        button.ClearValue(ForegroundProperty);
        button.Tag = null;
    }

    private void ShowRawOutput()
    {
        rawView = true;
        SetOutputView();
    }

    private void CollapseSidebar()
    {
        sourcesOpen = false;
        LeftPanel.IsEnabled = false;
        AnimateWidth(LeftPanel, 0);
        SidebarButton.Content = ((char)0xE76C).ToString();
    }

    private void OnSidebarGripStarted(object sender, DragStartedEventArgs e)
    {
        LeftPanel.BeginAnimation(WidthProperty, null);
        sidebarWantClose = false;
        if (sourcesOpen) return;
        // Grabbing the grip while the sidebar is closed pulls it open from the rail.
        sourcesOpen = true;
        sidebarDragOpening = true;
        LeftPanel.IsEnabled = true;
        LeftPanel.Width = 0;
        SidebarButton.Content = ((char)0xE76B).ToString();
    }

    private void OnSidebarGripDelta(object sender, DragDeltaEventArgs e)
    {
        double current = double.IsNaN(LeftPanel.Width) ? LeftPanel.ActualWidth : LeftPanel.Width;
        double next = current + e.HorizontalChange;
        if (sidebarDragOpening)
        {
            LeftPanel.Width = Math.Clamp(next, 0, SidebarMaxWidth);
            return;
        }
        sidebarWantClose = Mouse.GetPosition(ScaleHost).X < SidebarMinOpen - PanelCloseBuffer;
        LeftPanel.Width = Math.Clamp(next, SidebarMinOpen, SidebarMaxWidth);
    }

    private void OnSidebarGripCompleted(object sender, DragCompletedEventArgs e)
    {
        sidebarDragOpening = false;
        double width = double.IsNaN(LeftPanel.Width) ? LeftPanel.ActualWidth : LeftPanel.Width;
        if (sidebarWantClose || width < SidebarMinOpen / 2)
        {
            sidebarWantClose = false;
            CollapseSidebar();
            return;
        }
        if (width < SidebarMinOpen) LeftPanel.Width = width = SidebarMinOpen;
        sidebarWidth = width;
        Preferences.Set("SidebarWidth", width.ToString(CultureInfo.InvariantCulture));
    }
}
