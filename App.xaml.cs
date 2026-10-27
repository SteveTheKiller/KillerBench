using System.Windows;

namespace KillerBench;

public partial class App : Application
{
    private bool _commandLine;
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
        KillerFamily.Theming.AccentShadow.Initialize();
        Controls.AltDrag.Register();   // Alt+drag moves any window (Controls/AltDrag.cs)
        Services.AppTheme.Configure();
        var cli = KillerFamily.CommandLine.CommandLineEntry.GetArguments(e.Args, Cli.CommandEngine.CommandNames);
        if (cli != null)
        {
            _commandLine = true;
            _ = RunCommandAsync(cli);
            return;
        }
        if (TryRunInstallerCommand(e)) return;
        ScreenshotMode = e.Args.Any(IsScreenshotArgument);
        DemoMode = ScreenshotMode || e.Args.Any(IsDemoArgument);
        WindowsEngine.Register();
        Services.Preferences.Load();
        KillerFamily.Layout.ShortcutHints.Initialize(Services.Preferences.Get, Services.Preferences.Set);
        Services.ThemeManager.Initialize();
        Services.FilmGrain.Initialize();
        // The family LocaleManager reads and writes "Locale" through the theme settings hooks.
        // Demo screenshots are always English, without touching the saved language.
        if (ScreenshotMode)
        {
            var saved = Services.ThemeManager.GetSetting;
            Services.ThemeManager.GetSetting = key => key == "Locale" ? nameof(Services.Locale.EnUS) : saved(key);
            Services.LocaleManager.Initialize();
            Services.ThemeManager.GetSetting = saved;
        }
        else
        {
            // --lang-file <path>: translator test mode (Services/LocaleManager.cs).
            int langFile = Array.FindIndex(e.Args, a => string.Equals(a, "--lang-file", StringComparison.OrdinalIgnoreCase));
            if (langFile >= 0 && langFile + 1 < e.Args.Length)
                Services.LocaleManager.ExternalFile = System.IO.Path.GetFullPath(e.Args[langFile + 1]);
            Services.LocaleManager.Initialize();
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
        if (!_commandLine) Services.Preferences.Save();
        base.OnExit(e);
    }

    private async Task RunCommandAsync(string[] args)
    {
        int code;
        try
        {
            Services.CliConsole.AttachToParent();
            using var output = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            using var error = new System.IO.StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetOut(output);
            Console.SetError(error);
            code = await Task.Run(() => Cli.CommandEngine.RunAsync(args));
        }
        catch (Exception error)
        {
            try { Console.Error.WriteLine(error.Message); } catch { }
            code = 1;
        }
        Shutdown(code);
    }
}
