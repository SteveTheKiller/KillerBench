using System.Windows;
using KillerBench.Features.About;

namespace KillerBench.Shell;

// The About card's update half of the host: when the startup check may run, the repair hold, and the
// two update controls. The check, the prompts and the install live in Features\About\AboutController.cs
// and Services\UpdateService.cs, the same as in the rest of the family.
public partial class MainWindow : IAboutRepairHold
{
    /// <summary>After the window has painted (MainWindow.xaml.cs). Skipped in demo and screenshot
    /// modes and under the checks, which host the window in their own Application.</summary>
    private void CheckForUpdatesOnStartup()
    {
        if (Application.Current is App && !App.DemoMode && !App.ScreenshotMode) _about.CheckOnStartup();
    }

    bool IAboutRepairHold.HoldUpdateForRepair()
    {
        if (!repairRunning) return false;
        Report(Controls.LocalizedText.Get("Str_Ui_UpdateWaits"));
        return true;
    }

    // Set once the update commits to its handoff. The app closes as soon as the helper is running, so a
    // repair started in those seconds would be cut off mid-command; runs wait until the handoff ends.
    private bool updateHandoffActive;

    void IAboutRepairHold.HoldRunsForUpdate(bool hold) => updateHandoffActive = hold;

    private bool RefuseForUpdate()
    {
        if (!updateHandoffActive) return false;
        Report(Controls.LocalizedText.Get("Str_About_UpdateDetail"));
        return true;
    }
}
