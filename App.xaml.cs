using System.Windows;

namespace KillerBench;

public partial class App : Application
{
    public static bool DemoMode { get; private set; }
    public static bool ScreenshotMode { get; private set; }

    public static bool IsDemoArgument(string value) =>
        string.Equals(value, "--demo", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/demo", StringComparison.OrdinalIgnoreCase);

    public static bool IsScreenshotArgument(string value) =>
        string.Equals(value, "--demo-screenshot", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/demo-screenshot", StringComparison.OrdinalIgnoreCase);

    protected override void OnStartup(StartupEventArgs e)
    {
        KillerFamily.Accessibility.AccessibleControls.Initialize();
        if (TryRunInstallerCommand(e)) return;
        ScreenshotMode = e.Args.Any(IsScreenshotArgument);
        DemoMode = ScreenshotMode || e.Args.Any(IsDemoArgument);
        WindowsEngine.Register();
        Services.Preferences.Load();
        Services.ThemeManager.GetSetting = Services.Preferences.Get;
        Services.ThemeManager.SetSetting = Services.Preferences.Set;
        Services.ThemeManager.Initialize();
        Services.FilmGrain.Initialize();
        Services.LocaleManager.GetSetting = Services.Preferences.Get;
        Services.LocaleManager.SetSetting = Services.Preferences.Set;
        Services.LocaleManager.Initialize();
        if (ScreenshotMode)
        {
            Services.LocaleManager.ApplyTemporary(Services.Locale.EnUS);
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        }
        base.OnStartup(e);
        // Every window (dialogs, pickers) takes the family UI face from the active theme unless it sets
        // its own, so no dialog falls back to the system Segoe UI.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((s, _) =>
        {
            if (s is Window w && DependencyPropertyHelper.GetValueSource(w, System.Windows.Controls.Control.FontFamilyProperty).BaseValueSource == BaseValueSource.Default)
                w.SetResourceReference(System.Windows.Controls.Control.FontFamilyProperty, "UiFont");
        }));
        if (TryRunUninstall(e)) return;
        // The main window is created here instead of through StartupUri, so the installer and
        // uninstall hand-offs can exit without one.
        new MainWindow().Show();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Services.Preferences.Save();
        base.OnExit(e);
    }
}
