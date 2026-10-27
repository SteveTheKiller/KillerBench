using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KillerBench.Services;

internal static class CategoryNavigationChecks
{
    internal static int Run()
    {
        IsolatedDesktop.Verify();
        var application = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
        foreach (string resource in new[] { "Themes/Black.xaml", "Controls/Controls.xaml", "Themes/Instrument.xaml", "Strings/en-US.xaml" })
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/KillerBench;component/" + resource, UriKind.Relative) });
        ThemeManager.Initialize();
        string output = Path.Combine(Path.GetTempPath(), "KillerBench-category-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var window = new KillerBench.Shell.MainWindow
        {
            ShowActivated = false, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = SystemParameters.VirtualScreenLeft - 2000, Top = SystemParameters.VirtualScreenTop
        };
        try
        {
            window.Show();
            var list = (ListBox)window.FindName("CategoryList");
            var sidebar = (FrameworkElement)window.FindName("LeftPanel");
            Require(sidebar.Width == 160, "Fresh sidebar uses the narrower 160-pixel default");
            Settle(window);
            Console.WriteLine("Category font: " + Item(list, 0).FontFamily.Source);
            CategoryTypographyChecks.Run(window);
            foreach (var theme in new[] { Theme.Dark, Theme.Light, Theme.Black, Theme.SE98 })
            {
                ThemeManager.Apply(theme);
                foreach (double height in new[] { 500d, 700d, 1200d })
                {
                    window.Width = height == 500 ? 800 : 1025;
                    window.Height = height;
                    Settle(window);
                    VerifyLayout(list);
                    RenderedTypographyChecks.VerifySubtitle(window);
                    Capture(sidebar, Path.Combine(output, $"{theme}-{height}.png"));
                    if (theme == Theme.Dark && (height == 700 || height == 1200)) Capture(window, Path.Combine(output, $"workbench-{height}.png"));
                }
            }
            ThemeManager.Apply(Theme.Dark);
            window.Height = 500;
            Settle(window);
            var scroll = Descendants<ScrollViewer>(list).First();
            Require(scroll.ScrollableHeight > 0, "Short window scrolls instead of shrinking category controls");
            var first = Item(list, 0);
            window.Activate();
            Require(first.Focus(), "Category accepts keyboard focus on the isolated desktop");
            Settle(window);
            Require(first.IsKeyboardFocusWithin && Face(first).BorderBrush == window.FindResource("PrimaryBrush"),
                "Keyboard focus has the family accent outline");
            Capture(sidebar, Path.Combine(output, "short-keyboard-focus.png"));
            first.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(first), 0, Key.End)
            { RoutedEvent = Keyboard.KeyDownEvent });
            Settle(window);
            Require(list.SelectedIndex == 11 && scroll.VerticalOffset > 0, "End key selects and reveals the final category");
            var last = Item(list, 11);
            var top = last.TranslatePoint(new Point(), scroll).Y;
            Require(top >= -1 && top + last.ActualHeight <= scroll.ViewportHeight + 1,
                "Keyboard selection brings the entire minimum-height button into view");
            Require(UIElementAutomationPeer.CreatePeerForElement(last)!.GetName() == "Storage", "Automation exposes a text category name");
            Capture(sidebar, Path.Combine(output, "short-keyboard-end.png"));
            scroll.ScrollToTop();
            Settle(window);
            list.SelectedIndex = 0;
            Require(Face(first).Background == window.FindResource("SelectionBg") && first.Foreground == window.FindResource("SelectionFg"),
                "Selection retains the existing theme fill and readable foreground");
            var hover = Item(list, 1);
            var mouseOverKey = (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            hover.SetValue(mouseOverKey, true);
            Settle(window);
            Require(Face(hover).Background == window.FindResource("RowHoverBrush"), "Hover trigger retains the family row color");
            Capture(sidebar, Path.Combine(output, "short-selected-hover-focus.png"));
            hover.SetValue(mouseOverKey, false);
            scroll.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.MouseWheelEvent });
            Settle(window);
            Require(scroll.VerticalOffset > 0, "Mouse wheel reaches overflowing categories");
            sidebar.Width = 140;
            Settle(window);
            VerifyLayout(list);
            Capture(sidebar, Path.Combine(output, "short-minimum-sidebar-width.png"));
            sidebar.Width = 160;

