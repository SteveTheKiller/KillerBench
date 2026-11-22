using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

// ============================================================
// SHORTCUTS OVERLAY - F1, with a LIST view and a keyboard MAP view
//
// The family standard, extracted from KillerPDF (KeyboardMapOverlay.cs) with Killendar's single
// binding table and category colors folded in. ONE table (KsAll: the family rows at the bottom of
// this file, then the app's rows) drives both views and nothing else restates it, so the list and
// the map cannot drift apart.
//
// IN THE APP: return the app's own rows from KsAppRows in the app-owned
// Shell/ShortcutsOverlay.Rows.cs (this file stays the kit's), wire
// HandleShortcutsKey and HandleFamilyKey from the window's PreviewKeyDown, and build the named XAML
// parts listed under "XAML CONTRACT" below. Labels are Str_* keys, so the overlay follows the active
// locale.
//
// FAMILY KEYS. Some shortcuts mean the same thing in every app that has the feature: the toolbar
// look (Ctrl+Shift+1-6), hiding the toolbar (Alt+M), the sidebar (F9, and Ctrl+Alt+Shift+arrow to
// put it on that side) and Quit (Ctrl+Q). An app names the ones it has in FamilyKeysInApp
// (Rows.cs); the kit then lists their rows and HandleFamilyKey answers their keys through the
// optional hooks below, so no app picks its own chord for a shared feature. Ctrl+Q itself is
// answered by QuitShortcut.Attach; the flag only lists it.
//
// ------------------------------------------------------------
// THE GEOMETRY IS NOT NEGOTIABLE. Every number here is KillerPDF's, and an app that picks its own
// looks like a bad copy of the others:
//
//   key unit      46      (one cap plus its 4px gap)
//   cap height    44
//   cap letter    11pt, pinned TOP
//   action label  8.5pt, pinned BOTTOM, inside a ClipToBounds host
//   accent bar    3px, bottom, radius 1.5
//   hover lift    -3px over 90ms in, 0 over 130ms out
//   detail line   12.5pt mono under the board
//   card MaxWidth 1080 for the map, 640 for the list
//
// A Grid, NOT a StackPanel. The letter is pinned top and the action label pinned bottom, so both
// sit at the same height on every key whatever the label's length. Stacked, they center as a group
// and the letters wander up and down the row depending on whether the key is bound.
//
// ------------------------------------------------------------
// XAML CONTRACT - the overlay needs these named parts (README, "The F1 overlay", has the markup):
//   ShortcutsOverlay   the dimmed full-window Grid, Collapsed, MouseLeftButtonDown=ShortcutsOverlay_Click
//   ShortcutCardGrid   the card root, MouseLeftButtonDown=ShortcutsCard_Click. HorizontalAlignment
//                      Stretch with a margin, NOT Center: centered, the card sizes to the board's
//                      natural width and the window clips whatever does not fit. MaxWidth is set
//                      per view in code (1080 map, 640 list).
//   KsViewList, KsViewKeyboard   the two KsToggle view buttons (KsViewList_Click, KsViewKeyboard_Click)
//   KsListHostScroll   ScrollViewer around KsListHost, the StackPanel the list is built into
//   KsMapHost          StackPanel holding KsLayerTabs (StackPanel, horizontal - the layer tabs are
//                      generated) and then a
//                      <Viewbox Stretch="Uniform" StretchDirection="DownOnly" HorizontalAlignment="Center">
//                      around KsMapRows, the StackPanel the board is built into. DownOnly so it
//                      shrinks to fit a narrow window but never blows up on a wide one - a keyboard
//                      at 200% looks absurd and the cap text is sized for 100%. The detail line is
//                      added to KsMapHost after the Viewbox in code.
//   a close button wired to ShortcutsClose_Click
// ============================================================
namespace KillerBench.Shell
{
    /// <summary>
    /// Shortcut categories. An app uses the ones it has and ignores the rest - the point is that a
    /// category means the same COLOR in every app, so "the cyan keys are file operations" carries
    /// across the whole family. Add a case here rather than inventing a color locally.
    /// </summary>
    internal enum KsCat { File, View, Nav, Edit, Tools, Search, Help, Ocr }

