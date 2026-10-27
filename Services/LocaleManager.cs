using System;
using System.Globalization;
using System.Windows;

namespace KillerBench.Services
{
    public enum Locale { EnUS, Es, ZhTW, ZhCN, Bn, TrTR, De, Fr, Ja, Cs, PlPL, HuHU, It, RuRU, KkKZ, ViVN, UkUA, NbNO, PtBR, SlSI }

    public static class LocaleManager
    {
        public static Func<string, string?> GetSetting { get; set; } = _ => null;
        public static Action<string, string> SetSetting { get; set; } = (_, _) => { };

        private static Locale _current = Locale.EnUS;
        public static Locale Current => _current;

        public static void Initialize()
        {
            var saved = GetSetting("Locale");
            if (Enum.TryParse<Locale>(saved, out var locale) && Enum.IsDefined(locale))
            {
                _current = locale;
            }
            else
            {
                _current = LocaleForCulture(CultureInfo.CurrentUICulture);
                SetSetting("Locale", _current.ToString());
            }
            ApplyInternal(_current);
        }

        internal static Locale LocaleForCulture(CultureInfo culture)
        {
            string name = culture.Name;
            string language = culture.TwoLetterISOLanguageName;
            if (language == "zh")
                return name.Contains("Hant", StringComparison.OrdinalIgnoreCase) ||
                       name.EndsWith("-TW", StringComparison.OrdinalIgnoreCase) ||
                       name.EndsWith("-HK", StringComparison.OrdinalIgnoreCase) ||
                       name.EndsWith("-MO", StringComparison.OrdinalIgnoreCase)
                    ? Locale.ZhTW
                    : Locale.ZhCN;

            return language switch
            {
                "bn" => Locale.Bn, "cs" => Locale.Cs, "de" => Locale.De,
                "es" => Locale.Es, "fr" => Locale.Fr, "hu" => Locale.HuHU,
                "it" => Locale.It, "ja" => Locale.Ja, "kk" => Locale.KkKZ,
                "pl" => Locale.PlPL, "ru" => Locale.RuRU, "tr" => Locale.TrTR,
                "vi" => Locale.ViVN,
                "uk" => Locale.UkUA,
                "nb" or "no" or "nn" => Locale.NbNO,
                "pt" => Locale.PtBR,
                "sl" => Locale.SlSI,
                _ => Locale.EnUS,
            };
        }

        public static void Apply(Locale locale)
        {
            _current = locale;
            SetSetting("Locale", locale.ToString());
            ApplyInternal(locale);
        }

        internal static void ApplyTemporary(Locale locale)
        {
            _current = locale;
            ApplyInternal(locale);
        }

        private static void ApplyInternal(Locale locale)
        {
            var merged = Application.Current.Resources.MergedDictionaries;
            // The en-US base is found by its source rather than a fixed slot, because the theme,
            // the family control styles and KillerBench's own styles are merged ahead of it; the
            // chosen locale's overrides sit directly after it.
            int baseIndex = StringsBaseIndex(merged);
            int overrideIndex = baseIndex + 1;

            if (merged.Count > baseIndex)
                merged[baseIndex] = new ResourceDictionary { Source = new Uri("pack://application:,,,/KillerBench;component/Strings/en-US.xaml") };

            Uri? overrideUri = locale switch
            {
                Locale.Es   => new Uri("pack://application:,,,/KillerBench;component/Strings/es.xaml"),
                Locale.Fr   => new Uri("pack://application:,,,/KillerBench;component/Strings/fr-FR.xaml"),
                Locale.ZhTW => new Uri("pack://application:,,,/KillerBench;component/Strings/zh-TW.xaml"),
                Locale.ZhCN => new Uri("pack://application:,,,/KillerBench;component/Strings/zh-CN.xaml"),
                Locale.Bn   => new Uri("pack://application:,,,/KillerBench;component/Strings/bn.xaml"),
                Locale.TrTR => new Uri("pack://application:,,,/KillerBench;component/Strings/tr-TR.xaml"),
                Locale.De   => new Uri("pack://application:,,,/KillerBench;component/Strings/de-DE.xaml"),
                Locale.Ja   => new Uri("pack://application:,,,/KillerBench;component/Strings/ja-JP.xaml"),
                Locale.Cs   => new Uri("pack://application:,,,/KillerBench;component/Strings/cs-CZ.xaml"),
                Locale.PlPL => new Uri("pack://application:,,,/KillerBench;component/Strings/pl-PL.xaml"),
                Locale.HuHU => new Uri("pack://application:,,,/KillerBench;component/Strings/hu-HU.xaml"),
                Locale.It   => new Uri("pack://application:,,,/KillerBench;component/Strings/it-IT.xaml"),
                Locale.RuRU => new Uri("pack://application:,,,/KillerBench;component/Strings/ru-RU.xaml"),
                Locale.KkKZ => new Uri("pack://application:,,,/KillerBench;component/Strings/kk-KZ.xaml"),
                Locale.ViVN => new Uri("pack://application:,,,/KillerBench;component/Strings/vi-VN.xaml"),
                Locale.UkUA => new Uri("pack://application:,,,/KillerBench;component/Strings/uk-UA.xaml"),
                Locale.NbNO => new Uri("pack://application:,,,/KillerBench;component/Strings/nb-NO.xaml"),
                Locale.PtBR => new Uri("pack://application:,,,/KillerBench;component/Strings/pt-BR.xaml"),
                Locale.SlSI => new Uri("pack://application:,,,/KillerBench;component/Strings/sl-SI.xaml"),
                _           => null,   // English: base only
            };

            if (overrideUri is not null)
            {
                try
                {
                    var ov = new ResourceDictionary { Source = overrideUri };
                    if (merged.Count > overrideIndex) merged[overrideIndex] = ov; else merged.Add(ov);
                }
                catch
                {
                    if (merged.Count > overrideIndex) merged.RemoveAt(overrideIndex);
                }
            }
            else if (merged.Count > overrideIndex)
            {
                merged.RemoveAt(overrideIndex);
            }
        }

        /// <summary>Where the English string base is merged. Slot 3 if it cannot be found, which
        /// is where App.xaml places it.</summary>
        private static int StringsBaseIndex(System.Collections.ObjectModel.Collection<ResourceDictionary> merged)
        {
            for (int i = 0; i < merged.Count; i++)
                if (merged[i].Source?.OriginalString.EndsWith("Strings/en-US.xaml", StringComparison.OrdinalIgnoreCase) == true)
                    return i;
            return 3;
        }
    }
}
