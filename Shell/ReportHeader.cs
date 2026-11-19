using System.Windows;
using System.Windows.Controls;

namespace KillerBench.Shell;

// The report header keeps the view toggle beside the title until the pane gets too narrow, then drops it to its own row.
public partial class MainWindow
{
    private const double NarrowReportHeader = 420;

    private void OnEvidenceHeaderSized(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < NarrowReportHeader;
        bool isNarrow = ReferenceEquals(OutputViewButtons.Parent, NarrowViewButtonsHost);
        if (narrow == isNarrow) return;
        if (narrow)
        {
            EvidenceHeader.Children.Remove(OutputViewButtons);
            OutputViewButtons.Margin = new Thickness(0);
            NarrowViewButtonsHost.Child = OutputViewButtons;
            NarrowViewButtonsHost.Visibility = Visibility.Visible;
        }
        else
        {
            NarrowViewButtonsHost.Child = null;
            NarrowViewButtonsHost.Visibility = Visibility.Collapsed;
            OutputViewButtons.Margin = new Thickness(12, 0, 0, 0);
            // Dock order matters: the toggle sits after Back and Export, before the title that fills the rest.
            EvidenceHeader.Children.Insert(EvidenceHeader.Children.IndexOf(EvidenceTitle), OutputViewButtons);
        }
    }
}