    /// <summary>
    /// The keyboard map's modifier layers, in tab order. ALT+SHIFT carries the family-wide Film
    /// Grain chord (Alt+Shift+G, Shell/ThemeFlyout.cs), so every app's table lists that row on this
    /// layer and the map draws its tab.
    /// </summary>
    internal enum KsLayer { Base, Ctrl, CtrlShift, Shift, Alt, AltShift, CtrlAltShift }

    /// <summary>The family shortcuts an app has the feature for (see FAMILY KEYS above).</summary>
    [Flags]
    internal enum FamilyKeys
    {
        None = 0,
        ToolbarLook = 1,     // Ctrl+Shift+1-6: icon size and text placement (Shell/ToolbarStyle.cs)
        ToolbarHide = 2,     // Alt+M
        SidebarToggle = 4,   // F9
        SidebarSides = 8,    // Ctrl+Alt+Shift+arrow, the arrow pointing at the side
        Quit = 16,           // Ctrl+Q, answered by QuitShortcut.Attach
    }

    internal static class KsLayers
    {
        /// <summary>The layer tabs above the board, left to right.</summary>
        internal static readonly (KsLayer Layer, string Caption)[] Tabs =
        [
            (KsLayer.Base, "BASE"), (KsLayer.Ctrl, "CTRL"), (KsLayer.CtrlShift, "CTRL+SHIFT"),
            (KsLayer.Shift, "SHIFT"), (KsLayer.Alt, "ALT"), (KsLayer.AltShift, "ALT+SHIFT"),
            (KsLayer.CtrlAltShift, "CTRL+ALT+SHIFT"),
        ];

        /// <summary>Modifier keycaps that light up per layer. They define the layer rather than
        /// carry a binding of their own.</summary>
        internal static readonly Dictionary<KsLayer, string[]> Mods = new()
        {
            [KsLayer.Base] = [],
            [KsLayer.Ctrl] = ["Ctrl", "RCtrl"],
            [KsLayer.CtrlShift] = ["Ctrl", "RCtrl", "Shift", "RShift"],
            [KsLayer.Shift] = ["Shift", "RShift"],
            [KsLayer.Alt] = ["Alt", "RAlt"],
            [KsLayer.AltShift] = ["Alt", "RAlt", "Shift", "RShift"],
            [KsLayer.CtrlAltShift] = ["Ctrl", "RCtrl", "Alt", "RAlt", "Shift", "RShift"],
        };

        /// <summary>The layer to preview while modifiers are held, so holding Alt+Shift shows the
        /// Alt+Shift tab. Ctrl wins over Alt, so AltGr (reported as Ctrl+Alt) previews a Ctrl layer.</summary>
        internal static KsLayer For(System.Windows.Input.ModifierKeys m)
        {
            bool ctrl = m.HasFlag(System.Windows.Input.ModifierKeys.Control);
            bool shift = m.HasFlag(System.Windows.Input.ModifierKeys.Shift);
            bool alt = m.HasFlag(System.Windows.Input.ModifierKeys.Alt);
            return ctrl && alt && shift ? KsLayer.CtrlAltShift
                 : ctrl && shift ? KsLayer.CtrlShift
                 : ctrl ? KsLayer.Ctrl
                 : alt && shift ? KsLayer.AltShift
                 : alt ? KsLayer.Alt
                 : shift ? KsLayer.Shift
                 : KsLayer.Base;
        }

        /// <summary>The layer a binding sits on, read from how its keys are written ("Ctrl+Shift+S",
        /// "Alt+Shift+G", "Ctrl+O", "Alt+A", "Shift+F3"), so a table row cannot name one layer in
        /// its text and land on another on the map.</summary>
        internal static KsLayer ForKeys(string keys)
            => keys.StartsWith("Ctrl+Alt+Shift+", StringComparison.Ordinal) ? KsLayer.CtrlAltShift
             : keys.StartsWith("Ctrl+Shift+", StringComparison.Ordinal) ? KsLayer.CtrlShift
             : keys.StartsWith("Alt+Shift+", StringComparison.Ordinal) ? KsLayer.AltShift
             : keys.StartsWith("Ctrl+", StringComparison.Ordinal) ? KsLayer.Ctrl
             : keys.StartsWith("Alt+", StringComparison.Ordinal) ? KsLayer.Alt
             : keys.StartsWith("Shift+", StringComparison.Ordinal) ? KsLayer.Shift
             : KsLayer.Base;
    }

