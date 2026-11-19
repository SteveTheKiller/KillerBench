using System.Windows;
using KillerBench.Services;

namespace KillerBench.Shell;

// One pane: the page or tool fills the workspace, and output swaps in over it until Back.
public partial class MainWindow
{
    private bool rawView;

    private void InitializePanels()
    {
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
        // One pane: the output swaps in over the page and Back swaps the page in again. Nothing sits side by side.
        bool wasOpen = TerminalPanel.Visibility == Visibility.Visible;
        TerminalPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        OutputDivider.Visibility = Visibility.Collapsed;
        ContentPane.ColumnDefinitions[3].Width = new GridLength(0);
        ContentPane.ColumnDefinitions[2].MinWidth = open ? 0 : 300 / appScale;
        ContentPane.ColumnDefinitions[2].Width = open ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        ContentPane.ColumnDefinitions[4].MinWidth = 0;
        ContentPane.ColumnDefinitions[4].Width = open ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        RefreshPageVisibility();
        if (open != wasOpen && IsVisible && System.Windows.SystemParameters.ClientAreaAnimation)
        {
            var shown = open ? (FrameworkElement)TerminalPanel : ContentPane;
            var slide = new System.Windows.Media.TranslateTransform();
            shown.RenderTransform = slide;
            slide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
                new System.Windows.Media.Animation.DoubleAnimation(open ? 28 : -28, 0, TimeSpan.FromMilliseconds(150)));
        }
    }

    private void OnOutputBack(object sender, RoutedEventArgs e) => SetOutputOpen(false);

    // Clicking the item that is already selected still opens its details, since SelectionChanged won't fire.
    private void OnReopenSelected(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.ListBox list || e.OriginalSource is not DependencyObject source) return;
        if (System.Windows.Controls.ItemsControl.ContainerFromElement(list, source) is not System.Windows.Controls.ListBoxItem { IsSelected: true }) return;
        if (TerminalPanel.Visibility == Visibility.Visible) return;
        if (list != SessionHistory) ShowAreaEvidence();
    }

}
