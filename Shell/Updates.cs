using System.Windows;
using KillerBench.Features.About;

namespace KillerBench;

// The About card's update half of the host: when the startup check may run, the repair hold, and the
// two update controls. The check, the prompts and the install live in Features\About\AboutController.cs
// and Services\UpdateService.cs, the same as in the rest of the family.
public partial class MainWindow : IAboutRepairHold
{
    /// <summary>After the window has painted (MainWindow.xaml.cs). Skipped in demo and screenshot
    /// modes and under the checks, which host the window in their own Application.</summary>
    private void CheckForUpdatesOnStartup()
    {
        if (Application.Current is App && !App.DemoMode && !App.ScreenshotMode) about.CheckOnStartup();
    }

    bool IAboutRepairHold.HoldUpdateForRepair()
    {
        if (!repairRunning) return false;
        Report("The update waits until the running repair finishes. Open About to update afterward.");
        return true;
    }

    // Click, not Checked/Unchecked, so only a person's click saves; the card's own sync never writes.
    private void OnAboutStartupUpdateCheck(object sender, RoutedEventArgs e)
        => about.SetStartupCheck(AboutStartupUpdateCheck.IsChecked == true);

    private void OnAboutUpdate(object sender, RoutedEventArgs e) => about.Update();
}
