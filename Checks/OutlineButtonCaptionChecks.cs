using System.IO;
using System.Windows;
using System.Windows.Controls;

// A dialog's confirm button (Unlock) keeps hinted text: an effect on its caption would rasterize
// it and blur it at rest. Loads the shipped Black theme and kit styles; opens no window.
internal static class OutlineButtonCaptionChecks
{
    internal static void Run()
    {
        var root = new Border { Width = 300 };
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml" })
            root.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        var button = new Button { Content = "Unlock", Style = (Style)root.FindResource("OutlineButton") };
        root.Child = button;
        root.Measure(new Size(300, double.PositiveInfinity));
        root.Arrange(new Rect(root.DesiredSize));
        root.UpdateLayout();

        if (button.Template.FindName("label", button) is not ContentPresenter label)
            throw new InvalidDataException("OutlineButton's template has no ContentPresenter named label.");
        if (label.Effect is not null)
            throw new InvalidDataException("OutlineButton's caption carries an effect (" + label.Effect.GetType().Name
                + "), which rasterizes the text and blurs it at rest.");
        Console.WriteLine("PASS: outline button captions are not rasterized.");
    }
}
