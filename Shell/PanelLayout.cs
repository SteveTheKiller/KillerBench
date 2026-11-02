using System.Windows;
using KillerBench.Services;

namespace KillerBench.Shell;

// Category navigation stays visible. The selected tool fills the workspace until output is needed.
public partial class MainWindow
{
    private const double SidebarDefaultWidth = 160;
    private const double SidebarMinOpen = 140;
    private const double SidebarMaxWidth = 360;

    private double sidebarWidth = SidebarDefaultWidth;
    private bool rawView;

    private void InitializePanels()
    {
        sidebarWidth = ReadNumber("SidebarWidth", SidebarDefaultWidth, SidebarMinOpen, SidebarMaxWidth);
        LeftPanel.Width = sidebarWidth / appScale;
        SetOutputView();
        SetOutputOpen(false);
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

    private void SetOutputOpen(bool open)
    {
        if (TerminalPanel is null) return;
        bool wasOpen = TerminalPanel.Visibility == Visibility.Visible;
        TerminalPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        ContentPane.ColumnDefinitions[4].Width = new System.Windows.GridLength(open ? 3 : 0, System.Windows.GridUnitType.Star);
        SidebarButton.Tag = open ? "on" : null;
        SidebarButton.Content = ((char)(open ? 0xE76C : 0xE76B)).ToString();
        if (open && !wasOpen && IsVisible && System.Windows.SystemParameters.ClientAreaAnimation)
        {
            var slide = new System.Windows.Media.TranslateTransform();
            TerminalPanel.RenderTransform = slide;
            slide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
                new System.Windows.Media.Animation.DoubleAnimation(28, 0, TimeSpan.FromMilliseconds(150)));
        }
    }

}
