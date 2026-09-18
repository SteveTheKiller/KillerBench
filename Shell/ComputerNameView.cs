using System.Windows.Controls;

namespace KillerBench;

public partial class MainWindow
{
    private void OnProposedComputerNameChanged(object sender, TextChangedEventArgs e)
    {
        if (ComputerRenameConsent is not null) ComputerRenameConsent.IsChecked = false;
    }
}
