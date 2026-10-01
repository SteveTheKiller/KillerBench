using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using KillerBench.Services;

namespace KillerBench;

public partial class MainWindow
{
    private bool syncingTheme;
    private ContextMenu? languageMenu;
    private static readonly (Locale Loc, string Name, string Code)[] Languages =
        [
            (Locale.EnUS, "English",     "en-US"),
            (Locale.Bn,   "বাংলা",        "bn"),
            (Locale.Cs,   "Čeština",     "cs-CZ"),
            (Locale.De,   "Deutsch",     "de-DE"),
            (Locale.Es,   "Español",     "es"),
            (Locale.Fr,   "Français",    "fr-FR"),
            (Locale.HuHU, "Magyar",       "hu-HU"),
            (Locale.It,   "Italiano",     "it-IT"),
            (Locale.Ja,   "日本語",       "ja-JP"),
            (Locale.KkKZ, "Қазақша",     "kk-KZ"),
            (Locale.PlPL, "Polski",       "pl-PL"),
            (Locale.RuRU, "Русский",      "ru-RU"),
            (Locale.TrTR, "Türkçe",      "tr-TR"),
            (Locale.UkUA, "Українська",  "uk-UA"),
            (Locale.ViVN, "Tiếng Việt",  "vi-VN"),
            (Locale.ZhCN, "中文 (简体)",  "zh-CN"),
            (Locale.ZhTW, "中文 (繁體)",  "zh-TW"),
        ];

    private void OnThemes(object sender, RoutedEventArgs e)
    {
        if (ThemeFlyout.IsOpen) { ThemeFlyout.IsOpen = false; return; }
        syncingTheme = true;
        foreach (RadioButton radio in ThemeSubmenu.Children.OfType<RadioButton>())
            radio.IsChecked = Enum.TryParse<Theme>((string)radio.Tag, out var theme) && theme == ThemeManager.Current;
        syncingTheme = false;
        UpdateAccentStrip(false);
        OpenFamilyMenu(ThemeFlyout, ThemeButton);
    }
    private void OnThemeSelected(object sender, RoutedEventArgs e)
    {
        if (syncingTheme || sender is not RadioButton { Tag: string name } || !Enum.TryParse<Theme>(name, out var theme)) return;
        ThemeManager.Apply(theme);
        UpdateAccentStrip(true);
    }
    private void UpdateAccentStrip(bool animate)
    {
        if (AccentStripHost is null) return;
        bool show = ThemeManager.Current is Theme.Dark or Theme.Light or Theme.Black or Theme.Retro98;
        double target = show ? 38 : 0;
        if (animate) AnimateWidth(AccentStripHost, target); else { AccentStripHost.BeginAnimation(WidthProperty, null); AccentStripHost.Width = target; }
        if (!show) return;
        string family = ThemeLabel(ThemeManager.Current);
        var palette = new ResourceDictionary { Source = new Uri($"pack://application:,,,/KillerBench;component/Themes/{family}.xaml") };
        Accent[] order = [Accent.Red, Accent.Orange, Accent.Green, Accent.Teal, Accent.Blue, Accent.Purple];
        Border[] dots = [AccentStripDot0, AccentStripDot1, AccentStripDot2, AccentStripDot3, AccentStripDot4, AccentStripDot5];
        for (int i = 0; i < dots.Length; i++)
        {
            var accent = order[i];
            var colors = accent == Accent.Green ? palette : new ResourceDictionary { Source = new Uri($"pack://application:,,,/KillerBench;component/Themes/Accents/{family}/{accent}.xaml") };
            dots[i].Tag = accent;
            dots[i].ToolTip = accent.ToString();
            dots[i].Background = (Brush)colors["PrimaryBrush"];
            dots[i].BorderBrush = accent == ThemeManager.AccentChoiceFor(ThemeManager.Current) ? (Brush)FindResource("TextBrush") : Brushes.Transparent;
            dots[i].Focusable = true;
            dots[i].KeyDown -= OnAccentKey;
            dots[i].KeyDown += OnAccentKey;
        }
    }
    private void OnAccentKey(object sender, KeyEventArgs e)
    {
        if ((e.Key is Key.Enter or Key.Space) && sender is Border { Tag: Accent accent })
        { ThemeManager.ApplyAccent(ThemeManager.Current, accent); UpdateAccentStrip(false); e.Handled = true; }
    }
    private void OnAccentSelected(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: Accent accent })
        { ThemeManager.ApplyAccent(ThemeManager.Current, accent); UpdateAccentStrip(false); e.Handled = true; }
    }
    private void OpenFamilyMenu(ContextMenu menu, Button anchor)
    {
        menu.PlacementTarget = anchor;
        menu.Placement = PlacementMode.Custom;
        menu.CustomPopupPlacementCallback = (size, _, _) =>
        {
            var point = anchor.TranslatePoint(new Point(), this);
            double x = Math.Max(4 - point.X, Math.Min(anchor.ActualWidth - 22, ActualWidth - point.X - size.Width));
            double y = Math.Max(4 - point.Y, ActualHeight - 24 - point.Y - size.Height + 26);
            return [new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.None)];
        };
        menu.IsOpen = true;
        if (SystemParameters.ClientAreaAnimation)
            menu.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
    }
    private void OnLanguages(object sender, RoutedEventArgs e)
    {
        if (languageMenu?.IsOpen == true) { languageMenu.IsOpen = false; return; }
        languageMenu = new ContextMenu { Template = ThemeFlyout.Template, ItemContainerStyle = (Style)FindResource("PanelMenuItem") };
        var panel = new StackPanel { Margin = new Thickness(12, 10, 14, 10) };
        foreach (var (locale, name, code) in Languages)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(new TextBlock { Text = name, FontFamily = new FontFamily("Segoe UI, Microsoft JhengHei UI, Nirmala UI"), VerticalAlignment = VerticalAlignment.Center });
            var codeBlock = new TextBlock { Text = code, Margin = new Thickness(12,0,0,0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            codeBlock.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
            Grid.SetColumn(codeBlock, 1);
            grid.Children.Add(codeBlock);
            var radio = new RadioButton { Style = (Style)FindResource("ThemeRadio"), GroupName = "LangGroup", Content = grid, Tag = locale, IsChecked = LocaleManager.Current == locale };
            radio.Checked += (_, _) =>
            {
                LocaleManager.Apply(locale);
                Report($"Language: {name}.");
                languageMenu.IsOpen = false;
            };
            panel.Children.Add(radio);
        }
        languageMenu.Items.Add(panel);
        LangButton.ContextMenu = languageMenu;
        OpenFamilyMenu(languageMenu, LangButton);
    }
}
