using System;
using System.Windows;


namespace KillerBench.Services
{
    public enum Theme { Dark, Light, Black, Blood, Greed, Cyanotic, Retro98, Ectoplasm, Decay, Malaise, Sepulchre, Delirium, Mourning }

    public enum Accent { Green, Red, Blue, Purple, Orange, Teal, Yellow, Magenta }


    public static class ThemeManager
    {
        public static Func<string, string?> GetSetting { get; set; } = _ => null;
        public static Action<string, string> SetSetting { get; set; } = (_, _) => { };

        private static Theme _current = Theme.Dark;
        private static Accent _darkAccent  = Accent.Yellow;
        private static Accent _lightAccent = Accent.Green;
        private static Accent _blackAccent = Accent.Green;
        private static Accent _retroAccent = Accent.Green;

        public static Theme Current => _current;
        public static Accent AccentChoiceFor(Theme t) => AccentFor(t);

        private static Accent AccentFor(Theme t) =>
            t == Theme.Retro98 ? _retroAccent : t == Theme.Light ? _lightAccent : t == Theme.Black ? _blackAccent : _darkAccent;

        private static bool HasAccents(Theme t) =>
            t == Theme.Dark || t == Theme.Light || t == Theme.Black || t == Theme.Retro98;

        public static event Action? ThemeChanged;

        public static void Initialize()
        {
            _current     = Enum.TryParse<Theme>(GetSetting("Theme"), out var t) && Enum.IsDefined(t) ? t : _current;
            _darkAccent  = Enum.TryParse<Accent>(GetSetting("DarkAccent"),  out var da) ? da : _darkAccent;
            _lightAccent = Enum.TryParse<Accent>(GetSetting("LightAccent"), out var la) ? la : _lightAccent;
            _blackAccent = Enum.TryParse<Accent>(GetSetting("BlackAccent"), out var ba) ? ba : _blackAccent;
            _retroAccent = Enum.TryParse<Accent>(GetSetting("RetroAccent"), out var ra) ? ra : _retroAccent;
            LoadDict(_current);
        }

        public static void Apply(Theme theme)
        {
            _current = theme;
            SetSetting("Theme", theme.ToString());
            LoadDict(theme);
            ThemeChanged?.Invoke();
        }

        public static void ApplyAccent(Theme family, Accent accent)
        {
            if      (family == Theme.Retro98) { _retroAccent = accent; SetSetting("RetroAccent", accent.ToString()); }
            else if (family == Theme.Light) { _lightAccent = accent; SetSetting("LightAccent", accent.ToString()); }
            else if (family == Theme.Black) { _blackAccent = accent; SetSetting("BlackAccent", accent.ToString()); }
            else                            { _darkAccent  = accent; SetSetting("DarkAccent",  accent.ToString()); }

            if (_current == family)
            {
                LoadDict(_current);
                ThemeChanged?.Invoke();
            }
        }

        private static void SetIfAbsent(ResourceDictionary dict, string key, object value)
        {
            if (!dict.Contains(key)) dict[key] = value;
        }

        // Filled buttons and the outline button's hover fill carry the family's soft vertical
        // gradient (the KillerNotes treatment): 12% lighter at the top, 16% darker at the bottom.
        // 98SE, and any value that is already a gradient, stay exactly as they are.
        private static object ButtonFill(object brush, bool flat)
        {
            if (flat || brush is not System.Windows.Media.SolidColorBrush solid) return brush;
            var c = solid.Color;
            var top = System.Windows.Media.Color.FromArgb(c.A, (byte)(c.R + (255 - c.R) * 0.12),
                (byte)(c.G + (255 - c.G) * 0.12), (byte)(c.B + (255 - c.B) * 0.12));
            var bottom = System.Windows.Media.Color.FromArgb(c.A, (byte)(c.R * 0.84), (byte)(c.G * 0.84), (byte)(c.B * 0.84));
            var gradient = new System.Windows.Media.LinearGradientBrush(top, bottom, 90);
            gradient.Freeze();
            return gradient;
        }

