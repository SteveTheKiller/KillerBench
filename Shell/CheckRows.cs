using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KillerBench.Shell;

// The area page lists its checks as rows. The selected row opens in place and hosts the one shared
// options panel (CheckDetail), so a check's settings and consent always sit under its own name.
public partial class MainWindow
{
    private bool checkRowsHooked;

    private void OnCheckRowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!checkRowsHooked)
        {
            checkRowsHooked = true;
            // A new area swaps the rows; the selected row only exists once its container is generated.
            // LayoutUpdated runs after the pass, so the reparent never lands mid-measure.
            CheckRows.LayoutUpdated += (_, _) =>
            {
                if (CheckRows.SelectedItem is not null && (CheckDetail.Parent as Border)?.TemplatedParent != CheckRows.ItemContainerGenerator.ContainerFromItem(CheckRows.SelectedItem))
                    MoveCheckDetailToSelectedRow();
            };
        }
        MoveCheckDetailToSelectedRow();
    }

    private bool MoveCheckDetailToSelectedRow()
    {
        if (CheckRows.SelectedItem is null) return true;
        if (CheckRows.ItemContainerGenerator.ContainerFromItem(CheckRows.SelectedItem) is not ListBoxItem row)
        {
            // A refreshed view throws away the rows; generate them now so the options never sit in a dead one.
            CheckRows.UpdateLayout();
            if (CheckRows.ItemContainerGenerator.ContainerFromItem(CheckRows.SelectedItem) is not ListBoxItem fresh) return false;
            row = fresh;
        }
        row.ApplyTemplate();
        if (row.Template.FindName("RowDetailHost", row) is not Border host) return false;
        if (ReferenceEquals(host.Child, CheckDetail)) return true;
        switch (CheckDetail.Parent)
        {
            case Border previous: previous.Child = null; break;
            case Panel panel: panel.Children.Remove(CheckDetail); break;
        }
        host.Child = CheckDetail;
        return true;
    }
}