    /// <summary>
    /// One row of the table. <paramref name="caps"/> are the map key ids it lights (the ids in
    /// KsBoard.Rows), so one action can own several keys - "M or 1" lights both. LabelKey is a
    /// Str_* resource key so the overlay follows the active locale.
    /// </summary>
    internal readonly struct KsBinding(string keys, string labelKey, KsCat cat, params string[] caps)
    {
        public readonly string Keys = keys;          // how it reads on the list, e.g. "Ctrl+O"
        public readonly string LabelKey = labelKey;  // Str_* resource key
        public readonly KsCat Cat = cat;
        public readonly string[] Caps = caps;        // map key ids to light
        public KsLayer Layer => KsLayers.ForKeys(Keys);

        /// <summary>Per-cap labels, in Caps order, for a row whose keys each do their own thing
        /// ("Ctrl+Shift+1-6"): the list shows the row's label, the map each cap's own.</summary>
        public string[]? CapLabelKeys { get; init; }

        public string LabelFor(string cap)
        {
            int at = Array.IndexOf(Caps, cap);
            return CapLabelKeys is { } labels && at >= 0 && at < labels.Length ? labels[at] : LabelKey;
        }
    }

    /// <summary>The physical board the map draws: (id, cap, width in units); an empty id is a gap.
    /// KillerPDF's layout, the same in every app.</summary>
    internal static class KsBoard
    {
        // Arrow, erase and menu caps are built from codepoints rather than typed literally, so the
        // file stays pure ASCII - a literal glyph does not survive every tool in the chain. Declared
        // before Rows because static initializers run in declaration order and Rows reads them.
        internal static readonly string CapLeft  = ((char)0x2190).ToString();   // left arrow
        internal static readonly string CapUp    = ((char)0x2191).ToString();   // up arrow
        internal static readonly string CapRight = ((char)0x2192).ToString();   // right arrow
        internal static readonly string CapDown  = ((char)0x2193).ToString();   // down arrow
        private static readonly string CapBack  = ((char)0x232B).ToString();   // erase to the left
        private static readonly string CapMenu  = ((char)0x2630).ToString();   // trigram, the menu key

        internal static readonly (string Id, string Cap, double W)[][] Rows =
        [
            [("Esc","Esc",1), ("","",0.8), ("F1","F1",1), ("F2","F2",1), ("F3","F3",1), ("F4","F4",1), ("","",0.6),
             ("F5","F5",1), ("F6","F6",1), ("F7","F7",1), ("F8","F8",1), ("","",0.6),
             ("F9","F9",1), ("F10","F10",1), ("F11","F11",1), ("F12","F12",1)],
            [("Grave","`",1), ("D1","1",1), ("D2","2",1), ("D3","3",1), ("D4","4",1), ("D5","5",1), ("D6","6",1),
             ("D7","7",1), ("D8","8",1), ("D9","9",1), ("D0","0",1), ("Minus","-",1), ("Equals","=",1), ("Back",CapBack,2),
             ("","",0.6), ("Ins","Ins",1), ("Home","Home",1), ("PgUp","PgUp",1)],
            [("Tab","Tab",1.5), ("Q","Q",1), ("W","W",1), ("E","E",1), ("R","R",1), ("T","T",1), ("Y","Y",1),
             ("U","U",1), ("I","I",1), ("O","O",1), ("P","P",1), ("LBr","[",1), ("RBr","]",1), ("BSl","\\",1.5),
             ("","",0.6), ("Del","Del",1), ("End","End",1), ("PgDn","PgDn",1)],
            [("Caps","Caps",1.8), ("A","A",1), ("S","S",1), ("D","D",1), ("F","F",1), ("G","G",1), ("H","H",1),
             ("J","J",1), ("K","K",1), ("L","L",1), ("Semi",";",1), ("Quote","'",1), ("Enter","Enter",2.2)],
            [("Shift","Shift",2.3), ("Z","Z",1), ("X","X",1), ("C","C",1), ("V","V",1), ("B","B",1), ("N","N",1),
             ("M","M",1), ("Comma",",",1), ("Period",".",1), ("Slash","/",1), ("RShift","Shift",2.7),
             ("","",1.6), ("Up",CapUp,1)],
            [("Ctrl","Ctrl",1.5), ("Win","Win",1.2), ("Alt","Alt",1.5), ("Space","",6.8), ("RAlt","Alt",1.5),
             ("Menu",CapMenu,1), ("RCtrl","Ctrl",1.5), ("","",0.6),
             ("Left",CapLeft,1), ("Down",CapDown,1), ("Right",CapRight,1)],
        ];
    }