        private static void LoadDict(Theme theme)
        {
            var uri = new Uri($"pack://application:,,,/KillerBench;component/Themes/{(theme == Theme.Retro98 ? "98SE" : theme.ToString())}.xaml");
            var newDict = new ResourceDictionary { Source = uri };

            var accent = AccentFor(theme);
            if (HasAccents(theme) && accent != Accent.Green)
            {
                string family = theme == Theme.Retro98 ? "98SE" : theme == Theme.Light ? "Light" : theme == Theme.Black ? "Black" : "Dark";
                try
                {
                    var accentDict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/KillerBench;component/Themes/Accents/{family}/{accent}.xaml")
                    };
                    foreach (object key in accentDict.Keys)
                        newDict[key] = accentDict[key];
                    if (!accentDict.Contains("AccentLogo") && accentDict.Contains("HeaderLineBrush"))
                        newDict["AccentLogo"] = accentDict["HeaderLineBrush"];
                }
                catch { /* overlay file not present - base theme stands */ }
            }

            ApplyDerivedTokens(newDict);
            newDict["PrimaryFillBrush"] = ButtonFill(newDict["PrimaryBrush"], theme == Theme.Retro98);
            newDict["OutlineHoverFillBrush"] = ButtonFill(newDict["OutlineBtnBrush"], theme == Theme.Retro98);

            var merged = Application.Current.Resources.MergedDictionaries;
            if (merged.Count > 0) merged[0] = newDict;
            else merged.Add(newDict);
        }

        private static void ApplyDerivedTokens(ResourceDictionary d)
        {
            if (!d.Contains("MenuSeparatorBrush") && d.Contains("MenuBorderBrush"))
                d["MenuSeparatorBrush"] = d["MenuBorderBrush"];

            SetIfAbsent(d, "BevelLightThickness", new Thickness(0));
            SetIfAbsent(d, "BevelDarkThickness", new Thickness(0));
            SetIfAbsent(d, "BevelLightBrush", Transparent());
            SetIfAbsent(d, "BevelDarkBrush", Transparent());

            SetIfAbsent(d, "PaneBevelDarkBrush", Transparent());
            SetIfAbsent(d, "PaneBevelLightBrush", Transparent());

            SetIfAbsent(d, "FooterBevelDarkBrush", Transparent());
            SetIfAbsent(d, "FooterBevelLightBrush", Transparent());
            SetIfAbsent(d, "FooterCellLightThickness", new Thickness(0));
            SetIfAbsent(d, "FooterCellDarkThickness", new Thickness(0));
            SetIfAbsent(d, "FooterCellMargin", new Thickness(0));
            SetIfAbsent(d, "FooterCellPadding", new Thickness(0));
            SetIfAbsent(d, "FooterPadding", new Thickness(16, 0, 16, 0));

            SetIfAbsent(d, "SidebarPaneBrush", Transparent());

            if (!d.Contains("OutlineRestBrush") && d.Contains("OutlineBtnBrush"))
                d["OutlineRestBrush"] = d["OutlineBtnBrush"];
            if (!d.Contains("ButtonEdgeBrush") && d.Contains("CardBorderBrush"))
                d["ButtonEdgeBrush"] = d["CardBorderBrush"];
            if (!d.Contains("SurfaceHoverBrush") && d.Contains("RowHoverBrush"))
                d["SurfaceHoverBrush"] = d["RowHoverBrush"];
            if (!d.Contains("RadioHoverFgBrush") && d.Contains("PrimaryBrush"))
                d["RadioHoverFgBrush"] = d["PrimaryBrush"];
            if (!d.Contains("ComboHighlightTextBrush"))
                d["ComboHighlightTextBrush"] = d["PrimaryBrush"];
            if (!d.Contains("OutlineHoverTextBrush"))
                d["OutlineHoverTextBrush"] = d["OnPrimaryBrush"];

            if (!d.Contains("WindowEdgeBrush") && d.Contains("AppBorderBrush"))
                d["WindowEdgeBrush"] = d["AppBorderBrush"];
            SetIfAbsent(d, "WindowEdgeThickness", new Thickness(1));

            if (!d.Contains("DialogTitleBarBrush"))
                d["DialogTitleBarBrush"] = d.Contains("UseDialogCaption") && d.Contains("TitleBarBrush")
                    ? d["TitleBarBrush"]
                    : Transparent();
            SetIfAbsent(d, "DialogTitleBarHeight", 28.0);
            SetIfAbsent(d, "DialogHaloMargin", new Thickness(10));

            SetIfAbsent(d, "CaptionButtonBrush", Transparent());
            SetIfAbsent(d, "CaptionButtonMargin", new Thickness(0));
            SetIfAbsent(d, "CaptionButtonsMargin", new Thickness(0));
            SetIfAbsent(d, "CaptionCloseGap", new Thickness(0));

            SetIfAbsent(d, "TitleBarHeight", 36.0);
            SetIfAbsent(d, "CaptionButtonWidth", 44.0);
            SetIfAbsent(d, "CaptionButtonHeight", 36.0);
            SetIfAbsent(d, "TitleBarPadding", new Thickness(14, 0, 0, 0));
            SetIfAbsent(d, "TitleIconSize", 25.0);
            SetIfAbsent(d, "TitleIconMargin", new Thickness(0, 0, 7, 0));
            SetIfAbsent(d, "TitleWordmarkSize", 17.0);
            SetIfAbsent(d, "TitleWordmarkBoldSize", 22.0);
            SetIfAbsent(d, "WordmarkEmbossOpacity", 0.0);
            SetIfAbsent(d, "WordmarkAccentShadowOpacity", 0.0);
            SetIfAbsent(d, "ChromeFontFamily", new System.Windows.Media.FontFamily("Segoe UI"));

            bool flatCaption = d.Contains("UseDialogCaption");
            d["WordmarkVisibility"] = flatCaption ? Visibility.Collapsed : Visibility.Visible;
            d["PlainTitleVisibility"] = flatCaption ? Visibility.Visible : Visibility.Collapsed;
            d["CaptionGlyphWeight"] = flatCaption
                ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
            if (!d.Contains("CaptionHoverBrush"))
                d["CaptionHoverBrush"] = flatCaption || !d.Contains("RowHoverBrush")
                    ? Transparent() : d["RowHoverBrush"];
            if (!d.Contains("CaptionGlyphBrush") && d.Contains("ChromeTextBrush"))
                d["CaptionGlyphBrush"] = d["ChromeTextBrush"];
            SetIfAbsent(d, "CaptionCloseBrush", Hex("#e04444"));
            SetIfAbsent(d, "CaptionCloseHoverBrush", Hex("#e04444"));
            SetIfAbsent(d, "CaptionCloseHoverFgBrush",
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White));

