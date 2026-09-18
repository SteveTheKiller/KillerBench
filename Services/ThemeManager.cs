using System;
using System.Windows;


namespace KillerBench.Services
{
    public enum Theme { Dark, Light, Black, Blood, Greed, Cyanotic, Retro98, Ectoplasm, Decay, Malaise, Sepulchre, Delirium, Mourning }

    public enum Accent { Green, Red, Blue, Purple, Orange, Teal }


    public static class ThemeManager
    {
        public static Func<string, string?> GetSetting { get; set; } = _ => null;
        public static Action<string, string> SetSetting { get; set; } = (_, _) => { };

        private static Theme _current = Theme.Dark;
        private static Accent _darkAccent  = Accent.Orange;
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
                }
                catch { /* overlay file not present - base theme stands */ }
            }

            ApplyDerivedTokens(newDict);

            var merged = Application.Current.Resources.MergedDictionaries;
            if (merged.Count > 0) merged[0] = newDict;
            else merged.Add(newDict);
        }

        private static void ApplyDerivedTokens(ResourceDictionary d)
        {
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

            if (!d.Contains("WindowEdgeBrush") && d.Contains("AppBorderBrush"))
                d["WindowEdgeBrush"] = d["AppBorderBrush"];
            SetIfAbsent(d, "WindowEdgeThickness", new Thickness(1));

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

            if (!d.Contains("BgFlyout") && d.Contains("MenuBackgroundBrush"))
                d["BgFlyout"] = d["MenuBackgroundBrush"];
            if (!d.Contains("AboutPanelBrush") && d.Contains("MenuBackgroundBrush"))
                d["AboutPanelBrush"] = d["MenuBackgroundBrush"];
            if (d.Contains("HeaderLineBrush"))
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
            SetIfAbsent(d, "BarShadowOpacity", 0.38);
            SetIfAbsent(d, "FlyoutShadowOpacity", 0.55);
            SetIfAbsent(d, "WindowFrameThickness", new Thickness(0));
        }

        private static System.Windows.Media.SolidColorBrush Transparent() =>
            new(System.Windows.Media.Colors.Transparent);

        private static System.Windows.Media.SolidColorBrush Hex(string hex) =>
            new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }
}
