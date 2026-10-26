using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

/// <summary>
/// Writes every resolved theme resource for every theme and accent, one "theme/accent key value"
/// line each, so a theme refactor can be compared value by value before and after. Not a suite:
/// it produces a file and passes or fails nothing. It builds each palette with the theme engine's
/// CreateDictionary (or LoadDict on an engine without one) inside a bare Application, so no window
/// opens. The path comes from KILLERBENCH_THEME_DUMP.
/// </summary>
internal static class ThemeDumpChecks
{
    private const BindingFlags Any = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    internal static int Run()
    {
        string path = Environment.GetEnvironmentVariable("KILLERBENCH_THEME_DUMP")
            ?? throw new InvalidOperationException("Set KILLERBENCH_THEME_DUMP to the output file.");
        var assembly = typeof(KillerBench.App).Assembly;
        // Application-relative pack URIs in the theme engine resolve against the app, not this exe.
        typeof(Application).GetField("_resourceAssembly", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, assembly);
        var application = new Application();
        foreach (string source in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
        {
            if (!ResourceExists(source)) continue;
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/" + source, UriKind.Relative) });
        }
        // Engines with app hooks are configured exactly as the app configures them.
        assembly.GetType("KillerBench.Services.AppTheme")?.GetMethod("Configure", Any)?.Invoke(null, null);

        var manager = assembly.GetType("KillerBench.Services.ThemeManager", true)!;
        var themeType = assembly.GetType("KillerBench.Services.Theme", true)!;
        var accentType = assembly.GetType("KillerBench.Services.Accent", true)!;
        var create = manager.GetMethod("CreateDictionary", Any);
        var load = manager.GetMethods(Any).First(m => m.Name == "LoadDict");
        var lines = new List<string>();
        foreach (var theme in Enum.GetValues(themeType))
            foreach (var accent in Enum.GetValues(accentType))
            {
                manager.GetField("_current", Any)!.SetValue(null, theme);
                if (create != null)
                {
                    var built = (ResourceDictionary)create.Invoke(null, new[] { theme, accent })!;
                    application.Resources.MergedDictionaries[0] = built;
                }
                else
                {
                    // The engine before KillerUI reads the accent from its per-family fields.
                    foreach (string field in new[] { "_darkAccent", "_lightAccent", "_blackAccent", "_retroAccent", "_se98Accent" })
                        manager.GetField(field, Any)?.SetValue(null, accent);
                    load.Invoke(null, load.GetParameters().Length == 1 ? new[] { theme } : new[] { theme, accent });
                }
                var keys = new SortedSet<string>(StringComparer.Ordinal);
                void Collect(ResourceDictionary d)
                {
                    foreach (var key in d.Keys) if (key is string s) keys.Add(s);
                    foreach (var child in d.MergedDictionaries) Collect(child);
                }
                foreach (var d in application.Resources.MergedDictionaries) Collect(d);
                string name = theme.ToString() is "Retro98" or "SE98" ? "98SE" : theme.ToString()!;
                foreach (string key in keys)
                    lines.Add(name + "/" + accent + " " + key + " " + Describe(application.TryFindResource(key)));
            }
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
        Console.WriteLine("DUMPED: " + lines.Count + " values to " + path);
        return 0;
    }

    private static bool ResourceExists(string source)
    {
        try { return Application.GetResourceStream(new Uri("/KillerBench;component/" + source, UriKind.Relative)) != null; }
        catch (IOException) { return false; }
    }

    private static string Describe(object? value) => value switch
    {
        null => "null",
        SolidColorBrush b => "brush " + b.Color + " o" + F(b.Opacity),
        GradientBrush g => g.GetType().Name + " " + string.Join(",", g.GradientStops.Select(s => s.Color + "@" + F(s.Offset))) + " o" + F(g.Opacity),
        Color c => "color " + c,
        double d => "double " + F(d),
        bool b => "bool " + b,
        Thickness t => "thickness " + t,
        CornerRadius r => "radius " + r,
        DropShadowEffect e => "shadow " + e.Color + " b" + F(e.BlurRadius) + " d" + F(e.ShadowDepth) + " a" + F(e.Direction) + " o" + F(e.Opacity),
        string s => "string " + s.Replace("\n", "\\n"),
        FontFamily f => "font " + f.Source,
        Visibility v => "visibility " + v,
        _ => value.GetType().Name,
    };

    private static string F(double d) => d.ToString("0.####", CultureInfo.InvariantCulture);
}
