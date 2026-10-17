#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace KillerFamily.Accessibility
{
    internal static class AccessibleControls
    {
        private static bool _initialized;
        private static readonly IValueConverter TooltipName = new TooltipNameConverter();
        private static readonly IMultiValueConverter RunsName = new RunsNameConverter();
        private static Style? _focusStyle;
        private static Style? _compactFocusStyle;
        private static readonly DependencyProperty OwnedNameProperty = DependencyProperty.RegisterAttached(
            "OwnedName", typeof(BindingBase), typeof(AccessibleControls));
        private static readonly DependencyProperty TrackedTooltipProperty = DependencyProperty.RegisterAttached(
            "TrackedTooltip", typeof(bool), typeof(AccessibleControls), new PropertyMetadata(false));
        private static readonly DependencyProperty TrackedLoadProperty = DependencyProperty.RegisterAttached(
            "TrackedLoad", typeof(bool), typeof(AccessibleControls), new PropertyMetadata(false));
        private static readonly DependencyPropertyDescriptor TooltipDescriptor =
            DependencyPropertyDescriptor.FromProperty(FrameworkElement.ToolTipProperty, typeof(Control));

        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            EventManager.RegisterClassHandler(typeof(Control), FrameworkElement.SizeChangedEvent,
                new SizeChangedEventHandler(OnControlSizeChanged), true);
        }

        private static void OnControlSizeChanged(object sender, SizeChangedEventArgs args)
        {
            if (sender is not Control control) return;
            // A class Loaded handler alone does not make WPF broadcast Loaded to every child.
            // The first layout attaches an instance handler, including for later popup content.
            if (!(bool)control.GetValue(TrackedLoadProperty))
            {
                control.Loaded += OnControlLoaded;
                control.SetValue(TrackedLoadProperty, true);
            }
            Prepare(control);
        }

        private static void OnControlLoaded(object sender, RoutedEventArgs args)
        {
            if (sender is not Control control) return;
            Prepare(control);
            if (control is Window window)
                window.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                    new Action(() => AccessibilityAudit.Record(window)));
        }

        internal static void Prepare(Control control)
        {
            // The focus adorner is shown by WPF for keyboard navigation, without changing layout.
            if (control is not Window && control.Focusable && control.FocusVisualStyle == null)
                control.SetCurrentValue(Control.FocusVisualStyleProperty, IsCompact(control) ? CompactFocusStyle : FocusStyle);

            UpdateName(control);
        }

        private static void UpdateName(Control control)
        {
            var existing = BindingOperations.GetBindingBase(control, AutomationProperties.NameProperty);
            bool owned = existing != null && ReferenceEquals(existing, control.GetValue(OwnedNameProperty));
            if (!owned && IsReadable(AutomationProperties.GetName(control))) return;
            object? content = control is ContentControl button ? button.Content :
                control is HeaderedItemsControl items ? items.Header :
                control is HeaderedContentControl header ? header.Header : null;
            if (!(bool)control.GetValue(TrackedTooltipProperty))
            {
                TooltipDescriptor.AddValueChanged(control, OnTooltipChanged);
                control.Unloaded += OnControlUnloaded;
                control.SetValue(TrackedTooltipProperty, true);
            }
            if (IsReadable(ReadText(content)))
            {
                // WPF names a control from its tooltip before its content, and a rich tooltip
                // names it after its own type ("System.Windows.Controls.TextBlock"). With such a
                // tooltip, the readable content has to be given as the name explicitly.
                if (control.ToolTip == null || control.ToolTip is string)
                {
                    if (owned) BindingOperations.ClearBinding(control, AutomationProperties.NameProperty);
                    control.ClearValue(OwnedNameProperty);
                    return;
                }
                var contentName = new Binding(control is ContentControl ? nameof(ContentControl.Content)
                    : nameof(HeaderedItemsControl.Header)) { Source = control, Converter = TooltipName };
                BindingOperations.SetBinding(control, AutomationProperties.NameProperty, contentName);
                control.SetValue(OwnedNameProperty, contentName);
                return;
            }
            if (!IsReadable(ReadText(control.ToolTip)))
            {
                if (owned) BindingOperations.ClearBinding(control, AutomationProperties.NameProperty);
                control.ClearValue(OwnedNameProperty);
                return;
            }

            // A rich tooltip's localized TextBlock can change without replacing the tooltip. A
            // TextBlock built from Runs keeps its words in the Runs, so the name follows each one.
            TextBlock? label = FindTextBlock(control.ToolTip);
            BindingBase binding;
            if (label != null && string.IsNullOrWhiteSpace(label.Text) && RunsOf(label.Inlines).Any())
            {
                var joined = new MultiBinding { Converter = RunsName };
                foreach (Run run in RunsOf(label.Inlines))
                    joined.Bindings.Add(new Binding(nameof(Run.Text)) { Source = run });
                binding = joined;
            }
            else if (label != null)
                binding = new Binding(nameof(TextBlock.Text)) { Source = label };
            else
                binding = new Binding(nameof(FrameworkElement.ToolTip)) { Source = control, Converter = TooltipName };
            BindingOperations.SetBinding(control, AutomationProperties.NameProperty, binding);
            control.SetValue(OwnedNameProperty, binding);
        }

        private static TextBlock? FindTextBlock(object? value)
        {
            if (value is TextBlock block && IsReadable(BlockText(block))) return block;
            if (value is ContentControl content) return FindTextBlock(content.Content);
            if (value is Panel panel)
                foreach (UIElement child in panel.Children)
                {
                    TextBlock? found = FindTextBlock(child);
                    if (found != null) return found;
                }
            return null;
        }

        private static void OnTooltipChanged(object? sender, EventArgs args)
        {
            if (sender is Control control) UpdateName(control);
        }

        private static void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            if (sender is not Control control) return;
            TooltipDescriptor.RemoveValueChanged(control, OnTooltipChanged);
            control.Unloaded -= OnControlUnloaded;
            control.SetValue(TrackedTooltipProperty, false);
        }

        internal static string ReadText(object? value)
        {
            if (value is string text) return text.Trim();
            if (value is TextBlock block) return BlockText(block).Trim();
            if (value is ContentControl content) return ReadText(content.Content);
            if (value is Panel panel)
                foreach (UIElement child in panel.Children)
                {
                    string label = ReadText(child);
                    if (IsReadable(label)) return label;
                }
            return string.Empty;
        }

        /// <summary>A TextBlock's words, including those it holds in Runs rather than in Text.</summary>
        private static string BlockText(TextBlock block)
        {
            if (!string.IsNullOrWhiteSpace(block.Text)) return block.Text;
            return string.Concat(RunsOf(block.Inlines).Select(run => run.Text));
        }

        private static IEnumerable<Run> RunsOf(InlineCollection inlines)
        {
            foreach (Inline inline in inlines)
            {
                if (inline is Run run) yield return run;
                else if (inline is Span span)
                    foreach (Run inner in RunsOf(span.Inlines)) yield return inner;
            }
        }

        private static bool IsReadable(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("System.Windows.", StringComparison.Ordinal))
                return false;
            foreach (char character in text)
                if (char.IsLetterOrDigit(character) &&
                    CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.PrivateUse)
                    return true;
            return false;
        }

        private static Style FocusStyle => _focusStyle ??= BuildFocusStyle(2);

        /// <summary>
        /// Icon-sized controls such as rail buttons have no room inside for the outline, which
        /// then lands on the glyph itself. Theirs is drawn just outside the control instead.
        /// </summary>
        private static Style CompactFocusStyle => _compactFocusStyle ??= BuildFocusStyle(-3);

        private static bool IsCompact(Control control) =>
            control.ActualWidth > 0 && control.ActualHeight > 0 &&
            (control.ActualWidth < CompactSize || control.ActualHeight < CompactSize);

        private const double CompactSize = 21;

        private static Style BuildFocusStyle(double inset)
        {
            var ring = new FrameworkElementFactory(typeof(FocusRing));
            ring.SetValue(FrameworkElement.MarginProperty, new Thickness(inset));
            ring.SetValue(FocusRing.InsetProperty, inset);
            var template = new ControlTemplate(typeof(Control)) { VisualTree = ring };
            var style = new Style(typeof(Control));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Seal();
            return style;
        }

        private sealed class TooltipNameConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
                ReadText(value);

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
                Binding.DoNothing;
        }

        private sealed class RunsNameConverter : IMultiValueConverter
        {
            public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture) =>
                string.Concat(values.OfType<string>()).Trim();

            public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
                [];
        }
    }

    /// <summary>
    /// The keyboard focus outline: a thin solid ring in the theme accent, rounded like the
    /// controls. When the accent is too faint against the window surfaces to read as an
    /// outline, the ring uses the text color instead, and High Contrast uses the system
    /// highlight. WPF builds a new outline each time focus moves, so it follows theme changes.
    /// </summary>
    internal sealed class FocusRing : Border
    {
        internal static readonly DependencyProperty InsetProperty = DependencyProperty.Register(
            nameof(Inset), typeof(double), typeof(FocusRing), new PropertyMetadata(0.0));

        private const double RingThickness = 2;
        private const double MinimumContrast = 3;

        public FocusRing()
        {
            BorderThickness = new Thickness(RingThickness);
            SnapsToDevicePixels = true;
            IsHitTestVisible = false;
            Loaded += (_, _) => Apply();
        }

        internal double Inset
        {
            get => (double)GetValue(InsetProperty);
            set => SetValue(InsetProperty, value);
        }

        private void Apply()
        {
            BorderBrush = RingBrush();
            double radius = TryFindResource("ControlCornerRadius") is CornerRadius corner ? corner.TopLeft : 0;
            // An outline inside the control sits on a smaller curve, one outside it on a larger one.
            CornerRadius = new CornerRadius(radius <= 0 ? 0 : Math.Max(0, radius - Inset));
        }

        private Brush RingBrush()
        {
            if (SystemParameters.HighContrast) return SystemColors.HighlightBrush;
            Brush? text = TryFindResource("TextBrush") as Brush;
            if (TryFindResource("PrimaryBrush") is SolidColorBrush accent)
            {
                var surfaces = new[] { "BackgroundBrush", "PaneBrush" }
                    .SelectMany(key => SurfaceColors(TryFindResource(key) as Brush)).ToList();
                if (surfaces.All(surface => Contrast(Over(accent.Color, surface), surface) >= MinimumContrast))
                    return accent;
            }
            return text ?? SystemColors.WindowTextBrush;
        }

        private static IEnumerable<Color> SurfaceColors(Brush? brush) => brush switch
        {
            SolidColorBrush solid => new[] { solid.Color },
            GradientBrush gradient => gradient.GradientStops.Select(stop => stop.Color),
            _ => Enumerable.Empty<Color>(),
        };

        private static Color Over(Color top, Color bottom)
        {
            double alpha = top.A / 255.0;
            byte Mix(byte a, byte b) => (byte)Math.Round(a * alpha + b * (1 - alpha));
            return Color.FromRgb(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
        }

        private static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color color)
        {
            double Channel(byte value)
            {
                double c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        }
    }
}