            if (!d.Contains("FooterBackgroundBrush"))
                d["FooterBackgroundBrush"] = d["BackgroundBrush"];
            if (!d.Contains("BgFlyout") && d.Contains("MenuBackgroundBrush"))
                d["BgFlyout"] = d["MenuBackgroundBrush"];
            if (!d.Contains("AboutPanelBrush") && d.Contains("MenuBackgroundBrush"))
                d["AboutPanelBrush"] = d["MenuBackgroundBrush"];
            if (!d.Contains("AccentLogo") && d.Contains("HeaderLineBrush"))
                d["AccentLogo"] = d["HeaderLineBrush"];
            if (!d.Contains("SortButtonBrush") && d.Contains("PaneBrush"))
                d["SortButtonBrush"] = d["PaneBrush"];
            if (!d.Contains("SliderTrack") && d.Contains("PaneBrush"))
                d["SliderTrack"] = d["PaneBrush"];
            if (!d.Contains("TreeLineBrush") && d.Contains("PaneBorderBrush"))
                d["TreeLineBrush"] = d["PaneBorderBrush"];
            if (!d.Contains("TextStroke"))
                d["TextStroke"] = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Colors.Black,
                    BlurRadius = 3,
                    ShadowDepth = 0.8,
                    Direction = 270,
                    Opacity = 0.7,
                };

            d["AboutCaptionHeight"] = flatCaption ? d["DialogTitleBarHeight"] : 0.0;
            d["AboutPanelBorderThickness"] = flatCaption ? new Thickness(0) : new Thickness(1);
            d["AboutCloseWidth"] = flatCaption ? d["CaptionButtonWidth"] : 28.0;
            d["AboutCloseHeight"] = flatCaption ? d["CaptionButtonHeight"] : 26.0;
            double capH = d["AboutCaptionHeight"] is double ch ? ch : 0.0;
            double btnH = d["AboutCloseHeight"] is double bh ? bh : 0.0;
            d["AboutCloseMargin"] = flatCaption
                ? new Thickness(0, System.Math.Max(0, (capH - btnH) / 2.0), 4, 0)
                : new Thickness(0, 6, 6, 0);

            SetIfAbsent(d, "PaneShadowOpacity", 0.60);
            if (d["PaneShadowOpacity"] is double shadowOpacity && shadowOpacity > 0)
            {
                var shadow = new System.Windows.Media.Effects.DropShadowEffect
                { Color = System.Windows.Media.Colors.Black, BlurRadius = 16, ShadowDepth = 5, Direction = 270, Opacity = shadowOpacity };
                shadow.Freeze();
                d["PaneShadowEffect"] = shadow;
            }
            else d["PaneShadowEffect"] = null;

            ApplyConsoleTokens(d);
            SetIfAbsent(d, "BarShadowOpacity", 0.38);
            SetIfAbsent(d, "FlyoutShadowOpacity", 0.55);
            SetIfAbsent(d, "WindowFrameThickness", new Thickness(0));
        }

        private static System.Windows.Media.SolidColorBrush Transparent() =>
            new(System.Windows.Media.Colors.Transparent);

        private static void ApplyConsoleTokens(ResourceDictionary d)
        {
            var background = BrushColor(d, "BackgroundBrush", System.Windows.Media.Colors.Black);
            var primary = BrushColor(d, "PrimaryBrush", System.Windows.Media.Colors.White);
            var smoke = Mix(background, System.Windows.Media.Colors.Black, 0.72);
            var text = EnsureContrast(primary, smoke, 4.5);
            var borderColor = Mix(primary, smoke, 0.52);
            var surface = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(235, smoke.R, smoke.G, smoke.B));
            var foreground = new System.Windows.Media.SolidColorBrush(text);
            var border = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(170, borderColor.R, borderColor.G, borderColor.B));
            surface.Freeze();
            foreground.Freeze();
            border.Freeze();
            d["ConsoleSurfaceBrush"] = surface;
            d["ConsoleTextBrush"] = foreground;
            d["ConsoleBorderBrush"] = border;
            var shadow = new System.Windows.Media.Effects.DropShadowEffect
            { Color = System.Windows.Media.Colors.Black, BlurRadius = 18, ShadowDepth = 5, Direction = 270, Opacity = 0.42 };
            shadow.Freeze();
            d["ConsoleShadowEffect"] = shadow;
        }

        private static System.Windows.Media.Color BrushColor(ResourceDictionary d, string key, System.Windows.Media.Color fallback) =>
            d[key] is System.Windows.Media.SolidColorBrush brush ? brush.Color : fallback;

        private static System.Windows.Media.Color Mix(System.Windows.Media.Color source, System.Windows.Media.Color destination, double amount) =>
            System.Windows.Media.Color.FromRgb(
                (byte)Math.Round(source.R + (destination.R - source.R) * amount),
                (byte)Math.Round(source.G + (destination.G - source.G) * amount),
                (byte)Math.Round(source.B + (destination.B - source.B) * amount));

        private static System.Windows.Media.Color EnsureContrast(System.Windows.Media.Color foreground, System.Windows.Media.Color background, double minimum)
        {
            var result = foreground;
            var destination = Luminance(background) < 0.5 ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black;
            for (int attempt = 0; attempt < 12 && ContrastRatio(result, background) < minimum; attempt++)
                result = Mix(result, destination, 0.18);
            return result;
        }

        private static double ContrastRatio(System.Windows.Media.Color first, System.Windows.Media.Color second)
        {
            double firstLuminance = Luminance(first);
            double secondLuminance = Luminance(second);
            return (Math.Max(firstLuminance, secondLuminance) + 0.05) / (Math.Min(firstLuminance, secondLuminance) + 0.05);
        }

        private static double Luminance(System.Windows.Media.Color color)
        {
            static double Linear(byte channel)
            {
                double value = channel / 255.0;
                return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }

        private static System.Windows.Media.SolidColorBrush Hex(string hex) =>
            new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }
}
