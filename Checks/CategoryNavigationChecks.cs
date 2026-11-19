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
            var navigation = (ListBox)window.FindName("WorkspaceNavigation");
            navigation.SelectedItem = navigation.Items.OfType<ListBoxItem>().Single(item => Equals(item.Tag, "Tools"));
            var list = (ListBox)window.FindName("CategoryList");
            var strip = (FrameworkElement)window.FindName("LeftPanel");
            var tools = (FrameworkElement)window.FindName("ConsoleBank");
            var evidence = (FrameworkElement)window.FindName("TerminalPanel");
            Settle(window);
            Console.WriteLine("Category font: " + Item(list, 0).FontFamily.Source);
            CategoryTypographyChecks.Run(window);

            var sizes = new[] { (Width: 800, Height: 500), (Width: 1025, Height: 780), (Width: 1025, Height: 1200) };
            var themes = new[] { Theme.Dark, Theme.Light, Theme.Black, Theme.SE98 };
            var scroll = Descendants<ScrollViewer>(list).Single();
            var viewport = Descendants<ScrollContentPresenter>(scroll).Single();
            foreach (var theme in themes)
            {
                ThemeManager.Apply(theme);
                foreach (var size in sizes)
                {
                    window.Width = size.Width;
                    window.Height = size.Height;
                    Settle(window);
                    VerifyStrip(window, strip, tools, evidence);
                    VerifyLayout(list, viewport, compact: size.Width == 800);
                    Require(scroll.ScrollableWidth <= .5, $"{theme} sidebar never scrolls sideways at {size.Width}x{size.Height}");
                    // A short window scrolls the area list while Home, History and the app buttons stay put.
                    if (size.Height >= 780)
                    {
                        Require(scroll.ScrollableHeight <= .5, $"{theme} categories fit without scrolling at {size.Width}x{size.Height}");
                        for (int index = 0; index < list.Items.Count; index++)
                            Require(IsFullyVisible(Item(list, index), viewport), $"Category {index} fits inside the sidebar viewport");
                    }
                    foreach (string pinned in new[] { "SidebarHome", "SidebarHistory", "SidebarRail" })
                        Require(window.FindName(pinned) is FrameworkElement { IsVisible: true } element &&
                                BoundsIn(element, window) is var box && box.Bottom <= window.ActualHeight && box.Right <= BoundsIn(strip, window).Right + 1,
                            $"{pinned} stays visible inside the sidebar at {size.Width}x{size.Height}");

                    RenderedTypographyChecks.VerifySubtitle(window);
                    Capture(strip, Path.Combine(output, $"{theme}-{size.Width}x{size.Height}-strip.png"));
                    if (theme == Theme.Dark && size.Width == 1025)
                        Capture(window, Path.Combine(output, $"workbench-{size.Width}x{size.Height}.png"));
                }
            }

            ThemeManager.Apply(Theme.Dark);
            window.Width = 1025;
            window.Height = 780;
            Settle(window);
            window.Activate();
            VerifyKeyboard(window, list, scroll, viewport, expectOverflow: false);

            var first = Item(list, 0);
            Require(first.IsKeyboardFocusWithin && Face(first).BorderBrush == window.FindResource("PrimaryBrush"),
                "Keyboard focus has the family accent outline");
            Require(Face(first).Background == window.FindResource("SelectionBg") && first.Foreground == window.FindResource("SelectionFg"),
                "Selection retains the existing theme fill and readable foreground");
            var hover = Item(list, 1);
            var mouseOverKey = (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            hover.SetValue(mouseOverKey, true);
            try
            {
                Settle(window);
                Require(Face(hover).Background == window.FindResource("RowHoverBrush"), "Hover trigger retains the family row color");
                Capture(strip, Path.Combine(output, "short-selected-hover-focus.png"));
            }
            finally { hover.SetValue(mouseOverKey, false); }

            var zoom = typeof(KillerBench.Shell.MainWindow).GetMethod("ApplyAppScale", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var scaleHost = (FrameworkElement)window.FindName("ScaleHost");
            var toolsTab = navigation.SelectedItem;
            navigation.SelectedIndex = 0;
            navigation.SelectedItem = toolsTab;
            zoom.Invoke(window, [2.5d]);
            Settle(window);
            Require(scaleHost.LayoutTransform is ScaleTransform { ScaleX: 2.5, ScaleY: 2.5 },
                "The small-window overflow case uses the production 250% app scale");
            VerifyStrip(window, strip, tools, evidence);
            VerifyLayout(list, viewport, compact: true);
            Require(scroll.ScrollableHeight > 0 && scroll.ScrollableWidth <= .5 && !IsFullyVisible(Item(list, 11), viewport),
                "The zoomed strip exposes only vertical overflow and initially clips the last category");
            Capture(window, Path.Combine(output, "workbench-800x500-250-percent.png"));
            VerifyKeyboard(window, list, scroll, viewport, expectOverflow: true);
            scroll.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.MouseWheelEvent });
            Settle(window);
            Require(scroll.VerticalOffset > 0, "Mouse wheel reaches the zoomed strip's overflowing rows");

            Console.WriteLine($"PASS: {themes.Length * sizes.Length} sidebar layout cases across Dark, Light, Black and 98SE, plus one 800x500 250% overflow case; full and icon-only rows, glyphs, minimum height, pinned Home/History/app buttons, accessibility, Home/End, mouse-wheel scrolling, selection, simulated hover and keyboard focus.");
            Console.WriteLine("Category render evidence: " + output);
            return 0;
        }
        finally { window.Close(); application.Shutdown(); }
    }

    private static void VerifyStrip(Window window, FrameworkElement strip, FrameworkElement tools, FrameworkElement evidence)
    {
        Require(strip.IsVisible && tools.IsVisible && !evidence.IsVisible, "Tools shows the sidebar beside the tool pane alone, with output swapped out");
        Rect stripBounds = BoundsIn(strip, window);
        Rect toolBounds = BoundsIn(tools, window);
        Require(stripBounds.Right <= toolBounds.Left + 1 && stripBounds.Top <= toolBounds.Top + 1 && stripBounds.Bottom >= toolBounds.Bottom - 1,
            $"The sidebar runs down the left of the tool pane, at least as tall as it. Sidebar {stripBounds}, page {toolBounds}");
    }

    // The sidebar lists one area per row; below 900 effective pixels it shows icons only, with the name kept for tooltips and accessibility.
    private static void VerifyLayout(ListBox list, ScrollContentPresenter viewport, bool compact)
    {
        Require(list.Items.Count == 12, "The sidebar contains every tool category");
        Rect first = BoundsIn(Item(list, 0), viewport);
        Rect previous = first;
        var glyphs = new HashSet<string>();
        for (int index = 0; index < list.Items.Count; index++)
        {
            var item = Item(list, index);
            Rect bounds = BoundsIn(item, viewport);
            Require(item.ActualHeight >= 31.99, $"Category {index} keeps its 32-pixel minimum height");
            Require(bounds.Left >= -1 && bounds.Right <= viewport.ActualWidth + 1,
                $"Category {index} fits horizontally in the strip");
            if (index > 0)
                Require(bounds.Top >= previous.Bottom - 1 && Math.Abs(bounds.Left - first.Left) <= 1,
                    $"Category {index} sits on its own row under the previous one, without overlap");
            previous = bounds;

            var label = Descendants<DockPanel>(item).Single(panel => panel.Name == "CategoryLabel");
            var icon = Descendants<TextBlock>(label).Single(block => block.Name == "CategoryIcon");
            var text = Descendants<TextBlock>(label).Single(block => block.Name == "CategoryText");
            Rect iconBounds = BoundsIn(icon, item);
            Require(DockPanel.GetDock(icon) == Dock.Left && icon.FontSize == 16 && iconBounds.Right <= item.ActualWidth,
                $"The normal-size category glyph sits at the row's left. Dock {DockPanel.GetDock(icon)}, size {icon.FontSize}, icon {iconBounds}, row width {item.ActualWidth}");
            Require(icon.FontFamily.Source == "Segoe MDL2 Assets" && glyphs.Add(icon.Text), "Each category has a distinct family glyph");
            Require(new Typeface(icon.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out var font)
                && font.CharacterToGlyphMap.TryGetValue(icon.Text[0], out ushort glyph) && glyph != 0, "Category glyph exists in the installed font");
            if (compact)
            {
                Require(!text.IsVisible, $"Category {index} shows its icon only in the narrow sidebar");
            }
            else
            {
                Rect textBounds = BoundsIn(text, item);
                Require(iconBounds.Right <= textBounds.Left && iconBounds.Top < textBounds.Bottom && textBounds.Top < iconBounds.Bottom,
                    "The category glyph sits beside its label");
                Require(text.ActualHeight > 0 && textBounds.Top >= 0 && textBounds.Bottom <= item.ActualHeight,
                    "Category label fits vertically without clipping");
                var fullText = new FormattedText(text.Text, System.Globalization.CultureInfo.CurrentCulture, text.FlowDirection,
                    new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize,
                    text.Foreground, null, TextOptions.GetTextFormattingMode(text), VisualTreeHelper.GetDpi(text).PixelsPerDip);
                Require(fullText.Width <= text.ActualWidth + .5, "Full category label fits in the sidebar");
            }
            Require(UIElementAutomationPeer.CreatePeerForElement(item)!.GetName() == list.Items[index].ToString() && Equals(item.ToolTip, list.Items[index].ToString()),
                "Category text remains available to accessibility and tooltips");
        }
    }

    private static void VerifyKeyboard(Window window, ListBox list, ScrollViewer scroll, ScrollContentPresenter viewport, bool expectOverflow)
    {
        var first = Item(list, 0);
        var last = Item(list, 11);
        Require(first.Focus(), "Category accepts keyboard focus on the isolated desktop");
        Settle(window);
        SendKey(first, Key.End);
        Settle(window);
        Require(list.SelectedIndex == 11 && UIElementAutomationPeer.CreatePeerForElement(last)!.GetName() == "Storage" &&
                IsFullyVisible(last, viewport), "End selects Storage and reveals the entire button");
        Require(expectOverflow ? scroll.VerticalOffset > 0 : scroll.VerticalOffset <= .5,
            "End scrolls only when the categories overflow");

        SendKey(last, Key.Home);
        Settle(window);
        Require(list.SelectedIndex == 0 && UIElementAutomationPeer.CreatePeerForElement(first)!.GetName() == "Diagnostics" &&
                IsFullyVisible(first, viewport) && scroll.VerticalOffset <= .5,
            "Home selects Diagnostics and reveals the entire first button at the top");
    }

    private static void SendKey(ListBoxItem item, Key key)
    {
        item.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(item), 0, key)
        { RoutedEvent = Keyboard.KeyDownEvent });
    }

    private static bool IsFullyVisible(FrameworkElement element, ScrollContentPresenter viewport)
    {
        Rect bounds = BoundsIn(element, viewport);
        return bounds.Top >= -1 && bounds.Bottom <= viewport.ActualHeight + 1 &&
            bounds.Left >= -1 && bounds.Right <= viewport.ActualWidth + 1;
    }

    private static Rect BoundsIn(FrameworkElement element, Visual ancestor) =>
        element.TransformToAncestor(ancestor).TransformBounds(new Rect(element.RenderSize));

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
