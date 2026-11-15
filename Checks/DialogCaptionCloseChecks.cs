using System.IO;
using System.Xml.Linq;

// 98SE dialog caption closes size from the dialog band (Win98: band minus 2 wide, minus 4 tall,
// so 18x16 on the 20px band), not from the main caption's 16x14. Source-level; opens no window.
internal static class DialogCaptionCloseChecks
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    internal static void Run()
    {
        foreach (string file in new[] { "Controls/ConfirmDialog.xaml", "Controls/FileDialog.xaml" })
        {
            var closes = XDocument.Load(CheckSource.Find(file)).Descendants()
                .Where(e => e.Name.LocalName == "Button"
                    && (string?)e.Attribute("Style") is "{StaticResource OverlayCloseButton}" or "{StaticResource ChromeCloseButton}")
                .ToList();
            if (closes.Count == 0) throw new InvalidDataException(file + " has no caption close button.");
            foreach (var close in closes)
            {
                if ((string?)close.Attribute("Width") != "{DynamicResource DialogCloseWidth}"
                    || (string?)close.Attribute("Height") != "{DynamicResource DialogCloseHeight}")
                    throw new InvalidDataException(file + ": caption close is not sized by DialogCloseWidth/Height.");
                if ((string?)close.Attribute("VerticalAlignment") != "Center")
                    throw new InvalidDataException(file + ": caption close is not centered in the band.");
            }
        }

        string source = File.ReadAllText(CheckSource.Find("Services/ThemeManager.cs"));
        foreach (string line in new[]
        {
            "SetIfAbsent(\"DialogCloseWidth\", pickerCaption ? bar - 2 : 28.0);",
            "SetIfAbsent(\"DialogCloseHeight\", pickerCaption ? bar - 4 : 26.0);",
            "combined[\"CaptionGlyphSize\"] = flat ? 8.0 : 10.0;",
        })
            if (!source.Contains(line)) throw new InvalidDataException("ThemeManager.cs is missing: " + line);
        if (!File.ReadAllText(CheckSource.Find("Controls/Controls.xaml"))
                .Contains("<Setter Property=\"FontSize\" Value=\"{DynamicResource CaptionGlyphSize}\"/>"))
            throw new InvalidDataException("ChromeButton does not take CaptionGlyphSize.");
        Console.WriteLine("PASS: dialog caption closes size from the dialog band.");
    }
}