    internal static class KsPalette
    {
        /// <summary>
        /// Category colors are THEME BRUSHES, not hardcoded values: each app declares
        /// <c>KsCatFile</c>, <c>KsCatView</c> and so on in every Themes/*.xaml, and the map binds
        /// to them by name. That is how KillerPDF and KillerShell have always done it, and it is
        /// what lets a theme retune them - a hardcoded neon that is perfect on Dark can be
        /// unreadable on Light.
        ///
        /// THE CANONICAL VALUES, copied from KillerPDF/Themes/Dark.xaml. Add these to a new app's
        /// theme files verbatim; do not pick fresh ones, or the same category means two different
        /// colors in two apps and the whole point is lost:
        ///
        ///     KsCatFile    #00fff9   cyan
        ///     KsCatTools   #ffd319   yellow
        ///     KsCatEdit    #ff2975   hot pink
        ///     KsCatNav     #8c1eff   violet
        ///     KsCatView    #f222ff   magenta
        ///     KsCatSearch  #00b3ff   azure
        ///     KsCatHelp    #39ff14   green
        ///     KsCatOcr     #ff901f   orange
        ///
        /// An app only needs the categories it actually has. Killendar adds KsCatAppt at #ffd319,
        /// the same yellow as Tools, because appointments are its app-specific action category and
        /// Tools is KillerPDF's - one slot, one color, named for whatever the app calls it.
        /// </summary>
        internal static string BrushKey(KsCat cat) => "KsCat" + cat;
    }

    /// <summary>
    /// Builds one keycap. Static and parameterized so an app can call it from its own partial
    /// without inheriting anything - pass the app's Loc() result in as <paramref name="label"/>.
    /// </summary>
    internal static class KsKeyboard
    {
        internal const double Unit = 46;
        internal const double CapHeight = 44;

        /// <summary>
        /// One keycap. <paramref name="cat"/> null means the key carries no binding on this layer:
        /// it draws dim, does not lift, and shows no label or bar. Those unlit keys are what make
        /// the lit ones read as the interactive set, so they are drawn, not omitted.
        /// </summary>
        internal static Border Cap(string cap, double w, KsCat? cat, string? label,
                                   string? gesture, TextBlock? detail)
        {
            var capText = new TextBlock
            {
                Text = cap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 0, 0),
            };
            capText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

            var act = new TextBlock
            {
                FontSize = 8.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Visibility = Visibility.Collapsed,
                RenderTransform = new TranslateTransform(),
            };
            // Clipping host, so a label too long for the cap can be scrolled rather than silently
            // ellipsized. That label is the whole point of the map.
            var actHost = new Border
            {
                ClipToBounds = true,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2, 0, 2, 5),
                Child = act,
            };
            var bar = new Rectangle
            {
                Height = 3,
                VerticalAlignment = VerticalAlignment.Bottom,
                RadiusX = 1.5, RadiusY = 1.5,
                Margin = new Thickness(3, 0, 3, 0),
                Visibility = Visibility.Collapsed,
            };

            var inner = new Grid();
            inner.Children.Add(capText);
            inner.Children.Add(actHost);
            inner.Children.Add(bar);

