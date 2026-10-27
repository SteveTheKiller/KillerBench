using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace KillerBench.Services
{
    /// <summary>
    /// KillerBench's side of the family theme engine. ThemeManager.cs is the KillerUI template and
    /// is never edited here; everything particular to KillerBench comes in through its hooks:
    /// the saved settings, the Dark/Yellow first run, and the values in Themes/App.
    /// </summary>
    public static class AppTheme
    {
        /// <summary>Call once before ThemeManager.Initialize, in the app and in every check harness.</summary>
        public static void Configure()
        {
            ThemeManager.GetSetting = Get;
            ThemeManager.SetSetting = Set;
            ThemeManager.SetDefaults(Theme.Dark, dark: Accent.Yellow, light: Accent.Green, black: Accent.Green, se98: Accent.Green);
            ThemeManager.AppTokens = AppTokens;
            ThemeManager.AppDerivedTokens = AppDerivedTokens;
        }

        // KillerBench saved 98SE as "Retro98" and its accent as "RetroAccent" before it took the
        // family engine, which writes "98SE" and "98SEAccent". Older settings read through.
        private static string? Get(string key)
        {
            string? value = Preferences.Get(key);
            if (key == "Theme" && value == "Retro98") return "98SE";
            if (key == "98SEAccent" && value == null) return Preferences.Get("RetroAccent");
            // KillerBench saved Czech and Italian as "Cs" and "It" before the family Locale names.
            if (key == "Locale") return value switch { "Cs" => "CsCZ", "It" => "ItIT", _ => value };
            return value;
        }

        // The update check choice is saved at once, as it was before the shared About controller
        // read it through these hooks; everything else is saved when the app closes.
        private static void Set(string key, string value)
        {
            Preferences.Set(key, value);
            if (key == Features.About.AboutController.StartupCheckSetting) Preferences.Save();
        }

        private static string FileName(Theme theme) => theme == Theme.SE98 ? "98SE" : theme.ToString();

        // Before the family contract: KillerBench's per-theme values, then its accent values, then
        // its defaults for whatever a theme left unsaid.
        private static void AppTokens(ResourceDictionary d, Theme theme, Accent accent)
        {
            Merge(d, $"Themes/App/{FileName(theme)}.xaml", overwrite: true);
            if (ThemeManager.SupportsAccents(theme))
                Merge(d, $"Themes/App/Accents/{FileName(theme)}/{accent}.xaml", overwrite: true);
            Merge(d, "Themes/App/Defaults.xaml", overwrite: false);
        }

        // After the family contract: tokens KillerBench's own views build from family ones.
        private static void AppDerivedTokens(ResourceDictionary d, Theme theme, Accent accent)
        {
            // Report cards and metric tiles. 98SE draws them as white client areas inside its gray pane.
            ThemeManager.Mirror(d, "ReportCardBrush", "SurfaceBrush");
            ThemeManager.Mirror(d, "ReportMetricBrush", "PaneBrush");
            ApplyConsoleTokens(d);
        }

        // The raw output console: a smoked, nearly opaque surface under accent text that is lifted
        // or deepened only as far as 4.5:1 needs, with a soft accent border and its own shadow.
        private static void ApplyConsoleTokens(ResourceDictionary d)
        {
            var background = BrushColor(d, "BackgroundBrush", Colors.Black);
            var primary = BrushColor(d, "PrimaryBrush", Colors.White);
            var smoke = Mix(background, Colors.Black, 0.72);
            var text = EnsureContrast(primary, smoke, 4.5);
            var borderColor = Mix(primary, smoke, 0.52);
            d["ConsoleSurfaceBrush"] = Frozen(Color.FromArgb(235, smoke.R, smoke.G, smoke.B));
            d["ConsoleTextBrush"] = Frozen(text);
            d["ConsoleBorderBrush"] = Frozen(Color.FromArgb(170, borderColor.R, borderColor.G, borderColor.B));
            var shadow = new DropShadowEffect { Color = Colors.Black, BlurRadius = 18, ShadowDepth = 5, Direction = 270, Opacity = 0.42 };
            shadow.Freeze();
            d["ConsoleShadowEffect"] = shadow;
        }

        private static Color BrushColor(ResourceDictionary d, string key, Color fallback) =>
            d.Contains(key) && d[key] is SolidColorBrush brush ? brush.Color : fallback;

        private static Color Mix(Color source, Color destination, double amount) =>
            Color.FromRgb(
                (byte)Math.Round(source.R + (destination.R - source.R) * amount),
                (byte)Math.Round(source.G + (destination.G - source.G) * amount),
                (byte)Math.Round(source.B + (destination.B - source.B) * amount));

        private static Color EnsureContrast(Color foreground, Color background, double minimum)
        {
            var result = foreground;
            var destination = ColorContrast.Luminance(background) < 0.5 ? Colors.White : Colors.Black;
            for (int attempt = 0; attempt < 12 && ColorContrast.Contrast(result, background) < minimum; attempt++)
                result = Mix(result, destination, 0.18);
            return result;
        }

        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static void Merge(ResourceDictionary d, string path, bool overwrite)
        {
            ResourceDictionary source;
            try { source = new ResourceDictionary { Source = new Uri($"pack://application:,,,/KillerBench;component/{path}") }; }
            catch (IOException) { return; }   // no file for this theme or accent
            foreach (object key in source.Keys)
                if (overwrite || !d.Contains(key)) d[key] = source[key];
        }
    }
}
