using System.Windows.Controls;

namespace KillerBench;

public partial class MainWindow
{
    private void OnProposedComputerNameChanged(object sender, TextChangedEventArgs e)
    {
        ComputerRenameConsent?.IsChecked = false;
    }
}