            var key = new Border
            {
                Width = Unit * w - 4,
                Height = CapHeight,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 4, 0),
                Child = inner,
            };
            // KeyCapBrush, not SurfaceBrush directly: it tracks SurfaceBrush on every theme, but a
            // flat-caption theme (98SE) paints its caps white (ThemeManager.ApplyDerivedTokens).
            key.SetResourceReference(Border.BackgroundProperty, "KeyCapBrush");

            if (cat is KsCat c)
            {
                // SetResourceReference, not a fixed brush: the category colors are theme brushes,
                // so a live theme switch has to repaint the board rather than leave it on the old
                // palette. Same reason the rest of the family uses DynamicResource for brushes.
                string k = KsPalette.BrushKey(c);
                key.SetResourceReference(Border.BorderBrushProperty, k);
                act.Text = label ?? "";
                act.SetResourceReference(TextBlock.ForegroundProperty, k);
                act.Visibility = Visibility.Visible;
                bar.SetResourceReference(Shape.FillProperty, k);
                bar.Visibility = Visibility.Visible;

                var lift = new TranslateTransform();
                key.RenderTransform = lift;
                key.MouseEnter += (_, _2) =>
                {
                    if (detail != null) detail.Text = (gesture ?? "") + "   " + (label ?? "");
                    lift.BeginAnimation(TranslateTransform.YProperty,
                        new DoubleAnimation(-3, TimeSpan.FromMilliseconds(90)));
                };
                key.MouseLeave += (_, _2) =>
                {
                    if (detail != null) detail.Text = " ";
                    lift.BeginAnimation(TranslateTransform.YProperty,
                        new DoubleAnimation(0, TimeSpan.FromMilliseconds(130)));
                };
            }
            else
            {
                key.SetResourceReference(Border.BorderBrushProperty, "CardBorderBrush");
                capText.SetResourceReference(TextBlock.ForegroundProperty, "DimTextBrush");
            }
            return key;
        }

        /// <summary>The detail line that sits under the board and reads out the hovered key.
        /// Add it to the map host AFTER the Viewbox.</summary>
        internal static TextBlock DetailLine()
        {
            var t = new TextBlock
            {
                Text = " ",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12.5,
                Margin = new Thickness(2, 10, 0, 0),
                Height = 18,
            };
            t.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
            return t;
        }
    }

    public partial class MainWindow
    {
        // ---- THE table. The family rows below are in every app; the app's own rows follow them,
        //      from KsAppRows in Shell/ShortcutsOverlay.Rows.cs. Mirror the window's key handling
        //      exactly, and the landing site's help page, which lists the same set. ----
        private static KsBinding[]? _ksAll;
        private static KsBinding[] KsAll => _ksAll ??= [.. KsFamilyRows, .. FamilyKeyRows(FamilyKeysInApp()), .. KsAppRows()];

        /// <summary>The app's own rows, after the family rows. Implemented in the app-owned
        /// Shell/ShortcutsOverlay.Rows.cs.</summary>
        private static partial KsBinding[] KsAppRows();

        /// <summary>The family shortcuts this app has the feature for. Implemented in the app-owned
        /// Shell/ShortcutsOverlay.Rows.cs; FamilyKeys.None when it has none of them.</summary>
        private static partial FamilyKeys FamilyKeysInApp();

        // The hooks HandleFamilyKey calls. Implement the ones whose flag FamilyKeysInApp returns.
        // number is 1-6, as on the toolbar card: Small icons, Large icons, No text, Text beside,
        // Text under, Text only.
        partial void ApplyToolbarLookKey(int number);
        partial void ToggleToolbarKey();
        partial void ToggleSidebarKey();
        partial void PlaceSidebarKey(SidebarSide side);

        /// <summary>The rows for the family shortcuts an app has, written the same in every app.</summary>
        private static IEnumerable<KsBinding> FamilyKeyRows(FamilyKeys keys)
        {
            if (keys.HasFlag(FamilyKeys.Quit))
                yield return new("Ctrl+Q", "Str_Btn_Quit", KsCat.File, "Q");
            if (keys.HasFlag(FamilyKeys.SidebarToggle))
                yield return new("F9", "Str_KS_ToggleSidebar", KsCat.Nav, "F9");
            if (keys.HasFlag(FamilyKeys.SidebarSides))
                yield return new($"Ctrl+Alt+Shift+{KsBoard.CapLeft} / {KsBoard.CapRight} / {KsBoard.CapUp} / {KsBoard.CapDown}", "Str_KS_SidebarSide",
                    KsCat.Nav, "Left", "Right", "Up", "Down")
                    { CapLabelKeys = ["Str_Sidebar_Left", "Str_Sidebar_Right", "Str_Sidebar_Top", "Str_Sidebar_Bottom"] };
            if (keys.HasFlag(FamilyKeys.ToolbarHide))
                yield return new("Alt+M", "Str_Toolbar_Hide", KsCat.View, "M");
            if (keys.HasFlag(FamilyKeys.ToolbarLook))
                yield return new("Ctrl+Shift+1-6", "Str_KS_ToolbarStyle", KsCat.View, "D1", "D2", "D3", "D4", "D5", "D6")
                    { CapLabelKeys = ["Str_Toolbar_SmallIcons", "Str_Toolbar_LargeIcons", "Str_Toolbar_TextNone",
                                      "Str_Toolbar_TextBeside", "Str_Toolbar_TextUnder", "Str_Toolbar_TextOnly"] };
        }

        /// <summary>
        /// Answers the family shortcuts this app has (FamilyKeysInApp) through the hooks above. Call
        /// from the window's PreviewKeyDown after the app's own text-entry guards; returns true when it
        /// handled the key. Ctrl+Q is QuitShortcut's, so it is not answered here.
        /// </summary>
        internal bool HandleFamilyKey(KeyEventArgs e)
        {
            FamilyKeys keys = FamilyKeysInApp();
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys mods = Keyboard.Modifiers;
            if (keys.HasFlag(FamilyKeys.ToolbarLook) && mods == (ModifierKeys.Control | ModifierKeys.Shift)
                && key is >= Key.D1 and <= Key.D6)
            {
                ApplyToolbarLookKey(key - Key.D0);
                return e.Handled = true;
            }
            if (keys.HasFlag(FamilyKeys.ToolbarHide) && mods == ModifierKeys.Alt && key == Key.M)
            {
                ToggleToolbarKey();
                return e.Handled = true;
            }
            if (keys.HasFlag(FamilyKeys.SidebarToggle) && mods == ModifierKeys.None && key == Key.F9)
            {
                ToggleSidebarKey();
                return e.Handled = true;
            }
            if (keys.HasFlag(FamilyKeys.SidebarSides) && mods == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)
                && key is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                if (!e.IsRepeat)
                    PlaceSidebarKey(key switch
                    {
                        Key.Left => SidebarSide.Left,
                        Key.Right => SidebarSide.Right,
                        Key.Up => SidebarSide.Top,
                        _ => SidebarSide.Bottom,
                    });
                return e.Handled = true;
            }
            return false;
        }

        private static readonly KsBinding[] KsFamilyRows =
        [
            new("Alt+Shift+G", "Str_Theme_FilmGrain", KsCat.View, "G"),

            new("F1",  "Str_KS_ThisList", KsCat.Help, "F1"),
            new("F12", "Str_KS_About",    KsCat.Help, "F12"),
            new("Esc", "Str_KS_Close",    KsCat.Help, "Esc"),
        ];

        private bool _ksMapView;
        private KsLayer _ksLayer;
        private TextBlock? _ksDetail;

        private string KsLoc(string key) => TryFindResource(key) as string ?? key;

        /// <summary>
        /// F1 opens and closes the overlay, F12 the About card, and Esc closes whichever is open.
        /// While the map is showing, holding Ctrl, Shift or Alt previews that layer. Call from the
        /// window's PreviewKeyDown after the app's own text-entry guards; returns true when it
        /// handled the key.
        /// </summary>
        internal bool HandleShortcutsKey(KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            bool open = ShortcutsOverlay.Visibility == Visibility.Visible;

            if (key == Key.F1 && Keyboard.Modifiers == ModifierKeys.None)
            {
                ToggleShortcutsOverlay();
                return e.Handled = true;
            }
            if (key == Key.F12 && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (AboutOverlay.Visibility == Visibility.Visible) FadeOverlayOut(AboutOverlay);
                else
                {
                    if (open) FadeOverlayOut(ShortcutsOverlay);
                    ShowAboutOverlay();
                }
                return e.Handled = true;
            }
            if (key == Key.Escape)
            {
                if (open) { FadeOverlayOut(ShortcutsOverlay); return e.Handled = true; }
                if (AboutOverlay.Visibility == Visibility.Visible) { FadeOverlayOut(AboutOverlay); return e.Handled = true; }
                return false;
            }
            if (open && _ksMapView && key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                                                       or Key.LeftAlt or Key.RightAlt)
            {
                KsLayer held = KsLayers.For(Keyboard.Modifiers);
                if (held != _ksLayer && KsLayerHasRows(held)) { _ksLayer = held; BuildKeyboardMap(); }
                return e.Handled = true;
            }
            return false;
        }

        // ---- Show / hide ----

        private void ToggleShortcutsOverlay()
        {
            if (ShortcutsOverlay.Visibility == Visibility.Visible) { FadeOverlayOut(ShortcutsOverlay); return; }
            // One overlay at a time: About and this one would otherwise stack.
            if (AboutOverlay.Visibility == Visibility.Visible) AboutOverlay.Visibility = Visibility.Collapsed;
            _ksLayer = KsLayer.Base;
            ApplyShortcutView(read: true);
            FadeOverlayIn(ShortcutsOverlay);
        }

        private void ShortcutsButton_Click(object sender, RoutedEventArgs e) => ToggleShortcutsOverlay();
        private void ShortcutsOverlay_Click(object sender, MouseButtonEventArgs e) => FadeOverlayOut(ShortcutsOverlay);
        private void ShortcutsCard_Click(object sender, MouseButtonEventArgs e) => e.Handled = true;
        private void ShortcutsClose_Click(object sender, RoutedEventArgs e) => FadeOverlayOut(ShortcutsOverlay);

        private void KsViewList_Click(object sender, RoutedEventArgs e) => ApplyShortcutView(false, persist: true);
        private void KsViewKeyboard_Click(object sender, RoutedEventArgs e) => ApplyShortcutView(true, persist: true);

        /// <summary>Switches between the list and the map. The choice persists through the
        /// ThemeManager settings hooks, so the view you prefer is the one that opens next time.</summary>
        private void ApplyShortcutView(bool keyboard = false, bool persist = false, bool read = false)
        {
            if (read) keyboard = Services.ThemeManager.GetSetting("ShortcutsMapView") == "1";
            _ksMapView = keyboard;
            if (persist) Services.ThemeManager.SetSetting("ShortcutsMapView", keyboard ? "1" : "0");

            // Toggle the ScrollViewer, not the StackPanel inside it: collapsing the inner panel
            // leaves the scroller in the tree still holding its height.
            KsListHostScroll.Visibility = keyboard ? Visibility.Collapsed : Visibility.Visible;
            KsMapHost.Visibility = keyboard ? Visibility.Visible : Visibility.Collapsed;
            KsViewList.Tag = keyboard ? null : "on";
            KsViewKeyboard.Tag = keyboard ? "on" : null;

            // Sized per view, as KillerPDF does: a keyboard needs the room, and a list at 1080 is a
            // wall of whitespace with the keys on one edge and the descriptions on the other.
            ShortcutCardGrid.MaxWidth = keyboard ? 1080 : 640;

            if (keyboard) BuildKeyboardMap(); else BuildShortcutsList();
        }

        // ---- LIST view ----

        private void BuildShortcutsList()
        {
            KsListHost.Children.Clear();

            var columns = new Grid();
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var left = new StackPanel();
            var right = new StackPanel();
            Grid.SetColumn(left, 0);
            Grid.SetColumn(right, 2);
            columns.Children.Add(left);
            columns.Children.Add(right);

            // Groups in category order, each kept whole, each going to whichever column is shorter
            // so the two stay balanced without splitting a group at an arbitrary row. An action
            // with several keys (a compatibility alias) is one row, its keys joined.
            int leftRows = 0, rightRows = 0;
            foreach (KsCat cat in Enum.GetValues(typeof(KsCat)))
            {
                var rows = new List<(string Keys, string LabelKey)>();
                foreach (var b in KsAll)
                {
                    if (b.Cat != cat) continue;
                    int at = rows.FindIndex(r => r.LabelKey == b.LabelKey);
                    if (at >= 0) rows[at] = (rows[at].Keys + " / " + b.Keys, b.LabelKey);
                    else rows.Add((b.Keys, b.LabelKey));
                }
                if (rows.Count == 0) continue;

                bool toLeft = leftRows <= rightRows;
                var host = toLeft ? left : right;
                if (toLeft) leftRows += rows.Count + 2; else rightRows += rows.Count + 2;

                var header = new TextBlock
                {
                    Text = KsLoc("Str_KS_Grp" + cat),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, host.Children.Count == 0 ? 0 : 14, 0, 6),
                };
                // The list's section headings are the same category colors as the map, which is
                // what ties the two views together.
                header.SetResourceReference(TextBlock.ForegroundProperty, KsPalette.BrushKey(cat));
                host.Children.Add(header);

                foreach (var (keys, labelKey) in rows)
                {
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var keyText = new TextBlock
                    {
                        Text = keys, FontFamily = new FontFamily("Consolas"),
                        FontSize = 11.5, Margin = new Thickness(0, 0, 10, 0), TextWrapping = TextWrapping.Wrap,
                    };
                    keyText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
                    grid.Children.Add(keyText);

                    var label = new TextBlock
                    {
                        Text = KsLoc(labelKey), FontSize = 11.5,
                        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 0),
                    };
                    label.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
                    Grid.SetColumn(label, 1);
                    grid.Children.Add(label);
                    host.Children.Add(grid);
                }
            }
            KsListHost.Children.Add(columns);
        }

        // ---- MAP view ----

        private bool KsLayerHasRows(KsLayer layer)
        {
            if (layer == KsLayer.Base) return true;
            foreach (var b in KsAll) if (b.Layer == layer) return true;
            return false;
        }

        private void BuildKeyboardMap()
        {
            // Layer tabs: BASE plus every layer the table actually uses, in the family's order, so
            // an app never shows an empty layer and every app shows ALT+SHIFT for Film Grain.
            KsLayerTabs.Children.Clear();
            foreach (var (layer, caption) in KsLayers.Tabs)
            {
                if (!KsLayerHasRows(layer)) continue;
                var tab = new Button
                {
                    Content = caption,
                    Style = (Style)FindResource("KsToggle"),
                    Tag = layer == _ksLayer ? "on" : null,
                };
                KsLayer target = layer;
                tab.Click += (_, _) => { _ksLayer = target; BuildKeyboardMap(); };
                KsLayerTabs.Children.Add(tab);
            }

            // The detail line reads out the hovered key at a size you can actually read, so a
            // caption that had to be clipped on a narrow cap is still recoverable.
            if (_ksDetail == null)
            {
                _ksDetail = KsKeyboard.DetailLine();
                KsMapHost.Children.Add(_ksDetail);
            }
            _ksDetail.Text = " ";

            // Which cap ids are lit, and by what, on the layer being shown. The modifier caps that
            // define the layer light too, in the accent, without a binding of their own.
            var lit = new Dictionary<string, KsBinding>();
            foreach (var b in KsAll)
                if (b.Layer == _ksLayer)
                    foreach (var cap in b.Caps) lit[cap] = b;
            var mods = new HashSet<string>(KsLayers.Mods[_ksLayer]);

            KsMapRows.Children.Clear();
            foreach (var row in KsBoard.Rows)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                foreach (var (id, cap, w) in row)
                {
                    if (id.Length == 0)
                    {
                        panel.Children.Add(new Border { Width = KsKeyboard.Unit * w });
                        continue;
                    }
                    Border key = lit.TryGetValue(id, out var b)
                        ? KsKeyboard.Cap(cap, w, b.Cat, KsLoc(b.LabelFor(id)), b.Keys, _ksDetail)
                        : KsKeyboard.Cap(cap, w, null, null, null, null);
                    if (mods.Contains(id)) key.SetResourceReference(Border.BorderBrushProperty, "PrimaryBrush");
                    panel.Children.Add(key);
                }
                KsMapRows.Children.Add(panel);
            }
        }
    }
}