            // Cross the real allocation threshold repeatedly. Desired content cannot drive row height.
            window.Height = 900;
            Settle(window);
            double thresholdWindow = window.Height + (66 - (scroll.ViewportHeight / list.Items.Count)) * list.Items.Count;
            foreach (double offset in new[] { -3d, -1d, 0d, 1d, 3d, 0d, -1d, -3d })
            {
                window.Height = thresholdWindow + offset * list.Items.Count;
                Settle(window);
                VerifyLayout(list);
                double stableHeight = Item(list, 0).ActualHeight;
                Settle(window);
                Require(Math.Abs(stableHeight - Item(list, 0).ActualHeight) < .01, "Orientation threshold has stable layout");
                if (offset == -1 || offset == 1) Capture(sidebar, Path.Combine(output, $"threshold-{offset}.png"));
            }
            var zoom = typeof(KillerBench.Shell.MainWindow).GetMethod("ApplyAppScale", BindingFlags.Instance | BindingFlags.NonPublic)!;
            foreach (double scale in new[] { .7, 1d, 1.5, 2.5 })
            {
                zoom.Invoke(window, [scale]);
                Settle(window);
                Require(Math.Abs(sidebar.ActualWidth - 160) < .1, "App zoom preserves the narrower sidebar width");
            }
            zoom.Invoke(window, [1d]);
            // Enlarged interface uses fewer logical pixels and retains readable scrolling controls.
            var scaleHost = (FrameworkElement)window.FindName("ScaleHost");
            scaleHost.LayoutTransform = new ScaleTransform(1.5, 1.5);
            window.Height = 500;
            Settle(window);
            VerifyLayout(list);
            Require(scroll.ScrollableHeight > 0, "Enlarged interface retains overflow access");
            Capture(sidebar, Path.Combine(output, "short-150-percent.png"));
            // Use only process-local preferences, with no load or save of the user's settings file.
            var preferences = typeof(KillerBench.Shell.MainWindow).Assembly.GetType("KillerBench.Services.Preferences")!;
            var setPreference = preferences.GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic)!;
            var initializePanels = typeof(KillerBench.Shell.MainWindow).GetMethod("InitializePanels", BindingFlags.Instance | BindingFlags.NonPublic)!;
            foreach (double savedWidth in new[] { 180d, 230d })
            {
                setPreference.Invoke(null, ["SidebarWidth", savedWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
                initializePanels.Invoke(window, []);
                Require(sidebar.Width == savedWidth, "Saved sidebar width takes precedence over the new default");
            }
            sidebar.Width = 210;
            typeof(KillerBench.Shell.MainWindow).GetMethod("OnSidebarGripCompleted", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(window, [window.FindName("SidebarGrip"), new System.Windows.Controls.Primitives.DragCompletedEventArgs(0, 0, false)]);
            initializePanels.Invoke(window, []);
            Require(sidebar.Width == 210, "Splitter-completed width remains the restored sidebar width");
            setPreference.Invoke(null, ["SidebarWidth", "160"]);
            Console.WriteLine("PASS: category layout at 800x500, 1025x700 and 1025x1200 across Dark, Light, Black and 98SE; 160-pixel default, both icon orientations, full labels, saved and splitter widths, threshold stability, minimum heights, keyboard and mouse-wheel overflow, 140-pixel sidebar, accessible names, selection, simulated hover, focus and 150% layout scale.");
            Console.WriteLine("Category render evidence: " + output);
            return 0;
        }
        finally { window.Close(); application.Shutdown(); }
    }

    private static void VerifyLayout(ListBox list)
    {
        var scroll = Descendants<ScrollViewer>(list).First();
        double expected = Math.Max(42, scroll.ViewportHeight / list.Items.Count) - 2;
        var glyphs = new HashSet<string>();
        for (int index = 0; index < list.Items.Count; index++)
        {
            var item = Item(list, index);
            Require(item.ActualHeight >= 39.99 && Math.Abs(item.ActualHeight - expected) < 1,
                $"Category {index} shares viewport height and keeps a 40-pixel readable minimum: {item.ActualHeight}, expected {expected}");
            var label = Descendants<DockPanel>(item).Single(panel => panel.Name == "CategoryLabel");
            var icon = Descendants<TextBlock>(label).Single(block => block.Name == "CategoryIcon");
            var text = Descendants<TextBlock>(label).Single(block => block.Name == "CategoryText");
            Require(DockPanel.GetDock(icon) == (item.ActualHeight >= 64 ? Dock.Top : Dock.Left), "Icon orientation follows allocated button height");
            Require(icon.FontFamily.Source == "Segoe MDL2 Assets" && glyphs.Add(icon.Text), "Each category has a distinct family glyph");
            Require(new Typeface(icon.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out var font)
                && font.CharacterToGlyphMap.TryGetValue(icon.Text[0], out ushort glyph) && glyph != 0, "Category glyph exists in the installed font");
            var textOrigin = text.TranslatePoint(new Point(), item);
            Require(text.ActualHeight > 0 && textOrigin.Y >= 0 && textOrigin.Y + text.ActualHeight <= item.ActualHeight,
                "Category label fits vertically without clipping");
            var fullText = new FormattedText(text.Text, System.Globalization.CultureInfo.CurrentCulture, text.FlowDirection,
                new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize,
                text.Foreground, VisualTreeHelper.GetDpi(text).PixelsPerDip);
            Require(fullText.Width <= text.ActualWidth + .5, "Full category label fits at the existing minimum sidebar width");
            Require(UIElementAutomationPeer.CreatePeerForElement(item)!.GetName() == list.Items[index].ToString() && Equals(item.ToolTip, list.Items[index].ToString()),
                "Category text remains available to accessibility and narrow-sidebar tooltips");
        }
        Require(Math.Abs(scroll.ExtentHeight - Math.Max(scroll.ViewportHeight, 42 * list.Items.Count)) < 1,
            "Category buttons fill the viewport or expose their complete scroll extent");
    }

    private static ListBoxItem Item(ListBox list, int index) => (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(index);
    private static Border Face(ListBoxItem item) => (Border)item.Template.FindName("CategoryFace", item);
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Settle(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }
    private static void Capture(FrameworkElement visual, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth), (int)Math.Ceiling(visual.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
