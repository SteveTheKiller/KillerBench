using System.Windows;
using System.Windows.Input;
using KillerBench.Features;
using KillerBench.Services;

namespace KillerBench.Shell
{
    /// <summary>
    /// APP-OWNED: KillerBench's side of the shared About core (Shell/About.cs). It holds the shell
    /// services the controller reports through, the wordmark link, the F12 toggle, and the agent
    /// repair settings refresh that runs each time the card opens.
    /// </summary>
    public partial class MainWindow
    {
        Window IShellServices.Window => this;
        string IShellServices.Loc(string key) => TryFindResource(key) as string ?? key;
        void IShellServices.SetStatus(string text) => Report(text);

        // The card shows the current agent repair settings, so they are read again before it fades in.
        partial void ShowingCard(ref bool handled) => RefreshAgentRepairSettings();

        /// <summary>F12 opens the card, or closes it when it is already open.</summary>
        private Task ShowAboutAsync()
        {
            if (AboutOverlay.Visibility == Visibility.Visible) { FadeOverlayOut(AboutOverlay); return Task.CompletedTask; }
            ShortcutsOverlay.Visibility = Visibility.Collapsed;
            ShowAboutOverlay();
            return Task.CompletedTask;
        }

        private void OnAboutWebsite(object sender, MouseButtonEventArgs e)
        {
            WebLink.Open("https://killerbench.net");
            e.Handled = true;
        }
    }
}
