using System.Windows;

namespace KillerBench;

public partial class App : Application
{
    public static bool DemoMode { get; private set; }

    public static bool IsDemoArgument(string value) =>
        string.Equals(value, "--demo", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/demo", StringComparison.OrdinalIgnoreCase);

    protected override void OnStartup(StartupEventArgs e)
    {
        DemoMode = e.Args.Any(IsDemoArgument);
        KillerBench.Engine.WindowsEngine.Register();
        Services.Preferences.Load();
        Services.ThemeManager.GetSetting = Services.Preferences.Get;
        Services.ThemeManager.SetSetting = Services.Preferences.Set;
        Services.ThemeManager.Initialize();
        Services.LocaleManager.GetSetting = Services.Preferences.Get;
        Services.LocaleManager.SetSetting = Services.Preferences.Set;
        Services.LocaleManager.Initialize();
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Services.Preferences.Save();
        base.OnExit(e);
    }
}
