using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KillerBench.Services;

internal static class CategoryTypographyChecks
{
    // Family UI typography established by the Killer Sans migration. 98SE keeps its native face.
    private const string UiStack = "./Fonts/#Killer Sans UI, Segoe UI, Microsoft JhengHei UI, Nirmala UI";

    internal static void Run(KillerBench.MainWindow window)
    {
        RenderedTypographyChecks.VerifyFallbackRejected();
        var categories = (ListBox)window.FindName("CategoryList");
        var primary = new FontFamily(new Uri("pack://application:,,,/KillerBench;component/"), "./Fonts/#Killer Sans UI");
        Require(new Typeface(primary, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out var glyphs)
            && glyphs.FamilyNames.Values.Contains("Killer Sans UI") && glyphs.FontUri.Scheme == "pack",
            "Killer Sans UI resolves from the compiled app instead of silently falling back to an installed font");
        Require(categories.Items.Cast<object>().SelectMany(category => category.ToString()!).Distinct()
                .All(character => glyphs.CharacterToGlyphMap.TryGetValue(character, out ushort glyph) && glyph != 0),
            "The embedded UI face covers category label characters");

        Theme originalTheme = ThemeManager.Current;
        try
        {
            foreach (Theme theme in Enum.GetValues<Theme>())
            {
                ThemeManager.Apply(theme);
                window.UpdateLayout();
                var uiFont = (FontFamily)window.FindResource("UiFont");
                var expected = theme == Theme.SE98 ? new FontFamily("Microsoft Sans Serif") :
                    new FontFamily(new Uri("pack://application:,,,/KillerBench;component/"), UiStack);
                Require(uiFont.Equals(expected), $"{theme} retains the approved UI face and fallback stack");
                RenderedTypographyChecks.VerifySubtitle(window);
                for (int index = 0; index < categories.Items.Count; index++)
                {
                    var item = (ListBoxItem)categories.ItemContainerGenerator.ContainerFromIndex(index);
                    var label = FindTextBlocks(item).Single(block => block.Name == "CategoryText");
                    var icon = FindTextBlocks(item).Single(block => block.Name == "CategoryIcon");
                    Require(item.FontFamily.Equals(expected) && label.FontFamily.Equals(expected),
                        $"{theme} category {index} inherits the approved UI face through its container and label");
                    RenderedTypographyChecks.VerifyFont(label, theme == Theme.SE98 ? "Microsoft Sans Serif" : "Killer Sans UI",
                        theme == Theme.SE98 ? null : "KillerSansUI-Regular.ttf");
                    Require(icon.FontFamily.Source == "Segoe MDL2 Assets", "Category glyphs retain their separate icon face");
                }
            }
        }
        finally { ThemeManager.Apply(originalTheme); window.UpdateLayout(); }
        Console.WriteLine("PASS: actual subtitle and category glyphs match production font bytes across all 13 themes; 98SE retains its native face, and the previous Segoe UI fallback is rejected.");
    }

    private static IEnumerable<TextBlock> FindTextBlocks(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is TextBlock text) yield return text;
            foreach (var descendant in FindTextBlocks(child)) yield return descendant;
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
