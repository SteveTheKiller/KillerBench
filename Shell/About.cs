using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using KillerBench.Features;
using KillerBench.Features.About;

namespace KillerBench;

// The About overlay's window half: opening and closing it, the click handling, and the IAboutHost
// implementation that maps the controller's values onto the named XAML elements. All the About
// behavior lives in Features\About\AboutController.cs, and the values it reports come from
// Services\AppInfo.cs, Services\CodeSignature.cs and Services\UpdateService.cs. The update half of
// the host is Shell\Updates.cs.
public partial class MainWindow : IAboutHost
{
    private readonly AboutController about;

    private async void OnAbout(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        await ShowAboutAsync();
    }

    private Task ShowAboutAsync()
    {
        if (AboutOverlay.Visibility == Visibility.Visible) { CloseAbout(); return Task.CompletedTask; }
        ShortcutsOverlay.Visibility = Visibility.Collapsed;
        RefreshAgentRepairSettings();
        about.Show();
        return Task.CompletedTask;
    }

    // ---- IShellServices / IAboutHost ----

    Window IShellServices.Window => this;
    string IShellServices.Loc(string key) => TryFindResource(key) as string ?? key;
    void IShellServices.SetStatus(string text) => Report(text);

    string IAboutHost.Version     { set => AboutVersionBlock.Text = value; }
    string IAboutHost.ReleaseDate { set => AboutReleaseDateBlock.Text = value; }
    string IAboutHost.Publisher   { set => AboutPublisherBlock.Text = value; }
    string IAboutHost.Alias       { set => AboutAkaRun.Text = value; }
    string IAboutHost.Thumbprint  { set => AboutThumbprintBlock.Text = value; }
    string IAboutHost.Sha256      { set => AboutSha256Block.Text = value; }
    string IAboutHost.UpdateText  { set => AboutUpdateText.Text = value; }

    bool IAboutHost.AliasVisible
    {
        set => AboutAkaBlock.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    bool IAboutHost.UpdateVisible
    {
        set => AboutUpdateButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    bool IAboutHost.UpdateEnabled { set => AboutUpdateButton.IsEnabled = value; }

    bool IAboutHost.StartupUpdateCheck { set => AboutStartupUpdateCheck.IsChecked = value; }

    void IAboutHost.ShowCard()
    {
        AboutOverlay.Opacity = 1;
        AboutOverlay.Visibility = Visibility.Visible;
    }

    // ---- Handlers ----

    private void OnAboutBackdrop(object sender, MouseButtonEventArgs e) => CloseAbout();
    private void OnAboutCard(object sender, MouseButtonEventArgs e) => e.Handled = true;
    private void OnAboutClose(object sender, RoutedEventArgs e) => CloseAbout();
    private void CloseAbout() => AboutOverlay.Visibility = Visibility.Collapsed;
    private void OnAboutWebsite(object sender, MouseButtonEventArgs e) { OpenUrl("https://killerbench.net"); e.Handled = true; }
    private void OnAboutRelease(object sender, MouseButtonEventArgs e) { about.OpenReleaseNotes(); e.Handled = true; }
    private void OnAboutLink(object sender, RequestNavigateEventArgs e) { OpenUrl(e.Uri.AbsoluteUri); e.Handled = true; }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
